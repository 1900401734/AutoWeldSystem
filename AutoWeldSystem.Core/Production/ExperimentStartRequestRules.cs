using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Provides reusable rules for preparing start-work requests before MES upload.
/// </summary>
public static class ExperimentStartRequestRules
{
    /// <summary>
    /// Applies the locally generated task id to offline start requests when MES has not returned an id yet.
    /// </summary>
    /// <param name="task">Local weld task.</param>
    /// <param name="request">Start request that will be sent to MES.</param>
    /// <returns>true when the request id was changed.</returns>
    public static bool ApplyOfflineStartId(BizWeldTask task, ExperimentStartReq request)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(request);

        if (!task.IsOfflineCreated
            || !string.IsNullOrWhiteSpace(request.Id)
            || string.IsNullOrWhiteSpace(task.LocalExpStartId))
        {
            return false;
        }

        request.Id = task.LocalExpStartId.Trim();
        return true;
    }
}
