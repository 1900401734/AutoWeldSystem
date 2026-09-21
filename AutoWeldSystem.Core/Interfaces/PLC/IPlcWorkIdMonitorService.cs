using AutoWeldSystem.Core.DTOs.Plc;

namespace AutoWeldSystem.Core.Interfaces.PLC;

public interface IPlcWorkIdMonitorService : IAsyncDisposable
{
    event EventHandler<PlcWorkIdSnapshot>? WorkIdChanged;

    PlcWorkIdSnapshot Current { get; }

    /// <summary>
    /// 获取指定工位最近一次读取到的 PLC 工单号快照。
    /// </summary>
    PlcWorkIdSnapshot GetCurrent(int stationNo);

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task ReloadAddressAsync(CancellationToken cancellationToken = default);
}
