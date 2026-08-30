using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Plc;

/// <summary>
/// 报表与 MES 过程参数的输出小数位格式。
/// 采集时 <c>ExpressionReadService</c> 已按偏移量表达式的小数位格式化并存库，
/// 这里是输出端的第二次格式化：只能减位或补零，恢复不了采集时被截掉的精度。
/// 截断还是四舍五入沿用系统设置里的全局模式，不单独配置。
/// </summary>
public readonly record struct OutputNumericFormat(int? DecimalPlaces, string? Mode)
{
    /// <summary>
    /// 不调整小数位，输出沿用采集时的位数。
    /// </summary>
    public static readonly OutputNumericFormat None = new(null, null);

    public static OutputNumericFormat ForReport(AppSettings? settings)
        => Create(settings?.ReportDecimalPlaces, settings);

    public static OutputNumericFormat ForProcessParameter(AppSettings? settings)
        => Create(settings?.ProcessParameterDecimalPlaces, settings);

    /// <summary>
    /// 归一化输出小数位。负数按未配置处理，超出偏移量表达式允许的上限时收敛到上限。
    /// </summary>
    public static int? NormalizeDecimalPlaces(int? decimalPlaces)
        => decimalPlaces is >= 0
            ? Math.Min(decimalPlaces.Value, PlcOffsetExpression.MaxDecimalPlaces)
            : null;

    /// <summary>
    /// 按输出配置格式化。未配置小数位时只做 PLC 文本规范化，与改动前的行为一致；
    /// 非数值文本（OK/NG、报表里表示不适用的斜杠、空值）由格式化器原样返回。
    /// </summary>
    public string Apply(string? value)
        => PlcStringNumericFormatter.Format(value, DecimalPlaces, DecimalPlaces is >= 0, Mode);

    private static OutputNumericFormat Create(int? decimalPlaces, AppSettings? settings)
    {
        var normalized = NormalizeDecimalPlaces(decimalPlaces);
        if (normalized is null)
        {
            return None;
        }

        // 关闭全局数值处理时按四舍五入，与采集侧 ExpressionReadService.FormatNumericValue 的既有约定保持一致。
        var mode = settings?.EnablePlcStringNumericFormatting ?? true
            ? settings?.PlcStringNumericFormatMode
            : AppConstants.PlcStringNumericFormatModes.Round;
        return new OutputNumericFormat(normalized, mode);
    }
}
