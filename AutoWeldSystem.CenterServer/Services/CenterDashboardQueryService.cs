using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// Builds center dashboard snapshots from the latest stored device and station runtime data.
/// </summary>
public sealed class CenterDashboardQueryService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly CenterServerSettingsService _settingsService;
    private readonly object _dbLock = new();
    private readonly bool _demo;
    private readonly ILogger<CenterDashboardQueryService>? _logger;

    public CenterDashboardQueryService(SqlSugarDbContext dbContext, CenterServerSettingsService settingsService,
        IConfiguration? configuration = null, ILogger<CenterDashboardQueryService>? logger = null)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _demo = configuration?.GetValue<bool>("DashboardDemo") == true;
        _logger = logger;
    }

    /// <summary>
    /// Returns every dynamically registered device and its station snapshots.
    /// </summary>
    public CenterDashboardSnapshotDto GetSnapshot(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_demo)
        {
            return CenterDashboardDemoData.Create();
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var nodes = _dbContext.Db.Queryable<CenterDeviceNode>()
                .OrderBy(it => it.SystemType)
                .OrderBy(it => it.DeviceName)
                .ToList();
            var runtimes = _dbContext.Db.Queryable<CenterDeviceRuntimeSnapshot>()
                .ToList()
                .ToDictionary(it => it.DeviceId, StringComparer.OrdinalIgnoreCase);
            var stationGroups = _dbContext.Db.Queryable<CenterDeviceStationRuntimeSnapshot>()
                .ToList()
                .GroupBy(it => it.DeviceId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.OrderBy(it => it.StationNo).ToList(), StringComparer.OrdinalIgnoreCase);
            var timeoutSeconds = _settingsService.Get().OfflineTimeoutSeconds;

            return new CenterDashboardSnapshotDto
            {
                GeneratedAt = DateTime.Now,
                Devices = nodes.Select(node => BuildDevice(node, runtimes, stationGroups, timeoutSeconds)).ToList()
            };
        }
    }

    /// <summary>
    /// Builds one device card DTO. Station status is kept separate from client online state.
    /// </summary>
    private CenterDashboardDeviceDto BuildDevice(
        CenterDeviceNode node,
        IReadOnlyDictionary<string, CenterDeviceRuntimeSnapshot> runtimes,
        IReadOnlyDictionary<string, List<CenterDeviceStationRuntimeSnapshot>> stationGroups,
        int timeoutSeconds)
    {
        runtimes.TryGetValue(node.DeviceId, out var runtime);
        stationGroups.TryGetValue(node.DeviceId, out var stations);

        var deviceState = new CenterDashboardDeviceStateDto
        {
            ClientOnline = CenterTelemetryRules.IsClientOnline(runtime?.LastSeenAt ?? node.LastSeenAt, DateTime.Now, timeoutSeconds),
            LastSeenAt = runtime?.LastSeenAt ?? node.LastSeenAt,
            CollectedAt = runtime?.HeartbeatAt ?? node.LastSeenAt
        };

        return new CenterDashboardDeviceDto
        {
            DeviceId = node.DeviceId,
            DeviceName = FirstNonEmpty(runtime?.DeviceName, node.DeviceName),
            SystemType = FirstNonEmpty(runtime?.SystemType, node.SystemType),
            State = deviceState,
            ReportSync = ReadReportSync(runtime?.ReportSyncJson),
            Stations = (stations ?? new List<CenterDeviceStationRuntimeSnapshot>())
                .Select(station => BuildStation(station, deviceState, timeoutSeconds,
                    ex => _logger?.LogWarning(ex, "有效报警数据损坏，回退旧版状态。DeviceId={DeviceId}, Station={StationNo}", node.DeviceId, station.StationNo)))
                .ToList()
        };
    }

    /// <summary>
    /// Projects a stored station snapshot into the dashboard station DTO.
    /// </summary>
    internal static CenterDashboardStationDto BuildStation(
        CenterDeviceStationRuntimeSnapshot station,
        CenterDashboardDeviceStateDto deviceState,
        int timeoutSeconds,
        Action<Exception>? onAlarmError = null)
    {
        var runtime = new CenterDeviceRuntimeDto
        {
            PlcConnected = station.PlcConnected,
            PlcConnectionState = station.PlcConnectionState,
            PlcDeviceStatusCode = station.DeviceStatusCode,
            PlcDeviceStatusName = station.DeviceStatusName,
            AlarmMessage = station.AlarmMessage,
            LastSeenAt = deviceState.LastSeenAt,
            CollectedAt = station.CollectedAt
        };

        return new CenterDashboardStationDto
        {
            StationNo = station.StationNo,
            State = CenterTelemetryRules.BuildDashboardState(runtime, DateTime.Now, timeoutSeconds),
            CurrentWorkOrder = station.CurrentWorkOrder,
            ProductJobNo = station.ProductJobNo,
            ProductModel = station.ProductModel,
            TodayTotalCount = station.TodayTotalCount,
            TodayQualifiedCount = station.TodayQualifiedCount,
            TodayFailedCount = station.TodayFailedCount,
            WorkOrderQuantity = station.WorkOrderQuantity,
            ProductionDate = station.ProductionDate,
            TaskKey = station.TaskKey,
            ProgramName = station.ProgramName,
            StationName = station.StationName,
            StatusSource = station.StatusSource,
            EffectiveAlarm = CenterAlarmRules.Deserialize(station.EffectiveAlarmJson, onAlarmError),
            TaskTotalCount = station.TaskTotalCount,
            TaskQualifiedCount = station.TaskQualifiedCount,
            TaskFailedCount = station.TaskFailedCount
        };
    }

    private static CenterReportSyncSummaryDto? ReadReportSync(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return System.Text.Json.JsonSerializer.Deserialize<CenterReportSyncSummaryDto>(json); }
        catch (System.Text.Json.JsonException) { return null; }
    }

    private static string FirstNonEmpty(string? preferred, string? fallback)
        => string.IsNullOrWhiteSpace(preferred) ? fallback?.Trim() ?? string.Empty : preferred.Trim();
}
