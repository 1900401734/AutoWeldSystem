using System.Text;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Services.Log;

/// <summary>
/// 设备状态本地 JSONL 日志读写工具。
/// 设备状态仍以数据库承载上报和重试状态，这里只负责给现场留可直接打开的本地文件。
/// </summary>
public static class DeviceStatusLocalLogStore
{
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
    /// 追加写入一条设备状态日志。
    /// 返回 false 表示本地文件写入失败，调用方应吞掉该失败，避免影响生产主流程。
    /// </summary>
    public static bool TryAppend(BizDeviceStatusLog entry, AppSettings settings)
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

    /// <summary>
    /// 从本地 JSONL 文件读取设备状态日志。
    /// 读取失败或没有文件时返回空集合，由业务服务决定是否回退数据库。
    /// </summary>
    public static IReadOnlyList<BizDeviceStatusLog> Read(
        AppSettings settings,
        DateTime? from = null,
        DateTime? to = null,
        int maxCount = 200)
    {
        var take = Math.Clamp(maxCount, 1, 5000);
        try
        {
            return EnumerateCandidateDates(from, to)
                .SelectMany(date => ReadDate(settings, date, take))
                .Where(entry => IsInRange(entry, from, to))
                .OrderByDescending(entry => entry.OccurredTime)
                .Take(take)
                .ToList();
        }
        catch
        {
            return Array.Empty<BizDeviceStatusLog>();
        }
    }

    private static IEnumerable<BizDeviceStatusLog> ReadDate(AppSettings settings, DateTime date, int take)
    {
        var filePath = GetLogFilePath(settings, date);
        if (!File.Exists(filePath))
        {
            return Array.Empty<BizDeviceStatusLog>();
        }

        return LocalJsonLogFormatter.ReadLatestRecords(filePath, take)
            .Select(TryDeserialize)
            .Where(entry => entry is not null)
            .Cast<BizDeviceStatusLog>();
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
    {
        return (from is null || entry.OccurredTime >= from.Value)
            && (to is null || entry.OccurredTime <= to.Value);
    }

    private static BizDeviceStatusLog? TryDeserialize(string record)
    {
        try
        {
            return string.IsNullOrWhiteSpace(record)
                ? null
                : LocalJsonLogFormatter.Deserialize<BizDeviceStatusLog>(record);
        }
        catch
        {
            return null;
        }
    }
}
