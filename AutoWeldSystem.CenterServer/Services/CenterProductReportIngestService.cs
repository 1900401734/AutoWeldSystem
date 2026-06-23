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
/// Stores completed product data forwarded by equipment clients and refreshes center-side XLSX reports.
/// </summary>
public sealed class CenterProductReportIngestService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IHubContext<CenterDashboardHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly object _dbLock = new();

    public CenterProductReportIngestService(
        SqlSugarDbContext dbContext,
        IHubContext<CenterDashboardHub> hubContext,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _configuration = configuration;
    }

    /// <summary>
    /// Saves one completed product and refreshes the corresponding center report file.
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

        if (request.Points.Count == 0)
        {
            return Fail("Product report points are required.");
        }

        string reportPath;
        lock (_dbLock)
        {
            _dbContext.InitDatabase();
            UpsertDeviceNode(deviceId, request);
            ReplaceProductRecords(deviceId, request);
            RefreshStationCounts(deviceId, request);
            reportPath = WriteReportFile(deviceId, request);
        }

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
    /// Ensures product-only uploads can also register a new device node.
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
    /// Replaces the same product rows to keep retry uploads idempotent.
    /// </summary>
    private void ReplaceProductRecords(string deviceId, CenterProductReportRequest request)
    {
        _dbContext.Db.Deleteable<CenterProductRecord>()
            .Where(record => record.DeviceId == deviceId
                && record.StationNo == request.StationNo
                && record.WorkOrder == request.WorkOrder
                && record.ProductNo == request.ProductNo)
            .ExecuteCommand();

        var rows = request.Points
            .OrderBy(point => point.SequenceNo)
            .Select(point => new CenterProductRecord
            {
                DeviceId = deviceId,
                DeviceName = request.DeviceName.Trim(),
                SystemType = CenterTelemetryRules.NormalizeSystemType(request.SystemType),
                StationNo = request.StationNo,
                WorkOrder = request.WorkOrder.Trim(),
                ProductJobNo = request.ProductJobNo.Trim(),
                ProductNo = request.ProductNo.Trim(),
                ProductModel = request.ProductModel.Trim(),
                ProductResult = request.ProductResult.Trim(),
                SequenceNo = point.SequenceNo,
                TouchNo = point.TouchNo.Trim(),
                TestResult = point.TestResult.Trim(),
                CollectedAt = point.CollectedAt == default ? DateTime.Now : point.CollectedAt,
                CompletedAt = request.CompletedAt == default ? DateTime.Now : request.CompletedAt,
                RawDataJson = point.RawDataJson,
                CreatedAt = DateTime.Now
            })
            .ToList();

        _dbContext.Db.Insertable(rows).ExecuteCommand();
    }

    /// <summary>
    /// Updates dashboard counts immediately after a product report is accepted.
    /// </summary>
    private void RefreshStationCounts(string deviceId, CenterProductReportRequest request)
    {
        var start = DateTime.Today;
        var end = start.AddDays(1);
        var products = _dbContext.Db.Queryable<CenterProductRecord>()
            .Where(record => record.DeviceId == deviceId
                && record.StationNo == request.StationNo
                && record.CompletedAt >= start
                && record.CompletedAt < end)
            .ToList()
            .GroupBy(record => record.ProductNo, StringComparer.OrdinalIgnoreCase)
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

    private string WriteReportFile(string deviceId, CenterProductReportRequest request)
    {
        var reportDate = request.CompletedAt == default ? DateTime.Today : request.CompletedAt.Date;
        var rows = _dbContext.Db.Queryable<CenterProductRecord>()
            .Where(record => record.DeviceId == deviceId
                && record.StationNo == request.StationNo
                && record.WorkOrder == request.WorkOrder
                && record.CompletedAt >= reportDate
                && record.CompletedAt < reportDate.AddDays(1))
            .OrderBy(record => record.ProductNo)
            .OrderBy(record => record.SequenceNo)
            .ToList();
        var dynamicKeys = ResolveDynamicKeys(rows);
        var reportPath = BuildReportPath(deviceId, request.StationNo, request.WorkOrder, reportDate);

        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("产品数据");
        var fixedHeaders = new[]
        {
            "设备编号", "设备名称", "系统类型", "工位", "工单号", "产品工号", "PLC产品编号",
            "产品型号", "产品结果", "点位", "点位结果", "采集时间"
        };

        var headers = fixedHeaders.Concat(dynamicKeys).ToList();
        for (var column = 0; column < headers.Count; column++)
        {
            worksheet.Cell(1, column + 1).Value = headers[column];
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            WriteReportRow(worksheet, rowIndex + 2, rows[rowIndex], dynamicKeys);
        }

        var usedRange = worksheet.Range(1, 1, Math.Max(1, rows.Count + 1), headers.Count);
        usedRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(reportPath);
        return reportPath;
    }

    private static void WriteReportRow(
        IXLWorksheet worksheet,
        int rowNumber,
        CenterProductRecord row,
        IReadOnlyList<string> dynamicKeys)
    {
        var fixedValues = new[]
        {
            row.DeviceId,
            row.DeviceName,
            row.SystemType,
            row.StationNo.ToString(),
            row.WorkOrder,
            row.ProductJobNo,
            row.ProductNo,
            row.ProductModel,
            row.ProductResult,
            row.TouchNo,
            row.TestResult,
            row.CollectedAt.ToString("yyyy-MM-dd HH:mm:ss")
        };
        for (var column = 0; column < fixedValues.Length; column++)
        {
            worksheet.Cell(rowNumber, column + 1).Value = fixedValues[column];
        }

        var dynamicValues = ParseRawData(row.RawDataJson);
        for (var index = 0; index < dynamicKeys.Count; index++)
        {
            dynamicValues.TryGetValue(dynamicKeys[index], out var value);
            worksheet.Cell(rowNumber, fixedValues.Length + index + 1).Value = value ?? string.Empty;
        }
    }

    private IReadOnlyList<string> ResolveDynamicKeys(IReadOnlyList<CenterProductRecord> rows)
    {
        var keys = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            foreach (var key in ParseRawData(row.RawDataJson).Keys)
            {
                if (seen.Add(key))
                {
                    keys.Add(key);
                }
            }
        }

        return keys;
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

    private string BuildReportPath(string deviceId, int stationNo, string workOrder, DateTime reportDate)
    {
        var configuredRoot = _configuration.GetValue<string>("CenterServer:ReportDirectory");
        var root = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(AppContext.BaseDirectory, "CenterReports")
            : configuredRoot.Trim();
        var fileName = $"{SanitizeFileName(deviceId)}_S{stationNo}_{SanitizeFileName(workOrder)}_{reportDate:yyyyMMdd}.xlsx";
        return Path.Combine(root, reportDate.ToString("yyyyMMdd"), fileName);
    }

    private static bool IsProductOk(CenterProductRecord row)
        => string.Equals(row.ProductResult, ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase);

    private static string SanitizeFileName(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "未命名" : value.Trim();
        return Regex.Replace(normalized, """[\\/:*?""<>|]""", "_");
    }
}
