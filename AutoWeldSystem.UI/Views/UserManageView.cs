using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Security;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Forms;
using AutoWeldSystem.UI.Infrastructure;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces.UserManage;

namespace AutoWeldSystem.UI.Views;

public partial class UserManageView : BaseView
{
    private readonly ISysUserService _userService;
    private readonly IRbacService _rbacService;
    private readonly ILocalizationService _localizer;

    private readonly BindingSource _roleBindingSource = new();
    private readonly BindingSource _userBindingSource = new();

    private readonly List<SysRole> _allRoles = new();
    private readonly List<SysUser> _allUsers = new();

    private bool _initialized;
    private bool _handlingTreeCheck;
    private string _roleKeyword = string.Empty;
    private string _userKeyword = string.Empty;

    public UserManageView(ISysUserService userService, IRbacService rbacService, ILocalizationService localizer)
    {
        _userService = userService;
        _rbacService = rbacService;
        _localizer = localizer;

        InitializeComponent();

        ConfigureGrids();
        ConfigureQueries();
        WireEvents();
    }

    /// <summary>
    /// 语言变化时刷新按钮、标签和表格头。
    /// </summary>
    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ApplyRoleGridHeaders();
        ApplyUserGridHeaders();

        if (_initialized)
        {
            LoadPermissionTree(GetSelectedRole()?.Id);
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (_initialized)
        {
            return;
        }

        _initialized = true;
        ReloadAll();
    }

    /// <summary>
    /// 统一初始化两个表格。
    /// </summary>
    private void ConfigureGrids()
    {
        ConfigureRoleGrid();
        ConfigureUserGrid();
    }

    private void ConfigureQueries()
    {
        queryUsers.Text = string.Empty;
        queryRoles.Text = string.Empty;
    }

