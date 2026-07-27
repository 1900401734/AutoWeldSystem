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

    // 变更驱动推送：内容签名未变化时改推轻量心跳，避免看板日志被全量遥测刷屏。
    private string? _lastUploadedSignature;
    private DateTime _lastFullUploadAt = DateTime.MinValue;

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

        try
        {
            var request = BuildRequest(settings);
            var signature = CenterTelemetryRules.BuildSnapshotSignature(request);
            var needFullUpload = !string.Equals(signature, _lastUploadedSignature, StringComparison.Ordinal)
                || DateTime.Now - _lastFullUploadAt >= TimeSpan.FromSeconds(CenterServerConstants.TelemetryFullRefreshIntervalSeconds);

            CenterTelemetryAck response;
            if (needFullUpload)
            {
                response = await _client.UploadAsync(settings, request, cancellationToken);
                if (response.Success)
                {
                    _lastUploadedSignature = signature;
                    _lastFullUploadAt = DateTime.Now;
                }
            }
            else
            {
                // 内容未变化：改推空工位心跳保活，服务器侧只刷新在线时间、不动工位快照。
                response = await _client.UploadHeartbeatAsync(settings, BuildHeartbeatRequest(settings), cancellationToken);
            }

            if (!response.Success)
            {
                throw new InvalidOperationException(response.Message);
            }

            Publish(true, string.IsNullOrWhiteSpace(response.Message) ? "Center telemetry uploaded." : response.Message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 推送失败后清空签名：恢复连接的下一周期强制全量，兜底服务器重启或看板删除设备后的数据缺失。
            _lastUploadedSignature = null;
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
                WriteFailureLog(ex);
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
                .Select(BuildStationSnapshot)
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
    private CenterTelemetryStationSnapshot BuildStationSnapshot(int stationNo)
    {
        var connection = _plcCommunicationService.Current;
        var production = _productionMonitorService.GetCurrent(stationNo);
        var stationStatus = _deviceStatusService.GetLatestStatus(stationNo);
        var summary = GetTodayProductionSummary(stationNo);
        var plcStatusCode = ResolvePlcStatusCode(production);
        var latestStatus = plcStatusCode is null
            ? CenterTelemetryRules.ResolveLatestDeviceStatus(
                stationStatus,
                _deviceStatusService.GetLatestStatus(ProductionConstants.Stations.SharedStationNo))
            : stationStatus;
        var statusCode = plcStatusCode ?? latestStatus?.DeviceStatus ?? string.Empty;
        var counts = ResolveProductionCounts(production, summary);

        return new CenterTelemetryStationSnapshot
        {
            StationNo = stationNo,
            PlcConnected = connection.IsConnected,
            PlcConnectionState = connection.State.ToString(),
            DeviceStatusCode = statusCode,
            DeviceStatusName = CenterTelemetryRules.ResolveReportedStatusName(
                statusCode,
                plcStatusCode is null
                    ? FirstNonEmpty(latestStatus?.StatusName, DeviceStatusReportRules.GetStatusName(statusCode))
                    : null),
            AlarmMessage = FirstNonEmpty(production.AlarmMessage, stationStatus?.Remark),
            CurrentWorkOrder = summary.CurrentWorkOrder,
            ProductJobNo = summary.ProductJobNo,
            ProductModel = summary.ProductModel,
            TodayTotalCount = counts.Total,
            TodayQualifiedCount = counts.Qualified,
            TodayFailedCount = counts.Failed,
            CollectedAt = DateTime.Now
        };
    }

    private TodayProductionSummary GetTodayProductionSummary(int stationNo)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var today = DateTime.Today;
            var tasks = _dbContext.Db.Queryable<BizWeldTask>()
                .Where(it => it.StartTime >= today && it.StationNo == stationNo)
                .ToList();
            var active = tasks
                .Where(it => it.EndTime == null)
                .OrderByDescending(it => it.StartTime)
                .FirstOrDefault();

            return new TodayProductionSummary(
                tasks.Sum(it => Math.Max(0, it.ActualQty)),
                tasks.Sum(it => Math.Max(0, it.QualifiedQty)),
                tasks.Sum(it => Math.Max(0, it.FailedQty)),
                active?.SN ?? string.Empty,
                active?.ProductNum ?? string.Empty,
                active?.ProductModel ?? string.Empty);
        }
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
    /// Uses PLC production quantities first, then falls back to local task totals when PLC quantity reads failed.
    /// </summary>
    private static ProductionCounts ResolveProductionCounts(PlcProductionSnapshot production, TodayProductionSummary summary)
    {
        if (production.ProductionQuantitiesReadSuccess)
        {
            return new ProductionCounts(
                Math.Max(0, production.TotalProduction),
                Math.Max(0, production.AcceptedQuantity),
                Math.Max(0, production.RejectedQuantity));
        }

        return new ProductionCounts(summary.Total, summary.Qualified, summary.Failed);
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

    private sealed record TodayProductionSummary(
        int Total,
        int Qualified,
        int Failed,
        string CurrentWorkOrder,
        string ProductJobNo,
        string ProductModel);

    private sealed record ProductionCounts(int Total, int Qualified, int Failed);
}
