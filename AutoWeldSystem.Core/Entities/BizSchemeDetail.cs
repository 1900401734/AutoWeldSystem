using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// 测试方案明细。
/// 一行表示某套测试方案包含一个测试项。
/// </summary>
[SugarTable("Biz_SchemeDetail", TableDescription = "方案明细表")]
public class BizSchemeDetail
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "明细ID/序号")]
    public int DetailId { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "测试方案ID")]
    public string SchemeId { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "测试项ID")]
    public int ItemId { get; set; }

    [SugarColumn(ColumnDescription = "是否启用实际值")]
    public bool EnableActual { get; set; } = true;

    [SugarColumn(ColumnDescription = "是否启用上限")]
    public bool EnableUpper { get; set; } = true;

    [SugarColumn(ColumnDescription = "是否启用下限")]
    public bool EnableLower { get; set; } = true;

    [SugarColumn(ColumnDescription = "是否启用结果")]
    public bool EnableResult { get; set; } = true;
}
