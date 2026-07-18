namespace AutoWeldSystem.Core.DTOs.Mes.Request;

/// <summary>
/// 程序管理页保存程序时传入的编辑内容。
/// 使用 DTO 隔离界面输入和数据库实体，避免 UI 直接改动持久化对象。
/// </summary>
public sealed class SaveProgramReq
{
    public int Id { get; set; }

    public string ProgramName { get; set; } = string.Empty;

    public string ProductNum { get; set; } = string.Empty;

    public string ProductModel { get; set; } = string.Empty;

    public string RecipeCode { get; set; } = string.Empty;

    /// <summary>
    /// 双工位设备工位 2 使用的本地 PLC 配方编号，不上传到 MES。
    /// </summary>
    public string? Station2RecipeCode { get; set; }

    public string ComponentCode { get; set; } = string.Empty;

    public int SequenceNumber { get; set; } = 1;

    public string ProgramType { get; set; } = "0";

    public string ProgramContentJson { get; set; } = string.Empty;

    public string ProgramFilePath { get; set; } = string.Empty;

    public string WeldJobName { get; set; } = string.Empty;

    public string RobotJobName { get; set; } = string.Empty;

    public decimal CycleTimeSeconds { get; set; }

    /// <summary>
    /// 用户填写的 MES 备注。为空时由服务按新增、修改或删除动作自动补齐。
    /// </summary>
    public string MesRemark { get; set; } = string.Empty;

    public string LocalRemark { get; set; } = string.Empty;
}
