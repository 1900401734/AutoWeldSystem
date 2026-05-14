
namespace AutoWeldSystem.Core.DTOs;

public class AddDeviceRequest
{
    /// <summary>
    /// 旧设备编号
    /// </summary>
    public string OldDeviceId { get; set; } = string.Empty;
    /// <summary>
    /// 设备编号
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// 设备名称
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;
    /// <summary>
    /// IP地址
    /// </summary>
    public string IP { get; set; } = string.Empty;
    /// <summary>
    /// 查询设备状态接口地址
    /// </summary>
    public string DevStatusUrl { get; set; } = string.Empty;
    /// <summary>
    /// 设备端设置的数据采集平台接口地址
    /// </summary>
    public string PostDataDomain { get; set; } = string.Empty;
}
