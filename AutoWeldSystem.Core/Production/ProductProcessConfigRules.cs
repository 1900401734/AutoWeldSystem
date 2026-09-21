using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 产品专用配置优先，缺失时才使用同工位的显式默认工艺；默认包含地址、方案和表头。
/// </summary>
public static class ProductProcessConfigRules
{
    public static bool IsDefaultStation(int stationNo) => stationNo is 1 or 2;

    public static BizProductProcessConfig? SelectActive(
        IEnumerable<BizProductProcessConfig> configs, string? productNum, int stationNo)
    {
        ArgumentNullException.ThrowIfNull(configs);
        var product = productNum?.Trim();
        if (string.IsNullOrWhiteSpace(product)) return null;
        var station = stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo : stationNo;
        var active = configs.Where(config => config.Enabled).ToList();
        ValidateDefaults(active);
        var dedicated = active
            .Where(config => string.Equals(config.ProductNum?.Trim(), product, StringComparison.OrdinalIgnoreCase)
                && (config.StationNo == station || config.StationNo == ProductionConstants.Stations.SharedStationNo))
            .OrderByDescending(config => config.StationNo == station)
            .ThenBy(config => config.Id)
            .FirstOrDefault();
        return dedicated ?? active.SingleOrDefault(config => config.IsStationDefault == true && config.StationNo == station);
    }

    public static void ValidateDefaults(IEnumerable<BizProductProcessConfig> configs)
    {
        ArgumentNullException.ThrowIfNull(configs);
        var defaults = configs.Where(config => config.IsStationDefault == true).ToList();
        var invalid = defaults.FirstOrDefault(config => !IsDefaultStation(config.StationNo));
        if (invalid is not null)
            throw new InvalidOperationException($"产品工号“{invalid.ProductNum}”的工位默认只能设置为工位 1 或 2，不能使用工位 {invalid.StationNo}。");
        var duplicate = defaults.Where(config => config.Enabled).GroupBy(config => config.StationNo)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"工位 {duplicate.Key} 存在多条启用的默认工艺：{string.Join("、", duplicate.Select(config => config.ProductNum))}。请仅保留一条工位默认。");
    }

    public static void ValidateConfigurations(IEnumerable<BizProductProcessConfig> configs)
    {
        ArgumentNullException.ThrowIfNull(configs);
        var rows = configs.ToList();
        ValidateDefaults(rows);
        var duplicate = rows.Where(config => config.Enabled)
            .GroupBy(config => $"{config.ProductNum?.Trim()}{config.StationNo}", StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            var first = duplicate.First();
            throw new InvalidOperationException($"产品工号“{first.ProductNum}”、工位“{first.StationNo}”存在重复启用配置。");
        }
    }
}
