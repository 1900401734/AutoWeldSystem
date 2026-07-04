using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Shared visibility rules for upload task rows and retry scopes.
/// </summary>
public static class UploadTaskVisibilityRules
{
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
