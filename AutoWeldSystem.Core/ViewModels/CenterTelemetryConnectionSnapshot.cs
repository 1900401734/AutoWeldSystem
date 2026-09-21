namespace AutoWeldSystem.Core.ViewModels;

/// <summary>
/// Latest center-server telemetry connection result from the device-side sync service.
/// </summary>
public sealed record CenterTelemetryConnectionSnapshot(
    bool IsConnected,
    DateTime UpdatedTime,
    string Message);
