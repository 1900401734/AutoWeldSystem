using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.ViewModels;

/// <summary>
/// One independent device lifecycle log record.
/// It records software startup, connection self-checks, test-program running, and PLC alarm changes.
/// </summary>
public sealed class DeviceLifecycleLogEntry
{
    /// <summary>
    /// Unique trace id for correlating one lifecycle event in local files.
    /// </summary>
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Time when the event occurred. UI displays this value with millisecond precision.
    /// </summary>
    public DateTime OccurredTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Log level, for example Info, Warning, or Error.
    /// </summary>
    public string Level { get; set; } = "Info";

    /// <summary>
    /// Stable event type from <see cref="AppConstants.DeviceLifecycleEventTypes"/>.
    /// </summary>
    public string EventType { get; set; } = AppConstants.DeviceLifecycleEventTypes.SelfCheck;

    /// <summary>
    /// Device id from system settings at the time the event is recorded.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Station number for station-related events. Zero means the event is device-level.
    /// </summary>
    public int StationNo { get; set; }

    /// <summary>
    /// Business status such as Success, Failed, Alarm, or Recovered.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Short text shown in the log table.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Detailed text shown in the log detail panel.
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// Source module that generated the event, such as PLC, MES, CenterServer, or Application.
    /// </summary>
    public string Source { get; set; } = string.Empty;
}
