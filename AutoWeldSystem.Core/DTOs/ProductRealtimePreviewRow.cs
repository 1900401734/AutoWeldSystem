namespace AutoWeldSystem.Core.DTOs;

/// <summary>
/// 产品焊点实时预览行。
/// 主界面只显示值和结果，地址字段保留给地址预览或排查使用。
/// </summary>
public sealed class ProductRealtimePreviewRow
{
    /// <summary>
    /// 工位号：1/2
    /// </summary>
    public int StationNo { get; init; }

    /// <summary>
    /// 字符串：工位1/工位2
    /// </summary>
    public string Station { get; init; } = string.Empty;

    /// <summary>
    /// 产品编号
    /// </summary>
    public string ProductNo { get; init; } = string.Empty;

    /// <summary>
    /// 产品工号
    /// </summary>
    public string ProductNum { get; init; } = string.Empty;

    /// <summary>
    /// 产品型号
    /// </summary>
    public string ProductModel { get; init; } = string.Empty;

    public int TouchIndex { get; init; }

    public string TouchNo { get; init; } = string.Empty;

    public string TouchResult { get; init; } = "--";

    public string PointName { get; init; } = "焊点";

    public string PointNoHeader { get; init; } = "焊点序号";

    public string PointResultHeader { get; init; } = "焊点结果";

    public string PointCountHeader { get; init; } = "焊点数";

    /// <summary>
    /// 测试项字典主键，用作界面结构判断的稳定标识。
    /// </summary>
    public int ItemId { get; init; }

    public string ItemName { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public bool EnableActual { get; init; } = true;

    /// <summary>
    /// 启用上限
    /// </summary>
    public bool EnableUpper { get; init; } = true;

    /// <summary>
    /// 启用下限
    /// </summary>
    public bool EnableLower { get; init; } = true;

    /// <summary>
    /// 启用结果
    /// </summary>
    public bool EnableResult { get; init; } = true;

    public string ActualHeader { get; init; } = string.Empty;

    public string UpperHeader { get; init; } = string.Empty;

    public string LowerHeader { get; init; } = string.Empty;

    public string ResultHeader { get; init; } = string.Empty;

    /// <summary>
    /// 实际值
    /// </summary>
    public string ActualValue { get; init; } = "--";

    /// <summary>
    /// 上限值
    /// </summary>
    public string UpperValue { get; init; } = "--";

    /// <summary>
    /// 下限值
    /// </summary>
    public string LowerValue { get; init; } = "--";

    /// <summary>
    /// 结果
    /// </summary>
    public string Result { get; init; } = "--";

    public string RefreshTimeText { get; init; } = string.Empty;

    /// <summary>
    /// 实际值地址
    /// </summary>
    public string ActualAddress { get; init; } = string.Empty;

    /// <summary>
    /// 上限值地址
    /// </summary>
    public string UpperAddress { get; init; } = string.Empty;

    /// <summary>
    /// 下限值地址
    /// </summary>
    public string LowerAddress { get; init; } = string.Empty;

    /// <summary>
    /// 结果地址
    /// </summary>
    public string ResultAddress { get; init; } = string.Empty;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; init; }
}
