using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

[SugarTable(tableName: "Biz_PlcAddress", tableDescription: "PLC地址表")]
public class BizPlcAddress
{
    /// <summary>
    /// Multiple stations can share the same logical key.
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "逻辑地址键")]
    public string LogicalKey { get; set; } = string.Empty;

    /// <summary>
    /// Station number. 0 means this address is shared by all stations.
    /// </summary>
    [SugarColumn(ColumnDescription = "工位号")]
    public int StationNo { get; set; }

    [SugarColumn(Length = 100, ColumnDescription = "地址名称")]
    public string AddressName { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "PLC地址")]
    public string? Address { get; set; }

    [SugarColumn(Length = 30, ColumnDescription = "数据类型")]
    public string DataType { get; set; } = AppConstants.PlcDataTypes.Int16;

    [SugarColumn(ColumnDescription = "字符串长度")]
    public int DataLength { get; set; } = 1;

    [SugarColumn(ColumnDescription = "是否启用")]
    public bool Enabled { get; set; } = true;

    [SugarColumn(ColumnDescription = "排序")]
    public int Sort { get; set; }

    [SugarColumn(Length = 300, IsNullable = true, ColumnDescription = "备注")]
    public string? Description { get; set; }

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}
