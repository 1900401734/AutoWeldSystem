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
