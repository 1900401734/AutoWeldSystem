using System.Text.Json;
using AutoWeldSystem.CenterServer.Configuration;
using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 读取和保存中心服务器本机配置。
/// 配置文件放在程序目录下，避免依赖业务数据库启动成功后才能读取路径。
/// </summary>
public sealed class CenterServerSettingsService
{
    private const string SettingsFileName = "center-server-settings.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IConfiguration _configuration;
    private readonly object _syncRoot = new();
    private CenterServerLocalSettings? _cachedSettings;

    public CenterServerSettingsService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 保存配置后通知使用方刷新路径或运行参数。
    /// </summary>
    public event EventHandler<CenterServerLocalSettings>? SettingsChanged;

    /// <summary>
    /// 获取当前配置；首次读取时会自动创建默认配置文件。
    /// </summary>
    public CenterServerLocalSettings Get()
    {
        lock (_syncRoot)
        {
            _cachedSettings ??= LoadOrCreate();
            return Clone(_cachedSettings);
        }
    }

    /// <summary>
    /// 保存用户在看板设置页调整后的配置。
    /// </summary>
    public void Save(CenterServerLocalSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        CenterServerLocalSettings saved;
        lock (_syncRoot)
        {
            saved = Normalize(settings);
            Directory.CreateDirectory(Path.GetDirectoryName(GetSettingsFilePath())!);
            File.WriteAllText(GetSettingsFilePath(), JsonSerializer.Serialize(saved, JsonOptions));
            _cachedSettings = Clone(saved);
        }

        SettingsChanged?.Invoke(this, Clone(saved));
    }

    private CenterServerLocalSettings LoadOrCreate()
    {
        var filePath = GetSettingsFilePath();
        if (File.Exists(filePath))
        {
            try
            {
                var settings = JsonSerializer.Deserialize<CenterServerLocalSettings>(File.ReadAllText(filePath));
                if (settings is not null)
                {
                    return Normalize(settings);
                }
            }
            catch
            {
                // 配置文件损坏时使用默认值继续启动，避免中心服务器无法打开。
            }
        }

        var defaults = BuildDefaults();
        Save(defaults);
        return defaults;
    }

    private CenterServerLocalSettings BuildDefaults()
    {
        return Normalize(new CenterServerLocalSettings
        {
            EnableAutoStart = _configuration.GetValue("CenterServer:EnableAutoStart", true),
            LogDirectory = FirstNonEmpty(
                _configuration.GetValue<string>("CenterServer:DashboardLogDirectory"),
                _configuration.GetValue<string>("CenterServer:LogDirectory"),
                Path.Combine(AppContext.BaseDirectory, "CenterLogs")),
            DataDirectory = FirstNonEmpty(
                _configuration.GetValue<string>("CenterServer:DataDirectory"),
                _configuration.GetValue<string>("CenterServer:ReportDirectory"),
                Path.Combine(AppContext.BaseDirectory, "CenterData")),
            OfflineTimeoutSeconds = _configuration.GetValue(
                "CenterServer:OfflineTimeoutSeconds",
                CenterServerConstants.DefaultOfflineTimeoutSeconds)
        });
    }

    private static CenterServerLocalSettings Normalize(CenterServerLocalSettings settings)
    {
        return new CenterServerLocalSettings
        {
            EnableAutoStart = settings.EnableAutoStart,
            LogDirectory = NormalizePath(settings.LogDirectory, Path.Combine(AppContext.BaseDirectory, "CenterLogs")),
            DataDirectory = NormalizePath(settings.DataDirectory, Path.Combine(AppContext.BaseDirectory, "CenterData")),
            OfflineTimeoutSeconds = Math.Clamp(
                settings.OfflineTimeoutSeconds,
                3,
                3600)
        };
    }

    private static CenterServerLocalSettings Clone(CenterServerLocalSettings settings)
    {
        return new CenterServerLocalSettings
        {
            EnableAutoStart = settings.EnableAutoStart,
            LogDirectory = settings.LogDirectory,
            DataDirectory = settings.DataDirectory,
            OfflineTimeoutSeconds = settings.OfflineTimeoutSeconds
        };
    }

    private static string NormalizePath(string? value, string fallback)
    {
        var path = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    private static string GetSettingsFilePath()
        => Path.Combine(AppContext.BaseDirectory, SettingsFileName);
}
