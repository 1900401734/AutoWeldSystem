using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// MonitorView 运行提示缓存。
/// 按工位保存最后一次运行状态和异常提示，程序重启后可恢复用户上次看到的提示词。
/// </summary>
[SugarTable("Biz_RuntimeTipState", TableDescription = "运行提示缓存表")]
public class BizRuntimeTipState
{
    /// <summary>
    /// 工位号作为主键；单工位固定为 1，双工位分别保存 1/2。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, ColumnDescription = "工位号")]
    public int StationNo { get; set; } = ProductionConstants.Stations.DefaultStationNo;

    [SugarColumn(Length = 200, IsNullable = true, ColumnDescription = "运行状态资源键")]
    public string? RuntimeStatusKey { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "运行状态参数JSON")]
    public string? RuntimeStatusArgsJson { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "运行状态动态文本")]
    public string? RuntimeStatusText { get; set; }

    [SugarColumn(ColumnDescription = "运行状态是否成功")]
    public bool RuntimeStatusTextIsSuccess { get; set; }

    [SugarColumn(Length = 200, IsNullable = true, ColumnDescription = "异常提示资源键")]
    public string? RuntimeErrorKey { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "异常提示参数JSON")]
    public string? RuntimeErrorArgsJson { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "异常提示动态文本")]
    public string? RuntimeErrorText { get; set; }

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}
