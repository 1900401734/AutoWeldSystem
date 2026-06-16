using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// Reads product history for MonitorView and applies product-level test weld marks.
/// Keeping this logic in a service avoids direct database rules inside the UI.
/// </summary>
public interface IProductHistoryService
{
    /// <summary>
    /// Gets all completed products collected for the specified weld task and station.
    /// </summary>
    ProductHistorySnapshot GetSnapshot(int taskId, int stationNo);

    /// <summary>
    /// Marks or unmarks one completed product as a test weld part.
    /// The operation updates all weld point rows under the same product.
    /// </summary>
    ProductHistoryMarkResult SetProductTestFlag(int taskId, int stationNo, string productNo, bool isTest);
}
