using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Resolves which station recipe signals belong to the same production scope.
/// This keeps MonitorView and background PLC services aligned under dual-station modes.
/// </summary>
public static class RecipeStationScopeRules
{
    /// <summary>
    /// Gets the station list that should receive the same PC recipe value.
    /// </summary>
    /// <param name="enableDualStation">Whether dual-station mode is enabled.</param>
    /// <param name="enableDualWorkOrder">Whether each station runs an independent work order.</param>
    /// <param name="stationNo">Source station number.</param>
    /// <returns>Stations that share the same recipe write scope.</returns>
    public static int[] ResolveSharedRecipeStations(bool enableDualStation, bool enableDualWorkOrder, int stationNo)
    {
        return enableDualStation && !enableDualWorkOrder
            ? [1, 2]
            : [NormalizeStationNo(stationNo)];
    }

    /// <summary>
    /// Gets the station list that shares one weld task instance.
    /// Dual-station same-work-order mode creates a single task stamped with the starting
    /// station, so any per-station task lookup must widen to both stations.
    /// </summary>
    /// <param name="enableDualStation">Whether dual-station mode is enabled.</param>
    /// <param name="enableDualWorkOrder">Whether each station runs an independent work order.</param>
    /// <param name="stationNo">Source station number.</param>
    /// <returns>Stations that share the same weld task lookup scope.</returns>
    public static int[] ResolveSharedTaskStations(bool enableDualStation, bool enableDualWorkOrder, int stationNo)
    {
        return enableDualStation && !enableDualWorkOrder
            ? [1, 2]
            : [NormalizeStationNo(stationNo)];
    }

    /// <summary>
    /// Gets the station list that should be monitored for PLC recipe snapshots.
    /// </summary>
    /// <param name="enableDualStation">Whether dual-station mode is enabled.</param>
    /// <returns>Stations that should be scanned by the recipe monitor.</returns>
    public static int[] ResolveMonitorStations(bool enableDualStation)
    {
        return enableDualStation
            ? [1, 2]
            : [ProductionConstants.Stations.DefaultStationNo];
    }

    private static int NormalizeStationNo(int stationNo)
    {
        return stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;
    }
}
