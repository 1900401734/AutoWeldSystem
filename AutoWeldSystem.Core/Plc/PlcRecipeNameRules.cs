using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Plc;

/// <summary>
/// PLC 配方号、地址和显示名称之间的纯映射规则。
/// </summary>
public static class PlcRecipeNameRules
{
    /// <summary>
    /// 根据基地址和固定字节偏移计算指定配方号的地址。
    /// </summary>
    public static string ResolveAddress(BizPlcRecipeNameConfig config, int recipeCode)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (recipeCode <= 0 || recipeCode > config.RecipeCount)
        {
            throw new ArgumentOutOfRangeException(nameof(recipeCode), "配方号必须位于配置的槽位范围内。");
        }

        var byteOffset = checked((recipeCode - 1) * config.AddressOffset);
        return PlcOffsetExpression.AddByteOffset(config.BaseAddress, byteOffset);
    }

    /// <summary>
    /// 将槽位名称转换为下拉选项；空名称跳过但不会改变后续配方号。
    /// </summary>
    public static IReadOnlyList<PlcRecipeNameOption> BuildOptions(
        BizPlcRecipeNameConfig config,
        IReadOnlyDictionary<int, string?> namesByRecipeCode)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(namesByRecipeCode);

        var namedSlots = Enumerable.Range(1, config.RecipeCount)
            .Select(recipeCode => new
            {
                RecipeCode = recipeCode,
                Name = namesByRecipeCode.TryGetValue(recipeCode, out var name) ? NormalizeName(name) : null
            })
            .Where(slot => !string.IsNullOrWhiteSpace(slot.Name))
            .ToList();
        return namedSlots
            .Select(slot => new PlcRecipeNameOption(
                config.StationNo,
                slot.RecipeCode,
                slot.Name!,
                ResolveAddress(config, slot.RecipeCode),
                slot.Name!))
            .ToList();
    }

    /// <summary>
    /// 规范化从 PLC 读回的配方名称。
    /// PLC 定长字符串用 NUL 补齐，而 string.Trim() 不会去掉 NUL（不属于空白字符），
    /// 因此必须先截断到第一个 NUL，否则名称会带着不可见字符进入程序内容并上传 MES；
    /// 全部为 NUL 的空槽位也会被误判成"有名称"混进下拉列表。
    /// </summary>
    private static string? NormalizeName(string? name)
    {
        if (name is null)
        {
            return null;
        }

        var terminatorIndex = name.IndexOf('\0');
        var text = terminatorIndex >= 0 ? name[..terminatorIndex] : name;
        return text.Trim();
    }
}
