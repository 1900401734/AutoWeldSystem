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
    /// 判断操作员是否正在手输流转卡号，PLC 快照必须为该草稿让行。
    /// 现场约束：PLC 持续驱动工单号寄存器，轮询回填会把逐字符输入的半截工单号整段覆盖，
    /// 使流转卡号无法手动输完。输入框已被清空时视为撤销草稿，允许 PLC 重新接管。
    /// </summary>
    /// <param name="offlineEditedByUser">离线待开工草稿标志。</param>
    /// <param name="manualEditedByUser">在线人工输入草稿标志。</param>
    /// <param name="visibleWorkId">输入框当前可见文本。</param>
    public static bool HasManualDraft(bool offlineEditedByUser, bool manualEditedByUser, string? visibleWorkId)
    {
        return (offlineEditedByUser || manualEditedByUser)
            && !string.IsNullOrWhiteSpace(Normalize(visibleWorkId));
    }

    /// <summary>
    /// Trims a work-order value for comparison and persistence in the view state.
    /// </summary>
    public static string Normalize(string? workId)
    {
        return string.IsNullOrWhiteSpace(workId) ? string.Empty : workId.Trim();
    }
}