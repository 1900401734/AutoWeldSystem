using AutoWeldSystem.Core.DTOs.Upload;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 通用上传任务服务。
/// 负责查询和重新排队本地上传任务，实际上传执行器后续按任务类型逐步接入。
/// </summary>
public interface IUploadTaskService
{
    event EventHandler<UploadTaskStatusChangedEventArgs>? TaskStatusChanged;

    IReadOnlyList<UploadTaskSummary> GetTasks(string taskType, bool includeCompleted = false);

    /// <summary>
    /// Gets process-parameter rows, including persisted upload tasks and read-only product-history rows.
    /// </summary>
    IReadOnlyList<UploadTaskSummary> GetProcessParameterRows(bool includeCompleted = false);

    UploadTaskSummary? GetById(int id);

    BizUploadTask EnqueueOrUpdate(BizUploadTask task);

    Task<UploadTaskSummary?> ExecuteAsync(int id, CancellationToken cancellationToken = default);

    Task<int> ExecuteAllPendingAsync(string taskType, CancellationToken cancellationToken = default);

    void RequestRetry(int id);

    int RequestRetryAll(string taskType);

    void DeleteTask(int id);

    void HideWeldTaskUploadState(int weldTaskId);

    /// <summary>
    /// Deletes all weld point records for a specific product in a virtual process-parameter row.
    /// </summary>
    /// <param name="weldTaskId">Weld task id.</param>
    /// <param name="stationNo">Station number.</param>
    /// <param name="productNo">Product number.</param>
    void DeleteProcessParameterVirtualRow(int weldTaskId, int stationNo, string productNo);
}
