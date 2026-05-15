using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// Product instance under a weld task.
/// It reserves a unique ProductNo before weld point records are created, which prevents dual stations from colliding.
/// </summary>
[SugarTable("Biz_ProductInstance", TableDescription = "产品实例表")]
public class BizProductInstance
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(ColumnDescription = "焊接任务ID")]
    public int TaskId { get; set; }

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "开工任务ID")]
    public string? ExpStartId { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "设备编号")]
    public string DeviceId { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "工单号")]
    public string WorkOrderId { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "工序号")]
    public string ProcessNo { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "产品编号")]
    public string ProductNo { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "工位号")]
    public int StationNo { get; set; }

    [SugarColumn(ColumnDescription = "应采集焊点数量")]
    public int RequiredTouchCount { get; set; } = 1;

    [SugarColumn(ColumnDescription = "已采集焊点数量")]
    public int CollectedTouchCount { get; set; }

    [SugarColumn(Length = 20, ColumnDescription = "产品结果")]
    public string TestResult { get; set; } = ProductionConstants.TestResults.Unknown;

    [SugarColumn(Length = 20, ColumnDescription = "产品状态")]
    public string ProductStatus { get; set; } = ProductionConstants.ProductInstanceStatuses.Running;

    [SugarColumn(ColumnDescription = "开始时间")]
    public DateTime StartTime { get; set; } = DateTime.Now;

    [SugarColumn(IsNullable = true, ColumnDescription = "完成时间")]
    public DateTime? CompletedTime { get; set; }

    [SugarColumn(ColumnDescription = "创建时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}
