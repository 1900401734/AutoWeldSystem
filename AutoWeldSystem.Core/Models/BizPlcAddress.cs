using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// PLC 地址配置表。
/// AddressKey 是程序识别用的稳定键，Address 是用户按现场 PLC 实际点位填写的地址。
/// </summary>
[SugarTable("Biz_PlcAddress", TableDescription = "PLC地址表")]
public class BizPlcAddress
{
    [SugarColumn(IsPrimaryKey = true, Length = 50, ColumnDescription = "地址用途键")]
    public string AddressKey { get; set; } = string.Empty;

    /// <summary>
    /// Logical business key used by code. Multiple stations can share the same logical key.
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "逻辑地址键")]
    public string? LogicalKey { get; set; }

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

    [SugarColumn(ColumnDescription = "字符串长度或读取长度")]
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
