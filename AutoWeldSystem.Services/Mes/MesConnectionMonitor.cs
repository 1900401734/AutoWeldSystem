using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Mes;
using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Services.Mes;

/// <summary>
/// MES 连接监控。
/// 判断依据：按系统设置中的心跳间隔调用 MES 在线检测接口，成功返回 S 即在线，连续三次失败才确认离线。
/// </summary>
public sealed class MesConnectionMonitor : IMesConnectionMonitor, IDisposable
{
    private readonly IMesProvider _mesProvider;
    private readonly ILocalizationService _localizer;
    private readonly IAppSettingsService _settingsService;

    private AppSettings appSettings;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private int _consecutiveProbeFailures;
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
                await CheckOnceSafelyAsync(cancellationToken);
                await Task.Delay(ResolveHeartbeatInterval(), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
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

    /// <summary>
    /// 执行一次在线探测；未预期异常与普通失败共用连续失败阈值。
    /// </summary>
    internal async Task CheckOnceSafelyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await CheckOnceAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            ApplyProbeFailure(ex.Message);
        }
    }

    private async Task CheckOnceAsync(CancellationToken cancellationToken)
    {
        // 自动心跳不写 MES 交互日志，业务在线状态仍需连续三次失败才切换。
        var response = await _mesProvider.CheckSystemOnlineAsync(previousOnline: null, cancellationToken);
        var probeSucceeded = string.Equals(response.Status, AppConstants.MesStatus.Success, StringComparison.OrdinalIgnoreCase);
        if (probeSucceeded)
        {
            ApplyProbeSuccess();
            return;
        }

        ApplyProbeFailure(response.Msg);
    }

    private void ApplyProbeSuccess()
    {
        _consecutiveProbeFailures = 0;
        Publish(new MesConnectionSnapshot(
            true,
            DateTime.Now,
            DateTime.Now,
            _localizer.GetString(TextKeys.Mes.StateConnected)));
    }

    private void ApplyProbeFailure(string message)
    {
        _consecutiveProbeFailures = Math.Min(
            _consecutiveProbeFailures + 1,
            MesConnectionRules.OfflineFailureThreshold);

        if (!MesConnectionRules.IsOfflineConfirmed(_consecutiveProbeFailures)
            || (Current.UpdatedTime != default && !Current.IsConnected))
        {
            return;
        }

        Publish(new MesConnectionSnapshot(
            false,
            Current.LastSuccessTime,
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
