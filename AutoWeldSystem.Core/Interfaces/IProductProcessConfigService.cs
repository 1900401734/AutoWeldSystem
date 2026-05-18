using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 产品工艺配置服务。
/// 负责维护“产品工号 + 产品型号 + 工位”对应的焊点数量和采集参数组。
/// </summary>
public interface IProductProcessConfigService
{
    /// <summary>
    /// 获取产品工艺配置列表。
    /// </summary>
    IReadOnlyList<BizProductProcessConfig> GetAll(bool includeDisabled = false);

    /// <summary>
    /// 根据产品工号和产品型号查找启用的配置。
    /// </summary>
    BizProductProcessConfig? FindActive(
        string productNum,
        string productModel,
        int stationNo = ProductionConstants.Stations.DefaultStationNo);

    /// <summary>
    /// 保存单条配置。
    /// </summary>
    BizProductProcessConfig Save(BizProductProcessConfig config);

    /// <summary>
    /// 禁用配置。历史数据仍可保留，不做物理删除。
    /// </summary>
    void Disable(int id);
}
