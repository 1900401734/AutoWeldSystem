namespace AutoWeldSystem.Core.DTOs;

/// <summary>
/// 程序管理页保存程序时传入的编辑内容。
/// 使用 DTO 隔离界面输入和数据库实体，避免 UI 直接改动持久化对象。
/// </summary>
public sealed class ProgramSaveRequest
{
    public int Id { get; set; }

    public string ProgramName { get; set; } = string.Empty;

    public string ProductNum { get; set; } = string.Empty;

    public string ProductModel { get; set; } = string.Empty;

    public string RecipeCode { get; set; } = string.Empty;

    public string ComponentCode { get; set; } = string.Empty;

    public int SequenceNumber { get; set; } = 1;

    public string ProgramType { get; set; } = "0";

    public string ProgramContentJson { get; set; } = string.Empty;

    public string ProgramFilePath { get; set; } = string.Empty;

    public string WeldJobName { get; set; } = string.Empty;

    public string RobotJobName { get; set; } = string.Empty;

    public decimal CycleTimeSeconds { get; set; }

    public string Remark { get; set; } = string.Empty;

    public string LocalRemark { get; set; } = string.Empty;

    public string CommitMessage { get; set; } = string.Empty;
}
