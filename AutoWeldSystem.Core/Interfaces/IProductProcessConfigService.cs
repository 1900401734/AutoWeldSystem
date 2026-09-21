using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

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
    /// 优先匹配产品专用工位、同产品共享工位，未匹配时使用当前工位的显式默认工艺。
    /// </summary>
    BizProductProcessConfig? FindActive(
        string productNum,
        int stationNo = ProductionConstants.Stations.DefaultStationNo);

    /// <summary>
    /// 先根据任务绑定程序反查产品工号，再按工位查找专用或默认工艺；程序编号不作为绑定键。
    /// </summary>
    BizProductProcessConfig? FindActiveForTask(
        BizWeldTask task,
        int stationNo = ProductionConstants.Stations.DefaultStationNo);

    /// <summary>
    /// 保存单条产品工艺配置。
    /// </summary>
    BizProductProcessConfig Save(BizProductProcessConfig config);

    /// <summary>
    /// 原子保存本次编辑的配置集合，不删除未传入的行；全部成功后才回写新增主键。
    /// </summary>
    void SaveAll(IEnumerable<BizProductProcessConfig> configs);

    /// <summary>
    /// 禁用配置，历史数据仍可保留。
    /// </summary>
    void Disable(int id);

    /// <summary>
    /// 删除产品工艺配置行。
    /// </summary>
    void Delete(int id);
}
