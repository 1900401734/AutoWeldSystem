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
    /// 判断当前是否应保护流转卡号输入框不被程序化回填。
    /// 现场约束：PLC 持续写入工单号数据块且内容不含回车，界面刷新周期和 PLC 快照都会把
    /// 逐字符输入整段覆盖，导致流转卡号无法手动输完。草稿标志会被刷新周期在多处清零，
    /// 因此这里以「输入框有焦点」为主判据——焦点由操作系统维护，是操作员正在输入的事实证据；
    /// 失去焦点后继续按该工位保存的未确认草稿文本保护，使输入途中去点别的控件再回来不丢草稿。
    /// </summary>
    /// <param name="inputFocused">流转卡号输入框当前是否有焦点。</param>
    /// <param name="draftWorkId">该工位保存的未确认草稿文本。</param>
    /// <param name="visibleWorkId">输入框当前可见文本。</param>
    public static bool ShouldProtectManualInput(bool inputFocused, string? draftWorkId, string? visibleWorkId)
    {
        if (inputFocused)
        {
            return true;
        }

        var draft = Normalize(draftWorkId);
        return !string.IsNullOrEmpty(draft)
            && string.Equals(draft, Normalize(visibleWorkId), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 输入框被清空视为操作员撤销草稿，允许 PLC 值重新接管回填。
    /// </summary>
    public static bool IsDraftCleared(string? visibleWorkId)
        => string.IsNullOrWhiteSpace(Normalize(visibleWorkId));

    /// <summary>
    /// 判断已确认（回车固化）的手输工单号是否应继续锁定输入框，拒绝 PLC 值覆盖。
    /// 现场约束：PLC 工单号寄存器常驻有值，确认后若仍允许回填，手输工单会立刻被寄存器里的旧值顶掉。
    /// 因此确认值一旦生效即锁定，必须由 PLC 送来「新一次扫码」才解锁——判据是 PLC 值发生变化，
    /// 与上次已处理的扫码值不同即视为新扫码（同值重复扫码由调用方在 PLC 清空后重建基线来识别）。
    /// </summary>
    /// <param name="confirmedWorkId">该工位已回车确认的工单号。</param>
    /// <param name="visibleWorkId">输入框当前可见文本。</param>
    /// <param name="incomingPlcWorkId">本次 PLC 快照的工单号。</param>
    /// <param name="lastHandledPlcWorkId">上一次已处理的 PLC 工单号。</param>
    public static bool ShouldKeepConfirmedWorkOrder(
        string? confirmedWorkId,
        string? visibleWorkId,
        string? incomingPlcWorkId,
        string? lastHandledPlcWorkId)
    {
        var confirmed = Normalize(confirmedWorkId);
        if (string.IsNullOrEmpty(confirmed)
            || !string.Equals(confirmed, Normalize(visibleWorkId), StringComparison.OrdinalIgnoreCase))
        {
            // 没有确认值，或界面已被操作员改成别的内容，锁定不再成立。
            return false;
        }

        // PLC 送来与上次不同的值即为新一次扫码，允许覆盖已确认的手输工单。
        return !IsNewPlcScan(incomingPlcWorkId, lastHandledPlcWorkId);
    }

    /// <summary>
    /// 判断本次 PLC 工单号是否属于新一次扫码：与上次已处理值不同即为新扫码。
    /// </summary>
    public static bool IsNewPlcScan(string? incomingPlcWorkId, string? lastHandledPlcWorkId)
    {
        var incoming = Normalize(incomingPlcWorkId);
        if (string.IsNullOrEmpty(incoming))
        {
            return false;
        }

        return !string.Equals(incoming, Normalize(lastHandledPlcWorkId), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Trims a work-order value for comparison and persistence in the view state.
    /// </summary>
    public static string Normalize(string? workId)
    {
        return string.IsNullOrWhiteSpace(workId) ? string.Empty : workId.Trim();
    }
}