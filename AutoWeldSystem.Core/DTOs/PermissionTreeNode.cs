using AutoWeldSystem.Core.Enums;

namespace AutoWeldSystem.Core.DTOs;

public class PermissionTreeNode
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public PermissionType Type { get; set; }

    public bool Checked { get; set; }

    public int Sort { get; set; }

    public List<PermissionTreeNode> Children { get; set; } = new();
}
