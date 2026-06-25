using AutoWeldSystem.CenterServer.Hubs;
using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Data;
using Microsoft.AspNetCore.SignalR;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// Stores telemetry snapshots uploaded by equipment clients.
/// </summary>
public sealed class CenterTelemetryIngestService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IHubContext<CenterDashboardHub> _hubContext;
    private readonly CenterDashboardChangeNotifier _changeNotifier;
    private readonly object _dbLock = new();

    public CenterTelemetryIngestService(
        SqlSugarDbContext dbContext,
        IHubContext<CenterDashboardHub> hubContext,
        CenterDashboardChangeNotifier changeNotifier)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _changeNotifier = changeNotifier;
    }

    /// <summary>
    /// Upserts a device node and its latest runtime snapshot by DeviceId.
    /// </summary>
    public async Task<CenterTelemetryAck> IngestAsync(
        CenterTelemetrySnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        var deviceId = CenterTelemetryRules.ResolveDeviceKey(request);
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return new CenterTelemetryAck
            {
                Success = false,
                Message = "DeviceId is required.",
                ServerTime = DateTime.Now
            };
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            UpsertDeviceNode(deviceId, request);
            UpsertRuntimeSnapshot(deviceId, request);
        }

        _changeNotifier.Notify(deviceId);
        await _hubContext.Clients.All.SendAsync("CenterDashboardChanged", deviceId, cancellationToken);

        return new CenterTelemetryAck
        {
            Success = true,
            Message = "Accepted",
            ServerTime = DateTime.Now
        };
    }

    private void UpsertDeviceNode(string deviceId, CenterTelemetrySnapshotRequest request)
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

    private void UpsertRuntimeSnapshot(string deviceId, CenterTelemetrySnapshotRequest request)
    {
        var snapshot = new CenterDeviceRuntimeSnapshot
        {
            DeviceId = deviceId,
            DeviceName = request.DeviceName.Trim(),
            SystemType = CenterTelemetryRules.NormalizeSystemType(request.SystemType),
            HeartbeatAt = request.HeartbeatAt == default ? DateTime.Now : request.HeartbeatAt,
            LastSeenAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var exists = _dbContext.Db.Queryable<CenterDeviceRuntimeSnapshot>().Any(it => it.DeviceId == deviceId);
        if (exists)
        {
            _dbContext.Db.Updateable(snapshot).ExecuteCommand();
        }
        else
        {
            _dbContext.Db.Insertable(snapshot).ExecuteCommand();
        }

        UpsertStationSnapshots(deviceId, request.Stations);
    }

    /// <summary>
    /// Upserts each station independently so dual-station devices do not overwrite their runtime values.
    /// </summary>
    private void UpsertStationSnapshots(string deviceId, IReadOnlyCollection<CenterTelemetryStationSnapshot> stations)
    {
        foreach (var station in stations.Where(item => item.StationNo > 0).OrderBy(item => item.StationNo))
        {
            var snapshot = _dbContext.Db.Queryable<CenterDeviceStationRuntimeSnapshot>()
                .First(item => item.DeviceId == deviceId && item.StationNo == station.StationNo);
            snapshot ??= new CenterDeviceStationRuntimeSnapshot
            {
                DeviceId = deviceId,
                StationNo = station.StationNo
            };

            snapshot.PlcConnected = station.PlcConnected;
            snapshot.PlcConnectionState = station.PlcConnectionState.Trim();
            snapshot.DeviceStatusCode = station.DeviceStatusCode.Trim();
            snapshot.DeviceStatusName = CenterTelemetryRules.ResolvePlcStatusName(
                station.DeviceStatusCode,
                station.DeviceStatusName);
            snapshot.AlarmMessage = station.AlarmMessage.Trim();
            snapshot.CurrentWorkOrder = station.CurrentWorkOrder.Trim();
            snapshot.ProductJobNo = station.ProductJobNo.Trim();
            snapshot.ProductModel = station.ProductModel.Trim();
            snapshot.TodayTotalCount = Math.Max(0, station.TodayTotalCount);
            snapshot.TodayQualifiedCount = Math.Max(0, station.TodayQualifiedCount);
            snapshot.TodayFailedCount = Math.Max(0, station.TodayFailedCount);
            snapshot.CollectedAt = station.CollectedAt == default ? DateTime.Now : station.CollectedAt;
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
    }

}
