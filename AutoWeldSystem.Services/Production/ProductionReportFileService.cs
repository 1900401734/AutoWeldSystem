using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Data;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 生产报告文件服务实现。
/// 当前先生成 CSV 报告；Excel 文件可在字段稳定后复用同一份行数据扩展。
/// </summary>
public class ProductionReportFileService : IProductionReportFileService
{
    private static readonly string[] BaseHeaders =
    {
        "焊点序号",
        "工号",
        "批次",
        "数量",
        "零部件名称(图号)",
        "工序号",
        "峰值电流(KA)",
        "峰值电压(V)",
        "有效功率(KW)",
        "操作人员",
        "日期",
        "产品编号",
        "焊点编号",
        "工位",
        "测试结果"
    };

    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IProductionFlowLogService _productionLogService;
    private readonly object _dbLock = new();

    public ProductionReportFileService(
        SqlSugarDbContext dbContext,
        IAppSettingsService settingsService,
        IProductionFlowLogService productionLogService)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _productionLogService = productionLogService;
    }

    public BizProductionReportFile GenerateCsvReport(BizWeldTask task)
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
            WriteCsv(report.FilePath, BuildHeaders(task), records, task);

            report.UploadStatus = ProductionConstants.UploadStatuses.Pending;
            report.UploadMessage = $"CSV report generated, rows={records.Count}.";
            report.UpdatedTime = DateTime.Now;
            _productionLogService.Write(
                "ReportFileGenerated",
                "报告文件生成成功",
                $"FilePath={report.FilePath}, Rows={records.Count}",
                stationNo: task.StationNo,
                workOrderId: task.WorkOrderId,
                programId: task.ProgramId ?? string.Empty,
                plcAddress: report.FilePath);

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
                && report.FileFormat == "CSV");

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
            SN = task.WorkOrderId,
            ProcessNo = task.ProcessNo,
            FileCode = ProductionConstants.ReportFileCodes.Spreadsheet,
            MesFileType = ProductionConstants.MesFileTypes.ReportFile,
            FileFormat = "CSV",
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
                && report.SN == task.WorkOrderId
                && report.ProcessNo == task.ProcessNo
                && report.FileCode == ProductionConstants.ReportFileCodes.Spreadsheet)
            .ToList();

        return existingReports.Count == 0
            ? 1
            : existingReports.Max(report => report.SequenceNo) + 1;
    }

    private string[] BuildHeaders(BizWeldTask task)
    {
        var itemHeaders = GetSchemeItemsForTask(task)
            .SelectMany(BuildItemHeaders);

        var dynamicHeaders = itemHeaders
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(header => !BaseHeaders.Contains(header, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        return BaseHeaders.Concat(dynamicHeaders).ToArray();
    }

    private void WriteCsv(string filePath, string[] headers, IReadOnlyList<BizWeldPointRecord> records, BizWeldTask task)
    {
        var lines = new List<string>
        {
            ToCsvLine(headers)
        };

        foreach (var record in records)
        {
            var row = BuildRow(record, task);
            lines.Add(ToCsvLine(headers.Select(header => row.TryGetValue(header, out var value) ? value : string.Empty)));
        }

        File.WriteAllLines(filePath, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private Dictionary<string, string> BuildRow(BizWeldPointRecord record, BizWeldTask task)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["焊点序号"] = string.IsNullOrWhiteSpace(record.TouchNo) ? record.SequenceNo.ToString() : record.TouchNo,
            ["工号"] = task.WorkOrderId,
            ["批次"] = task.Batch,
            ["数量"] = task.ActualQty.ToString(),
            ["零部件名称(图号)"] = BuildPartName(task),
            ["工序号"] = task.ProcessNo,
            ["峰值电流(KA)"] = record.MaxElectric ?? string.Empty,
            ["峰值电压(V)"] = record.MaxVoltage ?? string.Empty,
            ["有效功率(KW)"] = record.ValidPower ?? string.Empty,
            ["操作人员"] = record.OperatorNo ?? task.EndOperatorNumber ?? task.StartOperatorNumber ?? string.Empty,
            ["日期"] = record.RecordTime.ToString("yyyy-MM-dd HH:mm:ss"),
            ["产品编号"] = record.ProductNo,
            ["焊点编号"] = record.TouchNo,
            ["工位"] = record.StationNo.ToString(),
            ["测试结果"] = record.TestResult
        };

        AddDynamicValues(row, record, task);
        return row;
    }

    private void AddDynamicValues(Dictionary<string, string> row, BizWeldPointRecord record, BizWeldTask task)
    {
        var rawValues = ParseRawData(record.RawDataJson);
        AddSchemeDynamicValues(row, rawValues, task);
    }

    private void AddSchemeDynamicValues(Dictionary<string, string> row, IReadOnlyDictionary<string, string> rawValues, BizWeldTask task)
    {
        foreach (var item in GetSchemeItemsForTask(task))
        {
            var header = GetItemHeader(item);
            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            var itemKey = ResolveItemKey(item);
            TryAddDynamicValue(row, header, GetRawValue(rawValues, item.ItemName, itemKey));
            TryAddDynamicValue(row, $"{header}上限", GetRawValue(rawValues, $"{item.ItemName}上限", $"{itemKey}_upper"));
            TryAddDynamicValue(row, $"{header}下限", GetRawValue(rawValues, $"{item.ItemName}下限", $"{itemKey}_lower"));
            TryAddDynamicValue(row, $"{header}结果", GetRawValue(rawValues, $"{item.ItemName}结果", $"{itemKey}_result"));
        }
    }

    private IReadOnlyList<DimTestItem> GetSchemeItemsForTask(BizWeldTask task)
    {
        var config = ResolveProductProcessConfig(task);
        if (config is null)
        {
            return Array.Empty<DimTestItem>();
        }

        var details = _dbContext.Db.Queryable<BizSchemeDetail>()
            .Where(detail => detail.SchemeId == config.SchemeId)
            .ToList();
        if (details.Count == 0)
        {
            return Array.Empty<DimTestItem>();
        }

        var itemIds = details.Select(detail => detail.ItemId).Distinct().ToList();
        var items = _dbContext.Db.Queryable<DimTestItem>()
            .Where(item => itemIds.Contains(item.ItemId))
            .ToList();

        return details
            .OrderBy(detail => detail.DetailId)
            .Select(detail => items.FirstOrDefault(item => item.ItemId == detail.ItemId))
            .Where(item => item is not null)
            .Select(item => item!)
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

    private static IEnumerable<string> BuildItemHeaders(DimTestItem item)
    {
        var header = GetItemHeader(item);
        if (string.IsNullOrWhiteSpace(header))
        {
            yield break;
        }

        yield return header;
        yield return $"{header}上限";
        yield return $"{header}下限";
        yield return $"{header}结果";
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
        var dataDirectory = _settingsService.Get().DataDirectory;
        var baseDirectory = string.IsNullOrWhiteSpace(dataDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "Data")
            : dataDirectory.Trim();

        return Path.Combine(baseDirectory, "Reports", SanitizePathPart(task.WorkOrderId), DateTime.Now.ToString("yyyyMMdd"));
    }

    private static string BuildFileName(BizWeldTask task, int sequenceNo)
    {
        return string.Join(
            "_",
            SanitizePathPart(task.DeviceId),
            SanitizePathPart(task.WorkOrderId),
            SanitizePathPart(task.ProcessNo),
            ProductionConstants.ReportFileCodes.Spreadsheet,
            sequenceNo.ToString("D3")) + ".csv";
    }

    private static string BuildPartName(BizWeldTask task)
    {
        if (string.IsNullOrWhiteSpace(task.DrawingNo))
        {
            return task.ProductName;
        }

        if (string.IsNullOrWhiteSpace(task.ProductName))
        {
            return task.DrawingNo;
        }

        return $"{task.ProductName}({task.DrawingNo})";
    }

    private static string ToCsvLine(IEnumerable<string> cells)
    {
        return string.Join(",", cells.Select(EscapeCsvCell));
    }

    private static string EscapeCsvCell(string? value)
    {
        var text = value ?? string.Empty;
        return text.Contains('"') || text.Contains(',') || text.Contains('\r') || text.Contains('\n')
            ? $"\"{text.Replace("\"", "\"\"")}\""
            : text;
    }

    private static string SanitizePathPart(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "NA" : value.Trim();
        return Regex.Replace(normalized, @"[\\/:*?""<>|]+", "-");
    }
}
