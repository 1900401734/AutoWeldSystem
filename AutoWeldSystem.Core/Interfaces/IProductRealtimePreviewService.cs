using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 产品焊点实时预览服务。
/// 后台高频读取 PLC 当前产品数据，供 MonitorView 做轻量展示。
/// </summary>
public interface IProductRealtimePreviewService : IAsyncDisposable
{
    event EventHandler<ProductRealtimePreviewSnapshot>? SnapshotChanged;

    ProductRealtimePreviewSnapshot? GetCurrent(int stationNo);

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
