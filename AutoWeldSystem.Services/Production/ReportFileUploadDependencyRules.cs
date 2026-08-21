using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 报告文件只能在本地任务完工且 MES 完工上报成功后执行。
/// </summary>
internal static class ReportFileUploadDependencyRules
{
    public static bool IsWeldTaskCompleted(BizWeldTask? task)
    {
        return task?.EndTime is not null
            && string.Equals(
                task.TaskStatus,
                ProductionConstants.ProductInstanceStatuses.Completed,
                StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsFinishReportSatisfied(IEnumerable<BizUploadTask> finishReportTasks)
    {
        ArgumentNullException.ThrowIfNull(finishReportTasks);
        var tasks = finishReportTasks
            .Where(task => !task.IsDeleted)
            .ToList();
        return tasks.Count == 0
            || tasks.Any(task => string.Equals(
                task.Status,
                ProductionConstants.UploadStatuses.Uploaded,
                StringComparison.OrdinalIgnoreCase));
    }

    public static bool ShouldReopenUploadedReport(BizUploadTask existing, BizWeldTask? weldTask)
    {
        ArgumentNullException.ThrowIfNull(existing);
        return string.Equals(
                existing.TaskType,
                ProductionConstants.UploadTaskTypes.ReportFile,
                StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                existing.Status,
                ProductionConstants.UploadStatuses.Uploaded,
                StringComparison.OrdinalIgnoreCase)
            && IsWeldTaskCompleted(weldTask)
            && (!existing.CompletedTime.HasValue || existing.CompletedTime.Value < weldTask!.EndTime!.Value);
    }
}
