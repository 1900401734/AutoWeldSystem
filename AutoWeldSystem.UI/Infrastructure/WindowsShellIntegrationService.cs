using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Runtime;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// Windows 桌面外壳集成服务。
/// 负责同步开机自启、最高权限计划任务和桌面快捷方式。
/// </summary>
internal sealed class WindowsShellIntegrationService : IWindowsShellIntegrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ShortcutExtension = ".lnk";
    private const int ProcessTimeoutMilliseconds = 30000;

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
    /// 从当前配置读取开机自启开关，并同步 Windows 启动入口。
    /// </summary>
    public StartupIntegrationResult ApplyStartupIntegration()
    {
        try
        {
            return ApplyStartupIntegration(_settingsService.Get());
        }
        catch (Exception ex)
        {
            LogFailure(ex, "WindowsShellIntegration.ApplyStartupIntegration");
            return StartupIntegrationResult.Failed($"开机自启同步失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 同步开机自启并确保桌面快捷方式存在。
    /// 任何失败都返回结果并写异常日志，不阻断主程序启动。
    /// </summary>
    public StartupIntegrationResult ApplyStartupIntegration(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var result = ApplyStartupEntryPlan(settings);
        EnsureDesktopShortcut();
        return result;
    }

    /// <summary>
    /// 兼容旧调用：只同步普通 Run 注册表自启。
    /// </summary>
    public StartupIntegrationResult SyncAutoStart(bool enabled)
    {
        try
        {
            if (enabled)
            {
                EnableRunKeyAutoStart();
                return StartupIntegrationResult.RunKeyEnabled();
            }

            DisableRunKeyAutoStart();
            DeleteElevatedScheduledTask(ignoreFailure: true);
            return StartupIntegrationResult.Disabled();
        }
        catch (Exception ex)
        {
            LogFailure(ex, "WindowsShellIntegration.SyncAutoStart", $"Enabled={enabled}");
            return StartupIntegrationResult.Failed($"普通开机自启同步失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 当前用户桌面不存在快捷方式时自动创建，方便现场人员从桌面进入程序。
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

    private StartupIntegrationResult ApplyStartupEntryPlan(AppSettings settings)
    {
        var plan = StartupIntegrationRules.CreatePlan(settings);
        return plan.Mode switch
        {
            StartupIntegrationMode.Disabled => DisableAllStartupEntries(),
            StartupIntegrationMode.CurrentUserRunKey => EnableCurrentUserRunKeyStartup(),
            StartupIntegrationMode.ElevatedScheduledTask => EnableElevatedScheduledTaskStartup(),
            _ => StartupIntegrationResult.Failed("未知开机自启模式。")
        };
    }

    private StartupIntegrationResult DisableAllStartupEntries()
    {
        try
        {
            DisableRunKeyAutoStart();
            DeleteElevatedScheduledTask(ignoreFailure: true);
            return StartupIntegrationResult.Disabled();
        }
        catch (Exception ex)
        {
            LogFailure(ex, "WindowsShellIntegration.DisableAllStartupEntries");
            return StartupIntegrationResult.Failed($"关闭开机自启失败：{ex.Message}");
        }
    }

    private StartupIntegrationResult EnableCurrentUserRunKeyStartup()
    {
        try
        {
            DeleteElevatedScheduledTask(ignoreFailure: true);
            EnableRunKeyAutoStart();
            return StartupIntegrationResult.RunKeyEnabled();
        }
        catch (Exception ex)
        {
            LogFailure(ex, "WindowsShellIntegration.EnableCurrentUserRunKeyStartup");
            return StartupIntegrationResult.Failed($"普通开机自启配置失败：{ex.Message}");
        }
    }

    private StartupIntegrationResult EnableElevatedScheduledTaskStartup()
    {
        try
        {
            CreateOrUpdateElevatedScheduledTask();
            DisableRunKeyAutoStart();
            return StartupIntegrationResult.ElevatedTaskEnabled();
        }
        catch (Exception ex)
        {
            LogFailure(ex, "WindowsShellIntegration.EnableElevatedScheduledTaskStartup");
            return EnableRunKeyFallback(ex);
        }
    }

    private StartupIntegrationResult EnableRunKeyFallback(Exception elevatedException)
    {
        try
        {
            EnableRunKeyAutoStart();
            return StartupIntegrationResult.RunKeyFallback(
                $"最高权限开机自启配置失败，已回退为普通开机自启。原因：{elevatedException.Message}");
        }
        catch (Exception fallbackException)
        {
            LogFailure(fallbackException, "WindowsShellIntegration.EnableRunKeyFallback");
            return StartupIntegrationResult.Failed(
                $"最高权限开机自启配置失败，普通开机自启回退也失败。原因：{elevatedException.Message}；回退失败：{fallbackException.Message}");
        }
    }

    private static void EnableRunKeyAutoStart()
    {
        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, true)
            ?? throw new InvalidOperationException("无法打开当前用户开机启动注册表项。");

        runKey.SetValue(
            AppConstants.ApplicationName,
            QuoteExecutablePath(GetExecutablePath()),
            RegistryValueKind.String);
    }

    private static void DisableRunKeyAutoStart()
    {
        using var existingRunKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        existingRunKey?.DeleteValue(AppConstants.ApplicationName, false);
    }

    private static void CreateOrUpdateElevatedScheduledTask()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("最高权限开机自启仅支持 Windows。");
        }

        var script = BuildScheduledTaskScript(
            taskName: AppConstants.ApplicationName,
            executablePath: GetExecutablePath(),
            workingDirectory: AppContext.BaseDirectory,
            userId: GetCurrentWindowsUserName());
        var encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

        if (IsCurrentProcessElevated())
        {
            RunHiddenProcess(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encodedScript}");
            return;
        }

        RunElevatedPowerShell(encodedScript);
    }

    private static string BuildScheduledTaskScript(
        string taskName,
        string executablePath,
        string workingDirectory,
        string userId)
    {
        var escapedTaskName = EscapePowerShellSingleQuotedText(taskName);
        var escapedExecutablePath = EscapePowerShellSingleQuotedText(executablePath);
        var escapedWorkingDirectory = EscapePowerShellSingleQuotedText(workingDirectory);
        var escapedUserId = EscapePowerShellSingleQuotedText(userId);

        return $"""
            $ErrorActionPreference = 'Stop'
            $ProgressPreference = 'SilentlyContinue'
            $VerbosePreference = 'SilentlyContinue'
            $InformationPreference = 'SilentlyContinue'
            $action = New-ScheduledTaskAction -Execute '{escapedExecutablePath}' -WorkingDirectory '{escapedWorkingDirectory}'
            $trigger = New-ScheduledTaskTrigger -AtLogOn -User '{escapedUserId}'
            $principal = New-ScheduledTaskPrincipal -UserId '{escapedUserId}' -LogonType Interactive -RunLevel Highest
            $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -StartWhenAvailable -MultipleInstances IgnoreNew
            $task = New-ScheduledTask -Action $action -Trigger $trigger -Principal $principal -Settings $settings
            Register-ScheduledTask -TaskName '{escapedTaskName}' -InputObject $task -Force | Out-Null
            """;
    }

    private static void DeleteElevatedScheduledTask(bool ignoreFailure)
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            RunHiddenProcess("schtasks.exe", $"/Delete /TN \"{AppConstants.ApplicationName}\" /F");
        }
        catch when (ignoreFailure)
        {
            // 清理计划任务失败不应影响普通启动项或主程序启动。
        }
    }

    private static void RunElevatedPowerShell(string encodedScript)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encodedScript}",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            }) ?? throw new InvalidOperationException("无法启动管理员权限 PowerShell。");

            if (!process.WaitForExit(ProcessTimeoutMilliseconds))
            {
                TryKill(process);
                throw new TimeoutException("管理员权限 PowerShell 执行超时。");
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"管理员权限 PowerShell 执行失败，ExitCode={process.ExitCode}。");
            }
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            throw new InvalidOperationException("用户取消了管理员权限确认，最高权限开机自启未创建。", ex);
        }
    }

    private static void RunHiddenProcess(string fileName, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(ProcessTimeoutMilliseconds))
        {
            TryKill(process);
            throw new TimeoutException($"{fileName} 执行超时。");
        }

        if (process.ExitCode != 0)
        {
            var message = FirstNonEmpty(error, output, $"ExitCode={process.ExitCode}");
            throw new InvalidOperationException(message);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // 结束超时进程失败时继续抛出原始超时异常。
        }
    }

    private static bool IsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static string GetCurrentWindowsUserName()
    {
        var identityName = WindowsIdentity.GetCurrent()?.Name;
        if (!string.IsNullOrWhiteSpace(identityName))
        {
            return identityName;
        }

        return $"{Environment.UserDomainName}\\{Environment.UserName}";
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
        => $"\"{executablePath}\"";

    private static string EscapePowerShellSingleQuotedText(string value)
        => value.Replace("'", "''", StringComparison.Ordinal);

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

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
