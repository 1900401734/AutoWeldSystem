namespace AutoWeldSystem.Core.Mes;

/// <summary>
/// MES 完工上报工时字段的精度规则。
/// </summary>
public static class MesWorkHourRules
{
    /// <summary>
    /// 保留的小数位数。MES 的 WorkHour 以小时为单位。
    /// </summary>
    public const int DecimalPlaces = 2;

    // 定标基数：decimal 自带标度且 JSON 按标度原样输出，只做 Round 时 1 会序列化成 1 而不是 1.00。
    // 加上这个 0.00m 把标度固定为两位，MES 收到的工时字面量始终是两位小数。
    private const decimal ScalePadding = 0.00m;

    /// <summary>
    /// 按开工到完工的时间区间计算上报工时，单位为小时，保留两位小数。
    /// 采用四舍五入而非 .NET 默认的银行家舍入：工时是给现场和对账看的时长，
    /// 默认 ToEven 会把 1.005 变成 1.00，现场核对时无法解释。
    /// 不足 0.01 小时（约 18 秒）的任务上报 0.00，这是舍入的真实结果，不抬升为 0.01。
    /// </summary>
    /// <param name="startTime">任务开工时间。</param>
    /// <param name="endTime">任务完工时间。</param>
    /// <returns>两位小数的工时；结束时间早于开工时间时返回 0.00。</returns>
    public static decimal FromRange(DateTime startTime, DateTime endTime)
    {
        if (endTime <= startTime)
        {
            return ScalePadding;
        }

        return Normalize(Convert.ToDecimal((endTime - startTime).TotalHours));
    }

    /// <summary>
    /// 把已有工时数值归一化为两位小数，用于补传前重算等已持有小时数的场景。
    /// </summary>
    /// <param name="workHour">以小时为单位的工时。</param>
    /// <returns>两位小数的工时；负值返回 0.00。</returns>
    public static decimal Normalize(decimal workHour)
    {
        if (workHour <= 0m)
        {
            return ScalePadding;
        }

        return Math.Round(workHour, DecimalPlaces, MidpointRounding.AwayFromZero) + ScalePadding;
    }
}
