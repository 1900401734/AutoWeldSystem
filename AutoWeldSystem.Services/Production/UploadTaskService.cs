using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 通用上传任务服务实现。
/// 当前先提供查询和人工重试排队能力，后续上传执行器可复用同一张任务表。
/// </summary>
public class UploadTaskService : IUploadTaskService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IMesProvider _mesProvider;
    private readonly object _dbLock = new();

    public UploadTaskService(SqlSugarDbContext dbContext, IMesProvider mesProvider)
    {
        _dbContext = dbContext;
        _mesProvider = mesProvider;
    }

    public IReadOnlyList<UploadTaskSummary> GetTasks(string taskType, bool includeCompleted = false)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var normalizedTaskType = NormalizeTaskType(taskType);
            var query = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.TaskType == normalizedTaskType);

            if (!includeCompleted)
            {
                query = query.Where(task => task.Status != ProductionConstants.UploadStatuses.Uploaded);
            }

            return query.ToList()
                .OrderByDescending(task => IsActionRequired(task.Status))
                .ThenByDescending(task => task.UpdatedTime)
                .Select(ToSummary)
                .ToList();
        }
    }

    public UploadTaskSummary? GetById(int id)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(id);
            return task is null ? null : ToSummary(task);
        }
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
            existing.Message = task.Message;
            existing.UpdatedTime = DateTime.Now;

            _dbContext.Db.Updateable(existing).ExecuteCommand();
            return _dbContext.Db.Queryable<BizUploadTask>().InSingle(existing.Id) ?? existing;
        }
    }

    public async Task<UploadTaskSummary?> ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        var task = MarkUploading(id);
        if (task is null)
        {
            return null;
        }

        var response = await ExecuteByTypeAsync(task, cancellationToken);
        return FinishExecution(task.Id, response);
    }

    public async Task<int> ExecuteAllPendingAsync(string taskType, CancellationToken cancellationToken = default)
    {
        List<int> taskIds;
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var normalizedTaskType = NormalizeTaskType(taskType);
            taskIds = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.TaskType == normalizedTaskType
                    && task.Status != ProductionConstants.UploadStatuses.Uploaded)
                .ToList()
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
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(id);
            if (task is null || task.Status == ProductionConstants.UploadStatuses.Uploaded)
            {
                return;
            }

            MarkRetryRequested(task);
            _dbContext.Db.Updateable(task).ExecuteCommand();
        }
    }

    public int RequestRetryAll(string taskType)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var normalizedTaskType = NormalizeTaskType(taskType);
            var tasks = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.TaskType == normalizedTaskType
                    && task.Status != ProductionConstants.UploadStatuses.Uploaded)
                .ToList();

            foreach (var task in tasks)
            {
                MarkRetryRequested(task);
                _dbContext.Db.Updateable(task).ExecuteCommand();
            }

            return tasks.Count;
        }
    }

    private BizUploadTask? MarkUploading(int id)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(id);
            if (task is null || task.Status == ProductionConstants.UploadStatuses.Uploaded)
            {
                return task;
            }

            task.Status = ProductionConstants.UploadStatuses.Uploading;
            task.LastAttemptTime = DateTime.Now;
            task.RetryCount++;
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(task).ExecuteCommand();
            return task;
        }
    }

    private async Task<MesBaseResponse<object>> ExecuteByTypeAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        return task.TaskType switch
        {
            ProductionConstants.UploadTaskTypes.ReportFile => await UploadReportFileAsync(task, cancellationToken),
            ProductionConstants.UploadTaskTypes.ProcessParameter => Unsupported("过程参数上传执行器尚未接入。"),
            ProductionConstants.UploadTaskTypes.ProgramFile => Unsupported("程序文件上传由程序管理服务处理。"),
            _ => Unsupported($"暂不支持的上传任务类型：{task.TaskType}")
        };
    }

    private async Task<MesBaseResponse<object>> UploadReportFileAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        var request = BuildReportFileRequest(task);
        if (request is null)
        {
            return Unsupported("报告文件任务缺少工单或文件路径信息。");
        }

        return await _mesProvider.UploadReportFileAsync(request, cancellationToken);
    }

    private ReportFileUploadRequest? BuildReportFileRequest(BizUploadTask task)
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

            var filePath = task.FilePath;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                filePath = _dbContext.Db.Queryable<BizProductionReportFile>()
                    .Where(report => report.TaskId == weldTask.Id)
                    .ToList()
                    .OrderByDescending(report => report.UpdatedTime)
                    .Select(report => report.FilePath)
                    .FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            return new ReportFileUploadRequest
            {
                ExpStartId = weldTask.ExpStartId ?? string.Empty,
                DeviceId = weldTask.DeviceId,
                SN = weldTask.WorkOrderId,
                ProcessNo = weldTask.ProcessNo,
                FileType = ProductionConstants.MesFileTypes.ReportFile,
                FilePath = filePath
            };
        }
    }

    private UploadTaskSummary? FinishExecution(int taskId, MesBaseResponse<object> response)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(taskId);
            if (task is null)
            {
                return null;
            }

            task.Status = response.IsSuccess
                ? ProductionConstants.UploadStatuses.Uploaded
                : ProductionConstants.UploadStatuses.Failed;
            task.Message = response.Msg;
            task.CompletedTime = response.IsSuccess ? DateTime.Now : null;
            task.NextRetryTime = response.IsSuccess ? null : DateTime.Now.AddMinutes(1);
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(task).ExecuteCommand();
            UpdateReportFileStatus(task, response);
            return ToSummary(task);
        }
    }

    private void UpdateReportFileStatus(BizUploadTask task, MesBaseResponse<object> response)
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

    private static MesBaseResponse<object> Unsupported(string message)
    {
        return new MesBaseResponse<object>
        {
            Status = AppConstants.MesStatus.Error,
            Msg = message
        };
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

    private static UploadTaskSummary ToSummary(BizUploadTask task)
    {
        return new UploadTaskSummary
        {
            Id = task.Id,
            TaskType = task.TaskType,
            Target = task.Target,
            BusinessId = task.BusinessId ?? string.Empty,
            Status = task.Status,
            RetryCount = task.RetryCount,
            MaxRetryCount = task.MaxRetryCount,
            NextRetryTime = task.NextRetryTime,
            LastAttemptTime = task.LastAttemptTime,
            CompletedTime = task.CompletedTime,
            FilePath = task.FilePath ?? string.Empty,
            Message = task.Message ?? string.Empty,
            CreatedTime = task.CreatedTime,
            UpdatedTime = task.UpdatedTime
        };
    }

    private static string NormalizeTaskType(string? taskType)
    {
        return taskType switch
        {
            ProductionConstants.UploadTaskTypes.ProcessParameter => ProductionConstants.UploadTaskTypes.ProcessParameter,
            ProductionConstants.UploadTaskTypes.ReportFile => ProductionConstants.UploadTaskTypes.ReportFile,
            ProductionConstants.UploadTaskTypes.ProgramFile => ProductionConstants.UploadTaskTypes.ProgramFile,
            ProductionConstants.UploadTaskTypes.DeviceStatus => ProductionConstants.UploadTaskTypes.DeviceStatus,
            _ => ProductionConstants.UploadTaskTypes.ProcessParameter
        };
    }
}
