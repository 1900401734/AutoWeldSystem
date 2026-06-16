using AntdUI;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.UserManage;
using AutoWeldSystem.Core.Security;
using AutoWeldSystem.UI.Base;
using Message = AntdUI.Message;

namespace AutoWeldSystem.UI.Forms;

public partial class RoleEditForm : BaseWindow
{
    private readonly IRbacService _rbacService;
    private readonly ILocalizationService _localizer;
    private readonly int? _currentRoleId;

    public SysRole? SavedRole { get; private set; }

    public RoleEditForm(IRbacService rbacService, ILocalizationService localizer, int? roleId = null)
    {
        InitializeComponent();

        _rbacService = rbacService;
        _localizer = localizer;
        _currentRoleId = roleId;

        Load += RoleEditForm_Load;
        btnSave.Click += BtnSave_Click;
        btnCancel.Click += BtnCancel_Click;
    }

    /// <summary>
    /// 语言变化时，统一刷新静态文本和标题。
    /// </summary>
    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ApplyPermissionTreeTexts(treePermissions.Items);
    }

    private void RoleEditForm_Load(object? sender, EventArgs e)
    {
        ApplyLocalizedTexts();
        LoadRoleInfo();
        LoadPermissionTree();
    }

    /// <summary>
    /// 静态标签和按钮文本统一在这里设置，避免散落在构造函数和加载函数里。
    /// </summary>
    private void ApplyLocalizedTexts()
    {
        Text = _currentRoleId.HasValue && _currentRoleId.Value > 0
            ? _localizer.GetString(TextKeys.Role.EditDialogTitle)
            : _localizer.GetString(TextKeys.Role.AddDialogTitle);

        pageHeader1.Text = Text;
        switchEnabled.Text = _localizer.GetString(TextKeys.Role.LabelEnabled);
        btnSave.Text = _localizer.GetString(TextKeys.Common.ActionSave);
        btnCancel.Text = _localizer.GetString(TextKeys.Common.ActionCancel);
        label2.Text = _localizer.GetString(TextKeys.Role.LabelCode);
        label1.Text = _localizer.GetString(TextKeys.Role.LabelName);
        label3.Text = _localizer.GetString(TextKeys.Role.LabelDescription);
    }

    /// <summary>
    /// 编辑模式时回填角色信息；新增模式则给出合理默认值。
    /// </summary>
    private void LoadRoleInfo()
    {
        if (!_currentRoleId.HasValue || _currentRoleId.Value <= 0)
        {
            switchEnabled.Checked = true;
            return;
        }

        var role = _rbacService.GetRoleById(_currentRoleId.Value);
        if (role is null)
        {
            Message.error(this, _localizer.GetString(TextKeys.Role.SelectFirst));
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        inputRoleName.Text = role.RoleName;
        inputRoleCode.Text = role.RoleCode;
        inputDescription.Text = role.Description ?? string.Empty;
        switchEnabled.Checked = role.Enabled;

        // 系统角色的编码通常不允许随意改动，避免破坏权限映射。
        inputRoleCode.Enabled = !role.IsSystem;
    }

    /// <summary>
    /// 把服务层的权限树 DTO 转成 AntdUI 树节点。
    /// </summary>
    private void LoadPermissionTree()
    {
        var treeNodes = _rbacService.GetPermissionTree(_currentRoleId);

        treePermissions.Items.Clear();
        foreach (var node in treeNodes)
        {
            treePermissions.Items.Add(ConvertToTreeItem(node));
        }
    }

    private TreeItem ConvertToTreeItem(PermissionTreeNode node)
    {
        var treeItem = new TreeItem
        {
            Text = GetPermissionText(node.Code, node.Name),
            Tag = new PermissionTreeItemTag(node.Id, node.Code),
            Checked = node.Checked,
            Expand = true
        };

        foreach (var child in node.Children)
        {
            treeItem.Sub.Add(ConvertToTreeItem(child));
        }

        return treeItem;
    }

    /// <summary>
    /// 保存逻辑放在表单内部，调用方只关心“是否保存成功”和“保存后的对象”。
    /// </summary>
    private void BtnSave_Click(object? sender, EventArgs e)
    {
        var roleName = inputRoleName.Text.Trim();
        if (string.IsNullOrWhiteSpace(roleName))
        {
            Message.error(this, _localizer.GetString(TextKeys.Role.NameRequired));
            return;
        }

        var roleToSave = new SysRole
        {
            Id = _currentRoleId ?? 0,
            RoleName = roleName,
            RoleCode = inputRoleCode.Text.Trim(),
            Description = inputDescription.Text,
            Enabled = switchEnabled.Checked
        };

        var selectedPermissionIds = GetCheckedPermissionIds(treePermissions.Items)
            .Distinct()
            .ToArray();

        try
        {
            SavedRole = _rbacService.SaveRole(roleToSave, selectedPermissionIds);
            Message.success(this, _localizer.GetString(TextKeys.Common.SaveSuccess));

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
    /// 递归提取所有勾选权限，便于一次性保存。
    /// </summary>
    private List<int> GetCheckedPermissionIds(TreeItemCollection items)
    {
        var permissionIds = new List<int>();
        foreach (TreeItem item in items)
        {
            if (item.Checked && item.Tag is PermissionTreeItemTag tag)
            {
                permissionIds.Add(tag.Id);
            }

            if (item.Sub.Count > 0)
            {
                permissionIds.AddRange(GetCheckedPermissionIds(item.Sub));
            }
        }

        return permissionIds;
    }

    private void ApplyPermissionTreeTexts(TreeItemCollection items)
    {
        foreach (TreeItem item in items)
        {
            if (item.Tag is PermissionTreeItemTag tag)
            {
                item.Text = GetPermissionText(tag.Code, item.Text ?? string.Empty);
            }

            if (item.Sub.Count > 0)
            {
                ApplyPermissionTreeTexts(item.Sub);
            }
        }
    }

    private string GetPermissionText(string permissionCode, string fallback)
    {
        var textKey = PermissionTextKeyMapper.GetTextKey(permissionCode);
        return string.IsNullOrWhiteSpace(textKey)
            ? fallback
            : _localizer.GetString(textKey);
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private sealed record PermissionTreeItemTag(int Id, string Code);
}
