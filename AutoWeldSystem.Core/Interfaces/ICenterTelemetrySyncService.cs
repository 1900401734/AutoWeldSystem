using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// Device-side background service that pushes runtime snapshots to the center server.
/// </summary>
public interface ICenterTelemetrySyncService : IAsyncDisposable
{
    /// <summary>
    /// Raised when a center-server push succeeds or fails.
    /// </summary>
    event EventHandler<CenterTelemetryConnectionSnapshot>? StatusChanged;

    /// <summary>
    /// Latest center-server push status.
    /// </summary>
    CenterTelemetryConnectionSnapshot Current { get; }

    /// <summary>
    /// Starts the periodic telemetry loop.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the periodic telemetry loop.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes one snapshot immediately.
    /// </summary>
    Task PushOnceAsync(CancellationToken cancellationToken = default);
}
