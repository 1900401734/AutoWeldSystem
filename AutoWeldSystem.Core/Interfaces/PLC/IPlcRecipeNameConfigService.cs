using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces.PLC;

/// <summary>
/// PLC 配方名称地址配置持久化服务。
/// </summary>
public interface IPlcRecipeNameConfigService
{
    /// <summary>
    /// 获取全部工位配置。
    /// </summary>
    IReadOnlyList<BizPlcRecipeNameConfig> GetAll();

    /// <summary>
    /// 获取指定实际工位的配置。
    /// </summary>
    BizPlcRecipeNameConfig? GetForStation(int stationNo);

    /// <summary>
    /// 保存全部工位配置；未包含的旧配置会被删除。
    /// </summary>
    void SaveAll(IEnumerable<BizPlcRecipeNameConfig> configs);
}
