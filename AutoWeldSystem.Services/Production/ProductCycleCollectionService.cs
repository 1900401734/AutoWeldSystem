using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Data;
using System.Globalization;
using System.Text.Json;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 产品周期采集服务。
/// 当前只实现最小闭环：按产品工艺配置分开读取产品头、焊点头和测试项数据区。
/// </summary>
public sealed class ProductCycleCollectionService : IProductCycleCollectionService
{
    private const string Category = "PLC.ProductCycleCollection";

    private readonly SqlSugarDbContext _dbContext;
    private readonly IProductProcessConfigService _productProcessConfigService;
    private readonly IAppSettingsService _settingsService;
    private readonly IPlcExpressionReadService _plcExpressionReadService;
    private readonly IOperationLogService _operationLogService;
    private readonly IProductionFlowLogService _productionLogService;
    private readonly IProductionReportFileService _reportFileService;
    private readonly object _dbLock = new();

    public ProductCycleCollectionService(
        SqlSugarDbContext dbContext,
        IProductProcessConfigService productProcessConfigService,
        IAppSettingsService settingsService,
        IPlcExpressionReadService plcExpressionReadService,
        IOperationLogService operationLogService,
        IProductionFlowLogService productionLogService,
        IProductionReportFileService reportFileService)
    {
        _dbContext = dbContext;
        _productProcessConfigService = productProcessConfigService;
        _settingsService = settingsService;
        _plcExpressionReadService = plcExpressionReadService;
        _operationLogService = operationLogService;
        _productionLogService = productionLogService;
        _reportFileService = reportFileService;
    }

    public async Task<IReadOnlyList<BizWeldPointRecord>> CollectAsync(
        BizWeldTask task,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default)
    {
        if (task.Id <= 0)
        {
            throw new BusinessOperationException(Category, "产品数据采集失败", "焊接任务尚未保存，无法采集产品数据。");
        }

        var normalizedStationNo = NormalizeStationNo(stationNo, task);
        var processConfig = ResolveProcessConfig(task, normalizedStationNo);
        var schemeItems = ResolveSchemeItems(processConfig.SchemeId);
        var settings = _settingsService.Get();
        var resultSource = ProductionConstants.InspectionResultSources.Normalize(settings.InspectionResultSource);
        var useProgramResult = WholePieceProgramResultRules.IsApplicable(
            settings.ProcessParameterDeviceType,
            resultSource);

        _productionLogService.Write(
            "ProductDataReadStart",
            ProductionFlowLogTexts.Summaries.ProductDataReadStart,
            $"SchemeId={processConfig.SchemeId}, ProductBase={processConfig.ProductBase}, TouchBase={processConfig.TouchBase}, TestBase={processConfig.TestBase}, TouchCount={processConfig.TouchCount}, ResultSource={resultSource}, ProgramResult={useProgramResult}",
            stationNo: normalizedStationNo,
            workOrderId: task.SN,
            programId: task.ProgramId ?? string.Empty);
        var header = await ReadProductHeaderAsync(processConfig, useProgramResult, cancellationToken);
        var records = new List<BizWeldPointRecord>();
        for (var touchIndex = 1; touchIndex <= header.ActualTouchCount; touchIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            records.Add(await ReadWeldPointRecordAsync(
                task,
                normalizedStationNo,
                processConfig,
                schemeItems,
                header,
                useProgramResult,
                touchIndex,
                cancellationToken));
        }

        if (useProgramResult)
        {
            ApplyProgramCalculatedResults(task, schemeItems, records);
        }

        ValidateCollectedRecords(processConfig, schemeItems, records);

        bool isRetest;
        try
        {
            isRetest = SaveRecords(task.Id, normalizedStationNo, records);
        }
        catch (Exception ex)
        {
            _productionLogService.Write(
                "ProductDataSaveFailed",
                ProductionFlowLogTexts.Summaries.ProductDataSaveFailed,
                $"ProductNumber={header.ProductNo}, TouchCount={records.Count}, Error={ex.Message}",
                "Error",
                normalizedStationNo,
                task.SN,
                header.ProductNo,
                task.ProgramId ?? string.Empty);
            throw;
        }

        RefreshReportAfterProductSaved(task, normalizedStationNo, header.ProductNo, records.Count);

        _productionLogService.Write(
            "ProductDataSaved",
            ProductionFlowLogTexts.Summaries.ProductDataSaved,
            $"ProductNumber={header.ProductNo}, TouchCount={records.Count}, Result={records.FirstOrDefault()?.ProductResult ?? header.ProductResult}",
            stationNo: normalizedStationNo,
            workOrderId: task.SN,
            productNo: header.ProductNo,
            programId: task.ProgramId ?? string.Empty);

        _operationLogService.Write(
            "ProductCycleCollection",
            $"Product collected, Station={normalizedStationNo}, WorkOrder={task.SN}, ProductNumber={header.ProductNo}, TouchCount={records.Count}, Retest={isRetest}, Result={records.FirstOrDefault()?.ProductResult ?? header.ProductResult}");

        return records;
    }

