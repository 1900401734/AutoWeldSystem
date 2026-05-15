using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// 单个工位的生产运行状态。
/// 后续双工位或多工位界面可以直接绑定这个对象，避免不同工位共用同一份工单和任务状态。
/// </summary>
public class ProductionStationRuntimeState
{
    /// <summary>
    /// 工位号。
    /// </summary>
    public int StationNo { get; set; } = ProductionConstants.Stations.DefaultStationNo;

    /// <summary>
    /// 当前工位绑定的工单。
    /// </summary>
    public MesWorkOrderResponse? CurrentWorkOrder { get; set; }

    /// <summary>
    /// 当前工位选中的工序。
    /// </summary>
    public ExpItemData? SelectedProcess { get; set; }

    /// <summary>
    /// 当前工位可选择的加工程序列表。
    /// </summary>
    public List<MesProgramListItemData> AvailablePrograms { get; set; } = [];

    /// <summary>
    /// 当前工位选中的加工程序。
    /// </summary>
    public MesProgramData? SelectedProgram { get; set; }

    /// <summary>
    /// 当前工位正在运行或刚完成的焊接任务。
    /// </summary>
    public BizWeldTask? ActiveTask { get; set; }

    /// <summary>
    /// 当前工位最近一次通过 MES 校验的员工号。
    /// </summary>
    public string MesOperatorNumber { get; set; } = string.Empty;

    /// <summary>
    /// 状态更新时间，便于后续界面判断哪个工位最近发生变化。
    /// </summary>
    public DateTime UpdatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 当前工位是否已绑定工单。
    /// </summary>
    public bool HasWorkOrder => CurrentWorkOrder is not null;

    /// <summary>
    /// 当前工位是否已选择加工程序。
    /// </summary>
    public bool HasProgram => SelectedProgram is not null;

    /// <summary>
    /// 当前工位是否存在运行中的焊接任务。
    /// </summary>
    public bool IsTaskRunning => ActiveTask is not null
        && string.Equals(ActiveTask.TaskStatus, "Running", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 清空当前工位的业务状态。
    /// </summary>
    public void Reset()
    {
        CurrentWorkOrder = null;
        SelectedProcess = null;
        AvailablePrograms = [];
        SelectedProgram = null;
        ActiveTask = null;
        MesOperatorNumber = string.Empty;
        UpdatedTime = DateTime.Now;
    }
}
