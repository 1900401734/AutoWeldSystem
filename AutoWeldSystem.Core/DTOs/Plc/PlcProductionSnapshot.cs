using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.DTOs.Plc;

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
    /// <summary>
    /// 生产指标所属工位。设备状态为共享信号，产量类指标按该工位区分。
    /// </summary>
    public int StationNo { get; init; } = ProductionConstants.Stations.DefaultStationNo;

    public bool TotalProductionReadSuccess { get; init; }

    public string TotalProductionReadMessage { get; init; } = string.Empty;

    public bool AcceptedQuantityReadSuccess { get; init; }

    public string AcceptedQuantityReadMessage { get; init; } = string.Empty;

    public bool RejectedQuantityReadSuccess { get; init; }

    public string RejectedQuantityReadMessage { get; init; } = string.Empty;

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
