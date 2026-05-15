using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 焊点数据采集服务。
/// 一次采集对应一个焊接开始到焊接结束周期，并生成一条焊点记录。
/// </summary>
public interface IWeldPointCollectionService
{
    /// <summary>
    /// 按任务和工位采集一条焊点数据。
    /// </summary>
    Task<BizWeldPointRecord> CollectAsync(
        BizWeldTask task,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default);
}
