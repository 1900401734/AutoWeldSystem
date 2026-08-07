using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Center;

/// <summary>
/// Pure rules shared by center telemetry ingestion, dashboard projection, and tests.
/// </summary>
public static class CenterTelemetryRules
{
    /// <summary>
    /// Resolves the stable device key used by the center server.
    /// </summary>
    public static string ResolveDeviceKey(CenterTelemetrySnapshotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.DeviceId.Trim();
    }

    /// <summary>
    /// Determines whether the device client is online based only on heartbeat freshness.
    /// </summary>
    public static bool IsClientOnline(DateTime? lastSeenAt, DateTime now, int offlineTimeoutSeconds)
    {
        if (lastSeenAt is null)
        {
            return false;
        }

        var timeout = Math.Max(1, offlineTimeoutSeconds);
        return now - lastSeenAt.Value <= TimeSpan.FromSeconds(timeout);
    }

    /// <summary>
    /// Builds the dashboard state without rewriting the PLC status when the client is offline.
    /// </summary>
    public static CenterDashboardDeviceStateDto BuildDashboardState(
        CenterDeviceRuntimeDto snapshot,
        DateTime now,
        int offlineTimeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new CenterDashboardDeviceStateDto
        {
            ClientOnline = IsClientOnline(snapshot.LastSeenAt, now, offlineTimeoutSeconds),
            PlcConnected = snapshot.PlcConnected,
            PlcConnectionState = snapshot.PlcConnectionState.Trim(),
            PlcDeviceStatusCode = snapshot.PlcDeviceStatusCode.Trim(),
            PlcDeviceStatusName = ResolveReportedStatusName(snapshot.PlcDeviceStatusCode, snapshot.PlcDeviceStatusName),
            AlarmMessage = snapshot.AlarmMessage.Trim(),
            LastSeenAt = snapshot.LastSeenAt,
            CollectedAt = snapshot.CollectedAt
        };
    }

    /// <summary>
    /// Normalizes an optional URL to the trailing-slash format used by HttpClient.
    /// </summary>
    public static string NormalizeBaseUrl(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? CenterServerConstants.DefaultBaseUrl
            : normalized.TrimEnd('/') + "/";
    }

    /// <summary>
    /// Keeps heartbeat intervals within a practical local-network range.
    /// </summary>
    public static int NormalizeHeartbeatIntervalSeconds(int value)
        => Math.Clamp(value, 2, 60);

    /// <summary>
    /// Keeps offline timeout above the heartbeat interval to avoid false offline states.
    /// </summary>
    public static int NormalizeOfflineTimeoutSeconds(int value)
        => Math.Clamp(value, 5, 300);

    /// <summary>
    /// Returns a stable Chinese name for known PLC status codes.
    /// </summary>
    public static string ResolvePlcStatusName(string? statusCode, string? fallbackName = null)
    {
        return statusCode?.Trim() switch
        {
            ProductionConstants.PlcDeviceStatuses.Text.Running => "运行",
            ProductionConstants.PlcDeviceStatuses.Text.Paused => "暂停/空闲",
            ProductionConstants.PlcDeviceStatuses.Text.Stopped => "停止",
            ProductionConstants.PlcDeviceStatuses.Text.Alarm => "报警",
            _ => string.IsNullOrWhiteSpace(fallbackName) ? "未知" : fallbackName.Trim()
        };
    }

    /// <summary>
    /// Preserves a source-aware status name and only derives a PLC name when the sender omitted it.
    /// </summary>
    public static string ResolveReportedStatusName(string? statusCode, string? reportedName)
        => string.IsNullOrWhiteSpace(reportedName)
            ? ResolvePlcStatusName(statusCode)
            : reportedName.Trim();

    /// <summary>
    /// 在工位与共享设备状态间选择较新的 JSONL 记录；时间相同时保留工位记录。
    /// </summary>
    public static BizDeviceStatusLog? ResolveLatestDeviceStatus(
        BizDeviceStatusLog? stationStatus,
        BizDeviceStatusLog? sharedStatus)
    {
        if (stationStatus is null)
        {
            return sharedStatus;
        }

        return sharedStatus is not null && sharedStatus.OccurredTime > stationStatus.OccurredTime
            ? sharedStatus
            : stationStatus;
    }

    /// <summary>
    /// Normalizes empty system types into the generic dashboard group.
    /// </summary>
    public static string NormalizeSystemType(string? value)
        => string.IsNullOrWhiteSpace(value) ? CenterServerConstants.SystemTypes.Other : value.Trim();

    /// <summary>
    /// 计算遥测快照的内容签名，用于判断本周期是否需要推送全量数据。
    /// 刻意排除 HeartbeatAt 与 CollectedAt：这两个时间戳每周期必变，纳入会使签名永远不同。
    /// </summary>
    public static string BuildSnapshotSignature(CenterTelemetrySnapshotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = new System.Text.StringBuilder();
        builder.Append(request.DeviceId.Trim()).Append('\n');
        builder.Append(request.DeviceName.Trim()).Append('\n');
        builder.Append(request.SystemType.Trim()).Append('\n');

        foreach (var station in request.Stations.OrderBy(it => it.StationNo))
        {
            builder.Append(station.StationNo).Append('|');
            builder.Append(station.PlcConnected).Append('|');
            builder.Append(station.PlcConnectionState).Append('|');
            builder.Append(station.DeviceStatusCode).Append('|');
            builder.Append(station.DeviceStatusName).Append('|');
            builder.Append(station.AlarmMessage).Append('|');
            builder.Append(station.CurrentWorkOrder).Append('|');
            builder.Append(station.ProductJobNo).Append('|');
            builder.Append(station.ProductModel).Append('|');
            builder.Append(station.TodayTotalCount).Append('|');
            builder.Append(station.TodayQualifiedCount).Append('|');
            builder.Append(station.TodayFailedCount).Append('\n');
        }

        return builder.ToString();
    }
}
