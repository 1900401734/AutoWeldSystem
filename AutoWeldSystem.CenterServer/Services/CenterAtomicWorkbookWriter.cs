using AutoWeldSystem.Core.Center;
using ClosedXML.Excel;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 在目标同目录写临时 XLSX、重开验证后再执行同卷原子创建或替换。
/// </summary>
internal sealed class CenterAtomicWorkbookWriter
{
    /// <summary>
    /// 成功与失败都会清理临时文件；替换失败时正式文件保持不变。
    /// </summary>
    public void Write(string reportPath, Action<XLWorkbook> populateWorkbook)
    {
        var directory = Path.GetDirectoryName(reportPath)!;
        var fileName = Path.GetFileName(reportPath);
        var temporaryPath = Path.Combine(directory, $".{fileName}.tmp-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                populateWorkbook(workbook);
                workbook.SaveAs(temporaryPath);
            }

            Validate(temporaryPath);
            if (File.Exists(reportPath))
            {
                File.Replace(temporaryPath, reportPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, reportPath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void Validate(string temporaryPath)
    {
        using var workbook = new XLWorkbook(temporaryPath);
        foreach (var worksheetName in new[]
        {
            CenterProductReportFormat.WorksheetName,
            CenterProductReportFormat.DataWorksheetName,
            CenterProductReportFormat.ColumnsWorksheetName,
            CenterProductReportFormat.TaskWorksheetName
        })
        {
            if (!workbook.Worksheets.Any(
                    sheet => string.Equals(sheet.Name, worksheetName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidDataException($"Center report worksheet is missing: {worksheetName}");
            }
        }
    }
}
