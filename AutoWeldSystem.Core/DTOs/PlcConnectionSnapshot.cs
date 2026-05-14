using AutoWeldSystem.Core.Enums;

namespace AutoWeldSystem.Core.DTOs;

public sealed record PlcConnectionSnapshot(
    PlcConnectionState State,
    bool IsConnected,
    string Endpoint,
    DateTime? LastConnectedTime,
    DateTime? LastHeartbeatTime,
    string Message);
