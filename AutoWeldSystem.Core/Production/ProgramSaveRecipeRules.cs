namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序保存时的配方号完整性规则。
/// UI 和服务均使用正整数文本，避免空值或历史非数字编号进入运行流程。
/// </summary>
public static class ProgramSaveRecipeRules
{
    /// <summary>
    /// 校验工位配方号；双工位设备允许一侧不适用，但至少配置一个工位。
    /// </summary>
    public static void Validate(string? station1RecipeCode, string? station2RecipeCode, bool enableDualStation)
    {
        if (!enableDualStation)
        {
            ValidatePositive(station1RecipeCode, "工位 1 配方号必须是正整数。");
            return;
        }

        var station1Empty = string.IsNullOrWhiteSpace(station1RecipeCode);
        var station2Empty = string.IsNullOrWhiteSpace(station2RecipeCode);
        if (station1Empty && station2Empty)
        {
            throw new InvalidOperationException("至少选择一个适用工位配方。");
        }

        if (!station1Empty)
        {
            ValidatePositive(station1RecipeCode, "工位 1 配方号必须是正整数。");
        }

        if (!station2Empty)
        {
            ValidatePositive(station2RecipeCode, "工位 2 配方号必须是正整数。");
        }
    }

    private static void ValidatePositive(string? value, string message)
    {
        if (!int.TryParse(value?.Trim(), out var recipeCode) || recipeCode <= 0)
        {
            throw new InvalidOperationException(message);
        }
    }
}
