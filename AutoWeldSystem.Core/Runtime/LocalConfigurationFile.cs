using System.Text;
using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Runtime;

/// <summary>
/// 本机配置文件的准备结果。
/// </summary>
public enum LocalConfigurationState
{
    /// <summary>ProgramData 下已有配置文件，直接使用。</summary>
    Existing,

    /// <summary>ProgramData 下没有配置，已从旧版程序目录复制一份。</summary>
    Migrated,

    /// <summary>两处都没有配置，已生成模板，程序应提示后退出。</summary>
    TemplateCreated
}

public sealed class LocalConfigurationResult
{
    public LocalConfigurationResult(LocalConfigurationState state, string filePath, string? migratedFromPath)
    {
        State = state;
        FilePath = filePath;
        MigratedFromPath = migratedFromPath;
    }

    public LocalConfigurationState State { get; }

    /// <summary>
    /// 程序实际读取的配置文件完整路径。
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// 迁移来源路径；未发生迁移时为 null。
    /// </summary>
    public string? MigratedFromPath { get; }
}

/// <summary>
/// 设备端数据库连接配置文件的定位与准备。
/// 配置固定放在 ProgramData 独立目录，现场只替换 exe 更新程序时不会被覆盖；
/// 旧版本把配置放在程序目录，新版本首次启动时自动复制一份过来，避免升级后反复提示缺配置。
/// </summary>
public static class LocalConfigurationFile
{
    public const string FileName = "appsettings.json";
    public const string ConnectionStringKey = "Database:ConnectionString";

    public static string ResolveDirectory(string commonApplicationDataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commonApplicationDataRoot);
        return Path.Combine(commonApplicationDataRoot, AppConstants.ApplicationName);
    }

    public static LocalConfigurationResult Prepare(string configDirectory, string? legacyDirectory, string templateContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDirectory);
        ArgumentNullException.ThrowIfNull(templateContent);

        var filePath = Path.Combine(configDirectory, FileName);
        if (File.Exists(filePath))
        {
            return new LocalConfigurationResult(LocalConfigurationState.Existing, filePath, null);
        }

        Directory.CreateDirectory(configDirectory);

        var legacyPath = string.IsNullOrWhiteSpace(legacyDirectory) ? null : Path.Combine(legacyDirectory, FileName);
        if (legacyPath is not null && File.Exists(legacyPath))
        {
            // 只复制不删除：旧文件可能仍被现场已注册的备份计划任务引用，由现场自行清理。
            File.Copy(legacyPath, filePath);
            return new LocalConfigurationResult(LocalConfigurationState.Migrated, filePath, legacyPath);
        }

        File.WriteAllText(filePath, templateContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return new LocalConfigurationResult(LocalConfigurationState.TemplateCreated, filePath, null);
    }
}
