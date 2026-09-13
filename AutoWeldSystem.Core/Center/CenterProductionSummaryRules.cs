using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Production;

namespace AutoWeldSystem.Core.Center;

/// <summary>日统计与任务累计分开；跨日工单不按开工日期切断，重测按最新完成记录归日。</summary>
public static class CenterProductionSummaryRules
{
    public static FinishQuantities ForDay(IEnumerable<BizWeldPointRecord> records, int stationNo, DateTime date)
    {
        var completedProducts = records
            .Where(record => record.StationNo == stationNo && record.ProductCompleted
                && !string.IsNullOrWhiteSpace(record.ProductNo))
            .GroupBy(record => (record.TaskId, ProductNo: record.ProductNo.Trim().ToUpperInvariant()))
            .Where(group => !group.Any(record => record.IsDeleted)
                && group.Max(record => record.Ts).Date == date.Date)
            .Select(group => group.OrderByDescending(record => record.Ts).First());
        // 不同任务可以从相同产品号重新计数；每个任务内部复用正式完工计数口径。
        var counts = completedProducts.GroupBy(record => record.TaskId)
            .Select(group => FinishQuantityRules.Calculate(group)).ToList();
        return new FinishQuantities(counts.Sum(count => count.ActualQty),
            counts.Sum(count => count.QualifiedQty), counts.Sum(count => count.FailedQty));
    }

    public static BizWeldTask? ActiveTask(IEnumerable<BizWeldTask> tasks) => tasks
        .Where(task => task.EndTime is null && task.TaskStatus is "Running" or "Paused")
        .OrderByDescending(task => task.StartTime).FirstOrDefault();

    public static int SumTaskTargets(IEnumerable<CenterDashboardStationDto> stations) => stations
        .Where(station => !string.IsNullOrWhiteSpace(station.CurrentWorkOrder) && station.WorkOrderQuantity > 0)
        .GroupBy(station => string.IsNullOrWhiteSpace(station.TaskKey) ? station.CurrentWorkOrder.Trim() : station.TaskKey, StringComparer.OrdinalIgnoreCase)
        .Sum(group => group.Max(station => station.WorkOrderQuantity));
}
