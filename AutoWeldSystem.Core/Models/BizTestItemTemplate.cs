using SqlSugar;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// 测试项目模板主表。
/// 一个模板代表一套可复用的焊点测试项目配置，例如“型号A标准模板”。
/// </summary>
[SugarTable("Biz_TestItemTemplate", TableDescription = "测试项目模板表")]
public class BizTestItemTemplate
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 模板编码，用于现场快速识别和后续接口扩展。
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "模板编码")]
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 用户可读模板名称。
    /// </summary>
    [SugarColumn(Length = 100, ColumnDescription = "模板名称")]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 模板版本号。后续模板变更追溯时可以使用。
    /// </summary>
    [SugarColumn(ColumnDescription = "版本号")]
    public int VersionNumber { get; set; } = 1;

    [SugarColumn(ColumnDescription = "是否启用")]
    public bool Enabled { get; set; } = true;

    [SugarColumn(ColumnDescription = "排序")]
    public int Sort { get; set; }

    [SugarColumn(Length = 300, IsNullable = true, ColumnDescription = "备注")]
    public string? Description { get; set; }

    [SugarColumn(ColumnDescription = "创建时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}
