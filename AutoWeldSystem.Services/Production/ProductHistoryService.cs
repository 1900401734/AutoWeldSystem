using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.ViewModels;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// Provides product-level history for MonitorView.
/// The database keeps one row per weld point, so this service groups rows into product parents.
/// 产品级人工动作（试焊件、重焊预约、软删、撤销）都在这里落库：按产品编号批量改写全部焊点行，读侧用 Any 聚合。
/// </summary>
public sealed class ProductHistoryService : IProductHistoryService
{
    private const string OperationCategory = "ProductHistory";

    private readonly SqlSugarDbContext _dbContext;
    private readonly ICenterProductForwardingService _centerProductForwardingService;
    private readonly IUploadTaskService? _uploadTaskService;
    private readonly IWeldPointUploadCoordinatorService? _uploadCoordinatorService;
    private readonly IAppSettingsService? _settingsService;
    private readonly IOperationLogService? _operationLogService;
    private readonly object _dbLock = new();

    public ProductHistoryService(
        SqlSugarDbContext dbContext,
        ICenterProductForwardingService centerProductForwardingService,
        IUploadTaskService? uploadTaskService = null,
        IWeldPointUploadCoordinatorService? uploadCoordinatorService = null,
        IAppSettingsService? settingsService = null,
        IOperationLogService? operationLogService = null)
    {
        _dbContext = dbContext;
        _centerProductForwardingService = centerProductForwardingService;
        _uploadTaskService = uploadTaskService;
        _uploadCoordinatorService = uploadCoordinatorService;
        _settingsService = settingsService;
        _operationLogService = operationLogService;
    }

    public ProductHistorySnapshot GetSnapshot(int taskId, int stationNo)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            EnsureTaskTouchCount(taskId);
            var records = GetTaskStationRecords(taskId, stationNo);

