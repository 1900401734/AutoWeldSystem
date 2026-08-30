using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Plc;
using System.Globalization;
using System.Text.Json;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 四面整件检测的 A/B 聚合规则。
/// 保留 1～4 面原始记录，只为报表和 MES 生成 A/B 输出行。
/// </summary>
public static class WholePieceAbAggregationRules
{
    private static readonly string[] RequiredSides = ["1", "2", "3", "4"];

    // A 面由面2、面4 组成，B 面由面1、面3 组成，由机台机械结构固定，不做配置。
    private static readonly string[] SideAFaces = ["2", "4"];
    private static readonly string[] SideBFaces = ["1", "3"];

    public static bool IsApplicable(string? deviceType, int touchCount)
        => string.Equals(
               deviceType?.Trim(),
               ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
               StringComparison.OrdinalIgnoreCase)
           && touchCount == 4;

    public static WholePieceAbAggregationResult Aggregate(
        IEnumerable<BizWeldPointRecord> records,
        IEnumerable<WholePieceAbValueDefinition> definitions,
        string? pairedAggregationMode,
        bool enableStringNumericFormatting,
        string? stringNumericFormatMode)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(definitions);

        var recordList = records.ToList();
        if (recordList.Count != 4)
        {
            return WholePieceAbAggregationResult.Failure($"四面检测记录数量必须为4，实际为{recordList.Count}。");
        }

