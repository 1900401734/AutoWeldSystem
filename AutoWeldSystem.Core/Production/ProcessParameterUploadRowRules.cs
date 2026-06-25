using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Upload;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Builds process-parameter rows that are derived from product history instead of persisted upload tasks.
/// </summary>
public static class ProcessParameterUploadRowRules
{
    /// <summary>
    /// Creates read-only rows for completed products whose process parameters have not been fully uploaded.
    /// </summary>
    public static IReadOnlyList<UploadTaskSummary> CreatePendingProductRows(
        BizWeldTask task,
        IEnumerable<BizWeldPointRecord> records,
        int uploadBatchSize)
    {
        ArgumentNullException.ThrowIfNull(task);

        var normalizedBatchSize = Math.Max(1, uploadBatchSize);
        return records
            .Where(record => record.TaskId == task.Id)
            .Where(record => record.ProductCompleted)
            .Where(record => !string.Equals(record.UploadStatus, ProductionConstants.UploadStatuses.Uploaded, StringComparison.OrdinalIgnoreCase))
            .Where(record => !string.IsNullOrWhiteSpace(record.ProductNo))
            .GroupBy(record => new { record.StationNo, ProductNo = record.ProductNo.Trim() })
            .OrderBy(group => group.Key.StationNo)
            .ThenBy(group => group.Min(record => record.SequenceNo))
            .Select(group => CreatePendingProductRow(task, group.Key.StationNo, group.Key.ProductNo, group.ToList(), normalizedBatchSize))
            .Cast<UploadTaskSummary>()
            .ToList();
    }

    private static UploadTaskSummary CreatePendingProductRow(
        BizWeldTask task,
        int stationNo,
        string productNo,
        IReadOnlyList<BizWeldPointRecord> records,
        int uploadBatchSize)
    {
        var status = UploadSummaryStatusResolver.AggregateUploadStatuses(records.Select(record => record.UploadStatus));
        var updatedTime = records
            .Select(record => record.UploadTime ?? record.Ts)
            .DefaultIfEmpty(DateTime.Now)
            .Max();

        return new UploadTaskSummary
        {
            Id = 0,
            TaskType = ProductionConstants.UploadTaskTypes.ProcessParameter,
            Target = ProductionConstants.UploadTargets.Mes,
            BusinessId = $"history:{task.Id}:{stationNo}:{productNo}",
            TaskIdentity = UploadTaskIdentityRules.Resolve(task),
            StationNo = stationNo,
            ProductNo = productNo,
            Status = status,
            IsVirtual = true,
            CanRetry = false,
            CanDelete = false,
            RetryCount = records.Sum(record => Math.Max(0, record.RetryCount)),
            MaxRetryCount = 0,
            FilePath = string.Empty,
            Message = CreateWaitingMessage(uploadBatchSize),
            DisplayMessage = CreateWaitingMessage(uploadBatchSize),
            CreatedTime = records.Min(record => record.Ts),
            UpdatedTime = updatedTime
        };
    }

    private static string CreateWaitingMessage(int uploadBatchSize)
    {
        return uploadBatchSize <= 1
            ? "等待自动创建过程参数上传任务。"
            : $"等待达到批次数量 {uploadBatchSize} 后自动创建过程参数上传任务。";
    }
}
