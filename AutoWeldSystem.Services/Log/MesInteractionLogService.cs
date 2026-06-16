using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Services.Log;

/// <summary>
/// MES 交互日志文件服务。
/// </summary>
public sealed class MesInteractionLogService : IMesInteractionLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IAppSettingsService _settingsService;
    private readonly object _writeLock = new();
    private AppSettings _currentSettings;

    public MesInteractionLogService(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
    }

    public event EventHandler<MesInteractionLogEntry>? LogWritten;

    public void Write(MesInteractionLogEntry entry)
    {
        try
        {
            var filePath = GetLogFilePath(entry.SendTime);
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
            // 日志失败不能影响 MES 主流程，避免一次磁盘异常导致生产接口调用失败。
        }
    }

    public IReadOnlyList<MesInteractionLogEntry> GetByDate(DateTime date, int take = 500)
    {
        try
        {
            var filePath = GetLogFilePath(date);
            if (!File.Exists(filePath))
            {
                return Array.Empty<MesInteractionLogEntry>();
            }

            return ReadLatestRecords(filePath, Math.Max(1, take))
                .Reverse()
                .Select(TryDeserialize)
                .Where(it => it is not null)
                .Cast<MesInteractionLogEntry>()
                .ToList();
        }
        catch
        {
            return Array.Empty<MesInteractionLogEntry>();
        }
    }

    public string GetLogDirectory()
    {
        var root = CurrentSettings.LogDirectory;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(AppContext.BaseDirectory, "Logs");
        }

        return Path.Combine(root, AppConstants.LogCategories.Mes);
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }

    private string GetLogFilePath(DateTime date)
    {
        return Path.Combine(GetLogDirectory(), $"{date:yyyy-MM-dd}.jsonl");
    }

    private static IEnumerable<string> ReadLatestRecords(string filePath, int take)
    {
        var records = new Queue<string>(take);

        // 只保留最后 take 条非空记录，避免日志文件变大后一次性把整份文件读进内存。
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

    private static MesInteractionLogEntry? TryDeserialize(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<MesInteractionLogEntry>(line, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
