using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// Optional lightweight history of center telemetry snapshots for troubleshooting.
/// </summary>
[SugarTable("Center_TelemetryLog", TableDescription = "中心服务器遥测历史表")]
public sealed class CenterTelemetryLog
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 50)]
    public string DeviceId { get; set; } = string.Empty;

    public int StationNo { get; set; } = 1;

    [SugarColumn(Length = 20)]
    public string DeviceStatusCode { get; set; } = string.Empty;

    [SugarColumn(Length = 500)]
    public string AlarmMessage { get; set; } = string.Empty;

    public int TodayTotalCount { get; set; }
    public int TodayQualifiedCount { get; set; }
    public int TodayFailedCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
