namespace AutoWeldSystem.Core.DTOs;

/// <summary>
/// 上传状态页使用的程序同步摘要。
/// 只暴露列表显示需要的字段，避免界面直接依赖数据库实体细节。
/// </summary>
public sealed class ProgramSyncSummary
{
    public int Id { get; set; }

    public string ProgramName { get; set; } = string.Empty;

    public string ProductNum { get; set; } = string.Empty;

    public string ProgramId { get; set; } = string.Empty;

    public string SyncStatus { get; set; } = string.Empty;

    public string SyncAction { get; set; } = string.Empty;

    public string SyncMessage { get; set; } = string.Empty;

    public DateTime? LastSyncTime { get; set; }

    public DateTime UpdatedTime { get; set; }
}
