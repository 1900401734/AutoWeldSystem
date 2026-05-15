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
    public async Task<MesWorkOrderResponse?> GetWorkOrderInfoAsync(string workId, CancellationToken cancellationToken = default)
    {
        ResetRuntime(keepSyncMessage: true);
        var response = await _mesProvider.GetWorkOrderInfoAsync(workId, cancellationToken);
        if (!response.IsSuccess || response.Data is null)
        {
            CurrentState.LastServerSyncMessage = response.Msg;
            NotifyStateChanged();
            return null;
        }

        CurrentState.CurrentWorkOrder = response.Data;
        _operationLogService.Write("WorkOrder", $"Work order loaded: {response.Data.SN}");
        NotifyStateChanged();
        return response.Data;
    }

    public void SelectProcess(ExpItemData process)
    {
        CurrentState.SelectedProcess = process;
        CurrentState.AvailablePrograms.Clear();
        CurrentState.SelectedProgram = null;
        NotifyStateChanged();
    }

    public async Task<IReadOnlyList<MesProgramListItemData>> LoadProgramsAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentState.CurrentWorkOrder is null)
        {
            return Array.Empty<MesProgramListItemData>();
        }

        var settings = _settingsService.Get();
        var response = await _mesProvider.GetProgramListAsync(
            settings.DeviceId,
            settings.UseProductNumberFilter ? CurrentState.CurrentWorkOrder.ProdNum : null,
            cancellationToken);

        if (!response.IsSuccess || response.Data is null)
        {
            CurrentState.AvailablePrograms.Clear();
            NotifyStateChanged();
            return Array.Empty<MesProgramListItemData>();
        }

        CurrentState.AvailablePrograms = response.Data;
        NotifyStateChanged();
        return CurrentState.AvailablePrograms;
    }

    public async Task<MesProgramData?> DownloadProgramAsync(MesProgramListItemData program, CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.Get();
        var response = await _mesProvider.DownloadProgramAsync(settings.DeviceId, program.Id, cancellationToken);
        if (!response.IsSuccess || response.Data is null)
        {
            NotifyStateChanged();
            return null;
        }

        CurrentState.SelectedProgram = response.Data;
        UpsertProgram(response.Data, settings.DeviceId);
        NotifyStateChanged();
        return response.Data;
    }

    public async Task<MesBaseResponse<MesUserInfoResponse>> ValidateMesOperatorAsync(string employeeNumber, CancellationToken cancellationToken = default)
    {
        var response = await _mesProvider.GetUserInfoAsync(employeeNumber, cancellationToken);
        if (response.IsSuccess)
        {
            CurrentState.MesOperatorNumber = employeeNumber;
        }

        NotifyStateChanged();
        return response;
    }

    public async Task<BizWeldTask> StartAsync(string employeeNumber, int actualQty, CancellationToken cancellationToken = default)
    {
        EnsureReadyForStart();

        var validation = await ValidateMesOperatorAsync(employeeNumber, cancellationToken);
        if (!validation.IsSuccess)
        {
            throw new BusinessOperationException("MES.ValidateOperator", "员工校验失败", validation.Msg);
        }

        var settings = _settingsService.Get();
        var workOrder = CurrentState.CurrentWorkOrder!;
        var process = CurrentState.SelectedProcess!;
        var program = CurrentState.SelectedProgram!;
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
        CurrentState.ActiveTask = task;
        CurrentState.MesOperatorNumber = employeeNumber;
        _operationLogService.Write("ExpStart", $"Start report submitted, MES Id={task.ExpStartId}, WorkOrder={task.WorkOrderId}");
        NotifyStateChanged();
        return task;
    }

    public async Task<MesBaseResponse<object>> ChangeStatusAsync(string statusCode, CancellationToken cancellationToken = default)
    {
        if (CurrentState.ActiveTask?.ExpStartId is null)
        {
            return new MesBaseResponse<object> { Status = "E", Msg = "No running Task" };
        }

        var response = await _mesProvider.ChangeWorkStatusAsync(new ExpStatusRequest
        {
            ExpStartId = CurrentState.ActiveTask.ExpStartId,
            DeviceId = CurrentState.ActiveTask.DeviceId,
            ExpStatus = statusCode,
            Ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        }, cancellationToken);

        if (response.IsSuccess)
        {
            CurrentState.ActiveTask.TaskStatus = statusCode switch
            {
                "2" => "Paused",
                "1" => "Completed",
                _ => "Running"
            };

            _dbContext.Db.Updateable(CurrentState.ActiveTask).UpdateColumns(it => new { it.TaskStatus }).ExecuteCommand();
            _operationLogService.Write("ExpStatus", $"Task status changed to {CurrentState.ActiveTask.TaskStatus}");
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
    public async Task<BizWeldTask> FinishAsync(string employeeNumber, int actualQty, int qualifiedQty, int failedQty, CancellationToken cancellationToken = default)
    {
        if (CurrentState.ActiveTask?.ExpStartId is null || CurrentState.SelectedProcess is null)
        {
            throw new BusinessOperationException("MES.FinishReport", "完工上报失败", "No task to finish");
        }

        var task = CurrentState.ActiveTask;
        var endOperator = string.IsNullOrWhiteSpace(employeeNumber)
            ? task.StartOperatorNumber ?? CurrentState.MesOperatorNumber
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
        CurrentState.ActiveTask = task;
        _operationLogService.Write("ExpEnd", $"Finish report submitted, WorkOrder={task.WorkOrderId}, UploadStatus={task.UploadStatus}");
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
        }

        _operationLogService.Write("RetryUpload", $"Pending upload retry triggered for {pendingTasks.Count} task(s).");
        NotifyStateChanged();
        return Task.CompletedTask;
    }

    public void UpdateProgramContent(string content)
    {
        if (CurrentState.SelectedProgram is null)
        {
            return;
        }

        CurrentState.SelectedProgram.ProgramContent = content;
        NotifyStateChanged();
    }

    public void Reset()
    {
        ResetRuntime(keepSyncMessage: true);
        NotifyStateChanged();
    }

    private void EnsureReadyForStart()
    {
        if (CurrentState.CurrentWorkOrder is null)
        {
            throw new BusinessOperationException("MES.StartReport", "开工上报失败", "No work order available");
        }

        if (CurrentState.SelectedProcess is null)
        {
            throw new BusinessOperationException("MES.StartReport", "开工上报失败", "No process selected");
        }

        if (CurrentState.SelectedProgram is null)
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
        CurrentState.Reset();
        if (keepSyncMessage)
        {
            CurrentState.LastServerSyncTime = lastSyncTime;
            CurrentState.LastServerSyncMessage = lastSyncMessage;
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

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
