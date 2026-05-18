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
    private readonly ICollectionParameterService _collectionParameterService;
    private readonly IProductNoGeneratorService _productNoGeneratorService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IOperationLogService _operationLogService;
    private readonly object _dbLock = new();

    public WeldPointCollectionService(
        SqlSugarDbContext dbContext,
        IProductProcessConfigService productProcessConfigService,
        ICollectionParameterService collectionParameterService,
        IProductNoGeneratorService productNoGeneratorService,
        IPlcCommunicationService plcCommunicationService,
        IOperationLogService operationLogService)
    {
        _dbContext = dbContext;
        _productProcessConfigService = productProcessConfigService;
        _collectionParameterService = collectionParameterService;
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
        var parameters = _collectionParameterService.GetEnabledParameters(processConfig.CollectionGroup, normalizedStationNo);
        var values = await ReadCollectionValuesAsync(parameters, cancellationToken);

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var product = _productNoGeneratorService.GetOrCreateStationProduct(
                task,
                normalizedStationNo,
                processConfig.WeldPointCount);
            var touchNumber = product.CollectedTouchCount + 1;

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

        // 没有配置时先使用最保守的默认值，避免采集流程因早期未配置而完全不可用。
        _operationLogService.Write(
            "WeldPointCollection",
            $"Product process config not found, fallback to default. Station={stationNo}, ProductNum={task.ProductNum}, ProductModel={task.ProductModel}");

        return new BizProductProcessConfig
        {
            ProductNum = task.ProductNum,
            ProductModel = task.ProductModel,
            StationNo = ProductionConstants.Stations.SharedStationNo,
            ProcessNo = "*",
            ProcessName = null,
            WeldPointCount = 1,
            CollectionGroup = "default",
            ProductNoSource = ProductionConstants.ProductNoSources.AutoIncrement,
            Enabled = true
        };
    }

    private async Task<Dictionary<string, string>> ReadCollectionValuesAsync(
        IReadOnlyList<BizCollectionParameter> parameters,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters.OrderBy(parameter => parameter.Sort).ThenBy(parameter => parameter.ParameterKey))
        {
            var value = await ReadParameterValueAsync(parameter, cancellationToken);
            if (value is null)
            {
                continue;
            }

            values[parameter.ParameterKey] = value;
            if (!string.IsNullOrWhiteSpace(parameter.MesFieldName))
            {
                values[parameter.MesFieldName.Trim()] = value;
            }
        }

        return values;
    }

    private async Task<string?> ReadParameterValueAsync(BizCollectionParameter parameter, CancellationToken cancellationToken)
    {
        var address = parameter.Address?.Trim();
        if (string.IsNullOrWhiteSpace(address))
        {
            if (parameter.Required)
            {
                throw new BusinessOperationException(
                    "PLC.WeldPointCollection",
                    "采集参数地址未配置",
                    $"必填采集参数“{parameter.ParameterName}”尚未配置 PLC 地址。");
            }

            return null;
        }

        return parameter.DataType switch
        {
            AppConstants.PlcDataTypes.Bool => FormatBoolValue(await _plcCommunicationService.ReadBoolAsync(address, cancellationToken), parameter),
            AppConstants.PlcDataTypes.Int32 => FormatNumericValue(await _plcCommunicationService.ReadInt32Async(address, cancellationToken), parameter),
            AppConstants.PlcDataTypes.Float => FormatNumericValue(await _plcCommunicationService.ReadFloatAsync(address, cancellationToken), parameter),
            AppConstants.PlcDataTypes.String => FormatStringValue(await _plcCommunicationService.ReadStringAsync(address, (ushort)Math.Max(1, parameter.DataLength), cancellationToken), parameter),
            _ => FormatNumericValue(await _plcCommunicationService.ReadInt16Async(address, cancellationToken), parameter)
        };
    }

    private static string? FormatBoolValue(PlcServiceResult<bool> result, BizCollectionParameter parameter)
    {
        EnsureReadSuccess(result.IsSuccess, result.Message, parameter);
        return result.Value ? "1" : "0";
    }

    private static string? FormatStringValue(PlcServiceResult<string> result, BizCollectionParameter parameter)
    {
        EnsureReadSuccess(result.IsSuccess, result.Message, parameter);
        return result.Value?.Trim().Trim('\0');
    }

    private static string? FormatNumericValue(PlcServiceResult<short> result, BizCollectionParameter parameter)
    {
        EnsureReadSuccess(result.IsSuccess, result.Message, parameter);
        return FormatScaledValue(result.Value, parameter);
    }

    private static string? FormatNumericValue(PlcServiceResult<int> result, BizCollectionParameter parameter)
    {
        EnsureReadSuccess(result.IsSuccess, result.Message, parameter);
        return FormatScaledValue(result.Value, parameter);
    }

    private static string? FormatNumericValue(PlcServiceResult<float> result, BizCollectionParameter parameter)
    {
        EnsureReadSuccess(result.IsSuccess, result.Message, parameter);
        return FormatScaledValue(Convert.ToDecimal(result.Value, CultureInfo.InvariantCulture), parameter);
    }

    private static string FormatScaledValue(decimal? rawValue, BizCollectionParameter parameter)
    {
        var scaledValue = (rawValue ?? 0m) * parameter.Scale + parameter.Offset;
        var roundedValue = Math.Round(scaledValue, Math.Clamp(parameter.DecimalPlaces, 0, 6), MidpointRounding.AwayFromZero);
        return parameter.DecimalPlaces <= 0
            ? roundedValue.ToString("0", CultureInfo.InvariantCulture)
            : roundedValue.ToString($"F{parameter.DecimalPlaces}", CultureInfo.InvariantCulture);
    }

    private static void EnsureReadSuccess(bool isSuccess, string message, BizCollectionParameter parameter)
    {
        if (isSuccess)
        {
            return;
        }

        throw new BusinessOperationException(
            "PLC.WeldPointCollection",
            "焊点数据采集失败",
            $"采集参数“{parameter.ParameterName}”读取失败：{message}");
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
