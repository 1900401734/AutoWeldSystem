using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序统计的完工数量。三项同源，保证 总数 = 合格 + 不良 恒等式对 MES 成立。
/// </summary>
public sealed record FinishQuantities(int ActualQty, int QualifiedQty, int FailedQty)
{
    public static FinishQuantities Empty { get; } = new(0, 0, 0);
}

/// <summary>
/// 程序计数模式下的完工数量统计规则。
/// 口径：任务下两工位合计、已完成采集且未删除的产品数（按产品编号去重，不是焊点行数）；
/// 合格 = 产品结果为 OK；不良 = 总数 − 合格，产品结果 Unknown 归入不良以保住恒等式。
/// 试焊件照常计入：报表与过程参数都带试焊件标志，客户按标志自行区分。
/// </summary>
public static class FinishQuantityRules
{
    public static FinishQuantities Calculate(IEnumerable<BizWeldPointRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var products = WeldPointRecordScopeRules.ExcludeDeleted(records)
            .Where(record => record.ProductCompleted)
            .Where(record => !string.IsNullOrWhiteSpace(record.ProductNo))
            .GroupBy(record => (record.StationNo, ProductNo: record.ProductNo.Trim()), StationProductComparer.Instance)
            .ToList();

        var total = products.Count;
        var qualified = products.Count(group => TestResultRules.IsOk(ProductResultResolver.Resolve(group)));
        return new FinishQuantities(total, qualified, total - qualified);
    }

    private sealed class StationProductComparer : IEqualityComparer<(int StationNo, string ProductNo)>
    {
        public static StationProductComparer Instance { get; } = new();

        public bool Equals((int StationNo, string ProductNo) x, (int StationNo, string ProductNo) y)
            => x.StationNo == y.StationNo
                && string.Equals(x.ProductNo, y.ProductNo, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((int StationNo, string ProductNo) obj)
            => HashCode.Combine(obj.StationNo, StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ProductNo));
    }
}
