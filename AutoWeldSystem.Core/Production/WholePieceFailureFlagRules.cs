namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 整件检测视觉失败标志规则。
/// 视觉检测失败或未识别到目标时向寄存器写入固定值 1，该值不是有效测量值。
/// 标志值写死不做配置：它是与视觉供应商约定的协议值，现场改错会让失败数据被当作合格值参与判定。
/// 现场量程不会与 1 冲突（高度、宽度不低于 10mm，对称度上限不超过 0.5）。
/// </summary>
public static class WholePieceFailureFlagRules
{
    /// <summary>
    /// 视觉失败标志值。
    /// </summary>
    public const decimal FailureFlagValue = 1m;

    /// <summary>
    /// 判断实测值是否为视觉失败标志。
    /// </summary>
    public static bool IsFailureFlag(decimal value) => value == FailureFlagValue;

    /// <summary>
    /// 判断测试项在缺面时是否必须判 NG。
    /// 严格项（对称度等）反映两侧相对偏差，缺任一面即无法确认，不能用另一面数值代替；
    /// 宽松项（高度、宽度）是单一尺寸，规格允许部分面不检测，有一面有效即可代表该尺寸。
    /// 新增测试项默认按严格处理，避免未经确认的项静默放行。
    /// </summary>
    public static bool IsStrictItem(string? itemName)
        => !WholePieceAbAggregationRules.IsProductLevelItem(itemName);

    /// <summary>
    /// 按失败标志策略筛选参与聚合的有效值。
    /// 严格项含任一失败标志时返回空集合，表示该项无法判定，由调用方判 NG；
    /// 宽松项剔除失败标志后返回剩余有效值，全部失败时同样返回空集合。
    /// </summary>
    public static IReadOnlyList<decimal> ResolveEffectiveValues(
        string? itemName,
        IReadOnlyList<decimal> sideValues)
    {
        ArgumentNullException.ThrowIfNull(sideValues);

        if (IsStrictItem(itemName))
        {
            return sideValues.Any(IsFailureFlag)
                ? Array.Empty<decimal>()
                : sideValues;
        }

        return sideValues.Where(value => !IsFailureFlag(value)).ToList();
    }
}
