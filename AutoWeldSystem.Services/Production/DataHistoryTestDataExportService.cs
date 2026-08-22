using AutoWeldSystem.Core.DTOs.DataManagement;
using AutoWeldSystem.Core.Production;
using ClosedXML.Excel;

namespace AutoWeldSystem.Services.Production;

public static class DataHistoryTestDataExportService
{
    public static void Export(
        string filePath,
        string workOrderId,
        IReadOnlyList<DataHistoryTestDataRow> productRows,
        IReadOnlyList<DataHistoryDynamicColumn> dynamicColumns)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("测试数据");
        var headers = new[] { "工单号", "工位", "产品号", "测试点号", "产品结果", "测试结果", "上传状态", "记录时间" }
            .Concat(dynamicColumns.Select(column => column.HeaderText)).ToArray();
        for (var i = 0; i < headers.Length; i++) worksheet.Cell(1, i + 1).Value = headers[i];

        var rowIndex = 2;
        foreach (var product in productRows)
        {
            var records = product.Children.Count > 0 ? product.Children : [product];
            foreach (var record in records)
            {
                worksheet.Cell(rowIndex, 1).Value = workOrderId;
                worksheet.Cell(rowIndex, 2).Value = product.StationNo;
                worksheet.Cell(rowIndex, 3).Value = product.ProductNo;
                worksheet.Cell(rowIndex, 4).Value = record.TouchNo;
                worksheet.Cell(rowIndex, 5).Value = TestResultRules.ToDisplayText(product.ProductResult);
                worksheet.Cell(rowIndex, 6).Value = TestResultRules.ToDisplayText(record.TestResult);
                worksheet.Cell(rowIndex, 7).Value = record.UploadStatus;
                if (record.RecordTime.HasValue) worksheet.Cell(rowIndex, 8).Value = record.RecordTime.Value;
                for (var i = 0; i < dynamicColumns.Count; i++)
                {
                    if (record.DynamicValues.TryGetValue(dynamicColumns[i].Key, out var value)) worksheet.Cell(rowIndex, i + 9).Value = value;
                }
                rowIndex++;
            }
        }
        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }
}
