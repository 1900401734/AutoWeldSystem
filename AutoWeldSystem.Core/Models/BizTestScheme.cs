using SqlSugar;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// 测试方案主表。
/// 一套测试方案定义当前产品需要采集哪些测试项。
/// </summary>
[SugarTable("Biz_TestScheme", TableDescription = "测试方案表")]
public class BizTestScheme
{
    [SugarColumn(IsPrimaryKey = true, Length = 50, ColumnDescription = "方案ID/序号")]
    public string SchemeId { get; set; } = string.Empty;

    [SugarColumn(Length = 100, ColumnDescription = "测试方案名称")]
    public string SchemeName { get; set; } = string.Empty;

    [SugarColumn(Length = 300, IsNullable = true, ColumnDescription = "方案描述")]
    public string? Description { get; set; }
}
