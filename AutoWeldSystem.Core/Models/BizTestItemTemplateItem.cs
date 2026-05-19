using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// 测试项目模板明细。
/// 一行代表一个现场测试项目，包含实际值、上下限和结果地址。
/// </summary>
[SugarTable("Biz_TestItemTemplateItem", TableDescription = "测试项目模板明细表")]
public class BizTestItemTemplateItem
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 所属模板 ID。
    /// </summary>
    [SugarColumn(ColumnDescription = "模板ID")]
    public int TemplateId { get; set; }

    /// <summary>
    /// 工位号。0 表示所有工位共享。
    /// </summary>
    [SugarColumn(ColumnDescription = "工位号")]
    public int StationNo { get; set; } = ProductionConstants.Stations.SharedStationNo;

    /// <summary>
    /// 焊点序号。0 表示所有焊点共用这组地址。
    /// </summary>
    [SugarColumn(ColumnDescription = "焊点序号")]
    public int TouchNo { get; set; }

    /// <summary>
    /// 稳定字段键，例如 max_electric。程序使用它写入 RawDataJson。
    /// </summary>
    [SugarColumn(Length = 80, ColumnDescription = "测试项目键")]
    public string ItemKey { get; set; } = string.Empty;

    /// <summary>
    /// 用户可读测试项目名，例如峰值电流。
    /// </summary>
    [SugarColumn(Length = 100, ColumnDescription = "测试项目名称")]
    public string ItemName { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "实际值地址")]
    public string? ActualAddress { get; set; }

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "上限地址")]
    public string? UpperAddress { get; set; }

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "下限地址")]
    public string? LowerAddress { get; set; }

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "结果地址")]
    public string? ResultAddress { get; set; }

    /// <summary>
    /// 实际值、上限、下限的数据类型。
    /// </summary>
    [SugarColumn(Length = 30, ColumnDescription = "数值数据类型")]
    public string ValueDataType { get; set; } = AppConstants.PlcDataTypes.Float;

    /// <summary>
    /// 结果地址的数据类型。PLC 原始值 3 表示 OK，其余表示 NG。
    /// </summary>
    [SugarColumn(Length = 30, ColumnDescription = "结果数据类型")]
    public string ResultDataType { get; set; } = AppConstants.PlcDataTypes.Int16;

    [SugarColumn(ColumnDescription = "数值读取长度")]
    public int ValueDataLength { get; set; } = 1;

    [SugarColumn(ColumnDescription = "结果读取长度")]
    public int ResultDataLength { get; set; } = 1;

    [SugarColumn(ColumnDataType = "decimal(18,6)", ColumnDescription = "缩放系数")]
    public decimal Scale { get; set; } = 1m;

    [SugarColumn(ColumnDataType = "decimal(18,6)", ColumnDescription = "偏移量")]
    public decimal Offset { get; set; }

    [SugarColumn(ColumnDescription = "小数位数")]
    public int DecimalPlaces { get; set; } = 2;

    [SugarColumn(Length = 20, IsNullable = true, ColumnDescription = "单位")]
    public string? Unit { get; set; }

    /// <summary>
    /// MES 字段前缀。实际值使用该前缀；上下限和结果追加 _upper/_lower/_result。
    /// </summary>
    [SugarColumn(Length = 80, IsNullable = true, ColumnDescription = "MES字段前缀")]
    public string? MesFieldPrefix { get; set; }

    /// <summary>
    /// 报表列名前缀。实际值使用该列名；上下限和结果追加后缀。
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "报表列名前缀")]
    public string? ReportColumnName { get; set; }

    [SugarColumn(ColumnDescription = "是否必采")]
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
