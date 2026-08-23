namespace AutoWeldSystem.Core.DTOs.DataManagement;

/// <summary>
/// 删除前的影响范围预览，用于二次确认时告知操作人实际删除量。
/// </summary>
public sealed class WorkOrderDeletionPreview
{
    /// <summary>
    /// 可删除的工单数量。
    /// </summary>
    public int WorkOrderCount { get; init; }

    /// <summary>
    /// 因处于运行中而跳过的工单数量。
    /// </summary>
    public int SkippedRunningCount { get; init; }

    /// <summary>
    /// 关联的焊点采集记录数量。
    /// </summary>
    public int RecordCount { get; init; }

    /// <summary>
    /// 关联的报表文件数量。
    /// </summary>
    public int ReportFileCount { get; init; }
}

/// <summary>
/// 删除执行结果汇总。
/// </summary>
public sealed class WorkOrderDeletionResult
{
    /// <summary>
    /// 实际删除的工单数量。
    /// </summary>
    public int DeletedWorkOrderCount { get; init; }

    /// <summary>
    /// 因处于运行中而跳过的工单数量。
    /// </summary>
    public int SkippedRunningCount { get; init; }

    /// <summary>
    /// 实际删除的焊点采集记录数量。
    /// </summary>
    public int DeletedRecordCount { get; init; }

    /// <summary>
    /// 成功删除的磁盘报表文件数量。
    /// </summary>
    public int DeletedReportFileCount { get; init; }

    /// <summary>
    /// 删除失败的磁盘报表文件数量；数据库记录已删除，磁盘文件残留。
    /// </summary>
    public int FailedFileDeletionCount { get; init; }
}
