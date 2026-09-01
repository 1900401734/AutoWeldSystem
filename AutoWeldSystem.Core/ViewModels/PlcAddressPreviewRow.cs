namespace AutoWeldSystem.Core.ViewModels;

/// <summary>
/// PLC 地址预览表格行。
/// 该类型只服务于界面对点，不参与生产数据保存。
/// </summary>
public sealed class PlcAddressPreviewRow
{
    public string Station { get; init; } = string.Empty;

    public string ProductNum { get; init; } = string.Empty;

    public string ProductModel { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string TouchNo { get; init; } = string.Empty;

    public string ValueRole { get; init; } = string.Empty;

    public string BaseAddress { get; init; } = string.Empty;

    public int ContextOffset { get; init; }

    public string Expression { get; init; } = string.Empty;

    public string DataType { get; init; } = string.Empty;

    public int Rule { get; init; }

    /// <summary>
    /// 表达式中的固定小数位配置；为空表示沿用历史默认格式。
    /// </summary>
    public int? DecimalPlaces { get; init; }

    public string ResolvedAddress { get; init; } = string.Empty;

    /// <summary>
    /// 计算式减数的最终地址；非计算式为空。
    /// </summary>
    public string SubtrahendAddress { get; init; } = string.Empty;

    /// <summary>
    /// 是否为需要两个地址相减的计算式。
    /// </summary>
    public bool IsCalculated => !string.IsNullOrWhiteSpace(SubtrahendAddress);

    /// <summary>
    /// 表格「最终地址」列的显示文本。计算式显示两个操作数地址，便于现场核对配对是否正确。
    /// </summary>
    public string AddressDisplay => IsCalculated
        ? $"{ResolvedAddress} - {SubtrahendAddress}"
        : ResolvedAddress;

    public static PlcAddressPreviewRow Info(int stationNo, string message)
    {
        return new PlcAddressPreviewRow
        {
            Station = $"工位{stationNo}",
            Category = "提示",
            ValueRole = message
        };
    }
}
