using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// Configurable PLC collection parameter.
/// The collection service reads these rows and maps PLC values to MES fields and report columns.
/// </summary>
[SugarTable("Biz_CollectionParameter", TableDescription = "采集参数地址表")]
public class BizCollectionParameter
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// Station number. Use 0 for parameters shared by all stations.
    /// </summary>
    [SugarColumn(ColumnDescription = "工位号")]
    public int StationNo { get; set; }

    /// <summary>
    /// Collection group key. Product process configuration binds to this group.
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "采集组")]
    public string CollectionGroup { get; set; } = "default";

    /// <summary>
    /// Stable parameter key used by code.
    /// </summary>
    [SugarColumn(Length = 80, ColumnDescription = "参数键")]
    public string ParameterKey { get; set; } = string.Empty;

    /// <summary>
    /// User-facing parameter name.
    /// </summary>
    [SugarColumn(Length = 100, ColumnDescription = "参数名称")]
    public string ParameterName { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "PLC地址")]
    public string? Address { get; set; }

    [SugarColumn(Length = 30, ColumnDescription = "数据类型")]
    public string DataType { get; set; } = AppConstants.PlcDataTypes.Int16;

    [SugarColumn(ColumnDescription = "读取长度")]
    public int DataLength { get; set; } = 1;

    [SugarColumn(ColumnDataType = "decimal(18,6)", ColumnDescription = "缩放系数")]
    public decimal Scale { get; set; } = 1m;

    [SugarColumn(ColumnDataType = "decimal(18,6)", ColumnDescription = "偏移量")]
    public decimal Offset { get; set; }

    [SugarColumn(ColumnDescription = "小数位数")]
    public int DecimalPlaces { get; set; } = 2;

    [SugarColumn(Length = 20, IsNullable = true, ColumnDescription = "单位")]
    public string? Unit { get; set; }

    [SugarColumn(Length = 80, IsNullable = true, ColumnDescription = "MES字段名")]
    public string? MesFieldName { get; set; }

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "报表列名")]
    public string? ReportColumnName { get; set; }

    [SugarColumn(ColumnDescription = "是否必填")]
    public bool Required { get; set; }

    [SugarColumn(ColumnDescription = "是否启用")]
    public bool Enabled { get; set; } = true;

    [SugarColumn(ColumnDescription = "排序")]
    public int Sort { get; set; }

    [SugarColumn(Length = 300, IsNullable = true, ColumnDescription = "备注")]
    public string? Description { get; set; }

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}
