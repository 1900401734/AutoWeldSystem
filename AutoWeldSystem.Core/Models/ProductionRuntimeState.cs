using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// 程序运行时状态，包含当前工单、选中工序、可用程序列表、选中程序、当前活动任务等信息。
/// </summary>
public class ProductionRuntimeState
{
    /// <summary>
    /// 上次与服务器同步的时间。
    /// </summary>
    public DateTime? LastServerSyncTime { get; set; }

    /// <summary>
    /// 上次与服务器同步的结果消息。
    /// </summary>
    public string? LastServerSyncMessage { get; set; }

    /// <summary>
    /// 当前单界面流程正在操作的工位号。后续会扩展为按工位管理的运行上下文。
    /// </summary>
    public int CurrentStationNo { get; set; } = ProductionConstants.Stations.DefaultStationNo;

    /// <summary>
    /// 按工位保存的运行状态。当前单界面仍使用下方兼容属性，后续多工位界面可直接读取这里。
    /// </summary>
    public Dictionary<int, ProductionStationRuntimeState> StationStates { get; set; } = [];

    /// <summary>
    /// 当前工单。
    /// </summary>
    public MesWorkOrderResponse? CurrentWorkOrder { get; set; }

    /// <summary>
    /// 选中工序。
    /// </summary>
    public ExpItemData? SelectedProcess { get; set; }

    /// <summary>
    /// 可用程序列表。
    /// </summary>
    public List<MesProgramListItemData> AvailablePrograms { get; set; } = [];

    /// <summary>
    /// 选中的程序。
    /// </summary>
    public MesProgramData? SelectedProgram { get; set; }

    /// <summary>
    /// 当前活动任务。
    /// </summary>
    public BizWeldTask? ActiveTask { get; set; }

    /// <summary>
    /// MES 操作员编号。
    /// </summary>
    public string MesOperatorNumber { get; set; } = string.Empty;

    /// <summary>
    /// 是否已获取当前工单。
    /// </summary>
    public bool HasWorkOrder => CurrentWorkOrder is not null;

    /// <summary>
    /// 是否已选择加工程序。
    /// </summary>
    public bool HasProgram => SelectedProgram is not null;

    /// <summary>
    /// 当前是否存在运行中的焊接任务。
    /// </summary>
    public bool IsTaskRunning => ActiveTask is not null
        && string.Equals(ActiveTask.TaskStatus, "Running", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 重置运行时状态。
    /// </summary>
    public void Reset()
    {
        CurrentWorkOrder = null;
        SelectedProcess = null;
        AvailablePrograms = [];
        SelectedProgram = null;
        ActiveTask = null;
        MesOperatorNumber = string.Empty;
        CurrentStationNo = ProductionConstants.Stations.DefaultStationNo;
        StationStates.Clear();
    }

    /// <summary>
    /// 切换当前正在操作的工位，并把兼容属性恢复为该工位的状态。
    /// </summary>
    public void RestoreStation(int stationNo)
    {
        CurrentStationNo = NormalizeStationNo(stationNo);
        var station = GetOrCreateStation(CurrentStationNo);

        CurrentWorkOrder = station.CurrentWorkOrder;
        SelectedProcess = station.SelectedProcess;
        AvailablePrograms = station.AvailablePrograms.ToList();
        SelectedProgram = station.SelectedProgram;
        ActiveTask = station.ActiveTask;
        MesOperatorNumber = station.MesOperatorNumber;
    }

    /// <summary>
    /// 将当前兼容属性保存到当前工位状态中。
    /// </summary>
    public void SaveCurrentStation()
    {
        var station = GetOrCreateStation(CurrentStationNo);

        station.CurrentWorkOrder = CurrentWorkOrder;
        station.SelectedProcess = SelectedProcess;
        station.AvailablePrograms = AvailablePrograms.ToList();
        station.SelectedProgram = SelectedProgram;
        station.ActiveTask = ActiveTask;
        station.MesOperatorNumber = MesOperatorNumber;
        station.UpdatedTime = DateTime.Now;
    }

    /// <summary>
    /// 清空当前工位状态，不影响其它工位。
    /// </summary>
    public void ResetCurrentStation()
    {
        var station = GetOrCreateStation(CurrentStationNo);
        station.Reset();
        RestoreStation(CurrentStationNo);
    }

    /// <summary>
    /// 获取指定工位状态，不存在时自动创建。
    /// </summary>
    public ProductionStationRuntimeState GetOrCreateStation(int stationNo)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        if (!StationStates.TryGetValue(normalizedStationNo, out var station))
        {
            station = new ProductionStationRuntimeState { StationNo = normalizedStationNo };
            StationStates[normalizedStationNo] = station;
        }

        return station;
    }

    private static int NormalizeStationNo(int stationNo)
    {
        return stationNo <= 0
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;
    }
}
