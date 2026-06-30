namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// PLC 写入调试窗口的预填参数。
/// 右键菜单只负责带入地址和类型，实际写入仍由用户在调试窗口中确认。
/// </summary>
/// <param name="Address">待写入的 PLC 地址。</param>
/// <param name="DataType">待写入的数据类型。</param>
/// <param name="ValueText">可选默认写入值。</param>
public sealed record PlcWriteDebugPreset(
    string Address,
    string DataType,
    string? ValueText = null);
