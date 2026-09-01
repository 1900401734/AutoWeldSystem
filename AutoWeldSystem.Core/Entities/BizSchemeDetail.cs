using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// 测试方案明细。
/// 一行表示某套测试方案包含一个测试项。
/// 五个通道互相独立：Enable* 对应界面“实时预览”，Save* 对应“本地保存”，
/// Forward* 对应“转发看板”，Report* 对应“写入报表”，Mes* 对应“过程参数”。
/// Mes* 与 MesFieldName 保留 MES 原名：这批字段直接对应上传协议，改名还会清空现场既有配置。
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
    public bool EnableActual { get; set; }

    [SugarColumn(ColumnDescription = "是否启用上限")]
    public bool EnableUpper { get; set; }

    [SugarColumn(ColumnDescription = "是否启用下限")]
    public bool EnableLower { get; set; }

    [SugarColumn(ColumnDescription = "是否启用结果")]
    public bool EnableResult { get; set; }

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "实际值显示表头")]
    public string? ActualHeader { get; set; }

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "上限显示表头")]
    public string? UpperHeader { get; set; }

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "下限显示表头")]
    public string? LowerHeader { get; set; }

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "结果显示表头")]
    public string? ResultHeader { get; set; }

    [SugarColumn(ColumnDescription = "实际值写入历史数据")]
    public bool SaveActual { get; set; }

    [SugarColumn(ColumnDescription = "上限写入历史数据")]
    public bool SaveUpper { get; set; }

    [SugarColumn(ColumnDescription = "下限写入历史数据")]
    public bool SaveLower { get; set; }

    [SugarColumn(ColumnDescription = "结果写入历史数据")]
    public bool SaveResult { get; set; }

    [SugarColumn(ColumnDescription = "实际值转发中心看板")]
    public bool ForwardActual { get; set; }

    [SugarColumn(ColumnDescription = "上限转发中心看板")]
    public bool ForwardUpper { get; set; }

    [SugarColumn(ColumnDescription = "下限转发中心看板")]
    public bool ForwardLower { get; set; }

    [SugarColumn(ColumnDescription = "结果转发中心看板")]
    public bool ForwardResult { get; set; }

    [SugarColumn(ColumnDescription = "实际值写入报表")]
    public bool ReportActual { get; set; }

    [SugarColumn(ColumnDescription = "上限写入报表")]
    public bool ReportUpper { get; set; }

    [SugarColumn(ColumnDescription = "下限写入报表")]
    public bool ReportLower { get; set; }

    [SugarColumn(ColumnDescription = "结果写入报表")]
    public bool ReportResult { get; set; }

    [SugarColumn(ColumnDescription = "实际值上传MES")]
    public bool MesActual { get; set; }

    [SugarColumn(ColumnDescription = "上限上传MES")]
    public bool MesUpper { get; set; }

    [SugarColumn(ColumnDescription = "下限上传MES")]
    public bool MesLower { get; set; }

    [SugarColumn(ColumnDescription = "结果上传MES")]
    public bool MesResult { get; set; }

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "实际值MES字段名")]
    public string? ActualMesFieldName { get; set; }

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "上限MES字段名")]
    public string? UpperMesFieldName { get; set; }

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "下限MES字段名")]
    public string? LowerMesFieldName { get; set; }

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "结果MES字段名")]
    public string? ResultMesFieldName { get; set; }
}
