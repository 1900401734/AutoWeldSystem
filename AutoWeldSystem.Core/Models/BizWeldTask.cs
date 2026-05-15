using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// 
/// </summary>
[SugarTable("Biz_WeldTask", TableDescription = "焊接任务表")]
public class BizWeldTask
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 离线时，设备自行生成并关联，待网络恢复后关联上传
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "试验开始ID")]
    public string? ExpStartId { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "设备编号")]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 工位号。用于支持同一台设备的多个工位同时生产不同工单或不同产品型号。
    /// </summary>
    [SugarColumn(ColumnDescription = "工位号")]
    public int StationNo { get; set; } = ProductionConstants.Stations.DefaultStationNo;

    [SugarColumn(Length = 50, ColumnDescription = "工单编号")]
    public string WorkOrderId { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "产品编号")]
    public string ProductNum { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "产品型号")]
    public string ProductModel { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "规格")]
    public string Spec { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "批次")]
    public string Batch { get; set; } = string.Empty;

    [SugarColumn(Length = 100, ColumnDescription = "产品名称")]
    public string ProductName { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "图纸编号")]
    public string DrawingNo { get; set; } = string.Empty;

    [SugarColumn(Length = 20, ColumnDescription = "工序编号")]
    public string ProcessNo { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "工序名称")]
    public string ProcessName { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "计划数量")]
    public int PlannedQty { get; set; }

    [SugarColumn(ColumnDescription = "实际数量")]
    public int ActualQty { get; set; }

    [SugarColumn(ColumnDescription = "合格数量")]
    public int QualifiedQty { get; set; }

    [SugarColumn(ColumnDescription = "不合格数量")]
    public int FailedQty { get; set; }

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "程序ID")]
    public string? ProgramId { get; set; }

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "程序名称")]
    public string? ProgramName { get; set; }

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "启动操作员编号")]
    public string? StartOperatorNumber { get; set; }

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "结束操作员编号")]
    public string? EndOperatorNumber { get; set; }

    [SugarColumn(ColumnDescription = "开始时间")]
    public DateTime StartTime { get; set; } = DateTime.Now;

    [SugarColumn(IsNullable = true, ColumnDescription = "结束时间")]
    public DateTime? EndTime { get; set; }

    [SugarColumn(Length = 20, ColumnDescription = "任务状态")]
    public string TaskStatus { get; set; } = "Ready";

    [SugarColumn(Length = 20, ColumnDescription = "上传状态")]
    public string UploadStatus { get; set; } = "Pending";

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "上传消息")]
    public string? UploadMessage { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "程序内容快照")]
    public string? ProgramContentSnapshot { get; set; }
}
