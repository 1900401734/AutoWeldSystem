using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.ViewModels;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Center;

/// <summary>
/// Periodically uploads local device runtime and production summary to the center server.
/// </summary>
public sealed class CenterTelemetrySyncService : ICenterTelemetrySyncService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IDeviceStatusService _deviceStatusService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IPlcProductionMonitorService _productionMonitorService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly CenterTelemetryClient _client;
    private readonly object _dbLock = new();

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private DateTime _lastFailureLogTime = DateTime.MinValue;

    // 仅记录最后一次成功同步的内容签名；心跳失败不能清空，否则恢复后会误推未变化的遥测。
    private string? _lastUploadedSignature;

    public CenterTelemetrySyncService(
        SqlSugarDbContext dbContext,
        IAppSettingsService settingsService,
        IDeviceStatusService deviceStatusService,
        IPlcCommunicationService plcCommunicationService,
        IPlcProductionMonitorService productionMonitorService,
        IProgramExceptionLogService exceptionLogService,
        CenterTelemetryClient client)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _deviceStatusService = deviceStatusService;
        _plcCommunicationService = plcCommunicationService;
        _productionMonitorService = productionMonitorService;
        _exceptionLogService = exceptionLogService;
        _client = client;
        Current = new CenterTelemetryConnectionSnapshot(false, default, "Center telemetry has not been pushed yet.");
    }

    public event EventHandler<CenterTelemetryConnectionSnapshot>? StatusChanged;

    public CenterTelemetryConnectionSnapshot Current { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_loopTask is { IsCompleted: false })
        {
            return Task.CompletedTask;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = Task.Run(() => RunAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }

        if (_loopTask is not null)
        {
            try
            {
                await _loopTask.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
            }
            catch
            {
                // Shutdown must not block the WinForms process from exiting.
            }
        }

        _cts?.Dispose();
        _cts = null;
        _loopTask = null;
    }

    public async Task PushOnceAsync(CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.Get();
        if (!settings.EnableCenterServerSync || string.IsNullOrWhiteSpace(settings.DeviceId))
        {
            return;
        }

        var request = BuildRequest(settings);
        await PushRequestAsync(settings, request, cancellationToken);
    }

    /// <summary>
    /// 先用心跳确认中心服务器可达，再按最后成功签名决定是否上传完整遥测。
    /// 断线期间只保留最新快照；恢复后若内容确有变化，再补发一次最新值。
    /// </summary>
    internal async Task PushRequestAsync(
        AppSettings settings,
        CenterTelemetrySnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var signature = CenterTelemetryRules.BuildSnapshotSignature(request);
            var heartbeatResponse = await _client.UploadHeartbeatAsync(
                settings,
                BuildHeartbeatRequest(settings),
                cancellationToken);
            if (!heartbeatResponse.Success)
            {
                var message = string.IsNullOrWhiteSpace(heartbeatResponse.Message)
                    ? "Center heartbeat rejected."
                    : heartbeatResponse.Message;
                // 已收到中心应答，属于中心交互业务失败；只保留服务器日志，不升级为程序异常。
                Publish(true, $"Connected; heartbeat failed: {message}");
                return;
            }

            Publish(true, string.IsNullOrWhiteSpace(heartbeatResponse.Message) ? "Connected" : heartbeatResponse.Message);

            if (string.Equals(signature, _lastUploadedSignature, StringComparison.Ordinal))
            {
                return;
            }

            var telemetryResponse = await _client.UploadAsync(settings, request, cancellationToken);
            if (!telemetryResponse.Success)
            {
                // 服务端已经应答，说明连接仍然正常；保留旧签名以便下一周期继续重试最新快照。
                var message = string.IsNullOrWhiteSpace(telemetryResponse.Message)
                    ? "Center telemetry sync failed."
                    : telemetryResponse.Message;
                Publish(true, $"Connected; telemetry sync failed: {message}");
                return;
            }

            _lastUploadedSignature = signature;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Publish(false, ex.Message);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var settings = _settingsService.Get();
            try
            {
                if (settings.EnableCenterServerSync)
                {
                    await PushOnceAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // 连接类异常已由共享客户端按首次/十分钟摘要记录，避免两个后台服务重复刷程序异常。
                if (!CenterServerAvailabilityLogGate.IsConnectivityFailure(ex, cancellationToken))
                {
                    WriteFailureLog(ex);
                }
            }

            var delay = CenterTelemetryRules.NormalizeHeartbeatIntervalSeconds(
                settings.CenterServerHeartbeatIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
        }
    }

    private CenterTelemetrySnapshotRequest BuildRequest(AppSettings settings)
    {
        return new CenterTelemetrySnapshotRequest
        {
            DeviceId = settings.DeviceId.Trim(),
            DeviceName = settings.DeviceName.Trim(),
            SystemType = CenterTelemetryRules.NormalizeSystemType(settings.CenterServerSystemType),
            HeartbeatAt = DateTime.Now,
            Stations = ResolveStationNumbers(settings)
                .Select(stationNo => BuildStationSnapshot(stationNo, settings))
                .ToList()
        };
    }

    /// <summary>
    /// 构建不带工位数据的保活心跳请求，服务器侧只刷新设备在线时间。
    /// </summary>
    private static CenterTelemetrySnapshotRequest BuildHeartbeatRequest(AppSettings settings)
    {
        return new CenterTelemetrySnapshotRequest
        {
            DeviceId = settings.DeviceId.Trim(),
            DeviceName = settings.DeviceName.Trim(),
            SystemType = CenterTelemetryRules.NormalizeSystemType(settings.CenterServerSystemType),
            HeartbeatAt = DateTime.Now
        };
    }

    /// <summary>
    /// Builds one station snapshot from the latest PLC monitor value and device-status JSONL fallback.
    /// </summary>
    private CenterTelemetryStationSnapshot BuildStationSnapshot(int stationNo, AppSettings settings)
    {
        var connection = _plcCommunicationService.Current;
        var production = _productionMonitorService.GetCurrent(stationNo);
        var stationStatus = _deviceStatusService.GetLatestStatus(stationNo);
        var sharedStatus = _deviceStatusService.GetLatestStatus(ProductionConstants.Stations.SharedStationNo);
        var productionDate = DateTime.Today;
        var summary = GetTodayProductionSummary(stationNo, productionDate);
        var task = GetActiveTask(stationNo, settings);
        var taskCounts = GetTaskCounts(task, settings);

        var snapshot = new CenterTelemetryStationSnapshot
        {
            StationNo = stationNo,
            PlcConnected = connection.IsConnected,
            PlcConnectionState = connection.State.ToString(),
            CurrentWorkOrder = task?.SN ?? string.Empty,
            ProductJobNo = task?.ProductNum ?? string.Empty,
            ProductModel = task?.ProductModel ?? string.Empty,
            TodayTotalCount = summary.ActualQty,
            TodayQualifiedCount = summary.QualifiedQty,
            TodayFailedCount = summary.FailedQty,
            WorkOrderQuantity = Math.Max(0, task?.StartAmount ?? 0),
            ProductionDate = productionDate,
            TaskKey = task?.LocalExpStartId,
            ProgramName = task?.ProgramName,
            StationName = settings.EnableDualStation ? (stationNo == 2 ? settings.Station2DisplayName : settings.Station1DisplayName) : null,
            TaskTotalCount = taskCounts?.ActualQty,
            TaskQualifiedCount = taskCounts?.QualifiedQty,
            TaskFailedCount = taskCounts?.FailedQty,
            CollectedAt = DateTime.Now
        };
        ApplyStatusSnapshot(snapshot, production, stationStatus, sharedStatus);
        return snapshot;
    }

    /// <summary>纯投影供实际遥测和回归共用，原始状态与有效报警分别传输。</summary>
    internal static void ApplyStatusSnapshot(
        CenterTelemetryStationSnapshot snapshot,
        PlcProductionSnapshot production,
        BizDeviceStatusLog? stationStatus,
        BizDeviceStatusLog? sharedStatus)
    {
        var plcStatusCode = ResolvePlcStatusCode(production);
        var latestStatus = CenterTelemetryRules.ResolveLatestDeviceStatus(stationStatus, sharedStatus);
        var statusCode = plcStatusCode ?? latestStatus?.DeviceStatus ?? string.Empty;
        snapshot.DeviceStatusCode = statusCode;
        snapshot.DeviceStatusName = CenterTelemetryRules.ResolveReportedStatusName(
            statusCode,
            plcStatusCode is null
                ? FirstNonEmpty(latestStatus?.StatusName, DeviceStatusReportRules.GetStatusName(statusCode))
                : null);
        snapshot.StatusSource = plcStatusCode is null ? "Lifecycle" : "PLC";
        snapshot.AlarmMessage = CenterTelemetryRules.ResolveAlarmMessage(production.AlarmMessage, latestStatus);
        snapshot.EffectiveAlarm = CenterAlarmRules.FromProductionSnapshot(production);
    }

    /// <summary>按产品最后完成日期计数，不用 PLC 当前任务累计数冒充今日产量。</summary>
    private FinishQuantities GetTodayProductionSummary(int stationNo, DateTime productionDate)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var tomorrow = productionDate.AddDays(1);
            // 只读取完成标记行，减少每次心跳搬运整天的逐点原始 JSON。
            var records = _dbContext.Db.Queryable<BizWeldPointRecord>()
                .Where(record => record.StationNo == stationNo && record.ProductCompleted
                    && record.Ts >= productionDate && record.Ts < tomorrow)
                .Select(record => new BizWeldPointRecord
                {
                    TaskId = record.TaskId,
                    StationNo = record.StationNo,
                    ProductNo = record.ProductNo,
                    ProductCompleted = record.ProductCompleted,
                    IsDeleted = record.IsDeleted,
                    Ts = record.Ts,
                    ProductResult = record.ProductResult,
                    RawDataJson = record.ProductResult == null || record.ProductResult == "" ? record.RawDataJson : null
                })
                .ToList();
            return CenterProductionSummaryRules.ForDay(records, stationNo, productionDate);
        }
    }

    private BizWeldTask? GetActiveTask(int stationNo, AppSettings settings)
    {
        var stations = RecipeStationScopeRules.ResolveSharedTaskStations(settings.EnableDualStation, settings.EnableDualWorkOrder, stationNo);
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            return CenterProductionSummaryRules.ActiveTask(_dbContext.Db.Queryable<BizWeldTask>()
                .Where(task => task.EndTime == null && stations.Contains(task.StationNo)).ToList());
        }
    }

    private FinishQuantities? GetTaskCounts(BizWeldTask? task, AppSettings settings)
    {
        if (task is null) return null;
        if (ProductionConstants.ProductionCountSources.IsProgram(settings.ProductionCountSource))
        {
            return new Production.ProductionCountService(_dbContext).GetTaskQuantities(task.Id);
        }
        // 共享任务按任务范围累加各工位计数；DTO 中每个工位携带同一份任务总数，由任务键去重显示。
        var stations = RecipeStationScopeRules.ResolveSharedTaskStations(settings.EnableDualStation, settings.EnableDualWorkOrder, task.StationNo);
        var snapshots = stations.Select(station => _productionMonitorService.GetCurrent(station)).ToList();
        if (snapshots.Any(snapshot => !snapshot.ProductionQuantitiesReadSuccess)) return null;
        return new FinishQuantities(snapshots.Sum(snapshot => Math.Max(0, snapshot.TotalProduction)),
            snapshots.Sum(snapshot => Math.Max(0, snapshot.AcceptedQuantity)), snapshots.Sum(snapshot => Math.Max(0, snapshot.RejectedQuantity)));
    }

    private void WriteFailureLog(Exception ex)
    {
        if (DateTime.Now - _lastFailureLogTime < TimeSpan.FromMinutes(1))
        {
            return;
        }

        _lastFailureLogTime = DateTime.Now;
        _exceptionLogService.Write(ex, "CenterTelemetrySyncService.Push");
    }

    private void Publish(bool isConnected, string message)
    {
        var snapshot = new CenterTelemetryConnectionSnapshot(
            isConnected,
            DateTime.Now,
            string.IsNullOrWhiteSpace(message) ? (isConnected ? "Connected" : "Disconnected") : message.Trim());
        Current = snapshot;
        StatusChanged?.Invoke(this, snapshot);
    }

    private static string? ResolvePlcStatusCode(PlcProductionSnapshot production)
    {
        if (production.DeviceStatusCode.HasValue
            && ProductionConstants.PlcDeviceStatuses.IsReportable(production.DeviceStatusCode.Value))
        {
            return production.DeviceStatusCode.Value.ToString();
        }

        return null;
    }

    /// <summary>
    /// Returns the station numbers that the current device should report to the center server.
    /// </summary>
    private static IEnumerable<int> ResolveStationNumbers(AppSettings settings)
    {
        yield return ProductionConstants.Stations.DefaultStationNo;

        if (settings.EnableDualStation)
        {
            yield return 2;
        }
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
}
