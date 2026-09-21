using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Defines which device-status log states must remain visible in pending uploads.
/// </summary>
public static class DeviceStatusUploadVisibilityRules
{
    /// <summary>
    /// Returns true only for logs that still require MES upload handling.
    /// </summary>
    public static bool ShouldInclude(string? reportStatus)
    {
        return string.Equals(reportStatus, ProductionConstants.UploadStatuses.Pending, StringComparison.OrdinalIgnoreCase)
            || string.Equals(reportStatus, ProductionConstants.UploadStatuses.Failed, StringComparison.OrdinalIgnoreCase);
    }
}
