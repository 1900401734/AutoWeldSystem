using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// PLC 报警地址配置。
/// 一个地址对应一种报警原因，地址可以跨 DB 块且不要求连续。
/// </summary>
[SugarTable(tableName: "Biz_PlcAlarmAddress", tableDescription: "PLC报警地址表")]
public class BizPlcAlarmAddress
{
    /// <summary>
    /// 主键。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 历史兼容字段。报警地址属于整台设备，不映射程序工位，新数据统一保存为 0。
    /// </summary>
    [SugarColumn(ColumnDescription = "历史兼容工位号，新数据统一为0")]
    public int StationNo { get; set; }

    /// <summary>
    /// PLC Bool 读取地址。
    /// </summary>
    [SugarColumn(Length = 100, ColumnDescription = "PLC读取地址")]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// 地址置位时显示和上报的报警内容。
    /// </summary>
    [SugarColumn(Length = 300, ColumnDescription = "报警内容")]
    public string AlarmContent { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用当前报警点。
    /// </summary>
    [SugarColumn(ColumnDescription = "是否启用")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 界面显示和读取顺序。
    /// </summary>
    [SugarColumn(ColumnDescription = "排序")]
    public int Sort { get; set; }

    /// <summary>
    /// 最近更新时间。
    /// </summary>
    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}
