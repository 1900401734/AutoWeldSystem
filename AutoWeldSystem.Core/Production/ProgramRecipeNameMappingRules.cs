using AutoWeldSystem.Core.DTOs.Plc;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序内容配方名称与 PLC 配方号的匹配规则。
/// 下载程序后，按配方名称匹配本机 PLC 槽位表，得到配方号再下发。
/// </summary>
public static class ProgramRecipeNameMappingRules
{
    /// <summary>
    /// 规范化配方名称：截断 PLC 定长字符串的 NUL 填充后再裁剪空白。
    /// 历史程序内容可能已带 NUL，匹配时必须同样处理，否则名称永远对不上槽位。
    /// </summary>
    private static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var terminatorIndex = value.IndexOf('\0');
        var text = terminatorIndex >= 0 ? value[..terminatorIndex] : value;
        return text.Trim();
    }

    /// <summary>
    /// 按配方名称匹配 PLC 槽位，返回对应配方号。
    /// 同名槽位取第一个；匹配时截断 NUL、裁剪空白且忽略大小写。
    /// </summary>
    /// <param name="recipeName">程序内容里的配方名称。</param>
    /// <param name="stationOptions">本机该工位的 PLC 配方名称槽位列表。</param>
    /// <returns>匹配到的配方号；未匹配到返回 null。</returns>
    public static string? ResolveRecipeCode(
        string? recipeName,
        IEnumerable<PlcRecipeNameOption> stationOptions)
    {
        var normalized = Normalize(recipeName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var matched = stationOptions
            .FirstOrDefault(option => string.Equals(
                Normalize(option.Name),
                normalized,
                StringComparison.OrdinalIgnoreCase));

        return matched?.RecipeCode.ToString();
    }

    /// <summary>
    /// 严格按配方名称匹配 PLC 槽位，匹配失败时构造明确错误信息。
    /// </summary>
    /// <param name="recipeName">程序内容里的配方名称。</param>
    /// <param name="stationNo">工位号。</param>
    /// <param name="stationOptions">本机该工位的 PLC 配方名称槽位列表。</param>
    /// <param name="recipeCode">匹配到的配方号。</param>
    /// <param name="errorMessage">匹配失败时的错误信息。</param>
    /// <returns>匹配成功返回 true；失败返回 false 并给出错误。</returns>
    public static bool TryResolveRecipeCode(
        string? recipeName,
        int stationNo,
        IEnumerable<PlcRecipeNameOption> stationOptions,
        out string recipeCode,
        out string errorMessage)
    {
        var normalized = Normalize(recipeName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            recipeCode = string.Empty;
            errorMessage = $"工位 {stationNo} 配方名称为空，无法匹配 PLC 槽位。";
            return false;
        }

        var resolved = ResolveRecipeCode(normalized, stationOptions);
        if (string.IsNullOrWhiteSpace(resolved))
        {
            recipeCode = string.Empty;
            var optionList = stationOptions.ToList();
            var availableNames = optionList
                .Select(option => Normalize(option.Name))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Take(5)
                .ToList();

            errorMessage = availableNames.Count > 0
                ? $"工位 {stationNo} 配方名称「{normalized}」在本机 PLC 配方表中未找到。可用配方：{string.Join("、", availableNames)}{(optionList.Count > 5 ? "……" : string.Empty)}。"
                : $"工位 {stationNo} 配方名称「{normalized}」在本机 PLC 配方表中未找到，且本机尚未配置该工位的 PLC 配方名称地址。";
            return false;
        }

        recipeCode = resolved;
        errorMessage = string.Empty;
        return true;
    }
}
