namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序名称解析规则。
/// 统一维护程序名称里的业务片段，避免 MES 下载和本地生成逻辑各写一套。
/// </summary>
public static class ProgramNameRules
{
    private const string ComponentStartMarker = "_CX_";
    private const string ComponentEndMarker = "_DH_";

    /// <summary>
    /// Parsed fields from the standard program name format.
    /// </summary>
    public sealed record ProgramNameParts(
        string DeviceId,
        string ComponentCode,
        int SequenceNumber,
        string ProductNum,
        string Description);

    /// <summary>
    /// Builds a standard program name with an optional local description suffix.
    /// </summary>
    public static string BuildProgramName(
        string? deviceId,
        string? componentCode,
        int sequenceNumber,
        string? productNum,
        string? description)
    {
        var normalizedDescription = NormalizeOptionalNamePart(description);
        var baseName = string.Join(
            "_",
            NormalizeNamePart(deviceId),
            "CX",
            NormalizeComponentNamePart(componentCode),
            "DH",
            Math.Max(1, sequenceNumber).ToString("000"),
            NormalizeNamePart(productNum?.Replace("#", string.Empty, StringComparison.Ordinal) ?? string.Empty));

        return string.IsNullOrWhiteSpace(normalizedDescription)
            ? baseName
            : $"{baseName}_{normalizedDescription}";
    }

    /// <summary>
    /// Parses the standard program name and its optional description suffix.
    /// </summary>
    public static bool TryParse(string? programName, out ProgramNameParts parts)
    {
        parts = new ProgramNameParts(string.Empty, string.Empty, 0, string.Empty, string.Empty);
        if (string.IsNullOrWhiteSpace(programName))
        {
            return false;
        }

        var normalizedName = programName.Trim();
        var startIndex = normalizedName.IndexOf(ComponentStartMarker, StringComparison.OrdinalIgnoreCase);
        if (startIndex <= 0)
        {
            return false;
        }

        var componentStart = startIndex + ComponentStartMarker.Length;
        var endIndex = normalizedName.IndexOf(ComponentEndMarker, componentStart, StringComparison.OrdinalIgnoreCase);
        if (endIndex <= componentStart)
        {
            return false;
        }

        var deviceId = normalizedName[..startIndex].Trim();
        var componentCode = normalizedName[componentStart..endIndex].Trim();
        var suffix = normalizedName[(endIndex + ComponentEndMarker.Length)..]
            .Split('_', StringSplitOptions.None);
        if (suffix.Length < 2
            || !int.TryParse(suffix[0], out var sequenceNumber)
            || sequenceNumber <= 0
            || string.IsNullOrWhiteSpace(suffix[1]))
        {
            return false;
        }

        var description = suffix.Length > 2
            ? string.Join("_", suffix[2..]).Trim()
            : string.Empty;
        if (suffix.Length > 2 && string.IsNullOrWhiteSpace(description))
        {
            return false;
        }

        parts = new ProgramNameParts(
            deviceId,
            componentCode,
            sequenceNumber,
            suffix[1].Trim().Replace("#", string.Empty, StringComparison.Ordinal),
            description);
        return true;
    }

    /// <summary>
    /// 从程序名称中提取部件图号。
    /// 当前程序名称格式为：设备编号_CX_部件图号_DH_流水号_产品工号。
    /// </summary>
    /// <param name="programName">MES 或本地维护的程序名称。</param>
    /// <param name="componentCode">解析成功时返回部件图号；失败时返回空字符串。</param>
    /// <returns>解析到非空部件图号时返回 true。</returns>
    public static bool TryExtractComponentCode(string? programName, out string componentCode)
    {
        componentCode = string.Empty;
        if (string.IsNullOrWhiteSpace(programName))
        {
            return false;
        }

        if (TryParse(programName, out var parsed))
        {
            componentCode = parsed.ComponentCode;
            return true;
        }

        var normalizedName = programName.Trim();
        var startIndex = normalizedName.IndexOf(ComponentStartMarker, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
        {
            return false;
        }

        var componentStart = startIndex + ComponentStartMarker.Length;
        var endIndex = normalizedName.IndexOf(ComponentEndMarker, componentStart, StringComparison.OrdinalIgnoreCase);
        if (endIndex <= componentStart)
        {
            return false;
        }

        var extracted = normalizedName[componentStart..endIndex].Trim();
        if (string.IsNullOrWhiteSpace(extracted))
        {
            return false;
        }

        componentCode = extracted;
        return true;
    }

    /// <summary>
    /// 部件图号片段保持用户原样输入，只去掉首尾空白。
    /// 现场图号本身带小数点（例如 RY.682.100），过滤后会与图纸不一致，无法反查部件。
    /// </summary>
    private static string NormalizeComponentNamePart(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length == 0 ? "NA" : trimmed;
    }

    private static string NormalizeNamePart(string? value)
    {
        var chars = (value ?? string.Empty)
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '-')
            .ToArray();

        return chars.Length == 0 ? "NA" : new string(chars);
    }

    private static string NormalizeOptionalNamePart(string? value)
    {
        var chars = (value ?? string.Empty)
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '-')
            .ToArray();

        return new string(chars);
    }
}
