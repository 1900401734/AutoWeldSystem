using System.Net.Sockets;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using HslCommunication;
using HslCommunication.Core.Net;
using HslCommunication.ModBus;
using HslCommunication.Profinet.Siemens;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// 基于 HslCommunication 的 PLC 通讯实现。
/// UI 和业务层只依赖 IPlcCommunicationService，不直接接触第三方库。
/// </summary>
public sealed class HslPlcCommunicationService : IPlcCommunicationService, IDisposable
{
    private readonly IAppSettingsService _settingsService;
    private readonly IOperationLogService _operationLogService;
    private readonly IPlcAddressService _plcAddressService;
    private readonly ILocalizationService _localizer;
    private readonly SemaphoreSlim _sync = new(1, 1);

    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;
    private NetworkDeviceBase? _client;
    private AppSettings _settings = new();
    private BizPlcAddress? _heartbeatAddress;
    private bool _disposed;

    public HslPlcCommunicationService(
        IAppSettingsService settingsService,
        IOperationLogService operationLogService,
        IPlcAddressService plcAddressService,
        ILocalizationService localizer)
    {
        _settingsService = settingsService;
        _operationLogService = operationLogService;
        _plcAddressService = plcAddressService;
        _localizer = localizer;

        Current = new PlcConnectionSnapshot(
            PlcConnectionState.Stopped,
            false,
            string.Empty,
            null,
            null,
            Text(TextKeys.Plc.MessageServiceStopped));
    }

    public event EventHandler<PlcConnectionSnapshot>? StatusChanged;

    public PlcConnectionSnapshot Current { get; private set; }

