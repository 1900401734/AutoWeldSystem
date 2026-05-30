namespace AutoWeldSystem.UI.Forms;

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

    public string ResolvedAddress { get; init; } = string.Empty;

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
