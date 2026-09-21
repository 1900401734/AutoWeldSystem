using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces.UserManage;

public interface ISysUserService
{
    void InitDb();

    UserLoginResult Login(string userNumber, string password);

    SysUser? GetUserById(int id);

    IReadOnlyList<SysUser> GetAllUsers();

    SysUser SaveUser(SysUser user, string? plainPassword = null);

    bool DeleteUser(int id);

    bool AssignRole(int userId, int roleId);

    bool HasPermission(string permissionCode);

    IReadOnlyCollection<string> GetPermissions(SysUser? user);
}
