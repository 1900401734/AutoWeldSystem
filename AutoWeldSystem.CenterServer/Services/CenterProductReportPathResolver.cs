using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AutoWeldSystem.Core.DTOs.CenterServer;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>解析新归档路径和旧哈希路径，所有目标均限制在配置根目录内。</summary>
internal sealed class CenterProductReportPathResolver
{
    private const int HashLength = 16;
    private const int ReadableSegmentLength = 40;

    public static bool HasArchiveIdentity(CenterProductReportRequest request)
        => request.ReportSequenceNo.HasValue || request.ReportFileName is not null;

    public static void ValidateArchiveIdentity(CenterProductReportRequest request)
    {
        if (!HasArchiveIdentity(request)) return;
        if (request.ReportSequenceNo is not > 0 || !IsArchiveFileName(request.ReportFileName)
            || !request.ReportFileName!.EndsWith(
                $"_BG_{request.ReportSequenceNo.Value.ToString("D3", CultureInfo.InvariantCulture)}.xlsx",
                StringComparison.Ordinal))
        {
            throw new ArgumentException("ReportFileName must be a safe XLSX filename with the declared positive ReportSequenceNo.");
        }
        if (request.StartTime == default)
        {
            throw new ArgumentException("StartTime is required for report archives.");
        }
    }

    /// <summary>保留旧算法，已落盘的历史文件不能因版本升级失去定位。</summary>
    public string BuildReportPath(string dataDirectory, string? deviceId, string? workOrder)
    {
        var root = NormalizeRoot(dataDirectory);
        return CombineInsideRoot(root, BuildSafeSegment(deviceId, "Device"), $"{BuildSafeSegment(workOrder, "WorkOrder")}.xlsx");
    }

    public string BuildArchivePath(string dataDirectory, CenterProductReportRequest request)
    {
        ValidateArchiveIdentity(request);
        return CombineInsideRoot(NormalizeRoot(dataDirectory), BuildDeviceSegment(request.DeviceId),
            request.StartTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture), request.ReportFileName!);
    }

    public string BuildRoutingLockPath(string dataDirectory, CenterProductReportRequest request)
    {
        // 同一逻辑任务的旧/新协议必须先串行选路，再获取正式文件锁，防止双写两份文件。
        var device = request.DeviceId.Trim().ToUpperInvariant();
        var order = request.WorkOrder.Trim().ToUpperInvariant();
        var identity = $"{device.Length}:{device}{order.Length}:{order}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        return CombineInsideRoot(NormalizeRoot(dataDirectory), ".locks", hash);
    }

    public string BuildReportLockPath(string dataDirectory, string reportPath)
    {
        var root = NormalizeRoot(dataDirectory);
        // 相对路径让不同盘符映射同一共享目录时仍使用同一把锁。
        var identity = Path.GetRelativePath(root, reportPath);
        if (OperatingSystem.IsWindows()) identity = identity.ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        return CombineInsideRoot(root, ".locks", $"report-{hash}");
    }

    public string NormalizeRoot(string dataDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new ArgumentException("Center report data directory is required.", nameof(dataDirectory));
        }
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(dataDirectory.Trim()));
    }

    public IEnumerable<string> EnumerateTaskArchivePaths(string root, CenterProductReportRequest request)
    {
        if (request.StartTime == default) yield break;
        var directory = CombineInsideRoot(root, BuildDeviceSegment(request.DeviceId),
            request.StartTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
        if (!Directory.Exists(directory)) yield break;
        foreach (var path in Directory.EnumerateFiles(directory, "*.xlsx", SearchOption.TopDirectoryOnly))
        {
            if (IsArchiveFileName(Path.GetFileName(path))) yield return path;
        }
    }

    /// <summary>只枚举两种正式结构，锁文件、临时工作簿和备份不参与历史统计。</summary>
    public IEnumerable<string> EnumerateReportPaths(string root)
    {
        foreach (var deviceDirectory in Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly))
        {
            var deviceName = Path.GetFileName(deviceDirectory);
            if (deviceName == ".locks") continue;
            if (IsSafeSegment(deviceName))
            {
                foreach (var path in Directory.EnumerateFiles(deviceDirectory, "*.xlsx", SearchOption.TopDirectoryOnly))
                {
                    if (IsSafeSegment(Path.GetFileNameWithoutExtension(path))) yield return path;
                }
            }
            foreach (var dateDirectory in Directory.EnumerateDirectories(deviceDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                if (!DateTime.TryParseExact(Path.GetFileName(dateDirectory), "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out _)) continue;
                foreach (var path in Directory.EnumerateFiles(dateDirectory, "*.xlsx", SearchOption.TopDirectoryOnly))
                {
                    if (IsArchiveFileName(Path.GetFileName(path))) yield return path;
                }
            }
        }
    }

    private static string BuildDeviceSegment(string? value)
        => IsLegalSegment(value) && value!.Length <= 80
            && !value.Equals(".locks", StringComparison.OrdinalIgnoreCase)
                ? value : BuildSafeSegment(value, "Device");

    private static bool IsArchiveFileName(string? value)
    {
        if (!IsLegalSegment(value) || value!.StartsWith('.') || !value.EndsWith(".xlsx", StringComparison.Ordinal)) return false;
        var marker = value.LastIndexOf("_BG_", StringComparison.Ordinal);
        return marker > 0 && int.TryParse(value.AsSpan(marker + 4, value.Length - marker - 9),
            NumberStyles.None, CultureInfo.InvariantCulture, out var sequence) && sequence > 0;
    }

    private static bool IsLegalSegment(string? value)
        => !string.IsNullOrWhiteSpace(value) && value.Length <= 255
            && value == value.Trim() && !value.EndsWith('.')
            && value is not "." and not ".." && !value.Any(IsUnsafeCharacter)
            && !IsWindowsReservedName(value);

    private static string BuildSafeSegment(string? value, string fallback)
    {
        var original = value ?? string.Empty;
        var builder = new StringBuilder();
        foreach (var character in original.Trim())
        {
            builder.Append(IsUnsafeCharacter(character) ? '-' : character);
        }
        var readable = builder.ToString().Trim(' ', '.', '-');
        if (string.IsNullOrWhiteSpace(readable) || readable is "." or "..") readable = fallback;
        if (IsWindowsReservedName(readable)) readable = $"_{readable}";
        if (readable.Length > ReadableSegmentLength) readable = readable[..ReadableSegmentLength].TrimEnd(' ', '.');
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(original)))[..HashLength];
        return $"{readable}--{hash}";
    }

    private static bool IsUnsafeCharacter(char character)
        => character < 32 || character is '<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*';

    private static bool IsSafeSegment(string value)
    {
        var separatorIndex = value.LastIndexOf("--", StringComparison.Ordinal);
        return separatorIndex > 0 && value.Length - separatorIndex - 2 == HashLength
            && value[(separatorIndex + 2)..].All(Uri.IsHexDigit);
    }

    private static bool IsWindowsReservedName(string value)
    {
        var baseName = value.Split('.', 2)[0];
        return baseName.Equals("CON", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("PRN", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("AUX", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("NUL", StringComparison.OrdinalIgnoreCase)
            || (baseName.Length == 4
                && (baseName.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                    || baseName.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
                && baseName[3] is >= '1' and <= '9');
    }

    private static string CombineInsideRoot(string root, params string[] parts)
    {
        var candidate = Path.GetFullPath(Path.Combine(new[] { root }.Concat(parts).ToArray()));
        var prefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Center report path escaped the configured data directory.");
        }
        return candidate;
    }
}
