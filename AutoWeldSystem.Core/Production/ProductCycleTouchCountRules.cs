using System.Globalization;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 产品周期实际采集点数规则，程序数量是允许采集的上限。
/// </summary>
public static class ProductCycleTouchCountRules
{
    public static int ResolveActualTouchCount(int programTouchCount, string? actualTouchCountText)
    {
        if (programTouchCount <= 0)
        {
            throw new InvalidOperationException("程序焊点数量无效。");
        }

        var actualTouchCount = int.TryParse(
                actualTouchCountText?.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsed)
            && parsed > 0
                ? parsed
                : programTouchCount;
        if (actualTouchCount > programTouchCount)
        {
            throw new InvalidOperationException(
                $"实际焊点数“{actualTouchCount}”不能大于程序焊点数“{programTouchCount}”。");
        }

        return actualTouchCount;
    }
}
