using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.DTOs.CenterServer;
using ClosedXML.Excel;
using System.Text.Json;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 一次完整的中心报表持久状态。
/// </summary>
internal sealed record CenterProductReportStoredState(
    IReadOnlyList<CenterProductReportStoredRow> Rows,
    IReadOnlyList<CenterProductReportColumn> Columns,
    CenterProductReportTaskState? TaskState)
{
    public static CenterProductReportStoredState Empty { get; } = new([], [], null);
}

/// <summary>
/// 隐藏任务元数据页保存的客户模板任务级字段。
/// </summary>
internal sealed record CenterProductReportTaskState(
    string ProductJobNo,
    string DrawingNo,
    string Batch,
    string WorkOrder,
    string Spec,
    string ProductModel,
    string ProcessNo,
    int Quantity,
    int QualifiedQty,
    DateTime StartTime,
    DateTime? EndTime,
    string OperatorNo)
{
    public static CenterProductReportTaskState FromRequest(
        CenterProductReportRequest request,
        DateTime? effectiveEndTime)
    {
        return new CenterProductReportTaskState(
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
            effectiveEndTime,
            request.OperatorNo.Trim());
    }

    public CenterProductReportHeaderValues ToHeaderValues()
    {
        return new CenterProductReportHeaderValues(
            ProductJobNo,
            DrawingNo,
            Batch,
            WorkOrder,
            Spec,
            ProductModel,
            ProcessNo,
            Quantity,
            QualifiedQty,
            StartTime,
            EndTime,
            OperatorNo);
    }

    public IReadOnlyDictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            [nameof(ProductJobNo)] = ProductJobNo,
            [nameof(DrawingNo)] = DrawingNo,
            [nameof(Batch)] = Batch,
            [nameof(WorkOrder)] = WorkOrder,
            [nameof(Spec)] = Spec,
            [nameof(ProductModel)] = ProductModel,
            [nameof(ProcessNo)] = ProcessNo,
            [nameof(Quantity)] = Quantity.ToString(),
            [nameof(QualifiedQty)] = QualifiedQty.ToString(),
            [nameof(StartTime)] = StartTime.ToString("O"),
            [nameof(EndTime)] = EndTime?.ToString("O") ?? string.Empty,
            [nameof(OperatorNo)] = OperatorNo
        };
    }

    public static CenterProductReportTaskState FromDictionary(IReadOnlyDictionary<string, string> values)
    {
        return new CenterProductReportTaskState(
            Get(values, nameof(ProductJobNo)),
            Get(values, nameof(DrawingNo)),
            Get(values, nameof(Batch)),
            Get(values, nameof(WorkOrder)),
            Get(values, nameof(Spec)),
            Get(values, nameof(ProductModel)),
            Get(values, nameof(ProcessNo)),
            GetInt(values, nameof(Quantity)),
            GetInt(values, nameof(QualifiedQty)),
            GetDate(values, nameof(StartTime)) ?? default,
            GetDate(values, nameof(EndTime)),
            Get(values, nameof(OperatorNo)));
    }

    private static string Get(IReadOnlyDictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value) ? value : string.Empty;

    private static int GetInt(IReadOnlyDictionary<string, string> values, string key)
        => int.TryParse(Get(values, key), out var value) ? value : 0;

    private static DateTime? GetDate(IReadOnlyDictionary<string, string> values, string key)
        => DateTime.TryParse(Get(values, key), out var value) ? value : null;
}

