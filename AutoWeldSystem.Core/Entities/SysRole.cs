using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

[SugarTable("Sys_Role", TableDescription = "角色表")]
public class SysRole
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "角色编码")]
    public string RoleCode { get; set; } = string.Empty;

    [SugarColumn(Length = 100, ColumnDescription = "角色名称")]
    public string RoleName { get; set; } = string.Empty;

    [SugarColumn(Length = 300, ColumnDescription = "描述", IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnDescription = "是否启用")]
    public bool Enabled { get; set; } = true;

    [SugarColumn(ColumnDescription = "内置角色")]
    public bool IsSystem { get; set; }

    [SugarColumn(ColumnDescription = "创建时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}
