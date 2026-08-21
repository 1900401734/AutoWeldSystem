using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 实时预览测试值有效性规则。
/// 测试值是否显示由面/焊点结果决定，不由数值是否为零或是否变化决定。
/// </summary>
public static class ProductRealtimePreviewRules
{
    /// <summary>
    /// 只有普通 OK/NG 结果表示当前面已经完成测试，可以读取并显示测试值。
    /// 焊前 NG、0、空、未知和读取失败均视为未完成测试。
    /// </summary>
    public static bool ShouldReadTestValues(string? touchResult)
        => TestResultRules.IsOk(touchResult) || TestResultRules.IsNg(touchResult);
}
