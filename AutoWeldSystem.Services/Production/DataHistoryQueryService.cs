using System.Text.Json;
using AutoWeldSystem.Core.DTOs.DataManagement;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// Reads local task, weld-point and report-file history for the data-management page.
/// Database access and test-scheme interpretation stay outside the UI layer.
/// </summary>
public sealed class DataHistoryQueryService : IDataHistoryQueryService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly object _queryLock = new();

    public DataHistoryQueryService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PagedResult<DataHistoryWorkOrderRow>> QueryWorkOrdersAsync(
        DataHistoryQueryCriteria criteria,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        return RunQueryAsync(() => QueryWorkOrders(criteria, pageIndex, pageSize), cancellationToken);
    }

    public Task<DataHistoryWeldParameterResult> QueryWeldParametersAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        return RunQueryAsync(() => QueryWeldParameters(taskId), cancellationToken);
    }

    public Task<PagedResult<DataHistoryCollectionRow>> QueryCollectionRecordsAsync(
        int taskId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return RunQueryAsync(() => QueryCollectionRecords(taskId, pageIndex, pageSize), cancellationToken);
    }

    public Task<IReadOnlyList<DataHistoryReportFileRow>> QueryReportFilesAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        return RunQueryAsync<IReadOnlyList<DataHistoryReportFileRow>>(
            () => QueryReportFiles(taskId),
            cancellationToken);
    }

    private Task<T> RunQueryAsync<T>(Func<T> query, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_queryLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _dbContext.InitDatabase();
                return query();
            }
        }, cancellationToken);
    }

    private PagedResult<DataHistoryWorkOrderRow> QueryWorkOrders(
        DataHistoryQueryCriteria criteria,
        int pageIndex,
        int pageSize)
    {
        var normalizedPageIndex = Math.Max(1, pageIndex);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 500);
        var productNum = Normalize(criteria.ProductNum);
        var batch = Normalize(criteria.Batch);
        var workOrderId = Normalize(criteria.SN);
        var startTime = criteria.StartTime;
        var endTime = criteria.EndTime < criteria.StartTime ? criteria.StartTime : criteria.EndTime;

        var query = _dbContext.Db.Queryable<BizWeldTask>()
            .Where(task => task.StartTime >= startTime && task.StartTime <= endTime);

        if (!string.IsNullOrEmpty(productNum))
        {
            query = query.Where(task => task.ProductNum.Contains(productNum));
        }

        if (!string.IsNullOrEmpty(batch))
        {
            query = query.Where(task => task.Batch.Contains(batch));
        }

        if (!string.IsNullOrEmpty(workOrderId))
        {
            query = query.Where(task => task.SN.Contains(workOrderId));
        }

        var totalCount = 0;
        var tasks = query
            .OrderBy(task => task.StartTime, SqlSugar.OrderByType.Desc)
            .OrderBy(task => task.Id, SqlSugar.OrderByType.Desc)
            .ToPageList(normalizedPageIndex, normalizedPageSize, ref totalCount);

        return new PagedResult<DataHistoryWorkOrderRow>
        {
            Items = tasks.Select(ToWorkOrderRow).ToList(),
            TotalCount = totalCount,
            PageIndex = normalizedPageIndex,
            PageSize = normalizedPageSize
        };
    }

    private DataHistoryWeldParameterResult QueryWeldParameters(int taskId)
    {
        var task = GetTask(taskId);
        if (task is null)
        {
            return new DataHistoryWeldParameterResult();
        }

        var records = GetTaskRecords(taskId);
        var schemeItems = ResolveSchemeItems(task, records);
        var dynamicColumns = BuildDynamicColumns(schemeItems);
        var rows = records.Select(record => new DataHistoryWeldParameterRow
        {
            StationNo = record.StationNo,
            ProductNo = record.ProductNo,
            TouchNo = record.TouchNo,
            TestResult = record.TestResult,
            RecordTime = record.Ts,
            DynamicValues = BuildDynamicValues(record, schemeItems)
        }).ToList();

        return new DataHistoryWeldParameterResult
        {
            DynamicColumns = dynamicColumns,
            Rows = rows
        };
    }

    private PagedResult<DataHistoryCollectionRow> QueryCollectionRecords(
        int taskId,
        int pageIndex,
        int pageSize)
    {
        var normalizedPageIndex = Math.Max(1, pageIndex);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 500);
        var totalCount = 0;
        var records = _dbContext.Db.Queryable<BizWeldPointRecord>()
            .Where(record => record.TaskId == taskId)
            .OrderBy(record => record.Ts, SqlSugar.OrderByType.Desc)
            .OrderBy(record => record.Id, SqlSugar.OrderByType.Desc)
            .ToPageList(normalizedPageIndex, normalizedPageSize, ref totalCount);

        return new PagedResult<DataHistoryCollectionRow>
        {
            Items = records.Select(ToCollectionRow).ToList(),
            TotalCount = totalCount,
            PageIndex = normalizedPageIndex,
            PageSize = normalizedPageSize
        };
    }

    private IReadOnlyList<DataHistoryReportFileRow> QueryReportFiles(int taskId)
    {
        return _dbContext.Db.Queryable<BizProductionReportFile>()
            .Where(report => report.TaskId == taskId)
            .OrderBy(report => report.CreatedTime, SqlSugar.OrderByType.Desc)
            .ToList()
            .Select(report => new DataHistoryReportFileRow
            {
                Id = report.Id,
                FileName = report.FileName,
                FileFormat = report.FileFormat,
                FilePath = report.FilePath,
                UploadStatus = report.UploadStatus,
                CreatedTime = report.CreatedTime,
                UpdatedTime = report.UpdatedTime
            })
            .ToList();
    }

    private BizWeldTask? GetTask(int taskId)
    {
        return taskId <= 0
            ? null
            : _dbContext.Db.Queryable<BizWeldTask>().InSingle(taskId);
    }

    private List<BizWeldPointRecord> GetTaskRecords(int taskId)
    {
        return _dbContext.Db.Queryable<BizWeldPointRecord>()
            .Where(record => record.TaskId == taskId)
            .OrderBy(record => record.StationNo)
            .OrderBy(record => record.ProductNo)
            .OrderBy(record => record.SequenceNo)
            .OrderBy(record => record.Id)
            .ToList();
    }

    private IReadOnlyList<SchemeItemDefinition> ResolveSchemeItems(
        BizWeldTask task,
        IReadOnlyList<BizWeldPointRecord> records)
    {
        if (string.IsNullOrWhiteSpace(task.ProductNum))
        {
            return Array.Empty<SchemeItemDefinition>();
        }

        var stationNumbers = records.Select(record => record.StationNo)
            .Where(stationNo => stationNo > 0)
            .Append(Math.Max(1, task.StationNo))
            .Distinct()
            .ToList();
        var configs = _dbContext.Db.Queryable<BizProductProcessConfig>()
            .Where(config => config.Enabled && config.ProductNum == task.ProductNum)
            .ToList()
            .Where(config => config.StationNo == 0 || stationNumbers.Contains(config.StationNo))
            .OrderBy(config => config.StationNo)
            .ThenBy(config => config.Id)
            .ToList();
        var schemeIds = configs.Select(config => config.SchemeId)
            .Where(schemeId => !string.IsNullOrWhiteSpace(schemeId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (schemeIds.Count == 0)
        {
            return Array.Empty<SchemeItemDefinition>();
        }

        var details = _dbContext.Db.Queryable<BizSchemeDetail>()
            .Where(detail => schemeIds.Contains(detail.SchemeId))
            .OrderBy(detail => detail.DetailId)
            .ToList()
            .Select(NormalizeLegacyDetailRoles)
            .ToList();
        var itemIds = details.Select(detail => detail.ItemId).Distinct().ToList();
        var items = _dbContext.Db.Queryable<DimTestItem>()
            .Where(item => itemIds.Contains(item.ItemId))
            .ToList();

        return details
            .Select(detail => new SchemeItemDefinition(
                items.FirstOrDefault(item => item.ItemId == detail.ItemId),
                detail))
            .Where(definition => definition.Item is not null)
            .Where(definition => HasAnyEnabledRole(definition.Detail))
            .GroupBy(definition => definition.Item!.ItemId)
            .Select(group => group.First())
            .ToList();
    }

    private static IReadOnlyList<DataHistoryDynamicColumn> BuildDynamicColumns(
        IReadOnlyList<SchemeItemDefinition> schemeItems)
    {
        var columns = new List<DataHistoryDynamicColumn>();
        foreach (var definition in schemeItems)
        {
            var item = definition.Item!;
            var detail = definition.Detail;
            var itemKey = ResolveItemKey(item);
            AddColumn(columns, detail.EnableActual, itemKey, $"{item.ItemName}实际值");
            AddColumn(columns, detail.EnableUpper, $"{itemKey}_upper", $"{item.ItemName}上限");
            AddColumn(columns, detail.EnableLower, $"{itemKey}_lower", $"{item.ItemName}下限");
            AddColumn(columns, detail.EnableResult, $"{itemKey}_result", $"{item.ItemName}结果");
        }

        return columns;
    }

    private static IReadOnlyDictionary<string, string> BuildDynamicValues(
        BizWeldPointRecord record,
        IReadOnlyList<SchemeItemDefinition> schemeItems)
    {
        var rawValues = ParseRawData(record.RawDataJson);
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in schemeItems)
        {
            var item = definition.Item!;
            var detail = definition.Detail;
            var itemKey = ResolveItemKey(item);
            if (detail.EnableActual)
            {
                values[itemKey] = FirstRawValue(rawValues, itemKey, item.ItemName) ?? string.Empty;
            }

            if (detail.EnableUpper)
            {
                values[$"{itemKey}_upper"] = FirstRawValue(rawValues, $"{itemKey}_upper", $"{item.ItemName}上限") ?? string.Empty;
            }

            if (detail.EnableLower)
            {
                values[$"{itemKey}_lower"] = FirstRawValue(rawValues, $"{itemKey}_lower", $"{item.ItemName}下限") ?? string.Empty;
            }

            if (detail.EnableResult)
            {
                values[$"{itemKey}_result"] = FirstRawValue(rawValues, $"{itemKey}_result", $"{item.ItemName}结果") ?? string.Empty;
            }
        }

        return values;
    }

    private static Dictionary<string, string> ParseRawData(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return document.RootElement.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => JsonElementToText(property.Value),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string JsonElementToText(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Undefined => string.Empty,
            _ => value.ToString()
        };
    }

    private static string? FirstRawValue(
        IReadOnlyDictionary<string, string> values,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static void AddColumn(
        ICollection<DataHistoryDynamicColumn> columns,
        bool enabled,
        string key,
        string headerText)
    {
        if (!enabled || columns.Any(column => string.Equals(column.Key, key, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        columns.Add(new DataHistoryDynamicColumn { Key = key, HeaderText = headerText });
    }

    private static BizSchemeDetail NormalizeLegacyDetailRoles(BizSchemeDetail detail)
    {
        if (HasAnyEnabledRole(detail))
        {
            return detail;
        }

        detail.EnableActual = true;
        detail.EnableUpper = true;
        detail.EnableLower = true;
        detail.EnableResult = true;
        return detail;
    }

    private static bool HasAnyEnabledRole(BizSchemeDetail detail)
    {
        return detail.EnableActual || detail.EnableUpper || detail.EnableLower || detail.EnableResult;
    }

    private static string ResolveItemKey(DimTestItem item)
    {
        return item.ItemId > 0 ? $"item_{item.ItemId}" : item.ItemName.Trim();
    }

    private static DataHistoryWorkOrderRow ToWorkOrderRow(BizWeldTask task)
    {
        var processDisplay = string.IsNullOrWhiteSpace(task.ProcessName)
            ? task.ProcessNo
            : $"{task.ProcessNo} {task.ProcessName}".Trim();
        return new DataHistoryWorkOrderRow
        {
            TaskId = task.Id,
            StationNo = task.StationNo,
            WorkOrderId = task.SN,
            ProductNum = task.ProductNum,
            Batch = task.Batch,
            ProductName = task.ProductName,
            ProcessDisplay = processDisplay,
            RecipeCode = task.RecipeCode ?? string.Empty,
            PlannedQty = task.StartAmount,
            ActualQty = task.ActualQty,
            QualifiedQty = task.QualifiedQty,
            FailedQty = task.FailedQty,
            OperatorNumber = task.UserNumber ?? string.Empty,
            StartTime = task.StartTime,
            EndTime = task.EndTime,
            TaskStatus = task.TaskStatus,
            UploadStatus = task.UploadStatus
        };
    }

    private static DataHistoryCollectionRow ToCollectionRow(BizWeldPointRecord record)
    {
        return new DataHistoryCollectionRow
        {
            Id = record.Id,
            SequenceNo = record.SequenceNo,
            StationNo = record.StationNo,
            ProductNo = record.ProductNo,
            TouchNo = record.TouchNo,
            TestResult = record.TestResult,
            IsTest = record.IsTest,
            ProductCompleted = record.ProductCompleted,
            UploadStatus = record.UploadStatus,
            OperatorNo = record.OperatorNo ?? string.Empty,
            RecordTime = record.Ts,
            RawDataJson = record.RawDataJson ?? string.Empty
        };
    }

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private sealed record SchemeItemDefinition(DimTestItem? Item, BizSchemeDetail Detail);
}
