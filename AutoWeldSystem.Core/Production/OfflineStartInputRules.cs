using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.Entities;
using System.Globalization;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Builds offline start inputs from the inline MonitorView editor.
/// Keeping these rules outside the form makes validation and request mapping reusable and testable.
/// </summary>
public static class OfflineStartInputRules
{
    /// <summary>
    /// Creates selectable local-program-name options for offline start.
    /// </summary>
    /// <param name="programs">Local program records maintained in the program library.</param>
    /// <returns>Options sorted by program name, product number, recipe code and local program id.</returns>
    public static IReadOnlyList<OfflineProgramNameOption> BuildProgramNameOptions(
        IEnumerable<BizProgram> programs,
        int stationNo,
        bool requireBothStations)
        => BuildProgramNameOptions(programs, stationNo, requireBothStations, productNumFilter: null);

    /// <summary>
    /// Creates selectable local-program-name options limited to one product number.
    /// </summary>
    /// <param name="programs">Local program records maintained in the program library.</param>
    /// <param name="productNumFilter">Product number to keep; null or blank keeps every product.</param>
    /// <returns>Options sorted by program name, product number, recipe code and local program id.</returns>
    public static IReadOnlyList<OfflineProgramNameOption> BuildProgramNameOptions(
        IEnumerable<BizProgram> programs,
        int stationNo,
        bool requireBothStations,
        string? productNumFilter)
    {
        ArgumentNullException.ThrowIfNull(programs);

        var normalizedFilter = Normalize(productNumFilter);
        var hasFilter = !string.IsNullOrWhiteSpace(normalizedFilter);
        var validPrograms = FilterValidPrograms(programs, stationNo, requireBothStations)
            .Where(program => !hasFilter
                || string.Equals(Normalize(program.ProductNum), normalizedFilter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(program => program.ProgramName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(program => program.ProductNum, StringComparer.OrdinalIgnoreCase)
            .ThenBy(program => program.Id)
            .ToList();

        var duplicateProgramNames = validPrograms
            .GroupBy(program => Normalize(program.ProgramName), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return validPrograms
            .Select(program => new OfflineProgramNameOption(
                program,
                ResolveDisplayText(
                    program,
                    duplicateProgramNames.Contains(Normalize(program.ProgramName)),
                    hasFilter)))
            .ToList();
    }

    /// <summary>
    /// Creates selectable product-number options for offline start.
    /// Only product numbers backed by a startable local program are listed, so every option can start a job.
    /// </summary>
    /// <param name="programs">Local program records maintained in the program library.</param>
    /// <returns>Distinct product numbers sorted case-insensitively.</returns>
    public static IReadOnlyList<OfflineProductNumOption> BuildProductNumOptions(
        IEnumerable<BizProgram> programs,
        int stationNo,
        bool requireBothStations)
    {
        ArgumentNullException.ThrowIfNull(programs);

        return FilterValidPrograms(programs, stationNo, requireBothStations)
            .GroupBy(program => Normalize(program.ProductNum), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                // 同一工号大小写不一致时取序数最小的写法，保证显示文本稳定可按 Ordinal 回查。
                var canonical = group
                    .Select(program => Normalize(program.ProductNum))
                    .OrderBy(productNum => productNum, StringComparer.Ordinal)
                    .First();
                return new OfflineProductNumOption(canonical, canonical, group.Count());
            })
            .ToList();
    }

    /// <summary>
    /// Keeps only programs that can actually start a job on the given station.
    /// </summary>
    private static IEnumerable<BizProgram> FilterValidPrograms(
        IEnumerable<BizProgram> programs,
        int stationNo,
        bool requireBothStations)
    {
        return programs
            .Where(program => !program.IsDeleted)
            .Where(program => !string.IsNullOrWhiteSpace(program.ProgramName))
            .Where(program => !string.IsNullOrWhiteSpace(program.ProductNum))
            .Where(program => !string.IsNullOrWhiteSpace(ProgramRecipeMappingRules.Resolve(program, stationNo)))
            .Where(program => !requireBothStations
                || (!string.IsNullOrWhiteSpace(ProgramRecipeMappingRules.Resolve(program, 1))
                    && !string.IsNullOrWhiteSpace(ProgramRecipeMappingRules.Resolve(program, 2))));
    }

    /// <summary>
    /// Builds the existing offline MES start request from inline editor values and the selected program.
    /// </summary>
    /// <param name="input">Values entered on MonitorView.</param>
    /// <param name="option">Selected program-name option.</param>
    /// <returns>Offline start request accepted by <c>IWeldTaskService.StartLocalAsync</c>.</returns>
    public static OfflineExperimentStartReq BuildRequest(OfflineStartInput input, OfflineProgramNameOption option)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(option);

        var program = option.Program;
        return new OfflineExperimentStartReq
        {
            StationNo = NormalizeStationNo(input.StationNo),
            WorkOrderId = NormalizeRequired(input.WorkOrderId, "工单号不能为空。"),
            Batch = Normalize(input.Batch),
            Spec = Normalize(input.Spec),
            ProcessNo = NormalizeRequired(input.ProcessNo, "工序号不能为空。"),
            ProcessName = FirstNonEmpty(input.ProcessName, "离线焊接"),
            PlannedQty = ResolvePlannedQty(input.PlannedQtyText),
            ProgramLocalId = program.Id,
            ProgramId = FirstNonEmpty(program.ProgramId, $"local-{program.Id}"),
            ProgramName = NormalizeRequired(program.ProgramName, "程序名称不能为空。"),
            ProgramType = FirstNonEmpty(program.ProgramType, "0"),
            ProgramContent = FirstNonEmpty(program.ProgramContent, "{}"),
            ProductNum = NormalizeRequired(program.ProductNum, "产品工号不能为空。"),
            ProductModel = Normalize(program.ProductModel),
            ProductName = Normalize(input.ProductName),
            DrawingNo = Normalize(input.DrawingNo),
            RecipeCode = NormalizeRequired(
                ProgramRecipeMappingRules.Resolve(program, input.StationNo),
                "配方号不能为空。")
        };
    }

private static string ResolveDisplayText(BizProgram program, bool includeIdentity, bool filteredByProductNum)
    {
        var programName = Normalize(program.ProgramName);
        if (!includeIdentity)
        {
            return programName;
        }

        // 已按工号筛选时再追加工号是冗余的，改用流水号区分同工号下的重名程序。
        return filteredByProductNum
            ? $"{programName} | 流水号={Math.Max(1, program.SequenceNumber):000}"
            : $"{programName} | 产品工号={Normalize(program.ProductNum)}";
    }

    private static int ResolvePlannedQty(string? value)
    {
        if (int.TryParse(Normalize(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return Math.Max(1, result);
        }

        return 1;
    }

    private static int NormalizeStationNo(int stationNo)
        => stationNo == 2 ? 2 : ProductionConstants.Stations.DefaultStationNo;

    private static string FirstNonEmpty(params string?[] values)
        => Normalize(values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)));

    private static string NormalizeRequired(string? value, string message)
    {
        var normalized = Normalize(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(message);
        }

        return normalized;
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}

/// <summary>
/// Local program option shown by the offline program-name selector.
/// </summary>
/// <param name="Program">The full local program record bound to the selected row.</param>
/// <param name="DisplayText">The visible dropdown text. Program name is primary; duplicated names include identity hints.</param>
public sealed record OfflineProgramNameOption(BizProgram Program, string DisplayText);

/// <summary>
/// Product-number option shown by the offline product-number selector.
/// </summary>
/// <param name="ProductNum">The trimmed product number used to filter program options.</param>
/// <param name="DisplayText">The visible dropdown text; identical to the product number so text lookups round-trip.</param>
/// <param name="ProgramCount">How many startable local programs share this product number.</param>
public sealed record OfflineProductNumOption(string ProductNum, string DisplayText, int ProgramCount);

/// <summary>
/// Inline offline-start values entered on MonitorView.
/// </summary>
public sealed record OfflineStartInput(
    int StationNo,
    string WorkOrderId,
    string Batch,
    string Spec,
    string ProcessNo,
    string ProcessName,
    string PlannedQtyText,
    string ProductName,
    string DrawingNo);
