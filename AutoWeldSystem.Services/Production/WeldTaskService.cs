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
    /// 获取工单信息
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

        var detail = MergeProgramListSnapshot(response.Data, program);
        station.SelectedProgram = detail;
        station.UpdatedTime = DateTime.Now;
        UpsertProgram(detail, settings.DeviceId);
        RefreshCompatibilityState(normalizedStationNo);
        NotifyStateChanged();
        return detail;
    }

    public void ApplyStartAdjustment(
        MesWorkOrderResponse workOrder,
        ExpItemData? process,
        string programContent,
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

        if (station.SelectedProgram is not null)
        {
            station.SelectedProgram.ProgramContent = string.IsNullOrWhiteSpace(programContent)
                ? "{}"
                : programContent.Trim();
        }

        station.UpdatedTime = DateTime.Now;
        RefreshCompatibilityState(normalizedStationNo);
        _operationLogService.Write("StartAdjustment", $"Start data adjusted locally, Station={normalizedStationNo}, SN={workOrder.SN}, ProductNum={workOrder.ProdNum}");
        NotifyStateChanged();
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
        bool employeeAlreadyValidated = false,
        CancellationToken cancellationToken = default)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var station = GetStation(normalizedStationNo);

        EnsureReadyForStart(station);

        if (!employeeAlreadyValidated)
        {
            var validation = await ValidateMesOperatorAsync(employeeNumber, normalizedStationNo, cancellationToken);
            if (!validation.IsSuccess)
            {
                throw new BusinessOperationException("MES.ValidateOperator", "员工校验失败", validation.Msg);
            }
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
        var uploadTasks = EnqueueFinishUploadTasks(task, settings.UploadMode);
        await ExecuteFinishUploadTasksAsync(task, uploadTasks, cancellationToken);
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

    /// <summary>
    /// MES 的程序详情接口可能只返回文件内容，列表接口中的程序工号、类型等信息需要保留下来。
    /// </summary>
    private static MesProgramData MergeProgramListSnapshot(MesProgramData detail, MesProgramListItemData snapshot)
    {
        detail.Id = FirstNonEmpty(detail.Id, snapshot.Id);
        detail.ProgramName = FirstNonEmpty(detail.ProgramName, snapshot.ProgramName);
        detail.ProductNum = FirstNonEmpty(detail.ProductNum, snapshot.ProductNum);
        detail.ProgramType = FirstNonEmpty(detail.ProgramType, snapshot.ProgramType);
        detail.DeviceId = FirstNonEmpty(detail.DeviceId, snapshot.DeviceId);
        return detail;
    }

    private static string FirstNonEmpty(string? primary, string? fallback)
        => string.IsNullOrWhiteSpace(primary) ? NormalizeText(fallback) : NormalizeText(primary);

    private BizProgram UpsertProgram(MesProgramData detail, string deviceId)
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

            return _dbContext.Db.Insertable(entity).ExecuteReturnEntity();
        }

        entity.ProgramName = detail.ProgramName;
        entity.ProductNum = detail.ProductNum;
        entity.ProgramType = detail.ProgramType;
        entity.ProgramContentJson = detail.ProgramContent;
        entity.ProgramFileBase64 = detail.ProgramFile;
        entity.UpdatedTime = DateTime.Now;
        _dbContext.Db.Updateable(entity).ExecuteCommand();
        return entity;
    }

    private static MesWorkOrderResponse CloneWorkOrder(MesWorkOrderResponse source)
    {
        return new MesWorkOrderResponse
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
            ItemID = source.ItemID,
            ItemTitle = source.ItemTitle,
            ItemCont = source.ItemCont,
            SequenceNo = source.SequenceNo,
            ItemName = NormalizeText(source.ItemName),
            ProcessNo = NormalizeText(source.ProcessNo),
            StartAmount = source.StartAmount
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
            reportFile = _reportFileService.GenerateCsvReport(task);
        }
        catch (Exception ex)
        {
            generationError = ex.Message;
            _operationLogService.Write("ReportFile", $"Report file generation failed, WorkOrder={task.WorkOrderId}, Error={ex.Message}");
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
