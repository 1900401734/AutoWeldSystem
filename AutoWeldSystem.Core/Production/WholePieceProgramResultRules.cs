using AutoWeldSystem.Core.Constants;
using System.Globalization;
using System.Text.Json;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 整件检测程序判定规则。PLC 面结果只负责确认测试完成，最终结果由任务程序快照中的最大允许值计算。
/// </summary>
public static class WholePieceProgramResultRules
{
    public static bool IsApplicable(string? deviceType, string? resultSource)
        => string.Equals(
               deviceType?.Trim(),
               ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
               StringComparison.OrdinalIgnoreCase)
           && string.Equals(
               ProductionConstants.InspectionResultSources.Normalize(resultSource),
               ProductionConstants.InspectionResultSources.Program,
               StringComparison.OrdinalIgnoreCase);

    public static WholePieceProgramFaceResult EvaluateFace(
        string? programContentSnapshot,
        IEnumerable<WholePieceProgramMeasurement> measurements)
    {
        ArgumentNullException.ThrowIfNull(measurements);

        var measurementList = measurements.ToList();
        if (measurementList.Count == 0)
        {
            return WholePieceProgramFaceResult.Failure("测试方案没有启用实际值采集的测试项，无法进行程序判定。");
        }

        var duplicateItems = measurementList
            .GroupBy(item => item.ItemName?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicateItems.Count > 0)
        {
            return WholePieceProgramFaceResult.Failure($"测试方案存在重复测试项：{string.Join("、", duplicateItems)}。");
        }

        if (!TryParseMaximumValues(programContentSnapshot, out var maximumValues, out var parseError))
        {
            return WholePieceProgramFaceResult.Failure(parseError);
        }

        var failedItems = new List<string>();
        foreach (var measurement in measurementList)
        {
            var itemName = measurement.ItemName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(itemName))
            {
                return WholePieceProgramFaceResult.Failure("测试方案存在名称为空的测试项。");
            }

            if (!maximumValues.TryGetValue(itemName, out var maximumText) || string.IsNullOrWhiteSpace(maximumText))
            {
                return WholePieceProgramFaceResult.Failure($"程序内容缺少测试项“{itemName}”的最大允许值。");
            }

            if (!decimal.TryParse(maximumText.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var maximum))
            {
                return WholePieceProgramFaceResult.Failure($"测试项“{itemName}”的最大允许值“{maximumText}”不是合法数字。");
            }

            if (!decimal.TryParse(measurement.ActualValue?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var actual))
            {
                return WholePieceProgramFaceResult.Failure($"测试项“{itemName}”的实测值“{measurement.ActualValue}”不是合法数字。");
            }

            if (actual > maximum)
            {
                failedItems.Add(itemName);
            }
        }

