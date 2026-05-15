using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Core.Interfaces;

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

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task ReloadAddressesAsync(CancellationToken cancellationToken = default);
}
