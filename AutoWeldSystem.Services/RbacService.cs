using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces.UserManage;
using AutoWeldSystem.Core.Security;
using AutoWeldSystem.Data;
using SqlSugar;

namespace AutoWeldSystem.Services;

public class RbacService : IRbacService
{
    private readonly SqlSugarDbContext _dbContext;

    public RbacService(SqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void InitializeRbac()
    {
        EnsureDefaultRoles();
        var createdPermissionCodes = EnsureDefaultPermissions();
        CleanupRetiredPermissions();
        EnsureDefaultRolePermissions();
        UpgradeDataDeletePermission(createdPermissionCodes);
    }

    public IReadOnlyList<SysRole> GetAllRoles(bool enabledOnly = false)
    {
        var query = _dbContext.Db.Queryable<SysRole>();
        if (enabledOnly)
        {
            query = query.Where(it => it.Enabled);
        }

        return query.OrderBy(it => it.IsSystem, OrderByType.Desc).OrderBy(it => it.Id).ToList();
    }

    public SysRole? GetRoleById(int roleId)
    {
        if (roleId <= 0)
        {
            return null;
        }

        return _dbContext.Db.Queryable<SysRole>().First(it => it.Id == roleId);
    }

    public SysRole? GetRoleByCode(string roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return null;
        }

        return _dbContext.Db.Queryable<SysRole>().First(it => it.RoleCode == roleCode);
    }

    public SysRole SaveRole(SysRole role, IEnumerable<int>? permissionIds = null)
    {
        ArgumentNullException.ThrowIfNull(role);

        role.RoleCode = NormalizeRoleCode(role.RoleCode, role.RoleName);
        role.RoleName = role.RoleName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(role.RoleName))
        {
            throw new UserFriendlyException(TextKeys.Role.NameRequired);
        }

        var existingByCode = _dbContext.Db.Queryable<SysRole>()
            .First(it => it.RoleCode == role.RoleCode && it.Id != role.Id);
        if (existingByCode is not null)
        {
            throw new UserFriendlyException(TextKeys.Role.CodeExists, role.RoleCode);
        }

        var now = DateTime.Now;
        if (role.Id <= 0)
        {
            role.CreatedTime = now;
            role.UpdatedTime = now;
            role = _dbContext.Db.Insertable(role).ExecuteReturnEntity();
        }
        else
        {
            var existing = GetRoleById(role.Id)
                ?? throw new UserFriendlyException(TextKeys.Role.NotExists);

            role.CreatedTime = existing.CreatedTime;
            role.IsSystem = existing.IsSystem;
            role.UpdatedTime = now;
            _dbContext.Db.Updateable(role).ExecuteCommand();
        }

        if (permissionIds is not null)
        {
            SaveRolePermissions(role.Id, permissionIds);
        }

