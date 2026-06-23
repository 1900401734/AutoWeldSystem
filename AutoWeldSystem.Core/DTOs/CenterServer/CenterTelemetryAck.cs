namespace AutoWeldSystem.Core.DTOs.CenterServer;

/// <summary>
/// Result returned after the center server accepts or rejects one telemetry snapshot.
/// </summary>
public sealed class CenterTelemetryAck
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ServerTime { get; set; } = DateTime.Now;
}
