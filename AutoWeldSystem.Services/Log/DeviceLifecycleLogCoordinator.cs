using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Services.Log;

/// <summary>
/// Centralizes device lifecycle logging subscriptions.
/// This keeps UI and business services focused on their own work while lifecycle logs remain consistent.
/// </summary>
public sealed class DeviceLifecycleLogCoordinator : IDeviceLifecycleLogCoordinator
{
    private const string SourceApplication = "Application";
    private const string SourcePlc = "PLC";
    private const string SourceMes = "MES";
    private const string SourceCenterServer = "CenterServer";

    private readonly IAppSettingsService _settingsService;
    private readonly IDeviceLifecycleLogService _logService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IMesConnectionMonitor _mesConnectionMonitor;
    private readonly ICenterTelemetrySyncService _centerTelemetrySyncService;
    private readonly IDeviceStatusService _deviceStatusService;
    private readonly object _sync = new();
    private readonly Dictionary<string, bool> _connectionStates = new();

    private AppSettings _currentSettings;
    private CancellationTokenSource? _startupReplayCancellation;
    private Task? _startupReplayTask;
    private bool _started;

    public DeviceLifecycleLogCoordinator(
        IAppSettingsService settingsService,
        IDeviceLifecycleLogService logService,
        IPlcCommunicationService plcCommunicationService,
        IMesConnectionMonitor mesConnectionMonitor,
        ICenterTelemetrySyncService centerTelemetrySyncService,
        IDeviceStatusService deviceStatusService)
    {
        _settingsService = settingsService;
        _logService = logService;
        _plcCommunicationService = plcCommunicationService;
        _mesConnectionMonitor = mesConnectionMonitor;
        _centerTelemetrySyncService = centerTelemetrySyncService;
        _deviceStatusService = deviceStatusService;
        _currentSettings = settingsService.Get();
    }

    public void Start()
    {
        lock (_sync)
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _settingsService.SettingsChanged += SettingsService_SettingsChanged;
            _plcCommunicationService.StatusChanged += PlcCommunicationService_StatusChanged;
            _mesConnectionMonitor.StatusChanged += MesConnectionMonitor_StatusChanged;
            _centerTelemetrySyncService.StatusChanged += CenterTelemetrySyncService_StatusChanged;
        }

