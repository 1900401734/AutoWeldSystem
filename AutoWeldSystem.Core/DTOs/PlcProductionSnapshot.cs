namespace AutoWeldSystem.Core.DTOs;

/// <summary>
/// PLC 生产监控快照。
/// 设备状态来自 PLC Int16 地址，生产指标来自地址维护页中配置的固定地址。
/// </summary>
public sealed record PlcProductionSnapshot(
    bool IsSuccess,
    short? DeviceStatusCode,
    int TotalProduction,
    int? TargetProduction,
    int AcceptedQuantity,
    int RejectedQuantity,
    DateTime UpdatedTime,
    string Message)
{
    public double? AcceptedRate => TotalProduction > 0
        ? (double)AcceptedQuantity / TotalProduction
        : null;

    public double? RejectedRate => TotalProduction > 0
        ? (double)RejectedQuantity / TotalProduction
        : null;

    public double? AchievementRate => TargetProduction.GetValueOrDefault() > 0
        ? (double)TotalProduction / TargetProduction!.Value
        : null;
}