        return WholePieceProgramFaceResult.Success(
            failedItems.Count == 0 ? ProductionConstants.TestResults.Ok : ProductionConstants.TestResults.Ng,
            failedItems);
    }

    /// <summary>
    /// 用 A/B 聚合后的合并值判定产品结果，与 MES 上传、报表口径一致。
    /// A、B 两行分别判定：EvaluateFace 不允许重复测试项，合在一起会因“对称度”重名直接失败。
    /// </summary>
    public static WholePieceProgramFaceResult EvaluateAggregated(
        string? programContentSnapshot,
        IReadOnlyList<WholePieceAbOutputRow> abRows,
        IEnumerable<WholePieceAbValueDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(abRows);
        ArgumentNullException.ThrowIfNull(definitions);

        var definitionList = definitions.ToList();
        var results = new List<string>();
        var failedItems = new List<string>();
        foreach (var row in abRows)
        {
            // 程序快照以原始测试项名为键，这里不能用“对称度A”这类显示列名。
            // 宽度只有 A 行有值，B 行留空，必须排除，否则会被当成非法数字导致整次判定失败。
            var measurements = definitionList
                .Where(definition => !IsSkippedOnSideB(definition.ItemName, row.SideNo))
                .Select(definition => new WholePieceProgramMeasurement(
                    definition.ItemName,
                    row.Values.TryGetValue(definition.OutputKey, out var value) ? value : null))
                .ToList();
            var rowResult = EvaluateFace(programContentSnapshot, measurements);
            if (!rowResult.IsSuccess)
            {
                return rowResult;
            }

            // 视觉检测失败时约定回传 0。高度、宽度的合并值仍为 0（或负值），
            // 说明参与聚合的面全部没有检测成功，只判“小于上限”会误判成 OK，这里必须判 NG。
            var invalidZeroItems = measurements
                .Where(measurement => WholePieceAbAggregationRules.IsProductLevelItem(measurement.ItemName)
                    && IsNonPositiveValue(measurement.ActualValue))
                .Select(measurement => measurement.ItemName)
                .ToList();
            results.Add(invalidZeroItems.Count > 0
                ? ProductionConstants.TestResults.Ng
                : rowResult.Result);

            // 失败项直接产出界面列名，供合并视图定位到具体列；高度是四面最大值，A/B 两行会各报一次，去重。
            foreach (var failedItem in rowResult.FailedItems.Concat(invalidZeroItems))
            {
                var columnName = WholePieceMergedDisplayRules.BuildColumnName(failedItem, row.SideNo);
                if (!failedItems.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                {
                    failedItems.Add(columnName);
                }
            }
        }

        var productResult = TestResultRules.ResolveProductResult(results);
        return string.Equals(productResult, ProductionConstants.TestResults.Unknown, StringComparison.OrdinalIgnoreCase)
            ? WholePieceProgramFaceResult.Failure("A/B合并值判定结果不完整，无法生成产品结果。")
            : WholePieceProgramFaceResult.Success(productResult, failedItems);
    }

    /// <summary>
    /// 实时预览允许已完成面的 NG 立即决定产品 NG；只有四面全部完成且全 OK 才显示 OK。
    /// </summary>
    public static string ResolveRealtimeProductResult(IEnumerable<string?> faceResults, int expectedFaceCount)
    {
        var results = faceResults.Select(TestResultRules.Normalize).ToList();
        if (results.Any(TestResultRules.IsPreWeldNg))
        {
            return ProductionConstants.TestResults.PreWeldNg;
        }

        if (results.Any(TestResultRules.IsNg))
        {
            return ProductionConstants.TestResults.Ng;
        }

        return results.Count == expectedFaceCount && results.All(TestResultRules.IsOk)
            ? ProductionConstants.TestResults.Ok
            : ProductionConstants.TestResults.Unknown;
    }

    /// <summary>
    /// 宽度只在 A 行有值，B 行留空，不参与合并值判定。
    /// </summary>
    private static bool IsSkippedOnSideB(string? itemName, string? sideNo)
        => WholePieceAbAggregationRules.IsSideAOnlyItem(itemName)
           && !string.Equals(
               sideNo?.Trim(),
               WholePieceMergedDisplayRules.SideASuffix,
               StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 判断实测值是否为 0 或负值。视觉检测失败约定回传 0，负值同样不是有效尺寸。
    /// </summary>
    private static bool IsNonPositiveValue(string? actualValue)
        => decimal.TryParse(
               actualValue?.Trim(),
               NumberStyles.Float,
               CultureInfo.InvariantCulture,
               out var value)
           && value <= 0m;

    private static bool TryParseMaximumValues(
        string? json,
        out Dictionary<string, string> values,
        out string errorMessage)
    {
        values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
        {
            errorMessage = "任务程序快照为空，无法读取最大允许值。";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                errorMessage = "任务程序快照不是有效的测试项对象。";
                return false;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                var itemName = property.Name.Trim();
                if (string.IsNullOrWhiteSpace(itemName))
                {
                    continue;
                }

                if (values.ContainsKey(itemName))
                {
                    errorMessage = $"任务程序快照存在重复测试项：{itemName}。";
                    return false;
                }

                values[itemName] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
            }

            errorMessage = string.Empty;
            return true;
        }
        catch (JsonException ex)
        {
            errorMessage = $"任务程序快照 JSON 无效：{ex.Message}";
            return false;
        }
    }
}

public sealed record WholePieceProgramMeasurement(string ItemName, string? ActualValue);

public sealed class WholePieceProgramFaceResult
{
    private WholePieceProgramFaceResult(bool isSuccess, string result, string errorMessage, IReadOnlyList<string> failedItems)
    {
        IsSuccess = isSuccess;
        Result = result;
        ErrorMessage = errorMessage;
        FailedItems = failedItems;
    }

    public bool IsSuccess { get; }

    public string Result { get; }

    public string ErrorMessage { get; }

    public IReadOnlyList<string> FailedItems { get; }

    public static WholePieceProgramFaceResult Success(string result, IReadOnlyList<string> failedItems)
        => new(true, result, string.Empty, failedItems);

    public static WholePieceProgramFaceResult Failure(string message)
        => new(false, ProductionConstants.TestResults.Unknown, message, Array.Empty<string>());
}
