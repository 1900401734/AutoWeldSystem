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
    /// <param name="request">Uploaded telemetry or heartbeat payload.</param>
    /// <param name="carriesStations">
    /// Whether the payload is a full telemetry snapshot that enumerates every station the
    /// device currently runs. Heartbeat payloads never carry stations, and an empty station
    /// list is indistinguishable from "device reports zero stations" at the DTO level, so the
    /// caller must state which endpoint it came from. Only a full snapshot may prune stale rows.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<CenterTelemetryAck> IngestAsync(
        CenterTelemetrySnapshotRequest request,
        bool carriesStations,
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
            UpsertRuntimeSnapshot(deviceId, request, carriesStations);
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

    private void UpsertRuntimeSnapshot(
        string deviceId,
        CenterTelemetrySnapshotRequest request,
        bool carriesStations)
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

        // 心跳不携带工位数据，跳过整段工位处理：它的空集合与"设备只剩一个工位"
        // 在 DTO 上无法区分，若照常执行会把工位行删光后又由下一次遥测补回，看板持续闪空。
        if (!carriesStations)
        {
            return;
        }

        UpsertStationSnapshots(deviceId, request.Stations);
    }

    /// <summary>
    /// Upserts each station independently so dual-station devices do not overwrite their runtime values.
    /// </summary>
    private void UpsertStationSnapshots(string deviceId, IReadOnlyCollection<CenterTelemetryStationSnapshot> stations)
    {
        var reportedStationNumbers = stations
            .Where(item => item.StationNo > 0)
            .Select(item => item.StationNo)
            .Distinct()
            .OrderBy(stationNo => stationNo)
            .ToList();

        foreach (var stationNo in reportedStationNumbers)
        {
            var station = stations.First(item => item.StationNo == stationNo);
            var snapshot = _dbContext.Db.Queryable<CenterDeviceStationRuntimeSnapshot>()
                .First(item => item.DeviceId == deviceId && item.StationNo == stationNo);
            snapshot ??= new CenterDeviceStationRuntimeSnapshot
            {
                DeviceId = deviceId,
                StationNo = stationNo
            };

            snapshot.PlcConnected = station.PlcConnected;
            snapshot.PlcConnectionState = station.PlcConnectionState.Trim();
            snapshot.DeviceStatusCode = station.DeviceStatusCode.Trim();
            snapshot.DeviceStatusName = CenterTelemetryRules.ResolveReportedStatusName(
                station.DeviceStatusCode,
                station.DeviceStatusName);
            snapshot.AlarmMessage = station.AlarmMessage.Trim();
            snapshot.CurrentWorkOrder = station.CurrentWorkOrder.Trim();
            snapshot.ProductJobNo = station.ProductJobNo.Trim();
            snapshot.ProductModel = station.ProductModel.Trim();
            snapshot.TodayTotalCount = Math.Max(0, station.TodayTotalCount);
            snapshot.TodayQualifiedCount = Math.Max(0, station.TodayQualifiedCount);
            snapshot.TodayFailedCount = Math.Max(0, station.TodayFailedCount);
            snapshot.WorkOrderQuantity = Math.Max(0, station.WorkOrderQuantity);
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

        PruneMissingStationSnapshots(deviceId, reportedStationNumbers);
    }

    /// <summary>
    /// 删除本次遥测未包含的工位行。设备从双工位切回单工位后，工位 2 的旧行
    /// 没有任何其它清理路径，会以过期工单号长期显示成一个"幽灵工位"，
    /// 且因工单号非空还会被判定为开工中。
    /// 仅在完整遥测快照下调用；空列表说明设备确实没有可上报的工位，此时不做删除，
    /// 避免设备端异常导致看板数据被清空。
    /// </summary>
    private void PruneMissingStationSnapshots(string deviceId, IReadOnlyCollection<int> reportedStationNumbers)
    {
        if (reportedStationNumbers.Count == 0)
        {
            return;
        }

        _dbContext.Db.Deleteable<CenterDeviceStationRuntimeSnapshot>()
            .Where(item => item.DeviceId == deviceId && !reportedStationNumbers.Contains(item.StationNo))
            .ExecuteCommand();
    }

}
