using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Runtime;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Provides small reusable rules for updating production task runtime state.
/// Keeping these rules outside the WinForms view and service makes the behavior easy to test.
/// </summary>
public static class WeldTaskRuntimeRules
{
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
