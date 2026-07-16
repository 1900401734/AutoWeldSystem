using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.DTOs.Upload;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Data;
using System.Text.Json;

namespace AutoWeldSystem.Services.Production;

public class WeldTaskService : IWeldTaskService
{
    private const string TaskStatusCompleted = "Completed";
    private const string TaskStatusRunning = "Running";

    private readonly IMesProvider _mesProvider;
    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IOperationLogService _operationLogService;
    private readonly ILocalizationService _localizer;
    private readonly IUploadTaskService _uploadTaskService;
    private readonly ICenterProductForwardingService _centerProductForwardingService;
    private readonly IProductionReportFileService _reportFileService;
    private readonly IDeviceLifecycleLogService _deviceLifecycleLogService;
    private readonly IDeviceStatusService _deviceStatusService;
    private readonly ISystemClockService _systemClockService;
    private AppSettings _currentSettings;

    public WeldTaskService(
        SqlSugarDbContext dbContext,
        IMesProvider mesProvider,
        IAppSettingsService settingsService,
        IOperationLogService operationLogService,
        ILocalizationService localizer,
        IUploadTaskService uploadTaskService,
        ICenterProductForwardingService centerProductForwardingService,
        IProductionReportFileService reportFileService,
        IDeviceLifecycleLogService deviceLifecycleLogService,
        IDeviceStatusService deviceStatusService,
        ISystemClockService systemClockService)
    {
        _mesProvider = mesProvider;
        _dbContext = dbContext;
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
        _operationLogService = operationLogService;
        _localizer = localizer;
        _uploadTaskService = uploadTaskService;
        _centerProductForwardingService = centerProductForwardingService;
        _reportFileService = reportFileService;
        _deviceLifecycleLogService = deviceLifecycleLogService;
        _deviceStatusService = deviceStatusService;
        _systemClockService = systemClockService;
        CurrentState = new ProductionRuntimeState();
    }

    public ProductionRuntimeState CurrentState { get; }

    public event EventHandler? StateChanged;

    /// <summary>
    /// 统一查询当前工位是否存在未完工任务，先看内存运行态，再查本地数据库兜底。
    /// </summary>
    public BizWeldTask? GetUnfinishedTask(int stationNo = ProductionConstants.Stations.DefaultStationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var stationNumbers = ResolveTaskScopeStationNumbers(normalizedStationNo);
        foreach (var scopedStationNo in stationNumbers)
        {
            var station = GetStation(scopedStationNo);
            if (IsUnfinishedTask(station.ActiveTask))
            {
                return station.ActiveTask;
            }
        }

        var query = _dbContext.Db.Queryable<BizWeldTask>()
            .Where(task => task.TaskStatus != TaskStatusCompleted && task.EndTime == null);
        query = stationNumbers.Length == 1
            ? query.Where(task => task.StationNo == stationNumbers[0])
            : query.Where(task => stationNumbers.Contains(task.StationNo));

        return query
            .OrderByDescending(task => task.StartTime)
            .OrderByDescending(task => task.Id)
            .First();
    }

    /// <summary>
    /// 将本地未完工任务恢复成运行态，供程序重启后继续完工上报。
    /// </summary>
    public BizWeldTask? RestoreUnfinishedTask(int stationNo = ProductionConstants.Stations.DefaultStationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var unfinishedTask = GetUnfinishedTask(normalizedStationNo);
        if (unfinishedTask is null)
        {
            return null;
        }

        var station = GetStation(normalizedStationNo);
        var alreadyRestored = station.ActiveTask?.Id == unfinishedTask.Id;
        if (alreadyRestored)
        {
            // UI 状态刷新会重复检查未完工任务；同一任务已恢复时直接返回，避免递归触发 StateChanged。
            return unfinishedTask;
        }

        var process = CreateProcessSnapshot(unfinishedTask);
        var workOrder = CreateWorkOrderSnapshot(unfinishedTask, process);
        var program = CreateProgramSnapshot(unfinishedTask);
        var operatorNumber = FirstNonEmpty(unfinishedTask.UserNumber, station.MesOperatorNumber);

        ApplyStartedRuntimeState(normalizedStationNo, workOrder, process, program, unfinishedTask, operatorNumber);
        ApplySharedStartedRuntimeStateIfNeeded(normalizedStationNo, workOrder, process, program, unfinishedTask, operatorNumber);
        _operationLogService.Write(
            "TaskRecovery",
            $"Unfinished task restored, Station={unfinishedTask.StationNo}, WorkOrder={unfinishedTask.SN}, MES Id={unfinishedTask.ExpStartId}");

        NotifyStateChanged();
        return unfinishedTask;
    }

    /// <summary>
    /// 同步 MES 服务器时间，并记录最近一次同步结果。
    /// </summary>
    public async Task<BasicRes<ServerTimeRes>> SyncServerTimeAsync(CancellationToken cancellationToken = default)
    {
        var response = await _mesProvider.GetServerTimeAsync(cancellationToken);

        if (!response.IsSuccess || response.Data is null)
        {
            CurrentState.LastServerSyncMessage = response.Msg;
            WriteServerTimeSelfCheckLog(SystemClockSyncResult.Failed(
                default,
                default,
                0,
                string.IsNullOrWhiteSpace(response.Msg) ? "MES 服务器校时接口调用失败。" : response.Msg));
            NotifyStateChanged();
            return response;
        }

        var parseResult = SystemClockSyncRules.TryParseServerTime(response.Data.CurrentTime, out var serverTime);
        if (!parseResult.Success)
        {
            CurrentState.LastServerSyncMessage = parseResult.Message;
            _operationLogService.Write("ServerTime", parseResult.Message, "Error");
            WriteServerTimeSelfCheckLog(parseResult);
            NotifyStateChanged();
            return response;
        }

        var clockResult = SynchronizeSystemClock(serverTime);
        CurrentState.LastServerSyncTime = serverTime;
        CurrentState.LastServerSyncMessage = BuildServerTimeSyncMessage(clockResult);
        WriteServerTimeSyncLog(clockResult);
        WriteServerTimeSelfCheckLog(clockResult);
        NotifyStateChanged();
        return response;
    }

    /// <summary>
    /// Compares the MES server time with the local clock and changes Windows time only when needed.
    /// </summary>
    private SystemClockSyncResult SynchronizeSystemClock(DateTime serverTime)
    {
        var localTimeBefore = _systemClockService.GetLocalTime();
        var decision = SystemClockSyncRules.Decide(serverTime, localTimeBefore);
        if (!decision.Changed)
        {
            return decision;
        }

        try
        {
            return _systemClockService.SetLocalTime(serverTime, localTimeBefore);
        }
        catch (Exception ex)
        {
            return SystemClockSyncResult.Failed(
                serverTime,
                localTimeBefore,
                decision.OffsetSeconds,
                $"系统时间修改失败：{ex.Message}");
        }
    }

    /// <summary>
    /// Builds the runtime message displayed by monitor screens after startup time sync.
    /// </summary>
    private static string BuildServerTimeSyncMessage(SystemClockSyncResult result)
    {
        var status = result.Success
            ? result.Changed ? "已校时" : "无需校时"
            : "校时失败";
        return $"{status}：服务器时间={result.ServerTime:yyyy-MM-dd HH:mm:ss}，本机原时间={result.LocalTimeBefore:yyyy-MM-dd HH:mm:ss}，偏差={result.OffsetSeconds:F3} 秒。{result.Message}";
    }

    /// <summary>
    /// Writes a compact audit record for server-time synchronization.
    /// </summary>
    private void WriteServerTimeSyncLog(SystemClockSyncResult result)
    {
        var level = result.Success ? "Info" : "Error";
        _operationLogService.Write(
            "ServerTime",
            $"ServerTime={result.ServerTime:yyyy-MM-dd HH:mm:ss}, LocalBefore={result.LocalTimeBefore:yyyy-MM-dd HH:mm:ss}, OffsetSeconds={result.OffsetSeconds:F3}, Changed={result.Changed}, Success={result.Success}, Message={result.Message}",
            level);
    }

