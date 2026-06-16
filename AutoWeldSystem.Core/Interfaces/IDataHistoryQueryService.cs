using AutoWeldSystem.Core.DTOs.DataManagement;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// Provides read-only local production-history queries for DataManageView.
/// </summary>
public interface IDataHistoryQueryService
{
    Task<PagedResult<DataHistoryWorkOrderRow>> QueryWorkOrdersAsync(
        DataHistoryQueryCriteria criteria,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<DataHistoryWeldParameterResult> QueryWeldParametersAsync(
        int taskId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<DataHistoryCollectionRow>> QueryCollectionRecordsAsync(
        int taskId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DataHistoryReportFileRow>> QueryReportFilesAsync(
        int taskId,
        CancellationToken cancellationToken = default);
}
