using System.Globalization;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 集中处理加工程序与 PLC 工位配方号之间的映射。
/// RecipeCode 始终代表单工位或工位 1；工位 2 优先使用 Station2RecipeCode。
/// </summary>
public static class ProgramRecipeMappingRules
{
    private const int Station2 = 2;

    /// <summary>
    /// 将配方号规范化为不带前导零的正整数文本；非法值返回空串。
    /// </summary>
    public static string Normalize(string? recipeCode)
    {
        var normalizedInput = recipeCode?.Trim().Trim('\0');
        return long.TryParse(
                normalizedInput,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var value)
            && value > 0
                ? value.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
    }

    /// <summary>
    /// 根据当前工位解析程序实际使用的 PLC 配方号。
    /// 旧程序没有有效工位 2 配方号时，安全回退到原 RecipeCode。
    /// </summary>
    public static string Resolve(BizProgram? program, int stationNo)
    {
        if (program is null)
        {
            return string.Empty;
        }

        var primaryRecipeCode = Normalize(program.RecipeCode);
        if (stationNo != Station2)
        {
            return primaryRecipeCode;
        }

        var station2RecipeCode = Normalize(program.Station2RecipeCode);
        return string.IsNullOrWhiteSpace(station2RecipeCode)
            ? primaryRecipeCode
            : station2RecipeCode;
    }

    /// <summary>
    /// 判断 PLC 回读配方号是否属于程序在指定工位上的配方槽位。
    /// </summary>
    public static bool Matches(BizProgram? program, int stationNo, string? recipeCode)
    {
        var expected = Resolve(program, stationNo);
        var actual = Normalize(recipeCode);
        return !string.IsNullOrWhiteSpace(expected)
            && string.Equals(expected, actual, StringComparison.Ordinal);
    }

    /// <summary>
    /// 按目标工位分别解析同一程序的配方号，防止共享任务把一个工位的配方复用到另一个工位。
    /// </summary>
    public static IReadOnlyList<ProgramRecipeTarget> ResolveTargets(
        BizProgram? program,
        IEnumerable<int> targetStations)
    {
        ArgumentNullException.ThrowIfNull(targetStations);

        return targetStations
            .Select(stationNo => stationNo == Station2 ? Station2 : 1)
            .Distinct()
            .Select(stationNo => new ProgramRecipeTarget(stationNo, Resolve(program, stationNo)))
            .ToList();
    }
}

/// <summary>
/// 指定工位及其实际需要下发、校验的 PLC 配方号。
/// </summary>
public sealed record ProgramRecipeTarget(int StationNo, string RecipeCode);
