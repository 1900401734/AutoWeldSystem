using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.DTOs.Plc;

namespace AutoWeldSystem.Core.Interfaces.PLC;

/// <summary>
/// PLC 焊接周期监控服务。
/// 负责监听焊接开始和焊接结束信号，并在一个焊点周期结束时触发采集。
/// </summary>
public interface IPlcWeldCycleMonitorService : IAsyncDisposable
{
    /// <summary>
    /// 焊点采集完成事件，后续 MonitorView 可用它实时刷新最新采集数据。
    /// </summary>
    event EventHandler<BizWeldPointRecord>? WeldPointCollected;

    /// <summary>
    /// 有效的 PLC 产品数据就绪上升沿。启动时残留的高电平不会触发该事件。
    /// </summary>
    event EventHandler<PlcProductReadySnapshot>? ProductReady;

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task ReloadAddressesAsync(CancellationToken cancellationToken = default);
}
