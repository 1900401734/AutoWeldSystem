using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// Device automatically registered by center telemetry.
/// </summary>
[SugarTable("Center_DeviceNode", TableDescription = "中心服务器设备节点表")]
public sealed class CenterDeviceNode
{
    [SugarColumn(IsPrimaryKey = true, Length = 50)]
    public string DeviceId { get; set; } = string.Empty;

    [SugarColumn(Length = 100)]
    public string DeviceName { get; set; } = string.Empty;

    [SugarColumn(Length = 80)]
    public string SystemType { get; set; } = string.Empty;

    public DateTime FirstSeenAt { get; set; } = DateTime.Now;

    public DateTime LastSeenAt { get; set; } = DateTime.Now;
}
