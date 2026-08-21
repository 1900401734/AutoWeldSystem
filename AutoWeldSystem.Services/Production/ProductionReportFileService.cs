using AutoWeldSystem.Core.Center;
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
    private const string ReportRoleActual = "actual";
    private const string ReportRoleUpper = "upper";
    private const string ReportRoleLower = "lower";
    private const string ReportRoleResult = "result";
    private const string HeaderStationNo = "工位";
    private const string HeaderProductNo = "产品编号";
    private const string HeaderProductResult = "产品结果";
    private const string HeaderTouchNo = "焊点编号";
    private const string HeaderTouchResult = "焊点结果";
    private const string ReportFormat = "XLSX";
    private const int DetailHeaderRow = CenterProductReportFormat.DetailHeaderRow;
    private const int DetailFirstDataRow = DetailHeaderRow + 1;

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
            // 生成报表前必须重读任务，确保完工后持久化的 EndTime 和统计进入最终文件。
            var latestTask = ProductionReportFileRules.ResolveLatestTask(
                task,
                taskId => _dbContext.Db.Queryable<BizWeldTask>().InSingle(taskId));
            var report = GetOrCreateReportRecord(latestTask);
            var records = _dbContext.Db.Queryable<BizWeldPointRecord>()
                .Where(record => record.TaskId == latestTask.Id)
                .ToList()
                .OrderBy(record => record.ProductNo, NaturalSortComparer.Instance)
                .ThenBy(record => record.StationNo)
                .ThenBy(record => record.SequenceNo)
                .ToList();

            Directory.CreateDirectory(Path.GetDirectoryName(report.FilePath)!);
            WriteXlsx(report.FilePath, BuildReportSchema(latestTask, records), records, latestTask);

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

    private ReportSchema BuildReportSchema(
        BizWeldTask task,
        IReadOnlyList<BizWeldPointRecord> records)
    {
        var stationConfigs = ResolveStationReportConfigs(task, records);
        return BuildReportSchemaForStationsWithDeviceType(stationConfigs, CurrentSettings.ProcessParameterDeviceType);
    }

    /// <summary>
    /// 按工位顺序构造稳定、去重的动态列并集，同时保留每个工位自己的取值配置。
    /// </summary>
    private static ReportSchema BuildReportSchemaForStations(
        IReadOnlyList<ResolvedStationReportConfig> stationConfigs)
        => BuildReportSchemaForStationsWithDeviceType(stationConfigs, string.Empty);

    private static ReportSchema BuildReportSchemaForStationsWithDeviceType(
        IReadOnlyList<ResolvedStationReportConfig> stationConfigs,
        string deviceType)
    {
        var orderedConfigs = stationConfigs
            .OrderBy(config => config.StationNo)
            .ToList();
        var displayOptions = ResolveCompatibleDisplayOptions(
            orderedConfigs,
            WholePieceAbAggregationRules.IsApplicable(deviceType, orderedConfigs.FirstOrDefault()?.Config.TouchCount ?? 0));
        var leadingColumns = BuildLeadingColumns(displayOptions);
        var dynamicColumns = orderedConfigs
            .SelectMany(config => config.SchemeItems.SelectMany(item => BuildItemColumnsForMode(
                item,
                WholePieceAbAggregationRules.IsApplicable(deviceType, config.Config.TouchCount))));
        var pointResultColumn = BuildPointResultColumn(displayOptions);
        var trailingColumns = BuildTrailingColumns();
        var columns = leadingColumns
            .Concat(dynamicColumns)
            .Concat(pointResultColumn)
            .Concat(trailingColumns)
            .DistinctBy(column => column.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var stationSchemeItems = orderedConfigs.ToDictionary(
            config => config.StationNo,
            config => config.SchemeItems);

        var stationConfigsByNumber = orderedConfigs.ToDictionary(
            config => config.StationNo,
            config => config.Config);
        return new ReportSchema(columns, stationSchemeItems, stationConfigsByNumber, displayOptions);
    }

    private void WriteXlsx(
        string filePath,
        ReportSchema schema,
        IReadOnlyList<BizWeldPointRecord> records,
        BizWeldTask task)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(CenterProductReportFormat.WorksheetName);
        var settings = CurrentSettings;
        var detailColumns = ResolveDetailColumns(schema.Columns, settings.EnableDualStation);
        var templateColumnCount = Math.Max(CenterProductReportFormat.TemplateMinimumColumnCount, detailColumns.Count);
        var stationNames = StationDisplayNameRules.NormalizeForLoad(
            settings.EnableDualStation,
            settings.Station1DisplayName,
            settings.Station2DisplayName);

        WriteTemplateHeader(worksheet, task, templateColumnCount);
        var outputRows = BuildOutputRows(schema, records, settings);
        WriteDetailHeader(worksheet, detailColumns);
        WriteDataRows(worksheet, schema, detailColumns, outputRows, stationNames);
        MergeRepeatedProductFields(worksheet, detailColumns, outputRows);
        ApplyWorksheetStyle(worksheet, detailColumns.Count, outputRows.Count, templateColumnCount);
        workbook.SaveAs(filePath);
    }

    /// <summary>
    /// 写入客户模板的多行任务信息区。最后一组值会扩展到实际报表末列。
    /// </summary>
    private static void WriteTemplateHeader(IXLWorksheet worksheet, BizWeldTask task, int lastColumn)
    {
        var values = new CenterProductReportHeaderValues(
            task.ProductNum,
            task.DrawingNo,
            task.Batch,
            task.SN,
            task.Spec,
            task.ProductModel,
            task.ProductName,
            task.ProcessName,
            task.ProcessNo,
            task.StartAmount,
            task.QualifiedQty,
            task.StartTime,
            task.EndTime,
            task.UserNumber ?? string.Empty);
        foreach (var block in CenterProductReportFormat.BuildTemplateHeaderBlocks(values, lastColumn))
        {
            WriteHeaderBlock(
                worksheet,
                block.Row,
                block.StartColumn,
                block.EndColumn,
                block.Label,
                block.Value);
        }
    }

    /// <inheritdoc />
    public bool ShouldUploadReportFile(BizWeldTask task)
    {
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            var latestTask = ProductionReportFileRules.ResolveLatestTask(
                task,
                taskId => _dbContext.Db.Queryable<BizWeldTask>().InSingle(taskId));
            var records = _dbContext.Db.Queryable<BizWeldPointRecord>()
                .Where(record => record.TaskId == latestTask.Id)
                .ToList();
            var reportDetails = ResolveStationReportConfigs(latestTask, records)
                .SelectMany(config => config.SchemeItems)
                .Select(item => item.Detail);
            return ReportFileUploadRules.ShouldUploadReportFile(reportDetails);
        }
    }

    /// <summary>
    /// 按客户模板合并整块公共字段，并在锚点单元格写入“标签 + 值”。
    /// </summary>
    private static void WriteHeaderBlock(
        IXLWorksheet worksheet,
        int row,
        int startColumn,
        int endColumn,
        string label,
        object? value)
    {
        var range = worksheet.Range(row, startColumn, row, Math.Max(startColumn, endColumn));
        range.Merge();
        range.FirstCell().Value = CenterProductReportFormat.BuildHeaderText(label, value);
    }

    /// <summary>
    /// 写入第十一行明细表头。
    /// </summary>
    private static void WriteDetailHeader(IXLWorksheet worksheet, IReadOnlyList<ReportColumn> columns)
    {
        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            worksheet.Cell(DetailHeaderRow, columnIndex + 1).Value = columns[columnIndex].Title;
        }
    }

    private IReadOnlyList<ReportOutputRow> BuildOutputRows(
        ReportSchema schema,
        IReadOnlyList<BizWeldPointRecord> records,
        AppSettings settings)
    {
        var rows = new List<ReportOutputRow>();
        foreach (var group in records.GroupBy(BuildProductMergeKey, StringComparer.OrdinalIgnoreCase))
        {
            var representative = group.OrderBy(record => record.SequenceNo).ThenBy(record => record.Id).First();
            var config = schema.ResolveConfig(representative.StationNo);
            var schemeItems = schema.ResolveSchemeItems(representative.StationNo);
            if (!WholePieceAbAggregationRules.IsApplicable(settings.ProcessParameterDeviceType, config?.TouchCount ?? 0))
            {
                var standardProductResult = ResolveProductResult(group);
                rows.AddRange(group.OrderBy(record => record.SequenceNo).ThenBy(record => record.Id)
                    .Select(record => BuildStandardOutputRow(record, schemeItems, standardProductResult)));
                continue;
            }

            var definitions = schemeItems
                .Where(item => SchemeDetailRoleRules.IsReportEnabled(item.Detail, SchemeDetailValueRole.Actual))
                .Select(item => new WholePieceAbValueDefinition(
                    item.Item.ItemId,
                    item.Item.ItemName,
                    BuildDynamicColumnKey(item.Item, ReportRoleActual),
                    item.Item.ActualExpression))
                .ToList();
            var aggregation = WholePieceAbAggregationRules.Aggregate(
                group,
                definitions,
                settings.EnablePlcStringNumericFormatting ?? true,
                settings.PlcStringNumericFormatMode);
            if (!aggregation.IsSuccess)
            {
                throw new InvalidOperationException(aggregation.ErrorMessage);
            }

            var productResult = ResolveProductResult(group);
            foreach (var output in aggregation.Rows)
            {
                rows.Add(new ReportOutputRow(
                    representative,
                    output.SideNo,
                    output.Result,
                    productResult,
                    output.Values));
            }
        }

        return rows
            .OrderBy(row => row.Source.ProductNo, NaturalSortComparer.Instance)
            .ThenBy(row => row.Source.StationNo)
            .ThenBy(row => row.Source.SequenceNo)
            .ThenBy(row => row.PointNo, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ReportOutputRow BuildStandardOutputRow(
        BizWeldPointRecord record,
        IReadOnlyList<SchemeReportItem> schemeItems,
        string productResult)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddSchemeDynamicValues(values, ParseRawData(record.RawDataJson), schemeItems);
        return new ReportOutputRow(
            record,
            string.IsNullOrWhiteSpace(record.TouchNo) ? record.SequenceNo.ToString() : record.TouchNo,
            record.TestResult,
            productResult,
            values);
    }

    private void WriteDataRows(
        IXLWorksheet worksheet,
        ReportSchema schema,
        IReadOnlyList<ReportColumn> detailColumns,
        IReadOnlyList<ReportOutputRow> rows,
        StationDisplayNames stationNames)
    {
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var output = rows[rowIndex];
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ColumnStationNo] = ResolveStationDisplayName(output.Source.StationNo, stationNames),
                [ColumnProductNo] = output.Source.ProductNo,
                [ColumnProductResult] = output.ProductResult,
                [ColumnTouchNo] = output.PointNo,
                [ColumnTouchResult] = output.PointResult
            };
            foreach (var pair in output.DynamicValues)
            {
                row[pair.Key] = pair.Value;
            }

            for (var columnIndex = 0; columnIndex < detailColumns.Count; columnIndex++)
            {
                var column = detailColumns[columnIndex];
                worksheet.Cell(rowIndex + DetailFirstDataRow, columnIndex + 1).Value = row.TryGetValue(column.Key, out var value)
                    ? value
                    : string.Empty;
            }
        }
    }

    private static void MergeRepeatedProductFields(
        IXLWorksheet worksheet,
        IReadOnlyList<ReportColumn> columns,
        IReadOnlyList<ReportOutputRow> rows)
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
        var groupStartRow = DetailFirstDataRow;
        var currentKey = BuildProductMergeKey(rows[0].Source);
        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var key = BuildProductMergeKey(rows[rowIndex].Source);
            if (!string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
            {
                MergeProductColumns(worksheet, groupStartRow, rowIndex + DetailHeaderRow, mergeColumns);
                groupStartRow = rowIndex + DetailFirstDataRow;
                currentKey = key;
            }
        }

        MergeProductColumns(worksheet, groupStartRow, rows.Count + DetailHeaderRow, mergeColumns);
    }

    private static void ApplyWorksheetStyle(
        IXLWorksheet worksheet,
        int detailColumnCount,
        int dataRowCount,
        int templateColumnCount)
    {
        var templateRange = worksheet.Range(1, 1, 9, templateColumnCount);
        templateRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        templateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        templateRange.Style.Alignment.WrapText = false;
        templateRange.Style.Alignment.ShrinkToFit = true;

        if (detailColumnCount <= 0)
        {
            ApplyTemplateDimensions(worksheet, templateColumnCount);
            return;
        }

        var lastRow = Math.Max(DetailHeaderRow, dataRowCount + DetailHeaderRow);
        var usedRange = worksheet.Range(DetailHeaderRow, 1, lastRow, detailColumnCount);
        usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        usedRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        usedRange.Style.Alignment.WrapText = true;

        var headerRange = worksheet.Range(DetailHeaderRow, 1, DetailHeaderRow, detailColumnCount);
        worksheet.Row(DetailHeaderRow).Height = 27d;
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E2F3");
        worksheet.SheetView.FreezeRows(DetailHeaderRow);
        worksheet.Columns(1, detailColumnCount).AdjustToContents();
        ApplyTemplateDimensions(worksheet, templateColumnCount);
    }

    /// <summary>
    /// 固定客户模板的标签和值列宽，避免中文标签被压成纵向多行。
    /// A:J 使用客户模板原始列宽；动态列超过 J 时为新增列设置可读宽度。
    /// </summary>
    private static void ApplyTemplateDimensions(IXLWorksheet worksheet, int templateColumnCount)
    {
        for (var columnIndex = 1; columnIndex <= templateColumnCount; columnIndex++)
        {
            var column = worksheet.Column(columnIndex);
            column.Width = CenterProductReportFormat.ResolveTemplateColumnWidth(columnIndex, column.Width);
        }
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

        var groupStartRow = DetailFirstDataRow;
        var currentKey = BuildProductMergeKey(records[0]);
        for (var recordIndex = 1; recordIndex < records.Count; recordIndex++)
        {
            var key = BuildProductMergeKey(records[recordIndex]);
            if (!string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
            {
                MergeProductColumns(worksheet, groupStartRow, recordIndex + DetailHeaderRow, mergeColumns);
                groupStartRow = recordIndex + DetailFirstDataRow;
                currentKey = key;
            }
        }

        MergeProductColumns(worksheet, groupStartRow, records.Count + DetailHeaderRow, mergeColumns);
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
                    ResolveProductResult(group)),
                StringComparer.OrdinalIgnoreCase);
    }

    private static ProductReportContext ResolveProductContext(
        BizWeldPointRecord record,
        IReadOnlyDictionary<string, ProductReportContext> contexts)
    {
        return contexts.TryGetValue(BuildProductMergeKey(record), out var context)
            ? context
            : new ProductReportContext(ResolveProductResult([record]));
    }

    /// <summary>
    /// 产品结果只读取 PLC 产品级字段；旧记录为空时回退 RawDataJson.product_result。
    /// 禁止根据焊点 TestResult 聚合推算产品结果。
    /// </summary>
    private static string ResolveProductResult(IEnumerable<BizWeldPointRecord> records)
    {
        var recordList = records.ToList();
        var persistedResult = recordList
            .Select(record => record.ProductResult)
            .FirstOrDefault(result => !string.IsNullOrWhiteSpace(result));
        if (!string.IsNullOrWhiteSpace(persistedResult))
        {
            return TestResultRules.Normalize(persistedResult);
        }

        foreach (var record in recordList)
        {
            var rawProductResult = GetRawValue(ParseRawData(record.RawDataJson), ColumnProductResult);
            if (!string.IsNullOrWhiteSpace(rawProductResult))
            {
                return TestResultRules.Normalize(rawProductResult);
            }
        }

        return ProductionConstants.TestResults.Unknown;
    }

    private Dictionary<string, string> BuildRow(
        BizWeldPointRecord record,
        ProductReportContext productContext,
        ReportSchema schema,
        StationDisplayNames stationNames)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [ColumnStationNo] = ResolveStationDisplayName(record.StationNo, stationNames),
            [ColumnProductNo] = record.ProductNo,
            [ColumnProductResult] = productContext.ProductResult,
            [ColumnTouchNo] = string.IsNullOrWhiteSpace(record.TouchNo) ? record.SequenceNo.ToString() : record.TouchNo,
            [ColumnTouchResult] = record.TestResult
        };

        AddDynamicValues(row, record, schema);
        return row;
    }

    private static string ResolveStationDisplayName(int stationNo, StationDisplayNames stationNames)
    {
        return stationNo == 2
            ? stationNames.Station2
            : stationNames.Station1;
    }

    private void AddDynamicValues(Dictionary<string, string> row, BizWeldPointRecord record, ReportSchema schema)
    {
        var rawValues = ParseRawData(record.RawDataJson);
        AddSchemeDynamicValues(row, rawValues, schema.ResolveSchemeItems(record.StationNo));
    }

    private static void AddSchemeDynamicValues(
        Dictionary<string, string> row,
        IReadOnlyDictionary<string, string> rawValues,
        IReadOnlyList<SchemeReportItem> schemeItems)
    {
        foreach (var schemeItem in schemeItems)
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

    /// <summary>
    /// 根据任务实际产生记录的工位选择各自配置；无记录时才回退任务工位。
    /// </summary>
    private IReadOnlyList<ResolvedStationReportConfig> ResolveStationReportConfigs(
        BizWeldTask task,
        IReadOnlyList<BizWeldPointRecord> records)
    {
        var productNum = ResolveTaskProductNum(task);
        if (string.IsNullOrWhiteSpace(productNum))
        {
            return [];
        }

        var configs = _dbContext.Db.Queryable<BizProductProcessConfig>()
            .Where(config => config.Enabled && config.ProductNum == productNum)
            .ToList()
            .OrderBy(config => config.Id)
            .ToList();
        var schemeItemsBySchemeId = new Dictionary<string, IReadOnlyList<SchemeReportItem>>(StringComparer.OrdinalIgnoreCase);
        var resolved = new List<ResolvedStationReportConfig>();
        foreach (var stationNo in ResolveReportStationNumbers(task, records))
        {
            var config = configs
                .Where(candidate => candidate.StationNo == ProductionConstants.Stations.SharedStationNo
                    || candidate.StationNo == stationNo)
                .OrderByDescending(candidate => candidate.StationNo == stationNo)
                .ThenBy(candidate => candidate.Id)
                .FirstOrDefault();
            if (config is null)
            {
                continue;
            }

            if (!schemeItemsBySchemeId.TryGetValue(config.SchemeId, out var schemeItems))
            {
                schemeItems = GetSchemeItemsForConfig(config);
                schemeItemsBySchemeId[config.SchemeId] = schemeItems;
            }

            resolved.Add(new ResolvedStationReportConfig(stationNo, config, schemeItems));
        }

        return resolved;
    }

    private static IReadOnlyList<int> ResolveReportStationNumbers(
        BizWeldTask task,
        IReadOnlyList<BizWeldPointRecord> records)
    {
        var stationNumbers = records
            .Select(record => NormalizeStationNo(record.StationNo))
            .Distinct()
            .OrderBy(stationNo => stationNo)
            .ToList();
        return stationNumbers.Count > 0
            ? stationNumbers
            : [NormalizeStationNo(task.StationNo)];
    }

    private static int NormalizeStationNo(int stationNo)
        => stationNo <= ProductionConstants.Stations.SharedStationNo
            ? ProductionConstants.Stations.DefaultStationNo
            : stationNo;

    /// <summary>
    /// 同一设备同一任务只能使用一组采集点标题，冲突时拒绝生成，避免静默错标。
    /// </summary>
    private static ReportDisplayOptions ResolveCompatibleDisplayOptions(
        IReadOnlyList<ResolvedStationReportConfig> stationConfigs,
        bool wholePieceInspection)
    {
        if (stationConfigs.Count == 0)
        {
            return ReportDisplayOptions.FromConfig(null, wholePieceInspection);
        }

        var options = stationConfigs
            .Select(config => ReportDisplayOptions.FromConfig(config.Config, wholePieceInspection))
            .Distinct()
            .ToList();
        if (options.Count > 1)
        {
            var stations = string.Join(", ", stationConfigs.Select(config => config.StationNo));
            throw new InvalidOperationException($"同一任务的工位采集点表头不一致，无法安全生成报表。工位：{stations}。");
        }

        return options[0];
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
        yield return new ReportColumn(ColumnTouchNo, displayOptions.PointNoHeader, MergeByProduct: false);
    }

    private static IEnumerable<ReportColumn> BuildPointResultColumn(ReportDisplayOptions displayOptions)
    {
        yield return new ReportColumn(ColumnTouchResult, displayOptions.PointResultHeader, MergeByProduct: false);
    }

    private static IEnumerable<ReportColumn> BuildTrailingColumns()
    {
        yield return new ReportColumn(ColumnProductResult, HeaderProductResult, MergeByProduct: true);
    }

    /// <summary>
    /// 单工位模式完全移除工位列；双工位模式保留并使用配置显示名称。
    /// </summary>
    private static IReadOnlyList<ReportColumn> ResolveDetailColumns(
        IReadOnlyList<ReportColumn> columns,
        bool enableDualStation)
    {
        return columns
            .Where(column => enableDualStation
                || !string.Equals(column.Key, ColumnStationNo, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static IEnumerable<ReportColumn> BuildItemColumns(SchemeReportItem schemeItem)
        => BuildItemColumnsForMode(schemeItem, wholePieceAb: false);

    private static IEnumerable<ReportColumn> BuildItemColumnsForMode(SchemeReportItem schemeItem, bool wholePieceAb)
    {
        var item = schemeItem.Item;
        var detail = schemeItem.Detail;

        if (SchemeDetailRoleRules.ShouldWriteReportRole(detail, SchemeDetailValueRole.Actual))
        {
            yield return new ReportColumn(
                BuildDynamicColumnKey(item, ReportRoleActual),
                TestItemUnitFormatRules.FormatHeader(SchemeDetailRoleRules.ResolveHeader(detail, item, SchemeDetailValueRole.Actual), item.Unit, SchemeDetailValueRole.Actual),
                MergeByProduct: false);
        }

        if (wholePieceAb)
        {
            yield break;
        }

        if (SchemeDetailRoleRules.ShouldWriteReportRole(detail, SchemeDetailValueRole.Upper))
        {
            yield return new ReportColumn(
                BuildDynamicColumnKey(item, ReportRoleUpper),
                TestItemUnitFormatRules.FormatHeader(SchemeDetailRoleRules.ResolveHeader(detail, item, SchemeDetailValueRole.Upper), item.Unit, SchemeDetailValueRole.Upper),
                MergeByProduct: false);
        }

        if (SchemeDetailRoleRules.ShouldWriteReportRole(detail, SchemeDetailValueRole.Lower))
        {
            yield return new ReportColumn(
                BuildDynamicColumnKey(item, ReportRoleLower),
                TestItemUnitFormatRules.FormatHeader(SchemeDetailRoleRules.ResolveHeader(detail, item, SchemeDetailValueRole.Lower), item.Unit, SchemeDetailValueRole.Lower),
                MergeByProduct: false);
        }

        if (SchemeDetailRoleRules.ShouldWriteReportRole(detail, SchemeDetailValueRole.Result))
        {
            yield return new ReportColumn(
                BuildDynamicColumnKey(item, ReportRoleResult),
                TestItemUnitFormatRules.FormatHeader(SchemeDetailRoleRules.ResolveHeader(detail, item, SchemeDetailValueRole.Result), item.Unit, SchemeDetailValueRole.Result),
                MergeByProduct: false);
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

    private static string SanitizePathPart(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "NA" : value.Trim();
        return Regex.Replace(normalized, @"[\\/:*?""<>|]+", "-");
    }

    private static bool HasAnyEnabledRole(BizSchemeDetail detail)
    {
        return SchemeDetailRoleRules.AllRoles.Any(role => SchemeDetailRoleRules.ShouldWriteReportRole(detail, role));
    }

    private sealed record ProductReportContext(string ProductResult);

    private sealed record ReportSchema
    {
        public ReportSchema(
            IReadOnlyList<ReportColumn> columns,
            IReadOnlyList<SchemeReportItem> schemeItems,
            ReportDisplayOptions displayOptions)
            : this(
                columns,
                new Dictionary<int, IReadOnlyList<SchemeReportItem>>
                {
                    [ProductionConstants.Stations.SharedStationNo] = schemeItems
                },
                new Dictionary<int, BizProductProcessConfig>(),
                displayOptions)
        {
        }

        public ReportSchema(
            IReadOnlyList<ReportColumn> columns,
            IReadOnlyDictionary<int, IReadOnlyList<SchemeReportItem>> stationSchemeItems,
            IReadOnlyDictionary<int, BizProductProcessConfig> stationConfigs,
            ReportDisplayOptions displayOptions)
        {
            Columns = columns;
            StationSchemeItems = stationSchemeItems;
            StationConfigs = stationConfigs;
            DisplayOptions = displayOptions;
        }

        public IReadOnlyList<ReportColumn> Columns { get; }

        public IReadOnlyDictionary<int, IReadOnlyList<SchemeReportItem>> StationSchemeItems { get; }

        public IReadOnlyDictionary<int, BizProductProcessConfig> StationConfigs { get; }

        public ReportDisplayOptions DisplayOptions { get; }

        public BizProductProcessConfig? ResolveConfig(int stationNo)
        {
            var normalizedStationNo = NormalizeStationNo(stationNo);
            if (StationConfigs.TryGetValue(normalizedStationNo, out var config))
            {
                return config;
            }

            return StationConfigs.TryGetValue(ProductionConstants.Stations.SharedStationNo, out var sharedConfig)
                ? sharedConfig
                : null;
        }

        public IReadOnlyList<SchemeReportItem> ResolveSchemeItems(int stationNo)
        {
            var normalizedStationNo = NormalizeStationNo(stationNo);
            if (StationSchemeItems.TryGetValue(normalizedStationNo, out var stationItems))
            {
                return stationItems;
            }

            return StationSchemeItems.TryGetValue(ProductionConstants.Stations.SharedStationNo, out var sharedItems)
                ? sharedItems
                : [];
        }
    }

    private sealed record ReportColumn(string Key, string Title, bool MergeByProduct);

    private sealed record ReportDisplayOptions(string PointNoHeader, string PointResultHeader)
    {
        public static ReportDisplayOptions FromConfig(BizProductProcessConfig? config, bool wholePieceInspection)
        {
            if (config is null)
            {
                return wholePieceInspection
                    ? new ReportDisplayOptions("检测面", "检测结果")
                    : new ReportDisplayOptions(HeaderTouchNo, HeaderTouchResult);
            }

            return new ReportDisplayOptions(
                CenterProductReportFormat.ResolvePointNoTitle(config.PointNoHeader, wholePieceInspection),
                CenterProductReportFormat.ResolvePointResultTitle(config.PointResultHeader, wholePieceInspection));
        }
    }

    private sealed record SchemeReportItem(DimTestItem Item, BizSchemeDetail Detail);

    private sealed record ReportOutputRow(
        BizWeldPointRecord Source,
        string PointNo,
        string PointResult,
        string ProductResult,
        IReadOnlyDictionary<string, string> DynamicValues);

    private sealed record ResolvedStationReportConfig(
        int StationNo,
        BizProductProcessConfig Config,
        IReadOnlyList<SchemeReportItem> SchemeItems);

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }
}
