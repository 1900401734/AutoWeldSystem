namespace AutoWeldSystem.Core.DTOs.CenterServer;

/// <summary>
/// Latest stored runtime values for one device.
/// </summary>
public sealed class CenterDeviceRuntimeDto
{
    public bool PlcConnected { get; set; }
    public string PlcConnectionState { get; set; } = string.Empty;
    public string PlcDeviceStatusCode { get; set; } = string.Empty;
    public string PlcDeviceStatusName { get; set; } = string.Empty;
    public string AlarmMessage { get; set; } = string.Empty;
    public DateTime? LastSeenAt { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.Now;
}
