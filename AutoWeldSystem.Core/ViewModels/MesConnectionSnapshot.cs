namespace AutoWeldSystem.Core.ViewModels;

/// <summary>
/// MES 连接状态快照。
/// 通过轻量接口轮询得到，用于 MonitorView 实时显示 MES 在线状态。
/// </summary>
public sealed record MesConnectionSnapshot(
    bool IsConnected,
    DateTime? LastSuccessTime,
    DateTime UpdatedTime,
    string Message);
