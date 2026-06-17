using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Data;
using System.Text.Json;
using AutoWeldSystem.Core.Runtime;

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
    private AppSettings _currentSettings;

    public WeldPointUploadCoordinatorService(
        SqlSugarDbContext dbContext,
        IAppSettingsService settingsService,
        IUploadTaskService uploadTaskService,
        IOperationLogService operationLogService)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
        _uploadTaskService = uploadTaskService;
        _operationLogService = operationLogService;
    }

    public async Task HandleCollectedAsync(BizWeldPointRecord record, CancellationToken cancellationToken = default)
    {
        if (!record.ProductCompleted)
        {
            return;
        }

        var settings = CurrentSettings;
        if (settings.UploadMode == UploadMode.Batch)
        {
            _operationLogService.Write(
                "WeldPointUpload",
                $"Batch upload mode, defer process parameter upload until finish report. TaskId={record.TaskId}, ProductNumber={record.ProductNo}");
            return;
        }

        if (settings.UploadMode == UploadMode.Realtime)
        {
            var uploadTask = EnqueueProductUploadTask(record, settings.UploadMode);
            await _uploadTaskService.ExecuteAsync(uploadTask.Id, cancellationToken);
            return;
        }

        var taskUploadTask = EnqueueTaskUploadTask(record, settings.UploadMode);
        if (settings.UploadMode == UploadMode.Quantity && IsQuantityThresholdReached(record.TaskId, settings.UploadBatchSize))
        {
            await _uploadTaskService.ExecuteAsync(taskUploadTask.Id, cancellationToken);
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

    private BizUploadTask EnqueueTaskUploadTask(BizWeldPointRecord record, UploadMode uploadMode)
    {
        return _uploadTaskService.EnqueueOrUpdate(new BizUploadTask
        {
            TaskType = ProductionConstants.UploadTaskTypes.ProcessParameter,
            Target = ProductionConstants.UploadTargets.Mes,
            BusinessId = BuildTaskBusinessId(record),
            WeldTaskId = record.TaskId,
            PayloadJson = BuildTaskUploadPayload(record, uploadMode),
            Status = ProductionConstants.UploadStatuses.Pending,
            NextRetryTime = DateTime.Now,
            Message = $"已达到或等待达到上传数量阈值，任务 {record.TaskId} 的待上传产品将批量上传。"
        });
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

    private static string BuildProductBusinessId(BizWeldPointRecord record)
    {
        // BizUploadTask.BusinessId is limited to 100 chars; station is part of the product identity on dual-station equipment.
        return $"task-{record.TaskId}:s{record.StationNo}:pp:{record.ProductNo}";
    }

    private static string BuildTaskBusinessId(BizWeldPointRecord record)
    {
        // Quantity mode intentionally uses one task-level upload task,
        // so one execution uploads all currently pending products together.
        return $"task-{record.TaskId}:pp:quantity";
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

    private static string BuildTaskUploadPayload(BizWeldPointRecord record, UploadMode uploadMode)
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
            record.ProcessNo
        });
    }

    private static string BuildPendingMessage(UploadMode uploadMode, BizWeldPointRecord record)
    {
        return uploadMode == UploadMode.Realtime
            ? $"产品 {record.ProductNo} 已采集完整，准备实时上传过程参数。"
            : $"产品 {record.ProductNo} 已采集完整，等待达到特定数量后上传过程参数。";
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }

}
