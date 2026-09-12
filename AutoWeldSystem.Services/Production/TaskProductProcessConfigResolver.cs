using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Production;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 统一按任务绑定程序和工位解析产品工艺，避免采集、历史和中心转发使用不同产品工号。
/// </summary>
internal static class TaskProductProcessConfigResolver
{
    public static IReadOnlyDictionary<int, BizProductProcessConfig> Resolve(
        IProductProcessConfigService service,
        BizWeldTask task,
        IEnumerable<int> stationNumbers)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(stationNumbers);

        var result = new Dictionary<int, BizProductProcessConfig>();
        foreach (var stationNo in stationNumbers
                     .Select(value => NormalizeStationNo(value, task))
                     .Distinct()
                     .OrderBy(value => value))
        {
            var config = service.FindActiveForTask(task, stationNo);
            if (config is not null)
            {
                result[stationNo] = config;
            }
        }

        return result;
    }

    public static string ValidateProgram(
        IProductProcessConfigService processService,
        ITestSchemeConfigService schemeService,
        BizWeldTask task,
        IEnumerable<int> stationNumbers,
        string? deviceType)
    {
        var content = ProgramContentJsonRules.NormalizeForProduction(task.ProgramContentSnapshot, deviceType);
        if (!WholePieceProgramResultRules.IsApplicable(deviceType)) return content;

        var limits = ProgramContentJsonRules.ReadLimits(content, deviceType);
        foreach (var stationNo in stationNumbers.Distinct())
        {
            var config = processService.FindActiveForTask(task, stationNo)
                ?? throw new InvalidOperationException($"工位 {stationNo} 未找到任务绑定程序的产品工艺配置。");
            var items = ReadSchemeItems(schemeService, config.SchemeId);
            WholePieceProgramResultRules.ValidateScheme(limits, items);
        }
        return content;
    }

    public static IReadOnlyList<(BizSchemeDetail Detail, DimTestItem Item)> ReadSchemeItems(
        ITestSchemeConfigService service, string schemeId)
    {
        var dictionary = service.GetItems().ToDictionary(item => item.ItemId);
        return service.GetDetails(schemeId, normalizeRoles: false).Select(detail =>
        {
            if (!dictionary.TryGetValue(detail.ItemId, out var item))
                throw new InvalidOperationException($"测试方案“{schemeId}”中的测试项 ID {detail.ItemId} 不存在。");
            return (detail, item);
        }).ToList();
    }

    public static int NormalizeStationNo(int stationNo, BizWeldTask task)
    {
        if (stationNo > ProductionConstants.Stations.SharedStationNo)
        {
            return stationNo;
        }

        return task.StationNo > ProductionConstants.Stations.SharedStationNo
            ? task.StationNo
            : ProductionConstants.Stations.DefaultStationNo;
    }
}
