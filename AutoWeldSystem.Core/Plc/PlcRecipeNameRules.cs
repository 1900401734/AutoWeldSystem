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
                Name = namesByRecipeCode.TryGetValue(recipeCode, out var name) ? name?.Trim() : null
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
}
