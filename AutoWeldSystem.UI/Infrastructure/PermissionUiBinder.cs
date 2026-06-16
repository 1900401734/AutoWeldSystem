using AutoWeldSystem.Core.Interfaces.UserManage;

namespace AutoWeldSystem.UI.Infrastructure;

public class PermissionUiBinder
{
    private readonly ISysUserService _userService;

    public PermissionUiBinder(ISysUserService userService)
    {
        _userService = userService;
    }

    public void Apply(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);
        ApplyRecursive(root);
    }

    public void ApplyVisible(Control control, string permissionCode)
    {
        control.Visible = _userService.HasPermission(permissionCode);
    }

    public void ApplyEnabled(Control control, string permissionCode)
    {
        control.Enabled = _userService.HasPermission(permissionCode);
    }

    private void ApplyRecursive(Control control)
    {
        ApplyPermissionTag(control);
        foreach (Control child in control.Controls)
        {
            ApplyRecursive(child);
        }
    }

    private void ApplyPermissionTag(Control control)
    {
        if (control.Tag is not string tag || string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        if (!TryParse(tag, out var permissionCode, out var mode))
        {
            return;
        }

        var allowed = _userService.HasPermission(permissionCode);
        switch (mode)
        {
            case PermissionTagMode.Enabled:
                control.Enabled = allowed;
                break;
            case PermissionTagMode.Both:
                control.Visible = allowed;
                control.Enabled = allowed;
                break;
            default:
                control.Visible = allowed;
                break;
        }
    }

    private static bool TryParse(string tag, out string permissionCode, out PermissionTagMode mode)
    {
        permissionCode = string.Empty;
        mode = PermissionTagMode.Visible;

        var parts = tag.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2 || !string.Equals(parts[0], "perm", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        permissionCode = parts[1];
        if (parts.Length < 3)
        {
            return true;
        }

        mode = parts[2].ToLowerInvariant() switch
        {
            "enabled" => PermissionTagMode.Enabled,
            "both" => PermissionTagMode.Both,
            _ => PermissionTagMode.Visible
        };

        return true;
    }

    private enum PermissionTagMode
    {
        Visible,
        Enabled,
        Both
    }
}
