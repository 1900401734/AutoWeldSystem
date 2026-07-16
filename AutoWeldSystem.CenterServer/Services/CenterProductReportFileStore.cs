using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.DTOs.CenterServer;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 协调中心生产报表的路径解析、状态合并、工作簿编解码和原子落盘。
/// 数据库与 SignalR 副作用不属于该类职责。
/// </summary>
public sealed class CenterProductReportFileStore
{
    private readonly CenterProductReportPathResolver _pathResolver = new();
    private readonly CenterProductReportWorkbookReader _reader = new();
    private readonly CenterProductReportWorkbookWriter _writer = new();
    private readonly CenterAtomicWorkbookWriter _atomicWriter = new();
    private readonly CenterReportPathLock _pathLock = new();

    /// <summary>
    /// 幂等写入产品明细，或只推进同一设备+流转卡的任务最终状态。
    /// </summary>
    public string Upsert(string dataDirectory, CenterProductReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var reportPath = _pathResolver.BuildReportPath(dataDirectory, request.DeviceId, request.WorkOrder);
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        using var pathLock = _pathLock.Acquire(reportPath);
        var existing = _reader.Load(reportPath);
        var rows = MergeRows(existing.Rows, request);
        var requestColumns = CenterProductReportFormat.FromDtos(request.ReportColumns);
        var columns = CenterProductReportFormat.BuildDetailColumns(existing.Columns.Concat(requestColumns));
        var taskState = ResolveTaskState(existing.TaskState, request);

        _atomicWriter.Write(reportPath, workbook => _writer.Populate(workbook, taskState, columns, rows));
        return reportPath;
    }

    /// <summary>
    /// 读取指定设备、工位和日期的产品摘要，供中心看板统计使用。
    /// </summary>
    public IReadOnlyList<CenterProductReportProductSummary> LoadProducts(
        string dataDirectory,
        string deviceId,
        int stationNo,
        DateTime reportDate)
    {
        var root = _pathResolver.NormalizeRoot(dataDirectory);
        if (!Directory.Exists(root))
        {
            return [];
        }

        var products = new List<CenterProductReportProductSummary>();
        foreach (var filePath in _pathResolver.EnumerateReportPaths(root))
        {
            var state = _reader.Load(filePath);
            products.AddRange(state.Rows
                .Where(row => row.StationNo == stationNo
                    && row.CompletedAt.Date == reportDate.Date
                    && string.Equals(row.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
                .Select(row => row.ToSummary()));
        }

        return products;
    }

    private static IReadOnlyList<CenterProductReportStoredRow> MergeRows(
        IReadOnlyList<CenterProductReportStoredRow> existingRows,
        CenterProductReportRequest request)
    {
        var rows = existingRows.ToList();
        if (!request.IsTaskFinishUpdate)
        {
            rows = rows.Where(row => !row.IsSameProduct(request)).ToList();
            rows.AddRange(request.Points
                .OrderBy(point => point.SequenceNo)
                .Select(point => CenterProductReportStoredRow.FromRequest(request, point)));
        }

        return rows
            .OrderBy(row => row.StationNo)
            .ThenBy(row => row.ProductNo)
            .ThenBy(row => row.SequenceNo)
            .ToList();
    }

    /// <summary>
    /// 已存在最终 EndTime 时整块任务元数据保持不变，迟到产品不得回退最终统计。
    /// 只有完工请求能首次推进 EndTime 和最终 QualifiedQty。
    /// </summary>
    private static CenterProductReportTaskState ResolveTaskState(
        CenterProductReportTaskState? existing,
        CenterProductReportRequest request)
    {
        if (existing?.EndTime is not null)
        {
            return existing;
        }

        return CenterProductReportTaskState.FromRequest(
            request,
            request.IsTaskFinishUpdate ? request.EndTime : null);
    }
}

/// <summary>
/// 中心报表中的产品级统计摘要。
/// </summary>
public sealed record CenterProductReportProductSummary(
    string DeviceId,
    int StationNo,
    string WorkOrder,
    string ProductNo,
    string ProductResult,
    DateTime CompletedAt);
