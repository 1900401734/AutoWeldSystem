using AutoWeldSystem.Core.Center;
using ClosedXML.Excel;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 严格读取中心报表隐藏页；损坏或缺少必需页时抛出异常，禁止按空报表继续覆盖。
/// </summary>
internal sealed class CenterProductReportWorkbookReader
{
    /// <summary>
    /// 文件不存在时返回空状态；文件存在时必须完整解析。
    /// </summary>
    public CenterProductReportStoredState Load(string reportPath)
    {
        if (!File.Exists(reportPath))
        {
            return CenterProductReportStoredState.Empty;
        }

        var workbookBytes = File.ReadAllBytes(reportPath);
        using var stream = new MemoryStream(workbookBytes, writable: false);
        using var workbook = new XLWorkbook(stream);
        var dataWorksheet = GetRequiredWorksheet(workbook, CenterProductReportFormat.DataWorksheetName);
        var columnsWorksheet = GetRequiredWorksheet(workbook, CenterProductReportFormat.ColumnsWorksheetName);
        var taskWorksheet = GetRequiredWorksheet(workbook, CenterProductReportFormat.TaskWorksheetName);
        return new CenterProductReportStoredState(
            ReadRows(dataWorksheet),
            ReadColumns(columnsWorksheet),
            ReadTaskState(taskWorksheet));
    }

    private static IXLWorksheet GetRequiredWorksheet(XLWorkbook workbook, string worksheetName)
    {
        return workbook.Worksheets.FirstOrDefault(
                sheet => string.Equals(sheet.Name, worksheetName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException($"Center report worksheet is missing: {worksheetName}");
    }

    private static IReadOnlyList<CenterProductReportStoredRow> ReadRows(IXLWorksheet worksheet)
    {
        var rows = new List<CenterProductReportStoredRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            rows.Add(CenterProductReportStoredRow.FromWorksheetRow(worksheet, rowNumber));
        }

        return rows;
    }

    private static IReadOnlyList<CenterProductReportColumn> ReadColumns(IXLWorksheet worksheet)
    {
        var columns = new List<CenterProductReportColumn>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var key = worksheet.Cell(rowNumber, 1).GetString();
            if (!string.IsNullOrWhiteSpace(key))
            {
                columns.Add(new CenterProductReportColumn(
                    key.Trim(),
                    worksheet.Cell(rowNumber, 2).GetString(),
                    worksheet.Cell(rowNumber, 3).GetBoolean()));
            }
        }

        return columns;
    }

    private static CenterProductReportTaskState ReadTaskState(IXLWorksheet worksheet)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNumber = 1; rowNumber <= lastRow; rowNumber++)
        {
            var key = worksheet.Cell(rowNumber, 1).GetString();
            if (!string.IsNullOrWhiteSpace(key))
            {
                values[key] = worksheet.Cell(rowNumber, 2).GetString();
            }
        }

        return CenterProductReportTaskState.FromDictionary(values);
    }
}
