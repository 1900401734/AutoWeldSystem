using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// PLC 生产状态监控。
/// 定时读取地址维护页中配置的设备状态和产量指标，向界面发布快照。
/// </summary>
public sealed class PlcProductionMonitorService : IPlcProductionMonitorService, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan BusinessLogInterval = TimeSpan.FromSeconds(30);

    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IPlcAddressService _plcAddressService;
    private readonly ILocalizationService _localizer;
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

    public PlcProductionMonitorService(
        IPlcCommunicationService plcCommunicationService,
        IPlcAddressService plcAddressService,
        ILocalizationService localizer,
        IProgramExceptionLogService exceptionLogService)
    {
        _plcCommunicationService = plcCommunicationService;
        _plcAddressService = plcAddressService;
        _localizer = localizer;
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
                WriteBusinessFailureLog(Current.StationNo, ex.Message);
                Publish(Current with
                {
                    IsSuccess = false,
                    DeviceStatusCode = null,
                    UpdatedTime = DateTime.Now,
                    Message = ex.Message
                });
                await Task.Delay(PollInterval, cancellationToken);
            }
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        var addresses = await GetAddressSnapshotAsync(cancellationToken);
        var stationNumbers = ResolveStationNumbers(addresses);
        var deviceStatusAddress = GetAddress(addresses, AppConstants.PlcAddressKeys.DeviceStatus, ProductionConstants.Stations.DefaultStationNo);
        if (deviceStatusAddress is null)
        {
            var message = _localizer.GetString(TextKeys.Plc.MessageAddressRequired);
            PublishFailureForStations(stationNumbers, message);
            return;
        }

        var statusResult = await _plcCommunicationService.ReadInt16Async(deviceStatusAddress.Address!, cancellationToken);
        if (!statusResult.IsSuccess)
        {
            PublishFailureForStations(stationNumbers, statusResult.Message);
            return;
        }

        foreach (var stationNo in stationNumbers)
        {
            var total = await ReadIntegerOrDefaultAsync(addresses, AppConstants.PlcAddressKeys.TotalProduction, stationNo, cancellationToken);
            var target = await ReadNullableIntegerAsync(addresses, AppConstants.PlcAddressKeys.TargetProduction, stationNo, cancellationToken);
            var accepted = await ReadIntegerOrDefaultAsync(addresses, AppConstants.PlcAddressKeys.AcceptedQuantity, stationNo, cancellationToken);
            var rejected = await ReadIntegerOrDefaultAsync(addresses, AppConstants.PlcAddressKeys.RejectedQuantity, stationNo, cancellationToken);

            Publish(CreateSnapshot(
                stationNo,
                true,
                statusResult.Value,
                total,
                target,
                accepted,
                rejected,
                DateTime.Now,
                string.Empty));
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

    private async Task<int> ReadIntegerOrDefaultAsync(
        IReadOnlyList<BizPlcAddress> addresses,
        string key,
        int stationNo,
        CancellationToken cancellationToken)
    {
        var value = await ReadNullableIntegerAsync(addresses, key, stationNo, cancellationToken);
        return value ?? 0;
    }

    private async Task<int?> ReadNullableIntegerAsync(
        IReadOnlyList<BizPlcAddress> addresses,
        string key,
        int stationNo,
        CancellationToken cancellationToken)
    {
        var address = GetAddress(addresses, key, stationNo);
        if (address is null)
        {
            return null;
        }

        var plcAddress = address.Address?.Trim();
        if (string.IsNullOrWhiteSpace(plcAddress))
        {
            return null;
        }

        var dataType = NormalizeDataType(address.DataType);
        return dataType switch
        {
            AppConstants.PlcDataTypes.Bool => ToInteger(await _plcCommunicationService.ReadBoolAsync(plcAddress, cancellationToken)),
            AppConstants.PlcDataTypes.Int32 => ToInteger(await _plcCommunicationService.ReadInt32Async(plcAddress, cancellationToken)),
            AppConstants.PlcDataTypes.Float => ToInteger(await _plcCommunicationService.ReadFloatAsync(plcAddress, cancellationToken)),
            AppConstants.PlcDataTypes.String => ToInteger(await _plcCommunicationService.ReadStringAsync(
                plcAddress,
                (ushort)Math.Max(1, address.DataLength),
                cancellationToken)),
            _ => ToInteger(await _plcCommunicationService.ReadInt16Async(plcAddress, cancellationToken))
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

    private static BizPlcAddress? GetAddress(IReadOnlyList<BizPlcAddress> addresses, string logicalKey, int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        return addresses
            .Where(address => string.Equals(GetLogicalKey(address), logicalKey, StringComparison.OrdinalIgnoreCase))
            .Where(address => address.StationNo == normalizedStationNo
                || address.StationNo == ProductionConstants.Stations.SharedStationNo)
            .OrderByDescending(address => address.StationNo == normalizedStationNo)
            .ThenByDescending(address => address.StationNo == ProductionConstants.Stations.SharedStationNo)
            .ThenBy(address => address.Sort)
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
                Message = message
            });
        }
    }

    private static IReadOnlyList<int> ResolveStationNumbers(IReadOnlyList<BizPlcAddress> addresses)
    {
        var stationNumbers = addresses
            .Where(address => IsStationMetricKey(GetLogicalKey(address)))
            .Select(address => NormalizeStationNo(address.StationNo))
            .Where(stationNo => stationNo > ProductionConstants.Stations.SharedStationNo)
            .Distinct()
            .OrderBy(stationNo => stationNo)
            .ToList();

        return stationNumbers.Count > 0
            ? stationNumbers
            : [ProductionConstants.Stations.DefaultStationNo];
    }

    private static bool IsStationMetricKey(string logicalKey)
    {
        return logicalKey is AppConstants.PlcAddressKeys.TotalProduction
            or AppConstants.PlcAddressKeys.TargetProduction
            or AppConstants.PlcAddressKeys.AcceptedQuantity
            or AppConstants.PlcAddressKeys.RejectedQuantity;
    }

    private static string GetLogicalKey(BizPlcAddress address)
    {
        return string.IsNullOrWhiteSpace(address.LogicalKey)
            ? address.AddressKey
            : address.LogicalKey.Trim();
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
        string message)
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
            StationNo = NormalizeStationNo(stationNo)
        };
    }

    private static int NormalizeStationNo(int stationNo)
    {
        return stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;
    }
}
