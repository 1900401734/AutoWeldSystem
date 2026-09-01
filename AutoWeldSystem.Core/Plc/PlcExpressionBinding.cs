using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Plc;

/// <summary>
/// PLC 偏移表达式解析后的可读地址信息。
/// 上层界面和采集服务只需要使用这个结果，不需要重复解析表达式格式。
/// SubtrahendAddress 有值表示计算式（如 (134-84)），读取时需要读两个地址并相减。
/// </summary>
public sealed record PlcExpressionBinding(string Address, string DataType, int Rule, string Expression, int? DecimalPlaces = null, bool IsAbsoluteAddress = false, string? SubtrahendAddress = null)
{
    public static PlcExpressionBinding Empty { get; } = new(
        string.Empty,
        AppConstants.PlcDataTypes.Int16,
        0,
        string.Empty,
        null);

    public bool HasAddress => !string.IsNullOrWhiteSpace(Address);

    /// <summary>
    /// 是否为需要两个地址相减的计算式。
    /// </summary>
    public bool IsCalculated => !string.IsNullOrWhiteSpace(SubtrahendAddress);
}
