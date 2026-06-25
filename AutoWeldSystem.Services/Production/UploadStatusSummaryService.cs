using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Upload;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 聚合上传总览行。
/// 总览状态以补传任务为优先来源，并使用已落库的业务事实兜底，避免在线成功链路显示为“无数据”。
/// </summary>
public sealed class UploadStatusSummaryService : IUploadStatusSummaryService
{
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

            var recentTasks = _dbContext.Db.Queryable<BizWeldTask>()
                .OrderByDescending(task => task.Id)
                .Take(Math.Max(1, maxCount * 3))
                .ToList();
            var unfinishedTasks = _dbContext.Db.Queryable<BizWeldTask>()
                .Where(task => task.EndTime == null && !task.UploadStateHidden)
                .ToList();
            var tasks = unfinishedTasks
                .Concat(recentTasks)
                .GroupBy(task => task.Id)
                .Select(group => group.First())
                .OrderByDescending(task => task.Id)
                .ToList();

            if (tasks.Count == 0)
            {
                return Array.Empty<UploadPendingSummaryRow>();
            }

            var taskIds = tasks.Select(task => task.Id).ToList();
            var uploadTasks = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.WeldTaskId.HasValue && taskIds.Contains(task.WeldTaskId.Value))
                .Where(task => !task.IsDeleted)
                .ToList();
            var weldPoints = _dbContext.Db.Queryable<BizWeldPointRecord>()
                .Where(record => taskIds.Contains(record.TaskId))
                .ToList();
            var reportFiles = _dbContext.Db.Queryable<BizProductionReportFile>()
                .Where(report => taskIds.Contains(report.TaskId))
                .ToList();
            var taskLookup = tasks.ToDictionary(task => task.Id);

            var rows = tasks
                .Select(task => BuildRow(task, uploadTasks, weldPoints, reportFiles))
                .Where(row => taskLookup.TryGetValue(row.WeldTaskId, out var task)
                    && UploadSummaryVisibilityRules.ShouldShow(task, row.PendingCount))
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

        var row = new UploadPendingSummaryRow
        {
            WeldTaskId = task.Id,
            TaskIdentity = UploadTaskIdentityRules.Resolve(task),
            WorkOrderId = task.SN,
            StationNo = task.StationNo,
            StartReportStatus = UploadSummaryStatusResolver.ResolveStartReportStatus(
                task,
                GetUploadStatuses(scopedUploads, ProductionConstants.UploadTaskTypes.StartReport)),
            ProcessParameterStatus = UploadSummaryStatusResolver.ResolveProcessParameterStatus(
                GetUploadStatuses(scopedUploads, ProductionConstants.UploadTaskTypes.ProcessParameter),
                scopedPoints),
            ReportFileStatus = UploadSummaryStatusResolver.ResolveReportFileStatus(
                GetUploadStatuses(scopedUploads, ProductionConstants.UploadTaskTypes.ReportFile),
                scopedReports),
            FinishReportStatus = UploadSummaryStatusResolver.ResolveFinishReportStatus(
                task,
                GetUploadStatuses(scopedUploads, ProductionConstants.UploadTaskTypes.FinishReport)),
            UpdatedTime = ResolveUpdatedTime(task, scopedUploads, scopedPoints, scopedReports)
        };

        row.PendingCount = CountPendingLike(
            row.StartReportStatus,
            row.ProcessParameterStatus,
            row.ReportFileStatus,
            row.FinishReportStatus);
        return row;
    }

    private static IEnumerable<string?> GetUploadStatuses(IEnumerable<BizUploadTask> tasks, string taskType)
        => tasks
            .Where(task => SameTaskType(task, taskType))
            .Select(task => task.Status);

    private static int CountPendingLike(params string[] statuses)
        => statuses.Count(UploadSummaryStatusResolver.IsPendingLike);

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

    private static bool SameTaskType(BizUploadTask task, string taskType)
    {
        return string.Equals(task.TaskType?.Trim(), taskType, StringComparison.OrdinalIgnoreCase);
    }
}
