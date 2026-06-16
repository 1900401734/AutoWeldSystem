using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Core.Interfaces.MES;

/// <summary>
/// MES 连接监控服务。
/// </summary>
public interface IMesConnectionMonitor : IAsyncDisposable
{
    event EventHandler<MesConnectionSnapshot>? StatusChanged;

    MesConnectionSnapshot Current { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
