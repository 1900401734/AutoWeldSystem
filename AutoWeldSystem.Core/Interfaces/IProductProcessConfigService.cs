using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 产品工艺配置服务。
/// 只负责维护“产品工号 + 工位”对应的产品工艺、测试方案和 PLC 数据区布局。
/// </summary>
public interface IProductProcessConfigService
{
    /// <summary>
    /// 获取产品工艺配置列表。
    /// </summary>
    IReadOnlyList<BizProductProcessConfig> GetAll(bool includeDisabled = false);

    /// <summary>
    /// 按产品工号和工位查找启用的产品工艺配置。
    /// </summary>
    BizProductProcessConfig? FindActive(
        string productNum,
        int stationNo = ProductionConstants.Stations.DefaultStationNo);

    /// <summary>
    /// 根据任务中的产品工号查找启用的产品工艺配置。
    /// 若任务带有本地程序编号，则只用该程序反查产品工号，不把程序编号作为绑定键。
    /// </summary>
    BizProductProcessConfig? FindActiveForTask(
        BizWeldTask task,
        int stationNo = ProductionConstants.Stations.DefaultStationNo);

    /// <summary>
    /// 保存单条产品工艺配置。
    /// </summary>
    BizProductProcessConfig Save(BizProductProcessConfig config);

    /// <summary>
    /// 禁用配置，历史数据仍可保留。
    /// </summary>
    void Disable(int id);

    /// <summary>
    /// 删除产品工艺配置行。
    /// </summary>
    void Delete(int id);
}
