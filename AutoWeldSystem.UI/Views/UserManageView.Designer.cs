namespace AutoWeldSystem.UI.Views
{
    partial class UserManageView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            userLayout = new TableLayoutPanel();
            userToolbarLayout = new TableLayoutPanel();
            flowUserButtons = new FlowLayoutPanel();
            btnAddUser = new AntdUI.Button();
            btnEditUser = new AntdUI.Button();
            btnDeleteUser = new AntdUI.Button();
            btnSetRole = new AntdUI.Button();
            btnResetPassword = new AntdUI.Button();
            queryUsers = new AutoWeldSystem.UI.Components.InputQuery(components);
            dgvUsers = new DataGridView();
            tabControl = new TabControl();
            tabPage1 = new TabPage();
            pageRole = new TabPage();
            roleLayout = new TableLayoutPanel();
            roleToolbarLayout = new TableLayoutPanel();
            flowRoleButtons = new FlowLayoutPanel();
            btnAddRole = new AntdUI.Button();
            btnEditRole = new AntdUI.Button();
            btnDeleteRole = new AntdUI.Button();
            btnRefreshRoles = new AntdUI.Button();
            queryRoles = new AutoWeldSystem.UI.Components.InputQuery(components);
            btnSavePermissions = new AntdUI.Button();
            splitRoleContent = new SplitContainer();
            dgvRoles = new DataGridView();
            permissionLayout = new TableLayoutPanel();
            lblSelectedRole = new Label();
            tvPermissions = new TreeView();
            lblPermissionHint = new Label();
            userLayout.SuspendLayout();
            userToolbarLayout.SuspendLayout();
            flowUserButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvUsers).BeginInit();
            tabControl.SuspendLayout();
            tabPage1.SuspendLayout();
            pageRole.SuspendLayout();
            roleLayout.SuspendLayout();
            roleToolbarLayout.SuspendLayout();
            flowRoleButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitRoleContent).BeginInit();
            splitRoleContent.Panel1.SuspendLayout();
            splitRoleContent.Panel2.SuspendLayout();
            splitRoleContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRoles).BeginInit();
            permissionLayout.SuspendLayout();
            SuspendLayout();
            // 
            // userLayout
            // 
            userLayout.ColumnCount = 1;
            userLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            userLayout.Controls.Add(userToolbarLayout, 0, 0);
            userLayout.Controls.Add(dgvUsers, 0, 1);
            userLayout.Dock = DockStyle.Fill;
            userLayout.Location = new Point(3, 3);
            userLayout.Margin = new Padding(4, 3, 4, 3);
            userLayout.Name = "userLayout";
            userLayout.RowCount = 2;
            userLayout.RowStyles.Add(new RowStyle());
            userLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            userLayout.Size = new Size(1352, 703);
            userLayout.TabIndex = 0;
            // 
            // userToolbarLayout
            // 
            userToolbarLayout.ColumnCount = 2;
            userToolbarLayout.ColumnStyles.Add(new ColumnStyle());
            userToolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            userToolbarLayout.Controls.Add(flowUserButtons, 0, 0);
            userToolbarLayout.Controls.Add(queryUsers, 1, 0);
            userToolbarLayout.Dock = DockStyle.Fill;
            userToolbarLayout.Location = new Point(20, 14);
            userToolbarLayout.Margin = new Padding(20, 14, 20, 9);
            userToolbarLayout.Name = "userToolbarLayout";
            userToolbarLayout.RowCount = 1;
            userToolbarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            userToolbarLayout.Size = new Size(1312, 53);
            userToolbarLayout.TabIndex = 0;
            // 
            // flowUserButtons
            // 
            flowUserButtons.AutoSize = true;
            flowUserButtons.Controls.Add(btnAddUser);
            flowUserButtons.Controls.Add(btnEditUser);
            flowUserButtons.Controls.Add(btnDeleteUser);
            flowUserButtons.Controls.Add(btnSetRole);
            flowUserButtons.Controls.Add(btnResetPassword);
            flowUserButtons.Dock = DockStyle.Fill;
            flowUserButtons.Location = new Point(0, 0);
            flowUserButtons.Margin = new Padding(0);
            flowUserButtons.Name = "flowUserButtons";
            flowUserButtons.Size = new Size(555, 53);
            flowUserButtons.TabIndex = 0;
            flowUserButtons.WrapContents = false;
            // 
            // btnAddUser
            // 
            btnAddUser.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnAddUser.BorderWidth = 1F;
            btnAddUser.IconSvg = "UserAddOutlined";
            btnAddUser.Location = new Point(0, 0);
            btnAddUser.Margin = new Padding(0, 0, 10, 0);
            btnAddUser.Name = "btnAddUser";
            btnAddUser.Size = new Size(89, 44);
            btnAddUser.TabIndex = 0;
            btnAddUser.Tag = "perm:button.user.add:visible";
            btnAddUser.Text = "新增";
            // 
            // btnEditUser
            // 
            btnEditUser.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnEditUser.BorderWidth = 1F;
            btnEditUser.IconSvg = "EditOutlined";
            btnEditUser.Location = new Point(99, 0);
            btnEditUser.Margin = new Padding(0, 0, 10, 0);
            btnEditUser.Name = "btnEditUser";
            btnEditUser.Size = new Size(89, 44);
            btnEditUser.TabIndex = 1;
            btnEditUser.Tag = "perm:button.user.edit:visible";
            btnEditUser.Text = "编辑";
            // 
            // btnDeleteUser
            // 
            btnDeleteUser.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnDeleteUser.BorderWidth = 1F;
            btnDeleteUser.IconSvg = "UserDeleteOutlined";
            btnDeleteUser.Location = new Point(198, 0);
            btnDeleteUser.Margin = new Padding(0, 0, 10, 0);
            btnDeleteUser.Name = "btnDeleteUser";
            btnDeleteUser.Size = new Size(89, 44);
            btnDeleteUser.TabIndex = 2;
            btnDeleteUser.Tag = "perm:button.user.delete:visible";
            btnDeleteUser.Text = "删除";
            // 
            // btnSetRole
            // 
            btnSetRole.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSetRole.BorderWidth = 1F;
            btnSetRole.IconSvg = "UserOutlined";
            btnSetRole.Location = new Point(297, 0);
            btnSetRole.Margin = new Padding(0, 0, 10, 0);
            btnSetRole.Name = "btnSetRole";
            btnSetRole.Size = new Size(124, 44);
            btnSetRole.TabIndex = 3;
            btnSetRole.Tag = "perm:button.user.assign-role:visible";
            btnSetRole.Text = "设置角色";
            // 
            // btnResetPassword
            // 
            btnResetPassword.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnResetPassword.BorderWidth = 1F;
            btnResetPassword.IconSvg = "RestOutlined";
            btnResetPassword.Location = new Point(431, 0);
            btnResetPassword.Margin = new Padding(0);
            btnResetPassword.Name = "btnResetPassword";
            btnResetPassword.Size = new Size(124, 44);
            btnResetPassword.TabIndex = 4;
            btnResetPassword.Tag = "perm:button.user.reset-password:visible";
            btnResetPassword.Text = "重置密码";
            // 
            // queryUsers
            // 
            queryUsers.AutoSize = true;
            queryUsers.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            queryUsers.Dock = DockStyle.Right;
            queryUsers.IsShowQueryButton = false;
            queryUsers.IsShowRefreshButton = false;
            queryUsers.Location = new Point(1157, 0);
            queryUsers.Margin = new Padding(0);
            queryUsers.MinimumSize = new Size(125, 46);
            queryUsers.Name = "queryUsers";
            queryUsers.QueryChanged = null;
            queryUsers.Size = new Size(155, 53);
            queryUsers.TabIndex = 1;
            // 
            // dgvUsers
            // 
            dgvUsers.AllowUserToAddRows = false;
            dgvUsers.AllowUserToDeleteRows = false;
            dgvUsers.BackgroundColor = SystemColors.Window;
            dgvUsers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvUsers.Dock = DockStyle.Fill;
            dgvUsers.Location = new Point(20, 85);
            dgvUsers.Margin = new Padding(20, 9, 20, 18);
            dgvUsers.MultiSelect = false;
            dgvUsers.Name = "dgvUsers";
            dgvUsers.ReadOnly = true;
            dgvUsers.RowHeadersVisible = false;
            dgvUsers.RowHeadersWidth = 51;
            dgvUsers.RowTemplate.Height = 25;
            dgvUsers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvUsers.Size = new Size(1312, 600);
            dgvUsers.TabIndex = 1;
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabPage1);
            tabControl.Controls.Add(pageRole);
            tabControl.Dock = DockStyle.Fill;
            tabControl.HotTrack = true;
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1366, 745);
            tabControl.TabIndex = 1;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(userLayout);
            tabPage1.Location = new Point(4, 32);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1358, 709);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "用户管理";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // pageRole
            // 
            pageRole.Controls.Add(roleLayout);
            pageRole.Location = new Point(4, 32);
            pageRole.Name = "pageRole";
            pageRole.Padding = new Padding(3);
            pageRole.Size = new Size(1358, 709);
            pageRole.TabIndex = 1;
            pageRole.Text = "角色权限";
            pageRole.UseVisualStyleBackColor = true;
            // 
            // roleLayout
            // 
            roleLayout.ColumnCount = 1;
            roleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            roleLayout.Controls.Add(roleToolbarLayout, 0, 0);
            roleLayout.Controls.Add(splitRoleContent, 0, 1);
            roleLayout.Dock = DockStyle.Fill;
            roleLayout.Location = new Point(3, 3);
            roleLayout.Margin = new Padding(4, 3, 4, 3);
            roleLayout.Name = "roleLayout";
            roleLayout.RowCount = 2;
            roleLayout.RowStyles.Add(new RowStyle());
            roleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            roleLayout.Size = new Size(1352, 703);
            roleLayout.TabIndex = 0;
            // 
            // roleToolbarLayout
            // 
            roleToolbarLayout.ColumnCount = 3;
            roleToolbarLayout.ColumnStyles.Add(new ColumnStyle());
            roleToolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            roleToolbarLayout.ColumnStyles.Add(new ColumnStyle());
            roleToolbarLayout.Controls.Add(flowRoleButtons, 0, 0);
            roleToolbarLayout.Controls.Add(queryRoles, 1, 0);
            roleToolbarLayout.Controls.Add(btnSavePermissions, 2, 0);
            roleToolbarLayout.Dock = DockStyle.Fill;
            roleToolbarLayout.Location = new Point(20, 14);
            roleToolbarLayout.Margin = new Padding(20, 14, 20, 9);
            roleToolbarLayout.Name = "roleToolbarLayout";
            roleToolbarLayout.RowCount = 1;
            roleToolbarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            roleToolbarLayout.Size = new Size(1312, 53);
            roleToolbarLayout.TabIndex = 0;
            // 
            // flowRoleButtons
            // 
            flowRoleButtons.AutoSize = true;
            flowRoleButtons.Controls.Add(btnAddRole);
            flowRoleButtons.Controls.Add(btnEditRole);
            flowRoleButtons.Controls.Add(btnDeleteRole);
            flowRoleButtons.Controls.Add(btnRefreshRoles);
            flowRoleButtons.Dock = DockStyle.Fill;
            flowRoleButtons.Location = new Point(0, 0);
            flowRoleButtons.Margin = new Padding(0);
            flowRoleButtons.Name = "flowRoleButtons";
            flowRoleButtons.Size = new Size(386, 53);
            flowRoleButtons.TabIndex = 0;
            flowRoleButtons.WrapContents = false;
            // 
            // btnAddRole
            // 
            btnAddRole.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnAddRole.BorderWidth = 1F;
            btnAddRole.IconSvg = "UserAddOutlined";
            btnAddRole.Location = new Point(0, 0);
            btnAddRole.Margin = new Padding(0, 0, 10, 0);
            btnAddRole.Name = "btnAddRole";
            btnAddRole.Size = new Size(89, 44);
            btnAddRole.TabIndex = 0;
            btnAddRole.Tag = "perm:button.role.add:visible";
            btnAddRole.Text = "新增";
            // 
            // btnEditRole
            // 
            btnEditRole.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnEditRole.BorderWidth = 1F;
            btnEditRole.IconSvg = "EditOutlined";
            btnEditRole.Location = new Point(99, 0);
            btnEditRole.Margin = new Padding(0, 0, 10, 0);
            btnEditRole.Name = "btnEditRole";
            btnEditRole.Size = new Size(89, 44);
            btnEditRole.TabIndex = 1;
            btnEditRole.Tag = "perm:button.role.edit:visible";
            btnEditRole.Text = "编辑";
            // 
            // btnDeleteRole
            // 
            btnDeleteRole.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnDeleteRole.BorderWidth = 1F;
            btnDeleteRole.IconSvg = "UserDeleteOutlined";
            btnDeleteRole.Location = new Point(198, 0);
            btnDeleteRole.Margin = new Padding(0, 0, 10, 0);
            btnDeleteRole.Name = "btnDeleteRole";
            btnDeleteRole.Size = new Size(89, 44);
            btnDeleteRole.TabIndex = 2;
            btnDeleteRole.Tag = "perm:button.role.delete:visible";
            btnDeleteRole.Text = "删除";
            // 
            // btnRefreshRoles
            // 
            btnRefreshRoles.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnRefreshRoles.BorderWidth = 1F;
            btnRefreshRoles.IconSvg = "ReloadOutlined";
            btnRefreshRoles.Location = new Point(297, 0);
            btnRefreshRoles.Margin = new Padding(0);
            btnRefreshRoles.Name = "btnRefreshRoles";
            btnRefreshRoles.Size = new Size(89, 44);
            btnRefreshRoles.TabIndex = 3;
            btnRefreshRoles.Tag = "perm:button.role.refresh:visible";
            btnRefreshRoles.Text = "刷新";
            // 
            // queryRoles
            // 
            queryRoles.AutoSize = true;
            queryRoles.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            queryRoles.Dock = DockStyle.Right;
            queryRoles.IsShowQueryButton = false;
            queryRoles.IsShowRefreshButton = false;
            queryRoles.Location = new Point(1069, 0);
            queryRoles.Margin = new Padding(0);
            queryRoles.MinimumSize = new Size(125, 46);
            queryRoles.Name = "queryRoles";
            queryRoles.QueryChanged = null;
            queryRoles.Size = new Size(155, 53);
            queryRoles.TabIndex = 1;
            // 
            // btnSavePermissions
            // 
            btnSavePermissions.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSavePermissions.Dock = DockStyle.Fill;
            btnSavePermissions.Location = new Point(1244, 0);
            btnSavePermissions.Margin = new Padding(20, 0, 0, 0);
            btnSavePermissions.Name = "btnSavePermissions";
            btnSavePermissions.Size = new Size(68, 53);
            btnSavePermissions.TabIndex = 2;
            btnSavePermissions.Tag = "perm:button.role.assign-permissions:visible";
            btnSavePermissions.Text = "应用";
            // 
            // splitRoleContent
            // 
            splitRoleContent.Dock = DockStyle.Fill;
            splitRoleContent.Location = new Point(20, 85);
            splitRoleContent.Margin = new Padding(20, 9, 20, 18);
            splitRoleContent.Name = "splitRoleContent";
            // 
            // splitRoleContent.Panel1
            // 
            splitRoleContent.Panel1.Controls.Add(dgvRoles);
            splitRoleContent.Panel1.Padding = new Padding(0, 0, 15, 0);
            // 
            // splitRoleContent.Panel2
            // 
            splitRoleContent.Panel2.Controls.Add(permissionLayout);
            splitRoleContent.Panel2.Padding = new Padding(15, 0, 0, 0);
            splitRoleContent.Size = new Size(1312, 600);
            splitRoleContent.SplitterDistance = 656;
            splitRoleContent.SplitterWidth = 5;
            splitRoleContent.TabIndex = 1;
            // 
            // dgvRoles
            // 
            dgvRoles.AllowUserToAddRows = false;
            dgvRoles.AllowUserToDeleteRows = false;
            dgvRoles.BackgroundColor = SystemColors.Window;
            dgvRoles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvRoles.Dock = DockStyle.Fill;
            dgvRoles.Location = new Point(0, 0);
            dgvRoles.Margin = new Padding(4, 3, 4, 3);
            dgvRoles.MultiSelect = false;
            dgvRoles.Name = "dgvRoles";
            dgvRoles.ReadOnly = true;
            dgvRoles.RowHeadersVisible = false;
            dgvRoles.RowHeadersWidth = 51;
            dgvRoles.RowTemplate.Height = 25;
            dgvRoles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvRoles.Size = new Size(641, 600);
            dgvRoles.TabIndex = 0;
            // 
            // permissionLayout
            // 
            permissionLayout.ColumnCount = 1;
            permissionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            permissionLayout.Controls.Add(lblSelectedRole, 0, 0);
            permissionLayout.Controls.Add(tvPermissions, 0, 1);
            permissionLayout.Controls.Add(lblPermissionHint, 0, 2);
            permissionLayout.Dock = DockStyle.Fill;
            permissionLayout.Location = new Point(15, 0);
            permissionLayout.Margin = new Padding(4, 3, 4, 3);
            permissionLayout.Name = "permissionLayout";
            permissionLayout.RowCount = 3;
            permissionLayout.RowStyles.Add(new RowStyle());
            permissionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            permissionLayout.RowStyles.Add(new RowStyle());
            permissionLayout.Size = new Size(636, 600);
            permissionLayout.TabIndex = 0;
            // 
            // lblSelectedRole
            // 
            lblSelectedRole.AutoSize = true;
            lblSelectedRole.Dock = DockStyle.Fill;
            lblSelectedRole.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold);
            lblSelectedRole.Location = new Point(0, 0);
            lblSelectedRole.Margin = new Padding(0, 0, 0, 14);
            lblSelectedRole.Name = "lblSelectedRole";
            lblSelectedRole.Size = new Size(636, 25);
            lblSelectedRole.TabIndex = 0;
            lblSelectedRole.Text = "请选择左侧角色以编辑权限";
            // 
            // tvPermissions
            // 
            tvPermissions.CheckBoxes = true;
            tvPermissions.Dock = DockStyle.Fill;
            tvPermissions.Location = new Point(0, 39);
            tvPermissions.Margin = new Padding(0);
            tvPermissions.Name = "tvPermissions";
            tvPermissions.Size = new Size(636, 528);
            tvPermissions.TabIndex = 1;
            // 
            // lblPermissionHint
            // 
            lblPermissionHint.AutoSize = true;
            lblPermissionHint.Dock = DockStyle.Fill;
            lblPermissionHint.ForeColor = SystemColors.GrayText;
            lblPermissionHint.Location = new Point(0, 576);
            lblPermissionHint.Margin = new Padding(0, 9, 0, 0);
            lblPermissionHint.Name = "lblPermissionHint";
            lblPermissionHint.Size = new Size(636, 24);
            lblPermissionHint.TabIndex = 2;
            lblPermissionHint.Text = "勾选后点击“应用”，页面权限控制界面入口，按钮权限控制操作入口。";
            // 
            // UserManageView
            // 
            AutoScaleDimensions = new SizeF(10F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tabControl);
            Font = new Font("Microsoft YaHei UI", 10.5F);
            Margin = new Padding(4, 3, 4, 3);
            Name = "UserManageView";
            Size = new Size(1366, 745);
            userLayout.ResumeLayout(false);
            userToolbarLayout.ResumeLayout(false);
            userToolbarLayout.PerformLayout();
            flowUserButtons.ResumeLayout(false);
            flowUserButtons.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvUsers).EndInit();
            tabControl.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            pageRole.ResumeLayout(false);
            roleLayout.ResumeLayout(false);
            roleToolbarLayout.ResumeLayout(false);
            roleToolbarLayout.PerformLayout();
            flowRoleButtons.ResumeLayout(false);
            flowRoleButtons.PerformLayout();
            splitRoleContent.Panel1.ResumeLayout(false);
            splitRoleContent.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitRoleContent).EndInit();
            splitRoleContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvRoles).EndInit();
            permissionLayout.ResumeLayout(false);
            permissionLayout.PerformLayout();
            ResumeLayout(false);
        }
        private TableLayoutPanel userLayout;
        private TableLayoutPanel userToolbarLayout;
        private FlowLayoutPanel flowUserButtons;
        private AntdUI.Button btnAddUser;
        private AntdUI.Button btnEditUser;
        private AntdUI.Button btnDeleteUser;
        private AntdUI.Button btnSetRole;
        private AntdUI.Button btnResetPassword;
        private Components.InputQuery queryUsers;
        private DataGridView dgvUsers;
        private TabControl tabControl;
        private TabPage tabPage1;
        private TabPage pageRole;
        private TableLayoutPanel roleLayout;
        private TableLayoutPanel roleToolbarLayout;
        private FlowLayoutPanel flowRoleButtons;
        private AntdUI.Button btnAddRole;
        private AntdUI.Button btnEditRole;
        private AntdUI.Button btnDeleteRole;
        private AntdUI.Button btnRefreshRoles;
        private Components.InputQuery queryRoles;
        private AntdUI.Button btnSavePermissions;
        private SplitContainer splitRoleContent;
        private DataGridView dgvRoles;
        private TableLayoutPanel permissionLayout;
        private Label lblSelectedRole;
        private TreeView tvPermissions;
        private Label lblPermissionHint;
    }
}
