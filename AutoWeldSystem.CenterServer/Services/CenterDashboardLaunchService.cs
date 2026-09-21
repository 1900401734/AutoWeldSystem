using System.Diagnostics;
using Microsoft.Win32;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>独立于接收服务的窗口启动器，只在监听成功后启动一次。</summary>
internal sealed class CenterDashboardLaunchService
{
    private readonly ILogger<CenterDashboardLaunchService> _logger;
    private int _opened;

    public CenterDashboardLaunchService(ILogger<CenterDashboardLaunchService> logger) => _logger = logger;

    public void OpenOnce(IEnumerable<string> addresses)
    {
        if (!OperatingSystem.IsWindows() || !Environment.UserInteractive || Interlocked.Exchange(ref _opened, 1) != 0) return;
        try
        {
            var url = ResolveDashboardUrl(addresses);
            var edgePath = FindEdge();
            if (edgePath is null)
            {
                _logger.LogWarning("未找到 Edge，使用默认浏览器打开看板。后台接收服务不受影响。");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                return;
            }
            var start = new ProcessStartInfo(edgePath) { UseShellExecute = false };
            start.ArgumentList.Add($"--app={url}");
            start.ArgumentList.Add("--no-first-run");
            start.ArgumentList.Add("--no-default-browser-check");
            start.ArgumentList.Add($"--user-data-dir={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoWeldSystem", "CenterDashboardBrowser")}");
            Process.Start(start)?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "无法自动打开看板，请通过浏览器访问本机服务。接收服务继续运行。");
        }
    }

    internal static string ResolveDashboardUrl(IEnumerable<string> addresses)
    {
        foreach (var address in addresses.OrderBy(address => address.StartsWith("https:", StringComparison.OrdinalIgnoreCase)))
        {
            var candidate = address.Replace("://*:", "://localhost:", StringComparison.Ordinal)
                .Replace("://+:", "://localhost:", StringComparison.Ordinal);
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) continue;
            var builder = new UriBuilder(uri) { Path = "/", Query = string.Empty, Fragment = string.Empty };
            if (uri.Host is "0.0.0.0" or "[::]" or "::") builder.Host = "localhost";
            return builder.Uri.AbsoluteUri;
        }
        throw new InvalidOperationException("中心服务没有可用于打开看板的 HTTP 监听地址。");
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static string? FindEdge()
    {
        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            using var key = hive.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe");
            if (key?.GetValue(null) is string path && File.Exists(path.Trim('"'))) return path.Trim('"');
        }
        foreach (var folder in new[] { Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.LocalApplicationData })
        {
            var path = Path.Combine(Environment.GetFolderPath(folder), "Microsoft", "Edge", "Application", "msedge.exe");
            if (File.Exists(path)) return path;
        }
        return null;
    }
}
