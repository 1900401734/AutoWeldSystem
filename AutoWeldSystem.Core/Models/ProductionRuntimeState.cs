using AutoWeldSystem.Core.DTOs;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// 程序运行时状态，包含当前工单、选中工序、可用程序列表、选中程序、当前活动任务等信息
/// </summary>
public class ProductionRuntimeState
{
    /// <summary>
    /// 上次与服务器同步的时间
    /// </summary>
    public DateTime? LastServerSyncTime { get; set; }

    /// <summary>
    /// 上次与服务器同步的结果消息
    /// </summary>
    public string? LastServerSyncMessage { get; set; }

    /// <summary>
    /// 当前工单
    /// </summary>
    public MesWorkOrderResponse? CurrentWorkOrder { get; set; }

    /// <summary>
    /// 选中工序
    /// </summary>
    public ExpItemData? SelectedProcess { get; set; }

    /// <summary>
    /// 可用程序列表
    /// </summary>
    public List<MesProgramListItemData> AvailablePrograms { get; set; } = [];

    /// <summary>
    /// 选中的程序
    /// </summary>
    public MesProgramData? SelectedProgram { get; set; }
    
    /// <summary>
    /// 当前活动任务
    /// </summary>
    public BizWeldTask? ActiveTask { get; set; }

    /// <summary>
    /// MES操作员编号
    /// </summary>
    public string MesOperatorNumber { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public bool HasWorkOrder => CurrentWorkOrder is not null;

    /// <summary>
    /// 
    /// </summary>
    public bool HasProgram => SelectedProgram is not null;

    /// <summary>
    /// 
    /// </summary>
    public bool IsTaskRunning => ActiveTask is not null && string.Equals(ActiveTask.TaskStatus, "Running", StringComparison.OrdinalIgnoreCase);
    
    /// <summary>
    /// 重置运行时状态
    /// </summary>  
    public void Reset()
    {
        CurrentWorkOrder = null;
        SelectedProcess = null;
        AvailablePrograms = [];
        SelectedProgram = null;
        ActiveTask = null;
        MesOperatorNumber = string.Empty;
    }
}
