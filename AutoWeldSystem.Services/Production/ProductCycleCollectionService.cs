using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
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
    private readonly IPlcExpressionReadService _plcExpressionReadService;
    private readonly IOperationLogService _operationLogService;
    private readonly IProductionFlowLogService _productionLogService;
    private readonly object _dbLock = new();

    public ProductCycleCollectionService(
        SqlSugarDbContext dbContext,
        IProductProcessConfigService productProcessConfigService,
        IPlcExpressionReadService plcExpressionReadService,
        IOperationLogService operationLogService,
        IProductionFlowLogService productionLogService)
    {
        _dbContext = dbContext;
        _productProcessConfigService = productProcessConfigService;
        _plcExpressionReadService = plcExpressionReadService;
        _operationLogService = operationLogService;
        _productionLogService = productionLogService;
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

        _productionLogService.Write(
            "ProductDataReadStart",
            "开始采集产品周期数据",
            $"SchemeId={processConfig.SchemeId}, ProductBase={processConfig.ProductBase}, TouchBase={processConfig.TouchBase}, TestBase={processConfig.TestBase}, TouchCount={processConfig.TouchCount}",
            stationNo: normalizedStationNo,
            workOrderId: task.WorkOrderId,
            programId: task.ProgramId ?? string.Empty);

        var header = await ReadProductHeaderAsync(processConfig, cancellationToken);
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
                touchIndex,
                cancellationToken));
        }

        SaveRecords(task.Id, normalizedStationNo, records);

        _productionLogService.Write(
            "ProductDataSaved",
            "产品采集数据已保存",
            $"ProductNo={header.ProductNo}, TouchCount={records.Count}, Result={header.ProductResult}",
            stationNo: normalizedStationNo,
            workOrderId: task.WorkOrderId,
            productNo: header.ProductNo,
            programId: task.ProgramId ?? string.Empty);

        _operationLogService.Write(
            "ProductCycleCollection",
            $"Product collected, Station={normalizedStationNo}, WorkOrder={task.WorkOrderId}, ProductNo={header.ProductNo}, TouchCount={records.Count}, Result={header.ProductResult}");

        return records;
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
                    return new SchemeItemSnapshot(detail.DetailId, item);
                })
                .ToList();
        }
    }

    private async Task<ProductHeaderSnapshot> ReadProductHeaderAsync(
        BizProductProcessConfig config,
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

        var productResultRaw = await ReadExpressionValueAsync(
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
            presetTouchText);
    }

    private async Task<BizWeldPointRecord> ReadWeldPointRecordAsync(
        BizWeldTask task,
        int stationNo,
        BizProductProcessConfig config,
        IReadOnlyList<SchemeItemSnapshot> schemeItems,
        ProductHeaderSnapshot header,
        int touchIndex,
        CancellationToken cancellationToken)
    {
        var touchContextOffset = config.TouchHeaderLen * (touchIndex - 1);
        var testContextOffset = config.TestAreaLen * (touchIndex - 1);

        var touchNo = await ReadExpressionValueAsync(
            config.TouchBase,
            touchContextOffset,
            config.TouchNoExpr,
            "焊点编号",
            cancellationToken);
        var touchResultRaw = await ReadExpressionValueAsync(
            config.TouchBase,
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
        AddValue(values, "touch_no_raw", touchNo);
        AddValue(values, "touch_result_raw", touchResultRaw);

        foreach (var schemeItem in schemeItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ReadTestItemValuesAsync(
                config,
                schemeItem.Item,
                testContextOffset,
                values,
                cancellationToken);
        }

        var resultRaw = FirstValue(values, "touch_result_raw", "product_result");
        return new BizWeldPointRecord
        {
            TaskId = task.Id,
            ExpStartId = task.ExpStartId,
            DeviceId = task.DeviceId,
            SN = task.WorkOrderId,
            ProcessNo = task.ProcessNo,
            ProductNo = header.ProductNo,
            TouchNo = string.IsNullOrWhiteSpace(touchNo) ? touchIndex.ToString(CultureInfo.InvariantCulture) : touchNo.Trim(),
            StationNo = stationNo,
            MaxElectric = FirstValue(values, "max_electric", "峰值电流"),
            MaxVoltage = FirstValue(values, "max_voltage", "峰值电压"),
            ValidPower = FirstValue(values, "valid_power", "有效功率"),
            Displacement = FirstValue(values, "displacement", "位移"),
            WeldTs = FirstValue(values, "weld_ts", "焊接时间"),
            TestResultRaw = resultRaw,
            TestResult = NormalizeTestResult(resultRaw),
            OperatorNo = task.StartOperatorNumber,
            RecordTime = DateTime.Now,
            ProductCompleted = touchIndex >= header.ActualTouchCount,
            UploadStatus = ProductionConstants.UploadStatuses.Pending,
            RawDataJson = JsonSerializer.Serialize(values)
        };
    }

    private async Task ReadTestItemValuesAsync(
        BizProductProcessConfig config,
        DimTestItem item,
        int testContextOffset,
        IDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var itemKey = ResolveItemKey(item);
        var actualValue = await ReadExpressionValueAsync(
            config.TestBase,
            testContextOffset,
            item.ActualExpression,
            $"{item.ItemName}实际值",
            cancellationToken);
        AddValue(values, itemKey, actualValue);
        AddValue(values, item.ItemName, actualValue);

        var upperValue = await ReadOptionalExpressionValueAsync(
            config.TestBase,
            testContextOffset,
            item.UpperExpression,
            $"{item.ItemName}上限",
            cancellationToken);
        AddValue(values, $"{itemKey}_upper", upperValue);
        AddValue(values, $"{item.ItemName}上限", upperValue);

        var lowerValue = await ReadOptionalExpressionValueAsync(
            config.TestBase,
            testContextOffset,
            item.LowerExpression,
            $"{item.ItemName}下限",
            cancellationToken);
        AddValue(values, $"{itemKey}_lower", lowerValue);
        AddValue(values, $"{item.ItemName}下限", lowerValue);

        var resultValue = await ReadOptionalExpressionValueAsync(
            config.TestBase,
            testContextOffset,
            item.ResultExpression,
            $"{item.ItemName}结果",
            cancellationToken);
        AddValue(values, $"{itemKey}_result", resultValue);
        AddValue(values, $"{item.ItemName}结果", resultValue);
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
            var result = await _plcExpressionReadService.ReadResolvedAddressTextAsync(
                binding.Address,
                binding.DataType,
                binding.Rule,
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

    private void SaveRecords(int taskId, int stationNo, IReadOnlyList<BizWeldPointRecord> records)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            foreach (var record in records)
            {
                record.SequenceNo = GetNextSequenceNo(taskId, stationNo);
                var saved = _dbContext.Db.Insertable(record).ExecuteReturnEntity();
                record.Id = saved.Id;
            }
        }
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
    {
        if (string.IsNullOrWhiteSpace(rawResult))
        {
            return ProductionConstants.TestResults.Unknown;
        }

        return string.Equals(rawResult.Trim(), ProductionConstants.TestResults.OkRawValue, StringComparison.Ordinal)
            || string.Equals(rawResult.Trim(), ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase)
            ? ProductionConstants.TestResults.Ok
            : ProductionConstants.TestResults.Ng;
    }

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

    private sealed record ProductHeaderSnapshot(
        string ProductNo,
        int ActualTouchCount,
        int PresetTouchCount,
        string ProductResult,
        string? PlcPresetTouchCount);

    private sealed record SchemeItemSnapshot(int DetailId, DimTestItem Item);
}
