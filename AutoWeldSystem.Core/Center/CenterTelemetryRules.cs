using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;

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
            PlcDeviceStatusName = ResolvePlcStatusName(snapshot.PlcDeviceStatusCode, snapshot.PlcDeviceStatusName),
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
            "1" => "运行",
            "2" => "暂停/空闲",
            "3" => "停止",
            "4" => "报警",
            _ => string.IsNullOrWhiteSpace(fallbackName) ? "未知" : fallbackName.Trim()
        };
    }

    /// <summary>
    /// Normalizes empty system types into the generic dashboard group.
    /// </summary>
    public static string NormalizeSystemType(string? value)
        => string.IsNullOrWhiteSpace(value) ? CenterServerConstants.SystemTypes.Other : value.Trim();
}
