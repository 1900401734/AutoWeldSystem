namespace AutoWeldSystem.Core.DTOs.CenterServer;

/// <summary>
/// Runtime state projected for dashboard display.
/// </summary>
public sealed class CenterDashboardDeviceStateDto
{
    public bool ClientOnline { get; set; }
    public bool PlcConnected { get; set; }
    public string PlcConnectionState { get; set; } = string.Empty;
    public string PlcDeviceStatusCode { get; set; } = string.Empty;
    public string PlcDeviceStatusName { get; set; } = string.Empty;
    public string AlarmMessage { get; set; } = string.Empty;
    public DateTime? LastSeenAt { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.Now;
}
