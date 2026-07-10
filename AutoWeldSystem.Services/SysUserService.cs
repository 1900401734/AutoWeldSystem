using System.Security.Cryptography;
using System.Text;
using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces.UserManage;
using AutoWeldSystem.Core.Security;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.Services;

public class SysUserService : ISysUserService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IRbacService _rbacService;

    public SysUserService(SqlSugarDbContext dbContext, IRbacService rbacService)
    {
        _dbContext = dbContext;
        _rbacService = rbacService;
    }

    public void InitDb()
    {
        _dbContext.InitDatabase();
        InitializeRbacPermissions();

        var hasAnyUser = _dbContext.Db.Queryable<SysUser>().Any();
        if (!hasAnyUser)
        {
            var adminRole = _rbacService.GetRoleByCode(AppConstants.Roles.Admin)
                ?? throw new InvalidOperationException("Admin role is missing.");
            var operatorRole = _rbacService.GetRoleByCode(AppConstants.Roles.Operator)
                ?? throw new InvalidOperationException("Operator role is missing.");
            var readonlyRole = _rbacService.GetRoleByCode(AppConstants.Roles.Readonly)
                ?? throw new InvalidOperationException("Readonly role is missing.");

            var seedUsers = new[]
            {
                new SysUser
                {
                    UserNumber = "admin",
                    UserName = "Administrator",
                    RoleId = adminRole.Id,
                    Role = adminRole.RoleCode,
                    PasswordHash = Hash(AppConstants.Defaults.InitialPassword)
                },
                new SysUser
                {
                    UserNumber = "operator",
                    UserName = "Operator",
                    RoleId = operatorRole.Id,
                    Role = operatorRole.RoleCode,
                    PasswordHash = Hash(AppConstants.Defaults.InitialPassword)
                },
                new SysUser
                {
                    UserNumber = "readonly",
                    UserName = "Readonly",
                    RoleId = readonlyRole.Id,
                    Role = readonlyRole.RoleCode,
                    PasswordHash = Hash(AppConstants.Defaults.InitialPassword)
                }
            };

            _dbContext.Db.Insertable(seedUsers).ExecuteCommand();
        }

        EnsureDeveloperUser();
    }

    /// <summary>
    /// 初始化权限目录，并兼容旧版本中管理员会被自动补齐全部权限的行为。
    /// 初始化前保存管理员的真实授权，初始化后恢复该授权，避免覆盖管理员手工取消的页面或按钮权限。
    /// </summary>
    private void InitializeRbacPermissions()
    {
        var stateTabCatalogWasMissing = !_rbacService.GetAllPermissions().Any(permission =>
            PermissionCodes.Tabs.State.All.Contains(permission.Code, StringComparer.OrdinalIgnoreCase));
        var rolesBeforeInitialization = _rbacService.GetAllRoles()
            .ToDictionary(role => role.RoleCode, StringComparer.OrdinalIgnoreCase);
        var adminPermissionsBeforeInitialization = CaptureRolePermissionCodes(
            rolesBeforeInitialization,
            AppConstants.Roles.Admin);

        _rbacService.InitializeRbac();

        RestoreConfigurableAdminPermissions(
            rolesBeforeInitialization,
            adminPermissionsBeforeInitialization,
            stateTabCatalogWasMissing);
        ApplyStateTabUpgradeDefaults(stateTabCatalogWasMissing, rolesBeforeInitialization);
    }

    /// <summary>
    /// 读取指定内置角色在初始化前已经保存的权限编码。
    /// 返回 null 表示这是首次安装，角色尚不存在。
    /// </summary>
    private IReadOnlyCollection<string>? CaptureRolePermissionCodes(
        IReadOnlyDictionary<string, SysRole> roles,
        string roleCode)
    {
        return roles.TryGetValue(roleCode, out var role)
            ? _rbacService.GetPermissionCodesByRole(role.Id)
            : null;
    }

    /// <summary>
    /// 管理员改为严格按角色授权：旧库恢复原授权，新安装使用“全部页面和按钮 + 三个客户页签”。
    /// </summary>
    private void RestoreConfigurableAdminPermissions(
        IReadOnlyDictionary<string, SysRole> rolesBeforeInitialization,
        IReadOnlyCollection<string>? permissionsBeforeInitialization,
        bool stateTabCatalogWasMissing)
    {
        var adminRole = _rbacService.GetRoleByCode(AppConstants.Roles.Admin);
        if (adminRole is null)
        {
            return;
        }

        IReadOnlyCollection<string> targetCodes;
        if (!rolesBeforeInitialization.ContainsKey(AppConstants.Roles.Admin))
        {
            targetCodes = RolePermissionInitializationRules.ResolveElevatedRoleDefaults(
                AppConstants.Roles.Admin,
                _rbacService.GetAllPermissions().Select(permission => permission.Code));
        }
        else
        {
            var originalCodes = permissionsBeforeInitialization ?? Array.Empty<string>();
            var upgradeDefaults = RolePermissionInitializationRules.ResolveStateTabUpgradeDefaults(
                AppConstants.Roles.Admin,
                stateTabCatalogWasMissing,
                originalCodes.Contains(PermissionCodes.Pages.StateManage, StringComparer.OrdinalIgnoreCase));
            targetCodes = originalCodes
                .Concat(upgradeDefaults)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        SaveRolePermissionCodes(adminRole.Id, targetCodes);
    }

    /// <summary>
    /// 首次增加页签权限时，为已有待上传数据页面权限的非开发、非管理员角色补充客户默认页签。
    /// 管理员已在 RestoreConfigurableAdminPermissions 中按快照单独处理。
    /// </summary>
    private void ApplyStateTabUpgradeDefaults(
        bool stateTabCatalogWasMissing,
        IReadOnlyDictionary<string, SysRole> rolesBeforeInitialization)
    {
        if (!stateTabCatalogWasMissing)
        {
            return;
        }

        foreach (var role in rolesBeforeInitialization.Values)
        {
            if (string.Equals(role.RoleCode, AppConstants.Roles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var originalCodes = _rbacService.GetPermissionCodesByRole(role.Id);
            var upgradeDefaults = RolePermissionInitializationRules.ResolveStateTabUpgradeDefaults(
                role.RoleCode,
                stateTabCatalogWasMissing,
                originalCodes.Contains(PermissionCodes.Pages.StateManage, StringComparer.OrdinalIgnoreCase));
            if (upgradeDefaults.Count == 0)
            {
                continue;
            }

            SaveRolePermissionCodes(
                role.Id,
                originalCodes.Concat(upgradeDefaults).Distinct(StringComparer.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// 将权限编码转换为当前数据库权限 Id 后保存，忽略已从目录移除的历史编码。
    /// </summary>
    private void SaveRolePermissionCodes(int roleId, IEnumerable<string> permissionCodes)
    {
        var requestedCodes = permissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var permissionIds = _rbacService.GetAllPermissions()
            .Where(permission => requestedCodes.Contains(permission.Code))
            .Select(permission => permission.Id)
            .ToArray();
        _rbacService.SaveRolePermissions(roleId, permissionIds);
    }

    public UserLoginResult Login(string userNumber, string password)
    {
        if (string.IsNullOrWhiteSpace(userNumber) || string.IsNullOrWhiteSpace(password))
        {
            return UserLoginResult.Fail(UserLoginFailureReason.InvalidCredentials);
        }

        var user = _dbContext.Db.Queryable<SysUser>()
            .First(it => it.UserNumber == userNumber.Trim());
        if (user is null)
        {
            return UserLoginResult.Fail(UserLoginFailureReason.InvalidCredentials);
        }

        if (!string.Equals(user.PasswordHash, Hash(password), StringComparison.Ordinal))
        {
            return UserLoginResult.Fail(UserLoginFailureReason.InvalidCredentials);
        }

        // Separate failure reasons let the UI show a precise localized message.
        if (!user.Enabled)
        {
            return UserLoginResult.Fail(UserLoginFailureReason.UserDisabled);
        }

        var role = _rbacService.GetRoleById(user.RoleId);
        if (role is null || !role.Enabled)
        {
            return UserLoginResult.Fail(UserLoginFailureReason.RoleDisabled);
        }

        user.Role = role.RoleCode;
        user.RoleName = role.RoleName;
        user.LastLoginTime = DateTime.Now;
        _dbContext.Db.Updateable(user).UpdateColumns(it => new { it.LastLoginTime }).ExecuteCommand();
        return UserLoginResult.Success(user);
    }

    public SysUser? GetUserById(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        var user = _dbContext.Db.Queryable<SysUser>().First(it => it.Id == id);
        if (user is null)
        {
            return null;
        }

        PopulateRole(user);
        if (IsDeveloperUser(user) && !IsCurrentDeveloper())
        {
            return null;
        }

        return user;
    }

    public IReadOnlyList<SysUser> GetAllUsers()
    {
        var users = _dbContext.Db.Queryable<SysUser>().OrderBy(it => it.Id).ToList();
        foreach (var user in users)
        {
            PopulateRole(user);
        }

        if (!IsCurrentDeveloper())
        {
            users = users.Where(user => !IsDeveloperUser(user)).ToList();
        }

        return users;
    }

    public SysUser SaveUser(SysUser user, string? plainPassword = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        user.UserNumber = user.UserNumber?.Trim() ?? string.Empty;
        user.UserName = user.UserName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(user.UserNumber))
        {
            throw new UserFriendlyException(TextKeys.User.NumberRequired);
        }

        var role = ResolveRole(user.RoleId, user.Role);
        if (role is null || !role.Enabled)
        {
            throw new UserFriendlyException(TextKeys.User.InvalidRole);
        }

        var existingUser = user.Id > 0 ? _dbContext.Db.Queryable<SysUser>().InSingle(user.Id) : null;
        if (((existingUser is not null && IsDeveloperUser(existingUser)) || IsDeveloperRole(role)) && !IsCurrentDeveloper())
        {
            throw new UserFriendlyException(TextKeys.User.InvalidRole);
        }

        var duplicateUser = _dbContext.Db.Queryable<SysUser>()
            .First(it => it.UserNumber == user.UserNumber && it.Id != user.Id);
        if (duplicateUser is not null)
        {
            throw new UserFriendlyException(TextKeys.User.NumberExists, user.UserNumber);
        }

        user.RoleId = role.Id;
        user.Role = role.RoleCode;
        user.RoleName = role.RoleName;

        if (!string.IsNullOrWhiteSpace(plainPassword))
        {
            // Hash only when the caller explicitly wants to replace the password.
            user.PasswordHash = Hash(plainPassword);
        }

        var now = DateTime.Now;
        if (user.Id <= 0)
        {
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                // New users always get a valid password so they can log in immediately.
                user.PasswordHash = Hash(AppConstants.Defaults.InitialPassword);
            }

            user.CreatedTime = now;
            user.UpdatedTime = now;
            user = _dbContext.Db.Insertable(user).ExecuteReturnEntity();
        }
        else
        {
            var existing = GetUserById(user.Id)
                ?? throw new UserFriendlyException(TextKeys.User.NotExists);

            user.CreatedTime = existing.CreatedTime;
            user.LastLoginTime = existing.LastLoginTime;
            user.PasswordHash = string.IsNullOrWhiteSpace(user.PasswordHash)
                ? existing.PasswordHash
                : user.PasswordHash;
            user.UpdatedTime = now;

            _dbContext.Db.Updateable(user).ExecuteCommand();
        }

        var saved = GetUserById(user.Id) ?? user;
        RefreshCurrentUserContext(saved);
        return saved;
    }

    public bool DeleteUser(int id)
    {
        if (id <= 0 || GlobalContext.CurrentUser?.Id == id)
        {
            return false;
        }

        var user = _dbContext.Db.Queryable<SysUser>().InSingle(id);
        if (user is not null && IsDeveloperUser(user) && !IsCurrentDeveloper())
        {
            return false;
        }

        return _dbContext.Db.Deleteable<SysUser>(id).ExecuteCommand() > 0;
    }

    public bool AssignRole(int userId, int roleId)
    {
        var user = GetUserById(userId);
        var role = _rbacService.GetRoleById(roleId);
        if (user is null || role is null || !role.Enabled)
        {
            return false;
        }

        if ((IsDeveloperUser(user) || IsDeveloperRole(role)) && !IsCurrentDeveloper())
        {
            return false;
        }

        user.RoleId = role.Id;
        user.Role = role.RoleCode;
        SaveUser(user);
        return true;
    }

    public bool HasPermission(string permissionCode)
    {
        return GlobalContext.HasPermission(permissionCode);
    }

    public IReadOnlyCollection<string> GetPermissions(SysUser? user)
    {
        if (user is null)
        {
            return Array.Empty<string>();
        }

        if (GlobalContext.CurrentUser?.Id == user.Id)
        {
            return GlobalContext.CurrentPermissions;
        }

        return _rbacService.GetPermissionCodesByUser(user.Id);
    }

    private void EnsureDeveloperUser()
    {
        var developerRole = _rbacService.GetRoleByCode(AppConstants.Roles.Developer)
            ?? throw new InvalidOperationException("Developer role is missing.");
        var developer = _dbContext.Db.Queryable<SysUser>()
            .First(user => user.UserNumber == "dev");

        if (developer is null)
        {
            _dbContext.Db.Insertable(new SysUser
            {
                UserNumber = "dev",
                UserName = "Developer",
                RoleId = developerRole.Id,
                Role = developerRole.RoleCode,
                PasswordHash = Hash("dev"),
                Enabled = true,
                CreatedTime = DateTime.Now,
                UpdatedTime = DateTime.Now
            }).ExecuteCommand();
            return;
        }

        developer.UserName = string.IsNullOrWhiteSpace(developer.UserName) ? "Developer" : developer.UserName.Trim();
        developer.RoleId = developerRole.Id;
        developer.Role = developerRole.RoleCode;
        developer.PasswordHash = Hash("dev");
        developer.Enabled = true;
        developer.UpdatedTime = DateTime.Now;
        _dbContext.Db.Updateable(developer)
            .UpdateColumns(user => new { user.UserName, user.RoleId, user.Role, user.PasswordHash, user.Enabled, user.UpdatedTime })
            .ExecuteCommand();
    }

    private SysRole? ResolveRole(int roleId, string? roleCode)
    {
        if (roleId > 0)
        {
            var roleById = _rbacService.GetRoleById(roleId);
            if (roleById is not null)
            {
                return roleById;
            }
        }

        if (!string.IsNullOrWhiteSpace(roleCode))
        {
            var roleByCode = _rbacService.GetRoleByCode(roleCode);
            if (roleByCode is not null)
            {
                return roleByCode;
            }
        }

        return _rbacService.GetRoleByCode(AppConstants.Roles.Operator);
    }

    private void PopulateRole(SysUser user)
    {
        var role = ResolveRole(user.RoleId, user.Role);
        if (role is null)
        {
            return;
        }

        user.RoleId = role.Id;
        user.Role = role.RoleCode;
        user.RoleName = role.RoleName;
    }

    private static bool IsDeveloperRole(SysRole role)
    {
        return string.Equals(role.RoleCode, AppConstants.Roles.Developer, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDeveloperUser(SysUser user)
    {
        return string.Equals(user.UserNumber, "dev", StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.Role, AppConstants.Roles.Developer, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCurrentDeveloper()
    {
        var currentUser = GlobalContext.CurrentUser;
        return currentUser is not null
            && (string.Equals(currentUser.UserNumber, "dev", StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentUser.Role, AppConstants.Roles.Developer, StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshCurrentUserContext(SysUser user)
    {
        if (GlobalContext.CurrentUser?.Id != user.Id)
        {
            return;
        }

        GlobalContext.SetCurrentUser(user, _rbacService.GetPermissionCodesByUser(user.Id));
    }

    private static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
