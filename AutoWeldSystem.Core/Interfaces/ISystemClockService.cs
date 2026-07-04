using AutoWeldSystem.Core.Production;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// Provides a narrow abstraction for reading and changing the local Windows system clock.
/// </summary>
public interface ISystemClockService
{
    /// <summary>
    /// Reads the current local machine time.
    /// </summary>
    DateTime GetLocalTime();

    /// <summary>
    /// Sets the local machine time to the server time.
    /// </summary>
    SystemClockSyncResult SetLocalTime(DateTime serverTime, DateTime localTimeBefore);
}
