namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Describes one system-clock synchronization decision or write result.
/// </summary>
public sealed class SystemClockSyncResult
{
    private SystemClockSyncResult(
        bool success,
        bool changed,
        DateTime serverTime,
        DateTime localTimeBefore,
        double offsetSeconds,
        string message)
    {
        Success = success;
        Changed = changed;
        ServerTime = serverTime;
        LocalTimeBefore = localTimeBefore;
        OffsetSeconds = offsetSeconds;
        Message = message;
    }

    /// <summary>
    /// Gets whether the synchronization decision or write operation succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets whether the local system time should be or has been changed.
    /// </summary>
    public bool Changed { get; }

    /// <summary>
    /// Gets the authoritative server time used for comparison.
    /// </summary>
    public DateTime ServerTime { get; }

    /// <summary>
    /// Gets the local machine time before synchronization.
    /// </summary>
    public DateTime LocalTimeBefore { get; }

    /// <summary>
    /// Gets the offset in seconds, calculated as server time minus local time.
    /// </summary>
    public double OffsetSeconds { get; }

    /// <summary>
    /// Gets a short user-facing result message.
    /// </summary>
    public string Message { get; }

    public static SystemClockSyncResult Unchanged(
        DateTime serverTime,
        DateTime localTimeBefore,
        double offsetSeconds,
        string message)
        => new(true, false, serverTime, localTimeBefore, offsetSeconds, message);

    public static SystemClockSyncResult ChangedResult(
        DateTime serverTime,
        DateTime localTimeBefore,
        double offsetSeconds,
        string message)
        => new(true, true, serverTime, localTimeBefore, offsetSeconds, message);

    public static SystemClockSyncResult Failed(
        DateTime serverTime,
        DateTime localTimeBefore,
        double offsetSeconds,
        string message)
        => new(false, false, serverTime, localTimeBefore, offsetSeconds, message);
}
