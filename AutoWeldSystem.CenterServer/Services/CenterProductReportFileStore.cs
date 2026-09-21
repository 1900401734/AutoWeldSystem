using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Production;

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
        CenterProductReportPathResolver.ValidateArchiveIdentity(request);
        var routeLockPath = _pathResolver.BuildRoutingLockPath(dataDirectory, request);
        Directory.CreateDirectory(Path.GetDirectoryName(routeLockPath)!);
        using var routeLock = _pathLock.Acquire(routeLockPath);
        var reportPath = ResolveReportPath(dataDirectory, request);
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        using var pathLock = _pathLock.Acquire(reportPath);
        var existing = _reader.Load(reportPath);
        if (existing.TaskState is not null
            && (CenterProductReportPathResolver.HasArchiveIdentity(request) || existing.TaskState.ReportSequenceNo.HasValue))
        {
            EnsureMatchingIdentity(existing, request);
        }
        var rows = MergeRows(existing.Rows, request);
        var requestColumns = CenterProductReportFormat.FromDtos(request.ReportColumns);
        CenterProductReportFormat.EnsureCompatiblePointHeaders(existing.Columns, requestColumns);
        var columns = CenterProductReportFormat.BuildDetailColumns(existing.Columns.Concat(requestColumns));
        var taskState = ResolveTaskState(existing.TaskState, request);

        _atomicWriter.Write(reportPath, workbook => _writer.Populate(workbook, taskState, columns, rows));
        return reportPath;
    }

    private string ResolveReportPath(string dataDirectory, CenterProductReportRequest request)
    {
        var legacyPath = _pathResolver.BuildReportPath(dataDirectory, request.DeviceId, request.WorkOrder);
        var hasIdentity = CenterProductReportPathResolver.HasArchiveIdentity(request);
        if (hasIdentity && File.Exists(legacyPath))
        {
            var legacy = _reader.Load(legacyPath);
            if (legacy.TaskState is null || legacy.TaskState.StartTime == default
                || string.IsNullOrWhiteSpace(legacy.TaskState.WorkOrder))
            {
                throw new InvalidDataException("旧报表缺少任务身份，无法安全定位，请人工核对原文件。");
            }
            if (MatchesTask(legacy, request))
            {
                EnsureMatchingIdentity(legacy, request);
                if (File.Exists(_pathResolver.BuildArchivePath(dataDirectory, request)))
                    throw new InvalidDataException("同一任务同时存在新旧报表，请人工核对，禁止自动合并或覆盖。");
                return legacyPath;
            }
        }
        if (hasIdentity) return _pathResolver.BuildArchivePath(dataDirectory, request);

        // 升级后迟到的旧协议也要命中已有新报表，不能因缺序号而另建一份旧文件。
        var matches = new List<string>();
        foreach (var path in _pathResolver.EnumerateTaskArchivePaths(_pathResolver.NormalizeRoot(dataDirectory), request))
        {
            var state = _reader.Load(path);
            if (MatchesTask(state, request)) matches.Add(path);
        }
        if (matches.Count > 1 || (matches.Count == 1 && File.Exists(legacyPath)
            && MatchesTask(_reader.Load(legacyPath), request)))
        {
            throw new InvalidDataException("旧请求匹配多个报表，缺少序号，禁止猜测目标文件。");
        }
        return matches.Count == 1 ? matches[0] : legacyPath;
    }

    private static bool MatchesTask(CenterProductReportStoredState state, CenterProductReportRequest request)
    {
        var task = state.TaskState;
        // MySQL 任务时间按秒持久化；旧设备内存请求可能保留毫秒，升级补传按同一持久化精度比较。
        if (task is null || task.StartTime == default
            || task.StartTime.Ticks / TimeSpan.TicksPerSecond != request.StartTime.Ticks / TimeSpan.TicksPerSecond
            || !string.Equals(task.WorkOrder, request.WorkOrder.Trim(), StringComparison.Ordinal)
            || !string.Equals(task.ProcessNo, request.ProcessNo.Trim(), StringComparison.Ordinal)) return false;
        var deviceId = task.DeviceId.Length > 0 ? task.DeviceId : state.Rows.FirstOrDefault()?.DeviceId;
        // 旧的纯完工报表没有明细设备号，设备身份由旧哈希路径限定。
        return deviceId is null || string.Equals(deviceId, request.DeviceId.Trim(), StringComparison.Ordinal);
    }

    private static void EnsureMatchingIdentity(CenterProductReportStoredState existing, CenterProductReportRequest request)
    {
        if (!MatchesTask(existing, request))
            throw new InvalidDataException("报表路径已被其他任务占用，禁止覆盖；请核对设备编号、流转卡、工序及开工时间。");
        var task = existing.TaskState!;
        if (task.ReportSequenceNo.HasValue && CenterProductReportPathResolver.HasArchiveIdentity(request)
            && (task.ReportSequenceNo != request.ReportSequenceNo
                || !string.Equals(task.ReportFileName, request.ReportFileName, StringComparison.Ordinal)))
        {
            throw new InvalidDataException("已保存报表的序号或文件名与请求不一致，禁止覆盖。");
        }
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
            try
            {
                // 每个正式历史文件只解析一次；单个损坏文件不能阻断其余看板数据。
                var state = _reader.Load(filePath);
                products.AddRange(state.Rows
                    .Where(row => row.StationNo == stationNo
                        && !row.IsDeleted
                        && row.CompletedAt.Date == reportDate.Date
                        && string.Equals(row.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
                    .Select(row => row.ToSummary()));
            }
            catch (Exception)
            {
                // 历史枚举采用隔离策略。目标文件的 Upsert 仍走严格读取并向上抛错，禁止覆盖损坏文件。
            }
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
            .ThenBy(row => row.ProductNo, NaturalSortComparer.Instance)
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
        var state = existing?.EndTime is not null
            ? existing
            : CenterProductReportTaskState.FromRequest(request, request.IsTaskFinishUpdate ? request.EndTime : null);
        // 最终统计不回退，但新设备补传仍需补齐隐藏归档身份；迟到旧请求不能清空它。
        return state with
        {
            DeviceId = existing?.DeviceId is { Length: > 0 } deviceId ? deviceId : request.DeviceId.Trim(),
            ReportSequenceNo = existing?.ReportSequenceNo ?? request.ReportSequenceNo,
            ReportFileName = existing?.ReportFileName ?? request.ReportFileName
        };
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
