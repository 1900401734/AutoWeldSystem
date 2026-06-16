using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace AutoWeldSystem.Core.Entities;

[SugarTable("Sys_User", TableDescription = "用户表")]
public class SysUser
{
    [Key]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "工号")]
    public string UserNumber { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "用户名")]
    public string UserName { get; set; } = string.Empty;

    [SugarColumn(Length = 200, ColumnDescription = "密码哈希")]
    public string PasswordHash { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "角色ID")]
    public int RoleId { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "Legacy role code", IsNullable = true)]
    public string? Role { get; set; }

    [SugarColumn(ColumnDescription = "是否启用")]
    public bool Enabled { get; set; } = true;

    [SugarColumn(ColumnDescription = "创建时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDescription = "更新时间", IsNullable = true)]
    public DateTime? UpdatedTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDescription = "最后登录时间", IsNullable = true)]
    public DateTime? LastLoginTime { get; set; }

    [SugarColumn(IsIgnore = true)]
    public string RoleName { get; set; } = string.Empty;
}
