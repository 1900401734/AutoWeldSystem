using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Production;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// PLC 生产状态监控。
/// 定时读取地址维护页中配置的设备状态和产量指标，向界面发布快照。
/// </summary>
public sealed class ProductionMonitorService : IPlcProductionMonitorService, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan BusinessLogInterval = TimeSpan.FromSeconds(30);

    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IPlcAddressService _plcAddressService;
    private readonly ILocalizationService _localizer;
    private readonly IMesConnectionMonitor _mesConnectionMonitorService;
    private readonly IDeviceStatusService _deviceStatusService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly IPlcAlarmAddressService _plcAlarmAddressService;
    private readonly IAppSettingsService _settingsService;
    private readonly SemaphoreSlim _addressSync = new(1, 1);
    private readonly object _businessLogSync = new();
    private readonly object _snapshotSync = new();
    private List<BizPlcAddress> _addresses = [];
    private readonly Dictionary<int, PlcProductionSnapshot> _stationSnapshots = new();
    private readonly Dictionary<int, string> _activeAlarmFailureKeys = new();
    private readonly HashSet<string> _sourceRemovedAlarmKeysAwaitingClear = new(StringComparer.OrdinalIgnoreCase);
    private PlcDeviceAlarmCycleState? _alarmCycleState;
    private PlcDeviceAlarmCycleState? _effectiveAlarmState;
    private PlcActiveAlarm? _pendingExceptionReassertion;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private string _lastBusinessLogKey = string.Empty;
    private DateTime _lastBusinessLogTime;
    private bool _disposed;

    public ProductionMonitorService(
        IPlcCommunicationService plcCommunicationService,
        IPlcAddressService plcAddressService,
        ILocalizationService localizer,
        IMesConnectionMonitor mesConnectionMonitorService,
        IDeviceStatusService deviceStatusService,
        IWeldTaskService weldTaskService,
        IProgramExceptionLogService exceptionLogService,
        IPlcAlarmAddressService plcAlarmAddressService,
        IAppSettingsService settingsService)
    {
        _plcCommunicationService = plcCommunicationService;
        _plcAddressService = plcAddressService;
        _localizer = localizer;
        _mesConnectionMonitorService = mesConnectionMonitorService;
        _deviceStatusService = deviceStatusService;
        _weldTaskService = weldTaskService;
        _exceptionLogService = exceptionLogService;
        _plcAlarmAddressService = plcAlarmAddressService;
        _settingsService = settingsService;
        Current = CreateSnapshot(ProductionConstants.Stations.DefaultStationNo, false, null, 0, null, 0, 0, DateTime.Now, string.Empty);
        _stationSnapshots[ProductionConstants.Stations.DefaultStationNo] = Current;
    }

    public event EventHandler<PlcProductionSnapshot>? StatusChanged;

    public PlcProductionSnapshot Current { get; private set; }

    public PlcProductionSnapshot GetCurrent(int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        lock (_snapshotSync)
        {
            return _stationSnapshots.TryGetValue(normalizedStationNo, out var snapshot)
                ? snapshot
                : CreateSnapshot(normalizedStationNo, false, null, 0, null, 0, 0, DateTime.Now, string.Empty);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_loopTask is { IsCompleted: false })
        {
            return;
        }

        await ReloadAddressesAsync(cancellationToken);
        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = Task.Run(() => RunAsync(_cts.Token), CancellationToken.None);
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

    public async Task ReloadAddressesAsync(CancellationToken cancellationToken = default)
    {
        var addresses = _plcAddressService.GetAll()
            .Where(it => it.Enabled && !string.IsNullOrWhiteSpace(it.Address))
            .ToList();

        await _addressSync.WaitAsync(cancellationToken);
        try
        {
            _addresses = addresses;
        }
        finally
        {
            _addressSync.Release();
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
        _addressSync.Dispose();
        _disposed = true;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(cancellationToken);
                await Task.Delay(PollInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                if (!IsPlcConnected(Current.StationNo))
                {
                    await Task.Delay(PollInterval, cancellationToken);
                    continue;
                }

                WriteBusinessFailureLog(Current.StationNo, ex.Message);
                Publish(Current with
                {
                    IsSuccess = false,
                    DeviceStatusCode = null,
                    UpdatedTime = DateTime.Now,
                    Message = ex.Message,
                    TotalProductionReadSuccess = false,
                    TotalProductionReadMessage = ex.Message,
                    AcceptedQuantityReadSuccess = false,
                    AcceptedQuantityReadMessage = ex.Message,
                    RejectedQuantityReadSuccess = false,
                    RejectedQuantityReadMessage = ex.Message,
                    AlarmMessage = string.Empty,
                    AlarmStationNo = null,
                    IsSoftwareAlarmActive = false,
                    SoftwareAlarmMessage = string.Empty
                });
                await Task.Delay(PollInterval, cancellationToken);
            }
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        var addresses = await GetAddressSnapshotAsync(cancellationToken);
        var settings = _settingsService.Get();
        var alarmReadingEnabled = settings.EnablePlcAlarmReading != false;
        if (!alarmReadingEnabled)
        {
            ClearAlarmReadFailureStates();
        }

        IReadOnlyList<BizPlcAlarmAddress> alarmAddresses = alarmReadingEnabled
            ? _plcAlarmAddressService.GetAll()
            : [];
        var stationNumbers = ResolveStationNumbers(addresses, alarmAddresses)
            .Concat(settings.EnableDualStation
                ? [ProductionConstants.Stations.DefaultStationNo, 2]
                : [ProductionConstants.Stations.DefaultStationNo])
            .Distinct()
            .OrderBy(stationNo => stationNo)
            .ToList();
        if (!stationNumbers.Any(IsPlcConnected))
        {
            PublishIdleForStations(stationNumbers);
            if (alarmReadingEnabled)
            {
                // PLC 全部离线时，仍需让已删除或禁用的配置闭合既有报警；保留仍配置的地址，避免离线误判恢复。
                await RecordDeviceAlarmCycleAsync(
                    settings.PlcAlarmTriggerMode,
                    stationNumbers.ToDictionary(stationNo => stationNo, _ => (short?)null),
                    [],
                    PlcDeviceAlarmCycleRules.ToConfiguredAlarms(alarmAddresses),
                    stationNumbers,
                    cancellationToken);
            }
            else
            {
                EnsureAlarmCycleState();
                ApplyEffectiveAlarmSnapshots(
                    stationNumbers,
                    new Dictionary<int, short?>(),
                    AppConstants.PlcAlarmTriggerModes.Normalize(settings.PlcAlarmTriggerMode));
            }
            return;
        }

        // 未连接、地址缺失或读取失败都保留为 null，双条件共享报警据此冻结而非误判恢复。
        var deviceStatuses = stationNumbers.ToDictionary(stationNo => stationNo, _ => (short?)null);
        var alarmReadResults = alarmReadingEnabled
            ? await ReadAlarmSignalsAsync(alarmAddresses, cancellationToken)
            : [];
        if (alarmReadingEnabled)
        {
            foreach (var stationNo in stationNumbers)
            {
                var failures = alarmReadResults
                    .Where(result => result.StationNo <= ProductionConstants.Stations.SharedStationNo
                        || result.StationNo == stationNo)
                    .Where(result => !result.IsSuccess)
                    .Select(result => new PlcAlarmReadFailure(result.Address.Trim(), result.FailureMessage.Trim()))
                    .ToList();
                UpdateAlarmReadFailureLog(stationNo, failures);
            }
        }

        foreach (var stationNo in stationNumbers)
        {
            if (!IsPlcConnected(stationNo))
            {
                PublishIdleForStations([stationNo]);
                continue;
            }

            var activeAlarm = alarmReadingEnabled
                ? PlcSoftwareAlarmRules.AggregateAlarmSignals(
                    stationNo,
                    alarmReadResults.Where(result =>
                        result.StationNo <= ProductionConstants.Stations.SharedStationNo
                        || result.StationNo == stationNo))
                : PlcAlarmSignalAggregation.Empty(stationNo);
            var deviceStatusAddress = GetAddress(addresses, AppConstants.PlcLogicalKeys.DeviceStatus, stationNo);
            if (deviceStatusAddress is null)
            {
                deviceStatuses[stationNo] = null;
                PublishFailureForStations([stationNo], "设备状态地址未配置。");
                continue;
            }

            var statusResult = await _plcCommunicationService.ReadInt16Async(deviceStatusAddress.Address!, cancellationToken);
            if (!statusResult.IsSuccess)
            {
                deviceStatuses[stationNo] = null;
                PublishFailureForStations([stationNo], statusResult.Message);
                continue;
            }

            var plcStatusCode = statusResult.Value;
            deviceStatuses[stationNo] = plcStatusCode;
            var statusMessage = BuildDeviceStatusValidationMessage(plcStatusCode);
            var externalAlarm = BuildExternalAlarmSnapshot(plcStatusCode, activeAlarm, stationNo);

            if (!ProductionConstants.PlcDeviceStatuses.IsReportable(plcStatusCode)
                && !string.IsNullOrWhiteSpace(statusMessage))
            {
                WriteBusinessFailureLog(stationNo, statusMessage);
            }

            var total = await ReadRequiredIntegerAsync(addresses, AppConstants.PlcLogicalKeys.TotalProduction, stationNo, cancellationToken);
            var accepted = await ReadRequiredIntegerAsync(addresses, AppConstants.PlcLogicalKeys.AcceptedQuantity, stationNo, cancellationToken);
            var rejected = await ReadRequiredIntegerAsync(addresses, AppConstants.PlcLogicalKeys.RejectedQuantity, stationNo, cancellationToken);
            var quantityMessage = BuildQuantityReadMessage(total, accepted, rejected);
            var snapshotMessage = BuildSnapshotMessage(statusMessage, quantityMessage);
            var isSnapshotSuccess = string.IsNullOrWhiteSpace(snapshotMessage);

            if (!string.IsNullOrWhiteSpace(quantityMessage))
            {
                WriteBusinessFailureLog(stationNo, quantityMessage);
            }

            if (!IsPlcConnected(stationNo))
            {
                PublishIdleForStations([stationNo]);
                continue;
            }

            Publish(CreateSnapshot(
                stationNo,
                isSnapshotSuccess,
                plcStatusCode,
                total.Value,
                null,
                accepted.Value,
                rejected.Value,
                DateTime.Now,
                snapshotMessage,
                total.IsSuccess,
                total.Message,
                accepted.IsSuccess,
                accepted.Message,
                rejected.IsSuccess,
                rejected.Message,
                externalAlarm.Message,
                plcStatusCode == ProductionConstants.PlcDeviceStatuses.Alarm
                    ? externalAlarm.ScopeStationNo
                    : null,
                false,
                string.Empty));
        }

        if (alarmReadingEnabled)
        {
            await RecordDeviceAlarmCycleAsync(
                settings.PlcAlarmTriggerMode,
                deviceStatuses,
                alarmReadResults,
                PlcDeviceAlarmCycleRules.ToConfiguredAlarms(alarmAddresses),
                stationNumbers,
                cancellationToken);
        }
        else
        {
            EnsureAlarmCycleState();
            ApplyEffectiveAlarmSnapshots(
                stationNumbers,
                deviceStatuses,
                AppConstants.PlcAlarmTriggerModes.Normalize(settings.PlcAlarmTriggerMode));
        }
    }

    private void ApplyEffectiveAlarmSnapshots(
        IReadOnlyList<int> stationNumbers,
        IReadOnlyDictionary<int, short?> deviceStatuses,
        string triggerMode)
    {
        var activeAlarms = (_effectiveAlarmState ?? _alarmCycleState)?.ActiveAlarms ?? [];
        var sharedAlarms = activeAlarms
            .Where(alarm => alarm.StationNo <= ProductionConstants.Stations.SharedStationNo)
            .ToList();
        foreach (var stationNo in stationNumbers)
        {
            var stationAlarms = activeAlarms
                .Where(alarm => alarm.StationNo == stationNo)
                .Concat(sharedAlarms)
                .DistinctBy(PlcDeviceAlarmCycleRules.GetAlarmKey, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var message = string.Join(
                "；",
                stationAlarms
                    .Select(alarm => alarm.AlarmContent.Trim())
                    .Where(content => !string.IsNullOrWhiteSpace(content))
                    .Distinct(StringComparer.OrdinalIgnoreCase));
            var pendingConfirmation = triggerMode == AppConstants.PlcAlarmTriggerModes.DeviceStatusAndAddress
                && deviceStatuses.GetValueOrDefault(stationNo) == ProductionConstants.PlcDeviceStatuses.Alarm
                && stationAlarms.Count == 0;
            var rawAlarmUnconfirmed = triggerMode == AppConstants.PlcAlarmTriggerModes.AddressOnly
                && deviceStatuses.GetValueOrDefault(stationNo) == ProductionConstants.PlcDeviceStatuses.Alarm
                && stationAlarms.Count == 0;
            var current = GetCurrent(stationNo);
            Publish(current with
            {
                IsSoftwareAlarmActive = stationAlarms.Count > 0,
                SoftwareAlarmMessage = message,
                IsAlarmPendingConfirmation = pendingConfirmation,
                IsRawAlarmUnconfirmed = rawAlarmUnconfirmed
            });
        }
    }

    private async Task<IReadOnlyList<BizPlcAddress>> GetAddressSnapshotAsync(CancellationToken cancellationToken)
    {
        await _addressSync.WaitAsync(cancellationToken);
        try
        {
            return _addresses.ToList();
        }
        finally
        {
            _addressSync.Release();
        }
    }

    private async Task<PlcQuantityReadResult> ReadRequiredIntegerAsync(
        IReadOnlyList<BizPlcAddress> addresses,
        string key,
        int stationNo,
        CancellationToken cancellationToken)
    {
        var address = GetAddress(addresses, key, stationNo);
        if (address is null)
        {
            return PlcQuantityReadResult.Fail($"{GetQuantityName(key)}地址未配置。");
        }

        var plcAddress = address.Address?.Trim();
        if (string.IsNullOrWhiteSpace(plcAddress))
        {
            return PlcQuantityReadResult.Fail($"{GetQuantityName(key)}PLC地址为空。");
        }

        var dataType = NormalizeDataType(address.DataType);
        return dataType switch
        {
            AppConstants.PlcDataTypes.Bool => ToQuantityReadResult(
                key,
                plcAddress,
                await _plcCommunicationService.ReadBoolAsync(plcAddress, cancellationToken)),
            AppConstants.PlcDataTypes.Int32 => ToQuantityReadResult(
                key,
                plcAddress,
                await _plcCommunicationService.ReadInt32Async(plcAddress, cancellationToken)),
            AppConstants.PlcDataTypes.Float => ToQuantityReadResult(
                key,
                plcAddress,
                await _plcCommunicationService.ReadFloatAsync(plcAddress, cancellationToken)),
            AppConstants.PlcDataTypes.String => ToQuantityReadResult(
                key,
                plcAddress,
                await _plcCommunicationService.ReadStringAsync(
                    plcAddress,
                    (ushort)Math.Max(1, address.DataLength),
                    cancellationToken)),
            _ => ToQuantityReadResult(
                key,
                plcAddress,
                await _plcCommunicationService.ReadInt16Async(plcAddress, cancellationToken))
        };
    }

    private bool IsPlcConnected(int stationNo)
    {
        return _plcCommunicationService.GetCurrent(stationNo).IsConnected;
    }

    private async Task RecordDeviceAlarmCycleAsync(
        string? triggerMode,
        IReadOnlyDictionary<int, short?> deviceStatuses,
        IReadOnlyList<PlcAlarmSignalReadResult> readResults,
        IReadOnlyList<PlcActiveAlarm> configuredAlarms,
        IReadOnlyList<int> stationNumbers,
        CancellationToken cancellationToken)
    {
        ClearSourceRemovedAlarmSuppressions(triggerMode, deviceStatuses, readResults, configuredAlarms);
        var state = EnsureAlarmCycleState();
        var decision = PlcDeviceAlarmCycleRules.Decide(
            state,
            triggerMode,
            deviceStatuses,
            readResults,
            configuredAlarms);
        _effectiveAlarmState = decision.NextState;
        ApplyEffectiveAlarmSnapshots(
            stationNumbers,
            deviceStatuses,
            AppConstants.PlcAlarmTriggerModes.Normalize(triggerMode));
        var persistedAlarms = state.ActiveAlarms.ToDictionary(
            PlcDeviceAlarmCycleRules.GetAlarmKey,
            StringComparer.OrdinalIgnoreCase);
        var sourceActiveAlarmKeys = decision.RecoveredAlarms.Count > 0
            ? GetSourceActiveAlarmKeys()
            : null;
        var hasRecordedRecovery = false;
        var hasRecordedNewException = false;
        var nextOccurredTime = DateTime.Now;

        foreach (var alarm in decision.RecoveredAlarms)
        {
            var alarmKey = PlcDeviceAlarmCycleRules.GetAlarmKey(alarm);
            if (sourceActiveAlarmKeys is not null && !sourceActiveAlarmKeys.Contains(alarmKey))
            {
                // JSONL 已删除时不重建状态 5；等 PLC 明确归零后才允许同地址进入下一周期。
                persistedAlarms.Remove(alarmKey);
                _sourceRemovedAlarmKeysAwaitingClear.Add(alarmKey);
                nextOccurredTime = nextOccurredTime.AddMilliseconds(1);
                continue;
            }

            if (await RecordDeviceStatusChangeAsync(
                    alarm.StationNo,
                    ProductionConstants.MesDeviceStatuses.Recovered,
                    alarm,
                    nextOccurredTime,
                    cancellationToken))
            {
                persistedAlarms.Remove(alarmKey);
                hasRecordedRecovery = true;
            }
            nextOccurredTime = nextOccurredTime.AddMilliseconds(1);
        }

        foreach (var alarm in decision.NewAlarms)
        {
            var alarmKey = PlcDeviceAlarmCycleRules.GetAlarmKey(alarm);
            if (_sourceRemovedAlarmKeysAwaitingClear.Contains(alarmKey))
            {
                nextOccurredTime = nextOccurredTime.AddMilliseconds(1);
                continue;
            }

            if (await RecordDeviceStatusChangeAsync(
                    alarm.StationNo,
                    ProductionConstants.MesDeviceStatuses.Exception,
                    alarm,
                    nextOccurredTime,
                    cancellationToken))
            {
                persistedAlarms[alarmKey] = alarm;
                hasRecordedNewException = true;
            }
            nextOccurredTime = nextOccurredTime.AddMilliseconds(1);
        }

        if (decision.ShouldReassertException && hasRecordedRecovery)
        {
            var remainingAlarm = decision.NextState.ActiveAlarms.First();
            var remainingAlarmKey = PlcDeviceAlarmCycleRules.GetAlarmKey(remainingAlarm);
            if (sourceActiveAlarmKeys?.Contains(remainingAlarmKey) == true)
            {
                _pendingExceptionReassertion = remainingAlarm;
            }
        }

        if (hasRecordedNewException)
        {
            _pendingExceptionReassertion = null;
        }
        else
        {
            await RetryPendingExceptionReassertionAsync(
                decision.NextState,
                sourceActiveAlarmKeys,
                nextOccurredTime,
                cancellationToken);
        }

        _alarmCycleState = new PlcDeviceAlarmCycleState(persistedAlarms.Values);
    }

    /// <summary>
    /// 部分恢复后的状态 4 必须在状态 5 后成功落盘；首次写入失败时下轮继续尝试，避免 MES 最终停在恢复状态。
    /// </summary>
    private async Task RetryPendingExceptionReassertionAsync(
        PlcDeviceAlarmCycleState effectiveState,
        HashSet<string>? sourceActiveAlarmKeys,
        DateTime occurredTime,
        CancellationToken cancellationToken)
    {
        if (_pendingExceptionReassertion is not { } alarm)
        {
            return;
        }

        var alarmKey = PlcDeviceAlarmCycleRules.GetAlarmKey(alarm);
        if (!effectiveState.ActiveAlarms.Any(activeAlarm =>
                string.Equals(
                    PlcDeviceAlarmCycleRules.GetAlarmKey(activeAlarm),
                    alarmKey,
                    StringComparison.OrdinalIgnoreCase)))
        {
            _pendingExceptionReassertion = null;
            return;
        }

        var activeSourceKeys = sourceActiveAlarmKeys ?? GetSourceActiveAlarmKeys();
        if (!activeSourceKeys.Contains(alarmKey))
        {
            _pendingExceptionReassertion = null;
            return;
        }

        if (await RecordDeviceStatusChangeAsync(
                alarm.StationNo,
                ProductionConstants.MesDeviceStatuses.Exception,
                alarm,
                occurredTime,
                cancellationToken))
        {
            _pendingExceptionReassertion = null;
        }
    }

    /// <summary>
    /// JSONL 被人工删除不是 PLC 状态变化；仅在成功读到清除后，才允许相同地址重新开启一个新周期。
    /// </summary>
    private void ClearSourceRemovedAlarmSuppressions(
        string? triggerMode,
        IReadOnlyDictionary<int, short?> deviceStatuses,
        IReadOnlyList<PlcAlarmSignalReadResult> readResults,
        IReadOnlyList<PlcActiveAlarm> configuredAlarms)
    {
        foreach (var alarmKey in _sourceRemovedAlarmKeysAwaitingClear.ToList())
        {
            var configuredAlarm = configuredAlarms.FirstOrDefault(alarm =>
                string.Equals(PlcDeviceAlarmCycleRules.GetAlarmKey(alarm), alarmKey, StringComparison.OrdinalIgnoreCase));
            var readResult = readResults.LastOrDefault(result =>
                string.Equals(
                    PlcDeviceAlarmCycleRules.GetAlarmKey(result.StationNo, result.Address),
                    alarmKey,
                    StringComparison.OrdinalIgnoreCase));
            if (configuredAlarm is null || readResult is null || !readResult.IsSuccess)
            {
                continue;
            }

            var clearDecision = PlcDeviceAlarmCycleRules.Decide(
                new PlcDeviceAlarmCycleState([configuredAlarm]),
                triggerMode,
                deviceStatuses,
                [readResult],
                [configuredAlarm]);
            if (clearDecision.RecoveredAlarms.Count > 0)
            {
                _sourceRemovedAlarmKeysAwaitingClear.Remove(alarmKey);
            }
        }
    }

    private HashSet<string> GetSourceActiveAlarmKeys()
        => PlcDeviceAlarmCycleRules.Restore(
                _deviceStatusService.GetLogs(from: null, to: null, maxCount: int.MaxValue))
            .ActiveAlarms
            .Select(PlcDeviceAlarmCycleRules.GetAlarmKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private PlcDeviceAlarmCycleState EnsureAlarmCycleState()
    {
        if (_alarmCycleState is not null)
        {
            return _alarmCycleState;
        }

        _alarmCycleState = PlcDeviceAlarmCycleRules.Restore(
            _deviceStatusService.GetLogs(from: null, to: null, maxCount: int.MaxValue));
        _effectiveAlarmState ??= _alarmCycleState;
        return _alarmCycleState;
    }

    private async Task<bool> RecordDeviceStatusChangeAsync(
        int stationNo,
        string mesStatusCode,
        PlcActiveAlarm? alarm,
        DateTime occurredTime,
        CancellationToken cancellationToken)
    {
        var normalizedStationNo = stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.SharedStationNo
            : NormalizeStationNo(stationNo);
        var taskStationNo = normalizedStationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : normalizedStationNo;
        var activeTask = normalizedStationNo <= ProductionConstants.Stations.SharedStationNo
            ? null
            : _weldTaskService.CurrentState.StationStates.TryGetValue(taskStationNo, out var stationState)
                ? stationState.ActiveTask
                : _weldTaskService.CurrentState.ActiveTask;

        var log = await _deviceStatusService.ChangeStatusAsync(
            mesStatusCode,
            BuildDeviceStatusRemark(normalizedStationNo, mesStatusCode, alarm),
            $"PLC-S{normalizedStationNo}",
            reportToMes: _mesConnectionMonitorService.Current.IsConnected,
            stationNo: normalizedStationNo,
            weldTaskId: activeTask?.Id,
            workOrderId: activeTask?.SN,
            occurredTime: occurredTime,
            forceWrite: mesStatusCode == ProductionConstants.MesDeviceStatuses.Exception
                && _alarmCycleState?.ActiveAlarms.Any(activeAlarm =>
                    string.Equals(activeAlarm.Address, alarm?.Address, StringComparison.OrdinalIgnoreCase)
                    && activeAlarm.StationNo == alarm?.StationNo) == true,
            alarmAddress: alarm?.Address,
            alarmContent: alarm?.AlarmContent,
            cancellationToken: cancellationToken);
        var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(log);
        return recordKey is not null && _deviceStatusService.GetLog(recordKey) is not null;
    }

    private async Task<IReadOnlyList<PlcAlarmSignalReadResult>> ReadAlarmSignalsAsync(
        IEnumerable<BizPlcAlarmAddress> alarmAddresses,
        CancellationToken cancellationToken)
    {
        var readResults = new List<PlcAlarmSignalReadResult>();

        var configuredAlarms = alarmAddresses
            .Where(alarm => alarm.Enabled && !string.IsNullOrWhiteSpace(alarm.Address))
            .DistinctBy(
                alarm => PlcDeviceAlarmCycleRules.GetAlarmKey(alarm.StationNo, alarm.Address),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(alarm => alarm.Sort)
            .ThenBy(alarm => alarm.Id)
            .ToList();
        foreach (var addressGroup in configuredAlarms.GroupBy(
                     alarm => AlarmAddressImportRules.NormalizeAddress(alarm.Address),
                     StringComparer.OrdinalIgnoreCase))
        {
            var plcAddress = addressGroup.Key;
            var readResult = await _plcCommunicationService.ReadBoolAsync(plcAddress, cancellationToken);
            foreach (var alarm in addressGroup)
            {
                readResults.Add(new PlcAlarmSignalReadResult(
                    alarm.StationNo,
                    plcAddress,
                    alarm.AlarmContent,
                    readResult.IsSuccess,
                    readResult.IsSuccess && readResult.Value,
                    readResult.Message));
            }
        }

        return readResults;
    }

    private void UpdateAlarmReadFailureLog(int stationNo, IReadOnlyList<PlcAlarmReadFailure> failures)
    {
        if (failures.Count > 0)
        {
            WriteAlarmReadFailureLog(stationNo, failures);
        }
        else
        {
            ClearAlarmReadFailureState(stationNo);
        }
    }

    private static string BuildDeviceStatusRemark(
        int stationNo,
        string mesStatusCode,
        PlcActiveAlarm? alarm)
    {
        if (string.Equals(mesStatusCode, ProductionConstants.MesDeviceStatuses.Exception, StringComparison.Ordinal)
            && alarm is not null)
        {
            return DeviceStatusReportRules.FormatExceptionRemark(
                alarm.AlarmContent,
                stationNo,
                alarm.StationNo <= ProductionConstants.Stations.SharedStationNo);
        }

        if (string.Equals(mesStatusCode, ProductionConstants.MesDeviceStatuses.Recovered, StringComparison.Ordinal)
            && alarm is not null)
        {
            return DeviceStatusReportRules.FormatRecoveryRemark(
                alarm.AlarmContent,
                stationNo,
                alarm.StationNo <= ProductionConstants.Stations.SharedStationNo);
        }

        return DeviceStatusReportRules.AppendStationRemark(
            DeviceStatusReportRules.GetStatusName(mesStatusCode),
            stationNo);
    }

    private static int? ToInteger(PlcServiceResult<bool> result)
    {
        return result.IsSuccess ? result.Value ? 1 : 0 : null;
    }

    private static int? ToInteger(PlcServiceResult<short> result)
    {
        return result.IsSuccess ? result.Value : null;
    }

    private static int? ToInteger(PlcServiceResult<int> result)
    {
        return result.IsSuccess ? result.Value : null;
    }

    private static int? ToInteger(PlcServiceResult<float> result)
    {
        return result.IsSuccess ? Convert.ToInt32(Math.Round(result.Value, MidpointRounding.AwayFromZero)) : null;
    }

    private static int? ToInteger(PlcServiceResult<string> result)
    {
        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Value))
        {
            return null;
        }

        var text = result.Value.Trim().Trim('\0');
        if (int.TryParse(text, out var intValue))
        {
            return intValue;
        }

        return double.TryParse(text, out var doubleValue)
            ? Convert.ToInt32(Math.Round(doubleValue, MidpointRounding.AwayFromZero))
            : null;
    }

    private static PlcQuantityReadResult ToQuantityReadResult(
        string key,
        string plcAddress,
        PlcServiceResult<bool> result)
    {
        return result.IsSuccess
            ? PlcQuantityReadResult.Success(result.Value ? 1 : 0)
            : PlcQuantityReadResult.Fail(BuildQuantityReadFailure(key, plcAddress, result.Message));
    }

    private static PlcQuantityReadResult ToQuantityReadResult(
        string key,
        string plcAddress,
        PlcServiceResult<short> result)
    {
        return result.IsSuccess
            ? PlcQuantityReadResult.Success(result.Value)
            : PlcQuantityReadResult.Fail(BuildQuantityReadFailure(key, plcAddress, result.Message));
    }

    private static PlcQuantityReadResult ToQuantityReadResult(
        string key,
        string plcAddress,
        PlcServiceResult<int> result)
    {
        return result.IsSuccess
            ? PlcQuantityReadResult.Success(result.Value)
            : PlcQuantityReadResult.Fail(BuildQuantityReadFailure(key, plcAddress, result.Message));
    }

    private static PlcQuantityReadResult ToQuantityReadResult(
        string key,
        string plcAddress,
        PlcServiceResult<float> result)
    {
        return result.IsSuccess
            ? PlcQuantityReadResult.Success(Convert.ToInt32(Math.Round(result.Value, MidpointRounding.AwayFromZero)))
            : PlcQuantityReadResult.Fail(BuildQuantityReadFailure(key, plcAddress, result.Message));
    }

    private static PlcQuantityReadResult ToQuantityReadResult(
        string key,
        string plcAddress,
        PlcServiceResult<string> result)
    {
        if (!result.IsSuccess)
        {
            return PlcQuantityReadResult.Fail(BuildQuantityReadFailure(key, plcAddress, result.Message));
        }

        var text = result.Value?.Trim().Trim('\0') ?? string.Empty;
        if (int.TryParse(text, out var intValue))
        {
            return PlcQuantityReadResult.Success(intValue);
        }

        if (double.TryParse(text, out var doubleValue))
        {
            return PlcQuantityReadResult.Success(Convert.ToInt32(Math.Round(doubleValue, MidpointRounding.AwayFromZero)));
        }

        return PlcQuantityReadResult.Fail($"{GetQuantityName(key)}地址“{plcAddress}”读取值无法转换为整数。");
    }

    private static string BuildQuantityReadFailure(string key, string plcAddress, string message)
    {
        var detail = string.IsNullOrWhiteSpace(message) ? "PLC未返回失败原因" : message.Trim();
        return $"{GetQuantityName(key)}地址“{plcAddress}”读取失败：{detail}";
    }

    private static string BuildQuantityReadMessage(params PlcQuantityReadResult[] results)
    {
        return string.Join("；", results
            .Where(result => !result.IsSuccess && !string.IsNullOrWhiteSpace(result.Message))
            .Select(result => result.Message)
            .Distinct());
    }

    /// <summary>
    /// Builds a local-only warning for PLC statuses that cannot be recorded or uploaded.
    /// </summary>
    private static string BuildDeviceStatusValidationMessage(short plcStatusCode)
    {
        if (plcStatusCode == ProductionConstants.PlcDeviceStatuses.Unknown
            || ProductionConstants.PlcDeviceStatuses.IsReportable(plcStatusCode))
        {
            return string.Empty;
        }

        return $"PLC设备状态值无效：{plcStatusCode}。仅支持 0=未知（本地显示）、1=运行、2=暂停/空闲、3=停止、4=报警；已跳过设备状态上报。";
    }

    /// <summary>
    /// Merges status and quantity warnings into one snapshot message for the UI.
    /// </summary>
    private static string BuildSnapshotMessage(params string[] messages)
    {
        return string.Join("；", messages
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string GetQuantityName(string key)
    {
        return key switch
        {
            AppConstants.PlcLogicalKeys.TotalProduction => "实际数量",
            AppConstants.PlcLogicalKeys.AcceptedQuantity => "合格数量",
            AppConstants.PlcLogicalKeys.RejectedQuantity => "失效数量",
            _ => key
        };
    }

    private static BizPlcAddress? GetAddress(IReadOnlyList<BizPlcAddress> addresses, string logicalKey, int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);

        return addresses
            .Where(address => string.Equals(address.LogicalKey, logicalKey, StringComparison.OrdinalIgnoreCase))
            .Where(address => address.StationNo == normalizedStationNo)
            .OrderBy(address => address.Sort)
            .FirstOrDefault();
    }

    private static string NormalizeDataType(string? dataType)
    {
        return AppConstants.PlcDataTypes.All.Contains(dataType)
            ? dataType!
            : AppConstants.PlcDataTypes.Int16;
    }

    private void Publish(PlcProductionSnapshot snapshot)
    {
        lock (_snapshotSync)
        {
            _stationSnapshots[snapshot.StationNo] = snapshot;
            if (snapshot.StationNo == ProductionConstants.Stations.DefaultStationNo)
            {
                Current = snapshot;
            }
        }

        StatusChanged?.Invoke(this, snapshot);
    }

    /// <summary>
    /// PLC 生产数据采集失败属于可预见业务异常，详细原因写入异常日志供日志管理页面查看。
    /// </summary>
    private void WriteBusinessFailureLog(int stationNo, string detail)
    {
        var summary = _localizer.GetString(TextKeys.Monitor.RuntimeError.ProductionCollectFailed);
        if (!ShouldWriteBusinessLog(stationNo, summary, detail))
        {
            return;
        }

        _exceptionLogService.WriteBusiness(
            "PLC.ProductionMonitor",
            summary,
            detail,
            $"读取设备状态、加工总数、合格数量或不良数量失败。Station={stationNo}");
    }

    private static AlarmReadSnapshot BuildExternalAlarmSnapshot(
        short plcStatusCode,
        PlcAlarmSignalAggregation activeAlarm,
        int stationNo)
    {
        if (plcStatusCode != ProductionConstants.PlcDeviceStatuses.Alarm)
        {
            return AlarmReadSnapshot.Empty(stationNo);
        }

        return new AlarmReadSnapshot(
            string.IsNullOrWhiteSpace(activeAlarm.Message)
                ? PlcSoftwareAlarmRules.GenericAlarmMessage
                : activeAlarm.Message.Trim(),
            activeAlarm.ScopeStationNo);
    }

    private void WriteAlarmReadFailureLog(int stationNo, IReadOnlyList<PlcAlarmReadFailure> failures)
    {
        var details = failures
            .OrderBy(failure => failure.Address, StringComparer.OrdinalIgnoreCase)
            .ThenBy(failure => failure.Message, StringComparer.OrdinalIgnoreCase)
            .Select(failure => $"报警地址“{failure.Address}”读取失败：{failure.Message}")
            .ToList();
        if (details.Count == 0)
        {
            ClearAlarmReadFailureState(stationNo);
            return;
        }

        var fingerprint = string.Join("\n", details);
        lock (_businessLogSync)
        {
            if (string.Equals(
                _activeAlarmFailureKeys.GetValueOrDefault(stationNo),
                fingerprint,
                StringComparison.Ordinal))
            {
                return;
            }

            _activeAlarmFailureKeys[stationNo] = fingerprint;
        }

        var summary = _localizer.GetString(TextKeys.Monitor.RuntimeError.PlcAlarmReadFailed);
        _exceptionLogService.WriteBusiness(
            "PLC.ProductionMonitor.AlarmRead",
            summary,
            string.Join(Environment.NewLine, details),
            $"{summary} Station={stationNo}");
    }

    private void ClearAlarmReadFailureState(int stationNo)
    {
        lock (_businessLogSync)
        {
            _activeAlarmFailureKeys.Remove(stationNo);
        }
    }

    private void ClearAlarmReadFailureStates()
    {
        lock (_businessLogSync)
        {
            _activeAlarmFailureKeys.Clear();
        }
    }

    private bool ShouldWriteBusinessLog(int stationNo, string summary, string detail)
    {
        var key = $"{stationNo}|{summary}|{detail}";
        lock (_businessLogSync)
        {
            var now = DateTime.Now;
            if (string.Equals(_lastBusinessLogKey, key, StringComparison.Ordinal)
                && now - _lastBusinessLogTime < BusinessLogInterval)
            {
                return false;
            }

            _lastBusinessLogKey = key;
            _lastBusinessLogTime = now;
            return true;
        }
    }

    private void PublishFailureForStations(
        IReadOnlyList<int> stationNumbers,
        string message,
        PlcSoftwareAlarmState? softwareAlarm = null)
    {
        var resolvedSoftwareAlarm = softwareAlarm ?? PlcSoftwareAlarmState.Inactive;
        foreach (var stationNo in stationNumbers)
        {
            WriteBusinessFailureLog(stationNo, message);
            var current = GetCurrent(stationNo);
            Publish(current with
            {
                IsSuccess = false,
                DeviceStatusCode = null,
                UpdatedTime = DateTime.Now,
                Message = message,
                TotalProductionReadSuccess = false,
                TotalProductionReadMessage = message,
                AcceptedQuantityReadSuccess = false,
                AcceptedQuantityReadMessage = message,
                RejectedQuantityReadSuccess = false,
                RejectedQuantityReadMessage = message,
                AlarmMessage = string.Empty,
                AlarmStationNo = null,
                IsSoftwareAlarmActive = resolvedSoftwareAlarm.IsActive,
                SoftwareAlarmMessage = resolvedSoftwareAlarm.Message
            });
        }
    }

    private void PublishIdleForStations(IReadOnlyList<int> stationNumbers)
    {
        foreach (var stationNo in stationNumbers)
        {
            var current = GetCurrent(stationNo);
            if (!current.IsSuccess && string.IsNullOrWhiteSpace(current.Message))
            {
                continue;
            }

            Publish(current with
            {
                IsSuccess = false,
                DeviceStatusCode = null,
                UpdatedTime = DateTime.Now,
                Message = string.Empty,
                AlarmMessage = string.Empty,
                AlarmStationNo = null,
                IsSoftwareAlarmActive = false,
                SoftwareAlarmMessage = string.Empty,
                TotalProductionReadSuccess = false,
                TotalProductionReadMessage = "PLC未连接或生产监控未就绪。",
                AcceptedQuantityReadSuccess = false,
                AcceptedQuantityReadMessage = "PLC未连接或生产监控未就绪。",
                RejectedQuantityReadSuccess = false,
                RejectedQuantityReadMessage = "PLC未连接或生产监控未就绪。"
            });
        }
    }

    private static IReadOnlyList<int> ResolveStationNumbers(
        IReadOnlyList<BizPlcAddress> addresses,
        IReadOnlyList<BizPlcAlarmAddress> alarmAddresses)
    {
        var productionStationNumbers = addresses
            .Where(address => IsStationProductionKey(address.LogicalKey))
            .Select(address => NormalizeStationNo(address.StationNo))
            .ToList();

        return PlcSoftwareAlarmRules.ResolveStationNumbers(
            productionStationNumbers,
            alarmAddresses);
    }

    private static bool IsStationProductionKey(string logicalKey)
    {
        return logicalKey is AppConstants.PlcLogicalKeys.DeviceStatus
            or AppConstants.PlcLogicalKeys.TotalProduction
            or AppConstants.PlcLogicalKeys.AcceptedQuantity
            or AppConstants.PlcLogicalKeys.RejectedQuantity;
    }

    private static PlcProductionSnapshot CreateSnapshot(
        int stationNo,
        bool isSuccess,
        short? deviceStatusCode,
        int totalProduction,
        int? targetProduction,
        int acceptedQuantity,
        int rejectedQuantity,
        DateTime updatedTime,
        string message,
        bool totalProductionReadSuccess = false,
        string totalProductionReadMessage = "",
        bool acceptedQuantityReadSuccess = false,
        string acceptedQuantityReadMessage = "",
        bool rejectedQuantityReadSuccess = false,
        string rejectedQuantityReadMessage = "",
        string alarmMessage = "",
        int? alarmStationNo = null,
        bool isSoftwareAlarmActive = false,
        string softwareAlarmMessage = "")
    {
        return new PlcProductionSnapshot(
            isSuccess,
            deviceStatusCode,
            totalProduction,
            targetProduction,
            acceptedQuantity,
            rejectedQuantity,
            updatedTime,
            message)
        {
            StationNo = NormalizeStationNo(stationNo),
            TotalProductionReadSuccess = totalProductionReadSuccess,
            TotalProductionReadMessage = totalProductionReadMessage,
            AcceptedQuantityReadSuccess = acceptedQuantityReadSuccess,
            AcceptedQuantityReadMessage = acceptedQuantityReadMessage,
            RejectedQuantityReadSuccess = rejectedQuantityReadSuccess,
            RejectedQuantityReadMessage = rejectedQuantityReadMessage,
            AlarmMessage = alarmMessage,
            AlarmStationNo = alarmStationNo,
            IsSoftwareAlarmActive = isSoftwareAlarmActive,
            SoftwareAlarmMessage = softwareAlarmMessage
        };
    }

    private sealed record AlarmReadSnapshot(string Message, int ScopeStationNo)
    {
        public static AlarmReadSnapshot Empty(int stationNo)
            => new(string.Empty, NormalizeStationNo(stationNo));

    }

    private sealed record PlcQuantityReadResult(bool IsSuccess, int Value, string Message)
    {
        public static PlcQuantityReadResult Success(int value) => new(true, value, string.Empty);

        public static PlcQuantityReadResult Fail(string message) => new(false, 0, message);
    }

    private static int NormalizeStationNo(int stationNo)
        => stationNo <= ProductionConstants.Stations.SharedStationNo ? ProductionConstants.Stations.DefaultStationNo : stationNo;
}
