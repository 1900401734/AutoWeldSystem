using AutoWeldSystem.CenterServer.Hubs;
using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Data;
using Microsoft.AspNetCore.SignalR;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 接收设备端产品/完工请求，协调文件存储、看板数据库快照和实时通知。
/// XLSX 的路径、状态合并和原子保存由 <see cref="CenterProductReportFileStore"/> 单独负责。
/// </summary>
public sealed class CenterProductReportIngestService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IHubContext<CenterDashboardHub> _hubContext;
    private readonly CenterServerSettingsService _settingsService;
    private readonly CenterDashboardChangeNotifier _changeNotifier;
    private readonly CenterProductReportFileStore _fileStore;
    private readonly object _dbLock = new();

    public CenterProductReportIngestService(
        SqlSugarDbContext dbContext,
        IHubContext<CenterDashboardHub> hubContext,
        CenterServerSettingsService settingsService,
        CenterDashboardChangeNotifier changeNotifier,
        CenterProductReportFileStore fileStore)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _settingsService = settingsService;
        _changeNotifier = changeNotifier;
        _fileStore = fileStore;
    }

    /// <summary>
    /// 保存一个完成产品，或只推进同一工单的完工任务状态。
    /// </summary>
    public async Task<CenterTelemetryAck> IngestAsync(
        CenterProductReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var deviceId = request.DeviceId.Trim();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return Fail("DeviceId is required.");
        }

        if (request.StationNo <= 0)
        {
            return Fail("StationNo is required.");
        }

        if (string.IsNullOrWhiteSpace(request.WorkOrder))
        {
            return Fail("WorkOrder is required.");
        }

        if (request.IsTaskFinishUpdate && request.EndTime is null)
        {
            return Fail("EndTime is required for task finish updates.");
        }

        if (!request.IsTaskFinishUpdate && request.Points.Count == 0)
        {
            return Fail("Product report points are required.");
        }

        var settings = _settingsService.Get();
        var reportPath = _fileStore.Upsert(settings.DataDirectory, request);

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            UpsertDeviceNode(deviceId, request);
            if (!request.IsTaskFinishUpdate)
            {
                RefreshStationCounts(settings.DataDirectory, deviceId, request);
            }
        }

        _changeNotifier.Notify(deviceId);
        await _hubContext.Clients.All.SendAsync("CenterDashboardChanged", deviceId, cancellationToken);

        return new CenterTelemetryAck
        {
            Success = true,
            Message = $"Accepted, report={reportPath}",
            ServerTime = DateTime.Now
        };
    }

    private static CenterTelemetryAck Fail(string message)
    {
        return new CenterTelemetryAck
        {
            Success = false,
            Message = message,
            ServerTime = DateTime.Now
        };
    }

    /// <summary>
    /// 产品上传也可以首次登记设备节点，保证新设备能出现在看板中。
    /// </summary>
    private void UpsertDeviceNode(string deviceId, CenterProductReportRequest request)
    {
        var now = DateTime.Now;
        var node = _dbContext.Db.Queryable<CenterDeviceNode>().InSingle(deviceId);
        if (node is null)
        {
            _dbContext.Db.Insertable(new CenterDeviceNode
            {
                DeviceId = deviceId,
                DeviceName = request.DeviceName.Trim(),
                SystemType = CenterTelemetryRules.NormalizeSystemType(request.SystemType),
                FirstSeenAt = now,
                LastSeenAt = now
            }).ExecuteCommand();
            return;
        }

        node.DeviceName = request.DeviceName.Trim();
        node.SystemType = CenterTelemetryRules.NormalizeSystemType(request.SystemType);
        node.LastSeenAt = now;
        _dbContext.Db.Updateable(node).ExecuteCommand();
    }

    /// <summary>
    /// 产品完成后从中心 XLSX 重算当天设备工位计数。
    /// </summary>
    private void RefreshStationCounts(
        string dataDirectory,
        string deviceId,
        CenterProductReportRequest request)
    {
        var products = _fileStore.LoadProducts(dataDirectory, deviceId, request.StationNo, DateTime.Today)
            .GroupBy(row => $"{row.WorkOrder}\u001F{row.ProductNo}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var snapshot = _dbContext.Db.Queryable<CenterDeviceStationRuntimeSnapshot>()
            .First(item => item.DeviceId == deviceId && item.StationNo == request.StationNo);
        snapshot ??= new CenterDeviceStationRuntimeSnapshot
        {
            DeviceId = deviceId,
            StationNo = request.StationNo
        };

        snapshot.CurrentWorkOrder = request.WorkOrder.Trim();
        snapshot.ProductJobNo = request.ProductJobNo.Trim();
        snapshot.ProductModel = request.ProductModel.Trim();
        snapshot.TodayTotalCount = products.Count;
        snapshot.TodayQualifiedCount = products.Count(IsProductOk);
        snapshot.TodayFailedCount = products.Count(item => !IsProductOk(item));
        snapshot.CollectedAt = DateTime.Now;
        snapshot.UpdatedAt = DateTime.Now;

        if (snapshot.Id > 0)
        {
            _dbContext.Db.Updateable(snapshot).ExecuteCommand();
        }
        else
        {
            _dbContext.Db.Insertable(snapshot).ExecuteCommand();
        }
    }

    private static bool IsProductOk(CenterProductReportProductSummary row)
        => string.Equals(row.ProductResult, ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase);
}
