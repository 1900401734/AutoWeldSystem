using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Data;
using System.Text.Json;

namespace AutoWeldSystem.Services.Production;

public class WeldTaskService : IWeldTaskService
{
    private readonly IMesProvider _mesProvider;
    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IOperationLogService _operationLogService;
    private readonly ILocalizationService _localizer;
    private readonly IUploadTaskService _uploadTaskService;
    private readonly IProductionReportFileService _reportFileService;

    public WeldTaskService(
        SqlSugarDbContext dbContext,
        IMesProvider mesProvider,
        IAppSettingsService settingsService,
        IOperationLogService operationLogService,
        ILocalizationService localizer,
        IUploadTaskService uploadTaskService,
        IProductionReportFileService reportFileService)
    {
        _mesProvider = mesProvider;
        _dbContext = dbContext;
        _settingsService = settingsService;
        _operationLogService = operationLogService;
        _localizer = localizer;
        _uploadTaskService = uploadTaskService;
        _reportFileService = reportFileService;
        CurrentState = new ProductionRuntimeState();
    }

    public ProductionRuntimeState CurrentState { get; }

    public event EventHandler? StateChanged;

    /// <summary>
    /// 同步时间
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MesBaseResponse<MesServerTimeResponse>> SyncServerTimeAsync(CancellationToken cancellationToken = default)
    {
        var response = await _mesProvider.GetServerTimeAsync(cancellationToken);

        if (response.IsSuccess && response.Data is not null && DateTime.TryParse(response.Data.CurrentTime, out var serverTime))
        {
            CurrentState.LastServerSyncTime = serverTime;
            CurrentState.LastServerSyncMessage = $"{serverTime:yyyy-MM-dd HH:mm:ss}";
            _operationLogService.Write("ServerTime", $"Server time sync succeeded: {serverTime:yyyy-MM-dd HH:mm:ss}");
        }
        else
        {
            CurrentState.LastServerSyncMessage = response.Msg;
        }

        NotifyStateChanged();
        return response;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="workId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MesWorkOrderResponse?> GetWorkOrderInfoAsync(
        string workId,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        ResetStationRuntime(normalizedStationNo);
        var response = await _mesProvider.GetWorkOrderInfoAsync(workId, cancellationToken);
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

        var settings = _settingsService.Get();
        var response = await _mesProvider.GetProgramListAsync(
            settings.DeviceId,
            settings.UseProductNumberFilter ? station.CurrentWorkOrder.ProdNum : null,
            cancellationToken);

        if (!response.IsSuccess || response.Data is null)
        {
            station.AvailablePrograms.Clear();
            station.UpdatedTime = DateTime.Now;
            RefreshCompatibilityState(normalizedStationNo);
            NotifyStateChanged();
            return Array.Empty<MesProgramListItemData>();
        }

        station.AvailablePrograms = response.Data;
        station.UpdatedTime = DateTime.Now;
        RefreshCompatibilityState(normalizedStationNo);
        NotifyStateChanged();
        return station.AvailablePrograms;
    }

    public async Task<MesProgramData?> DownloadProgramAsync(
        MesProgramListItemData program,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);
        var settings = _settingsService.Get();
        var response = await _mesProvider.DownloadProgramAsync(settings.DeviceId, program.Id, cancellationToken);
        if (!response.IsSuccess || response.Data is null)
        {
            NotifyStateChanged();
            return null;
        }

