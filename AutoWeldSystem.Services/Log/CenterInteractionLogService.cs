using System.Text;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Services.Log;

/// <summary>
/// 中心服务器交互日志文件服务。
/// </summary>
public sealed class CenterInteractionLogService : ICenterInteractionLogService
{
    // 全量记录时遥测每 5 秒一条，单日文件可达数十 MB；限制尾部读取字节数避免界面加载随文件增长变慢。
    private const long MaxHistoryReadBytes = 8L * 1024 * 1024;

    private readonly IAppSettingsService _settingsService;
    private readonly object _writeLock = new();
    private AppSettings _currentSettings;

    public CenterInteractionLogService(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
    }

    public event EventHandler<CenterInteractionLogEntry>? LogWritten;

    public void Write(CenterInteractionLogEntry entry)
    {
        try
        {
            var filePath = GetLogFilePath(entry.SendTime);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var json = LocalJsonLogFormatter.Serialize(entry);

            lock (_writeLock)
            {
                File.AppendAllText(filePath, json + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
            }

            LogWritten?.Invoke(this, entry);
        }
        catch
        {
            // 日志失败不能影响中心服务器推送主流程，避免一次磁盘异常导致遥测或产品上报失败。
        }
    }

    public IReadOnlyList<CenterInteractionLogEntry> GetByDate(DateTime date, int take = 500)
    {
        try
        {
            var filePath = GetLogFilePath(date);
            if (!File.Exists(filePath))
            {
                return Array.Empty<CenterInteractionLogEntry>();
            }

            return LocalJsonLogFormatter.ReadLatestRecords(filePath, Math.Max(1, take), MaxHistoryReadBytes)
                .Reverse()
                .Select(TryDeserialize)
                .Where(entry => entry is not null)
                .Cast<CenterInteractionLogEntry>()
                .ToList();
        }
        catch
        {
            return Array.Empty<CenterInteractionLogEntry>();
        }
    }

    public string GetLogDirectory()
    {
        var root = CurrentSettings.LogDirectory;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(AppContext.BaseDirectory, "Logs");
        }

        return Path.Combine(root, AppConstants.LogCategories.CenterServer);
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

    private static CenterInteractionLogEntry? TryDeserialize(string record)
    {
        if (string.IsNullOrWhiteSpace(record))
        {
            return null;
        }

        try
        {
            return LocalJsonLogFormatter.Deserialize<CenterInteractionLogEntry>(record);
        }
        catch
        {
            return null;
        }
    }
}
