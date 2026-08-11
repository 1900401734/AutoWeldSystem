namespace AutoWeldSystem.Core.DTOs.CenterServer;

/// <summary>
/// Current data used by the center dashboard.
/// </summary>
public sealed class CenterDashboardSnapshotDto
{
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public List<CenterDashboardDeviceDto> Devices { get; set; } = new();
}

/// <summary>
/// Dashboard row/card for one dynamically registered device.
/// </summary>
public sealed class CenterDashboardDeviceDto
{
    /// <summary>
    /// Stable device id.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the device.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Logical system type used for grouping.
    /// </summary>
    public string SystemType { get; set; } = string.Empty;

    /// <summary>
    /// Device online state calculated from heartbeat freshness.
    /// </summary>
    public CenterDashboardDeviceStateDto State { get; set; } = new();

    /// <summary>
    /// Station-level runtime data displayed inside this device card.
    /// </summary>
    public List<CenterDashboardStationDto> Stations { get; set; } = new();

    /// <summary>
    /// Sum of today's total production count across all stations.
    /// </summary>
    public int TodayTotalCount => Stations.Sum(station => Math.Max(0, station.TodayTotalCount));

    /// <summary>
    /// Sum of today's qualified production count across all stations.
    /// </summary>
    public int TodayQualifiedCount => Stations.Sum(station => Math.Max(0, station.TodayQualifiedCount));

    /// <summary>
    /// Sum of today's failed production count across all stations.
    /// </summary>
    public int TodayFailedCount => Stations.Sum(station => Math.Max(0, station.TodayFailedCount));

    /// <summary>
    /// 设备级工单数量。双工位同工单时只有一份计划量，按工单号去重后求和；
    /// 工单号为空的工位不计入，避免未开工工位把分母拉大。
    /// </summary>
    public int WorkOrderQuantity => Stations
        .Where(station => !string.IsNullOrWhiteSpace(station.CurrentWorkOrder)
            && station.WorkOrderQuantity > 0)
        .GroupBy(station => station.CurrentWorkOrder.Trim(), StringComparer.OrdinalIgnoreCase)
        .Sum(group => group.Max(station => station.WorkOrderQuantity));
}

/// <summary>
/// Dashboard data displayed for one station inside a device card.
/// </summary>
public sealed class CenterDashboardStationDto
{
    /// <summary>
    /// Station number on the equipment client.
    /// </summary>
    public int StationNo { get; set; } = 1;

    /// <summary>
    /// Runtime state of this station.
    /// </summary>
    public CenterDashboardDeviceStateDto State { get; set; } = new();

    /// <summary>
    /// Current work order of this station.
    /// </summary>
    public string CurrentWorkOrder { get; set; } = string.Empty;

    /// <summary>
    /// Product job number configured in the local task.
    /// </summary>
    public string ProductJobNo { get; set; } = string.Empty;

    /// <summary>
    /// Optional product model.
    /// </summary>
    public string ProductModel { get; set; } = string.Empty;

    /// <summary>
    /// Work order planned quantity, used as the achievement-rate denominator.
    /// </summary>
    public int WorkOrderQuantity { get; set; }

    /// <summary>
    /// Today's total production count.
    /// </summary>
    public int TodayTotalCount { get; set; }

    /// <summary>
    /// Today's qualified production count.
    /// </summary>
    public int TodayQualifiedCount { get; set; }

    /// <summary>
    /// Today's failed production count.
    /// </summary>
    public int TodayFailedCount { get; set; }
}
