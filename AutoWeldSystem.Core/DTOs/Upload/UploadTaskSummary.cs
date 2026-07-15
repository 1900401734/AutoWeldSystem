namespace AutoWeldSystem.Core.DTOs.Upload;

/// <summary>
/// 上传状态界面使用的通用上传任务摘要。
/// 过程参数、报告文件和未来的转发任务都可以通过该模型展示。
/// </summary>
public sealed class UploadTaskSummary
{
    public int Id { get; set; }

    public string TaskType { get; set; } = string.Empty;

    public string Target { get; set; } = string.Empty;

    public string BusinessId { get; set; } = string.Empty;

    /// <summary>
    /// Related device-status log ID when this row represents a device-status upload.
    /// </summary>
    public int? DeviceStatusLogId { get; set; }

    public string TaskIdentity { get; set; } = string.Empty;

    /// <summary>
    /// Station number associated with the upload row. Virtual process-parameter rows use it for product-history grouping.
    /// </summary>
    public int StationNo { get; set; }

    /// <summary>
    /// Product number represented by a process-parameter row. Empty for task-level rows.
    /// </summary>
    public string ProductNo { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// True when the row is derived from product history rather than a persisted upload task.
    /// </summary>
    public bool IsVirtual { get; set; }

    /// <summary>
    /// Whether the row can be manually retried from the upload-state page.
    /// </summary>
    public bool CanRetry { get; set; } = true;

    /// <summary>
    /// Whether the row can be manually deleted from the upload-state page.
    /// </summary>
    public bool CanDelete { get; set; } = true;

    public int RetryCount { get; set; }

    public int MaxRetryCount { get; set; }

    public DateTime? NextRetryTime { get; set; }

    public DateTime? LastAttemptTime { get; set; }

    public DateTime? CompletedTime { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Operator-facing message. Falls back to <see cref="Message"/> when empty.
    /// </summary>
    public string DisplayMessage { get; set; } = string.Empty;

    public DateTime CreatedTime { get; set; }

    public DateTime UpdatedTime { get; set; }
}
