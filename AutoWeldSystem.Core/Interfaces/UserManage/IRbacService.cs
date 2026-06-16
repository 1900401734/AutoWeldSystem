using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces.UserManage;

public interface IRbacService
{
    void InitializeRbac();

    IReadOnlyList<SysRole> GetAllRoles(bool enabledOnly = false);

    SysRole? GetRoleById(int roleId);

    SysRole? GetRoleByCode(string roleCode);

    SysRole SaveRole(SysRole role, IEnumerable<int>? permissionIds = null);

    bool DeleteRole(int roleId);

    IReadOnlyList<SysPermission> GetAllPermissions(bool enabledOnly = false);

    IReadOnlyList<SysPermission> GetPermissionsByRole(int roleId);

    IReadOnlyCollection<string> GetPermissionCodesByRole(int roleId);

    IReadOnlyCollection<string> GetPermissionCodesByUser(int userId);

    IReadOnlyList<PermissionTreeNode> GetPermissionTree(int? roleId = null, bool enabledOnly = false);

    void SaveRolePermissions(int roleId, IEnumerable<int> permissionIds);
}
