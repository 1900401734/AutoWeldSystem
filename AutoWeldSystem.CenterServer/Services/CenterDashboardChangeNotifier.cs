namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// In-process dashboard notification bus.
/// Blazor Server pages can subscribe directly, so dashboard refresh does not depend on the page opening a SignalR client to itself.
/// </summary>
public sealed class CenterDashboardChangeNotifier
{
    /// <summary>
    /// Raised after telemetry or product-report ingestion changes dashboard data.
    /// </summary>
    public event EventHandler<CenterDashboardChangedEventArgs>? DashboardChanged;

    /// <summary>
    /// Notifies dashboard subscribers that a device snapshot changed.
    /// </summary>
    public void Notify(string deviceId)
    {
        DashboardChanged?.Invoke(this, new CenterDashboardChangedEventArgs(deviceId, DateTime.Now));
    }
}

/// <summary>
/// Dashboard refresh event payload.
/// </summary>
public sealed record CenterDashboardChangedEventArgs(string DeviceId, DateTime ChangedAt);
