namespace AutoWeldSystem.Core.DTOs.CenterServer;

/// <summary>设备端有效报警的只读传输快照，不覆盖 PLC 原始设备状态。</summary>
public sealed class CenterEffectiveAlarmDto
{
    public bool IsActive { get; set; }
    public bool IsPendingConfirmation { get; set; }
    public bool IsRawAlarmUnconfirmed { get; set; }
    public string Message { get; set; } = string.Empty;
}
