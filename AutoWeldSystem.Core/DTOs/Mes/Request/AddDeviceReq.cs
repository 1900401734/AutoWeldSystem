using System.ComponentModel;

namespace AutoWeldSystem.Core.DTOs.Mes.Request;

public class AddDeviceReq
{
    [DisplayName("旧设备编号")]
    public string? OldDeviceId { get; set; } // 当新增设备编号时，OldDeviceId留空，当修改设备编号时，OldDeviceId为当前设备的设备编号，以此为更新条件，更新其他设备信息

    [DisplayName("设备编号")]
    public string DeviceId { get; set; } = string.Empty;

    [DisplayName("设备名称")]
    public string? DeviceName { get; set; }

    [DisplayName("IP地址")]
    public string? IP { get; set; }

    [DisplayName("查询设备状态接口地址")]
    public string DevStatusUrl { get; set; } = string.Empty;

    [DisplayName("设备端设置的数据采集平台接口地址")]
    public string PostDataDomain { get; set; } = string.Empty;
}
