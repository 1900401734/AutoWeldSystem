using System.Globalization;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Centralizes the MES server-time parsing and local-clock synchronization threshold.
/// </summary>
public static class SystemClockSyncRules
{
    /// <summary>
    /// The minimum absolute time difference that requires changing the local clock.
    /// </summary>
    public const double SyncThresholdSeconds = 5d;

    /// <summary>
    /// Parses the server time returned by MES.
    /// </summary>
    public static SystemClockSyncResult TryParseServerTime(string? serverTimeText, out DateTime serverTime)
    {
        if (DateTime.TryParse(
            serverTimeText,
            CultureInfo.CurrentCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out serverTime))
        {
            return SystemClockSyncResult.Unchanged(serverTime, default, 0, "服务器时间解析成功。");
        }

        serverTime = default;
        return SystemClockSyncResult.Failed(default, default, 0, $"服务器时间格式无效：{serverTimeText ?? string.Empty}");
    }

    /// <summary>
    /// Decides whether the local clock needs to be adjusted to the server time.
    /// </summary>
    public static SystemClockSyncResult Decide(DateTime serverTime, DateTime localTime)
    {
        var offsetSeconds = (serverTime - localTime).TotalSeconds;
        if (Math.Abs(offsetSeconds) <= SyncThresholdSeconds)
        {
            return SystemClockSyncResult.Unchanged(
                serverTime,
                localTime,
                offsetSeconds,
                $"服务器时间与本机时间相差 {offsetSeconds:F3} 秒，未超过 5 秒，无需校时。");
        }

        return SystemClockSyncResult.ChangedResult(
            serverTime,
            localTime,
            offsetSeconds,
            $"服务器时间与本机时间相差 {offsetSeconds:F3} 秒，准备校时。");
    }
}
