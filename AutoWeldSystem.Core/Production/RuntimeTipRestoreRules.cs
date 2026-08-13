namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 运行提示恢复规则。
/// 运行提示描述的是某次开工过程中的进展，只有该任务仍在进行时才有参考价值。
/// </summary>
public static class RuntimeTipRestoreRules
{
    /// <summary>
    /// 判断是否应恢复上一次保存的运行提示。
    /// 未开工工位恢复旧提示会造成误导：例如上次退出时停留在“工单信息已获取”，
    /// 但重启后并未真正获取工单，现场会据此做出错误判断，因此未开工时一律用默认提示。
    /// </summary>
    /// <param name="hasUnfinishedTask">当前工位是否存在未完工任务。</param>
    /// <returns>true 表示可以恢复历史提示；false 表示应使用默认提示。</returns>
    public static bool ShouldRestoreRuntimeTip(bool hasUnfinishedTask)
    {
        return hasUnfinishedTask;
    }
}
