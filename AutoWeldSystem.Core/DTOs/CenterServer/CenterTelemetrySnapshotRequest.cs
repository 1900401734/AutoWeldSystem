namespace AutoWeldSystem.Core.DTOs.CenterServer;

/// <summary>
/// Latest state snapshot uploaded by one equipment client to the center server.
/// </summary>
public sealed class CenterTelemetrySnapshotRequest
{
    /// <summary>
    /// Stable device id configured on the equipment client.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Human readable device name configured on the equipment client.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Logical system type used by the center dashboard for grouping.
    /// </summary>
    public string SystemType { get; set; } = string.Empty;

    /// <summary>
    /// Device-side heartbeat time. It is used only for online/offline judgement.
    /// </summary>
    public DateTime HeartbeatAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Latest runtime snapshots for every station reported by this device.
    /// </summary>
    public List<CenterTelemetryStationSnapshot> Stations { get; set; } = new();

    /// <summary>中心报表独立同步状态；旧设备不支持时为空，不混入 MES 上传状态。</summary>
    public CenterReportSyncSummaryDto? ReportSync { get; set; }
}

public sealed class CenterReportSyncSummaryDto
{
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
}

/// <summary>
/// Latest station-level runtime snapshot uploaded by one equipment client.
/// </summary>
public sealed class CenterTelemetryStationSnapshot
{
    /// <summary>
    /// Local station number. Single-station devices use 1.
    /// </summary>
    public int StationNo { get; set; } = 1;

    /// <summary>
    /// Whether the station's PLC business connection is verified.
    /// </summary>
    public bool PlcConnected { get; set; }

    /// <summary>
    /// Current PLC connection state text from the equipment client.
    /// </summary>
    public string PlcConnectionState { get; set; } = string.Empty;

    /// <summary>
    /// PLC raw status code when available; otherwise the latest device-status JSONL MES code.
    /// </summary>
    public string DeviceStatusCode { get; set; } = string.Empty;

    /// <summary>
    /// Display name matching the selected PLC or JSONL source.
    /// </summary>
    public string DeviceStatusName { get; set; } = string.Empty;

    /// <summary>
    /// Alarm content resolved by the equipment client.
    /// </summary>
    public string AlarmMessage { get; set; } = string.Empty;

    /// <summary>设备端按报警触发模式判定的有效状态；null 表示旧设备，空对象表示明确无报警。</summary>
    public CenterEffectiveAlarmDto? EffectiveAlarm { get; set; }

    /// <summary>
    /// Current work order of this station. Dual work-order mode can report different values per station.
    /// </summary>
    public string CurrentWorkOrder { get; set; } = string.Empty;

    /// <summary>
    /// Product job number configured in the local task.
    /// </summary>
    public string ProductJobNo { get; set; } = string.Empty;

    /// <summary>
    /// Optional product model displayed on the dashboard.
    /// </summary>
    public string ProductModel { get; set; } = string.Empty;

    /// <summary>
    /// Today's total production count for this station.
    /// </summary>
    public int TodayTotalCount { get; set; }

    /// <summary>
    /// Work order planned quantity (MES 工单的生产数量) for this station.
    /// 双工位同工单时两个工位上报同一个值，看板按工单号去重后才能算对达成率分母。
    /// </summary>
    public int WorkOrderQuantity { get; set; }

    /// <summary>
    /// Today's qualified production count for this station.
    /// </summary>
    public int TodayQualifiedCount { get; set; }

    /// <summary>
    /// Today's failed production count for this station.
    /// </summary>
    public int TodayFailedCount { get; set; }

    /// <summary>
    /// Device-side collection time for this station snapshot.
    /// </summary>
    public DateTime CollectedAt { get; set; } = DateTime.Now;

    /// <summary>新增字段为空代表旧协议，不能把 PLC 累计数冒充当日采集产量。</summary>
    public DateTime? ProductionDate { get; set; }
    public string? TaskKey { get; set; }
    public string? ProgramName { get; set; }
    public string? StationName { get; set; }
    public string? StatusSource { get; set; }
    public int? TaskTotalCount { get; set; }
    public int? TaskQualifiedCount { get; set; }
    public int? TaskFailedCount { get; set; }
}
