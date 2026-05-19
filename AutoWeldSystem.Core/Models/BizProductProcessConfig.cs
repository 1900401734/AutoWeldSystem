using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// Product process configuration.
/// It tells the collection engine how many weld points a product number/model has and which test item template to use.
/// </summary>
[SugarTable("Biz_ProductProcessConfig", TableDescription = "产品型号工艺配置表")]
public class BizProductProcessConfig
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// Product number. This is the primary key used to bind a program to the matching test item template.
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "产品工号")]
    public string? ProductNum { get; set; }

    /// <summary>
    /// Product model returned by MES or configured manually. This is an auxiliary match key.
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "产品型号")]
    public string ProductModel { get; set; } = string.Empty;

    /// <summary>
    /// Station number. Use 0 for configuration shared by all stations.
    /// </summary>
    [SugarColumn(ColumnDescription = "工位号")]
    public int StationNo { get; set; } = ProductionConstants.Stations.SharedStationNo;

    /// <summary>
    /// MES process number.
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "工序号")]
    public string ProcessNo { get; set; } = string.Empty;

    /// <summary>
    /// Process display name.
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "工序名称")]
    public string? ProcessName { get; set; }

    /// <summary>
    /// Required weld point count for one product.
    /// </summary>
    [SugarColumn(ColumnDescription = "每件产品焊点数量")]
    public int WeldPointCount { get; set; } = 1;

    /// <summary>
    /// 绑定的测试项目模板 ID。0 表示暂未绑定模板。
    /// </summary>
    [SugarColumn(ColumnDescription = "测试项目模板ID")]
    public int TemplateId { get; set; }

    /// <summary>
    /// Program matching rule reserved for future automatic program lookup.
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = true, ColumnDescription = "程序匹配规则")]
    public string? ProgramMatchRule { get; set; }

    /// <summary>
    /// Product number source. Default is auto increment on the PC side.
    /// </summary>
    [SugarColumn(Length = 30, ColumnDescription = "产品编号来源")]
    public string ProductNoSource { get; set; } = ProductionConstants.ProductNoSources.AutoIncrement;

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
