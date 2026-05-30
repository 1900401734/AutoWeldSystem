namespace AutoWeldSystem.Core.DTOs;

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
    string ProductResult,
    DateTime RefreshTime,
    IReadOnlyList<ProductRealtimePreviewRow> Rows,
    string Message = "")
{
    public string Station => $"工位{StationNo}";

    public string RefreshTimeText => RefreshTime == default
        ? string.Empty
        : RefreshTime.ToString("HH:mm:ss");
}
