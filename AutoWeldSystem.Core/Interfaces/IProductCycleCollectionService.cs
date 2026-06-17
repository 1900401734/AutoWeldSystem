using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// Product-cycle collection service.
/// One collection reads a complete product data block from PLC and saves all weld-point records under that ProductNumber.
/// </summary>
public interface IProductCycleCollectionService
{
    /// <summary>
    /// Collects one complete product from PLC according to the configured product data block layout.
    /// </summary>
    Task<IReadOnlyList<BizWeldPointRecord>> CollectAsync(
        BizWeldTask task,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);
}
