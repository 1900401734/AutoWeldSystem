using System.Globalization;
using System.Text;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Production;

namespace AutoWeldSystem.Services.Log;

/// <summary>
/// 设备状态本地 JSONL 日志读写工具。
/// </summary>
public static class DeviceStatusLocalLogStore
{
    // ponytail: 设备状态写入量很低，先使用进程内全局锁；出现实测争用后再按日期文件拆锁。
    private static readonly object SyncRoot = new();

    /// <summary>
    /// 获取设备状态日志目录。
    /// LogDirectory 未配置时回退到程序目录下的 Logs，保持与其他本地日志一致。
    /// </summary>
    public static string GetLogDirectory(AppSettings settings)
    {
        var root = settings.LogDirectory;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(AppContext.BaseDirectory, "Logs");
        }

        return Path.Combine(root, AppConstants.LogCategories.DeviceStatus);
    }

    /// <summary>
    /// 追加设备状态首版本；无可靠记录键时拒绝写入。
    /// </summary>
    public static bool TryAppend(BizDeviceStatusLog entry, AppSettings settings)
    {
        if (DeviceStatusRecordIdentityRules.GetRecordKey(entry) is null)
        {
            return false;
        }

        lock (SyncRoot)
        {
            return TryAppendCore(entry, settings);
        }
    }

    /// <summary>
    /// 仅在同一记录键的首版本仍存在时追加结果版本。
    /// </summary>
    public static bool TryAppendVersion(BizDeviceStatusLog entry, AppSettings settings)
    {
        var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(entry);
        if (recordKey is null)
        {
            return false;
        }

        lock (SyncRoot)
        {
            var filePath = GetLogFilePath(settings, entry.OccurredTime);
            if (!File.Exists(filePath))
            {
                return false;
            }

            var sourceExists = ReadFile(filePath, onError: null)
                .Any(log => string.Equals(
                    DeviceStatusRecordIdentityRules.GetRecordKey(log),
                    recordKey,
                    StringComparison.OrdinalIgnoreCase));
            return sourceExists && TryAppendCore(entry, settings);
        }
    }

    /// <summary>
    /// 按记录键删除选中事件的全部追加版本。
    /// </summary>
    public static bool TryRemove(IReadOnlyCollection<BizDeviceStatusLog> entries, AppSettings settings)
    {
        var recordKeysByDate = entries
            .Select(entry => new
            {
                Entry = entry,
                RecordKey = DeviceStatusRecordIdentityRules.GetRecordKey(entry)
            })
            .Where(item => item.RecordKey is not null && item.Entry.OccurredTime != default)
            .GroupBy(item => item.Entry.OccurredTime.Date)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.RecordKey!).ToHashSet(StringComparer.OrdinalIgnoreCase));
        if (recordKeysByDate.Count == 0)
        {
            return true;
        }

        lock (SyncRoot)
        {
            return TryRewriteWithoutKeys(recordKeysByDate, settings);
        }
    }

    /// <summary>
    /// 读取每个记录键的最后追加版本。
    /// </summary>
    public static IReadOnlyList<BizDeviceStatusLog> Read(
        AppSettings settings,
        DateTime? from = null,
        DateTime? to = null,
        int maxCount = 200,
        Action<Exception, string>? onError = null)
    {
        var take = Math.Clamp(maxCount, 1, 5000);
        lock (SyncRoot)
        {
            return ReadLatestCore(settings, from, to, onError)
                .Where(entry => IsInRange(entry, from, to))
                .OrderByDescending(entry => entry.OccurredTime)
                .Take(take)
                .ToList();
        }
    }

    public static IReadOnlyList<BizDeviceStatusLog> ReadPending(
        AppSettings settings,
        Action<Exception, string>? onError = null)
    {
        lock (SyncRoot)
        {
            return ReadLatestCore(settings, from: null, to: null, onError: onError)
                .Where(entry => DeviceStatusUploadVisibilityRules.ShouldInclude(entry.ReportStatus))
                .OrderByDescending(entry => entry.OccurredTime)
                .ToList();
        }
    }

    public static BizDeviceStatusLog? ReadByRecordKey(
        AppSettings settings,
        string recordKey,
        Action<Exception, string>? onError = null)
    {
        var normalized = DeviceStatusRecordIdentityRules.NormalizeRecordKey(recordKey);
        if (normalized is null)
        {
            return null;
        }

        lock (SyncRoot)
        {
            return ReadLatestCore(settings, from: null, to: null, onError: onError)
                .FirstOrDefault(entry => string.Equals(
                    DeviceStatusRecordIdentityRules.GetRecordKey(entry),
                    normalized,
                    StringComparison.OrdinalIgnoreCase));
        }
    }

    public static BizDeviceStatusLog? ReadLatestForStation(
        AppSettings settings,
        int stationNo,
        Action<Exception, string>? onError = null)
    {
        lock (SyncRoot)
        {
            return ReadLatestCore(settings, from: null, to: null, onError: onError)
                .Where(entry => entry.StationNo == stationNo)
                .OrderByDescending(entry => entry.OccurredTime)
                .FirstOrDefault();
        }
    }

    private static bool TryAppendCore(BizDeviceStatusLog entry, AppSettings settings)
    {
        try
        {
            entry.OccurredTime = entry.OccurredTime == default ? DateTime.Now : entry.OccurredTime;
            var filePath = GetLogFilePath(settings, entry.OccurredTime);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var json = LocalJsonLogFormatter.Serialize(entry);
            File.AppendAllText(filePath, json + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IReadOnlyList<BizDeviceStatusLog> ReadLatestCore(
        AppSettings settings,
        DateTime? from,
        DateTime? to,
        Action<Exception, string>? onError)
    {
        var latestByKey = new Dictionary<string, BizDeviceStatusLog>(StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> filePaths;
        try
        {
            filePaths = EnumerateCandidateFiles(settings, from, to).ToList();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex, $"Directory={GetLogDirectory(settings)}");
            return Array.Empty<BizDeviceStatusLog>();
        }

        foreach (var filePath in filePaths)
        {
            foreach (var entry in ReadFile(filePath, onError))
            {
                var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(entry);
                if (recordKey is null)
                {
                    onError?.Invoke(
                        new InvalidDataException("设备状态记录缺少有效 RecordId 或旧 Id。"),
                        $"File={filePath}");
                    continue;
                }

                latestByKey[recordKey] = entry;
            }
        }

        return latestByKey.Values.ToList();
    }

    private static IReadOnlyList<BizDeviceStatusLog> ReadFile(
        string filePath,
        Action<Exception, string>? onError)
    {
        if (!File.Exists(filePath))
        {
            return Array.Empty<BizDeviceStatusLog>();
        }

        var entries = new List<BizDeviceStatusLog>();
        try
        {
            foreach (var record in LocalJsonLogFormatter.ReadAllRecords(filePath))
            {
                try
                {
                    var entry = LocalJsonLogFormatter.Deserialize<BizDeviceStatusLog>(record);
                    if (entry is not null)
                    {
                        entries.Add(entry);
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex, $"File={filePath}");
                }
            }
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex, $"File={filePath}");
        }

        return entries;
    }

    private static IEnumerable<string> EnumerateCandidateFiles(
        AppSettings settings,
        DateTime? from,
        DateTime? to)
    {
        if (from is not null || to is not null)
        {
            foreach (var date in EnumerateCandidateDates(from, to))
            {
                var filePath = GetLogFilePath(settings, date);
                if (File.Exists(filePath))
                {
                    yield return filePath;
                }
            }

            yield break;
        }

        var directory = GetLogDirectory(settings);
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        foreach (var filePath in Directory
            .EnumerateFiles(directory, "*.jsonl", SearchOption.TopDirectoryOnly)
            .Where(filePath => DateTime.TryParseExact(
                Path.GetFileNameWithoutExtension(filePath),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
            .OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase))
        {
            yield return filePath;
        }
    }

    private static bool TryRewriteWithoutKeys(
        IReadOnlyDictionary<DateTime, HashSet<string>> recordKeysByDate,
        AppSettings settings)
    {
        var rewrites = new List<LocalFileRewrite>();
        try
        {
            foreach (var (date, recordKeys) in recordKeysByDate)
            {
                var filePath = GetLogFilePath(settings, date);
                if (!File.Exists(filePath))
                {
                    continue;
                }

                var retainedRecords = LocalJsonLogFormatter.ReadAllRecords(filePath)
                    .Where(record => !ShouldRemove(record, recordKeys))
                    .ToList();
                rewrites.Add(new LocalFileRewrite(filePath, FormatRecords(retainedRecords)));
            }

            foreach (var rewrite in rewrites)
            {
                rewrite.TempPath = $"{rewrite.FilePath}.{Guid.NewGuid():N}.tmp";
                File.WriteAllText(rewrite.TempPath, rewrite.Content, Encoding.UTF8);
            }

            foreach (var rewrite in rewrites)
            {
                rewrite.BackupPath = $"{rewrite.FilePath}.{Guid.NewGuid():N}.bak";
                File.Copy(rewrite.FilePath, rewrite.BackupPath, overwrite: true);
                if (string.IsNullOrEmpty(rewrite.Content))
                {
                    File.Delete(rewrite.FilePath);
                }
                else
                {
                    File.Move(rewrite.TempPath!, rewrite.FilePath, overwrite: true);
                }

                rewrite.Applied = true;
            }

            return true;
        }
        catch
        {
            foreach (var rewrite in rewrites.Where(rewrite => rewrite.Applied).Reverse())
            {
                if (!string.IsNullOrWhiteSpace(rewrite.BackupPath) && File.Exists(rewrite.BackupPath))
                {
                    File.Copy(rewrite.BackupPath, rewrite.FilePath, overwrite: true);
                }
            }

            return false;
        }
        finally
        {
            foreach (var rewrite in rewrites)
            {
                TryDeleteFile(rewrite.TempPath);
                TryDeleteFile(rewrite.BackupPath);
            }
        }
    }

    private static bool ShouldRemove(string record, ISet<string> recordKeys)
    {
        try
        {
            var entry = LocalJsonLogFormatter.Deserialize<BizDeviceStatusLog>(record);
            var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(entry);
            return recordKey is not null && recordKeys.Contains(recordKey);
        }
        catch
        {
            return false;
        }
    }

    private static string GetLogFilePath(AppSettings settings, DateTime date)
        => Path.Combine(GetLogDirectory(settings), $"{date:yyyy-MM-dd}.jsonl");

    private static IEnumerable<DateTime> EnumerateCandidateDates(DateTime? from, DateTime? to)
    {
        var start = (from ?? to ?? DateTime.Today).Date;
        var end = (to ?? from ?? DateTime.Today).Date;
        if (end < start)
        {
            yield break;
        }

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    private static bool IsInRange(BizDeviceStatusLog entry, DateTime? from, DateTime? to)
        => (from is null || entry.OccurredTime >= from.Value)
            && (to is null || entry.OccurredTime <= to.Value);

    private static string FormatRecords(IEnumerable<string> records)
    {
        var values = records.ToList();
        return values.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine + Environment.NewLine, values) + Environment.NewLine + Environment.NewLine;
    }

    private static void TryDeleteFile(string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private sealed class LocalFileRewrite
    {
        public LocalFileRewrite(string filePath, string content)
        {
            FilePath = filePath;
            Content = content;
        }

        public string FilePath { get; }

        public string Content { get; }

        public string? TempPath { get; set; }

        public string? BackupPath { get; set; }

        public bool Applied { get; set; }
    }
}
