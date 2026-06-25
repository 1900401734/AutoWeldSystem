using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// Generic upload/outbox task.
/// It keeps MES uploads, report files, and future forwarding tasks in one retry model.
/// </summary>
[SugarTable("Biz_UploadTask", TableDescription = "Upload task table")]
public class BizUploadTask
{
    /// <summary>
    /// Local database primary key.
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// Upload task type, for example start report, process parameter, report file, or finish report.
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "Upload task type")]
    public string TaskType { get; set; } = ProductionConstants.UploadTaskTypes.ProcessParameter;

    /// <summary>
    /// Target platform, such as MES or central server.
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "Upload target")]
    public string Target { get; set; } = ProductionConstants.UploadTargets.Mes;

    /// <summary>
    /// Business de-duplication key.
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "Business id")]
    public string? BusinessId { get; set; }

    /// <summary>
    /// Related weld task id.
    /// </summary>
    [SugarColumn(IsNullable = true, ColumnDescription = "Weld task id")]
    public int? WeldTaskId { get; set; }

    /// <summary>
    /// Serialized request payload or routing metadata.
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "Payload json")]
    public string? PayloadJson { get; set; }

    /// <summary>
    /// Report file path when this upload task points to a local file.
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true, ColumnDescription = "File path")]
    public string? FilePath { get; set; }

    /// <summary>
    /// Current upload status.
    /// </summary>
    [SugarColumn(Length = 20, ColumnDescription = "Upload status")]
    public string Status { get; set; } = ProductionConstants.UploadStatuses.Pending;

    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    [SugarColumn(ColumnDescription = "Retry count")]
    public int RetryCount { get; set; }

    /// <summary>
    /// Maximum retry count.
    /// </summary>
    [SugarColumn(ColumnDescription = "Max retry count")]
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Next retry time.
    /// </summary>
    [SugarColumn(IsNullable = true, ColumnDescription = "Next retry time")]
    public DateTime? NextRetryTime { get; set; }

    /// <summary>
    /// Last attempt time.
    /// </summary>
    [SugarColumn(IsNullable = true, ColumnDescription = "Last attempt time")]
    public DateTime? LastAttemptTime { get; set; }

    /// <summary>
    /// Completion time when upload succeeds.
    /// </summary>
    [SugarColumn(IsNullable = true, ColumnDescription = "Completed time")]
    public DateTime? CompletedTime { get; set; }

    /// <summary>
    /// Last processing message.
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "Message")]
    public string? Message { get; set; }

    /// <summary>
    /// Soft delete marker used by the upload-state page.
    /// Deleted rows are kept for diagnostics but excluded from retry queues.
    /// </summary>
    [SugarColumn(ColumnDescription = "Is deleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Time when the row was soft deleted.
    /// </summary>
    [SugarColumn(IsNullable = true, ColumnDescription = "Deleted time")]
    public DateTime? DeletedTime { get; set; }

    /// <summary>
    /// Created time.
    /// </summary>
    [SugarColumn(ColumnDescription = "Created time")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Last updated time.
    /// </summary>
    [SugarColumn(ColumnDescription = "Updated time")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}
