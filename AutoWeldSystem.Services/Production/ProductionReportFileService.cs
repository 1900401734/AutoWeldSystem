using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
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

    private static readonly string[] LeadingHeaders =
    {
        HeaderStationNo,
        HeaderProductNo,
        HeaderProductResult,
        HeaderTouchNo,
        HeaderTouchResult
    };

    private static readonly string[] TrailingHeaders =
    {
        HeaderWorkOrder,
        HeaderBatch,
        HeaderQuantity,
        HeaderPartName,
        HeaderProcessNo,
        HeaderOperator,
        HeaderRecordTime
    };

    private static readonly HashSet<string> ProductMergeHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        HeaderStationNo,
        HeaderProductNo,
        HeaderProductResult,
        HeaderWorkOrder,
        HeaderBatch,
        HeaderQuantity,
        HeaderPartName,
        HeaderProcessNo,
        HeaderOperator,
        HeaderRecordTime
    };

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
            WriteXlsx(report.FilePath, BuildHeaders(task), records, task);

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

    private string[] BuildHeaders(BizWeldTask task)
    {
        var dynamicHeaders = GetSchemeItemsForTask(task)
            .SelectMany(BuildItemHeaders)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(header => !LeadingHeaders.Contains(header, StringComparer.OrdinalIgnoreCase))
            .Where(header => !TrailingHeaders.Contains(header, StringComparer.OrdinalIgnoreCase));

        return LeadingHeaders
            .Concat(dynamicHeaders)
            .Concat(TrailingHeaders)
            .ToArray();
    }

    private void WriteXlsx(
        string filePath,
        IReadOnlyList<string> headers,
        IReadOnlyList<BizWeldPointRecord> records,
        BizWeldTask task)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("生产报表");
        WriteHeaderRow(worksheet, headers);
        WriteDataRows(worksheet, headers, records, task);
        MergeRepeatedProductFields(worksheet, headers, records);
        ApplyWorksheetStyle(worksheet, headers.Count, records.Count);
        workbook.SaveAs(filePath);
    }

    private static void WriteHeaderRow(IXLWorksheet worksheet, IReadOnlyList<string> headers)
    {
        for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
        {
            worksheet.Cell(1, columnIndex + 1).Value = headers[columnIndex];
        }
    }

    private void WriteDataRows(
        IXLWorksheet worksheet,
        IReadOnlyList<string> headers,
        IReadOnlyList<BizWeldPointRecord> records,
        BizWeldTask task)
    {
        var productContexts = BuildProductContexts(records);
        for (var rowIndex = 0; rowIndex < records.Count; rowIndex++)
        {
            var record = records[rowIndex];
            var row = BuildRow(record, task, ResolveProductContext(record, productContexts));
            for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
            {
                var header = headers[columnIndex];
                worksheet.Cell(rowIndex + 2, columnIndex + 1).Value = row.TryGetValue(header, out var value)
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
        IReadOnlyList<string> headers,
        IReadOnlyList<BizWeldPointRecord> records)
    {
        if (records.Count <= 1)
        {
            return;
        }

        var mergeColumns = headers
            .Select((header, index) => new { Header = header, Column = index + 1 })
            .Where(item => ProductMergeHeaders.Contains(item.Header))
            .Select(item => item.Column)
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
    {
        var recordList = records.ToList();
        if (recordList.Any(record => string.Equals(record.TestResult, ProductionConstants.TestResults.Ng, StringComparison.OrdinalIgnoreCase)))
        {
            return ProductionConstants.TestResults.Ng;
        }

        return recordList.All(record => string.Equals(record.TestResult, ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase))
            ? ProductionConstants.TestResults.Ok
            : ProductionConstants.TestResults.Unknown;
    }

    private Dictionary<string, string> BuildRow(BizWeldPointRecord record, BizWeldTask task, ProductReportContext productContext)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [HeaderStationNo] = record.StationNo.ToString(),
            [HeaderProductNo] = record.ProductNo,
            [HeaderProductResult] = productContext.ProductResult,
            [HeaderTouchNo] = string.IsNullOrWhiteSpace(record.TouchNo) ? record.SequenceNo.ToString() : record.TouchNo,
            [HeaderTouchResult] = record.TestResult,
            [HeaderWorkOrder] = task.SN,
            [HeaderBatch] = task.Batch,
            [HeaderQuantity] = ResolveReportQuantity(task).ToString(),
            [HeaderPartName] = BuildPartName(task),
            [HeaderProcessNo] = task.ProcessNo,
            [HeaderOperator] = record.OperatorNo ?? task.EndOperatorNumber ?? task.UserNumber ?? string.Empty,
            [HeaderRecordTime] = productContext.RecordTime.ToString("yyyy-MM-dd HH:mm:ss")
        };

        AddDynamicValues(row, record, task);
        return row;
    }

    private void AddDynamicValues(Dictionary<string, string> row, BizWeldPointRecord record, BizWeldTask task)
    {
        var rawValues = ParseRawData(record.RawDataJson);
        AddSchemeDynamicValues(row, record, rawValues, task);
    }

    private void AddSchemeDynamicValues(
        Dictionary<string, string> row,
        BizWeldPointRecord record,
        IReadOnlyDictionary<string, string> rawValues,
        BizWeldTask task)
    {
        foreach (var schemeItem in GetSchemeItemsForTask(task))
        {
            var item = schemeItem.Item;
            var detail = schemeItem.Detail;
            var header = GetItemHeader(item);
            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            var itemKey = ResolveItemKey(item);
            if (detail.EnableActual)
            {
                TryAddDynamicValue(row, header, GetRawValue(rawValues, item.ItemName, itemKey) ?? string.Empty);
            }

            if (detail.EnableUpper)
            {
                TryAddDynamicValue(row, $"{header}上限", GetRawValue(rawValues, $"{item.ItemName}上限", $"{itemKey}_upper"));
            }

            if (detail.EnableLower)
            {
                TryAddDynamicValue(row, $"{header}下限", GetRawValue(rawValues, $"{item.ItemName}下限", $"{itemKey}_lower"));
            }

            if (detail.EnableResult)
            {
                TryAddDynamicValue(row, $"{header}结果", GetRawValue(rawValues, $"{item.ItemName}结果", $"{itemKey}_result"));
            }
        }
    }

    private IReadOnlyList<SchemeReportItem> GetSchemeItemsForTask(BizWeldTask task)
    {
        var config = ResolveProductProcessConfig(task);
        if (config is null)
        {
            return Array.Empty<SchemeReportItem>();
        }

        var details = _dbContext.Db.Queryable<BizSchemeDetail>()
            .Where(detail => detail.SchemeId == config.SchemeId)
            .ToList()
            .Select(NormalizeLegacyDetailRoles)
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

    private static IEnumerable<string> BuildItemHeaders(SchemeReportItem schemeItem)
    {
        var item = schemeItem.Item;
        var detail = schemeItem.Detail;
        var header = GetItemHeader(item);
        if (string.IsNullOrWhiteSpace(header))
        {
            yield break;
        }

        if (detail.EnableActual)
        {
            yield return header;
        }

        if (detail.EnableUpper)
        {
            yield return $"{header}上限";
        }

        if (detail.EnableLower)
        {
            yield return $"{header}下限";
        }

        if (detail.EnableResult)
        {
            yield return $"{header}结果";
        }
    }

    private static string GetItemHeader(DimTestItem item)
    {
        return item.ItemName?.Trim() ?? string.Empty;
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
        return detail.EnableActual || detail.EnableUpper || detail.EnableLower || detail.EnableResult;
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

    private sealed record ProductReportContext(string ProductResult, DateTime RecordTime);

    private sealed record SchemeReportItem(DimTestItem Item, BizSchemeDetail Detail);

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }
}
