using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// Monitors PLC product-cycle signals and triggers one complete product data collection.
/// Each station keeps its own signal snapshot so dual-station equipment can run independently.
/// </summary>
public sealed class PlcWeldCycleMonitorService : IPlcWeldCycleMonitorService, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan BusinessLogInterval = TimeSpan.FromSeconds(30);

    private readonly IPlcAddressService _addressService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IProductCycleCollectionService _productCycleCollectionService;
    private readonly IWeldPointUploadCoordinatorService _weldPointUploadCoordinatorService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly IOperationLogService _operationLogService;
    private readonly IProductionFlowLogService _productionLogService;
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
        IProductCycleCollectionService productCycleCollectionService,
        IWeldPointUploadCoordinatorService weldPointUploadCoordinatorService,
        IProgramExceptionLogService exceptionLogService,
        IOperationLogService operationLogService,
        IProductionFlowLogService productionLogService)
    {
        _addressService = addressService;
        _plcCommunicationService = plcCommunicationService;
        _weldTaskService = weldTaskService;
        _productCycleCollectionService = productCycleCollectionService;
        _weldPointUploadCoordinatorService = weldPointUploadCoordinatorService;
        _exceptionLogService = exceptionLogService;
        _operationLogService = operationLogService;
        _productionLogService = productionLogService;
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
                    ProductDataReadyAddress = FindAddress(addresses, AppConstants.PlcAddressKeys.ProductDataReady, stationNo),
                    ProductCollectionFeedbackAddress = FindAddress(addresses, AppConstants.PlcAddressKeys.ProductCollectionFeedback, stationNo)
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
                if (!IsPlcConnected())
                {
                    await Task.Delay(PollInterval, cancellationToken);
                    continue;
                }

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
        await PollProductCycleAsync(task, stationState, cancellationToken);
    }

    private async Task PollProductCycleAsync(BizWeldTask task, StationCycleState stationState, CancellationToken cancellationToken)
    {
        if (!IsUsable(stationState.ProductDataReadyAddress) || !IsUsable(stationState.ProductCollectionFeedbackAddress))
        {
            return;
        }

        if (!IsPlcConnected())
        {
            return;
        }

        short readyValue;
        try
        {
            readyValue = await ReadNumberSignalAsync(stationState.ProductDataReadyAddress!, $"工位{task.StationNo}产品数据就绪信号", cancellationToken);
        }
        catch (BusinessOperationException) when (!IsPlcConnected())
        {
            return;
        }

        var ready = readyValue == 1;
        if (!ready)
        {
            if (stationState.ProductDataReadyHandled || stationState.ProductFeedbackWritten)
            {
                WriteProductionLog(
                    "ProductDataReadyReset",
                    "PLC已清空产品数据就绪信号",
                    $"ReadyValue={readyValue}",
                    task,
                    plcSignal: AppConstants.PlcAddressKeys.ProductDataReady,
                    plcAddress: stationState.ProductDataReadyAddress?.Address);
                await WriteProductCollectionFeedbackAsync(stationState, 0, cancellationToken);
            }

            stationState.ProductDataReadyHandled = false;
            stationState.ProductFeedbackWritten = false;
            return;
        }

        if (stationState.ProductDataReadyHandled)
        {
            return;
        }

        WriteProductionLog(
            "ProductDataReady",
            "检测到产品数据就绪信号",
            $"ReadyValue={readyValue}, ReadyAddress={stationState.ProductDataReadyAddress?.Address}, FeedbackAddress={stationState.ProductCollectionFeedbackAddress?.Address}",
            task,
            plcSignal: AppConstants.PlcAddressKeys.ProductDataReady,
            plcAddress: stationState.ProductDataReadyAddress?.Address);
        await CollectProductCycleAsync(task, stationState, cancellationToken);
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
                ProductDataReadyAddress = _addressService.GetByKey(AppConstants.PlcAddressKeys.ProductDataReady, normalizedStationNo),
                ProductCollectionFeedbackAddress = _addressService.GetByKey(AppConstants.PlcAddressKeys.ProductCollectionFeedback, normalizedStationNo)
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

    private async Task CollectProductCycleAsync(BizWeldTask task, StationCycleState stationState, CancellationToken cancellationToken)
    {
        try
        {
            WriteProductionLog(
                "ProductCollectionStart",
                "开始读取整件产品数据",
                $"ProgramId={task.ProgramId}, ReadyAddress={stationState.ProductDataReadyAddress?.Address}",
                task,
                plcSignal: AppConstants.PlcAddressKeys.ProductDataReady,
                plcAddress: stationState.ProductDataReadyAddress?.Address);
            var records = await _productCycleCollectionService.CollectAsync(task, task.StationNo, cancellationToken);
            await WriteProductCollectionFeedbackAsync(stationState, 1, cancellationToken);
            stationState.ProductDataReadyHandled = true;
            stationState.ProductFeedbackWritten = true;
            WriteProductionLog(
                "ProductCollectionFeedback",
                "已反馈PLC采集成功",
                $"Feedback=1, Records={records.Count}, Address={stationState.ProductCollectionFeedbackAddress?.Address}",
                task,
                productNo: records.FirstOrDefault()?.ProductNo,
                plcSignal: AppConstants.PlcAddressKeys.ProductCollectionFeedback,
                plcAddress: stationState.ProductCollectionFeedbackAddress?.Address);

            foreach (var record in records)
            {
                WeldPointCollected?.Invoke(this, record);
                await _weldPointUploadCoordinatorService.HandleCollectedAsync(record, cancellationToken);
            }
        }
        catch (BusinessOperationException ex)
        {
            if (!IsPlcConnected())
            {
                return;
            }

            await WriteProductCollectionFeedbackAsync(stationState, 2, cancellationToken);
            stationState.ProductDataReadyHandled = true;
            stationState.ProductFeedbackWritten = true;
            WriteProductionLog(
                "ProductCollectionFeedback",
                "已反馈PLC采集失败",
                ex.Detail,
                task,
                level: "Error",
                plcSignal: AppConstants.PlcAddressKeys.ProductCollectionFeedback,
                plcAddress: stationState.ProductCollectionFeedbackAddress?.Address);
            WriteBusinessFailureLog(ex.Message, ex.Detail);
        }
        catch (Exception ex)
        {
            if (!IsPlcConnected())
            {
                return;
            }

            await WriteProductCollectionFeedbackAsync(stationState, 2, cancellationToken);
            stationState.ProductDataReadyHandled = true;
            stationState.ProductFeedbackWritten = true;
            WriteProductionLog(
                "ProductCollectionFeedback",
                "已反馈PLC采集失败",
                ex.Message,
                task,
                level: "Error",
                plcSignal: AppConstants.PlcAddressKeys.ProductCollectionFeedback,
                plcAddress: stationState.ProductCollectionFeedbackAddress?.Address);
            _exceptionLogService.Write(ex, "PLC.ProductCycleMonitor", $"Station={task.StationNo}, WorkOrder={task.WorkOrderId}");
        }
    }

    private async Task<short> ReadNumberSignalAsync(BizPlcAddress address, string signalName, CancellationToken cancellationToken)
    {
        var dataType = NormalizeDataType(address.DataType);
        if (dataType == AppConstants.PlcDataTypes.Bool)
        {
            var boolResult = await _plcCommunicationService.ReadBoolAsync(address.Address!, cancellationToken);
            if (boolResult.IsSuccess)
            {
                return boolResult.Value ? (short)1 : (short)0;
            }

            throw new BusinessOperationException("PLC.ProductCycleMonitor", $"{signalName}读取失败", boolResult.Message);
        }

        if (dataType == AppConstants.PlcDataTypes.Int32)
        {
            var intResult = await _plcCommunicationService.ReadInt32Async(address.Address!, cancellationToken);
            if (intResult.IsSuccess)
            {
                return (short)intResult.Value;
            }

            throw new BusinessOperationException("PLC.ProductCycleMonitor", $"{signalName}读取失败", intResult.Message);
        }

        var result = await _plcCommunicationService.ReadInt16Async(address.Address!, cancellationToken);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new BusinessOperationException("PLC.ProductCycleMonitor", $"{signalName}读取失败", result.Message);
    }

    private async Task WriteProductCollectionFeedbackAsync(StationCycleState stationState, short value, CancellationToken cancellationToken)
    {
        if (!IsUsable(stationState.ProductCollectionFeedbackAddress))
        {
            return;
        }

        if (!IsPlcConnected())
        {
            return;
        }

        var address = stationState.ProductCollectionFeedbackAddress!;
        var result = NormalizeDataType(address.DataType) switch
        {
            AppConstants.PlcDataTypes.Bool => await _plcCommunicationService.WriteBoolAsync(address.Address!, value > 0, cancellationToken),
            AppConstants.PlcDataTypes.Int32 => await _plcCommunicationService.WriteInt32Async(address.Address!, value, cancellationToken),
            AppConstants.PlcDataTypes.Float => await _plcCommunicationService.WriteFloatAsync(address.Address!, value, cancellationToken),
            _ => await _plcCommunicationService.WriteInt16Async(address.Address!, value, cancellationToken)
        };

        if (!result.IsSuccess)
        {
            if (!IsPlcConnected())
            {
                return;
            }

            WriteBusinessFailureLog($"工位{stationState.StationNo}产品采集反馈写入失败", result.Message);
        }
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
        return string.Equals(logicalKey, AppConstants.PlcAddressKeys.ProductDataReady, StringComparison.OrdinalIgnoreCase)
            || string.Equals(logicalKey, AppConstants.PlcAddressKeys.ProductCollectionFeedback, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDataType(string? dataType)
    {
        return AppConstants.PlcDataTypes.All.Contains(dataType)
            ? dataType!
            : AppConstants.PlcDataTypes.Int16;
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

    private bool IsPlcConnected()
    {
        return _plcCommunicationService.Current.IsConnected;
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
            "监控产品数据就绪信号并触发整件产品数据采集。");
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

    private void WriteProductionLog(
        string step,
        string summary,
        string detail,
        BizWeldTask task,
        string level = "Info",
        string? productNo = null,
        string? plcSignal = null,
        string? plcAddress = null)
    {
        _productionLogService.Write(
            step,
            summary,
            detail,
            level,
            task.StationNo,
            task.WorkOrderId,
            productNo ?? string.Empty,
            task.ProgramId ?? string.Empty,
            plcSignal ?? string.Empty,
            plcAddress ?? string.Empty);
    }

    private sealed class StationCycleState
    {
        public int StationNo { get; init; }

        public BizPlcAddress? ProductDataReadyAddress { get; init; }

        public BizPlcAddress? ProductCollectionFeedbackAddress { get; init; }

        public bool ProductDataReadyHandled { get; set; }

        public bool ProductFeedbackWritten { get; set; }

        public void ResetSignalSnapshot()
        {
            ProductDataReadyHandled = false;
            ProductFeedbackWritten = false;
        }
    }
}
