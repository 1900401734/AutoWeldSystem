namespace AutoWeldSystem.Core.DTOs.Upload;

/// <summary>
/// 上传总览行。
/// 总览只展示当前任务补传链路：开工、过程参数、xlsx 报表、完工。
/// </summary>
public sealed class UploadPendingSummaryRow
{
    public int SequenceNo { get; set; }

    /// <summary>
    /// MES 已返回时显示 ExpStartId；离线未补传开工时显示本地 32 位 GUID。
    /// </summary>
    public string TaskIdentity { get; set; } = string.Empty;

    public string WorkOrderId { get; set; } = string.Empty;

    public int StationNo { get; set; }

    public string StartReportStatus { get; set; } = string.Empty;

    public string ProcessParameterStatus { get; set; } = string.Empty;

    public string ReportFileStatus { get; set; } = string.Empty;

    public string FinishReportStatus { get; set; } = string.Empty;

    public int PendingCount { get; set; }

    public DateTime UpdatedTime { get; set; }
}
