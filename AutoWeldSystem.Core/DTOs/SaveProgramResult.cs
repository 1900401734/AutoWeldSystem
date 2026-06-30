using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.DTOs;

/// <summary>
/// 程序保存后的本地实体和“本次保存”同步决策。
/// </summary>
public sealed class SaveProgramResult
{
    /// <summary>
    /// 已保存到本地数据库的程序实体。
    /// </summary>
    public BizProgram Program { get; init; } = new();

    /// <summary>
    /// 本次保存是否产生了需要立即同步到 MES 的动作。
    /// </summary>
    public bool ShouldSyncNow => !string.IsNullOrWhiteSpace(CurrentSaveSyncAction);

    /// <summary>
    /// 本次保存产生的 MES 动作；历史遗留待同步动作不会写入这里。
    /// </summary>
    public string? CurrentSaveSyncAction { get; init; }
}
