using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Core.Interfaces.Log;

/// <summary>
/// 中心服务器交互日志服务。
/// 服务层负责写本地文件，界面层通过事件实时刷新，不让 UI 直接参与文件 IO。
/// </summary>
public interface ICenterInteractionLogService
{
    event EventHandler<CenterInteractionLogEntry>? LogWritten;

    void Write(CenterInteractionLogEntry entry);

    IReadOnlyList<CenterInteractionLogEntry> GetByDate(DateTime date, int take = 500);

    string GetLogDirectory();
}
