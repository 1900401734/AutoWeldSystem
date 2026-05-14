using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;

namespace AutoWeldSystem.Services.Mes;

/// <summary>
/// MES 连接监控。
/// 判断依据：定时调用 MES ServerTime 轻量接口，成功返回 S 即在线，否则视为离线。
/// </summary>
public sealed class MesConnectionMonitorService : IMesConnectionMonitorService, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IMesProvider _mesProvider;
    private readonly ILocalizationService _localizer;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _disposed;

    public MesConnectionMonitorService(IMesProvider mesProvider, ILocalizationService localizer)
    {
        _mesProvider = mesProvider;
        _localizer = localizer;
        Current = new MesConnectionSnapshot(false, null, default, _localizer.GetString(TextKeys.Mes.StateChecking));
    }

    public event EventHandler<MesConnectionSnapshot>? StatusChanged;

    public MesConnectionSnapshot Current { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_loopTask is { IsCompleted: false })
        {
            return Task.CompletedTask;
        }

        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = Task.Run(() => RunAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }

        if (_loopTask is null)
        {
            return;
        }

        try
        {
            await _loopTask.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
        }
        catch
        {
            // 停止监控不应影响程序退出。
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
        }

        _cts?.Dispose();
        _disposed = true;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await CheckOnceAsync(cancellationToken);
                await Task.Delay(PollInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Publish(new MesConnectionSnapshot(false, Current.LastSuccessTime, DateTime.Now, ex.Message));
                await Task.Delay(PollInterval, cancellationToken);
            }
        }
    }

    private async Task CheckOnceAsync(CancellationToken cancellationToken)
    {
        var response = await _mesProvider.ProbeServerTimeAsync(cancellationToken);
        var isConnected = string.Equals(response.Status, AppConstants.MesStatus.Success, StringComparison.OrdinalIgnoreCase);
        var message = isConnected
            ? _localizer.GetString(TextKeys.Mes.StateConnected)
            : response.Msg;

        Publish(new MesConnectionSnapshot(
            isConnected,
            isConnected ? DateTime.Now : Current.LastSuccessTime,
            DateTime.Now,
            message));
    }

    private void Publish(MesConnectionSnapshot snapshot)
    {
        Current = snapshot;
        StatusChanged?.Invoke(this, snapshot);
    }
}
