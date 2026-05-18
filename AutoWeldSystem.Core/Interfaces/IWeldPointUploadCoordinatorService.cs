using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// Coordinates upload tasks created after weld point collection.
/// The collection service only reads PLC data and saves records; this service decides when to enqueue or execute uploads.
/// </summary>
public interface IWeldPointUploadCoordinatorService
{
    /// <summary>
    /// Handles upload scheduling after one weld point has been collected.
    /// </summary>
    Task HandleCollectedAsync(BizWeldPointRecord record, CancellationToken cancellationToken = default);
}
