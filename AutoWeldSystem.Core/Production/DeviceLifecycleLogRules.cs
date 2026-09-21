using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Enums;
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
    private const string SourceDeviceApi = "DeviceApi";

    /// <summary>
    /// Returns true only when a connection self-check result should be logged.
    /// The first result is always logged; repeated same-state results are ignored.
    /// </summary>
    public static bool HasConnectionStatusChanged(bool? previousConnected, bool currentConnected)
        => !previousConnected.HasValue || previousConnected.Value != currentConnected;

    /// <summary>
    /// Returns true only for PLC states that represent a completed connection result.
    /// Starting, reconnecting, and stopping are operational transitions rather than failures.
    /// </summary>
    public static bool ShouldRecordPlcConnectionState(PlcConnectionState state)
        => state is PlcConnectionState.Connected
            or PlcConnectionState.Disconnected
            or PlcConnectionState.Faulted
            or PlcConnectionState.Unverified;

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
            Summary = BuildConnectionSummary(normalizedSource, connected),
            Detail = NormalizeText(message, connected ? "连接成功" : "连接失败")
        };
    }

    /// <summary>
    /// Creates a startup self-check entry for MES server-time synchronization.
    /// The event type stays SelfCheck so the log page treats time sync as part of boot self-check.
    /// </summary>
    public static DeviceLifecycleLogEntry CreateServerTimeSelfCheckEntry(
        string deviceId,
        SystemClockSyncResult result,
        DateTime occurredTime)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new DeviceLifecycleLogEntry
        {
            OccurredTime = occurredTime,
            Level = result.Success ? "Info" : "Warning",
            EventType = AppConstants.DeviceLifecycleEventTypes.SelfCheck,
            DeviceId = NormalizeText(deviceId, string.Empty),
            StationNo = 0,
            Source = "MES",
            Status = result.Success ? StatusSuccess : StatusFailed,
            Summary = result.Success ? "MES服务器校时成功" : "MES服务器校时失败",
            Detail = $"ServerTime={FormatDateTime(result.ServerTime)}, LocalBefore={FormatDateTime(result.LocalTimeBefore)}, OffsetSeconds={result.OffsetSeconds:F3}, Changed={result.Changed}, Success={result.Success}, Message={NormalizeText(result.Message, string.Empty)}"
        };
    }

    /// <summary>
    /// Creates a startup self-check entry for the local HTTP device API listener.
    /// The HTTP listener is part of software boot readiness, so it stays under SelfCheck.
    /// </summary>
    public static DeviceLifecycleLogEntry CreateDeviceApiHttpSelfCheckEntry(
        string deviceId,
        string deviceBaseUrl,
        bool success,
        string message,
        DateTime occurredTime)
    {
        return new DeviceLifecycleLogEntry
        {
            OccurredTime = occurredTime,
            Level = success ? "Info" : "Warning",
            EventType = AppConstants.DeviceLifecycleEventTypes.SelfCheck,
            DeviceId = NormalizeText(deviceId, string.Empty),
            StationNo = 0,
            Source = SourceDeviceApi,
            Status = success ? StatusSuccess : StatusFailed,
            Summary = success ? "HTTP服务启动成功" : "HTTP服务启动失败",
            Detail = $"DeviceBaseUrl={NormalizeText(deviceBaseUrl, "--")}, Success={success}, Message={NormalizeText(message, string.Empty)}"
        };
    }

    /// <summary>
    /// Creates an audit entry for remote device-status API queries.
    /// Every query is logged so platform polling remains traceable.
    /// </summary>
    public static DeviceLifecycleLogEntry CreateDeviceApiRemoteAccessEntry(
        string deviceId,
        string requestType,
        string requestedDeviceId,
        string currentDeviceId,
        string responseStatus,
        string resultMessage,
        bool success,
        DateTime occurredTime)
    {
        return new DeviceLifecycleLogEntry
        {
            OccurredTime = occurredTime,
            Level = success ? "Info" : "Warning",
            EventType = AppConstants.DeviceLifecycleEventTypes.RemoteAccess,
            DeviceId = NormalizeText(deviceId, string.Empty),
            StationNo = 0,
            Source = SourceDeviceApi,
            Status = success ? StatusSuccess : StatusFailed,
            Summary = success ? "远程设备状态查询成功" : "远程设备状态查询失败",
            Detail = $"RequestType={NormalizeText(requestType, "--")}, RequestedDeviceId={NormalizeText(requestedDeviceId, "--")}, CurrentDeviceId={NormalizeText(currentDeviceId, "--")}, ResponseStatus={NormalizeText(responseStatus, "--")}, Message={NormalizeText(resultMessage, string.Empty)}"
        };
    }

    /// <summary>
    /// Creates an audit entry for remote device-id configuration changes.
    /// The request may fail validation, so both requested values and response status are preserved.
    /// </summary>
    public static DeviceLifecycleLogEntry CreateDeviceApiRemoteConfigChangedEntry(
        string deviceId,
        string oldDeviceId,
        string newDeviceId,
        string deviceName,
        string devStatusUrl,
        string postDataDomain,
        string responseStatus,
        string resultMessage,
        bool success,
        DateTime occurredTime)
    {
        return new DeviceLifecycleLogEntry
        {
            OccurredTime = occurredTime,
            Level = success ? "Info" : "Warning",
            EventType = AppConstants.DeviceLifecycleEventTypes.RemoteConfigChanged,
            DeviceId = NormalizeText(deviceId, string.Empty),
            StationNo = 0,
            Source = SourceDeviceApi,
            Status = success ? StatusSuccess : StatusFailed,
            Summary = success ? "远程设备编号设置成功" : "远程设备编号设置失败",
            Detail = $"RequestType=POST /api/DeviceID, OldDeviceId={NormalizeText(oldDeviceId, "--")}, NewDeviceId={NormalizeText(newDeviceId, "--")}, DeviceName={NormalizeText(deviceName, "--")}, DevStatusUrl={NormalizeText(devStatusUrl, "--")}, PostDataDomain={NormalizeText(postDataDomain, "--")}, ResponseStatus={NormalizeText(responseStatus, "--")}, Message={NormalizeText(resultMessage, string.Empty)}"
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
    /// 把设备状态 JSONL 记录投影为设备日志行，使设备日志页签能显示 0/1/4/5/6/7 全部状态。
    /// 只做显示层投影，不改写设备状态原始记录和 MES 状态码。
    /// </summary>
    public static DeviceLifecycleLogEntry CreateDeviceStatusEntry(BizDeviceStatusLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        var statusCode = NormalizeText(log.DeviceStatus, string.Empty);
        var isAlarm = statusCode == ProductionConstants.MesDeviceStatuses.Exception;
        var isRecovered = statusCode == ProductionConstants.MesDeviceStatuses.Recovered;
        var reason = isAlarm || isRecovered
            ? NormalizeText(log.AlarmContent, NormalizeText(log.Remark, string.Empty))
            : string.Empty;
        var summary = ResolveDeviceStatusSummary(statusCode, log.StatusName);
        return new DeviceLifecycleLogEntry
        {
            TraceId = NormalizeText(log.RecordId, log.Id.ToString()),
            OccurredTime = log.OccurredTime,
            Level = isAlarm ? "Warning" : "Info",
            EventType = isAlarm
                ? AppConstants.DeviceLifecycleEventTypes.FaultAlarm
                : isRecovered
                    ? AppConstants.DeviceLifecycleEventTypes.FaultRecovered
                    : AppConstants.DeviceLifecycleEventTypes.DeviceStatusReport,
            DeviceId = NormalizeText(log.DeviceId, string.Empty),
            StationNo = Math.Max(0, log.StationNo),
            Source = NormalizeText(log.Source, "Software"),
            Status = isAlarm ? StatusAlarm : isRecovered ? StatusRecovered : StatusSuccess,
            Summary = string.IsNullOrEmpty(reason) ? summary : $"{summary}：{reason}",
            Detail = BuildDeviceStatusDetail(log, statusCode, reason)
        };
    }

    private static string ResolveDeviceStatusSummary(string statusCode, string? statusName)
    {
        return statusCode switch
        {
            ProductionConstants.MesDeviceStatuses.Stopped => "停机",
            ProductionConstants.MesDeviceStatuses.PoweredOn => "开机",
            ProductionConstants.MesDeviceStatuses.Exception => "故障报警",
            ProductionConstants.MesDeviceStatuses.Recovered => "故障恢复",
            ProductionConstants.MesDeviceStatuses.ProgramStarted => "程序执行开始",
            ProductionConstants.MesDeviceStatuses.ProgramEnded => "程序执行结束",
            _ => NormalizeText(statusName, $"设备状态 {NormalizeText(statusCode, "--")}")
        };
    }

    private static string BuildDeviceStatusDetail(BizDeviceStatusLog log, string statusCode, string reason)
    {
        var parts = new List<string>
        {
            $"DeviceStatus={NormalizeText(statusCode, "--")}",
            $"StatusName={NormalizeText(log.StatusName, "--")}",
            $"WorkOrder={NormalizeText(log.WorkOrderId, "--")}",
            $"ReportStatus={NormalizeText(log.ReportStatus, "--")}"
        };

        if (!string.IsNullOrEmpty(reason))
        {
            parts.Add($"AlarmContent={reason}");
        }

        if (!string.IsNullOrWhiteSpace(log.AlarmAddress))
        {
            parts.Add($"AlarmAddress={log.AlarmAddress!.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(log.ReportMessage))
        {
            parts.Add($"ReportMessage={log.ReportMessage!.Trim()}");
        }

        return string.Join(", ", parts);
    }

    private static string NormalizeText(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string BuildConnectionSummary(string source, bool connected)
    {
        var displaySource = ResolveConnectionSourceName(source);
        return $"{displaySource}连接{(connected ? "成功" : "失败")}";
    }

    private static string ResolveConnectionSourceName(string source)
        => string.Equals(source, "CenterServer", StringComparison.OrdinalIgnoreCase)
            ? "看板"
            : source;

    private static string FormatDateTime(DateTime value)
        => value == default ? "--" : value.ToString("yyyy-MM-dd HH:mm:ss");
}

/// <summary>
/// Result of checking whether a PLC alarm transition should produce a lifecycle log.
/// </summary>
public sealed record DeviceAlarmLogDecision(bool ShouldWrite, string EventType)
{
    public static DeviceAlarmLogDecision Write(string eventType) => new(true, eventType);

    public static DeviceAlarmLogDecision Skip() => new(false, string.Empty);
}
