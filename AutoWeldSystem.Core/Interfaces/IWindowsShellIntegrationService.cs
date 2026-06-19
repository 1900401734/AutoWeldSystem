using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// Windows 桌面外壳集成服务，负责开机自启和桌面快捷方式等系统级入口。
/// </summary>
public interface IWindowsShellIntegrationService
{
    /// <summary>
    /// 根据当前系统设置同步开机自启，并确保桌面快捷方式存在。
    /// </summary>
    void ApplyStartupIntegration();

    /// <summary>
    /// 根据指定设置同步开机自启，并确保桌面快捷方式存在。
    /// </summary>
    void ApplyStartupIntegration(AppSettings settings);

    /// <summary>
    /// 启用或关闭当前 Windows 用户登录后的开机自启。
    /// </summary>
    void SyncAutoStart(bool enabled);

    /// <summary>
    /// 确保当前 Windows 用户桌面存在程序快捷方式。
    /// </summary>
    void EnsureDesktopShortcut();
}
