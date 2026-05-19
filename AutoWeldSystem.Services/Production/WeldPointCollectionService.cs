using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Data;
using System.Globalization;
using System.Text.Json;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 焊点数据采集服务实现。
/// 该服务只负责“读取 PLC 参数 -> 生成 ProductNo/TouchNo -> 保存焊点记录”，不直接决定何时触发采集。
/// </summary>
public sealed class WeldPointCollectionService : IWeldPointCollectionService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IProductProcessConfigService _productProcessConfigService;
    private readonly ITestItemTemplateService _testItemTemplateService;
    private readonly IProductNoGeneratorService _productNoGeneratorService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IOperationLogService _operationLogService;
    private readonly object _dbLock = new();

    public WeldPointCollectionService(
        SqlSugarDbContext dbContext,
        IProductProcessConfigService productProcessConfigService,
        ITestItemTemplateService testItemTemplateService,
        IProductNoGeneratorService productNoGeneratorService,
        IPlcCommunicationService plcCommunicationService,
        IOperationLogService operationLogService)
    {
        _dbContext = dbContext;
        _productProcessConfigService = productProcessConfigService;
        _testItemTemplateService = testItemTemplateService;
        _productNoGeneratorService = productNoGeneratorService;
        _plcCommunicationService = plcCommunicationService;
        _operationLogService = operationLogService;
    }

    public async Task<BizWeldPointRecord> CollectAsync(
        BizWeldTask task,
        int stationNo = ProductionConstants.Stations.DefaultStationNo,
        CancellationToken cancellationToken = default)
    {
        if (task.Id <= 0)
        {
            throw new BusinessOperationException("PLC.WeldPointCollection", "焊点数据采集失败", "焊接任务尚未保存，无法采集焊点数据。");
        }

        var normalizedStationNo = NormalizeStationNo(stationNo, task);
        var processConfig = ResolveProcessConfig(task, normalizedStationNo);
        BizProductInstance product;
        int touchNumber;

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            product = _productNoGeneratorService.GetOrCreateStationProduct(
                task,
                normalizedStationNo,
                processConfig.WeldPointCount);
            touchNumber = product.CollectedTouchCount + 1;
        }

        var values = await ReadCollectionValuesAsync(processConfig, normalizedStationNo, touchNumber, cancellationToken);

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var record = BuildRecord(task, normalizedStationNo, product, touchNumber, values);
            record = _dbContext.Db.Insertable(record).ExecuteReturnEntity();
            _productNoGeneratorService.UpdateProgress(product.Id, touchNumber, record.TestResultRaw ?? record.TestResult);

            _operationLogService.Write(
                "WeldPointCollection",
                $"Weld point collected, Station={record.StationNo}, WorkOrder={record.SN}, ProductNo={record.ProductNo}, TouchNo={record.TouchNo}, Result={record.TestResult}");
            return record;
        }
    }

    private BizProductProcessConfig ResolveProcessConfig(BizWeldTask task, int stationNo)
    {
        var config = _productProcessConfigService.FindActive(task.ProductNum, task.ProductModel, stationNo);
        if (config is not null)
        {
            return config;
        }

        throw new BusinessOperationException(
            "PLC.WeldPointCollection",
            "产品工艺配置未找到",
            $"未找到产品工号“{task.ProductNum}”、产品型号“{task.ProductModel}”、工位“{stationNo}”对应的产品工艺配置。");
    }

    private async Task<Dictionary<string, string>> ReadCollectionValuesAsync(
        BizProductProcessConfig processConfig,
        int stationNo,
        int touchNumber,
        CancellationToken cancellationToken)
    {
        if (processConfig.TemplateId <= 0)
        {
            throw new BusinessOperationException(
                "PLC.WeldPointCollection",
                "测试项目模板未绑定",
                $"产品工号“{processConfig.ProductNum}”尚未绑定测试项目模板。");
        }

        var items = _testItemTemplateService.GetEnabledItems(processConfig.TemplateId, stationNo, touchNumber);
        if (items.Count == 0)
        {
            throw new BusinessOperationException(
                "PLC.WeldPointCollection",
                "测试项目地址未配置",
                $"模板“{processConfig.TemplateId}”下没有工位“{stationNo}”、焊点“{touchNumber}”可用的测试项目地址。");
        }

        return await ReadTemplateItemValuesAsync(items, cancellationToken);
    }

    private async Task<Dictionary<string, string>> ReadTemplateItemValuesAsync(
        IReadOnlyList<BizTestItemTemplateItem> items,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items.OrderBy(item => item.Sort).ThenBy(item => item.ItemKey))
        {
            var actualValue = await ReadTemplateItemAddressValueAsync(
                item,
                item.ActualAddress,
                item.ValueDataType,
                item.ValueDataLength,
                item.Required,
                "实际值",
                cancellationToken);
            AddValue(values, item.ItemKey, actualValue);
            AddValue(values, item.MesFieldPrefix, actualValue);

            var upperValue = await ReadTemplateItemAddressValueAsync(
                item,
                item.UpperAddress,
                item.ValueDataType,
                item.ValueDataLength,
                false,
                "上限",
                cancellationToken);
            AddValue(values, $"{item.ItemKey}_upper", upperValue);
            AddMesRoleValues(values, item.MesFieldPrefix, "Upper", "upper", upperValue);

            var lowerValue = await ReadTemplateItemAddressValueAsync(
                item,
                item.LowerAddress,
                item.ValueDataType,
                item.ValueDataLength,
                false,
                "下限",
                cancellationToken);
            AddValue(values, $"{item.ItemKey}_lower", lowerValue);
            AddMesRoleValues(values, item.MesFieldPrefix, "Lower", "lower", lowerValue);

            var resultValue = await ReadTemplateItemAddressValueAsync(
                item,
                item.ResultAddress,
                item.ResultDataType,
                item.ResultDataLength,
                false,
                "结果",
                cancellationToken);
            AddValue(values, $"{item.ItemKey}_result", resultValue);
            AddMesRoleValues(values, item.MesFieldPrefix, "Result", "result", resultValue);
            if (IsOverallResultItem(item))
            {
                AddValue(values, "test_result_raw", resultValue);
            }
        }

        return values;
    }

    private async Task<string?> ReadTemplateItemAddressValueAsync(
        BizTestItemTemplateItem item,
        string? addressText,
        string dataType,
        int dataLength,
        bool required,
        string valueRole,
        CancellationToken cancellationToken)
    {
        var address = addressText?.Trim();
        if (string.IsNullOrWhiteSpace(address))
        {
            if (required)
            {
                throw new BusinessOperationException(
                    "PLC.WeldPointCollection",
                    "采集参数地址未配置",
                    $"必采测试项目“{item.ItemName}”的{valueRole}地址尚未配置。");
            }

            return null;
        }

        return dataType switch
        {
            AppConstants.PlcDataTypes.Bool => FormatBoolValue(await _plcCommunicationService.ReadBoolAsync(address, cancellationToken), item, valueRole),
            AppConstants.PlcDataTypes.Int32 => FormatNumericValue(await _plcCommunicationService.ReadInt32Async(address, cancellationToken), item, valueRole),
            AppConstants.PlcDataTypes.Float => FormatNumericValue(await _plcCommunicationService.ReadFloatAsync(address, cancellationToken), item, valueRole),
            AppConstants.PlcDataTypes.String => FormatStringValue(await _plcCommunicationService.ReadStringAsync(address, (ushort)Math.Max(1, dataLength), cancellationToken), item, valueRole),
            _ => FormatNumericValue(await _plcCommunicationService.ReadInt16Async(address, cancellationToken), item, valueRole)
        };
    }

    private static string? FormatBoolValue(PlcServiceResult<bool> result, BizTestItemTemplateItem item, string valueRole)
    {
        EnsureReadSuccess(result.IsSuccess, result.Message, item, valueRole);
        return result.Value ? "1" : "0";
    }

    private static string? FormatStringValue(PlcServiceResult<string> result, BizTestItemTemplateItem item, string valueRole)
    {
        EnsureReadSuccess(result.IsSuccess, result.Message, item, valueRole);
        return result.Value?.Trim().Trim('\0');
    }

    private static string? FormatNumericValue(PlcServiceResult<short> result, BizTestItemTemplateItem item, string valueRole)
    {
        EnsureReadSuccess(result.IsSuccess, result.Message, item, valueRole);
        return FormatScaledValue(result.Value, item);
    }

    private static string? FormatNumericValue(PlcServiceResult<int> result, BizTestItemTemplateItem item, string valueRole)
    {
        EnsureReadSuccess(result.IsSuccess, result.Message, item, valueRole);
        return FormatScaledValue(result.Value, item);
    }

    private static string? FormatNumericValue(PlcServiceResult<float> result, BizTestItemTemplateItem item, string valueRole)
    {
        EnsureReadSuccess(result.IsSuccess, result.Message, item, valueRole);
        return FormatScaledValue(Convert.ToDecimal(result.Value, CultureInfo.InvariantCulture), item);
    }

    private static string FormatScaledValue(decimal? rawValue, BizTestItemTemplateItem item)
    {
        var scaledValue = (rawValue ?? 0m) * item.Scale + item.Offset;
        var roundedValue = Math.Round(scaledValue, Math.Clamp(item.DecimalPlaces, 0, 6), MidpointRounding.AwayFromZero);
        return item.DecimalPlaces <= 0
            ? roundedValue.ToString("0", CultureInfo.InvariantCulture)
            : roundedValue.ToString($"F{item.DecimalPlaces}", CultureInfo.InvariantCulture);
    }

    private static void EnsureReadSuccess(bool isSuccess, string message, BizTestItemTemplateItem item, string valueRole)
    {
        if (isSuccess)
        {
            return;
        }

        throw new BusinessOperationException(
            "PLC.WeldPointCollection",
            "焊点数据采集失败",
            $"测试项目“{item.ItemName}”{valueRole}读取失败：{message}");
    }

    private static void AddValue(IDictionary<string, string> values, string? key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key) || value is null)
        {
            return;
        }

        values[key.Trim()] = value;
    }

    private static void AddMesRoleValues(
        IDictionary<string, string> values,
        string? prefix,
        string pascalRole,
        string snakeRole,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(prefix) || value is null)
        {
            return;
        }

        var normalizedPrefix = prefix.Trim();
        values[$"{normalizedPrefix}{pascalRole}"] = value;
        values[$"{normalizedPrefix}_{snakeRole}"] = value;
    }

    private static bool IsOverallResultItem(BizTestItemTemplateItem item)
    {
        return string.Equals(item.ItemKey, "test_result", StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.ItemKey, "touch_result", StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.ItemName, "当前焊点结果", StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.ItemName, "焊点结果", StringComparison.OrdinalIgnoreCase);
    }

    private BizWeldPointRecord BuildRecord(
        BizWeldTask task,
        int stationNo,
        BizProductInstance product,
        int touchNumber,
        IReadOnlyDictionary<string, string> values)
    {
        var testResultRaw = FindValue(values, "test_result_raw", "test_result", "TestResult", "Result");
        var record = new BizWeldPointRecord
        {
            TaskId = task.Id,
            ExpStartId = task.ExpStartId,
            DeviceId = task.DeviceId,
            SN = task.WorkOrderId,
            ProcessNo = task.ProcessNo,
            ProductNo = product.ProductNo,
            TouchNo = touchNumber.ToString(CultureInfo.InvariantCulture),
            StationNo = stationNo,
            SequenceNo = GetNextSequenceNo(task.Id, stationNo),
            MaxElectric = FindValue(values, "max_electric", "MaxElectric"),
            MaxVoltage = FindValue(values, "max_voltage", "MaxVoltage"),
            ValidPower = FindValue(values, "valid_power", "ValidPower"),
            Displacement = FindValue(values, "displacement", "Displacement"),
            WeldTs = FindValue(values, "weld_ts", "WeldTs"),
            TestResultRaw = testResultRaw,
            TestResult = NormalizeTestResult(testResultRaw),
            OperatorNo = task.StartOperatorNumber,
            RecordTime = DateTime.Now,
            ProductCompleted = touchNumber >= product.RequiredTouchCount,
            UploadStatus = ProductionConstants.UploadStatuses.Pending,
            RawDataJson = JsonSerializer.Serialize(values)
        };

        return record;
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

    private static string? FindValue(IReadOnlyDictionary<string, string> values, params string[] keys)
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
}
