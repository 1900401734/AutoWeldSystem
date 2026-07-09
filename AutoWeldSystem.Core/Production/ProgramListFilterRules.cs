using AutoWeldSystem.Core.DTOs;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// MES 程序列表客户端筛选规则。
/// 未开启“按产品工号筛选程序”时返回本地同步的程序（不窄化）；
/// 开启后按工单产品工号在客户端筛选。
/// </summary>
public static class ProgramListFilterRules
{
    /// <summary>
    /// 按设置和工单产品工号筛选 MES 程序列表。
    /// </summary>
    /// <param name="programs">MES 返回的程序列表。</param>
    /// <param name="useProductNumberFilter">是否开启按产品工号筛选。</param>
    /// <param name="workOrderProdNum">当前工单的产品工号；空白时不收窄。</param>
    /// <returns>筛选后的程序列表。</returns>
    public static IReadOnlyList<MesProgramListItemData> Filter(
        IReadOnlyList<MesProgramListItemData> programs,
        bool useProductNumberFilter,
        string? workOrderProdNum)
    {
        ArgumentNullException.ThrowIfNull(programs);

        if (!useProductNumberFilter || string.IsNullOrWhiteSpace(workOrderProdNum))
        {
            return programs;
        }

        var normalizedProdNum = workOrderProdNum.Trim();
        return programs
            .Where(program => string.Equals(
                program.ProductNum?.Trim(),
                normalizedProdNum,
                StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
