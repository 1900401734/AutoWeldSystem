using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Resolves the user-facing task id shown on upload status screens.
/// The internal BizUploadTask.BusinessId remains a de-duplication key and is not suitable for operators.
/// </summary>
public static class UploadTaskIdentityRules
{
    /// <summary>
    /// Resolves the task id by business priority: MES id, local 32-char id, local database id fallback.
    /// </summary>
    /// <param name="task">Related weld task, when available.</param>
    /// <param name="fallbackTaskId">Database task id used when the task row could not be loaded.</param>
    /// <param name="fallbackText">Final fallback text for non-task upload rows.</param>
    /// <returns>User-facing task id text.</returns>
    public static string Resolve(BizWeldTask? task, int? fallbackTaskId = null, string? fallbackText = null)
    {
        if (!string.IsNullOrWhiteSpace(task?.ExpStartId))
        {
            return task.ExpStartId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(task?.LocalExpStartId))
        {
            return task.LocalExpStartId.Trim();
        }

        if (task?.Id > 0)
        {
            return ToFixedLocalId(task.Id);
        }

        if (fallbackTaskId is > 0)
        {
            return ToFixedLocalId(fallbackTaskId.Value);
        }

        return fallbackText?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Converts an integer database id into the existing fixed-width local id fallback format.
    /// </summary>
    private static string ToFixedLocalId(int taskId)
    {
        return taskId.ToString("x").PadLeft(32, '0');
    }
}
