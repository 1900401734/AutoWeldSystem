using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Shared visibility rules for upload task rows and retry scopes.
/// </summary>
public static class UploadTaskVisibilityRules
{
    /// <summary>
    /// 判断任务是否属于 MES 过程参数上传。
    /// CentralServer 任务不应进入过程参数页签或过程参数汇总状态。
    /// </summary>
    public static bool IsMesProcessParameterTask(BizUploadTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        return string.Equals(task.TaskType?.Trim(), ProductionConstants.UploadTaskTypes.ProcessParameter, StringComparison.OrdinalIgnoreCase)
            && string.Equals(task.Target?.Trim(), ProductionConstants.UploadTargets.Mes, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns whether a task should be included in upload-state lists or retry ranges.
    /// </summary>
    /// <param name="task">Upload task.</param>
    /// <param name="includeCompleted">Whether uploaded rows should still be included.</param>
    /// <returns>True when the upload task is visible/actionable.</returns>
    public static bool ShouldInclude(BizUploadTask task, bool includeCompleted)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.IsDeleted)
        {
            return false;
        }

        return includeCompleted
            || !string.Equals(task.Status, ProductionConstants.UploadStatuses.Uploaded, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns whether a task can be executed by retry logic.
    /// Skipped tasks are terminal because an upload switch intentionally disabled them.
    /// </summary>
    public static bool ShouldRetry(BizUploadTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.IsDeleted)
        {
            return false;
        }

        return task.Status is ProductionConstants.UploadStatuses.Pending
            or ProductionConstants.UploadStatuses.Failed
            or ProductionConstants.UploadStatuses.Retrying;
    }
}
