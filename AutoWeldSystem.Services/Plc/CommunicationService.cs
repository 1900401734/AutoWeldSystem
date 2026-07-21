using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.Core.Runtime;
using HslCommunication;
using HslCommunication.Core.Net;
using HslCommunication.ModBus;
using HslCommunication.Profinet.Siemens;

namespace AutoWeldSystem.Services.Plc;

public sealed class CommunicationService : IPlcCommunicationService, IDisposable
{
    private static readonly TimeSpan PcHeartbeatWriteInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PlcHeartbeatStaleThreshold = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan ConnectionObjectModeInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromSeconds(10);

    private const int HeartbeatFailureThreshold = 3;
    private const int PlcCommunicationTimeoutMilliseconds = 3000;

    private static readonly string[] VerificationLogicalKeyPriority =
    [
        AppConstants.PlcLogicalKeys.DeviceStatus,
        AppConstants.PlcLogicalKeys.TotalProduction,
        AppConstants.PlcLogicalKeys.AcceptedQuantity,
        AppConstants.PlcLogicalKeys.RejectedQuantity,
        AppConstants.PlcLogicalKeys.WorkId,
        AppConstants.PlcLogicalKeys.PlcRecipeCode,
        AppConstants.PlcLogicalKeys.ProductDataReady,
        AppConstants.PlcLogicalKeys.DeviceMode
    ];

    private readonly IAppSettingsService _settingsService;
    private readonly IOperationLogService _operationLogService;
    private readonly IPlcAddressService _plcAddressService;
    private readonly ILocalizationService _localizer;
    private readonly SemaphoreSlim _sync = new(1, 1);

    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;
    private NetworkDeviceBase? _client;
    private AppSettings _settings = new();
    private AppSettings _currentSettings;
    private IReadOnlyDictionary<int, HeartbeatAddressPair> _heartbeatAddresses = new Dictionary<int, HeartbeatAddressPair>();
    private readonly Dictionary<int, StationHeartbeatRuntime> _heartbeatStates = new();
    private readonly Dictionary<int, PlcConnectionSnapshot> _stationSnapshots = new();
    private readonly object _snapshotSync = new();
    private int _stopping;
    private bool _disposed;

    public CommunicationService(
        IAppSettingsService settingsService,
        IOperationLogService operationLogService,
        IPlcAddressService plcAddressService,
        ILocalizationService localizer)
    {
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
        _operationLogService = operationLogService;
        _plcAddressService = plcAddressService;
        _localizer = localizer;

        Current = new PlcConnectionSnapshot(PlcConnectionState.Stopped, false, string.Empty, null, null, Text(TextKeys.Plc.MessageServiceStopped));
        _stationSnapshots[ProductionConstants.Stations.DefaultStationNo] = Current;
    }

    public event EventHandler<PlcConnectionSnapshot>? StatusChanged;

    public PlcConnectionSnapshot Current { get; private set; }

    public PlcConnectionSnapshot GetCurrent(int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        lock (_snapshotSync)
        {
            return _stationSnapshots.TryGetValue(normalizedStationNo, out var snapshot)
                ? snapshot
                : Current with { StationNo = normalizedStationNo };
        }
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_loopTask is { IsCompleted: false })
        {
            return Task.CompletedTask;
        }

        Volatile.Write(ref _stopping, 0);
        _settings = CurrentSettings;
        _heartbeatAddresses = LoadHeartbeatAddresses(_settings);
        ResetHeartbeatStates(ResolveRuntimeStationNumbers(_settings, _heartbeatAddresses));

        _loopCts?.Dispose();
        var loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopCts = loopCts;
        _loopTask = Task.Run(
            () => RunConnectionLoopAsync(_settings, _heartbeatAddresses, loopCts.Token),
            CancellationToken.None);

