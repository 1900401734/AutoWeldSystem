using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 按工位保存和读取 MonitorView 的运行状态/异常提示。
/// UI 只负责展示，持久化细节集中在服务层，避免页面直接操作数据库。
/// </summary>
public interface IRuntimeTipStateService
{
    BizRuntimeTipState Get(int stationNo);

    void Save(BizRuntimeTipState state);
}
