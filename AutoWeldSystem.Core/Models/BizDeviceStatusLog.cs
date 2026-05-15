using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// Local device status log.
/// Device logs are kept locally first; later they can be uploaded as report files if required.
/// </summary>
[SugarTable("Biz_DeviceStatusLog", TableDescription = "设备状态日志表")]
public class BizDeviceStatusLog
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "设备编号")]
    public string DeviceId { get; set; } = string.Empty;

    [SugarColumn(Length = 10, ColumnDescription = "设备状态编码")]
    public string DeviceStatus { get; set; } = ProductionConstants.MesDeviceStatuses.PoweredOn;

    [SugarColumn(Length = 50, ColumnDescription = "设备状态名称")]
    public string StatusName { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "来源")]
    public string Source { get; set; } = "Software";

    [SugarColumn(Length = 300, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }

    [SugarColumn(ColumnDescription = "发生时间")]
    public DateTime OccurredTime { get; set; } = DateTime.Now;

    [SugarColumn(Length = 20, ColumnDescription = "上报状态")]
    public string ReportStatus { get; set; } = ProductionConstants.UploadStatuses.Pending;

    [SugarColumn(IsNullable = true, ColumnDescription = "上报时间")]
    public DateTime? ReportTime { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "上报消息")]
    public string? ReportMessage { get; set; }
}
