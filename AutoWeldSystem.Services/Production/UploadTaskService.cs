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
    private readonly object _dbLock = new();

    public UploadTaskService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
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
