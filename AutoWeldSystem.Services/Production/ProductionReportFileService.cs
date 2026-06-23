using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Data;
using ClosedXML.Excel;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoWeldSystem.Core.Runtime;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 生产报表文件服务。
/// 负责把本地焊点记录整理为真实 XLSX 文件，并记录本地文件上传状态。
/// </summary>
public class ProductionReportFileService : IProductionReportFileService
{
    private const string ColumnStationNo = "station_no";
    private const string ColumnProductNo = "product_no";
    private const string ColumnProductResult = "product_result";
    private const string ColumnTouchNo = "touch_no";
    private const string ColumnTouchResult = "touch_result";
    private const string ColumnWorkOrder = "work_order";
    private const string ColumnBatch = "batch";
    private const string ColumnQuantity = "quantity";
    private const string ColumnPartName = "part_name";
    private const string ColumnProcessNo = "process_no";
    private const string ColumnOperator = "operator";
    private const string ColumnRecordTime = "record_time";
    private const string ReportRoleActual = "actual";
    private const string ReportRoleUpper = "upper";
    private const string ReportRoleLower = "lower";
    private const string ReportRoleResult = "result";
    private const string HeaderStationNo = "工位";
    private const string HeaderProductNo = "产品编号";
    private const string HeaderProductResult = "产品结果";
    private const string HeaderTouchNo = "焊点编号";
    private const string HeaderTouchResult = "焊点结果";
    private const string HeaderWorkOrder = "工号";
    private const string HeaderBatch = "批次";
    private const string HeaderQuantity = "数量";
    private const string HeaderPartName = "零部件名称";
    private const string HeaderProcessNo = "工序号";
    private const string HeaderOperator = "操作人员";
    private const string HeaderRecordTime = "日期";
    private const string ReportFormat = "XLSX";

    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IProductionFlowLogService _productionLogService;
    private readonly object _dbLock = new();
    private AppSettings _currentSettings;

