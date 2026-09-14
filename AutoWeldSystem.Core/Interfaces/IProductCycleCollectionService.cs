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
    /// 只读解析程序统计模式下该任务、该工位下一次采集的编号与覆盖标志，不占号或写入记录。
    /// PLC 原始编号仅用于识别整件检测的被动重测。
    /// </summary>
    (string ProductNo, bool IsOverwrite) ResolvePendingProductNo(
        int taskId,
        int stationNo,
        string? plcProductNo,
        string? processParameterDeviceType);

    /// <summary>
    /// Collects one complete product from PLC according to the configured product data block layout.
    /// </summary>
    Task<IReadOnlyList<BizWeldPointRecord>> CollectAsync(
        BizWeldTask task,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);
}
