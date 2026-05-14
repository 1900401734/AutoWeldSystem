using AutoWeldSystem.Core.DTOs;

namespace AutoWeldSystem.Core.Interfaces;

public interface IPlcWorkIdMonitorService : IAsyncDisposable
{
    event EventHandler<PlcWorkIdSnapshot>? WorkIdChanged;

    PlcWorkIdSnapshot Current { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task ReloadAddressAsync(CancellationToken cancellationToken = default);
}
