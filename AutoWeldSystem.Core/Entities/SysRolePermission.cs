using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace AutoWeldSystem.Core.Entities;

[SugarTable("Sys_RolePermission",TableDescription ="角色权限映射表")]
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
