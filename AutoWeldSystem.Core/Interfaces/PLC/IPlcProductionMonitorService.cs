using AutoWeldSystem.Core.DTOs.Plc;

namespace AutoWeldSystem.Core.Interfaces.PLC;

/// <summary>
/// PLC 生产状态监控服务。
/// </summary>
public interface IPlcProductionMonitorService : IAsyncDisposable
{
    event EventHandler<PlcProductionSnapshot>? StatusChanged;

    PlcProductionSnapshot Current { get; }

    /// <summary>
    /// 获取指定工位最近一次读取到的生产指标快照。
    /// </summary>
    PlcProductionSnapshot GetCurrent(int stationNo);

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task ReloadAddressesAsync(CancellationToken cancellationToken = default);
}