    /// <summary>
    /// 每完成一件产品就增量刷新一次 XLSX 报表，避免等到完工上报时才统一生成。
    /// 报表服务会根据 TaskId 重读持久化任务，完工时同一入口会覆盖为最终统计和结束时间。
    /// 报表生成失败不应阻断 PLC 采集反馈，因此这里只记录日志。
    /// </summary>
    private void RefreshReportAfterProductSaved(BizWeldTask task, int stationNo, string productNo, int touchCount)
    {
        try
        {
            var reportFile = _reportFileService.GenerateXlsxReport(task);
            _operationLogService.Write(
                "ReportFile",
                $"Report file refreshed after product saved, Station={stationNo}, WorkOrder={task.SN}, ProductNumber={productNo}, TouchCount={touchCount}, FilePath={reportFile.FilePath}");
        }
        catch (Exception ex)
        {
            _operationLogService.Write(
                "ReportFile",
                $"Report file refresh failed after product saved, Station={stationNo}, WorkOrder={task.SN}, ProductNumber={productNo}, Error={ex.Message}");
        }
    }

    private BizProductProcessConfig ResolveProcessConfig(BizWeldTask task, int stationNo)
    {
        var config = _productProcessConfigService.FindActiveForTask(task, stationNo);
        if (config is not null)
        {
            return config;
        }

        throw new BusinessOperationException(
            Category,
            "产品工艺配置未找到",
            $"未找到产品工号“{task.ProductNum}”、工位“{stationNo}”对应的产品工艺配置。");
    }

    private IReadOnlyList<SchemeItemSnapshot> ResolveSchemeItems(string schemeId)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var details = _dbContext.Db.Queryable<BizSchemeDetail>()
                .Where(detail => detail.SchemeId == schemeId)
                .ToList()
                .OrderBy(detail => detail.DetailId)
                .ToList();
            if (details.Count == 0)
            {
                throw new BusinessOperationException(Category, "测试方案未配置", $"测试方案“{schemeId}”未配置任何测试项。");
            }

            var itemIds = details.Select(detail => detail.ItemId).Distinct().ToList();
            var items = _dbContext.Db.Queryable<DimTestItem>()
                .Where(item => itemIds.Contains(item.ItemId))
                .ToList();

