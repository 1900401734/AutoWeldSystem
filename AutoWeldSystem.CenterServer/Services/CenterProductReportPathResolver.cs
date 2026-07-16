using System.Security.Cryptography;
using System.Text;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 将设备编号和流转卡号解析为稳定、无字符替换碰撞且不能逃逸根目录的安全路径。
/// </summary>
internal sealed class CenterProductReportPathResolver
{
    private const int HashLength = 16;
    private const int ReadableSegmentLength = 40;

    /// <summary>
    /// 生成“root / 安全设备段 / 安全流转卡段.xlsx”，并做最终全路径边界校验。
    /// </summary>
    public string BuildReportPath(string dataDirectory, string? deviceId, string? workOrder)
    {
        var root = NormalizeRoot(dataDirectory);
        var devicePart = BuildSafeSegment(deviceId, "Device");
        var workOrderPart = BuildSafeSegment(workOrder, "WorkOrder");
        var reportPath = Path.GetFullPath(Path.Combine(root, devicePart, $"{workOrderPart}.xlsx"));
        EnsureInsideRoot(root, reportPath);
        return reportPath;
    }

    /// <summary>
    /// 规范化配置根目录；空路径直接拒绝。
    /// </summary>
    public string NormalizeRoot(string dataDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new ArgumentException("Center report data directory is required.", nameof(dataDirectory));
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(dataDirectory.Trim()));
    }

    /// <summary>
    /// 只枚举由本解析器生成的两级正式 XLSX，排除临时、备份、锁文件和未知工作簿。
    /// </summary>
    public IEnumerable<string> EnumerateReportPaths(string root)
    {
        foreach (var deviceDirectory in Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly))
        {
            if (!IsSafeSegment(Path.GetFileName(deviceDirectory)))
            {
                continue;
            }

            foreach (var reportPath in Directory.EnumerateFiles(deviceDirectory, "*.xlsx", SearchOption.TopDirectoryOnly))
            {
                if (IsSafeSegment(Path.GetFileNameWithoutExtension(reportPath)))
                {
                    yield return reportPath;
                }
            }
        }
    }

    private static string BuildSafeSegment(string? value, string fallback)
    {
        var original = value ?? string.Empty;
        var canonical = original.Trim();
        var builder = new StringBuilder(canonical.Length);
        foreach (var character in canonical)
        {
            builder.Append(IsUnsafeCharacter(character) ? '-' : character);
        }

        var readable = builder.ToString().Trim(' ', '.', '-');
        if (string.IsNullOrWhiteSpace(readable) || readable is "." or "..")
        {
            readable = fallback;
        }

        if (IsWindowsReservedName(readable))
        {
            readable = $"_{readable}";
        }

        if (readable.Length > ReadableSegmentLength)
        {
            readable = readable[..ReadableSegmentLength].TrimEnd(' ', '.');
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(original)))[..HashLength];
        return $"{readable}--{hash}";
    }

    private static bool IsUnsafeCharacter(char character)
        => character < 32 || character is '<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*';

    private static bool IsSafeSegment(string value)
    {
        var separatorIndex = value.LastIndexOf("--", StringComparison.Ordinal);
        if (separatorIndex <= 0 || value.Length - separatorIndex - 2 != HashLength)
        {
            return false;
        }

        return value[(separatorIndex + 2)..].All(Uri.IsHexDigit);
    }

    private static bool IsWindowsReservedName(string value)
    {
        var baseName = value.Split('.', 2)[0];
        if (baseName.Equals("CON", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("PRN", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("AUX", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("NUL", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return baseName.Length == 4
            && (baseName.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                || baseName.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
            && baseName[3] is >= '1' and <= '9';
    }

    private static void EnsureInsideRoot(string root, string candidatePath)
    {
        var rootPrefix = Path.EndsInDirectorySeparator(root)
            ? root
            : root + Path.DirectorySeparatorChar;
        if (!candidatePath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Center report path escaped the configured data directory.");
        }
    }
}
