using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.PLC;

namespace AutoWeldSystem.Services.Plc;

public sealed class WorkIdMonitorService : IPlcWorkIdMonitorService, IDisposable
{
    private static readonly TimeSpan BusinessLogInterval = TimeSpan.FromSeconds(30);

    private readonly IPlcAddressService _addressService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly ILocalizationService _localizer;
    private readonly IProgramExceptionLogService _exceptionLogService;

    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly object _businessLogSync = new();
    private readonly object _snapshotSync = new();
    private Dictionary<int, BizPlcAddress?> _addresses = new();
    private Dictionary<int, PlcWorkIdSnapshot> _stationSnapshots = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private string _lastBusinessLogKey = string.Empty;
    private DateTime _lastBusinessLogTime;
    private bool _disposed;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    public WorkIdMonitorService(
        IPlcAddressService addressService,
        IPlcCommunicationService plcCommunicationService,
        ILocalizationService localizer,
        IProgramExceptionLogService exceptionLogService)
    {
        _addressService = addressService;
        _plcCommunicationService = plcCommunicationService;
        _localizer = localizer;
        _exceptionLogService = exceptionLogService;
        Current = CreateSnapshot(ProductionConstants.Stations.DefaultStationNo, false, string.Empty, default, string.Empty);
        _stationSnapshots[ProductionConstants.Stations.DefaultStationNo] = Current;
    }

    public event EventHandler<PlcWorkIdSnapshot>? WorkIdChanged;

    public PlcWorkIdSnapshot Current { get; private set; }

