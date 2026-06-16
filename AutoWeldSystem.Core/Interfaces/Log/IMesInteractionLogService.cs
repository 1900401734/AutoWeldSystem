using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Core.Interfaces.Log;

/// <summary>
/// MES 交互日志服务。
/// 服务层负责写本地文件，界面层通过事件实时刷新，不让 UI 直接参与文件 IO。
/// </summary>
public interface IMesInteractionLogService
{
    event EventHandler<MesInteractionLogEntry>? LogWritten;

    void Write(MesInteractionLogEntry entry);

    IReadOnlyList<MesInteractionLogEntry> GetByDate(DateTime date, int take = 500);

    string GetLogDirectory();
}
