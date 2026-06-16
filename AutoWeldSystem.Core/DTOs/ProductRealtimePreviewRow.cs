namespace AutoWeldSystem.Core.DTOs;

/// <summary>
/// 产品焊点实时预览行。
/// 主界面只显示值和结果，地址字段保留给地址预览或排查使用。
/// </summary>
public sealed class ProductRealtimePreviewRow
{
    public int StationNo { get; init; }

    public string Station { get; init; } = string.Empty;

    public string ProductNo { get; init; } = string.Empty;

    public string ProductNum { get; init; } = string.Empty;

    public string ProductModel { get; init; } = string.Empty;

    public int TouchIndex { get; init; }

    public string TouchNo { get; init; } = string.Empty;

    public string TouchResult { get; init; } = "--";

    /// <summary>
    /// 测试项字典主键，用作界面结构判断的稳定标识。
    /// </summary>
    public int ItemId { get; init; }

    public string ItemName { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public bool EnableActual { get; init; } = true;

    public bool EnableUpper { get; init; } = true;

    public bool EnableLower { get; init; } = true;

    public bool EnableResult { get; init; } = true;

    public string ActualValue { get; init; } = "--";

    public string UpperValue { get; init; } = "--";

    public string LowerValue { get; init; } = "--";

    public string Result { get; init; } = "--";

    public string RefreshTimeText { get; init; } = string.Empty;

    public string ActualAddress { get; init; } = string.Empty;

    public string UpperAddress { get; init; } = string.Empty;

    public string LowerAddress { get; init; } = string.Empty;

    public string ResultAddress { get; init; } = string.Empty;

    public int Sort { get; init; }
}
