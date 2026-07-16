using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// Queues completed product data for asynchronous forwarding to the center server.
/// </summary>
public interface ICenterProductForwardingService : IAsyncDisposable
{
    /// <summary>
    /// Starts the background retry loop for center product forwarding tasks.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the background retry loop.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues one completed product without performing network I/O on the caller thread.
    /// </summary>
    void EnqueueCompletedProduct(
        BizWeldTask task,
        int stationNo,
        IReadOnlyList<BizWeldPointRecord> records);

    /// <summary>
    /// Queues one task-level finish update after EndTime and final quantities are persisted locally.
    /// </summary>
    void EnqueueTaskFinishUpdate(BizWeldTask task);
}
