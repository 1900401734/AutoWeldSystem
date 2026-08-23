using System.Diagnostics;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.DataManagement;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 级联删除历史工单及其关联数据。
/// 多表删除放在同一事务，避免部分删除留下孤儿采集记录和报表记录。
/// </summary>
public sealed class DataHistoryMaintenanceService : IDataHistoryMaintenanceService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _appSettingsService;
    private readonly IOperationLogService _operationLogService;
    private readonly object _dbLock = new();

    public DataHistoryMaintenanceService(
        SqlSugarDbContext dbContext,
        IAppSettingsService appSettingsService,
        IOperationLogService operationLogService)
    {
        _dbContext = dbContext;
        _appSettingsService = appSettingsService;
        _operationLogService = operationLogService;
    }

    public Task<WorkOrderDeletionPreview> PreviewDeleteByIdsAsync(
        IReadOnlyCollection<int> taskIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskIds);
        var ids = NormalizeIds(taskIds);
        return RunAsync(() => Preview(ids), cancellationToken);
    }

    public Task<WorkOrderDeletionPreview> PreviewDeleteFailedAsync(
        CancellationToken cancellationToken = default)
    {
        return RunAsync(() => Preview(QueryFailedIds()), cancellationToken);
    }

    public Task<WorkOrderDeletionPreview> PreviewDeleteByDateAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(() => Preview(QueryIdsByDate(startTime, endTime)), cancellationToken);
    }

    public Task<WorkOrderDeletionResult> DeleteByIdsAsync(
        IReadOnlyCollection<int> taskIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskIds);
        var ids = NormalizeIds(taskIds);
        return RunAsync(() => DeleteCore(ids, $"按选中工单删除（{ids.Count} 项）"), cancellationToken);
    }

    public Task<WorkOrderDeletionResult> DeleteFailedAsync(
        CancellationToken cancellationToken = default)
    {
        return RunAsync(() => DeleteCore(QueryFailedIds(), "清理上传失败工单"), cancellationToken);
    }

    public Task<WorkOrderDeletionResult> DeleteByDateAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            () => DeleteCore(
                QueryIdsByDate(startTime, endTime),
                $"按日期清理（{startTime:yyyy-MM-dd} 至 {endTime:yyyy-MM-dd}）"),
            cancellationToken);
    }

    public WorkOrderDeletionResult DeleteWorkOrder(int taskId)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            return DeleteCore([taskId], $"删除工单（ID {taskId}）");
        }
    }

    private Task<T> RunAsync<T>(Func<T> operation, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default!;
            }

            lock (_dbLock)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return default!;
                }

                _dbContext.InitDatabase();
                return operation();
            }
        });
    }

    private static List<int> NormalizeIds(IReadOnlyCollection<int> taskIds)
    {
        return taskIds.Where(static id => id > 0).Distinct().ToList();
    }

    private List<int> QueryFailedIds()
    {
        return _dbContext.Db.Queryable<BizWeldTask>()
            .Where(task => task.UploadStatus == ProductionConstants.UploadStatuses.Failed)
            .Select(task => task.Id)
            .ToList();
    }

    private List<int> QueryIdsByDate(DateTime startTime, DateTime endTime)
    {
        var normalizedEnd = endTime < startTime ? startTime : endTime;
        return _dbContext.Db.Queryable<BizWeldTask>()
            .Where(task => task.StartTime >= startTime && task.StartTime <= normalizedEnd)
            .Select(task => task.Id)
            .ToList();
    }

    private WorkOrderDeletionPreview Preview(List<int> candidateIds)
    {
        if (candidateIds.Count == 0)
        {
            return new WorkOrderDeletionPreview();
        }

        var statuses = _dbContext.Db.Queryable<BizWeldTask>()
            .Where(task => candidateIds.Contains(task.Id))
            .Select(task => new { task.Id, task.TaskStatus })
            .ToList();

        var deletableIds = statuses
            .Where(item => WorkOrderDeletionRules.CanDelete(item.TaskStatus))
            .Select(item => item.Id)
            .ToList();
        var skipped = statuses.Count - deletableIds.Count;

        if (deletableIds.Count == 0)
        {
            return new WorkOrderDeletionPreview { SkippedRunningCount = skipped };
        }

        var recordCount = _dbContext.Db.Queryable<BizWeldPointRecord>()
            .Where(record => deletableIds.Contains(record.TaskId))
            .Count();
        var reportFileCount = _dbContext.Db.Queryable<BizProductionReportFile>()
            .Where(file => deletableIds.Contains(file.TaskId))
            .Count();

        return new WorkOrderDeletionPreview
        {
            WorkOrderCount = deletableIds.Count,
            SkippedRunningCount = skipped,
            RecordCount = recordCount,
            ReportFileCount = reportFileCount
        };
    }

    private WorkOrderDeletionResult DeleteCore(List<int> candidateIds, string auditScope)
    {
        if (candidateIds.Count == 0)
        {
            return new WorkOrderDeletionResult();
        }

        var statuses = _dbContext.Db.Queryable<BizWeldTask>()
            .Where(task => candidateIds.Contains(task.Id))
            .Select(task => new { task.Id, task.TaskStatus })
            .ToList();

        // 运行中工单仍被 ProductionRuntimeState 引用，直接跳过而不是连带删除
        var ids = statuses
            .Where(item => WorkOrderDeletionRules.CanDelete(item.TaskStatus))
            .Select(item => item.Id)
            .ToList();
        var skipped = statuses.Count - ids.Count;
        if (ids.Count == 0)
        {
            return new WorkOrderDeletionResult { SkippedRunningCount = skipped };
        }

        // 事务提交后数据库行已不存在，因此先取出磁盘路径
        var reportPaths = _dbContext.Db.Queryable<BizProductionReportFile>()
            .Where(file => ids.Contains(file.TaskId))
            .Select(file => file.FilePath)
            .ToList();

        var deletedRecordCount = 0;
        var tran = _dbContext.Db.Ado.UseTran(() =>
        {
            _dbContext.Db.Deleteable<BizUploadTask>()
                .Where(uploadTask => uploadTask.WeldTaskId.HasValue && ids.Contains(uploadTask.WeldTaskId.Value))
                .ExecuteCommand();
            deletedRecordCount = _dbContext.Db.Deleteable<BizWeldPointRecord>()
                .Where(record => ids.Contains(record.TaskId))
                .ExecuteCommand();
            _dbContext.Db.Deleteable<BizProductionReportFile>()
                .Where(file => ids.Contains(file.TaskId))
                .ExecuteCommand();
            _dbContext.Db.Deleteable<BizWeldData>()
                .Where(data => ids.Contains(data.TaskId))
                .ExecuteCommand();
            _dbContext.Db.Deleteable<BizWeldTask>()
                .Where(task => ids.Contains(task.Id))
                .ExecuteCommand();
        });
        if (!tran.IsSuccess)
        {
            throw new InvalidOperationException(
                $"删除历史工单失败：{tran.ErrorException?.Message ?? "未知数据库错误"}",
                tran.ErrorException);
        }

        var (deletedFiles, failedFiles) = DeleteReportFiles(reportPaths);

        var result = new WorkOrderDeletionResult
        {
            DeletedWorkOrderCount = ids.Count,
            SkippedRunningCount = skipped,
            DeletedRecordCount = deletedRecordCount,
            DeletedReportFileCount = deletedFiles,
            FailedFileDeletionCount = failedFiles
        };

        WriteAuditLog(auditScope, result);
        return result;
    }

    /// <summary>
    /// 删除磁盘报表文件。数据库事务已提交，因此文件删除失败只累计计数，不影响删除结果。
    /// </summary>
    private (int Deleted, int Failed) DeleteReportFiles(IReadOnlyCollection<string?> reportPaths)
    {
        var reportRoot = WorkOrderDeletionRules.ResolveReportRootDirectory(_appSettingsService.Get()?.DataDirectory);
        var deleted = 0;
        var failed = 0;

        foreach (var path in reportPaths)
        {
            if (!WorkOrderDeletionRules.IsDeletableReportPath(path, reportRoot))
            {
                continue;
            }

            try
            {
                var fullPath = Path.GetFullPath(path!.Trim());
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                File.Delete(fullPath);
                deleted++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // 报表被 Excel 占用或权限不足时保留磁盘文件
                failed++;
                Trace.TraceWarning($"删除报表文件失败：{path}，原因：{ex.Message}");
            }
        }

        return (deleted, failed);
    }

    private void WriteAuditLog(string auditScope, WorkOrderDeletionResult result)
    {
        try
        {
            var detail = $"{auditScope}；删除工单 {result.DeletedWorkOrderCount} 条，"
                + $"采集记录 {result.DeletedRecordCount} 条，报表文件 {result.DeletedReportFileCount} 个"
                + $"，跳过运行中工单 {result.SkippedRunningCount} 条"
                + $"，报表文件删除失败 {result.FailedFileDeletionCount} 个";
            _operationLogService.Write("WorkOrderDelete", detail, "Warning");
        }
        catch (Exception ex)
        {
            // 审计日志写入失败不能回滚已完成的删除
            Trace.TraceWarning($"写入历史工单删除审计日志失败：{ex.Message}");
        }
    }
}
