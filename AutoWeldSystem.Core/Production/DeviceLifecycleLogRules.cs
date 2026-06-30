using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Shared rules for deciding when device lifecycle logs should be written.
/// Keeping these checks here prevents PLC, MES, center-sync, and UI code from duplicating status logic.
/// </summary>
public static class DeviceLifecycleLogRules
{
    private const string StatusSuccess = "Success";
    private const string StatusFailed = "Failed";
    private const string StatusAlarm = "Alarm";
    private const string StatusRecovered = "Recovered";

    /// <summary>
    /// Returns true only when a connection self-check result should be logged.
    /// The first result is always logged; repeated same-state results are ignored.
    /// </summary>
    public static bool HasConnectionStatusChanged(bool? previousConnected, bool currentConnected)
        => !previousConnected.HasValue || previousConnected.Value != currentConnected;

    /// <summary>
    /// Creates a device lifecycle self-check entry for PLC, MES, or center-server connectivity.
    /// </summary>
    public static DeviceLifecycleLogEntry CreateSelfCheckEntry(
        string deviceId,
        int stationNo,
        string source,
        bool connected,
        string message,
        DateTime occurredTime)
    {
        var normalizedSource = NormalizeText(source, "Unknown");
        return new DeviceLifecycleLogEntry
        {
            OccurredTime = occurredTime,
            Level = connected ? "Info" : "Warning",
            EventType = AppConstants.DeviceLifecycleEventTypes.SelfCheck,
            DeviceId = NormalizeText(deviceId, string.Empty),
            StationNo = Math.Max(0, stationNo),
            Source = normalizedSource,
            Status = connected ? StatusSuccess : StatusFailed,
            Summary = $"{normalizedSource}自检{(connected ? "成功" : "失败")}",
            Detail = NormalizeText(message, connected ? "连接成功" : "连接失败")
        };
    }

    /// <summary>
    /// Decides whether a PLC device alarm change should be logged.
    /// A log is written when alarm starts, alarm reason changes, or alarm recovers.
    /// </summary>
    public static DeviceAlarmLogDecision DecideAlarmTransition(
        short? previousStatusCode,
        string? previousAlarmMessage,
        short? currentStatusCode,
        string? currentAlarmMessage)
    {
        var wasAlarm = previousStatusCode == ProductionConstants.PlcDeviceStatuses.Alarm;
        var isAlarm = currentStatusCode == ProductionConstants.PlcDeviceStatuses.Alarm;
        if (!wasAlarm && isAlarm)
        {
            return DeviceAlarmLogDecision.Write(AppConstants.DeviceLifecycleEventTypes.FaultAlarm);
        }

        var previousMessage = NormalizeText(previousAlarmMessage, string.Empty);
        var currentMessage = NormalizeText(currentAlarmMessage, string.Empty);
        if (wasAlarm && isAlarm && !string.Equals(previousMessage, currentMessage, StringComparison.Ordinal))
        {
            return DeviceAlarmLogDecision.Write(AppConstants.DeviceLifecycleEventTypes.FaultAlarm);
        }

        if (wasAlarm && currentStatusCode.HasValue && !isAlarm)
        {
            return DeviceAlarmLogDecision.Write(AppConstants.DeviceLifecycleEventTypes.FaultRecovered);
        }

        return DeviceAlarmLogDecision.Skip();
    }

    /// <summary>
    /// Creates the lifecycle entry for a PLC alarm or alarm recovery decision.
    /// </summary>
    public static DeviceLifecycleLogEntry CreateAlarmEntry(
        string deviceId,
        int stationNo,
        string eventType,
        string alarmMessage,
        DateTime occurredTime)
    {
        var recovered = string.Equals(eventType, AppConstants.DeviceLifecycleEventTypes.FaultRecovered, StringComparison.Ordinal);
        var detail = NormalizeText(alarmMessage, recovered ? "PLC设备报警已恢复" : "PLC设备报警，未匹配到已启用的报警原因");
        return new DeviceLifecycleLogEntry
        {
            OccurredTime = occurredTime,
            Level = recovered ? "Info" : "Warning",
            EventType = eventType,
            DeviceId = NormalizeText(deviceId, string.Empty),
            StationNo = Math.Max(0, stationNo),
            Source = "PLC",
            Status = recovered ? StatusRecovered : StatusAlarm,
            Summary = recovered ? "PLC故障报警恢复" : "PLC故障报警",
            Detail = detail
        };
    }

    /// <summary>
    /// Creates the lifecycle entry for successful MES start report.
    /// </summary>
    public static DeviceLifecycleLogEntry CreateTestProgramRunningEntry(
        string deviceId,
        int stationNo,
        string taskId,
        string workOrder,
        DateTime occurredTime)
    {
        var normalizedTaskId = NormalizeText(taskId, "--");
        var normalizedWorkOrder = NormalizeText(workOrder, "--");
        return new DeviceLifecycleLogEntry
        {
            OccurredTime = occurredTime,
            Level = "Info",
            EventType = AppConstants.DeviceLifecycleEventTypes.TestProgramRunning,
            DeviceId = NormalizeText(deviceId, string.Empty),
            StationNo = Math.Max(0, stationNo),
            Source = "MES",
            Status = StatusSuccess,
            Summary = "测试程序运行",
            Detail = $"开工上报成功，任务ID={normalizedTaskId}，工单={normalizedWorkOrder}"
        };
    }

    /// <summary>
    /// Creates the lifecycle entry written once when the software starts.
    /// </summary>
    public static DeviceLifecycleLogEntry CreateSoftwareStartedEntry(string deviceId, DateTime occurredTime)
    {
        return new DeviceLifecycleLogEntry
        {
            OccurredTime = occurredTime,
            Level = "Info",
            EventType = AppConstants.DeviceLifecycleEventTypes.SoftwareStarted,
            DeviceId = NormalizeText(deviceId, string.Empty),
            Source = "Application",
            Status = StatusSuccess,
            Summary = "软件开启",
            Detail = "AutoWeldSystem 软件已启动。"
        };
    }

    private static string NormalizeText(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}

/// <summary>
/// Result of checking whether a PLC alarm transition should produce a lifecycle log.
/// </summary>
public sealed record DeviceAlarmLogDecision(bool ShouldWrite, string EventType)
{
    public static DeviceAlarmLogDecision Write(string eventType) => new(true, eventType);

    public static DeviceAlarmLogDecision Skip() => new(false, string.Empty);
}
