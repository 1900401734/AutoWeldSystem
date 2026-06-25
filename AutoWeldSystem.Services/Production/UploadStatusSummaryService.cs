using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Upload;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 上传总览聚合服务。
/// 只聚合任务补传链路：开工、过程参数、xlsx 报表、完工；程序同步和设备/工单状态不进入总览。
/// </summary>
public sealed class UploadStatusSummaryService : IUploadStatusSummaryService
{
    private const string NoData = "无数据";
    private const string Pending = "待上传";
    private const string Uploading = "上传中";
    private const string Failed = "失败";
    private const string Uploaded = "已上传";

    private readonly SqlSugarDbContext _dbContext;
    private readonly object _dbLock = new();

    public UploadStatusSummaryService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<UploadPendingSummaryRow> GetSummary(int maxCount = 200)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();

            var tasks = _dbContext.Db.Queryable<BizWeldTask>()
                .OrderByDescending(task => task.Id)
                .Take(Math.Max(1, maxCount * 3))
                .ToList();
            if (tasks.Count == 0)
            {
                return Array.Empty<UploadPendingSummaryRow>();
            }

            var taskIds = tasks.Select(task => task.Id).ToList();
            var uploadTasks = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.WeldTaskId.HasValue && taskIds.Contains(task.WeldTaskId.Value))
                .ToList();
            var weldPoints = _dbContext.Db.Queryable<BizWeldPointRecord>()
                .Where(record => taskIds.Contains(record.TaskId))
                .ToList();
            var reportFiles = _dbContext.Db.Queryable<BizProductionReportFile>()
                .Where(report => taskIds.Contains(report.TaskId))
                .ToList();

            var rows = tasks
                .Select(task => BuildRow(task, uploadTasks, weldPoints, reportFiles))
                .Where(row => row.PendingCount > 0)
                .Where(row => !string.Equals(row.FinishReportStatus, Uploaded, StringComparison.Ordinal))
                .OrderByDescending(row => row.PendingCount)
                .ThenByDescending(row => row.UpdatedTime)
                .Take(Math.Max(1, maxCount))
                .ToList();

            for (var index = 0; index < rows.Count; index++)
            {
                rows[index].SequenceNo = index + 1;
            }

            return rows;
        }
    }

    private static UploadPendingSummaryRow BuildRow(
        BizWeldTask task,
        IReadOnlyList<BizUploadTask> uploadTasks,
        IReadOnlyList<BizWeldPointRecord> weldPoints,
        IReadOnlyList<BizProductionReportFile> reportFiles)
    {
        var scopedUploads = uploadTasks.Where(item => item.WeldTaskId == task.Id).ToList();
        var scopedPoints = weldPoints.Where(item => item.TaskId == task.Id).ToList();
        var scopedReports = reportFiles.Where(item => item.TaskId == task.Id).ToList();

        var processStatuses = scopedUploads
            .Where(item => SameTaskType(item, ProductionConstants.UploadTaskTypes.ProcessParameter))
            .Select(item => item.Status)
            .Concat(scopedPoints.Select(point => point.UploadStatus));
        var reportStatuses = scopedUploads
            .Where(item => SameTaskType(item, ProductionConstants.UploadTaskTypes.ReportFile))
            .Select(item => item.Status)
            .Concat(scopedReports.Select(report => report.UploadStatus));

        var row = new UploadPendingSummaryRow
        {
            TaskIdentity = ResolveTaskIdentity(task),
            WorkOrderId = task.SN,
            StationNo = task.StationNo,
            StartReportStatus = AggregateUploadTasks(scopedUploads, ProductionConstants.UploadTaskTypes.StartReport),
            ProcessParameterStatus = AggregateStatuses(processStatuses),
            ReportFileStatus = AggregateStatuses(reportStatuses),
            FinishReportStatus = AggregateUploadTasks(scopedUploads, ProductionConstants.UploadTaskTypes.FinishReport),
            UpdatedTime = ResolveUpdatedTime(task, scopedUploads, scopedPoints, scopedReports)
        };

        row.PendingCount = CountPendingLike(
            row.StartReportStatus,
            row.ProcessParameterStatus,
            row.ReportFileStatus,
            row.FinishReportStatus);
        return row;
    }

    private static string AggregateUploadTasks(IEnumerable<BizUploadTask> tasks, string taskType)
    {
        return AggregateStatuses(tasks
            .Where(task => SameTaskType(task, taskType))
            .Select(task => task.Status));
    }

    private static string AggregateStatuses(IEnumerable<string?> statuses)
    {
        var normalized = statuses
            .Select(status => status?.Trim())
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (normalized.Count == 0)
        {
            return NoData;
        }

        if (normalized.Any(status => SameStatus(status, ProductionConstants.UploadStatuses.Uploading)))
        {
            return Uploading;
        }

        if (normalized.Any(status => SameStatus(status, ProductionConstants.UploadStatuses.Failed)
            || SameStatus(status, ProductionConstants.UploadStatuses.Retrying)))
        {
            return Failed;
        }

        if (normalized.Any(status => SameStatus(status, ProductionConstants.UploadStatuses.Pending)))
        {
            return Pending;
        }

        return Uploaded;
    }

    private static int CountPendingLike(params string[] statuses)
    {
        return statuses.Count(status =>
            string.Equals(status, Pending, StringComparison.Ordinal)
            || string.Equals(status, Uploading, StringComparison.Ordinal)
            || string.Equals(status, Failed, StringComparison.Ordinal));
    }

    private static DateTime ResolveUpdatedTime(
        BizWeldTask task,
        IReadOnlyList<BizUploadTask> uploadTasks,
        IReadOnlyList<BizWeldPointRecord> weldPoints,
        IReadOnlyList<BizProductionReportFile> reportFiles)
    {
        var times = new List<DateTime> { task.EndTime ?? task.StartTime };
        times.AddRange(uploadTasks.Select(item => item.UpdatedTime));
        times.AddRange(weldPoints.Select(item => item.UploadTime ?? item.Ts));
        times.AddRange(reportFiles.Select(item => item.UpdatedTime));
        return times.Max();
    }

    private static string ResolveTaskIdentity(BizWeldTask task)
    {
        if (!string.IsNullOrWhiteSpace(task.ExpStartId))
        {
            return task.ExpStartId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(task.LocalExpStartId))
        {
            return task.LocalExpStartId.Trim();
        }

        return task.Id.ToString("x").PadLeft(32, '0');
    }

    private static bool SameStatus(string? left, string right)
    {
        return string.Equals(left?.Trim(), right, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SameTaskType(BizUploadTask task, string taskType)
    {
        return string.Equals(task.TaskType?.Trim(), taskType, StringComparison.OrdinalIgnoreCase);
    }
}
