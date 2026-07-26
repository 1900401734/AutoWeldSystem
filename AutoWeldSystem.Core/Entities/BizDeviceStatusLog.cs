using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// 设备状态 JSONL 记录；Id 只用于兼容旧文件，新记录使用 RecordId。
/// </summary>
public class BizDeviceStatusLog
{
    public string? RecordId { get; set; }

    public int Id { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public int StationNo { get; set; } = ProductionConstants.Stations.DefaultStationNo;

    public int? WeldTaskId { get; set; }

    public string? WorkOrderId { get; set; }

    public string DeviceStatus { get; set; } = string.Empty;

    public string StatusName { get; set; } = string.Empty;

    public string Source { get; set; } = "Software";

    public string? Remark { get; set; }

    public string? AlarmAddress { get; set; }

    public string? AlarmContent { get; set; }

    public DateTime OccurredTime { get; set; } = DateTime.Now;

    public string ReportStatus { get; set; } = ProductionConstants.UploadStatuses.Pending;

    public DateTime? ReportTime { get; set; }

    public string? ReportMessage { get; set; }
}
