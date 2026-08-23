using AutoWeldSystem.Core.DTOs.DataManagement;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 历史工单维护：级联删除工单及其关联数据。
/// 与只读的 IDataHistoryQueryService 分开，避免查询接口承担写操作。
/// </summary>
public interface IDataHistoryMaintenanceService
{
    /// <summary>
    /// 预览按工单 ID 删除的影响范围。
    /// </summary>
    Task<WorkOrderDeletionPreview> PreviewDeleteByIdsAsync(
        IReadOnlyCollection<int> taskIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 预览清理上传失败工单的影响范围。
    /// </summary>
    Task<WorkOrderDeletionPreview> PreviewDeleteFailedAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 预览按日期区间清理的影响范围。
    /// </summary>
    Task<WorkOrderDeletionPreview> PreviewDeleteByDateAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按工单 ID 级联删除工单及其关联数据。
    /// </summary>
    Task<WorkOrderDeletionResult> DeleteByIdsAsync(
        IReadOnlyCollection<int> taskIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理上传状态为失败的工单及其关联数据。
    /// </summary>
    Task<WorkOrderDeletionResult> DeleteFailedAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按日期区间清理工单及其关联数据。
    /// </summary>
    Task<WorkOrderDeletionResult> DeleteByDateAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步删除单个工单及其关联数据，供既有同步调用方复用。
    /// </summary>
    WorkOrderDeletionResult DeleteWorkOrder(int taskId);
}
