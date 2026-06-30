using System.Text;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Services.Log;

/// <summary>
/// Writes independent device lifecycle logs to local JSONL files.
/// The service is intentionally file-based so device startup and alarm evidence remains available without database access.
/// </summary>
public sealed class DeviceLifecycleLogService : IDeviceLifecycleLogService
{
    private readonly IAppSettingsService _settingsService;
    private readonly object _writeLock = new();
    private AppSettings _currentSettings;

    public DeviceLifecycleLogService(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
    }

    public event EventHandler<DeviceLifecycleLogEntry>? LogWritten;

    public void Write(DeviceLifecycleLogEntry entry)
    {
        try
        {
            entry.OccurredTime = entry.OccurredTime == default ? DateTime.Now : entry.OccurredTime;
            var filePath = GetLogFilePath(entry.OccurredTime);
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
            // Lifecycle log failures must never interrupt PLC collection or MES upload.
        }
    }

    public IReadOnlyList<DeviceLifecycleLogEntry> GetByDate(DateTime date, int take = 1000)
    {
        try
        {
            var filePath = GetLogFilePath(date);
            if (!File.Exists(filePath))
            {
                return Array.Empty<DeviceLifecycleLogEntry>();
            }

            return LocalJsonLogFormatter.ReadLatestRecords(filePath, Math.Max(1, take))
                .Reverse()
                .Select(TryDeserialize)
                .Where(entry => entry is not null)
                .Cast<DeviceLifecycleLogEntry>()
                .ToList();
        }
        catch
        {
            return Array.Empty<DeviceLifecycleLogEntry>();
        }
    }

    public string GetLogDirectory()
    {
        var root = CurrentSettings.LogDirectory;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(AppContext.BaseDirectory, "Logs");
        }

        return Path.Combine(root, AppConstants.LogCategories.DeviceLifecycle);
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }

    private string GetLogFilePath(DateTime date)
        => Path.Combine(GetLogDirectory(), $"{date:yyyy-MM-dd}.jsonl");

    private static DeviceLifecycleLogEntry? TryDeserialize(string record)
    {
        try
        {
            return string.IsNullOrWhiteSpace(record)
                ? null
                : LocalJsonLogFormatter.Deserialize<DeviceLifecycleLogEntry>(record);
        }
        catch
        {
            return null;
        }
    }
}
