using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Mes;
using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Services.Mes;

/// <summary>
/// MES 连接监控。
/// 判断依据：按系统设置中的心跳间隔调用 MES 在线检测接口，成功返回 S 即在线，否则视为离线。
/// </summary>
public sealed class MesConnectionMonitor : IMesConnectionMonitor, IDisposable
{
    private readonly IMesProvider _mesProvider;
    private readonly ILocalizationService _localizer;
    private readonly IAppSettingsService _settingsService;

    private AppSettings appSettings;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _disposed;

    public MesConnectionMonitor(IMesProvider mesProvider, ILocalizationService localizer, IAppSettingsService appSettingsService)
    {
        _mesProvider = mesProvider;
        _localizer = localizer;
        _settingsService = appSettingsService;
        appSettings = _settingsService.Get();
        _settingsService.SettingsChanged += OnSettingsChanged;
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
        _settingsService.SettingsChanged -= OnSettingsChanged;
        _disposed = true;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await CheckOnceAsync(cancellationToken);
                await Task.Delay(ResolveHeartbeatInterval(), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Publish(new MesConnectionSnapshot(false, Current.LastSuccessTime, DateTime.Now, ex.Message));
                await Task.Delay(ResolveHeartbeatInterval(), cancellationToken);
            }
        }
    }

    /// <summary>
    /// 每轮重新读取心跳间隔，设置页改动在下一轮生效（已在途的等待不会被打断）。
    /// </summary>
    private TimeSpan ResolveHeartbeatInterval()
    {
        var seconds = MesConnectionRules.NormalizeHeartbeatIntervalSeconds(appSettings.MesHeartbeatIntervalSeconds);
        return TimeSpan.FromSeconds(seconds);
    }

    private async Task CheckOnceAsync(CancellationToken cancellationToken)
    {
        // 首轮尚未探测过，传 null 让 provider 写一条基线日志；之后只在在线状态跳变时写。
        var previousOnline = Current.UpdatedTime == default
            ? (bool?)null
            : Current.IsConnected;
        var response = await _mesProvider.CheckSystemOnlineAsync(previousOnline, cancellationToken);
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

    private void OnSettingsChanged(object? sender, Core.Runtime.AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref appSettings, e.CurrentSettings);
    }
}
