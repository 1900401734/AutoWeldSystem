using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.DTOs.Upload;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Data;
using System.Globalization;
using System.Text.Json;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 通用上传任务服务实现。
/// 当前先提供查询和人工重试排队能力，后续上传执行器可复用同一张任务表。
/// </summary>
public class UploadTaskService : IUploadTaskService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IMesProvider _mesProvider;
    private readonly IAppSettingsService _settingsService;
    private readonly IProductionFlowLogService _productionLogService;
    private readonly IDeviceLifecycleLogService _deviceLifecycleLogService;
    private readonly IDeviceStatusService _deviceStatusService;
    private readonly object _dbLock = new();

    public UploadTaskService(
        SqlSugarDbContext dbContext,
        IMesProvider mesProvider,
        IAppSettingsService settingsService,
        IProductionFlowLogService productionLogService,
        IDeviceLifecycleLogService deviceLifecycleLogService,
        IDeviceStatusService deviceStatusService)
    {
        _dbContext = dbContext;
        _mesProvider = mesProvider;
        _settingsService = settingsService;
        _productionLogService = productionLogService;
        _deviceLifecycleLogService = deviceLifecycleLogService;
        _deviceStatusService = deviceStatusService;
    }

    /// <summary>
    /// Raised when an upload task status or upload-summary visibility changes.
    /// </summary>
    public event EventHandler<UploadTaskStatusChangedEventArgs>? TaskStatusChanged;

    public IReadOnlyList<UploadTaskSummary> GetTasks(string taskType, bool includeCompleted = false)
    {
        var normalizedTaskType = NormalizeTaskType(taskType);
        HashSet<string>? deviceStatusRecordKeys = null;
        if (normalizedTaskType == ProductionConstants.UploadTaskTypes.DeviceStatus)
        {
            deviceStatusRecordKeys = SyncDeviceStatusTasksFromLogs();
        }
        else if (normalizedTaskType == ProductionConstants.UploadTaskTypes.ReportFile)
        {
            SyncReportFileTasksFromReports();
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var query = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.TaskType == normalizedTaskType && !task.IsDeleted);

            if (!includeCompleted)
            {
                query = query.Where(task => task.Status != ProductionConstants.UploadStatuses.Uploaded);
            }

            var rows = query.ToList()
                .OrderByDescending(task => IsActionRequired(task.Status))
                .ThenByDescending(task => task.UpdatedTime)
                .Select(ToSummary)
                .ToList();

            if (deviceStatusRecordKeys is not null)
            {
                rows = rows
                    .Where(row => !string.IsNullOrWhiteSpace(row.DeviceStatusRecordKey)
                        && deviceStatusRecordKeys.Contains(row.DeviceStatusRecordKey))
                    .ToList();
            }

            return rows;
        }
    }

    public IReadOnlyList<UploadTaskSummary> GetProcessParameterRows(bool includeCompleted = false)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var uploadTasks = QueryUploadTasks(ProductionConstants.UploadTaskTypes.ProcessParameter, includeCompleted);
            var rows = uploadTasks
                .Select(ToSummary)
                .ToList();

            var pendingRecords = _dbContext.Db.Queryable<BizWeldPointRecord>()
                .Where(record => record.ProductCompleted
                    && record.UploadStatus != ProductionConstants.UploadStatuses.Uploaded)
                .ToList();
            if (pendingRecords.Count == 0)
            {
                return rows;
            }

            var weldTaskIds = pendingRecords
                .Select(record => record.TaskId)
                .Distinct()
                .ToList();
            var weldTasks = _dbContext.Db.Queryable<BizWeldTask>()
                .Where(task => weldTaskIds.Contains(task.Id))
                .ToList();
            var openProcessTasks = uploadTasks
                .Where(task => task.Status != ProductionConstants.UploadStatuses.Uploaded)
                .ToList();
            var batchSize = Math.Max(1, _settingsService.Get().UploadBatchSize);

            foreach (var weldTask in weldTasks.OrderByDescending(task => task.Id))
            {
                var taskRecords = pendingRecords
                    .Where(record => record.TaskId == weldTask.Id)
                    .Where(record => !IsCoveredByOpenProcessTask(record, openProcessTasks))
                    .ToList();
                rows.AddRange(ProcessParameterUploadRowRules.CreatePendingProductRows(weldTask, taskRecords, batchSize));
            }

            return rows
                .OrderByDescending(row => IsActionRequired(row.Status))
                .ThenBy(row => row.IsVirtual)
                .ThenByDescending(row => row.UpdatedTime)
                .ToList();
        }
    }

    public UploadTaskSummary? GetById(int id)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(id);
            return task is null || task.IsDeleted ? null : ToSummary(task);
        }
    }

    private List<BizUploadTask> QueryUploadTasks(string taskType, bool includeCompleted)
    {
        var normalizedTaskType = NormalizeTaskType(taskType);
        var query = _dbContext.Db.Queryable<BizUploadTask>()
            .Where(task => task.TaskType == normalizedTaskType && !task.IsDeleted);

        if (!includeCompleted)
        {
            query = query.Where(task => task.Status != ProductionConstants.UploadStatuses.Uploaded);
        }

        return query.ToList();
    }

    /// <summary>
    /// Reconciles pending and failed device-status logs into the upload-task index.
    /// </summary>
    private HashSet<string> SyncDeviceStatusTasksFromLogs()
    {
        var logs = _deviceStatusService.GetPendingLogs().ToList();
        var activeRecordKeys = logs
            .Select(DeviceStatusRecordIdentityRules.GetRecordKey)
            .Where(recordKey => recordKey is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var log in logs)
        {
            _ = _deviceStatusService.EnsurePendingUploadTask(log);
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var now = DateTime.Now;
            var staleTasks = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.TaskType == ProductionConstants.UploadTaskTypes.DeviceStatus
                    && !task.IsDeleted
                    && task.Status != ProductionConstants.UploadStatuses.Uploaded)
                .ToList()
                .Where(task =>
                {
                    var recordKey = DeviceStatusRecordIdentityRules.ReadTaskRecordKey(task.BusinessId, task.PayloadJson);
                    return (recordKey is null || !activeRecordKeys.Contains(recordKey))
                        && !_deviceStatusService.ShouldPreserveUploadingTask(task);
                })
                .ToList();
            foreach (var task in staleTasks)
            {
                var existingStatus = task.Status;
                var existingLastAttemptTime = task.LastAttemptTime;
                task.IsDeleted = true;
                task.DeletedTime = now;
                task.UpdatedTime = now;
                task.Message = "Device status JSONL source is missing or no longer pending.";
                _ = _dbContext.Db.Updateable(task)
                    .UpdateColumns(taskRow => new
                    {
                        taskRow.IsDeleted,
                        taskRow.DeletedTime,
                        taskRow.UpdatedTime,
                        taskRow.Message
                    })
                    .Where(taskRow => taskRow.Id == task.Id
                        && taskRow.Status == existingStatus
                        && taskRow.LastAttemptTime == existingLastAttemptTime)
                    .ExecuteCommand();
            }
        }

        return activeRecordKeys;
    }

    public BizUploadTask EnqueueOrUpdate(BizUploadTask task)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            Normalize(task);

            var existing = FindExistingTask(task);
            if (existing is null)
            {
                task.CreatedTime = DateTime.Now;
                task.UpdatedTime = DateTime.Now;
                return _dbContext.Db.Insertable(task).ExecuteReturnEntity();
            }

            if (existing.IsDeleted)
            {
                return existing;
            }

            if (existing.Status == ProductionConstants.UploadStatuses.Uploaded)
            {
                return existing;
            }

            existing.WeldTaskId = task.WeldTaskId;
            existing.PayloadJson = task.PayloadJson;
            existing.FilePath = task.FilePath;
            existing.Status = task.Status;
            existing.Target = task.Target;
            existing.MaxRetryCount = task.MaxRetryCount;
            existing.NextRetryTime = task.NextRetryTime;
            existing.CompletedTime = task.CompletedTime;
            existing.Message = task.Message;
            existing.UpdatedTime = DateTime.Now;

            _dbContext.Db.Updateable(existing).ExecuteCommand();
            return _dbContext.Db.Queryable<BizUploadTask>().InSingle(existing.Id) ?? existing;
        }
    }

    private void SyncReportFileTasksFromReports()
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var reports = _dbContext.Db.Queryable<BizProductionReportFile>()
                .Where(report => report.FileCode == ProductionConstants.ReportFileCodes.Spreadsheet
                    && report.MesFileType == ProductionConstants.MesFileTypes.ReportFile)
                .ToList()
                .Where(ShouldSyncReportFileTask)
                .GroupBy(report => report.TaskId)
                .Select(group => group
                    .OrderByDescending(report => report.UpdatedTime)
                    .ThenByDescending(report => report.Id)
                    .First())
                .ToList();
            if (reports.Count == 0)
            {
                return;
            }

            var taskIds = reports.Select(report => report.TaskId).Distinct().ToList();
            var weldTasks = _dbContext.Db.Queryable<BizWeldTask>()
                .Where(task => taskIds.Contains(task.Id))
                .ToList()
                .ToDictionary(task => task.Id);

            foreach (var report in reports)
            {
                if (!weldTasks.TryGetValue(report.TaskId, out var weldTask))
                {
                    continue;
                }

                UpsertReportFileUploadTask(weldTask, report);
            }
        }
    }

    private void UpsertReportFileUploadTask(BizWeldTask weldTask, BizProductionReportFile report)
    {
        var now = DateTime.Now;
        var businessId = BuildUploadBusinessId(weldTask, "report-file");
        var existing = _dbContext.Db.Queryable<BizUploadTask>()
            .Where(task => task.TaskType == ProductionConstants.UploadTaskTypes.ReportFile
                && task.Target == ProductionConstants.UploadTargets.Mes
                && (task.BusinessId == businessId || task.WeldTaskId == weldTask.Id))
            .ToList()
            .OrderBy(task => task.IsDeleted)
            .ThenByDescending(task => task.UpdatedTime)
            .FirstOrDefault();
        if (existing is null)
        {
            var uploadTask = BuildReportFileUploadTask(weldTask, report, businessId, now);
            Normalize(uploadTask);
            _dbContext.Db.Insertable(uploadTask).ExecuteCommand();
            return;
        }

        if (existing.IsDeleted
            || existing.Status == ProductionConstants.UploadStatuses.Uploaded
            || existing.Status == ProductionConstants.UploadStatuses.Uploading)
        {
            return;
        }

        existing.WeldTaskId = weldTask.Id;
        existing.PayloadJson = BuildReportFileUploadPayload(weldTask);
        existing.FilePath = report.FilePath;
        existing.Status = NormalizeStatus(report.UploadStatus);
        existing.Target = ProductionConstants.UploadTargets.Mes;
        existing.NextRetryTime = now;
        existing.Message = "Report file restored from generated XLSX record.";
        existing.UpdatedTime = now;
        Normalize(existing);
        _dbContext.Db.Updateable(existing).ExecuteCommand();
    }

    private static bool ShouldSyncReportFileTask(BizProductionReportFile report)
    {
        return !string.IsNullOrWhiteSpace(report.FilePath)
            && string.Equals(report.FileFormat, "XLSX", StringComparison.OrdinalIgnoreCase)
            && IsActionRequired(NormalizeStatus(report.UploadStatus));
    }

    private static BizUploadTask BuildReportFileUploadTask(
        BizWeldTask weldTask,
        BizProductionReportFile report,
        string businessId,
        DateTime now)
    {
        return new BizUploadTask
        {
            TaskType = ProductionConstants.UploadTaskTypes.ReportFile,
            Target = ProductionConstants.UploadTargets.Mes,
            BusinessId = businessId,
            WeldTaskId = weldTask.Id,
            PayloadJson = BuildReportFileUploadPayload(weldTask),
            FilePath = report.FilePath,
            Status = NormalizeStatus(report.UploadStatus),
            NextRetryTime = now,
            Message = "Report file restored from generated XLSX record.",
            CreatedTime = now,
            UpdatedTime = now
        };
    }

    private static string BuildReportFileUploadPayload(BizWeldTask task)
    {
        return JsonSerializer.Serialize(new
        {
            TaskType = ProductionConstants.UploadTaskTypes.ReportFile,
            UploadMode = "Batch",
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
    public async Task<UploadTaskSummary?> ExecuteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var candidate = GetRetryableTask(id);
        if (candidate is null)
        {
            return null;
        }

        string? recordKey = null;
        if (string.Equals(
            candidate.TaskType,
            ProductionConstants.UploadTaskTypes.DeviceStatus,
            StringComparison.OrdinalIgnoreCase))
        {
            recordKey = DeviceStatusRecordIdentityRules.ReadTaskRecordKey(
                candidate.BusinessId,
                candidate.PayloadJson);
            var source = recordKey is null ? null : _deviceStatusService.GetLog(recordKey);
            if (source is null || !DeviceStatusUploadVisibilityRules.ShouldInclude(source.ReportStatus))
            {
                SoftDeleteDeviceStatusTask(candidate, "Device status JSONL source is missing or no longer pending.");
                return null;
            }
        }

        var task = MarkUploading(id);
        if (task is null)
        {
            return null;
        }

        BasicRes<object>? response = recordKey is null
            ? await ExecuteByTypeAsync(task, cancellationToken)
            : await UploadDeviceStatusAsync(recordKey, cancellationToken);
        if (response is null)
        {
            SoftDeleteDeviceStatusTask(task, "Device status JSONL source was removed before MES upload.");
            return null;
        }

        return FinishExecution(task.Id, response);
    }

    public async Task<int> ExecuteAllPendingAsync(
        string taskType,
        CancellationToken cancellationToken = default)
    {
        var normalizedTaskType = NormalizeTaskType(taskType);
        if (normalizedTaskType == ProductionConstants.UploadTaskTypes.DeviceStatus)
        {
            _ = SyncDeviceStatusTasksFromLogs();
        }
        else if (normalizedTaskType == ProductionConstants.UploadTaskTypes.ReportFile)
        {
            SyncReportFileTasksFromReports();
        }

        List<int> taskIds;
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            taskIds = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.TaskType == normalizedTaskType && !task.IsDeleted)
                .ToList()
                .Where(UploadTaskVisibilityRules.ShouldRetry)
                .Select(task => task.Id)
                .ToList();
        }

        var executedCount = 0;
        foreach (var taskId in taskIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await ExecuteAsync(taskId, cancellationToken) is not null)
            {
                executedCount++;
            }
        }

        return executedCount;
    }

    public void RequestRetry(int id)
    {
        UploadTaskStatusChangedEventArgs? changed = null;
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(id);
            if (task is null || !UploadTaskVisibilityRules.ShouldRetry(task))
            {
                return;
            }

            MarkRetryRequested(task);
            _dbContext.Db.Updateable(task).ExecuteCommand();
            changed = ToStatusChangedEvent(task);
        }

        PublishTaskStatusChanged(changed);
    }

    public int RequestRetryAll(string taskType)
    {
        var normalizedTaskType = NormalizeTaskType(taskType);
        if (normalizedTaskType == ProductionConstants.UploadTaskTypes.DeviceStatus)
        {
            _ = SyncDeviceStatusTasksFromLogs();
        }
        else if (normalizedTaskType == ProductionConstants.UploadTaskTypes.ReportFile)
        {
            SyncReportFileTasksFromReports();
        }

        var changes = new List<UploadTaskStatusChangedEventArgs>();
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var tasks = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.TaskType == normalizedTaskType
                    && !task.IsDeleted)
                .ToList();

            foreach (var task in tasks.Where(UploadTaskVisibilityRules.ShouldRetry))
            {
                MarkRetryRequested(task);
                _dbContext.Db.Updateable(task).ExecuteCommand();
                changes.Add(ToStatusChangedEvent(task));
            }
        }

        foreach (var change in changes)
        {
            PublishTaskStatusChanged(change);
        }

        return changes.Count;
    }

    public void DeleteTask(int id)
    {
        UploadTaskStatusChangedEventArgs? changed = null;
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(id);
            if (task is null || task.IsDeleted)
            {
                return;
            }

            task.IsDeleted = true;
            task.DeletedTime = DateTime.Now;
            task.UpdatedTime = DateTime.Now;
            task.Message = "Deleted from upload state page.";
            _dbContext.Db.Updateable(task).ExecuteCommand();
            changed = ToStatusChangedEvent(task, "Deleted");
        }

        PublishTaskStatusChanged(changed);
    }

    public void HideWeldTaskUploadState(int weldTaskId)
    {
        UploadTaskStatusChangedEventArgs? changed = null;
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizWeldTask>().InSingle(weldTaskId);
            if (task is null || task.UploadStateHidden)
            {
                return;
            }

            task.UploadStateHidden = true;
            _dbContext.Db.Updateable(task)
                .UpdateColumns(it => new { it.UploadStateHidden })
                .Where(it => it.Id == task.Id)
                .ExecuteCommand();

            changed = new UploadTaskStatusChangedEventArgs
            {
                WeldTaskId = task.Id,
                TaskType = "Summary",
                Status = "Hidden"
            };
        }

        PublishTaskStatusChanged(changed);
    }

    private BizUploadTask? GetRetryableTask(int id)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(id);
            return task is not null && UploadTaskVisibilityRules.ShouldRetry(task)
                ? task
                : null;
        }
    }

    private void SoftDeleteDeviceStatusTask(BizUploadTask expectedTask, string message)
    {
        UploadTaskStatusChangedEventArgs? changed = null;
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var expectedStatus = expectedTask.Status;
            var expectedLastAttemptTime = expectedTask.LastAttemptTime;
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(expectedTask.Id);
            if (task is null
                || task.IsDeleted
                || task.Status == ProductionConstants.UploadStatuses.Uploaded)
            {
                return;
            }

            task.IsDeleted = true;
            task.DeletedTime = DateTime.Now;
            task.UpdatedTime = DateTime.Now;
            task.Message = message;
            var affectedRows = _dbContext.Db.Updateable(task)
                .UpdateColumns(taskRow => new
                {
                    taskRow.IsDeleted,
                    taskRow.DeletedTime,
                    taskRow.UpdatedTime,
                    taskRow.Message
                })
                .Where(taskRow => taskRow.Id == expectedTask.Id
                    && taskRow.Status == expectedStatus
                    && taskRow.LastAttemptTime == expectedLastAttemptTime)
                .ExecuteCommand();
            if (affectedRows > 0)
            {
                changed = ToStatusChangedEvent(task, "Deleted");
            }
        }

        PublishTaskStatusChanged(changed);
    }

    private BizUploadTask? MarkUploading(int id)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(id);
            if (task is null || !UploadTaskVisibilityRules.ShouldRetry(task))
            {
                return null;
            }

            task.Status = ProductionConstants.UploadStatuses.Uploading;
            task.LastAttemptTime = DateTime.Now;
            task.RetryCount++;
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(task).ExecuteCommand();
            return task;
        }
    }

    private async Task<BasicRes<object>> ExecuteByTypeAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        return task.TaskType switch
        {
            ProductionConstants.UploadTaskTypes.StartReport => await UploadStartReportAsync(task, cancellationToken),
            ProductionConstants.UploadTaskTypes.FinishReport => await UploadFinishReportAsync(task, cancellationToken),
            ProductionConstants.UploadTaskTypes.WorkOrderStatus => await UploadWorkOrderStatusAsync(task, cancellationToken),
            ProductionConstants.UploadTaskTypes.ReportFile => await UploadReportFileAsync(task, cancellationToken),
            ProductionConstants.UploadTaskTypes.ProcessParameter => await UploadProcessParametersAsync(task, cancellationToken),
            ProductionConstants.UploadTaskTypes.ProgramFile => Unsupported("程序文件上传由程序管理服务处理。"),
            _ => Unsupported($"暂不支持的上传任务类型：{task.TaskType}")
        };
    }

    /// <summary>
    /// 上传当前任务下尚未成功上传的焊点记录。
    /// 先尝试整批上传，失败后按 ProductNumber 降级，最后再降级到单条焊点，尽量保住已成功的数据。
    /// </summary>
    private async Task<BasicRes<object>> UploadStartReportAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        var request = ReadPayloadRequest<ExperimentStartReq>(task.PayloadJson);
        if (request is null)
        {
            return Unsupported("Start report task payload is missing.");
        }

        ApplyOfflineStartRequestId(task, request);

        var response = await _mesProvider.StartWorkAsync(request, cancellationToken);
        if (!response.IsSuccess || response.Data is null || string.IsNullOrWhiteSpace(response.Data.Id))
        {
            return Unsupported(response.Msg);
        }

        UpdateTaskExpStartId(task, response.Data.Id);
        WriteStartReportLifecycleLog(task, response.Data.Id);
        await RecordProgramStartedStatusAsync(task, cancellationToken);
        return Success(string.IsNullOrWhiteSpace(response.Msg) ? "Start report uploaded." : response.Msg);
    }

    /// <summary>
    /// Backfills the device-generated local id for offline start reports that were queued before this rule existed.
    /// </summary>
    private void ApplyOfflineStartRequestId(BizUploadTask task, ExperimentStartReq request)
    {
        var weldTask = GetWeldTask(task);
        if (weldTask is null)
        {
            return;
        }

        ExperimentStartRequestRules.ApplyOfflineStartId(weldTask, request);
    }

    /// <summary>
    /// Writes the independent device log when a queued offline start report is finally accepted by MES.
    /// </summary>
    private void WriteStartReportLifecycleLog(BizUploadTask task, string expStartId)
    {
        var weldTask = GetWeldTask(task);
        if (weldTask is null)
        {
            return;
        }

        _deviceLifecycleLogService.Write(DeviceLifecycleLogRules.CreateTestProgramRunningEntry(
            weldTask.DeviceId,
            weldTask.StationNo,
            FirstNonEmpty(expStartId, weldTask.ExpStartId, weldTask.LocalExpStartId),
            weldTask.SN,
            DateTime.Now));
    }

    private async Task RecordProgramStartedStatusAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        var weldTask = GetWeldTask(task);
        if (weldTask is null)
        {
            return;
        }

        await RecordProgramStartedStatusAsync(weldTask, cancellationToken);
    }

    private Task RecordProgramStartedStatusAsync(BizWeldTask task, CancellationToken cancellationToken)
    {
        return _deviceStatusService.ChangeStatusAsync(
            ProductionConstants.MesDeviceStatuses.ProgramStarted,
            DeviceStatusReportRules.AppendStationRemark(
                DeviceStatusReportRules.GetStatusName(ProductionConstants.MesDeviceStatuses.ProgramStarted),
                task.StationNo),
            "MES",
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
            "MES",
            stationNo: task.StationNo,
            weldTaskId: task.Id,
            workOrderId: task.SN,
            cancellationToken: cancellationToken);
    }

    private async Task<BasicRes<object>> UploadFinishReportAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        var weldTask = GetWeldTask(task);
        if (weldTask is null)
        {
            return Unsupported("Finish report task has no weld task.");
        }

        if (string.IsNullOrWhiteSpace(weldTask.ExpStartId))
        {
            return Unsupported("Finish report is waiting for start report upload.");
        }

        var request = ReadPayloadRequest<ExperimentEndReq>(task.PayloadJson) ?? new ExperimentEndReq();
        request.ExpStartId = weldTask.ExpStartId;
        request.DeviceId = FirstNonEmpty(request.DeviceId, weldTask.DeviceId);
        request.SN = FirstNonEmpty(request.SN, weldTask.SN);
        request.ProcessNo = FirstNonEmpty(request.ProcessNo, weldTask.ProcessNo);
        request.EndExperID = FirstNonEmpty(request.EndExperID, weldTask.EndOperatorNumber, weldTask.UserNumber, Environment.UserName);
        request.EndTs = FirstNonEmpty(request.EndTs, weldTask.EndTime?.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        request.ExpStatus = ProductionConstants.MesWorkOrderStatuses.Completed;
        request.WorkHour = request.WorkHour <= 0
            ? Convert.ToDecimal(((weldTask.EndTime ?? DateTime.Now) - weldTask.StartTime).TotalHours)
            : request.WorkHour;
        request.ExpQty = request.ExpQty <= 0 ? weldTask.ActualQty : request.ExpQty;
        request.QualifyNumber = request.QualifyNumber <= 0 ? weldTask.QualifiedQty : request.QualifyNumber;
        request.FailureNumber = request.FailureNumber <= 0 ? weldTask.FailedQty : request.FailureNumber;

        var response = await _mesProvider.EndWorkAsync(request, cancellationToken);
        if (response.IsSuccess)
        {
            await RecordProgramEndedStatusAsync(weldTask, cancellationToken);
        }

        return response;
    }

    private async Task<BasicRes<object>> UploadWorkOrderStatusAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        if (!IsWorkOrderStatusReportEnabled())
        {
            return Skipped("Work-order status report is disabled in system settings.");
        }

        var weldTask = GetWeldTask(task);
        if (weldTask is null)
        {
            return Unsupported("Work-order status task has no weld task.");
        }

        if (string.IsNullOrWhiteSpace(weldTask.ExpStartId))
        {
            return Unsupported("Work-order status is waiting for start report upload.");
        }

        var statusCode = ReadStatusCode(task.PayloadJson);
        if (string.Equals(statusCode, ProductionConstants.MesWorkOrderStatuses.Completed, StringComparison.OrdinalIgnoreCase)
            && !IsFinishReportUploadedOrAbsent(weldTask.Id))
        {
            return Unsupported("Completed status is waiting for finish report upload.");
        }

        var request = ReadPayloadRequest<ReportExperimentStatusReq>(task.PayloadJson) ?? new ReportExperimentStatusReq();
        request.ExpStartId = weldTask.ExpStartId;
        request.DeviceId = FirstNonEmpty(request.DeviceId, weldTask.DeviceId);
        request.ExpStatus = FirstNonEmpty(statusCode, request.ExpStatus);
        request.Ts = FirstNonEmpty(request.Ts, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        return await _mesProvider.ChangeWorkStatusAsync(request, cancellationToken);
    }

    private Task<BasicRes<object>?> UploadDeviceStatusAsync(
        string recordKey,
        CancellationToken cancellationToken)
        => _deviceStatusService.RetryUploadAsync(recordKey, cancellationToken);

    private async Task<BasicRes<object>> UploadProcessParametersAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        if (!EnsureTaskExpStartReady(task, out var message))
        {
            return Unsupported(message);
        }

        var records = GetPendingWeldPointRecords(task);
        if (records.Count == 0)
        {
            return Success("没有待上传的过程参数。");
        }

        var batchResponse = await UploadProcessParameterGroupAsync(records, cancellationToken);
        if (batchResponse.IsSuccess)
        {
            UpdateWeldPointUploadStatus(records, batchResponse);
            return batchResponse;
        }

        if (!IsProductScopedTask(task))
        {
            UpdateWeldPointUploadStatus(records, batchResponse);
            return Unsupported($"过程参数批量上传失败，任务级上传不会拆分为单件上传。原因：{batchResponse.Msg}");
        }

        var failedMessages = new List<string>();
        foreach (var productGroup in records.GroupBy(record => record.ProductNo).OrderBy(group => group.Key))
        {
            var productRecords = productGroup.ToList();
            var productResponse = await UploadProcessParameterGroupAsync(productRecords, cancellationToken);
            if (productResponse.IsSuccess)
            {
                UpdateWeldPointUploadStatus(productRecords, productResponse);
                continue;
            }

            foreach (var record in productRecords.OrderBy(record => record.SequenceNo))
            {
                var singleResponse = await UploadProcessParameterGroupAsync(new[] { record }, cancellationToken);
                UpdateWeldPointUploadStatus(new[] { record }, singleResponse);
                if (!singleResponse.IsSuccess)
                {
                    failedMessages.Add($"ProductNumber={record.ProductNo}, TouchNo={record.TouchNo}: {singleResponse.Msg}");
                }
            }
        }

        return failedMessages.Count == 0
            ? Success($"过程参数已通过降级策略上传成功。整批失败原因：{batchResponse.Msg}")
            : Unsupported($"过程参数部分上传失败。整批失败原因：{batchResponse.Msg}；明细：{FormatFailureMessages(failedMessages)}");
    }

    private IReadOnlyList<BizWeldPointRecord> GetPendingWeldPointRecords(BizUploadTask task)
    {
        if (task.WeldTaskId is null)
        {
            return Array.Empty<BizWeldPointRecord>();
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            return _dbContext.Db.Queryable<BizWeldPointRecord>()
                .Where(record => record.TaskId == task.WeldTaskId.Value
                    && record.UploadStatus != ProductionConstants.UploadStatuses.Uploaded)
                .ToList()
                .Where(record => IsRecordInTaskScope(record, task))
                .OrderBy(record => record.StationNo)
                .ThenBy(record => record.ProductNo)
                .ThenBy(record => record.SequenceNo)
                .ToList();
        }
    }

    private bool EnsureTaskExpStartReady(BizUploadTask task, out string message)
    {
        message = string.Empty;
        var weldTask = GetWeldTask(task);
        if (weldTask is null)
        {
            message = "Upload task has no weld task.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(weldTask.ExpStartId))
        {
            message = "Upload task is waiting for start report upload.";
            return false;
        }

        UpdateWeldPointExpStartId(weldTask.Id, weldTask.ExpStartId);
        return true;
    }

    private BizWeldTask? GetWeldTask(BizUploadTask task)
    {
        if (task.WeldTaskId is null)
        {
            return null;
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            return _dbContext.Db.Queryable<BizWeldTask>().InSingle(task.WeldTaskId.Value);
        }
    }

    private void UpdateTaskExpStartId(BizUploadTask task, string expStartId)
    {
        if (task.WeldTaskId is null)
        {
            return;
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var weldTask = _dbContext.Db.Queryable<BizWeldTask>().InSingle(task.WeldTaskId.Value);
            if (weldTask is null)
            {
                return;
            }

            weldTask.ExpStartId = expStartId.Trim();
            weldTask.UploadMessage = "Start report uploaded to MES.";
            _dbContext.Db.Updateable(weldTask)
                .UpdateColumns(it => new { it.ExpStartId, it.UploadMessage })
                .Where(it => it.Id == weldTask.Id)
                .ExecuteCommand();

            UpdateWeldPointExpStartId(weldTask.Id, weldTask.ExpStartId);
        }
    }

    private void UpdateWeldPointExpStartId(int weldTaskId, string expStartId)
    {
        _dbContext.Db.Updateable<BizWeldPointRecord>()
            .SetColumns(record => record.ExpStartId == expStartId)
            .Where(record => record.TaskId == weldTaskId && record.ExpStartId == null)
            .ExecuteCommand();
    }

    private bool IsFinishReportUploadedOrAbsent(int weldTaskId)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var finishTask = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.WeldTaskId == weldTaskId
                    && task.TaskType == ProductionConstants.UploadTaskTypes.FinishReport)
                .OrderByDescending(task => task.UpdatedTime)
                .First();

            return finishTask is null
                || finishTask.Status == ProductionConstants.UploadStatuses.Uploaded;
        }
    }

    private async Task<BasicRes<object>> UploadProcessParameterGroupAsync(
        IReadOnlyList<BizWeldPointRecord> records,
        CancellationToken cancellationToken)
    {
        var settings = _settingsService.Get();
        var deviceType = NormalizeProcessParameterDeviceType(settings.ProcessParameterDeviceType);
        var showTestFlagInHistory = settings.ShowTestFlagInHistory != false;
        var schemeItemCache = new Dictionary<string, IReadOnlyList<ProcessParameterSchemeItem>>(StringComparer.OrdinalIgnoreCase);
        var items = records
            .Select(record => ToProcessParameterUploadItem(
                record,
                deviceType,
                showTestFlagInHistory,
                ResolveProcessParameterSchemeItems(record, schemeItemCache)))
            .ToList();
        return await _mesProvider.UploadProcessParametersAsync(items, cancellationToken);
    }

    private void UpdateWeldPointUploadStatus(IReadOnlyList<BizWeldPointRecord> records, BasicRes<object> response)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            foreach (var record in records)
            {
                record.UploadStatus = response.IsSuccess
                    ? ProductionConstants.UploadStatuses.Uploaded
                    : ProductionConstants.UploadStatuses.Failed;
                record.UploadTime = response.IsSuccess ? DateTime.Now : null;
                record.UploadMessage = response.Msg;
                record.RetryCount = response.IsSuccess ? record.RetryCount : record.RetryCount + 1;
                _dbContext.Db.Updateable(record).ExecuteCommand();
            }
        }
    }

    private IReadOnlyList<ProcessParameterSchemeItem> ResolveProcessParameterSchemeItems(
        BizWeldPointRecord record,
        Dictionary<string, IReadOnlyList<ProcessParameterSchemeItem>> cache)
    {
        var cacheKey = $"{record.TaskId}\u001F{record.ProductNo}\u001F{record.StationNo}";
        if (cache.TryGetValue(cacheKey, out var cachedItems))
        {
            return cachedItems;
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var config = ResolveProductProcessConfig(record);
            var items = GetMesSchemeItemsForConfig(config);
            cache[cacheKey] = items;
            return items;
        }
    }

    private BizProductProcessConfig? ResolveProductProcessConfig(BizWeldPointRecord record)
    {
        var task = _dbContext.Db.Queryable<BizWeldTask>().InSingle(record.TaskId);
        if (task is null)
        {
            return null;
        }

        var productNum = ResolveTaskProductNum(task);
        if (string.IsNullOrWhiteSpace(productNum))
        {
            return null;
        }

        var stationNo = record.StationNo > ProductionConstants.Stations.SharedStationNo
            ? record.StationNo
            : task.StationNo;
        stationNo = stationNo > ProductionConstants.Stations.SharedStationNo
            ? stationNo
            : ProductionConstants.Stations.DefaultStationNo;

        return _dbContext.Db.Queryable<BizProductProcessConfig>()
            .Where(config => config.Enabled && config.ProductNum == productNum)
            .ToList()
            .Where(config => config.StationNo == ProductionConstants.Stations.SharedStationNo || config.StationNo == stationNo)
            .OrderByDescending(config => config.StationNo == stationNo)
            .ThenBy(config => config.Id)
            .FirstOrDefault();
    }

    private IReadOnlyList<ProcessParameterSchemeItem> GetMesSchemeItemsForConfig(BizProductProcessConfig? config)
    {
        if (config is null)
        {
            return Array.Empty<ProcessParameterSchemeItem>();
        }

        var details = _dbContext.Db.Queryable<BizSchemeDetail>()
            .Where(detail => detail.SchemeId == config.SchemeId)
            .ToList();
        if (details.Count == 0)
        {
            return Array.Empty<ProcessParameterSchemeItem>();
        }

        var itemIds = details.Select(detail => detail.ItemId).Distinct().ToList();
        var items = _dbContext.Db.Queryable<DimTestItem>()
            .Where(item => itemIds.Contains(item.ItemId))
            .ToList();

        return details
            .OrderBy(detail => detail.DetailId)
            .Select(detail => new
            {
                Item = items.FirstOrDefault(item => item.ItemId == detail.ItemId),
                Detail = detail
            })
            .Where(item => item.Item is not null)
            .Select(item =>
            {
                SchemeDetailRoleRules.ClearUnavailableRoles(item.Detail, item.Item!);
                return item;
            })
            .Where(item => HasAnyMesEnabledRole(item.Detail))
            .Select(item => new ProcessParameterSchemeItem(item.Item!, item.Detail))
            .ToList();
    }

    private string ResolveTaskProductNum(BizWeldTask task)
    {
        if (!string.IsNullOrWhiteSpace(task.ProgramId))
        {
            var programs = _dbContext.Db.Queryable<BizProgram>()
                .Where(program => !program.IsDeleted && program.ProgramId == task.ProgramId.Trim())
                .ToList();

            var localProgram = programs
                .OrderByDescending(program => IsExactTextMatch(program.DeviceId, task.DeviceId))
                .ThenByDescending(program => program.UpdatedTime)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(localProgram?.ProductNum))
            {
                return localProgram.ProductNum.Trim();
            }
        }

        return task.ProductNum.Trim();
    }

    private static ProcessParameterUploadItem ToProcessParameterUploadItem(
        BizWeldPointRecord record,
        string deviceType,
        bool showTestFlagInHistory,
        IReadOnlyList<ProcessParameterSchemeItem> schemeItems)
    {
        var item = new ProcessParameterUploadItem
        {
            ExpStartId = record.ExpStartId,
            DeviceId = record.DeviceId,
            SN = record.SN,
            ProcessNo = record.ProcessNo,
            ProductNo = record.ProductNo,
            TouchNo = ShouldWriteTouchNo(deviceType) ? record.TouchNo : null,
            Type = ResolveProcessParameterType(deviceType),
            IsTest = ProcessParameterIsTestRules.Resolve(record.IsTest, showTestFlagInHistory, deviceType),
            Ts = record.Ts.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
        };

        AddMesDynamicFields(item, record.RawDataJson, schemeItems);
        return item;
    }

    private static void AddMesDynamicFields(
        ProcessParameterUploadItem uploadItem,
        string? rawDataJson,
        IReadOnlyList<ProcessParameterSchemeItem> schemeItems)
    {
        if (schemeItems.Count == 0)
        {
            return;
        }

        var rawValues = ParseRawData(rawDataJson);
        foreach (var schemeItem in schemeItems)
        {
            AddMesDynamicField(uploadItem, rawValues, schemeItem, SchemeDetailValueRole.Actual);
            AddMesDynamicField(uploadItem, rawValues, schemeItem, SchemeDetailValueRole.Upper);
            AddMesDynamicField(uploadItem, rawValues, schemeItem, SchemeDetailValueRole.Lower);
            AddMesDynamicField(uploadItem, rawValues, schemeItem, SchemeDetailValueRole.Result);
        }
    }

    private static void AddMesDynamicField(
        ProcessParameterUploadItem uploadItem,
        IReadOnlyDictionary<string, string> rawValues,
        ProcessParameterSchemeItem schemeItem,
        SchemeDetailValueRole role)
    {
        if (!ShouldUploadMesRole(schemeItem.Detail, role, out var mesFieldName))
        {
            return;
        }

        var value = ResolveRawRoleValue(rawValues, schemeItem.Item, role) ?? string.Empty;
        TryAddDynamicField(uploadItem, mesFieldName, value);
    }

    private static bool ShouldUploadMesRole(
        BizSchemeDetail detail,
        SchemeDetailValueRole role,
        out string mesFieldName)
    {
        mesFieldName = SchemeDetailRoleRules.ShouldUploadMesRole(detail, role)
            ? SchemeDetailRoleRules.GetMesFieldName(detail, role) ?? string.Empty
            : string.Empty;
        mesFieldName = mesFieldName.Trim();
        return !string.IsNullOrWhiteSpace(mesFieldName);
    }

    private static string? ResolveRawRoleValue(
        IReadOnlyDictionary<string, string> rawValues,
        DimTestItem item,
        SchemeDetailValueRole role)
    {
        var itemKey = ResolveItemKey(item);
        return role switch
        {
            SchemeDetailValueRole.Actual => GetRawValue(rawValues, itemKey, item.ItemName),
            SchemeDetailValueRole.Upper => GetRawValue(rawValues, $"{itemKey}_upper", $"{item.ItemName}上限"),
            SchemeDetailValueRole.Lower => GetRawValue(rawValues, $"{itemKey}_lower", $"{item.ItemName}下限"),
            SchemeDetailValueRole.Result => GetRawValue(rawValues, $"{itemKey}_result", $"{item.ItemName}结果"),
            _ => null
        };
    }

    private static void TryAddDynamicField(ProcessParameterUploadItem uploadItem, string fieldName, string value)
    {
        if (string.IsNullOrWhiteSpace(fieldName) || IsReservedProcessParameterField(fieldName))
        {
            return;
        }

        // 方案保存已校验同一方案内不重复；这里仍用 TryAdd 防止历史脏数据覆盖已生成字段。
        uploadItem.DynamicFields.TryAdd(fieldName.Trim(), value);
    }

    private static bool IsReservedProcessParameterField(string fieldName)
    {
        return fieldName.Equals(nameof(ProcessParameterUploadItem.ExpStartId), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.DeviceId), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.SN), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.ProcessNo), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.ProductNo), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.TouchNo), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.IsTest), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.Type), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.Ts), StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldWriteTouchNo(string deviceType)
        => !string.Equals(deviceType, ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck, StringComparison.OrdinalIgnoreCase);

    private static string ResolveProcessParameterType(string deviceType)
        => string.Equals(deviceType, ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic, StringComparison.OrdinalIgnoreCase)
            ? "EM"
            : "WP";

    private static string NormalizeProcessParameterDeviceType(string? value)
    {
        return value?.Trim() switch
        {
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck => ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceWeld => ProductionConstants.ProcessParameterDeviceTypes.WholePieceWeld,
            _ => ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic
        };
    }

    private static bool HasAnyEnabledRole(BizSchemeDetail detail)
    {
        return SchemeDetailRoleRules.HasAnyCollectEnabled(detail);
    }

    private static bool HasAnyMesEnabledRole(BizSchemeDetail detail)
    {
        return SchemeDetailRoleRules.AllRoles.Any(role => SchemeDetailRoleRules.ShouldUploadMesRole(detail, role));
    }

    private static bool IsExactTextMatch(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetRawValue(IReadOnlyDictionary<string, string> rawValues, params string?[] keys)
    {
        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key) && rawValues.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static string ResolveItemKey(DimTestItem item)
    {
        return item.ItemName.Trim() switch
        {
            "峰值电流" => "max_electric",
            "峰值电压" => "max_voltage",
            "有效功率" => "valid_power",
            "位移" => "displacement",
            "焊接时间" => "weld_ts",
            var name when !string.IsNullOrWhiteSpace(name) => $"item_{item.ItemId}",
            _ => $"item_{item.ItemId}"
        };
    }

    private static Dictionary<string, string> ParseRawData(string? rawDataJson)
    {
        if (string.IsNullOrWhiteSpace(rawDataJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(rawDataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return document.RootElement.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => property.Value.ToString(),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task<BasicRes<object>> UploadReportFileAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        var request = BuildReportFileRequest(task);
        if (request is null)
        {
            return Unsupported("报告文件任务缺少工单或文件路径信息。");
        }

        return await _mesProvider.UploadReportFileAsync(request, cancellationToken);
    }

    private UploadReportFileReq? BuildReportFileRequest(BizUploadTask task)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var weldTask = task.WeldTaskId is null
                ? null
                : _dbContext.Db.Queryable<BizWeldTask>().InSingle(task.WeldTaskId.Value);
            if (weldTask is null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(weldTask.ExpStartId))
            {
                return null;
            }

            // 产品增量刷新和完工刷新可能更新同一报表记录，上传时始终优先读取最新记录。
            var reportFiles = _dbContext.Db.Queryable<BizProductionReportFile>()
                .Where(report => report.TaskId == weldTask.Id)
                .ToList();
            var latestReportFilePath = ProductionReportFileRules.SelectLatestUploadFilePath(reportFiles, weldTask.Id);
            var filePath = FirstNonEmpty(latestReportFilePath, task.FilePath);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            return new UploadReportFileReq
            {
                ExpStartId = weldTask.ExpStartId ?? string.Empty,
                DeviceId = weldTask.DeviceId,
                SN = weldTask.SN,
                ProcessNo = weldTask.ProcessNo,
                FileType = ProductionConstants.MesFileTypes.ReportFile,
                FilePath = filePath
            };
        }
    }

    private UploadTaskSummary? FinishExecution(int taskId, BasicRes<object> response)
    {
        UploadTaskSummary? summary;
        UploadTaskStatusChangedEventArgs? changed = null;
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(taskId);
            if (task is null || task.IsDeleted)
            {
                return null;
            }

            task.Status = IsSkippedResponse(response)
                ? ProductionConstants.UploadStatuses.Skipped
                : response.IsSuccess
                    ? ProductionConstants.UploadStatuses.Uploaded
                    : ProductionConstants.UploadStatuses.Failed;
            task.Message = response.Msg;
            task.CompletedTime = response.IsSuccess || IsSkippedResponse(response) ? DateTime.Now : null;
            task.NextRetryTime = response.IsSuccess || IsSkippedResponse(response) ? null : DateTime.Now.AddMinutes(1);
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(task).ExecuteCommand();
            UpdateReportFileStatus(task, response);
            summary = ToSummary(task);
            changed = ToStatusChangedEvent(task);
        }

        PublishTaskStatusChanged(changed);
        return summary;
    }

    private void WriteUploadFlowLog(BizUploadTask task, BasicRes<object> response)
    {
        var payload = ReadUploadPayload(task.PayloadJson);
        var step = ResolveUploadFlowStep(task.TaskType, response.IsSuccess);
        if (string.IsNullOrWhiteSpace(step))
        {
            return;
        }

        _productionLogService.Write(
            step,
            ResolveUploadSummary(task.TaskType, response.IsSuccess),
            response.Msg,
            response.IsSuccess ? "Info" : "Error",
            payload.StationNo,
            payload.WorkOrderId,
            payload.ProductNo,
            plcAddress: task.FilePath ?? string.Empty);
    }

    private static string ResolveUploadFlowStep(string taskType, bool success)
    {
        return taskType switch
        {
            ProductionConstants.UploadTaskTypes.StartReport => success
                ? "StartReportUploadSucceeded"
                : "StartReportUploadFailed",
            ProductionConstants.UploadTaskTypes.FinishReport => success
                ? "FinishReportUploadSucceeded"
                : "FinishReportUploadFailed",
            ProductionConstants.UploadTaskTypes.WorkOrderStatus => success
                ? "WorkOrderStatusUploadSucceeded"
                : "WorkOrderStatusUploadFailed",
            ProductionConstants.UploadTaskTypes.DeviceStatus => success
                ? "DeviceStatusUploadSucceeded"
                : "DeviceStatusUploadFailed",
            ProductionConstants.UploadTaskTypes.ProcessParameter => success
                ? "ProcessParameterUploadSucceeded"
                : "ProcessParameterUploadFailed",
            ProductionConstants.UploadTaskTypes.ReportFile => success
                ? "ReportFileUploadSucceeded"
                : "ReportFileUploadFailed",
            _ => string.Empty
        };
    }

    private static string ResolveUploadSummary(string taskType, bool success)
    {
        return taskType switch
        {
            ProductionConstants.UploadTaskTypes.ProcessParameter => success
                ? ProductionFlowLogTexts.Summaries.ProcessParameterUploadSucceeded
                : ProductionFlowLogTexts.Summaries.ProcessParameterUploadFailed,
            ProductionConstants.UploadTaskTypes.ReportFile => success
                ? ProductionFlowLogTexts.Summaries.ReportFileUploadSucceeded
                : ProductionFlowLogTexts.Summaries.ReportFileUploadFailed,
            _ => success
                ? ProductionFlowLogTexts.Summaries.UploadSucceeded
                : ProductionFlowLogTexts.Summaries.UploadFailed
        };
    }

    private void UpdateReportFileStatus(BizUploadTask task, BasicRes<object> response)
    {
        if (task.TaskType != ProductionConstants.UploadTaskTypes.ReportFile)
        {
            return;
        }

        var report = _dbContext.Db.Queryable<BizProductionReportFile>()
            .Where(item => item.TaskId == task.WeldTaskId || item.FilePath == task.FilePath)
            .ToList()
            .OrderByDescending(item => item.UpdatedTime)
            .FirstOrDefault();
        if (report is null)
        {
            return;
        }

        report.UploadStatus = task.Status;
        report.UploadTime = response.IsSuccess ? DateTime.Now : null;
        report.UploadMessage = response.Msg;
        report.UpdatedTime = DateTime.Now;
        _dbContext.Db.Updateable(report).ExecuteCommand();
    }

    private static BasicRes<object> Success(string message)
    {
        return new BasicRes<object>
        {
            Status = AppConstants.MesStatus.Success,
            Msg = message,
            Data = new object()
        };
    }

    private static BasicRes<object> Skipped(string message)
    {
        return new BasicRes<object>
        {
            Status = ProductionConstants.UploadStatuses.Skipped,
            Msg = message,
            Data = new object()
        };
    }

    private static BasicRes<object> Unsupported(string message)
    {
        return new BasicRes<object>
        {
            Status = AppConstants.MesStatus.Error,
            Msg = message
        };
    }

    private static bool IsSkippedResponse(BasicRes<object> response)
    {
        return string.Equals(response.Status, ProductionConstants.UploadStatuses.Skipped, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatFailureMessages(IReadOnlyList<string> messages)
    {
        var visibleMessages = messages.Take(5).ToList();
        var suffix = messages.Count > visibleMessages.Count
            ? $"；其余 {messages.Count - visibleMessages.Count} 条失败请查看 MES 交互日志"
            : string.Empty;

        return string.Join("；", visibleMessages) + suffix;
    }

    private static bool IsRecordInTaskScope(BizWeldPointRecord record, BizUploadTask task)
    {
        var stationNo = ProcessParameterUploadPayloadRules.ReadStationNo(task.PayloadJson);
        if (stationNo > 0 && record.StationNo != stationNo)
        {
            return false;
        }

        var productNos = ProcessParameterUploadPayloadRules.ReadProductNos(task.PayloadJson);
        return productNos.Count == 0
            || productNos.Contains(record.ProductNo, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsCoveredByOpenProcessTask(BizWeldPointRecord record, IReadOnlyList<BizUploadTask> openProcessTasks)
    {
        foreach (var task in openProcessTasks.Where(task => task.WeldTaskId == record.TaskId))
        {
            var stationNo = ProcessParameterUploadPayloadRules.ReadStationNo(task.PayloadJson);
            if (stationNo > 0 && stationNo != record.StationNo)
            {
                continue;
            }

            var productNos = ProcessParameterUploadPayloadRules.ReadProductNos(task.PayloadJson);
            if (productNos.Count == 0 || productNos.Contains(record.ProductNo, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsProductScopedTask(BizUploadTask task)
    {
        return ProcessParameterUploadPayloadRules.ReadProductNos(task.PayloadJson).Count > 0;
    }

    private static T? ReadPayloadRequest<T>(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return default;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var element = document.RootElement.TryGetProperty("Request", out var requestElement)
                ? requestElement
                : document.RootElement;
            return element.Deserialize<T>();
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static string ReadStatusCode(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            if (root.TryGetProperty("StatusCode", out var statusCodeElement))
            {
                return statusCodeElement.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("Request", out var requestElement)
                && requestElement.TryGetProperty("ExpStatus", out var expStatusElement))
            {
                return expStatusElement.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
        }

        return string.Empty;
    }

    private static UploadPayloadInfo ReadUploadPayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return new UploadPayloadInfo();
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            return new UploadPayloadInfo
            {
                StationNo = ReadInt(root, "StationNo"),
                WorkOrderId = FirstNonEmpty(ReadString(root, "SN"), ReadString(root, "WorkOrder")),
                ProductNo = FirstNonEmpty(ReadString(root, "ProductNo"), ReadString(root, "ProductNumber"))
            };
        }
        catch (JsonException)
        {
            return new UploadPayloadInfo();
        }
    }

    private static int ReadInt(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) && element.TryGetInt32(out var value)
            ? value
            : 0;
    }

    private static string ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element)
            ? element.GetString() ?? string.Empty
            : string.Empty;
    }

    private static void MarkRetryRequested(BizUploadTask task)
    {
        task.Status = ProductionConstants.UploadStatuses.Pending;
        task.NextRetryTime = DateTime.Now;
        task.Message = "Manual retry requested.";
        task.UpdatedTime = DateTime.Now;
    }

    private BizUploadTask? FindExistingTask(BizUploadTask task)
    {
        return _dbContext.Db.Queryable<BizUploadTask>()
            .First(existing => existing.TaskType == task.TaskType
                && existing.Target == task.Target
                && existing.BusinessId == task.BusinessId);
    }

    private static void Normalize(BizUploadTask task)
    {
        task.TaskType = NormalizeTaskType(task.TaskType);
        task.Target = string.IsNullOrWhiteSpace(task.Target)
            ? ProductionConstants.UploadTargets.Mes
            : task.Target.Trim();
        task.BusinessId = string.IsNullOrWhiteSpace(task.BusinessId)
            ? throw new InvalidOperationException("上传任务业务ID不能为空。")
            : task.BusinessId.Trim();
        task.Status = NormalizeStatus(task.Status);
        task.FilePath = NormalizeNullableText(task.FilePath);
        task.PayloadJson = NormalizeNullableText(task.PayloadJson);
        task.Message = NormalizeNullableText(task.Message);
        task.MaxRetryCount = Math.Max(1, task.MaxRetryCount);
        task.RetryCount = Math.Max(0, task.RetryCount);
    }

    private static string NormalizeStatus(string? status)
    {
        return status switch
        {
            ProductionConstants.UploadStatuses.Pending => ProductionConstants.UploadStatuses.Pending,
            ProductionConstants.UploadStatuses.Uploading => ProductionConstants.UploadStatuses.Uploading,
            ProductionConstants.UploadStatuses.Uploaded => ProductionConstants.UploadStatuses.Uploaded,
            ProductionConstants.UploadStatuses.Failed => ProductionConstants.UploadStatuses.Failed,
            ProductionConstants.UploadStatuses.Retrying => ProductionConstants.UploadStatuses.Retrying,
            ProductionConstants.UploadStatuses.Skipped => ProductionConstants.UploadStatuses.Skipped,
            _ => ProductionConstants.UploadStatuses.Pending
        };
    }

    private static string? NormalizeNullableText(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue)
            ? null
            : normalizedValue;
    }

    private static bool IsActionRequired(string? status)
    {
        return status is ProductionConstants.UploadStatuses.Pending
            or ProductionConstants.UploadStatuses.Failed
            or ProductionConstants.UploadStatuses.Retrying;
    }

    private bool IsWorkOrderStatusReportEnabled()
        => _settingsService.Get().EnableWorkOrderStatusReport != false;

    private static string BuildUploadBusinessId(BizWeldTask task, string uploadKind)
    {
        var stableTaskId = FirstNonEmpty(
            task.ExpStartId,
            task.LocalExpStartId,
            task.Id.ToString("x").PadLeft(32, '0'));

        return $"{stableTaskId}:{uploadKind}";
    }
    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    private UploadTaskSummary ToSummary(BizUploadTask task)
    {
        var payload = ReadUploadPayload(task.PayloadJson);
        var productNos = ProcessParameterUploadPayloadRules.ReadProductNos(task.PayloadJson);
        var productText = productNos.Count > 0
            ? string.Join(", ", productNos)
            : payload.ProductNo;
        var message = task.Message ?? string.Empty;
        var recordKey = string.Equals(
            task.TaskType,
            ProductionConstants.UploadTaskTypes.DeviceStatus,
            StringComparison.OrdinalIgnoreCase)
                ? DeviceStatusRecordIdentityRules.ReadTaskRecordKey(task.BusinessId, task.PayloadJson)
                : null;
        var deviceStatusLog = recordKey is null ? null : _deviceStatusService.GetLog(recordKey);

        return new UploadTaskSummary
        {
            Id = task.Id,
            TaskType = task.TaskType,
            Target = task.Target,
            BusinessId = task.BusinessId ?? string.Empty,
            DeviceStatusRecordKey = recordKey ?? string.Empty,
            TaskIdentity = ResolveTaskSummaryIdentity(task, deviceStatusLog),
            StationNo = deviceStatusLog?.StationNo ?? payload.StationNo,
            ProductNo = productText,
            Status = task.Status,
            IsVirtual = false,
            CanRetry = task.Status != ProductionConstants.UploadStatuses.Uploaded,
            CanDelete = true,
            RetryCount = task.RetryCount,
            MaxRetryCount = task.MaxRetryCount,
            NextRetryTime = task.NextRetryTime,
            LastAttemptTime = task.LastAttemptTime,
            CompletedTime = task.CompletedTime,
            FilePath = ResolveDisplayFilePath(task),
            Message = message,
            DisplayMessage = message,
            CreatedTime = task.CreatedTime,
            UpdatedTime = task.UpdatedTime
        };
    }

    private string ResolveDisplayFilePath(BizUploadTask task)
    {
        if (!string.IsNullOrWhiteSpace(task.FilePath))
        {
            return task.FilePath.Trim();
        }

        if (!string.Equals(task.TaskType, ProductionConstants.UploadTaskTypes.ReportFile, StringComparison.OrdinalIgnoreCase)
            || task.WeldTaskId is null)
        {
            return string.Empty;
        }

        return _dbContext.Db.Queryable<BizProductionReportFile>()
            .Where(report => report.TaskId == task.WeldTaskId.Value)
            .OrderByDescending(report => report.UpdatedTime)
            .First()?.FilePath ?? string.Empty;
    }

    /// <summary>
    /// Creates the event payload for upload task changes.
    /// </summary>
    private static UploadTaskStatusChangedEventArgs ToStatusChangedEvent(BizUploadTask task, string? status = null)
    {
        return new UploadTaskStatusChangedEventArgs
        {
            UploadTaskId = task.Id,
            WeldTaskId = task.WeldTaskId,
            TaskType = task.TaskType,
            Status = status ?? task.Status
        };
    }

    /// <summary>
    /// Raises the upload task status event after database locks have been released.
    /// </summary>
    private void PublishTaskStatusChanged(UploadTaskStatusChangedEventArgs? args)
    {
        if (args is not null)
        {
            TaskStatusChanged?.Invoke(this, args);
        }
    }

    /// <summary>
    /// Resolves the first column text for an upload-status detail row.
    /// Device-status tasks use their status code and name instead of a weld task id.
    /// </summary>
    private string ResolveTaskSummaryIdentity(
        BizUploadTask task,
        BizDeviceStatusLog? deviceStatusLog)
    {
        if (deviceStatusLog is not null)
        {
            return DeviceStatusReportRules.FormatStatusIdentity(deviceStatusLog.DeviceStatus);
        }

        return ResolveTaskIdentity(task);
    }

    /// <summary>
    /// Resolves the operator-facing task id for upload-status detail rows.
    /// </summary>
    private string ResolveTaskIdentity(BizUploadTask task)
    {
        var weldTask = task.WeldTaskId is null
            ? null
            : _dbContext.Db.Queryable<BizWeldTask>().InSingle(task.WeldTaskId.Value);

        return UploadTaskIdentityRules.Resolve(weldTask, task.WeldTaskId, task.BusinessId);
    }

    private sealed record UploadPayloadInfo
    {
        public int StationNo { get; init; }

        public string WorkOrderId { get; init; } = string.Empty;

        public string ProductNo { get; init; } = string.Empty;
    }

    private sealed record ProcessParameterSchemeItem(DimTestItem Item, BizSchemeDetail Detail);

    private static string NormalizeTaskType(string? taskType)
    {
        return taskType switch
        {
            ProductionConstants.UploadTaskTypes.StartReport => ProductionConstants.UploadTaskTypes.StartReport,
            ProductionConstants.UploadTaskTypes.FinishReport => ProductionConstants.UploadTaskTypes.FinishReport,
            ProductionConstants.UploadTaskTypes.WorkOrderStatus => ProductionConstants.UploadTaskTypes.WorkOrderStatus,
            ProductionConstants.UploadTaskTypes.ProcessParameter => ProductionConstants.UploadTaskTypes.ProcessParameter,
            ProductionConstants.UploadTaskTypes.ReportFile => ProductionConstants.UploadTaskTypes.ReportFile,
            ProductionConstants.UploadTaskTypes.ProgramFile => ProductionConstants.UploadTaskTypes.ProgramFile,
            ProductionConstants.UploadTaskTypes.DeviceStatus => ProductionConstants.UploadTaskTypes.DeviceStatus,
            _ => ProductionConstants.UploadTaskTypes.ProcessParameter
        };
    }
}
