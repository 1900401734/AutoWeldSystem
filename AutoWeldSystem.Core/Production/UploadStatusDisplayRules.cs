using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Converts upload status codes into operator-facing text.
/// Keeping this rule in Core prevents each UI page from translating the same status differently.
/// </summary>
public static class UploadStatusDisplayRules
{
    /// <summary>
    /// Returns the display text for a persisted upload status code.
    /// </summary>
    public static string GetDisplayText(string? status)
    {
        return status?.Trim() switch
        {
            ProductionConstants.UploadStatuses.Pending => "待上传",
            ProductionConstants.UploadStatuses.Uploading => "上传中",
            ProductionConstants.UploadStatuses.Uploaded => "已上传",
            ProductionConstants.UploadStatuses.Failed => "上传失败",
            ProductionConstants.UploadStatuses.Retrying => "重试中",
            ProductionConstants.UploadStatuses.Skipped => "已跳过",
            UploadSummaryStatusResolver.NoData => "无数据",
            _ => status ?? string.Empty
        };
    }

    /// <summary>
    /// Returns status text for the upload state page, using MES connection state for operator-facing pending hints.
    /// </summary>
    public static string GetDisplayText(string? status, bool mesConnected)
    {
        var normalizedStatus = status?.Trim();
        if (!mesConnected && string.Equals(normalizedStatus, ProductionConstants.UploadStatuses.Failed, StringComparison.OrdinalIgnoreCase))
        {
            return "待上传";
        }

        if (mesConnected && string.Equals(normalizedStatus, UploadSummaryStatusResolver.NoData, StringComparison.Ordinal))
        {
            return "待上传";
        }

        return GetDisplayText(status);
    }
}
