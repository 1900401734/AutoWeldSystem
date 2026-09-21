using AutoWeldSystem.Core.Interfaces.UserManage;

namespace AutoWeldSystem.UI.Infrastructure;

public class PermissionUiBinder
{
    private readonly ISysUserService _userService;
    private readonly Dictionary<Control, EnabledGuard> _enabledGuards = new();

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
        SetPermissionEnabled(control, permissionCode, _userService.HasPermission(permissionCode));
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

        mode = NormalizeMode(permissionCode, mode);
        var allowed = _userService.HasPermission(permissionCode);
        switch (mode)
        {
            case PermissionTagMode.Enabled:
                SetPermissionEnabled(control, permissionCode, allowed);
                break;
            case PermissionTagMode.Both:
                control.Visible = allowed;
                SetPermissionEnabled(control, permissionCode, allowed);
                break;
            default:
                control.Visible = allowed;
                RemoveEnabledGuard(control);
                break;
        }
    }

    /// <summary>
    /// 页面权限控制入口是否显示，按钮权限控制按钮是否可点击。
    /// 这里做一次兜底，避免旧 Tag 写错后继续按隐藏处理。
    /// </summary>
    private static PermissionTagMode NormalizeMode(string permissionCode, PermissionTagMode configuredMode)
    {
        if (permissionCode.StartsWith("button.", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionTagMode.Enabled;
        }

        if (permissionCode.StartsWith("page.", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionTagMode.Visible;
        }

        return configuredMode;
    }

    /// <summary>
    /// 设置权限启用状态，并在无权限时加保护，防止业务刷新逻辑把按钮重新打开。
    /// </summary>
    private void SetPermissionEnabled(Control control, string permissionCode, bool allowed)
    {
        if (allowed)
        {
            RemoveEnabledGuard(control);
            control.Enabled = true;
            return;
        }

        EnsureEnabledGuard(control, permissionCode);
        control.Enabled = false;
    }

    private void EnsureEnabledGuard(Control control, string permissionCode)
    {
        if (!_enabledGuards.TryGetValue(control, out var guard))
        {
            guard = new EnabledGuard(permissionCode);
            guard.EnabledChanged = (_, _) => EnforceEnabledGuard(control);
            guard.Disposed = (_, _) => RemoveEnabledGuard(control);
            control.EnabledChanged += guard.EnabledChanged;
            control.Disposed += guard.Disposed;
            _enabledGuards[control] = guard;
        }

        guard.PermissionCode = permissionCode;
        EnforceEnabledGuard(control);
    }

    private void EnforceEnabledGuard(Control control)
    {
        if (!_enabledGuards.TryGetValue(control, out var guard) || guard.IsResetting)
        {
            return;
        }

        if (_userService.HasPermission(guard.PermissionCode))
        {
            RemoveEnabledGuard(control);
            return;
        }

        if (!control.Enabled)
        {
            return;
        }

        guard.IsResetting = true;
        control.Enabled = false;
        guard.IsResetting = false;
    }

    private void RemoveEnabledGuard(Control control)
    {
        if (!_enabledGuards.Remove(control, out var guard))
        {
            return;
        }

        control.EnabledChanged -= guard.EnabledChanged;
        control.Disposed -= guard.Disposed;
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

    private sealed class EnabledGuard(string permissionCode)
    {
        public string PermissionCode { get; set; } = permissionCode;

        public bool IsResetting { get; set; }

        public EventHandler? EnabledChanged { get; set; }

        public EventHandler? Disposed { get; set; }
    }
}
