using SqlSugar;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// 测试项字典。
/// 字典项定义测试项名称、单位，以及单个焊点测试项数据区内的字段偏移表达式。
/// </summary>
[SugarTable("Dim_TestItem", TableDescription = "测试项字典表")]
public class DimTestItem
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "测试项ID/序号")]
    public int ItemId { get; set; }

    [SugarColumn(Length = 100, ColumnDescription = "项目名称")]
    public string ItemName { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "实际值偏移表达式")]
    public string ActualExpression { get; set; } = "0:F-0";

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "上限偏移表达式")]
    public string? UpperExpression { get; set; }

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "下限偏移表达式")]
    public string? LowerExpression { get; set; }

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "结果偏移表达式")]
    public string? ResultExpression { get; set; }

    [SugarColumn(Length = 20, IsNullable = true, ColumnDescription = "单位")]
    public string? Unit { get; set; }
}
