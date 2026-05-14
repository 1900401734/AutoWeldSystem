using AutoWeldSystem.Core.DTOs;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// MES 连接监控服务。
/// </summary>
public interface IMesConnectionMonitorService : IAsyncDisposable
{
    event EventHandler<MesConnectionSnapshot>? StatusChanged;

    MesConnectionSnapshot Current { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
