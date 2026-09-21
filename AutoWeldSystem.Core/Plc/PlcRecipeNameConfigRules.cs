using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Plc;

/// <summary>
/// PLC 配方名称配置的规范化与校验规则。
/// </summary>
public static class PlcRecipeNameConfigRules
{
    public const int MaxRecipeCount = 64;

    /// <summary>
    /// 复制、清理并校验配置，避免界面对象在保存过程中被直接修改。
    /// </summary>
    public static IReadOnlyList<BizPlcRecipeNameConfig> NormalizeAndValidate(
        IEnumerable<BizPlcRecipeNameConfig> configs,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(configs);

        var normalized = configs.Select(config => Normalize(config, now)).ToList();
        foreach (var config in normalized)
        {
            Validate(config);
        }

        var duplicateStation = normalized
            .GroupBy(config => config.StationNo)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateStation is not null)
        {
            throw new InvalidOperationException($"工位 {duplicateStation.Key} 的配方名称配置重复。");
        }

        return normalized.OrderBy(config => config.StationNo).ToList();
    }

    private static BizPlcRecipeNameConfig Normalize(BizPlcRecipeNameConfig config, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new BizPlcRecipeNameConfig
        {
            Id = config.Id,
            StationNo = config.StationNo,
            BaseAddress = config.BaseAddress?.Trim() ?? string.Empty,
            RecipeCount = config.RecipeCount,
            AddressOffset = config.AddressOffset,
            StringLength = config.StringLength,
            Enabled = config.Enabled,
            CreatedTime = config.CreatedTime == default ? now : config.CreatedTime,
            UpdatedTime = now
        };
    }

    private static void Validate(BizPlcRecipeNameConfig config)
    {
        if (config.StationNo <= ProductionConstants.Stations.SharedStationNo)
        {
            throw new InvalidOperationException("配方名称配置不支持共享工位。");
        }

        if (string.IsNullOrWhiteSpace(config.BaseAddress))
        {
            throw new InvalidOperationException($"工位 {config.StationNo} 的配方名称基地址不能为空。");
        }

        if (config.RecipeCount <= 0)
        {
            throw new InvalidOperationException("配方数量必须大于 0。");
        }

        if (config.RecipeCount > MaxRecipeCount)
        {
            throw new InvalidOperationException($"配方数量不能超过 {MaxRecipeCount}。");
        }

        if (config.AddressOffset <= 0)
        {
            throw new InvalidOperationException("配方名称地址偏移量必须大于 0。");
        }

        if (config.StringLength <= 0 || config.StringLength > ushort.MaxValue)
        {
            throw new InvalidOperationException($"配方名称字符串长度必须在 1-{ushort.MaxValue} 之间。");
        }

        try
        {
            var lastOffset = checked((config.RecipeCount - 1) * config.AddressOffset);
            PlcOffsetExpression.AddByteOffset(config.BaseAddress, lastOffset);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw new InvalidOperationException(
                $"工位 {config.StationNo} 的配方名称地址配置无效：{ex.Message}",
                ex);
        }
    }
}
