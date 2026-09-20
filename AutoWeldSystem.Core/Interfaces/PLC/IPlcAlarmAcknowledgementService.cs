namespace AutoWeldSystem.Core.Interfaces.PLC;

/// <summary>
/// PLC 报警通知的进程级已读状态。
/// 主屏与扩展屏各有一个监控视图实例，已读状态放在共享服务中，任一屏关闭卡片或清除摘要都能同步到另一屏。
/// </summary>
public interface IPlcAlarmAcknowledgementService
{
    /// <summary>
    /// 已读状态发生变化时触发；写入相同值不触发，避免两个视图互相回写形成回环。
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// 已手动关闭通知卡片的报警签名；null 表示当前报警尚未关闭卡片。
    /// </summary>
    string? DismissedNotificationSignature { get; }

    /// <summary>
    /// 已通过“清除”按钮清除右侧异常摘要的报警签名；null 表示摘要尚未清除。
    /// </summary>
    string? DismissedSummarySignature { get; }

    /// <summary>
    /// 监控视图创建时登记；用于判断是否还有视图在使用已读状态。
    /// </summary>
    void Attach();

    /// <summary>
    /// 监控视图销毁时注销；最后一个视图注销后清空已读状态，重新登录时仍在持续的报警会重新弹出。
    /// </summary>
    void Detach();

    /// <summary>
    /// 标记通知卡片已关闭，不影响右侧摘要。
    /// </summary>
    void DismissNotification(string signature);

    /// <summary>
    /// 标记右侧摘要已清除；清除摘要时通知卡片一并关闭，因此同时更新两个签名。
    /// </summary>
    void DismissSummary(string signature);

    /// <summary>
    /// 报警恢复后清空全部已读签名，下一次报警可以重新弹出。
    /// </summary>
    void Reset();
}