        var occurredTime = DateTime.Now;
        _logService.Write(DeviceLifecycleLogRules.CreateSoftwareStartedEntry(CurrentDeviceId, occurredTime));
        RecordSoftwareStartedStatus(occurredTime);
        RecordInitialConnectionSnapshots();
    }

    public void Stop()
    {
        CancellationTokenSource? startupReplayCancellation;
        Task? startupReplayTask;
        lock (_sync)
        {
            if (!_started)
            {
                return;
            }

            _started = false;
            _settingsService.SettingsChanged -= SettingsService_SettingsChanged;
            _plcCommunicationService.StatusChanged -= PlcCommunicationService_StatusChanged;
            _mesConnectionMonitor.StatusChanged -= MesConnectionMonitor_StatusChanged;
            _centerTelemetrySyncService.StatusChanged -= CenterTelemetrySyncService_StatusChanged;
            startupReplayCancellation = _startupReplayCancellation;
            startupReplayTask = _startupReplayTask;
        }

        var occurredTime = DateTime.Now;
        try
        {
            _logService.Write(DeviceLifecycleLogRules.CreateSoftwareStoppedEntry(CurrentDeviceId, occurredTime));
        }
        catch
        {
            // 软件关闭状态上报比生命周期日志写入更关键，日志失败不能阻断停机状态上传。
        }

        RecordSoftwareStoppedStatus(occurredTime, startupReplayCancellation, startupReplayTask);
    }

    private string CurrentDeviceId => Volatile.Read(ref _currentSettings).DeviceId?.Trim() ?? string.Empty;

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void RecordSoftwareStartedStatus(DateTime occurredTime)
    {
        try
        {
            _deviceStatusService.ChangeStatusAsync(
                    ProductionConstants.MesDeviceStatuses.PoweredOn,
                    "开机",
                    SourceApplication,
                    reportToMes: false,
                    stationNo: ProductionConstants.Stations.SharedStationNo,
                    occurredTime: occurredTime,
                    forceWrite: true)
                .GetAwaiter()
                .GetResult();
        }
        catch
        {
            // 开机状态写入失败不能阻断主程序启动，后续补传仍可处理已有 JSONL。
        }

        var replayCancellation = new CancellationTokenSource();
        lock (_sync)
        {
            if (!_started)
            {
                replayCancellation.Dispose();
                return;
            }

            var replayTask = RetryPendingUploadsSafelyAsync(replayCancellation.Token);
            _startupReplayCancellation = replayCancellation;
            _startupReplayTask = replayTask;
            ObserveStartupReplayCompletion(replayCancellation, replayTask);
        }
    }

    private void ObserveStartupReplayCompletion(CancellationTokenSource replayCancellation, Task replayTask)
    {
        _ = replayTask.ContinueWith(
            _ =>
            {
                lock (_sync)
                {
                    if (ReferenceEquals(_startupReplayCancellation, replayCancellation))
                    {
                        _startupReplayCancellation = null;
                    }

                    if (ReferenceEquals(_startupReplayTask, replayTask))
                    {
                        _startupReplayTask = null;
                    }
                }

                replayCancellation.Dispose();
            },
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);
    }

    private void RecordSoftwareStoppedStatus(
        DateTime occurredTime,
        CancellationTokenSource? startupReplayCancellation,
        Task? startupReplayTask)
    {
        try
        {
            var timeoutSeconds = Math.Max(3, CurrentSettings.MesTimeoutSeconds);
            using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var timeoutToken = timeoutSource.Token;
            try
            {
                startupReplayCancellation?.Cancel();
            }
            catch (Exception)
            {
                // 取消源已释放或取消回调异常都不能阻断最终停机状态落盘和上传。
            }

            // 设备状态调用在首次 await 前仍会同步等待 JSONL 锁并写文件，放到线程池后才能让整个退出等待受同一超时约束。
            var stopUploadTask = Task.Run(
                () => _deviceStatusService.ChangeStatusAsync(
                    ProductionConstants.MesDeviceStatuses.Stopped,
                    "停机",
                    SourceApplication,
                    reportToMes: true,
                    stationNo: ProductionConstants.Stations.SharedStationNo,
                    occurredTime: occurredTime,
                    forceWrite: true,
                    cancellationToken: timeoutToken),
                CancellationToken.None);
            startupReplayTask?.WaitAsync(timeoutToken).GetAwaiter().GetResult();
            stopUploadTask.WaitAsync(timeoutToken).GetAwaiter().GetResult();
        }
        catch
        {
            // 超时或写入失败时继续退出；已落盘的 Pending/Failed 状态由下次启动补传。
        }
    }

    private async Task RetryPendingUploadsSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _deviceStatusService.RetryPendingUploadsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 退出时取消启动补传，让最终停机状态在同一超时预算内取得上传门禁。
        }
        catch
        {
            // 启动补传失败保留 JSONL 待传状态，不阻断登录界面。
        }
    }

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }

    private void RecordInitialConnectionSnapshots()
    {
        foreach (var stationNo in ResolveStationNumbers(CurrentSettings))
        {
            RecordPlcConnection(_plcCommunicationService.GetCurrent(stationNo));
        }

        if (_mesConnectionMonitor.Current.UpdatedTime != default)
        {
            RecordConnection(SourceMes, 0, _mesConnectionMonitor.Current.IsConnected, _mesConnectionMonitor.Current.Message);
        }

        if (CurrentSettings.EnableCenterServerSync && _centerTelemetrySyncService.Current.UpdatedTime != default)
        {
            RecordConnection(SourceCenterServer, 0, _centerTelemetrySyncService.Current.IsConnected, _centerTelemetrySyncService.Current.Message);
        }
    }

    private void PlcCommunicationService_StatusChanged(object? sender, PlcConnectionSnapshot snapshot)
    {
        RecordPlcConnection(snapshot);
    }

    private void MesConnectionMonitor_StatusChanged(object? sender, MesConnectionSnapshot snapshot)
    {
        RecordConnection(SourceMes, 0, snapshot.IsConnected, snapshot.Message);
    }

    private void CenterTelemetrySyncService_StatusChanged(object? sender, CenterTelemetryConnectionSnapshot snapshot)
    {
        if (!CurrentSettings.EnableCenterServerSync)
        {
            return;
        }

        RecordConnection(SourceCenterServer, 0, snapshot.IsConnected, snapshot.Message);
    }

    private void RecordPlcConnection(PlcConnectionSnapshot snapshot)
    {
        if (!DeviceLifecycleLogRules.ShouldRecordPlcConnectionState(snapshot.State))
        {
            return;
        }

        RecordConnection(SourcePlc, Math.Max(1, snapshot.StationNo), snapshot.IsConnected, snapshot.Message);
    }

    private void RecordConnection(string source, int stationNo, bool connected, string message)
    {
        var key = $"{source}:{stationNo}";
        lock (_sync)
        {
            _connectionStates.TryGetValue(key, out var previous);
            var hasPrevious = _connectionStates.ContainsKey(key);
            if (!DeviceLifecycleLogRules.HasConnectionStatusChanged(hasPrevious ? previous : null, connected))
            {
                return;
            }

            _connectionStates[key] = connected;
        }

        _logService.Write(DeviceLifecycleLogRules.CreateSelfCheckEntry(
            CurrentDeviceId,
            stationNo,
            source,
            connected,
            message,
            DateTime.Now));
    }

    private static IEnumerable<int> ResolveStationNumbers(AppSettings settings)
    {
        yield return ProductionConstants.Stations.DefaultStationNo;
        if (settings.EnableDualStation)
        {
            yield return 2;
        }
    }

}