    public PlcWorkIdSnapshot GetCurrent(int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        lock (_snapshotSync)
        {
            return _stationSnapshots.TryGetValue(normalizedStationNo, out var snapshot)
                ? snapshot
                : CreateSnapshot(normalizedStationNo, false, string.Empty, default, string.Empty);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_loopTask is { IsCompleted: false })
        {
            return;
        }

        await ReloadAddressAsync(cancellationToken);
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
        }
    }

    public async Task ReloadAddressAsync(CancellationToken cancellationToken = default)
    {
        var addresses = ResolveWorkIdAddresses();

        await _sync.WaitAsync(cancellationToken);
        try
        {
            _addresses = addresses;

            lock (_snapshotSync)
            {
                foreach (var stationNo in addresses.Keys)
                {
                    if (!_stationSnapshots.ContainsKey(stationNo))
                    {
                        _stationSnapshots[stationNo] = CreateSnapshot(stationNo, false, string.Empty, default, string.Empty);
                    }
                }
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
                await ReadOnceAsync(cancellationToken);
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

                WriteBusinessFailureLog(Current.StationNo, ex.Message);
                Publish(Current with
                {
                    IsSuccess = false,
                    UpdatedTime = DateTime.Now,
                    Message = ex.Message
                });
                await Task.Delay(PollInterval, cancellationToken);
            }
        }
    }

    private async Task ReadOnceAsync(CancellationToken cancellationToken)
    {
        var addresses = await GetAddressSnapshotAsync(cancellationToken);
        foreach (var (stationNo, address) in addresses.OrderBy(it => it.Key))
        {
            await ReadStationOnceAsync(stationNo, address, cancellationToken);
        }
    }

    private async Task ReadStationOnceAsync(
        int stationNo,
        BizPlcAddress? address,
        CancellationToken cancellationToken)
    {
        var current = GetCurrent(stationNo);
        if (!IsPlcConnected(stationNo))
        {
            PublishIdle(stationNo, current);
            return;
        }

        if (address is null || string.IsNullOrWhiteSpace(address.Address))
        {
            PublishIdle(stationNo, current);
            return;
        }

        var length = (ushort)Math.Max(1, address.DataLength);
        var result = await _plcCommunicationService.ReadStringAsync(address.Address, length, cancellationToken);
        if (!result.IsSuccess)
        {
            if (!IsPlcConnected(stationNo))
            {
                PublishIdle(stationNo, current);
                return;
            }

            WriteBusinessFailureLog(stationNo, result.Message);
            Publish(CreateSnapshot(stationNo, false, current.WorkId, DateTime.Now, result.Message));
            return;
        }

        var workId = (result.Value ?? string.Empty).Trim('\0', ' ', '\r', '\n', '\t');
        Publish(CreateSnapshot(stationNo, true, workId, DateTime.Now, string.Empty));
    }

    private bool IsAnyPlcStationConnected()
    {
        return _addresses.Keys.Count == 0
            ? _plcCommunicationService.Current.IsConnected
            : _addresses.Keys.Any(IsPlcConnected);
    }

    private bool IsPlcConnected(int stationNo)
    {
        return _plcCommunicationService.GetCurrent(stationNo).IsConnected;
    }

    private void PublishIdle(int stationNo, PlcWorkIdSnapshot current)
    {
        if (!current.IsSuccess && string.IsNullOrWhiteSpace(current.Message))
        {
            return;
        }

        Publish(CreateSnapshot(stationNo, false, current.WorkId, DateTime.Now, string.Empty));
    }

    private async Task<Dictionary<int, BizPlcAddress?>> GetAddressSnapshotAsync(CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            return new Dictionary<int, BizPlcAddress?>(_addresses);
        }
        finally
        {
            _sync.Release();
        }
    }

    private void Publish(PlcWorkIdSnapshot snapshot)
    {
        var previous = GetCurrent(snapshot.StationNo);
        if (snapshot.Equals(previous))
        {
            return;
        }

        lock (_snapshotSync)
        {
            _stationSnapshots[snapshot.StationNo] = snapshot;
            if (snapshot.StationNo == ProductionConstants.Stations.DefaultStationNo)
            {
                Current = snapshot;
            }
        }

        WorkIdChanged?.Invoke(this, snapshot);
    }

    /// <summary>
    /// PLC 工单号读取失败属于可预见业务异常，详细原因写日志，界面只显示“工单号读取失败”。
    /// </summary>
    private void WriteBusinessFailureLog(int stationNo, string detail)
    {
        var summary = _localizer.GetString(TextKeys.Monitor.RuntimeError.WorkIdReadFailed);
        if (!ShouldWriteBusinessLog(stationNo, summary, detail))
        {
            return;
        }

        _exceptionLogService.WriteBusiness(
            "PLC.WorkIdMonitor",
            summary,
            detail,
            $"读取地址维护中配置的工单号地址失败。Station={stationNo}");
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

    private Dictionary<int, BizPlcAddress?> ResolveWorkIdAddresses()
    {
        var addresses = _addressService.GetAll()
            .Where(IsWorkIdAddress)
            .GroupBy(address => NormalizeStationNo(address.StationNo))
            .ToDictionary(
                group => group.Key,
                group => NormalizeReadableAddress(group.OrderBy(address => address.Sort).First()),
                EqualityComparer<int>.Default);

        if (addresses.Count == 0)
        {
            addresses[ProductionConstants.Stations.DefaultStationNo] = NormalizeReadableAddress(
                _addressService.GetAddress(AppConstants.PlcLogicalKeys.WorkId, ProductionConstants.Stations.DefaultStationNo));
        }

        return addresses
            .OrderBy(item => item.Key)
            .ToDictionary(item => item.Key, item => item.Value);
    }

    private static BizPlcAddress? NormalizeReadableAddress(BizPlcAddress? address)
    {
        return address is { Enabled: true } && !string.IsNullOrWhiteSpace(address.Address)
            ? address
            : null;
    }

    private static bool IsWorkIdAddress(BizPlcAddress address)
    {
        return string.Equals(address.LogicalKey, AppConstants.PlcLogicalKeys.WorkId, StringComparison.OrdinalIgnoreCase);
    }

    private static PlcWorkIdSnapshot CreateSnapshot(
        int stationNo,
        bool isSuccess,
        string workId,
        DateTime updatedTime,
        string message)
    {
        return new PlcWorkIdSnapshot(isSuccess, workId, updatedTime, message)
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
