namespace AutoWeldSystem.Core.DTOs.DeviceApi;

/// <summary>
/// 设备端状态查询接口返回的数据节点。
/// 平台只需要设备编号和 MES 设备状态码，不暴露 PLC 原始状态。
/// </summary>
public sealed class DeviceStatusQueryRes
{
    /// <summary>
    /// 当前设备编号，来自本地系统设置。
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 当前 MES 设备状态码：0/1/4/5/6/7。
    /// </summary>
    public string DeviceStatus { get; set; } = string.Empty;
}
