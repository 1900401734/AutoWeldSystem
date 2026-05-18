using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 采集参数配置服务。
/// 负责维护 PLC 采集参数地址，以及参数到 MES 字段、报表列的映射。
/// </summary>
public interface ICollectionParameterService
{
    /// <summary>
    /// 获取全部采集参数配置。
    /// </summary>
    IReadOnlyList<BizCollectionParameter> GetAll(bool includeDisabled = false);

    /// <summary>
    /// 获取指定采集组和工位的启用参数。
    /// StationNo 为 0 的参数表示所有工位共享。
    /// </summary>
    IReadOnlyList<BizCollectionParameter> GetEnabledParameters(string collectionGroup, int stationNo);

    /// <summary>
    /// 保存单条采集参数。
    /// </summary>
    BizCollectionParameter Save(BizCollectionParameter parameter);

    /// <summary>
    /// 批量保存采集参数。
    /// </summary>
    IReadOnlyList<BizCollectionParameter> SaveAll(IEnumerable<BizCollectionParameter> parameters);

    /// <summary>
    /// 禁用采集参数。
    /// </summary>
    void Disable(int id);

    /// <summary>
    /// 删除采集参数。
    /// 测试项是现场可配置数据，用户确认删除后应从本地配置中移除。
    /// </summary>
    void Delete(int id);
}
