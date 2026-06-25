namespace AutoWeldSystem.Core.DTOs.Upload;

/// <summary>
/// Event payload raised when an upload task or upload-summary visibility changes.
/// </summary>
public sealed class UploadTaskStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Related upload task id when the change came from a BizUploadTask row.
    /// </summary>
    public int UploadTaskId { get; init; }

    /// <summary>
    /// Related weld task id when known.
    /// </summary>
    public int? WeldTaskId { get; init; }

    /// <summary>
    /// Upload task type, for example ProcessParameter.
    /// </summary>
    public string TaskType { get; init; } = string.Empty;

    /// <summary>
    /// Current upload status or the synthetic operation name for visibility changes.
    /// </summary>
    public string Status { get; init; } = string.Empty;
}
