namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 主界面开工/完工复合按钮状态决策。
/// UI 层只负责把这些布尔值映射到具体按钮和权限。
/// </summary>
public static class MonitorReportButtonRules
{
    /// <summary>
    /// 根据 MES 连接状态和任务状态决定在线上报与离线开工按钮的展示。
    /// </summary>
    /// <param name="isReadOnly">当前工位视图是否只读。</param>
    /// <param name="mesConnected">MES 是否已连接。</param>
    /// <param name="hasOnlineRunningTask">当前工位是否有在线未完工任务。</param>
    /// <param name="hasOfflineRunningTask">当前工位是否有离线未完工任务。</param>
    /// <returns>按钮状态决策。</returns>
    public static MonitorReportButtonDecision Decide(
        bool isReadOnly,
        bool mesConnected,
        bool hasOnlineRunningTask,
        bool hasOfflineRunningTask)
    {
        var canOperate = !isReadOnly;
        return new MonitorReportButtonDecision(
            ShowStartReportButton: !hasOnlineRunningTask,
            ShowFinishReportButton: hasOnlineRunningTask,
            OnlineReportEnabled: canOperate && mesConnected,
            LocalWorkOrderEnabled: canOperate && (!mesConnected || hasOfflineRunningTask));
    }
}

/// <summary>
/// 主界面开工/完工复合按钮状态。
/// </summary>
/// <param name="ShowStartReportButton">是否显示开工上报按钮。</param>
/// <param name="ShowFinishReportButton">是否显示完工上报按钮。</param>
/// <param name="OnlineReportEnabled">在线上报按钮是否可用。</param>
/// <param name="LocalWorkOrderEnabled">离线开工/本地完工按钮是否可用。</param>
public sealed record MonitorReportButtonDecision(
    bool ShowStartReportButton,
    bool ShowFinishReportButton,
    bool OnlineReportEnabled,
    bool LocalWorkOrderEnabled);
