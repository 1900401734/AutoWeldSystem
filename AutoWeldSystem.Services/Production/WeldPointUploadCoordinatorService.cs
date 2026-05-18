using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Data;
using System.Text.Json;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// Creates and executes process-parameter upload tasks after a product's weld points are complete.
/// Forwarding to a central server is deliberately left as a separate target so it can be added later without changing collection.
/// </summary>
public sealed class WeldPointUploadCoordinatorService : IWeldPointUploadCoordinatorService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IUploadTaskService _uploadTaskService;
    private readonly IOperationLogService _operationLogService;
    private readonly object _dbLock = new();

    public WeldPointUploadCoordinatorService(
        SqlSugarDbContext dbContext,
        IAppSettingsService settingsService,
        IUploadTaskService uploadTaskService,
        IOperationLogService operationLogService)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _uploadTaskService = uploadTaskService;
        _operationLogService = operationLogService;
    }

    public async Task HandleCollectedAsync(BizWeldPointRecord record, CancellationToken cancellationToken = default)
    {
        if (!record.ProductCompleted)
        {
            return;
        }

        var settings = _settingsService.Get();
        if (settings.UploadMode == UploadMode.Batch)
        {
            _operationLogService.Write(
                "WeldPointUpload",
                $"Batch upload mode, defer process parameter upload until finish report. TaskId={record.TaskId}, ProductNo={record.ProductNo}");
            return;
        }

        var uploadTask = EnqueueProductUploadTask(record, settings.UploadMode);
        if (settings.UploadMode == UploadMode.Realtime)
        {
            await _uploadTaskService.ExecuteAsync(uploadTask.Id, cancellationToken);
            return;
        }

        if (settings.UploadMode == UploadMode.Quantity && IsQuantityThresholdReached(record.TaskId, settings.UploadBatchSize))
        {
            await ExecutePendingProductTasksAsync(record.TaskId, cancellationToken);
        }
    }

    private BizUploadTask EnqueueProductUploadTask(BizWeldPointRecord record, UploadMode uploadMode)
    {
        return _uploadTaskService.EnqueueOrUpdate(new BizUploadTask
        {
            TaskType = ProductionConstants.UploadTaskTypes.ProcessParameter,
            Target = ProductionConstants.UploadTargets.Mes,
            BusinessId = BuildProductBusinessId(record),
            WeldTaskId = record.TaskId,
            PayloadJson = BuildProductUploadPayload(record, uploadMode),
            Status = ProductionConstants.UploadStatuses.Pending,
            NextRetryTime = DateTime.Now,
            Message = BuildPendingMessage(uploadMode, record)
        });
    }

    private async Task ExecutePendingProductTasksAsync(int weldTaskId, CancellationToken cancellationToken)
    {
        foreach (var taskId in GetPendingProductUploadTaskIds(weldTaskId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _uploadTaskService.ExecuteAsync(taskId, cancellationToken);
        }
    }

    private bool IsQuantityThresholdReached(int weldTaskId, int configuredBatchSize)
    {
        var batchSize = Math.Max(1, configuredBatchSize);
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var pendingCompletedProducts = _dbContext.Db.Queryable<BizWeldPointRecord>()
                .Where(record => record.TaskId == weldTaskId
                    && record.ProductCompleted
                    && record.UploadStatus != ProductionConstants.UploadStatuses.Uploaded)
                .ToList()
                .Select(record => record.ProductNo)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            return pendingCompletedProducts >= batchSize;
        }
    }

    private IReadOnlyList<int> GetPendingProductUploadTaskIds(int weldTaskId)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            return _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.WeldTaskId == weldTaskId
                    && task.TaskType == ProductionConstants.UploadTaskTypes.ProcessParameter
                    && task.Status != ProductionConstants.UploadStatuses.Uploaded)
                .ToList()
                .Where(IsProductScopedTask)
                .OrderBy(task => task.CreatedTime)
                .Select(task => task.Id)
                .ToList();
        }
    }

    private static bool IsProductScopedTask(BizUploadTask task)
    {
        return !string.IsNullOrWhiteSpace(ReadProductNo(task.PayloadJson));
    }

    private static string BuildProductBusinessId(BizWeldPointRecord record)
    {
        // BizUploadTask.BusinessId is limited to 100 chars; local task id + ProductNo is stable and compact.
        return $"task-{record.TaskId}:pp:{record.ProductNo}";
    }

    private static string BuildProductUploadPayload(BizWeldPointRecord record, UploadMode uploadMode)
    {
        return JsonSerializer.Serialize(new
        {
            TaskType = ProductionConstants.UploadTaskTypes.ProcessParameter,
            UploadMode = uploadMode.ToString(),
            record.TaskId,
            record.StationNo,
            record.ExpStartId,
            record.DeviceId,
            record.SN,
            record.ProcessNo,
            record.ProductNo
        });
    }

    private static string BuildPendingMessage(UploadMode uploadMode, BizWeldPointRecord record)
    {
        return uploadMode == UploadMode.Realtime
            ? $"产品 {record.ProductNo} 已采集完整，准备实时上传过程参数。"
            : $"产品 {record.ProductNo} 已采集完整，等待达到特定数量后上传过程参数。";
    }

    private static string? ReadProductNo(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            return document.RootElement.TryGetProperty("ProductNo", out var productNoElement)
                ? productNoElement.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
