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

    /// <summary>
    /// 判断 PLC 是否已成功把工单号寄存器清空；空格写入会在此统一视为空字符串。
    /// </summary>
    public static bool ShouldResetAfterPlcClear(bool readSuccess, string? workId)
    {
        return readSuccess && string.IsNullOrWhiteSpace(Normalize(workId));
    }

    /// <summary>
    /// 判断本次工位快照是否属于启动后的首个读数，只能作为基准值记录而不能触发业务动作。
    /// 现场 PLC 会在梯形图里持续驱动工单号寄存器，上位机无法清空，
    /// 因此程序启动时寄存器里往往残留上一轮的条码；若直接使用会误判为新扫码。
    /// </summary>
    /// <param name="hasBaseline">该工位是否已经记录过基准工单号。</param>
    /// <param name="readSuccess">PLC 工单号地址是否读取成功。</param>
    /// <param name="workId">本次读到的工单号。</param>
    /// <returns>true 表示只记录基准值，不得填充界面或查询 MES。</returns>
    public static bool ShouldCaptureBaselineOnly(bool hasBaseline, bool readSuccess, string? workId)
    {
        return !hasBaseline
            && readSuccess
            && !string.IsNullOrWhiteSpace(Normalize(workId));
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
