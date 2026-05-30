using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;

namespace AutoWeldSystem.Services.Logging;

/// <summary>
/// 本地生产流程日志服务。
/// 每一行 JSON 表示一个业务步骤，文件之间按日期拆分，避免单个日志文件过大。
/// </summary>
public sealed class ProductionFlowLogService : IProductionFlowLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IAppSettingsService _settingsService;
    private readonly object _writeLock = new();

    public ProductionFlowLogService(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public event EventHandler<ProductionFlowLogEntry>? LogWritten;

    public void Write(ProductionFlowLogEntry entry)
    {
        try
        {
            entry.OccurredTime = entry.OccurredTime == default ? DateTime.Now : entry.OccurredTime;
            var filePath = GetLogFilePath(entry.OccurredTime);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var json = JsonSerializer.Serialize(entry, JsonOptions);

            lock (_writeLock)
            {
                File.AppendAllText(filePath, json + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
            }

            LogWritten?.Invoke(this, entry);
        }
        catch
        {
            // 日志写入失败不能影响生产主流程。
        }
    }

    public void Write(
        string step,
        string summary,
        string detail = "",
        string level = "Info",
        int stationNo = 0,
        string workOrderId = "",
        string productNo = "",
        string programId = "",
        string plcSignal = "",
        string plcAddress = "",
        long? durationMilliseconds = null)
    {
        Write(new ProductionFlowLogEntry
        {
            Level = string.IsNullOrWhiteSpace(level) ? "Info" : level.Trim(),
            Step = step.Trim(),
            Summary = summary.Trim(),
            Detail = detail.Trim(),
            StationNo = stationNo,
            WorkOrderId = workOrderId.Trim(),
            ProductNo = productNo.Trim(),
            ProgramId = programId.Trim(),
            PlcSignal = plcSignal.Trim(),
            PlcAddress = plcAddress.Trim(),
            DurationMilliseconds = durationMilliseconds
        });
    }

    public IReadOnlyList<ProductionFlowLogEntry> GetByDate(DateTime date, int take = 500)
    {
        try
        {
            var filePath = GetLogFilePath(date);
            if (!File.Exists(filePath))
            {
                return Array.Empty<ProductionFlowLogEntry>();
            }

            return ReadLatestRecords(filePath, Math.Max(1, take))
                .Reverse()
                .Select(TryDeserialize)
                .Where(entry => entry is not null)
                .Cast<ProductionFlowLogEntry>()
                .ToList();
        }
        catch
        {
            return Array.Empty<ProductionFlowLogEntry>();
        }
    }

    public string GetLogDirectory()
    {
        var root = _settingsService.Get().LogDirectory;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(AppContext.BaseDirectory, "Logs");
        }

        return Path.Combine(root, AppConstants.LogCategories.ProductionFlow);
    }

    private string GetLogFilePath(DateTime date)
    {
        return Path.Combine(GetLogDirectory(), $"{date:yyyy-MM-dd}.jsonl");
    }

    private static IEnumerable<string> ReadLatestRecords(string filePath, int take)
    {
        var records = new Queue<string>(take);
        foreach (var line in File.ReadLines(filePath, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (records.Count >= take)
            {
                records.Dequeue();
            }

            records.Enqueue(line);
        }

        return records;
    }

    private static ProductionFlowLogEntry? TryDeserialize(string line)
    {
        try
        {
            return string.IsNullOrWhiteSpace(line)
                ? null
                : JsonSerializer.Deserialize<ProductionFlowLogEntry>(line, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