/// <summary>
/// 隐藏数据页中的一个点明细行。
/// </summary>
internal sealed class CenterProductReportStoredRow
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

    /// <summary>
    /// 记录该产品请求实际声明的列键，防止后续工位扩展列并集后读取到本行不适用的原始值。
    /// 旧报表缺少此字段时保持空值，由写入器按兼容模式处理。
    /// </summary>
    public string ReportColumnKeysJson { get; set; } = string.Empty;

    public static CenterProductReportStoredRow FromRequest(
        CenterProductReportRequest request,
        CenterProductReportPointDto point)
    {
        return new CenterProductReportStoredRow
        {
            DeviceId = request.DeviceId.Trim(),
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
            RawDataJson = point.RawDataJson ?? string.Empty,
            ReportColumnKeysJson = JsonSerializer.Serialize(
                request.ReportColumns
                    .Where(column => !string.IsNullOrWhiteSpace(column.Key))
                    .Select(column => column.Key.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase))
        };
    }

    public bool IsSameProduct(CenterProductReportRequest request)
    {
        return StationNo == request.StationNo
            && string.Equals(WorkOrder, request.WorkOrder.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(ProductNo, request.ProductNo.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public CenterProductReportProductSummary ToSummary()
        => new(DeviceId, StationNo, WorkOrder, ProductNo, ProductResult, CompletedAt);

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [CenterProductReportDataColumns.DeviceId] = DeviceId,
            [CenterProductReportDataColumns.DeviceName] = DeviceName,
            [CenterProductReportDataColumns.SystemType] = SystemType,
            [CenterProductReportDataColumns.StationNo] = StationNo.ToString(),
            [CenterProductReportDataColumns.StationName] = StationName,
            [CenterProductReportDataColumns.WorkOrder] = WorkOrder,
            [CenterProductReportDataColumns.Batch] = Batch,
            [CenterProductReportDataColumns.Quantity] = Quantity.ToString(),
            [CenterProductReportDataColumns.PartName] = PartName,
            [CenterProductReportDataColumns.ProcessNo] = ProcessNo,
            [CenterProductReportDataColumns.OperatorNo] = OperatorNo,
            [CenterProductReportDataColumns.ProductJobNo] = ProductJobNo,
            [CenterProductReportDataColumns.ProductNo] = ProductNo,
            [CenterProductReportDataColumns.ProductModel] = ProductModel,
            [CenterProductReportDataColumns.ProductResult] = ProductResult,
            [CenterProductReportDataColumns.SequenceNo] = SequenceNo.ToString(),
            [CenterProductReportDataColumns.TouchNo] = TouchNo,
            [CenterProductReportDataColumns.TestResult] = TestResult,
            [CenterProductReportDataColumns.CollectedAt] = CollectedAt.ToString("O"),
            [CenterProductReportDataColumns.CompletedAt] = CompletedAt.ToString("O"),
            [CenterProductReportDataColumns.RawDataJson] = RawDataJson,
            [CenterProductReportDataColumns.ReportColumnKeysJson] = ReportColumnKeysJson
        };
    }

    public static CenterProductReportStoredRow FromWorksheetRow(IXLWorksheet worksheet, int rowNumber)
    {
        return new CenterProductReportStoredRow
        {
            DeviceId = Get(worksheet, rowNumber, CenterProductReportDataColumns.DeviceId),
            DeviceName = Get(worksheet, rowNumber, CenterProductReportDataColumns.DeviceName),
            SystemType = Get(worksheet, rowNumber, CenterProductReportDataColumns.SystemType),
            StationNo = GetInt(worksheet, rowNumber, CenterProductReportDataColumns.StationNo, 1),
            StationName = Get(worksheet, rowNumber, CenterProductReportDataColumns.StationName),
            WorkOrder = Get(worksheet, rowNumber, CenterProductReportDataColumns.WorkOrder),
            Batch = Get(worksheet, rowNumber, CenterProductReportDataColumns.Batch),
            Quantity = GetInt(worksheet, rowNumber, CenterProductReportDataColumns.Quantity, 0),
            PartName = Get(worksheet, rowNumber, CenterProductReportDataColumns.PartName),
            ProcessNo = Get(worksheet, rowNumber, CenterProductReportDataColumns.ProcessNo),
            OperatorNo = Get(worksheet, rowNumber, CenterProductReportDataColumns.OperatorNo),
            ProductJobNo = Get(worksheet, rowNumber, CenterProductReportDataColumns.ProductJobNo),
            ProductNo = Get(worksheet, rowNumber, CenterProductReportDataColumns.ProductNo),
            ProductModel = Get(worksheet, rowNumber, CenterProductReportDataColumns.ProductModel),
            ProductResult = Get(worksheet, rowNumber, CenterProductReportDataColumns.ProductResult),
            SequenceNo = GetInt(worksheet, rowNumber, CenterProductReportDataColumns.SequenceNo, 0),
            TouchNo = Get(worksheet, rowNumber, CenterProductReportDataColumns.TouchNo),
            TestResult = Get(worksheet, rowNumber, CenterProductReportDataColumns.TestResult),
            CollectedAt = GetDate(worksheet, rowNumber, CenterProductReportDataColumns.CollectedAt),
            CompletedAt = GetDate(worksheet, rowNumber, CenterProductReportDataColumns.CompletedAt),
            RawDataJson = Get(worksheet, rowNumber, CenterProductReportDataColumns.RawDataJson),
            ReportColumnKeysJson = Get(worksheet, rowNumber, CenterProductReportDataColumns.ReportColumnKeysJson)
        };
    }

    private static string Get(IXLWorksheet worksheet, int rowNumber, string columnName)
        => worksheet.Cell(rowNumber, CenterProductReportDataColumns.IndexOf(columnName) + 1).GetString();

    private static int GetInt(IXLWorksheet worksheet, int rowNumber, string columnName, int fallback)
        => int.TryParse(Get(worksheet, rowNumber, columnName), out var value) ? value : fallback;

    private static DateTime GetDate(IXLWorksheet worksheet, int rowNumber, string columnName)
        => DateTime.TryParse(Get(worksheet, rowNumber, columnName), out var value) ? value : DateTime.Now;
}

/// <summary>
/// 隐藏数据页稳定列协议。
/// </summary>
internal static class CenterProductReportDataColumns
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
    public const string ReportColumnKeysJson = "ReportColumnKeysJson";

    public static readonly IReadOnlyList<string> All =
    [
        DeviceId, DeviceName, SystemType, StationNo, StationName, WorkOrder, Batch, Quantity,
        PartName, ProcessNo, OperatorNo, ProductJobNo, ProductNo, ProductModel, ProductResult,
        SequenceNo, TouchNo, TestResult, CollectedAt, CompletedAt, RawDataJson, ReportColumnKeysJson
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