        station.SelectedProgram = response.Data;
        station.UpdatedTime = DateTime.Now;
        UpsertProgram(response.Data, settings.DeviceId);
        RefreshCompatibilityState(normalizedStationNo);
        NotifyStateChanged();
        return response.Data;
    }

    public async Task<MesBaseResponse<MesUserInfoResponse>> ValidateMesOperatorAsync(
        string employeeNumber,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);
        var response = await _mesProvider.GetUserInfoAsync(employeeNumber, cancellationToken);
        if (response.IsSuccess)
        {
            station.MesOperatorNumber = employeeNumber;
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
        CancellationToken cancellationToken = default)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);

        EnsureReadyForStart(station);

        var validation = await ValidateMesOperatorAsync(employeeNumber, normalizedStationNo, cancellationToken);
        if (!validation.IsSuccess)
        {
            throw new BusinessOperationException("MES.ValidateOperator", "员工校验失败", validation.Msg);
        }

        var settings = _settingsService.Get();
        var workOrder = station.CurrentWorkOrder!;
        var process = station.SelectedProcess!;
        var program = station.SelectedProgram!;
        var request = new ExpStartRequest
        {
            DeviceId = settings.DeviceId,
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
            ExpStatus = "0",
            ProgramName = program.ProgramName,
            PramaterActual = string.IsNullOrWhiteSpace(program.ProgramContent) ? "{}" : program.ProgramContent
        };

        var response = await _mesProvider.StartWorkAsync(request, cancellationToken);
        if (!response.IsSuccess || response.Data is null)
        {
            throw new BusinessOperationException("MES.StartReport", "开工上报失败", response.Msg);
        }

        var task = new BizWeldTask
        {
            ExpStartId = response.Data.Id,
            StationNo = normalizedStationNo,
            WorkOrderId = workOrder.SN,
            ProductNum = workOrder.ProdNum,
            ProductModel = workOrder.ProdModel,
            Spec = workOrder.Spec,
            Batch = workOrder.Batch,
            ProductName = workOrder.ProductName,
            DrawingNo = workOrder.DrawingNo,
            DeviceId = settings.DeviceId,
            ProcessNo = process.ProcessNo,
            ProcessName = process.ItemName,
            PlannedQty = process.StartAmount,
            ActualQty = actualQty,
            ProgramId = program.Id,
            ProgramName = program.ProgramName,
            StartOperatorNumber = employeeNumber,
            StartTime = DateTime.Now,
            TaskStatus = "Running",
            UploadStatus = settings.UploadMode == UploadMode.Realtime ? "Realtime" : "Pending",
            ProgramContentSnapshot = program.ProgramContent
        };

        task = _dbContext.Db.Insertable(task).ExecuteReturnEntity();
        station.ActiveTask = task;
        station.MesOperatorNumber = employeeNumber;
        station.UpdatedTime = DateTime.Now;
        RefreshCompatibilityState(normalizedStationNo);
        _operationLogService.Write("ExpStart", $"Start report submitted, Station={task.StationNo}, MES Id={task.ExpStartId}, WorkOrder={task.WorkOrderId}");
        NotifyStateChanged();
        return task;
    }

    public async Task<MesBaseResponse<object>> ChangeStatusAsync(
        string statusCode,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);
        if (station.ActiveTask?.ExpStartId is null)
        {
            return new MesBaseResponse<object> { Status = "E", Msg = "No running Task" };
        }

        var response = await _mesProvider.ChangeWorkStatusAsync(new ExpStatusRequest
        {
            ExpStartId = station.ActiveTask.ExpStartId,
            DeviceId = station.ActiveTask.DeviceId,
            ExpStatus = statusCode,
            Ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        }, cancellationToken);

        if (response.IsSuccess)
        {
            station.ActiveTask.TaskStatus = statusCode switch
            {
                "2" => "Paused",
                "1" => "Completed",
                _ => "Running"
            };

            station.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(station.ActiveTask).UpdateColumns(it => new { it.TaskStatus }).ExecuteCommand();
            _operationLogService.Write("ExpStatus", $"Task status changed, Station={normalizedStationNo}, Status={station.ActiveTask.TaskStatus}");
            RefreshCompatibilityState(normalizedStationNo);
            NotifyStateChanged();
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
    public async Task<BizWeldTask> FinishAsync(
        string employeeNumber,
        int actualQty,
        int qualifiedQty,
        int failedQty,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);
        if (station.ActiveTask?.ExpStartId is null || station.SelectedProcess is null)
        {
            throw new BusinessOperationException("MES.FinishReport", "完工上报失败", "No task to finish");
        }

        var task = station.ActiveTask;
        var endOperator = string.IsNullOrWhiteSpace(employeeNumber)
            ? task.StartOperatorNumber ?? station.MesOperatorNumber
            : employeeNumber;

        var response = await _mesProvider.EndWorkAsync(new ExpEndRequest
        {
            ExpStartId = task.ExpStartId,
            DeviceId = task.DeviceId,
            SN = task.WorkOrderId,
            ProcessNo = task.ProcessNo,
            EndTs = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            EndExperID = endOperator,
            ExpStatus = "1",
            WorkHour = Convert.ToDecimal((DateTime.Now - task.StartTime).TotalHours),
            ExpQty = actualQty,
            QualifyNumber = qualifiedQty,
            FailureNumber = failedQty
        }, cancellationToken);

        if (!response.IsSuccess)
        {
            throw new BusinessOperationException("MES.FinishReport", "完工上报失败", response.Msg);
        }

        task.ActualQty = actualQty;
        task.QualifiedQty = qualifiedQty;
        task.FailedQty = failedQty;
        task.EndOperatorNumber = endOperator;
        task.EndTime = DateTime.Now;
        task.TaskStatus = "Completed";
        var settings = _settingsService.Get();
        task.UploadStatus = ResolveUploadStatus(settings.UploadMode);
        task.UploadMessage = ResolveUploadMessage(settings.UploadMode);

        _dbContext.Db.Updateable(task).ExecuteCommand();
        EnqueueFinishUploadTasks(task, settings.UploadMode);
        station.ActiveTask = task;
        station.UpdatedTime = DateTime.Now;
        RefreshCompatibilityState(normalizedStationNo);
        _operationLogService.Write("ExpEnd", $"Finish report submitted, Station={task.StationNo}, WorkOrder={task.WorkOrderId}, UploadStatus={task.UploadStatus}");
        NotifyStateChanged();
        return task;
    }

    public Task RetryPendingUploadsAsync(CancellationToken cancellationToken = default)
    {
        var pendingTasks = _dbContext.Db.Queryable<BizWeldTask>()
            .Where(it => it.TaskStatus == "Completed" && it.UploadStatus != "Uploaded")
            .ToList();

        foreach (var task in pendingTasks)
        {
            task.UploadStatus = "Retrying";
            task.UploadMessage = "Manual retry has been triggered. Process data and report upload integration is reserved for the next step.";
            _dbContext.Db.Updateable(task).ExecuteCommand();
        }

        if (CurrentState.ActiveTask is not null)
        {
            CurrentState.ActiveTask = _dbContext.Db.Queryable<BizWeldTask>().InSingle(CurrentState.ActiveTask.Id);
            CurrentState.SaveCurrentStation();
        }

        _operationLogService.Write("RetryUpload", $"Pending upload retry triggered for {pendingTasks.Count} task(s).");
        NotifyStateChanged();
        return Task.CompletedTask;
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

    public void Reset()
    {
        ResetRuntime(keepSyncMessage: true);
        NotifyStateChanged();
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

    private void UpsertProgram(MesProgramData detail, string deviceId)
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
                ProgramContentJson = detail.ProgramContent,
                ProgramFileBase64 = detail.ProgramFile,
                UpdatedTime = DateTime.Now
            };

            _dbContext.Db.Insertable(entity).ExecuteCommand();
            return;
        }

        entity.ProgramName = detail.ProgramName;
        entity.ProductNum = detail.ProductNum;
        entity.ProgramType = detail.ProgramType;
        entity.ProgramContentJson = detail.ProgramContent;
        entity.ProgramFileBase64 = detail.ProgramFile;
        entity.UpdatedTime = DateTime.Now;
        _dbContext.Db.Updateable(entity).ExecuteCommand();
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

    private static string ResolveUploadStatus(UploadMode mode)
    {
        return mode switch
        {
            UploadMode.Realtime => "Uploaded",
            UploadMode.Quantity => "WaitingQuantityUpload",
            _ => "WaitingBatchUpload"
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
    private void EnqueueFinishUploadTasks(BizWeldTask task, UploadMode uploadMode)
    {
        if (uploadMode != UploadMode.Realtime)
        {
            EnqueueProcessParameterTask(task, uploadMode);
        }

        EnqueueReportFileTask(task, uploadMode);
    }

    private void EnqueueProcessParameterTask(BizWeldTask task, UploadMode uploadMode)
    {
        _uploadTaskService.EnqueueOrUpdate(new BizUploadTask
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

    private void EnqueueReportFileTask(BizWeldTask task, UploadMode uploadMode)
    {
        BizProductionReportFile? reportFile = null;
        string? generationError = null;

        try
        {
            reportFile = _reportFileService.GenerateCsvReport(task);
        }
        catch (Exception ex)
        {
            generationError = ex.Message;
            _operationLogService.Write("ReportFile", $"Report file generation failed, WorkOrder={task.WorkOrderId}, Error={ex.Message}");
        }

        _uploadTaskService.EnqueueOrUpdate(new BizUploadTask
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

    private static string BuildUploadBusinessId(BizWeldTask task, string uploadKind)
    {
        var stableTaskId = string.IsNullOrWhiteSpace(task.ExpStartId)
            ? $"local-{task.Id}"
            : task.ExpStartId.Trim();

        return $"{stableTaskId}:{uploadKind}";
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
            task.DeviceId,
            SN = task.WorkOrderId,
            task.ProductNum,
            task.ProductModel,
            task.Batch,
            task.ProcessNo,
            task.ProcessName,
            task.ActualQty,
            task.QualifiedQty,
            task.FailedQty,
            StartTime = task.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
            EndTime = task.EndTime?.ToString("yyyy-MM-dd HH:mm:ss"),
            OperatorNumber = task.EndOperatorNumber ?? task.StartOperatorNumber
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
        return stationNo <= 0
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
