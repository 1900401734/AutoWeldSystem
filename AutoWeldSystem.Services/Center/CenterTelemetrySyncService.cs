using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.PLC;
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
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IPlcProductionMonitorService _productionMonitorService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly CenterTelemetryClient _client;
    private readonly object _dbLock = new();

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private DateTime _lastFailureLogTime = DateTime.MinValue;

    public CenterTelemetrySyncService(
        SqlSugarDbContext dbContext,
        IAppSettingsService settingsService,
        IPlcCommunicationService plcCommunicationService,
        IPlcProductionMonitorService productionMonitorService,
        IProgramExceptionLogService exceptionLogService,
        CenterTelemetryClient client)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
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
            var response = await _client.UploadAsync(settings, request, cancellationToken);
            if (!response.Success)
            {
                Publish(false, response.Message);
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
    /// Builds one station snapshot from the latest PLC monitor value and local task fallback.
    /// </summary>
    private CenterTelemetryStationSnapshot BuildStationSnapshot(int stationNo)
    {
        var connection = _plcCommunicationService.Current;
        var production = _productionMonitorService.GetCurrent(stationNo);
        var latestStatus = GetLatestDeviceStatus(stationNo);
        var summary = GetTodayProductionSummary(stationNo);
        var statusCode = ResolvePlcStatusCode(production, latestStatus);
        var counts = ResolveProductionCounts(production, summary);

        return new CenterTelemetryStationSnapshot
        {
            StationNo = stationNo,
            PlcConnected = connection.IsConnected,
            PlcConnectionState = connection.State.ToString(),
            DeviceStatusCode = statusCode,
            DeviceStatusName = CenterTelemetryRules.ResolvePlcStatusName(statusCode, latestStatus?.StatusName),
            AlarmMessage = FirstNonEmpty(production.AlarmMessage, latestStatus?.Remark),
            CurrentWorkOrder = summary.CurrentWorkOrder,
            ProductJobNo = summary.ProductJobNo,
            ProductModel = summary.ProductModel,
            TodayTotalCount = counts.Total,
            TodayQualifiedCount = counts.Qualified,
            TodayFailedCount = counts.Failed,
            CollectedAt = DateTime.Now
        };
    }

    private BizDeviceStatusLog? GetLatestDeviceStatus(int stationNo)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            return _dbContext.Db.Queryable<BizDeviceStatusLog>()
                .Where(it => it.StationNo == stationNo)
                .OrderByDescending(it => it.OccurredTime)
                .First();
        }
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

    private static string ResolvePlcStatusCode(PlcProductionSnapshot production, BizDeviceStatusLog? latestStatus)
    {
        if (production.DeviceStatusCode.HasValue
            && ProductionConstants.PlcDeviceStatuses.IsReportable(production.DeviceStatusCode.Value))
        {
            return production.DeviceStatusCode.Value.ToString();
        }

        return latestStatus?.DeviceStatus ?? string.Empty;
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
