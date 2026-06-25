using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Centralizes upload-summary visibility rules so UI and services do not duplicate task filtering logic.
/// </summary>
public static class UploadSummaryVisibilityRules
{
    /// <summary>
    /// Returns whether a weld task should appear in the upload summary.
    /// </summary>
    /// <param name="task">Weld task.</param>
    /// <param name="pendingCount">Number of upload items that still need operator attention.</param>
    /// <returns>True when the row must be shown.</returns>
    public static bool ShouldShow(BizWeldTask task, int pendingCount)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.UploadStateHidden)
        {
            return false;
        }

        return task.EndTime is null || pendingCount > 0;
    }
}
