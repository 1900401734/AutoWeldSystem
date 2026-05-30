using SqlSugar;

namespace AutoWeldSystem.Core.Models;

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
}
