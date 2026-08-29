using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Production;

namespace AutoWeldSystem.Core.ViewModels;

/// <summary>
/// 单个工位的产品实时采集快照。
/// MonitorView 订阅该快照即可刷新界面，不直接读取 PLC。
/// </summary>
public sealed record ProductRealtimePreviewSnapshot(
    int StationNo,
    string ProductNo,
    string ProductNum,
    string ProductModel,
    string SchemeId,
    string TouchCountText,
    string PointName,
    string ProductResult,
    DateTime RefreshTime,
    IReadOnlyList<ProductRealtimePreviewRow> Rows,
    string Message = "")
{
    public string Station => $"工位{StationNo}";

    public string RefreshTimeText => RefreshTime == default
        ? string.Empty
        : RefreshTime.ToString("HH:mm:ss");

    /// <summary>
    /// 四面整件检测的合并显示列。非四面整件检测或四面未采集齐时为空。
    /// </summary>
    public IReadOnlyList<WholePieceMergedColumn> MergedColumns { get; init; } = Array.Empty<WholePieceMergedColumn>();

    /// <summary>
    /// 合并显示列对应的值，键为 <see cref="WholePieceMergedColumn.ColumnName"/>。
    /// </summary>
    public IReadOnlyDictionary<string, string> MergedValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 合并显示对应的 A/B 聚合字段定义，供产品历史用同一组规则聚合历史记录。
    /// </summary>
    public IReadOnlyList<WholePieceAbValueDefinition> MergedDefinitions { get; init; } =
        Array.Empty<WholePieceAbValueDefinition>();

    /// <summary>
    /// 合并显示中超出程序设定值的列名。仅程序计算模式下有值，PLC 读取模式没有判定依据，保持为空。
    /// </summary>
    public IReadOnlyList<string> MergedFailedColumns { get; init; } = Array.Empty<string>();
}
