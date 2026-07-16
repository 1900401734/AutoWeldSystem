using AutoWeldSystem.CenterServer.Hubs;
using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Data;
using Microsoft.AspNetCore.SignalR;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 定义中心产品报表文件写入成功后必须执行的生产副作用。
/// </summary>
public interface ICenterProductReportIngestSideEffects
{
    /// <summary>
    /// 更新数据库看板快照，并向本机通知器和 SignalR 客户端发布变更。
    /// </summary>
    Task ApplyAsync(
        string dataDirectory,
        string deviceId,
        CenterProductReportRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 负责中心产品报表 ingest 成功后的 SqlSugar、统计和实时通知副作用。
/// </summary>
public sealed class CenterProductReportIngestSideEffects : ICenterProductReportIngestSideEffects
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IHubContext<CenterDashboardHub> _hubContext;
    private readonly CenterDashboardChangeNotifier _changeNotifier;
    private readonly CenterProductReportFileStore _fileStore;
    private readonly object _dbLock = new();

    public CenterProductReportIngestSideEffects(
        SqlSugarDbContext dbContext,
        IHubContext<CenterDashboardHub> hubContext,
        CenterDashboardChangeNotifier changeNotifier,
        CenterProductReportFileStore fileStore)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _changeNotifier = changeNotifier;
        _fileStore = fileStore;
    }

    /// <inheritdoc />
    public async Task ApplyAsync(
        string dataDirectory,
        string deviceId,
        CenterProductReportRequest request,
        CancellationToken cancellationToken = default)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            UpsertDeviceNode(deviceId, request);
            if (!request.IsTaskFinishUpdate)
            {
                RefreshStationCounts(dataDirectory, deviceId, request);
            }
        }

        _changeNotifier.Notify(deviceId);
        await _hubContext.Clients.All.SendAsync("CenterDashboardChanged", deviceId, cancellationToken);
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
    /// 产品完成后从中心正式 XLSX 重算当天设备工位计数。
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