        return GetRoleById(role.Id) ?? role;
    }

    public bool DeleteRole(int roleId)
    {
        var role = GetRoleById(roleId);
        if (role is null || role.IsSystem)
        {
            return false;
        }

        if (_dbContext.Db.Queryable<SysUser>().Any(it => it.RoleId == roleId))
        {
            return false;
        }

        var tran = _dbContext.Db.Ado.UseTran(() =>
        {
            _dbContext.Db.Deleteable<SysRolePermission>().Where(it => it.RoleId == roleId).ExecuteCommand();
            _dbContext.Db.Deleteable<SysRole>(roleId).ExecuteCommand();
        });

        if (!tran.IsSuccess)
        {
            throw tran.ErrorException ?? new InvalidOperationException("DeleteRole failed.");
        }

        return true;
    }

    public IReadOnlyList<SysPermission> GetAllPermissions(bool enabledOnly = false)
    {
        var query = _dbContext.Db.Queryable<SysPermission>();
        if (enabledOnly)
        {
            query = query.Where(it => it.Enabled);
        }

        return query
            .OrderBy(it => it.Sort)
            .OrderBy(it => it.Id)
            .ToList();
    }

    public IReadOnlyList<SysPermission> GetPermissionsByRole(int roleId)
    {
        if (roleId <= 0)
        {
            return Array.Empty<SysPermission>();
        }

        var permissionIds = _dbContext.Db.Queryable<SysRolePermission>()
            .Where(it => it.RoleId == roleId)
            .Select(it => it.PermissionId)
            .ToList();

        if (permissionIds.Count == 0)
        {
            return Array.Empty<SysPermission>();
        }

        return _dbContext.Db.Queryable<SysPermission>()
            .Where(it => permissionIds.Contains(it.Id))
            .OrderBy(it => it.Sort)
            .OrderBy(it => it.Id)
            .ToList();
    }

    public IReadOnlyCollection<string> GetPermissionCodesByRole(int roleId)
    {
        return GetPermissionsByRole(roleId)
            .Where(static item => item.Enabled)
            .Select(static item => item.Code)
            .ToArray();
    }

    public IReadOnlyCollection<string> GetPermissionCodesByUser(int userId)
    {
        if (userId <= 0)
        {
            return Array.Empty<string>();
        }

        var user = _dbContext.Db.Queryable<SysUser>().First(it => it.Id == userId && it.Enabled);
        if (user is null || user.RoleId <= 0)
        {
            return Array.Empty<string>();
        }

        var role = GetRoleById(user.RoleId);
        if (role is null || !role.Enabled)
        {
            return Array.Empty<string>();
        }

        return GetPermissionCodesByRole(user.RoleId);
    }

    public IReadOnlyList<PermissionTreeNode> GetPermissionTree(int? roleId = null, bool enabledOnly = false)
    {
        var permissions = GetAllPermissions(enabledOnly);
        var selectedCodes = roleId.HasValue && roleId.Value > 0
            ? new HashSet<string>(GetPermissionCodesByRole(roleId.Value), StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var nodeLookup = permissions.ToDictionary(
            item => item.Code,
            item => new PermissionTreeNode
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                Type = ParsePermissionType(item.Type),
                Checked = selectedCodes.Contains(item.Code),
                Sort = item.Sort
            },
            StringComparer.OrdinalIgnoreCase);

        var roots = new List<PermissionTreeNode>();
        foreach (var permission in permissions)
        {
            var node = nodeLookup[permission.Code];
            if (string.IsNullOrWhiteSpace(permission.ParentCode)
                || !nodeLookup.TryGetValue(permission.ParentCode, out var parent))
            {
                roots.Add(node);
                continue;
            }

            parent.Children.Add(node);
        }

        SortTree(roots);
        return roots;
    }

    public void SaveRolePermissions(int roleId, IEnumerable<int> permissionIds)
    {
        var role = GetRoleById(roleId)
            ?? throw new UserFriendlyException(TextKeys.Role.NotExists);

        var validPermissionIds = GetAllPermissions()
            .Select(static item => item.Id)
            .Intersect(permissionIds ?? Enumerable.Empty<int>())
            .Distinct()
            .ToList();

        var now = DateTime.Now;
        var tran = _dbContext.Db.Ado.UseTran(() =>
        {
            _dbContext.Db.Deleteable<SysRolePermission>().Where(it => it.RoleId == roleId).ExecuteCommand();

            if (validPermissionIds.Count > 0)
            {
                var mappings = validPermissionIds.Select(permissionId => new SysRolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    CreatedTime = now
                }).ToList();

                _dbContext.Db.Insertable(mappings).ExecuteCommand();
            }

            role.UpdatedTime = now;
            _dbContext.Db.Updateable(role).UpdateColumns(it => new { it.UpdatedTime }).ExecuteCommand();
        });

        if (!tran.IsSuccess)
        {
            throw tran.ErrorException ?? new InvalidOperationException("SaveRolePermissions failed.");
        }

        RefreshCurrentSessionIfAffected(roleId);
    }

    private void EnsureDefaultRoles()
    {
        var defaults = new[]
        {
            new SysRole
            {
                RoleCode = AppConstants.Roles.Developer,
                RoleName = "������",
                Description = "����ϵͳ�����߽�ɫ",
                Enabled = true,
                IsSystem = true
            },
            new SysRole
            {
                RoleCode = AppConstants.Roles.Admin,
                RoleName = "����Ա",
                Description = "����ϵͳ����Ա��ɫ",
                Enabled = true,
                IsSystem = true
            },
            new SysRole
            {
                RoleCode = AppConstants.Roles.Operator,
                RoleName = "����Ա",
                Description = "����ϵͳ����Ա��ɫ",
                Enabled = true,
                IsSystem = true
            },
            new SysRole
            {
                RoleCode = AppConstants.Roles.Readonly,
                RoleName = "ֻ���û�",
                Description = "����ϵͳֻ����ɫ",
                Enabled = true,
                IsSystem = true
            }
        };

        foreach (var item in defaults)
        {
            if (GetRoleByCode(item.RoleCode) is not null)
            {
                continue;
            }

            item.CreatedTime = DateTime.Now;
            item.UpdatedTime = item.CreatedTime;
            _dbContext.Db.Insertable(item).ExecuteCommand();
        }
    }

    /// <summary>
    /// 补齐权限目录，返回本次新建的权限码，供旧数据库的一次性补权使用。
    /// </summary>
    private HashSet<string> EnsureDefaultPermissions()
    {
        var existing = GetAllPermissions().ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);
        var createdCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in PermissionCatalog.All)
        {
            if (!existing.TryGetValue(definition.Code, out var permission))
            {
                permission = new SysPermission
                {
                    Code = definition.Code,
                    CreatedTime = DateTime.Now
                };
            }

            permission.Name = definition.Name;
            permission.Type = definition.Type.ToString();
            permission.ParentCode = definition.ParentCode;
            permission.Sort = definition.Sort;
            permission.Description = definition.Description;
            permission.Enabled = true;
            permission.UpdatedTime = DateTime.Now;

            if (permission.Id <= 0)
            {
                _dbContext.Db.Insertable(permission).ExecuteCommand();
                createdCodes.Add(definition.Code);
            }
            else
            {
                _dbContext.Db.Updateable(permission).ExecuteCommand();
            }
        }

        return createdCodes;
    }

    /// <summary>
    /// 旧数据库首次引入历史数据删除权限时，为管理员补权，避免升级后按钮一直置灰。
    /// </summary>
    private void UpgradeDataDeletePermission(HashSet<string> createdPermissionCodes)
    {
        if (!createdPermissionCodes.Contains(PermissionCodes.Buttons.Data.Delete))
        {
            return;
        }

        var permissions = GetAllPermissions().ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);
        if (!permissions.TryGetValue(PermissionCodes.Buttons.Data.Delete, out var deletePermission)
            || !permissions.TryGetValue(PermissionCodes.Pages.DataManage, out var dataManagePermission))
        {
            return;
        }

        foreach (var role in GetAllRoles())
        {
            var hasDataManagePage = _dbContext.Db.Queryable<SysRolePermission>()
                .Any(item => item.RoleId == role.Id && item.PermissionId == dataManagePermission.Id);
            var upgradeCodes = RolePermissionInitializationRules.ResolveDataDeleteUpgradeDefaults(
                role.RoleCode,
                dataDeleteCatalogWasMissing: true,
                hasDataManagePagePermission: hasDataManagePage);
            if (upgradeCodes.Count == 0)
            {
                continue;
            }

            AppendMissingRolePermissions(role.Id, [deletePermission.Id]);
        }
    }

    private void CleanupRetiredPermissions()
    {
        var retiredCodes = new[]
        {
            "button.data.export",
            "button.monitor.start-report",
            "button.monitor.finish-report",
            "button.monitor.edit-work-order",
            "button.monitor.change-work-order",
            "button.monitor.get-work-order"
        };

        var permissions = _dbContext.Db.Queryable<SysPermission>()
            .Where(item => retiredCodes.Contains(item.Code))
            .ToList();

        if (permissions.Count == 0)
        {
            return;
        }

        var permissionIds = permissions.Select(item => item.Id).ToList();
        var tran = _dbContext.Db.Ado.UseTran(() =>
        {
            _dbContext.Db.Deleteable<SysRolePermission>()
                .Where(item => permissionIds.Contains(item.PermissionId))
                .ExecuteCommand();
            _dbContext.Db.Deleteable<SysPermission>()
                .Where(item => permissionIds.Contains(item.Id))
                .ExecuteCommand();
        });

        if (!tran.IsSuccess)
        {
            throw tran.ErrorException ?? new InvalidOperationException("CleanupRetiredPermissions failed.");
        }
    }

    private void EnsureDefaultRolePermissions()
    {
        var roles = GetAllRoles().ToDictionary(item => item.RoleCode, StringComparer.OrdinalIgnoreCase);
        var permissions = GetAllPermissions().ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var pair in BuildDefaultRolePermissionMap())
        {
            if (!roles.TryGetValue(pair.Key, out var role))
            {
                continue;
            }

            var existingCount = _dbContext.Db.Queryable<SysRolePermission>().Count(it => it.RoleId == role.Id);
            var permissionIds = pair.Value
                .Where(permissions.ContainsKey)
                .Select(code => permissions[code].Id)
                .ToArray();

            if (existingCount > 0
                && RolePermissionInitializationRules.ShouldAppendMissingDefaults(role.RoleCode))
            {
                AppendMissingRolePermissions(role.Id, permissionIds);
                continue;
            }

            if (existingCount > 0)
            {
                continue;
            }

            SaveRolePermissions(role.Id, permissionIds);
        }
    }

    private void AppendMissingRolePermissions(int roleId, IReadOnlyCollection<int> desiredPermissionIds)
    {
        var existingIds = _dbContext.Db.Queryable<SysRolePermission>()
            .Where(item => item.RoleId == roleId)
            .Select(item => item.PermissionId)
            .ToList()
            .ToHashSet();
        var missing = desiredPermissionIds
            .Where(permissionId => !existingIds.Contains(permissionId))
            .Select(permissionId => new SysRolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                CreatedTime = DateTime.Now
            })
            .ToList();

        if (missing.Count > 0)
        {
            _dbContext.Db.Insertable(missing).ExecuteCommand();
        }
    }

    private static Dictionary<string, IReadOnlyCollection<string>> BuildDefaultRolePermissionMap()
    {
        var allCodes = PermissionCatalog.All.Select(static item => item.Code).ToArray();

        return new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [AppConstants.Roles.Developer] = allCodes,
            [AppConstants.Roles.Admin] = allCodes,
            [AppConstants.Roles.Operator] = new[]
            {
                PermissionCodes.Pages.Monitor,
                PermissionCodes.Pages.DataManage,
                PermissionCodes.Pages.ProgramManage,
                PermissionCodes.Buttons.Monitor.OnlineReport,
                PermissionCodes.Buttons.Monitor.LocalWorkOrder,
                PermissionCodes.Buttons.Auth.SwitchUser,
                PermissionCodes.Buttons.Auth.Logout,
                PermissionCodes.Buttons.Auth.AddressPreview,
                PermissionCodes.Buttons.Data.Query,
                PermissionCodes.Buttons.Data.Reset,
                PermissionCodes.Buttons.Data.OpenReport,
                PermissionCodes.Buttons.Data.OpenReportFolder,
                PermissionCodes.Buttons.Program.Add,
                PermissionCodes.Buttons.Program.Edit,
                PermissionCodes.Buttons.Program.Delete,
                PermissionCodes.Buttons.Program.Sync,
                PermissionCodes.Buttons.Program.PullMes,
                PermissionCodes.Buttons.Program.Refresh,
                PermissionCodes.Buttons.Program.BuildName
            },
            [AppConstants.Roles.Readonly] = new[]
            {
                PermissionCodes.Pages.Monitor,
                PermissionCodes.Pages.DataManage,
                PermissionCodes.Buttons.Data.Query,
                PermissionCodes.Buttons.Data.Reset,
                PermissionCodes.Buttons.Data.OpenReport,
                PermissionCodes.Buttons.Data.OpenReportFolder,
                PermissionCodes.Buttons.Auth.SwitchUser,
                PermissionCodes.Buttons.Auth.Logout
            }
        };
    }

    private void RefreshCurrentSessionIfAffected(int roleId)
    {
        var currentUser = GlobalContext.CurrentUser;
        if (currentUser?.RoleId != roleId)
        {
            return;
        }

        var user = _dbContext.Db.Queryable<SysUser>().First(it => it.Id == currentUser.Id);
        if (user is null)
        {
            GlobalContext.Clear();
            return;
        }

        var role = GetRoleById(user.RoleId);
        if (role is not null)
        {
            user.Role = role.RoleCode;
            user.RoleName = role.RoleName;
        }

        GlobalContext.SetCurrentUser(user, GetPermissionCodesByRole(roleId));
    }

    private static PermissionType ParsePermissionType(string? type)
    {
        return Enum.TryParse<PermissionType>(type, true, out var parsed)
            ? parsed
            : PermissionType.Button;
    }

    private static string NormalizeRoleCode(string? roleCode, string? roleName)
    {
        var value = (roleCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            value = (roleName ?? string.Empty).Trim().ToLowerInvariant();
        }

        value = new string(value
            .Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_')
            .ToArray())
            .Trim('_');

        return string.IsNullOrWhiteSpace(value)
            ? $"role_{Guid.NewGuid():N}"
            : value;
    }

    private static void SortTree(List<PermissionTreeNode> nodes)
    {
        nodes.Sort(static (left, right) => left.Sort != right.Sort
            ? left.Sort.CompareTo(right.Sort)
            : string.Compare(left.Code, right.Code, StringComparison.OrdinalIgnoreCase));

        foreach (var node in nodes)
        {
            SortTree(node.Children);
        }
    }
}
