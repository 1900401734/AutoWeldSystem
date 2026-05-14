using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace AutoWeldSystem.Core.Models;

[SugarTable("Sys_RolePermission")]
public class SysRolePermission
{
    [Key]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(ColumnDescription = "角色ID")]
    public int RoleId { get; set; }

    [SugarColumn(ColumnDescription = "权限ID")]
    public int PermissionId { get; set; }

    [SugarColumn(ColumnDescription = "创建时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;
}