    /// <summary>
    /// 角色表结构只初始化一次，列标题走独立方法，方便语言切换时复用。
    /// </summary>
    private void ConfigureRoleGrid()
    {
        TableStyleHelper.ApplyDataGridView(dgvRoles);
        dgvRoles.AutoGenerateColumns = false;
        dgvRoles.Columns.Clear();

        dgvRoles.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SysRole.RoleCode),
            FillWeight = 20
        });
        dgvRoles.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SysRole.RoleName),
            FillWeight = 20
        });
        dgvRoles.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SysRole.Description),
            FillWeight = 35
        });
        dgvRoles.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(SysRole.Enabled),
            FillWeight = 10
        });
        dgvRoles.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SysRole.UpdatedTime),
            DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" },
            FillWeight = 25
        });

        dgvRoles.DataSource = _roleBindingSource;
        ApplyRoleGridHeaders();
    }

    /// <summary>
    /// 用户表结构只初始化一次，列标题同样单独处理。
    /// </summary>
    private void ConfigureUserGrid()
    {
        TableStyleHelper.ApplyDataGridView(dgvUsers);
        dgvUsers.AutoGenerateColumns = false;
        dgvUsers.Columns.Clear();

        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SysUser.Id),
            FillWeight = 12
        });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SysUser.UserNumber),
            FillWeight = 18
        });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SysUser.UserName),
            FillWeight = 18
        });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SysUser.RoleName),
            FillWeight = 16
        });
        dgvUsers.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(SysUser.Enabled),
            FillWeight = 10
        });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SysUser.LastLoginTime),
            DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" },
            FillWeight = 22
        });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SysUser.UpdatedTime),
            DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" },
            FillWeight = 22
        });

        dgvUsers.DataSource = _userBindingSource;
        ApplyUserGridHeaders();
    }

    /// <summary>
    /// 把运行时事件集中绑定，方便后续排查交互问题。
    /// </summary>
    private void WireEvents()
    {
        queryUsers.QueryClick += (_, keyword) => ApplyUserFilter(keyword);
        queryRoles.QueryClick += (_, keyword) => ApplyRoleFilter(keyword);

        dgvRoles.CellDoubleClick += (_, _) => BtnEditRole_Click(this, EventArgs.Empty);
        // 点击角色行后立即刷新右侧权限树，避免必须手动刷新角色列表。
        dgvRoles.SelectionChanged += DgvRoles_SelectionChanged;
        dgvUsers.CellDoubleClick += (_, _) => BtnEditUser_Click(this, EventArgs.Empty);
        tvPermissions.AfterCheck += TvPermissions_AfterCheck;

        btnAddRole.Click += BtnAddRole_Click;
        btnEditRole.Click += BtnEditRole_Click;
        btnDeleteRole.Click += BtnDeleteRole_Click;
        btnSavePermissions.Click += BtnSavePermissions_Click;
        btnRefreshRoles.Click += (_, _) => ReloadRoles(GetSelectedRole()?.Id);

        btnAddUser.Click += BtnAddUser_Click;
        btnEditUser.Click += BtnEditUser_Click;
        btnDeleteUser.Click += BtnDeleteUser_Click;
        btnSetRole.Click += BtnSetRole_Click;
        btnResetPassword.Click += BtnResetPassword_Click;
    }

    /// <summary>
    /// 所有纯文本控件统一在这里设置，便于切语言时刷新。
    /// </summary>
    private void ApplyLocalizedTexts()
    {
        if (tabControl.TabCount >= 2)
        {
            tabControl.TabPages[0].Text = _localizer.GetString(TextKeys.User.TabTitle);
            tabControl.TabPages[1].Text = _localizer.GetString(TextKeys.Role.TabTitle);
        }

        btnAddRole.Text = _localizer.GetString(TextKeys.Common.ActionAdd);
        btnEditRole.Text = _localizer.GetString(TextKeys.Common.ActionEdit);
        btnDeleteRole.Text = _localizer.GetString(TextKeys.Common.ActionDelete);
        btnRefreshRoles.Text = _localizer.GetString(TextKeys.Common.ActionRefresh);
        btnSavePermissions.Text = _localizer.GetString(TextKeys.Common.ActionApply);

        btnAddUser.Text = _localizer.GetString(TextKeys.Common.ActionAdd);
        btnEditUser.Text = _localizer.GetString(TextKeys.Common.ActionEdit);
        btnDeleteUser.Text = _localizer.GetString(TextKeys.Common.ActionDelete);
        btnSetRole.Text = _localizer.GetString(TextKeys.Common.ActionSetRole);
        btnResetPassword.Text = _localizer.GetString(TextKeys.Common.ActionResetPassword);

        lblPermissionHint.Text = _localizer.GetString(TextKeys.Role.PermissionHint);
        if (GetSelectedRole() is null)
        {
            lblSelectedRole.Text = _localizer.GetString(TextKeys.Role.SelectLeft);
        }
    }

    private void ApplyRoleGridHeaders()
    {
        if (dgvRoles.Columns.Count < 5)
        {
            return;
        }

        dgvRoles.Columns[0].HeaderText = _localizer.GetString(TextKeys.Grid.RoleCode);
        dgvRoles.Columns[1].HeaderText = _localizer.GetString(TextKeys.Grid.RoleName);
        dgvRoles.Columns[2].HeaderText = _localizer.GetString(TextKeys.Grid.RoleDescription);
        dgvRoles.Columns[3].HeaderText = _localizer.GetString(TextKeys.Grid.RoleEnabled);
        dgvRoles.Columns[4].HeaderText = _localizer.GetString(TextKeys.Grid.RoleUpdatedTime);
    }

    private void ApplyUserGridHeaders()
    {
        if (dgvUsers.Columns.Count < 7)
        {
            return;
        }

        dgvUsers.Columns[0].HeaderText = _localizer.GetString(TextKeys.Grid.UserId);
        dgvUsers.Columns[1].HeaderText = _localizer.GetString(TextKeys.Grid.UserNumber);
        dgvUsers.Columns[2].HeaderText = _localizer.GetString(TextKeys.Grid.UserName);
        dgvUsers.Columns[3].HeaderText = _localizer.GetString(TextKeys.Grid.UserRole);
        dgvUsers.Columns[4].HeaderText = _localizer.GetString(TextKeys.Grid.UserEnabled);
        dgvUsers.Columns[5].HeaderText = _localizer.GetString(TextKeys.Grid.UserLastLoginTime);
        dgvUsers.Columns[6].HeaderText = _localizer.GetString(TextKeys.Grid.UserUpdatedTime);
    }

    /// <summary>
    /// 初次加载或操作完成后，统一刷新用户和角色数据。
    /// </summary>
    private void ReloadAll(int? roleId = null, int? userId = null)
    {
        ReloadRoles(roleId);
        ReloadUsers(userId);
    }

    private void ReloadRoles(int? selectedRoleId = null)
    {
        _allRoles.Clear();
        _allRoles.AddRange(_rbacService.GetAllRoles().Where(CanShowRole));
        ApplyRoleFilter(_roleKeyword, selectedRoleId);
    }

    private void ReloadUsers(int? selectedUserId = null)
    {
        _allUsers.Clear();
        _allUsers.AddRange(_userService.GetAllUsers());
        ApplyUserFilter(_userKeyword, selectedUserId);
    }

    /// <summary>
    /// 角色搜索结果会保持选中态，避免刷新后光标丢失。
    /// </summary>
    private void ApplyRoleFilter(string? keyword, int? selectedRoleId = null)
    {
        _roleKeyword = keyword?.Trim() ?? string.Empty;

        var filteredRoles = _allRoles
            .Where(role => string.IsNullOrWhiteSpace(_roleKeyword)
                || Contains(role.RoleCode, _roleKeyword)
                || Contains(role.RoleName, _roleKeyword)
                || Contains(role.Description, _roleKeyword))
            .OrderBy(role => role.IsSystem ? 0 : 1)
            .ThenBy(role => role.Id)
            .ToList();

        _roleBindingSource.DataSource = filteredRoles;
        if (filteredRoles.Count == 0)
        {
            lblSelectedRole.Text = _localizer.GetString(TextKeys.Role.NoMatch);
            tvPermissions.Nodes.Clear();
            return;
        }

        SelectGridRow(dgvRoles, selectedRoleId);
        EnsureCurrentRow(dgvRoles);
        LoadPermissionTree(GetSelectedRole()?.Id);
    }

    /// <summary>
    /// 用户搜索结果同样保持选中态。
    /// </summary>
    private void ApplyUserFilter(string? keyword, int? selectedUserId = null)
    {
        _userKeyword = keyword?.Trim() ?? string.Empty;

        var filteredUsers = _allUsers
            .Where(user => string.IsNullOrWhiteSpace(_userKeyword)
                || Contains(user.UserNumber, _userKeyword)
                || Contains(user.UserName, _userKeyword)
                || Contains(user.RoleName, _userKeyword))
            .OrderBy(user => user.Id)
            .ToList();

        _userBindingSource.DataSource = filteredUsers;
        if (filteredUsers.Count == 0)
        {
            return;
        }

        SelectGridRow(dgvUsers, selectedUserId);
        EnsureCurrentRow(dgvUsers);
    }

    /// <summary>
    /// 权限树显示和勾选状态都由服务层提供数据，View 只负责渲染。
    /// </summary>
    private void LoadPermissionTree(int? roleId)
    {
        _handlingTreeCheck = true;
        tvPermissions.BeginUpdate();
        tvPermissions.Nodes.Clear();

        if (!roleId.HasValue || roleId.Value <= 0)
        {
            lblSelectedRole.Text = _localizer.GetString(TextKeys.Role.SelectLeft);
            tvPermissions.EndUpdate();
            _handlingTreeCheck = false;
            return;
        }

        var role = _rbacService.GetRoleById(roleId.Value);
        lblSelectedRole.Text = role is null
            ? _localizer.GetString(TextKeys.Role.SelectLeft)
            : _localizer.GetString(TextKeys.Role.CurrentSelection, role.RoleName, role.RoleCode);

        var permissions = _rbacService.GetPermissionTree(roleId.Value, true);
        foreach (var permission in permissions)
        {
            tvPermissions.Nodes.Add(CreatePermissionNode(permission));
        }

        tvPermissions.ExpandAll();
        ScrollPermissionTreeToTop();
        tvPermissions.EndUpdate();
        _handlingTreeCheck = false;
    }

    /// <summary>
    /// 权限树展开后，WinForms 可能自动滚动到最后一个节点，这里统一恢复到顶部。
    /// </summary>
    private void ScrollPermissionTreeToTop()
    {
        if (tvPermissions.Nodes.Count == 0)
        {
            return;
        }

        var firstNode = tvPermissions.Nodes[0];
        tvPermissions.TopNode = firstNode;
        firstNode.EnsureVisible();
    }

    private TreeNode CreatePermissionNode(PermissionTreeNode node)
    {
        var treeNode = new TreeNode(GetPermissionText(node))
        {
            Tag = node,
            Checked = node.Checked
        };

        foreach (var child in node.Children.OrderBy(item => item.Sort))
        {
            treeNode.Nodes.Add(CreatePermissionNode(child));
        }

        return treeNode;
    }

    private string GetPermissionText(PermissionTreeNode node)
    {
        var textKey = PermissionTextKeyMapper.GetTextKey(node.Code);
        return string.IsNullOrWhiteSpace(textKey)
            ? node.Name
            : _localizer.GetString(textKey);
    }

    private void DgvRoles_SelectionChanged(object? sender, EventArgs e)
    {
        LoadPermissionTree(GetSelectedRole()?.Id);
    }

    /// <summary>
    /// 子节点跟随父节点，父节点根据子节点聚合状态自动更新。
    /// </summary>
    private void TvPermissions_AfterCheck(object? sender, TreeViewEventArgs e)
    {
        var node = e.Node;
        if (_handlingTreeCheck || node is null)
        {
            return;
        }

        _handlingTreeCheck = true;
        SetChildCheckedState(node, node.Checked);
        UpdateParentCheckedState(node.Parent);
        _handlingTreeCheck = false;
    }

    private void SetChildCheckedState(TreeNode node, bool isChecked)
    {
        foreach (TreeNode child in node.Nodes)
        {
            child.Checked = isChecked;
            SetChildCheckedState(child, isChecked);
        }
    }

    private void UpdateParentCheckedState(TreeNode? node)
    {
        if (node is null)
        {
            return;
        }

        node.Checked = node.Nodes.Cast<TreeNode>().Any(child => child.Checked);
        UpdateParentCheckedState(node.Parent);
    }

    private void BtnSavePermissions_Click(object? sender, EventArgs e)
    {
        var role = GetSelectedRole();
        if (role is null)
        {
            ShowWarning(TextKeys.Role.SelectFirst);
            return;
        }

        try
        {
            var permissionIds = CollectCheckedPermissionIds(tvPermissions.Nodes).Distinct().ToArray();
            _rbacService.SaveRolePermissions(role.Id, permissionIds);
            ReloadRoles(role.Id);
            ShowInfo(TextKeys.Role.PermissionsApplied);
        }
        catch (UserFriendlyException ex)
        {
            ShowError(ex);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    #region 用户操作

    private void BtnAddUser_Click(object? sender, EventArgs e)
    {
        if (!HasEnabledRoles())
        {
            ShowWarning(TextKeys.User.NoEnabledRoles);
            return;
        }

        OpenUserEditor();
    }

    private void BtnEditUser_Click(object? sender, EventArgs e)
    {
        var user = GetSelectedUser();
        if (user is null)
        {
            ShowWarning(TextKeys.User.SelectFirst);
            return;
        }

        if (!HasEnabledRoles())
        {
            ShowWarning(TextKeys.User.NoEnabledRoles);
            return;
        }

        OpenUserEditor(user.Id);
    }

    private void BtnDeleteUser_Click(object? sender, EventArgs e)
    {
        var user = GetSelectedUser();
        if (user is null)
        {
            ShowWarning(TextKeys.User.SelectFirst);
            return;
        }

        if (!Confirm(
                _localizer.GetString(TextKeys.User.DeleteConfirm, user.UserName),
                _localizer.GetString(TextKeys.Common.TitleConfirmDelete)))
        {
            return;
        }

        if (!_userService.DeleteUser(user.Id))
        {
            ShowWarning(TextKeys.User.DeleteSelfBlocked);
            return;
        }

        ReloadUsers();
    }

    private void BtnSetRole_Click(object? sender, EventArgs e)
    {
        var user = GetSelectedUser();
        if (user is null)
        {
            ShowWarning(TextKeys.User.SelectFirst);
            return;
        }

        if (!HasEnabledRoles())
        {
            ShowWarning(TextKeys.User.NoEnabledRoles);
            return;
        }

        OpenUserEditor(user.Id);
    }

    private void BtnResetPassword_Click(object? sender, EventArgs e)
    {
        var user = GetSelectedUser();
        if (user is null)
        {
            ShowWarning(TextKeys.User.SelectFirst);
            return;
        }

        if (!Confirm(
                _localizer.GetString(TextKeys.User.ResetPasswordConfirm, user.UserName, AppConstants.Defaults.InitialPassword),
                _localizer.GetString(TextKeys.Common.TitleConfirmReset)))
        {
            return;
        }

        try
        {
            _userService.SaveUser(user, AppConstants.Defaults.InitialPassword);
            ReloadUsers(user.Id);
            ShowInfo(TextKeys.User.ResetPasswordSuccess, AppConstants.Defaults.InitialPassword);
        }
        catch (UserFriendlyException ex)
        {
            ShowError(ex);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    /// <summary>
    /// 用户编辑弹窗统一从这里打开，避免新增和编辑各写一套流程。
    /// </summary>
    private void OpenUserEditor(int? userId = null)
    {
        using var form = new UserEditForm(_userService, _rbacService, _localizer, userId);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        ReloadUsers(form.SavedUser?.Id ?? userId);
    }

    #endregion

    #region 角色操作

    private void BtnAddRole_Click(object? sender, EventArgs e)
    {
        OpenRoleEditor();
    }

    private void BtnEditRole_Click(object? sender, EventArgs e)
    {
        var role = GetSelectedRole();
        if (role is null)
        {
            ShowWarning(TextKeys.Role.SelectFirst);
            return;
        }

        OpenRoleEditor(role.Id);
    }

    private void BtnDeleteRole_Click(object? sender, EventArgs e)
    {
        var role = GetSelectedRole();
        if (role is null)
        {
            ShowWarning(TextKeys.Role.SelectFirst);
            return;
        }

        if (!Confirm(
                _localizer.GetString(TextKeys.Role.DeleteConfirm, role.RoleName),
                _localizer.GetString(TextKeys.Common.TitleConfirmDelete)))
        {
            return;
        }

        try
        {
            if (!_rbacService.DeleteRole(role.Id))
            {
                ShowWarning(TextKeys.Role.DeleteBlocked);
                return;
            }

            ReloadRoles();
            ReloadUsers(GetSelectedUser()?.Id);
        }
        catch (UserFriendlyException ex)
        {
            ShowError(ex);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    /// <summary>
    /// 角色编辑弹窗统一从这里打开，新增和编辑复用同一条链路。
    /// </summary>
    private void OpenRoleEditor(int? roleId = null)
    {
        using var form = new RoleEditForm(_rbacService, _localizer, roleId);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        ReloadRoles(form.SavedRole?.Id ?? roleId);
        ReloadUsers(GetSelectedUser()?.Id);
    }

    #endregion

    private bool HasEnabledRoles()
    {
        return _rbacService.GetAllRoles(true).Any(CanShowRole);
    }

    private static bool CanShowRole(SysRole role)
    {
        return IsCurrentDeveloper()
            || !string.Equals(role.RoleCode, AppConstants.Roles.Developer, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCurrentDeveloper()
    {
        var currentUser = GlobalContext.CurrentUser;
        return currentUser is not null
            && (string.Equals(currentUser.UserNumber, "dev", StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentUser.Role, AppConstants.Roles.Developer, StringComparison.OrdinalIgnoreCase));
    }

    private SysRole? GetSelectedRole()
    {
        return dgvRoles.CurrentRow?.DataBoundItem as SysRole;
    }

    private SysUser? GetSelectedUser()
    {
        return dgvUsers.CurrentRow?.DataBoundItem as SysUser;
    }

    /// <summary>
    /// 根据实体 Id 恢复选中行。
    /// </summary>
    private void SelectGridRow(DataGridView grid, int? id)
    {
        if (!id.HasValue || id.Value <= 0)
        {
            return;
        }

        foreach (DataGridViewRow row in grid.Rows)
        {
            switch (row.DataBoundItem)
            {
                case SysRole role when role.Id == id.Value:
                    row.Selected = true;
                    grid.CurrentCell = row.Cells[0];
                    return;
                case SysUser user when user.Id == id.Value:
                    row.Selected = true;
                    grid.CurrentCell = row.Cells[0];
                    return;
            }
        }
    }

    /// <summary>
    /// 如果当前没有选中行，就默认选中第一行，避免后续逻辑拿到 null。
    /// </summary>
    private static void EnsureCurrentRow(DataGridView grid)
    {
        if (grid.CurrentRow is not null || grid.Rows.Count == 0)
        {
            return;
        }

        grid.Rows[0].Selected = true;
        grid.CurrentCell = grid.Rows[0].Cells[0];
    }

    private IEnumerable<int> CollectCheckedPermissionIds(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Checked && node.Tag is PermissionTreeNode permissionNode)
            {
                yield return permissionNode.Id;
            }

            foreach (var childId in CollectCheckedPermissionIds(node.Nodes))
            {
                yield return childId;
            }
        }
    }

    private static bool Contains(string? source, string keyword)
    {
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private void ShowInfo(string messageKey, params object[] args)
    {
        MessageBox.Show(
            this,
            _localizer.GetString(messageKey, args),
            _localizer.GetString(TextKeys.Common.TitleInfo),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ShowWarning(string messageKey, params object[] args)
    {
        MessageBox.Show(
            this,
            _localizer.GetString(messageKey, args),
            _localizer.GetString(TextKeys.Common.TitleWarning),
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private void ShowError(Exception ex)
    {
        var message = ex is UserFriendlyException friendlyException
            ? _localizer.GetString(friendlyException.MessageKey, friendlyException.Args.ToArray())
            : ex.Message;

        ShowError(message);
    }

    private void ShowError(string message)
    {
        MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleError),
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private bool Confirm(string message, string title)
    {
        return MessageBox.Show(this, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            == DialogResult.Yes;
    }
}
