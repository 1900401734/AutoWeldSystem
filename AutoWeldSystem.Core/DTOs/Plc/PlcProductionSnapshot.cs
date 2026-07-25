using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.DTOs.Plc;

/// <summary>
/// 生产指标快照。
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
    public int StationNo { get; init; } = ProductionConstants.Stations.DefaultStationNo;

    public bool TotalProductionReadSuccess { get; init; }

    public string TotalProductionReadMessage { get; init; } = string.Empty;

    public bool AcceptedQuantityReadSuccess { get; init; }

    public string AcceptedQuantityReadMessage { get; init; } = string.Empty;

    public bool RejectedQuantityReadSuccess { get; init; }

    public string RejectedQuantityReadMessage { get; init; } = string.Empty;

    public string AlarmMessage { get; init; } = string.Empty;

    /// <summary>
    /// Station scope of the active alarm. Null means no active alarm is stored in this snapshot.
    /// </summary>
    public int? AlarmStationNo { get; init; }

    /// <summary>
    /// 是否触发按当前系统设置判定后的本机有效报警。
    /// </summary>
    public bool IsSoftwareAlarmActive { get; init; }

    /// <summary>
    /// 本机软件报警显示内容，不参与 MES、生命周期日志或中心服务器报警上报。
    /// </summary>
    public string SoftwareAlarmMessage { get; init; } = string.Empty;

    /// <summary>
    /// 双条件模式下 PLC 原始状态为 4，但本轮没有匹配到有效报警地址。
    /// </summary>
    public bool IsAlarmPendingConfirmation { get; init; }

    /// <summary>
    /// 仅地址模式中原始状态 4 没有任何有效报警地址，界面按未知状态显示。
    /// </summary>
    public bool IsRawAlarmUnconfirmed { get; init; }

    public bool ProductionQuantitiesReadSuccess =>
        TotalProductionReadSuccess
        && AcceptedQuantityReadSuccess
        && RejectedQuantityReadSuccess;

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
