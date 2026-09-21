using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.DTOs.Plc;

public sealed record PlcWorkIdSnapshot(
    bool IsSuccess,
    string WorkId,
    DateTime UpdatedTime,
    string Message)
{
    /// <summary>
    /// PLC 工单号所属工位。默认 1，用于兼容单工位旧逻辑。
    /// </summary>
    public int StationNo { get; init; } = ProductionConstants.Stations.DefaultStationNo;
}
