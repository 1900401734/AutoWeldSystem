namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 主界面在线上报按钮状态决策。
/// UI 层只负责把这些布尔值映射到具体按钮和权限。
/// </summary>
public static class MonitorReportButtonRules
{
    /// <summary>
    /// 根据 MES 连接状态和任务状态决定在线上报与离线开工按钮的展示和动作。
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
            OnlineReportAction: hasOnlineRunningTask ? MonitorOnlineReportAction.Finish : MonitorOnlineReportAction.Start,
            ShowOnlineReportButton: canOperate,
            OnlineReportEnabled: canOperate && mesConnected,
            LocalWorkOrderEnabled: canOperate && (!mesConnected || hasOfflineRunningTask));
    }
}

/// <summary>
/// 主界面在线上报按钮当前动作。
/// </summary>
public enum MonitorOnlineReportAction
{
    /// <summary>
    /// 当前在线按钮执行开工上报。
    /// </summary>
    Start,

    /// <summary>
    /// 当前在线按钮执行完工上报。
    /// </summary>
    Finish
}

/// <summary>
/// 主界面在线上报按钮状态。
/// </summary>
/// <param name="OnlineReportAction">在线上报按钮当前执行的业务动作。</param>
/// <param name="ShowOnlineReportButton">是否显示在线上报按钮。</param>
/// <param name="OnlineReportEnabled">在线上报按钮是否可用。</param>
/// <param name="LocalWorkOrderEnabled">离线开工/本地完工按钮是否可用。</param>
public sealed record MonitorReportButtonDecision(
    MonitorOnlineReportAction OnlineReportAction,
    bool ShowOnlineReportButton,
    bool OnlineReportEnabled,
    bool LocalWorkOrderEnabled);
