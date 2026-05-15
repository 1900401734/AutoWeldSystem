using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// ProductNo 生成服务。
/// 它负责在同一工单内按 1、2、3 递增分配产品编号，并保证双工位并发时不重复。
/// </summary>
public interface IProductNoGeneratorService
{
    /// <summary>
    /// 获取工位当前未完成产品；没有时自动创建新的 ProductNo。
    /// </summary>
    BizProductInstance GetOrCreateStationProduct(BizWeldTask task, int stationNo, int requiredTouchCount);

    /// <summary>
    /// 记录产品采集进度。达到应采集焊点数量时会自动标记为完成。
    /// </summary>
    BizProductInstance UpdateProgress(int productInstanceId, int collectedTouchCount, string? testResult = null);

    /// <summary>
    /// 获取指定工单下一个将被分配的数字编号。
    /// </summary>
    int PeekNextNumber(int taskId);
}
