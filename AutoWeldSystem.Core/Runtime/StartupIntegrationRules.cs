using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Runtime;

/// <summary>
/// 开机自启模式。
/// 独立成枚举，便于测试启动策略，不直接依赖 Windows 注册表或计划任务 API。
/// </summary>
public enum StartupIntegrationMode
{
    Disabled,
    CurrentUserRunKey,
    ElevatedScheduledTask
}

/// <summary>
/// 开机自启同步计划。
/// 服务层根据该计划决定创建计划任务、写 Run 注册表，或清理启动项。
/// </summary>
public sealed class StartupIntegrationPlan
{
    public StartupIntegrationPlan(
        StartupIntegrationMode mode,
        bool enableRunKey,
        bool enableElevatedTask)
    {
        Mode = mode;
        EnableRunKey = enableRunKey;
        EnableElevatedTask = enableElevatedTask;
    }

    /// <summary>
    /// 当前配置期望使用的启动模式。
    /// </summary>
    public StartupIntegrationMode Mode { get; }

    /// <summary>
    /// 是否应保留当前用户 Run 注册表启动项。
    /// </summary>
    public bool EnableRunKey { get; }

    /// <summary>
    /// 是否应保留最高权限计划任务启动项。
    /// </summary>
    public bool EnableElevatedTask { get; }
}

/// <summary>
/// 根据系统设置计算 Windows 开机自启策略。
/// </summary>
public static class StartupIntegrationRules
{
    public static StartupIntegrationPlan CreatePlan(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var enableAutoStart = settings.EnableAutoStart ?? true;
        if (!enableAutoStart)
        {
            return new StartupIntegrationPlan(
                StartupIntegrationMode.Disabled,
                enableRunKey: false,
                enableElevatedTask: false);
        }

        var enableElevatedAutoStart = settings.EnableElevatedAutoStart ?? true;
        if (enableElevatedAutoStart)
        {
            return new StartupIntegrationPlan(
                StartupIntegrationMode.ElevatedScheduledTask,
                enableRunKey: false,
                enableElevatedTask: true);
        }

        return new StartupIntegrationPlan(
            StartupIntegrationMode.CurrentUserRunKey,
            enableRunKey: true,
            enableElevatedTask: false);
    }
}
