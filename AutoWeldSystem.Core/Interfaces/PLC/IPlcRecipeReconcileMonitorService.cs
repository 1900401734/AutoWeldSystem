using AutoWeldSystem.Core.DTOs.Plc;

namespace AutoWeldSystem.Core.Interfaces.PLC;

/// <summary>
/// Monitors PLC recipe codes and reconciles PC recipe values when a running task requires it.
/// </summary>
public interface IPlcRecipeReconcileMonitorService : IAsyncDisposable
{
    /// <summary>
    /// Raised when a station PLC recipe snapshot changes.
    /// </summary>
    event EventHandler<PlcRecipeCodeSnapshot>? RecipeCodeChanged;

    /// <summary>
    /// Starts the background recipe monitoring loop.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the background recipe monitoring loop.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest PLC-side recipe snapshot for a station.
    /// </summary>
    /// <param name="stationNo">Station number.</param>
    /// <returns>Latest known snapshot, or a failed snapshot when no successful read has happened yet.</returns>
    PlcRecipeCodeSnapshot GetCurrent(int stationNo);
}
