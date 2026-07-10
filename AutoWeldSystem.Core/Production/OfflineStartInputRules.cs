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
    public static IReadOnlyList<OfflineProgramNameOption> BuildProgramNameOptions(IEnumerable<BizProgram> programs)
    {
        ArgumentNullException.ThrowIfNull(programs);

        var validPrograms = programs
            .Where(program => !program.IsDeleted)
            .Where(program => !string.IsNullOrWhiteSpace(program.ProgramName))
            .Where(program => !string.IsNullOrWhiteSpace(program.ProductNum))
            .Where(program => !string.IsNullOrWhiteSpace(program.RecipeCode))
            .Where(program => !string.IsNullOrWhiteSpace(program.ProgramContent))
            .OrderBy(program => program.ProgramName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(program => program.ProductNum, StringComparer.OrdinalIgnoreCase)
            .ThenBy(program => program.RecipeCode, StringComparer.OrdinalIgnoreCase)
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
                ResolveDisplayText(program, duplicateProgramNames.Contains(Normalize(program.ProgramName)))))
            .ToList();
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
            RecipeCode = NormalizeRequired(program.RecipeCode, "配方号不能为空。")
        };
    }

    private static string ResolveDisplayText(BizProgram program, bool includeIdentity)
    {
        var programName = Normalize(program.ProgramName);
        if (!includeIdentity)
        {
            return programName;
        }

        // 程序名称重名时追加关键身份字段，避免操作员选错配方。
        return $"{programName} | 产品工号={Normalize(program.ProductNum)} | 配方号={Normalize(program.RecipeCode)}";
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
