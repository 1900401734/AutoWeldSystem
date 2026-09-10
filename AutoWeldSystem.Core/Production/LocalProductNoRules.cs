using System.Globalization;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序自算产品编号规则。
/// 现场 PLC 编号为 Int16 原值（裸十进制 1,2,3…），程序自算沿用同一形状，报表编号列外观不变。
/// 编号只增不回收：已删除产品的行仍留在库中参与取最大值，避免新品复用旧号后撞上自然键去重被静默丢件。
/// </summary>
public static class LocalProductNoRules
{
    /// <summary>
    /// 依据该任务该工位已有的全部产品编号（含已删除）计算下一个编号。
    /// 非数字编号（PLC 模式遗留）忽略；无可解析编号时从 1 开始。
    /// </summary>
    public static string NextProductNo(IEnumerable<string?> existingProductNos)
    {
        ArgumentNullException.ThrowIfNull(existingProductNos);

        long max = 0;
        foreach (var productNo in existingProductNos)
        {
            if (TryParse(productNo, out var value) && value > max)
            {
                max = value;
            }
        }

        return (max + 1).ToString(CultureInfo.InvariantCulture);
    }

    public static bool TryParse(string? productNo, out long value)
        => long.TryParse(productNo?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
