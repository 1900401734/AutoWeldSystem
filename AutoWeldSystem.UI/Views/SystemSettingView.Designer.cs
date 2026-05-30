namespace AutoWeldSystem.UI.Views
{
    partial class SystemSettingView
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
            rootLayout = new TableLayoutPanel();
            titleLayout = new TableLayoutPanel();
            lblTitle = new Label();
            lblDescription = new Label();
            btnSaveAll = new AntdUI.Button();
            tabSettingCategories = new TabControl();
            tabBasicSettings = new TabPage();
            grpMasterConfig = new GroupBox();
            layoutMasterConfig = new TableLayoutPanel();
            tlpMasterIp = new TableLayoutPanel();
            lblMasterIp = new AntdUI.Label();
            input_MasterIp = new AntdUI.Input();
            btnConnectMasterController = new AntdUI.Button();
            tableLayoutPanel8 = new TableLayoutPanel();
            lblMasterPort = new AntdUI.Label();
            input_MasterPort = new AntdUI.Input();
            grpPlcConfig = new GroupBox();
            tlpPlcConfig = new TableLayoutPanel();
            tlpPlcIp = new TableLayoutPanel();
            lblPlcIp = new AntdUI.Label();
            input_PlcIp = new AntdUI.Input();
            btnConnectPlc = new AntdUI.Button();
            tlpPlcPort = new TableLayoutPanel();
            lblPlcPort = new AntdUI.Label();
            input_PlcPort = new AntdUI.Input();
            tableLayoutPanel7 = new TableLayoutPanel();
            lblPlcType = new AntdUI.Label();
            select_PlcType = new AntdUI.Select();
            groupBox1 = new GroupBox();
            tableLayoutPanel5 = new TableLayoutPanel();
            chkEnableDualStationMode = new AntdUI.Checkbox();
            chkValidateRecipeBeforeStart = new AntdUI.Checkbox();
            grpMesConfig = new GroupBox();
            tableLayoutPanelMesConfig = new TableLayoutPanel();
            chkUseProductNumberFilter = new AntdUI.Checkbox();
            grpAppConfig = new GroupBox();
            layoutAppSettings = new TableLayoutPanel();
            tlpDeviceId = new TableLayoutPanel();
            lblDeviceId = new AntdUI.Label();
            input_DeviceID = new AntdUI.Input();
            btnSyncDevice = new AntdUI.Button();
            tlpDeviceName = new TableLayoutPanel();
            lblDeviceName = new AntdUI.Label();
            input_DeviceName = new AntdUI.Input();
            tlpLogPath = new TableLayoutPanel();
            lblLogPath = new AntdUI.Label();
            input_LogsPath = new AntdUI.Input();
            btnChangeLogPath = new AntdUI.Button();
            btnOpenLogPath = new AntdUI.Button();
            tlpDeviveUrl = new TableLayoutPanel();
            lblDeviceUrl = new AntdUI.Label();
            input_DeviceUrl = new AntdUI.Input();
            tlpMesUrl = new TableLayoutPanel();
            lblMesUrl = new AntdUI.Label();
            input_BaseUrl = new AntdUI.Input();
            btnTestConnection = new AntdUI.Button();
            tlpDataPath = new TableLayoutPanel();
            lblDataPath = new AntdUI.Label();
            input_DataPath = new AntdUI.Input();
            btnChangeDataPath = new AntdUI.Button();
            btnOpenDataPath = new AntdUI.Button();
            rootLayout.SuspendLayout();
            titleLayout.SuspendLayout();
            tabSettingCategories.SuspendLayout();
            tabBasicSettings.SuspendLayout();
            grpMasterConfig.SuspendLayout();
            layoutMasterConfig.SuspendLayout();
            tlpMasterIp.SuspendLayout();
            tableLayoutPanel8.SuspendLayout();
            grpPlcConfig.SuspendLayout();
            tlpPlcConfig.SuspendLayout();
            tlpPlcIp.SuspendLayout();
            tlpPlcPort.SuspendLayout();
            tableLayoutPanel7.SuspendLayout();
            groupBox1.SuspendLayout();
            tableLayoutPanel5.SuspendLayout();
            grpMesConfig.SuspendLayout();
            tableLayoutPanelMesConfig.SuspendLayout();
            grpAppConfig.SuspendLayout();
            layoutAppSettings.SuspendLayout();
            tlpDeviceId.SuspendLayout();
            tlpDeviceName.SuspendLayout();
            tlpLogPath.SuspendLayout();
            tlpDeviveUrl.SuspendLayout();
            tlpMesUrl.SuspendLayout();
            tlpDataPath.SuspendLayout();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(titleLayout, 0, 0);
            rootLayout.Controls.Add(tabSettingCategories, 0, 1);
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.RowCount = 2;
            rootLayout.RowStyles.Add(new RowStyle());
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.Size = new Size(1519, 789);
            rootLayout.TabIndex = 0;
            // 
            // titleLayout
            // 
            titleLayout.AutoSize = true;
            titleLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            titleLayout.ColumnCount = 2;
            titleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            titleLayout.ColumnStyles.Add(new ColumnStyle());
            titleLayout.Controls.Add(lblTitle, 0, 0);
            titleLayout.Controls.Add(lblDescription, 0, 1);
            titleLayout.Controls.Add(btnSaveAll, 1, 0);
            titleLayout.Dock = DockStyle.Fill;
            titleLayout.Location = new Point(24, 3);
            titleLayout.Margin = new Padding(24, 3, 24, 8);
            titleLayout.Name = "titleLayout";
            titleLayout.RowCount = 2;
            titleLayout.RowStyles.Add(new RowStyle());
            titleLayout.RowStyles.Add(new RowStyle());
            titleLayout.Size = new Size(1471, 51);
            titleLayout.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(0, 0);
            lblTitle.Margin = new Padding(0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(1359, 31);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "系统设置";
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblDescription
            // 
            lblDescription.AutoEllipsis = true;
            lblDescription.AutoSize = true;
            lblDescription.Dock = DockStyle.Fill;
            lblDescription.ForeColor = SystemColors.GrayText;
            lblDescription.Location = new Point(0, 31);
            lblDescription.Margin = new Padding(0);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(1359, 20);
            lblDescription.TabIndex = 1;
            lblDescription.Text = "维护基础系统参数、MES 参数、PLC 参数和本地路径。";
            lblDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnSaveAll
            // 
            btnSaveAll.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSaveAll.BorderWidth = 1F;
            btnSaveAll.Dock = DockStyle.Right;
            btnSaveAll.IconSvg = "SaveOutlined";
            btnSaveAll.Location = new Point(1359, 0);
            btnSaveAll.Margin = new Padding(0);
            btnSaveAll.Name = "btnSaveAll";
            titleLayout.SetRowSpan(btnSaveAll, 2);
            btnSaveAll.Size = new Size(112, 51);
            btnSaveAll.TabIndex = 0;
            btnSaveAll.Text = "应用全部";
            // 
            // tabSettingCategories
            // 
            tabSettingCategories.Controls.Add(tabBasicSettings);
            tabSettingCategories.Dock = DockStyle.Fill;
            tabSettingCategories.HotTrack = true;
            tabSettingCategories.Location = new Point(24, 65);
            tabSettingCategories.Margin = new Padding(24, 3, 24, 8);
            tabSettingCategories.Name = "tabSettingCategories";
            tabSettingCategories.SelectedIndex = 0;
            tabSettingCategories.Size = new Size(1471, 716);
            tabSettingCategories.TabIndex = 1;
            // 
            // tabBasicSettings
            // 
            tabBasicSettings.Controls.Add(grpMasterConfig);
            tabBasicSettings.Controls.Add(grpPlcConfig);
            tabBasicSettings.Controls.Add(groupBox1);
            tabBasicSettings.Controls.Add(grpMesConfig);
            tabBasicSettings.Controls.Add(grpAppConfig);
            tabBasicSettings.Location = new Point(4, 29);
            tabBasicSettings.Name = "tabBasicSettings";
            tabBasicSettings.Padding = new Padding(3);
            tabBasicSettings.Size = new Size(1463, 683);
            tabBasicSettings.TabIndex = 0;
            tabBasicSettings.Text = "基础设置";
            tabBasicSettings.UseVisualStyleBackColor = true;
            // 
            // grpMasterConfig
            // 
            grpMasterConfig.Controls.Add(layoutMasterConfig);
            grpMasterConfig.Location = new Point(6, 149);
            grpMasterConfig.Margin = new Padding(0);
            grpMasterConfig.Name = "grpMasterConfig";
            grpMasterConfig.Size = new Size(297, 106);
            grpMasterConfig.TabIndex = 2;
            grpMasterConfig.TabStop = false;
            grpMasterConfig.Text = "总控配置";
            // 
            // layoutMasterConfig
            // 
            layoutMasterConfig.ColumnCount = 1;
            layoutMasterConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutMasterConfig.Controls.Add(tlpMasterIp, 0, 0);
            layoutMasterConfig.Controls.Add(tableLayoutPanel8, 0, 1);
            layoutMasterConfig.Dock = DockStyle.Fill;
            layoutMasterConfig.Location = new Point(3, 23);
            layoutMasterConfig.Name = "layoutMasterConfig";
            layoutMasterConfig.RowCount = 2;
            layoutMasterConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layoutMasterConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layoutMasterConfig.Size = new Size(291, 80);
            layoutMasterConfig.TabIndex = 0;
            // 
            // tlpMasterIp
            // 
            tlpMasterIp.ColumnCount = 3;
            tlpMasterIp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38F));
            tlpMasterIp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMasterIp.ColumnStyles.Add(new ColumnStyle());
            tlpMasterIp.Controls.Add(lblMasterIp, 0, 0);
            tlpMasterIp.Controls.Add(input_MasterIp, 1, 0);
            tlpMasterIp.Controls.Add(btnConnectMasterController, 2, 0);
            tlpMasterIp.Dock = DockStyle.Fill;
            tlpMasterIp.Location = new Point(0, 0);
            tlpMasterIp.Margin = new Padding(0);
            tlpMasterIp.Name = "tlpMasterIp";
            tlpMasterIp.RowCount = 1;
            tlpMasterIp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMasterIp.Size = new Size(291, 40);
            tlpMasterIp.TabIndex = 0;
            // 
            // lblMasterIp
            // 
            lblMasterIp.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMasterIp.Dock = DockStyle.Right;
            lblMasterIp.Location = new Point(25, 0);
            lblMasterIp.Margin = new Padding(0);
            lblMasterIp.Name = "lblMasterIp";
            lblMasterIp.Size = new Size(13, 40);
            lblMasterIp.TabIndex = 0;
            lblMasterIp.Text = "IP";
            // 
            // input_MasterIp
            // 
            input_MasterIp.Dock = DockStyle.Fill;
            input_MasterIp.Location = new Point(38, 0);
            input_MasterIp.Margin = new Padding(0);
            input_MasterIp.Name = "input_MasterIp";
            input_MasterIp.Size = new Size(172, 40);
            input_MasterIp.TabIndex = 1;
            // 
            // btnConnectMasterController
            // 
            btnConnectMasterController.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnConnectMasterController.BorderWidth = 1F;
            btnConnectMasterController.Dock = DockStyle.Fill;
            btnConnectMasterController.IconSvg = "ApiOutlined";
            btnConnectMasterController.Location = new Point(210, 0);
            btnConnectMasterController.Margin = new Padding(0);
            btnConnectMasterController.Name = "btnConnectMasterController";
            btnConnectMasterController.Size = new Size(81, 40);
            btnConnectMasterController.TabIndex = 2;
            btnConnectMasterController.Text = "连接";
            // 
            // tableLayoutPanel8
            // 
            tableLayoutPanel8.ColumnCount = 2;
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel8.Controls.Add(lblMasterPort, 0, 0);
            tableLayoutPanel8.Controls.Add(input_MasterPort, 1, 0);
            tableLayoutPanel8.Dock = DockStyle.Fill;
            tableLayoutPanel8.Location = new Point(0, 40);
            tableLayoutPanel8.Margin = new Padding(0);
            tableLayoutPanel8.Name = "tableLayoutPanel8";
            tableLayoutPanel8.RowCount = 1;
            tableLayoutPanel8.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel8.Size = new Size(291, 40);
            tableLayoutPanel8.TabIndex = 1;
            // 
            // lblMasterPort
            // 
            lblMasterPort.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMasterPort.Dock = DockStyle.Fill;
            lblMasterPort.Location = new Point(0, 0);
            lblMasterPort.Margin = new Padding(0);
            lblMasterPort.Name = "lblMasterPort";
            lblMasterPort.Size = new Size(32, 40);
            lblMasterPort.TabIndex = 0;
            lblMasterPort.Text = "端口";
            // 
            // input_MasterPort
            // 
            input_MasterPort.Dock = DockStyle.Fill;
            input_MasterPort.Location = new Point(32, 0);
            input_MasterPort.Margin = new Padding(0);
            input_MasterPort.Name = "input_MasterPort";
            input_MasterPort.Size = new Size(259, 40);
            input_MasterPort.TabIndex = 1;
            // 
            // grpPlcConfig
            // 
            grpPlcConfig.Controls.Add(tlpPlcConfig);
            grpPlcConfig.Location = new Point(6, 6);
            grpPlcConfig.Margin = new Padding(0);
            grpPlcConfig.Name = "grpPlcConfig";
            grpPlcConfig.Size = new Size(297, 140);
            grpPlcConfig.TabIndex = 1;
            grpPlcConfig.TabStop = false;
            grpPlcConfig.Text = "PLC配置";
            // 
            // tlpPlcConfig
            // 
            tlpPlcConfig.ColumnCount = 1;
            tlpPlcConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpPlcConfig.Controls.Add(tlpPlcIp, 0, 0);
            tlpPlcConfig.Controls.Add(tlpPlcPort, 0, 1);
            tlpPlcConfig.Controls.Add(tableLayoutPanel7, 0, 2);
            tlpPlcConfig.Dock = DockStyle.Fill;
            tlpPlcConfig.Location = new Point(3, 23);
            tlpPlcConfig.Name = "tlpPlcConfig";
            tlpPlcConfig.RowCount = 3;
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333359F));
            tlpPlcConfig.Size = new Size(291, 114);
            tlpPlcConfig.TabIndex = 0;
            // 
            // tlpPlcIp
            // 
            tlpPlcIp.ColumnCount = 3;
            tlpPlcIp.ColumnStyles.Add(new ColumnStyle());
            tlpPlcIp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpPlcIp.ColumnStyles.Add(new ColumnStyle());
            tlpPlcIp.Controls.Add(lblPlcIp, 0, 0);
            tlpPlcIp.Controls.Add(input_PlcIp, 1, 0);
            tlpPlcIp.Controls.Add(btnConnectPlc, 2, 0);
            tlpPlcIp.Dock = DockStyle.Fill;
            tlpPlcIp.Location = new Point(0, 0);
            tlpPlcIp.Margin = new Padding(0);
            tlpPlcIp.Name = "tlpPlcIp";
            tlpPlcIp.RowCount = 1;
            tlpPlcIp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpPlcIp.Size = new Size(291, 37);
            tlpPlcIp.TabIndex = 0;
            // 
            // lblPlcIp
            // 
            lblPlcIp.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcIp.Dock = DockStyle.Fill;
            lblPlcIp.Location = new Point(0, 0);
            lblPlcIp.Margin = new Padding(0);
            lblPlcIp.Name = "lblPlcIp";
            lblPlcIp.Size = new Size(13, 37);
            lblPlcIp.TabIndex = 0;
            lblPlcIp.Text = "IP";
            // 
            // input_PlcIp
            // 
            input_PlcIp.Dock = DockStyle.Fill;
            input_PlcIp.Location = new Point(13, 0);
            input_PlcIp.Margin = new Padding(0);
            input_PlcIp.Name = "input_PlcIp";
            input_PlcIp.Size = new Size(197, 37);
            input_PlcIp.TabIndex = 1;
            // 
            // btnConnectPlc
            // 
            btnConnectPlc.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnConnectPlc.BorderWidth = 1F;
            btnConnectPlc.Dock = DockStyle.Fill;
            btnConnectPlc.IconSvg = "ApiOutlined";
            btnConnectPlc.Location = new Point(210, 0);
            btnConnectPlc.Margin = new Padding(0);
            btnConnectPlc.Name = "btnConnectPlc";
            btnConnectPlc.Size = new Size(81, 37);
            btnConnectPlc.TabIndex = 2;
            btnConnectPlc.Text = "连接";
            // 
            // tlpPlcPort
            // 
            tlpPlcPort.ColumnCount = 2;
            tlpPlcPort.ColumnStyles.Add(new ColumnStyle());
            tlpPlcPort.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpPlcPort.Controls.Add(lblPlcPort, 0, 0);
            tlpPlcPort.Controls.Add(input_PlcPort, 1, 0);
            tlpPlcPort.Dock = DockStyle.Fill;
            tlpPlcPort.Location = new Point(0, 37);
            tlpPlcPort.Margin = new Padding(0);
            tlpPlcPort.Name = "tlpPlcPort";
            tlpPlcPort.RowCount = 1;
            tlpPlcPort.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpPlcPort.Size = new Size(291, 37);
            tlpPlcPort.TabIndex = 1;
            // 
            // lblPlcPort
            // 
            lblPlcPort.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcPort.Dock = DockStyle.Fill;
            lblPlcPort.Location = new Point(0, 0);
            lblPlcPort.Margin = new Padding(0);
            lblPlcPort.Name = "lblPlcPort";
            lblPlcPort.Size = new Size(32, 37);
            lblPlcPort.TabIndex = 0;
            lblPlcPort.Text = "端口";
            // 
            // input_PlcPort
            // 
            input_PlcPort.Dock = DockStyle.Fill;
            input_PlcPort.Location = new Point(32, 0);
            input_PlcPort.Margin = new Padding(0);
            input_PlcPort.Name = "input_PlcPort";
            input_PlcPort.Size = new Size(259, 37);
            input_PlcPort.TabIndex = 1;
            // 
            // tableLayoutPanel7
            // 
            tableLayoutPanel7.ColumnCount = 2;
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel7.Controls.Add(lblPlcType, 0, 0);
            tableLayoutPanel7.Controls.Add(select_PlcType, 1, 0);
            tableLayoutPanel7.Dock = DockStyle.Fill;
            tableLayoutPanel7.Location = new Point(0, 74);
            tableLayoutPanel7.Margin = new Padding(0);
            tableLayoutPanel7.Name = "tableLayoutPanel7";
            tableLayoutPanel7.RowCount = 1;
            tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel7.Size = new Size(291, 40);
            tableLayoutPanel7.TabIndex = 2;
            // 
            // lblPlcType
            // 
            lblPlcType.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcType.Dock = DockStyle.Fill;
            lblPlcType.Location = new Point(0, 0);
            lblPlcType.Margin = new Padding(0);
            lblPlcType.Name = "lblPlcType";
            lblPlcType.Size = new Size(32, 40);
            lblPlcType.TabIndex = 0;
            lblPlcType.Text = "类型";
            // 
            // select_PlcType
            // 
            select_PlcType.Dock = DockStyle.Fill;
            select_PlcType.Location = new Point(32, 0);
            select_PlcType.Margin = new Padding(0);
            select_PlcType.Name = "select_PlcType";
            select_PlcType.Size = new Size(259, 40);
            select_PlcType.TabIndex = 1;
            // 
            // grpErrorTips
            // 
            groupBox1.Controls.Add(tableLayoutPanel5);
            groupBox1.Location = new Point(919, 6);
            groupBox1.Name = "grpErrorTips";
            groupBox1.Size = new Size(538, 73);
            groupBox1.TabIndex = 4;
            groupBox1.TabStop = false;
            groupBox1.Text = "生产配置";
            // 
            // tableLayoutPanel5
            // 
            tableLayoutPanel5.ColumnCount = 2;
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel5.Controls.Add(chkEnableDualStationMode, 0, 0);
            tableLayoutPanel5.Controls.Add(chkValidateRecipeBeforeStart, 1, 0);
            tableLayoutPanel5.Dock = DockStyle.Fill;
            tableLayoutPanel5.Location = new Point(3, 23);
            tableLayoutPanel5.Name = "tableLayoutPanel5";
            tableLayoutPanel5.RowCount = 1;
            tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel5.Size = new Size(532, 47);
            tableLayoutPanel5.TabIndex = 0;
            // 
            // chkEnableDualStationMode
            // 
            chkEnableDualStationMode.Dock = DockStyle.Fill;
            chkEnableDualStationMode.Location = new Point(0, 0);
            chkEnableDualStationMode.Margin = new Padding(0);
            chkEnableDualStationMode.Name = "chkEnableDualStationMode";
            chkEnableDualStationMode.Size = new Size(266, 47);
            chkEnableDualStationMode.TabIndex = 0;
            chkEnableDualStationMode.Text = "启用双工位";
            // 
            // chkValidateRecipeBeforeStart
            // 
            chkValidateRecipeBeforeStart.Dock = DockStyle.Fill;
            chkValidateRecipeBeforeStart.Location = new Point(266, 0);
            chkValidateRecipeBeforeStart.Margin = new Padding(0);
            chkValidateRecipeBeforeStart.Name = "chkValidateRecipeBeforeStart";
            chkValidateRecipeBeforeStart.Size = new Size(266, 47);
            chkValidateRecipeBeforeStart.TabIndex = 1;
            chkValidateRecipeBeforeStart.Text = "开工前校验配方";
            // 
            // grpMesConfig
            // 
            grpMesConfig.Controls.Add(tableLayoutPanelMesConfig);
            grpMesConfig.Location = new Point(919, 85);
            grpMesConfig.Name = "grpMesConfig";
            grpMesConfig.Size = new Size(538, 73);
            grpMesConfig.TabIndex = 3;
            grpMesConfig.TabStop = false;
            grpMesConfig.Text = "MES配置";
            // 
            // tableLayoutPanelMesConfig
            // 
            tableLayoutPanelMesConfig.ColumnCount = 1;
            tableLayoutPanelMesConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelMesConfig.Controls.Add(chkUseProductNumberFilter, 0, 0);
            tableLayoutPanelMesConfig.Dock = DockStyle.Fill;
            tableLayoutPanelMesConfig.Location = new Point(3, 23);
            tableLayoutPanelMesConfig.Name = "tableLayoutPanelMesConfig";
            tableLayoutPanelMesConfig.RowCount = 1;
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelMesConfig.Size = new Size(532, 47);
            tableLayoutPanelMesConfig.TabIndex = 0;
            // 
            // chkUseProductNumberFilter
            // 
            chkUseProductNumberFilter.Dock = DockStyle.Fill;
            chkUseProductNumberFilter.Location = new Point(0, 0);
            chkUseProductNumberFilter.Margin = new Padding(0);
            chkUseProductNumberFilter.Name = "chkUseProductNumberFilter";
            chkUseProductNumberFilter.Size = new Size(532, 47);
            chkUseProductNumberFilter.TabIndex = 0;
            chkUseProductNumberFilter.Text = "使用产品编号过滤";
            // 
            // grpAppConfig
            // 
            grpAppConfig.Controls.Add(layoutAppSettings);
            grpAppConfig.Location = new Point(312, 6);
            grpAppConfig.Margin = new Padding(0);
            grpAppConfig.Name = "grpAppConfig";
            grpAppConfig.Size = new Size(601, 249);
            grpAppConfig.TabIndex = 0;
            grpAppConfig.TabStop = false;
            grpAppConfig.Text = "应用配置";
            // 
            // layoutAppSettings
            // 
            layoutAppSettings.AutoSize = true;
            layoutAppSettings.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layoutAppSettings.ColumnCount = 1;
            layoutAppSettings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutAppSettings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            layoutAppSettings.Controls.Add(tlpDeviceId, 0, 0);
            layoutAppSettings.Controls.Add(tlpDeviceName, 0, 1);
            layoutAppSettings.Controls.Add(tlpLogPath, 0, 2);
            layoutAppSettings.Controls.Add(tlpDeviveUrl, 0, 5);
            layoutAppSettings.Controls.Add(tlpMesUrl, 0, 4);
            layoutAppSettings.Controls.Add(tlpDataPath, 0, 3);
            layoutAppSettings.Dock = DockStyle.Fill;
            layoutAppSettings.Location = new Point(3, 23);
            layoutAppSettings.Name = "layoutAppSettings";
            layoutAppSettings.RowCount = 6;
            layoutAppSettings.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666679F));
            layoutAppSettings.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            layoutAppSettings.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            layoutAppSettings.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            layoutAppSettings.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            layoutAppSettings.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            layoutAppSettings.Size = new Size(595, 223);
            layoutAppSettings.TabIndex = 0;
            // 
            // tlpDeviceId
            // 
            tlpDeviceId.ColumnCount = 3;
            tlpDeviceId.ColumnStyles.Add(new ColumnStyle());
            tlpDeviceId.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpDeviceId.ColumnStyles.Add(new ColumnStyle());
            tlpDeviceId.Controls.Add(lblDeviceId, 0, 0);
            tlpDeviceId.Controls.Add(input_DeviceID, 1, 0);
            tlpDeviceId.Controls.Add(btnSyncDevice, 2, 0);
            tlpDeviceId.Dock = DockStyle.Fill;
            tlpDeviceId.Location = new Point(0, 0);
            tlpDeviceId.Margin = new Padding(0);
            tlpDeviceId.Name = "tlpDeviceId";
            tlpDeviceId.RowCount = 1;
            tlpDeviceId.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpDeviceId.Size = new Size(595, 37);
            tlpDeviceId.TabIndex = 0;
            // 
            // lblDeviceId
            // 
            lblDeviceId.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceId.Dock = DockStyle.Fill;
            lblDeviceId.Location = new Point(0, 0);
            lblDeviceId.Margin = new Padding(0);
            lblDeviceId.Name = "lblDeviceId";
            lblDeviceId.Size = new Size(63, 37);
            lblDeviceId.TabIndex = 0;
            lblDeviceId.Text = "设备编号";
            // 
            // input_DeviceID
            // 
            input_DeviceID.Dock = DockStyle.Fill;
            input_DeviceID.Location = new Point(63, 0);
            input_DeviceID.Margin = new Padding(0);
            input_DeviceID.Name = "input_DeviceID";
            input_DeviceID.Size = new Size(451, 37);
            input_DeviceID.TabIndex = 1;
            // 
            // btnSyncDevice
            // 
            btnSyncDevice.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSyncDevice.BorderWidth = 1F;
            btnSyncDevice.Dock = DockStyle.Fill;
            btnSyncDevice.IconSvg = "CloudUploadOutlined";
            btnSyncDevice.Location = new Point(514, 0);
            btnSyncDevice.Margin = new Padding(0);
            btnSyncDevice.Name = "btnSyncDevice";
            btnSyncDevice.Size = new Size(81, 37);
            btnSyncDevice.TabIndex = 2;
            btnSyncDevice.Text = "同步";
            // 
            // tlpDeviceName
            // 
            tlpDeviceName.ColumnCount = 2;
            tlpDeviceName.ColumnStyles.Add(new ColumnStyle());
            tlpDeviceName.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpDeviceName.Controls.Add(lblDeviceName, 0, 0);
            tlpDeviceName.Controls.Add(input_DeviceName, 1, 0);
            tlpDeviceName.Dock = DockStyle.Fill;
            tlpDeviceName.Location = new Point(0, 37);
            tlpDeviceName.Margin = new Padding(0);
            tlpDeviceName.Name = "tlpDeviceName";
            tlpDeviceName.RowCount = 1;
            tlpDeviceName.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpDeviceName.Size = new Size(595, 37);
            tlpDeviceName.TabIndex = 1;
            // 
            // lblDeviceName
            // 
            lblDeviceName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceName.Dock = DockStyle.Fill;
            lblDeviceName.Location = new Point(0, 0);
            lblDeviceName.Margin = new Padding(0);
            lblDeviceName.Name = "lblDeviceName";
            lblDeviceName.Size = new Size(63, 37);
            lblDeviceName.TabIndex = 0;
            lblDeviceName.Text = "设备名称";
            // 
            // input_DeviceName
            // 
            input_DeviceName.Dock = DockStyle.Fill;
            input_DeviceName.Location = new Point(63, 0);
            input_DeviceName.Margin = new Padding(0);
            input_DeviceName.Name = "input_DeviceName";
            input_DeviceName.Size = new Size(532, 37);
            input_DeviceName.TabIndex = 1;
            // 
            // tlpLogPath
            // 
            tlpLogPath.ColumnCount = 4;
            tlpLogPath.ColumnStyles.Add(new ColumnStyle());
            tlpLogPath.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpLogPath.ColumnStyles.Add(new ColumnStyle());
            tlpLogPath.ColumnStyles.Add(new ColumnStyle());
            tlpLogPath.Controls.Add(lblLogPath, 0, 0);
            tlpLogPath.Controls.Add(input_LogsPath, 1, 0);
            tlpLogPath.Controls.Add(btnChangeLogPath, 2, 0);
            tlpLogPath.Controls.Add(btnOpenLogPath, 3, 0);
            tlpLogPath.Dock = DockStyle.Fill;
            tlpLogPath.Location = new Point(0, 74);
            tlpLogPath.Margin = new Padding(0);
            tlpLogPath.Name = "tlpLogPath";
            tlpLogPath.RowCount = 1;
            tlpLogPath.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpLogPath.Size = new Size(595, 37);
            tlpLogPath.TabIndex = 4;
            // 
            // lblLogPath
            // 
            lblLogPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblLogPath.Dock = DockStyle.Fill;
            lblLogPath.Location = new Point(0, 0);
            lblLogPath.Margin = new Padding(0);
            lblLogPath.Name = "lblLogPath";
            lblLogPath.Size = new Size(63, 37);
            lblLogPath.TabIndex = 0;
            lblLogPath.Text = "日志目录";
            // 
            // input_LogsPath
            // 
            input_LogsPath.Dock = DockStyle.Fill;
            input_LogsPath.Location = new Point(63, 0);
            input_LogsPath.Margin = new Padding(0);
            input_LogsPath.Name = "input_LogsPath";
            input_LogsPath.Size = new Size(370, 37);
            input_LogsPath.TabIndex = 1;
            // 
            // btnChangeLogPath
            // 
            btnChangeLogPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnChangeLogPath.BorderWidth = 1F;
            btnChangeLogPath.Dock = DockStyle.Fill;
            btnChangeLogPath.IconSvg = "FolderOpenOutlined";
            btnChangeLogPath.Location = new Point(433, 0);
            btnChangeLogPath.Margin = new Padding(0);
            btnChangeLogPath.Name = "btnChangeLogPath";
            btnChangeLogPath.Size = new Size(81, 37);
            btnChangeLogPath.TabIndex = 2;
            btnChangeLogPath.Text = "更改";
            // 
            // btnOpenLogPath
            // 
            btnOpenLogPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnOpenLogPath.BorderWidth = 1F;
            btnOpenLogPath.Dock = DockStyle.Fill;
            btnOpenLogPath.IconSvg = "FolderOutlined";
            btnOpenLogPath.Location = new Point(514, 0);
            btnOpenLogPath.Margin = new Padding(0);
            btnOpenLogPath.Name = "btnOpenLogPath";
            btnOpenLogPath.Size = new Size(81, 37);
            btnOpenLogPath.TabIndex = 3;
            btnOpenLogPath.Text = "打开";
            // 
            // tlpDeviveUrl
            // 
            tlpDeviveUrl.ColumnCount = 2;
            tlpDeviveUrl.ColumnStyles.Add(new ColumnStyle());
            tlpDeviveUrl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpDeviveUrl.Controls.Add(lblDeviceUrl, 0, 0);
            tlpDeviveUrl.Controls.Add(input_DeviceUrl, 1, 0);
            tlpDeviveUrl.Dock = DockStyle.Fill;
            tlpDeviveUrl.Location = new Point(0, 185);
            tlpDeviveUrl.Margin = new Padding(0);
            tlpDeviveUrl.Name = "tlpDeviveUrl";
            tlpDeviveUrl.RowCount = 1;
            tlpDeviveUrl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpDeviveUrl.Size = new Size(595, 38);
            tlpDeviveUrl.TabIndex = 2;
            // 
            // lblDeviceUrl
            // 
            lblDeviceUrl.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceUrl.Dock = DockStyle.Fill;
            lblDeviceUrl.Location = new Point(0, 0);
            lblDeviceUrl.Margin = new Padding(0);
            lblDeviceUrl.Name = "lblDeviceUrl";
            lblDeviceUrl.Size = new Size(63, 38);
            lblDeviceUrl.TabIndex = 0;
            lblDeviceUrl.Text = "状态地址";
            // 
            // input_DeviceUrl
            // 
            input_DeviceUrl.Dock = DockStyle.Fill;
            input_DeviceUrl.Location = new Point(63, 0);
            input_DeviceUrl.Margin = new Padding(0);
            input_DeviceUrl.Name = "input_DeviceUrl";
            input_DeviceUrl.Size = new Size(532, 38);
            input_DeviceUrl.TabIndex = 1;
            // 
            // tlpMesUrl
            // 
            tlpMesUrl.ColumnCount = 3;
            tlpMesUrl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 61F));
            tlpMesUrl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesUrl.ColumnStyles.Add(new ColumnStyle());
            tlpMesUrl.Controls.Add(lblMesUrl, 0, 0);
            tlpMesUrl.Controls.Add(input_BaseUrl, 1, 0);
            tlpMesUrl.Controls.Add(btnTestConnection, 2, 0);
            tlpMesUrl.Dock = DockStyle.Fill;
            tlpMesUrl.Location = new Point(0, 148);
            tlpMesUrl.Margin = new Padding(0);
            tlpMesUrl.Name = "tlpMesUrl";
            tlpMesUrl.RowCount = 1;
            tlpMesUrl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesUrl.Size = new Size(595, 37);
            tlpMesUrl.TabIndex = 3;
            // 
            // lblMesUrl
            // 
            lblMesUrl.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesUrl.Dock = DockStyle.Fill;
            lblMesUrl.Location = new Point(0, 0);
            lblMesUrl.Margin = new Padding(0);
            lblMesUrl.Name = "lblMesUrl";
            lblMesUrl.Size = new Size(61, 37);
            lblMesUrl.TabIndex = 0;
            lblMesUrl.Text = "MES地址";
            // 
            // input_BaseUrl
            // 
            input_BaseUrl.Dock = DockStyle.Fill;
            input_BaseUrl.Location = new Point(61, 0);
            input_BaseUrl.Margin = new Padding(0);
            input_BaseUrl.Name = "input_BaseUrl";
            input_BaseUrl.Size = new Size(453, 37);
            input_BaseUrl.TabIndex = 1;
            // 
            // btnTestConnection
            // 
            btnTestConnection.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnTestConnection.BorderWidth = 1F;
            btnTestConnection.Dock = DockStyle.Fill;
            btnTestConnection.IconSvg = "ApiOutlined";
            btnTestConnection.Location = new Point(514, 0);
            btnTestConnection.Margin = new Padding(0);
            btnTestConnection.Name = "btnTestConnection";
            btnTestConnection.Size = new Size(81, 37);
            btnTestConnection.TabIndex = 2;
            btnTestConnection.Text = "测试";
            // 
            // tlpDataPath
            // 
            tlpDataPath.ColumnCount = 4;
            tlpDataPath.ColumnStyles.Add(new ColumnStyle());
            tlpDataPath.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpDataPath.ColumnStyles.Add(new ColumnStyle());
            tlpDataPath.ColumnStyles.Add(new ColumnStyle());
            tlpDataPath.Controls.Add(lblDataPath, 0, 0);
            tlpDataPath.Controls.Add(input_DataPath, 1, 0);
            tlpDataPath.Controls.Add(btnChangeDataPath, 2, 0);
            tlpDataPath.Controls.Add(btnOpenDataPath, 3, 0);
            tlpDataPath.Dock = DockStyle.Fill;
            tlpDataPath.Location = new Point(0, 111);
            tlpDataPath.Margin = new Padding(0);
            tlpDataPath.Name = "tlpDataPath";
            tlpDataPath.RowCount = 1;
            tlpDataPath.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpDataPath.Size = new Size(595, 37);
            tlpDataPath.TabIndex = 5;
            // 
            // lblDataPath
            // 
            lblDataPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDataPath.Dock = DockStyle.Fill;
            lblDataPath.Location = new Point(0, 0);
            lblDataPath.Margin = new Padding(0);
            lblDataPath.Name = "lblDataPath";
            lblDataPath.Size = new Size(63, 37);
            lblDataPath.TabIndex = 0;
            lblDataPath.Text = "数据目录";
            // 
            // input_DataPath
            // 
            input_DataPath.Dock = DockStyle.Fill;
            input_DataPath.Location = new Point(63, 0);
            input_DataPath.Margin = new Padding(0);
            input_DataPath.Name = "input_DataPath";
            input_DataPath.Size = new Size(370, 37);
            input_DataPath.TabIndex = 1;
            // 
            // btnChangeDataPath
            // 
            btnChangeDataPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnChangeDataPath.BorderWidth = 1F;
            btnChangeDataPath.Dock = DockStyle.Fill;
            btnChangeDataPath.IconSvg = "FolderOpenOutlined";
            btnChangeDataPath.Location = new Point(433, 0);
            btnChangeDataPath.Margin = new Padding(0);
            btnChangeDataPath.Name = "btnChangeDataPath";
            btnChangeDataPath.Size = new Size(81, 37);
            btnChangeDataPath.TabIndex = 2;
            btnChangeDataPath.Text = "更改";
            // 
            // btnOpenDataPath
            // 
            btnOpenDataPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnOpenDataPath.BorderWidth = 1F;
            btnOpenDataPath.Dock = DockStyle.Fill;
            btnOpenDataPath.IconSvg = "FolderOutlined";
            btnOpenDataPath.Location = new Point(514, 0);
            btnOpenDataPath.Margin = new Padding(0);
            btnOpenDataPath.Name = "btnOpenDataPath";
            btnOpenDataPath.Size = new Size(81, 37);
            btnOpenDataPath.TabIndex = 3;
            btnOpenDataPath.Text = "打开";
            // 
            // SystemSettingView
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(rootLayout);
            Name = "SystemSettingView";
            Size = new Size(1519, 789);
            rootLayout.ResumeLayout(false);
            rootLayout.PerformLayout();
            titleLayout.ResumeLayout(false);
            titleLayout.PerformLayout();
            tabSettingCategories.ResumeLayout(false);
            tabBasicSettings.ResumeLayout(false);
            grpMasterConfig.ResumeLayout(false);
            layoutMasterConfig.ResumeLayout(false);
            tlpMasterIp.ResumeLayout(false);
            tlpMasterIp.PerformLayout();
            tableLayoutPanel8.ResumeLayout(false);
            tableLayoutPanel8.PerformLayout();
            grpPlcConfig.ResumeLayout(false);
            tlpPlcConfig.ResumeLayout(false);
            tlpPlcIp.ResumeLayout(false);
            tlpPlcIp.PerformLayout();
            tlpPlcPort.ResumeLayout(false);
            tlpPlcPort.PerformLayout();
            tableLayoutPanel7.ResumeLayout(false);
            tableLayoutPanel7.PerformLayout();
            groupBox1.ResumeLayout(false);
            tableLayoutPanel5.ResumeLayout(false);
            grpMesConfig.ResumeLayout(false);
            tableLayoutPanelMesConfig.ResumeLayout(false);
            grpAppConfig.ResumeLayout(false);
            grpAppConfig.PerformLayout();
            layoutAppSettings.ResumeLayout(false);
            tlpDeviceId.ResumeLayout(false);
            tlpDeviceId.PerformLayout();
            tlpDeviceName.ResumeLayout(false);
            tlpDeviceName.PerformLayout();
            tlpLogPath.ResumeLayout(false);
            tlpLogPath.PerformLayout();
            tlpDeviveUrl.ResumeLayout(false);
            tlpDeviveUrl.PerformLayout();
            tlpMesUrl.ResumeLayout(false);
            tlpMesUrl.PerformLayout();
            tlpDataPath.ResumeLayout(false);
            tlpDataPath.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel rootLayout;
        private GroupBox grpMasterConfig;
        private TableLayoutPanel layoutMasterConfig;
        private TableLayoutPanel tlpMasterIp;
        private AntdUI.Button btnConnectMasterController;
        private AntdUI.Label lblMasterIp;
        private AntdUI.Input input_MasterIp;
        private TableLayoutPanel tableLayoutPanel8;
        private AntdUI.Input input_MasterPort;
        private AntdUI.Label lblMasterPort;
        private GroupBox grpPlcConfig;
        private TableLayoutPanel tlpPlcConfig;
        private TableLayoutPanel tlpPlcIp;
        private AntdUI.Input input_PlcIp;
        private AntdUI.Button btnConnectPlc;
        private AntdUI.Label lblPlcIp;
        private TableLayoutPanel tableLayoutPanel7;
        private AntdUI.Select select_PlcType;
        private AntdUI.Label lblPlcType;
        private TableLayoutPanel tlpPlcPort;
        private AntdUI.Label lblPlcPort;
        private AntdUI.Input input_PlcPort;
        private GroupBox grpAppConfig;
        private TableLayoutPanel layoutAppSettings;
        private TableLayoutPanel tlpDeviveUrl;
        private AntdUI.Input input_DeviceUrl;
        private AntdUI.Label lblDeviceUrl;
        private TableLayoutPanel tlpDeviceId;
        private AntdUI.Button btnSyncDevice;
        private AntdUI.Input input_DeviceID;
        private AntdUI.Label lblDeviceId;
        private TableLayoutPanel tlpMesUrl;
        private AntdUI.Label lblMesUrl;
        private AntdUI.Input input_BaseUrl;
        private AntdUI.Button btnTestConnection;
        private TableLayoutPanel tlpDeviceName;
        private AntdUI.Label lblDeviceName;
        private AntdUI.Input input_DeviceName;
        private TableLayoutPanel tlpDataPath;
        private AntdUI.Label lblDataPath;
        private AntdUI.Button btnOpenDataPath;
        private AntdUI.Input input_DataPath;
        private AntdUI.Button btnChangeDataPath;
        private TableLayoutPanel tlpLogPath;
        private AntdUI.Label lblLogPath;
        private AntdUI.Button btnOpenLogPath;
        private AntdUI.Input input_LogsPath;
        private AntdUI.Button btnChangeLogPath;
        private AntdUI.Checkbox chkEnableDualStationMode;
        private AntdUI.Checkbox chkValidateRecipeBeforeStart;
        private AntdUI.Button btnSaveAll;
        private TableLayoutPanel titleLayout;
        private Label lblTitle;
        private Label lblDescription;
        private TabControl tabSettingCategories;
        private TabPage tabBasicSettings;
        private GroupBox groupBox1;
        private TableLayoutPanel tableLayoutPanel5;
        private GroupBox grpMesConfig;
        private TableLayoutPanel tableLayoutPanelMesConfig;
        private AntdUI.Checkbox chkUseProductNumberFilter;
    }
}
