using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 测试方案配置服务。
/// 统一维护测试方案、方案明细和测试项字典，避免界面直接操作数据库。
/// </summary>
public interface ITestSchemeConfigService
{
    /// <summary>
    /// 获取全部测试方案。
    /// </summary>
    IReadOnlyList<BizTestScheme> GetSchemes();

    /// <summary>
    /// 保存一条测试方案。
    /// </summary>
    BizTestScheme SaveScheme(BizTestScheme scheme);

    /// <summary>
    /// 删除测试方案，并删除该方案下的明细。
    /// </summary>
    void DeleteScheme(string schemeId);

    /// <summary>
    /// 获取方案明细；schemeId 为空时返回全部明细。
    /// </summary>
    IReadOnlyList<BizSchemeDetail> GetDetails(string? schemeId = null);

    /// <summary>
    /// 保存一条方案明细。
    /// </summary>
    BizSchemeDetail SaveDetail(BizSchemeDetail detail);

    /// <summary>
    /// 删除一条方案明细。
    /// </summary>
    void DeleteDetail(int detailId);

    /// <summary>
    /// 获取全部测试项字典。
    /// </summary>
    IReadOnlyList<DimTestItem> GetItems();

    /// <summary>
    /// 保存一条测试项字典。
    /// </summary>
    DimTestItem SaveItem(DimTestItem item);

    /// <summary>
    /// 删除测试项，并删除引用该测试项的方案明细。
    /// </summary>
    void DeleteItem(int itemId);
}
