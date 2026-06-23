using Microsoft.AspNetCore.SignalR;

namespace AutoWeldSystem.CenterServer.Hubs;

/// <summary>
/// SignalR hub used to notify dashboard pages that a device snapshot changed.
/// </summary>
public sealed class CenterDashboardHub : Hub
{
}
