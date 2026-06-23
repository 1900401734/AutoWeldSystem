using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// Product point record forwarded from an equipment client and stored by the center server.
/// </summary>
[SugarTable("Center_ProductRecord", TableDescription = "中心服务器产品采集记录表")]
public sealed class CenterProductRecord
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 50)]
    public string DeviceId { get; set; } = string.Empty;

    [SugarColumn(Length = 100)]
    public string DeviceName { get; set; } = string.Empty;

    [SugarColumn(Length = 80)]
    public string SystemType { get; set; } = string.Empty;

    public int StationNo { get; set; } = 1;

    [SugarColumn(Length = 50)]
    public string WorkOrder { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string ProductJobNo { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string ProductNo { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string ProductModel { get; set; } = string.Empty;

    [SugarColumn(Length = 20)]
    public string ProductResult { get; set; } = string.Empty;

    public int SequenceNo { get; set; }

    [SugarColumn(Length = 50)]
    public string TouchNo { get; set; } = string.Empty;

    [SugarColumn(Length = 20)]
    public string TestResult { get; set; } = string.Empty;

    public DateTime CollectedAt { get; set; } = DateTime.Now;

    public DateTime CompletedAt { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? RawDataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
