using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Production;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// Monitors PLC product-cycle signals and triggers one complete product data collection.
/// Each station keeps its own signal snapshot so dual-station equipment can run independently.
/// </summary>
public sealed class WeldCycleMonitorService : IPlcWeldCycleMonitorService, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan BusinessLogInterval = TimeSpan.FromSeconds(30);

    private readonly IPlcAddressService _addressService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IProductCycleCollectionService _productCycleCollectionService;
    private readonly IWeldPointUploadCoordinatorService _weldPointUploadCoordinatorService;
    private readonly ICenterProductForwardingService _centerProductForwardingService;
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

    public WeldCycleMonitorService(
        IPlcAddressService addressService,
        IPlcCommunicationService plcCommunicationService,
        IWeldTaskService weldTaskService,
        IProductCycleCollectionService productCycleCollectionService,
        IWeldPointUploadCoordinatorService weldPointUploadCoordinatorService,
        ICenterProductForwardingService centerProductForwardingService,
        IProgramExceptionLogService exceptionLogService,
        IOperationLogService operationLogService,
        IProductionFlowLogService productionLogService)
    {
        _addressService = addressService;
        _plcCommunicationService = plcCommunicationService;
        _weldTaskService = weldTaskService;
        _productCycleCollectionService = productCycleCollectionService;
        _weldPointUploadCoordinatorService = weldPointUploadCoordinatorService;
        _centerProductForwardingService = centerProductForwardingService;
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
                    ProductDataReadyAddress = FindAddress(addresses, AppConstants.PlcLogicalKeys.ProductDataReady, stationNo),
                    ProductCollectionFeedbackAddress = FindAddress(addresses, AppConstants.PlcLogicalKeys.ProductCollectionFeedback, stationNo)
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
                if (!IsAnyPlcStationConnected())
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
            await PollIdleProductReadySignalsAsync(cancellationToken);
            return;
        }

        foreach (var activeTask in activeTasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stationState = await GetStationStateAsync(activeTask.StationNo, cancellationToken);
            await PollStationAsync(activeTask.Task, stationState, cancellationToken);
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

        if (!IsPlcConnected(stationState.StationNo))
        {
            return;
        }

        short readyValue;
        try
        {
            readyValue = await ReadNumberSignalAsync(stationState.ProductDataReadyAddress!, $"工位{stationState.StationNo}产品数据就绪信号", cancellationToken);
        }
        catch (BusinessOperationException) when (!IsPlcConnected(stationState.StationNo))
        {
            return;
        }

        var ready = readyValue == 1;
        if (!ready)
        {
            await HandleReadyLowAsync(stationState, readyValue, task, cancellationToken);
            return;
        }

        if (!stationState.ReadySignalInitialized)
        {
            stationState.ReadySignalInitialized = true;
            stationState.LastReadyHigh = true;
            stationState.ReadyHighObserved = true;
            stationState.AwaitingReadyReset = true;
            stationState.ObservedTaskId = task.Id;
            WriteProductionLog(
                "ProductDataReadyStaleHigh",
                ProductionFlowLogTexts.Summaries.ProductDataReadyStaleHigh,
                $"ReadyValue={readyValue}, Task={task.SN}, Detail=首次观察到高电平，等待PLC先复位为0。",
                task,
                stationNo: stationState.StationNo,
                plcSignal: AppConstants.PlcLogicalKeys.ProductDataReady,
                plcAddress: stationState.ProductDataReadyAddress?.Address);
            return;
        }

        if (stationState.LastReadyHigh)
        {
            stationState.ReadyHighObserved = true;
            if (stationState.ObservedTaskId.HasValue && stationState.ObservedTaskId.Value != task.Id)
            {
                if (!stationState.AwaitingReadyReset)
                {
                    WriteProductionLog(
                        "ProductDataReadyStaleHigh",
                        ProductionFlowLogTexts.Summaries.ProductDataReadyStaleHigh,
                        $"ReadyValue={readyValue}, PreviousTaskId={stationState.ObservedTaskId}, CurrentTaskId={task.Id}",
                        task,
                        stationNo: stationState.StationNo,
                        plcSignal: AppConstants.PlcLogicalKeys.ProductDataReady,
                        plcAddress: stationState.ProductDataReadyAddress?.Address);
                }

                stationState.AwaitingReadyReset = true;
                return;
            }
            if (stationState.AwaitingReadyReset)
            {
                return;
            }

            if (stationState.ProductDataReadyHandled)
            {
                await RetryPendingFeedbackAsync(stationState, task, cancellationToken);
                return;
            }
        }
        else
        {
            stationState.LastReadyHigh = true;
            stationState.ReadyHighObserved = true;
            stationState.ObservedTaskId = task.Id;
        }

        WriteProductionLog(
            "ProductDataReady",
            ProductionFlowLogTexts.Summaries.ProductDataReady,
            $"ReadyValue={readyValue}, ReadyAddress={stationState.ProductDataReadyAddress?.Address}, FeedbackAddress={stationState.ProductCollectionFeedbackAddress?.Address}, Edge=0->1",
            task,
            stationNo: stationState.StationNo,
            plcSignal: AppConstants.PlcLogicalKeys.ProductDataReady,
            plcAddress: stationState.ProductDataReadyAddress?.Address);
        await CollectProductCycleAsync(task, stationState, cancellationToken);
    }

    private IReadOnlyList<ActiveStationTask> GetActiveTasks()
    {
        var stationTasks = _weldTaskService.CurrentState.StationStates.Values
            .Where(station => station.IsTaskRunning && station.ActiveTask is not null)
            .Select(station => new ActiveStationTask(NormalizeStationNo(station.StationNo), station.ActiveTask!))
            .OrderBy(activeTask => activeTask.StationNo)
            .ToList();

        if (stationTasks.Count > 0)
        {
            return stationTasks;
        }

        return _weldTaskService.CurrentState.IsTaskRunning && _weldTaskService.CurrentState.ActiveTask is not null
            ? new[] { new ActiveStationTask(NormalizeStationNo(_weldTaskService.CurrentState.CurrentStationNo), _weldTaskService.CurrentState.ActiveTask) }
            : Array.Empty<ActiveStationTask>();
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
                ProductDataReadyAddress = _addressService.GetAddress(AppConstants.PlcLogicalKeys.ProductDataReady, normalizedStationNo),
                ProductCollectionFeedbackAddress = _addressService.GetAddress(AppConstants.PlcLogicalKeys.ProductCollectionFeedback, normalizedStationNo)
            };
            _stationStates[normalizedStationNo] = stationState;
            return stationState;
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task PollIdleProductReadySignalsAsync(CancellationToken cancellationToken)
    {
        List<StationCycleState> stationStates;
        await _sync.WaitAsync(cancellationToken);
        try
        {
            stationStates = _stationStates.Values.ToList();
        }
        finally
        {
            _sync.Release();
        }

        foreach (var stationState in stationStates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsUsable(stationState.ProductDataReadyAddress)
                || !IsUsable(stationState.ProductCollectionFeedbackAddress)
                || !IsPlcConnected(stationState.StationNo))
            {
                continue;
            }

            short readyValue;
            try
            {
                readyValue = await ReadNumberSignalAsync(
                    stationState.ProductDataReadyAddress!,
                    $"工位{stationState.StationNo}产品数据就绪信号",
                    cancellationToken);
            }
            catch (BusinessOperationException) when (!IsPlcConnected(stationState.StationNo))
            {
                continue;
            }

            if (readyValue != 1)
            {
                await HandleReadyLowAsync(stationState, readyValue, task: null, cancellationToken);
                continue;
            }

            if (stationState.PendingFeedbackValue.HasValue && !stationState.ProductFeedbackWritten)
            {
                await RetryPendingFeedbackAsync(stationState, task: null, cancellationToken);
            }

            var firstHighObservation = !stationState.ReadySignalInitialized || !stationState.LastReadyHigh;
            stationState.ReadySignalInitialized = true;
            stationState.ReadyHighObserved = true;
            stationState.LastReadyHigh = true;
            stationState.AwaitingReadyReset = true;
            if (firstHighObservation)
            {
                _operationLogService.Write(
                    "ProductDataReadyStaleHigh",
                    $"工位{stationState.StationNo}产品数据就绪仍为1，等待PLC复位为0后再接受下一次产品数据。Task=none");
            }
        }
    }

    private async Task HandleReadyLowAsync(
        StationCycleState stationState,
        short readyValue,
        BizWeldTask? task,
        CancellationToken cancellationToken)
    {
        var hadCycleState = stationState.ReadyHighObserved
            || stationState.ProductDataReadyHandled
            || stationState.ProductFeedbackWritten
            || stationState.PendingFeedbackValue.HasValue;
        if (hadCycleState)
        {
            if (task is not null)
            {
                WriteProductionLog(
                    "ProductDataReadyReset",
                    ProductionFlowLogTexts.Summaries.ProductDataReadyReset,
                    $"ReadyValue={readyValue}, FeedbackReset=0",
                    task,
                    stationNo: stationState.StationNo,
                    plcSignal: AppConstants.PlcLogicalKeys.ProductDataReady,
                    plcAddress: stationState.ProductDataReadyAddress?.Address);
            }
            else
            {
                _operationLogService.Write(
                    "ProductDataReadyReset",
                    $"Station={stationState.StationNo}, ReadyValue={readyValue}, FeedbackReset=0");
            }

            await WriteProductCollectionFeedbackAsync(stationState, 0, cancellationToken);
        }

        stationState.ReadySignalInitialized = true;
        stationState.LastReadyHigh = false;
        stationState.ReadyHighObserved = false;
        stationState.AwaitingReadyReset = false;
        stationState.ProductDataReadyHandled = false;
        stationState.ProductFeedbackWritten = false;
        stationState.PendingFeedbackValue = null;
        stationState.ObservedTaskId = null;
    }

    private async Task RetryPendingFeedbackAsync(
        StationCycleState stationState,
        BizWeldTask? task,
        CancellationToken cancellationToken)
    {
        if (stationState.ProductFeedbackWritten || !stationState.PendingFeedbackValue.HasValue)
        {
            return;
        }

        var feedbackValue = stationState.PendingFeedbackValue.Value;
        if (!await WriteProductCollectionFeedbackAsync(stationState, feedbackValue, cancellationToken))
        {
            return;
        }

        stationState.ProductFeedbackWritten = true;
        if (task is not null)
        {
            WriteProductionLog(
                "ProductCollectionFeedback",
                feedbackValue == 1
                    ? ProductionFlowLogTexts.Summaries.ProductCollectionFeedbackSucceeded
                    : ProductionFlowLogTexts.Summaries.ProductCollectionFeedbackFailed,
                $"Feedback={feedbackValue}, Retry=true, Address={stationState.ProductCollectionFeedbackAddress?.Address}",
                task,
                stationNo: stationState.StationNo,
                level: feedbackValue == 1 ? "Info" : "Error",
                plcSignal: AppConstants.PlcLogicalKeys.ProductCollectionFeedback,
                plcAddress: stationState.ProductCollectionFeedbackAddress?.Address);
        }
        else
        {
            _operationLogService.Write(
                "ProductCollectionFeedbackRetry",
                $"Station={stationState.StationNo}, Feedback={feedbackValue}, Retry=true, Address={stationState.ProductCollectionFeedbackAddress?.Address}");
        }
    }

    private async Task CollectProductCycleAsync(BizWeldTask task, StationCycleState stationState, CancellationToken cancellationToken)
    {
        IReadOnlyList<BizWeldPointRecord> records;
        try
        {
            WriteProductionLog(
                "ProductCollectionStart",
                ProductionFlowLogTexts.Summaries.ProductCollectionStart,
                $"ProgramId={task.ProgramId}, ReadyAddress={stationState.ProductDataReadyAddress?.Address}",
                task,
                stationNo: stationState.StationNo,
                plcSignal: AppConstants.PlcLogicalKeys.ProductDataReady,
                plcAddress: stationState.ProductDataReadyAddress?.Address);
            records = await _productCycleCollectionService.CollectAsync(task, stationState.StationNo, cancellationToken);
        }
        catch (ProductCollectionHandledException ex)
        {
            await CompleteCollectionWithHandledErrorAsync(task, stationState, ex.Detail, ex.Message, cancellationToken);
            return;
        }
        catch (BusinessOperationException ex)
        {
            await CompleteCollectionWithFailureAsync(task, stationState, ex.Detail, ex.Message, cancellationToken);
            return;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await CompleteCollectionWithFailureAsync(task, stationState, ex.Message, ex.Message, cancellationToken);
            _exceptionLogService.Write(ex, "PLC.ProductCycleMonitor", $"Station={stationState.StationNo}, WorkOrder={task.SN}");
            return;
        }

        stationState.ProductDataReadyHandled = true;
        stationState.ObservedTaskId = task.Id;
        stationState.PendingFeedbackValue = 1;
        if (await WriteProductCollectionFeedbackAsync(stationState, 1, cancellationToken))
        {
            stationState.ProductFeedbackWritten = true;
            WriteProductionLog(
                "ProductCollectionFeedback",
                ProductionFlowLogTexts.Summaries.ProductCollectionFeedbackSucceeded,
                $"Feedback=1, Records={records.Count}, Address={stationState.ProductCollectionFeedbackAddress?.Address}",
                task,
                stationNo: stationState.StationNo,
                productNo: records.FirstOrDefault()?.ProductNo,
                plcSignal: AppConstants.PlcLogicalKeys.ProductCollectionFeedback,
                plcAddress: stationState.ProductCollectionFeedbackAddress?.Address);
        }
        else
        {
            WriteProductionLog(
                "ProductCollectionFeedback",
                ProductionFlowLogTexts.Summaries.ProductCollectionFeedbackPending,
                $"Feedback=1, Records={records.Count}, ReadyValue=1, Address={stationState.ProductCollectionFeedbackAddress?.Address}",
                task,
                stationNo: stationState.StationNo,
                productNo: records.FirstOrDefault()?.ProductNo,
                level: "Error",
                plcSignal: AppConstants.PlcLogicalKeys.ProductCollectionFeedback,
                plcAddress: stationState.ProductCollectionFeedbackAddress?.Address);
        }

        try
        {
            _centerProductForwardingService.EnqueueCompletedProduct(task, stationState.StationNo, records);
            foreach (var record in records)
            {
                WeldPointCollected?.Invoke(this, record);
                await _weldPointUploadCoordinatorService.HandleCollectedAsync(record, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 下游转发或上传失败不能把已经完成的PLC采集反馈从1改成2。
            _exceptionLogService.Write(ex, "PLC.ProductCyclePostProcessing", $"Station={stationState.StationNo}, WorkOrder={task.SN}");
        }
    }

    private async Task CompleteCollectionWithHandledErrorAsync(
        BizWeldTask task,
        StationCycleState stationState,
        string detail,
        string logMessage,
        CancellationToken cancellationToken)
    {
        stationState.ProductDataReadyHandled = true;
        stationState.ObservedTaskId = task.Id;
        stationState.PendingFeedbackValue = 1;
        var feedbackWritten = IsPlcConnected(stationState.StationNo)
            && await WriteProductCollectionFeedbackAsync(stationState, 1, cancellationToken);
        stationState.ProductFeedbackWritten = feedbackWritten;

        WriteProductionLog(
            "ProductCollectionConfigurationFailed",
            ProductionFlowLogTexts.Summaries.ProductCollectionConfigurationFailed,
            $"Feedback=1, FeedbackWritten={feedbackWritten}, ReadyValue=1, {detail}",
            task,
            stationNo: stationState.StationNo,
            level: "Error",
            plcSignal: AppConstants.PlcLogicalKeys.ProductCollectionFeedback,
            plcAddress: stationState.ProductCollectionFeedbackAddress?.Address);
        WriteBusinessFailureLog(logMessage, detail);
    }

    private async Task CompleteCollectionWithFailureAsync(
        BizWeldTask task,
        StationCycleState stationState,
        string detail,
        string logMessage,
        CancellationToken cancellationToken)
    {
        stationState.ProductDataReadyHandled = true;
        stationState.ObservedTaskId = task.Id;
        stationState.PendingFeedbackValue = 2;
        var feedbackWritten = IsPlcConnected(stationState.StationNo)
            && await WriteProductCollectionFeedbackAsync(stationState, 2, cancellationToken);
        stationState.ProductFeedbackWritten = feedbackWritten;

        WriteProductionLog(
            "ProductCollectionFeedback",
            feedbackWritten
                ? ProductionFlowLogTexts.Summaries.ProductCollectionFeedbackFailed
                : ProductionFlowLogTexts.Summaries.ProductCollectionFeedbackPending,
            $"Feedback=2, FeedbackWritten={feedbackWritten}, ReadyValue=1, {detail}",
            task,
            stationNo: stationState.StationNo,
            level: "Error",
            plcSignal: AppConstants.PlcLogicalKeys.ProductCollectionFeedback,
            plcAddress: stationState.ProductCollectionFeedbackAddress?.Address);
        WriteBusinessFailureLog(logMessage, detail);
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

    private async Task<bool> WriteProductCollectionFeedbackAsync(StationCycleState stationState, short value, CancellationToken cancellationToken)
    {
        if (!IsUsable(stationState.ProductCollectionFeedbackAddress)
            || !IsPlcConnected(stationState.StationNo))
        {
            return false;
        }

        var address = stationState.ProductCollectionFeedbackAddress!;
        try
        {
            var result = NormalizeDataType(address.DataType) switch
            {
                AppConstants.PlcDataTypes.Bool => await _plcCommunicationService.WriteBoolAsync(address.Address!, value > 0, cancellationToken),
                AppConstants.PlcDataTypes.Int32 => await _plcCommunicationService.WriteInt32Async(address.Address!, value, cancellationToken),
                AppConstants.PlcDataTypes.Float => await _plcCommunicationService.WriteFloatAsync(address.Address!, value, cancellationToken),
                _ => await _plcCommunicationService.WriteInt16Async(address.Address!, value, cancellationToken)
            };

            if (result.IsSuccess)
            {
                return true;
            }

            if (IsPlcConnected(stationState.StationNo))
            {
                WriteBusinessFailureLog(
                    $"工位{stationState.StationNo}产品采集反馈写入失败",
                    $"Feedback={value}, Address={address.Address}, Error={result.Message}");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            WriteBusinessFailureLog(
                $"工位{stationState.StationNo}产品采集反馈写入失败",
                $"Feedback={value}, Address={address.Address}, Error={ex.Message}");
        }

        return false;
    }

    private static BizPlcAddress? FindAddress(IReadOnlyList<BizPlcAddress> addresses, string logicalKey, int stationNo)
    {
        return addresses
            .Where(address => string.Equals(address.LogicalKey, logicalKey, StringComparison.OrdinalIgnoreCase))
            .Where(address => address.StationNo == stationNo)
            .OrderBy(address => address.Sort)
            .FirstOrDefault();
    }

    private static bool IsWeldSignalAddress(BizPlcAddress address)
    {
        var logicalKey = address.LogicalKey;
        return string.Equals(logicalKey, AppConstants.PlcLogicalKeys.ProductDataReady, StringComparison.OrdinalIgnoreCase)
            || string.Equals(logicalKey, AppConstants.PlcLogicalKeys.ProductCollectionFeedback, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDataType(string? dataType)
    {
        return AppConstants.PlcDataTypes.All.Contains(dataType)
            ? dataType!
            : AppConstants.PlcDataTypes.Int16;
    }

    private static bool IsUsable(BizPlcAddress? address)
    {
        return address is { Enabled: true }
            && !string.IsNullOrWhiteSpace(address.Address);
    }

    private static int NormalizeStationNo(int stationNo)
    {
        return stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;
    }

    private bool IsAnyPlcStationConnected()
    {
        return _stationStates.Keys.Count == 0
            ? _plcCommunicationService.Current.IsConnected
            : _stationStates.Keys.Any(IsPlcConnected);
    }

    private bool IsPlcConnected(int stationNo)
    {
        return _plcCommunicationService.GetCurrent(stationNo).IsConnected;
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
        int? stationNo = null,
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
            stationNo ?? task.StationNo,
            task.SN,
            productNo ?? string.Empty,
            task.ProgramId ?? string.Empty,
            plcSignal ?? string.Empty,
            plcAddress ?? string.Empty);
    }

    private sealed record ActiveStationTask(int StationNo, BizWeldTask Task);

    private sealed class StationCycleState
    {
        public int StationNo { get; init; }

        public BizPlcAddress? ProductDataReadyAddress { get; init; }

        public BizPlcAddress? ProductCollectionFeedbackAddress { get; init; }

        public bool ReadySignalInitialized { get; set; }

        public bool LastReadyHigh { get; set; }

        public bool ReadyHighObserved { get; set; }

        public bool AwaitingReadyReset { get; set; }

        public int? ObservedTaskId { get; set; }

        public bool ProductDataReadyHandled { get; set; }

        public bool ProductFeedbackWritten { get; set; }

        public short? PendingFeedbackValue { get; set; }
    }
}
