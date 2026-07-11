namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Provides the shared confirmation rules for manually entered and PLC-provided work-order numbers.
/// </summary>
public static class WorkOrderInputConfirmationRules
{
    /// <summary>
    /// Returns whether the visible work-order text matches the most recently confirmed value.
    /// </summary>
    public static bool IsConfirmed(string? visibleWorkId, string? confirmedWorkId)
    {
        return string.Equals(
            Normalize(visibleWorkId),
            Normalize(confirmedWorkId),
            StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(Normalize(confirmedWorkId));
    }

    /// <summary>
    /// Returns whether a PLC snapshot may replace the work-order input immediately.
    /// </summary>
    public static bool ShouldApplyPlcSnapshot(bool stationIsIdle, bool readSucceeded, string? workId)
    {
        return stationIsIdle
            && readSucceeded
            && !string.IsNullOrWhiteSpace(Normalize(workId));
    }

    /// <summary>
    /// Trims a work-order value for comparison and persistence in the view state.
    /// </summary>
    public static string Normalize(string? workId)
    {
        return string.IsNullOrWhiteSpace(workId) ? string.Empty : workId.Trim();
    }
}