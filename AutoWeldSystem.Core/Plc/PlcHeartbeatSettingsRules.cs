using System;

namespace AutoWeldSystem.Core.Plc;

/// <summary>
/// PLC 心跳相关设置的默认值、边界和采样判定规则。
/// </summary>
public static class PlcHeartbeatSettingsRules
{
    public const int DefaultReadIntervalMilliseconds = 300;
    public const int MinReadIntervalMilliseconds = 100;
    public const int MaxReadIntervalMilliseconds = 5000;

    public const int DefaultTimeoutSeconds = 3;
    public const int MinTimeoutSeconds = 1;
    public const int MaxTimeoutSeconds = 60;

    public const int DefaultCommunicationTimeoutMilliseconds = 3000;
    public const int MinCommunicationTimeoutMilliseconds = 100;
    public const int MaxCommunicationTimeoutMilliseconds = 30000;

    public static int NormalizeReadIntervalMilliseconds(int value)
        => Math.Clamp(
            value <= 0 ? DefaultReadIntervalMilliseconds : value,
            MinReadIntervalMilliseconds,
            MaxReadIntervalMilliseconds);

    public static int NormalizeTimeoutSeconds(int value)
        => Math.Clamp(
            value <= 0 ? DefaultTimeoutSeconds : value,
            MinTimeoutSeconds,
            MaxTimeoutSeconds);

    public static int NormalizeCommunicationTimeoutMilliseconds(int value)
        => Math.Clamp(
            value <= 0 ? DefaultCommunicationTimeoutMilliseconds : value,
            MinCommunicationTimeoutMilliseconds,
            MaxCommunicationTimeoutMilliseconds);

    public static bool IsSamplingDelayed(DateTime? lastSampleTime, DateTime currentSampleTime, int timeoutSeconds)
    {
        if (!lastSampleTime.HasValue)
        {
            return false;
        }

        return currentSampleTime - lastSampleTime.Value
            > TimeSpan.FromSeconds(NormalizeTimeoutSeconds(timeoutSeconds));
    }
}
