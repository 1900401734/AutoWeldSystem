using AutoWeldSystem.Core.Production;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 程序计数模式下按本地采集记录统计任务产量，供完工上报与监控页指标共用同一口径。
/// </summary>
public interface IProductionCountService
{
    FinishQuantities GetTaskQuantities(int taskId);
}
