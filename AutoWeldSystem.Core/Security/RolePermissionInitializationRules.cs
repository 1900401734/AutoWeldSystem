using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Security;

/// <summary>
/// 集中定义内置角色的权限初始化和页签权限升级策略。
/// 纯规则类避免数据库初始化代码重复判断角色和默认页签。
/// </summary>
public static class RolePermissionInitializationRules
{
    /// <summary>
    /// 生成开发者或管理员首次安装时的默认权限。
    /// 开发者拥有全部权限，管理员保留原有页面和按钮，但只开放客户默认页签。
    /// </summary>
    public static IReadOnlyList<string> ResolveElevatedRoleDefaults(
        string? roleCode,
        IEnumerable<string> allPermissionCodes)
    {
        var allCodes = (allPermissionCodes ?? Array.Empty<string>())
            .Where(static code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (IsDeveloper(roleCode))
        {
            return allCodes;
        }

        if (!IsAdmin(roleCode))
        {
            return Array.Empty<string>();
        }

        var stateTabCodes = PermissionCodes.Tabs.State.All.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return allCodes
            .Where(code => !stateTabCodes.Contains(code))
            .Concat(PermissionCodes.Tabs.State.CustomerDefaults)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// 只有开发者在每次启动时自动补齐权限，防止覆盖管理员的人工配置。
    /// </summary>
    public static bool ShouldAppendMissingDefaults(string? roleCode)
    {
        return IsDeveloper(roleCode);
    }

    /// <summary>
    /// 旧数据库首次出现页签权限目录时，为已有页面权限的角色补齐对应的默认页签。
    /// 后续启动返回空集合，因此不会重新补回管理员取消的页签。
    /// </summary>
    public static IReadOnlyList<string> ResolveTabUpgradeDefaults(
        string? roleCode,
        bool tabCatalogWasMissing,
        bool hasParentPagePermission,
        IEnumerable<string> upgradeDefaultTabCodes)
    {
        if (!tabCatalogWasMissing || !hasParentPagePermission || IsDeveloper(roleCode))
        {
            return Array.Empty<string>();
        }

        return (upgradeDefaultTabCodes ?? Array.Empty<string>())
            .Where(static code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// 旧数据库首次出现待上传数据页签权限目录时，为已有待上传页面权限的客户角色开放三个默认页签。
    /// 后续启动返回空集合，因此不会重新补回管理员取消的页签。
    /// </summary>
    public static IReadOnlyList<string> ResolveStateTabUpgradeDefaults(
        string? roleCode,
        bool stateTabCatalogWasMissing,
        bool hasStateManagePagePermission)
    {
        return ResolveTabUpgradeDefaults(
            roleCode,
            stateTabCatalogWasMissing,
            hasStateManagePagePermission,
            PermissionCodes.Tabs.State.CustomerDefaults);
    }

    /// <summary>
    /// 旧数据库首次出现历史数据删除权限时，为已有历史数据页权限的管理员补齐该权限。
    /// 后续启动返回空集合，因此不会重新补回管理员手工取消的权限。
    /// </summary>
    public static IReadOnlyList<string> ResolveDataDeleteUpgradeDefaults(
        string? roleCode,
        bool dataDeleteCatalogWasMissing,
        bool hasDataManagePagePermission)
    {
        if (!dataDeleteCatalogWasMissing || !hasDataManagePagePermission || !IsAdmin(roleCode))
        {
            return Array.Empty<string>();
        }

        return new[] { PermissionCodes.Buttons.Data.Delete };
    }

    private static bool IsDeveloper(string? roleCode)
    {
        return string.Equals(roleCode, AppConstants.Roles.Developer, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAdmin(string? roleCode)
    {
        return string.Equals(roleCode, AppConstants.Roles.Admin, StringComparison.OrdinalIgnoreCase);
    }
}
