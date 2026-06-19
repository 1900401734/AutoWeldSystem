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
            grpAppConfig = new GroupBox();
            tableLayoutPanel1 = new TableLayoutPanel();
            tlpLogPath = new TableLayoutPanel();
            lblLogPath = new AntdUI.Label();
            input_LogsPath = new AntdUI.Input();
            btnChangeLogPath = new AntdUI.Button();
            btnOpenLogPath = new AntdUI.Button();
            tlpDataPath = new TableLayoutPanel();
            lblDataPath = new AntdUI.Label();
            input_DataPath = new AntdUI.Input();
            btnChangeDataPath = new AntdUI.Button();
            btnOpenDataPath = new AntdUI.Button();
            chkEnableAutoStart = new AntdUI.Checkbox();
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
            grpProductionConfig = new GroupBox();
            tlpProductConfig = new TableLayoutPanel();
            chkEnableDualStation = new AntdUI.Checkbox();
            chkEnableDualWorkOrder = new AntdUI.Checkbox();
            tableLayoutPanel2 = new TableLayoutPanel();
            lblUploadMode = new AntdUI.Label();
            selectUploadMode = new AntdUI.Select();
            chkValidateRecipeBeforeStart = new AntdUI.Checkbox();
            chkEnableFinishExpQtyPrompt = new AntdUI.Checkbox();
            tableLayoutPanel3 = new TableLayoutPanel();
            lblUploadBatchSize = new AntdUI.Label();
            inputUploadBatchSize = new AntdUI.Input();
            tableLayoutPanelHeartbeat = new TableLayoutPanel();
            lblPlcHeartbeatInterval = new AntdUI.Label();
            inputPlcHeartbeatInterval = new AntdUI.Input();
            grpMesConfig = new GroupBox();
            tableLayoutPanelMesConfig = new TableLayoutPanel();
            chkUseProductNumberFilter = new AntdUI.Checkbox();
            tableLayoutPanel4 = new TableLayoutPanel();
            label1 = new AntdUI.Label();
            input_MesTimeout = new AntdUI.InputNumber();
            lblProcessParameterDeviceType = new AntdUI.Label();
            selectProcessParameterDeviceType = new AntdUI.Select();
            grpDeviceConfig = new GroupBox();
            layoutDeviceNoConfig = new TableLayoutPanel();
            tlpDeviceId = new TableLayoutPanel();
            lblDeviceId = new AntdUI.Label();
            input_DeviceID = new AntdUI.Input();
            btnSyncDevice = new AntdUI.Button();
            tlpDeviceName = new TableLayoutPanel();
            lblDeviceName = new AntdUI.Label();
            input_DeviceName = new AntdUI.Input();
            tlpDeviveUrl = new TableLayoutPanel();
            lblDeviceUrl = new AntdUI.Label();
            input_DeviceUrl = new AntdUI.Input();
            tlpMesUrl = new TableLayoutPanel();
            lblMesUrl = new AntdUI.Label();
            input_BaseUrl = new AntdUI.Input();
            btnTestConnection = new AntdUI.Button();
            rootLayout.SuspendLayout();
            titleLayout.SuspendLayout();
            tabSettingCategories.SuspendLayout();
            tabBasicSettings.SuspendLayout();
            grpAppConfig.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            tlpLogPath.SuspendLayout();
            tlpDataPath.SuspendLayout();
            grpMasterConfig.SuspendLayout();
            layoutMasterConfig.SuspendLayout();
            tlpMasterIp.SuspendLayout();
            tableLayoutPanel8.SuspendLayout();
            grpPlcConfig.SuspendLayout();
            tlpPlcConfig.SuspendLayout();
            tlpPlcIp.SuspendLayout();
            tlpPlcPort.SuspendLayout();
            tableLayoutPanel7.SuspendLayout();
            grpProductionConfig.SuspendLayout();
            tlpProductConfig.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            tableLayoutPanelHeartbeat.SuspendLayout();
            grpMesConfig.SuspendLayout();
            tableLayoutPanelMesConfig.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            grpDeviceConfig.SuspendLayout();
            layoutDeviceNoConfig.SuspendLayout();
            tlpDeviceId.SuspendLayout();
            tlpDeviceName.SuspendLayout();
            tlpDeviveUrl.SuspendLayout();
            tlpMesUrl.SuspendLayout();
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
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 9.632446F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 90.36755F));
            rootLayout.Size = new Size(1519, 789);
            rootLayout.TabIndex = 0;
            //
            // titleLayout
            //
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
            titleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            titleLayout.Size = new Size(1471, 65);
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
            lblDescription.Size = new Size(1359, 34);
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
            btnSaveAll.Size = new Size(112, 65);
            btnSaveAll.TabIndex = 0;
            btnSaveAll.Tag = "perm:button.system.save:enabled";
            btnSaveAll.Text = "应用全部";
            //
            // tabSettingCategories
            //
            tabSettingCategories.Controls.Add(tabBasicSettings);
            tabSettingCategories.Dock = DockStyle.Fill;
            tabSettingCategories.HotTrack = true;
            tabSettingCategories.Location = new Point(24, 79);
            tabSettingCategories.Margin = new Padding(24, 3, 24, 8);
            tabSettingCategories.Name = "tabSettingCategories";
            tabSettingCategories.SelectedIndex = 0;
            tabSettingCategories.Size = new Size(1471, 702);
            tabSettingCategories.TabIndex = 1;
            //
            // tabBasicSettings
            //
            tabBasicSettings.Controls.Add(grpAppConfig);
            tabBasicSettings.Controls.Add(grpMasterConfig);
            tabBasicSettings.Controls.Add(grpPlcConfig);
            tabBasicSettings.Controls.Add(grpProductionConfig);
            tabBasicSettings.Controls.Add(grpMesConfig);
            tabBasicSettings.Controls.Add(grpDeviceConfig);
            tabBasicSettings.Location = new Point(4, 29);
            tabBasicSettings.Name = "tabBasicSettings";
            tabBasicSettings.Padding = new Padding(3);
            tabBasicSettings.Size = new Size(1463, 669);
            tabBasicSettings.TabIndex = 0;
            tabBasicSettings.Text = "基础设置";
            tabBasicSettings.UseVisualStyleBackColor = true;
            //
            // grpAppConfig
            //
            grpAppConfig.Controls.Add(tableLayoutPanel1);
            grpAppConfig.Location = new Point(312, 209);
            grpAppConfig.Name = "grpAppConfig";
            grpAppConfig.Size = new Size(601, 161);
            grpAppConfig.TabIndex = 5;
            grpAppConfig.TabStop = false;
            grpAppConfig.Text = "应用配置";
            //
            // tableLayoutPanel1
            //
            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(tlpLogPath, 0, 0);
            tableLayoutPanel1.Controls.Add(tlpDataPath, 0, 1);
            tableLayoutPanel1.Controls.Add(chkEnableAutoStart, 0, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(3, 23);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanel1.Size = new Size(595, 135);
            tableLayoutPanel1.TabIndex = 6;
            //
            // tlpLogPath
            //
            tlpLogPath.AutoSize = true;
            tlpLogPath.AutoSizeMode = AutoSizeMode.GrowAndShrink;
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
            tlpLogPath.Location = new Point(0, 0);
            tlpLogPath.Margin = new Padding(0);
            tlpLogPath.Name = "tlpLogPath";
            tlpLogPath.RowCount = 1;
            tlpLogPath.RowStyles.Add(new RowStyle());
            tlpLogPath.Size = new Size(595, 45);
            tlpLogPath.TabIndex = 4;
            //
            // lblLogPath
            //
            lblLogPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblLogPath.Dock = DockStyle.Fill;
            lblLogPath.Location = new Point(0, 0);
            lblLogPath.Margin = new Padding(0);
            lblLogPath.Name = "lblLogPath";
            lblLogPath.Size = new Size(63, 45);
            lblLogPath.TabIndex = 0;
            lblLogPath.Text = "日志目录";
            //
            // input_LogsPath
            //
            input_LogsPath.Dock = DockStyle.Fill;
            input_LogsPath.Location = new Point(63, 0);
            input_LogsPath.Margin = new Padding(0);
            input_LogsPath.Name = "input_LogsPath";
            input_LogsPath.Size = new Size(370, 45);
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
            btnChangeLogPath.Size = new Size(81, 45);
            btnChangeLogPath.TabIndex = 2;
            btnChangeLogPath.Tag = "perm:button.system.change-path:enabled";
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
            btnOpenLogPath.Size = new Size(81, 45);
            btnOpenLogPath.TabIndex = 3;
            btnOpenLogPath.Tag = "perm:button.system.open-path:enabled";
            btnOpenLogPath.Text = "打开";
            //
            // tlpDataPath
            //
            tlpDataPath.AutoSize = true;
            tlpDataPath.AutoSizeMode = AutoSizeMode.GrowAndShrink;
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
            tlpDataPath.Location = new Point(0, 45);
            tlpDataPath.Margin = new Padding(0);
            tlpDataPath.Name = "tlpDataPath";
            tlpDataPath.RowCount = 1;
            tlpDataPath.RowStyles.Add(new RowStyle());
            tlpDataPath.Size = new Size(595, 45);
            tlpDataPath.TabIndex = 5;
            //
            // lblDataPath
            //
            lblDataPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDataPath.Dock = DockStyle.Fill;
            lblDataPath.Location = new Point(0, 0);
            lblDataPath.Margin = new Padding(0);
            lblDataPath.Name = "lblDataPath";
            lblDataPath.Size = new Size(63, 45);
            lblDataPath.TabIndex = 0;
            lblDataPath.Text = "数据目录";
            //
            // input_DataPath
            //
            input_DataPath.Dock = DockStyle.Fill;
            input_DataPath.Location = new Point(63, 0);
            input_DataPath.Margin = new Padding(0);
            input_DataPath.Name = "input_DataPath";
            input_DataPath.Size = new Size(370, 45);
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
            btnChangeDataPath.Size = new Size(81, 45);
            btnChangeDataPath.TabIndex = 2;
            btnChangeDataPath.Tag = "perm:button.system.change-path:enabled";
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
            btnOpenDataPath.Size = new Size(81, 45);
            btnOpenDataPath.TabIndex = 3;
            btnOpenDataPath.Tag = "perm:button.system.open-path:enabled";
            btnOpenDataPath.Text = "打开";
            //
            // chkEnableAutoStart
            //
            chkEnableAutoStart.Checked = true;
            chkEnableAutoStart.CheckState = CheckState.Checked;
            chkEnableAutoStart.Dock = DockStyle.Fill;
            chkEnableAutoStart.Location = new Point(0, 90);
            chkEnableAutoStart.Margin = new Padding(0);
            chkEnableAutoStart.Name = "chkEnableAutoStart";
            chkEnableAutoStart.Padding = new Padding(8, 0, 0, 0);
            chkEnableAutoStart.Size = new Size(595, 45);
            chkEnableAutoStart.TabIndex = 6;
            chkEnableAutoStart.Text = "开机自启";
            //
            // grpMasterConfig
            //
            grpMasterConfig.Controls.Add(layoutMasterConfig);
            grpMasterConfig.Location = new Point(6, 168);
            grpMasterConfig.Margin = new Padding(0);
            grpMasterConfig.Name = "grpMasterConfig";
            grpMasterConfig.Size = new Size(297, 115);
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
            layoutMasterConfig.Size = new Size(291, 89);
            layoutMasterConfig.TabIndex = 0;
            //
            // tlpMasterIp
            //
            tlpMasterIp.AutoSize = true;
            tlpMasterIp.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpMasterIp.ColumnCount = 3;
            tlpMasterIp.ColumnStyles.Add(new ColumnStyle());
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
            tlpMasterIp.RowStyles.Add(new RowStyle());
            tlpMasterIp.Size = new Size(291, 44);
            tlpMasterIp.TabIndex = 0;
            //
            // lblMasterIp
            //
            lblMasterIp.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMasterIp.Dock = DockStyle.Right;
            lblMasterIp.Location = new Point(0, 0);
            lblMasterIp.Margin = new Padding(0);
            lblMasterIp.Name = "lblMasterIp";
            lblMasterIp.Size = new Size(13, 45);
            lblMasterIp.TabIndex = 0;
            lblMasterIp.Text = "IP";
            //
            // input_MasterIp
            //
            input_MasterIp.Dock = DockStyle.Fill;
            input_MasterIp.Location = new Point(13, 0);
            input_MasterIp.Margin = new Padding(0);
            input_MasterIp.Name = "input_MasterIp";
            input_MasterIp.Size = new Size(197, 45);
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
            btnConnectMasterController.Size = new Size(81, 45);
            btnConnectMasterController.TabIndex = 2;
            btnConnectMasterController.Tag = "perm:button.system.connect-master:enabled";
            btnConnectMasterController.Text = "连接";
            //
            // tableLayoutPanel8
            //
            tableLayoutPanel8.AutoSize = true;
            tableLayoutPanel8.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel8.ColumnCount = 2;
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel8.Controls.Add(lblMasterPort, 0, 0);
            tableLayoutPanel8.Controls.Add(input_MasterPort, 1, 0);
            tableLayoutPanel8.Dock = DockStyle.Fill;
            tableLayoutPanel8.Location = new Point(0, 44);
            tableLayoutPanel8.Margin = new Padding(0);
            tableLayoutPanel8.Name = "tableLayoutPanel8";
            tableLayoutPanel8.RowCount = 1;
            tableLayoutPanel8.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel8.Size = new Size(291, 45);
            tableLayoutPanel8.TabIndex = 1;
            //
            // lblMasterPort
            //
            lblMasterPort.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMasterPort.Dock = DockStyle.Fill;
            lblMasterPort.Location = new Point(0, 0);
            lblMasterPort.Margin = new Padding(0);
            lblMasterPort.Name = "lblMasterPort";
            lblMasterPort.Size = new Size(32, 45);
            lblMasterPort.TabIndex = 0;
            lblMasterPort.Text = "端口";
            //
            // input_MasterPort
            //
            input_MasterPort.Dock = DockStyle.Fill;
            input_MasterPort.Location = new Point(32, 0);
            input_MasterPort.Margin = new Padding(0);
            input_MasterPort.Name = "input_MasterPort";
            input_MasterPort.Size = new Size(259, 45);
            input_MasterPort.TabIndex = 1;
            //
            // grpPlcConfig
            //
            grpPlcConfig.Controls.Add(tlpPlcConfig);
            grpPlcConfig.Location = new Point(6, 6);
            grpPlcConfig.Margin = new Padding(0);
            grpPlcConfig.Name = "grpPlcConfig";
            grpPlcConfig.Size = new Size(297, 162);
            grpPlcConfig.TabIndex = 1;
            grpPlcConfig.TabStop = false;
            grpPlcConfig.Text = "PLC配置";
            //
            // tlpPlcConfig
            //
            tlpPlcConfig.AutoSize = true;
            tlpPlcConfig.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpPlcConfig.ColumnCount = 1;
            tlpPlcConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpPlcConfig.Controls.Add(tlpPlcIp, 0, 0);
            tlpPlcConfig.Controls.Add(tlpPlcPort, 0, 1);
            tlpPlcConfig.Controls.Add(tableLayoutPanel7, 0, 2);
            tlpPlcConfig.Dock = DockStyle.Fill;
            tlpPlcConfig.Location = new Point(3, 23);
            tlpPlcConfig.Name = "tlpPlcConfig";
            tlpPlcConfig.RowCount = 3;
            tlpPlcConfig.RowStyles.Add(new RowStyle());
            tlpPlcConfig.RowStyles.Add(new RowStyle());
            tlpPlcConfig.RowStyles.Add(new RowStyle());
            tlpPlcConfig.Size = new Size(291, 136);
            tlpPlcConfig.TabIndex = 0;
            //
            // tlpPlcIp
            //
            tlpPlcIp.AutoSize = true;
            tlpPlcIp.AutoSizeMode = AutoSizeMode.GrowAndShrink;
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
            tlpPlcIp.RowStyles.Add(new RowStyle());
            tlpPlcIp.Size = new Size(291, 45);
            tlpPlcIp.TabIndex = 0;
            //
            // lblPlcIp
            //
            lblPlcIp.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcIp.Dock = DockStyle.Fill;
            lblPlcIp.Location = new Point(0, 0);
            lblPlcIp.Margin = new Padding(0);
            lblPlcIp.Name = "lblPlcIp";
            lblPlcIp.Size = new Size(13, 45);
            lblPlcIp.TabIndex = 0;
            lblPlcIp.Text = "IP";
            //
            // input_PlcIp
            //
            input_PlcIp.Dock = DockStyle.Fill;
            input_PlcIp.Location = new Point(13, 0);
            input_PlcIp.Margin = new Padding(0);
            input_PlcIp.Name = "input_PlcIp";
            input_PlcIp.Size = new Size(197, 45);
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
            btnConnectPlc.Size = new Size(81, 45);
            btnConnectPlc.TabIndex = 2;
            btnConnectPlc.Tag = "perm:button.system.connect-plc:enabled";
            btnConnectPlc.Text = "连接";
            //
            // tlpPlcPort
            //
            tlpPlcPort.AutoSize = true;
            tlpPlcPort.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpPlcPort.ColumnCount = 2;
            tlpPlcPort.ColumnStyles.Add(new ColumnStyle());
            tlpPlcPort.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpPlcPort.Controls.Add(lblPlcPort, 0, 0);
            tlpPlcPort.Controls.Add(input_PlcPort, 1, 0);
            tlpPlcPort.Dock = DockStyle.Fill;
            tlpPlcPort.Location = new Point(0, 45);
            tlpPlcPort.Margin = new Padding(0);
            tlpPlcPort.Name = "tlpPlcPort";
            tlpPlcPort.RowCount = 1;
            tlpPlcPort.RowStyles.Add(new RowStyle());
            tlpPlcPort.Size = new Size(291, 45);
            tlpPlcPort.TabIndex = 1;
            //
            // lblPlcPort
            //
            lblPlcPort.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcPort.Dock = DockStyle.Fill;
            lblPlcPort.Location = new Point(0, 0);
            lblPlcPort.Margin = new Padding(0);
            lblPlcPort.Name = "lblPlcPort";
            lblPlcPort.Size = new Size(32, 45);
            lblPlcPort.TabIndex = 0;
            lblPlcPort.Text = "端口";
            //
            // input_PlcPort
            //
            input_PlcPort.Dock = DockStyle.Fill;
            input_PlcPort.Location = new Point(32, 0);
            input_PlcPort.Margin = new Padding(0);
            input_PlcPort.Name = "input_PlcPort";
            input_PlcPort.Size = new Size(259, 45);
            input_PlcPort.TabIndex = 1;
            //
            // tableLayoutPanel7
            //
            tableLayoutPanel7.AutoSize = true;
            tableLayoutPanel7.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel7.ColumnCount = 2;
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel7.Controls.Add(lblPlcType, 0, 0);
            tableLayoutPanel7.Controls.Add(select_PlcType, 1, 0);
            tableLayoutPanel7.Dock = DockStyle.Fill;
            tableLayoutPanel7.Location = new Point(0, 90);
            tableLayoutPanel7.Margin = new Padding(0);
            tableLayoutPanel7.Name = "tableLayoutPanel7";
            tableLayoutPanel7.RowCount = 1;
            tableLayoutPanel7.RowStyles.Add(new RowStyle());
            tableLayoutPanel7.Size = new Size(291, 46);
            tableLayoutPanel7.TabIndex = 2;
            //
            // lblPlcType
            //
            lblPlcType.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcType.Dock = DockStyle.Fill;
            lblPlcType.Location = new Point(0, 0);
            lblPlcType.Margin = new Padding(0);
            lblPlcType.Name = "lblPlcType";
            lblPlcType.Size = new Size(32, 46);
            lblPlcType.TabIndex = 0;
            lblPlcType.Text = "类型";
            //
            // select_PlcType
            //
            select_PlcType.Dock = DockStyle.Fill;
            select_PlcType.Location = new Point(32, 0);
            select_PlcType.Margin = new Padding(0);
            select_PlcType.Name = "select_PlcType";
            select_PlcType.Size = new Size(259, 46);
            select_PlcType.TabIndex = 1;
            //
            // grpProductionConfig
            //
            grpProductionConfig.Controls.Add(tlpProductConfig);
            grpProductionConfig.Location = new Point(919, 6);
            grpProductionConfig.Name = "grpProductionConfig";
            grpProductionConfig.Size = new Size(538, 164);
            grpProductionConfig.TabIndex = 4;
            grpProductionConfig.TabStop = false;
            grpProductionConfig.Text = "生产配置";
            //
            // tlpProductConfig
            //
            tlpProductConfig.ColumnCount = 2;
            tlpProductConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpProductConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpProductConfig.Controls.Add(chkEnableDualStation, 0, 0);
            tlpProductConfig.Controls.Add(chkEnableDualWorkOrder, 1, 0);
            tlpProductConfig.Controls.Add(tableLayoutPanel2, 0, 2);
            tlpProductConfig.Controls.Add(chkValidateRecipeBeforeStart, 0, 1);
            tlpProductConfig.Controls.Add(chkEnableFinishExpQtyPrompt, 1, 1);
            tlpProductConfig.Controls.Add(tableLayoutPanel3, 1, 2);
            tlpProductConfig.Controls.Add(tableLayoutPanelHeartbeat, 0, 3);
            tlpProductConfig.Dock = DockStyle.Fill;
            tlpProductConfig.Location = new Point(3, 23);
            tlpProductConfig.Name = "tlpProductConfig";
            tlpProductConfig.RowCount = 4;
            tlpProductConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tlpProductConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tlpProductConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tlpProductConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tlpProductConfig.Size = new Size(532, 138);
            tlpProductConfig.TabIndex = 0;
            //
            // chkEnableDualStation
            //
            chkEnableDualStation.Dock = DockStyle.Fill;
            chkEnableDualStation.Location = new Point(0, 0);
            chkEnableDualStation.Margin = new Padding(0);
            chkEnableDualStation.Name = "chkEnableDualStation";
            chkEnableDualStation.Size = new Size(266, 34);
            chkEnableDualStation.TabIndex = 0;
            chkEnableDualStation.Text = "启用双工位";
            //
            // chkEnableDualWorkOrder
            //
            chkEnableDualWorkOrder.Dock = DockStyle.Fill;
            chkEnableDualWorkOrder.Location = new Point(266, 0);
            chkEnableDualWorkOrder.Margin = new Padding(0);
            chkEnableDualWorkOrder.Name = "chkEnableDualWorkOrder";
            chkEnableDualWorkOrder.Size = new Size(266, 34);
            chkEnableDualWorkOrder.TabIndex = 1;
            chkEnableDualWorkOrder.Text = "启用双工单";
            //
            // tableLayoutPanel2
            //
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(lblUploadMode, 0, 0);
            tableLayoutPanel2.Controls.Add(selectUploadMode, 1, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(0, 68);
            tableLayoutPanel2.Margin = new Padding(0);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(266, 34);
            tableLayoutPanel2.TabIndex = 6;
            //
            // lblUploadMode
            //
            lblUploadMode.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblUploadMode.Dock = DockStyle.Fill;
            lblUploadMode.Location = new Point(0, 0);
            lblUploadMode.Margin = new Padding(0);
            lblUploadMode.Name = "lblUploadMode";
            lblUploadMode.Padding = new Padding(8, 0, 0, 0);
            lblUploadMode.Size = new Size(71, 34);
            lblUploadMode.TabIndex = 3;
            lblUploadMode.Text = "上传模式";
            //
            // selectUploadMode
            //
            selectUploadMode.Dock = DockStyle.Fill;
            selectUploadMode.Location = new Point(71, 0);
            selectUploadMode.Margin = new Padding(0);
            selectUploadMode.Name = "selectUploadMode";
            selectUploadMode.Size = new Size(195, 34);
            selectUploadMode.TabIndex = 4;
            //
            // chkValidateRecipeBeforeStart
            //
            chkValidateRecipeBeforeStart.Dock = DockStyle.Fill;
            chkValidateRecipeBeforeStart.Location = new Point(0, 34);
            chkValidateRecipeBeforeStart.Margin = new Padding(0);
            chkValidateRecipeBeforeStart.Name = "chkValidateRecipeBeforeStart";
            chkValidateRecipeBeforeStart.Size = new Size(266, 34);
            chkValidateRecipeBeforeStart.TabIndex = 2;
            chkValidateRecipeBeforeStart.Text = "开工后校验配方";
            //
            // chkEnableFinishExpQtyPrompt
            //
            chkEnableFinishExpQtyPrompt.Dock = DockStyle.Fill;
            chkEnableFinishExpQtyPrompt.Location = new Point(266, 34);
            chkEnableFinishExpQtyPrompt.Margin = new Padding(0);
            chkEnableFinishExpQtyPrompt.Name = "chkEnableFinishExpQtyPrompt";
            chkEnableFinishExpQtyPrompt.Size = new Size(266, 34);
            chkEnableFinishExpQtyPrompt.TabIndex = 3;
            chkEnableFinishExpQtyPrompt.Text = "启用完工实际数量输入弹窗";
            //
            // tableLayoutPanel3
            //
            tableLayoutPanel3.ColumnCount = 2;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Controls.Add(lblUploadBatchSize, 0, 0);
            tableLayoutPanel3.Controls.Add(inputUploadBatchSize, 1, 0);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(266, 68);
            tableLayoutPanel3.Margin = new Padding(0);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 1;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Size = new Size(266, 34);
            tableLayoutPanel3.TabIndex = 6;
            //
            // lblUploadBatchSize
            //
            lblUploadBatchSize.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblUploadBatchSize.Dock = DockStyle.Fill;
            lblUploadBatchSize.Location = new Point(0, 0);
            lblUploadBatchSize.Margin = new Padding(0);
            lblUploadBatchSize.Name = "lblUploadBatchSize";
            lblUploadBatchSize.Padding = new Padding(8, 0, 0, 0);
            lblUploadBatchSize.Size = new Size(71, 34);
            lblUploadBatchSize.TabIndex = 5;
            lblUploadBatchSize.Text = "上传数量";
            //
            // inputUploadBatchSize
            //
            inputUploadBatchSize.Dock = DockStyle.Fill;
            inputUploadBatchSize.Location = new Point(71, 0);
            inputUploadBatchSize.Margin = new Padding(0);
            inputUploadBatchSize.Name = "inputUploadBatchSize";
            inputUploadBatchSize.Size = new Size(195, 34);
            inputUploadBatchSize.TabIndex = 6;
            inputUploadBatchSize.Text = "1";
            //
            // tableLayoutPanelHeartbeat
            //
            tableLayoutPanelHeartbeat.ColumnCount = 2;
            tlpProductConfig.SetColumnSpan(tableLayoutPanelHeartbeat, 2);
            tableLayoutPanelHeartbeat.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelHeartbeat.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelHeartbeat.Controls.Add(lblPlcHeartbeatInterval, 0, 0);
            tableLayoutPanelHeartbeat.Controls.Add(inputPlcHeartbeatInterval, 1, 0);
            tableLayoutPanelHeartbeat.Dock = DockStyle.Fill;
            tableLayoutPanelHeartbeat.Location = new Point(0, 102);
            tableLayoutPanelHeartbeat.Margin = new Padding(0);
            tableLayoutPanelHeartbeat.Name = "tableLayoutPanelHeartbeat";
            tableLayoutPanelHeartbeat.RowCount = 1;
            tableLayoutPanelHeartbeat.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelHeartbeat.Size = new Size(532, 36);
            tableLayoutPanelHeartbeat.TabIndex = 7;
            //
            // lblPlcHeartbeatInterval
            //
            lblPlcHeartbeatInterval.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcHeartbeatInterval.Dock = DockStyle.Fill;
            lblPlcHeartbeatInterval.Location = new Point(0, 0);
            lblPlcHeartbeatInterval.Margin = new Padding(0);
            lblPlcHeartbeatInterval.Name = "lblPlcHeartbeatInterval";
            lblPlcHeartbeatInterval.Padding = new Padding(8, 0, 0, 0);
            lblPlcHeartbeatInterval.Size = new Size(155, 36);
            lblPlcHeartbeatInterval.TabIndex = 0;
            lblPlcHeartbeatInterval.Text = "PLC心跳监测频率(ms)";
            //
            // inputPlcHeartbeatInterval
            //
            inputPlcHeartbeatInterval.Dock = DockStyle.Fill;
            inputPlcHeartbeatInterval.Location = new Point(155, 0);
            inputPlcHeartbeatInterval.Margin = new Padding(0);
            inputPlcHeartbeatInterval.Name = "inputPlcHeartbeatInterval";
            inputPlcHeartbeatInterval.Size = new Size(377, 36);
            inputPlcHeartbeatInterval.TabIndex = 1;
            inputPlcHeartbeatInterval.Text = "300";
            //
            // grpMesConfig
            //
            grpMesConfig.Controls.Add(tableLayoutPanelMesConfig);
            grpMesConfig.Location = new Point(919, 176);
            grpMesConfig.Name = "grpMesConfig";
            grpMesConfig.Size = new Size(538, 138);
            grpMesConfig.TabIndex = 3;
            grpMesConfig.TabStop = false;
            grpMesConfig.Text = "MES Config";
            //
            // tableLayoutPanelMesConfig
            //
            tableLayoutPanelMesConfig.ColumnCount = 2;
            tableLayoutPanelMesConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            tableLayoutPanelMesConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelMesConfig.Controls.Add(chkUseProductNumberFilter, 0, 0);
            tableLayoutPanelMesConfig.Controls.Add(tableLayoutPanel4, 0, 1);
            tableLayoutPanelMesConfig.Controls.Add(lblProcessParameterDeviceType, 0, 2);
            tableLayoutPanelMesConfig.Controls.Add(selectProcessParameterDeviceType, 1, 2);
            tableLayoutPanelMesConfig.Dock = DockStyle.Fill;
            tableLayoutPanelMesConfig.Location = new Point(3, 23);
            tableLayoutPanelMesConfig.Name = "tableLayoutPanelMesConfig";
            tableLayoutPanelMesConfig.RowCount = 3;
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tableLayoutPanelMesConfig.Size = new Size(532, 112);
            tableLayoutPanelMesConfig.TabIndex = 0;
            //
            // chkUseProductNumberFilter
            //
            tableLayoutPanelMesConfig.SetColumnSpan(chkUseProductNumberFilter, 2);
            chkUseProductNumberFilter.Dock = DockStyle.Fill;
            chkUseProductNumberFilter.Location = new Point(0, 0);
            chkUseProductNumberFilter.Margin = new Padding(0);
            chkUseProductNumberFilter.Name = "chkUseProductNumberFilter";
            chkUseProductNumberFilter.Size = new Size(532, 34);
            chkUseProductNumberFilter.TabIndex = 0;
            chkUseProductNumberFilter.Text = "Use product number filter";
            //
            // tableLayoutPanel4
            //
            tableLayoutPanel4.ColumnCount = 2;
            tableLayoutPanelMesConfig.SetColumnSpan(tableLayoutPanel4, 2);
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Controls.Add(label1, 0, 0);
            tableLayoutPanel4.Controls.Add(input_MesTimeout, 1, 0);
            tableLayoutPanel4.Dock = DockStyle.Fill;
            tableLayoutPanel4.Location = new Point(0, 34);
            tableLayoutPanel4.Margin = new Padding(0);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 1;
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Size = new Size(532, 34);
            tableLayoutPanel4.TabIndex = 1;
            //
            // label1
            //
            label1.AutoSizeMode = AntdUI.TAutoSize.Width;
            label1.Dock = DockStyle.Fill;
            label1.Location = new Point(0, 0);
            label1.Margin = new Padding(0);
            label1.Name = "label1";
            label1.Padding = new Padding(8, 0, 0, 0);
            label1.Size = new Size(112, 34);
            label1.TabIndex = 0;
            label1.Text = "MES Timeout(s)";
            //
            // input_MesTimeout
            //
            input_MesTimeout.Dock = DockStyle.Fill;
            input_MesTimeout.Location = new Point(112, 0);
            input_MesTimeout.Margin = new Padding(0);
            input_MesTimeout.Name = "input_MesTimeout";
            input_MesTimeout.Size = new Size(420, 34);
            input_MesTimeout.TabIndex = 1;
            input_MesTimeout.Text = "0";
            //
            // lblProcessParameterDeviceType
            //
            lblProcessParameterDeviceType.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProcessParameterDeviceType.Dock = DockStyle.Fill;
            lblProcessParameterDeviceType.Location = new Point(0, 68);
            lblProcessParameterDeviceType.Margin = new Padding(0);
            lblProcessParameterDeviceType.Name = "lblProcessParameterDeviceType";
            lblProcessParameterDeviceType.Padding = new Padding(8, 0, 0, 0);
            lblProcessParameterDeviceType.Size = new Size(164, 44);
            lblProcessParameterDeviceType.TabIndex = 2;
            lblProcessParameterDeviceType.Text = "Process parameter type";
            //
            // selectProcessParameterDeviceType
            //
            selectProcessParameterDeviceType.Dock = DockStyle.Fill;
            selectProcessParameterDeviceType.Location = new Point(190, 70);
            selectProcessParameterDeviceType.Margin = new Padding(0, 2, 0, 2);
            selectProcessParameterDeviceType.Name = "selectProcessParameterDeviceType";
            selectProcessParameterDeviceType.Size = new Size(342, 40);
            selectProcessParameterDeviceType.TabIndex = 3;
            //
            // grpDeviceConfig
            //
            grpDeviceConfig.Controls.Add(layoutDeviceNoConfig);
            grpDeviceConfig.Location = new Point(312, 6);
            grpDeviceConfig.Margin = new Padding(0);
            grpDeviceConfig.Name = "grpDeviceConfig";
            grpDeviceConfig.Size = new Size(601, 200);
            grpDeviceConfig.TabIndex = 0;
            grpDeviceConfig.TabStop = false;
            grpDeviceConfig.Text = "设备编号管理";
            //
            // layoutDeviceNoConfig
            //
            layoutDeviceNoConfig.ColumnCount = 1;
            layoutDeviceNoConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutDeviceNoConfig.Controls.Add(tlpDeviceId, 0, 0);
            layoutDeviceNoConfig.Controls.Add(tlpDeviceName, 0, 1);
            layoutDeviceNoConfig.Controls.Add(tlpDeviveUrl, 0, 3);
            layoutDeviceNoConfig.Controls.Add(tlpMesUrl, 0, 2);
            layoutDeviceNoConfig.Dock = DockStyle.Fill;
            layoutDeviceNoConfig.Location = new Point(3, 23);
            layoutDeviceNoConfig.Name = "layoutDeviceNoConfig";
            layoutDeviceNoConfig.RowCount = 4;
            layoutDeviceNoConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            layoutDeviceNoConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            layoutDeviceNoConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            layoutDeviceNoConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            layoutDeviceNoConfig.Size = new Size(595, 174);
            layoutDeviceNoConfig.TabIndex = 0;
            //
            // tlpDeviceId
            //
            tlpDeviceId.AutoSize = true;
            tlpDeviceId.AutoSizeMode = AutoSizeMode.GrowAndShrink;
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
            tlpDeviceId.RowStyles.Add(new RowStyle());
            tlpDeviceId.Size = new Size(595, 43);
            tlpDeviceId.TabIndex = 0;
            //
            // lblDeviceId
            //
            lblDeviceId.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceId.Dock = DockStyle.Fill;
            lblDeviceId.Location = new Point(0, 0);
            lblDeviceId.Margin = new Padding(0);
            lblDeviceId.Name = "lblDeviceId";
            lblDeviceId.Size = new Size(63, 45);
            lblDeviceId.TabIndex = 0;
            lblDeviceId.Text = "设备编号";
            //
            // input_DeviceID
            //
            input_DeviceID.Dock = DockStyle.Fill;
            input_DeviceID.Location = new Point(63, 0);
            input_DeviceID.Margin = new Padding(0);
            input_DeviceID.Name = "input_DeviceID";
            input_DeviceID.Size = new Size(451, 45);
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
            btnSyncDevice.Size = new Size(81, 45);
            btnSyncDevice.TabIndex = 2;
            btnSyncDevice.Tag = "perm:button.system.sync-device:enabled";
            btnSyncDevice.Text = "同步";
            //
            // tlpDeviceName
            //
            tlpDeviceName.AutoSize = true;
            tlpDeviceName.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpDeviceName.ColumnCount = 2;
            tlpDeviceName.ColumnStyles.Add(new ColumnStyle());
            tlpDeviceName.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpDeviceName.Controls.Add(lblDeviceName, 0, 0);
            tlpDeviceName.Controls.Add(input_DeviceName, 1, 0);
            tlpDeviceName.Dock = DockStyle.Fill;
            tlpDeviceName.Location = new Point(0, 43);
            tlpDeviceName.Margin = new Padding(0);
            tlpDeviceName.Name = "tlpDeviceName";
            tlpDeviceName.RowCount = 1;
            tlpDeviceName.RowStyles.Add(new RowStyle());
            tlpDeviceName.Size = new Size(595, 43);
            tlpDeviceName.TabIndex = 1;
            //
            // lblDeviceName
            //
            lblDeviceName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceName.Dock = DockStyle.Fill;
            lblDeviceName.Location = new Point(0, 0);
            lblDeviceName.Margin = new Padding(0);
            lblDeviceName.Name = "lblDeviceName";
            lblDeviceName.Size = new Size(63, 45);
            lblDeviceName.TabIndex = 0;
            lblDeviceName.Text = "设备名称";
            //
            // input_DeviceName
            //
            input_DeviceName.Dock = DockStyle.Fill;
            input_DeviceName.Location = new Point(63, 0);
            input_DeviceName.Margin = new Padding(0);
            input_DeviceName.Name = "input_DeviceName";
            input_DeviceName.Size = new Size(532, 45);
            input_DeviceName.TabIndex = 1;
            //
            // tlpDeviveUrl
            //
            tlpDeviveUrl.AutoSize = true;
            tlpDeviveUrl.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpDeviveUrl.ColumnCount = 2;
            tlpDeviveUrl.ColumnStyles.Add(new ColumnStyle());
            tlpDeviveUrl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpDeviveUrl.Controls.Add(lblDeviceUrl, 0, 0);
            tlpDeviveUrl.Controls.Add(input_DeviceUrl, 1, 0);
            tlpDeviveUrl.Dock = DockStyle.Fill;
            tlpDeviveUrl.Location = new Point(0, 129);
            tlpDeviveUrl.Margin = new Padding(0);
            tlpDeviveUrl.Name = "tlpDeviveUrl";
            tlpDeviveUrl.RowCount = 1;
            tlpDeviveUrl.RowStyles.Add(new RowStyle());
            tlpDeviveUrl.Size = new Size(595, 45);
            tlpDeviveUrl.TabIndex = 2;
            //
            // lblDeviceUrl
            //
            lblDeviceUrl.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceUrl.Dock = DockStyle.Fill;
            lblDeviceUrl.Location = new Point(0, 0);
            lblDeviceUrl.Margin = new Padding(0);
            lblDeviceUrl.Name = "lblDeviceUrl";
            lblDeviceUrl.Size = new Size(63, 46);
            lblDeviceUrl.TabIndex = 0;
            lblDeviceUrl.Text = "状态地址";
            //
            // input_DeviceUrl
            //
            input_DeviceUrl.Dock = DockStyle.Fill;
            input_DeviceUrl.Location = new Point(63, 0);
            input_DeviceUrl.Margin = new Padding(0);
            input_DeviceUrl.Name = "input_DeviceUrl";
            input_DeviceUrl.Size = new Size(532, 46);
            input_DeviceUrl.TabIndex = 1;
            //
            // tlpMesUrl
            //
            tlpMesUrl.AutoSize = true;
            tlpMesUrl.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpMesUrl.ColumnCount = 3;
            tlpMesUrl.ColumnStyles.Add(new ColumnStyle());
            tlpMesUrl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesUrl.ColumnStyles.Add(new ColumnStyle());
            tlpMesUrl.Controls.Add(lblMesUrl, 0, 0);
            tlpMesUrl.Controls.Add(input_BaseUrl, 1, 0);
            tlpMesUrl.Controls.Add(btnTestConnection, 2, 0);
            tlpMesUrl.Dock = DockStyle.Fill;
            tlpMesUrl.Location = new Point(0, 86);
            tlpMesUrl.Margin = new Padding(0);
            tlpMesUrl.Name = "tlpMesUrl";
            tlpMesUrl.RowCount = 1;
            tlpMesUrl.RowStyles.Add(new RowStyle());
            tlpMesUrl.Size = new Size(595, 43);
            tlpMesUrl.TabIndex = 3;
            //
            // lblMesUrl
            //
            lblMesUrl.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesUrl.Dock = DockStyle.Fill;
            lblMesUrl.Location = new Point(0, 0);
            lblMesUrl.Margin = new Padding(0);
            lblMesUrl.Name = "lblMesUrl";
            lblMesUrl.Size = new Size(61, 45);
            lblMesUrl.TabIndex = 0;
            lblMesUrl.Text = "MES地址";
            //
            // input_BaseUrl
            //
            input_BaseUrl.Dock = DockStyle.Fill;
            input_BaseUrl.Location = new Point(61, 0);
            input_BaseUrl.Margin = new Padding(0);
            input_BaseUrl.Name = "input_BaseUrl";
            input_BaseUrl.Padding = new Padding(2, 0, 0, 0);
            input_BaseUrl.Size = new Size(453, 45);
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
            btnTestConnection.Size = new Size(81, 45);
            btnTestConnection.TabIndex = 2;
            btnTestConnection.Tag = "perm:button.system.test-mes:enabled";
            btnTestConnection.Text = "测试";
            //
            // SystemSettingView
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(rootLayout);
            Name = "SystemSettingView";
            Size = new Size(1519, 789);
            rootLayout.ResumeLayout(false);
            titleLayout.ResumeLayout(false);
            titleLayout.PerformLayout();
            tabSettingCategories.ResumeLayout(false);
            tabBasicSettings.ResumeLayout(false);
            grpAppConfig.ResumeLayout(false);
            grpAppConfig.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            tlpLogPath.ResumeLayout(false);
            tlpLogPath.PerformLayout();
            tlpDataPath.ResumeLayout(false);
            tlpDataPath.PerformLayout();
            grpMasterConfig.ResumeLayout(false);
            layoutMasterConfig.ResumeLayout(false);
            layoutMasterConfig.PerformLayout();
            tlpMasterIp.ResumeLayout(false);
            tlpMasterIp.PerformLayout();
            tableLayoutPanel8.ResumeLayout(false);
            tableLayoutPanel8.PerformLayout();
            grpPlcConfig.ResumeLayout(false);
            grpPlcConfig.PerformLayout();
            tlpPlcConfig.ResumeLayout(false);
            tlpPlcConfig.PerformLayout();
            tlpPlcIp.ResumeLayout(false);
            tlpPlcIp.PerformLayout();
            tlpPlcPort.ResumeLayout(false);
            tlpPlcPort.PerformLayout();
            tableLayoutPanel7.ResumeLayout(false);
            tableLayoutPanel7.PerformLayout();
            grpProductionConfig.ResumeLayout(false);
            tlpProductConfig.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel3.PerformLayout();
            tableLayoutPanelHeartbeat.ResumeLayout(false);
            tableLayoutPanelHeartbeat.PerformLayout();
            grpMesConfig.ResumeLayout(false);
            tableLayoutPanelMesConfig.ResumeLayout(false);
            tableLayoutPanelMesConfig.PerformLayout();
            tableLayoutPanel4.ResumeLayout(false);
            tableLayoutPanel4.PerformLayout();
            grpDeviceConfig.ResumeLayout(false);
            layoutDeviceNoConfig.ResumeLayout(false);
            layoutDeviceNoConfig.PerformLayout();
            tlpDeviceId.ResumeLayout(false);
            tlpDeviceId.PerformLayout();
            tlpDeviceName.ResumeLayout(false);
            tlpDeviceName.PerformLayout();
            tlpDeviveUrl.ResumeLayout(false);
            tlpDeviveUrl.PerformLayout();
            tlpMesUrl.ResumeLayout(false);
            tlpMesUrl.PerformLayout();
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
        private GroupBox grpDeviceConfig;
        private TableLayoutPanel layoutDeviceNoConfig;
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
        private AntdUI.Checkbox chkEnableAutoStart;
        private TableLayoutPanel tlpLogPath;
        private AntdUI.Label lblLogPath;
        private AntdUI.Button btnOpenLogPath;
        private AntdUI.Input input_LogsPath;
        private AntdUI.Button btnChangeLogPath;
        private AntdUI.Checkbox chkEnableDualStation;
        private AntdUI.Checkbox chkEnableDualWorkOrder;
        private AntdUI.Checkbox chkValidateRecipeBeforeStart;
        private AntdUI.Checkbox chkEnableFinishExpQtyPrompt;
        private AntdUI.Label lblUploadMode;
        private AntdUI.Select selectUploadMode;
        private AntdUI.Label lblUploadBatchSize;
        private AntdUI.Input inputUploadBatchSize;
        private TableLayoutPanel tableLayoutPanelHeartbeat;
        private AntdUI.Label lblPlcHeartbeatInterval;
        private AntdUI.Input inputPlcHeartbeatInterval;
        private AntdUI.Button btnSaveAll;
        private TableLayoutPanel titleLayout;
        private Label lblTitle;
        private Label lblDescription;
        private TabControl tabSettingCategories;
        private TabPage tabBasicSettings;
        private GroupBox grpProductionConfig;
        private TableLayoutPanel tlpProductConfig;
        private GroupBox grpMesConfig;
        private TableLayoutPanel tableLayoutPanelMesConfig;
        private AntdUI.Checkbox chkUseProductNumberFilter;
        private TableLayoutPanel tableLayoutPanel1;
        private GroupBox grpAppConfig;
        private TableLayoutPanel tableLayoutPanel2;
        private TableLayoutPanel tableLayoutPanel3;
        private TableLayoutPanel tableLayoutPanel4;
        private AntdUI.Label label1;
        private AntdUI.InputNumber input_MesTimeout;
        private AntdUI.Label lblProcessParameterDeviceType;
        private AntdUI.Select selectProcessParameterDeviceType;
    }
}
