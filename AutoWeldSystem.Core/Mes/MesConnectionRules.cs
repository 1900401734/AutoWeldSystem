namespace AutoWeldSystem.Core.Mes;

/// <summary>
/// MES 在线检测心跳规则。
/// 心跳只做轻量探活，间隔由现场按网络状况配置。
/// </summary>
public static class MesConnectionRules
{
    public const int DefaultHeartbeatIntervalSeconds = 5;

    public const int MinHeartbeatIntervalSeconds = 1;

    public const int MaxHeartbeatIntervalSeconds = 300;

    /// <summary>
    /// 连续探测失败达到该次数后，才确认 MES 离线。
    /// </summary>
    public const int OfflineFailureThreshold = 3;

    /// <summary>
    /// 自动在线探测的独立超时时间，不跟随业务 MES 请求超时设置。
    /// </summary>
    public const int OnlineProbeTimeoutSeconds = 3;

    /// <summary>
    /// 未确认离线期间的失败重探间隔（秒）。
    /// 现场约束：确认离线需要连续三次失败，若每次都等完整心跳间隔，
    /// 在线转离线最快也要 3 倍心跳间隔（默认 15 秒），改地址或断网后指示灯迟迟不变。
    /// 失败后改用该短间隔重探，把确认离线压缩到数秒，同时不增加在线态的轮询开销。
    /// </summary>
    public const int FailureRetryIntervalSeconds = 1;

    /// <summary>
    /// 判断连续失败次数是否已达到离线确认阈值。
    /// </summary>
    public static bool IsOfflineConfirmed(int consecutiveFailures)
        => consecutiveFailures >= OfflineFailureThreshold;

    /// <summary>
    /// 解析下一轮探测前的等待间隔。
    /// 尚未确认离线时用短间隔快速凑满失败次数；已在线或已确认离线后回到正常心跳间隔，
    /// 避免离线期间高频重试拖慢网络恢复判断以外的其它请求。
    /// </summary>
    /// <param name="heartbeatIntervalSeconds">系统设置中的心跳间隔（秒）。</param>
    /// <param name="consecutiveFailures">当前连续失败次数。</param>
    public static int ResolveNextProbeDelaySeconds(int heartbeatIntervalSeconds, int consecutiveFailures)
    {
        var heartbeatSeconds = NormalizeHeartbeatIntervalSeconds(heartbeatIntervalSeconds);
        if (consecutiveFailures <= 0 || IsOfflineConfirmed(consecutiveFailures))
        {
            return heartbeatSeconds;
        }

        // 心跳本身可能被配置得比重探间隔更短，此时不得反而拉长等待。
        return Math.Min(heartbeatSeconds, FailureRetryIntervalSeconds);
    }

    /// <summary>
    /// 判断离线态下的新一次失败是否需要重新发布快照。
    /// 现场约束：现场会通过改错路由后缀、改错 MES 地址或禁用网口来验证业务逻辑，
    /// 这些切换都停留在离线态，只有失败原因变化。原实现在离线后直接跳过发布，
    /// 导致指示灯文本和失败原因永久冻结在第一次失败上，看起来像“无法切换”。
    /// 因此离线原因发生变化时必须重新发布，仅在原因完全相同时才抑制重复通知。
    /// </summary>
    /// <param name="isCurrentlyOffline">当前是否已确认离线。</param>
    /// <param name="currentMessage">当前快照中的失败原因。</param>
    /// <param name="incomingMessage">本次探测的失败原因。</param>
    public static bool ShouldRepublishOfflineFailure(
        bool isCurrentlyOffline,
        string? currentMessage,
        string? incomingMessage)
    {
        if (!isCurrentlyOffline)
        {
            return true;
        }

        return !string.Equals(
            currentMessage?.Trim() ?? string.Empty,
            incomingMessage?.Trim() ?? string.Empty,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// 归一化心跳间隔。
    /// CodeFirst 为旧数据行新增该列时会填 0，这里回退到默认间隔，避免升级后出现零延迟空转。
    /// </summary>
    public static int NormalizeHeartbeatIntervalSeconds(int value)
        => value <= 0
            ? DefaultHeartbeatIntervalSeconds
            : Math.Clamp(value, MinHeartbeatIntervalSeconds, MaxHeartbeatIntervalSeconds);
}
