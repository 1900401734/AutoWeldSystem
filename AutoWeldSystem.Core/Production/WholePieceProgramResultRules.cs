using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Plc;
using System.Globalization;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 整件检测程序判定规则。PLC 面结果只负责确认测试完成，最终结果由任务程序快照中的上下限计算。
/// </summary>
public static class WholePieceProgramResultRules
{
    /// <summary>
    /// 判断是否走整件检测程序判定。
    /// 「检测结果来源」配置已移除：视觉只回传检测完成信号，面结果寄存器恒为完成值，
    /// 无论如何都无法由 PLC 给出合格结论，因此整件检测设备固定走程序判定。
    /// </summary>
    public static bool IsApplicable(string? deviceType)
        => string.Equals(
               deviceType?.Trim(),
               ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
               StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 开工与各输出入口共用方案校验，不能先按出口裁剪掉参与判定的测试项。
    /// </summary>
    public static void ValidateScheme(
        IReadOnlyDictionary<string, ProgramLimitRange> limits,
        IEnumerable<(BizSchemeDetail Detail, DimTestItem Item)> schemeItems)
    {
        var items = schemeItems.ToList();
        var participating = items.Where(item => SchemeDetailRoleRules.ShouldEvaluateProgramRole(
            item.Detail, SchemeDetailValueRole.Actual)).ToList();
        if (participating.Count == 0)
        {
            throw new InvalidOperationException("整件检测测试方案没有启用上报实际值的测试项，无法进行程序判定。");
        }

        var maximums = limits.Where(pair => pair.Value.UpperLimit.HasValue).ToDictionary(
            pair => pair.Key, pair => pair.Value.UpperLimit!.Value.ToString(CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase);
        var missing = SchemeDetailRoleRules.FindUploadItemsMissingMaximum(
            items.Select(item => (item.Detail, item.Item.ItemName)), maximums);
        if (missing.Count > 0)
            throw new InvalidOperationException($"测试项“{string.Join("、", missing)}”勾选了上报但未配置设定上限，请先补齐设定上限。");

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var mesFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (detail, item) in items)
        {
            if (SchemeDetailRoleRules.AllRoles.Where(role => role != SchemeDetailValueRole.Actual)
                .Any(role => SchemeDetailRoleRules.IsUploadEnabled(detail, role)))
            {
                throw new InvalidOperationException($"整件检测测试项“{item.ItemName}”只允许上报实际值，不能上报上限、下限或结果角色。");
            }
            if (!SchemeDetailRoleRules.ShouldEvaluateProgramRole(detail, SchemeDetailValueRole.Actual)) continue;
            var name = item.ItemName?.Trim() ?? string.Empty;
            if (name.Length == 0 || !names.Add(name))
            {
                throw new InvalidOperationException($"测试方案存在空名称或重复测试项“{name}”。");
            }
            PlcOffsetExpression expression;
            try
            {
                expression = PlcOffsetExpression.Parse(item.ActualExpression ?? string.Empty);
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                throw new InvalidOperationException($"测试项“{name}”实际值表达式无效：{ex.Message}", ex);
            }
            if (expression.IsAbsoluteAddress)
            {
                throw new InvalidOperationException($"测试项“{name}”用于 A/B 聚合时必须使用按面偏移的相对地址。");
            }
            if (SchemeDetailRoleRules.IsMesEnabled(detail, SchemeDetailValueRole.Actual))
            {
                var field = SchemeDetailRoleRules.GetMesFieldName(detail, SchemeDetailValueRole.Actual)?.Trim();
                if (string.IsNullOrEmpty(field) || !mesFields.Add(field))
                {
                    throw new InvalidOperationException($"测试项“{name}”的过程参数字段名为空或重复。");
                }
            }
        }
    }

    /// <summary>
    /// 按 A/B 合并值判定一组测试项。逐面判定已取消：面结果寄存器恒为检测完成信号，
    /// 不承载合格信息，产品是否合格只由合并值决定。
    /// 聚合值为空表示参与聚合的面全部视觉失败，该项判 NG。
    /// <paramref name="judgementDecimalPlaces"/> 为「判定与上报小数位」：
    /// 判定前先按该位数处理聚合值，保证操作员在合并视图看到的数值与判定口径一致。
    /// </summary>
    private static WholePieceProgramFaceResult EvaluateFace(
        IReadOnlyDictionary<string, ProgramLimitRange> limits,
        IEnumerable<WholePieceProgramMeasurement> measurements,
        int? judgementDecimalPlaces,
        string? numericFormatMode)
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

        var failedItems = new List<string>();
        var evaluatedCount = 0;
        foreach (var measurement in measurementList)
        {
            var itemName = measurement.ItemName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(itemName))
            {
                return WholePieceProgramFaceResult.Failure("测试方案存在名称为空的测试项。");
            }

            // 调用方仅传入参与判定项；缺上限是配置错误，不能跳过后放行。
            if (!limits.TryGetValue(itemName, out var range) || range.UpperLimit is null)
            {
                return WholePieceProgramFaceResult.Failure($"测试项“{itemName}”未配置设定上限。");
            }

            evaluatedCount++;

            // 聚合侧已剔除视觉失败标志：值为空说明参与聚合的面全部失败，该尺寸未测到，必须判 NG。
            if (string.IsNullOrWhiteSpace(measurement.ActualValue))
            {
                failedItems.Add(itemName);
                continue;
            }

            if (!decimal.TryParse(measurement.ActualValue.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                return WholePieceProgramFaceResult.Failure($"测试项“{itemName}”的实测值“{measurement.ActualValue}”不是合法数字。");
            }

            // 先按判定与上报小数位处理，再与上下限比较：合并视图显示的就是判定所用的值。
            var judgedText = PlcStringNumericFormatter.Format(
                measurement.ActualValue,
                judgementDecimalPlaces,
                judgementDecimalPlaces is >= 0,
                numericFormatMode);
            if (!decimal.TryParse(judgedText.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var actual))
            {
                return WholePieceProgramFaceResult.Failure($"测试项“{itemName}”的实测值“{measurement.ActualValue}”不是合法数字。");
            }

            if (!range.Contains(actual))
            {
                failedItems.Add(itemName);
            }
        }

        if (evaluatedCount == 0)
        {
            return WholePieceProgramFaceResult.Failure("参与判定的测试项都没有配置设定上限，无法进行程序判定。");
        }

        return WholePieceProgramFaceResult.Success(
            failedItems.Count == 0 ? ProductionConstants.TestResults.Ok : ProductionConstants.TestResults.Ng,
            failedItems);
    }

