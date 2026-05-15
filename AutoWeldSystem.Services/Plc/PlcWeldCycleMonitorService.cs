using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// PLC 焊接周期监控服务实现。
/// 当前地址维护界面只有一组焊接开始/结束地址，因此先按默认工位运行；后续地址配置带工位后可扩展为多工位循环。
/// </summary>
public sealed class PlcWeldCycleMonitorService : IPlcWeldCycleMonitorService, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan BusinessLogInterval = TimeSpan.FromSeconds(30);

    private readonly IPlcAddressService _addressService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IWeldPointCollectionService _weldPointCollectionService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly IOperationLogService _operationLogService;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly object _businessLogSync = new();

    private BizPlcAddress? _startAddress;
    private BizPlcAddress? _endAddress;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _lastStartSignal;
    private bool _lastEndSignal;
    private bool _hasSignalSnapshot;
    private bool _cycleStarted;
    private string _lastBusinessLogKey = string.Empty;
    private DateTime _lastBusinessLogTime;
    private bool _disposed;

    public PlcWeldCycleMonitorService(
        IPlcAddressService addressService,
        IPlcCommunicationService plcCommunicationService,
        IWeldTaskService weldTaskService,
        IWeldPointCollectionService weldPointCollectionService,
        IProgramExceptionLogService exceptionLogService,
        IOperationLogService operationLogService)
    {
        _addressService = addressService;
        _plcCommunicationService = plcCommunicationService;
        _weldTaskService = weldTaskService;
        _weldPointCollectionService = weldPointCollectionService;
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
            // 停止监控不应影响程序退出。
        }
    }

    public async Task ReloadAddressesAsync(CancellationToken cancellationToken = default)
    {
        var startAddress = _addressService.GetByKey(AppConstants.PlcAddressKeys.WeldStart);
        var endAddress = _addressService.GetByKey(AppConstants.PlcAddressKeys.WeldEnd);

        await _sync.WaitAsync(cancellationToken);
        try
        {
            _startAddress = startAddress;
            _endAddress = endAddress;
            _hasSignalSnapshot = false;
            _cycleStarted = false;
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
        var task = GetActiveTask();
        if (task is null)
        {
            ResetCycleState();
            return;
        }

        var (startAddress, endAddress) = await GetAddressSnapshotAsync(cancellationToken);
        if (!IsUsable(startAddress) || !IsUsable(endAddress))
        {
            WriteBusinessFailureLog("焊接信号地址未配置", "请在地址维护中配置焊接开始和焊接结束 PLC 地址。");
            return;
        }

        var startSignal = await ReadBoolSignalAsync(startAddress!, "焊接开始信号", cancellationToken);
        var endSignal = await ReadBoolSignalAsync(endAddress!, "焊接结束信号", cancellationToken);

        if (!_hasSignalSnapshot)
        {
            _lastStartSignal = startSignal;
            _lastEndSignal = endSignal;
            _cycleStarted = startSignal;
            _hasSignalSnapshot = true;
            return;
        }

        var startRising = startSignal && !_lastStartSignal;
        var endRising = endSignal && !_lastEndSignal;

        if (startRising)
        {
            _cycleStarted = true;
            _operationLogService.Write("WeldCycle", $"Weld start detected, Station={task.StationNo}, WorkOrder={task.WorkOrderId}");
        }

        if (endRising)
        {
            await CollectWeldPointAsync(task, _cycleStarted, cancellationToken);
            _cycleStarted = false;
        }

        _lastStartSignal = startSignal;
        _lastEndSignal = endSignal;
    }

    private BizWeldTask? GetActiveTask()
    {
        var station = _weldTaskService.CurrentState.GetOrCreateStation(ProductionConstants.Stations.DefaultStationNo);
        return station.IsTaskRunning ? station.ActiveTask : null;
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

    private async Task<(BizPlcAddress? StartAddress, BizPlcAddress? EndAddress)> GetAddressSnapshotAsync(CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            return (_startAddress, _endAddress);
        }
        finally
        {
            _sync.Release();
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

    private static bool IsUsable(BizPlcAddress? address)
    {
        return address is { Enabled: true }
            && !string.IsNullOrWhiteSpace(address.Address);
    }

    private void ResetCycleState()
    {
        _hasSignalSnapshot = false;
        _cycleStarted = false;
        _lastStartSignal = false;
        _lastEndSignal = false;
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
}
