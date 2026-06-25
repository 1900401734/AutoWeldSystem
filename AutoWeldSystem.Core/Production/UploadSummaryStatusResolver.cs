using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Resolves upload-summary statuses from upload tasks and persisted business facts.
/// This prevents successful online actions from being shown as "no data" when no retry task exists.
/// </summary>
public static class UploadSummaryStatusResolver
{
    /// <summary>
    /// Status text used when an upload phase has not produced any task or business data yet.
    /// </summary>
    public const string NoData = "无数据";

    /// <summary>
    /// Resolves the start-report status from retry tasks first, then the weld task ExpStartId.
    /// </summary>
    public static string ResolveStartReportStatus(BizWeldTask task, IEnumerable<string?> uploadTaskStatuses)
    {
        ArgumentNullException.ThrowIfNull(task);

        var uploadStatus = AggregateUploadStatuses(uploadTaskStatuses);
        if (!IsNoData(uploadStatus))
        {
            return uploadStatus;
        }

        return string.IsNullOrWhiteSpace(task.ExpStartId)
            ? ProductionConstants.UploadStatuses.Pending
            : ProductionConstants.UploadStatuses.Uploaded;
    }

    /// <summary>
    /// Resolves process-parameter status from retry tasks first, then weld point upload states.
    /// </summary>
    public static string ResolveProcessParameterStatus(
        IEnumerable<string?> uploadTaskStatuses,
        IEnumerable<BizWeldPointRecord> weldPointRecords)
    {
        var uploadStatus = AggregateUploadStatuses(uploadTaskStatuses);
        if (!IsNoData(uploadStatus))
        {
            return uploadStatus;
        }

        return AggregateUploadStatuses(weldPointRecords.Select(record => record.UploadStatus));
    }

    /// <summary>
    /// Resolves report-file status from retry tasks first, then the generated report records.
    /// </summary>
    public static string ResolveReportFileStatus(
        IEnumerable<string?> uploadTaskStatuses,
        IEnumerable<BizProductionReportFile> reportFiles)
    {
        var uploadStatus = AggregateUploadStatuses(uploadTaskStatuses);
        if (!IsNoData(uploadStatus))
        {
            return uploadStatus;
        }

        return AggregateUploadStatuses(reportFiles.Select(report => report.UploadStatus));
    }

    /// <summary>
    /// Resolves finish-report status from retry tasks first, then the completed weld task status.
    /// </summary>
    public static string ResolveFinishReportStatus(BizWeldTask task, IEnumerable<string?> uploadTaskStatuses)
    {
        ArgumentNullException.ThrowIfNull(task);

        var uploadStatus = AggregateUploadStatuses(uploadTaskStatuses);
        if (!IsNoData(uploadStatus))
        {
            return uploadStatus;
        }

        if (task.EndTime is null)
        {
            return NoData;
        }

        return string.Equals(task.UploadStatus, ProductionConstants.UploadStatuses.Uploaded, StringComparison.OrdinalIgnoreCase)
            ? ProductionConstants.UploadStatuses.Uploaded
            : ProductionConstants.UploadStatuses.Pending;
    }

    /// <summary>
    /// Aggregates statuses by action priority: uploading, failed, retrying, pending, uploaded, no data.
    /// </summary>
    public static string AggregateUploadStatuses(IEnumerable<string?> statuses)
    {
        var normalized = statuses
            .Select(status => status?.Trim())
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
        {
            return NoData;
        }

        if (normalized.Any(status => SameStatus(status, ProductionConstants.UploadStatuses.Uploading)))
        {
            return ProductionConstants.UploadStatuses.Uploading;
        }

        if (normalized.Any(status => SameStatus(status, ProductionConstants.UploadStatuses.Failed)))
        {
            return ProductionConstants.UploadStatuses.Failed;
        }

        if (normalized.Any(status => SameStatus(status, ProductionConstants.UploadStatuses.Retrying)))
        {
            return ProductionConstants.UploadStatuses.Retrying;
        }

        if (normalized.Any(status => SameStatus(status, ProductionConstants.UploadStatuses.Pending)))
        {
            return ProductionConstants.UploadStatuses.Pending;
        }

        return ProductionConstants.UploadStatuses.Uploaded;
    }

    /// <summary>
    /// Returns whether a summary status requires operator attention or upload execution.
    /// </summary>
    public static bool IsPendingLike(string? status)
    {
        return SameStatus(status, ProductionConstants.UploadStatuses.Pending)
            || SameStatus(status, ProductionConstants.UploadStatuses.Uploading)
            || SameStatus(status, ProductionConstants.UploadStatuses.Failed)
            || SameStatus(status, ProductionConstants.UploadStatuses.Retrying);
    }

    /// <summary>
    /// Returns whether the status represents an absent phase rather than a real upload state.
    /// </summary>
    public static bool IsNoData(string? status)
    {
        return string.Equals(status?.Trim(), NoData, StringComparison.Ordinal);
    }

    private static bool SameStatus(string? left, string right)
    {
        return string.Equals(left?.Trim(), right, StringComparison.OrdinalIgnoreCase);
    }
}
