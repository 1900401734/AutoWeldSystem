using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AutoWeldSystem.CenterServer.Models;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 以 JSONL 格式记录设备端每次推送到中心服务器的数据。
/// 文件按日期、设备、工位、请求类型拆分，方便现场直接定位。
/// </summary>
public sealed class CenterPushJsonlLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex InvalidFileNameChars = new("""[\\/:*?""<>|]+""", RegexOptions.Compiled);

    private readonly CenterServerSettingsService _settingsService;
    private readonly ILogger<CenterPushJsonlLogService> _logger;
    private readonly object _writeLock = new();

    public CenterPushJsonlLogService(
        CenterServerSettingsService settingsService,
        ILogger<CenterPushJsonlLogService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// 记录 telemetry 或 heartbeat 请求。
    /// </summary>
    public void WriteTelemetry(
        string requestType,
        CenterTelemetrySnapshotRequest request,
        CenterTelemetryAck result,
        Exception? exception = null)
    {
        var stations = request.Stations.Count == 0
            ? [0]
            : request.Stations.Where(station => station.StationNo > 0).Select(station => station.StationNo).DefaultIfEmpty(0);

        foreach (var stationNo in stations)
        {
            Write(new CenterPushLogRecord
            {
                ReceivedAt = DateTime.Now,
                RequestType = requestType,
                DeviceId = request.DeviceId.Trim(),
                DeviceName = request.DeviceName.Trim(),
                SystemType = request.SystemType.Trim(),
                StationNo = stationNo,
                Success = result.Success && exception is null,
                Message = result.Message,
                Error = exception?.Message,
                Payload = ToJsonNode(request)
            });
        }
    }

    /// <summary>
    /// 记录产品完成数据推送请求。
    /// </summary>
    public void WriteProductReport(
        CenterProductReportRequest request,
        CenterTelemetryAck result,
        Exception? exception = null)
    {
        Write(new CenterPushLogRecord
        {
            ReceivedAt = DateTime.Now,
            RequestType = AppConstants.CenterInteractionTypes.ProductReport,
            DeviceId = request.DeviceId.Trim(),
            DeviceName = request.DeviceName.Trim(),
            SystemType = request.SystemType.Trim(),
            StationNo = Math.Max(0, request.StationNo),
            Success = result.Success && exception is null,
            Message = result.Message,
            Error = exception?.Message,
            Payload = ToJsonNode(request)
        });
    }

    /// <summary>
    /// 读取最新日志，供看板页面展示。
    /// </summary>
    public IReadOnlyList<CenterPushLogRecord> QueryRecent(CenterPushLogQuery query)
    {
        var limit = Math.Clamp(query.Limit <= 0 ? 200 : query.Limit, 1, 1000);
        var root = _settingsService.Get().LogDirectory;
        if (!Directory.Exists(root))
        {
            return Array.Empty<CenterPushLogRecord>();
        }

        var records = new List<CenterPushLogRecord>(limit);
        foreach (var file in Directory.EnumerateFiles(root, "*.jsonl", SearchOption.AllDirectories)
                     .OrderByDescending(File.GetLastWriteTime)
                     .Take(500))
        {
            ReadFile(file, query, records, limit);
            if (records.Count >= limit)
            {
                break;
            }
        }

        return records
            .OrderByDescending(record => record.ReceivedAt)
            .Take(limit)
            .ToList();
    }

    private void Write(CenterPushLogRecord record)
    {
        try
        {
            var filePath = BuildLogPath(record);
            var json = JsonSerializer.Serialize(record, JsonOptions);
            lock (_writeLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.AppendAllText(filePath, json + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Write center push JSONL log failed.");
        }
    }

    private void ReadFile(
        string filePath,
        CenterPushLogQuery query,
        ICollection<CenterPushLogRecord> records,
        int limit)
    {
        try
        {
            foreach (var line in File.ReadLines(filePath).Reverse())
            {
                if (records.Count >= limit)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var record = JsonSerializer.Deserialize<CenterPushLogRecord>(line, JsonOptions);
                if (record is not null && Matches(record, query))
                {
                    records.Add(record);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Read center push JSONL log failed. File={File}", filePath);
        }
    }

    private static bool Matches(CenterPushLogRecord record, CenterPushLogQuery query)
    {
        if (query.StationNo is not null && record.StationNo != query.StationNo.Value)
        {
            return false;
        }

        if (query.Success is not null && record.Success != query.Success.Value)
        {
            return false;
        }

        if (!ContainsAny(record.RequestType, query.RequestType))
        {
            return false;
        }

        if (!ContainsAny($"{record.DeviceName} {record.DeviceId}", query.DeviceKeyword))
        {
            return false;
        }

        return ContainsAny($"{record.Message} {record.Error} {record.DeviceName} {record.DeviceId} {record.SystemType}", query.Keyword);
    }

    private string BuildLogPath(CenterPushLogRecord record)
    {
        var root = _settingsService.Get().LogDirectory;
        var datePart = record.ReceivedAt.ToString("yyyyMMdd");
        var devicePart = SanitizePathPart($"{FirstNonEmpty(record.DeviceName, "Device")}_{FirstNonEmpty(record.DeviceId, "Unknown")}");
        var stationPart = record.StationNo > 0 ? $"Station{record.StationNo}" : "Station0";
        var requestPart = SanitizePathPart(record.RequestType);
        return Path.Combine(root, datePart, devicePart, stationPart, $"{requestPart}.jsonl");
    }

    private static JsonNode? ToJsonNode<T>(T value)
    {
        try
        {
            return JsonSerializer.SerializeToNode(value, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string SanitizePathPart(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "NA" : value.Trim();
        return InvalidFileNameChars.Replace(normalized, "-");
    }

    private static bool ContainsAny(string? text, string? keyword)
    {
        return string.IsNullOrWhiteSpace(keyword)
            || (text ?? string.Empty).Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
}
