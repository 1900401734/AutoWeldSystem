using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Plc;
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
    private readonly IPlcProductionMonitorService _plcProductionMonitorService;
    private readonly IDeviceStatusService _deviceStatusService;
    private readonly object _sync = new();
    private readonly Dictionary<string, bool> _connectionStates = new();
    private readonly Dictionary<int, AlarmState> _alarmStates = new();

    private AppSettings _currentSettings;
    private bool _started;

    public DeviceLifecycleLogCoordinator(
        IAppSettingsService settingsService,
        IDeviceLifecycleLogService logService,
        IPlcCommunicationService plcCommunicationService,
        IMesConnectionMonitor mesConnectionMonitor,
        ICenterTelemetrySyncService centerTelemetrySyncService,
        IPlcProductionMonitorService plcProductionMonitorService,
        IDeviceStatusService deviceStatusService)
    {
        _settingsService = settingsService;
        _logService = logService;
        _plcCommunicationService = plcCommunicationService;
        _mesConnectionMonitor = mesConnectionMonitor;
        _centerTelemetrySyncService = centerTelemetrySyncService;
        _plcProductionMonitorService = plcProductionMonitorService;
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
            _plcProductionMonitorService.StatusChanged += PlcProductionMonitorService_StatusChanged;
        }

        var occurredTime = DateTime.Now;
        _logService.Write(DeviceLifecycleLogRules.CreateSoftwareStartedEntry(CurrentDeviceId, occurredTime));
        RecordSoftwareStartedStatus(occurredTime);
        RecordInitialConnectionSnapshots();
    }

    public void Stop()
    {
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
            _plcProductionMonitorService.StatusChanged -= PlcProductionMonitorService_StatusChanged;
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

        RecordSoftwareStoppedStatus(occurredTime);
    }

    private string CurrentDeviceId => Volatile.Read(ref _currentSettings).DeviceId?.Trim() ?? string.Empty;

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void RecordSoftwareStartedStatus(DateTime occurredTime)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _deviceStatusService.ChangeStatusAsync(
                    ProductionConstants.MesDeviceStatuses.PoweredOn,
                    "Software started successfully.",
                    SourceApplication,
                    stationNo: ProductionConstants.Stations.SharedStationNo,
                    occurredTime: occurredTime,
                    forceWrite: true);
            }
            catch
            {
                // Startup status reporting must not block the main application.
            }
        });
    }

    private void RecordSoftwareStoppedStatus(DateTime occurredTime)
    {
        try
        {
            _ = _deviceStatusService.ChangeStatusAsync(
                ProductionConstants.MesDeviceStatuses.Stopped,
                "Software is closing.",
                SourceApplication,
                reportToMes: true,
                stationNo: ProductionConstants.Stations.SharedStationNo,
                occurredTime: occurredTime,
                forceWrite: true,
                reportInBackground: true);
        }
        catch
        {
            // Shutdown must continue even if the local status log cannot be written.
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

    private void PlcProductionMonitorService_StatusChanged(object? sender, PlcProductionSnapshot snapshot)
    {
        RecordAlarmChange(snapshot);
    }

    private void RecordPlcConnection(PlcConnectionSnapshot snapshot)
    {
        if (snapshot.State == Core.Enums.PlcConnectionState.Stopped && snapshot.LastConnectedTime is null)
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

    private void RecordAlarmChange(PlcProductionSnapshot snapshot)
    {
        var stationNo = Math.Max(1, snapshot.StationNo);
        DeviceAlarmLogDecision decision;
        lock (_sync)
        {
            _alarmStates.TryGetValue(stationNo, out var previous);
            decision = DeviceLifecycleLogRules.DecideAlarmTransition(
                previous.StatusCode,
                previous.AlarmMessage,
                snapshot.DeviceStatusCode,
                snapshot.AlarmMessage);
            _alarmStates[stationNo] = new AlarmState(snapshot.DeviceStatusCode, snapshot.AlarmMessage);
        }

        if (!decision.ShouldWrite)
        {
            return;
        }

        _logService.Write(DeviceLifecycleLogRules.CreateAlarmEntry(
            CurrentDeviceId,
            stationNo,
            decision.EventType,
            snapshot.AlarmMessage,
            snapshot.UpdatedTime == default ? DateTime.Now : snapshot.UpdatedTime));
    }

    private static IEnumerable<int> ResolveStationNumbers(AppSettings settings)
    {
        yield return ProductionConstants.Stations.DefaultStationNo;
        if (settings.EnableDualStation)
        {
            yield return 2;
        }
    }

    private readonly record struct AlarmState(short? StatusCode, string? AlarmMessage);
}
