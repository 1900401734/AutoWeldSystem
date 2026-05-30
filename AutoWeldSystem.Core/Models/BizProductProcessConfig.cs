using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// 产品工艺配置。
/// 一行配置描述某个产品在某个工位下的焊点数量、测试方案以及 PLC 数据区布局。
/// </summary>
[SugarTable("Biz_ProductProcess", TableDescription = "产品工艺表")]
public class BizProductProcessConfig
{
    [SugarColumn(ColumnName = "ProcessId", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "工艺ID/序号")]
    public int Id { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "测试方案ID")]
    public string SchemeId { get; set; } = "S01";

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "产品工号")]
    public string? ProductNum { get; set; }

    [SugarColumn(ColumnDescription = "工位号，0表示通用")]
    public int StationNo { get; set; } = ProductionConstants.Stations.SharedStationNo;

    [SugarColumn(ColumnDescription = "焊点数量")]
    public int TouchCount { get; set; } = 1;

    [SugarColumn(Length = 100, ColumnDescription = "产品头基地址")]
    public string ProductBase { get; set; } = "DB8.0";

    [SugarColumn(ColumnDescription = "产品头长度")]
    public int ProductLen { get; set; } = 32;

    [SugarColumn(Length = 50, ColumnDescription = "产品编号偏移表达式")]
    public string ProductNoExpr { get; set; } = "0:I-0";

    [SugarColumn(Length = 50, ColumnDescription = "产品结果偏移表达式")]
    public string ProductResultExpr { get; set; } = "4:H-4";

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "实际焊点数偏移表达式")]
    public string? ActualTouchCountExpr { get; set; }

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "预设焊点数偏移表达式")]
    public string? PresetTouchCountExpr { get; set; }

    [SugarColumn(Length = 100, ColumnDescription = "焊点头基地址")]
    public string TouchBase { get; set; } = "DB8.32";

    [SugarColumn(ColumnDescription = "焊点头长度")]
    public int TouchHeaderLen { get; set; } = 16;

    [SugarColumn(Length = 50, ColumnDescription = "焊点编号偏移表达式")]
    public string TouchNoExpr { get; set; } = "0:I-0";

    [SugarColumn(Length = 50, ColumnDescription = "焊点结果偏移表达式")]
    public string TouchResultExpr { get; set; } = "4:H-4";

    [SugarColumn(Length = 100, ColumnDescription = "测试项基地址")]
    public string TestBase { get; set; } = "DB8.100";

    [SugarColumn(ColumnDescription = "单个焊点的测试项数据区长度")]
    public int TestAreaLen { get; set; } = 48;

    [SugarColumn(ColumnName = "Enable", ColumnDescription = "是否启用")]
    public bool Enabled { get; set; } = true;

    [SugarColumn(ColumnDescription = "创建时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;

}