    /// <summary>
    /// Writes MES server-time synchronization as a startup self-check device lifecycle log.
    /// Device log failures are swallowed because they must not block startup or time sync.
    /// </summary>
    private void WriteServerTimeSelfCheckLog(SystemClockSyncResult result)
    {
        try
        {
            _deviceLifecycleLogService.Write(DeviceLifecycleLogRules.CreateServerTimeSelfCheckEntry(
                _currentSettings.DeviceId,
                result,
                DateTime.Now));
        }
        catch (Exception ex)
        {
            _operationLogService.Write("ServerTime", $"设备自检日志写入失败：{ex.Message}", "Warning");
        }
    }

    /// <summary>
    /// 获取工单信息
    /// </summary>
    /// <param name="workId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<WorkOrderRes?> GetWorkOrderInfoAsync(
        string workId,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedStationNo = NormalizeStationNo(stationNo);
        ResetStationRuntime(normalizedStationNo);
        var response = await _mesProvider.GetWorkOrderInfoAsync(workId, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!response.IsSuccess || response.Data is null)
        {
            CurrentState.LastServerSyncMessage = response.Msg;
            RefreshCompatibilityState(normalizedStationNo);
            NotifyStateChanged();
            return null;
        }

        var station = GetStation(normalizedStationNo);
        station.CurrentWorkOrder = response.Data;
        station.UpdatedTime = DateTime.Now;
        RefreshCompatibilityState(normalizedStationNo);
        _operationLogService.Write("WorkOrder", $"Work order loaded, Station={normalizedStationNo}, SN={response.Data.SN}");
        NotifyStateChanged();
        return response.Data;
    }

    public void SelectStation(int stationNo)
    {
        CurrentState.SaveCurrentStation();
        CurrentState.RestoreStation(stationNo);
        NotifyStateChanged();
    }

    public void SelectProcess(ExpItemData process, int stationNo = ProductionConstants.Stations.DefaultStationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);
        station.SelectedProcess = process;
        station.AvailablePrograms.Clear();
        station.SelectedProgram = null;
        station.UpdatedTime = DateTime.Now;
        RefreshCompatibilityState(normalizedStationNo);
        NotifyStateChanged();
    }

    public async Task<IReadOnlyList<MesProgramListItemData>> LoadProgramsAsync(
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);
        if (station.CurrentWorkOrder is null)
        {
            return Array.Empty<MesProgramListItemData>();
        }

        var settings = CurrentSettings;
        // 未开启“按产品工号筛选程序”时返回本地同步的全量程序；开启后按工单产品工号在客户端筛选。
        var response = await _mesProvider.GetProgramListAsync(
            settings.DeviceId,
            null,
            cancellationToken);

        if (!response.IsSuccess || response.Data is null)
        {
            station.AvailablePrograms.Clear();
            station.UpdatedTime = DateTime.Now;
            RefreshCompatibilityState(normalizedStationNo);
            NotifyStateChanged();
            return Array.Empty<MesProgramListItemData>();
        }

        var workOrderProdNum = station.CurrentWorkOrder?.ProdNum;
        station.AvailablePrograms = ProgramListFilterRules.Filter(
            response.Data,
            settings.UseProductNumberFilter,
            workOrderProdNum).ToList();
        station.UpdatedTime = DateTime.Now;
        RefreshCompatibilityState(normalizedStationNo);
        NotifyStateChanged();
        return station.AvailablePrograms;
    }

    public async Task<ProgramDataRes?> DownloadProgramAsync(
        MesProgramListItemData program,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);
        var settings = CurrentSettings;
        var response = await _mesProvider.DownloadProgramAsync(settings.DeviceId, program.Id, cancellationToken);
        if (!response.IsSuccess || response.Data is null)
        {
            NotifyStateChanged();
            return null;
        }

        var detail = MergeProgramListSnapshot(response.Data, program);
        var localProgram = UpsertProgram(detail, settings.DeviceId);
        detail.RecipeCode = FirstNonEmpty(localProgram?.RecipeCode, detail.RecipeCode);
        station.SelectedProgram = detail;
        station.UpdatedTime = DateTime.Now;
        RefreshCompatibilityState(normalizedStationNo);
        NotifyStateChanged();
        return detail;
    }

    public void ApplyStartAdjustment(
        WorkOrderRes workOrder,
        ExpItemData? process,
        ProgramDataRes program,
        int stationNo = ProductionConstants.Stations.DefaultStationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);
        if (station.ActiveTask is not null)
        {
            throw new BusinessOperationException("StartAdjustment", "开工信息调整失败", "当前工位已生成生产任务，不能再调整开工信息。");
        }

        station.CurrentWorkOrder = CloneWorkOrder(workOrder);
        if (process is not null)
        {
            station.SelectedProcess = CloneProcess(process);
        }

        station.SelectedProgram = CloneProgram(program);

        station.UpdatedTime = DateTime.Now;
        RefreshCompatibilityState(normalizedStationNo);
        _operationLogService.Write(
            "StartAdjustment",
            $"Start data adjusted locally, Station={normalizedStationNo}, SN={workOrder.SN}, ProductNumber={workOrder.ProdNum}, ProgramName={program.ProgramName}, Recipe={program.RecipeCode}");
        NotifyStateChanged();
    }

    public async Task<BasicRes<UserInfoRes>> ValidateMesOperatorAsync(
        string employeeNumber,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);
        var response = await _mesProvider.GetUserInfoAsync(employeeNumber, cancellationToken);
        if (response.IsSuccess)
        {
            station.MesOperatorInfo = CreateOperatorInfo(response.Data, employeeNumber);
            station.MesOperatorNumber = station.MesOperatorInfo.UserNumber;
            station.UpdatedTime = DateTime.Now;
            RefreshCompatibilityState(normalizedStationNo);
        }

        NotifyStateChanged();
        return response;
    }

    public async Task<BizWeldTask> StartAsync(
        string employeeNumber,
        int actualQty,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        bool employeeAlreadyValidated = false,
        CancellationToken cancellationToken = default)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);

        EnsureNoUnfinishedTask(normalizedStationNo);
        EnsureReadyForStart(station);

        if (!employeeAlreadyValidated)
        {
            var validation = await ValidateMesOperatorAsync(employeeNumber, normalizedStationNo, cancellationToken);
            if (!validation.IsSuccess)
            {
                throw new BusinessOperationException("MES.ValidateOperator", "员工校验失败", validation.Msg);
            }
        }

        var settings = CurrentSettings;
        var workOrder = station.CurrentWorkOrder!;
        var process = station.SelectedProcess!;
        var program = station.SelectedProgram!;
        var operatorInfo = CreateOperatorInfo(station.MesOperatorInfo, employeeNumber);
        var startOperatorNumber = FirstNonEmpty(operatorInfo.UserNumber, employeeNumber);
        var request = BuildStartRequest(settings.DeviceId, workOrder, process, program, actualQty, startOperatorNumber);

        var response = await _mesProvider.StartWorkAsync(request, cancellationToken);
        if (!response.IsSuccess || response.Data is null)
        {
            throw new BusinessOperationException("MES.StartReport", "开工上报失败", response.Msg);
        }

        var task = new BizWeldTask
        {
            LocalExpStartId = CreateLocalTaskGuid(),
            ExpStartId = response.Data.Id,
            StationNo = normalizedStationNo,
            SN = workOrder.SN,
            ProductNum = workOrder.ProdNum,
            ProductModel = workOrder.ProdModel,
            Spec = workOrder.Spec,
            Batch = workOrder.Batch,
            ProductName = workOrder.ProductName,
            DrawingNo = workOrder.DrawingNo,
            DeviceId = settings.DeviceId,
            ProcessNo = process.ProcessNo,
            ProcessName = process.ItemName,
            StartAmount = process.StartAmount,
            ActualQty = actualQty,
            ProgramId = program.Id,
            ProgramName = program.ProgramName,
            RecipeCode = ResolveProgramRecipeCode(program, settings.DeviceId),
            UserNumber = startOperatorNumber,
            UserName = operatorInfo.UserName,
            DeptName = operatorInfo.DeptName,
            TeamName = operatorInfo.TeamName,
            StartTime = DateTime.Now,
            TaskStatus = TaskStatusRunning,
            UploadStatus = settings.UploadMode == UploadMode.Realtime ? "Realtime" : "Pending",
            ProgramContentSnapshot = program.ProgramContent
        };

        task = _dbContext.Db.Insertable(task).ExecuteReturnEntity();
        ApplyStartedRuntimeState(normalizedStationNo, workOrder, process, program, task, startOperatorNumber);
        ApplySharedStartedRuntimeStateIfNeeded(normalizedStationNo, workOrder, process, program, task, startOperatorNumber);
        _operationLogService.Write("ExpStart", $"Start report submitted, Station={task.StationNo}, MES Id={task.ExpStartId}, WorkOrder={task.SN}");
        WriteTestProgramRunningLog(task);
        await RecordProgramStartedStatusAsync(task, cancellationToken);
        NotifyStateChanged();
        return task;
    }

    /// <summary>
    /// Creates a local running task and queues the MES start report for later retry.
    /// </summary>
    public async Task<BizWeldTask> StartLocalAsync(
        OfflineExperimentStartReq request,
        string operatorNumber,
        int actualQty,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NormalizeLocalRequest(request);

        var normalizedStationNo = NormalizeStationNo(request.StationNo);
        EnsureNoUnfinishedTask(normalizedStationNo);

        var settings = CurrentSettings;
        var workOrder = CreateLocalWorkOrder(request);
        var process = CreateLocalProcess(request);
        var program = CreateLocalProgram(request, settings.DeviceId);
        var localOperatorNumber = ResolveLocalOperatorNumber(operatorNumber);
        var localOperatorInfo = CreateLocalOperatorInfo(localOperatorNumber);
        var startRequest = BuildStartRequest(settings.DeviceId, workOrder, process, program, actualQty, localOperatorNumber);

        var task = new BizWeldTask
        {
            LocalExpStartId = CreateLocalTaskGuid(),
            IsOfflineCreated = true,
            StationNo = normalizedStationNo,
            SN = workOrder.SN,
            ProductNum = workOrder.ProdNum,
            ProductModel = workOrder.ProdModel,
            Spec = workOrder.Spec,
            Batch = workOrder.Batch,
            ProductName = workOrder.ProductName,
            DrawingNo = workOrder.DrawingNo,
            DeviceId = settings.DeviceId,
            ProcessNo = process.ProcessNo,
            ProcessName = process.ItemName,
            StartAmount = process.StartAmount,
            ActualQty = actualQty,
            ProgramId = program.Id,
            ProgramName = program.ProgramName,
            RecipeCode = request.RecipeCode,
            UserNumber = localOperatorNumber,
            UserName = localOperatorInfo.UserName,
            DeptName = localOperatorInfo.DeptName,
            TeamName = localOperatorInfo.TeamName,
            StartTime = DateTime.Now,
            TaskStatus = TaskStatusRunning,
            UploadStatus = ProductionConstants.UploadStatuses.Pending,
            UploadMessage = "Local task created offline. Start report is queued for MES retry.",
            ProgramContentSnapshot = program.ProgramContent
        };

        task = _dbContext.Db.Insertable(task).ExecuteReturnEntity();
        ApplyStartedRuntimeState(normalizedStationNo, workOrder, process, program, task, localOperatorNumber);
        ApplySharedStartedRuntimeStateIfNeeded(normalizedStationNo, workOrder, process, program, task, localOperatorNumber);
        EnqueueStartReportTask(task, startRequest);
        EnqueueWorkOrderStatusTask(task, ProductionConstants.MesWorkOrderStatuses.StartedOrRestarted);

        _operationLogService.Write("LocalExpStart", $"Local task started, Station={task.StationNo}, WorkOrder={task.SN}, Recipe={task.RecipeCode}");
        await RecordProgramStartedStatusAsync(task, cancellationToken);
        NotifyStateChanged();
        return task;
    }

    public async Task<BasicRes<object>> ChangeStatusAsync(
        string statusCode,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);
        if (!IsWorkOrderStatusReportEnabled())
        {
            if (station.ActiveTask is null)
            {
                return new BasicRes<object> { Status = AppConstants.MesStatus.Error, Msg = "No running Task" };
            }

            ApplyWorkOrderStatusLocalState(station, normalizedStationNo, statusCode);
            return new BasicRes<object>
            {
                Status = AppConstants.MesStatus.Success,
                Msg = "Work-order status report is disabled in system settings."
            };
        }

        if (station.ActiveTask?.ExpStartId is null)
        {
            if (station.ActiveTask is not null)
            {
                EnqueueWorkOrderStatusTask(station.ActiveTask, statusCode);
                return new BasicRes<object>
                {
                    Status = AppConstants.MesStatus.Success,
                    Msg = "Work-order status is queued for MES retry."
                };
            }

            return new BasicRes<object> { Status = AppConstants.MesStatus.Error, Msg = "No running Task" };
        }

        var request = BuildStatusRequest(station.ActiveTask, statusCode);
        var response = await _mesProvider.ChangeWorkStatusAsync(request, cancellationToken);
        if (!response.IsSuccess)
        {
            EnqueueWorkOrderStatusTask(station.ActiveTask, statusCode);
        }

        if (response.IsSuccess)
        {
            ApplyWorkOrderStatusLocalState(station, normalizedStationNo, statusCode);
        }

        return response;
    }

    /// <summary>
    /// 完工上报
    /// </summary>
    /// <param name="employeeNumber"></param>
    /// <param name="actualQty"></param>
    /// <param name="qualifiedQty"></param>
    /// <param name="failedQty"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<BizWeldTask> FinishAsync(string employeeNumber, int actualQty, int qualifiedQty, int failedQty,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);
        var task = IsUnfinishedTask(station.ActiveTask)
            ? station.ActiveTask!
            : RestoreUnfinishedTask(normalizedStationNo);

        if (task?.ExpStartId is null)
        {
            throw new BusinessOperationException("MES.FinishReport", "完工上报失败", "No task to finish");
        }

        var endOperator = string.IsNullOrWhiteSpace(employeeNumber)
            ? task.UserNumber ?? station.MesOperatorNumber
            : employeeNumber;
        // 完工请求和本地任务必须共享同一个结束时间，避免报表与 MES 时间出现毫秒级漂移。
        var finishTime = DateTime.Now;

        var finishRequest = new ExperimentEndReq
        {
            ExpStartId = task.ExpStartId,
            DeviceId = task.DeviceId,
            SN = task.SN,
            ProcessNo = task.ProcessNo,
            EndTs = finishTime.ToString("yyyy-MM-dd HH:mm:ss"),
            EndExperID = endOperator,
            ExpStatus = "1",
            WorkHour = Convert.ToDecimal((finishTime - task.StartTime).TotalHours),
            ExpQty = actualQty,
            QualifyNumber = qualifiedQty,
            FailureNumber = failedQty
        };

        BasicRes<object> response;
        try
        {
            response = await _mesProvider.EndWorkAsync(finishRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            // MES failures must not block local completion. Keep the reason in the retry task.
            response = new BasicRes<object>()
            {
                Status = "F",
                Msg = ex.Message
            };
        }

        var finishUploaded = response.IsSuccess;
        var finishUploadMessage = finishUploaded
            ? (string.IsNullOrWhiteSpace(response.Msg) ? "Finish report uploaded." : response.Msg)
            : $"Finish report failed and was queued for retry: {response.Msg}";

        if (!finishUploaded)
        {
            _operationLogService.Write("ExpEnd", $"Finish report failed before retry queue, Station={task.StationNo}, WorkOrder={task.SN}, Message={response.Msg}");
        }

        task.ActualQty = actualQty;
        task.QualifiedQty = qualifiedQty;
        task.FailedQty = failedQty;
        task.EndOperatorNumber = endOperator;
        task.EndTime = finishTime;
        task.TaskStatus = TaskStatusCompleted;
        var settings = CurrentSettings;
        task.UploadStatus = finishUploaded
            ? ResolveUploadStatus(settings.UploadMode)
            : ProductionConstants.UploadStatuses.Pending;
        task.UploadMessage = finishUploaded
            ? ResolveUploadMessage(settings.UploadMode)
            : finishUploadMessage;

        _dbContext.Db.Updateable(task).ExecuteCommand();
        _centerProductForwardingService.EnqueueTaskFinishUpdate(task);
        if (finishUploaded)
        {
            await RecordProgramEndedStatusAsync(task, cancellationToken);
        }

        var finishReportTask = EnqueueFinishReportTask(
            task,
            finishRequest,
            finishUploaded ? ProductionConstants.UploadStatuses.Uploaded : ProductionConstants.UploadStatuses.Pending,
            finishUploadMessage);
        var uploadTasks = EnqueueFinishUploadTasks(task, settings.UploadMode).ToList();
        if (!finishUploaded)
        {
            uploadTasks.Add(finishReportTask);
        }

        await ExecuteFinishUploadTasksAsync(task, uploadTasks, cancellationToken);
        ApplyFinishedRuntimeState(normalizedStationNo, task);
        _operationLogService.Write("ExpEnd", $"Finish report handled, Station={task.StationNo}, WorkOrder={task.SN}, UploadStatus={task.UploadStatus}, FinishUploaded={finishUploaded}");
        NotifyStateChanged();
        return task;
    }

    /// <summary>
    /// Completes an offline-created task locally and queues all MES uploads for recovery.
    /// </summary>
    public async Task<BizWeldTask> FinishLocalAsync(
        string employeeNumber,
        int actualQty,
        int qualifiedQty,
        int failedQty,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);
        var task = IsUnfinishedTask(station.ActiveTask)
            ? station.ActiveTask!
            : RestoreUnfinishedTask(normalizedStationNo);

        if (task is null || !task.IsOfflineCreated)
        {
            throw new BusinessOperationException("Local.FinishReport", "本地完工失败", "No offline task to finish.");
        }

        var endOperator = ResolveLocalOperatorNumber(employeeNumber);
        // 离线完工同样只捕获一次结束时间，持久化后再生成最终报表。
        var finishTime = DateTime.Now;
        task.ActualQty = actualQty;
        task.QualifiedQty = qualifiedQty;
        task.FailedQty = failedQty;
        task.EndOperatorNumber = endOperator;
        task.EndTime = finishTime;
        task.TaskStatus = TaskStatusCompleted;
        task.UploadStatus = ProductionConstants.UploadStatuses.Pending;
        task.UploadMessage = "Local finish completed offline. Finish data is queued for MES retry.";

        _dbContext.Db.Updateable(task).ExecuteCommand();
        _centerProductForwardingService.EnqueueTaskFinishUpdate(task);
        EnqueueFinishReportTask(task, BuildEndRequest(task, endOperator, actualQty, qualifiedQty, failedQty));
        EnqueueWorkOrderStatusTask(task, ProductionConstants.MesWorkOrderStatuses.Completed);
        EnqueueFinishUploadTasks(task, CurrentSettings.UploadMode);

        ApplyFinishedRuntimeState(normalizedStationNo, task);
        _operationLogService.Write("LocalExpEnd", $"Local task finished, Station={task.StationNo}, WorkOrder={task.SN}, UploadStatus={task.UploadStatus}");
        await RecordProgramEndedStatusAsync(task, cancellationToken);
        NotifyStateChanged();
        return task;
    }

    public Task RetryPendingUploadsAsync(CancellationToken cancellationToken = default)
    {
        return RetryPendingUploadsInternalAsync(cancellationToken);
    }

    public void UpdateProgramContent(string content, int stationNo = ProductionConstants.Stations.DefaultStationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);
        if (station.SelectedProgram is null)
        {
            return;
        }

        station.SelectedProgram.ProgramContent = content;
        station.UpdatedTime = DateTime.Now;
        RefreshCompatibilityState(normalizedStationNo);
        NotifyStateChanged();
    }

    /// <summary>
    /// Updates a task recipe code after PLC confirmation and keeps every runtime station that references the task in sync.
    /// </summary>
    public bool TryUpdateRecipeCode(
        int taskId,
        string recipeCode,
        int stationNo = ProductionConstants.Stations.DefaultStationNo)
    {
        var normalizedRecipeCode = NormalizeText(recipeCode);
        if (taskId <= 0 || string.IsNullOrWhiteSpace(normalizedRecipeCode))
        {
            return false;
        }

        var task = _dbContext.Db.Queryable<BizWeldTask>().InSingle(taskId);
        if (task is null)
        {
            return false;
        }

        task.RecipeCode = normalizedRecipeCode;
        _dbContext.Db.Updateable(task)
            .UpdateColumns(it => new { it.RecipeCode })
            .ExecuteCommand();

        // Multiple stations can hold the same task instance in dual-station same-work-order mode.
        foreach (var station in CurrentState.StationStates.Values)
        {
            if (station.ActiveTask?.Id != taskId)
            {
                continue;
            }

            station.ActiveTask.RecipeCode = normalizedRecipeCode;
            if (station.SelectedProgram is not null)
            {
                station.SelectedProgram.RecipeCode = normalizedRecipeCode;
            }

            station.UpdatedTime = DateTime.Now;
        }

        RefreshCompatibilityState(stationNo);
        NotifyStateChanged();
        return true;
    }

    public void Reset()
    {
        ResetRuntime(keepSyncMessage: true);
        NotifyStateChanged();
    }

    private async Task RetryPendingUploadsInternalAsync(CancellationToken cancellationToken)
    {
        var executedCount = 0;
        executedCount += await _uploadTaskService.ExecuteAllPendingAsync(ProductionConstants.UploadTaskTypes.StartReport, cancellationToken);
        executedCount += await _uploadTaskService.ExecuteAllPendingAsync(ProductionConstants.UploadTaskTypes.ProcessParameter, cancellationToken);
        executedCount += await _uploadTaskService.ExecuteAllPendingAsync(ProductionConstants.UploadTaskTypes.ReportFile, cancellationToken);
        executedCount += await _uploadTaskService.ExecuteAllPendingAsync(ProductionConstants.UploadTaskTypes.FinishReport, cancellationToken);

        if (CurrentState.ActiveTask is not null)
        {
            CurrentState.ActiveTask = _dbContext.Db.Queryable<BizWeldTask>().InSingle(CurrentState.ActiveTask.Id);
            CurrentState.SaveCurrentStation();
        }

        _operationLogService.Write("RetryUpload", $"Pending upload retry executed for {executedCount} task(s).");
        NotifyStateChanged();
    }

    private static ExperimentStartReq BuildStartRequest(
        string deviceId,
        WorkOrderRes workOrder,
        ExpItemData process,
        ProgramDataRes program,
        int actualQty,
        string employeeNumber)
    {
        return new ExperimentStartReq
        {
            DeviceId = deviceId,
            SN = workOrder.SN,
            ProductNum = workOrder.ProdNum,
            ProductName = workOrder.ProductName,
            DrawingNo = workOrder.DrawingNo,
            Batch = workOrder.Batch,
            Qty = process.StartAmount,
            ProcessNo = process.ProcessNo,
            ItemName = process.ItemName,
            ExpQty = actualQty,
            StartTs = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            StartExperID = employeeNumber,
            ExpStatus = ProductionConstants.MesWorkOrderStatuses.StartedOrRestarted,
            ProgramName = program.ProgramName,
            PramaterActual = string.IsNullOrWhiteSpace(program.ProgramContent) ? "{}" : program.ProgramContent
        };
    }

    private static ExperimentEndReq BuildEndRequest(
        BizWeldTask task,
        string employeeNumber,
        int actualQty,
        int qualifiedQty,
        int failedQty)
    {
        var endTime = task.EndTime ?? DateTime.Now;
        return new ExperimentEndReq
        {
            ExpStartId = task.ExpStartId ?? string.Empty,
            DeviceId = task.DeviceId,
            SN = task.SN,
            ProcessNo = task.ProcessNo,
            EndTs = endTime.ToString("yyyy-MM-dd HH:mm:ss"),
            EndExperID = employeeNumber,
            ExpStatus = ProductionConstants.MesWorkOrderStatuses.Completed,
            WorkHour = Convert.ToDecimal((endTime - task.StartTime).TotalHours),
            ExpQty = actualQty,
            QualifyNumber = qualifiedQty,
            FailureNumber = failedQty
        };
    }

    private static ReportExperimentStatusReq BuildStatusRequest(BizWeldTask task, string statusCode)
    {
        return new ReportExperimentStatusReq
        {
            ExpStartId = task.ExpStartId ?? string.Empty,
            DeviceId = task.DeviceId,
            ExpStatus = statusCode,
            Ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    private static WorkOrderRes CreateLocalWorkOrder(OfflineExperimentStartReq request)
    {
        var process = CreateLocalProcess(request);
        return new WorkOrderRes
        {
            SN = request.WorkOrderId,
            ProdNum = request.ProductNum,
            ProdModel = request.ProductModel,
            Spec = request.Spec,
            Batch = request.Batch,
            ProductName = request.ProductName,
            DrawingNo = request.DrawingNo,
            ProjectFrom = "Local",
            ExpItems = [process]
        };
    }

    private static ExpItemData CreateLocalProcess(OfflineExperimentStartReq request)
    {
        return new ExpItemData
        {
            ItemId = request.ProgramLocalId,
            ItemName = request.ProcessName,
            ProcessNo = request.ProcessNo,
            StartAmount = Math.Max(1, request.PlannedQty)
        };
    }

    private static ProgramDataRes CreateLocalProgram(OfflineExperimentStartReq request, string deviceId)
    {
        return new ProgramDataRes
        {
            Id = request.ProgramId,
            ProgramName = request.ProgramName,
            DeviceId = deviceId,
            ProductNum = request.ProductNum,
            RecipeCode = request.RecipeCode,
            ProgramType = request.ProgramType,
            ProgramContent = string.IsNullOrWhiteSpace(request.ProgramContent) ? "{}" : request.ProgramContent
        };
    }

    private void ApplyStartedRuntimeState(
        int stationNo,
        WorkOrderRes workOrder,
        ExpItemData process,
        ProgramDataRes program,
        BizWeldTask task,
        string operatorNumber)
    {
        var station = GetStation(stationNo);
        station.CurrentWorkOrder = CloneWorkOrder(workOrder);
        station.SelectedProcess = CloneProcess(process);
        station.SelectedProgram = program;
        station.AvailablePrograms = CreateProgramListSnapshot(task);
        station.ActiveTask = task;
        station.MesOperatorInfo = CreateTaskOperatorInfo(task, operatorNumber);
        station.MesOperatorNumber = station.MesOperatorInfo?.UserNumber ?? operatorNumber;
        station.UpdatedTime = DateTime.Now;
        RefreshCompatibilityState(stationNo);
    }

    /// <summary>
    /// 双工位同工单只创建一个任务，但两个工位都要持有同一个运行任务，用于各自预览和采集。
    /// </summary>
    private void ApplySharedStartedRuntimeStateIfNeeded(
        int sourceStationNo,
        WorkOrderRes workOrder,
        ExpItemData process,
        ProgramDataRes program,
        BizWeldTask task,
        string operatorNumber)
    {
        if (!IsDualStationSameWorkOrder())
        {
            return;
        }

        var normalizedSourceStationNo = NormalizeStationNo(sourceStationNo);
        foreach (var stationNo in GetDualStationNumbers())
        {
            if (stationNo == normalizedSourceStationNo)
            {
                continue;
            }

            ApplyStartedRuntimeState(stationNo, workOrder, process, program, task, operatorNumber);
        }
    }

    /// <summary>
    /// 同工单模式下，完工一次即结束共享任务，两个工位运行态都需要同步到已完工状态。
    /// </summary>
    private void ApplyFinishedRuntimeState(int sourceStationNo, BizWeldTask task)
    {
        foreach (var stationNo in ResolveTaskScopeStationNumbers(sourceStationNo))
        {
            var station = GetStation(stationNo);
            WeldTaskRuntimeRules.ClearFinishedTask(station, task);
        }

        RefreshCompatibilityState(CurrentState.CurrentStationNo);
    }

    private BizUploadTask EnqueueStartReportTask(BizWeldTask task, ExperimentStartReq request)
    {
        ExperimentStartRequestRules.ApplyOfflineStartId(task, request);

        return _uploadTaskService.EnqueueOrUpdate(new BizUploadTask
        {
            TaskType = ProductionConstants.UploadTaskTypes.StartReport,
            Target = ProductionConstants.UploadTargets.Mes,
            BusinessId = BuildUploadBusinessId(task, "start-report"),
            WeldTaskId = task.Id,
            PayloadJson = JsonSerializer.Serialize(new
            {
                TaskType = ProductionConstants.UploadTaskTypes.StartReport,
                WeldTaskId = task.Id,
                task.StationNo,
                task.SN,
                task.ProductNum,
                task.RecipeCode,
                Request = request
            }),
            Status = ProductionConstants.UploadStatuses.Pending,
            NextRetryTime = DateTime.Now,
            Message = "Start report is queued for MES retry."
        });
    }

    private BizUploadTask EnqueueFinishReportTask(
        BizWeldTask task,
        ExperimentEndReq request,
        string status = ProductionConstants.UploadStatuses.Pending,
        string? message = null)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(status)
            ? ProductionConstants.UploadStatuses.Pending
            : status.Trim();
        var isUploaded = string.Equals(normalizedStatus, ProductionConstants.UploadStatuses.Uploaded, StringComparison.OrdinalIgnoreCase);

        return _uploadTaskService.EnqueueOrUpdate(new BizUploadTask
        {
            TaskType = ProductionConstants.UploadTaskTypes.FinishReport,
            Target = ProductionConstants.UploadTargets.Mes,
            BusinessId = BuildUploadBusinessId(task, "finish-report"),
            WeldTaskId = task.Id,
            PayloadJson = JsonSerializer.Serialize(new
            {
                TaskType = ProductionConstants.UploadTaskTypes.FinishReport,
                WeldTaskId = task.Id,
                task.StationNo,
                task.SN,
                task.ProductNum,
                task.RecipeCode,
                Request = request
            }),
            Status = normalizedStatus,
            NextRetryTime = isUploaded ? null : DateTime.Now,
            CompletedTime = isUploaded ? DateTime.Now : null,
            Message = message ?? (isUploaded
                ? "Finish report uploaded."
                : "Finish report is queued for MES retry.")
        });
    }

    private BizUploadTask? EnqueueWorkOrderStatusTask(BizWeldTask task, string statusCode)
    {
        if (!IsWorkOrderStatusReportEnabled())
        {
            return null;
        }

        return _uploadTaskService.EnqueueOrUpdate(new BizUploadTask
        {
            TaskType = ProductionConstants.UploadTaskTypes.WorkOrderStatus,
            Target = ProductionConstants.UploadTargets.Mes,
            BusinessId = BuildUploadBusinessId(task, $"work-status-{statusCode}"),
            WeldTaskId = task.Id,
            PayloadJson = JsonSerializer.Serialize(new
            {
                TaskType = ProductionConstants.UploadTaskTypes.WorkOrderStatus,
                WeldTaskId = task.Id,
                task.StationNo,
                task.SN,
                task.ProductNum,
                StatusCode = statusCode,
                Request = BuildStatusRequest(task, statusCode)
            }),
            Status = ProductionConstants.UploadStatuses.Pending,
            NextRetryTime = DateTime.Now,
            Message = "Work-order status is queued for MES retry."
        });
    }

    private static void NormalizeLocalRequest(OfflineExperimentStartReq request)
    {
        request.StationNo = NormalizeStationNo(request.StationNo);
        request.WorkOrderId = NormalizeText(request.WorkOrderId);
        request.Batch = NormalizeText(request.Batch);
        request.Spec = NormalizeText(request.Spec);
        request.ProcessNo = NormalizeText(request.ProcessNo);
        request.ProcessName = NormalizeText(request.ProcessName);
        request.ProgramId = NormalizeText(request.ProgramId);
        request.ProgramName = NormalizeText(request.ProgramName);
        request.ProgramType = NormalizeText(request.ProgramType);
        request.ProgramContent = string.IsNullOrWhiteSpace(request.ProgramContent) ? "{}" : request.ProgramContent.Trim();
        request.ProductNum = NormalizeText(request.ProductNum);
        request.ProductModel = NormalizeText(request.ProductModel);
        request.ProductName = NormalizeText(request.ProductName);
        request.DrawingNo = NormalizeText(request.DrawingNo);
        request.RecipeCode = NormalizeText(request.RecipeCode);
        request.PlannedQty = Math.Max(1, request.PlannedQty);

        if (string.IsNullOrWhiteSpace(request.WorkOrderId))
        {
            throw new BusinessOperationException("Local.StartReport", "本地开工失败", "Local work order number is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ProgramName) || string.IsNullOrWhiteSpace(request.RecipeCode))
        {
            throw new BusinessOperationException("Local.StartReport", "本地开工失败", "Local program and recipe code are required.");
        }
    }

    private static string ResolveLocalOperatorNumber(string? operatorNumber)
    {
        var currentUserNumber = GlobalContext.CurrentUser?.UserNumber;
        return FirstNonEmpty(operatorNumber, currentUserNumber, Environment.UserName, "local");
    }

    /// <summary>
    /// Resolves the recipe code from the local program record selected by the MES program ID.
    /// </summary>
    private string ResolveProgramRecipeCode(ProgramDataRes program, string deviceId)
    {
        _dbContext.InitDatabase();
        var programId = NormalizeText(program.Id);
        var localProgram = !string.IsNullOrWhiteSpace(programId)
            ? _dbContext.Db.Queryable<BizProgram>()
                .Where(item => item.ProgramId == programId)
                .ToList()
                .OrderByDescending(item => SameText(item.DeviceId, deviceId))
                .FirstOrDefault()
            : null;

        return FirstNonEmpty(localProgram?.RecipeCode, program.RecipeCode);
    }

    private void EnsureReadyForStart(ProductionStationRuntimeState station)
    {
        if (station.CurrentWorkOrder is null)
        {
            throw new BusinessOperationException("MES.StartReport", "开工上报失败", "No work order available");
        }

        if (station.SelectedProcess is null)
        {
            throw new BusinessOperationException("MES.StartReport", "开工上报失败", "No process selected");
        }

        if (station.SelectedProgram is null)
        {
            throw new BusinessOperationException("MES.StartReport", "开工上报失败", "No program downloaded");
        }

    }

    /// <summary>
    /// 开工前的服务层硬拦截，避免 UI 之外的调用绕过“同工位只能有一个未完工任务”的规则。
    /// </summary>
    private void EnsureNoUnfinishedTask(int stationNo)
    {
        var unfinishedTask = GetUnfinishedTask(stationNo);
        if (unfinishedTask is null)
        {
            return;
        }

        throw new BusinessOperationException(
            "MES.StartReport",
            _localizer.GetString(TextKeys.Monitor.Message.StartBlockedByUnfinishedTask),
            BuildUnfinishedTaskDetail(unfinishedTask));
    }

    /// <summary>
    /// EndTime 有值或状态为 Completed 都视为已完工；上传状态不参与开工拦截。
    /// </summary>
    private static bool IsUnfinishedTask(BizWeldTask? task)
    {
        return task is not null
            && task.EndTime is null
            && !string.Equals(task.TaskStatus, TaskStatusCompleted, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildUnfinishedTaskDetail(BizWeldTask task)
    {
        return $"Station={task.StationNo}; WorkOrder={task.SN}; MES Id={task.ExpStartId}; Status={task.TaskStatus}; StartTime={task.StartTime:yyyy-MM-dd HH:mm:ss}";
    }

    /// <summary>
    /// 使用本地任务快照重建工单对象，让重启后的界面可以继续显示原工单信息。
    /// </summary>
    private static WorkOrderRes CreateWorkOrderSnapshot(BizWeldTask task, ExpItemData process)
    {
        return new WorkOrderRes
        {
            SN = task.SN,
            ProdNum = task.ProductNum,
            ProdModel = task.ProductModel,
            Spec = task.Spec,
            Batch = task.Batch,
            ProductName = task.ProductName,
            DrawingNo = task.DrawingNo,
            ExpItems = [process]
        };
    }

    /// <summary>
    /// 使用本地任务快照重建工序对象；完工上报只需要工序号和工序名称继续保持一致。
    /// </summary>
    private static ExpItemData CreateProcessSnapshot(BizWeldTask task)
    {
        return new ExpItemData
        {
            ProcessNo = task.ProcessNo,
            ItemName = task.ProcessName,
            StartAmount = Math.Max(1, task.StartAmount)
        };
    }

    /// <summary>
    /// 使用本地任务快照重建程序对象，避免重启后必须重新下载程序才能完工。
    /// </summary>
    private static ProgramDataRes CreateProgramSnapshot(BizWeldTask task)
    {
        return new ProgramDataRes
        {
            Id = task.ProgramId ?? string.Empty,
            ProgramName = task.ProgramName ?? string.Empty,
            DeviceId = task.DeviceId,
            ProductNum = task.ProductNum,
            RecipeCode = task.RecipeCode ?? string.Empty,
            ProgramContent = string.IsNullOrWhiteSpace(task.ProgramContentSnapshot)
                ? "{}"
                : task.ProgramContentSnapshot
        };
    }

    private static List<MesProgramListItemData> CreateProgramListSnapshot(BizWeldTask task)
    {
        if (string.IsNullOrWhiteSpace(task.ProgramId) && string.IsNullOrWhiteSpace(task.ProgramName))
        {
            return [];
        }

        return
        [
            new MesProgramListItemData
            {
                Id = task.ProgramId ?? string.Empty,
                ProgramName = task.ProgramName ?? string.Empty,
                DeviceId = task.DeviceId,
                ProductNum = task.ProductNum
            }
        ];
    }

    /// <summary>
    /// MES 的程序详情接口可能只返回文件内容，列表接口中的程序工号、类型等信息需要保留下来。
    /// </summary>
    private static ProgramDataRes MergeProgramListSnapshot(ProgramDataRes detail, MesProgramListItemData snapshot)
    {
        detail.Id = FirstNonEmpty(detail.Id, snapshot.Id);
        detail.ProgramName = FirstNonEmpty(detail.ProgramName, snapshot.ProgramName);
        detail.ProductNum = FirstNonEmpty(detail.ProductNum, snapshot.ProductNum);
        detail.ProgramType = FirstNonEmpty(detail.ProgramType, snapshot.ProgramType);
        detail.DeviceId = FirstNonEmpty(detail.DeviceId, snapshot.DeviceId);
        return detail;
    }

    private static string FirstNonEmpty(params string?[] values)
        => NormalizeText(values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)));

    /// <summary>
    /// 统一规范 MES 员工信息。MES 偶发不返回员工号时，用用户输入的工号兜底。
    /// </summary>
    private static UserInfoRes CreateOperatorInfo(UserInfoRes? source, string fallbackUserNumber)
    {
        return new UserInfoRes
        {
            UserNumber = FirstNonEmpty(source?.UserNumber, fallbackUserNumber),
            UserName = NormalizeText(source?.UserName),
            DeptName = NormalizeText(source?.DeptName),
            TeamName = NormalizeText(source?.TeamName)
        };
    }

    /// <summary>
    /// 离线工单无法校验 MES 员工，使用本地系统用户作为员工快照。
    /// </summary>
    private static UserInfoRes CreateLocalOperatorInfo(string operatorNumber)
    {
        var currentUser = GlobalContext.CurrentUser;
        return new UserInfoRes
        {
            UserNumber = FirstNonEmpty(operatorNumber, currentUser?.UserNumber, Environment.UserName, "local"),
            UserName = FirstNonEmpty(currentUser?.UserName, Environment.UserName),
            DeptName = string.Empty,
            TeamName = string.Empty
        };
    }

    /// <summary>
    /// 从已入库的任务快照恢复员工信息，供软件重启后回填 MonitorView。
    /// </summary>
    private static UserInfoRes? CreateTaskOperatorInfo(BizWeldTask task, string? fallbackUserNumber)
    {
        var userNumber = FirstNonEmpty(task.UserNumber, fallbackUserNumber);
        var userName = NormalizeText(task.UserName);
        var deptName = NormalizeText(task.DeptName);
        var teamName = NormalizeText(task.TeamName);
        if (string.IsNullOrWhiteSpace(userNumber)
            && string.IsNullOrWhiteSpace(userName)
            && string.IsNullOrWhiteSpace(deptName)
            && string.IsNullOrWhiteSpace(teamName))
        {
            return null;
        }

        return new UserInfoRes
        {
            UserNumber = userNumber,
            UserName = userName,
            DeptName = deptName,
            TeamName = teamName
        };
    }

    private static bool SameText(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private BizProgram UpsertProgram(ProgramDataRes detail, string deviceId)
    {
        var entity = _dbContext.Db.Queryable<BizProgram>().First(it => it.ProgramId == detail.Id && it.DeviceId == deviceId);

        if (entity is null)
        {
            entity = new BizProgram
            {
                ProgramId = detail.Id,
                ProgramName = detail.ProgramName,
                ProductNum = detail.ProductNum,
                DeviceId = deviceId,
                ProgramType = detail.ProgramType,
                RecipeCode = NormalizeText(detail.RecipeCode),
                ProgramContent = detail.ProgramContent,
                ProgramFile = detail.ProgramFile,
                UpdatedTime = DateTime.Now
            };

            return _dbContext.Db.Insertable(entity).ExecuteReturnEntity();
        }

        entity.ProgramName = detail.ProgramName;
        entity.ProductNum = detail.ProductNum;
        entity.ProgramType = detail.ProgramType;
        entity.RecipeCode = FirstNonEmpty(detail.RecipeCode, entity.RecipeCode);
        entity.ProgramContent = detail.ProgramContent;
        entity.ProgramFile = detail.ProgramFile;
        entity.UpdatedTime = DateTime.Now;
        _dbContext.Db.Updateable(entity).ExecuteCommand();
        return entity;
    }

    private static WorkOrderRes CloneWorkOrder(WorkOrderRes source)
    {
        return new WorkOrderRes
        {
            SN = NormalizeText(source.SN),
            ProdNum = NormalizeText(source.ProdNum),
            ProdModel = NormalizeText(source.ProdModel),
            Spec = NormalizeText(source.Spec),
            Batch = NormalizeText(source.Batch),
            ProductName = NormalizeText(source.ProductName),
            DrawingNo = NormalizeText(source.DrawingNo),
            ProjectFrom = NormalizeText(source.ProjectFrom),
            ExpItems = (source.ExpItems ?? []).Select(CloneProcess).ToList()
        };
    }

    private static ExpItemData CloneProcess(ExpItemData source)
    {
        return new ExpItemData
        {
            ItemId = source.ItemId,
            ItemTitle = source.ItemTitle,
            ItemCont = source.ItemCont,
            SequenceNo = source.SequenceNo,
            ItemName = NormalizeText(source.ItemName),
            ProcessNo = NormalizeText(source.ProcessNo),
            StartAmount = source.StartAmount
        };
    }

    /// <summary>
    /// 复制开工确认后的程序快照，避免界面继续编辑时影响已经保存的运行态。
    /// </summary>
    private static ProgramDataRes CloneProgram(ProgramDataRes source)
    {
        return new ProgramDataRes
        {
            Id = NormalizeText(source.Id),
            ProgramName = NormalizeText(source.ProgramName),
            DeviceId = NormalizeText(source.DeviceId),
            ProgramContent = string.IsNullOrWhiteSpace(source.ProgramContent) ? "{}" : source.ProgramContent.Trim(),
            ProgramType = string.IsNullOrWhiteSpace(source.ProgramType) ? "0" : source.ProgramType.Trim(),
            ProductNum = NormalizeText(source.ProductNum),
            ProgramFile = source.ProgramFile ?? string.Empty,
            Remark = source.Remark ?? string.Empty,
            RecipeCode = NormalizeText(source.RecipeCode)
        };
    }

    private static string NormalizeText(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private void ResetRuntime(bool keepSyncMessage)
    {
        var lastSyncTime = CurrentState.LastServerSyncTime;
        var lastSyncMessage = CurrentState.LastServerSyncMessage;
        ResetStationRuntime(CurrentState.CurrentStationNo);
        if (keepSyncMessage)
        {
            CurrentState.LastServerSyncTime = lastSyncTime;
            CurrentState.LastServerSyncMessage = lastSyncMessage;
        }
    }

    /// <summary>
    /// 获取指定工位的运行状态，工位不存在时由运行状态对象自动创建。
    /// </summary>
    private ProductionStationRuntimeState GetStation(int stationNo)
    {
        return CurrentState.GetOrCreateStation(NormalizeStationNo(stationNo));
    }

    /// <summary>
    /// 清空指定工位的业务上下文，不影响其它工位。
    /// </summary>
    private void ResetStationRuntime(int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);
        station.Reset();
        RefreshCompatibilityState(normalizedStationNo);
    }

    /// <summary>
    /// 如果被更新的是当前界面正在查看的工位，则刷新旧版兼容属性，保持现有 MonitorView 不需要立即改造。
    /// </summary>
    private void RefreshCompatibilityState(int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        if (CurrentState.CurrentStationNo == normalizedStationNo)
        {
            CurrentState.RestoreStation(normalizedStationNo);
        }
    }

    /// <summary>
    /// 单工位和双工位双工单只看当前工位；双工位同工单需要把工位1/2视为同一个任务范围。
    /// </summary>
    private int[] ResolveTaskScopeStationNumbers(int stationNo)
    {
        return IsDualStationSameWorkOrder()
            ? GetDualStationNumbers()
            : [NormalizeStationNo(stationNo)];
    }

    private bool IsDualStationSameWorkOrder()
    {
        var settings = CurrentSettings;
        return settings.EnableDualStation && !settings.EnableDualWorkOrder;
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private bool IsWorkOrderStatusReportEnabled()
        => CurrentSettings.EnableWorkOrderStatusReport != false;

    private void ApplyWorkOrderStatusLocalState(
        ProductionStationRuntimeState station,
        int stationNo,
        string statusCode)
    {
        if (station.ActiveTask is null)
        {
            return;
        }

        station.ActiveTask.TaskStatus = statusCode switch
        {
            "2" => "Paused",
            "1" => TaskStatusCompleted,
            _ => TaskStatusRunning
        };

        station.UpdatedTime = DateTime.Now;
        _dbContext.Db.Updateable(station.ActiveTask)
            .UpdateColumns(it => new { it.TaskStatus })
            .ExecuteCommand();
        _operationLogService.Write("ExpStatus", $"Task status changed, Station={stationNo}, Status={station.ActiveTask.TaskStatus}");
        RefreshCompatibilityState(stationNo);
        NotifyStateChanged();
    }

    /// <summary>
    /// Writes the independent device log for a successful MES start report.
    /// </summary>
    private void WriteTestProgramRunningLog(BizWeldTask task)
    {
        _deviceLifecycleLogService.Write(DeviceLifecycleLogRules.CreateTestProgramRunningEntry(
            task.DeviceId,
            task.StationNo,
            FirstNonEmpty(task.ExpStartId, task.LocalExpStartId),
            task.SN,
            DateTime.Now));
    }

    private Task RecordProgramStartedStatusAsync(BizWeldTask task, CancellationToken cancellationToken)
    {
        return _deviceStatusService.ChangeStatusAsync(
            ProductionConstants.MesDeviceStatuses.ProgramStarted,
            DeviceStatusReportRules.AppendStationRemark(
                DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.ProgramStarted),
                task.StationNo),
            task.IsOfflineCreated ? "Local" : "MES",
            stationNo: task.StationNo,
            weldTaskId: task.Id,
            workOrderId: task.SN,
            cancellationToken: cancellationToken);
    }

    private Task RecordProgramEndedStatusAsync(BizWeldTask task, CancellationToken cancellationToken)
    {
        return _deviceStatusService.ChangeStatusAsync(
            ProductionConstants.MesDeviceStatuses.ProgramEnded,
            DeviceStatusReportRules.AppendStationRemark(
                DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.ProgramEnded),
                task.StationNo),
            task.IsOfflineCreated ? "Local" : "MES",
            stationNo: task.StationNo,
            weldTaskId: task.Id,
            workOrderId: task.SN,
            cancellationToken: cancellationToken);
    }

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }

    private static int[] GetDualStationNumbers()
    {
        return [1, 2];
    }

    private static string ResolveUploadStatus(UploadMode mode)
    {
        return mode switch
        {
            UploadMode.Realtime => ProductionConstants.UploadStatuses.Uploaded,
            UploadMode.Quantity => ProductionConstants.UploadStatuses.Pending,
            _ => ProductionConstants.UploadStatuses.Pending
        };
    }

    private string ResolveUploadMessage(UploadMode mode)
    {
        return mode switch
        {
            UploadMode.Realtime => "Realtime upload",
            UploadMode.Quantity => "Quantity upload",
            _ => "Batch upload"
        };
    }

    /// <summary>
    /// 完工上报成功后创建本地上传任务。
    /// 当前只负责任务排队，真实上传执行器后续按任务类型逐步接入。
    /// </summary>
    private IReadOnlyList<BizUploadTask> EnqueueFinishUploadTasks(BizWeldTask task, UploadMode uploadMode)
    {
        return new[]
        {
            EnqueueProcessParameterTask(task, uploadMode),
            EnqueueReportFileTask(task, uploadMode)
        };
    }

    private BizUploadTask EnqueueProcessParameterTask(BizWeldTask task, UploadMode uploadMode)
    {
        return _uploadTaskService.EnqueueOrUpdate(new BizUploadTask
        {
            TaskType = ProductionConstants.UploadTaskTypes.ProcessParameter,
            Target = ProductionConstants.UploadTargets.Mes,
            BusinessId = BuildUploadBusinessId(task, "process-parameter"),
            WeldTaskId = task.Id,
            PayloadJson = BuildUploadPayload(task, uploadMode, ProductionConstants.UploadTaskTypes.ProcessParameter),
            Status = ProductionConstants.UploadStatuses.Pending,
            NextRetryTime = DateTime.Now,
            Message = $"{GetUploadModeName(uploadMode)}模式完工后排队，等待过程参数上传执行器处理。"
        });
    }

    private BizUploadTask EnqueueReportFileTask(BizWeldTask task, UploadMode uploadMode)
    {
        BizProductionReportFile? reportFile = null;
        string? generationError = null;

        try
        {
            reportFile = _reportFileService.GenerateXlsxReport(task);
        }
        catch (Exception ex)
        {
            generationError = ex.Message;
            _operationLogService.Write("ReportFile", $"Report file generation failed, WorkOrder={task.SN}, Error={ex.Message}");
        }

        return _uploadTaskService.EnqueueOrUpdate(new BizUploadTask
        {
            TaskType = ProductionConstants.UploadTaskTypes.ReportFile,
            Target = ProductionConstants.UploadTargets.Mes,
            BusinessId = BuildUploadBusinessId(task, "report-file"),
            WeldTaskId = task.Id,
            PayloadJson = BuildUploadPayload(task, uploadMode, ProductionConstants.UploadTaskTypes.ReportFile),
            FilePath = reportFile?.FilePath,
            Status = reportFile is null ? ProductionConstants.UploadStatuses.Failed : ProductionConstants.UploadStatuses.Pending,
            NextRetryTime = DateTime.Now,
            Message = reportFile is null
                ? $"报告文件生成失败：{generationError}"
                : "报告文件已生成，等待上传执行器处理。"
        });
    }

    /// <summary>
    /// 完工后立即尝试执行本次任务产生的上传任务。
    /// 网络或 MES 异常时任务会保留在上传状态页，供用户恢复后手动重试。
    /// </summary>
    private async Task ExecuteFinishUploadTasksAsync(
        BizWeldTask task,
        IReadOnlyList<BizUploadTask> uploadTasks,
        CancellationToken cancellationToken)
    {
        var summaries = new List<UploadTaskSummary>();
        foreach (var uploadTask in uploadTasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var summary = await _uploadTaskService.ExecuteAsync(uploadTask.Id, cancellationToken);
            if (summary is not null)
            {
                summaries.Add(summary);
            }
        }

        UpdateTaskUploadState(task, summaries);
    }

    private void UpdateTaskUploadState(BizWeldTask task, IReadOnlyList<UploadTaskSummary> uploadSummaries)
    {
        if (uploadSummaries.Count == 0)
        {
            return;
        }

        var failedTasks = uploadSummaries
            .Where(summary => summary.Status == ProductionConstants.UploadStatuses.Failed)
            .ToList();
        var allUploaded = uploadSummaries.All(summary => summary.Status == ProductionConstants.UploadStatuses.Uploaded);

        task.UploadStatus = allUploaded
            ? ProductionConstants.UploadStatuses.Uploaded
            : failedTasks.Count > 0
                ? ProductionConstants.UploadStatuses.Failed
                : ProductionConstants.UploadStatuses.Pending;
        task.UploadMessage = allUploaded
            ? "完工后上传任务已全部完成。"
            : failedTasks.Count > 0
                ? $"完工后仍有 {failedTasks.Count} 个上传任务失败，请在上传状态页重试。"
                : "完工后上传任务已排队，请在上传状态页查看进度。";

        _dbContext.Db.Updateable(task)
            .UpdateColumns(it => new { it.UploadStatus, it.UploadMessage })
            .Where(it => it.Id == task.Id)
            .ExecuteCommand();
    }

    private static string BuildUploadBusinessId(BizWeldTask task, string uploadKind)
    {
        var stableTaskId = FirstNonEmpty(
            task.ExpStartId,
            task.LocalExpStartId,
            task.Id.ToString("x").PadLeft(32, '0'));

        return $"{stableTaskId}:{uploadKind}";
    }

    private static string CreateLocalTaskGuid()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static string BuildUploadPayload(BizWeldTask task, UploadMode uploadMode, string taskType)
    {
        return JsonSerializer.Serialize(new
        {
            TaskType = taskType,
            UploadMode = uploadMode.ToString(),
            WeldTaskId = task.Id,
            task.StationNo,
            task.ExpStartId,
            task.IsOfflineCreated,
            task.DeviceId,
            SN = task.SN,
            task.ProductNum,
            task.ProductModel,
            task.RecipeCode,
            task.Batch,
            task.ProcessNo,
            task.ProcessName,
            task.ActualQty,
            task.QualifiedQty,
            task.FailedQty,
            StartTime = task.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
            EndTime = task.EndTime?.ToString("yyyy-MM-dd HH:mm:ss"),
            OperatorNumber = task.EndOperatorNumber ?? task.UserNumber
        });
    }

    private static string GetUploadModeName(UploadMode mode)
    {
        return mode switch
        {
            UploadMode.Realtime => "单件实时上传",
            UploadMode.Quantity => "特定数量上传",
            _ => "整批上传"
        };
    }

    private static int NormalizeStationNo(int stationNo)
    {
        return stationNo <= 0 ? ProductionConstants.Stations.DefaultStationNo : stationNo;
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
