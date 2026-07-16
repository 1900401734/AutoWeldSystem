using System.Text.Json;
using System.Text.RegularExpressions;
using AutoWeldSystem.CenterServer.Hubs;
using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Data;
using ClosedXML.Excel;
using Microsoft.AspNetCore.SignalR;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 接收设备端转发的单件产品数据，并直接维护中心服务器本地 Excel 报表。
/// 产品明细不写入中心数据库，数据库只保留看板最新运行快照。
/// </summary>
public sealed class CenterProductReportIngestService
{
    private static readonly Regex InvalidFileNameChars = new("""[\\/:*?""<>|]+""", RegexOptions.Compiled);

    private readonly SqlSugarDbContext _dbContext;
    private readonly IHubContext<CenterDashboardHub> _hubContext;
    private readonly CenterServerSettingsService _settingsService;
    private readonly CenterDashboardChangeNotifier _changeNotifier;
    private readonly object _dbLock = new();
    private readonly object _fileLock = new();

    public CenterProductReportIngestService(
        SqlSugarDbContext dbContext,
        IHubContext<CenterDashboardHub> hubContext,
        CenterServerSettingsService settingsService,
        CenterDashboardChangeNotifier changeNotifier)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _settingsService = settingsService;
        _changeNotifier = changeNotifier;
    }

    /// <summary>
    /// 保存一个完成产品，并刷新对应的中心端 Excel 报表。
    /// </summary>
    public async Task<CenterTelemetryAck> IngestAsync(
        CenterProductReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var deviceId = request.DeviceId.Trim();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return Fail("DeviceId is required.");
        }

        if (request.StationNo <= 0)
        {
            return Fail("StationNo is required.");
        }

        if (!request.IsTaskFinishUpdate && request.Points.Count == 0)
        {
            return Fail("Product report points are required.");
        }

        string reportPath;
        lock (_fileLock)
        {
            reportPath = WriteReportFile(deviceId, request);
        }

        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            UpsertDeviceNode(deviceId, request);
            if (!request.IsTaskFinishUpdate)
            {
                RefreshStationCounts(deviceId, request);
            }
        }

        _changeNotifier.Notify(deviceId);
        await _hubContext.Clients.All.SendAsync("CenterDashboardChanged", deviceId, cancellationToken);

        return new CenterTelemetryAck
        {
            Success = true,
            Message = $"Accepted, report={reportPath}",
            ServerTime = DateTime.Now
        };
    }

    private static CenterTelemetryAck Fail(string message)
    {
        return new CenterTelemetryAck
        {
            Success = false,
            Message = message,
            ServerTime = DateTime.Now
        };
    }

    /// <summary>
    /// 产品上传也可以首次登记设备节点，保证新设备能出现在看板中。
    /// </summary>
    private void UpsertDeviceNode(string deviceId, CenterProductReportRequest request)
    {
        var now = DateTime.Now;
        var node = _dbContext.Db.Queryable<CenterDeviceNode>().InSingle(deviceId);
        if (node is null)
        {
            _dbContext.Db.Insertable(new CenterDeviceNode
            {
                DeviceId = deviceId,
                DeviceName = request.DeviceName.Trim(),
                SystemType = CenterTelemetryRules.NormalizeSystemType(request.SystemType),
                FirstSeenAt = now,
                LastSeenAt = now
            }).ExecuteCommand();
            return;
        }

        node.DeviceName = request.DeviceName.Trim();
        node.SystemType = CenterTelemetryRules.NormalizeSystemType(request.SystemType);
        node.LastSeenAt = now;
        _dbContext.Db.Updateable(node).ExecuteCommand();
    }

    /// <summary>
    /// 写入或重写 Excel 报表。
    /// 产品请求幂等替换同一产品；完工请求只刷新任务级表头，不改动点明细。
    /// </summary>
    private string WriteReportFile(string deviceId, CenterProductReportRequest request)
    {
        var reportPath = BuildReportPath(deviceId, request);
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);

        var rows = LoadExistingRows(reportPath).ToList();
        if (!request.IsTaskFinishUpdate)
        {
            rows = rows
                .Where(row => !IsSameProduct(row, request))
                .ToList();
            rows.AddRange(BuildRows(deviceId, request));
        }

        rows = rows
            .OrderBy(row => row.StationNo)
            .ThenBy(row => row.ProductNo)
            .ThenBy(row => row.SequenceNo)
            .ToList();

        var columns = ResolveColumns(reportPath, request);
        using var workbook = new XLWorkbook();
        WriteReportWorksheet(workbook, request, columns, rows);
        WriteDataWorksheet(workbook, rows);
        WriteColumnsWorksheet(workbook, columns);
        workbook.SaveAs(reportPath);
        return reportPath;
    }

    /// <summary>
    /// 产品完成后立即更新看板计数。
    /// 计数来自 Excel 中当天的产品记录，而不是中心数据库。
    /// </summary>
    private void RefreshStationCounts(
        string deviceId,
        CenterProductReportRequest request)
    {
        var today = DateTime.Today;
        var products = LoadTodayProducts(deviceId, request.StationNo, today)
            .GroupBy(row => $"{row.WorkOrder}\u001F{row.ProductNo}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var snapshot = _dbContext.Db.Queryable<CenterDeviceStationRuntimeSnapshot>()
            .First(item => item.DeviceId == deviceId && item.StationNo == request.StationNo);
        snapshot ??= new CenterDeviceStationRuntimeSnapshot
        {
            DeviceId = deviceId,
            StationNo = request.StationNo
        };

        snapshot.CurrentWorkOrder = request.WorkOrder.Trim();
        snapshot.ProductJobNo = request.ProductJobNo.Trim();
        snapshot.ProductModel = request.ProductModel.Trim();
        snapshot.TodayTotalCount = products.Count;
        snapshot.TodayQualifiedCount = products.Count(IsProductOk);
        snapshot.TodayFailedCount = products.Count(item => !IsProductOk(item));
        snapshot.CollectedAt = DateTime.Now;
        snapshot.UpdatedAt = DateTime.Now;

        if (snapshot.Id > 0)
        {
            _dbContext.Db.Updateable(snapshot).ExecuteCommand();
        }
        else
        {
            _dbContext.Db.Insertable(snapshot).ExecuteCommand();
        }
    }

    private IReadOnlyList<CenterProductReportRow> LoadTodayProducts(string deviceId, int stationNo, DateTime reportDate)
    {
        var root = _settingsService.Get().DataDirectory;
        if (!Directory.Exists(root))
        {
            return [];
        }

        var rows = new List<CenterProductReportRow>();
        foreach (var filePath in Directory.EnumerateFiles(root, "*.xlsx", SearchOption.AllDirectories))
        {
            rows.AddRange(LoadExistingRows(filePath)
                .Where(row => row.StationNo == stationNo
                    && row.CompletedAt.Date == reportDate.Date
                    && string.Equals(row.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase)));
        }

        return rows;
    }

    private IReadOnlyList<CenterProductReportColumn> ResolveColumns(
        string reportPath,
        CenterProductReportRequest request)
    {
        var savedColumns = LoadExistingColumns(reportPath);
        var requestColumns = CenterProductReportFormat.FromDtos(request.ReportColumns);
        return CenterProductReportFormat.BuildDetailColumns(savedColumns.Concat(requestColumns));
    }

    private static IReadOnlyList<CenterProductReportRow> BuildRows(
        string deviceId,
        CenterProductReportRequest request)
    {
        return request.Points
            .OrderBy(point => point.SequenceNo)
            .Select(point => new CenterProductReportRow
            {
                DeviceId = deviceId,
                DeviceName = request.DeviceName.Trim(),
                SystemType = CenterTelemetryRules.NormalizeSystemType(request.SystemType),
                StationNo = request.StationNo,
                StationName = request.StationName.Trim(),
                WorkOrder = request.WorkOrder.Trim(),
                Batch = request.Batch.Trim(),
                Quantity = request.Quantity,
                PartName = request.PartName.Trim(),
                ProcessNo = request.ProcessNo.Trim(),
                OperatorNo = string.IsNullOrWhiteSpace(point.OperatorNo)
                    ? request.OperatorNo.Trim()
                    : point.OperatorNo.Trim(),
                ProductJobNo = request.ProductJobNo.Trim(),
                ProductNo = request.ProductNo.Trim(),
                ProductModel = request.ProductModel.Trim(),
                ProductResult = request.ProductResult.Trim(),
                SequenceNo = point.SequenceNo,
                TouchNo = point.TouchNo.Trim(),
                TestResult = point.TestResult.Trim(),
                CollectedAt = point.CollectedAt == default ? DateTime.Now : point.CollectedAt,
                CompletedAt = request.CompletedAt == default ? DateTime.Now : request.CompletedAt,
                RawDataJson = point.RawDataJson ?? string.Empty
            })
            .ToList();
    }

    private static void WriteReportWorksheet(
        XLWorkbook workbook,
        CenterProductReportRequest request,
        IReadOnlyList<CenterProductReportColumn> columns,
        IReadOnlyList<CenterProductReportRow> rows)
    {
        var worksheet = workbook.Worksheets.Add(CenterProductReportFormat.WorksheetName);
        var templateColumnCount = Math.Max(CenterProductReportFormat.TemplateMinimumColumnCount, columns.Count);
        WriteTemplateHeader(worksheet, request, templateColumnCount);
        for (var column = 0; column < columns.Count; column++)
        {
            worksheet.Cell(CenterProductReportFormat.DetailHeaderRow, column + 1).Value = columns[column].Title;
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            WriteReportRow(
                worksheet,
                rowIndex + CenterProductReportFormat.DetailFirstDataRow,
                rows[rowIndex],
                columns);
        }

        MergeRepeatedProductFields(worksheet, columns, rows);
        ApplyWorksheetStyle(worksheet, columns.Count, rows.Count, templateColumnCount);
    }

    /// <summary>
    /// 使用与设备端完全相同的客户模板块写入任务级信息。
    /// </summary>
    private static void WriteTemplateHeader(
        IXLWorksheet worksheet,
        CenterProductReportRequest request,
        int lastColumn)
    {
        var values = new CenterProductReportHeaderValues(
            request.ProductJobNo.Trim(),
            request.DrawingNo.Trim(),
            request.Batch.Trim(),
            request.WorkOrder.Trim(),
            request.Spec.Trim(),
            request.ProductModel.Trim(),
            request.ProcessNo.Trim(),
            request.Quantity,
            request.QualifiedQty,
            request.StartTime,
            request.EndTime,
            request.OperatorNo.Trim());
        foreach (var block in CenterProductReportFormat.BuildTemplateHeaderBlocks(values, lastColumn))
        {
            var range = worksheet.Range(block.Row, block.StartColumn, block.Row, block.EndColumn);
            range.Merge();
            range.FirstCell().Value = CenterProductReportFormat.BuildHeaderText(block.Label, block.Value);
        }
    }

    private static void WriteReportRow(
        IXLWorksheet worksheet,
        int rowNumber,
        CenterProductReportRow row,
        IReadOnlyList<CenterProductReportColumn> columns)
    {
        var values = BuildReportRow(row);
        foreach (var pair in ParseRawData(row.RawDataJson))
        {
            values.TryAdd(pair.Key, pair.Value);
        }

        for (var index = 0; index < columns.Count; index++)
        {
            values.TryGetValue(columns[index].Key, out var value);
            worksheet.Cell(rowNumber, index + 1).Value = value ?? string.Empty;
        }
    }

    private static Dictionary<string, string> BuildReportRow(CenterProductReportRow row)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [CenterProductReportFormat.ColumnStationNo] = string.IsNullOrWhiteSpace(row.StationName)
                ? row.StationNo.ToString()
                : row.StationName,
            [CenterProductReportFormat.ColumnProductNo] = row.ProductNo,
            [CenterProductReportFormat.ColumnProductResult] = row.ProductResult,
            [CenterProductReportFormat.ColumnTouchNo] = string.IsNullOrWhiteSpace(row.TouchNo)
                ? row.SequenceNo.ToString()
                : row.TouchNo,
            [CenterProductReportFormat.ColumnTouchResult] = row.TestResult,
            [CenterProductReportFormat.ColumnWorkOrder] = row.WorkOrder,
            [CenterProductReportFormat.ColumnBatch] = row.Batch,
            [CenterProductReportFormat.ColumnQuantity] = row.Quantity.ToString(),
            [CenterProductReportFormat.ColumnPartName] = row.PartName,
            [CenterProductReportFormat.ColumnProcessNo] = row.ProcessNo,
            [CenterProductReportFormat.ColumnOperator] = row.OperatorNo,
            [CenterProductReportFormat.ColumnRecordTime] = row.CompletedAt.ToString(CenterProductReportFormat.DateTimeFormat)
        };
    }

    private static void WriteDataWorksheet(
        XLWorkbook workbook,
        IReadOnlyList<CenterProductReportRow> rows)
    {
        var worksheet = workbook.Worksheets.Add(CenterProductReportFormat.DataWorksheetName);
        worksheet.Visibility = XLWorksheetVisibility.Hidden;
        var headers = CenterDataColumns.All;
        for (var index = 0; index < headers.Count; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var values = rows[rowIndex].ToDictionary();
            for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
            {
                values.TryGetValue(headers[columnIndex], out var value);
                worksheet.Cell(rowIndex + 2, columnIndex + 1).Value = value ?? string.Empty;
            }
        }
    }

    private static void WriteColumnsWorksheet(
        XLWorkbook workbook,
        IReadOnlyList<CenterProductReportColumn> columns)
    {
        var worksheet = workbook.Worksheets.Add(CenterProductReportFormat.ColumnsWorksheetName);
        worksheet.Visibility = XLWorksheetVisibility.Hidden;
        worksheet.Cell(1, 1).Value = "Key";
        worksheet.Cell(1, 2).Value = "Title";
        worksheet.Cell(1, 3).Value = "MergeByProduct";
        for (var index = 0; index < columns.Count; index++)
        {
            worksheet.Cell(index + 2, 1).Value = columns[index].Key;
            worksheet.Cell(index + 2, 2).Value = columns[index].Title;
            worksheet.Cell(index + 2, 3).Value = columns[index].MergeByProduct;
        }
    }

    private static IReadOnlyList<CenterProductReportRow> LoadExistingRows(string reportPath)
    {
        if (!File.Exists(reportPath))
        {
            return [];
        }

        try
        {
            using var workbook = new XLWorkbook(reportPath);
            var worksheet = workbook.Worksheets.FirstOrDefault(
                sheet => string.Equals(sheet.Name, CenterProductReportFormat.DataWorksheetName, StringComparison.OrdinalIgnoreCase));
            if (worksheet is null)
            {
                return [];
            }

            var rows = new List<CenterProductReportRow>();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                rows.Add(CenterProductReportRow.FromWorksheetRow(worksheet, rowNumber));
            }

            return rows;
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<CenterProductReportColumn> LoadExistingColumns(string reportPath)
    {
        if (!File.Exists(reportPath))
        {
            return [];
        }

        try
        {
            using var workbook = new XLWorkbook(reportPath);
            var worksheet = workbook.Worksheets.FirstOrDefault(
                sheet => string.Equals(sheet.Name, CenterProductReportFormat.ColumnsWorksheetName, StringComparison.OrdinalIgnoreCase));
            if (worksheet is null)
            {
                return [];
            }

            var columns = new List<CenterProductReportColumn>();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var key = worksheet.Cell(rowNumber, 1).GetString();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                columns.Add(new CenterProductReportColumn(
                    key.Trim(),
                    worksheet.Cell(rowNumber, 2).GetString(),
                    worksheet.Cell(rowNumber, 3).GetBoolean()));
            }

            return columns;
        }
        catch
        {
            return [];
        }
    }

    private static void ApplyWorksheetStyle(
        IXLWorksheet worksheet,
        int columnCount,
        int dataRowCount,
        int templateColumnCount)
    {
        var templateRange = worksheet.Range(1, 1, 7, templateColumnCount);
        templateRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        templateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        templateRange.Style.Alignment.WrapText = false;
        templateRange.Style.Alignment.ShrinkToFit = true;

        if (columnCount > 0)
        {
            var lastRow = Math.Max(
                CenterProductReportFormat.DetailHeaderRow,
                dataRowCount + CenterProductReportFormat.DetailHeaderRow);
            var usedRange = worksheet.Range(
                CenterProductReportFormat.DetailHeaderRow,
                1,
                lastRow,
                columnCount);
            usedRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            usedRange.Style.Alignment.WrapText = true;
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            var headerRange = worksheet.Range(
                CenterProductReportFormat.DetailHeaderRow,
                1,
                CenterProductReportFormat.DetailHeaderRow,
                columnCount);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E2F3");
            worksheet.SheetView.FreezeRows(CenterProductReportFormat.DetailHeaderRow);
            worksheet.Columns(1, columnCount).AdjustToContents();
        }

        for (var columnIndex = 1; columnIndex <= templateColumnCount; columnIndex++)
        {
            var column = worksheet.Column(columnIndex);
            column.Width = CenterProductReportFormat.ResolveTemplateColumnWidth(columnIndex, column.Width);
        }
    }

    private static void MergeRepeatedProductFields(
        IXLWorksheet worksheet,
        IReadOnlyList<CenterProductReportColumn> columns,
        IReadOnlyList<CenterProductReportRow> rows)
    {
        if (rows.Count <= 1)
        {
            return;
        }

        var mergeColumns = columns
            .Select((column, index) => new { Column = column, Index = index + 1 })
            .Where(item => item.Column.MergeByProduct)
            .Select(item => item.Index)
            .ToArray();

        var groupStartRow = CenterProductReportFormat.DetailFirstDataRow;
        var currentKey = BuildProductMergeKey(rows[0]);
        for (var recordIndex = 1; recordIndex < rows.Count; recordIndex++)
        {
            var key = BuildProductMergeKey(rows[recordIndex]);
            if (!string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
            {
                MergeProductColumns(
                    worksheet,
                    groupStartRow,
                    recordIndex + CenterProductReportFormat.DetailHeaderRow,
                    mergeColumns);
                groupStartRow = recordIndex + CenterProductReportFormat.DetailFirstDataRow;
                currentKey = key;
            }
        }

        MergeProductColumns(
            worksheet,
            groupStartRow,
            rows.Count + CenterProductReportFormat.DetailHeaderRow,
            mergeColumns);
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

    private static string BuildProductMergeKey(CenterProductReportRow row)
    {
        return CenterProductReportFormat.BuildProductMergeKey(row.StationNo, row.WorkOrder, row.ProductNo);
    }

    private static bool IsSameProduct(CenterProductReportRow row, CenterProductReportRequest request)
    {
        return row.StationNo == request.StationNo
            && string.Equals(row.WorkOrder, request.WorkOrder.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(row.ProductNo, request.ProductNo.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, string> ParseRawData(string? rawDataJson)
    {
        if (string.IsNullOrWhiteSpace(rawDataJson))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            using var document = JsonDocument.Parse(rawDataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>();
            }

            return document.RootElement.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString() ?? string.Empty
                        : property.Value.ToString(),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    private string BuildReportPath(string deviceId, CenterProductReportRequest request)
    {
        var root = _settingsService.Get().DataDirectory;
        var devicePart = SanitizeFileName(FirstNonEmpty(deviceId, "UnknownDevice"));
        var workOrderPart = SanitizeFileName(FirstNonEmpty(request.WorkOrder, "NoWorkOrder"));
        return Path.Combine(root, devicePart, $"{workOrderPart}.xlsx");
    }

    private static bool IsProductOk(CenterProductReportRow row)
        => string.Equals(row.ProductResult, ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase);

    private static string SanitizeFileName(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "NA" : value.Trim();
        return InvalidFileNameChars.Replace(normalized, "-");
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private sealed class CenterProductReportRow
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string SystemType { get; set; } = string.Empty;
        public int StationNo { get; set; } = 1;
        public string StationName { get; set; } = string.Empty;
        public string WorkOrder { get; set; } = string.Empty;
        public string Batch { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string PartName { get; set; } = string.Empty;
        public string ProcessNo { get; set; } = string.Empty;
        public string OperatorNo { get; set; } = string.Empty;
        public string ProductJobNo { get; set; } = string.Empty;
        public string ProductNo { get; set; } = string.Empty;
        public string ProductModel { get; set; } = string.Empty;
        public string ProductResult { get; set; } = string.Empty;
        public int SequenceNo { get; set; }
        public string TouchNo { get; set; } = string.Empty;
        public string TestResult { get; set; } = string.Empty;
        public DateTime CollectedAt { get; set; } = DateTime.Now;
        public DateTime CompletedAt { get; set; } = DateTime.Now;
        public string RawDataJson { get; set; } = string.Empty;

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [CenterDataColumns.DeviceId] = DeviceId,
                [CenterDataColumns.DeviceName] = DeviceName,
                [CenterDataColumns.SystemType] = SystemType,
                [CenterDataColumns.StationNo] = StationNo.ToString(),
                [CenterDataColumns.StationName] = StationName,
                [CenterDataColumns.WorkOrder] = WorkOrder,
                [CenterDataColumns.Batch] = Batch,
                [CenterDataColumns.Quantity] = Quantity.ToString(),
                [CenterDataColumns.PartName] = PartName,
                [CenterDataColumns.ProcessNo] = ProcessNo,
                [CenterDataColumns.OperatorNo] = OperatorNo,
                [CenterDataColumns.ProductJobNo] = ProductJobNo,
                [CenterDataColumns.ProductNo] = ProductNo,
                [CenterDataColumns.ProductModel] = ProductModel,
                [CenterDataColumns.ProductResult] = ProductResult,
                [CenterDataColumns.SequenceNo] = SequenceNo.ToString(),
                [CenterDataColumns.TouchNo] = TouchNo,
                [CenterDataColumns.TestResult] = TestResult,
                [CenterDataColumns.CollectedAt] = CollectedAt.ToString("O"),
                [CenterDataColumns.CompletedAt] = CompletedAt.ToString("O"),
                [CenterDataColumns.RawDataJson] = RawDataJson
            };
        }

        public static CenterProductReportRow FromWorksheetRow(IXLWorksheet worksheet, int rowNumber)
        {
            var row = new CenterProductReportRow
            {
                DeviceId = Get(worksheet, rowNumber, CenterDataColumns.DeviceId),
                DeviceName = Get(worksheet, rowNumber, CenterDataColumns.DeviceName),
                SystemType = Get(worksheet, rowNumber, CenterDataColumns.SystemType),
                StationNo = GetInt(worksheet, rowNumber, CenterDataColumns.StationNo, 1),
                StationName = Get(worksheet, rowNumber, CenterDataColumns.StationName),
                WorkOrder = Get(worksheet, rowNumber, CenterDataColumns.WorkOrder),
                Batch = Get(worksheet, rowNumber, CenterDataColumns.Batch),
                Quantity = GetInt(worksheet, rowNumber, CenterDataColumns.Quantity, 0),
                PartName = Get(worksheet, rowNumber, CenterDataColumns.PartName),
                ProcessNo = Get(worksheet, rowNumber, CenterDataColumns.ProcessNo),
                OperatorNo = Get(worksheet, rowNumber, CenterDataColumns.OperatorNo),
                ProductJobNo = Get(worksheet, rowNumber, CenterDataColumns.ProductJobNo),
                ProductNo = Get(worksheet, rowNumber, CenterDataColumns.ProductNo),
                ProductModel = Get(worksheet, rowNumber, CenterDataColumns.ProductModel),
                ProductResult = Get(worksheet, rowNumber, CenterDataColumns.ProductResult),
                SequenceNo = GetInt(worksheet, rowNumber, CenterDataColumns.SequenceNo, 0),
                TouchNo = Get(worksheet, rowNumber, CenterDataColumns.TouchNo),
                TestResult = Get(worksheet, rowNumber, CenterDataColumns.TestResult),
                CollectedAt = GetDate(worksheet, rowNumber, CenterDataColumns.CollectedAt),
                CompletedAt = GetDate(worksheet, rowNumber, CenterDataColumns.CompletedAt),
                RawDataJson = Get(worksheet, rowNumber, CenterDataColumns.RawDataJson)
            };
            return row;
        }

        private static string Get(IXLWorksheet worksheet, int rowNumber, string columnName)
        {
            var columnIndex = CenterDataColumns.IndexOf(columnName) + 1;
            return worksheet.Cell(rowNumber, columnIndex).GetString();
        }

        private static int GetInt(IXLWorksheet worksheet, int rowNumber, string columnName, int fallback)
        {
            return int.TryParse(Get(worksheet, rowNumber, columnName), out var value) ? value : fallback;
        }

        private static DateTime GetDate(IXLWorksheet worksheet, int rowNumber, string columnName)
        {
            return DateTime.TryParse(Get(worksheet, rowNumber, columnName), out var value)
                ? value
                : DateTime.Now;
        }
    }

    private static class CenterDataColumns
    {
        public const string DeviceId = "DeviceId";
        public const string DeviceName = "DeviceName";
        public const string SystemType = "SystemType";
        public const string StationNo = "StationNo";
        public const string StationName = "StationName";
        public const string WorkOrder = "WorkOrder";
        public const string Batch = "Batch";
        public const string Quantity = "Quantity";
        public const string PartName = "PartName";
        public const string ProcessNo = "ProcessNo";
        public const string OperatorNo = "OperatorNo";
        public const string ProductJobNo = "ProductJobNo";
        public const string ProductNo = "ProductNo";
        public const string ProductModel = "ProductModel";
        public const string ProductResult = "ProductResult";
        public const string SequenceNo = "SequenceNo";
        public const string TouchNo = "TouchNo";
        public const string TestResult = "TestResult";
        public const string CollectedAt = "CollectedAt";
        public const string CompletedAt = "CompletedAt";
        public const string RawDataJson = "RawDataJson";

        public static readonly IReadOnlyList<string> All =
        [
            DeviceId,
            DeviceName,
            SystemType,
            StationNo,
            StationName,
            WorkOrder,
            Batch,
            Quantity,
            PartName,
            ProcessNo,
            OperatorNo,
            ProductJobNo,
            ProductNo,
            ProductModel,
            ProductResult,
            SequenceNo,
            TouchNo,
            TestResult,
            CollectedAt,
            CompletedAt,
            RawDataJson
        ];

        public static int IndexOf(string columnName)
        {
            for (var index = 0; index < All.Count; index++)
            {
                if (string.Equals(All[index], columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            throw new InvalidOperationException($"Center report data column is missing: {columnName}");
        }
    }
}
