using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Data;
using System.Text.Json;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// Coordinates process-parameter upload tasks after a completed product has been collected.
/// </summary>
public sealed class WeldPointUploadCoordinatorService : IWeldPointUploadCoordinatorService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IUploadTaskService _uploadTaskService;
    private readonly IOperationLogService _operationLogService;
    private readonly IProductionFlowLogService _productionLogService;
    private readonly object _dbLock = new();
    private AppSettings _currentSettings;

    public WeldPointUploadCoordinatorService(
        SqlSugarDbContext dbContext,
        IAppSettingsService settingsService,
        IUploadTaskService uploadTaskService,
        IOperationLogService operationLogService,
        IProductionFlowLogService productionLogService)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
        _uploadTaskService = uploadTaskService;
        _operationLogService = operationLogService;
        _productionLogService = productionLogService;
    }

    /// <summary>
    /// Handles upload scheduling after one collected record reports that its product is complete.
    /// </summary>
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
                $"Batch upload mode, defer process parameter upload until finish report. TaskId={record.TaskId}, ProductNo={record.ProductNo}");
            return;
        }

        if (settings.UploadMode == UploadMode.Realtime)
        {
            var uploadTask = EnqueueProductUploadTask(record, settings.UploadMode);
            await _uploadTaskService.ExecuteAsync(uploadTask.Id, cancellationToken);
            return;
        }

        if (settings.UploadMode != UploadMode.Quantity)
        {
            return;
        }

        // 重测属修正性操作，数据正确性优先于减少 MES 调用次数，不等凑满批次即单独重传。
        if (IsRetestReupload(record, settings.ProcessParameterDeviceType))
        {
            var retestTask = EnqueueProductUploadTask(record, settings.UploadMode);
            _operationLogService.Write(
                "WeldPointUpload",
                $"Retest reupload bypassed quantity batch, TaskId={record.TaskId}, Station={record.StationNo}, ProductNo={record.ProductNo}, UploadTaskId={retestTask.Id}");
            await _uploadTaskService.ExecuteAsync(retestTask.Id, cancellationToken);
            return;
        }

        // 凑满批次后还要等下一个产品采集完成才上传，避开刚采完那一刻；
        // 等不到下一个产品就完工时，由 WeldTaskService 的完工补传兜底。
        var productNos = TakeUploadableQuantityBatchProductNos(record.TaskId, record.StationNo, settings.UploadBatchSize);
        if (productNos.Count == 0)
        {
            return;
        }

        var quantityTask = EnqueueQuantityBatchUploadTask(record, settings.UploadMode, productNos);
        WriteQuantityBatchCreatedLog(record, productNos, quantityTask.Id);
        await _uploadTaskService.ExecuteAsync(quantityTask.Id, cancellationToken);
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

    private BizUploadTask EnqueueQuantityBatchUploadTask(
        BizWeldPointRecord record,
        UploadMode uploadMode,
        IReadOnlyList<string> productNos)
    {
        return _uploadTaskService.EnqueueOrUpdate(new BizUploadTask
        {
            TaskType = ProductionConstants.UploadTaskTypes.ProcessParameter,
            Target = ProductionConstants.UploadTargets.Mes,
            BusinessId = ProcessParameterBatchUploadRules.BuildQuantityBusinessId(record.TaskId, record.StationNo, productNos),
            WeldTaskId = record.TaskId,
            PayloadJson = BuildQuantityBatchUploadPayload(record, uploadMode, productNos),
            Status = ProductionConstants.UploadStatuses.Pending,
            NextRetryTime = DateTime.Now,
            Message = $"Quantity upload batch is ready. ProductCount={productNos.Count}"
        });
    }

    private IReadOnlyList<string> TakeUploadableQuantityBatchProductNos(int weldTaskId, int stationNo, int configuredBatchSize)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var records = _dbContext.Db.Queryable<BizWeldPointRecord>()
                .Where(record => record.TaskId == weldTaskId
                    && record.StationNo == stationNo
                    && record.ProductCompleted
                    && record.UploadStatus != ProductionConstants.UploadStatuses.Uploaded)
                .ToList();
            var excludedProductNos = GetOpenProcessParameterProductNos(weldTaskId, stationNo);

            return ProcessParameterBatchUploadRules.TakeUploadableBatch(
                records,
                weldTaskId,
                stationNo,
                configuredBatchSize,
                excludedProductNos);
        }
    }

    private IReadOnlyList<string> GetOpenProcessParameterProductNos(int weldTaskId, int stationNo)
    {
        return _dbContext.Db.Queryable<BizUploadTask>()
            .Where(task => task.WeldTaskId == weldTaskId
                && task.TaskType == ProductionConstants.UploadTaskTypes.ProcessParameter
                && !task.IsDeleted
                && task.Status != ProductionConstants.UploadStatuses.Uploaded)
            .ToList()
            .Where(task =>
            {
                var scopedStationNo = ProcessParameterUploadPayloadRules.ReadStationNo(task.PayloadJson);
                return scopedStationNo <= 0 || scopedStationNo == stationNo;
            })
            .SelectMany(task => ProcessParameterUploadPayloadRules.ReadProductNos(task.PayloadJson))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildProductBusinessId(BizWeldPointRecord record)
    {
        return $"task-{record.TaskId}:s{record.StationNo}:pp:{record.ProductNo}";
    }

    /// <summary>
    /// 判断本次上传是否为重测重传。
    /// 数量模式下该产品若已存在上传成功的过程参数任务，说明本轮是覆盖后的重测数据。
    /// </summary>
    private bool IsRetestReupload(BizWeldPointRecord record, string? processParameterDeviceType)
    {
        if (!ProductRetestRules.IsSupportedDeviceType(processParameterDeviceType))
        {
            return false;
        }

        var businessId = BuildProductBusinessId(record);
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            return _dbContext.Db.Queryable<BizUploadTask>()
                .Any(task => task.BusinessId == businessId
                    && task.TaskType == ProductionConstants.UploadTaskTypes.ProcessParameter
                    && !task.IsDeleted
                    && task.Status == ProductionConstants.UploadStatuses.Uploaded);
        }
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

    private static string BuildQuantityBatchUploadPayload(
        BizWeldPointRecord record,
        UploadMode uploadMode,
        IReadOnlyList<string> productNos)
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
            ProductNos = productNos
        });
    }

    private void WriteQuantityBatchCreatedLog(BizWeldPointRecord record, IReadOnlyList<string> productNos, int uploadTaskId)
    {
        var detail = $"UploadTaskId={uploadTaskId}, BatchSize={productNos.Count}, ProductNos={string.Join(",", productNos)}";
        _operationLogService.Write("WeldPointUpload", $"Quantity process-parameter upload batch created. {detail}");
        _productionLogService.Write(
            "ProcessParameterQuantityBatchCreated",
            ProductionFlowLogTexts.Summaries.ProcessParameterQuantityBatchCreated,
            detail,
            "Info",
            record.StationNo,
            record.SN,
            string.Join(",", productNos));
    }

    private static string BuildPendingMessage(UploadMode uploadMode, BizWeldPointRecord record)
    {
        return uploadMode == UploadMode.Realtime
            ? $"Product {record.ProductNo} completed. Realtime process-parameter upload is ready."
            : $"Product {record.ProductNo} completed. Waiting for quantity upload threshold.";
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }
}
