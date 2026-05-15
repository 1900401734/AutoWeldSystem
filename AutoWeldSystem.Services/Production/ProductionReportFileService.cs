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
    private readonly object _dbLock = new();

    public ProductionReportFileService(SqlSugarDbContext dbContext, IAppSettingsService settingsService)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
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
            WriteCsv(report.FilePath, BuildHeaders(), records, task);

            report.UploadStatus = ProductionConstants.UploadStatuses.Pending;
            report.UploadMessage = $"CSV report generated, rows={records.Count}.";
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

    private string[] BuildHeaders()
    {
        var dynamicHeaders = _dbContext.Db.Queryable<BizCollectionParameter>()
            .Where(parameter => parameter.Enabled && parameter.ReportColumnName != null && parameter.ReportColumnName != "")
            .ToList()
            .OrderBy(parameter => parameter.Sort)
            .Select(parameter => parameter.ReportColumnName!.Trim())
            .Where(header => !string.IsNullOrWhiteSpace(header))
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

        AddDynamicValues(row, record);
        return row;
    }

    private void AddDynamicValues(Dictionary<string, string> row, BizWeldPointRecord record)
    {
        var rawValues = ParseRawData(record.RawDataJson);
        var parameters = _dbContext.Db.Queryable<BizCollectionParameter>()
            .Where(parameter => parameter.Enabled && parameter.ReportColumnName != null && parameter.ReportColumnName != "")
            .ToList();

        foreach (var parameter in parameters)
        {
            var header = parameter.ReportColumnName?.Trim();
            if (string.IsNullOrWhiteSpace(header) || row.ContainsKey(header))
            {
                continue;
            }

            row[header] = GetRecordValue(record, parameter, rawValues);
        }
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

    private static string GetRecordValue(
        BizWeldPointRecord record,
        BizCollectionParameter parameter,
        IReadOnlyDictionary<string, string> rawValues)
    {
        if (rawValues.TryGetValue(parameter.ParameterKey, out var byKey))
        {
            return byKey;
        }

        if (!string.IsNullOrWhiteSpace(parameter.MesFieldName)
            && rawValues.TryGetValue(parameter.MesFieldName, out var byMesField))
        {
            return byMesField;
        }

        return parameter.ParameterKey switch
        {
            "max_electric" => record.MaxElectric ?? string.Empty,
            "max_voltage" => record.MaxVoltage ?? string.Empty,
            "valid_power" => record.ValidPower ?? string.Empty,
            "displacement" => record.Displacement ?? string.Empty,
            "weld_ts" => record.WeldTs ?? string.Empty,
            "test_result" => record.TestResult,
            "test_result_raw" => record.TestResultRaw ?? string.Empty,
            _ => string.Empty
        };
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
