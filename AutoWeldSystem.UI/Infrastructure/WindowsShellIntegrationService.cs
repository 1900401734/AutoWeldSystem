using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using Microsoft.Win32;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// Windows 桌面外壳集成服务，集中处理当前用户开机自启和桌面快捷方式。
/// </summary>
internal sealed class WindowsShellIntegrationService : IWindowsShellIntegrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ShortcutExtension = ".lnk";

    private readonly IAppSettingsService _settingsService;
    private readonly IProgramExceptionLogService _exceptionLogService;

    public WindowsShellIntegrationService(
        IAppSettingsService settingsService,
        IProgramExceptionLogService exceptionLogService)
    {
        _settingsService = settingsService;
        _exceptionLogService = exceptionLogService;
    }

    /// <summary>
    /// 从当前配置读取开机自启开关，并同步 Windows 入口。
    /// </summary>
    public void ApplyStartupIntegration()
    {
        try
        {
            ApplyStartupIntegration(_settingsService.Get());
        }
        catch (Exception ex)
        {
            LogFailure(ex, "WindowsShellIntegration.ApplyStartupIntegration");
        }
    }

    /// <summary>
    /// 同步开机自启并确保桌面快捷方式存在；任何失败只记录日志，不阻断主程序启动。
    /// </summary>
    public void ApplyStartupIntegration(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        SyncAutoStart(settings.EnableAutoStart ?? true);
        EnsureDesktopShortcut();
    }

    /// <summary>
    /// 使用当前用户 Run 注册表项启用或关闭登录后自启，不要求管理员权限。
    /// </summary>
    public void SyncAutoStart(bool enabled)
    {
        try
        {
            if (enabled)
            {
                using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, true)
                    ?? throw new InvalidOperationException("无法打开当前用户开机启动注册表项。");
                runKey.SetValue(AppConstants.ApplicationName, QuoteExecutablePath(GetExecutablePath()), RegistryValueKind.String);
                return;
            }

            using var existingRunKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            existingRunKey?.DeleteValue(AppConstants.ApplicationName, false);
        }
        catch (Exception ex)
        {
            LogFailure(ex, "WindowsShellIntegration.SyncAutoStart", $"Enabled={enabled}");
        }
    }

    /// <summary>
    /// 当前用户桌面不存在快捷方式时自动创建，方便现场操作人员从桌面进入程序。
    /// </summary>
    public void EnsureDesktopShortcut()
    {
        try
        {
            var shortcutPath = GetDesktopShortcutPath();
            if (File.Exists(shortcutPath))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);
            CreateShortcut(shortcutPath, GetExecutablePath());
        }
        catch (Exception ex)
        {
            LogFailure(ex, "WindowsShellIntegration.EnsureDesktopShortcut");
        }
    }

    private static string GetExecutablePath()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            executablePath = Application.ExecutablePath;
        }

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("无法获取当前程序可执行文件路径。");
        }

        return Path.GetFullPath(executablePath);
    }

    private static string GetDesktopShortcutPath()
    {
        var desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktopDirectory))
        {
            throw new InvalidOperationException("无法获取当前用户桌面路径。");
        }

        return Path.Combine(desktopDirectory, $"{AppConstants.ApplicationName}{ShortcutExtension}");
    }

    private static string QuoteExecutablePath(string executablePath)
    {
        return $"\"{executablePath}\"";
    }

    private static void CreateShortcut(string shortcutPath, string executablePath)
    {
        object? shell = null;
        object? shortcut = null;
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell")
                ?? throw new InvalidOperationException("当前系统不支持 WScript.Shell 快捷方式组件。");

            shell = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("无法创建 WScript.Shell 实例。");

            shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                binder: null,
                target: shell,
                args: new object[] { shortcutPath })
                ?? throw new InvalidOperationException("无法创建桌面快捷方式对象。");

            var shortcutType = shortcut.GetType();
            SetShortcutProperty(shortcutType, shortcut, "TargetPath", executablePath);
            SetShortcutProperty(shortcutType, shortcut, "WorkingDirectory", AppContext.BaseDirectory);
            SetShortcutProperty(shortcutType, shortcut, "Description", AppConstants.ApplicationName);
            if (File.Exists(AppAssets.IconPath))
            {
                SetShortcutProperty(shortcutType, shortcut, "IconLocation", AppAssets.IconPath);
            }

            shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, binder: null, target: shortcut, args: null);
        }
        finally
        {
            ReleaseComObject(shortcut);
            ReleaseComObject(shell);
        }
    }

    private static void SetShortcutProperty(Type shortcutType, object shortcut, string propertyName, object value)
    {
        shortcutType.InvokeMember(
            propertyName,
            BindingFlags.SetProperty,
            binder: null,
            target: shortcut,
            args: new[] { value });
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }

    private void LogFailure(Exception exception, string source, string? context = null)
    {
        try
        {
            _exceptionLogService.Write(exception, source, context);
        }
        catch
        {
            // 外壳集成失败不能反向影响启动或设置保存流程。
        }
    }
}
