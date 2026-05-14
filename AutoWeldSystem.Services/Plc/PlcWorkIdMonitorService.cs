using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Services.Plc;

public sealed class PlcWorkIdMonitorService : IPlcWorkIdMonitorService, IDisposable
{
    private static readonly TimeSpan BusinessLogInterval = TimeSpan.FromSeconds(30);

    private readonly IPlcAddressService _addressService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly ILocalizationService _localizer;
    private readonly IProgramExceptionLogService _exceptionLogService;

    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly object _businessLogSync = new();
    private BizPlcAddress? _address;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private string _lastBusinessLogKey = string.Empty;
    private DateTime _lastBusinessLogTime;
    private bool _disposed;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    public PlcWorkIdMonitorService(
        IPlcAddressService addressService,
        IPlcCommunicationService plcCommunicationService,
        ILocalizationService localizer,
        IProgramExceptionLogService exceptionLogService)
    {
        _addressService = addressService;
        _plcCommunicationService = plcCommunicationService;
        _localizer = localizer;
        _exceptionLogService = exceptionLogService;
        Current = new PlcWorkIdSnapshot(false, string.Empty, default, string.Empty);
    }

    public event EventHandler<PlcWorkIdSnapshot>? WorkIdChanged;

    public PlcWorkIdSnapshot Current { get; private set; }

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
        var address = _addressService.GetByKey(AppConstants.PlcAddressKeys.WorkId);

        await _sync.WaitAsync(cancellationToken);
        try
        {
            _address = address;
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
                WriteBusinessFailureLog(ex.Message);
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
        var address = await GetAddressAsync(cancellationToken);
        if (address is null || string.IsNullOrWhiteSpace(address.Address))
        {
            var message = _localizer.GetString(TextKeys.Plc.MessageAddressRequired);
            WriteBusinessFailureLog(message);
            Publish(new PlcWorkIdSnapshot(false, Current.WorkId, DateTime.Now, message));
            return;
        }

        var length = (ushort)Math.Max(1, address.DataLength);
        var result = await _plcCommunicationService.ReadStringAsync(address.Address, length, cancellationToken);
        if (!result.IsSuccess)
        {
            WriteBusinessFailureLog(result.Message);
            Publish(new PlcWorkIdSnapshot(false, Current.WorkId, DateTime.Now, result.Message));
            return;
        }

        var workId = (result.Value ?? string.Empty).Trim('\0', ' ', '\r', '\n', '\t');
        Publish(new PlcWorkIdSnapshot(true, workId, DateTime.Now, string.Empty));
    }

    private async Task<BizPlcAddress?> GetAddressAsync(CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            return _address;
        }
        finally
        {
            _sync.Release();
        }
    }

    private void Publish(PlcWorkIdSnapshot snapshot)
    {
        if (snapshot.Equals(Current))
        {
            return;
        }

        Current = snapshot;
        WorkIdChanged?.Invoke(this, snapshot);
    }

    /// <summary>
    /// PLC 工单号读取失败属于可预见业务异常，详细原因写日志，界面只显示“工单号读取失败”。
    /// </summary>
    private void WriteBusinessFailureLog(string detail)
    {
        var summary = _localizer.GetString(TextKeys.Monitor.RuntimeError.WorkIdReadFailed);
        if (!ShouldWriteBusinessLog(summary, detail))
        {
            return;
        }

        _exceptionLogService.WriteBusiness(
            "PLC.WorkIdMonitor",
            summary,
            detail,
            "读取地址维护中配置的工单号地址失败。");
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
