namespace AutoWeldSystem.CenterServer.Configuration;

/// <summary>
/// 中心服务器本机配置。
/// 这些配置只影响中心服务器自身，不进入业务数据库。
/// </summary>
public sealed class CenterServerLocalSettings
{
    /// <summary>
    /// 是否随当前 Windows 用户登录后自动启动中心服务器。
    /// </summary>
    public bool EnableAutoStart { get; set; } = true;

    /// <summary>
    /// 设备推送 JSONL 日志根目录。
    /// </summary>
    public string LogDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 中心服务器产品 Excel 报表根目录。
    /// </summary>
    public string DataDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 设备客户端心跳超时时间，单位秒。
    /// </summary>
    public int OfflineTimeoutSeconds { get; set; } = 15;
}
