namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 产品历史单采集点的显示规则。
/// </summary>
public static class ProductHistoryDisplayRules
{
    /// <summary>
    /// 只有工艺配置一个采集点且产品实际仅有一条记录时，才直接显示为产品单行。
    /// </summary>
    public static bool ShouldFlattenSinglePoint(int? configuredPointCount, int recordCount)
        => configuredPointCount == 1 && recordCount == 1;
}
