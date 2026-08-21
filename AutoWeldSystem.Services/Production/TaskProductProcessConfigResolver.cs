using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;

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
