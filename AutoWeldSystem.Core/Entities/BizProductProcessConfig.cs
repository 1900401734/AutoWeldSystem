using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

[SugarTable(tableName: "Biz_ProductProcess", tableDescription: "产品工艺表")]
public class BizProductProcessConfig
{
    [SugarColumn(ColumnName = "ProcessId", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "工艺ID/序号")]
    public int Id { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "测试方案ID")]
    public string SchemeId { get; set; } = "S01";

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "产品工号")]
    public string? ProductNum { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "工位号，0表示通用")]
    public int StationNo { get; set; } = ProductionConstants.Stations.SharedStationNo;

    [SugarColumn(ColumnDescription = "焊点数量")]
    public int TouchCount { get; set; } = 1;

    [SugarColumn(Length = 100, ColumnDescription = "产品头基地址")]
    public string ProductBase { get; set; } = "DB8.0";

    [SugarColumn(ColumnDescription = "产品头长度")]
    public int ProductLen { get; set; } = 32;

    [SugarColumn(Length = 50, ColumnDescription = "产品编号偏移表达式")]
    public string ProductNoExpr { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "产品结果偏移表达式")]
    public string ProductResultExpr { get; set; } = string.Empty;

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "实际焊点数偏移表达式")]
    public string? ActualTouchCountExpr { get; set; } = string.Empty;

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "预设焊点数偏移表达式")]
    public string? PresetTouchCountExpr { get; set; } = string.Empty;

    [SugarColumn(Length = 100, ColumnDescription = "焊点头基地址")]
    public string TouchBase { get; set; } = string.Empty;

    /// <summary>
    /// 焊点编号独立基地址。旧数据为空时回退使用 TouchBase。
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "焊点编号基地址")]
    public string? TouchNoBase { get; set; } = string.Empty;

    /// <summary>
    /// 焊点结果独立基地址。旧数据为空时回退使用 TouchBase。
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "焊点结果基地址")]
    public string? TouchResultBase { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "焊点头长度")]
    public int TouchHeaderLen { get; set; } = 16;

    [SugarColumn(Length = 50, ColumnDescription = "焊点编号偏移表达式")]
    public string TouchNoExpr { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "焊点结果偏移表达式")]
    public string TouchResultExpr { get; set; } = string.Empty;

    [SugarColumn(Length = 100, ColumnDescription = "测试项基地址")]
    public string TestBase { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "单个焊点的测试项数据区长度")]
    public int TestAreaLen { get; set; } = 130;

    [SugarColumn(ColumnName = "Enable", ColumnDescription = "是否启用")]
    public bool Enabled { get; set; } = true;

    [SugarColumn(ColumnDescription = "创建时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;

}
