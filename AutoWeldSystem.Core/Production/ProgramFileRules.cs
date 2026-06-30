using System.Text;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序 JSON 文件的命名、路径和编码规则。
/// </summary>
public static class ProgramFileRules
{
    /// <summary>
    /// 程序文件默认目录。
    /// </summary>
    public const string DefaultProgramFileDirectory = @"D:\AutoWeldProgramFiles";

    /// <summary>
    /// 自动生成的程序内容文件默认使用 JSON 扩展名。
    /// </summary>
    public const string DefaultProgramFileType = ".json";

    /// <summary>
    /// 按“配方号_程序名称.json”生成安全文件名。
    /// </summary>
    public static string BuildFileName(string? recipeCode, string? programName)
    {
        // recipeCode 仅为兼容旧调用保留；文件命名现在只依赖程序名称。
        _ = recipeCode;
        var normalizedProgramName = NormalizeRequired(programName, "程序名称不能为空，无法生成程序文件名。");
        return $"{SanitizeFileNamePart(normalizedProgramName)}.json";
    }

    /// <summary>
    /// 根据目录、配方号和程序名称生成完整程序文件路径。
    /// </summary>
    public static string BuildFilePath(string? directory, string? recipeCode, string? programName)
    {
        var normalizedDirectory = string.IsNullOrWhiteSpace(directory)
            ? DefaultProgramFileDirectory
            : directory.Trim();
        return Path.Combine(normalizedDirectory, BuildFileName(recipeCode, programName));
    }

    /// <summary>
    /// 根据文件名或路径解析 MES 需要的文件类型字符串。
    /// </summary>
    public static string ResolveFileType(string? fileNameOrPath)
    {
        var extension = Path.GetExtension(fileNameOrPath?.Trim() ?? string.Empty);
        return string.IsNullOrWhiteSpace(extension)
            ? DefaultProgramFileType
            : extension.ToLowerInvariant();
    }

    /// <summary>
    /// 将 JSON 内容按 UTF-8 编码后转换为 Base64。
    /// </summary>
    public static string EncodeJsonToBase64(string? json)
    {
        var normalizedJson = string.IsNullOrWhiteSpace(json) ? "{}" : json.Trim();
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(normalizedJson));
    }

    private static string SanitizeFileNamePart(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        var chars = value
            .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
            .ToArray();

        return new string(chars);
    }

    private static string NormalizeRequired(string? value, string message)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new InvalidOperationException(message)
            : normalized;
    }
}
