namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Decides whether a PLC work-order snapshot should trigger an automatic MES work-order query.
/// </summary>
public static class WorkOrderAutoQueryRules
{
    /// <summary>
    /// Returns true only when the snapshot represents a new queryable work order for an idle online station.
    /// </summary>
    /// <param name="mesConnected">Whether MES is currently online.</param>
    /// <param name="hasRunningTask">Whether the station already has an unfinished task.</param>
    /// <param name="workIdReadSuccess">Whether the PLC work-order address was read successfully.</param>
    /// <param name="workId">Work order read from PLC or entered on the screen.</param>
    /// <param name="lastRequestedWorkId">Last work order that has already been automatically queried for this station.</param>
    /// <param name="queryInProgress">Whether an automatic query is already running for this station.</param>
    /// <returns>true when automatic query should start; otherwise false.</returns>
    public static bool ShouldAutoQuery(
        bool mesConnected,
        bool hasRunningTask,
        bool workIdReadSuccess,
        string? workId,
        string? lastRequestedWorkId,
        bool queryInProgress)
    {
        if (!mesConnected || hasRunningTask || !workIdReadSuccess || queryInProgress)
        {
            return false;
        }

        var normalizedWorkId = Normalize(workId);
        if (string.IsNullOrWhiteSpace(normalizedWorkId))
        {
            return false;
        }

        return !string.Equals(
            normalizedWorkId,
            Normalize(lastRequestedWorkId),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
