using SqlSugar;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// 记录焊接的过程数据，用于上传MES
/// </summary>
[SugarTable("Biz_WeldData", TableDescription = "焊接数据表")]
public class BizWeldData
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(ColumnDescription = "绑定的任务主键ID")]
    public int TaskId { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "产品编码")]
    public string ProductCode { get; set; } = string.Empty;

    [SugarColumn(Length = 20, ColumnDescription = "焊点编号")]
    public string TouchNo { get; set; } = string.Empty;

    [SugarColumn(ColumnDataType = "decimal(18,4)", ColumnDescription = "峰值电流")]
    public decimal PeakCurrent { get; set; }

    [SugarColumn(ColumnDataType = "decimal(18,4)", ColumnDescription = "峰值电压")]
    public decimal PeakVoltage { get; set; }

    [SugarColumn(ColumnDataType = "decimal(18,4)", ColumnDescription = "有效功率")]
    public decimal EffectivePower { get; set; }

    [SugarColumn(Length = 20, ColumnDescription = "焊点结果")]
    public string Result { get; set; } = "OK";

    [SugarColumn(ColumnDescription = "是否已上传")]
    public bool Uploaded { get; set; }

    [SugarColumn(ColumnDescription = "记录时间")]
    public DateTime RecordTime { get; set; } = DateTime.Now;
}