    /// <summary>
    /// 用 A/B 聚合后的合并值判定产品结果，与报告文件、过程参数口径一致。
    /// </summary>
    public static WholePieceProgramFaceResult EvaluateAggregated(
        string? programContentSnapshot,
        IReadOnlyList<WholePieceAbOutputRow> abRows,
        IEnumerable<WholePieceAbValueDefinition> definitions,
        int? judgementDecimalPlaces,
        string? numericFormatMode)
    {
        var evaluated = EvaluateAggregatedRows(
            programContentSnapshot,
            abRows,
            definitions,
            judgementDecimalPlaces,
            numericFormatMode);
        return evaluated.IsSuccess
            ? WholePieceProgramFaceResult.Success(evaluated.ProductResult, evaluated.FailedItems)
            : WholePieceProgramFaceResult.Failure(evaluated.ErrorMessage);
    }

    /// <summary>
    /// 按 A/B 合并值逐行判定，同时给出每行结果和产品结果。
    /// A、B 两行分别判定：EvaluateFace 不允许重复测试项，合在一起会因“对称度”重名直接失败。
    /// 行结果必须由这里产出：报表和 MES 的行结果若沿用面记录从严合并，
    /// 会出现“某行 NG 但产品 OK”的矛盾——高度取四面最大值，单面检测失败不影响产品结果。
    /// </summary>
    public static WholePieceProgramAggregatedResult EvaluateAggregatedRows(
        string? programContentSnapshot,
        IReadOnlyList<WholePieceAbOutputRow> abRows,
        IEnumerable<WholePieceAbValueDefinition> definitions,
        int? judgementDecimalPlaces,
        string? numericFormatMode)
    {
        if (!ProgramContentJsonRules.TryReadLimits(programContentSnapshot, out var limits, out var error,
                ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck))
        {
            return WholePieceProgramAggregatedResult.Failure(error);
        }

        return EvaluateAggregatedRows(limits, abRows, definitions, judgementDecimalPlaces, numericFormatMode);
    }

