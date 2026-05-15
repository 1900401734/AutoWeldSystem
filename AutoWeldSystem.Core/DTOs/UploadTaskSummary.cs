namespace AutoWeldSystem.Core.DTOs;

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

    public string Status { get; set; } = string.Empty;

    public int RetryCount { get; set; }

    public int MaxRetryCount { get; set; }

    public DateTime? NextRetryTime { get; set; }

    public DateTime? LastAttemptTime { get; set; }

    public DateTime? CompletedTime { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedTime { get; set; }

    public DateTime UpdatedTime { get; set; }
}
