using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.DTOs.Plc;

/// <summary>
/// 一次有效的 PLC 产品数据就绪上升沿。
/// 首次上升沿只建立当前产品基线，后续上升沿才允许实时预览切换到下一产品编号。
/// </summary>
public sealed record PlcProductReadySnapshot(
    int StationNo,
    int TaskId,
    DateTime TriggeredTime)
{
    public int NormalizedStationNo => StationNo <= ProductionConstants.Stations.SharedStationNo
        ? ProductionConstants.Stations.DefaultStationNo
        : StationNo;
}
