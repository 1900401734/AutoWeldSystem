using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// Latest center-server heartbeat snapshot for one device.
/// Station production state is stored in <see cref="CenterDeviceStationRuntimeSnapshot"/>.
/// </summary>
[SugarTable("Center_DeviceRuntimeSnapshot", TableDescription = "中心服务器设备最新心跳表")]
public sealed class CenterDeviceRuntimeSnapshot
{
    /// <summary>
    /// Stable device id.
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, Length = 50)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Display name uploaded by the equipment client.
    /// </summary>
    [SugarColumn(Length = 100)]
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Logical system type used for grouping.
    /// </summary>
    [SugarColumn(Length = 80)]
    public string SystemType { get; set; } = string.Empty;

    /// <summary>
    /// Device-side heartbeat timestamp.
    /// </summary>
    public DateTime HeartbeatAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Server-side receive timestamp.
    /// </summary>
    public DateTime LastSeenAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Last update time of this row.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
