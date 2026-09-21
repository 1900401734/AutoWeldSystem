using AntdUI;
using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.UserManage;
using AutoWeldSystem.UI.Base;
using Message = AntdUI.Message;

namespace AutoWeldSystem.UI.Forms;

public partial class UserEditForm : BaseWindow
{
    private readonly ISysUserService _userService;
    private readonly IRbacService _rbacService;
    private readonly ILocalizationService _localizer;
    private readonly int? _currentUserId;

    public SysUser? SavedUser { get; private set; }

    public UserEditForm(
        ISysUserService userService,
        IRbacService rbacService,
        ILocalizationService localizer,
        int? userId = null)
    {
        InitializeComponent();

        _userService = userService;
        _rbacService = rbacService;
        _localizer = localizer;
        _currentUserId = userId;

        // 密码框应当始终以密文显示，避免屏幕泄露。
        inputPasscode.UseSystemPasswordChar = true;

        Load += UserEditForm_Load;
        btnSave.Click += BtnSave_Click;
    }

    /// <summary>
    /// 语言变化时刷新所有静态文本。
    /// </summary>
    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
    }

    private void UserEditForm_Load(object? sender, EventArgs e)
    {
        ApplyLocalizedTexts();

        var currentUser = _currentUserId.HasValue && _currentUserId.Value > 0
            ? _userService.GetUserById(_currentUserId.Value)
            : null;

        LoadRoleTree(currentUser?.RoleId);
        LoadUserInfo(currentUser);
    }

    /// <summary>
    /// 统一设置标题、标签和按钮文字。
    /// </summary>
    private void ApplyLocalizedTexts()
    {
        Text = _currentUserId.HasValue && _currentUserId.Value > 0
            ? _localizer.GetString(TextKeys.User.EditDialogTitle)
            : _localizer.GetString(TextKeys.User.AddDialogTitle);

        pageHeader1.Text = Text;
        label1.Text = _localizer.GetString(TextKeys.User.LabelName);
        label2.Text = _localizer.GetString(TextKeys.User.LabelNumber);
        label4.Text = _localizer.GetString(TextKeys.User.LabelPassword);
        lblRole.Text = _localizer.GetString(TextKeys.User.LabelRole);
        btnSave.Text = _localizer.GetString(TextKeys.Common.ActionSave);
    }

    /// <summary>
    /// 编辑模式回填用户信息；新增模式保持空白。
    /// 编辑时密码框默认留空，表示“不修改密码”。
    /// </summary>
    private void LoadUserInfo(SysUser? user)
    {
        if (user is null)
        {
            inputUserName.Text = string.Empty;
            inputUserNumber.Text = string.Empty;
            inputPasscode.Text = string.Empty;
            return;
        }

        inputUserName.Text = user.UserName;
        inputUserNumber.Text = user.UserNumber;
        inputPasscode.Text = string.Empty;
    }

    /// <summary>
    /// 用户只能拥有一个角色，所以这里只展示可勾选角色，并在保存时强校验单选。
    /// </summary>
    private void LoadRoleTree(int? selectedRoleId)
    {
        var roles = _rbacService.GetAllRoles()
            .Where(role => role.Enabled || role.Id == selectedRoleId)
            .Where(role => IsCurrentDeveloper()
                || !string.Equals(role.RoleCode, AppConstants.Roles.Developer, StringComparison.OrdinalIgnoreCase))
            .OrderBy(role => role.IsSystem ? 0 : 1)
            .ThenBy(role => role.Id)
            .ToList();

        treeRole.Items.Clear();
        foreach (var role in roles)
        {
            treeRole.Items.Add(new TreeItem
            {
                Text = $"{role.RoleName} ({role.RoleCode})",
                Tag = role.Id,
                Checked = role.Id == selectedRoleId
            });
        }
    }

    /// <summary>
    /// 保存用户，并根据是否输入密码决定“设置新密码”还是“保留旧密码”。
    /// </summary>
    private void BtnSave_Click(object? sender, EventArgs e)
    {
        var userName = inputUserName.Text.Trim();
        var userNumber = inputUserNumber.Text.Trim();
        var plainPassword = inputPasscode.Text.Trim();

        if (string.IsNullOrWhiteSpace(userName))
        {
            Message.error(this, _localizer.GetString(TextKeys.User.NameRequired));
            return;
        }

        if (string.IsNullOrWhiteSpace(userNumber))
        {
            Message.error(this, _localizer.GetString(TextKeys.User.NumberRequired));
            return;
        }

        var selectedRoleIds = GetCheckedRoleIds(treeRole.Items).Distinct().ToArray();
        if (selectedRoleIds.Length == 0)
        {
            Message.error(this, _localizer.GetString(TextKeys.User.RoleRequired));
            return;
        }

        if (selectedRoleIds.Length > 1)
        {
            Message.error(this, _localizer.GetString(TextKeys.User.SingleRoleOnly));
            return;
        }

        var existingUser = _currentUserId.HasValue && _currentUserId.Value > 0
            ? _userService.GetUserById(_currentUserId.Value)
            : null;

        var userToSave = new SysUser
        {
            Id = _currentUserId ?? 0,
            UserName = userName,
            UserNumber = userNumber,
            RoleId = selectedRoleIds[0],
            Enabled = existingUser?.Enabled ?? true
        };

        // 新增用户但未输入密码时，仍然给一个统一默认密码，确保可以登录。
        var passwordToSave = plainPassword;
        if (_currentUserId.GetValueOrDefault() <= 0 && string.IsNullOrWhiteSpace(passwordToSave))
        {
            passwordToSave = AppConstants.Defaults.InitialPassword;
        }

        try
        {
            SavedUser = _userService.SaveUser(
                userToSave,
                string.IsNullOrWhiteSpace(passwordToSave) ? null : passwordToSave);

            var successMessage = _currentUserId.HasValue && _currentUserId.Value > 0
                ? _localizer.GetString(TextKeys.Common.SaveSuccess)
                : string.IsNullOrWhiteSpace(plainPassword)
                    ? _localizer.GetString(TextKeys.User.AddSuccessDefaultPassword, AppConstants.Defaults.InitialPassword)
                    : _localizer.GetString(TextKeys.User.AddSuccessWithPassword);

            Message.success(this, successMessage);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (UserFriendlyException ex)
        {
            Message.error(this, _localizer.GetString(ex.MessageKey, ex.Args.ToArray()));
        }
        catch (Exception ex)
        {
            Message.error(this, _localizer.GetString(TextKeys.Common.SaveFailed, ex.Message));
        }
    }

    /// <summary>
    /// 递归收集已勾选角色 Id。
    /// </summary>
    private List<int> GetCheckedRoleIds(TreeItemCollection items)
    {
        var roleIds = new List<int>();
        foreach (TreeItem item in items)
        {
            if (item.Checked && item.Tag is int roleId)
            {
                roleIds.Add(roleId);
            }

            if (item.Sub.Count > 0)
            {
                roleIds.AddRange(GetCheckedRoleIds(item.Sub));
            }
        }

        return roleIds;
    }

    private static bool IsCurrentDeveloper()
    {
        var currentUser = GlobalContext.CurrentUser;
        return currentUser is not null
            && (string.Equals(currentUser.UserNumber, "dev", StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentUser.Role, AppConstants.Roles.Developer, StringComparison.OrdinalIgnoreCase));
    }
}
