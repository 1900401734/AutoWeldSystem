using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// Latest runtime snapshot for one station of one center-server device.
/// </summary>
[SugarTable("Center_DeviceStationRuntimeSnapshot", TableDescription = "中心服务器设备工位最新运行快照表")]
public sealed class CenterDeviceStationRuntimeSnapshot
{
    /// <summary>
    /// Surrogate primary key used by SqlSugar.
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// Stable device id.
    /// </summary>
    [SugarColumn(Length = 50)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Station number on the equipment client.
    /// </summary>
    public int StationNo { get; set; } = 1;

    /// <summary>
    /// Whether the station's PLC business connection is verified.
    /// </summary>
    public bool PlcConnected { get; set; }

    /// <summary>
    /// PLC connection state text.
    /// </summary>
    [SugarColumn(Length = 50)]
    public string PlcConnectionState { get; set; } = string.Empty;

    /// <summary>
    /// PLC raw device status code.
    /// </summary>
    [SugarColumn(Length = 20)]
    public string DeviceStatusCode { get; set; } = string.Empty;

    /// <summary>
    /// PLC device status display name.
    /// </summary>
    [SugarColumn(Length = 50)]
    public string DeviceStatusName { get; set; } = string.Empty;

    /// <summary>
    /// Alarm message resolved by the equipment client.
    /// </summary>
    [SugarColumn(Length = 500)]
    public string AlarmMessage { get; set; } = string.Empty;

    /// <summary>
    /// Current work order of this station.
    /// </summary>
    [SugarColumn(Length = 50)]
    public string CurrentWorkOrder { get; set; } = string.Empty;

    /// <summary>
    /// Product job number configured in the local task.
    /// </summary>
    [SugarColumn(Length = 50)]
    public string ProductJobNo { get; set; } = string.Empty;

    /// <summary>
    /// Optional product model.
    /// </summary>
    [SugarColumn(Length = 50)]
    public string ProductModel { get; set; } = string.Empty;

    public int TodayTotalCount { get; set; }
    public int TodayQualifiedCount { get; set; }
    public int TodayFailedCount { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
