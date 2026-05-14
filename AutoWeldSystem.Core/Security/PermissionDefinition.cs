using AutoWeldSystem.Core.Enums;

namespace AutoWeldSystem.Core.Security;

public sealed record PermissionDefinition(
    string Code,
    string Name,
    PermissionType Type,
    string? ParentCode = null,
    int Sort = 0,
    string? Description = null);
