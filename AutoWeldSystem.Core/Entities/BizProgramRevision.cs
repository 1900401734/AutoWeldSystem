using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// 加工程序版本提交记录。
/// 每次本地保存程序时写入一条快照，形成类似 Git commit 的可追溯历史。
/// </summary>
[SugarTable("Biz_ProgramRevision", TableDescription = "加工程序版本记录表")]
public class BizProgramRevision
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(ColumnDescription = "本地程序ID")]
    public int ProgramLocalId { get; set; }

    [SugarColumn(Length = 32, ColumnDescription = "云端程序ID", IsNullable = true)]
    public string? ProgramId { get; set; }

    [SugarColumn(ColumnDescription = "版本号")]
    public int VersionNumber { get; set; }

    [SugarColumn(Length = 40, ColumnDescription = "提交ID")]
    public string CommitId { get; set; } = string.Empty;

    [SugarColumn(Length = 200, ColumnDescription = "提交说明", IsNullable = true)]
    public string? CommitMessage { get; set; }

    [SugarColumn(Length = 100, ColumnDescription = "程序名称")]
    public string ProgramName { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "产品工号")]
    public string ProductNum { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "配方编号", IsNullable = true)]
    public string? RecipeCode { get; set; }

    /// <summary>
    /// 保存本次版本对应的工位 2 配方编号。
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "工位2配方编号", IsNullable = true)]
    public string? Station2RecipeCode { get; set; }

    [SugarColumn(ColumnDataType = "text", ColumnDescription = "工艺参数JSON", IsNullable = true)]
    public string? ProgramContentJson { get; set; }

    [SugarColumn(Length = 500, ColumnDescription = "本地备注", IsNullable = true)]
    public string? LocalRemark { get; set; }

    [SugarColumn(ColumnDataType = "longtext", ColumnDescription = "执行文件Base64", IsNullable = true)]
    public string? ProgramFileBase64 { get; set; }

    [SugarColumn(Length = 30, ColumnDescription = "提交人编号")]
    public string UserNumber { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "提交人姓名")]
    public string UserName { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "提交时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;
}
