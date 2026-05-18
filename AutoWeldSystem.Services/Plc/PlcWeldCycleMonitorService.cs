using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// Monitors weld start/end PLC signals and triggers weld point collection.
/// Each station keeps its own signal snapshot so dual-station equipment can run independently.
/// </summary>
public sealed class PlcWeldCycleMonitorService : IPlcWeldCycleMonitorService, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan BusinessLogInterval = TimeSpan.FromSeconds(30);

    private readonly IPlcAddressService _addressService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IWeldPointCollectionService _weldPointCollectionService;
    private readonly IWeldPointUploadCoordinatorService _weldPointUploadCoordinatorService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly IOperationLogService _operationLogService;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly object _businessLogSync = new();
    private readonly Dictionary<int, StationCycleState> _stationStates = new();

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private string _lastBusinessLogKey = string.Empty;
    private DateTime _lastBusinessLogTime;
    private bool _disposed;

    public PlcWeldCycleMonitorService(
        IPlcAddressService addressService,
        IPlcCommunicationService plcCommunicationService,
        IWeldTaskService weldTaskService,
        IWeldPointCollectionService weldPointCollectionService,
        IWeldPointUploadCoordinatorService weldPointUploadCoordinatorService,
        IProgramExceptionLogService exceptionLogService,
        IOperationLogService operationLogService)
    {
        _addressService = addressService;
        _plcCommunicationService = plcCommunicationService;
        _weldTaskService = weldTaskService;
        _weldPointCollectionService = weldPointCollectionService;
        _weldPointUploadCoordinatorService = weldPointUploadCoordinatorService;
        _exceptionLogService = exceptionLogService;
        _operationLogService = operationLogService;
    }

    public event EventHandler<BizWeldPointRecord>? WeldPointCollected;

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
            // The monitor is background infrastructure; shutdown failures should not block application exit.
        }
    }

    public async Task ReloadAddressesAsync(CancellationToken cancellationToken = default)
    {
        var addresses = _addressService.GetAll();
        var stationNumbers = addresses
            .Where(IsWeldSignalAddress)
            .Select(address => address.StationNo)
            .Where(stationNo => stationNo > ProductionConstants.Stations.SharedStationNo)
            .DefaultIfEmpty(ProductionConstants.Stations.DefaultStationNo)
            .Distinct()
            .OrderBy(stationNo => stationNo)
            .ToList();

        await _sync.WaitAsync(cancellationToken);
        try
        {
            _stationStates.Clear();
            foreach (var stationNo in stationNumbers)
            {
                _stationStates[stationNo] = new StationCycleState
                {
                    StationNo = stationNo,
                    StartAddress = FindAddress(addresses, AppConstants.PlcAddressKeys.WeldStart, stationNo),
                    EndAddress = FindAddress(addresses, AppConstants.PlcAddressKeys.WeldEnd, stationNo)
                };
            }
        }
        finally
        {
            _sync.Release();
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
        _sync.Dispose();
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
                WriteBusinessFailureLog("焊接周期监控失败", ex.Message);
                await Task.Delay(PollInterval, cancellationToken);
            }
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        var activeTasks = GetActiveTasks();
        if (activeTasks.Count == 0)
        {
            await ResetAllCycleStatesAsync(cancellationToken);
            return;
        }

        foreach (var task in activeTasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stationState = await GetStationStateAsync(task.StationNo, cancellationToken);
            await PollStationAsync(task, stationState, cancellationToken);
        }
    }

    private async Task PollStationAsync(BizWeldTask task, StationCycleState stationState, CancellationToken cancellationToken)
    {
        if (!IsUsable(stationState.StartAddress) || !IsUsable(stationState.EndAddress))
        {
            WriteBusinessFailureLog(
                $"工位{task.StationNo}焊接信号地址未配置",
                "请在地址维护中配置该工位的焊接开始和焊接结束 PLC 地址。");
            return;
        }

        var startSignal = await ReadBoolSignalAsync(stationState.StartAddress!, $"工位{task.StationNo}焊接开始信号", cancellationToken);
        var endSignal = await ReadBoolSignalAsync(stationState.EndAddress!, $"工位{task.StationNo}焊接结束信号", cancellationToken);

        if (!stationState.HasSignalSnapshot)
        {
            stationState.LastStartSignal = startSignal;
            stationState.LastEndSignal = endSignal;
            stationState.CycleStarted = startSignal;
            stationState.HasSignalSnapshot = true;
            return;
        }

        var startRising = startSignal && !stationState.LastStartSignal;
        var endRising = endSignal && !stationState.LastEndSignal;

        if (startRising)
        {
            stationState.CycleStarted = true;
            _operationLogService.Write("WeldCycle", $"Weld start detected, Station={task.StationNo}, WorkOrder={task.WorkOrderId}");
        }

        if (endRising)
        {
            await CollectWeldPointAsync(task, stationState.CycleStarted, cancellationToken);
            stationState.CycleStarted = false;
        }

        stationState.LastStartSignal = startSignal;
        stationState.LastEndSignal = endSignal;
    }

    private IReadOnlyList<BizWeldTask> GetActiveTasks()
    {
        var stationTasks = _weldTaskService.CurrentState.StationStates.Values
            .Where(station => station.IsTaskRunning && station.ActiveTask is not null)
            .Select(station => station.ActiveTask!)
            .OrderBy(task => task.StationNo)
            .ToList();

        if (stationTasks.Count > 0)
        {
            return stationTasks;
        }

        return _weldTaskService.CurrentState.IsTaskRunning && _weldTaskService.CurrentState.ActiveTask is not null
            ? new[] { _weldTaskService.CurrentState.ActiveTask }
            : Array.Empty<BizWeldTask>();
    }

    private async Task<StationCycleState> GetStationStateAsync(int stationNo, CancellationToken cancellationToken)
    {
        var normalizedStationNo = stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;

        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_stationStates.TryGetValue(normalizedStationNo, out var stationState))
            {
                return stationState;
            }

            stationState = new StationCycleState
            {
                StationNo = normalizedStationNo,
                StartAddress = _addressService.GetByKey(AppConstants.PlcAddressKeys.WeldStart, normalizedStationNo),
                EndAddress = _addressService.GetByKey(AppConstants.PlcAddressKeys.WeldEnd, normalizedStationNo)
            };
            _stationStates[normalizedStationNo] = stationState;
            return stationState;
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task ResetAllCycleStatesAsync(CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            foreach (var stationState in _stationStates.Values)
            {
                stationState.ResetSignalSnapshot();
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task CollectWeldPointAsync(BizWeldTask task, bool hasStartSignal, CancellationToken cancellationToken)
    {
        try
        {
            if (!hasStartSignal)
            {
                _operationLogService.Write(
                    "WeldCycle",
                    $"Weld end detected without a start edge, collect anyway. Station={task.StationNo}, WorkOrder={task.WorkOrderId}");
            }

            var record = await _weldPointCollectionService.CollectAsync(task, task.StationNo, cancellationToken);
            WeldPointCollected?.Invoke(this, record);
            await _weldPointUploadCoordinatorService.HandleCollectedAsync(record, cancellationToken);
        }
        catch (BusinessOperationException ex)
        {
            WriteBusinessFailureLog(ex.Message, ex.Detail);
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "PLC.WeldCycleMonitor", $"Station={task.StationNo}, WorkOrder={task.WorkOrderId}");
        }
    }

    private async Task<bool> ReadBoolSignalAsync(BizPlcAddress address, string signalName, CancellationToken cancellationToken)
    {
        var result = await _plcCommunicationService.ReadBoolAsync(address.Address!, cancellationToken);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new BusinessOperationException(
            "PLC.WeldCycleMonitor",
            $"{signalName}读取失败",
            result.Message);
    }

    private static BizPlcAddress? FindAddress(IReadOnlyList<BizPlcAddress> addresses, string logicalKey, int stationNo)
    {
        return addresses
            .Where(address => string.Equals(GetLogicalKey(address), logicalKey, StringComparison.OrdinalIgnoreCase))
            .Where(address => address.StationNo == stationNo
                || address.StationNo == ProductionConstants.Stations.SharedStationNo)
            .OrderByDescending(address => address.StationNo == stationNo)
            .ThenByDescending(address => address.StationNo == ProductionConstants.Stations.SharedStationNo)
            .ThenBy(address => address.Sort)
            .FirstOrDefault();
    }

    private static bool IsWeldSignalAddress(BizPlcAddress address)
    {
        var logicalKey = GetLogicalKey(address);
        return string.Equals(logicalKey, AppConstants.PlcAddressKeys.WeldStart, StringComparison.OrdinalIgnoreCase)
            || string.Equals(logicalKey, AppConstants.PlcAddressKeys.WeldEnd, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetLogicalKey(BizPlcAddress address)
    {
        return string.IsNullOrWhiteSpace(address.LogicalKey)
            ? address.AddressKey
            : address.LogicalKey.Trim();
    }

    private static bool IsUsable(BizPlcAddress? address)
    {
        return address is { Enabled: true }
            && !string.IsNullOrWhiteSpace(address.Address);
    }

    private void WriteBusinessFailureLog(string summary, string detail)
    {
        if (!ShouldWriteBusinessLog(summary, detail))
        {
            return;
        }

        _exceptionLogService.WriteBusiness(
            "PLC.WeldCycleMonitor",
            summary,
            detail,
            "监控焊接开始/结束信号并触发焊点数据采集。");
    }

    private bool ShouldWriteBusinessLog(string summary, string detail)
    {
        var key = $"{summary}|{detail}";
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

    private sealed class StationCycleState
    {
        public int StationNo { get; init; }

        public BizPlcAddress? StartAddress { get; init; }

        public BizPlcAddress? EndAddress { get; init; }

        public bool LastStartSignal { get; set; }

        public bool LastEndSignal { get; set; }

        public bool HasSignalSnapshot { get; set; }

        public bool CycleStarted { get; set; }

        public void ResetSignalSnapshot()
        {
            LastStartSignal = false;
            LastEndSignal = false;
            HasSignalSnapshot = false;
            CycleStarted = false;
        }
    }
}