        return Task.CompletedTask;
    }

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
        var readAddress = ResolveStringReadAddress(address);
        return ExecuteReadAsync(readAddress, client => client.ReadStringAsync(readAddress, Math.Max((ushort)1, length)), cancellationToken);
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

    public Task<PlcServiceResult> WriteStringAsync(string address, string value, CancellationToken cancellationToken = default)
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
        Volatile.Write(ref _stopping, 1);
        var loopCts = Volatile.Read(ref _loopCts);
        var loopTask = Volatile.Read(ref _loopTask);

        if (loopCts is not null)
        {
            await loopCts.CancelAsync();
        }

        if (loopTask is not null)
        {
            try
            {
                await loopTask.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
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
            await CloseClientAsync(closeVendorConnection: true, cancellationToken);
            await DrainCommunicationLockAsync(cancellationToken);
        }
        else
        {
            // DI 容器释放时可能已经开始卸载依赖，手动退出只清引用即可，进程结束会回收 socket。
            ForgetClientReference();
        }

        DetachStoppedLoop(loopCts, loopTask);

        PublishForStations(
            ResolveRuntimeStationNumbers(CurrentSettings, _heartbeatAddresses),
            PlcConnectionState.Stopped,
            false,
            Text(TextKeys.Plc.MessageServiceStopped));
    }

    /// <summary>
    /// 从当前生命周期字段中摘除已取消的循环，允许重启创建新循环。
    /// 超时未结束的旧循环仍由其取消令牌阻止发布连接状态或处理迟到的 PLC 结果。
    /// </summary>
    /// <param name="loopCts">本次停止对应的循环令牌源。</param>
    /// <param name="loopTask">本次停止对应的循环任务。</param>
    private void DetachStoppedLoop(CancellationTokenSource? loopCts, Task? loopTask)
    {
        if (loopTask is not null)
        {
            Interlocked.CompareExchange(ref _loopTask, null, loopTask);
        }

        if (loopCts is null
            || !ReferenceEquals(Interlocked.CompareExchange(ref _loopCts, null, loopCts), loopCts))
        {
            return;
        }

        if (loopTask is null || loopTask.IsCompleted)
        {
            loopCts.Dispose();
            return;
        }

        // 第三方调用可能晚于有界停止返回；等待旧任务结束后再释放 CTS，避免令牌注册与释放竞态。
        _ = loopTask.ContinueWith(
            static (completedTask, state) =>
            {
                _ = completedTask.Exception;
                ((CancellationTokenSource)state!).Dispose();
            },
            loopCts,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private void DisposeCore()
    {
        if (_disposed)
        {
            return;
        }

        // 先阻止新调用进入；已有调用持锁时保留信号量，确保其最终 Release 不访问已释放对象。
        Volatile.Write(ref _disposed, true);
        _loopCts?.Dispose();
        _settingsService.SettingsChanged -= SettingsService_SettingsChanged;
        try
        {
            if (_sync.Wait(0))
            {
                _sync.Dispose();
            }
        }
        catch (ObjectDisposedException)
        {
            // A concurrent disposal has already completed semaphore cleanup.
        }
    }

    /// <summary>
    /// 后台循环：未连接时重连，已连接时执行心跳；心跳失败后关闭旧连接并等待下一轮重连。
    /// </summary>
    private async Task RunConnectionLoopAsync(
        AppSettings settings,
        IReadOnlyDictionary<int, HeartbeatAddressPair> heartbeatAddresses,
        CancellationToken cancellationToken)
    {
        var endpoint = BuildEndpoint(settings);
        var reconnectDelay = TimeSpan.Zero;
        var stationNumbers = ResolveRuntimeStationNumbers(CurrentSettings, heartbeatAddresses);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var runtimeSettings = CurrentSettings;
                endpoint = BuildEndpoint(runtimeSettings);
                var nextStationNumbers = ResolveRuntimeStationNumbers(runtimeSettings, heartbeatAddresses);
                if (!stationNumbers.SequenceEqual(nextStationNumbers))
                {
                    heartbeatAddresses = LoadHeartbeatAddresses(runtimeSettings);
                    _heartbeatAddresses = heartbeatAddresses;
                    stationNumbers = ResolveRuntimeStationNumbers(runtimeSettings, heartbeatAddresses);
                    ResetHeartbeatStates(stationNumbers);
                }

                var plcHeartbeatReadInterval = ResolvePlcHeartbeatReadInterval(runtimeSettings);
                if (_client is null)
                {
                    var endpointValidation = ValidateEndpointSettings(runtimeSettings);
                    if (!endpointValidation.IsSuccess)
                    {
                        PublishForStations(stationNumbers, PlcConnectionState.Disconnected, false, endpointValidation.Message, endpoint: endpoint);
                        await Task.Delay(ConnectionObjectModeInterval, cancellationToken);
                        continue;
                    }

                    if (reconnectDelay > TimeSpan.Zero)
                    {
                        PublishForStations(stationNumbers, PlcConnectionState.Reconnecting, false, $"PLC reconnect retry waits {reconnectDelay.TotalSeconds:0}s.", endpoint: endpoint);
                        await Task.Delay(reconnectDelay, cancellationToken);
                    }

                    PublishForStations(stationNumbers, PlcConnectionState.Reconnecting, false, Text(TextKeys.Plc.MessageConnecting, endpoint), endpoint: endpoint);

                    var connectResult = await ConnectAsync(runtimeSettings, cancellationToken);
                    if (!connectResult.IsSuccess)
                    {
                        PublishForStations(stationNumbers, PlcConnectionState.Disconnected, false, connectResult.Message, endpoint: endpoint);
                        reconnectDelay = NextReconnectDelay(reconnectDelay);
                        continue;
                    }

                    reconnectDelay = TimeSpan.Zero;
                    ResetHeartbeatStates(stationNumbers);
                }

                var disconnectedHeartbeatStations = new List<int>();
                foreach (var stationNo in stationNumbers)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var heartbeatAddress = ResolveHeartbeatAddressPair(heartbeatAddresses, stationNo);
                    var runtime = GetOrCreateHeartbeatRuntime(stationNo);
                    if (!ShouldUsePlcHeartbeat(heartbeatAddress))
                    {
                        var verificationResult = await VerifyBusinessAddressAsync(heartbeatAddress.VerificationAddress, cancellationToken);
                        if (!verificationResult.IsSuccess)
                        {
                            Publish(stationNo, PlcConnectionState.Unverified, false, verificationResult.Message, endpoint: endpoint);
                            continue;
                        }

                        PublishConnectedIfDue(stationNo, verificationResult.Message, endpoint);
                        await TryWritePcHeartbeatIfDueAsync(runtime, heartbeatAddress, DateTime.Now, cancellationToken);
                        continue;
                    }

                    var heartbeatResult = await MaintainSoftHeartbeatAsync(runtime, heartbeatAddress, cancellationToken);
                    if (heartbeatResult.ShouldDisconnect)
                    {
                        disconnectedHeartbeatStations.Add(stationNo);
                        Publish(stationNo, PlcConnectionState.Disconnected, false, heartbeatResult.Message, endpoint: endpoint);
                        continue;
                    }

                    if (heartbeatResult.IsFaulted)
                    {
                        Publish(stationNo, PlcConnectionState.Faulted, false, heartbeatResult.Message, DateTime.Now, endpoint: endpoint);
                        await TryWritePcHeartbeatIfDueAsync(runtime, heartbeatAddress, DateTime.Now, cancellationToken);
                        continue;
                    }

                    if (heartbeatResult.IsHealthy)
                    {
                        PublishConnectedIfDue(stationNo, heartbeatResult.Message, endpoint);
                    }

                    await TryWritePcHeartbeatIfDueAsync(runtime, heartbeatAddress, DateTime.Now, cancellationToken);
                }

                var heartbeatStationCount = stationNumbers.Count(stationNo => ShouldUsePlcHeartbeat(ResolveHeartbeatAddressPair(heartbeatAddresses, stationNo)));
                if (heartbeatStationCount > 0 && disconnectedHeartbeatStations.Count >= heartbeatStationCount)
                {
                    await MarkDisconnectedAsync(
                        "PLC heartbeat reads failed for all enabled stations.",
                        endpoint,
                        stationNumbers,
                        cancellationToken);
                    reconnectDelay = TimeSpan.Zero;
                    continue;
                }

                var delay = heartbeatStationCount > 0 ? plcHeartbeatReadInterval : ConnectionObjectModeInterval;
                await Task.Delay(delay, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            // 停止期间第三方调用或信号量释放产生的异常属于预期退出，不再发布故障状态。
        }
        catch (Exception ex)
        {
            PublishForStations(stationNumbers, PlcConnectionState.Faulted, false, ex.Message, endpoint: endpoint);
            WriteOperationLog("Faulted", ex.Message);
        }
    }

    /// <summary>
    /// PLC heartbeat read interval is configurable, but the service keeps a small lower bound
    /// to avoid creating a busy polling loop when the operator enters an unsafe value.
    /// </summary>
    private static TimeSpan ResolvePlcHeartbeatReadInterval(AppSettings settings)
    {
        var milliseconds = Math.Clamp(
            settings.PlcHeartbeatReadIntervalMilliseconds <= 0 ? 300 : settings.PlcHeartbeatReadIntervalMilliseconds,
            100,
            5000);

        return TimeSpan.FromMilliseconds(milliseconds);
    }

    /// <summary>
    /// 创建 HSL 客户端并建立持久连接，后续读写复用该连接。
    /// </summary>
    private async Task<PlcServiceResult> ConnectAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Volatile.Read(ref _disposed) || Volatile.Read(ref _stopping) != 0)
        {
            return PlcServiceResult.Fail(Text(TextKeys.Plc.MessageServiceStopped));
        }

        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (Volatile.Read(ref _disposed) || Volatile.Read(ref _stopping) != 0)
            {
                return PlcServiceResult.Fail(Text(TextKeys.Plc.MessageServiceStopped));
            }

            var endpointValidation = ValidateEndpointSettings(settings);
            if (!endpointValidation.IsSuccess)
            {
                return endpointValidation;
            }

            if (_client is not null)
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
            OperateResult connectResult;
            try
            {
                connectResult = await client.ConnectServerAsync();
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested)
            {
                await CloseVendorClientAsync(client, CancellationToken.None);
                throw new OperationCanceledException("PLC connection was cancelled.", ex, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested || Volatile.Read(ref _stopping) != 0)
            {
                await CloseVendorClientAsync(client, CancellationToken.None);
                cancellationToken.ThrowIfCancellationRequested();
                return PlcServiceResult.Fail(Text(TextKeys.Plc.MessageServiceStopped));
            }

            if (!connectResult.IsSuccess)
            {
                SafeCloseClient(client);
                return PlcServiceResult.Fail(connectResult.Message);
            }

            // 连接成功后才原子发布引用；StopAsync 可能并发摘除，因此发布后必须再次检查生命周期。
            var publishedClient = Interlocked.CompareExchange(ref _client, client, null);
            if (publishedClient is not null)
            {
                await CloseVendorClientAsync(client, CancellationToken.None);
                return PlcServiceResult.Success(Text(TextKeys.Plc.MessageAlreadyConnected));
            }

            if (cancellationToken.IsCancellationRequested
                || Volatile.Read(ref _stopping) != 0
                || Volatile.Read(ref _disposed))
            {
                if (ReferenceEquals(Interlocked.CompareExchange(ref _client, null, client), client))
                {
                    await CloseVendorClientAsync(client, CancellationToken.None);
                }

                cancellationToken.ThrowIfCancellationRequested();
                return PlcServiceResult.Fail(Text(TextKeys.Plc.MessageServiceStopped));
            }

            var endpoint = BuildEndpoint(settings);

            WriteOperationLog("Connected", endpoint);
            return PlcServiceResult.Success(Text(TextKeys.Plc.MessageConnected, endpoint));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("PLC connection was cancelled.", ex, cancellationToken);
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
                ConnectTimeOut = BuildPlcTimeout(),
                ReceiveTimeOut = BuildPlcTimeout()
            };

            modbus.SetPersistentConnection();
            return PlcServiceResult<NetworkDeviceBase>.Success(modbus);
        }

        if (IsPlcType(settings.PlcType, AppConstants.PlcTypes.SiemensS71200))
        {
            var siemens = new SiemensS7Net(SiemensPLCS.S1200, settings.PlcIp)
            {
                Port = settings.PlcPort,
                ConnectTimeOut = BuildPlcTimeout(),
                ReceiveTimeOut = BuildPlcTimeout()
            };

            siemens.SetPersistentConnection();
            return PlcServiceResult<NetworkDeviceBase>.Success(siemens);
        }

        return PlcServiceResult<NetworkDeviceBase>.Fail(
            Text(TextKeys.Plc.MessageUnsupportedType, settings.PlcType ?? string.Empty));
    }

    /// <summary>
    /// Validates the endpoint before creating an HSL client so empty IP values never fall back to 127.0.0.1.
    /// </summary>
    private PlcServiceResult ValidateEndpointSettings(AppSettings settings)
    {
        return string.IsNullOrWhiteSpace(settings.PlcIp) || settings.PlcPort is <= 0 or > 65535
            ? PlcServiceResult.Fail(Text(TextKeys.Plc.MessageEndpointRequired))
            : PlcServiceResult.Success();
    }

    private IReadOnlyDictionary<int, HeartbeatAddressPair> LoadHeartbeatAddresses(AppSettings settings)
    {
        var stationNumbers = ResolveRuntimeStationNumbers(settings, new Dictionary<int, HeartbeatAddressPair>());
        var addresses = new Dictionary<int, HeartbeatAddressPair>();
        try
        {
            var allAddresses = _plcAddressService.GetAll();
            foreach (var stationNo in stationNumbers)
            {
                addresses[stationNo] = new HeartbeatAddressPair(
                    FindAddress(allAddresses, AppConstants.PlcLogicalKeys.PcHeartBeat, stationNo),
                    FindAddress(allAddresses, AppConstants.PlcLogicalKeys.PlcHeartBeat, stationNo),
                    ResolveVerificationAddress(allAddresses, stationNo));
            }
        }
        catch
        {
            // Address maintenance may not be initialized yet. The monitor loop will report the missing signals.
        }

        return addresses;
    }

    /// <summary>
    /// Finds an address by logical key and station without treating disabled or blank rows as usable.
    /// </summary>
    private static BizPlcAddress? FindAddress(
        IReadOnlyList<BizPlcAddress> addresses,
        string logicalKey,
        int stationNo)
    {
        return addresses
            .Where(address => string.Equals(address.LogicalKey, logicalKey, StringComparison.OrdinalIgnoreCase))
            .Where(address => NormalizeStationNo(address.StationNo) == NormalizeStationNo(stationNo))
            .OrderBy(address => address.Sort)
            .FirstOrDefault(address => IsUsableHeartbeatAddress(address, out _));
    }

    /// <summary>
    /// Selects a readable business address used only to verify that the TCP endpoint is the expected PLC.
    /// </summary>
    private static BizPlcAddress? ResolveVerificationAddress(
        IReadOnlyList<BizPlcAddress> addresses,
        int stationNo)
    {
        foreach (var logicalKey in VerificationLogicalKeyPriority)
        {
            var address = FindAddress(addresses, logicalKey, stationNo);
            if (IsUsableHeartbeatAddress(address, out _))
            {
                return address;
            }
        }

        return null;
    }

    private async Task<HeartbeatCheckResult> MaintainSoftHeartbeatAsync(
        StationHeartbeatRuntime runtime,
        HeartbeatAddressPair heartbeatAddresses,
        CancellationToken cancellationToken)
    {
        var livenessFailureMessages = new List<string>();
        var livenessMessage = string.Empty;
        var plcReadResult = await ReadHeartbeatValueAsync(heartbeatAddresses.PlcHeartbeat, cancellationToken);
        if (!plcReadResult.IsSuccess)
        {
            livenessFailureMessages.Add(plcReadResult.Message);
        }
        else
        {
            runtime.HeartbeatFailureCount = 0;

            var now = DateTime.Now;
            var changeState = UpdatePlcHeartbeatChange(runtime, plcReadResult.Value ?? string.Empty, now);
            var unchangedDuration = now - runtime.LastPlcHeartbeatChangeTime;

            if (changeState == PlcHeartbeatChangeState.Baseline)
            {
                return HeartbeatCheckResult.Transient(
                    $"PLC heartbeat baseline value received at {runtime.LastPlcHeartbeatChangeTime:HH:mm:ss}.");
            }

            if (changeState == PlcHeartbeatChangeState.Unchanged)
            {
                if (unchangedDuration > PlcHeartbeatStaleThreshold)
                {
                    return HeartbeatCheckResult.Faulted(
                        Text(TextKeys.Plc.MessageHeartbeatNoChange, unchangedDuration.TotalSeconds));
                }

                return HeartbeatCheckResult.Transient(
                    $"PLC heartbeat waiting for change, unchanged for {unchangedDuration.TotalSeconds:0.0}s.");
            }

            livenessMessage = $"PLC heartbeat changed at {runtime.LastPlcHeartbeatChangeTime:HH:mm:ss}.";
        }

        if (livenessFailureMessages.Count > 0)
        {
            runtime.HeartbeatFailureCount++;
            if (runtime.HeartbeatFailureCount >= HeartbeatFailureThreshold)
            {
                return HeartbeatCheckResult.Disconnect(
                    $"PLC heartbeat failed {runtime.HeartbeatFailureCount} times: {string.Join("; ", livenessFailureMessages)}");
            }

            return HeartbeatCheckResult.Transient(
                $"PLC heartbeat transient failure {runtime.HeartbeatFailureCount}/{HeartbeatFailureThreshold}: {string.Join("; ", livenessFailureMessages)}");
        }

        return HeartbeatCheckResult.Healthy(livenessMessage);
    }

    private async Task<PlcServiceResult> WriteHeartbeatValueAsync(
        BizPlcAddress? heartbeatAddress,
        int value,
        CancellationToken cancellationToken)
    {
        if (!IsUsableHeartbeatAddress(heartbeatAddress, out var address))
        {
            return PlcServiceResult.Fail("PC heartbeat business address is not configured or disabled.");
        }

        var textValue = value.ToString();
        return NormalizeDataType(heartbeatAddress!.DataType) switch
        {
            AppConstants.PlcDataTypes.Bool => await ExecuteRawWriteAsync(
                address,
                client => client.WriteAsync(address, value > 0),
                cancellationToken),
            AppConstants.PlcDataTypes.Int32 => await ExecuteRawWriteAsync(
                address,
                client => client.WriteAsync(address, value),
                cancellationToken),
            AppConstants.PlcDataTypes.Float => await ExecuteRawWriteAsync(
                address,
                client => client.WriteAsync(address, Convert.ToSingle(value)),
                cancellationToken),
            AppConstants.PlcDataTypes.String => await ExecuteRawWriteAsync(
                address,
                client => client.WriteAsync(address, textValue),
                cancellationToken),
            _ => await ExecuteRawWriteAsync(
                address,
                client => client.WriteAsync(address, Convert.ToInt16(value)),
                cancellationToken)
        };
    }

    private async Task<PlcServiceResult<string>> ReadHeartbeatValueAsync(
        BizPlcAddress? heartbeatAddress,
        CancellationToken cancellationToken)
    {
        if (!IsUsableHeartbeatAddress(heartbeatAddress, out var address))
        {
            return PlcServiceResult<string>.Fail("PLC heartbeat business address is not configured or disabled.");
        }

        return await ReadConfiguredAddressValueAsync(heartbeatAddress!, address, cancellationToken);
    }

    /// <summary>
    /// Reads one configured PLC business address to prove that the TCP endpoint is the expected PLC.
    /// </summary>
    private async Task<PlcServiceResult> VerifyBusinessAddressAsync(
        BizPlcAddress? verificationAddress,
        CancellationToken cancellationToken)
    {
        if (!IsUsableHeartbeatAddress(verificationAddress, out var address))
        {
            return PlcServiceResult.Fail(Text(TextKeys.Plc.MessageVerificationAddressMissing));
        }

        var verifiedAddress = verificationAddress!;
        var readResult = await ReadConfiguredAddressValueAsync(verifiedAddress, address, cancellationToken);
        var addressLabel = $"{verifiedAddress.LogicalKey}:{address}";
        return readResult.IsSuccess
            ? PlcServiceResult.Success(Text(TextKeys.Plc.MessageBusinessVerificationSucceeded, addressLabel))
            : PlcServiceResult.Fail(Text(TextKeys.Plc.MessageBusinessVerificationFailed, $"{addressLabel} {readResult.Message}"));
    }

    /// <summary>
    /// Converts any supported PLC address value to text for heartbeat and verification checks.
    /// </summary>
    private async Task<PlcServiceResult<string>> ReadConfiguredAddressValueAsync(
        BizPlcAddress plcAddress,
        string address,
        CancellationToken cancellationToken)
    {
        return NormalizeDataType(plcAddress.DataType) switch
        {
            AppConstants.PlcDataTypes.Bool => NormalizeHeartbeatRead(await ExecuteRawReadAsync(
                address,
                client => client.ReadBoolAsync(address),
                cancellationToken)),
            AppConstants.PlcDataTypes.Int32 => NormalizeHeartbeatRead(await ExecuteRawReadAsync(
                address,
                client => client.ReadInt32Async(address),
                cancellationToken)),
            AppConstants.PlcDataTypes.Float => NormalizeHeartbeatRead(await ExecuteRawReadAsync(
                address,
                client => client.ReadFloatAsync(address),
                cancellationToken)),
            AppConstants.PlcDataTypes.String => NormalizeHeartbeatRead(await ExecuteRawReadAsync(
                ResolveStringReadAddress(address),
                client => client.ReadStringAsync(ResolveStringReadAddress(address), (ushort)Math.Max(1, plcAddress.DataLength)),
                cancellationToken)),
            _ => NormalizeHeartbeatRead(await ExecuteRawReadAsync(
                address,
                client => client.ReadInt16Async(address),
                cancellationToken))
        };
    }

    private static PlcServiceResult<string> NormalizeHeartbeatRead<T>(PlcServiceResult<T> result)
    {
        if (!result.IsSuccess)
        {
            return PlcServiceResult<string>.Fail(result.Message);
        }

        var value = result.Value switch
        {
            null => string.Empty,
            bool boolValue => boolValue ? "1" : "0",
            IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => result.Value.ToString() ?? string.Empty
        };

        return PlcServiceResult<string>.Success(value.Trim().Trim('\0'));
    }

    private PlcHeartbeatChangeState UpdatePlcHeartbeatChange(StationHeartbeatRuntime runtime, string value, DateTime now)
    {
        if (runtime.LastPlcHeartbeatValue is null)
        {
            runtime.LastPlcHeartbeatValue = value;
            runtime.LastPlcHeartbeatChangeTime = now;
            return PlcHeartbeatChangeState.Baseline;
        }

        if (!string.Equals(runtime.LastPlcHeartbeatValue, value, StringComparison.Ordinal))
        {
            runtime.LastPlcHeartbeatValue = value;
            runtime.LastPlcHeartbeatChangeTime = now;
            return PlcHeartbeatChangeState.Changed;
        }

        return PlcHeartbeatChangeState.Unchanged;
    }

    private void ResetHeartbeatStates(IEnumerable<int> stationNumbers)
    {
        foreach (var stationNo in stationNumbers.Select(NormalizeStationNo).Distinct())
        {
            GetOrCreateHeartbeatRuntime(stationNo).Reset();
        }
    }

    /// <summary>
    /// PC 心跳保持 1 秒写一次，PLC 心跳读取由主循环按 500ms 执行。
    /// </summary>
    private static bool ShouldWritePcHeartbeat(StationHeartbeatRuntime runtime, DateTime now)
    {
        return runtime.LastPcHeartbeatWriteTime == DateTime.MinValue
            || now - runtime.LastPcHeartbeatWriteTime >= PcHeartbeatWriteInterval;
    }

    /// <summary>
    /// PC 心跳只是上位机输出信号，写入失败不参与 PLC 连接状态判定。
    /// </summary>
    private async Task TryWritePcHeartbeatIfDueAsync(
        StationHeartbeatRuntime runtime,
        HeartbeatAddressPair heartbeatAddresses,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!ShouldWritePcHeartbeat(runtime, now) || !IsUsableHeartbeatAddress(heartbeatAddresses.PcHeartbeat, out _))
        {
            return;
        }

        runtime.LastPcHeartbeatWriteTime = now;
        var pcWriteResult = await WriteHeartbeatValueAsync(
            heartbeatAddresses.PcHeartbeat,
            runtime.NextPcHeartbeatValue,
            cancellationToken);
        if (pcWriteResult.IsSuccess)
        {
            runtime.NextPcHeartbeatValue = runtime.NextPcHeartbeatValue == 0 ? 1 : 0;
        }
    }

    private static bool ShouldUsePlcHeartbeat(HeartbeatAddressPair heartbeatAddresses)
    {
        return IsUsableHeartbeatAddress(heartbeatAddresses.PlcHeartbeat, out _);
    }

    private static bool IsUsableHeartbeatAddress(BizPlcAddress? heartbeatAddress, out string address)
    {
        address = heartbeatAddress?.Address?.Trim() ?? string.Empty;
        return heartbeatAddress is not null
            && heartbeatAddress.Enabled
            && !string.IsNullOrWhiteSpace(address);
    }

    /// <summary>
    /// 重连退避：断线后立即试一次，后续按 1、2、4、8、10 秒封顶重试。
    /// </summary>
    private static TimeSpan NextReconnectDelay(TimeSpan currentDelay)
    {
        if (currentDelay <= TimeSpan.Zero)
        {
            return TimeSpan.FromSeconds(1);
        }

        var nextSeconds = Math.Min(MaxReconnectDelay.TotalSeconds, currentDelay.TotalSeconds * 2);
        return TimeSpan.FromSeconds(nextSeconds);
    }

    /// <summary>
    /// 统一封装读取逻辑，业务层不用重复写连接检查、异常处理和断线标记。
    /// </summary>
    private async Task<PlcServiceResult<T>> ExecuteReadAsync<T>(string address,
        Func<NetworkDeviceBase, Task<OperateResult<T>>> action,
        CancellationToken cancellationToken)
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

        return await ExecuteRawReadAsync(address, action, cancellationToken);
    }

    /// <summary>
    /// Reads through the current TCP client without requiring business verification.
    /// This path is used only by heartbeat and connection verification.
    /// </summary>
    private async Task<PlcServiceResult<T>> ExecuteRawReadAsync<T>(
        string address,
        Func<NetworkDeviceBase, Task<OperateResult<T>>> action,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _sync.WaitAsync(cancellationToken);
        try
        {
            var client = Volatile.Read(ref _client);
            if (client is null)
            {
                return PlcServiceResult<T>.Fail(Text(TextKeys.Plc.MessageNotConnected));
            }

            var result = await action(client);
            cancellationToken.ThrowIfCancellationRequested();
            if (result.IsSuccess)
            {
                return PlcServiceResult<T>.Success(result.Content);
            }

            return PlcServiceResult<T>.Fail(result.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("PLC read was cancelled.", ex, cancellationToken);
        }
        catch (Exception ex)
        {
            return PlcServiceResult<T>.Fail(ex.Message);
        }
        finally
        {
            _sync.Release();
        }
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

        return await ExecuteRawWriteAsync(address, action, cancellationToken);
    }

    /// <summary>
    /// Writes through the current TCP client without requiring business verification.
    /// This path is used only by heartbeat maintenance.
    /// </summary>
    private async Task<PlcServiceResult> ExecuteRawWriteAsync(
        string address,
        Func<NetworkDeviceBase, Task<OperateResult>> action,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _sync.WaitAsync(cancellationToken);
        try
        {
            var client = Volatile.Read(ref _client);
            if (client is null)
            {
                return PlcServiceResult.Fail(Text(TextKeys.Plc.MessageNotConnected));
            }

            var result = await action(client);
            cancellationToken.ThrowIfCancellationRequested();
            if (result.IsSuccess)
            {
                return PlcServiceResult.Success(Text(TextKeys.Plc.MessageWriteSucceeded));
            }

            return PlcServiceResult.Fail(result.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("PLC write was cancelled.", ex, cancellationToken);
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
    /// 业务读写只检查当前连接状态，实际重连统一交给后台巡检任务处理。
    /// </summary>
    private Task<PlcServiceResult> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            return Task.FromResult(PlcServiceResult.Fail(Text(TextKeys.Plc.MessageNotConnected)));
        }

        if (HasVerifiedConnection())
        {
            return Task.FromResult(PlcServiceResult.Success(Text(TextKeys.Plc.MessageAlreadyConnected)));
        }

        var message = string.IsNullOrWhiteSpace(Current.Message)
            ? Text(TextKeys.Plc.MessageVerificationAddressMissing)
            : Current.Message;
        return Task.FromResult(PlcServiceResult.Fail(message));
    }

    /// <summary>
    /// Business reads and writes are allowed only after at least one station has passed PLC address verification.
    /// </summary>
    private bool HasVerifiedConnection()
    {
        lock (_snapshotSync)
        {
            return _stationSnapshots.Values.Any(snapshot => snapshot.IsConnected);
        }
    }

    /// <summary>
    /// 心跳健康后才对外发布已连接；已连接状态下按固定周期刷新，避免 UI 高频抖动。
    /// </summary>
    private void PublishConnectedIfDue(int stationNo, string message, string endpoint)
    {
        var now = DateTime.Now;
        var current = GetCurrent(stationNo);
        var connectedTime = current.State == PlcConnectionState.Connected && current.LastConnectedTime.HasValue
            ? current.LastConnectedTime
            : now;

        Publish(stationNo, PlcConnectionState.Connected, true, message, now, connectedTime, endpoint);
    }

    private async Task MarkDisconnectedAsync(
        string message,
        string endpoint,
        IReadOnlyList<int> stationNumbers,
        CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (Volatile.Read(ref _stopping) != 0)
            {
                return;
            }

            await MarkDisconnectedCoreAsync(message, endpoint, stationNumbers);
        }
        finally
        {
            _sync.Release();
        }
    }

    private Task MarkDisconnectedCoreAsync(string message, string endpoint, IReadOnlyList<int> stationNumbers)
    {
        CloseClientCore(closeVendorConnection: true);
        ResetHeartbeatStates(stationNumbers);
        PublishForStations(stationNumbers, PlcConnectionState.Disconnected, false, message, endpoint: endpoint);
        WriteOperationLog("Disconnected", message);
        return Task.CompletedTask;
    }

    private async Task CloseClientAsync(bool closeVendorConnection, CancellationToken cancellationToken)
    {
        var client = Interlocked.Exchange(ref _client, null);
        if (!closeVendorConnection || client is null)
        {
            return;
        }

        await CloseVendorClientAsync(client, cancellationToken);
    }

    /// <summary>
    /// 请求关闭第三方 PLC 客户端，并限制等待时间，避免 socket 清理阻塞程序退出。
    /// </summary>
    /// <param name="client">待关闭的 PLC 客户端。</param>
    /// <param name="cancellationToken">停止流程取消令牌。</param>
    private static async Task CloseVendorClientAsync(NetworkDeviceBase client, CancellationToken cancellationToken)
    {
        Task? closeTask = null;
        try
        {
            closeTask = client.ConnectCloseAsync();
            await closeTask.WaitAsync(
                TimeSpan.FromMilliseconds(PlcCommunicationTimeoutMilliseconds),
                cancellationToken);
        }
        catch
        {
            // 第三方关闭调用失败或超时时保留已摘除状态，进程退出会回收剩余 socket。
            if (closeTask is not null)
            {
                // WaitAsync 不会取消第三方任务；继续观察迟到异常，避免触发未观察任务异常。
                _ = closeTask.ContinueWith(
                    static completedTask => _ = completedTask.Exception,
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }
    }

    private void CloseClientCore(bool closeVendorConnection)
    {
        var client = Interlocked.Exchange(ref _client, null);

        if (!closeVendorConnection || client is null)
        {
            return;
        }

        SafeCloseClient(client);
    }

    private async Task DrainCommunicationLockAsync(CancellationToken cancellationToken)
    {
        var lockTaken = false;
        try
        {
            lockTaken = await _sync.WaitAsync(
                TimeSpan.FromMilliseconds(PlcCommunicationTimeoutMilliseconds),
                cancellationToken);
        }
        catch
        {
            // 关闭路径不得因残留 PLC 调用或外部取消而阻塞进程退出。
        }
        finally
        {
            if (lockTaken)
            {
                _sync.Release();
            }
        }
    }

    private void ForgetClientReference()
    {
        Interlocked.Exchange(ref _client, null);
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
    private void PublishForStations(
        IReadOnlyList<int> stationNumbers,
        PlcConnectionState state,
        bool isConnected,
        string message,
        DateTime? heartbeatTime = null,
        DateTime? connectedTime = null,
        string? endpoint = null)
    {
        foreach (var stationNo in stationNumbers)
        {
            Publish(stationNo, state, isConnected, message, heartbeatTime, connectedTime, endpoint);
        }
    }

    private void Publish(int stationNo, PlcConnectionState state, bool isConnected, string message, DateTime? heartbeatTime = null, DateTime? connectedTime = null, string? endpoint = null)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var current = GetCurrent(normalizedStationNo);
        var snapshot = new PlcConnectionSnapshot(
            state,
            isConnected,
            endpoint ?? current.Endpoint,
            connectedTime ?? current.LastConnectedTime,
            heartbeatTime ?? current.LastHeartbeatTime,
            message)
        {
            StationNo = normalizedStationNo
        };

        var shouldNotify = current.State != snapshot.State
            || current.IsConnected != snapshot.IsConnected;

        lock (_snapshotSync)
        {
            _stationSnapshots[normalizedStationNo] = snapshot;
            if (normalizedStationNo == ProductionConstants.Stations.DefaultStationNo)
            {
                Current = snapshot;
            }
        }

        if (shouldNotify)
        {
            StatusChanged?.Invoke(this, snapshot);
        }
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

    private StationHeartbeatRuntime GetOrCreateHeartbeatRuntime(int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        if (_heartbeatStates.TryGetValue(normalizedStationNo, out var runtime))
        {
            return runtime;
        }

        runtime = new StationHeartbeatRuntime();
        _heartbeatStates[normalizedStationNo] = runtime;
        return runtime;
    }

    private static HeartbeatAddressPair ResolveHeartbeatAddressPair(
        IReadOnlyDictionary<int, HeartbeatAddressPair> heartbeatAddresses,
        int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        return heartbeatAddresses.TryGetValue(normalizedStationNo, out var addressPair)
            ? addressPair
            : new HeartbeatAddressPair(null, null, null);
    }

    private static IReadOnlyList<int> ResolveRuntimeStationNumbers(
        AppSettings settings,
        IReadOnlyDictionary<int, HeartbeatAddressPair> heartbeatAddresses)
    {
        var stationNumbers = new SortedSet<int>
        {
            ProductionConstants.Stations.DefaultStationNo
        };

        if (settings.EnableDualStation)
        {
            stationNumbers.Add(2);
        }

        return stationNumbers.ToList();
    }

    private static int NormalizeStationNo(int stationNo)
    {
        return stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;
    }

    private string ResolveStringReadAddress(string address)
    {
        if (!IsPlcType(CurrentSettings.PlcType, AppConstants.PlcTypes.SiemensS71200))
        {
            return address;
        }

        try
        {
            return PlcOffsetExpression.AddByteOffset(address, 2);
        }
        catch
        {
            return address;
        }
    }

    private static bool IsPlcType(string? actual, string expected)
    {
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
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

    private static int BuildPlcTimeout()
    {
        return PlcCommunicationTimeoutMilliseconds;
    }

    /// <summary>
    /// 软心跳使用的一组业务信号地址。
    /// </summary>
    private sealed record HeartbeatAddressPair(
        BizPlcAddress? PcHeartbeat,
        BizPlcAddress? PlcHeartbeat,
        BizPlcAddress? VerificationAddress);

    /// <summary>
    /// 每个工位独立维护心跳采样状态，避免双工位共用一个运行态导致误判。
    /// </summary>
    private sealed class StationHeartbeatRuntime
    {
        public int NextPcHeartbeatValue { get; set; }

        public int HeartbeatFailureCount { get; set; }

        public string? LastPlcHeartbeatValue { get; set; }

        public DateTime LastPcHeartbeatWriteTime { get; set; } = DateTime.MinValue;

        public DateTime LastPlcHeartbeatChangeTime { get; set; } = DateTime.MinValue;

        public void Reset()
        {
            NextPcHeartbeatValue = 0;
            HeartbeatFailureCount = 0;
            LastPlcHeartbeatValue = null;
            LastPcHeartbeatWriteTime = DateTime.MinValue;
            LastPlcHeartbeatChangeTime = DateTime.MinValue;
        }
    }

    /// <summary>
    /// 区分“心跳健康”“暂时失败但继续容错”和“需要断线重连”三种内部结果。
    /// </summary>
    private sealed record HeartbeatCheckResult(bool IsHealthy, bool ShouldDisconnect, bool IsFaulted, string Message)
    {
        public static HeartbeatCheckResult Healthy(string message)
        {
            return new HeartbeatCheckResult(true, false, false, message);
        }

        public static HeartbeatCheckResult Transient(string message)
        {
            return new HeartbeatCheckResult(false, false, false, message);
        }

        public static HeartbeatCheckResult Faulted(string message)
        {
            return new HeartbeatCheckResult(false, false, true, message);
        }

        public static HeartbeatCheckResult Disconnect(string message)
        {
            return new HeartbeatCheckResult(false, true, false, message);
        }
    }

    private enum PlcHeartbeatChangeState
    {
        Baseline,
        Changed,
        Unchanged
    }
}