            return new ProductHistorySnapshot
            {
                TaskId = taskId,
                StationNo = stationNo,
                Products = BuildProducts(records)
            };
        }
    }

    public ProductHistoryMarkResult SetProductTestFlag(int taskId, int stationNo, string productNo, bool isTest)
    {
        return ApplyProductAction(
            taskId,
            stationNo,
            productNo,
            "试焊件标记",
            requireOperable: true,
            allowDeleted: false,
            apply: normalizedProductNo => _dbContext.Db.Updateable<BizWeldPointRecord>()
                // Product-level marking must update all weld points under the same ProductNumber,
                // otherwise the process-parameter upload payload would contain mixed IsTest values.
                .SetColumns(record => record.IsTest == isTest)
                .Where(record => record.TaskId == taskId
                    && record.StationNo == stationNo
                    && record.ProductNo == normalizedProductNo)
                .ExecuteCommand(),
            successMessage: normalizedProductNo => isTest
                ? $"产品 {normalizedProductNo} 已标记为试焊件。"
                : $"产品 {normalizedProductNo} 已取消试焊件标记。");
    }

    public ProductHistoryMarkResult MarkReweld(int taskId, int stationNo, string productNo)
    {
        return ApplyProductAction(
            taskId,
            stationNo,
            productNo,
            "重焊预约",
            requireOperable: true,
            allowDeleted: false,
            apply: normalizedProductNo =>
            {
                // 单槽位：同任务同工位只能有一件待覆盖产品，改换目标时先清掉旧预约，避免下一件覆盖到错的产品。
                _dbContext.Db.Updateable<BizWeldPointRecord>()
                    .SetColumns(record => record.IsReweldPending == false)
                    .Where(record => record.TaskId == taskId
                        && record.StationNo == stationNo
                        && record.IsReweldPending)
                    .ExecuteCommand();
                _dbContext.Db.Updateable<BizWeldPointRecord>()
                    .SetColumns(record => record.IsReweldPending == true)
                    .Where(record => record.TaskId == taskId
                        && record.StationNo == stationNo
                        && record.ProductNo == normalizedProductNo)
                    .ExecuteCommand();
            },
            successMessage: normalizedProductNo => $"产品 {normalizedProductNo} 已预约重焊，下一件采集将覆盖该产品。",
            refreshCenter: false);
    }

    public ProductHistoryMarkResult CancelReweld(int taskId, int stationNo, string productNo)
    {
        return ApplyProductAction(
            taskId,
            stationNo,
            productNo,
            "取消重焊预约",
            requireOperable: false,
            allowDeleted: false,
            apply: normalizedProductNo => _dbContext.Db.Updateable<BizWeldPointRecord>()
                .SetColumns(record => record.IsReweldPending == false)
                .Where(record => record.TaskId == taskId
                    && record.StationNo == stationNo
                    && record.ProductNo == normalizedProductNo)
                .ExecuteCommand(),
            successMessage: normalizedProductNo => $"产品 {normalizedProductNo} 已取消重焊预约。",
            refreshCenter: false);
    }

    public ProductHistoryMarkResult DeleteProduct(int taskId, int stationNo, string productNo)
    {
        return ApplyProductAction(
            taskId,
            stationNo,
            productNo,
            "删除产品",
            requireOperable: true,
            allowDeleted: false,
            apply: normalizedProductNo =>
            {
                _dbContext.Db.Updateable<BizWeldPointRecord>()
                    .SetColumns(record => record.IsDeleted == true)
                    .SetColumns(record => record.IsReweldPending == false)
                    .Where(record => record.TaskId == taskId
                        && record.StationNo == stationNo
                        && record.ProductNo == normalizedProductNo)
                    .ExecuteCommand();
                // 已入队但未上传成功的过程参数任务会被无限重试，必须置为终态，否则已删产品仍会被上报。
                _uploadTaskService?.SkipProcessParameterTasks(taskId, stationNo, normalizedProductNo);
            },
            successMessage: normalizedProductNo => $"产品 {normalizedProductNo} 已删除，不再参与上传、报表与产量统计。");
    }

    public ProductHistoryMarkResult RestoreProduct(int taskId, int stationNo, string productNo)
    {
        var result = ApplyProductAction(
            taskId,
            stationNo,
            productNo,
            "撤销删除",
            requireOperable: false,
            allowDeleted: true,
            apply: normalizedProductNo => _dbContext.Db.Updateable<BizWeldPointRecord>()
                // 用 == 逐列赋值：SqlSugar 的 new T{} 形式会忽略 null 值，无法把上传时间与消息清空。
                .SetColumns(record => record.IsDeleted == false)
                .SetColumns(record => record.UploadStatus == ProductionConstants.UploadStatuses.Pending)
                .SetColumns(record => record.UploadTime == null)
                .SetColumns(record => record.UploadMessage == null)
                .SetColumns(record => record.RetryCount == 0)
                .Where(record => record.TaskId == taskId
                    && record.StationNo == stationNo
                    && record.ProductNo == normalizedProductNo)
                .ExecuteCommand(),
            successMessage: normalizedProductNo => $"产品 {normalizedProductNo} 已恢复。");

        if (result.IsSuccess)
        {
            RequeueUpload(taskId, stationNo, productNo.Trim());
        }

        return result;
    }

    /// <summary>
    /// 撤销删除后按当前上传模式重新入队：Batch 模式等完工补传；Realtime/Quantity 交给上传协调服务按既有规则处理。
    /// 调用方在 UI 线程，上传可能触发 HTTP，因此后台执行；入队失败不回滚已恢复的本地数据，由完工补传兜底。
    /// </summary>
    private void RequeueUpload(int taskId, int stationNo, string productNo)
    {
        if (_uploadCoordinatorService is null || _settingsService is null
            || _settingsService.Get().UploadMode == UploadMode.Batch)
        {
            return;
        }

        BizWeldPointRecord? lastRecord;
        lock (_dbLock)
        {
            lastRecord = GetTaskStationRecords(taskId, stationNo)
                .Where(record => string.Equals(record.ProductNo, productNo, StringComparison.OrdinalIgnoreCase))
                .LastOrDefault(record => record.ProductCompleted);
        }

        if (lastRecord is null)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _uploadCoordinatorService.HandleCollectedAsync(lastRecord);
            }
            catch (Exception ex)
            {
                _operationLogService?.Write(
                    OperationCategory,
                    $"Requeue after restore failed, TaskId={taskId}, Station={stationNo}, ProductNumber={productNo}, Error={ex.Message}");
            }
        });
    }

    /// <summary>
    /// 产品级动作的统一骨架：锁 → 查记录 → 校验 → 批量改写 → 重查 → 重推看板 → 写操作日志。
    /// 服务层不信任 UI 门禁，重新查库校验。
    /// </summary>
    private ProductHistoryMarkResult ApplyProductAction(
        int taskId,
        int stationNo,
        string productNo,
        string actionName,
        bool requireOperable,
        bool allowDeleted,
        Action<string> apply,
        Func<string, string> successMessage,
        bool refreshCenter = true)
    {
        var normalizedProductNo = productNo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedProductNo))
        {
            return ProductHistoryMarkResult.Failed($"产品编号为空，无法{actionName}。");
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            EnsureTaskTouchCount(taskId);
            var records = GetTaskStationRecords(taskId, stationNo)
                .Where(record => string.Equals(record.ProductNo, normalizedProductNo, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (records.Count == 0 || !records.Any(record => record.ProductCompleted))
            {
                return ProductHistoryMarkResult.Failed($"未找到已完成采集的产品，无法{actionName}。");
            }

            var isDeleted = records.Any(record => record.IsDeleted);
            if (isDeleted && !allowDeleted)
            {
                return ProductHistoryMarkResult.Failed($"产品已删除，无法{actionName}。");
            }

            if (!isDeleted && allowDeleted)
            {
                return ProductHistoryMarkResult.Failed("产品未删除，无需撤销。");
            }

            if (requireOperable && !ProductHistoryActionRules.CanOperate(records, out var disabledReason))
            {
                return ProductHistoryMarkResult.Failed(disabledReason);
            }

            apply(normalizedProductNo);

            var updatedRecords = GetTaskStationRecords(taskId, stationNo)
                .Where(record => string.Equals(record.ProductNo, normalizedProductNo, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var product = BuildProduct(updatedRecords);

            if (refreshCenter)
            {
                // 动作发生在采集完成之后，中心看板此前已收到旧数据，
                // 因此按更新后的记录重新入队一次：BusinessId 幂等，中心侧按同产品覆盖旧行。
                RefreshCenterReport(
                    _dbContext.Db.Queryable<BizWeldTask>().InSingle(taskId),
                    stationNo,
                    updatedRecords);
            }

            _operationLogService?.Write(
                OperationCategory,
                $"{actionName}, TaskId={taskId}, Station={stationNo}, ProductNumber={normalizedProductNo}, Operator={GlobalContext.CurrentUser?.UserNumber ?? GlobalContext.CurrentUser?.UserName ?? string.Empty}");

            return product is null
                ? ProductHistoryMarkResult.Failed($"{actionName}已保存，但刷新产品历史失败。")
                : ProductHistoryMarkResult.Success(product, successMessage(normalizedProductNo));
        }
    }

    /// <summary>
    /// 标记变更后重推该产品到中心看板，使看板报表与本地一致。
    /// 中心同步未启用时入队方法自行短路；重推失败不得回滚已保存的本地标记，
    /// 因此只吞异常，产品数据仍由现有重试队列在下次完工补漏时补齐。
    /// </summary>
    private void RefreshCenterReport(
        BizWeldTask? task,
        int stationNo,
        IReadOnlyList<BizWeldPointRecord> records)
    {
        if (task is null || records.Count == 0)
        {
            return;
        }

        try
        {
            _centerProductForwardingService.EnqueueCompletedProduct(task, stationNo, records);
        }
        catch (Exception)
        {
            // 本地标记已生效，看板重推属于尽力而为，不能让它推翻用户已完成的操作。
        }
    }

    private IReadOnlyList<BizWeldPointRecord> GetTaskStationRecords(int taskId, int stationNo)
    {
        return _dbContext.Db.Queryable<BizWeldPointRecord>()
            .Where(record => record.TaskId == taskId && record.StationNo == stationNo)
            .ToList()
            .OrderBy(record => record.ProductNo, NaturalSortComparer.Instance)
            .ThenBy(record => record.SequenceNo)
            .ThenBy(record => record.Id)
            .ToList();
    }

    private void EnsureTaskTouchCount(int taskId)
    {
        var task = _dbContext.Db.Queryable<BizWeldTask>().InSingle(taskId);
        _ = ProgramContentJsonRules.GetRequiredTouchCount(task?.ProgramContentSnapshot);
    }

    private static IReadOnlyList<ProductHistoryProduct> BuildProducts(IReadOnlyList<BizWeldPointRecord> records)
    {
        return records
            .Where(record => !string.IsNullOrWhiteSpace(record.ProductNo))
            .GroupBy(record => record.ProductNo, StringComparer.OrdinalIgnoreCase)
            .Select(group => BuildProduct(group.ToList()))
            .Where(product => product is not null)
            .Cast<ProductHistoryProduct>()
            .OrderBy(product => product.LastRecordTime ?? DateTime.MinValue)
            .ThenBy(product => product.ProductNo)
            .ToList();
    }

    private static ProductHistoryProduct? BuildProduct(IReadOnlyList<BizWeldPointRecord> records)
    {
        if (records.Count == 0 || !records.Any(record => record.ProductCompleted))
        {
            return null;
        }

        var orderedRecords = records
            .OrderBy(record => record.SequenceNo)
            .ThenBy(record => record.Id)
            .ToList();
        var firstRecord = orderedRecords[0];
        var canOperate = ProductHistoryActionRules.CanOperate(orderedRecords, out var disabledReason);

        return new ProductHistoryProduct
        {
            TaskId = firstRecord.TaskId,
            StationNo = firstRecord.StationNo,
            ProductNo = firstRecord.ProductNo,
            Result = ResolveProductResult(orderedRecords),
            UploadStatus = ResolveProductUploadStatus(orderedRecords),
            // 产品级标记冗余在每条焊点行上，任一行为真即视为已标记，个别行漏改也不会出现半格状态。
            IsTest = orderedRecords.Any(record => record.IsTest),
            IsDeleted = orderedRecords.Any(record => record.IsDeleted),
            IsReweldPending = orderedRecords.Any(record => record.IsReweldPending),
            TouchCount = orderedRecords.Count,
            LastRecordTime = orderedRecords.Max(record => record.Ts),
            Points = orderedRecords.Select(ToPoint).ToList(),
            CanMarkTest = canOperate,
            MarkDisabledReason = disabledReason,
            CanOperate = canOperate,
            OperateDisabledReason = disabledReason
        };
    }

    /// <summary>
    /// 产品结果解析统一委托给 Core 的 <see cref="ProductResultResolver"/>，与完工统计、中心转发同口径；
    /// 保留该入口供回归测试通过反射验证“不按焊点结果聚合”的约束。
    /// </summary>
    private static string ResolveProductResult(IReadOnlyList<BizWeldPointRecord> records)
        => ProductResultResolver.Resolve(records);

    private static ProductHistoryPoint ToPoint(BizWeldPointRecord record)
    {
        return new ProductHistoryPoint
        {
            Id = record.Id,
            SequenceNo = record.SequenceNo,
            TouchNo = record.TouchNo,
            Result = record.TestResult,
            UploadStatus = record.UploadStatus,
            IsTest = record.IsTest,
            RecordTime = record.Ts,
            RawDataJson = record.RawDataJson ?? string.Empty
        };
    }

    private static string ResolveProductUploadStatus(IReadOnlyList<BizWeldPointRecord> records)
    {
        var statuses = records.Select(record => record.UploadStatus).ToList();
        if (statuses.All(status => status == ProductionConstants.UploadStatuses.Uploaded))
        {
            return ProductionConstants.UploadStatuses.Uploaded;
        }

        if (statuses.Any(status => status == ProductionConstants.UploadStatuses.Uploading))
        {
            return ProductionConstants.UploadStatuses.Uploading;
        }

        if (statuses.Any(status => status == ProductionConstants.UploadStatuses.Failed))
        {
            return ProductionConstants.UploadStatuses.Failed;
        }

        if (statuses.Any(status => status == ProductionConstants.UploadStatuses.Retrying))
        {
            return ProductionConstants.UploadStatuses.Retrying;
        }

        if (statuses.Any(status => status == ProductionConstants.UploadStatuses.Pending))
        {
            return ProductionConstants.UploadStatuses.Pending;
        }

        return statuses.FirstOrDefault() ?? ProductionConstants.UploadStatuses.Pending;
    }
}
