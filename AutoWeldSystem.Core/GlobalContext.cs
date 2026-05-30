using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Models;
using System.Globalization;

namespace AutoWeldSystem.Core;

public static class GlobalContext
{
    private static HashSet<string> _currentPermissions = new(StringComparer.OrdinalIgnoreCase);

    public static event EventHandler? SessionChanged;
    public static event EventHandler? LanguageChanged;

    public static SysUser? CurrentUser { get; private set; }

    public static IReadOnlyCollection<string> CurrentPermissions => _currentPermissions;

    public static bool IsAdmin => CurrentUser?.Role == AppConstants.Roles.Admin;

    public static bool IsOperator => CurrentUser?.Role == AppConstants.Roles.Operator;
    //public static bool IsOperator => HasRole(UserRole.Operator);

    public static bool IsReadonly => CurrentUser?.Role == AppConstants.Roles.Readonly;
    //public static bool IsReadonly => HasRole(UserRole.Readonly);

    public static bool IsAuthenticated => CurrentUser is not null;

    public static bool IsLogout { get; set; }

    public static string CurrentLanguage { get; private set; } = AppConstants.Languages.Chinese;

    public static void SetLanguage(string cultureCode)
    {
        CurrentLanguage = cultureCode;
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(cultureCode);
        Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureCode);
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static bool HasRole(UserRole role)
        => string.Equals(CurrentUser?.Role, role.ToString(), StringComparison.OrdinalIgnoreCase);

    public static bool HasPermission(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return false;
        }

        return IsAdmin || _currentPermissions.Contains(permissionCode);
    }

    public static void SetCurrentUser(SysUser? user, IEnumerable<string>? permissions = null)
    {
        CurrentUser = user;
        _currentPermissions = permissions is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(
                permissions.Where(static item => !string.IsNullOrWhiteSpace(item)),
                StringComparer.OrdinalIgnoreCase);

        SessionChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void Clear()
    {
        CurrentUser = null;
        _currentPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        SessionChanged?.Invoke(null, EventArgs.Empty);
    }
}
