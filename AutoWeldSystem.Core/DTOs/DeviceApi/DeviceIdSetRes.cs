namespace AutoWeldSystem.Core.DTOs.DeviceApi;

/// <summary>
/// 远程设置设备编号接口返回的数据节点。
/// 返回保存后的关键配置，便于平台确认设备端已应用。
/// </summary>
public sealed class DeviceIdSetRes
{
    /// <summary>
    /// 保存后的设备编号。
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 保存后的设备名称。
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 设备端状态查询完整地址。
    /// </summary>
    public string DevStatusUrl { get; set; } = string.Empty;

    /// <summary>
    /// 设备端保存的数据采集平台接口地址。
    /// </summary>
    public string PostDataDomain { get; set; } = string.Empty;
}
