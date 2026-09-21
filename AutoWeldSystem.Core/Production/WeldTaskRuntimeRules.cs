using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Runtime;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Provides small reusable rules for updating production task runtime state.
/// Keeping these rules outside the WinForms view and service makes the behavior easy to test.
/// </summary>
public static class WeldTaskRuntimeRules
{
    public static bool IsAbandoned(BizWeldTask? task)
        => string.Equals(task?.TaskStatus, ProductionConstants.ProductInstanceStatuses.Abandoned, StringComparison.OrdinalIgnoreCase);

    public static bool IsProductionUpload(BizUploadTask task)
        => task.TaskType is ProductionConstants.UploadTaskTypes.StartReport
            or ProductionConstants.UploadTaskTypes.FinishReport
            or ProductionConstants.UploadTaskTypes.WorkOrderStatus
            or ProductionConstants.UploadTaskTypes.ProcessParameter
            or ProductionConstants.UploadTaskTypes.ReportFile
            or ProductionConstants.UploadTaskTypes.CenterProductReport;

    public static void EnsureNotAbandoned(BizWeldTask task)
    {
        if (IsAbandoned(task))
            throw new InvalidOperationException($"任务 {task.Id} 已异常结束，不能恢复生产、生成正式报告或补传。");
    }

    /// <summary>
    /// Clears the station runtime after a task is finished.
    /// </summary>
    /// <param name="station">Station runtime state to update.</param>
    /// <param name="finishedTask">Task that has just been completed.</param>
    /// <returns>true when the station was cleared; otherwise false.</returns>
    public static bool ClearFinishedTask(ProductionStationRuntimeState station, BizWeldTask finishedTask)
    {
        ArgumentNullException.ThrowIfNull(station);
        ArgumentNullException.ThrowIfNull(finishedTask);

        if (!ShouldClearStation(station, finishedTask))
        {
            return false;
        }

        // A finished task must not keep occupying ActiveTask, otherwise scanned work orders cannot appear.
        station.Reset();
        return true;
    }

    /// <summary>
    /// Determines whether a station runtime points to the finished task and should be cleared.
    /// </summary>
    /// <param name="station">Station runtime state to check.</param>
    /// <param name="finishedTask">Task that has just been completed.</param>
    /// <returns>true when the station references the finished task.</returns>
    public static bool ShouldClearStation(ProductionStationRuntimeState station, BizWeldTask finishedTask)
    {
        ArgumentNullException.ThrowIfNull(station);
        ArgumentNullException.ThrowIfNull(finishedTask);

        return station.ActiveTask?.Id == finishedTask.Id;
    }
}
