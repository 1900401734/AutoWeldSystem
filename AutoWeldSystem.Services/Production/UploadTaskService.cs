using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.DTOs.Upload;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Data;
using System.Globalization;
using System.Text.Json;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 通用上传任务服务实现。
/// 当前先提供查询和人工重试排队能力，后续上传执行器可复用同一张任务表。
/// </summary>
public class UploadTaskService : IUploadTaskService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IMesProvider _mesProvider;
    private readonly IAppSettingsService _settingsService;
    private readonly IProductionFlowLogService _productionLogService;
    private readonly object _dbLock = new();

    public UploadTaskService(
        SqlSugarDbContext dbContext,
        IMesProvider mesProvider,
        IAppSettingsService settingsService,
        IProductionFlowLogService productionLogService)
    {
        _dbContext = dbContext;
        _mesProvider = mesProvider;
        _settingsService = settingsService;
        _productionLogService = productionLogService;
    }

    public IReadOnlyList<UploadTaskSummary> GetTasks(string taskType, bool includeCompleted = false)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var normalizedTaskType = NormalizeTaskType(taskType);
            var query = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.TaskType == normalizedTaskType);

            if (!includeCompleted)
            {
                query = query.Where(task => task.Status != ProductionConstants.UploadStatuses.Uploaded);
            }

            return query.ToList()
                .OrderByDescending(task => IsActionRequired(task.Status))
                .ThenByDescending(task => task.UpdatedTime)
                .Select(ToSummary)
                .ToList();
        }
    }

    public UploadTaskSummary? GetById(int id)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(id);
            return task is null ? null : ToSummary(task);
        }
    }

    public BizUploadTask EnqueueOrUpdate(BizUploadTask task)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            Normalize(task);

            var existing = FindExistingTask(task);
            if (existing is null)
            {
                task.CreatedTime = DateTime.Now;
                task.UpdatedTime = DateTime.Now;
                return _dbContext.Db.Insertable(task).ExecuteReturnEntity();
            }

            if (existing.Status == ProductionConstants.UploadStatuses.Uploaded)
            {
                return existing;
            }

            existing.WeldTaskId = task.WeldTaskId;
            existing.PayloadJson = task.PayloadJson;
            existing.FilePath = task.FilePath;
            existing.Status = task.Status;
            existing.Target = task.Target;
            existing.MaxRetryCount = task.MaxRetryCount;
            existing.NextRetryTime = task.NextRetryTime;
            existing.CompletedTime = task.CompletedTime;
            existing.Message = task.Message;
            existing.UpdatedTime = DateTime.Now;

            _dbContext.Db.Updateable(existing).ExecuteCommand();
            return _dbContext.Db.Queryable<BizUploadTask>().InSingle(existing.Id) ?? existing;
        }
    }

    public async Task<UploadTaskSummary?> ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        var task = MarkUploading(id);
        if (task is null)
        {
            return null;
        }

        var response = await ExecuteByTypeAsync(task, cancellationToken);
        var summary = FinishExecution(task.Id, response);
        return summary;
    }

    public async Task<int> ExecuteAllPendingAsync(string taskType, CancellationToken cancellationToken = default)
    {
        List<int> taskIds;
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var normalizedTaskType = NormalizeTaskType(taskType);
            taskIds = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.TaskType == normalizedTaskType
                    && task.Status != ProductionConstants.UploadStatuses.Uploaded)
                .ToList()
                .Select(task => task.Id)
                .ToList();
        }

        var executedCount = 0;
        foreach (var taskId in taskIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await ExecuteAsync(taskId, cancellationToken) is not null)
            {
                executedCount++;
            }
        }

        return executedCount;
    }

    public void RequestRetry(int id)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(id);
            if (task is null || task.Status == ProductionConstants.UploadStatuses.Uploaded)
            {
                return;
            }

            MarkRetryRequested(task);
            _dbContext.Db.Updateable(task).ExecuteCommand();
        }
    }

    public int RequestRetryAll(string taskType)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var normalizedTaskType = NormalizeTaskType(taskType);
            var tasks = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.TaskType == normalizedTaskType
                    && task.Status != ProductionConstants.UploadStatuses.Uploaded)
                .ToList();

            foreach (var task in tasks)
            {
                MarkRetryRequested(task);
                _dbContext.Db.Updateable(task).ExecuteCommand();
            }

            return tasks.Count;
        }
    }

    private BizUploadTask? MarkUploading(int id)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(id);
            if (task is null || task.Status == ProductionConstants.UploadStatuses.Uploaded)
            {
                return task;
            }

            task.Status = ProductionConstants.UploadStatuses.Uploading;
            task.LastAttemptTime = DateTime.Now;
            task.RetryCount++;
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(task).ExecuteCommand();
            return task;
        }
    }

    private async Task<BasicRes<object>> ExecuteByTypeAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        return task.TaskType switch
        {
            ProductionConstants.UploadTaskTypes.StartReport => await UploadStartReportAsync(task, cancellationToken),
            ProductionConstants.UploadTaskTypes.FinishReport => await UploadFinishReportAsync(task, cancellationToken),
            ProductionConstants.UploadTaskTypes.WorkOrderStatus => await UploadWorkOrderStatusAsync(task, cancellationToken),
            ProductionConstants.UploadTaskTypes.DeviceStatus => await UploadDeviceStatusAsync(task, cancellationToken),
            ProductionConstants.UploadTaskTypes.ReportFile => await UploadReportFileAsync(task, cancellationToken),
            ProductionConstants.UploadTaskTypes.ProcessParameter => await UploadProcessParametersAsync(task, cancellationToken),
            ProductionConstants.UploadTaskTypes.ProgramFile => Unsupported("程序文件上传由程序管理服务处理。"),
            _ => Unsupported($"暂不支持的上传任务类型：{task.TaskType}")
        };
    }

    /// <summary>
    /// 上传当前任务下尚未成功上传的焊点记录。
    /// 先尝试整批上传，失败后按 ProductNumber 降级，最后再降级到单条焊点，尽量保住已成功的数据。
    /// </summary>
    private async Task<BasicRes<object>> UploadStartReportAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        var request = ReadPayloadRequest<ExperimentStartReq>(task.PayloadJson);
        if (request is null)
        {
            return Unsupported("Start report task payload is missing.");
        }

        var response = await _mesProvider.StartWorkAsync(request, cancellationToken);
        if (!response.IsSuccess || response.Data is null || string.IsNullOrWhiteSpace(response.Data.Id))
        {
            return Unsupported(response.Msg);
        }

        UpdateTaskExpStartId(task, response.Data.Id);
        return Success(string.IsNullOrWhiteSpace(response.Msg) ? "Start report uploaded." : response.Msg);
    }

    private async Task<BasicRes<object>> UploadFinishReportAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        var weldTask = GetWeldTask(task);
        if (weldTask is null)
        {
            return Unsupported("Finish report task has no weld task.");
        }

        if (string.IsNullOrWhiteSpace(weldTask.ExpStartId))
        {
            return Unsupported("Finish report is waiting for start report upload.");
        }

        var request = ReadPayloadRequest<ExperimentEndReq>(task.PayloadJson) ?? new ExperimentEndReq();
        request.ExpStartId = weldTask.ExpStartId;
        request.DeviceId = FirstNonEmpty(request.DeviceId, weldTask.DeviceId);
        request.SN = FirstNonEmpty(request.SN, weldTask.SN);
        request.ProcessNo = FirstNonEmpty(request.ProcessNo, weldTask.ProcessNo);
        request.EndExperID = FirstNonEmpty(request.EndExperID, weldTask.EndOperatorNumber, weldTask.UserNumber, Environment.UserName);
        request.EndTs = FirstNonEmpty(request.EndTs, weldTask.EndTime?.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        request.ExpStatus = ProductionConstants.MesWorkOrderStatuses.Completed;
        request.WorkHour = request.WorkHour <= 0
            ? Convert.ToDecimal(((weldTask.EndTime ?? DateTime.Now) - weldTask.StartTime).TotalHours)
            : request.WorkHour;
        request.ExpQty = request.ExpQty <= 0 ? weldTask.ActualQty : request.ExpQty;
        request.QualifyNumber = request.QualifyNumber <= 0 ? weldTask.QualifiedQty : request.QualifyNumber;
        request.FailureNumber = request.FailureNumber <= 0 ? weldTask.FailedQty : request.FailureNumber;

        return await _mesProvider.EndWorkAsync(request, cancellationToken);
    }

    private async Task<BasicRes<object>> UploadWorkOrderStatusAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        var weldTask = GetWeldTask(task);
        if (weldTask is null)
        {
            return Unsupported("Work-order status task has no weld task.");
        }

        if (string.IsNullOrWhiteSpace(weldTask.ExpStartId))
        {
            return Unsupported("Work-order status is waiting for start report upload.");
        }

        var statusCode = ReadStatusCode(task.PayloadJson);
        if (string.Equals(statusCode, ProductionConstants.MesWorkOrderStatuses.Completed, StringComparison.OrdinalIgnoreCase)
            && !IsFinishReportUploadedOrAbsent(weldTask.Id))
        {
            return Unsupported("Completed status is waiting for finish report upload.");
        }

        var request = ReadPayloadRequest<ReportExperimentStatusReq>(task.PayloadJson) ?? new ReportExperimentStatusReq();
        request.ExpStartId = weldTask.ExpStartId;
        request.DeviceId = FirstNonEmpty(request.DeviceId, weldTask.DeviceId);
        request.ExpStatus = FirstNonEmpty(statusCode, request.ExpStatus);
        request.Ts = FirstNonEmpty(request.Ts, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        return await _mesProvider.ChangeWorkStatusAsync(request, cancellationToken);
    }

    private async Task<BasicRes<object>> UploadDeviceStatusAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        var request = ReadDeviceStatusRequest(task.PayloadJson);
        if (request is null)
        {
            return Unsupported("Device status task payload is missing.");
        }

        var response = await _mesProvider.ReportDeviceStatusAsync(request.Request, cancellationToken);
        UpdateDeviceStatusLog(request.LogId, response);
        return response;
    }

    private async Task<BasicRes<object>> UploadProcessParametersAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        if (!EnsureTaskExpStartReady(task, out var message))
        {
            return Unsupported(message);
        }

        var records = GetPendingWeldPointRecords(task);
        if (records.Count == 0)
        {
            return Success("没有待上传的过程参数。");
        }

        var batchResponse = await UploadProcessParameterGroupAsync(records, cancellationToken);
        if (batchResponse.IsSuccess)
        {
            UpdateWeldPointUploadStatus(records, batchResponse);
            return batchResponse;
        }

        if (!IsProductScopedTask(task))
        {
            UpdateWeldPointUploadStatus(records, batchResponse);
            return Unsupported($"过程参数批量上传失败，任务级上传不会拆分为单件上传。原因：{batchResponse.Msg}");
        }

        var failedMessages = new List<string>();
        foreach (var productGroup in records.GroupBy(record => record.ProductNo).OrderBy(group => group.Key))
        {
            var productRecords = productGroup.ToList();
            var productResponse = await UploadProcessParameterGroupAsync(productRecords, cancellationToken);
            if (productResponse.IsSuccess)
            {
                UpdateWeldPointUploadStatus(productRecords, productResponse);
                continue;
            }

            foreach (var record in productRecords.OrderBy(record => record.SequenceNo))
            {
                var singleResponse = await UploadProcessParameterGroupAsync(new[] { record }, cancellationToken);
                UpdateWeldPointUploadStatus(new[] { record }, singleResponse);
                if (!singleResponse.IsSuccess)
                {
                    failedMessages.Add($"ProductNumber={record.ProductNo}, TouchNo={record.TouchNo}: {singleResponse.Msg}");
                }
            }
        }

        return failedMessages.Count == 0
            ? Success($"过程参数已通过降级策略上传成功。整批失败原因：{batchResponse.Msg}")
            : Unsupported($"过程参数部分上传失败。整批失败原因：{batchResponse.Msg}；明细：{FormatFailureMessages(failedMessages)}");
    }

    private IReadOnlyList<BizWeldPointRecord> GetPendingWeldPointRecords(BizUploadTask task)
    {
        if (task.WeldTaskId is null)
        {
            return Array.Empty<BizWeldPointRecord>();
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            return _dbContext.Db.Queryable<BizWeldPointRecord>()
                .Where(record => record.TaskId == task.WeldTaskId.Value
                    && record.UploadStatus != ProductionConstants.UploadStatuses.Uploaded)
                .ToList()
                .Where(record => IsRecordInTaskScope(record, task))
                .OrderBy(record => record.StationNo)
                .ThenBy(record => record.ProductNo)
                .ThenBy(record => record.SequenceNo)
                .ToList();
        }
    }

    private bool EnsureTaskExpStartReady(BizUploadTask task, out string message)
    {
        message = string.Empty;
        var weldTask = GetWeldTask(task);
        if (weldTask is null)
        {
            message = "Upload task has no weld task.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(weldTask.ExpStartId))
        {
            message = "Upload task is waiting for start report upload.";
            return false;
        }

        UpdateWeldPointExpStartId(weldTask.Id, weldTask.ExpStartId);
        return true;
    }

    private BizWeldTask? GetWeldTask(BizUploadTask task)
    {
        if (task.WeldTaskId is null)
        {
            return null;
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            return _dbContext.Db.Queryable<BizWeldTask>().InSingle(task.WeldTaskId.Value);
        }
    }

    private void UpdateTaskExpStartId(BizUploadTask task, string expStartId)
    {
        if (task.WeldTaskId is null)
        {
            return;
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var weldTask = _dbContext.Db.Queryable<BizWeldTask>().InSingle(task.WeldTaskId.Value);
            if (weldTask is null)
            {
                return;
            }

            weldTask.ExpStartId = expStartId.Trim();
            weldTask.UploadMessage = "Start report uploaded to MES.";
            _dbContext.Db.Updateable(weldTask)
                .UpdateColumns(it => new { it.ExpStartId, it.UploadMessage })
                .Where(it => it.Id == weldTask.Id)
                .ExecuteCommand();

            UpdateWeldPointExpStartId(weldTask.Id, weldTask.ExpStartId);
        }
    }

    private void UpdateWeldPointExpStartId(int weldTaskId, string expStartId)
    {
        _dbContext.Db.Updateable<BizWeldPointRecord>()
            .SetColumns(record => record.ExpStartId == expStartId)
            .Where(record => record.TaskId == weldTaskId && record.ExpStartId == null)
            .ExecuteCommand();
    }

    private bool IsFinishReportUploadedOrAbsent(int weldTaskId)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var finishTask = _dbContext.Db.Queryable<BizUploadTask>()
                .Where(task => task.WeldTaskId == weldTaskId
                    && task.TaskType == ProductionConstants.UploadTaskTypes.FinishReport)
                .OrderByDescending(task => task.UpdatedTime)
                .First();

            return finishTask is null
                || finishTask.Status == ProductionConstants.UploadStatuses.Uploaded;
        }
    }

    private async Task<BasicRes<object>> UploadProcessParameterGroupAsync(
        IReadOnlyList<BizWeldPointRecord> records,
        CancellationToken cancellationToken)
    {
        var settings = _settingsService.Get();
        var deviceType = NormalizeProcessParameterDeviceType(settings.ProcessParameterDeviceType);
        var schemeItemCache = new Dictionary<string, IReadOnlyList<ProcessParameterSchemeItem>>(StringComparer.OrdinalIgnoreCase);
        var items = records
            .Select(record => ToProcessParameterUploadItem(
                record,
                deviceType,
                ResolveProcessParameterSchemeItems(record, schemeItemCache)))
            .ToList();
        return await _mesProvider.UploadProcessParametersAsync(items, cancellationToken);
    }

    private void UpdateWeldPointUploadStatus(IReadOnlyList<BizWeldPointRecord> records, BasicRes<object> response)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            foreach (var record in records)
            {
                record.UploadStatus = response.IsSuccess
                    ? ProductionConstants.UploadStatuses.Uploaded
                    : ProductionConstants.UploadStatuses.Failed;
                record.UploadTime = response.IsSuccess ? DateTime.Now : null;
                record.UploadMessage = response.Msg;
                record.RetryCount = response.IsSuccess ? record.RetryCount : record.RetryCount + 1;
                _dbContext.Db.Updateable(record).ExecuteCommand();
            }
        }
    }

    private IReadOnlyList<ProcessParameterSchemeItem> ResolveProcessParameterSchemeItems(
        BizWeldPointRecord record,
        Dictionary<string, IReadOnlyList<ProcessParameterSchemeItem>> cache)
    {
        var cacheKey = $"{record.TaskId}\u001F{record.ProductNo}\u001F{record.StationNo}";
        if (cache.TryGetValue(cacheKey, out var cachedItems))
        {
            return cachedItems;
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var config = ResolveProductProcessConfig(record);
            var items = GetMesSchemeItemsForConfig(config);
            cache[cacheKey] = items;
            return items;
        }
    }

    private BizProductProcessConfig? ResolveProductProcessConfig(BizWeldPointRecord record)
    {
        var task = _dbContext.Db.Queryable<BizWeldTask>().InSingle(record.TaskId);
        if (task is null)
        {
            return null;
        }

        var productNum = ResolveTaskProductNum(task);
        if (string.IsNullOrWhiteSpace(productNum))
        {
            return null;
        }

        var stationNo = record.StationNo > ProductionConstants.Stations.SharedStationNo
            ? record.StationNo
            : task.StationNo;
        stationNo = stationNo > ProductionConstants.Stations.SharedStationNo
            ? stationNo
            : ProductionConstants.Stations.DefaultStationNo;

        return _dbContext.Db.Queryable<BizProductProcessConfig>()
            .Where(config => config.Enabled && config.ProductNum == productNum)
            .ToList()
            .Where(config => config.StationNo == ProductionConstants.Stations.SharedStationNo || config.StationNo == stationNo)
            .OrderByDescending(config => config.StationNo == stationNo)
            .ThenBy(config => config.Id)
            .FirstOrDefault();
    }

    private IReadOnlyList<ProcessParameterSchemeItem> GetMesSchemeItemsForConfig(BizProductProcessConfig? config)
    {
        if (config is null)
        {
            return Array.Empty<ProcessParameterSchemeItem>();
        }

        var details = _dbContext.Db.Queryable<BizSchemeDetail>()
            .Where(detail => detail.SchemeId == config.SchemeId)
            .ToList()
            .Select(NormalizeLegacyDetailRoles)
            .Where(HasAnyMesEnabledRole)
            .ToList();
        if (details.Count == 0)
        {
            return Array.Empty<ProcessParameterSchemeItem>();
        }

        var itemIds = details.Select(detail => detail.ItemId).Distinct().ToList();
        var items = _dbContext.Db.Queryable<DimTestItem>()
            .Where(item => itemIds.Contains(item.ItemId))
            .ToList();

        return details
            .OrderBy(detail => detail.DetailId)
            .Select(detail => new
            {
                Item = items.FirstOrDefault(item => item.ItemId == detail.ItemId),
                Detail = detail
            })
            .Where(item => item.Item is not null)
            .Select(item => new ProcessParameterSchemeItem(item.Item!, item.Detail))
            .ToList();
    }

    private string ResolveTaskProductNum(BizWeldTask task)
    {
        if (!string.IsNullOrWhiteSpace(task.ProgramId))
        {
            var programs = _dbContext.Db.Queryable<BizProgram>()
                .Where(program => !program.IsDeleted && program.ProgramId == task.ProgramId.Trim())
                .ToList();

            var localProgram = programs
                .OrderByDescending(program => IsExactTextMatch(program.DeviceId, task.DeviceId))
                .ThenByDescending(program => program.UpdatedTime)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(localProgram?.ProductNum))
            {
                return localProgram.ProductNum.Trim();
            }
        }

        return task.ProductNum.Trim();
    }

    private static ProcessParameterUploadItem ToProcessParameterUploadItem(
        BizWeldPointRecord record,
        string deviceType,
        IReadOnlyList<ProcessParameterSchemeItem> schemeItems)
    {
        var item = new ProcessParameterUploadItem
        {
            ExpStartId = record.ExpStartId,
            DeviceId = record.DeviceId,
            SN = record.SN,
            ProcessNo = record.ProcessNo,
            ProductNo = record.ProductNo,
            TouchNo = ShouldWriteTouchNo(deviceType) ? record.TouchNo : null,
            Type = ResolveProcessParameterType(deviceType),
            IsTest = record.IsTest,
            Ts = record.Ts.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
        };

        AddMesDynamicFields(item, record.RawDataJson, schemeItems);
        return item;
    }

    private static void AddMesDynamicFields(
        ProcessParameterUploadItem uploadItem,
        string? rawDataJson,
        IReadOnlyList<ProcessParameterSchemeItem> schemeItems)
    {
        if (schemeItems.Count == 0)
        {
            return;
        }

        var rawValues = ParseRawData(rawDataJson);
        foreach (var schemeItem in schemeItems)
        {
            AddMesDynamicField(uploadItem, rawValues, schemeItem, ProcessParameterValueRole.Actual);
            AddMesDynamicField(uploadItem, rawValues, schemeItem, ProcessParameterValueRole.Upper);
            AddMesDynamicField(uploadItem, rawValues, schemeItem, ProcessParameterValueRole.Lower);
            AddMesDynamicField(uploadItem, rawValues, schemeItem, ProcessParameterValueRole.Result);
        }
    }

    private static void AddMesDynamicField(
        ProcessParameterUploadItem uploadItem,
        IReadOnlyDictionary<string, string> rawValues,
        ProcessParameterSchemeItem schemeItem,
        ProcessParameterValueRole role)
    {
        if (!ShouldUploadMesRole(schemeItem.Detail, role, out var mesFieldName))
        {
            return;
        }

        var value = ResolveRawRoleValue(rawValues, schemeItem.Item, role) ?? string.Empty;
        TryAddDynamicField(uploadItem, mesFieldName, value);
    }

    private static bool ShouldUploadMesRole(
        BizSchemeDetail detail,
        ProcessParameterValueRole role,
        out string mesFieldName)
    {
        mesFieldName = role switch
        {
            ProcessParameterValueRole.Actual when detail.EnableActual && detail.MesActual == true => detail.ActualMesFieldName ?? string.Empty,
            ProcessParameterValueRole.Upper when detail.EnableUpper && detail.MesUpper == true => detail.UpperMesFieldName ?? string.Empty,
            ProcessParameterValueRole.Lower when detail.EnableLower && detail.MesLower == true => detail.LowerMesFieldName ?? string.Empty,
            ProcessParameterValueRole.Result when detail.EnableResult && detail.MesResult == true => detail.ResultMesFieldName ?? string.Empty,
            _ => string.Empty
        };

        mesFieldName = mesFieldName.Trim();
        return !string.IsNullOrWhiteSpace(mesFieldName);
    }

    private static string? ResolveRawRoleValue(
        IReadOnlyDictionary<string, string> rawValues,
        DimTestItem item,
        ProcessParameterValueRole role)
    {
        var itemKey = ResolveItemKey(item);
        return role switch
        {
            ProcessParameterValueRole.Actual => GetRawValue(rawValues, itemKey, item.ItemName),
            ProcessParameterValueRole.Upper => GetRawValue(rawValues, $"{itemKey}_upper", $"{item.ItemName}上限"),
            ProcessParameterValueRole.Lower => GetRawValue(rawValues, $"{itemKey}_lower", $"{item.ItemName}下限"),
            ProcessParameterValueRole.Result => GetRawValue(rawValues, $"{itemKey}_result", $"{item.ItemName}结果"),
            _ => null
        };
    }

    private static void TryAddDynamicField(ProcessParameterUploadItem uploadItem, string fieldName, string value)
    {
        if (string.IsNullOrWhiteSpace(fieldName) || IsReservedProcessParameterField(fieldName))
        {
            return;
        }

        // 方案保存已校验同一方案内不重复；这里仍用 TryAdd 防止历史脏数据覆盖已生成字段。
        uploadItem.DynamicFields.TryAdd(fieldName.Trim(), value);
    }

    private static bool IsReservedProcessParameterField(string fieldName)
    {
        return fieldName.Equals(nameof(ProcessParameterUploadItem.ExpStartId), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.DeviceId), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.SN), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.ProcessNo), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.ProductNo), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.TouchNo), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.Type), StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals(nameof(ProcessParameterUploadItem.Ts), StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldWriteTouchNo(string deviceType)
        => !string.Equals(deviceType, ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck, StringComparison.OrdinalIgnoreCase);

    private static string ResolveProcessParameterType(string deviceType)
        => string.Equals(deviceType, ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic, StringComparison.OrdinalIgnoreCase)
            ? "EM"
            : "WP";

    private static string NormalizeProcessParameterDeviceType(string? value)
    {
        return value?.Trim() switch
        {
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck => ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceWeld => ProductionConstants.ProcessParameterDeviceTypes.WholePieceWeld,
            _ => ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic
        };
    }

    private static bool HasAnyEnabledRole(BizSchemeDetail detail)
    {
        return detail.EnableActual || detail.EnableUpper || detail.EnableLower || detail.EnableResult;
    }

    private static bool HasAnyMesEnabledRole(BizSchemeDetail detail)
    {
        return detail.EnableActual && detail.MesActual == true
            || detail.EnableUpper && detail.MesUpper == true
            || detail.EnableLower && detail.MesLower == true
            || detail.EnableResult && detail.MesResult == true;
    }

    private static BizSchemeDetail NormalizeLegacyDetailRoles(BizSchemeDetail detail)
    {
        if (HasAnyEnabledRole(detail))
        {
            return detail;
        }

        detail.EnableActual = true;
        detail.EnableUpper = true;
        detail.EnableLower = true;
        detail.EnableResult = true;
        return detail;
    }

    private static bool IsExactTextMatch(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetRawValue(IReadOnlyDictionary<string, string> rawValues, params string?[] keys)
    {
        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key) && rawValues.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return null;
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

    private static Dictionary<string, string> ParseRawData(string? rawDataJson)
    {
        if (string.IsNullOrWhiteSpace(rawDataJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(rawDataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return document.RootElement.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => property.Value.ToString(),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task<BasicRes<object>> UploadReportFileAsync(BizUploadTask task, CancellationToken cancellationToken)
    {
        var request = BuildReportFileRequest(task);
        if (request is null)
        {
            return Unsupported("报告文件任务缺少工单或文件路径信息。");
        }

        return await _mesProvider.UploadReportFileAsync(request, cancellationToken);
    }

    private UploadReportFileReq? BuildReportFileRequest(BizUploadTask task)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var weldTask = task.WeldTaskId is null
                ? null
                : _dbContext.Db.Queryable<BizWeldTask>().InSingle(task.WeldTaskId.Value);
            if (weldTask is null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(weldTask.ExpStartId))
            {
                return null;
            }

            var filePath = task.FilePath;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                filePath = _dbContext.Db.Queryable<BizProductionReportFile>()
                    .Where(report => report.TaskId == weldTask.Id)
                    .ToList()
                    .OrderByDescending(report => report.UpdatedTime)
                    .Select(report => report.FilePath)
                    .FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            return new UploadReportFileReq
            {
                ExpStartId = weldTask.ExpStartId ?? string.Empty,
                DeviceId = weldTask.DeviceId,
                SN = weldTask.SN,
                ProcessNo = weldTask.ProcessNo,
                FileType = ProductionConstants.MesFileTypes.ReportFile,
                FilePath = filePath
            };
        }
    }

    private UploadTaskSummary? FinishExecution(int taskId, BasicRes<object> response)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var task = _dbContext.Db.Queryable<BizUploadTask>().InSingle(taskId);
            if (task is null)
            {
                return null;
            }

            task.Status = response.IsSuccess
                ? ProductionConstants.UploadStatuses.Uploaded
                : ProductionConstants.UploadStatuses.Failed;
            task.Message = response.Msg;
            task.CompletedTime = response.IsSuccess ? DateTime.Now : null;
            task.NextRetryTime = response.IsSuccess ? null : DateTime.Now.AddMinutes(1);
            task.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(task).ExecuteCommand();
            UpdateReportFileStatus(task, response);
            return ToSummary(task);
        }
    }

    private void WriteUploadFlowLog(BizUploadTask task, BasicRes<object> response)
    {
        var payload = ReadUploadPayload(task.PayloadJson);
        var step = ResolveUploadFlowStep(task.TaskType, response.IsSuccess);
        if (string.IsNullOrWhiteSpace(step))
        {
            return;
        }

        _productionLogService.Write(
            step,
            ResolveUploadSummary(task.TaskType, response.IsSuccess),
            response.Msg,
            response.IsSuccess ? "Info" : "Error",
            payload.StationNo,
            payload.WorkOrderId,
            payload.ProductNo,
            plcAddress: task.FilePath ?? string.Empty);
    }

    private static string ResolveUploadFlowStep(string taskType, bool success)
    {
        return taskType switch
        {
            ProductionConstants.UploadTaskTypes.StartReport => success
                ? "StartReportUploadSucceeded"
                : "StartReportUploadFailed",
            ProductionConstants.UploadTaskTypes.FinishReport => success
                ? "FinishReportUploadSucceeded"
                : "FinishReportUploadFailed",
            ProductionConstants.UploadTaskTypes.WorkOrderStatus => success
                ? "WorkOrderStatusUploadSucceeded"
                : "WorkOrderStatusUploadFailed",
            ProductionConstants.UploadTaskTypes.DeviceStatus => success
                ? "DeviceStatusUploadSucceeded"
                : "DeviceStatusUploadFailed",
            ProductionConstants.UploadTaskTypes.ProcessParameter => success
                ? "ProcessParameterUploadSucceeded"
                : "ProcessParameterUploadFailed",
            ProductionConstants.UploadTaskTypes.ReportFile => success
                ? "ReportFileUploadSucceeded"
                : "ReportFileUploadFailed",
            _ => string.Empty
        };
    }

    private static string ResolveUploadSummary(string taskType, bool success)
    {
        return taskType switch
        {
            ProductionConstants.UploadTaskTypes.ProcessParameter => success ? "过程参数上传成功" : "过程参数上传失败",
            ProductionConstants.UploadTaskTypes.ReportFile => success ? "报告文件上传成功" : "报告文件上传失败",
            _ => success ? "上传成功" : "上传失败"
        };
    }

    private void UpdateReportFileStatus(BizUploadTask task, BasicRes<object> response)
    {
        if (task.TaskType != ProductionConstants.UploadTaskTypes.ReportFile)
        {
            return;
        }

        var report = _dbContext.Db.Queryable<BizProductionReportFile>()
            .Where(item => item.TaskId == task.WeldTaskId || item.FilePath == task.FilePath)
            .ToList()
            .OrderByDescending(item => item.UpdatedTime)
            .FirstOrDefault();
        if (report is null)
        {
            return;
        }

        report.UploadStatus = task.Status;
        report.UploadTime = response.IsSuccess ? DateTime.Now : null;
        report.UploadMessage = response.Msg;
        report.UpdatedTime = DateTime.Now;
        _dbContext.Db.Updateable(report).ExecuteCommand();
    }

    private void UpdateDeviceStatusLog(int logId, BasicRes<object> response)
    {
        if (logId <= 0)
        {
            return;
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var log = _dbContext.Db.Queryable<BizDeviceStatusLog>().InSingle(logId);
            if (log is null)
            {
                return;
            }

            log.ReportStatus = response.IsSuccess
                ? ProductionConstants.UploadStatuses.Uploaded
                : ProductionConstants.UploadStatuses.Failed;
            log.ReportTime = DateTime.Now;
            log.ReportMessage = response.Msg;
            _dbContext.Db.Updateable(log)
                .UpdateColumns(it => new { it.ReportStatus, it.ReportTime, it.ReportMessage })
                .Where(it => it.Id == log.Id)
                .ExecuteCommand();
        }
    }

    private static BasicRes<object> Success(string message)
    {
        return new BasicRes<object>
        {
            Status = AppConstants.MesStatus.Success,
            Msg = message,
            Data = new object()
        };
    }

    private static BasicRes<object> Unsupported(string message)
    {
        return new BasicRes<object>
        {
            Status = AppConstants.MesStatus.Error,
            Msg = message
        };
    }

    private static string FormatFailureMessages(IReadOnlyList<string> messages)
    {
        var visibleMessages = messages.Take(5).ToList();
        var suffix = messages.Count > visibleMessages.Count
            ? $"；其余 {messages.Count - visibleMessages.Count} 条失败请查看 MES 交互日志"
            : string.Empty;

        return string.Join("；", visibleMessages) + suffix;
    }

    private static bool IsRecordInTaskScope(BizWeldPointRecord record, BizUploadTask task)
    {
        var productNo = ReadProductNo(task.PayloadJson);
        return string.IsNullOrWhiteSpace(productNo)
            || string.Equals(record.ProductNo, productNo, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsProductScopedTask(BizUploadTask task)
    {
        return !string.IsNullOrWhiteSpace(ReadProductNo(task.PayloadJson));
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
            return document.RootElement.TryGetProperty("ProductNumber", out var productNoElement)
                ? productNoElement.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static T? ReadPayloadRequest<T>(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return default;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var element = document.RootElement.TryGetProperty("Request", out var requestElement)
                ? requestElement
                : document.RootElement;
            return element.Deserialize<T>();
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static string ReadStatusCode(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            if (root.TryGetProperty("StatusCode", out var statusCodeElement))
            {
                return statusCodeElement.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("Request", out var requestElement)
                && requestElement.TryGetProperty("ExpStatus", out var expStatusElement))
            {
                return expStatusElement.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
        }

        return string.Empty;
    }

    private static DeviceStatusUploadRequest? ReadDeviceStatusRequest(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            var logId = ReadInt(root, "LogId");
            var request = new ReportDeviceStatusReq
            {
                DeviceId = ReadString(root, "DeviceId"),
                DevStatus = ReadString(root, "DevStatus"),
                Ts = ReadString(root, "Ts"),
                Remark = ReadString(root, "Remark")
            };

            return string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.DevStatus)
                ? null
                : new DeviceStatusUploadRequest(logId, request);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static UploadPayloadInfo ReadUploadPayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return new UploadPayloadInfo();
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            return new UploadPayloadInfo
            {
                StationNo = ReadInt(root, "StationNo"),
                WorkOrderId = FirstNonEmpty(ReadString(root, "SN"), ReadString(root, "WorkOrder")),
                ProductNo = ReadString(root, "ProductNumber")
            };
        }
        catch (JsonException)
        {
            return new UploadPayloadInfo();
        }
    }

    private static int ReadInt(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) && element.TryGetInt32(out var value)
            ? value
            : 0;
    }

    private static string ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element)
            ? element.GetString() ?? string.Empty
            : string.Empty;
    }

    private static void MarkRetryRequested(BizUploadTask task)
    {
        task.Status = ProductionConstants.UploadStatuses.Pending;
        task.NextRetryTime = DateTime.Now;
        task.Message = "Manual retry requested.";
        task.UpdatedTime = DateTime.Now;
    }

    private BizUploadTask? FindExistingTask(BizUploadTask task)
    {
        return _dbContext.Db.Queryable<BizUploadTask>()
            .First(existing => existing.TaskType == task.TaskType
                && existing.Target == task.Target
                && existing.BusinessId == task.BusinessId);
    }

    private static void Normalize(BizUploadTask task)
    {
        task.TaskType = NormalizeTaskType(task.TaskType);
        task.Target = string.IsNullOrWhiteSpace(task.Target)
            ? ProductionConstants.UploadTargets.Mes
            : task.Target.Trim();
        task.BusinessId = string.IsNullOrWhiteSpace(task.BusinessId)
            ? throw new InvalidOperationException("上传任务业务ID不能为空。")
            : task.BusinessId.Trim();
        task.Status = NormalizeStatus(task.Status);
        task.FilePath = NormalizeNullableText(task.FilePath);
        task.PayloadJson = NormalizeNullableText(task.PayloadJson);
        task.Message = NormalizeNullableText(task.Message);
        task.MaxRetryCount = Math.Max(1, task.MaxRetryCount);
        task.RetryCount = Math.Max(0, task.RetryCount);
    }

    private static string NormalizeStatus(string? status)
    {
        return status switch
        {
            ProductionConstants.UploadStatuses.Pending => ProductionConstants.UploadStatuses.Pending,
            ProductionConstants.UploadStatuses.Uploading => ProductionConstants.UploadStatuses.Uploading,
            ProductionConstants.UploadStatuses.Uploaded => ProductionConstants.UploadStatuses.Uploaded,
            ProductionConstants.UploadStatuses.Failed => ProductionConstants.UploadStatuses.Failed,
            ProductionConstants.UploadStatuses.Retrying => ProductionConstants.UploadStatuses.Retrying,
            ProductionConstants.UploadStatuses.Skipped => ProductionConstants.UploadStatuses.Skipped,
            _ => ProductionConstants.UploadStatuses.Pending
        };
    }

    private static string? NormalizeNullableText(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue)
            ? null
            : normalizedValue;
    }

    private static bool IsActionRequired(string? status)
    {
        return status is ProductionConstants.UploadStatuses.Pending
            or ProductionConstants.UploadStatuses.Failed
            or ProductionConstants.UploadStatuses.Retrying;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    private static UploadTaskSummary ToSummary(BizUploadTask task)
    {
        return new UploadTaskSummary
        {
            Id = task.Id,
            TaskType = task.TaskType,
            Target = task.Target,
            BusinessId = task.BusinessId ?? string.Empty,
            Status = task.Status,
            RetryCount = task.RetryCount,
            MaxRetryCount = task.MaxRetryCount,
            NextRetryTime = task.NextRetryTime,
            LastAttemptTime = task.LastAttemptTime,
            CompletedTime = task.CompletedTime,
            FilePath = task.FilePath ?? string.Empty,
            Message = task.Message ?? string.Empty,
            CreatedTime = task.CreatedTime,
            UpdatedTime = task.UpdatedTime
        };
    }

    private sealed record UploadPayloadInfo
    {
        public int StationNo { get; init; }

        public string WorkOrderId { get; init; } = string.Empty;

        public string ProductNo { get; init; } = string.Empty;
    }

    private sealed record DeviceStatusUploadRequest(int LogId, ReportDeviceStatusReq Request);

    private sealed record ProcessParameterSchemeItem(DimTestItem Item, BizSchemeDetail Detail);

    private enum ProcessParameterValueRole
    {
        Actual,
        Upper,
        Lower,
        Result
    }

    private static string NormalizeTaskType(string? taskType)
    {
        return taskType switch
        {
            ProductionConstants.UploadTaskTypes.StartReport => ProductionConstants.UploadTaskTypes.StartReport,
            ProductionConstants.UploadTaskTypes.FinishReport => ProductionConstants.UploadTaskTypes.FinishReport,
            ProductionConstants.UploadTaskTypes.WorkOrderStatus => ProductionConstants.UploadTaskTypes.WorkOrderStatus,
            ProductionConstants.UploadTaskTypes.ProcessParameter => ProductionConstants.UploadTaskTypes.ProcessParameter,
            ProductionConstants.UploadTaskTypes.ReportFile => ProductionConstants.UploadTaskTypes.ReportFile,
            ProductionConstants.UploadTaskTypes.ProgramFile => ProductionConstants.UploadTaskTypes.ProgramFile,
            ProductionConstants.UploadTaskTypes.DeviceStatus => ProductionConstants.UploadTaskTypes.DeviceStatus,
            _ => ProductionConstants.UploadTaskTypes.ProcessParameter
        };
    }
}