    public ProductionReportFileService(
        SqlSugarDbContext dbContext,
        IAppSettingsService settingsService,
        IProductionFlowLogService productionLogService)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
        _productionLogService = productionLogService;
    }

    public BizProductionReportFile GenerateXlsxReport(BizWeldTask task)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var report = GetOrCreateReportRecord(task);
            var records = _dbContext.Db.Queryable<BizWeldPointRecord>()
                .Where(record => record.TaskId == task.Id)
                .ToList()
                .OrderBy(record => record.ProductNo)
                .ThenBy(record => record.StationNo)
                .ThenBy(record => record.SequenceNo)
                .ToList();

            Directory.CreateDirectory(Path.GetDirectoryName(report.FilePath)!);
            WriteXlsx(report.FilePath, BuildReportSchema(task), records, task);

            report.FileFormat = ReportFormat;
            report.UploadStatus = ProductionConstants.UploadStatuses.Pending;
            report.UploadMessage = $"XLSX report generated, rows={records.Count}.";
            report.UpdatedTime = DateTime.Now;
            if (report.Id <= 0)
            {
                return _dbContext.Db.Insertable(report).ExecuteReturnEntity();
            }

            _dbContext.Db.Updateable(report).ExecuteCommand();
            return _dbContext.Db.Queryable<BizProductionReportFile>().InSingle(report.Id) ?? report;
        }
    }

    private BizProductionReportFile GetOrCreateReportRecord(BizWeldTask task)
    {
        var existing = _dbContext.Db.Queryable<BizProductionReportFile>()
            .First(report => report.TaskId == task.Id
                && report.FileCode == ProductionConstants.ReportFileCodes.Spreadsheet
                && report.FileFormat == ReportFormat);

        if (existing is not null)
        {
            return existing;
        }

        var sequenceNo = GetNextSequenceNo(task);
        var fileName = BuildFileName(task, sequenceNo);
        var filePath = Path.Combine(GetReportDirectory(task), fileName);

        return new BizProductionReportFile
        {
            TaskId = task.Id,
            ExpStartId = task.ExpStartId,
            DeviceId = task.DeviceId,
            SN = task.SN,
            ProcessNo = task.ProcessNo,
            FileCode = ProductionConstants.ReportFileCodes.Spreadsheet,
            MesFileType = ProductionConstants.MesFileTypes.ReportFile,
            FileFormat = ReportFormat,
            FileName = fileName,
            FilePath = filePath,
            SequenceNo = sequenceNo,
            UploadStatus = ProductionConstants.UploadStatuses.Pending,
            CreatedTime = DateTime.Now,
            UpdatedTime = DateTime.Now
        };
    }

    private int GetNextSequenceNo(BizWeldTask task)
    {
        var existingReports = _dbContext.Db.Queryable<BizProductionReportFile>()
            .Where(report => report.DeviceId == task.DeviceId
                && report.SN == task.SN
                && report.ProcessNo == task.ProcessNo
                && report.FileCode == ProductionConstants.ReportFileCodes.Spreadsheet)
            .ToList();

        return existingReports.Count == 0
            ? 1
            : existingReports.Max(report => report.SequenceNo) + 1;
    }

    private ReportSchema BuildReportSchema(BizWeldTask task)
    {
        var config = ResolveProductProcessConfig(task);
        var displayOptions = ReportDisplayOptions.FromConfig(config);
        var schemeItems = GetSchemeItemsForConfig(config);
        var leadingColumns = BuildLeadingColumns(displayOptions);
        var dynamicColumns = schemeItems.SelectMany(BuildItemColumns);
        var trailingColumns = BuildTrailingColumns();
        var columns = leadingColumns
            .Concat(dynamicColumns)
            .Concat(trailingColumns)
            .DistinctBy(column => column.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ReportSchema(columns, schemeItems, displayOptions);
    }

    private void WriteXlsx(
        string filePath,
        ReportSchema schema,
        IReadOnlyList<BizWeldPointRecord> records,
        BizWeldTask task)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("生产报表");
        WriteHeaderRow(worksheet, schema.Columns);
        WriteDataRows(worksheet, schema, records, task);
        MergeRepeatedProductFields(worksheet, schema.Columns, records);
        ApplyWorksheetStyle(worksheet, schema.Columns.Count, records.Count);
        workbook.SaveAs(filePath);
    }

    private static void WriteHeaderRow(IXLWorksheet worksheet, IReadOnlyList<ReportColumn> columns)
    {
        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            worksheet.Cell(1, columnIndex + 1).Value = columns[columnIndex].Title;
        }
    }

    private void WriteDataRows(
        IXLWorksheet worksheet,
        ReportSchema schema,
        IReadOnlyList<BizWeldPointRecord> records,
        BizWeldTask task)
    {
        var productContexts = BuildProductContexts(records);
        for (var rowIndex = 0; rowIndex < records.Count; rowIndex++)
        {
            var record = records[rowIndex];
            var row = BuildRow(record, task, ResolveProductContext(record, productContexts), schema);
            for (var columnIndex = 0; columnIndex < schema.Columns.Count; columnIndex++)
            {
                var column = schema.Columns[columnIndex];
                worksheet.Cell(rowIndex + 2, columnIndex + 1).Value = row.TryGetValue(column.Key, out var value)
                    ? value
                    : string.Empty;
            }
        }
    }

    private static void ApplyWorksheetStyle(IXLWorksheet worksheet, int columnCount, int dataRowCount)
    {
        if (columnCount <= 0)
        {
            return;
        }

        var lastRow = Math.Max(1, dataRowCount + 1);
        var usedRange = worksheet.Range(1, 1, lastRow, columnCount);
        usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        usedRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        usedRange.Style.Alignment.WrapText = true;

        var headerRange = worksheet.Range(1, 1, 1, columnCount);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF2FF");
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns(1, columnCount).AdjustToContents();
    }

    private static void MergeRepeatedProductFields(
        IXLWorksheet worksheet,
        IReadOnlyList<ReportColumn> columns,
        IReadOnlyList<BizWeldPointRecord> records)
    {
        if (records.Count <= 1)
        {
            return;
        }

        var mergeColumns = columns
            .Select((column, index) => new { Column = column, Index = index + 1 })
            .Where(item => item.Column.MergeByProduct)
            .Select(item => item.Index)
            .ToArray();

        var groupStartRow = 2;
        var currentKey = BuildProductMergeKey(records[0]);
        for (var recordIndex = 1; recordIndex < records.Count; recordIndex++)
        {
            var key = BuildProductMergeKey(records[recordIndex]);
            if (!string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
            {
                MergeProductColumns(worksheet, groupStartRow, recordIndex + 1, mergeColumns);
                groupStartRow = recordIndex + 2;
                currentKey = key;
            }
        }

        MergeProductColumns(worksheet, groupStartRow, records.Count + 1, mergeColumns);
    }

    private static void MergeProductColumns(IXLWorksheet worksheet, int startRow, int endRow, IReadOnlyList<int> columns)
    {
        if (endRow <= startRow)
        {
            return;
        }

        foreach (var column in columns)
        {
            var range = worksheet.Range(startRow, column, endRow, column);
            var distinctValues = range.Cells()
                .Select(cell => cell.GetString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            if (distinctValues <= 1)
            {
                range.Merge();
                range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }
    }

    private static string BuildProductMergeKey(BizWeldPointRecord record)
    {
        return $"{record.StationNo}\u001F{record.ProductNo}";
    }

    private static IReadOnlyDictionary<string, ProductReportContext> BuildProductContexts(IReadOnlyList<BizWeldPointRecord> records)
    {
        return records
            .GroupBy(BuildProductMergeKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => new ProductReportContext(
                    ResolveProductResult(group),
                    group.Max(record => record.Ts)),
                StringComparer.OrdinalIgnoreCase);
    }

    private static ProductReportContext ResolveProductContext(
        BizWeldPointRecord record,
        IReadOnlyDictionary<string, ProductReportContext> contexts)
    {
        return contexts.TryGetValue(BuildProductMergeKey(record), out var context)
            ? context
            : new ProductReportContext(record.TestResult, record.Ts);
    }

    private static string ResolveProductResult(IEnumerable<BizWeldPointRecord> records)
        => TestResultRules.ResolveProductResult(records.Select(record => record.TestResult));

    private Dictionary<string, string> BuildRow(
        BizWeldPointRecord record,
        BizWeldTask task,
        ProductReportContext productContext,
        ReportSchema schema)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [ColumnStationNo] = record.StationNo.ToString(),
            [ColumnProductNo] = record.ProductNo,
            [ColumnProductResult] = productContext.ProductResult,
            [ColumnTouchNo] = string.IsNullOrWhiteSpace(record.TouchNo) ? record.SequenceNo.ToString() : record.TouchNo,
            [ColumnTouchResult] = record.TestResult,
            [ColumnWorkOrder] = task.SN,
            [ColumnBatch] = task.Batch,
            [ColumnQuantity] = ResolveReportQuantity(task).ToString(),
            [ColumnPartName] = BuildPartName(task),
            [ColumnProcessNo] = task.ProcessNo,
            [ColumnOperator] = record.OperatorNo ?? task.EndOperatorNumber ?? task.UserNumber ?? string.Empty,
            [ColumnRecordTime] = productContext.RecordTime.ToString("yyyy-MM-dd HH:mm:ss")
        };

        AddDynamicValues(row, record, schema);
        return row;
    }

    private void AddDynamicValues(Dictionary<string, string> row, BizWeldPointRecord record, ReportSchema schema)
    {
        var rawValues = ParseRawData(record.RawDataJson);
        AddSchemeDynamicValues(row, rawValues, schema);
    }

    private void AddSchemeDynamicValues(
        Dictionary<string, string> row,
        IReadOnlyDictionary<string, string> rawValues,
        ReportSchema schema)
    {
        foreach (var schemeItem in schema.SchemeItems)
        {
            var item = schemeItem.Item;
            var detail = schemeItem.Detail;
            var itemKey = ResolveItemKey(item);
            if (SchemeDetailRoleRules.ShouldWriteReportRole(detail, SchemeDetailValueRole.Actual))
            {
                TryAddDynamicValue(row, BuildDynamicColumnKey(item, ReportRoleActual), GetRawValue(rawValues, item.ItemName, itemKey) ?? string.Empty);
            }

            if (SchemeDetailRoleRules.ShouldWriteReportRole(detail, SchemeDetailValueRole.Upper))
            {
                TryAddDynamicValue(row, BuildDynamicColumnKey(item, ReportRoleUpper), GetRawValue(rawValues, $"{item.ItemName}上限", $"{itemKey}_upper"));
            }

            if (SchemeDetailRoleRules.ShouldWriteReportRole(detail, SchemeDetailValueRole.Lower))
            {
                TryAddDynamicValue(row, BuildDynamicColumnKey(item, ReportRoleLower), GetRawValue(rawValues, $"{item.ItemName}下限", $"{itemKey}_lower"));
            }

            if (SchemeDetailRoleRules.ShouldWriteReportRole(detail, SchemeDetailValueRole.Result))
            {
                TryAddDynamicValue(row, BuildDynamicColumnKey(item, ReportRoleResult), GetRawValue(rawValues, $"{item.ItemName}结果", $"{itemKey}_result"));
            }
        }
    }

    private IReadOnlyList<SchemeReportItem> GetSchemeItemsForConfig(BizProductProcessConfig? config)
    {
        if (config is null)
        {
            return Array.Empty<SchemeReportItem>();
        }

        var details = _dbContext.Db.Queryable<BizSchemeDetail>()
            .Where(detail => detail.SchemeId == config.SchemeId)
            .ToList();
        if (details.Count == 0)
        {
            return Array.Empty<SchemeReportItem>();
        }

        var itemIds = details.Select(detail => detail.ItemId).Distinct().ToList();
        var items = _dbContext.Db.Queryable<DimTestItem>()
            .Where(item => itemIds.Contains(item.ItemId))
            .ToList();

        return details
            .OrderBy(detail => detail.DetailId)
            .Select(detail => new
            {
                Item = items.FirstOrDefault(item => item.ItemId == detail.ItemId),
                Detail = detail
            })
            .Where(item => item.Item is not null)
            .Select(item =>
            {
                SchemeDetailRoleRules.ClearUnavailableRoles(item.Detail, item.Item!);
                return item;
            })
            .Where(item => HasAnyEnabledRole(item.Detail))
            .Select(item => new SchemeReportItem(item.Item!, item.Detail))
            .ToList();
    }

    private BizProductProcessConfig? ResolveProductProcessConfig(BizWeldTask task)
    {
        var productNum = ResolveTaskProductNum(task);
        if (string.IsNullOrWhiteSpace(productNum))
        {
            return null;
        }

        var stationNo = task.StationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : task.StationNo;

        return _dbContext.Db.Queryable<BizProductProcessConfig>()
            .Where(config => config.Enabled && config.ProductNum == productNum)
            .ToList()
            .Where(config => config.StationNo == ProductionConstants.Stations.SharedStationNo || config.StationNo == stationNo)
            .OrderByDescending(config => config.StationNo == stationNo)
            .ThenBy(config => config.Id)
            .FirstOrDefault();
    }

    private static bool IsExactTextMatch(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private string ResolveTaskProductNum(BizWeldTask task)
    {
        if (!string.IsNullOrWhiteSpace(task.ProgramId))
        {
            var programs = _dbContext.Db.Queryable<BizProgram>()
                .Where(program => !program.IsDeleted && program.ProgramId == task.ProgramId.Trim())
                .ToList();

            var localProgram = programs
                .OrderByDescending(program => IsExactTextMatch(program.DeviceId, task.DeviceId))
                .ThenByDescending(program => program.UpdatedTime)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(localProgram?.ProductNum))
            {
                return localProgram.ProductNum.Trim();
            }
        }

        return task.ProductNum.Trim();
    }

    private static IEnumerable<ReportColumn> BuildLeadingColumns(ReportDisplayOptions displayOptions)
    {
        yield return new ReportColumn(ColumnStationNo, HeaderStationNo, MergeByProduct: true);
        yield return new ReportColumn(ColumnProductNo, HeaderProductNo, MergeByProduct: true);
        yield return new ReportColumn(ColumnProductResult, HeaderProductResult, MergeByProduct: true);
        yield return new ReportColumn(ColumnTouchNo, displayOptions.PointNoHeader, MergeByProduct: false);
        yield return new ReportColumn(ColumnTouchResult, displayOptions.PointResultHeader, MergeByProduct: false);
    }

    private static IEnumerable<ReportColumn> BuildTrailingColumns()
    {
        yield return new ReportColumn(ColumnWorkOrder, HeaderWorkOrder, MergeByProduct: true);
        yield return new ReportColumn(ColumnBatch, HeaderBatch, MergeByProduct: true);
        yield return new ReportColumn(ColumnQuantity, HeaderQuantity, MergeByProduct: true);
        yield return new ReportColumn(ColumnPartName, HeaderPartName, MergeByProduct: true);
        yield return new ReportColumn(ColumnProcessNo, HeaderProcessNo, MergeByProduct: true);
        yield return new ReportColumn(ColumnOperator, HeaderOperator, MergeByProduct: true);
        yield return new ReportColumn(ColumnRecordTime, HeaderRecordTime, MergeByProduct: true);
    }

    private static IEnumerable<ReportColumn> BuildItemColumns(SchemeReportItem schemeItem)
    {
        var item = schemeItem.Item;
        var detail = schemeItem.Detail;
        var itemName = NormalizeDisplayText(item.ItemName, $"测试项{item.ItemId}");

        if (SchemeDetailRoleRules.ShouldWriteReportRole(detail, SchemeDetailValueRole.Actual))
        {
            yield return new ReportColumn(BuildDynamicColumnKey(item, ReportRoleActual), NormalizeDisplayText(detail.ActualHeader, $"{itemName}实际值"), MergeByProduct: false);
        }

        if (SchemeDetailRoleRules.ShouldWriteReportRole(detail, SchemeDetailValueRole.Upper))
        {
            yield return new ReportColumn(BuildDynamicColumnKey(item, ReportRoleUpper), NormalizeDisplayText(detail.UpperHeader, $"{itemName}上限"), MergeByProduct: false);
        }

        if (SchemeDetailRoleRules.ShouldWriteReportRole(detail, SchemeDetailValueRole.Lower))
        {
            yield return new ReportColumn(BuildDynamicColumnKey(item, ReportRoleLower), NormalizeDisplayText(detail.LowerHeader, $"{itemName}下限"), MergeByProduct: false);
        }

        if (SchemeDetailRoleRules.ShouldWriteReportRole(detail, SchemeDetailValueRole.Result))
        {
            yield return new ReportColumn(BuildDynamicColumnKey(item, ReportRoleResult), NormalizeDisplayText(detail.ResultHeader, $"{itemName}结果"), MergeByProduct: false);
        }
    }

    private static string BuildDynamicColumnKey(DimTestItem item, string role)
        => $"{ResolveItemKey(item)}_{role}";

    private static string NormalizeDisplayText(string? value, string fallback)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue)
            ? fallback
            : normalizedValue;
    }

    private static void TryAddDynamicValue(Dictionary<string, string> row, string header, string? value)
    {
        if (string.IsNullOrWhiteSpace(header) || row.ContainsKey(header))
        {
            return;
        }

        row[header] = value ?? string.Empty;
    }

    private static string? GetRawValue(IReadOnlyDictionary<string, string> rawValues, params string?[] keys)
    {
        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key) && rawValues.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static string ResolveItemKey(DimTestItem item)
    {
        return item.ItemName.Trim() switch
        {
            "峰值电流" => "max_electric",
            "峰值电压" => "max_voltage",
            "有效功率" => "valid_power",
            "位移" => "displacement",
            "焊接时间" => "weld_ts",
            var name when !string.IsNullOrWhiteSpace(name) => $"item_{item.ItemId}",
            _ => $"item_{item.ItemId}"
        };
    }

    private static Dictionary<string, string> ParseRawData(string? rawDataJson)
    {
        if (string.IsNullOrWhiteSpace(rawDataJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(rawDataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return document.RootElement.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => property.Value.ToString(),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private string GetReportDirectory(BizWeldTask task)
    {
        var dataDirectory = CurrentSettings.DataDirectory;
        var baseDirectory = string.IsNullOrWhiteSpace(dataDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "Data")
            : dataDirectory.Trim();

        return Path.Combine(baseDirectory, "Reports", SanitizePathPart(task.SN), DateTime.Now.ToString("yyyyMMdd"));
    }

    private static string BuildFileName(BizWeldTask task, int sequenceNo)
    {
        return string.Join(
            "_",
            SanitizePathPart(task.DeviceId),
            SanitizePathPart(task.SN),
            SanitizePathPart(task.ProcessNo),
            ProductionConstants.ReportFileCodes.Spreadsheet,
            sequenceNo.ToString("D3")) + ".xlsx";
    }

    private static string BuildPartName(BizWeldTask task)
    {
        return task.ProductName;
    }

    private static int ResolveReportQuantity(BizWeldTask task)
    {
        // StartAmount is captured from the selected MES process StartAmount at start time.
        // It is the production quantity shown in the work-order process list.
        return task.StartAmount > 0 ? task.StartAmount : task.ActualQty;
    }

    private static string SanitizePathPart(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "NA" : value.Trim();
        return Regex.Replace(normalized, @"[\\/:*?""<>|]+", "-");
    }

    private static bool HasAnyEnabledRole(BizSchemeDetail detail)
    {
        return SchemeDetailRoleRules.AllRoles.Any(role => SchemeDetailRoleRules.ShouldWriteReportRole(detail, role));
    }

    private sealed record ProductReportContext(string ProductResult, DateTime RecordTime);

    private sealed record ReportSchema(
        IReadOnlyList<ReportColumn> Columns,
        IReadOnlyList<SchemeReportItem> SchemeItems,
        ReportDisplayOptions DisplayOptions);

    private sealed record ReportColumn(string Key, string Title, bool MergeByProduct);

    private sealed record ReportDisplayOptions(string PointNoHeader, string PointResultHeader)
    {
        public static ReportDisplayOptions FromConfig(BizProductProcessConfig? config)
        {
            if (config is null)
            {
                return new ReportDisplayOptions(HeaderTouchNo, HeaderTouchResult);
            }

            var pointName = NormalizeDisplayText(config.PointName, "焊点");
            return new ReportDisplayOptions(
                NormalizeDisplayText(config.PointNoHeader, $"{pointName}序号"),
                NormalizeDisplayText(config.PointResultHeader, $"{pointName}结果"));
        }
    }

    private sealed record SchemeReportItem(DimTestItem Item, BizSchemeDetail Detail);

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }
}
