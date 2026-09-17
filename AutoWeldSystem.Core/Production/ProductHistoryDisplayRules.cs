namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 产品历史单条记录的显示规则。
/// </summary>
public static class ProductHistoryDisplayRules
{
    /// <summary>
    /// 按实际记录数决定是否直接显示产品单行，不依赖程序预设的面/焊点数量。
    /// </summary>
    public static bool ShouldFlattenSingleRecord(int recordCount)
        => recordCount == 1;
}
