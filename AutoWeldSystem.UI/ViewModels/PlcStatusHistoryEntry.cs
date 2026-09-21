using AutoWeldSystem.Core.Enums;

namespace AutoWeldSystem.UI.ViewModels;

/// <summary>
/// Keeps only the fields needed to explain when and why the PLC connection state changed.
/// </summary>
public sealed record PlcStatusHistoryEntry(
    int StationNo,
    DateTime ChangedTime,
    PlcConnectionState State,
    bool IsConnected,
    string Message);