    /// <summary>
    /// 启动后台循环。设置和心跳地址只在启动时读取一次，避免循环中频繁访问数据库。
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_loopTask is { IsCompleted: false })
        {
            return Task.CompletedTask;
        }

        _settings = _settingsService.Get();
        _heartbeatAddress = LoadHeartbeatAddress(_settings);

        _loopCts?.Dispose();
        var loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopCts = loopCts;
        _loopTask = Task.Run(
            () => RunConnectionLoopAsync(_settings, _heartbeatAddress, loopCts.Token),
            CancellationToken.None);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 正常停止服务时会关闭 PLC 连接；程序退出释放 DI 容器时会走内部静默停止，避免退出阶段再触发 HSL 关闭异常。
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return StopInternalAsync(closeClient: true, cancellationToken);
    }

    /// <summary>
    /// 系统设置或地址配置保存后调用，用最新参数重新创建 PLC 连接。
    /// </summary>
    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken);
        await StartAsync(cancellationToken);
    }

    public Task<PlcServiceResult<bool>> ReadBoolAsync(string address, CancellationToken cancellationToken = default)
    {
        return ExecuteReadAsync(address, client => client.ReadBoolAsync(address), cancellationToken);
    }

    public Task<PlcServiceResult<short>> ReadInt16Async(string address, CancellationToken cancellationToken = default)
    {
        return ExecuteReadAsync(address, client => client.ReadInt16Async(address), cancellationToken);
    }

    public Task<PlcServiceResult<int>> ReadInt32Async(string address, CancellationToken cancellationToken = default)
    {
        return ExecuteReadAsync(address, client => client.ReadInt32Async(address), cancellationToken);
    }

    public Task<PlcServiceResult<float>> ReadFloatAsync(string address, CancellationToken cancellationToken = default)
    {
        return ExecuteReadAsync(address, client => client.ReadFloatAsync(address), cancellationToken);
    }

    public Task<PlcServiceResult<string>> ReadStringAsync(string address, ushort length, CancellationToken cancellationToken = default)
    {
        return ExecuteReadAsync(address, client => client.ReadStringAsync(address, Math.Max((ushort)1, length)), cancellationToken);
    }

    public Task<PlcServiceResult> WriteBoolAsync(string address, bool value, CancellationToken cancellationToken = default)
    {
        return ExecuteWriteAsync(address, client => client.WriteAsync(address, value), cancellationToken);
    }

    public Task<PlcServiceResult> WriteInt16Async(string address, short value, CancellationToken cancellationToken = default)
    {
        return ExecuteWriteAsync(address, client => client.WriteAsync(address, value), cancellationToken);
    }

    public Task<PlcServiceResult> WriteInt32Async(string address, int value, CancellationToken cancellationToken = default)
    {
        return ExecuteWriteAsync(address, client => client.WriteAsync(address, value), cancellationToken);
    }

    public Task<PlcServiceResult> WriteFloatAsync(string address, float value, CancellationToken cancellationToken = default)
    {
        return ExecuteWriteAsync(address, client => client.WriteAsync(address, value), cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopInternalAsync(closeClient: false, CancellationToken.None);
        DisposeCore();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            StopInternalAsync(closeClient: false, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch
        {
            // 退出程序时不再把后台通讯释放异常抛给 UI。
        }

        DisposeCore();
    }

    private async Task StopInternalAsync(bool closeClient, CancellationToken cancellationToken)
    {
        if (_loopCts is not null)
        {
            await _loopCts.CancelAsync();
        }

        if (_loopTask is not null)
        {
            try
            {
                await _loopTask.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (TimeoutException)
            {
            }
            catch
            {
                // 后台任务异常已经通过状态快照记录，停止流程不重复抛出。
            }
        }

        if (closeClient)
        {
            try
            {
                await CloseClientAsync(closeVendorConnection: true);
            }
            catch
            {
                ForgetClientReference();
            }
        }
        else
        {
            // DI 容器释放时可能已经开始卸载依赖，手动退出只清引用即可，进程结束会回收 socket。
            ForgetClientReference();
        }

        Publish(PlcConnectionState.Stopped, false, Text(TextKeys.Plc.MessageServiceStopped));
    }

    private void DisposeCore()
    {
        if (_disposed)
        {
            return;
        }

        _loopCts?.Dispose();
        _sync.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// 后台循环：未连接时重连，已连接时执行心跳；心跳失败后关闭旧连接并等待下一轮重连。
    /// </summary>
    private async Task RunConnectionLoopAsync(
        AppSettings settings,
        BizPlcAddress? heartbeatAddress,
        CancellationToken cancellationToken)
    {
        var endpoint = BuildEndpoint(settings);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!Current.IsConnected)
                {
                    Publish(PlcConnectionState.Reconnecting, false, Text(TextKeys.Plc.MessageConnecting, endpoint), endpoint: endpoint);

                    var connectResult = await ConnectAsync(settings, cancellationToken);
                    if (!connectResult.IsSuccess)
                    {
                        Publish(PlcConnectionState.Disconnected, false, connectResult.Message, endpoint: endpoint);
                        await DelayAsync(settings.PlcReconnectIntervalSeconds, cancellationToken);
                        continue;
                    }
                }

                var heartbeatResult = await CheckHeartbeatAsync(settings, heartbeatAddress, cancellationToken);
                if (!heartbeatResult.IsSuccess)
                {
                    await MarkDisconnectedAsync(heartbeatResult.Message, endpoint);
                    await DelayAsync(settings.PlcReconnectIntervalSeconds, cancellationToken);
                    continue;
                }

                Publish(
                    PlcConnectionState.Connected,
                    true,
                    heartbeatResult.Message,
                    heartbeatTime: DateTime.Now,
                    endpoint: endpoint);

                await DelayAsync(settings.PlcHeartbeatIntervalSeconds, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Publish(PlcConnectionState.Faulted, false, ex.Message, endpoint: endpoint);
            WriteOperationLog("Faulted", ex.Message);
        }
    }

    /// <summary>
    /// 创建 HSL 客户端并建立持久连接，后续读写复用该连接。
    /// </summary>
    private async Task<PlcServiceResult> ConnectAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_client is not null && Current.IsConnected)
            {
                return PlcServiceResult.Success(Text(TextKeys.Plc.MessageAlreadyConnected));
            }

            CloseClientCore(closeVendorConnection: true);

            var clientResult = CreateClient(settings);
            if (!clientResult.IsSuccess || clientResult.Value is null)
            {
                return PlcServiceResult.Fail(clientResult.Message);
            }

            var client = clientResult.Value;
            var connectResult = await client.ConnectServerAsync();
            if (!connectResult.IsSuccess)
            {
                SafeCloseClient(client);
                return PlcServiceResult.Fail(connectResult.Message);
            }

            _client = client;
            var endpoint = BuildEndpoint(settings);

            Publish(
                PlcConnectionState.Connected,
                true,
                Text(TextKeys.Plc.MessageConnected, endpoint),
                connectedTime: DateTime.Now,
                endpoint: endpoint);

            WriteOperationLog("Connected", endpoint);
            return PlcServiceResult.Success(Text(TextKeys.Plc.MessageConnected, endpoint));
        }
        catch (Exception ex)
        {
            return PlcServiceResult.Fail(ex.Message);
        }
        finally
        {
            _sync.Release();
        }
    }

    /// <summary>
    /// 根据配置创建具体 PLC 客户端；后续新增协议时只扩展此方法。
    /// </summary>
    private PlcServiceResult<NetworkDeviceBase> CreateClient(AppSettings settings)
    {
        if (IsPlcType(settings.PlcType, AppConstants.PlcTypes.ModbusTcp))
        {
            var modbus = new ModbusTcpNet(settings.PlcIp, settings.PlcPort)
            {
                AddressStartWithZero = true,
                ConnectTimeOut = Math.Max(1000, settings.PlcConnectTimeoutMilliseconds),
                ReceiveTimeOut = Math.Max(1000, settings.PlcConnectTimeoutMilliseconds)
            };

            modbus.SetPersistentConnection();
            return PlcServiceResult<NetworkDeviceBase>.Success(modbus);
        }

        if (IsSiemensS71200(settings.PlcType))
        {
            var siemens = new SiemensS7Net(SiemensPLCS.S1200, settings.PlcIp)
            {
                Port = settings.PlcPort,
                ConnectTimeOut = Math.Max(1000, settings.PlcConnectTimeoutMilliseconds),
                ReceiveTimeOut = Math.Max(1000, settings.PlcConnectTimeoutMilliseconds)
            };

            siemens.SetPersistentConnection();
            return PlcServiceResult<NetworkDeviceBase>.Success(siemens);
        }

        return PlcServiceResult<NetworkDeviceBase>.Fail(
            Text(TextKeys.Plc.MessageUnsupportedType, settings.PlcType));
    }

    /// <summary>
    /// 优先使用系统设置中的心跳地址；如果为空，则使用地址维护页中配置的 PLC Heartbeat。
    /// </summary>
    private BizPlcAddress? LoadHeartbeatAddress(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.PlcHeartbeatAddress))
        {
            return new BizPlcAddress
            {
                AddressKey = AppConstants.PlcAddressKeys.PlcHeartBeat,
                AddressName = "PLC Heartbeat",
                Address = settings.PlcHeartbeatAddress.Trim(),
                DataType = AppConstants.PlcDataTypes.Int16,
                DataLength = 1,
                Enabled = true
            };
        }

        try
        {
            return _plcAddressService.GetByKey(AppConstants.PlcAddressKeys.PlcHeartBeat);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 有心跳地址时读取 PLC 地址；没有心跳地址时至少做 TCP 探测，保证服务端停止后状态能变为断开。
    /// </summary>
    private async Task<PlcServiceResult> CheckHeartbeatAsync(
        AppSettings settings,
        BizPlcAddress? heartbeatAddress,
        CancellationToken cancellationToken)
    {
        var address = heartbeatAddress?.Address?.Trim();
        if (heartbeatAddress is null || !heartbeatAddress.Enabled || string.IsNullOrWhiteSpace(address))
        {
            return await ProbeTcpEndpointAsync(settings, cancellationToken);
        }

        var dataType = NormalizeDataType(heartbeatAddress.DataType);
        var readResult = dataType switch
        {
            AppConstants.PlcDataTypes.Bool => ToPlainResult(await ExecuteReadAsync(
                address,
                client => client.ReadBoolAsync(address),
                cancellationToken,
                markDisconnectedOnFailure: false)),
            AppConstants.PlcDataTypes.Int32 => ToPlainResult(await ExecuteReadAsync(
                address,
                client => client.ReadInt32Async(address),
                cancellationToken,
                markDisconnectedOnFailure: false)),
            AppConstants.PlcDataTypes.Float => ToPlainResult(await ExecuteReadAsync(
                address,
                client => client.ReadFloatAsync(address),
                cancellationToken,
                markDisconnectedOnFailure: false)),
            AppConstants.PlcDataTypes.String => ToPlainResult(await ExecuteReadAsync(
                address,
                client => client.ReadStringAsync(address, (ushort)Math.Max(1, heartbeatAddress.DataLength)),
                cancellationToken,
                markDisconnectedOnFailure: false)),
            _ => ToPlainResult(await ExecuteReadAsync(
                address,
                client => client.ReadInt16Async(address),
                cancellationToken,
                markDisconnectedOnFailure: false))
        };

        return readResult.IsSuccess
            ? PlcServiceResult.Success(Text(TextKeys.Plc.MessageHeartbeatSucceeded))
            : PlcServiceResult.Fail(Text(TextKeys.Plc.MessageHeartbeatFailed, readResult.Message));
    }

    /// <summary>
    /// 无心跳地址时使用 TCP 端口探测，避免“连接已断但界面仍显示已连接”。
    /// </summary>
    private async Task<PlcServiceResult> ProbeTcpEndpointAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            using var tcpClient = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutMilliseconds = Math.Max(1000, settings.PlcConnectTimeoutMilliseconds);
            var connectTask = tcpClient.ConnectAsync(settings.PlcIp, settings.PlcPort, timeoutCts.Token).AsTask();
            var timeoutTask = Task.Delay(timeoutMilliseconds, cancellationToken);
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask != connectTask)
            {
                await timeoutCts.CancelAsync();
                return PlcServiceResult.Fail(Text(TextKeys.Plc.MessageTcpProbeFailed, Text(TextKeys.Plc.MessageTimeout)));
            }

            await connectTask;
            return PlcServiceResult.Success(Text(TextKeys.Plc.MessageHeartbeatSkipped));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return PlcServiceResult.Fail(Text(TextKeys.Plc.MessageTcpProbeFailed, ex.Message));
        }
    }

    /// <summary>
    /// 统一封装读取逻辑，业务层不用重复写连接检查、异常处理和断线标记。
    /// </summary>
    private async Task<PlcServiceResult<T>> ExecuteReadAsync<T>(string address,
        Func<NetworkDeviceBase, Task<OperateResult<T>>> action,
        CancellationToken cancellationToken,
        bool markDisconnectedOnFailure = true)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return PlcServiceResult<T>.Fail(Text(TextKeys.Plc.MessageAddressRequired));
        }

        var connectResult = await EnsureConnectedAsync(cancellationToken);
        if (!connectResult.IsSuccess)
        {
            return PlcServiceResult<T>.Fail(connectResult.Message);
        }

        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_client is null)
            {
                return PlcServiceResult<T>.Fail(Text(TextKeys.Plc.MessageNotConnected));
            }

            var result = await action(_client);
            if (result.IsSuccess)
            {
                return PlcServiceResult<T>.Success(result.Content);
            }

            if (markDisconnectedOnFailure)
            {
                await MarkDisconnectedCoreAsync(result.Message, Current.Endpoint);
            }

            return PlcServiceResult<T>.Fail(result.Message);
        }
        catch (Exception ex)
        {
            if (markDisconnectedOnFailure)
            {
                await MarkDisconnectedCoreAsync(ex.Message, Current.Endpoint);
            }

            return PlcServiceResult<T>.Fail(ex.Message);
        }
        finally
        {
            _sync.Release();
        }
    }

    private static PlcServiceResult ToPlainResult<T>(PlcServiceResult<T> result)
    {
        return result.IsSuccess
            ? PlcServiceResult.Success(result.Message)
            : PlcServiceResult.Fail(result.Message);
    }

    /// <summary>
    /// 统一封装写入逻辑，写失败后标记断线并交给后台循环重连。
    /// </summary>
    private async Task<PlcServiceResult> ExecuteWriteAsync(
        string address,
        Func<NetworkDeviceBase, Task<OperateResult>> action,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return PlcServiceResult.Fail(Text(TextKeys.Plc.MessageAddressRequired));
        }

        var connectResult = await EnsureConnectedAsync(cancellationToken);
        if (!connectResult.IsSuccess)
        {
            return connectResult;
        }

        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_client is null)
            {
                return PlcServiceResult.Fail(Text(TextKeys.Plc.MessageNotConnected));
            }

            var result = await action(_client);
            if (result.IsSuccess)
            {
                return PlcServiceResult.Success(Text(TextKeys.Plc.MessageWriteSucceeded));
            }

            await MarkDisconnectedCoreAsync(result.Message, Current.Endpoint);
            return PlcServiceResult.Fail(result.Message);
        }
        catch (Exception ex)
        {
            await MarkDisconnectedCoreAsync(ex.Message, Current.Endpoint);
            return PlcServiceResult.Fail(ex.Message);
        }
        finally
        {
            _sync.Release();
        }
    }

    /// <summary>
    /// 业务读写前确保已连接；如果当前断开，会立即尝试连接一次。
    /// </summary>
    private async Task<PlcServiceResult> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_client is not null && Current.IsConnected)
        {
            return PlcServiceResult.Success(Text(TextKeys.Plc.MessageAlreadyConnected));
        }

        return await ConnectAsync(_settings, cancellationToken);
    }

    private async Task MarkDisconnectedAsync(string message, string endpoint)
    {
        await _sync.WaitAsync();
        try
        {
            await MarkDisconnectedCoreAsync(message, endpoint);
        }
        finally
        {
            _sync.Release();
        }
    }

    private Task MarkDisconnectedCoreAsync(string message, string endpoint)
    {
        CloseClientCore(closeVendorConnection: true);
        Publish(PlcConnectionState.Disconnected, false, message, endpoint: endpoint);
        WriteOperationLog("Disconnected", message);
        return Task.CompletedTask;
    }

    private async Task CloseClientAsync(bool closeVendorConnection)
    {
        var lockTaken = false;
        try
        {
            await _sync.WaitAsync();
            lockTaken = true;
            CloseClientCore(closeVendorConnection);
        }
        finally
        {
            if (lockTaken)
            {
                _sync.Release();
            }
        }
    }

    private void CloseClientCore(bool closeVendorConnection)
    {
        var client = _client;
        _client = null;

        if (!closeVendorConnection || client is null)
        {
            return;
        }

        SafeCloseClient(client);
    }

    private void ForgetClientReference()
    {
        _client = null;
    }

    private static void SafeCloseClient(NetworkDeviceBase client)
    {
        try
        {
            client.ConnectClose();
        }
        catch
        {
            // 关闭连接失败不影响后续重连或程序退出。
        }
    }

    /// <summary>
    /// 发布状态快照，MonitorView 通过 StatusChanged 实时刷新 PLC 状态。
    /// </summary>
    private void Publish(
        PlcConnectionState state,
        bool isConnected,
        string message,
        DateTime? heartbeatTime = null,
        DateTime? connectedTime = null,
        string? endpoint = null)
    {
        var snapshot = new PlcConnectionSnapshot(
            state,
            isConnected,
            endpoint ?? Current.Endpoint,
            connectedTime ?? Current.LastConnectedTime,
            heartbeatTime ?? Current.LastHeartbeatTime,
            message);

        Current = snapshot;
        StatusChanged?.Invoke(this, snapshot);
    }

    private void WriteOperationLog(string action, string detail)
    {
        try
        {
            _operationLogService.Write("PLC", $"{action}: {detail}");
        }
        catch
        {
            // 通讯服务不能因为日志写入失败而停止。
        }
    }

    private string Text(string key, params object[] args)
    {
        return _localizer.GetString(key, args);
    }

    private static bool IsPlcType(string? actual, string expected)
    {
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSiemensS71200(string? plcType)
    {
        return string.Equals(plcType, AppConstants.PlcTypes.SiemensS71200, StringComparison.OrdinalIgnoreCase)
            || string.Equals(plcType, AppConstants.PlcTypes.SiemensS7Legacy, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDataType(string? dataType)
    {
        return AppConstants.PlcDataTypes.All.Contains(dataType)
            ? dataType!
            : AppConstants.PlcDataTypes.Int16;
    }

    private static string BuildEndpoint(AppSettings settings)
    {
        return $"{settings.PlcType}@{settings.PlcIp}:{settings.PlcPort}";
    }

    private static Task DelayAsync(int seconds, CancellationToken cancellationToken)
    {
        return Task.Delay(TimeSpan.FromSeconds(Math.Max(1, seconds)), cancellationToken);
    }
}
