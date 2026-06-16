using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// 核心业务实体，代表从开工到完工的一次完整的生产任务周期。负责管理工单信息、程序关联、数据采集、MES同步的全流程，支持在线/离线双模式和多工位并行生产。
/// </summary>
[SugarTable("Biz_WeldTask", TableDescription = "焊接任务表")]
public class BizWeldTask
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "设备编号")]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 工位号。用于支持同一台设备的多个工位同时生产不同工单或不同产品型号。
    /// </summary>
    [SugarColumn(ColumnDescription = "工位号")]
    public int StationNo { get; set; } = ProductionConstants.Stations.DefaultStationNo;

    #region 工单信息

    /// <summary>
    /// 工单号/流转卡号
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "工单号/流转卡号")]
    public string SN { get; set; } = string.Empty;

    /// <summary>
    /// 产品工号
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "产品工号")]
    public string ProductNum { get; set; } = string.Empty;

    /// <summary>
    /// 产品型号
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "产品型号")]
    public string ProductModel { get; set; } = string.Empty;

    /// <summary>
    /// 规格
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "规格")]
    public string Spec { get; set; } = string.Empty;

    /// <summary>
    /// 批次
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "批次")]
    public string Batch { get; set; } = string.Empty;

    /// <summary>
    /// 部件名称
    /// </summary>
    [SugarColumn(Length = 100, ColumnDescription = "部件名称")]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 零件图号
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "零件图号")]
    public string DrawingNo { get; set; } = string.Empty;

    /// <summary>
    /// 项目来源，如：TDM、MES
    /// </summary>
    [SugarColumn(Length = 20, ColumnDescription = "项目来源")]
    public string ProjectFrom { get; set; } = string.Empty;

    /// <summary>
    /// 工序号
    /// </summary>
    [SugarColumn(Length = 20, ColumnDescription = "工序号")]
    public string ProcessNo { get; set; } = string.Empty;

    /// <summary>
    /// 工序名称
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "工序名称")]
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// 生产数量
    /// </summary>
    [SugarColumn(ColumnDescription = "生产数量/工单数量")]
    public int StartAmount { get; set; }

    #endregion

    #region 开/完工信息

    /// <summary>
    /// 本地任务标识。在线和离线任务都会生成，用于 MES 尚未返回 ExpStartId 时保持可追踪。
    /// </summary>
    [SugarColumn(Length = 32, IsNullable = true, ColumnDescription = "本地任务ID")]
    public string LocalExpStartId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 离线时，设备自行生成并关联，待网络恢复后关联上传
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "任务ID")]
    public string? ExpStartId { get; set; }

    /// <summary>
    /// 开始时间。
    /// </summary>
    [SugarColumn(ColumnDescription = "开始时间")]
    public DateTime StartTime { get; set; } = DateTime.Now;

    [SugarColumn(IsNullable = true, ColumnDescription = "结束时间")]
    public DateTime? EndTime { get; set; }

    #endregion

    #region 统计信息

    /// <summary>
    /// 实际数量/加工总数 = 合格数量 + 不合格数量，从PLC读取。
    [SugarColumn(ColumnDescription = "实际数量/加工总数")]
    public int ActualQty { get; set; }

    /// <summary>
    /// 合格数量，从PLC读取。
    /// </summary>
    [SugarColumn(ColumnDescription = "合格数量")]
    public int QualifiedQty { get; set; }

    /// <summary>
    /// 不良数量，PLC读取。
    /// </summary>
    [SugarColumn(ColumnDescription = "不良数量")]
    public int FailedQty { get; set; }

    #endregion

    #region 程序信息

    /// <summary>
    /// 程序ID。新增程序时MES返回，标识本地唯一加工程序。
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "程序ID")]
    public string? ProgramId { get; set; }

    /// <summary>
    /// 程序名称，招标协议上的18位字符串
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "程序名称")]
    public string? ProgramName { get; set; }

    /// <summary>
    /// 配方编号。关联PLC配方，标识本地唯一加工程序。
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "配方编号")]
    public string? RecipeCode { get; set; }

    #endregion

    #region 员工信息

    /// <summary>
    /// 开工时 MES 返回的员工号。
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "员工编号")]
    public string? UserNumber { get; set; }

    /// <summary>
    /// 开工时 MES 返回的员工姓名。
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "员工姓名")]
    public string? UserName { get; set; }

    /// <summary>
    /// 开工时 MES 返回的员工部门名称。
    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "部门名称")]
    public string? DeptName { get; set; }

    /// <summary>
    /// 开工时 MES 返回的员工班组名称。
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "班组名称")]
    public string? TeamName { get; set; }

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "结束人员")]
    public string? EndOperatorNumber { get; set; }

    #endregion

    [SugarColumn(Length = 20, ColumnDescription = "任务状态")]
    public string TaskStatus { get; set; } = "Ready";

    [SugarColumn(Length = 20, ColumnDescription = "上传状态")]
    public string UploadStatus { get; set; } = "Pending";

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "上传消息")]
    public string? UploadMessage { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "程序内容快照")]
    public string? ProgramContentSnapshot { get; set; }

    /// <summary>
    /// Whether this task was created locally while MES was disconnected.
    /// </summary>
    [SugarColumn(ColumnDescription = "是否离线本地创建")]
    public bool IsOfflineCreated { get; set; }
}