            return details
                .Select(detail =>
                {
                    var item = items.FirstOrDefault(it => it.ItemId == detail.ItemId)
                        ?? throw new BusinessOperationException(Category, "测试项字典缺失", $"测试项ID“{detail.ItemId}”不存在。");
                    SchemeDetailRoleRules.ClearUnavailableRoles(detail, item);
                    return new SchemeItemSnapshot(detail.DetailId, item, detail);
                })
                .Where(snapshot => HasAnyEnabledRole(snapshot.Detail))
                .ToList();
        }
    }

    private async Task<ProductHeaderSnapshot> ReadProductHeaderAsync(
        BizProductProcessConfig config,
        bool useProgramResult,
        CancellationToken cancellationToken)
    {
        var productNo = await ReadExpressionValueAsync(
            config.ProductBase,
            0,
            config.ProductNoExpr,
            "产品编号",
            cancellationToken);
        if (string.IsNullOrWhiteSpace(productNo))
        {
            throw new BusinessOperationException(Category, "产品数据采集失败", "产品头中未读取到产品编号。");
        }

        var productResultRaw = useProgramResult
            ? await ReadBestEffortExpressionValueAsync(
                config.ProductBase,
                0,
                config.ProductResultExpr,
                cancellationToken)
            : await ReadExpressionValueAsync(
                config.ProductBase,
                0,
                config.ProductResultExpr,
                "产品结果",
                cancellationToken);
        var presetTouchText = await ReadOptionalExpressionValueAsync(
            config.ProductBase,
            0,
            config.PresetTouchCountExpr,
            "预设焊点数",
            cancellationToken);
        var actualTouchText = await ReadOptionalExpressionValueAsync(
            config.ProductBase,
            0,
            config.ActualTouchCountExpr,
            "实际焊点数",
            cancellationToken);

        var actualTouchCount = ParsePositiveInt(actualTouchText) ?? config.TouchCount;
        if (actualTouchCount <= 0)
        {
            throw new BusinessOperationException(Category, "产品数据采集失败", "实际焊点数无效。");
        }

        if (actualTouchCount > config.TouchCount)
        {
            throw new BusinessOperationException(
                Category,
                "产品数据采集失败",
                $"实际焊点数“{actualTouchCount}”不能大于本地预设焊点数“{config.TouchCount}”。");
        }

        return new ProductHeaderSnapshot(
            productNo.Trim(),
            actualTouchCount,
            config.TouchCount,
            NormalizeTestResult(productResultRaw),
            productResultRaw,
            presetTouchText);
    }

    private async Task<BizWeldPointRecord> ReadWeldPointRecordAsync(
        BizWeldTask task,
        int stationNo,
        BizProductProcessConfig config,
        IReadOnlyList<SchemeItemSnapshot> schemeItems,
        ProductHeaderSnapshot header,
        bool useProgramResult,
        int touchIndex,
        CancellationToken cancellationToken)
    {
        var touchContextOffset = config.TouchHeaderLen * (touchIndex - 1);
        var testContextOffset = config.TestAreaLen * (touchIndex - 1);

        var touchNo = await ReadExpressionValueAsync(
            ResolveTouchNoBase(config),
            touchContextOffset,
            config.TouchNoExpr,
            "焊点编号",
            cancellationToken);
        var touchResultRaw = await ReadExpressionValueAsync(
            ResolveTouchResultBase(config),
            touchContextOffset,
            config.TouchResultExpr,
            "焊点结果",
            cancellationToken);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["product_no"] = header.ProductNo,
            ["touch_count"] = header.PresetTouchCount.ToString(CultureInfo.InvariantCulture),
            ["actual_touch_count"] = header.ActualTouchCount.ToString(CultureInfo.InvariantCulture)
        };
        AddValue(values, "plc_preset_touch_count", header.PlcPresetTouchCount);
        AddValue(values, "product_result", header.ProductResult);
        AddValue(values, "product_result_raw", header.ProductResultRaw);
        AddValue(values, "touch_no_raw", touchNo);
        AddValue(values, "touch_result_raw", touchResultRaw);

        var testResult = NormalizeTestResult(touchResultRaw);
        if (!TestResultRules.IsPreWeldNg(testResult)
            && (!useProgramResult || ProductRealtimePreviewRules.ShouldReadTestValues(testResult)))
        {
            foreach (var schemeItem in schemeItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ReadTestItemValuesAsync(
                    config,
                    schemeItem,
                    testContextOffset,
                    values,
                    useProgramResult,
                    cancellationToken);
            }
        }

        return new BizWeldPointRecord
        {
            TaskId = task.Id,
            ExpStartId = task.ExpStartId ?? string.Empty,
            DeviceId = task.DeviceId ?? string.Empty,
            SN = task.SN ?? string.Empty,
            ProcessNo = task.ProcessNo ?? string.Empty,
            ProductNo = header.ProductNo ?? string.Empty,
            TouchNo = string.IsNullOrWhiteSpace(touchNo) ? touchIndex.ToString(CultureInfo.InvariantCulture) : touchNo.Trim(),
            StationNo = stationNo,
            TestResult = testResult,
            ProductResult = header.ProductResult,
            OperatorNo = task.UserNumber ?? string.Empty,
            Ts = DateTime.Now,
            ProductCompleted = touchIndex >= header.ActualTouchCount,
            UploadStatus = ProductionConstants.UploadStatuses.Pending,
            RawDataJson = JsonSerializer.Serialize(values)
        };
    }

    private async Task ReadTestItemValuesAsync(
        BizProductProcessConfig config,
        SchemeItemSnapshot schemeItem,
        int testContextOffset,
        IDictionary<string, string> values,
        bool useProgramResult,
        CancellationToken cancellationToken)
    {
        var item = schemeItem.Item;
        var itemKey = ResolveItemKey(item);
        if (SchemeDetailRoleRules.ShouldReadProductRole(schemeItem.Detail, SchemeDetailValueRole.Actual)
            || (useProgramResult && schemeItem.Detail.EnableActual))
        {
            var actualValue = await ReadExpressionValueAsync(
                config.TestBase,
                testContextOffset,
                item.ActualExpression,
                $"{item.ItemName}实际值",
                cancellationToken);
            AddValue(values, itemKey, actualValue);
            AddValue(values, item.ItemName, actualValue);
        }

        if (SchemeDetailRoleRules.ShouldReadProductRole(schemeItem.Detail, SchemeDetailValueRole.Upper))
        {
            var upperValue = await ReadOptionalExpressionValueAsync(
                config.TestBase,
                testContextOffset,
                item.UpperExpression,
                $"{item.ItemName}上限",
                cancellationToken);
            AddValue(values, $"{itemKey}_upper", upperValue);
            AddValue(values, $"{item.ItemName}上限", upperValue);
        }

        if (SchemeDetailRoleRules.ShouldReadProductRole(schemeItem.Detail, SchemeDetailValueRole.Lower))
        {
            var lowerValue = await ReadOptionalExpressionValueAsync(
                config.TestBase,
                testContextOffset,
                item.LowerExpression,
                $"{item.ItemName}下限",
                cancellationToken);
            AddValue(values, $"{itemKey}_lower", lowerValue);
            AddValue(values, $"{item.ItemName}下限", lowerValue);
        }

        if (SchemeDetailRoleRules.ShouldReadProductRole(schemeItem.Detail, SchemeDetailValueRole.Result))
        {
            var resultValue = await ReadOptionalExpressionValueAsync(
                config.TestBase,
                testContextOffset,
                item.ResultExpression,
                $"{item.ItemName}结果",
                cancellationToken);
            AddValue(values, $"{itemKey}_result", resultValue);
            AddValue(values, $"{item.ItemName}结果", resultValue);
        }
    }

    private async Task<string?> ReadBestEffortExpressionValueAsync(
        string baseAddress,
        int contextOffset,
        string? expressionText,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(expressionText))
        {
            return null;
        }

        try
        {
            return await ReadExpressionValueAsync(
                baseAddress,
                contextOffset,
                expressionText,
                "PLC原始产品结果",
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // 程序计算模式不依赖 PLC 产品结果；读取失败只影响原始追溯字段，不能阻塞产品采集。
            return null;
        }
    }

    private async Task<string?> ReadOptionalExpressionValueAsync(
        string baseAddress,
        int contextOffset,
        string? expressionText,
        string valueRole,
        CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(expressionText)
            ? null
            : await ReadExpressionValueAsync(baseAddress, contextOffset, expressionText, valueRole, cancellationToken);
    }

    private async Task<string> ReadExpressionValueAsync(
        string baseAddress,
        int contextOffset,
        string expressionText,
        string valueRole,
        CancellationToken cancellationToken)
    {
        try
        {
            var binding = _plcExpressionReadService.Resolve(baseAddress, contextOffset, expressionText);
            var result = await _plcExpressionReadService.ReadBindingTextAsync(
                binding,
                valueRole,
                cancellationToken: cancellationToken);
            if (result.IsSuccess)
            {
                return result.Value ?? string.Empty;
            }

            throw new BusinessOperationException(Category, "产品数据采集失败", result.Message);
        }
        catch (FormatException ex)
        {
            throw new BusinessOperationException(Category, "偏移表达式无效", $"{valueRole}：{ex.Message}");
        }
    }

    private void ApplyProgramCalculatedResults(
        BizWeldTask task,
        IReadOnlyList<SchemeItemSnapshot> schemeItems,
        IReadOnlyList<BizWeldPointRecord> records)
    {
        var participatingItems = schemeItems
            .Where(item => item.Detail.EnableActual)
            .ToList();

        foreach (var record in records)
        {
            if (TestResultRules.IsPreWeldNg(record.TestResult))
            {
                continue;
            }

            if (!ProductRealtimePreviewRules.ShouldReadTestValues(record.TestResult))
            {
                throw new BusinessOperationException(
                    Category,
                    "产品数据采集失败",
                    $"产品“{record.ProductNo}”面“{record.TouchNo}”的 PLC 结果未表示测试完成，无法进行程序判定。");
            }

            var rawValues = ParseRawData(record.RawDataJson);
            var measurements = participatingItems
                .Select(item => new WholePieceProgramMeasurement(
                    item.Item.ItemName,
                    FirstValue(rawValues, ResolveItemKey(item.Item), item.Item.ItemName)))
                .ToList();
            var result = WholePieceProgramResultRules.EvaluateFace(task.ProgramContentSnapshot, measurements);
            if (!result.IsSuccess)
            {
                throw new ProductCollectionHandledException(
                    Category,
                    "产品数据采集配置错误",
                    $"产品“{record.ProductNo}”面“{record.TouchNo}”程序判定失败：{result.ErrorMessage}");
            }

            record.TestResult = result.Result;
            record.RawDataJson = AddRawValues(record.RawDataJson, new Dictionary<string, string>
            {
                ["program_touch_result"] = result.Result
            });
        }

        var productResult = TestResultRules.ResolveProductResult(records.Select(record => record.TestResult));
        if (string.Equals(productResult, ProductionConstants.TestResults.Unknown, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessOperationException(Category, "产品数据采集失败", "四面程序判定结果不完整，无法生成产品结果。");
        }

        foreach (var record in records)
        {
            record.ProductResult = productResult;
            record.RawDataJson = AddRawValues(record.RawDataJson, new Dictionary<string, string>
            {
                ["program_product_result"] = productResult
            });
        }
    }

    private static IReadOnlyDictionary<string, string> ParseRawData(string? rawDataJson)
    {
        if (string.IsNullOrWhiteSpace(rawDataJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(rawDataJson);
            return values is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string AddRawValues(string? rawDataJson, IReadOnlyDictionary<string, string> additions)
    {
        var values = new Dictionary<string, string>(ParseRawData(rawDataJson), StringComparer.OrdinalIgnoreCase);
        foreach (var addition in additions)
        {
            values[addition.Key] = addition.Value;
        }

        return JsonSerializer.Serialize(values);
    }

    private void ValidateCollectedRecords(
        BizProductProcessConfig config,
        IReadOnlyList<SchemeItemSnapshot> schemeItems,
        IReadOnlyList<BizWeldPointRecord> records)
    {
        var settings = _settingsService.Get();
        if (!WholePieceAbAggregationRules.IsApplicable(settings.ProcessParameterDeviceType, config.TouchCount))
        {
            return;
        }

        var invalidOutput = schemeItems.FirstOrDefault(item => SchemeDetailRoleRules.AllRoles
            .Where(role => role != SchemeDetailValueRole.Actual)
            .Any(role => SchemeDetailRoleRules.IsReportEnabled(item.Detail, role)
                || SchemeDetailRoleRules.IsMesEnabled(item.Detail, role)));
        if (invalidOutput is not null)
        {
            throw new BusinessOperationException(
                Category,
                "产品数据采集失败",
                $"整件检测A/B模式只允许测试项“{invalidOutput.Item.ItemName}”的实际值写入报表或上传MES。");
        }

        var duplicateMesFields = schemeItems
            .Where(item => SchemeDetailRoleRules.IsMesEnabled(item.Detail, SchemeDetailValueRole.Actual))
            .Select(item => SchemeDetailRoleRules.GetMesFieldName(item.Detail, SchemeDetailValueRole.Actual)?.Trim())
            .Where(field => !string.IsNullOrWhiteSpace(field))
            .GroupBy(field => field!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicateMesFields.Count > 0)
        {
            throw new BusinessOperationException(
                Category,
                "产品数据采集失败",
                $"整件检测A/B模式存在重复MES字段名：{string.Join("、", duplicateMesFields)}。");
        }

        var definitions = schemeItems
            .Where(item => SchemeDetailRoleRules.IsReportEnabled(item.Detail, SchemeDetailValueRole.Actual)
                || SchemeDetailRoleRules.IsMesEnabled(item.Detail, SchemeDetailValueRole.Actual))
            .Select(item => new WholePieceAbValueDefinition(
                item.Item.ItemId,
                item.Item.ItemName,
                ResolveItemKey(item.Item),
                item.Item.ActualExpression))
            .ToList();
        if (definitions.Count == 0)
        {
            return;
        }

        var validation = WholePieceAbAggregationRules.ValidateSourceRecords(records, definitions);
        if (!validation.IsSuccess)
        {
            throw new BusinessOperationException(Category, "产品数据采集失败", validation.ErrorMessage);
        }
    }

    private bool SaveRecords(int taskId, int stationNo, IReadOnlyList<BizWeldPointRecord> records)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var isRetest = IsRetestCollection(taskId, stationNo, records);
            var nextSequenceNo = GetNextSequenceNo(taskId, stationNo);
            foreach (var record in records)
            {
                var existingRecord = FindExistingRecord(record);
                if (existingRecord is not null)
                {
                    record.Id = existingRecord.Id;
                    record.SequenceNo = existingRecord.SequenceNo;
                    if (!isRetest)
                    {
                        continue;
                    }

                    // 重测就地覆盖已有记录，使报表、产品历史和上传任务沿用同一产品级自然键。
                    ProductRetestRules.ApplyRetestValues(existingRecord, record);
                    _dbContext.Db.Updateable(existingRecord).ExecuteCommand();
                    record.UploadStatus = existingRecord.UploadStatus;
                    continue;
                }

                record.SequenceNo = nextSequenceNo++;
                var saved = _dbContext.Db.Insertable(record).ExecuteReturnEntity();
                record.Id = saved.Id;
            }

            if (isRetest)
            {
                RemoveStaleRetestRecords(taskId, stationNo, records);
            }

            return isRetest;
        }
    }

    /// <summary>
    /// 判断本轮采集是否为同一产品的重测。
    /// 只有整件检测设备参与判定，且必须与本任务本工位最近一次采集的产品编号相同。
    /// </summary>
    private bool IsRetestCollection(int taskId, int stationNo, IReadOnlyList<BizWeldPointRecord> records)
    {
        var incomingProductNo = records.FirstOrDefault()?.ProductNo;
        if (string.IsNullOrWhiteSpace(incomingProductNo))
        {
            return false;
        }

        var deviceType = _settingsService.Get().ProcessParameterDeviceType;
        if (!ProductRetestRules.IsSupportedDeviceType(deviceType))
        {
            return false;
        }

        return ProductRetestRules.IsRetest(
            deviceType,
            FindLatestProductNo(taskId, stationNo),
            incomingProductNo);
    }

    /// <summary>
    /// 读取本任务本工位最近一次采集的产品编号，用于识别“紧邻上一轮”的重测。
    /// </summary>
    private string? FindLatestProductNo(int taskId, int stationNo)
    {
        return _dbContext.Db.Queryable<BizWeldPointRecord>()
            .Where(record => record.TaskId == taskId && record.StationNo == stationNo)
            .ToList()
            .OrderByDescending(record => record.SequenceNo)
            .ThenByDescending(record => record.Id)
            .FirstOrDefault()
            ?.ProductNo;
    }

    /// <summary>
    /// 删除上一轮多余、本轮未覆盖的残留面记录，避免同一产品混合两轮数据。
    /// 现场 PLC 等全部视觉数据测试完成才触发采集，实际面数恒定，该分支正常不会命中。
    /// </summary>
    private void RemoveStaleRetestRecords(int taskId, int stationNo, IReadOnlyList<BizWeldPointRecord> records)
    {
        var productNo = records.FirstOrDefault()?.ProductNo?.Trim();
        if (string.IsNullOrWhiteSpace(productNo))
        {
            return;
        }

        var existingRecords = _dbContext.Db.Queryable<BizWeldPointRecord>()
            .Where(record => record.TaskId == taskId
                && record.StationNo == stationNo
                && record.ProductNo == productNo)
            .ToList();
        var staleRecords = ProductRetestRules.SelectStaleRecords(existingRecords, records);
        if (staleRecords.Count == 0)
        {
            return;
        }

        _dbContext.Db.Deleteable(staleRecords.ToList()).ExecuteCommand();
        _operationLogService.Write(
            "ProductCycleCollection",
            $"Retest removed stale records, Station={stationNo}, ProductNumber={productNo}, StaleCount={staleRecords.Count}");
    }

    /// <summary>
    /// 同工单双工位可能共享产品就绪业务信号；使用产品级自然键避免同一焊点被重复插入。
    /// </summary>
    private BizWeldPointRecord? FindExistingRecord(BizWeldPointRecord record)
    {
        return _dbContext.Db.Queryable<BizWeldPointRecord>()
            .First(existing => existing.TaskId == record.TaskId
                && existing.StationNo == record.StationNo
                && existing.ProductNo == record.ProductNo
                && existing.TouchNo == record.TouchNo);
    }

    private int GetNextSequenceNo(int taskId, int stationNo)
    {
        var existingRecords = _dbContext.Db.Queryable<BizWeldPointRecord>()
            .Where(record => record.TaskId == taskId && record.StationNo == stationNo)
            .ToList();

        return existingRecords.Count == 0
            ? 1
            : existingRecords.Max(record => record.SequenceNo) + 1;
    }

    private static void EnsureReadSuccess(bool isSuccess, string message, string address, string valueRole)
    {
        if (isSuccess)
        {
            return;
        }

        throw new BusinessOperationException(
            Category,
            "产品数据采集失败",
            $"{valueRole}地址“{address}”读取失败：{message}");
    }

    private static int? ParsePositiveInt(string? value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return null;
        }

        return result > 0 ? result : null;
    }

    private static void AddValue(IDictionary<string, string> values, string? key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key) || value is null)
        {
            return;
        }

        values[key.Trim()] = value;
    }

    private static bool HasAnyEnabledRole(BizSchemeDetail detail)
    {
        return SchemeDetailRoleRules.HasAnyConfiguredRole(detail);
    }

    private static string? FirstValue(IReadOnlyDictionary<string, string> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value))
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

    private static string NormalizeTestResult(string? rawResult)
        => TestResultRules.Normalize(rawResult);

    private static int NormalizeStationNo(int stationNo, BizWeldTask task)
    {
        if (stationNo > ProductionConstants.Stations.SharedStationNo)
        {
            return stationNo;
        }

        return task.StationNo > ProductionConstants.Stations.SharedStationNo
            ? task.StationNo
            : ProductionConstants.Stations.DefaultStationNo;
    }

    private static string ResolveTouchNoBase(BizProductProcessConfig config)
        => string.IsNullOrWhiteSpace(config.TouchNoBase) ? config.TouchBase : config.TouchNoBase!.Trim();

    private static string ResolveTouchResultBase(BizProductProcessConfig config)
        => string.IsNullOrWhiteSpace(config.TouchResultBase) ? config.TouchBase : config.TouchResultBase!.Trim();

    private sealed record ProductHeaderSnapshot(
        string ProductNo,
        int ActualTouchCount,
        int PresetTouchCount,
        string ProductResult,
        string? ProductResultRaw,
        string? PlcPresetTouchCount);

    private sealed record SchemeItemSnapshot(int DetailId, DimTestItem Item, BizSchemeDetail Detail);
}
