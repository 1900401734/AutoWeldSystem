using System.Reflection;
using Microsoft.Win32;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 中心服务器 Windows 登录自启服务。
/// 使用当前用户 Run 注册表项，不需要管理员权限。
/// </summary>
public sealed class CenterServerWindowsStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "AutoWeldSystem.CenterServer";

    private readonly ILogger<CenterServerWindowsStartupService> _logger;

    public CenterServerWindowsStartupService(ILogger<CenterServerWindowsStartupService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 根据配置启用或关闭中心服务器开机自启。
    /// </summary>
    public void Apply(bool enabled)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                _logger.LogInformation("Center server auto-start is skipped because the current OS is not Windows.");
                return;
            }

            if (enabled)
            {
                using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
                    ?? throw new InvalidOperationException("无法打开当前用户开机启动注册表项。");
                runKey.SetValue(RunValueName, BuildStartupCommand(), RegistryValueKind.String);
                return;
            }

            using var existingRunKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            existingRunKey?.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Apply center server auto-start failed. Enabled={Enabled}", enabled);
        }
    }

    private static string BuildStartupCommand()
    {
        var processPath = Environment.ProcessPath;
        var assemblyPath = Assembly.GetEntryAssembly()?.Location;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            throw new InvalidOperationException("无法获取中心服务器启动进程路径。");
        }

        if (IsDotNetHost(processPath) && !string.IsNullOrWhiteSpace(assemblyPath))
        {
            return $"{Quote(processPath)} {Quote(assemblyPath)}";
        }

        return Quote(processPath);
    }

    private static bool IsDotNetHost(string processPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(processPath);
        return string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase);
    }

    private static string Quote(string value)
        => $"\"{value}\"";
}
