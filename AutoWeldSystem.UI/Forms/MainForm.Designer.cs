namespace AutoWeldSystem.UI.Forms;

partial class MainForm
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        AntdUI.SegmentedItem segmentedItem1 = new AntdUI.SegmentedItem();
        AntdUI.SegmentedItem segmentedItem2 = new AntdUI.SegmentedItem();
        AntdUI.SegmentedItem segmentedItem3 = new AntdUI.SegmentedItem();
        AntdUI.SegmentedItem segmentedItem4 = new AntdUI.SegmentedItem();
        AntdUI.SegmentedItem segmentedItem5 = new AntdUI.SegmentedItem();
        AntdUI.SegmentedItem segmentedItem6 = new AntdUI.SegmentedItem();
        AntdUI.SegmentedItem segmentedItem7 = new AntdUI.SegmentedItem();
        AntdUI.SegmentedItem segmentedItem8 = new AntdUI.SegmentedItem();
        segmented1 = new AntdUI.Segmented();
        pnlContent = new AntdUI.Panel();
        pageHeader2 = new AntdUI.PageHeader();
        tlpUserAndLang = new TableLayoutPanel();
        lblCurrentUser = new AntdUI.Label();
        lblCurUser = new AntdUI.Label();
        select_Lang = new AntdUI.Select();
        lblCurLang = new AntdUI.Label();
        SystemInfoLayout = new TableLayoutPanel();
        btnSwitchUser = new AntdUI.Button();
        btnAddressPreview = new AntdUI.Button();
        btnLogout = new AntdUI.Button();
        tableLayoutPanel1 = new TableLayoutPanel();
        pageHeader2.SuspendLayout();
        tlpUserAndLang.SuspendLayout();
        SystemInfoLayout.SuspendLayout();
        tableLayoutPanel1.SuspendLayout();
        SuspendLayout();
        // 
        // segmented1
        // 
        segmented1.BackColor = SystemColors.Control;
        resources.ApplyResources(segmented1, "segmented1");
        segmented1.Full = true;
        segmentedItem1.LocalizationText = "MonitorView";
        segmentedItem1.Text = "生产监控";
        segmentedItem2.Text = "数据管理";
        segmentedItem3.Text = "用户管理";
        segmentedItem4.Text = "程序管理";
        segmentedItem5.Text = "日志管理";
        segmentedItem6.Text = "上传状态";
        segmentedItem7.Text = "系统设置";
        segmentedItem8.Text = "地址维护";
        segmented1.Items.Add(segmentedItem1);
        segmented1.Items.Add(segmentedItem2);
        segmented1.Items.Add(segmentedItem3);
        segmented1.Items.Add(segmentedItem4);
        segmented1.Items.Add(segmentedItem5);
        segmented1.Items.Add(segmentedItem6);
        segmented1.Items.Add(segmentedItem7);
        segmented1.Items.Add(segmentedItem8);
        segmented1.Name = "segmented1";
        segmented1.SelectIndexChanged += segmented1_SelectIndexChanged;
        // 
        // pnlContent
        // 
        pnlContent.Back = SystemColors.Control;
        pnlContent.BackColor = SystemColors.ControlLightLight;
        resources.ApplyResources(pnlContent, "pnlContent");
        pnlContent.Name = "pnlContent";
        // 
        // pageHeader2
        // 
        pageHeader2.Controls.Add(tlpUserAndLang);
        pageHeader2.Controls.Add(SystemInfoLayout);
        resources.ApplyResources(pageHeader2, "pageHeader2");
        pageHeader2.Name = "pageHeader2";
        pageHeader2.ShowButton = true;
        pageHeader2.ShowIcon = true;
        // 
        // tlpUserAndLang
        // 
        resources.ApplyResources(tlpUserAndLang, "tlpUserAndLang");
        tlpUserAndLang.Controls.Add(lblCurrentUser, 1, 0);
        tlpUserAndLang.Controls.Add(lblCurUser, 0, 0);
        tlpUserAndLang.Controls.Add(select_Lang, 3, 0);
        tlpUserAndLang.Controls.Add(lblCurLang, 2, 0);
        tlpUserAndLang.Name = "tlpUserAndLang";
        // 
        // lblCurrentUser
        // 
        resources.ApplyResources(lblCurrentUser, "lblCurrentUser");
        lblCurrentUser.Name = "lblCurrentUser";
        lblCurrentUser.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // lblCurUser
        // 
        lblCurUser.AutoEllipsis = true;
        lblCurUser.AutoSizeMode = AntdUI.TAutoSize.Width;
        lblCurUser.AutoSizePadding = true;
        resources.ApplyResources(lblCurUser, "lblCurUser");
        lblCurUser.Name = "lblCurUser";
        lblCurUser.TextAlign = ContentAlignment.MiddleRight;
        // 
        // select_Lang
        // 
        select_Lang.BackColor = SystemColors.Control;
        resources.ApplyResources(select_Lang, "select_Lang");
        select_Lang.Name = "select_Lang";
        select_Lang.TextAlign = HorizontalAlignment.Center;
        // 
        // lblCurLang
        // 
        lblCurLang.AutoEllipsis = true;
        lblCurLang.AutoSizeMode = AntdUI.TAutoSize.Width;
        lblCurLang.AutoSizePadding = true;
        resources.ApplyResources(lblCurLang, "lblCurLang");
        lblCurLang.Name = "lblCurLang";
        lblCurLang.TextAlign = ContentAlignment.MiddleRight;
        // 
        // SystemInfoLayout
        // 
        resources.ApplyResources(SystemInfoLayout, "SystemInfoLayout");
        SystemInfoLayout.Controls.Add(btnSwitchUser, 0, 0);
        SystemInfoLayout.Controls.Add(btnAddressPreview, 2, 0);
        SystemInfoLayout.Controls.Add(btnLogout, 1, 0);
        SystemInfoLayout.Name = "SystemInfoLayout";
        // 
        // btnSwitchUser
        // 
        btnSwitchUser.AutoEllipsis = true;
        btnSwitchUser.AutoSizeMode = AntdUI.TAutoSize.Width;
        btnSwitchUser.BorderWidth = 1F;
        btnSwitchUser.DefaultBack = SystemColors.Control;
        btnSwitchUser.DisplayStyle = AntdUI.TButtonDisplayStyle.Image;
        resources.ApplyResources(btnSwitchUser, "btnSwitchUser");
        btnSwitchUser.IconSvg = "UserSwitchOutlined";
        btnSwitchUser.Name = "btnSwitchUser";
        btnSwitchUser.Shape = AntdUI.TShape.Circle;
        btnSwitchUser.Tag = "perm:button.auth.switch-user:visible";
        // 
        // btnAddressPreview
        // 
        btnAddressPreview.AutoSizeMode = AntdUI.TAutoSize.Width;
        btnAddressPreview.BorderWidth = 1F;
        btnAddressPreview.DefaultBack = SystemColors.Control;
        btnAddressPreview.DisplayStyle = AntdUI.TButtonDisplayStyle.Image;
        resources.ApplyResources(btnAddressPreview, "btnAddressPreview");
        btnAddressPreview.IconSvg = "EyeOutlined";
        btnAddressPreview.Name = "btnAddressPreview";
        btnAddressPreview.Shape = AntdUI.TShape.Circle;
        btnAddressPreview.Tag = "perm:button.auth.address-preview:visible";
        // 
        // btnLogout
        // 
        btnLogout.AutoEllipsis = true;
        btnLogout.AutoSizeMode = AntdUI.TAutoSize.Width;
        btnLogout.BorderWidth = 1F;
        btnLogout.DefaultBack = SystemColors.Control;
        btnLogout.DisplayStyle = AntdUI.TButtonDisplayStyle.Image;
        resources.ApplyResources(btnLogout, "btnLogout");
        btnLogout.IconSvg = "LogoutOutlined";
        btnLogout.Name = "btnLogout";
        btnLogout.Shape = AntdUI.TShape.Circle;
        btnLogout.Tag = "perm:button.auth.logout:visible";
        // 
        // tableLayoutPanel1
        // 
        resources.ApplyResources(tableLayoutPanel1, "tableLayoutPanel1");
        tableLayoutPanel1.Controls.Add(pageHeader2, 0, 0);
        tableLayoutPanel1.Controls.Add(pnlContent, 0, 1);
        tableLayoutPanel1.Name = "tableLayoutPanel1";
        // 
        // MainForm
        // 
        resources.ApplyResources(this, "$this");
        AutoScaleMode = AutoScaleMode.Dpi;
        Controls.Add(tableLayoutPanel1);
        Controls.Add(segmented1);
        Name = "MainForm";
        WindowState = FormWindowState.Maximized;
        pageHeader2.ResumeLayout(false);
        pageHeader2.PerformLayout();
        tlpUserAndLang.ResumeLayout(false);
        tlpUserAndLang.PerformLayout();
        SystemInfoLayout.ResumeLayout(false);
        SystemInfoLayout.PerformLayout();
        tableLayoutPanel1.ResumeLayout(false);
        ResumeLayout(false);
    }
    private AntdUI.Segmented segmented1;
    private AntdUI.Panel pnlContent;
    private AntdUI.PageHeader pageHeader2;
    private TableLayoutPanel tableLayoutPanel1;
    private TableLayoutPanel SystemInfoLayout;
    private AntdUI.Label lblCurLang;
    private AntdUI.Label lblCurUser;
    private AntdUI.Select select_Lang;
    private AntdUI.Button btnLogout;
    private AntdUI.Button btnSwitchUser;
    private AntdUI.Button btnAddressPreview;
    private TableLayoutPanel tlpUserAndLang;
    private AntdUI.Label lblCurrentUser;
}
