using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Interfaces.PLC;

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
    private readonly SemaphoreSlim _addressSync = new(1, 1);
    private readonly object _businessLogSync = new();
    private readonly object _snapshotSync = new();
    private List<BizPlcAddress> _addresses = [];
    private readonly Dictionary<int, PlcProductionSnapshot> _stationSnapshots = new();
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
        IProgramExceptionLogService exceptionLogService)
    {
        _plcCommunicationService = plcCommunicationService;
        _plcAddressService = plcAddressService;
        _localizer = localizer;
        _mesConnectionMonitorService = mesConnectionMonitorService;
        _deviceStatusService = deviceStatusService;
        _weldTaskService = weldTaskService;
        _exceptionLogService = exceptionLogService;
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
                    RejectedQuantityReadMessage = ex.Message
                });
                await Task.Delay(PollInterval, cancellationToken);
            }
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        var addresses = await GetAddressSnapshotAsync(cancellationToken);
        var stationNumbers = ResolveStationNumbers(addresses);
        if (!stationNumbers.Any(IsPlcConnected))
        {
            PublishIdleForStations(stationNumbers);
            return;
        }

        foreach (var stationNo in stationNumbers)
        {
            if (!IsPlcConnected(stationNo))
            {
                PublishIdleForStations([stationNo]);
                continue;
            }

            var deviceStatusAddress = GetAddress(addresses, AppConstants.PlcLogicalKeys.DeviceStatus, stationNo);
            if (deviceStatusAddress is null)
            {
                PublishFailureForStations([stationNo], "设备状态地址未配置。");
                continue;
            }

            var statusResult = await _plcCommunicationService.ReadInt16Async(deviceStatusAddress.Address!, cancellationToken);
            if (!statusResult.IsSuccess)
            {
                PublishFailureForStations([stationNo], statusResult.Message);
                continue;
            }

            await RecordDeviceStatusChangeAsync(stationNo, statusResult.Value, cancellationToken);

            var total = await ReadRequiredIntegerAsync(addresses, AppConstants.PlcLogicalKeys.TotalProduction, stationNo, cancellationToken);
            var accepted = await ReadRequiredIntegerAsync(addresses, AppConstants.PlcLogicalKeys.AcceptedQuantity, stationNo, cancellationToken);
            var rejected = await ReadRequiredIntegerAsync(addresses, AppConstants.PlcLogicalKeys.RejectedQuantity, stationNo, cancellationToken);
            var quantityMessage = BuildQuantityReadMessage(total, accepted, rejected);
            var isSnapshotSuccess = string.IsNullOrWhiteSpace(quantityMessage);

            if (!isSnapshotSuccess)
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
                statusResult.Value,
                total.Value,
                null,
                accepted.Value,
                rejected.Value,
                DateTime.Now,
                quantityMessage,
                total.IsSuccess,
                total.Message,
                accepted.IsSuccess,
                accepted.Message,
                rejected.IsSuccess,
                rejected.Message));
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

    private async Task RecordDeviceStatusChangeAsync(int stationNo, short plcStatusCode, CancellationToken cancellationToken)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var activeTask = _weldTaskService.CurrentState.StationStates.TryGetValue(normalizedStationNo, out var stationState)
            ? stationState.ActiveTask
            : _weldTaskService.CurrentState.ActiveTask;

        await _deviceStatusService.ChangeStatusAsync(
            MapPlcDeviceStatusToMesStatus(plcStatusCode),
            $"PLC status station={normalizedStationNo}, code={plcStatusCode}",
            $"PLC-S{normalizedStationNo}",
            reportToMes: _mesConnectionMonitorService.Current.IsConnected,
            stationNo: normalizedStationNo,
            weldTaskId: activeTask?.Id,
            workOrderId: activeTask?.SN,
            cancellationToken);
    }

    private static string MapPlcDeviceStatusToMesStatus(short plcStatusCode)
    {
        return plcStatusCode switch
        {
            ProductionConstants.PlcDeviceStatuses.Running => ProductionConstants.MesDeviceStatuses.ProgramStarted,
            ProductionConstants.PlcDeviceStatuses.Paused => ProductionConstants.MesDeviceStatuses.Stopped,
            ProductionConstants.PlcDeviceStatuses.Stopped => ProductionConstants.MesDeviceStatuses.Stopped,
            ProductionConstants.PlcDeviceStatuses.Alarm => ProductionConstants.MesDeviceStatuses.Exception,
            _ => ProductionConstants.MesDeviceStatuses.PoweredOn
        };
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

    private void PublishFailureForStations(IReadOnlyList<int> stationNumbers, string message)
    {
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
                RejectedQuantityReadMessage = message
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
                TotalProductionReadSuccess = false,
                TotalProductionReadMessage = "PLC未连接或生产监控未就绪。",
                AcceptedQuantityReadSuccess = false,
                AcceptedQuantityReadMessage = "PLC未连接或生产监控未就绪。",
                RejectedQuantityReadSuccess = false,
                RejectedQuantityReadMessage = "PLC未连接或生产监控未就绪。"
            });
        }
    }

    private static IReadOnlyList<int> ResolveStationNumbers(IReadOnlyList<BizPlcAddress> addresses)
    {
        var stationNumbers = addresses
            .Where(address => IsStationProductionKey(address.LogicalKey))
            .Select(address => NormalizeStationNo(address.StationNo))
            .Where(stationNo => stationNo > ProductionConstants.Stations.SharedStationNo)
            .Distinct()
            .OrderBy(stationNo => stationNo)
            .ToList();

        return stationNumbers.Count > 0
            ? stationNumbers
            : [ProductionConstants.Stations.DefaultStationNo];
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
        string rejectedQuantityReadMessage = "")
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
            RejectedQuantityReadMessage = rejectedQuantityReadMessage
        };
    }

    private sealed record PlcQuantityReadResult(bool IsSuccess, int Value, string Message)
    {
        public static PlcQuantityReadResult Success(int value) => new(true, value, string.Empty);

        public static PlcQuantityReadResult Fail(string message) => new(false, 0, message);
    }

    private static int NormalizeStationNo(int stationNo)
        => stationNo <= ProductionConstants.Stations.SharedStationNo ? ProductionConstants.Stations.DefaultStationNo : stationNo;
}
