namespace AutoWeldSystem.Core.Runtime;

/// <summary>
/// Windows 开机自启同步结果。
/// 系统设置页可根据该结果提示现场人员是否已经启用最高权限启动。
/// </summary>
public sealed class StartupIntegrationResult
{
    private StartupIntegrationResult(
        bool success,
        bool usedElevatedTask,
        bool fallbackToRunKey,
        string message)
    {
        Success = success;
        UsedElevatedTask = usedElevatedTask;
        FallbackToRunKey = fallbackToRunKey;
        Message = message;
    }

    /// <summary>
    /// 是否按期望完成启动项同步。
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// 是否已经使用最高权限计划任务作为开机自启入口。
    /// </summary>
    public bool UsedElevatedTask { get; }

    /// <summary>
    /// 最高权限计划任务失败后，是否已经回退到普通 Run 注册表自启。
    /// </summary>
    public bool FallbackToRunKey { get; }

    /// <summary>
    /// 面向用户和日志的简短结果说明。
    /// </summary>
    public string Message { get; }

    public static StartupIntegrationResult Disabled()
        => new(true, false, false, "已关闭开机自启。");

    public static StartupIntegrationResult RunKeyEnabled()
        => new(true, false, false, "已启用普通开机自启。");

    public static StartupIntegrationResult ElevatedTaskEnabled()
        => new(true, true, false, "已启用最高权限开机自启。");

    public static StartupIntegrationResult Failed(string message)
        => new(false, false, false, message);

    public static StartupIntegrationResult RunKeyFallback(string message)
        => new(false, false, true, message);
}
