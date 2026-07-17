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
    /// 是否触发本机软件报警。该状态由 PLC 原始状态 4 或独立 Bool 报警地址共同决定。
    /// </summary>
    public bool IsSoftwareAlarmActive { get; init; }

    /// <summary>
    /// 本机软件报警显示内容，不参与 MES、生命周期日志或中心服务器报警上报。
    /// </summary>
    public string SoftwareAlarmMessage { get; init; } = string.Empty;

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
