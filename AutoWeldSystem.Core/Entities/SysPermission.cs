using System.ComponentModel.DataAnnotations;
using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

[SugarTable("Sys_Permission",tableDescription:"权限表")]
public class SysPermission
{
    [Key]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, ColumnDescription = "权限编码")]
    public string Code { get; set; } = string.Empty;

    [SugarColumn(Length = 100, ColumnDescription = "权限名称")]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 20, ColumnDescription = "权限类型")]
    public string Type { get; set; } = string.Empty;

    [SugarColumn(Length = 100, ColumnDescription = "父级权限编码", IsNullable = true)]
    public string? ParentCode { get; set; }

    [SugarColumn(ColumnDescription = "排序")]
    public int Sort { get; set; }

    [SugarColumn(Length = 300, ColumnDescription = "描述", IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnDescription = "是否启用")]
    public bool Enabled { get; set; } = true;

    [SugarColumn(ColumnDescription = "创建时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}