    public static WholePieceProgramAggregatedResult EvaluateAggregatedRows(
        IReadOnlyDictionary<string, ProgramLimitRange> limits,
        IReadOnlyList<WholePieceAbOutputRow> abRows,
        IEnumerable<WholePieceAbValueDefinition> definitions,
        int? judgementDecimalPlaces,
        string? numericFormatMode)
    {
        ArgumentNullException.ThrowIfNull(abRows);
        ArgumentNullException.ThrowIfNull(definitions);
        if (abRows.Count != 2 || !abRows.Select(row => row.SideNo).ToHashSet(StringComparer.OrdinalIgnoreCase)
                .SetEquals(new[] { "A", "B" }))
        {
            return WholePieceProgramAggregatedResult.Failure("整件检测必须同时提供唯一的 A、B 两行聚合结果。");
        }

        var definitionList = definitions.ToList();
        if (definitionList.Count == 0)
        {
            return WholePieceProgramAggregatedResult.Failure("没有参与判定的测试项。");
        }
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
            // 聚合侧留空表示参与聚合的面全部视觉失败，该行对应测试项判 NG。
            // 只有宽度的方案在 B 行没有适用项；该行不影响产品结果。
            var rowResult = measurements.Count == 0
                ? WholePieceProgramFaceResult.Success(ProductionConstants.TestResults.Ok, Array.Empty<string>())
                : EvaluateFace(limits, measurements, judgementDecimalPlaces, numericFormatMode);
            if (!rowResult.IsSuccess)
            {
                return WholePieceProgramAggregatedResult.Failure(rowResult.ErrorMessage);
            }

            results.Add(rowResult.Result);

            // 失败项直接产出界面列名，供合并视图定位到具体列；高度是四面最大值，A/B 两行会各报一次，去重。
            foreach (var failedItem in rowResult.FailedItems)
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
            ? WholePieceProgramAggregatedResult.Failure("A/B合并值判定结果不完整，无法生成产品结果。")
            : WholePieceProgramAggregatedResult.Success(productResult, results, failedItems);
    }

    /// <summary>
    /// 把 A/B 行的结果替换成按该行合并值判定的结果，使报表和 MES 与产品结果同源。
    /// 配置错误必须中止正常输出，不能保留 PLC 面结果伪装成有效判定。
    /// </summary>
    public static IReadOnlyList<WholePieceAbOutputRow> ApplyAggregatedRowResults(
        string? programContentSnapshot,
        IReadOnlyList<WholePieceAbOutputRow> abRows,
        IEnumerable<WholePieceAbValueDefinition> definitions,
        int? judgementDecimalPlaces,
        string? numericFormatMode)
    {
        ArgumentNullException.ThrowIfNull(abRows);

        var evaluated = EvaluateAggregatedRows(
            programContentSnapshot,
            abRows,
            definitions,
            judgementDecimalPlaces,
            numericFormatMode);
        if (!evaluated.IsSuccess)
        {
            throw new InvalidOperationException(evaluated.ErrorMessage);
        }

        // 行级 OK/NG 已取消：A/B 两行统一填产品结果，与合并视图和产品判定同源。
        return abRows
            .Select(row => row with { Result = evaluated.ProductResult })
            .ToList();
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
    /// 解析程序快照中的设定上限，供开工校验复用同一套解析口径。
    /// </summary>
    public static bool TryReadMaximumValues(
        string? programContentSnapshot,
        out Dictionary<string, string> values,
        out string errorMessage)
        => TryParseMaximumValues(programContentSnapshot, out values, out errorMessage);

    private static bool TryParseMaximumValues(
        string? programContentSnapshot,
        out Dictionary<string, string> values,
        out string errorMessage)
    {
        var success = ProgramContentJsonRules.TryReadLimits(programContentSnapshot, out var limits, out errorMessage,
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck);
        values = limits.Where(pair => pair.Value.UpperLimit.HasValue).ToDictionary(
            pair => pair.Key,
            pair => pair.Value.UpperLimit!.Value.ToString(CultureInfo.InvariantCulture),
            StringComparer.OrdinalIgnoreCase);
        return success;
    }

}

public sealed record WholePieceProgramMeasurement(string ItemName, string? ActualValue);

/// <summary>
/// A/B 合并值判定结果。<see cref="RowResults"/> 与传入的行顺序一致，
/// 供报表和 MES 把行结果对齐到产品结果的同一套口径。
/// </summary>
public sealed class WholePieceProgramAggregatedResult
{
    private WholePieceProgramAggregatedResult(
        bool isSuccess,
        string productResult,
        string errorMessage,
        IReadOnlyList<string> rowResults,
        IReadOnlyList<string> failedItems)
    {
        IsSuccess = isSuccess;
        ProductResult = productResult;
        ErrorMessage = errorMessage;
        RowResults = rowResults;
        FailedItems = failedItems;
    }

    public bool IsSuccess { get; }

    public string ProductResult { get; }

    public string ErrorMessage { get; }

    public IReadOnlyList<string> RowResults { get; }

    public IReadOnlyList<string> FailedItems { get; }

    public static WholePieceProgramAggregatedResult Success(
        string productResult,
        IReadOnlyList<string> rowResults,
        IReadOnlyList<string> failedItems)
        => new(true, productResult, string.Empty, rowResults, failedItems);

    public static WholePieceProgramAggregatedResult Failure(string message)
        => new(false, ProductionConstants.TestResults.Unknown, message, Array.Empty<string>(), Array.Empty<string>());
}

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
