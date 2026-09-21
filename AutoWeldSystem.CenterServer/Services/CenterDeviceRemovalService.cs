using AutoWeldSystem.CenterServer.Hubs;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Data;
using Microsoft.AspNetCore.SignalR;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 手动删除看板设备：清理设备节点、心跳快照与全部工位快照。
/// 用于设备下线或报废后移除永远残留的卡片；若设备仍在推送，遥测 ingest 会自动重新注册。
/// </summary>
public sealed class CenterDeviceRemovalService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IHubContext<CenterDashboardHub> _hubContext;
    private readonly CenterDashboardChangeNotifier _changeNotifier;
    private readonly object _dbLock = new();

    public CenterDeviceRemovalService(
        SqlSugarDbContext dbContext,
        IHubContext<CenterDashboardHub> hubContext,
        CenterDashboardChangeNotifier changeNotifier)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _changeNotifier = changeNotifier;
    }

    /// <summary>
    /// 删除指定设备在三张快照表中的全部行，返回是否删到任何行。
    /// 三表删除放在同一事务：避免部分删除留下孤儿行，设备将来重新注册时带回过期工位计数。
    /// </summary>
    public async Task<bool> RemoveDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var normalizedId = deviceId?.Trim() ?? string.Empty;
        if (normalizedId.Length == 0)
        {
            return false;
        }

        var removedRows = 0;
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var tran = _dbContext.Db.Ado.UseTran(() =>
            {
                removedRows += _dbContext.Db.Deleteable<CenterDeviceStationRuntimeSnapshot>()
                    .Where(it => it.DeviceId == normalizedId)
                    .ExecuteCommand();
                removedRows += _dbContext.Db.Deleteable<CenterDeviceRuntimeSnapshot>()
                    .Where(it => it.DeviceId == normalizedId)
                    .ExecuteCommand();
                removedRows += _dbContext.Db.Deleteable<CenterDeviceNode>()
                    .Where(it => it.DeviceId == normalizedId)
                    .ExecuteCommand();
            });
            if (!tran.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"删除设备失败：{tran.ErrorException?.Message ?? "未知数据库错误"}",
                    tran.ErrorException);
            }
        }

        if (removedRows > 0)
        {
            _changeNotifier.Notify(normalizedId);
            await _hubContext.Clients.All.SendAsync("CenterDashboardChanged", normalizedId, cancellationToken);
        }

        return removedRows > 0;
    }
}