        var sideRecords = new Dictionary<string, BizWeldPointRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in recordList)
        {
            var side = NormalizeSide(record.TouchNo);
            if (side is null)
            {
                return WholePieceAbAggregationResult.Failure($"产品“{record.ProductNo}”面号“{record.TouchNo}”无效，必须为1、2、3或4。");
            }

            if (!sideRecords.TryAdd(side, record))
            {
                return WholePieceAbAggregationResult.Failure($"产品“{record.ProductNo}”存在重复面号“{side}”。");
            }
        }

        var missingSides = RequiredSides.Where(side => !sideRecords.ContainsKey(side)).ToList();
        if (missingSides.Count > 0)
        {
            return WholePieceAbAggregationResult.Failure($"产品“{recordList[0].ProductNo}”缺少面{string.Join("、", missingSides)}。");
        }

        var rows = new List<WholePieceAbOutputRow>
        {
            BuildRow("A", sideRecords["2"], sideRecords["4"]),
            BuildRow("B", sideRecords["1"], sideRecords["3"])
        };
        if (rows.Any(row => string.Equals(
                row.Result,
                ProductionConstants.TestResults.Unknown,
                StringComparison.OrdinalIgnoreCase)))
        {
            return WholePieceAbAggregationResult.Failure("A/B配对面结果缺失或未知，不能生成报表或上传MES。");
        }

        return AggregateCore(
            rows,
            definitions.ToList(),
            (side, definition) => TryReadRawValue(sideRecords[side], definition),
            recordList[0].ProductNo ?? string.Empty,
            pairedAggregationMode,
            requireRelativeAddress: true,
            enableStringNumericFormatting,
            stringNumericFormatMode);
    }

    /// <summary>
    /// 实时预览用的 A/B 聚合。输入按面号给出测试项实测值，避免预览路径伪造采集记录。
    /// 预览按面偏移读取，不做绝对地址校验。
    /// </summary>
    public static WholePieceAbAggregationResult AggregatePreview(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sideItemValues,
        IReadOnlyDictionary<string, string> sideResults,
        IEnumerable<WholePieceAbValueDefinition> definitions,
        string? pairedAggregationMode,
        bool enableStringNumericFormatting,
        string? stringNumericFormatMode)
    {
        ArgumentNullException.ThrowIfNull(sideItemValues);
        ArgumentNullException.ThrowIfNull(sideResults);
        ArgumentNullException.ThrowIfNull(definitions);

        var missingSides = RequiredSides.Where(side => !sideItemValues.ContainsKey(side)).ToList();
        if (missingSides.Count > 0)
        {
            return WholePieceAbAggregationResult.Failure($"缺少面{string.Join("、", missingSides)}的实测值。");
        }

        var rows = new List<WholePieceAbOutputRow>
        {
            BuildPreviewRow("A", sideResults, "2", "4"),
            BuildPreviewRow("B", sideResults, "1", "3")
        };

        return AggregateCore(
            rows,
            definitions.ToList(),
            (side, definition) => TryReadPreviewValue(sideItemValues[side], side, definition),
            string.Empty,
            pairedAggregationMode,
            requireRelativeAddress: false,
            enableStringNumericFormatting,
            stringNumericFormatMode);
    }

    /// <summary>
    /// A/B 聚合的公共数值计算。取值方式由调用方注入，采集记录与实时预览共用同一套规则。
    /// </summary>
    private static WholePieceAbAggregationResult AggregateCore(
        IReadOnlyList<WholePieceAbOutputRow> rows,
        IReadOnlyList<WholePieceAbValueDefinition> definitionList,
        Func<string, WholePieceAbValueDefinition, RawNumericValue> readValue,
        string productNo,
        string? pairedAggregationMode,
        bool requireRelativeAddress,
        bool enableStringNumericFormatting,
        string? stringNumericFormatMode)
    {
        var duplicateKeys = definitionList
            .GroupBy(definition => definition.OutputKey, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicateKeys.Count > 0)
        {
            return WholePieceAbAggregationResult.Failure($"A/B聚合字段重复：{string.Join("、", duplicateKeys)}。");
        }

        var pairedMode = ProductionConstants.PairedAggregationModes.Normalize(pairedAggregationMode);
        var productPrefix = string.IsNullOrWhiteSpace(productNo) ? string.Empty : $"产品“{productNo}”";
        foreach (var definition in definitionList)
        {
            if (!TryParseExpression(definition.ActualExpression, out var expression, out var expressionError))
            {
                return WholePieceAbAggregationResult.Failure($"测试项“{definition.ItemName}”实际值表达式无效：{expressionError}");
            }

            if (requireRelativeAddress && expression.IsAbsoluteAddress)
            {
                return WholePieceAbAggregationResult.Failure($"测试项“{definition.ItemName}”用于A/B聚合时必须使用按面偏移的相对地址，不能使用绝对地址。");
            }

            if (IsFourSideMaximumItem(definition.ItemName))
            {
                var values = RequiredSides
                    .Select(side => readValue(side, definition))
                    .ToList();
                if (values.Any(value => !value.IsSuccess))
                {
                    var failure = values.First(value => !value.IsSuccess);
                    return WholePieceAbAggregationResult.Failure(
                        $"{productPrefix}测试项“{definition.ItemName}”数据无效：{failure.ErrorMessage}");
                }

                var formattedMaximum = FormatAggregatedValue(
                    values.Max(value => value.Value),
                    expression.DecimalPlaces,
                    enableStringNumericFormatting,
                    stringNumericFormatMode);
                foreach (var row in rows)
                {
                    row.Values[definition.OutputKey] = formattedMaximum;
                }

                continue;
            }

            if (IsSideAOnlyItem(definition.ItemName))
            {
                // 现场产品 A/B 两面宽度不同，且过程参数与报表只要求 A 面宽度：
                // A 行取面2、面4 的最大值，B 行留空，不参与判定、上传和报表。
                var values = SideAFaces
                    .Select(side => readValue(side, definition))
                    .ToList();
                if (values.Any(value => !value.IsSuccess))
                {
                    var failure = values.First(value => !value.IsSuccess);
                    return WholePieceAbAggregationResult.Failure(
                        $"{productPrefix}A面测试项“{definition.ItemName}”数据无效：{failure.ErrorMessage}");
                }

                var formattedMaximum = FormatAggregatedValue(
                    values.Max(value => value.Value),
                    expression.DecimalPlaces,
                    enableStringNumericFormatting,
                    stringNumericFormatMode);
                foreach (var row in rows)
                {
                    row.Values[definition.OutputKey] = IsSideA(row.SideNo) ? formattedMaximum : string.Empty;
                }

                continue;
            }

            foreach (var row in rows)
            {
                var sideNumbers = IsSideA(row.SideNo) ? SideAFaces : SideBFaces;
                var values = sideNumbers
                    .Select(side => readValue(side, definition))
                    .ToList();
                if (values.Any(value => !value.IsSuccess))
                {
                    var failure = values.First(value => !value.IsSuccess);
                    return WholePieceAbAggregationResult.Failure(
                        $"{productPrefix}{row.SideNo}面测试项“{definition.ItemName}”数据无效：{failure.ErrorMessage}");
                }

                // 高度取四面最大值、宽度只取 A 面，其余测试项的配对聚合方式由系统设置决定。
                var aggregated = pairedMode == ProductionConstants.PairedAggregationModes.Maximum
                    ? Math.Max(values[0].Value, values[1].Value)
                    : (values[0].Value + values[1].Value) / 2m;
                row.Values[definition.OutputKey] = FormatAggregatedValue(
                    aggregated,
                    expression.DecimalPlaces,
                    enableStringNumericFormatting,
                    stringNumericFormatMode);
            }
        }

        return WholePieceAbAggregationResult.Success(rows);
    }

    public static WholePieceAbAggregationResult ValidateSourceRecords(
        IEnumerable<BizWeldPointRecord> records,
        IEnumerable<WholePieceAbValueDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(definitions);

        var recordList = records.ToList();
        if (recordList.Count != 4)
        {
            return WholePieceAbAggregationResult.Failure($"四面检测记录数量必须为4，实际为{recordList.Count}。");
        }

        var sideRecords = new Dictionary<string, BizWeldPointRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in recordList)
        {
            var side = NormalizeSide(record.TouchNo);
            if (side is null)
            {
                return WholePieceAbAggregationResult.Failure($"产品“{record.ProductNo}”面号“{record.TouchNo}”无效，必须为1、2、3或4。");
            }

            if (!sideRecords.TryAdd(side, record))
            {
                return WholePieceAbAggregationResult.Failure($"产品“{record.ProductNo}”存在重复面号“{side}”。");
            }
        }

        var missingSides = RequiredSides.Where(side => !sideRecords.ContainsKey(side)).ToList();
        if (missingSides.Count > 0)
        {
            return WholePieceAbAggregationResult.Failure($"产品“{recordList[0].ProductNo}”缺少面{string.Join("、", missingSides)}。");
        }

        var pairedResults = new[]
        {
            TestResultRules.ResolveProductResult([sideRecords["2"].TestResult, sideRecords["4"].TestResult]),
            TestResultRules.ResolveProductResult([sideRecords["1"].TestResult, sideRecords["3"].TestResult])
        };
        if (pairedResults.Any(result => string.Equals(
                result,
                ProductionConstants.TestResults.Unknown,
                StringComparison.OrdinalIgnoreCase)))
        {
            return WholePieceAbAggregationResult.Failure("A/B配对面结果缺失或未知，不能生成报表或上传MES。");
        }

        foreach (var definition in definitions)
        {
            if (!TryParseExpression(definition.ActualExpression, out var expression, out var expressionError))
            {
                return WholePieceAbAggregationResult.Failure($"测试项“{definition.ItemName}”实际值表达式无效：{expressionError}");
            }

            if (expression.IsAbsoluteAddress)
            {
                return WholePieceAbAggregationResult.Failure($"测试项“{definition.ItemName}”用于A/B聚合时必须使用相对地址，不能使用绝对地址。");
            }

            foreach (var side in RequiredSides)
            {
                var rawValues = ParseRawData(sideRecords[side].RawDataJson);
                var itemKey = ResolveItemKey(definition.ItemId, definition.ItemName);
                var value = FirstRawValue(rawValues, itemKey, definition.ItemName);
                if (string.IsNullOrWhiteSpace(value))
                {
                    return WholePieceAbAggregationResult.Failure($"产品“{recordList[0].ProductNo}”面{side}缺少测试项“{definition.ItemName}”实际值。");
                }

                if (!decimal.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    return WholePieceAbAggregationResult.Failure($"产品“{recordList[0].ProductNo}”面{side}测试项“{definition.ItemName}”值“{value}”不是合法数字。");
                }
            }
        }

        return WholePieceAbAggregationResult.Success(Array.Empty<WholePieceAbOutputRow>());
    }
    private static WholePieceAbOutputRow BuildRow(
        string sideNo,
        BizWeldPointRecord first,
        BizWeldPointRecord second)
    {
        return new WholePieceAbOutputRow(
            sideNo,
            TestResultRules.ResolveProductResult([first.TestResult, second.TestResult]),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    private static WholePieceAbOutputRow BuildPreviewRow(
        string sideNo,
        IReadOnlyDictionary<string, string> sideResults,
        string firstSide,
        string secondSide)
    {
        sideResults.TryGetValue(firstSide, out var firstResult);
        sideResults.TryGetValue(secondSide, out var secondResult);
        return new WholePieceAbOutputRow(
            sideNo,
            TestResultRules.ResolveProductResult([firstResult ?? string.Empty, secondResult ?? string.Empty]),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    private static string? NormalizeSide(string? side)
    {
        var normalized = side?.Trim();
        return normalized is "1" or "2" or "3" or "4" ? normalized : null;
    }

    private static bool TryParseExpression(
        string expressionText,
        out PlcOffsetExpression expression,
        out string errorMessage)
    {
        try
        {
            expression = PlcOffsetExpression.Parse(expressionText);
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            expression = default!;
            errorMessage = ex.Message;
            return false;
        }
    }

    private static RawNumericValue TryReadRawValue(
        BizWeldPointRecord record,
        WholePieceAbValueDefinition definition)
    {
        var rawValues = ParseRawData(record.RawDataJson);
        var itemKey = ResolveItemKey(definition.ItemId, definition.ItemName);
        var value = FirstRawValue(rawValues, itemKey, definition.ItemName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return RawNumericValue.Failure($"面{record.TouchNo}缺少实际值。");
        }

        if (!decimal.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var numericValue))
        {
            return RawNumericValue.Failure($"面{record.TouchNo}的值“{value}”不是合法数字。");
        }

        return RawNumericValue.Success(numericValue);
    }

    private static RawNumericValue TryReadPreviewValue(
        IReadOnlyDictionary<string, string> itemValues,
        string side,
        WholePieceAbValueDefinition definition)
    {
        // 实时预览行以测试项名称为键，历史记录的 RawDataJson 以 item_{id} 等 raw key 为键，两者都要支持。
        var value = FirstRawValue(
            itemValues,
            ResolveItemKey(definition.ItemId, definition.ItemName),
            definition.ItemName.Trim());
        if (string.IsNullOrWhiteSpace(value))
        {
            return RawNumericValue.Failure($"面{side}缺少实际值。");
        }

        if (!decimal.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var numericValue))
        {
            return RawNumericValue.Failure($"面{side}的值“{value}”不是合法数字。");
        }

        return RawNumericValue.Success(numericValue);
    }

    private static string FormatAggregatedValue(
        decimal aggregatedValue,
        int? decimalPlaces,
        bool enableStringNumericFormatting,
        string? stringNumericFormatMode)
    {
        var rawText = aggregatedValue.ToString(CultureInfo.InvariantCulture);
        return PlcStringNumericFormatter.Format(
            rawText,
            decimalPlaces,
            enableStringNumericFormatting,
            stringNumericFormatMode);
    }

    /// <summary>
    /// 产品级测试项：在监控合并视图中只占一列，且实测值 0 表示视觉检测失败，不能被当作合格值。
    /// </summary>
    public static bool IsProductLevelItem(string? itemName)
        => IsFourSideMaximumItem(itemName) || IsSideAOnlyItem(itemName);

    /// <summary>
    /// 高度四面理论上一致，取四面最大值，A/B 两行同值，报表可按产品跨行合并单元格。
    /// </summary>
    public static bool IsFourSideMaximumItem(string? itemName)
        => string.Equals(itemName?.Trim(), "高度", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 宽度在 A、B 两面不同，只取 A 面（面2、面4）最大值；B 行留空，因此报表不能跨行合并。
    /// </summary>
    public static bool IsSideAOnlyItem(string? itemName)
        => string.Equals(itemName?.Trim(), "宽度", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 判断面号是否属于 A 面。A 面由面2、面4 组成，由机台机械结构固定。
    /// </summary>
    public static bool IsSideAFace(string? touchNo)
        => SideAFaces.Contains(touchNo?.Trim(), StringComparer.OrdinalIgnoreCase);

    private static bool IsSideA(string? sideNo)
        => string.Equals(sideNo?.Trim(), "A", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, string> ParseRawData(string? rawDataJson)
    {
        if (string.IsNullOrWhiteSpace(rawDataJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(rawDataJson);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.EnumerateObject().ToDictionary(
                    property => property.Name,
                    property => property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString() ?? string.Empty
                        : property.Value.ToString(),
                    StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string? FirstRawValue(
        IReadOnlyDictionary<string, string> values,
        params string[] keys)
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

    private static string ResolveItemKey(int itemId, string itemName)
    {
        return itemName.Trim() switch
        {
            "峰值电流" => "max_electric",
            "峰值电压" => "max_voltage",
            "有效功率" => "valid_power",
            "位移" => "displacement",
            "焊接时间" => "weld_ts",
            var name when !string.IsNullOrWhiteSpace(name) => $"item_{itemId}",
            _ => $"item_{itemId}"
        };
    }

    private readonly record struct RawNumericValue(bool IsSuccess, decimal Value, string ErrorMessage)
    {
        public static RawNumericValue Success(decimal value) => new(true, value, string.Empty);

        public static RawNumericValue Failure(string message) => new(false, 0m, message);
    }
}

public sealed record WholePieceAbValueDefinition(
    int ItemId,
    string ItemName,
    string OutputKey,
    string ActualExpression);

public sealed record WholePieceAbOutputRow(
    string SideNo,
    string Result,
    Dictionary<string, string> Values);

public sealed class WholePieceAbAggregationResult
{
    private WholePieceAbAggregationResult(
        bool isSuccess,
        string errorMessage,
        IReadOnlyList<WholePieceAbOutputRow> rows)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Rows = rows;
    }

    public bool IsSuccess { get; }

    public string ErrorMessage { get; }

    public IReadOnlyList<WholePieceAbOutputRow> Rows { get; }

    public static WholePieceAbAggregationResult Success(IReadOnlyList<WholePieceAbOutputRow> rows)
        => new(true, string.Empty, rows);

    public static WholePieceAbAggregationResult Failure(string message)
        => new(false, message, Array.Empty<WholePieceAbOutputRow>());
}