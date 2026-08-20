using System.Text.Json;
using AutoWeldSystem.Core.Center;
using ClosedXML.Excel;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 按共享客户模板写入可见页，并同步生成数据、列定义和任务元数据隐藏页。
/// </summary>
internal sealed class CenterProductReportWorkbookWriter
{
    /// <summary>
    /// 将完整报表状态写入一个全新的工作簿实例。
    /// </summary>
    public void Populate(
        XLWorkbook workbook,
        CenterProductReportTaskState taskState,
        IReadOnlyList<CenterProductReportColumn> columns,
        IReadOnlyList<CenterProductReportStoredRow> rows)
    {
        WriteVisibleWorksheet(workbook, taskState, columns, rows);
        WriteDataWorksheet(workbook, rows);
        WriteColumnsWorksheet(workbook, columns);
        WriteTaskWorksheet(workbook, taskState);
    }

    private static void WriteVisibleWorksheet(
        XLWorkbook workbook,
        CenterProductReportTaskState taskState,
        IReadOnlyList<CenterProductReportColumn> columns,
        IReadOnlyList<CenterProductReportStoredRow> rows)
    {
        var worksheet = workbook.Worksheets.Add(CenterProductReportFormat.WorksheetName);
        var templateColumnCount = Math.Max(CenterProductReportFormat.TemplateMinimumColumnCount, columns.Count);
        WriteTemplateHeader(worksheet, taskState, templateColumnCount);
        for (var column = 0; column < columns.Count; column++)
        {
            worksheet.Cell(CenterProductReportFormat.DetailHeaderRow, column + 1).Value = columns[column].Title;
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            WriteDetailRow(
                worksheet,
                rowIndex + CenterProductReportFormat.DetailFirstDataRow,
                rows[rowIndex],
                columns);
        }

        MergeRepeatedProductFields(worksheet, columns, rows);
        ApplyStyle(worksheet, columns.Count, rows.Count, templateColumnCount);
    }

    private static void WriteTemplateHeader(
        IXLWorksheet worksheet,
        CenterProductReportTaskState taskState,
        int lastColumn)
    {
        foreach (var block in CenterProductReportFormat.BuildTemplateHeaderBlocks(taskState.ToHeaderValues(), lastColumn))
        {
            var range = worksheet.Range(block.Row, block.StartColumn, block.Row, block.EndColumn);
            range.Merge();
            range.FirstCell().Value = CenterProductReportFormat.BuildHeaderText(block.Label, block.Value);
        }
    }

    private static void WriteDetailRow(
        IXLWorksheet worksheet,
        int rowNumber,
        CenterProductReportStoredRow row,
        IReadOnlyList<CenterProductReportColumn> columns)
    {
        var values = BuildFixedValues(row);
        var applicableColumnKeys = ParseReportColumnKeys(row.ReportColumnKeysJson);
        foreach (var pair in ParseRawData(row.RawDataJson))
        {
            if (applicableColumnKeys.Count == 0 || applicableColumnKeys.Contains(pair.Key))
            {
                values.TryAdd(pair.Key, pair.Value);
            }
        }

        for (var index = 0; index < columns.Count; index++)
        {
            values.TryGetValue(columns[index].Key, out var value);
            worksheet.Cell(rowNumber, index + 1).Value = value ?? string.Empty;
        }
    }

    private static Dictionary<string, string> BuildFixedValues(CenterProductReportStoredRow row)
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
        IReadOnlyList<CenterProductReportStoredRow> rows)
    {
        var worksheet = workbook.Worksheets.Add(CenterProductReportFormat.DataWorksheetName);
        worksheet.Visibility = XLWorksheetVisibility.Hidden;
        var headers = CenterProductReportDataColumns.All;
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

    private static void WriteTaskWorksheet(XLWorkbook workbook, CenterProductReportTaskState taskState)
    {
        var worksheet = workbook.Worksheets.Add(CenterProductReportFormat.TaskWorksheetName);
        worksheet.Visibility = XLWorksheetVisibility.Hidden;
        var rowNumber = 1;
        foreach (var pair in taskState.ToDictionary())
        {
            worksheet.Cell(rowNumber, 1).Value = pair.Key;
            worksheet.Cell(rowNumber, 2).Value = pair.Value;
            rowNumber++;
        }
    }

    private static void ApplyStyle(
        IXLWorksheet worksheet,
        int columnCount,
        int dataRowCount,
        int templateColumnCount)
    {
        var templateRange = worksheet.Range(1, 1, 9, templateColumnCount);
        templateRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        templateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        templateRange.Style.Alignment.WrapText = false;
        templateRange.Style.Alignment.ShrinkToFit = true;

        if (columnCount > 0)
        {
            var lastRow = Math.Max(CenterProductReportFormat.DetailHeaderRow, dataRowCount + CenterProductReportFormat.DetailHeaderRow);
            var usedRange = worksheet.Range(CenterProductReportFormat.DetailHeaderRow, 1, lastRow, columnCount);
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
            worksheet.Row(CenterProductReportFormat.DetailHeaderRow).Height = 27d;
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
        IReadOnlyList<CenterProductReportStoredRow> rows)
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

    private static void MergeProductColumns(
        IXLWorksheet worksheet,
        int startRow,
        int endRow,
        IReadOnlyList<int> columns)
    {
        if (endRow <= startRow)
        {
            return;
        }

        foreach (var column in columns)
        {
            var range = worksheet.Range(startRow, column, endRow, column);
            if (range.Cells().Select(cell => cell.GetString()).Distinct(StringComparer.OrdinalIgnoreCase).Count() <= 1)
            {
                range.Merge();
                range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }
    }

    private static string BuildProductMergeKey(CenterProductReportStoredRow row)
        => CenterProductReportFormat.BuildProductMergeKey(row.StationNo, row.WorkOrder, row.ProductNo);

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

    /// <summary>
    /// 新报表按每行声明的列键隔离工位专属值；空值表示旧报表，继续兼容原有全量读取行为。
    /// </summary>
    private static IReadOnlySet<string> ParseReportColumnKeys(string? columnKeysJson)
    {
        if (string.IsNullOrWhiteSpace(columnKeysJson))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(columnKeysJson)?
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
