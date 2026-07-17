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
            grpCenterServerConfig = new GroupBox();
            tableLayoutPanelCenterServer = new TableLayoutPanel();
            chkEnableCenterServerSync = new AntdUI.Checkbox();
            tlpCenterServerBaseUrl = new TableLayoutPanel();
            lblCenterServerBaseUrl = new AntdUI.Label();
            inputCenterServerBaseUrl = new AntdUI.Input();
            tlpCenterServerSystemType = new TableLayoutPanel();
            lblCenterServerSystemType = new AntdUI.Label();
            selectCenterServerSystemType = new AntdUI.Select();
            tlpCenterServerHeartbeat = new TableLayoutPanel();
            lblCenterServerHeartbeatInterval = new AntdUI.Label();
            inputCenterServerHeartbeatInterval = new AntdUI.Input();
            grpAppConfig = new GroupBox();
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel4 = new TableLayoutPanel();
            chkEnableAutoStart = new AntdUI.Checkbox();
            chkEnableElevatedAutoStart = new AntdUI.Checkbox();
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
            tlpProgramFilePath = new TableLayoutPanel();
            lblProgramFilePath = new AntdUI.Label();
            input_ProgramFilePath = new AntdUI.Input();
            btnChangeProgramFilePath = new AntdUI.Button();
            btnOpenProgramFilePath = new AntdUI.Button();
            grpMesConfig = new GroupBox();
            tableLayoutPanelMesConfig = new TableLayoutPanel();
            tlpCheckbox3 = new TableLayoutPanel();
            chkEnablePostDataCustomHeader = new AntdUI.Checkbox();
            tlpCheckbox2 = new TableLayoutPanel();
            chkEnableWorkOrderStatusReport = new AntdUI.Checkbox();
            chkEnableDeviceStatusReport = new AntdUI.Checkbox();
            tlpProcessParameterType = new TableLayoutPanel();
            input_MesTimeout = new AntdUI.InputNumber();
            lblMesTimeout = new AntdUI.Label();
            lblProcessParameterDeviceType = new AntdUI.Label();
            selectProcessParameterDeviceType = new AntdUI.Select();
            tlpMesUserRoute = new TableLayoutPanel();
            lblMesUserRoute = new AntdUI.Label();
            inputMesUserRoute = new AntdUI.Input();
            tlpCheckbox1 = new TableLayoutPanel();
            chkUseProductNumberFilter = new AntdUI.Checkbox();
            chkShowTestFlagInHistory = new AntdUI.Checkbox();
            tlpMesWorkOrderRoute = new TableLayoutPanel();
            lblMesWorkOrderRoute = new AntdUI.Label();
            inputMesWorkOrderRoute = new AntdUI.Input();
            tlpMesServerTimeRoute = new TableLayoutPanel();
            lblMesServerTimeRoute = new AntdUI.Label();
            inputMesServerTimeRoute = new AntdUI.Input();
            tlpPostDataHeader = new TableLayoutPanel();
            inputPostDataHeaderValue = new AntdUI.Input();
            lblPostDataHeaderValue = new AntdUI.Label();
            inputPostDataHeaderKey = new AntdUI.Input();
            lblPostDataHeaderKey = new AntdUI.Label();
            tlpMesProgramManageRoute = new TableLayoutPanel();
            lblMesProgramManageRoute = new AntdUI.Label();
            inputMesProgramManageRoute = new AntdUI.Input();
            tlpMesStartWorkRoute = new TableLayoutPanel();
            lblMesStartWorkRoute = new AntdUI.Label();
            inputMesStartWorkRoute = new AntdUI.Input();
            tlpMesWorkStatusRoute = new TableLayoutPanel();
            lblMesWorkStatusRoute = new AntdUI.Label();
            inputMesWorkStatusRoute = new AntdUI.Input();
            tlpMesEndWorkRoute = new TableLayoutPanel();
            lblMesEndWorkRoute = new AntdUI.Label();
            inputMesEndWorkRoute = new AntdUI.Input();
            tlpMesReportFileRoute = new TableLayoutPanel();
            lblMesReportFileRoute = new AntdUI.Label();
            inputMesReportFileRoute = new AntdUI.Input();
            tlpMesPostDataRoute = new TableLayoutPanel();
            lblMesPostDataRoute = new AntdUI.Label();
            inputMesPostDataRoute = new AntdUI.Input();
            tlpMesDeviceRoute = new TableLayoutPanel();
            lblMesDeviceRoute = new AntdUI.Label();
            inputMesDeviceRoute = new AntdUI.Input();
            tlpMesDeviceStatusRoute = new TableLayoutPanel();
            lblMesDeviceStatusRoute = new AntdUI.Label();
            inputMesDeviceStatusRoute = new AntdUI.Input();
            grpProductionConfig = new GroupBox();
            tlpProductConfig = new TableLayoutPanel();
            stationDisplayNameLayout = new TableLayoutPanel();
            lblStation1DisplayName = new AntdUI.Label();
            inputStation1DisplayName = new AntdUI.Input();
            lblStation2DisplayName = new AntdUI.Label();
            inputStation2DisplayName = new AntdUI.Input();
            tlpUploadConfig = new TableLayoutPanel();
            inputUploadBatchSize = new AntdUI.Input();
            lblUploadBatchSize = new AntdUI.Label();
            selectUploadMode = new AntdUI.Select();
            lblUploadMode = new AntdUI.Label();
            chkEnableDualStation = new AntdUI.Checkbox();
            chkUseOperatorInputDialog = new AntdUI.Checkbox();
            chkValidateRecipeBeforeStart = new AntdUI.Checkbox();
            chkEnableFinishExpQtyPrompt = new AntdUI.Checkbox();
            tableLayoutPanelHeartbeat = new TableLayoutPanel();
            lblPlcHeartbeatInterval = new AntdUI.Label();
            inputPlcHeartbeatInterval = new AntdUI.Input();
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
            chkEnablePlcAlarmReading = new AntdUI.Checkbox();
            tlpPlcStringNumericMode = new TableLayoutPanel();
            lblPlcStringNumericFormatMode = new AntdUI.Label();
            selectPlcStringNumericFormatMode = new AntdUI.Select();
            chkEnablePlcStringNumericFormatting = new AntdUI.Checkbox();
            rootLayout.SuspendLayout();
            titleLayout.SuspendLayout();
            tabSettingCategories.SuspendLayout();
            tabBasicSettings.SuspendLayout();
            grpCenterServerConfig.SuspendLayout();
            tableLayoutPanelCenterServer.SuspendLayout();
            tlpCenterServerBaseUrl.SuspendLayout();
            tlpCenterServerSystemType.SuspendLayout();
            tlpCenterServerHeartbeat.SuspendLayout();
            grpAppConfig.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            tlpLogPath.SuspendLayout();
            tlpDataPath.SuspendLayout();
            tlpProgramFilePath.SuspendLayout();
            grpMesConfig.SuspendLayout();
            tableLayoutPanelMesConfig.SuspendLayout();
            tlpCheckbox3.SuspendLayout();
            tlpCheckbox2.SuspendLayout();
            tlpProcessParameterType.SuspendLayout();
            tlpMesUserRoute.SuspendLayout();
            tlpCheckbox1.SuspendLayout();
            tlpMesWorkOrderRoute.SuspendLayout();
            tlpMesServerTimeRoute.SuspendLayout();
            tlpPostDataHeader.SuspendLayout();
            tlpMesProgramManageRoute.SuspendLayout();
            tlpMesStartWorkRoute.SuspendLayout();
            tlpMesWorkStatusRoute.SuspendLayout();
            tlpMesEndWorkRoute.SuspendLayout();
            tlpMesReportFileRoute.SuspendLayout();
            tlpMesPostDataRoute.SuspendLayout();
            tlpMesDeviceRoute.SuspendLayout();
            tlpMesDeviceStatusRoute.SuspendLayout();
            grpProductionConfig.SuspendLayout();
            tlpProductConfig.SuspendLayout();
            stationDisplayNameLayout.SuspendLayout();
            tlpUploadConfig.SuspendLayout();
            tableLayoutPanelHeartbeat.SuspendLayout();
            grpDeviceConfig.SuspendLayout();
            layoutDeviceNoConfig.SuspendLayout();
            tlpDeviceId.SuspendLayout();
            tlpDeviceName.SuspendLayout();
            tlpDeviveUrl.SuspendLayout();
            tlpMesUrl.SuspendLayout();
            grpPlcConfig.SuspendLayout();
            tlpPlcConfig.SuspendLayout();
            tlpPlcIp.SuspendLayout();
            tlpPlcPort.SuspendLayout();
            tableLayoutPanel7.SuspendLayout();
            tlpPlcStringNumericMode.SuspendLayout();
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
            tabBasicSettings.Controls.Add(grpCenterServerConfig);
            tabBasicSettings.Controls.Add(grpAppConfig);
            tabBasicSettings.Controls.Add(grpMesConfig);
            tabBasicSettings.Controls.Add(grpProductionConfig);
            tabBasicSettings.Controls.Add(grpDeviceConfig);
            tabBasicSettings.Controls.Add(grpPlcConfig);
            tabBasicSettings.Location = new Point(4, 29);
            tabBasicSettings.Name = "tabBasicSettings";
            tabBasicSettings.Padding = new Padding(3);
            tabBasicSettings.Size = new Size(1463, 669);
            tabBasicSettings.TabIndex = 0;
            tabBasicSettings.Text = "基础设置";
            tabBasicSettings.UseVisualStyleBackColor = true;
            // 
            // grpCenterServerConfig
            // 
            grpCenterServerConfig.Controls.Add(tableLayoutPanelCenterServer);
            grpCenterServerConfig.Location = new Point(780, 463);
            grpCenterServerConfig.Margin = new Padding(0);
            grpCenterServerConfig.Name = "grpCenterServerConfig";
            grpCenterServerConfig.Size = new Size(541, 182);
            grpCenterServerConfig.TabIndex = 6;
            grpCenterServerConfig.TabStop = false;
            grpCenterServerConfig.Text = "中心服务器";
            // 
            // tableLayoutPanelCenterServer
            // 
            tableLayoutPanelCenterServer.ColumnCount = 1;
            tableLayoutPanelCenterServer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelCenterServer.Controls.Add(chkEnableCenterServerSync, 0, 0);
            tableLayoutPanelCenterServer.Controls.Add(tlpCenterServerBaseUrl, 0, 1);
            tableLayoutPanelCenterServer.Controls.Add(tlpCenterServerSystemType, 0, 2);
            tableLayoutPanelCenterServer.Controls.Add(tlpCenterServerHeartbeat, 0, 3);
            tableLayoutPanelCenterServer.Dock = DockStyle.Fill;
            tableLayoutPanelCenterServer.Location = new Point(3, 23);
            tableLayoutPanelCenterServer.Name = "tableLayoutPanelCenterServer";
            tableLayoutPanelCenterServer.RowCount = 4;
            tableLayoutPanelCenterServer.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelCenterServer.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelCenterServer.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelCenterServer.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelCenterServer.Size = new Size(535, 156);
            tableLayoutPanelCenterServer.TabIndex = 0;
            // 
            // chkEnableCenterServerSync
            // 
            chkEnableCenterServerSync.Dock = DockStyle.Fill;
            chkEnableCenterServerSync.Location = new Point(0, 0);
            chkEnableCenterServerSync.Margin = new Padding(0);
            chkEnableCenterServerSync.Name = "chkEnableCenterServerSync";
            chkEnableCenterServerSync.Size = new Size(535, 45);
            chkEnableCenterServerSync.TabIndex = 0;
            chkEnableCenterServerSync.Text = "启用中心服务器同步";
            // 
            // tlpCenterServerBaseUrl
            // 
            tlpCenterServerBaseUrl.ColumnCount = 2;
            tlpCenterServerBaseUrl.ColumnStyles.Add(new ColumnStyle());
            tlpCenterServerBaseUrl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpCenterServerBaseUrl.Controls.Add(lblCenterServerBaseUrl, 0, 0);
            tlpCenterServerBaseUrl.Controls.Add(inputCenterServerBaseUrl, 1, 0);
            tlpCenterServerBaseUrl.Dock = DockStyle.Fill;
            tlpCenterServerBaseUrl.Location = new Point(0, 45);
            tlpCenterServerBaseUrl.Margin = new Padding(0);
            tlpCenterServerBaseUrl.Name = "tlpCenterServerBaseUrl";
            tlpCenterServerBaseUrl.RowCount = 1;
            tlpCenterServerBaseUrl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpCenterServerBaseUrl.Size = new Size(535, 45);
            tlpCenterServerBaseUrl.TabIndex = 1;
            // 
            // lblCenterServerBaseUrl
            // 
            lblCenterServerBaseUrl.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblCenterServerBaseUrl.Dock = DockStyle.Fill;
            lblCenterServerBaseUrl.Location = new Point(0, 0);
            lblCenterServerBaseUrl.Margin = new Padding(0);
            lblCenterServerBaseUrl.Name = "lblCenterServerBaseUrl";
            lblCenterServerBaseUrl.Padding = new Padding(8, 0, 0, 0);
            lblCenterServerBaseUrl.Size = new Size(118, 45);
            lblCenterServerBaseUrl.TabIndex = 0;
            lblCenterServerBaseUrl.Text = "中心服务器地址";
            // 
            // inputCenterServerBaseUrl
            // 
            inputCenterServerBaseUrl.Dock = DockStyle.Fill;
            inputCenterServerBaseUrl.Location = new Point(118, 0);
            inputCenterServerBaseUrl.Margin = new Padding(0);
            inputCenterServerBaseUrl.Name = "inputCenterServerBaseUrl";
            inputCenterServerBaseUrl.Size = new Size(417, 45);
            inputCenterServerBaseUrl.TabIndex = 1;
            // 
            // tlpCenterServerSystemType
            // 
            tlpCenterServerSystemType.ColumnCount = 2;
            tlpCenterServerSystemType.ColumnStyles.Add(new ColumnStyle());
            tlpCenterServerSystemType.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpCenterServerSystemType.Controls.Add(lblCenterServerSystemType, 0, 0);
            tlpCenterServerSystemType.Controls.Add(selectCenterServerSystemType, 1, 0);
            tlpCenterServerSystemType.Dock = DockStyle.Fill;
            tlpCenterServerSystemType.Location = new Point(0, 90);
            tlpCenterServerSystemType.Margin = new Padding(0);
            tlpCenterServerSystemType.Name = "tlpCenterServerSystemType";
            tlpCenterServerSystemType.RowCount = 1;
            tlpCenterServerSystemType.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpCenterServerSystemType.Size = new Size(535, 45);
            tlpCenterServerSystemType.TabIndex = 2;
            // 
            // lblCenterServerSystemType
            // 
            lblCenterServerSystemType.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblCenterServerSystemType.Dock = DockStyle.Fill;
            lblCenterServerSystemType.Location = new Point(0, 0);
            lblCenterServerSystemType.Margin = new Padding(0);
            lblCenterServerSystemType.Name = "lblCenterServerSystemType";
            lblCenterServerSystemType.Padding = new Padding(8, 0, 0, 0);
            lblCenterServerSystemType.Size = new Size(71, 45);
            lblCenterServerSystemType.TabIndex = 0;
            lblCenterServerSystemType.Text = "系统类型";
            // 
            // selectCenterServerSystemType
            // 
            selectCenterServerSystemType.Dock = DockStyle.Fill;
            selectCenterServerSystemType.Location = new Point(71, 0);
            selectCenterServerSystemType.Margin = new Padding(0);
            selectCenterServerSystemType.MaxCount = 10;
            selectCenterServerSystemType.Name = "selectCenterServerSystemType";
            selectCenterServerSystemType.Size = new Size(464, 45);
            selectCenterServerSystemType.TabIndex = 1;
            // 
            // tlpCenterServerHeartbeat
            // 
            tlpCenterServerHeartbeat.ColumnCount = 2;
            tlpCenterServerHeartbeat.ColumnStyles.Add(new ColumnStyle());
            tlpCenterServerHeartbeat.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpCenterServerHeartbeat.Controls.Add(lblCenterServerHeartbeatInterval, 0, 0);
            tlpCenterServerHeartbeat.Controls.Add(inputCenterServerHeartbeatInterval, 1, 0);
            tlpCenterServerHeartbeat.Dock = DockStyle.Fill;
            tlpCenterServerHeartbeat.Location = new Point(0, 135);
            tlpCenterServerHeartbeat.Margin = new Padding(0);
            tlpCenterServerHeartbeat.Name = "tlpCenterServerHeartbeat";
            tlpCenterServerHeartbeat.RowCount = 1;
            tlpCenterServerHeartbeat.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpCenterServerHeartbeat.Size = new Size(535, 45);
            tlpCenterServerHeartbeat.TabIndex = 4;
            // 
            // lblCenterServerHeartbeatInterval
            // 
            lblCenterServerHeartbeatInterval.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblCenterServerHeartbeatInterval.Dock = DockStyle.Fill;
            lblCenterServerHeartbeatInterval.Location = new Point(0, 0);
            lblCenterServerHeartbeatInterval.Margin = new Padding(0);
            lblCenterServerHeartbeatInterval.Name = "lblCenterServerHeartbeatInterval";
            lblCenterServerHeartbeatInterval.Padding = new Padding(8, 0, 0, 0);
            lblCenterServerHeartbeatInterval.Size = new Size(87, 45);
            lblCenterServerHeartbeatInterval.TabIndex = 0;
            lblCenterServerHeartbeatInterval.Text = "心跳间隔(s)";
            // 
            // inputCenterServerHeartbeatInterval
            // 
            inputCenterServerHeartbeatInterval.Dock = DockStyle.Fill;
            inputCenterServerHeartbeatInterval.Location = new Point(87, 0);
            inputCenterServerHeartbeatInterval.Margin = new Padding(0);
            inputCenterServerHeartbeatInterval.Name = "inputCenterServerHeartbeatInterval";
            inputCenterServerHeartbeatInterval.Size = new Size(448, 45);
            inputCenterServerHeartbeatInterval.TabIndex = 1;
            inputCenterServerHeartbeatInterval.Text = "5";
            // 
            // grpAppConfig
            // 
            grpAppConfig.Controls.Add(tableLayoutPanel1);
            grpAppConfig.Location = new Point(317, 215);
            grpAppConfig.Margin = new Padding(0);
            grpAppConfig.Name = "grpAppConfig";
            grpAppConfig.Size = new Size(455, 204);
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
            tableLayoutPanel1.Controls.Add(tableLayoutPanel4, 0, 3);
            tableLayoutPanel1.Controls.Add(tlpLogPath, 0, 0);
            tableLayoutPanel1.Controls.Add(tlpDataPath, 0, 1);
            tableLayoutPanel1.Controls.Add(tlpProgramFilePath, 0, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(3, 23);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Size = new Size(449, 178);
            tableLayoutPanel1.TabIndex = 6;
            // 
            // tableLayoutPanel4
            // 
            tableLayoutPanel4.ColumnCount = 2;
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel4.Controls.Add(chkEnableAutoStart, 0, 0);
            tableLayoutPanel4.Controls.Add(chkEnableElevatedAutoStart, 1, 0);
            tableLayoutPanel4.Location = new Point(0, 135);
            tableLayoutPanel4.Margin = new Padding(0);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 1;
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Size = new Size(408, 45);
            tableLayoutPanel4.TabIndex = 7;
            // 
            // chkEnableAutoStart
            // 
            chkEnableAutoStart.Checked = true;
            chkEnableAutoStart.CheckState = CheckState.Checked;
            chkEnableAutoStart.Dock = DockStyle.Fill;
            chkEnableAutoStart.Location = new Point(0, 0);
            chkEnableAutoStart.Margin = new Padding(0);
            chkEnableAutoStart.Name = "chkEnableAutoStart";
            chkEnableAutoStart.Padding = new Padding(8, 0, 0, 0);
            chkEnableAutoStart.Size = new Size(204, 45);
            chkEnableAutoStart.TabIndex = 6;
            chkEnableAutoStart.Text = "开机自启";
            // 
            // chkEnableElevatedAutoStart
            // 
            chkEnableElevatedAutoStart.Checked = true;
            chkEnableElevatedAutoStart.CheckState = CheckState.Checked;
            chkEnableElevatedAutoStart.Dock = DockStyle.Fill;
            chkEnableElevatedAutoStart.Location = new Point(204, 0);
            chkEnableElevatedAutoStart.Margin = new Padding(0);
            chkEnableElevatedAutoStart.Name = "chkEnableElevatedAutoStart";
            chkEnableElevatedAutoStart.Padding = new Padding(8, 0, 0, 0);
            chkEnableElevatedAutoStart.Size = new Size(204, 45);
            chkEnableElevatedAutoStart.TabIndex = 7;
            chkEnableElevatedAutoStart.Text = "以管理员权限运行";
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
            tlpLogPath.Size = new Size(449, 45);
            tlpLogPath.TabIndex = 4;
            // 
            // lblLogPath
            // 
            lblLogPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblLogPath.Dock = DockStyle.Fill;
            lblLogPath.Location = new Point(0, 0);
            lblLogPath.Margin = new Padding(0);
            lblLogPath.Name = "lblLogPath";
            lblLogPath.Padding = new Padding(8, 0, 0, 0);
            lblLogPath.Size = new Size(71, 45);
            lblLogPath.TabIndex = 0;
            lblLogPath.Text = "日志目录";
            // 
            // input_LogsPath
            // 
            input_LogsPath.Dock = DockStyle.Fill;
            input_LogsPath.Location = new Point(71, 0);
            input_LogsPath.Margin = new Padding(0);
            input_LogsPath.Name = "input_LogsPath";
            input_LogsPath.Size = new Size(216, 45);
            input_LogsPath.TabIndex = 1;
            // 
            // btnChangeLogPath
            // 
            btnChangeLogPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnChangeLogPath.BorderWidth = 1F;
            btnChangeLogPath.Dock = DockStyle.Fill;
            btnChangeLogPath.IconSvg = "FolderOpenOutlined";
            btnChangeLogPath.Location = new Point(287, 0);
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
            btnOpenLogPath.Location = new Point(368, 0);
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
            tlpDataPath.Size = new Size(449, 45);
            tlpDataPath.TabIndex = 5;
            // 
            // lblDataPath
            // 
            lblDataPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDataPath.Dock = DockStyle.Fill;
            lblDataPath.Location = new Point(0, 0);
            lblDataPath.Margin = new Padding(0);
            lblDataPath.Name = "lblDataPath";
            lblDataPath.Padding = new Padding(8, 0, 0, 0);
            lblDataPath.Size = new Size(71, 45);
            lblDataPath.TabIndex = 0;
            lblDataPath.Text = "数据目录";
            // 
            // input_DataPath
            // 
            input_DataPath.Dock = DockStyle.Fill;
            input_DataPath.Location = new Point(71, 0);
            input_DataPath.Margin = new Padding(0);
            input_DataPath.Name = "input_DataPath";
            input_DataPath.Size = new Size(216, 45);
            input_DataPath.TabIndex = 1;
            // 
            // btnChangeDataPath
            // 
            btnChangeDataPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnChangeDataPath.BorderWidth = 1F;
            btnChangeDataPath.Dock = DockStyle.Fill;
            btnChangeDataPath.IconSvg = "FolderOpenOutlined";
            btnChangeDataPath.Location = new Point(287, 0);
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
            btnOpenDataPath.Location = new Point(368, 0);
            btnOpenDataPath.Margin = new Padding(0);
            btnOpenDataPath.Name = "btnOpenDataPath";
            btnOpenDataPath.Size = new Size(81, 45);
            btnOpenDataPath.TabIndex = 3;
            btnOpenDataPath.Tag = "perm:button.system.open-path:enabled";
            btnOpenDataPath.Text = "打开";
            // 
            // tlpProgramFilePath
            // 
            tlpProgramFilePath.AutoSize = true;
            tlpProgramFilePath.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpProgramFilePath.ColumnCount = 4;
            tlpProgramFilePath.ColumnStyles.Add(new ColumnStyle());
            tlpProgramFilePath.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpProgramFilePath.ColumnStyles.Add(new ColumnStyle());
            tlpProgramFilePath.ColumnStyles.Add(new ColumnStyle());
            tlpProgramFilePath.Controls.Add(lblProgramFilePath, 0, 0);
            tlpProgramFilePath.Controls.Add(input_ProgramFilePath, 1, 0);
            tlpProgramFilePath.Controls.Add(btnChangeProgramFilePath, 2, 0);
            tlpProgramFilePath.Controls.Add(btnOpenProgramFilePath, 3, 0);
            tlpProgramFilePath.Dock = DockStyle.Fill;
            tlpProgramFilePath.Location = new Point(0, 90);
            tlpProgramFilePath.Margin = new Padding(0);
            tlpProgramFilePath.Name = "tlpProgramFilePath";
            tlpProgramFilePath.RowCount = 1;
            tlpProgramFilePath.RowStyles.Add(new RowStyle());
            tlpProgramFilePath.Size = new Size(449, 45);
            tlpProgramFilePath.TabIndex = 7;
            // 
            // lblProgramFilePath
            // 
            lblProgramFilePath.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProgramFilePath.Dock = DockStyle.Fill;
            lblProgramFilePath.Location = new Point(0, 0);
            lblProgramFilePath.Margin = new Padding(0);
            lblProgramFilePath.Name = "lblProgramFilePath";
            lblProgramFilePath.Padding = new Padding(8, 0, 0, 0);
            lblProgramFilePath.Size = new Size(71, 45);
            lblProgramFilePath.TabIndex = 0;
            lblProgramFilePath.Text = "程序目录";
            // 
            // input_ProgramFilePath
            // 
            input_ProgramFilePath.Dock = DockStyle.Fill;
            input_ProgramFilePath.Location = new Point(71, 0);
            input_ProgramFilePath.Margin = new Padding(0);
            input_ProgramFilePath.Name = "input_ProgramFilePath";
            input_ProgramFilePath.Size = new Size(216, 45);
            input_ProgramFilePath.TabIndex = 1;
            // 
            // btnChangeProgramFilePath
            // 
            btnChangeProgramFilePath.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnChangeProgramFilePath.BorderWidth = 1F;
            btnChangeProgramFilePath.Dock = DockStyle.Fill;
            btnChangeProgramFilePath.IconSvg = "FolderOpenOutlined";
            btnChangeProgramFilePath.Location = new Point(287, 0);
            btnChangeProgramFilePath.Margin = new Padding(0);
            btnChangeProgramFilePath.Name = "btnChangeProgramFilePath";
            btnChangeProgramFilePath.Size = new Size(81, 45);
            btnChangeProgramFilePath.TabIndex = 2;
            btnChangeProgramFilePath.Tag = "perm:button.system.change-path:enabled";
            btnChangeProgramFilePath.Text = "更改";
            // 
            // btnOpenProgramFilePath
            // 
            btnOpenProgramFilePath.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnOpenProgramFilePath.BorderWidth = 1F;
            btnOpenProgramFilePath.Dock = DockStyle.Fill;
            btnOpenProgramFilePath.IconSvg = "FolderOutlined";
            btnOpenProgramFilePath.Location = new Point(368, 0);
            btnOpenProgramFilePath.Margin = new Padding(0);
            btnOpenProgramFilePath.Name = "btnOpenProgramFilePath";
            btnOpenProgramFilePath.Size = new Size(81, 45);
            btnOpenProgramFilePath.TabIndex = 3;
            btnOpenProgramFilePath.Tag = "perm:button.system.open-path:enabled";
            btnOpenProgramFilePath.Text = "打开";
            // 
            // grpMesConfig
            // 
            grpMesConfig.Controls.Add(tableLayoutPanelMesConfig);
            grpMesConfig.Location = new Point(780, 260);
            grpMesConfig.Margin = new Padding(0);
            grpMesConfig.Name = "grpMesConfig";
            grpMesConfig.Size = new Size(541, 203);
            grpMesConfig.TabIndex = 3;
            grpMesConfig.TabStop = false;
            grpMesConfig.Text = "MES Config";
            // 
            // tableLayoutPanelMesConfig
            // 
            tableLayoutPanelMesConfig.AutoScroll = true;
            tableLayoutPanelMesConfig.AutoSize = true;
            tableLayoutPanelMesConfig.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanelMesConfig.ColumnCount = 1;
            tableLayoutPanelMesConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelMesConfig.Controls.Add(tlpCheckbox3, 0, 3);
            tableLayoutPanelMesConfig.Controls.Add(tlpCheckbox2, 0, 2);
            tableLayoutPanelMesConfig.Controls.Add(tlpProcessParameterType, 0, 0);
            tableLayoutPanelMesConfig.Controls.Add(tlpMesUserRoute, 0, 5);
            tableLayoutPanelMesConfig.Controls.Add(tlpCheckbox1, 0, 1);
            tableLayoutPanelMesConfig.Controls.Add(tlpMesWorkOrderRoute, 0, 6);
            tableLayoutPanelMesConfig.Controls.Add(tlpMesServerTimeRoute, 0, 7);
            tableLayoutPanelMesConfig.Controls.Add(tlpPostDataHeader, 0, 4);
            tableLayoutPanelMesConfig.Controls.Add(tlpMesProgramManageRoute, 0, 8);
            tableLayoutPanelMesConfig.Controls.Add(tlpMesStartWorkRoute, 0, 9);
            tableLayoutPanelMesConfig.Controls.Add(tlpMesWorkStatusRoute, 0, 10);
            tableLayoutPanelMesConfig.Controls.Add(tlpMesEndWorkRoute, 0, 11);
            tableLayoutPanelMesConfig.Controls.Add(tlpMesReportFileRoute, 0, 12);
            tableLayoutPanelMesConfig.Controls.Add(tlpMesPostDataRoute, 0, 13);
            tableLayoutPanelMesConfig.Controls.Add(tlpMesDeviceRoute, 0, 14);
            tableLayoutPanelMesConfig.Controls.Add(tlpMesDeviceStatusRoute, 0, 15);
            tableLayoutPanelMesConfig.Dock = DockStyle.Fill;
            tableLayoutPanelMesConfig.Location = new Point(3, 23);
            tableLayoutPanelMesConfig.Name = "tableLayoutPanelMesConfig";
            tableLayoutPanelMesConfig.RowCount = 16;
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tableLayoutPanelMesConfig.Size = new Size(535, 177);
            tableLayoutPanelMesConfig.TabIndex = 0;
            // 
            // tlpCheckbox3
            // 
            tlpCheckbox3.ColumnCount = 2;
            tlpCheckbox3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCheckbox3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCheckbox3.Controls.Add(chkEnablePostDataCustomHeader, 0, 0);
            tlpCheckbox3.Dock = DockStyle.Fill;
            tlpCheckbox3.Location = new Point(0, 135);
            tlpCheckbox3.Margin = new Padding(0);
            tlpCheckbox3.Name = "tlpCheckbox3";
            tlpCheckbox3.RowCount = 1;
            tlpCheckbox3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCheckbox3.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tlpCheckbox3.Size = new Size(535, 45);
            tlpCheckbox3.TabIndex = 20;
            // 
            // chkEnablePostDataCustomHeader
            // 
            tlpCheckbox3.SetColumnSpan(chkEnablePostDataCustomHeader, 2);
            chkEnablePostDataCustomHeader.Dock = DockStyle.Fill;
            chkEnablePostDataCustomHeader.Location = new Point(0, 0);
            chkEnablePostDataCustomHeader.Margin = new Padding(0);
            chkEnablePostDataCustomHeader.Name = "chkEnablePostDataCustomHeader";
            chkEnablePostDataCustomHeader.Padding = new Padding(8, 0, 0, 0);
            chkEnablePostDataCustomHeader.Size = new Size(535, 45);
            chkEnablePostDataCustomHeader.TabIndex = 19;
            chkEnablePostDataCustomHeader.Text = "PostData启用Header";
            // 
            // tlpCheckbox2
            // 
            tlpCheckbox2.ColumnCount = 2;
            tlpCheckbox2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCheckbox2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCheckbox2.Controls.Add(chkEnableWorkOrderStatusReport, 1, 0);
            tlpCheckbox2.Controls.Add(chkEnableDeviceStatusReport, 0, 0);
            tlpCheckbox2.Dock = DockStyle.Fill;
            tlpCheckbox2.Location = new Point(0, 90);
            tlpCheckbox2.Margin = new Padding(0);
            tlpCheckbox2.Name = "tlpCheckbox2";
            tlpCheckbox2.RowCount = 1;
            tlpCheckbox2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpCheckbox2.Size = new Size(535, 45);
            tlpCheckbox2.TabIndex = 7;
            // 
            // chkEnableWorkOrderStatusReport
            // 
            chkEnableWorkOrderStatusReport.Dock = DockStyle.Fill;
            chkEnableWorkOrderStatusReport.Location = new Point(267, 0);
            chkEnableWorkOrderStatusReport.Margin = new Padding(0);
            chkEnableWorkOrderStatusReport.Name = "chkEnableWorkOrderStatusReport";
            chkEnableWorkOrderStatusReport.Padding = new Padding(8, 0, 0, 0);
            chkEnableWorkOrderStatusReport.Size = new Size(268, 45);
            chkEnableWorkOrderStatusReport.TabIndex = 6;
            chkEnableWorkOrderStatusReport.Text = "启用工单状态上报";
            // 
            // chkEnableDeviceStatusReport
            // 
            chkEnableDeviceStatusReport.Checked = true;
            chkEnableDeviceStatusReport.CheckState = CheckState.Checked;
            chkEnableDeviceStatusReport.Dock = DockStyle.Fill;
            chkEnableDeviceStatusReport.Location = new Point(0, 0);
            chkEnableDeviceStatusReport.Margin = new Padding(0);
            chkEnableDeviceStatusReport.Name = "chkEnableDeviceStatusReport";
            chkEnableDeviceStatusReport.Padding = new Padding(8, 0, 0, 0);
            chkEnableDeviceStatusReport.Size = new Size(267, 45);
            chkEnableDeviceStatusReport.TabIndex = 5;
            chkEnableDeviceStatusReport.Text = "启用设备状态上报";
            // 
            // tlpProcessParameterType
            // 
            tlpProcessParameterType.ColumnCount = 4;
            tlpProcessParameterType.ColumnStyles.Add(new ColumnStyle());
            tlpProcessParameterType.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
            tlpProcessParameterType.ColumnStyles.Add(new ColumnStyle());
            tlpProcessParameterType.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpProcessParameterType.Controls.Add(input_MesTimeout, 3, 0);
            tlpProcessParameterType.Controls.Add(lblMesTimeout, 2, 0);
            tlpProcessParameterType.Controls.Add(lblProcessParameterDeviceType, 0, 0);
            tlpProcessParameterType.Controls.Add(selectProcessParameterDeviceType, 1, 0);
            tlpProcessParameterType.Dock = DockStyle.Fill;
            tlpProcessParameterType.Location = new Point(0, 0);
            tlpProcessParameterType.Margin = new Padding(0);
            tlpProcessParameterType.Name = "tlpProcessParameterType";
            tlpProcessParameterType.RowCount = 1;
            tlpProcessParameterType.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProcessParameterType.Size = new Size(535, 45);
            tlpProcessParameterType.TabIndex = 7;
            // 
            // input_MesTimeout
            // 
            input_MesTimeout.Dock = DockStyle.Fill;
            input_MesTimeout.Location = new Point(484, 0);
            input_MesTimeout.Margin = new Padding(0);
            input_MesTimeout.Name = "input_MesTimeout";
            input_MesTimeout.Size = new Size(51, 45);
            input_MesTimeout.TabIndex = 1;
            input_MesTimeout.Text = "0";
            // 
            // lblMesTimeout
            // 
            lblMesTimeout.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesTimeout.Dock = DockStyle.Fill;
            lblMesTimeout.Location = new Point(370, 0);
            lblMesTimeout.Margin = new Padding(0);
            lblMesTimeout.Name = "lblMesTimeout";
            lblMesTimeout.Padding = new Padding(10, 0, 0, 0);
            lblMesTimeout.Size = new Size(114, 45);
            lblMesTimeout.TabIndex = 0;
            lblMesTimeout.Text = "MES Timeout(s)";
            // 
            // lblProcessParameterDeviceType
            // 
            lblProcessParameterDeviceType.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProcessParameterDeviceType.Dock = DockStyle.Fill;
            lblProcessParameterDeviceType.Location = new Point(0, 0);
            lblProcessParameterDeviceType.Margin = new Padding(0);
            lblProcessParameterDeviceType.Name = "lblProcessParameterDeviceType";
            lblProcessParameterDeviceType.Padding = new Padding(10, 0, 0, 0);
            lblProcessParameterDeviceType.Size = new Size(166, 45);
            lblProcessParameterDeviceType.TabIndex = 2;
            lblProcessParameterDeviceType.Text = "Process parameter type";
            // 
            // selectProcessParameterDeviceType
            // 
            selectProcessParameterDeviceType.Dock = DockStyle.Fill;
            selectProcessParameterDeviceType.Location = new Point(166, 0);
            selectProcessParameterDeviceType.Margin = new Padding(0);
            selectProcessParameterDeviceType.MaxCount = 10;
            selectProcessParameterDeviceType.Name = "selectProcessParameterDeviceType";
            selectProcessParameterDeviceType.Size = new Size(204, 45);
            selectProcessParameterDeviceType.TabIndex = 3;
            // 
            // tlpMesUserRoute
            // 
            tlpMesUserRoute.ColumnCount = 2;
            tlpMesUserRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tlpMesUserRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesUserRoute.Controls.Add(lblMesUserRoute, 0, 0);
            tlpMesUserRoute.Controls.Add(inputMesUserRoute, 1, 0);
            tlpMesUserRoute.Dock = DockStyle.Fill;
            tlpMesUserRoute.Location = new Point(0, 225);
            tlpMesUserRoute.Margin = new Padding(0);
            tlpMesUserRoute.Name = "tlpMesUserRoute";
            tlpMesUserRoute.RowCount = 1;
            tlpMesUserRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesUserRoute.Size = new Size(535, 45);
            tlpMesUserRoute.TabIndex = 8;
            // 
            // lblMesUserRoute
            // 
            lblMesUserRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesUserRoute.Dock = DockStyle.Fill;
            lblMesUserRoute.Location = new Point(0, 0);
            lblMesUserRoute.Margin = new Padding(0);
            lblMesUserRoute.Name = "lblMesUserRoute";
            lblMesUserRoute.Padding = new Padding(10, 0, 0, 0);
            lblMesUserRoute.Size = new Size(105, 45);
            lblMesUserRoute.TabIndex = 0;
            lblMesUserRoute.Text = "员工信息路由";
            // 
            // inputMesUserRoute
            // 
            inputMesUserRoute.Dock = DockStyle.Fill;
            inputMesUserRoute.Location = new Point(150, 0);
            inputMesUserRoute.Margin = new Padding(0);
            inputMesUserRoute.Name = "inputMesUserRoute";
            inputMesUserRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesUserRoute.Size = new Size(385, 45);
            inputMesUserRoute.TabIndex = 1;
            inputMesUserRoute.Text = "api/User";
            // 
            // tlpCheckbox1
            // 
            tlpCheckbox1.ColumnCount = 2;
            tableLayoutPanelMesConfig.SetColumnSpan(tlpCheckbox1, 2);
            tlpCheckbox1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCheckbox1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCheckbox1.Controls.Add(chkUseProductNumberFilter, 1, 0);
            tlpCheckbox1.Controls.Add(chkShowTestFlagInHistory, 0, 0);
            tlpCheckbox1.Dock = DockStyle.Fill;
            tlpCheckbox1.Location = new Point(0, 45);
            tlpCheckbox1.Margin = new Padding(0);
            tlpCheckbox1.Name = "tlpCheckbox1";
            tlpCheckbox1.RowCount = 1;
            tlpCheckbox1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpCheckbox1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tlpCheckbox1.Size = new Size(535, 45);
            tlpCheckbox1.TabIndex = 1;
            // 
            // chkUseProductNumberFilter
            // 
            chkUseProductNumberFilter.Dock = DockStyle.Fill;
            chkUseProductNumberFilter.Location = new Point(267, 0);
            chkUseProductNumberFilter.Margin = new Padding(0);
            chkUseProductNumberFilter.Name = "chkUseProductNumberFilter";
            chkUseProductNumberFilter.Padding = new Padding(8, 0, 0, 0);
            chkUseProductNumberFilter.Size = new Size(268, 45);
            chkUseProductNumberFilter.TabIndex = 0;
            chkUseProductNumberFilter.Text = "Use product number filter";
            // 
            // chkShowTestFlagInHistory
            // 
            chkShowTestFlagInHistory.Checked = true;
            chkShowTestFlagInHistory.CheckState = CheckState.Checked;
            chkShowTestFlagInHistory.Dock = DockStyle.Fill;
            chkShowTestFlagInHistory.Location = new Point(0, 0);
            chkShowTestFlagInHistory.Margin = new Padding(0);
            chkShowTestFlagInHistory.Name = "chkShowTestFlagInHistory";
            chkShowTestFlagInHistory.Padding = new Padding(8, 0, 0, 0);
            chkShowTestFlagInHistory.Size = new Size(267, 45);
            chkShowTestFlagInHistory.TabIndex = 4;
            chkShowTestFlagInHistory.Text = "产品历史显示试焊件";
            // 
            // tlpMesWorkOrderRoute
            // 
            tlpMesWorkOrderRoute.ColumnCount = 2;
            tlpMesWorkOrderRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tlpMesWorkOrderRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesWorkOrderRoute.Controls.Add(lblMesWorkOrderRoute, 0, 0);
            tlpMesWorkOrderRoute.Controls.Add(inputMesWorkOrderRoute, 1, 0);
            tlpMesWorkOrderRoute.Dock = DockStyle.Fill;
            tlpMesWorkOrderRoute.Location = new Point(0, 270);
            tlpMesWorkOrderRoute.Margin = new Padding(0);
            tlpMesWorkOrderRoute.Name = "tlpMesWorkOrderRoute";
            tlpMesWorkOrderRoute.RowCount = 1;
            tlpMesWorkOrderRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesWorkOrderRoute.Size = new Size(535, 45);
            tlpMesWorkOrderRoute.TabIndex = 9;
            // 
            // lblMesWorkOrderRoute
            // 
            lblMesWorkOrderRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesWorkOrderRoute.Dock = DockStyle.Fill;
            lblMesWorkOrderRoute.Location = new Point(0, 0);
            lblMesWorkOrderRoute.Margin = new Padding(0);
            lblMesWorkOrderRoute.Name = "lblMesWorkOrderRoute";
            lblMesWorkOrderRoute.Padding = new Padding(10, 0, 0, 0);
            lblMesWorkOrderRoute.Size = new Size(105, 45);
            lblMesWorkOrderRoute.TabIndex = 0;
            lblMesWorkOrderRoute.Text = "工单信息路由";
            // 
            // inputMesWorkOrderRoute
            // 
            inputMesWorkOrderRoute.Dock = DockStyle.Fill;
            inputMesWorkOrderRoute.Location = new Point(150, 0);
            inputMesWorkOrderRoute.Margin = new Padding(0);
            inputMesWorkOrderRoute.Name = "inputMesWorkOrderRoute";
            inputMesWorkOrderRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesWorkOrderRoute.Size = new Size(385, 45);
            inputMesWorkOrderRoute.TabIndex = 1;
            inputMesWorkOrderRoute.Text = "api/ItemsOfBatchTech";
            // 
            // tlpMesServerTimeRoute
            // 
            tlpMesServerTimeRoute.ColumnCount = 2;
            tlpMesServerTimeRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tlpMesServerTimeRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesServerTimeRoute.Controls.Add(lblMesServerTimeRoute, 0, 0);
            tlpMesServerTimeRoute.Controls.Add(inputMesServerTimeRoute, 1, 0);
            tlpMesServerTimeRoute.Dock = DockStyle.Fill;
            tlpMesServerTimeRoute.Location = new Point(0, 315);
            tlpMesServerTimeRoute.Margin = new Padding(0);
            tlpMesServerTimeRoute.Name = "tlpMesServerTimeRoute";
            tlpMesServerTimeRoute.RowCount = 1;
            tlpMesServerTimeRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesServerTimeRoute.Size = new Size(535, 45);
            tlpMesServerTimeRoute.TabIndex = 10;
            // 
            // lblMesServerTimeRoute
            // 
            lblMesServerTimeRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesServerTimeRoute.Dock = DockStyle.Fill;
            lblMesServerTimeRoute.Location = new Point(0, 0);
            lblMesServerTimeRoute.Margin = new Padding(0);
            lblMesServerTimeRoute.Name = "lblMesServerTimeRoute";
            lblMesServerTimeRoute.Padding = new Padding(10, 0, 0, 0);
            lblMesServerTimeRoute.Size = new Size(105, 45);
            lblMesServerTimeRoute.TabIndex = 0;
            lblMesServerTimeRoute.Text = "设备校时路由";
            // 
            // inputMesServerTimeRoute
            // 
            inputMesServerTimeRoute.Dock = DockStyle.Fill;
            inputMesServerTimeRoute.Location = new Point(150, 0);
            inputMesServerTimeRoute.Margin = new Padding(0);
            inputMesServerTimeRoute.Name = "inputMesServerTimeRoute";
            inputMesServerTimeRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesServerTimeRoute.Size = new Size(385, 45);
            inputMesServerTimeRoute.TabIndex = 1;
            inputMesServerTimeRoute.Text = "api/ServerTime";
            // 
            // tlpPostDataHeader
            // 
            tlpPostDataHeader.ColumnCount = 4;
            tlpPostDataHeader.ColumnStyles.Add(new ColumnStyle());
            tlpPostDataHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tlpPostDataHeader.ColumnStyles.Add(new ColumnStyle());
            tlpPostDataHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tlpPostDataHeader.Controls.Add(inputPostDataHeaderValue, 3, 0);
            tlpPostDataHeader.Controls.Add(lblPostDataHeaderValue, 2, 0);
            tlpPostDataHeader.Controls.Add(inputPostDataHeaderKey, 1, 0);
            tlpPostDataHeader.Controls.Add(lblPostDataHeaderKey, 0, 0);
            tlpPostDataHeader.Dock = DockStyle.Fill;
            tlpPostDataHeader.Location = new Point(0, 180);
            tlpPostDataHeader.Margin = new Padding(0);
            tlpPostDataHeader.Name = "tlpPostDataHeader";
            tlpPostDataHeader.RowCount = 1;
            tlpPostDataHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpPostDataHeader.Size = new Size(535, 45);
            tlpPostDataHeader.TabIndex = 20;
            // 
            // inputPostDataHeaderValue
            // 
            inputPostDataHeaderValue.Dock = DockStyle.Fill;
            inputPostDataHeaderValue.Location = new Point(325, 0);
            inputPostDataHeaderValue.Margin = new Padding(0);
            inputPostDataHeaderValue.Name = "inputPostDataHeaderValue";
            inputPostDataHeaderValue.Padding = new Padding(2, 0, 0, 0);
            inputPostDataHeaderValue.Size = new Size(210, 45);
            inputPostDataHeaderValue.TabIndex = 1;
            // 
            // lblPostDataHeaderValue
            // 
            lblPostDataHeaderValue.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPostDataHeaderValue.Dock = DockStyle.Fill;
            lblPostDataHeaderValue.Location = new Point(225, 0);
            lblPostDataHeaderValue.Margin = new Padding(0);
            lblPostDataHeaderValue.Name = "lblPostDataHeaderValue";
            lblPostDataHeaderValue.Padding = new Padding(10, 0, 0, 0);
            lblPostDataHeaderValue.Size = new Size(100, 45);
            lblPostDataHeaderValue.TabIndex = 0;
            lblPostDataHeaderValue.Text = "Header Value";
            // 
            // inputPostDataHeaderKey
            // 
            inputPostDataHeaderKey.Dock = DockStyle.Fill;
            inputPostDataHeaderKey.Location = new Point(86, 0);
            inputPostDataHeaderKey.Margin = new Padding(0);
            inputPostDataHeaderKey.Name = "inputPostDataHeaderKey";
            inputPostDataHeaderKey.Padding = new Padding(2, 0, 0, 0);
            inputPostDataHeaderKey.Size = new Size(139, 45);
            inputPostDataHeaderKey.TabIndex = 1;
            // 
            // lblPostDataHeaderKey
            // 
            lblPostDataHeaderKey.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPostDataHeaderKey.Dock = DockStyle.Fill;
            lblPostDataHeaderKey.Location = new Point(0, 0);
            lblPostDataHeaderKey.Margin = new Padding(0);
            lblPostDataHeaderKey.Name = "lblPostDataHeaderKey";
            lblPostDataHeaderKey.Padding = new Padding(10, 0, 0, 0);
            lblPostDataHeaderKey.Size = new Size(86, 45);
            lblPostDataHeaderKey.TabIndex = 0;
            lblPostDataHeaderKey.Text = "Header Key";
            // 
            // tlpMesProgramManageRoute
            // 
            tlpMesProgramManageRoute.ColumnCount = 2;
            tlpMesProgramManageRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tlpMesProgramManageRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesProgramManageRoute.Controls.Add(lblMesProgramManageRoute, 0, 0);
            tlpMesProgramManageRoute.Controls.Add(inputMesProgramManageRoute, 1, 0);
            tlpMesProgramManageRoute.Dock = DockStyle.Fill;
            tlpMesProgramManageRoute.Location = new Point(0, 360);
            tlpMesProgramManageRoute.Margin = new Padding(0);
            tlpMesProgramManageRoute.Name = "tlpMesProgramManageRoute";
            tlpMesProgramManageRoute.RowCount = 1;
            tlpMesProgramManageRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesProgramManageRoute.Size = new Size(535, 45);
            tlpMesProgramManageRoute.TabIndex = 11;
            // 
            // lblMesProgramManageRoute
            // 
            lblMesProgramManageRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesProgramManageRoute.Dock = DockStyle.Fill;
            lblMesProgramManageRoute.Location = new Point(0, 0);
            lblMesProgramManageRoute.Margin = new Padding(0);
            lblMesProgramManageRoute.Name = "lblMesProgramManageRoute";
            lblMesProgramManageRoute.Padding = new Padding(10, 0, 0, 0);
            lblMesProgramManageRoute.Size = new Size(105, 45);
            lblMesProgramManageRoute.TabIndex = 0;
            lblMesProgramManageRoute.Text = "程序管理路由";
            // 
            // inputMesProgramManageRoute
            // 
            inputMesProgramManageRoute.Dock = DockStyle.Fill;
            inputMesProgramManageRoute.Location = new Point(150, 0);
            inputMesProgramManageRoute.Margin = new Padding(0);
            inputMesProgramManageRoute.Name = "inputMesProgramManageRoute";
            inputMesProgramManageRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesProgramManageRoute.Size = new Size(385, 45);
            inputMesProgramManageRoute.TabIndex = 1;
            inputMesProgramManageRoute.Text = "api/ExpProgram";
            // 
            // tlpMesStartWorkRoute
            // 
            tlpMesStartWorkRoute.ColumnCount = 2;
            tlpMesStartWorkRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tlpMesStartWorkRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesStartWorkRoute.Controls.Add(lblMesStartWorkRoute, 0, 0);
            tlpMesStartWorkRoute.Controls.Add(inputMesStartWorkRoute, 1, 0);
            tlpMesStartWorkRoute.Dock = DockStyle.Fill;
            tlpMesStartWorkRoute.Location = new Point(0, 405);
            tlpMesStartWorkRoute.Margin = new Padding(0);
            tlpMesStartWorkRoute.Name = "tlpMesStartWorkRoute";
            tlpMesStartWorkRoute.RowCount = 1;
            tlpMesStartWorkRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesStartWorkRoute.Size = new Size(535, 45);
            tlpMesStartWorkRoute.TabIndex = 12;
            // 
            // lblMesStartWorkRoute
            // 
            lblMesStartWorkRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesStartWorkRoute.Dock = DockStyle.Fill;
            lblMesStartWorkRoute.Location = new Point(0, 0);
            lblMesStartWorkRoute.Margin = new Padding(0);
            lblMesStartWorkRoute.Name = "lblMesStartWorkRoute";
            lblMesStartWorkRoute.Padding = new Padding(10, 0, 0, 0);
            lblMesStartWorkRoute.Size = new Size(105, 45);
            lblMesStartWorkRoute.TabIndex = 0;
            lblMesStartWorkRoute.Text = "开工上报路由";
            // 
            // inputMesStartWorkRoute
            // 
            inputMesStartWorkRoute.Dock = DockStyle.Fill;
            inputMesStartWorkRoute.Location = new Point(150, 0);
            inputMesStartWorkRoute.Margin = new Padding(0);
            inputMesStartWorkRoute.Name = "inputMesStartWorkRoute";
            inputMesStartWorkRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesStartWorkRoute.Size = new Size(385, 45);
            inputMesStartWorkRoute.TabIndex = 1;
            inputMesStartWorkRoute.Text = "api/ExpStartV2";
            // 
            // tlpMesWorkStatusRoute
            // 
            tlpMesWorkStatusRoute.ColumnCount = 2;
            tlpMesWorkStatusRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tlpMesWorkStatusRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesWorkStatusRoute.Controls.Add(lblMesWorkStatusRoute, 0, 0);
            tlpMesWorkStatusRoute.Controls.Add(inputMesWorkStatusRoute, 1, 0);
            tlpMesWorkStatusRoute.Dock = DockStyle.Fill;
            tlpMesWorkStatusRoute.Location = new Point(0, 450);
            tlpMesWorkStatusRoute.Margin = new Padding(0);
            tlpMesWorkStatusRoute.Name = "tlpMesWorkStatusRoute";
            tlpMesWorkStatusRoute.RowCount = 1;
            tlpMesWorkStatusRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesWorkStatusRoute.Size = new Size(535, 45);
            tlpMesWorkStatusRoute.TabIndex = 13;
            // 
            // lblMesWorkStatusRoute
            // 
            lblMesWorkStatusRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesWorkStatusRoute.Dock = DockStyle.Fill;
            lblMesWorkStatusRoute.Location = new Point(0, 0);
            lblMesWorkStatusRoute.Margin = new Padding(0);
            lblMesWorkStatusRoute.Name = "lblMesWorkStatusRoute";
            lblMesWorkStatusRoute.Padding = new Padding(10, 0, 0, 0);
            lblMesWorkStatusRoute.Size = new Size(105, 45);
            lblMesWorkStatusRoute.TabIndex = 0;
            lblMesWorkStatusRoute.Text = "工单状态路由";
            // 
            // inputMesWorkStatusRoute
            // 
            inputMesWorkStatusRoute.Dock = DockStyle.Fill;
            inputMesWorkStatusRoute.Location = new Point(150, 0);
            inputMesWorkStatusRoute.Margin = new Padding(0);
            inputMesWorkStatusRoute.Name = "inputMesWorkStatusRoute";
            inputMesWorkStatusRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesWorkStatusRoute.Size = new Size(385, 45);
            inputMesWorkStatusRoute.TabIndex = 1;
            inputMesWorkStatusRoute.Text = "api/ExpStatus";
            // 
            // tlpMesEndWorkRoute
            // 
            tlpMesEndWorkRoute.ColumnCount = 2;
            tlpMesEndWorkRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tlpMesEndWorkRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesEndWorkRoute.Controls.Add(lblMesEndWorkRoute, 0, 0);
            tlpMesEndWorkRoute.Controls.Add(inputMesEndWorkRoute, 1, 0);
            tlpMesEndWorkRoute.Dock = DockStyle.Fill;
            tlpMesEndWorkRoute.Location = new Point(0, 495);
            tlpMesEndWorkRoute.Margin = new Padding(0);
            tlpMesEndWorkRoute.Name = "tlpMesEndWorkRoute";
            tlpMesEndWorkRoute.RowCount = 1;
            tlpMesEndWorkRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesEndWorkRoute.Size = new Size(535, 45);
            tlpMesEndWorkRoute.TabIndex = 14;
            // 
            // lblMesEndWorkRoute
            // 
            lblMesEndWorkRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesEndWorkRoute.Dock = DockStyle.Fill;
            lblMesEndWorkRoute.Location = new Point(0, 0);
            lblMesEndWorkRoute.Margin = new Padding(0);
            lblMesEndWorkRoute.Name = "lblMesEndWorkRoute";
            lblMesEndWorkRoute.Padding = new Padding(10, 0, 0, 0);
            lblMesEndWorkRoute.Size = new Size(105, 45);
            lblMesEndWorkRoute.TabIndex = 0;
            lblMesEndWorkRoute.Text = "完工上报路由";
            // 
            // inputMesEndWorkRoute
            // 
            inputMesEndWorkRoute.Dock = DockStyle.Fill;
            inputMesEndWorkRoute.Location = new Point(150, 0);
            inputMesEndWorkRoute.Margin = new Padding(0);
            inputMesEndWorkRoute.Name = "inputMesEndWorkRoute";
            inputMesEndWorkRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesEndWorkRoute.Size = new Size(385, 45);
            inputMesEndWorkRoute.TabIndex = 1;
            inputMesEndWorkRoute.Text = "api/ExpEnd";
            // 
            // tlpMesReportFileRoute
            // 
            tlpMesReportFileRoute.ColumnCount = 2;
            tlpMesReportFileRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tlpMesReportFileRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesReportFileRoute.Controls.Add(lblMesReportFileRoute, 0, 0);
            tlpMesReportFileRoute.Controls.Add(inputMesReportFileRoute, 1, 0);
            tlpMesReportFileRoute.Dock = DockStyle.Fill;
            tlpMesReportFileRoute.Location = new Point(0, 540);
            tlpMesReportFileRoute.Margin = new Padding(0);
            tlpMesReportFileRoute.Name = "tlpMesReportFileRoute";
            tlpMesReportFileRoute.RowCount = 1;
            tlpMesReportFileRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesReportFileRoute.Size = new Size(535, 45);
            tlpMesReportFileRoute.TabIndex = 15;
            // 
            // lblMesReportFileRoute
            // 
            lblMesReportFileRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesReportFileRoute.Dock = DockStyle.Fill;
            lblMesReportFileRoute.Location = new Point(0, 0);
            lblMesReportFileRoute.Margin = new Padding(0);
            lblMesReportFileRoute.Name = "lblMesReportFileRoute";
            lblMesReportFileRoute.Padding = new Padding(10, 0, 0, 0);
            lblMesReportFileRoute.Size = new Size(105, 45);
            lblMesReportFileRoute.TabIndex = 0;
            lblMesReportFileRoute.Text = "报告文件路由";
            // 
            // inputMesReportFileRoute
            // 
            inputMesReportFileRoute.Dock = DockStyle.Fill;
            inputMesReportFileRoute.Location = new Point(150, 0);
            inputMesReportFileRoute.Margin = new Padding(0);
            inputMesReportFileRoute.Name = "inputMesReportFileRoute";
            inputMesReportFileRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesReportFileRoute.Size = new Size(385, 45);
            inputMesReportFileRoute.TabIndex = 1;
            inputMesReportFileRoute.Text = "api/ExpFile";
            // 
            // tlpMesPostDataRoute
            // 
            tlpMesPostDataRoute.ColumnCount = 2;
            tlpMesPostDataRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tlpMesPostDataRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesPostDataRoute.Controls.Add(lblMesPostDataRoute, 0, 0);
            tlpMesPostDataRoute.Controls.Add(inputMesPostDataRoute, 1, 0);
            tlpMesPostDataRoute.Dock = DockStyle.Fill;
            tlpMesPostDataRoute.Location = new Point(0, 585);
            tlpMesPostDataRoute.Margin = new Padding(0);
            tlpMesPostDataRoute.Name = "tlpMesPostDataRoute";
            tlpMesPostDataRoute.RowCount = 1;
            tlpMesPostDataRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesPostDataRoute.Size = new Size(535, 45);
            tlpMesPostDataRoute.TabIndex = 16;
            // 
            // lblMesPostDataRoute
            // 
            lblMesPostDataRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesPostDataRoute.Dock = DockStyle.Fill;
            lblMesPostDataRoute.Location = new Point(0, 0);
            lblMesPostDataRoute.Margin = new Padding(0);
            lblMesPostDataRoute.Name = "lblMesPostDataRoute";
            lblMesPostDataRoute.Padding = new Padding(10, 0, 0, 0);
            lblMesPostDataRoute.Size = new Size(105, 45);
            lblMesPostDataRoute.TabIndex = 0;
            lblMesPostDataRoute.Text = "采集参数路由";
            // 
            // inputMesPostDataRoute
            // 
            inputMesPostDataRoute.Dock = DockStyle.Fill;
            inputMesPostDataRoute.Location = new Point(150, 0);
            inputMesPostDataRoute.Margin = new Padding(0);
            inputMesPostDataRoute.Name = "inputMesPostDataRoute";
            inputMesPostDataRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesPostDataRoute.Size = new Size(385, 45);
            inputMesPostDataRoute.TabIndex = 1;
            inputMesPostDataRoute.Text = "api/PostData";
            // 
            // tlpMesDeviceRoute
            // 
            tlpMesDeviceRoute.ColumnCount = 2;
            tlpMesDeviceRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tlpMesDeviceRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesDeviceRoute.Controls.Add(lblMesDeviceRoute, 0, 0);
            tlpMesDeviceRoute.Controls.Add(inputMesDeviceRoute, 1, 0);
            tlpMesDeviceRoute.Dock = DockStyle.Fill;
            tlpMesDeviceRoute.Location = new Point(0, 630);
            tlpMesDeviceRoute.Margin = new Padding(0);
            tlpMesDeviceRoute.Name = "tlpMesDeviceRoute";
            tlpMesDeviceRoute.RowCount = 1;
            tlpMesDeviceRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesDeviceRoute.Size = new Size(535, 45);
            tlpMesDeviceRoute.TabIndex = 17;
            // 
            // lblMesDeviceRoute
            // 
            lblMesDeviceRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesDeviceRoute.Dock = DockStyle.Fill;
            lblMesDeviceRoute.Location = new Point(0, 0);
            lblMesDeviceRoute.Margin = new Padding(0);
            lblMesDeviceRoute.Name = "lblMesDeviceRoute";
            lblMesDeviceRoute.Padding = new Padding(10, 0, 0, 0);
            lblMesDeviceRoute.Size = new Size(105, 45);
            lblMesDeviceRoute.TabIndex = 0;
            lblMesDeviceRoute.Text = "设备编号路由";
            // 
            // inputMesDeviceRoute
            // 
            inputMesDeviceRoute.Dock = DockStyle.Fill;
            inputMesDeviceRoute.Location = new Point(150, 0);
            inputMesDeviceRoute.Margin = new Padding(0);
            inputMesDeviceRoute.Name = "inputMesDeviceRoute";
            inputMesDeviceRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesDeviceRoute.Size = new Size(385, 45);
            inputMesDeviceRoute.TabIndex = 1;
            inputMesDeviceRoute.Text = "api/Device";
            // 
            // tlpMesDeviceStatusRoute
            // 
            tlpMesDeviceStatusRoute.ColumnCount = 2;
            tlpMesDeviceStatusRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tlpMesDeviceStatusRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesDeviceStatusRoute.Controls.Add(lblMesDeviceStatusRoute, 0, 0);
            tlpMesDeviceStatusRoute.Controls.Add(inputMesDeviceStatusRoute, 1, 0);
            tlpMesDeviceStatusRoute.Dock = DockStyle.Fill;
            tlpMesDeviceStatusRoute.Location = new Point(0, 675);
            tlpMesDeviceStatusRoute.Margin = new Padding(0);
            tlpMesDeviceStatusRoute.Name = "tlpMesDeviceStatusRoute";
            tlpMesDeviceStatusRoute.RowCount = 1;
            tlpMesDeviceStatusRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesDeviceStatusRoute.Size = new Size(535, 45);
            tlpMesDeviceStatusRoute.TabIndex = 18;
            // 
            // lblMesDeviceStatusRoute
            // 
            lblMesDeviceStatusRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesDeviceStatusRoute.Dock = DockStyle.Fill;
            lblMesDeviceStatusRoute.Location = new Point(0, 0);
            lblMesDeviceStatusRoute.Margin = new Padding(0);
            lblMesDeviceStatusRoute.Name = "lblMesDeviceStatusRoute";
            lblMesDeviceStatusRoute.Padding = new Padding(10, 0, 0, 0);
            lblMesDeviceStatusRoute.Size = new Size(105, 45);
            lblMesDeviceStatusRoute.TabIndex = 0;
            lblMesDeviceStatusRoute.Text = "设备状态路由";
            // 
            // inputMesDeviceStatusRoute
            // 
            inputMesDeviceStatusRoute.Dock = DockStyle.Fill;
            inputMesDeviceStatusRoute.Location = new Point(150, 0);
            inputMesDeviceStatusRoute.Margin = new Padding(0);
            inputMesDeviceStatusRoute.Name = "inputMesDeviceStatusRoute";
            inputMesDeviceStatusRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesDeviceStatusRoute.Size = new Size(385, 45);
            inputMesDeviceStatusRoute.TabIndex = 1;
            inputMesDeviceStatusRoute.Text = "api/DeviceStatusV2";
            // 
            // grpProductionConfig
            // 
            grpProductionConfig.Controls.Add(tlpProductConfig);
            grpProductionConfig.Location = new Point(780, 5);
            grpProductionConfig.Margin = new Padding(0);
            grpProductionConfig.Name = "grpProductionConfig";
            grpProductionConfig.Size = new Size(541, 255);
            grpProductionConfig.TabIndex = 4;
            grpProductionConfig.TabStop = false;
            grpProductionConfig.Text = "生产配置";
            // 
            // tlpProductConfig
            // 
            tlpProductConfig.ColumnCount = 2;
            tlpProductConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpProductConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpProductConfig.Controls.Add(stationDisplayNameLayout, 0, 2);
            tlpProductConfig.Controls.Add(tlpUploadConfig, 0, 3);
            tlpProductConfig.Controls.Add(chkEnableDualStation, 0, 0);
            tlpProductConfig.Controls.Add(chkUseOperatorInputDialog, 1, 0);
            tlpProductConfig.Controls.Add(chkValidateRecipeBeforeStart, 0, 1);
            tlpProductConfig.Controls.Add(chkEnableFinishExpQtyPrompt, 1, 1);
            tlpProductConfig.Controls.Add(tableLayoutPanelHeartbeat, 0, 4);
            tlpProductConfig.Dock = DockStyle.Fill;
            tlpProductConfig.Location = new Point(3, 23);
            tlpProductConfig.Name = "tlpProductConfig";
            tlpProductConfig.RowCount = 5;
            tlpProductConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tlpProductConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tlpProductConfig.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpProductConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tlpProductConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tlpProductConfig.Size = new Size(535, 229);
            tlpProductConfig.TabIndex = 0;
            // 
            // stationDisplayNameLayout
            // 
            stationDisplayNameLayout.ColumnCount = 4;
            tlpProductConfig.SetColumnSpan(stationDisplayNameLayout, 2);
            stationDisplayNameLayout.ColumnStyles.Add(new ColumnStyle());
            stationDisplayNameLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            stationDisplayNameLayout.ColumnStyles.Add(new ColumnStyle());
            stationDisplayNameLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            stationDisplayNameLayout.Controls.Add(lblStation1DisplayName, 0, 0);
            stationDisplayNameLayout.Controls.Add(inputStation1DisplayName, 1, 0);
            stationDisplayNameLayout.Controls.Add(lblStation2DisplayName, 2, 0);
            stationDisplayNameLayout.Controls.Add(inputStation2DisplayName, 3, 0);
            stationDisplayNameLayout.Dock = DockStyle.Fill;
            stationDisplayNameLayout.Location = new Point(0, 90);
            stationDisplayNameLayout.Margin = new Padding(0);
            stationDisplayNameLayout.Name = "stationDisplayNameLayout";
            stationDisplayNameLayout.RowCount = 1;
            stationDisplayNameLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            stationDisplayNameLayout.Size = new Size(535, 45);
            stationDisplayNameLayout.TabIndex = 7;
            // 
            // lblStation1DisplayName
            // 
            lblStation1DisplayName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblStation1DisplayName.Dock = DockStyle.Fill;
            lblStation1DisplayName.Location = new Point(0, 0);
            lblStation1DisplayName.Margin = new Padding(0);
            lblStation1DisplayName.Name = "lblStation1DisplayName";
            lblStation1DisplayName.Padding = new Padding(8, 0, 0, 0);
            lblStation1DisplayName.Size = new Size(119, 45);
            lblStation1DisplayName.TabIndex = 0;
            lblStation1DisplayName.Text = "工位 1 显示名称";
            // 
            // inputStation1DisplayName
            // 
            inputStation1DisplayName.Dock = DockStyle.Fill;
            inputStation1DisplayName.Location = new Point(119, 0);
            inputStation1DisplayName.Margin = new Padding(0);
            inputStation1DisplayName.Name = "inputStation1DisplayName";
            inputStation1DisplayName.Size = new Size(148, 45);
            inputStation1DisplayName.TabIndex = 1;
            inputStation1DisplayName.Text = "左";
            // 
            // lblStation2DisplayName
            // 
            lblStation2DisplayName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblStation2DisplayName.Dock = DockStyle.Fill;
            lblStation2DisplayName.Location = new Point(267, 0);
            lblStation2DisplayName.Margin = new Padding(0);
            lblStation2DisplayName.Name = "lblStation2DisplayName";
            lblStation2DisplayName.Padding = new Padding(8, 0, 0, 0);
            lblStation2DisplayName.Size = new Size(119, 45);
            lblStation2DisplayName.TabIndex = 2;
            lblStation2DisplayName.Text = "工位 2 显示名称";
            // 
            // inputStation2DisplayName
            // 
            inputStation2DisplayName.Dock = DockStyle.Fill;
            inputStation2DisplayName.Location = new Point(386, 0);
            inputStation2DisplayName.Margin = new Padding(0);
            inputStation2DisplayName.Name = "inputStation2DisplayName";
            inputStation2DisplayName.Size = new Size(149, 45);
            inputStation2DisplayName.TabIndex = 3;
            inputStation2DisplayName.Text = "右";
            // 
            // tlpUploadConfig
            // 
            tlpUploadConfig.ColumnCount = 4;
            tlpProductConfig.SetColumnSpan(tlpUploadConfig, 2);
            tlpUploadConfig.ColumnStyles.Add(new ColumnStyle());
            tlpUploadConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            tlpUploadConfig.ColumnStyles.Add(new ColumnStyle());
            tlpUploadConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tlpUploadConfig.Controls.Add(inputUploadBatchSize, 3, 0);
            tlpUploadConfig.Controls.Add(lblUploadBatchSize, 2, 0);
            tlpUploadConfig.Controls.Add(selectUploadMode, 1, 0);
            tlpUploadConfig.Controls.Add(lblUploadMode, 0, 0);
            tlpUploadConfig.Dock = DockStyle.Fill;
            tlpUploadConfig.Location = new Point(0, 135);
            tlpUploadConfig.Margin = new Padding(0);
            tlpUploadConfig.Name = "tlpUploadConfig";
            tlpUploadConfig.RowCount = 1;
            tlpUploadConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpUploadConfig.Size = new Size(535, 45);
            tlpUploadConfig.TabIndex = 7;
            // 
            // inputUploadBatchSize
            // 
            inputUploadBatchSize.Dock = DockStyle.Fill;
            inputUploadBatchSize.Location = new Point(417, 0);
            inputUploadBatchSize.Margin = new Padding(0);
            inputUploadBatchSize.Name = "inputUploadBatchSize";
            inputUploadBatchSize.Size = new Size(118, 45);
            inputUploadBatchSize.TabIndex = 6;
            inputUploadBatchSize.Text = "1";
            // 
            // lblUploadBatchSize
            // 
            lblUploadBatchSize.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblUploadBatchSize.Dock = DockStyle.Fill;
            lblUploadBatchSize.Location = new Point(346, 0);
            lblUploadBatchSize.Margin = new Padding(0);
            lblUploadBatchSize.Name = "lblUploadBatchSize";
            lblUploadBatchSize.Padding = new Padding(8, 0, 0, 0);
            lblUploadBatchSize.Size = new Size(71, 45);
            lblUploadBatchSize.TabIndex = 5;
            lblUploadBatchSize.Text = "上传数量";
            // 
            // selectUploadMode
            // 
            selectUploadMode.Dock = DockStyle.Fill;
            selectUploadMode.Location = new Point(71, 0);
            selectUploadMode.Margin = new Padding(0);
            selectUploadMode.MaxCount = 10;
            selectUploadMode.Name = "selectUploadMode";
            selectUploadMode.Size = new Size(275, 45);
            selectUploadMode.TabIndex = 4;
            // 
            // lblUploadMode
            // 
            lblUploadMode.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblUploadMode.Dock = DockStyle.Fill;
            lblUploadMode.Location = new Point(0, 0);
            lblUploadMode.Margin = new Padding(0);
            lblUploadMode.Name = "lblUploadMode";
            lblUploadMode.Padding = new Padding(8, 0, 0, 0);
            lblUploadMode.Size = new Size(71, 45);
            lblUploadMode.TabIndex = 3;
            lblUploadMode.Text = "上传模式";
            // 
            // chkEnableDualStation
            // 
            chkEnableDualStation.Dock = DockStyle.Fill;
            chkEnableDualStation.Location = new Point(0, 0);
            chkEnableDualStation.Margin = new Padding(0);
            chkEnableDualStation.Name = "chkEnableDualStation";
            chkEnableDualStation.Size = new Size(267, 45);
            chkEnableDualStation.TabIndex = 0;
            chkEnableDualStation.Text = "启用双工位";
            // 
            // chkUseOperatorInputDialog
            // 
            chkUseOperatorInputDialog.Dock = DockStyle.Fill;
            chkUseOperatorInputDialog.Location = new Point(267, 0);
            chkUseOperatorInputDialog.Margin = new Padding(0);
            chkUseOperatorInputDialog.Name = "chkUseOperatorInputDialog";
            chkUseOperatorInputDialog.Padding = new Padding(8, 0, 0, 0);
            chkUseOperatorInputDialog.Size = new Size(268, 45);
            chkUseOperatorInputDialog.TabIndex = 1;
            chkUseOperatorInputDialog.Text = "Operator modal input";
            // 
            // chkValidateRecipeBeforeStart
            // 
            chkValidateRecipeBeforeStart.Dock = DockStyle.Fill;
            chkValidateRecipeBeforeStart.Location = new Point(0, 45);
            chkValidateRecipeBeforeStart.Margin = new Padding(0);
            chkValidateRecipeBeforeStart.Name = "chkValidateRecipeBeforeStart";
            chkValidateRecipeBeforeStart.Size = new Size(267, 45);
            chkValidateRecipeBeforeStart.TabIndex = 2;
            chkValidateRecipeBeforeStart.Text = "开工后校验配方";
            // 
            // chkEnableFinishExpQtyPrompt
            // 
            chkEnableFinishExpQtyPrompt.Dock = DockStyle.Fill;
            chkEnableFinishExpQtyPrompt.Location = new Point(267, 45);
            chkEnableFinishExpQtyPrompt.Margin = new Padding(0);
            chkEnableFinishExpQtyPrompt.Name = "chkEnableFinishExpQtyPrompt";
            chkEnableFinishExpQtyPrompt.Padding = new Padding(8, 0, 0, 0);
            chkEnableFinishExpQtyPrompt.Size = new Size(268, 45);
            chkEnableFinishExpQtyPrompt.TabIndex = 3;
            chkEnableFinishExpQtyPrompt.Text = "启用完工实际数量输入弹窗";
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
            tableLayoutPanelHeartbeat.Location = new Point(0, 180);
            tableLayoutPanelHeartbeat.Margin = new Padding(0);
            tableLayoutPanelHeartbeat.Name = "tableLayoutPanelHeartbeat";
            tableLayoutPanelHeartbeat.RowCount = 1;
            tableLayoutPanelHeartbeat.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelHeartbeat.Size = new Size(535, 49);
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
            lblPlcHeartbeatInterval.Size = new Size(155, 49);
            lblPlcHeartbeatInterval.TabIndex = 0;
            lblPlcHeartbeatInterval.Text = "PLC心跳监测频率(ms)";
            // 
            // inputPlcHeartbeatInterval
            // 
            inputPlcHeartbeatInterval.Dock = DockStyle.Fill;
            inputPlcHeartbeatInterval.Location = new Point(155, 0);
            inputPlcHeartbeatInterval.Margin = new Padding(0);
            inputPlcHeartbeatInterval.Name = "inputPlcHeartbeatInterval";
            inputPlcHeartbeatInterval.Size = new Size(380, 49);
            inputPlcHeartbeatInterval.TabIndex = 1;
            inputPlcHeartbeatInterval.Text = "300";
            // 
            // grpDeviceConfig
            // 
            grpDeviceConfig.Controls.Add(layoutDeviceNoConfig);
            grpDeviceConfig.Location = new Point(317, 4);
            grpDeviceConfig.Margin = new Padding(0);
            grpDeviceConfig.Name = "grpDeviceConfig";
            grpDeviceConfig.Size = new Size(455, 211);
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
            layoutDeviceNoConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            layoutDeviceNoConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            layoutDeviceNoConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            layoutDeviceNoConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            layoutDeviceNoConfig.Size = new Size(449, 185);
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
            tlpDeviceId.Size = new Size(449, 45);
            tlpDeviceId.TabIndex = 0;
            // 
            // lblDeviceId
            // 
            lblDeviceId.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceId.Dock = DockStyle.Fill;
            lblDeviceId.Location = new Point(0, 0);
            lblDeviceId.Margin = new Padding(0);
            lblDeviceId.Name = "lblDeviceId";
            lblDeviceId.Padding = new Padding(8, 0, 0, 0);
            lblDeviceId.Size = new Size(71, 45);
            lblDeviceId.TabIndex = 0;
            lblDeviceId.Text = "设备编号";
            // 
            // input_DeviceID
            // 
            input_DeviceID.Dock = DockStyle.Fill;
            input_DeviceID.Location = new Point(71, 0);
            input_DeviceID.Margin = new Padding(0);
            input_DeviceID.Name = "input_DeviceID";
            input_DeviceID.Size = new Size(297, 45);
            input_DeviceID.TabIndex = 1;
            // 
            // btnSyncDevice
            // 
            btnSyncDevice.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSyncDevice.BorderWidth = 1F;
            btnSyncDevice.Dock = DockStyle.Fill;
            btnSyncDevice.IconSvg = "CloudUploadOutlined";
            btnSyncDevice.Location = new Point(368, 0);
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
            tlpDeviceName.Location = new Point(0, 45);
            tlpDeviceName.Margin = new Padding(0);
            tlpDeviceName.Name = "tlpDeviceName";
            tlpDeviceName.RowCount = 1;
            tlpDeviceName.RowStyles.Add(new RowStyle());
            tlpDeviceName.Size = new Size(449, 45);
            tlpDeviceName.TabIndex = 1;
            // 
            // lblDeviceName
            // 
            lblDeviceName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceName.Dock = DockStyle.Fill;
            lblDeviceName.Location = new Point(0, 0);
            lblDeviceName.Margin = new Padding(0);
            lblDeviceName.Name = "lblDeviceName";
            lblDeviceName.Padding = new Padding(8, 0, 0, 0);
            lblDeviceName.Size = new Size(71, 45);
            lblDeviceName.TabIndex = 0;
            lblDeviceName.Text = "设备名称";
            // 
            // input_DeviceName
            // 
            input_DeviceName.Dock = DockStyle.Fill;
            input_DeviceName.Location = new Point(71, 0);
            input_DeviceName.Margin = new Padding(0);
            input_DeviceName.Name = "input_DeviceName";
            input_DeviceName.Size = new Size(378, 45);
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
            tlpDeviveUrl.Location = new Point(0, 135);
            tlpDeviveUrl.Margin = new Padding(0);
            tlpDeviveUrl.Name = "tlpDeviveUrl";
            tlpDeviveUrl.RowCount = 1;
            tlpDeviveUrl.RowStyles.Add(new RowStyle());
            tlpDeviveUrl.Size = new Size(449, 50);
            tlpDeviveUrl.TabIndex = 2;
            // 
            // lblDeviceUrl
            // 
            lblDeviceUrl.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceUrl.Dock = DockStyle.Fill;
            lblDeviceUrl.Location = new Point(0, 0);
            lblDeviceUrl.Margin = new Padding(0);
            lblDeviceUrl.Name = "lblDeviceUrl";
            lblDeviceUrl.Padding = new Padding(8, 0, 0, 0);
            lblDeviceUrl.Size = new Size(71, 50);
            lblDeviceUrl.TabIndex = 0;
            lblDeviceUrl.Text = "状态地址";
            // 
            // input_DeviceUrl
            // 
            input_DeviceUrl.Dock = DockStyle.Fill;
            input_DeviceUrl.Location = new Point(71, 0);
            input_DeviceUrl.Margin = new Padding(0);
            input_DeviceUrl.Name = "input_DeviceUrl";
            input_DeviceUrl.Size = new Size(378, 50);
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
            tlpMesUrl.Location = new Point(0, 90);
            tlpMesUrl.Margin = new Padding(0);
            tlpMesUrl.Name = "tlpMesUrl";
            tlpMesUrl.RowCount = 1;
            tlpMesUrl.RowStyles.Add(new RowStyle());
            tlpMesUrl.Size = new Size(449, 45);
            tlpMesUrl.TabIndex = 3;
            // 
            // lblMesUrl
            // 
            lblMesUrl.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesUrl.Dock = DockStyle.Fill;
            lblMesUrl.Location = new Point(0, 0);
            lblMesUrl.Margin = new Padding(0);
            lblMesUrl.Name = "lblMesUrl";
            lblMesUrl.Padding = new Padding(8, 0, 0, 0);
            lblMesUrl.Size = new Size(69, 45);
            lblMesUrl.TabIndex = 0;
            lblMesUrl.Text = "MES地址";
            // 
            // input_BaseUrl
            // 
            input_BaseUrl.Dock = DockStyle.Fill;
            input_BaseUrl.Location = new Point(69, 0);
            input_BaseUrl.Margin = new Padding(0);
            input_BaseUrl.Name = "input_BaseUrl";
            input_BaseUrl.Padding = new Padding(2, 0, 0, 0);
            input_BaseUrl.Size = new Size(299, 45);
            input_BaseUrl.TabIndex = 1;
            // 
            // btnTestConnection
            // 
            btnTestConnection.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnTestConnection.BorderWidth = 1F;
            btnTestConnection.Dock = DockStyle.Fill;
            btnTestConnection.IconSvg = "ApiOutlined";
            btnTestConnection.Location = new Point(368, 0);
            btnTestConnection.Margin = new Padding(0);
            btnTestConnection.Name = "btnTestConnection";
            btnTestConnection.Size = new Size(81, 45);
            btnTestConnection.TabIndex = 2;
            btnTestConnection.Tag = "perm:button.system.test-mes:enabled";
            btnTestConnection.Text = "测试";
            // 
            // grpPlcConfig
            // 
            grpPlcConfig.Controls.Add(tlpPlcConfig);
            grpPlcConfig.Location = new Point(3, 4);
            grpPlcConfig.Margin = new Padding(0);
            grpPlcConfig.Name = "grpPlcConfig";
            grpPlcConfig.Size = new Size(308, 295);
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
            tlpPlcConfig.Controls.Add(chkEnablePlcAlarmReading, 0, 3);
            tlpPlcConfig.Controls.Add(tlpPlcStringNumericMode, 0, 5);
            tlpPlcConfig.Controls.Add(chkEnablePlcStringNumericFormatting, 0, 4);
            tlpPlcConfig.Dock = DockStyle.Fill;
            tlpPlcConfig.Location = new Point(3, 23);
            tlpPlcConfig.Name = "tlpPlcConfig";
            tlpPlcConfig.RowCount = 6;
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            tlpPlcConfig.Size = new Size(302, 269);
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
            tlpPlcIp.Size = new Size(302, 45);
            tlpPlcIp.TabIndex = 0;
            // 
            // lblPlcIp
            // 
            lblPlcIp.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcIp.Dock = DockStyle.Fill;
            lblPlcIp.Location = new Point(0, 0);
            lblPlcIp.Margin = new Padding(0);
            lblPlcIp.Name = "lblPlcIp";
            lblPlcIp.Padding = new Padding(8, 0, 0, 0);
            lblPlcIp.Size = new Size(21, 45);
            lblPlcIp.TabIndex = 0;
            lblPlcIp.Text = "IP";
            // 
            // input_PlcIp
            // 
            input_PlcIp.Dock = DockStyle.Fill;
            input_PlcIp.Location = new Point(21, 0);
            input_PlcIp.Margin = new Padding(0);
            input_PlcIp.Name = "input_PlcIp";
            input_PlcIp.Size = new Size(200, 45);
            input_PlcIp.TabIndex = 1;
            // 
            // btnConnectPlc
            // 
            btnConnectPlc.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnConnectPlc.BorderWidth = 1F;
            btnConnectPlc.Dock = DockStyle.Fill;
            btnConnectPlc.IconSvg = "ApiOutlined";
            btnConnectPlc.Location = new Point(221, 0);
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
            tlpPlcPort.Size = new Size(302, 45);
            tlpPlcPort.TabIndex = 1;
            // 
            // lblPlcPort
            // 
            lblPlcPort.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcPort.Dock = DockStyle.Fill;
            lblPlcPort.Location = new Point(0, 0);
            lblPlcPort.Margin = new Padding(0);
            lblPlcPort.Name = "lblPlcPort";
            lblPlcPort.Padding = new Padding(8, 0, 0, 0);
            lblPlcPort.Size = new Size(40, 45);
            lblPlcPort.TabIndex = 0;
            lblPlcPort.Text = "端口";
            // 
            // input_PlcPort
            // 
            input_PlcPort.Dock = DockStyle.Fill;
            input_PlcPort.Location = new Point(40, 0);
            input_PlcPort.Margin = new Padding(0);
            input_PlcPort.Name = "input_PlcPort";
            input_PlcPort.Size = new Size(262, 45);
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
            tableLayoutPanel7.Size = new Size(302, 45);
            tableLayoutPanel7.TabIndex = 2;
            // 
            // lblPlcType
            // 
            lblPlcType.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcType.Dock = DockStyle.Fill;
            lblPlcType.Location = new Point(0, 0);
            lblPlcType.Margin = new Padding(0);
            lblPlcType.Name = "lblPlcType";
            lblPlcType.Padding = new Padding(8, 0, 0, 0);
            lblPlcType.Size = new Size(40, 46);
            lblPlcType.TabIndex = 0;
            lblPlcType.Text = "类型";
            // 
            // select_PlcType
            // 
            select_PlcType.Dock = DockStyle.Fill;
            select_PlcType.Location = new Point(40, 0);
            select_PlcType.Margin = new Padding(0);
            select_PlcType.MaxCount = 10;
            select_PlcType.Name = "select_PlcType";
            select_PlcType.Size = new Size(262, 46);
            select_PlcType.TabIndex = 1;
            // 
            // chkEnablePlcAlarmReading
            // 
            chkEnablePlcAlarmReading.Checked = true;
            chkEnablePlcAlarmReading.CheckState = CheckState.Checked;
            chkEnablePlcAlarmReading.Dock = DockStyle.Fill;
            chkEnablePlcAlarmReading.Location = new Point(0, 135);
            chkEnablePlcAlarmReading.Margin = new Padding(0);
            chkEnablePlcAlarmReading.Name = "chkEnablePlcAlarmReading";
            chkEnablePlcAlarmReading.Padding = new Padding(8, 0, 0, 0);
            chkEnablePlcAlarmReading.Size = new Size(302, 45);
            chkEnablePlcAlarmReading.TabIndex = 5;
            chkEnablePlcAlarmReading.Text = "启用报警信息读取";
            // 
            // tlpPlcStringNumericMode
            // 
            tlpPlcStringNumericMode.AutoSize = true;
            tlpPlcStringNumericMode.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpPlcStringNumericMode.ColumnCount = 2;
            tlpPlcStringNumericMode.ColumnStyles.Add(new ColumnStyle());
            tlpPlcStringNumericMode.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpPlcStringNumericMode.Controls.Add(lblPlcStringNumericFormatMode, 0, 0);
            tlpPlcStringNumericMode.Controls.Add(selectPlcStringNumericFormatMode, 1, 0);
            tlpPlcStringNumericMode.Dock = DockStyle.Fill;
            tlpPlcStringNumericMode.Location = new Point(0, 225);
            tlpPlcStringNumericMode.Margin = new Padding(0);
            tlpPlcStringNumericMode.Name = "tlpPlcStringNumericMode";
            tlpPlcStringNumericMode.RowCount = 1;
            tlpPlcStringNumericMode.RowStyles.Add(new RowStyle());
            tlpPlcStringNumericMode.Size = new Size(302, 45);
            tlpPlcStringNumericMode.TabIndex = 4;
            // 
            // lblPlcStringNumericFormatMode
            // 
            lblPlcStringNumericFormatMode.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcStringNumericFormatMode.Dock = DockStyle.Fill;
            lblPlcStringNumericFormatMode.Location = new Point(0, 0);
            lblPlcStringNumericFormatMode.Margin = new Padding(0);
            lblPlcStringNumericFormatMode.Name = "lblPlcStringNumericFormatMode";
            lblPlcStringNumericFormatMode.Padding = new Padding(8, 0, 0, 0);
            lblPlcStringNumericFormatMode.Size = new Size(71, 45);
            lblPlcStringNumericFormatMode.TabIndex = 0;
            lblPlcStringNumericFormatMode.Text = "处理方式";
            // 
            // selectPlcStringNumericFormatMode
            // 
            selectPlcStringNumericFormatMode.Dock = DockStyle.Fill;
            selectPlcStringNumericFormatMode.Location = new Point(71, 0);
            selectPlcStringNumericFormatMode.Margin = new Padding(0);
            selectPlcStringNumericFormatMode.MaxCount = 10;
            selectPlcStringNumericFormatMode.Name = "selectPlcStringNumericFormatMode";
            selectPlcStringNumericFormatMode.Size = new Size(231, 45);
            selectPlcStringNumericFormatMode.TabIndex = 1;
            // 
            // chkEnablePlcStringNumericFormatting
            // 
            chkEnablePlcStringNumericFormatting.Checked = true;
            chkEnablePlcStringNumericFormatting.CheckState = CheckState.Checked;
            chkEnablePlcStringNumericFormatting.Dock = DockStyle.Fill;
            chkEnablePlcStringNumericFormatting.Location = new Point(0, 180);
            chkEnablePlcStringNumericFormatting.Margin = new Padding(0);
            chkEnablePlcStringNumericFormatting.Name = "chkEnablePlcStringNumericFormatting";
            chkEnablePlcStringNumericFormatting.Padding = new Padding(8, 0, 0, 0);
            chkEnablePlcStringNumericFormatting.Size = new Size(302, 45);
            chkEnablePlcStringNumericFormatting.TabIndex = 3;
            chkEnablePlcStringNumericFormatting.Text = "启用PLC字符串数值处理";
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
            grpCenterServerConfig.ResumeLayout(false);
            tableLayoutPanelCenterServer.ResumeLayout(false);
            tlpCenterServerBaseUrl.ResumeLayout(false);
            tlpCenterServerBaseUrl.PerformLayout();
            tlpCenterServerSystemType.ResumeLayout(false);
            tlpCenterServerSystemType.PerformLayout();
            tlpCenterServerHeartbeat.ResumeLayout(false);
            tlpCenterServerHeartbeat.PerformLayout();
            grpAppConfig.ResumeLayout(false);
            grpAppConfig.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            tableLayoutPanel4.ResumeLayout(false);
            tlpLogPath.ResumeLayout(false);
            tlpLogPath.PerformLayout();
            tlpDataPath.ResumeLayout(false);
            tlpDataPath.PerformLayout();
            tlpProgramFilePath.ResumeLayout(false);
            tlpProgramFilePath.PerformLayout();
            grpMesConfig.ResumeLayout(false);
            grpMesConfig.PerformLayout();
            tableLayoutPanelMesConfig.ResumeLayout(false);
            tlpCheckbox3.ResumeLayout(false);
            tlpCheckbox2.ResumeLayout(false);
            tlpProcessParameterType.ResumeLayout(false);
            tlpProcessParameterType.PerformLayout();
            tlpMesUserRoute.ResumeLayout(false);
            tlpMesUserRoute.PerformLayout();
            tlpCheckbox1.ResumeLayout(false);
            tlpMesWorkOrderRoute.ResumeLayout(false);
            tlpMesWorkOrderRoute.PerformLayout();
            tlpMesServerTimeRoute.ResumeLayout(false);
            tlpMesServerTimeRoute.PerformLayout();
            tlpPostDataHeader.ResumeLayout(false);
            tlpPostDataHeader.PerformLayout();
            tlpMesProgramManageRoute.ResumeLayout(false);
            tlpMesProgramManageRoute.PerformLayout();
            tlpMesStartWorkRoute.ResumeLayout(false);
            tlpMesStartWorkRoute.PerformLayout();
            tlpMesWorkStatusRoute.ResumeLayout(false);
            tlpMesWorkStatusRoute.PerformLayout();
            tlpMesEndWorkRoute.ResumeLayout(false);
            tlpMesEndWorkRoute.PerformLayout();
            tlpMesReportFileRoute.ResumeLayout(false);
            tlpMesReportFileRoute.PerformLayout();
            tlpMesPostDataRoute.ResumeLayout(false);
            tlpMesPostDataRoute.PerformLayout();
            tlpMesDeviceRoute.ResumeLayout(false);
            tlpMesDeviceRoute.PerformLayout();
            tlpMesDeviceStatusRoute.ResumeLayout(false);
            tlpMesDeviceStatusRoute.PerformLayout();
            grpProductionConfig.ResumeLayout(false);
            tlpProductConfig.ResumeLayout(false);
            stationDisplayNameLayout.ResumeLayout(false);
            stationDisplayNameLayout.PerformLayout();
            tlpUploadConfig.ResumeLayout(false);
            tlpUploadConfig.PerformLayout();
            tableLayoutPanelHeartbeat.ResumeLayout(false);
            tableLayoutPanelHeartbeat.PerformLayout();
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
            tlpPlcStringNumericMode.ResumeLayout(false);
            tlpPlcStringNumericMode.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel rootLayout;
        private GroupBox grpPlcConfig;
        private TableLayoutPanel tlpPlcConfig;
        private TableLayoutPanel tlpPlcIp;
        private AntdUI.Input input_PlcIp;
        private AntdUI.Button btnConnectPlc;
        private AntdUI.Label lblPlcIp;
        private TableLayoutPanel tableLayoutPanel7;
        private AntdUI.Select select_PlcType;
        private AntdUI.Label lblPlcType;
        private AntdUI.Checkbox chkEnablePlcStringNumericFormatting;
        //private AntdUI.Checkbox chkEnablePlcAlarmReading;
        private TableLayoutPanel tlpPlcStringNumericMode;
        private AntdUI.Label lblPlcStringNumericFormatMode;
        private AntdUI.Select selectPlcStringNumericFormatMode;
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
        private TableLayoutPanel tlpProgramFilePath;
        private AntdUI.Label lblProgramFilePath;
        private AntdUI.Button btnOpenProgramFilePath;
        private AntdUI.Input input_ProgramFilePath;
        private AntdUI.Button btnChangeProgramFilePath;
        private AntdUI.Checkbox chkEnableAutoStart;
        private AntdUI.Checkbox chkEnableElevatedAutoStart;
        private TableLayoutPanel tlpLogPath;
        private AntdUI.Label lblLogPath;
        private AntdUI.Button btnOpenLogPath;
        private AntdUI.Input input_LogsPath;
        private AntdUI.Button btnChangeLogPath;
        private AntdUI.Checkbox chkEnableDualStation;
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
        private AntdUI.Checkbox chkUseOperatorInputDialog;
        private TableLayoutPanel tableLayoutPanel1;
        private GroupBox grpAppConfig;
        private TableLayoutPanel tableLayoutPanel2;
        private TableLayoutPanel tlpCheckbox1;
        private AntdUI.Label lblMesTimeout;
        private AntdUI.InputNumber input_MesTimeout;
        private TableLayoutPanel tlpMesUserRoute;
        private AntdUI.Label lblMesUserRoute;
        private AntdUI.Input inputMesUserRoute;
        private TableLayoutPanel tlpMesWorkOrderRoute;
        private AntdUI.Label lblMesWorkOrderRoute;
        private AntdUI.Input inputMesWorkOrderRoute;
        private TableLayoutPanel tlpMesServerTimeRoute;
        private AntdUI.Label lblMesServerTimeRoute;
        private AntdUI.Input inputMesServerTimeRoute;
        private TableLayoutPanel tlpMesProgramManageRoute;
        private AntdUI.Label lblMesProgramManageRoute;
        private AntdUI.Input inputMesProgramManageRoute;
        private TableLayoutPanel tlpMesStartWorkRoute;
        private AntdUI.Label lblMesStartWorkRoute;
        private AntdUI.Input inputMesStartWorkRoute;
        private TableLayoutPanel tlpMesWorkStatusRoute;
        private AntdUI.Label lblMesWorkStatusRoute;
        private AntdUI.Input inputMesWorkStatusRoute;
        private TableLayoutPanel tlpMesEndWorkRoute;
        private AntdUI.Label lblMesEndWorkRoute;
        private AntdUI.Input inputMesEndWorkRoute;
        private TableLayoutPanel tlpMesReportFileRoute;
        private AntdUI.Label lblMesReportFileRoute;
        private AntdUI.Input inputMesReportFileRoute;
        private TableLayoutPanel tlpMesPostDataRoute;
        private AntdUI.Label lblMesPostDataRoute;
        private AntdUI.Input inputMesPostDataRoute;
        private AntdUI.Checkbox chkEnablePostDataCustomHeader;
        private TableLayoutPanel tlpPostDataHeader;
        private AntdUI.Label lblPostDataHeaderKey;
        private AntdUI.Input inputPostDataHeaderKey;
        private AntdUI.Label lblPostDataHeaderValue;
        private AntdUI.Input inputPostDataHeaderValue;
        private TableLayoutPanel tlpMesDeviceRoute;
        private AntdUI.Label lblMesDeviceRoute;
        private AntdUI.Input inputMesDeviceRoute;
        private TableLayoutPanel tlpMesDeviceStatusRoute;
        private AntdUI.Label lblMesDeviceStatusRoute;
        private AntdUI.Input inputMesDeviceStatusRoute;
        private AntdUI.Label lblProcessParameterDeviceType;
        private AntdUI.Select selectProcessParameterDeviceType;
        private AntdUI.Checkbox chkShowTestFlagInHistory;
        private AntdUI.Checkbox chkEnableDeviceStatusReport;
        private AntdUI.Checkbox chkEnableWorkOrderStatusReport;
        private GroupBox grpCenterServerConfig;
        private TableLayoutPanel tableLayoutPanelCenterServer;
        private AntdUI.Checkbox chkEnableCenterServerSync;
        private TableLayoutPanel tlpCenterServerBaseUrl;
        private AntdUI.Label lblCenterServerBaseUrl;
        private AntdUI.Input inputCenterServerBaseUrl;
        private TableLayoutPanel tlpCenterServerSystemType;
        private AntdUI.Label lblCenterServerSystemType;
        private AntdUI.Select selectCenterServerSystemType;
        private TableLayoutPanel tlpCenterServerHeartbeat;
        private AntdUI.Label lblCenterServerHeartbeatInterval;
        private AntdUI.Input inputCenterServerHeartbeatInterval;
        private AntdUI.Checkbox chkEnablePlcAlarmReading;
        private TableLayoutPanel tlpProcessParameterType;
        private TableLayoutPanel tlpCheckbox2;
        private TableLayoutPanel tlpCheckbox3;
        private TableLayoutPanel tableLayoutPanel4;
        private TableLayoutPanel tlpUploadConfig;
        private TableLayoutPanel stationDisplayNameLayout;
        private AntdUI.Label lblStation1DisplayName;
        private AntdUI.Input inputStation1DisplayName;
        private AntdUI.Label lblStation2DisplayName;
        private AntdUI.Input inputStation2DisplayName;
    }
}
