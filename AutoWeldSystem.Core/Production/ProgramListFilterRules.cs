using AutoWeldSystem.Core.DTOs;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// MES 程序列表按产品工号查询和筛选规则。
/// 未开启“按产品工号筛选程序”时取回并保留全部程序（不窄化）；
/// 开启后先让 MES 按工号查询，再按同一工号在客户端兜底筛选。
/// </summary>
public static class ProgramListFilterRules
{
    /// <summary>
    /// 生成 MES 程序列表接口的 productNum 查询参数。
    /// 工号原样传出（含 "#"），由 HTTP 层负责百分号编码。
    /// 返回 null 表示本次不带该参数，由 MES 返回该设备的全部程序。
    /// </summary>
    /// <param name="useProductNumberFilter">是否开启按产品工号筛选。</param>
    /// <param name="workOrderProdNum">当前工单的产品工号；空白时不带参数。</param>
    /// <returns>可直接作为查询参数的产品工号；不需要按工号查询时返回 null。</returns>
    public static string? ResolveQueryProductNum(
        bool useProductNumberFilter,
        string? workOrderProdNum)
    {
        if (!useProductNumberFilter || string.IsNullOrWhiteSpace(workOrderProdNum))
        {
            return null;
        }

        return workOrderProdNum.Trim();
    }

    /// <summary>
    /// 按设置和工单产品工号筛选 MES 程序列表。
    /// MES 已按 productNum 查询时该筛选是兜底，兼容忽略该参数的旧版 MES。
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
