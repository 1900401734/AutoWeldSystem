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
    /// 归一化心跳间隔。
    /// CodeFirst 为旧数据行新增该列时会填 0，这里回退到默认间隔，避免升级后出现零延迟空转。
    /// </summary>
    public static int NormalizeHeartbeatIntervalSeconds(int value)
        => value <= 0
            ? DefaultHeartbeatIntervalSeconds
            : Math.Clamp(value, MinHeartbeatIntervalSeconds, MaxHeartbeatIntervalSeconds);
}
