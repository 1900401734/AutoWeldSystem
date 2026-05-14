using AutoWeldSystem.Core.DTOs;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// PLC 生产状态监控服务。
/// </summary>
public interface IPlcProductionMonitorService : IAsyncDisposable
{
    event EventHandler<PlcProductionSnapshot>? StatusChanged;

    PlcProductionSnapshot Current { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task ReloadAddressesAsync(CancellationToken cancellationToken = default);
}
