namespace AutoWeldSystem.Core.Interfaces.Log;

/// <summary>
/// Coordinates device lifecycle log subscriptions across PLC, MES, center-server, and production services.
/// </summary>
public interface IDeviceLifecycleLogCoordinator
{
    /// <summary>
    /// Starts lifecycle logging and subscribes to runtime events.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops lifecycle logging and unsubscribes from runtime events.
    /// </summary>
    void Stop();
}
