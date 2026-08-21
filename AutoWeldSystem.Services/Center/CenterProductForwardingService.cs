using System.Text.Json;
using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Data;
using AutoWeldSystem.Services.Production;

namespace AutoWeldSystem.Services.Center;

/// <summary>
/// Queues completed products locally and forwards them to the center server on a background loop.
/// </summary>
public sealed class CenterProductForwardingService : ICenterProductForwardingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IUploadTaskService _uploadTaskService;
    private readonly IProductionFlowLogService _productionLogService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly IProductProcessConfigService _productProcessConfigService;
    private readonly CenterTelemetryClient _client;
    private readonly object _dbLock = new();

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private DateTime _lastFailureLogTime = DateTime.MinValue;

    public CenterProductForwardingService(
        SqlSugarDbContext dbContext,
        IAppSettingsService settingsService,
        IUploadTaskService uploadTaskService,
        IProductionFlowLogService productionLogService,
        IProgramExceptionLogService exceptionLogService,
        IProductProcessConfigService productProcessConfigService,
        CenterTelemetryClient client)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _uploadTaskService = uploadTaskService;
        _productionLogService = productionLogService;
        _exceptionLogService = exceptionLogService;
        _productProcessConfigService = productProcessConfigService;
        _client = client;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_loopTask is { IsCompleted: false })
        {
            return Task.CompletedTask;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = Task.Run(() => RunAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }

        if (_loopTask is not null)
        {
            try
            {
                await _loopTask.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
            }
            catch
            {
                // Shutdown must not block the WinForms process from exiting.
            }
        }

        _cts?.Dispose();
        _cts = null;
        _loopTask = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    public void EnqueueCompletedProduct(
        BizWeldTask task,
        int stationNo,
        IReadOnlyList<BizWeldPointRecord> records)
    {
        var settings = _settingsService.Get();
        if (!settings.EnableCenterServerSync || records.Count == 0)
        {
            return;
        }

        var normalizedStationNo = TaskProductProcessConfigResolver.NormalizeStationNo(stationNo, task);
        var configs = TaskProductProcessConfigResolver.Resolve(
            _productProcessConfigService,
            task,
            [normalizedStationNo]);
        configs.TryGetValue(normalizedStationNo, out var config);
        var request = BuildRequest(settings, task, stationNo, records, config);
        var uploadTask = _uploadTaskService.EnqueueOrUpdate(new BizUploadTask
        {
            TaskType = ProductionConstants.UploadTaskTypes.CenterProductReport,
            Target = ProductionConstants.UploadTargets.CentralServer,
            BusinessId = BuildBusinessId(request),
            WeldTaskId = task.Id,
            PayloadJson = JsonSerializer.Serialize(request, JsonOptions),
            Status = ProductionConstants.UploadStatuses.Pending,
            NextRetryTime = DateTime.Now,
            Message = "中心服务器产品数据转发已入队。"
        });

        _productionLogService.Write(
            "CenterProductForwardQueued",
            ProductionFlowLogTexts.Summaries.CenterProductForwardQueued,
            $"UploadTaskId={uploadTask.Id}, ProductNo={request.ProductNo}, PointCount={request.Points.Count}",
            stationNo: stationNo,
            workOrderId: task.SN,
            productNo: request.ProductNo,
            programId: task.ProgramId ?? string.Empty);
    }

    /// <summary>
    /// 将已持久化的工单完工统计放入现有中心服务器重试队列。
    /// 完工请求只包含任务级字段，不重复携带产品点明细。
    /// </summary>
    public void EnqueueTaskFinishUpdate(BizWeldTask task)
    {
        var settings = _settingsService.Get();
        if (!settings.EnableCenterServerSync)
        {
            return;
        }

        var request = BuildTaskFinishRequest(settings, task);
        var uploadTask = _uploadTaskService.EnqueueOrUpdate(new BizUploadTask
        {
            TaskType = ProductionConstants.UploadTaskTypes.CenterProductReport,
            Target = ProductionConstants.UploadTargets.CentralServer,
            BusinessId = BuildBusinessId(request),
            WeldTaskId = task.Id,
            PayloadJson = JsonSerializer.Serialize(request, JsonOptions),
            Status = ProductionConstants.UploadStatuses.Pending,
            NextRetryTime = DateTime.Now,
            Message = "中心服务器工单完工更新已入队。"
        });

        _productionLogService.Write(
            "CenterTaskFinishUpdateQueued",
            ProductionFlowLogTexts.Summaries.CenterTaskFinishUpdateQueued,
            $"UploadTaskId={uploadTask.Id}, EndTime={request.EndTime:yyyy-MM-dd HH:mm:ss}, QualifiedQty={request.QualifiedQty}",
            stationNo: request.StationNo,
            workOrderId: request.WorkOrder,
            programId: task.ProgramId ?? string.Empty);
    }

    /// <summary>
    /// Processes pending center forwarding tasks without blocking product collection.
    /// </summary>
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ExecutePendingAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // 连接类异常已由共享客户端按首次/十分钟摘要记录，避免两个后台服务重复刷程序异常。
                if (!CenterServerAvailabilityLogGate.IsConnectivityFailure(ex, cancellationToken))
                {
                    WriteFailureLog(ex);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }

    private async Task ExecutePendingAsync(CancellationToken cancellationToken)
    {
        var taskIds = GetPendingTaskIds();
        foreach (var taskId in taskIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var task = MarkUploading(taskId);
            if (task is null)
            {
                continue;
            }

            await ExecuteTaskAsync(task, cancellationToken);
        }
    }

    private IReadOnlyList<int> GetPendingTaskIds()
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            return _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.TaskType == ProductionConstants.UploadTaskTypes.CenterProductReport
                    && task.Target == ProductionConstants.UploadTargets.CentralServer
                    && !task.IsDeleted
                    && task.Status != ProductionConstants.UploadStatuses.Uploaded
                    && task.RetryCount < task.MaxRetryCount
                    && (task.NextRetryTime == null || task.NextRetryTime <= DateTime.Now))
                .OrderBy(task => task.CreatedTime)
                .Select(task => task.Id)
                .ToList();
        }
    }

    private BizUploadTask? MarkUploading(int taskId)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(taskId);
            if (task is null || task.IsDeleted || task.Status == ProductionConstants.UploadStatuses.Uploaded)
            {
                return null;
            }

            task.Status = ProductionConstants.UploadStatuses.Uploading;
            task.LastAttemptTime = DateTime.Now;
            task.RetryCount++;
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(task).ExecuteCommand();
            return task;
        }
    }

    private async Task ExecuteTaskAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        var settings = _settingsService.Get();
        if (!settings.EnableCenterServerSync)
        {
            MarkRetry(task, "中心服务器同步未启用。");
            return;
        }

        var request = JsonSerializer.Deserialize<CenterProductReportRequest>(task.PayloadJson ?? string.Empty, JsonOptions);
        if (request is null)
        {
            MarkFailed(task, "中心服务器产品转发任务缺少请求内容。");
            return;
        }

        var response = await _client.UploadProductReportAsync(settings, request, cancellationToken);
        if (response.Success)
        {
            MarkUploaded(task, response.Message);
            _productionLogService.Write(
                "CenterProductForwardSucceeded",
                ProductionFlowLogTexts.Summaries.CenterProductForwardSucceeded,
                $"UploadTaskId={task.Id}, ProductNo={request.ProductNo}, PointCount={request.Points.Count}",
                stationNo: request.StationNo,
                workOrderId: request.WorkOrder,
                productNo: request.ProductNo);
            return;
        }

        MarkRetry(task, response.Message);
        _productionLogService.Write(
            "CenterProductForwardFailed",
            ProductionFlowLogTexts.Summaries.CenterProductForwardFailed,
            $"UploadTaskId={task.Id}, ProductNo={request.ProductNo}, Error={response.Message}",
            "Warning",
            request.StationNo,
            request.WorkOrder,
            request.ProductNo);
    }

    private void MarkUploaded(BizUploadTask task, string message)
    {
        lock (_dbLock)
        {
            task.Status = ProductionConstants.UploadStatuses.Uploaded;
            task.CompletedTime = DateTime.Now;
            task.Message = string.IsNullOrWhiteSpace(message) ? "Center product report uploaded." : message;
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(task).ExecuteCommand();
        }
    }

    private void MarkRetry(BizUploadTask task, string message)
    {
        lock (_dbLock)
        {
            task.Status = task.RetryCount >= task.MaxRetryCount
                ? ProductionConstants.UploadStatuses.Failed
                : ProductionConstants.UploadStatuses.Retrying;
            task.NextRetryTime = task.Status == ProductionConstants.UploadStatuses.Failed
                ? null
                : DateTime.Now.AddSeconds(Math.Min(120, 10 * Math.Max(1, task.RetryCount)));
            task.Message = message;
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(task).ExecuteCommand();
        }
    }

    private void MarkFailed(BizUploadTask task, string message)
    {
        lock (_dbLock)
        {
            task.Status = ProductionConstants.UploadStatuses.Failed;
            task.Message = message;
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(task).ExecuteCommand();
        }
    }

    private void WriteFailureLog(Exception ex)
    {
        if (DateTime.Now - _lastFailureLogTime < TimeSpan.FromMinutes(1))
        {
            return;
        }

        _lastFailureLogTime = DateTime.Now;
        _exceptionLogService.Write(ex, "CenterProductForwardingService");
    }

    private CenterProductReportRequest BuildRequest(
        AppSettings settings,
        BizWeldTask task,
        int stationNo,
        IReadOnlyList<BizWeldPointRecord> records,
        BizProductProcessConfig? config)
    {
        var orderedRecords = records.OrderBy(record => record.SequenceNo).ThenBy(record => record.Id).ToList();
        var first = orderedRecords[0];
        var savedFields = BuildSavedFieldDefinitions(config);
        return new CenterProductReportRequest
        {
            DeviceId = settings.DeviceId.Trim(),
            DeviceName = settings.DeviceName.Trim(),
            SystemType = CenterTelemetryRules.NormalizeSystemType(settings.CenterServerSystemType),
            StationNo = stationNo,
            StationName = ResolveStationName(settings, stationNo),
            WorkOrder = task.SN ?? string.Empty,
            Batch = task.Batch ?? string.Empty,
            Quantity = task.StartAmount,
            PartName = task.ProductName ?? string.Empty,
            ProcessName = task.ProcessName ?? string.Empty,
            ProcessNo = task.ProcessNo ?? string.Empty,
            OperatorNo = task.UserNumber ?? string.Empty,
            ProductJobNo = task.ProductNum ?? string.Empty,
            DrawingNo = task.DrawingNo ?? string.Empty,
            Spec = task.Spec ?? string.Empty,
            ProductNo = first.ProductNo,
            ProductModel = task.ProductModel ?? string.Empty,
            ProductResult = ResolveProductResult(orderedRecords),
            StartTime = task.StartTime,
            EndTime = task.EndTime,
            QualifiedQty = task.QualifiedQty,
            IsTaskFinishUpdate = false,
            CompletedAt = orderedRecords.Max(record => record.Ts),
            ReportColumns = BuildReportColumns(settings, config),
            Points = orderedRecords.Select(record => new CenterProductReportPointDto
            {
                SequenceNo = record.SequenceNo,
                TouchNo = record.TouchNo,
                TestResult = record.TestResult,
                CollectedAt = record.Ts,
                OperatorNo = record.OperatorNo ?? string.Empty,
                RawDataJson = FilterRawDataJson(record.RawDataJson, savedFields)
            }).ToList()
        };
    }

    private static CenterProductReportRequest BuildTaskFinishRequest(AppSettings settings, BizWeldTask task)
    {
        return new CenterProductReportRequest
        {
            DeviceId = settings.DeviceId.Trim(),
            DeviceName = settings.DeviceName.Trim(),
            SystemType = CenterTelemetryRules.NormalizeSystemType(settings.CenterServerSystemType),
            StationNo = task.StationNo <= ProductionConstants.Stations.SharedStationNo
                ? ProductionConstants.Stations.DefaultStationNo
                : task.StationNo,
            StationName = ResolveStationName(settings, task.StationNo),
            WorkOrder = task.SN ?? string.Empty,
            Batch = task.Batch ?? string.Empty,
            Quantity = task.StartAmount,
            PartName = task.ProductName ?? string.Empty,
            ProcessName = task.ProcessName ?? string.Empty,
            ProcessNo = task.ProcessNo ?? string.Empty,
            OperatorNo = task.UserNumber ?? string.Empty,
            ProductJobNo = task.ProductNum ?? string.Empty,
            DrawingNo = task.DrawingNo ?? string.Empty,
            Spec = task.Spec ?? string.Empty,
            ProductModel = task.ProductModel ?? string.Empty,
            StartTime = task.StartTime,
            EndTime = task.EndTime,
            QualifiedQty = task.QualifiedQty,
            IsTaskFinishUpdate = true,
            CompletedAt = task.EndTime ?? task.StartTime,
            ReportColumns = [],
            Points = []
        };
    }

    private static string ResolveStationName(AppSettings settings, int stationNo)
    {
        if (!settings.EnableDualStation)
        {
            return string.Empty;
        }

        var names = StationDisplayNameRules.NormalizeForLoad(
            dualStationEnabled: true,
            settings.Station1DisplayName,
            settings.Station2DisplayName);
        return stationNo == 2
            ? names.Station2
            : names.Station1;
    }

    /// <summary>
    /// 产品结果优先读取采集时已固化的产品级字段；旧记录为空时回退 RawDataJson.product_result。
    /// PLC读取模式不根据焊点 TestResult 重新推算产品结果；程序计算模式已在采集时写入该字段。
    /// </summary>
    private static string ResolveProductResult(IReadOnlyList<BizWeldPointRecord> records)
    {
        var persistedResult = records
            .Select(record => record.ProductResult)
            .FirstOrDefault(result => !string.IsNullOrWhiteSpace(result));
        if (!string.IsNullOrWhiteSpace(persistedResult))
        {
            return TestResultRules.Normalize(persistedResult);
        }

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.RawDataJson))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(record.RawDataJson);
                if (document.RootElement.TryGetProperty(CenterProductReportFormat.ColumnProductResult, out var value))
                {
                    return TestResultRules.Normalize(value.ToString());
                }
            }
            catch (JsonException)
            {
                // 历史原始数据损坏时保持 Unknown，不能退回焊点结果聚合。
            }
        }

        return ProductionConstants.TestResults.Unknown;
    }

    /// <summary>
    /// 生成设备端生产报表列定义。
    /// 中心服务器使用这份列定义，确保 Excel 表头跟设备端配置保持一致。
    /// </summary>
    private IReadOnlyList<SavedFieldDefinition> BuildSavedFieldDefinitions(BizProductProcessConfig? config)
    {
        if (config is null)
        {
            return [];
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var details = _dbContext.Db.Queryable<BizSchemeDetail>()
                .Where(detail => detail.SchemeId == config.SchemeId)
                .ToList();
            var itemIds = details.Select(detail => detail.ItemId).Distinct().ToList();
            var items = _dbContext.Db.Queryable<DimTestItem>()
                .Where(item => itemIds.Contains(item.ItemId))
                .ToList();
            var fields = new List<SavedFieldDefinition>();
            foreach (var detail in details)
            {
                var item = items.FirstOrDefault(candidate => candidate.ItemId == detail.ItemId);
                if (item is null)
                {
                    continue;
                }

                var itemKey = ResolveItemKey(item);
                AddSavedField(fields, detail.SaveActual, itemKey, item.ItemName);
                AddSavedField(fields, detail.SaveUpper, $"{itemKey}_upper", $"{item.ItemName}上限");
                AddSavedField(fields, detail.SaveLower, $"{itemKey}_lower", $"{item.ItemName}下限");
                AddSavedField(fields, detail.SaveResult, $"{itemKey}_result", $"{item.ItemName}结果");
            }

            return fields;
        }
    }

    private static void AddSavedField(
        ICollection<SavedFieldDefinition> fields,
        bool enabled,
        string key,
        string fallbackKey)
    {
        if (enabled)
        {
            fields.Add(new SavedFieldDefinition(key, fallbackKey));
        }
    }

    private static string FilterRawDataJson(
        string? rawDataJson,
        IReadOnlyList<SavedFieldDefinition> fields)
    {
        if (fields.Count == 0 || string.IsNullOrWhiteSpace(rawDataJson))
        {
            return "{}";
        }

        try
        {
            using var document = JsonDocument.Parse(rawDataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return "{}";
            }

            var rawValues = document.RootElement.EnumerateObject().ToDictionary(
                property => property.Name,
                property => property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);
            var filtered = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in fields)
            {
                if (rawValues.TryGetValue(field.Key, out var value)
                    || rawValues.TryGetValue(field.FallbackKey, out value))
                {
                    filtered[field.Key] = value;
                }
            }

            return JsonSerializer.Serialize(filtered);
        }
        catch (JsonException)
        {
            return "{}";
        }
    }

    private List<CenterProductReportColumnDto> BuildReportColumns(
        AppSettings settings,
        BizProductProcessConfig? config)
    {
        var columns = new List<CenterProductReportColumnDto>();

        if (settings.EnableDualStation)
        {
            columns.Add(new CenterProductReportColumnDto { Key = CenterProductReportFormat.ColumnStationNo, Title = "工位", MergeByProduct = true });
        }

        columns.Add(new CenterProductReportColumnDto { Key = CenterProductReportFormat.ColumnProductNo, Title = "产品编号", MergeByProduct = true });
        var wholePieceInspection = WholePieceAbAggregationRules.IsApplicable(
            settings.ProcessParameterDeviceType,
            config?.TouchCount ?? 0);
        columns.Add(new CenterProductReportColumnDto
        {
            Key = CenterProductReportFormat.ColumnTouchNo,
            Title = ResolvePointNoHeader(config, wholePieceInspection),
            MergeByProduct = false
        });

        columns.AddRange(BuildDynamicReportColumns(config, wholePieceInspection));
        columns.Add(new CenterProductReportColumnDto
        {
            Key = CenterProductReportFormat.ColumnTouchResult,
            Title = ResolvePointResultHeader(config, wholePieceInspection),
            MergeByProduct = false
        });
        columns.Add(new CenterProductReportColumnDto { Key = CenterProductReportFormat.ColumnProductResult, Title = "产品结果", MergeByProduct = true });
        return columns;
    }

    private static string ResolvePointNoHeader(BizProductProcessConfig? config, bool wholePieceInspection)
    {
        return CenterProductReportFormat.ResolvePointNoTitle(config?.PointNoHeader, wholePieceInspection);
    }

    private static string ResolvePointResultHeader(BizProductProcessConfig? config, bool wholePieceInspection)
    {
        return CenterProductReportFormat.ResolvePointResultTitle(config?.PointResultHeader, wholePieceInspection);
    }

    private List<CenterProductReportColumnDto> BuildDynamicReportColumns(BizProductProcessConfig? config, bool wholePieceInspection)
    {
        if (config is null)
        {
            return [];
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var details = _dbContext.Db.Queryable<BizSchemeDetail>()
                .Where(detail => detail.SchemeId == config.SchemeId)
                .ToList();
            if (details.Count == 0)
            {
                return [];
            }

            var itemIds = details.Select(detail => detail.ItemId).Distinct().ToList();
            var items = _dbContext.Db.Queryable<DimTestItem>()
                .Where(item => itemIds.Contains(item.ItemId))
                .ToList();

            return details
                .OrderBy(detail => detail.DetailId)
                .SelectMany(detail => BuildDynamicReportColumns(
                    detail,
                    items.FirstOrDefault(item => item.ItemId == detail.ItemId),
                    wholePieceInspection))
                .ToList();
        }
    }

    private static IEnumerable<CenterProductReportColumnDto> BuildDynamicReportColumns(
        BizSchemeDetail detail,
        DimTestItem? item)
        => BuildDynamicReportColumns(detail, item, wholePieceInspection: false);

    private static IEnumerable<CenterProductReportColumnDto> BuildDynamicReportColumns(
        BizSchemeDetail detail,
        DimTestItem? item,
        bool wholePieceInspection)
    {
        if (item is null)
        {
            yield break;
        }

        SchemeDetailRoleRules.ClearUnavailableRoles(detail, item);
        var itemKey = ResolveItemKey(item);
        if (ShouldForwardSavedRole(detail, SchemeDetailValueRole.Actual))
        {
            yield return BuildDynamicColumn(
                itemKey,
                detail.ActualHeader,
                SchemeDetailRoleRules.GetDefaultHeader(item, SchemeDetailValueRole.Actual),
                item.Unit,
                SchemeDetailValueRole.Actual,
                wholePieceInspection && WholePieceAbAggregationRules.IsProductMaximumItem(item.ItemName));
        }

        if (ShouldForwardSavedRole(detail, SchemeDetailValueRole.Upper))
        {
            yield return BuildDynamicColumn($"{itemKey}_upper", detail.UpperHeader, SchemeDetailRoleRules.GetDefaultHeader(item, SchemeDetailValueRole.Upper), item.Unit, SchemeDetailValueRole.Upper);
        }

        if (ShouldForwardSavedRole(detail, SchemeDetailValueRole.Lower))
        {
            yield return BuildDynamicColumn($"{itemKey}_lower", detail.LowerHeader, SchemeDetailRoleRules.GetDefaultHeader(item, SchemeDetailValueRole.Lower), item.Unit, SchemeDetailValueRole.Lower);
        }

        if (ShouldForwardSavedRole(detail, SchemeDetailValueRole.Result))
        {
            yield return BuildDynamicColumn($"{itemKey}_result", detail.ResultHeader, SchemeDetailRoleRules.GetDefaultHeader(item, SchemeDetailValueRole.Result), item.Unit, SchemeDetailValueRole.Result);
        }
    }

    /// <summary>
    /// 中心服务器只同步本地保存通道的数据；报表或 MES 独占字段不得进入中心报表协议。
    /// </summary>
    private static bool ShouldForwardSavedRole(BizSchemeDetail detail, SchemeDetailValueRole role)
    {
        return SchemeDetailRoleRules.ShouldPersistRole(detail, role)
            && SchemeDetailRoleRules.IsSaveEnabled(detail, role);
    }

    private static CenterProductReportColumnDto BuildDynamicColumn(
        string key,
        string? title,
        string fallbackTitle,
        string? unit,
        SchemeDetailValueRole role,
        bool mergeByProduct = false)
    {
        return new CenterProductReportColumnDto
        {
            Key = key,
            Title = TestItemUnitFormatRules.FormatHeader(NormalizeDisplayText(title, fallbackTitle), unit, role),
            MergeByProduct = mergeByProduct
        };
    }

    private static string NormalizeDisplayText(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string ResolveItemKey(DimTestItem item)
    {
        return item.ItemName.Trim() switch
        {
            "峰值电流" => "max_electric",
            "峰值电压" => "max_voltage",
            "有效功率" => "valid_power",
            "位移" => "displacement",
            "焊接时间" => "weld_ts",
            var name when !string.IsNullOrWhiteSpace(name) => $"item_{item.ItemId}",
            _ => $"item_{item.ItemId}"
        };
    }

    private sealed record SavedFieldDefinition(string Key, string FallbackKey);

    private static string BuildBusinessId(CenterProductReportRequest request)
    {
        return CenterProductForwardingRules.BuildBusinessId(request);
    }
}
