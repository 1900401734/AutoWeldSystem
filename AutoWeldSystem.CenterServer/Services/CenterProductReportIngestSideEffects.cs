using AutoWeldSystem.CenterServer.Hubs;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Data;
using Microsoft.AspNetCore.SignalR;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>报表落盘后的通知，不回写实时工单、在线时间或遥测产量。</summary>
public interface ICenterProductReportIngestSideEffects
{
    Task ApplyAsync(string dataDirectory, string deviceId, CenterProductReportRequest request, CancellationToken cancellationToken = default);
}

public sealed class CenterProductReportIngestSideEffects : ICenterProductReportIngestSideEffects
{
    private readonly IHubContext<CenterDashboardHub> _hubContext;
    private readonly CenterDashboardChangeNotifier _changeNotifier;

    public CenterProductReportIngestSideEffects(
        SqlSugarDbContext dbContext,
        IHubContext<CenterDashboardHub> hubContext,
        CenterDashboardChangeNotifier changeNotifier,
        CenterProductReportFileStore fileStore)
    {
        _hubContext = hubContext;
        _changeNotifier = changeNotifier;
    }

    public async Task ApplyAsync(string dataDirectory, string deviceId, CenterProductReportRequest request, CancellationToken cancellationToken = default)
    {
        // 迟到的历史报表不能让旧工单重新变成“正在生产”，也不能把已收报表数冒充实时产量。
        _changeNotifier.Notify(deviceId);
        await _hubContext.Clients.All.SendAsync("CenterDashboardChanged", deviceId, cancellationToken);
    }
}
