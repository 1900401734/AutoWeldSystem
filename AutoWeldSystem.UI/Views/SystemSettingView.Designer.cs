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
            basicSettingsViewport = new Panel();
            basicSettingsLayout = new TableLayoutPanel();
            leftSettingsColumn = new TableLayoutPanel();
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
            tlpPlcAlarmTriggerMode = new TableLayoutPanel();
            lblPlcAlarmTriggerMode = new AntdUI.Label();
            selectPlcAlarmTriggerMode = new AntdUI.Select();
            chkEnablePlcStringNumericFormatting = new AntdUI.Checkbox();
            tlpPlcStringNumericMode = new TableLayoutPanel();
            lblPlcStringNumericFormatMode = new AntdUI.Label();
            selectPlcStringNumericFormatMode = new AntdUI.Select();
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
            tlpMesDeviceStatusQueryRoute = new TableLayoutPanel();
            lblMesDeviceStatusQueryRoute = new AntdUI.Label();
            inputMesDeviceStatusQueryRoute = new AntdUI.Input();
            tlpMesDeviceIdSetRoute = new TableLayoutPanel();
            lblMesDeviceIdSetRoute = new AntdUI.Label();
            inputMesDeviceIdSetRoute = new AntdUI.Input();
            middleSettingsColumn = new TableLayoutPanel();
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
            lblPlcHeartbeatTimeout = new AntdUI.Label();
            inputPlcHeartbeatTimeout = new AntdUI.Input();
            lblPlcCommunicationTimeout = new AntdUI.Label();
            inputPlcCommunicationTimeout = new AntdUI.Input();
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
            rightSettingsColumn = new TableLayoutPanel();
            grpMesConfig = new GroupBox();
            tableLayoutPanelMesConfig = new TableLayoutPanel();
            tlpCheckbox2 = new TableLayoutPanel();
            chkEnablePostDataCustomHeader = new AntdUI.Checkbox();
            chkEnableWorkOrderStatusReport = new AntdUI.Checkbox();
            tlpProcessParameterType = new TableLayoutPanel();
            lblProcessParameterDeviceType = new AntdUI.Label();
            selectProcessParameterDeviceType = new AntdUI.Select();
            tlpInspectionResultSource = new TableLayoutPanel();
            lblInspectionResultSource = new AntdUI.Label();
            selectInspectionResultSource = new AntdUI.Select();
            tlpRealtimePointNumberSource = new TableLayoutPanel();
            lblRealtimePointNumberSource = new AntdUI.Label();
            selectRealtimePointNumberSource = new AntdUI.Select();
            tlpMesUserRoute = new TableLayoutPanel();
            lblMesUserRoute = new AntdUI.Label();
            inputMesUserRoute = new AntdUI.Input();
            tlpCheckbox1 = new TableLayoutPanel();
            chkShowTestFlagInHistory = new AntdUI.Checkbox();
            chkEnableDeviceStatusReport = new AntdUI.Checkbox();
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
            tlpMesSysRoute = new TableLayoutPanel();
            lblMesSysRoute = new AntdUI.Label();
            inputMesSysRoute = new AntdUI.Input();
            tlpMesHeartbeat = new TableLayoutPanel();
            input_MesTimeout = new AntdUI.InputNumber();
            lblMesHeartbeatInterval = new AntdUI.Label();
            lblMesTimeout = new AntdUI.Label();
            inputMesHeartbeatInterval = new AntdUI.Input();
            rootLayout.SuspendLayout();
            titleLayout.SuspendLayout();
            tabSettingCategories.SuspendLayout();
            tabBasicSettings.SuspendLayout();
            basicSettingsViewport.SuspendLayout();
            basicSettingsLayout.SuspendLayout();
            leftSettingsColumn.SuspendLayout();
            grpPlcConfig.SuspendLayout();
            tlpPlcConfig.SuspendLayout();
            tlpPlcIp.SuspendLayout();
            tlpPlcPort.SuspendLayout();
            tableLayoutPanel7.SuspendLayout();
            tlpPlcAlarmTriggerMode.SuspendLayout();
            tlpPlcStringNumericMode.SuspendLayout();
            grpDeviceConfig.SuspendLayout();
            layoutDeviceNoConfig.SuspendLayout();
            tlpDeviceId.SuspendLayout();
            tlpDeviceName.SuspendLayout();
            tlpDeviveUrl.SuspendLayout();
            tlpMesUrl.SuspendLayout();
            tlpMesDeviceStatusQueryRoute.SuspendLayout();
            tlpMesDeviceIdSetRoute.SuspendLayout();
            middleSettingsColumn.SuspendLayout();
            grpProductionConfig.SuspendLayout();
            tlpProductConfig.SuspendLayout();
            stationDisplayNameLayout.SuspendLayout();
            tlpUploadConfig.SuspendLayout();
            tableLayoutPanelHeartbeat.SuspendLayout();
            grpAppConfig.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            tlpLogPath.SuspendLayout();
            tlpDataPath.SuspendLayout();
            grpCenterServerConfig.SuspendLayout();
            tableLayoutPanelCenterServer.SuspendLayout();
            tlpCenterServerBaseUrl.SuspendLayout();
            tlpCenterServerSystemType.SuspendLayout();
            tlpCenterServerHeartbeat.SuspendLayout();
            rightSettingsColumn.SuspendLayout();
            grpMesConfig.SuspendLayout();
            tableLayoutPanelMesConfig.SuspendLayout();
            tlpCheckbox2.SuspendLayout();
            tlpProcessParameterType.SuspendLayout();
            tlpInspectionResultSource.SuspendLayout();
            tlpRealtimePointNumberSource.SuspendLayout();
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
            tlpMesSysRoute.SuspendLayout();
            tlpMesHeartbeat.SuspendLayout();
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
            rootLayout.Margin = new Padding(2, 3, 2, 3);
            rootLayout.Name = "rootLayout";
            rootLayout.RowCount = 2;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 9.632446F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 90.36755F));
            rootLayout.Size = new Size(1346, 1012);
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
            titleLayout.Location = new Point(19, 3);
            titleLayout.Margin = new Padding(19, 3, 19, 7);
            titleLayout.Name = "titleLayout";
            titleLayout.RowCount = 2;
            titleLayout.RowStyles.Add(new RowStyle());
            titleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            titleLayout.Size = new Size(1308, 87);
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
            lblTitle.Size = new Size(1196, 31);
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
            lblDescription.Size = new Size(1196, 56);
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
            btnSaveAll.Location = new Point(1196, 0);
            btnSaveAll.Margin = new Padding(0);
            btnSaveAll.Name = "btnSaveAll";
            titleLayout.SetRowSpan(btnSaveAll, 2);
            btnSaveAll.Size = new Size(112, 87);
            btnSaveAll.TabIndex = 0;
            btnSaveAll.Tag = "perm:button.system.save:enabled";
            btnSaveAll.Text = "应用全部";
            // 
            // tabSettingCategories
            // 
            tabSettingCategories.Controls.Add(tabBasicSettings);
            tabSettingCategories.Dock = DockStyle.Fill;
            tabSettingCategories.HotTrack = true;
            tabSettingCategories.Location = new Point(19, 100);
            tabSettingCategories.Margin = new Padding(19, 3, 19, 7);
            tabSettingCategories.Name = "tabSettingCategories";
            tabSettingCategories.SelectedIndex = 0;
            tabSettingCategories.Size = new Size(1308, 905);
            tabSettingCategories.TabIndex = 1;
            // 
            // tabBasicSettings
            // 
            tabBasicSettings.Controls.Add(basicSettingsViewport);
            tabBasicSettings.Location = new Point(4, 29);
            tabBasicSettings.Margin = new Padding(2, 3, 2, 3);
            tabBasicSettings.Name = "tabBasicSettings";
            tabBasicSettings.Padding = new Padding(2, 3, 2, 3);
            tabBasicSettings.Size = new Size(1300, 872);
            tabBasicSettings.TabIndex = 0;
            tabBasicSettings.Text = "基础设置";
            tabBasicSettings.UseVisualStyleBackColor = true;
            // 
            // basicSettingsViewport
            // 
            basicSettingsViewport.AutoScroll = true;
            basicSettingsViewport.Controls.Add(basicSettingsLayout);
            basicSettingsViewport.Dock = DockStyle.Fill;
            basicSettingsViewport.Location = new Point(2, 3);
            basicSettingsViewport.Margin = new Padding(2, 3, 2, 3);
            basicSettingsViewport.Name = "basicSettingsViewport";
            basicSettingsViewport.Padding = new Padding(6, 7, 6, 7);
            basicSettingsViewport.Size = new Size(1296, 866);
            basicSettingsViewport.TabIndex = 0;
            // 
            // basicSettingsLayout
            // 
            basicSettingsLayout.AutoSize = true;
            basicSettingsLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            basicSettingsLayout.ColumnCount = 3;
            basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33334F));
            basicSettingsLayout.Controls.Add(leftSettingsColumn, 0, 0);
            basicSettingsLayout.Controls.Add(middleSettingsColumn, 1, 0);
            basicSettingsLayout.Controls.Add(rightSettingsColumn, 2, 0);
            basicSettingsLayout.Dock = DockStyle.Fill;
            basicSettingsLayout.Location = new Point(6, 7);
            basicSettingsLayout.Margin = new Padding(2, 3, 2, 3);
            basicSettingsLayout.Name = "basicSettingsLayout";
            basicSettingsLayout.RowCount = 1;
            basicSettingsLayout.RowStyles.Add(new RowStyle());
            basicSettingsLayout.Size = new Size(1284, 852);
            basicSettingsLayout.TabIndex = 0;
            // 
            // leftSettingsColumn
            // 
            leftSettingsColumn.AutoSize = true;
            leftSettingsColumn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            leftSettingsColumn.ColumnCount = 1;
            leftSettingsColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftSettingsColumn.Controls.Add(grpPlcConfig, 0, 0);
            leftSettingsColumn.Controls.Add(grpDeviceConfig, 0, 1);
            leftSettingsColumn.Dock = DockStyle.Top;
            leftSettingsColumn.Location = new Point(2, 3);
            leftSettingsColumn.Margin = new Padding(2, 3, 2, 3);
            leftSettingsColumn.Name = "leftSettingsColumn";
            leftSettingsColumn.RowCount = 2;
            leftSettingsColumn.RowStyles.Add(new RowStyle());
            leftSettingsColumn.RowStyles.Add(new RowStyle());
            leftSettingsColumn.Size = new Size(423, 579);
            leftSettingsColumn.TabIndex = 0;
            // 
            // grpPlcConfig
            // 
            grpPlcConfig.AutoSize = true;
            grpPlcConfig.Controls.Add(tlpPlcConfig);
            grpPlcConfig.Dock = DockStyle.Top;
            grpPlcConfig.Location = new Point(5, 5);
            grpPlcConfig.Margin = new Padding(5);
            grpPlcConfig.Name = "grpPlcConfig";
            grpPlcConfig.Padding = new Padding(2, 3, 2, 3);
            grpPlcConfig.Size = new Size(413, 299);
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
            tlpPlcConfig.Controls.Add(tlpPlcAlarmTriggerMode, 0, 4);
            tlpPlcConfig.Controls.Add(chkEnablePlcStringNumericFormatting, 0, 5);
            tlpPlcConfig.Controls.Add(tlpPlcStringNumericMode, 0, 6);
            tlpPlcConfig.Dock = DockStyle.Fill;
            tlpPlcConfig.Location = new Point(2, 23);
            tlpPlcConfig.Margin = new Padding(2, 3, 2, 3);
            tlpPlcConfig.Name = "tlpPlcConfig";
            tlpPlcConfig.RowCount = 7;
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tlpPlcConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tlpPlcConfig.Size = new Size(409, 273);
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
            tlpPlcIp.Size = new Size(409, 39);
            tlpPlcIp.TabIndex = 0;
            // 
            // lblPlcIp
            // 
            lblPlcIp.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcIp.Dock = DockStyle.Fill;
            lblPlcIp.Location = new Point(0, 0);
            lblPlcIp.Margin = new Padding(0);
            lblPlcIp.Name = "lblPlcIp";
            lblPlcIp.Padding = new Padding(6, 0, 0, 0);
            lblPlcIp.Size = new Size(19, 39);
            lblPlcIp.TabIndex = 0;
            lblPlcIp.Text = "IP";
            // 
            // input_PlcIp
            // 
            input_PlcIp.Dock = DockStyle.Fill;
            input_PlcIp.Location = new Point(19, 0);
            input_PlcIp.Margin = new Padding(0);
            input_PlcIp.Name = "input_PlcIp";
            input_PlcIp.Size = new Size(309, 39);
            input_PlcIp.TabIndex = 1;
            // 
            // btnConnectPlc
            // 
            btnConnectPlc.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnConnectPlc.BorderWidth = 1F;
            btnConnectPlc.Dock = DockStyle.Fill;
            btnConnectPlc.IconSvg = "ApiOutlined";
            btnConnectPlc.Location = new Point(328, 0);
            btnConnectPlc.Margin = new Padding(0);
            btnConnectPlc.Name = "btnConnectPlc";
            btnConnectPlc.Size = new Size(81, 39);
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
            tlpPlcPort.Location = new Point(0, 39);
            tlpPlcPort.Margin = new Padding(0);
            tlpPlcPort.Name = "tlpPlcPort";
            tlpPlcPort.RowCount = 1;
            tlpPlcPort.RowStyles.Add(new RowStyle());
            tlpPlcPort.Size = new Size(409, 39);
            tlpPlcPort.TabIndex = 1;
            // 
            // lblPlcPort
            // 
            lblPlcPort.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcPort.Dock = DockStyle.Fill;
            lblPlcPort.Location = new Point(0, 0);
            lblPlcPort.Margin = new Padding(0);
            lblPlcPort.Name = "lblPlcPort";
            lblPlcPort.Padding = new Padding(6, 0, 0, 0);
            lblPlcPort.Size = new Size(38, 39);
            lblPlcPort.TabIndex = 0;
            lblPlcPort.Text = "端口";
            // 
            // input_PlcPort
            // 
            input_PlcPort.Dock = DockStyle.Fill;
            input_PlcPort.Location = new Point(38, 0);
            input_PlcPort.Margin = new Padding(0);
            input_PlcPort.Name = "input_PlcPort";
            input_PlcPort.Size = new Size(371, 39);
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
            tableLayoutPanel7.Location = new Point(0, 78);
            tableLayoutPanel7.Margin = new Padding(0);
            tableLayoutPanel7.Name = "tableLayoutPanel7";
            tableLayoutPanel7.RowCount = 1;
            tableLayoutPanel7.RowStyles.Add(new RowStyle());
            tableLayoutPanel7.Size = new Size(409, 39);
            tableLayoutPanel7.TabIndex = 2;
            // 
            // lblPlcType
            // 
            lblPlcType.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcType.Dock = DockStyle.Fill;
            lblPlcType.Location = new Point(0, 0);
            lblPlcType.Margin = new Padding(0);
            lblPlcType.Name = "lblPlcType";
            lblPlcType.Padding = new Padding(6, 0, 0, 0);
            lblPlcType.Size = new Size(38, 40);
            lblPlcType.TabIndex = 0;
            lblPlcType.Text = "类型";
            // 
            // select_PlcType
            // 
            select_PlcType.Dock = DockStyle.Fill;
            select_PlcType.Location = new Point(38, 0);
            select_PlcType.Margin = new Padding(0);
            select_PlcType.MaxCount = 10;
            select_PlcType.Name = "select_PlcType";
            select_PlcType.Size = new Size(371, 40);
            select_PlcType.TabIndex = 1;
            // 
            // chkEnablePlcAlarmReading
            // 
            chkEnablePlcAlarmReading.Checked = true;
            chkEnablePlcAlarmReading.CheckState = CheckState.Checked;
            chkEnablePlcAlarmReading.Dock = DockStyle.Fill;
            chkEnablePlcAlarmReading.Location = new Point(0, 117);
            chkEnablePlcAlarmReading.Margin = new Padding(0);
            chkEnablePlcAlarmReading.Name = "chkEnablePlcAlarmReading";
            chkEnablePlcAlarmReading.Padding = new Padding(6, 0, 0, 0);
            chkEnablePlcAlarmReading.Size = new Size(409, 39);
            chkEnablePlcAlarmReading.TabIndex = 5;
            chkEnablePlcAlarmReading.Text = "启用报警信息读取";
            // 
            // tlpPlcAlarmTriggerMode
            // 
            tlpPlcAlarmTriggerMode.AutoSize = true;
            tlpPlcAlarmTriggerMode.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpPlcAlarmTriggerMode.ColumnCount = 2;
            tlpPlcAlarmTriggerMode.ColumnStyles.Add(new ColumnStyle());
            tlpPlcAlarmTriggerMode.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpPlcAlarmTriggerMode.Controls.Add(lblPlcAlarmTriggerMode, 0, 0);
            tlpPlcAlarmTriggerMode.Controls.Add(selectPlcAlarmTriggerMode, 1, 0);
            tlpPlcAlarmTriggerMode.Dock = DockStyle.Fill;
            tlpPlcAlarmTriggerMode.Location = new Point(0, 156);
            tlpPlcAlarmTriggerMode.Margin = new Padding(0);
            tlpPlcAlarmTriggerMode.Name = "tlpPlcAlarmTriggerMode";
            tlpPlcAlarmTriggerMode.RowCount = 1;
            tlpPlcAlarmTriggerMode.RowStyles.Add(new RowStyle());
            tlpPlcAlarmTriggerMode.Size = new Size(409, 39);
            tlpPlcAlarmTriggerMode.TabIndex = 4;
            // 
            // lblPlcAlarmTriggerMode
            // 
            lblPlcAlarmTriggerMode.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcAlarmTriggerMode.Dock = DockStyle.Fill;
            lblPlcAlarmTriggerMode.Location = new Point(0, 0);
            lblPlcAlarmTriggerMode.Margin = new Padding(0);
            lblPlcAlarmTriggerMode.Name = "lblPlcAlarmTriggerMode";
            lblPlcAlarmTriggerMode.Padding = new Padding(6, 0, 0, 0);
            lblPlcAlarmTriggerMode.Size = new Size(69, 39);
            lblPlcAlarmTriggerMode.TabIndex = 0;
            lblPlcAlarmTriggerMode.Text = "报警模式";
            // 
            // selectPlcAlarmTriggerMode
            // 
            selectPlcAlarmTriggerMode.Dock = DockStyle.Fill;
            selectPlcAlarmTriggerMode.Location = new Point(69, 0);
            selectPlcAlarmTriggerMode.Margin = new Padding(0);
            selectPlcAlarmTriggerMode.MaxCount = 10;
            selectPlcAlarmTriggerMode.Name = "selectPlcAlarmTriggerMode";
            selectPlcAlarmTriggerMode.Size = new Size(340, 39);
            selectPlcAlarmTriggerMode.TabIndex = 1;
            // 
            // chkEnablePlcStringNumericFormatting
            // 
            chkEnablePlcStringNumericFormatting.Checked = true;
            chkEnablePlcStringNumericFormatting.CheckState = CheckState.Checked;
            chkEnablePlcStringNumericFormatting.Dock = DockStyle.Fill;
            chkEnablePlcStringNumericFormatting.Location = new Point(0, 195);
            chkEnablePlcStringNumericFormatting.Margin = new Padding(0);
            chkEnablePlcStringNumericFormatting.Name = "chkEnablePlcStringNumericFormatting";
            chkEnablePlcStringNumericFormatting.Padding = new Padding(6, 0, 0, 0);
            chkEnablePlcStringNumericFormatting.Size = new Size(409, 39);
            chkEnablePlcStringNumericFormatting.TabIndex = 3;
            chkEnablePlcStringNumericFormatting.Text = "启用PLC字符串数值处理";
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
            tlpPlcStringNumericMode.Location = new Point(0, 234);
            tlpPlcStringNumericMode.Margin = new Padding(0);
            tlpPlcStringNumericMode.Name = "tlpPlcStringNumericMode";
            tlpPlcStringNumericMode.RowCount = 1;
            tlpPlcStringNumericMode.RowStyles.Add(new RowStyle());
            tlpPlcStringNumericMode.Size = new Size(409, 39);
            tlpPlcStringNumericMode.TabIndex = 4;
            // 
            // lblPlcStringNumericFormatMode
            // 
            lblPlcStringNumericFormatMode.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcStringNumericFormatMode.Dock = DockStyle.Fill;
            lblPlcStringNumericFormatMode.Location = new Point(0, 0);
            lblPlcStringNumericFormatMode.Margin = new Padding(0);
            lblPlcStringNumericFormatMode.Name = "lblPlcStringNumericFormatMode";
            lblPlcStringNumericFormatMode.Padding = new Padding(6, 0, 0, 0);
            lblPlcStringNumericFormatMode.Size = new Size(69, 39);
            lblPlcStringNumericFormatMode.TabIndex = 0;
            lblPlcStringNumericFormatMode.Text = "处理方式";
            // 
            // selectPlcStringNumericFormatMode
            // 
            selectPlcStringNumericFormatMode.Dock = DockStyle.Fill;
            selectPlcStringNumericFormatMode.Location = new Point(69, 0);
            selectPlcStringNumericFormatMode.Margin = new Padding(0);
            selectPlcStringNumericFormatMode.MaxCount = 10;
            selectPlcStringNumericFormatMode.Name = "selectPlcStringNumericFormatMode";
            selectPlcStringNumericFormatMode.Size = new Size(340, 39);
            selectPlcStringNumericFormatMode.TabIndex = 1;
            // 
            // grpDeviceConfig
            // 
            grpDeviceConfig.AutoSize = true;
            grpDeviceConfig.Controls.Add(layoutDeviceNoConfig);
            grpDeviceConfig.Dock = DockStyle.Top;
            grpDeviceConfig.Location = new Point(5, 314);
            grpDeviceConfig.Margin = new Padding(5);
            grpDeviceConfig.Name = "grpDeviceConfig";
            grpDeviceConfig.Padding = new Padding(2, 3, 2, 3);
            grpDeviceConfig.Size = new Size(413, 260);
            grpDeviceConfig.TabIndex = 0;
            grpDeviceConfig.TabStop = false;
            grpDeviceConfig.Text = "设备编号管理";
            // 
            // layoutDeviceNoConfig
            // 
            layoutDeviceNoConfig.AutoSize = true;
            layoutDeviceNoConfig.ColumnCount = 1;
            layoutDeviceNoConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutDeviceNoConfig.Controls.Add(tlpDeviceId, 0, 0);
            layoutDeviceNoConfig.Controls.Add(tlpDeviceName, 0, 1);
            layoutDeviceNoConfig.Controls.Add(tlpDeviveUrl, 0, 3);
            layoutDeviceNoConfig.Controls.Add(tlpMesUrl, 0, 2);
            layoutDeviceNoConfig.Controls.Add(tlpMesDeviceStatusQueryRoute, 0, 4);
            layoutDeviceNoConfig.Controls.Add(tlpMesDeviceIdSetRoute, 0, 5);
            layoutDeviceNoConfig.Dock = DockStyle.Fill;
            layoutDeviceNoConfig.Location = new Point(2, 23);
            layoutDeviceNoConfig.Margin = new Padding(2, 3, 2, 3);
            layoutDeviceNoConfig.Name = "layoutDeviceNoConfig";
            layoutDeviceNoConfig.RowCount = 6;
            layoutDeviceNoConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            layoutDeviceNoConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            layoutDeviceNoConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            layoutDeviceNoConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            layoutDeviceNoConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            layoutDeviceNoConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            layoutDeviceNoConfig.Size = new Size(409, 234);
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
            tlpDeviceId.Size = new Size(409, 39);
            tlpDeviceId.TabIndex = 0;
            // 
            // lblDeviceId
            // 
            lblDeviceId.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceId.Dock = DockStyle.Fill;
            lblDeviceId.Location = new Point(0, 0);
            lblDeviceId.Margin = new Padding(0);
            lblDeviceId.Name = "lblDeviceId";
            lblDeviceId.Padding = new Padding(6, 0, 0, 0);
            lblDeviceId.Size = new Size(69, 39);
            lblDeviceId.TabIndex = 0;
            lblDeviceId.Text = "设备编号";
            // 
            // input_DeviceID
            // 
            input_DeviceID.Dock = DockStyle.Fill;
            input_DeviceID.Location = new Point(69, 0);
            input_DeviceID.Margin = new Padding(0);
            input_DeviceID.Name = "input_DeviceID";
            input_DeviceID.Size = new Size(259, 39);
            input_DeviceID.TabIndex = 1;
            // 
            // btnSyncDevice
            // 
            btnSyncDevice.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSyncDevice.BorderWidth = 1F;
            btnSyncDevice.Dock = DockStyle.Fill;
            btnSyncDevice.IconSvg = "CloudUploadOutlined";
            btnSyncDevice.Location = new Point(328, 0);
            btnSyncDevice.Margin = new Padding(0);
            btnSyncDevice.Name = "btnSyncDevice";
            btnSyncDevice.Size = new Size(81, 39);
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
            tlpDeviceName.Location = new Point(0, 39);
            tlpDeviceName.Margin = new Padding(0);
            tlpDeviceName.Name = "tlpDeviceName";
            tlpDeviceName.RowCount = 1;
            tlpDeviceName.RowStyles.Add(new RowStyle());
            tlpDeviceName.Size = new Size(409, 39);
            tlpDeviceName.TabIndex = 1;
            // 
            // lblDeviceName
            // 
            lblDeviceName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceName.Dock = DockStyle.Fill;
            lblDeviceName.Location = new Point(0, 0);
            lblDeviceName.Margin = new Padding(0);
            lblDeviceName.Name = "lblDeviceName";
            lblDeviceName.Padding = new Padding(6, 0, 0, 0);
            lblDeviceName.Size = new Size(69, 39);
            lblDeviceName.TabIndex = 0;
            lblDeviceName.Text = "设备名称";
            // 
            // input_DeviceName
            // 
            input_DeviceName.Dock = DockStyle.Fill;
            input_DeviceName.Location = new Point(69, 0);
            input_DeviceName.Margin = new Padding(0);
            input_DeviceName.Name = "input_DeviceName";
            input_DeviceName.Size = new Size(340, 39);
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
            tlpDeviveUrl.Location = new Point(0, 117);
            tlpDeviveUrl.Margin = new Padding(0);
            tlpDeviveUrl.Name = "tlpDeviveUrl";
            tlpDeviveUrl.RowCount = 1;
            tlpDeviveUrl.RowStyles.Add(new RowStyle());
            tlpDeviveUrl.Size = new Size(409, 39);
            tlpDeviveUrl.TabIndex = 2;
            // 
            // lblDeviceUrl
            // 
            lblDeviceUrl.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceUrl.Dock = DockStyle.Fill;
            lblDeviceUrl.Location = new Point(0, 0);
            lblDeviceUrl.Margin = new Padding(0);
            lblDeviceUrl.Name = "lblDeviceUrl";
            lblDeviceUrl.Padding = new Padding(6, 0, 0, 0);
            lblDeviceUrl.Size = new Size(69, 43);
            lblDeviceUrl.TabIndex = 0;
            lblDeviceUrl.Text = "状态地址";
            // 
            // input_DeviceUrl
            // 
            input_DeviceUrl.Dock = DockStyle.Fill;
            input_DeviceUrl.Location = new Point(69, 0);
            input_DeviceUrl.Margin = new Padding(0);
            input_DeviceUrl.Name = "input_DeviceUrl";
            input_DeviceUrl.Size = new Size(340, 43);
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
            tlpMesUrl.Location = new Point(0, 78);
            tlpMesUrl.Margin = new Padding(0);
            tlpMesUrl.Name = "tlpMesUrl";
            tlpMesUrl.RowCount = 1;
            tlpMesUrl.RowStyles.Add(new RowStyle());
            tlpMesUrl.Size = new Size(409, 39);
            tlpMesUrl.TabIndex = 3;
            // 
            // lblMesUrl
            // 
            lblMesUrl.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesUrl.Dock = DockStyle.Fill;
            lblMesUrl.Location = new Point(0, 0);
            lblMesUrl.Margin = new Padding(0);
            lblMesUrl.Name = "lblMesUrl";
            lblMesUrl.Padding = new Padding(6, 0, 0, 0);
            lblMesUrl.Size = new Size(67, 39);
            lblMesUrl.TabIndex = 0;
            lblMesUrl.Text = "MES地址";
            // 
            // input_BaseUrl
            // 
            input_BaseUrl.Dock = DockStyle.Fill;
            input_BaseUrl.Location = new Point(67, 0);
            input_BaseUrl.Margin = new Padding(0);
            input_BaseUrl.Name = "input_BaseUrl";
            input_BaseUrl.Padding = new Padding(2, 0, 0, 0);
            input_BaseUrl.Size = new Size(261, 39);
            input_BaseUrl.TabIndex = 1;
            // 
            // btnTestConnection
            // 
            btnTestConnection.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnTestConnection.BorderWidth = 1F;
            btnTestConnection.Dock = DockStyle.Fill;
            btnTestConnection.IconSvg = "ApiOutlined";
            btnTestConnection.Location = new Point(328, 0);
            btnTestConnection.Margin = new Padding(0);
            btnTestConnection.Name = "btnTestConnection";
            btnTestConnection.Size = new Size(81, 39);
            btnTestConnection.TabIndex = 2;
            btnTestConnection.Tag = "perm:button.system.test-mes:enabled";
            btnTestConnection.Text = "测试";
            // 
            // tlpMesDeviceStatusQueryRoute
            // 
            tlpMesDeviceStatusQueryRoute.ColumnCount = 2;
            tlpMesDeviceStatusQueryRoute.ColumnStyles.Add(new ColumnStyle());
            tlpMesDeviceStatusQueryRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesDeviceStatusQueryRoute.Controls.Add(lblMesDeviceStatusQueryRoute, 0, 0);
            tlpMesDeviceStatusQueryRoute.Controls.Add(inputMesDeviceStatusQueryRoute, 1, 0);
            tlpMesDeviceStatusQueryRoute.Location = new Point(0, 156);
            tlpMesDeviceStatusQueryRoute.Margin = new Padding(0);
            tlpMesDeviceStatusQueryRoute.Name = "tlpMesDeviceStatusQueryRoute";
            tlpMesDeviceStatusQueryRoute.RowCount = 1;
            tlpMesDeviceStatusQueryRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesDeviceStatusQueryRoute.Size = new Size(409, 39);
            tlpMesDeviceStatusQueryRoute.TabIndex = 20;
            // 
            // lblMesDeviceStatusQueryRoute
            // 
            lblMesDeviceStatusQueryRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesDeviceStatusQueryRoute.Dock = DockStyle.Fill;
            lblMesDeviceStatusQueryRoute.Location = new Point(0, 0);
            lblMesDeviceStatusQueryRoute.Margin = new Padding(0);
            lblMesDeviceStatusQueryRoute.Name = "lblMesDeviceStatusQueryRoute";
            lblMesDeviceStatusQueryRoute.Padding = new Padding(8, 0, 0, 0);
            lblMesDeviceStatusQueryRoute.Size = new Size(103, 39);
            lblMesDeviceStatusQueryRoute.TabIndex = 0;
            lblMesDeviceStatusQueryRoute.Text = "查询设备状态";
            // 
            // inputMesDeviceStatusQueryRoute
            // 
            inputMesDeviceStatusQueryRoute.Dock = DockStyle.Fill;
            inputMesDeviceStatusQueryRoute.Location = new Point(103, 0);
            inputMesDeviceStatusQueryRoute.Margin = new Padding(0);
            inputMesDeviceStatusQueryRoute.Name = "inputMesDeviceStatusQueryRoute";
            inputMesDeviceStatusQueryRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesDeviceStatusQueryRoute.Size = new Size(306, 39);
            inputMesDeviceStatusQueryRoute.TabIndex = 1;
            // 
            // tlpMesDeviceIdSetRoute
            // 
            tlpMesDeviceIdSetRoute.ColumnCount = 2;
            tlpMesDeviceIdSetRoute.ColumnStyles.Add(new ColumnStyle());
            tlpMesDeviceIdSetRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesDeviceIdSetRoute.Controls.Add(lblMesDeviceIdSetRoute, 0, 0);
            tlpMesDeviceIdSetRoute.Controls.Add(inputMesDeviceIdSetRoute, 1, 0);
            tlpMesDeviceIdSetRoute.Dock = DockStyle.Fill;
            tlpMesDeviceIdSetRoute.Location = new Point(0, 195);
            tlpMesDeviceIdSetRoute.Margin = new Padding(0);
            tlpMesDeviceIdSetRoute.Name = "tlpMesDeviceIdSetRoute";
            tlpMesDeviceIdSetRoute.RowCount = 1;
            tlpMesDeviceIdSetRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesDeviceIdSetRoute.Size = new Size(409, 39);
            tlpMesDeviceIdSetRoute.TabIndex = 21;
            // 
            // lblMesDeviceIdSetRoute
            // 
            lblMesDeviceIdSetRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesDeviceIdSetRoute.Dock = DockStyle.Fill;
            lblMesDeviceIdSetRoute.Location = new Point(0, 0);
            lblMesDeviceIdSetRoute.Margin = new Padding(0);
            lblMesDeviceIdSetRoute.Name = "lblMesDeviceIdSetRoute";
            lblMesDeviceIdSetRoute.Padding = new Padding(8, 0, 0, 0);
            lblMesDeviceIdSetRoute.Size = new Size(103, 39);
            lblMesDeviceIdSetRoute.TabIndex = 0;
            lblMesDeviceIdSetRoute.Text = "设置设备编号";
            // 
            // inputMesDeviceIdSetRoute
            // 
            inputMesDeviceIdSetRoute.Dock = DockStyle.Fill;
            inputMesDeviceIdSetRoute.Location = new Point(103, 0);
            inputMesDeviceIdSetRoute.Margin = new Padding(0);
            inputMesDeviceIdSetRoute.Name = "inputMesDeviceIdSetRoute";
            inputMesDeviceIdSetRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesDeviceIdSetRoute.Size = new Size(306, 39);
            inputMesDeviceIdSetRoute.TabIndex = 1;
            // 
            // middleSettingsColumn
            // 
            middleSettingsColumn.AutoSize = true;
            middleSettingsColumn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            middleSettingsColumn.ColumnCount = 1;
            middleSettingsColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            middleSettingsColumn.Controls.Add(grpProductionConfig, 0, 0);
            middleSettingsColumn.Controls.Add(grpAppConfig, 0, 1);
            middleSettingsColumn.Controls.Add(grpCenterServerConfig, 0, 2);
            middleSettingsColumn.Dock = DockStyle.Top;
            middleSettingsColumn.Location = new Point(429, 3);
            middleSettingsColumn.Margin = new Padding(2, 3, 2, 3);
            middleSettingsColumn.Name = "middleSettingsColumn";
            middleSettingsColumn.RowCount = 3;
            middleSettingsColumn.RowStyles.Add(new RowStyle());
            middleSettingsColumn.RowStyles.Add(new RowStyle());
            middleSettingsColumn.RowStyles.Add(new RowStyle());
            middleSettingsColumn.Size = new Size(423, 655);
            middleSettingsColumn.TabIndex = 1;
            // 
            // grpProductionConfig
            // 
            grpProductionConfig.AutoSize = true;
            grpProductionConfig.Controls.Add(tlpProductConfig);
            grpProductionConfig.Dock = DockStyle.Top;
            grpProductionConfig.Location = new Point(5, 5);
            grpProductionConfig.Margin = new Padding(5);
            grpProductionConfig.Name = "grpProductionConfig";
            grpProductionConfig.Padding = new Padding(2, 3, 2, 3);
            grpProductionConfig.Size = new Size(413, 299);
            grpProductionConfig.TabIndex = 4;
            grpProductionConfig.TabStop = false;
            grpProductionConfig.Text = "生产配置";
            // 
            // tlpProductConfig
            // 
            tlpProductConfig.AutoSize = true;
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
            tlpProductConfig.Location = new Point(2, 23);
            tlpProductConfig.Margin = new Padding(2, 3, 2, 3);
            tlpProductConfig.Name = "tlpProductConfig";
            tlpProductConfig.RowCount = 5;
            tlpProductConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tlpProductConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tlpProductConfig.RowStyles.Add(new RowStyle());
            tlpProductConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tlpProductConfig.RowStyles.Add(new RowStyle());
            tlpProductConfig.Size = new Size(409, 273);
            tlpProductConfig.TabIndex = 0;
            // 
            // stationDisplayNameLayout
            // 
            stationDisplayNameLayout.AutoSize = true;
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
            stationDisplayNameLayout.Location = new Point(0, 78);
            stationDisplayNameLayout.Margin = new Padding(0);
            stationDisplayNameLayout.Name = "stationDisplayNameLayout";
            stationDisplayNameLayout.RowCount = 1;
            stationDisplayNameLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            stationDisplayNameLayout.Size = new Size(409, 39);
            stationDisplayNameLayout.TabIndex = 7;
            // 
            // lblStation1DisplayName
            // 
            lblStation1DisplayName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblStation1DisplayName.Dock = DockStyle.Fill;
            lblStation1DisplayName.Location = new Point(0, 0);
            lblStation1DisplayName.Margin = new Padding(0);
            lblStation1DisplayName.Name = "lblStation1DisplayName";
            lblStation1DisplayName.Padding = new Padding(6, 0, 0, 0);
            lblStation1DisplayName.Size = new Size(117, 39);
            lblStation1DisplayName.TabIndex = 0;
            lblStation1DisplayName.Text = "工位 1 显示名称";
            // 
            // inputStation1DisplayName
            // 
            inputStation1DisplayName.Dock = DockStyle.Fill;
            inputStation1DisplayName.Location = new Point(117, 0);
            inputStation1DisplayName.Margin = new Padding(0);
            inputStation1DisplayName.Name = "inputStation1DisplayName";
            inputStation1DisplayName.Size = new Size(87, 39);
            inputStation1DisplayName.TabIndex = 1;
            inputStation1DisplayName.Text = "左";
            // 
            // lblStation2DisplayName
            // 
            lblStation2DisplayName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblStation2DisplayName.Dock = DockStyle.Fill;
            lblStation2DisplayName.Location = new Point(204, 0);
            lblStation2DisplayName.Margin = new Padding(0);
            lblStation2DisplayName.Name = "lblStation2DisplayName";
            lblStation2DisplayName.Padding = new Padding(6, 0, 0, 0);
            lblStation2DisplayName.Size = new Size(117, 39);
            lblStation2DisplayName.TabIndex = 2;
            lblStation2DisplayName.Text = "工位 2 显示名称";
            // 
            // inputStation2DisplayName
            // 
            inputStation2DisplayName.Dock = DockStyle.Fill;
            inputStation2DisplayName.Location = new Point(321, 0);
            inputStation2DisplayName.Margin = new Padding(0);
            inputStation2DisplayName.Name = "inputStation2DisplayName";
            inputStation2DisplayName.Size = new Size(88, 39);
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
            tlpUploadConfig.Location = new Point(0, 117);
            tlpUploadConfig.Margin = new Padding(0);
            tlpUploadConfig.Name = "tlpUploadConfig";
            tlpUploadConfig.RowCount = 1;
            tlpUploadConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpUploadConfig.Size = new Size(409, 39);
            tlpUploadConfig.TabIndex = 7;
            // 
            // inputUploadBatchSize
            // 
            inputUploadBatchSize.Dock = DockStyle.Fill;
            inputUploadBatchSize.Location = new Point(327, 0);
            inputUploadBatchSize.Margin = new Padding(0);
            inputUploadBatchSize.Name = "inputUploadBatchSize";
            inputUploadBatchSize.Size = new Size(82, 39);
            inputUploadBatchSize.TabIndex = 6;
            inputUploadBatchSize.Text = "1";
            // 
            // lblUploadBatchSize
            // 
            lblUploadBatchSize.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblUploadBatchSize.Dock = DockStyle.Fill;
            lblUploadBatchSize.Location = new Point(258, 0);
            lblUploadBatchSize.Margin = new Padding(0);
            lblUploadBatchSize.Name = "lblUploadBatchSize";
            lblUploadBatchSize.Padding = new Padding(6, 0, 0, 0);
            lblUploadBatchSize.Size = new Size(69, 39);
            lblUploadBatchSize.TabIndex = 5;
            lblUploadBatchSize.Text = "上传数量";
            // 
            // selectUploadMode
            // 
            selectUploadMode.Dock = DockStyle.Fill;
            selectUploadMode.Location = new Point(69, 0);
            selectUploadMode.Margin = new Padding(0);
            selectUploadMode.MaxCount = 10;
            selectUploadMode.Name = "selectUploadMode";
            selectUploadMode.Size = new Size(189, 39);
            selectUploadMode.TabIndex = 4;
            // 
            // lblUploadMode
            // 
            lblUploadMode.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblUploadMode.Dock = DockStyle.Fill;
            lblUploadMode.Location = new Point(0, 0);
            lblUploadMode.Margin = new Padding(0);
            lblUploadMode.Name = "lblUploadMode";
            lblUploadMode.Padding = new Padding(6, 0, 0, 0);
            lblUploadMode.Size = new Size(69, 39);
            lblUploadMode.TabIndex = 3;
            lblUploadMode.Text = "上传模式";
            // 
            // chkEnableDualStation
            // 
            chkEnableDualStation.Dock = DockStyle.Fill;
            chkEnableDualStation.Location = new Point(0, 0);
            chkEnableDualStation.Margin = new Padding(0);
            chkEnableDualStation.Name = "chkEnableDualStation";
            chkEnableDualStation.Size = new Size(204, 39);
            chkEnableDualStation.TabIndex = 0;
            chkEnableDualStation.Text = "启用双工位";
            // 
            // chkUseOperatorInputDialog
            // 
            chkUseOperatorInputDialog.Dock = DockStyle.Fill;
            chkUseOperatorInputDialog.Location = new Point(204, 0);
            chkUseOperatorInputDialog.Margin = new Padding(0);
            chkUseOperatorInputDialog.Name = "chkUseOperatorInputDialog";
            chkUseOperatorInputDialog.Padding = new Padding(6, 0, 0, 0);
            chkUseOperatorInputDialog.Size = new Size(205, 39);
            chkUseOperatorInputDialog.TabIndex = 1;
            chkUseOperatorInputDialog.Text = "操作员弹窗输入";
            // 
            // chkValidateRecipeBeforeStart
            // 
            chkValidateRecipeBeforeStart.Dock = DockStyle.Fill;
            chkValidateRecipeBeforeStart.Location = new Point(0, 39);
            chkValidateRecipeBeforeStart.Margin = new Padding(0);
            chkValidateRecipeBeforeStart.Name = "chkValidateRecipeBeforeStart";
            chkValidateRecipeBeforeStart.Size = new Size(204, 39);
            chkValidateRecipeBeforeStart.TabIndex = 2;
            chkValidateRecipeBeforeStart.Text = "开工后校验配方";
            // 
            // chkEnableFinishExpQtyPrompt
            // 
            chkEnableFinishExpQtyPrompt.Dock = DockStyle.Fill;
            chkEnableFinishExpQtyPrompt.Location = new Point(204, 39);
            chkEnableFinishExpQtyPrompt.Margin = new Padding(0);
            chkEnableFinishExpQtyPrompt.Name = "chkEnableFinishExpQtyPrompt";
            chkEnableFinishExpQtyPrompt.Padding = new Padding(6, 0, 0, 0);
            chkEnableFinishExpQtyPrompt.Size = new Size(205, 39);
            chkEnableFinishExpQtyPrompt.TabIndex = 3;
            chkEnableFinishExpQtyPrompt.Text = "启用完工实际数量输入弹窗";
            // 
            // tableLayoutPanelHeartbeat
            // 
            tableLayoutPanelHeartbeat.AutoSize = true;
            tableLayoutPanelHeartbeat.ColumnCount = 2;
            tlpProductConfig.SetColumnSpan(tableLayoutPanelHeartbeat, 2);
            tableLayoutPanelHeartbeat.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelHeartbeat.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelHeartbeat.Controls.Add(lblPlcHeartbeatInterval, 0, 0);
            tableLayoutPanelHeartbeat.Controls.Add(inputPlcHeartbeatInterval, 1, 0);
            tableLayoutPanelHeartbeat.Controls.Add(lblPlcHeartbeatTimeout, 0, 1);
            tableLayoutPanelHeartbeat.Controls.Add(inputPlcHeartbeatTimeout, 1, 1);
            tableLayoutPanelHeartbeat.Controls.Add(lblPlcCommunicationTimeout, 0, 2);
            tableLayoutPanelHeartbeat.Controls.Add(inputPlcCommunicationTimeout, 1, 2);
            tableLayoutPanelHeartbeat.Dock = DockStyle.Top;
            tableLayoutPanelHeartbeat.Location = new Point(0, 156);
            tableLayoutPanelHeartbeat.Margin = new Padding(0);
            tableLayoutPanelHeartbeat.Name = "tableLayoutPanelHeartbeat";
            tableLayoutPanelHeartbeat.RowCount = 3;
            tableLayoutPanelHeartbeat.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelHeartbeat.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelHeartbeat.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelHeartbeat.Size = new Size(409, 117);
            tableLayoutPanelHeartbeat.TabIndex = 7;
            // 
            // lblPlcHeartbeatInterval
            // 
            lblPlcHeartbeatInterval.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcHeartbeatInterval.Dock = DockStyle.Fill;
            lblPlcHeartbeatInterval.Location = new Point(0, 0);
            lblPlcHeartbeatInterval.Margin = new Padding(0);
            lblPlcHeartbeatInterval.Name = "lblPlcHeartbeatInterval";
            lblPlcHeartbeatInterval.Padding = new Padding(6, 0, 0, 0);
            lblPlcHeartbeatInterval.Size = new Size(153, 39);
            lblPlcHeartbeatInterval.TabIndex = 0;
            lblPlcHeartbeatInterval.Text = "PLC心跳监测频率(ms)";
            // 
            // inputPlcHeartbeatInterval
            // 
            inputPlcHeartbeatInterval.Dock = DockStyle.Fill;
            inputPlcHeartbeatInterval.Location = new Point(153, 0);
            inputPlcHeartbeatInterval.Margin = new Padding(0);
            inputPlcHeartbeatInterval.Name = "inputPlcHeartbeatInterval";
            inputPlcHeartbeatInterval.Size = new Size(256, 39);
            inputPlcHeartbeatInterval.TabIndex = 1;
            inputPlcHeartbeatInterval.Text = "300";
            // 
            // lblPlcHeartbeatTimeout
            // 
            lblPlcHeartbeatTimeout.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcHeartbeatTimeout.Dock = DockStyle.Fill;
            lblPlcHeartbeatTimeout.Location = new Point(0, 39);
            lblPlcHeartbeatTimeout.Margin = new Padding(0);
            lblPlcHeartbeatTimeout.Name = "lblPlcHeartbeatTimeout";
            lblPlcHeartbeatTimeout.Padding = new Padding(6, 0, 0, 0);
            lblPlcHeartbeatTimeout.Size = new Size(140, 39);
            lblPlcHeartbeatTimeout.TabIndex = 2;
            lblPlcHeartbeatTimeout.Text = "PLC心跳超时时间(s)";
            // 
            // inputPlcHeartbeatTimeout
            // 
            inputPlcHeartbeatTimeout.Dock = DockStyle.Fill;
            inputPlcHeartbeatTimeout.Location = new Point(153, 39);
            inputPlcHeartbeatTimeout.Margin = new Padding(0);
            inputPlcHeartbeatTimeout.Name = "inputPlcHeartbeatTimeout";
            inputPlcHeartbeatTimeout.Size = new Size(256, 39);
            inputPlcHeartbeatTimeout.TabIndex = 3;
            inputPlcHeartbeatTimeout.Text = "3";
            // 
            // lblPlcCommunicationTimeout
            // 
            lblPlcCommunicationTimeout.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcCommunicationTimeout.Dock = DockStyle.Fill;
            lblPlcCommunicationTimeout.Location = new Point(0, 78);
            lblPlcCommunicationTimeout.Margin = new Padding(0);
            lblPlcCommunicationTimeout.Name = "lblPlcCommunicationTimeout";
            lblPlcCommunicationTimeout.Padding = new Padding(6, 0, 0, 0);
            lblPlcCommunicationTimeout.Size = new Size(122, 39);
            lblPlcCommunicationTimeout.TabIndex = 4;
            lblPlcCommunicationTimeout.Text = "PLC通讯超时(ms)";
            // 
            // inputPlcCommunicationTimeout
            // 
            inputPlcCommunicationTimeout.Dock = DockStyle.Fill;
            inputPlcCommunicationTimeout.Location = new Point(153, 78);
            inputPlcCommunicationTimeout.Margin = new Padding(0);
            inputPlcCommunicationTimeout.Name = "inputPlcCommunicationTimeout";
            inputPlcCommunicationTimeout.Size = new Size(256, 39);
            inputPlcCommunicationTimeout.TabIndex = 5;
            inputPlcCommunicationTimeout.Text = "3000";
            // 
            // grpAppConfig
            // 
            grpAppConfig.AutoSize = true;
            grpAppConfig.Controls.Add(tableLayoutPanel1);
            grpAppConfig.Dock = DockStyle.Top;
            grpAppConfig.Location = new Point(5, 314);
            grpAppConfig.Margin = new Padding(5);
            grpAppConfig.Name = "grpAppConfig";
            grpAppConfig.Padding = new Padding(2, 3, 2, 3);
            grpAppConfig.Size = new Size(413, 144);
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
            tableLayoutPanel1.Controls.Add(tableLayoutPanel4, 0, 2);
            tableLayoutPanel1.Controls.Add(tlpLogPath, 0, 0);
            tableLayoutPanel1.Controls.Add(tlpDataPath, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(2, 23);
            tableLayoutPanel1.Margin = new Padding(2, 3, 2, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 17F));
            tableLayoutPanel1.Size = new Size(409, 118);
            tableLayoutPanel1.TabIndex = 6;
            // 
            // tableLayoutPanel4
            // 
            tableLayoutPanel4.AutoSize = true;
            tableLayoutPanel4.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel4.ColumnCount = 2;
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel4.Controls.Add(chkEnableAutoStart, 0, 0);
            tableLayoutPanel4.Controls.Add(chkEnableElevatedAutoStart, 1, 0);
            tableLayoutPanel4.Dock = DockStyle.Fill;
            tableLayoutPanel4.Location = new Point(0, 78);
            tableLayoutPanel4.Margin = new Padding(0);
            tableLayoutPanel4.MinimumSize = new Size(0, 39);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 1;
            tableLayoutPanel4.RowStyles.Add(new RowStyle());
            tableLayoutPanel4.Size = new Size(409, 40);
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
            chkEnableAutoStart.Padding = new Padding(6, 0, 0, 0);
            chkEnableAutoStart.Size = new Size(204, 40);
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
            chkEnableElevatedAutoStart.Padding = new Padding(6, 0, 0, 0);
            chkEnableElevatedAutoStart.Size = new Size(205, 40);
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
            tlpLogPath.Size = new Size(409, 39);
            tlpLogPath.TabIndex = 4;
            // 
            // lblLogPath
            // 
            lblLogPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblLogPath.Dock = DockStyle.Fill;
            lblLogPath.Location = new Point(0, 0);
            lblLogPath.Margin = new Padding(0);
            lblLogPath.Name = "lblLogPath";
            lblLogPath.Padding = new Padding(6, 0, 0, 0);
            lblLogPath.Size = new Size(69, 39);
            lblLogPath.TabIndex = 0;
            lblLogPath.Text = "日志目录";
            // 
            // input_LogsPath
            // 
            input_LogsPath.Dock = DockStyle.Fill;
            input_LogsPath.Location = new Point(69, 0);
            input_LogsPath.Margin = new Padding(0);
            input_LogsPath.Name = "input_LogsPath";
            input_LogsPath.Size = new Size(178, 39);
            input_LogsPath.TabIndex = 1;
            // 
            // btnChangeLogPath
            // 
            btnChangeLogPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnChangeLogPath.BorderWidth = 1F;
            btnChangeLogPath.Dock = DockStyle.Fill;
            btnChangeLogPath.IconSvg = "FolderOpenOutlined";
            btnChangeLogPath.Location = new Point(247, 0);
            btnChangeLogPath.Margin = new Padding(0);
            btnChangeLogPath.Name = "btnChangeLogPath";
            btnChangeLogPath.Size = new Size(81, 39);
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
            btnOpenLogPath.Location = new Point(328, 0);
            btnOpenLogPath.Margin = new Padding(0);
            btnOpenLogPath.Name = "btnOpenLogPath";
            btnOpenLogPath.Size = new Size(81, 39);
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
            tlpDataPath.Location = new Point(0, 39);
            tlpDataPath.Margin = new Padding(0);
            tlpDataPath.Name = "tlpDataPath";
            tlpDataPath.RowCount = 1;
            tlpDataPath.RowStyles.Add(new RowStyle());
            tlpDataPath.Size = new Size(409, 39);
            tlpDataPath.TabIndex = 5;
            // 
            // lblDataPath
            // 
            lblDataPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDataPath.Dock = DockStyle.Fill;
            lblDataPath.Location = new Point(0, 0);
            lblDataPath.Margin = new Padding(0);
            lblDataPath.Name = "lblDataPath";
            lblDataPath.Padding = new Padding(6, 0, 0, 0);
            lblDataPath.Size = new Size(69, 39);
            lblDataPath.TabIndex = 0;
            lblDataPath.Text = "数据目录";
            // 
            // input_DataPath
            // 
            input_DataPath.Dock = DockStyle.Fill;
            input_DataPath.Location = new Point(69, 0);
            input_DataPath.Margin = new Padding(0);
            input_DataPath.Name = "input_DataPath";
            input_DataPath.Size = new Size(178, 39);
            input_DataPath.TabIndex = 1;
            // 
            // btnChangeDataPath
            // 
            btnChangeDataPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnChangeDataPath.BorderWidth = 1F;
            btnChangeDataPath.Dock = DockStyle.Fill;
            btnChangeDataPath.IconSvg = "FolderOpenOutlined";
            btnChangeDataPath.Location = new Point(247, 0);
            btnChangeDataPath.Margin = new Padding(0);
            btnChangeDataPath.Name = "btnChangeDataPath";
            btnChangeDataPath.Size = new Size(81, 39);
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
            btnOpenDataPath.Location = new Point(328, 0);
            btnOpenDataPath.Margin = new Padding(0);
            btnOpenDataPath.Name = "btnOpenDataPath";
            btnOpenDataPath.Size = new Size(81, 39);
            btnOpenDataPath.TabIndex = 3;
            btnOpenDataPath.Tag = "perm:button.system.open-path:enabled";
            btnOpenDataPath.Text = "打开";
            // 
            // grpCenterServerConfig
            // 
            grpCenterServerConfig.AutoSize = true;
            grpCenterServerConfig.Controls.Add(tableLayoutPanelCenterServer);
            grpCenterServerConfig.Dock = DockStyle.Top;
            grpCenterServerConfig.Location = new Point(5, 468);
            grpCenterServerConfig.Margin = new Padding(5);
            grpCenterServerConfig.Name = "grpCenterServerConfig";
            grpCenterServerConfig.Padding = new Padding(2, 3, 2, 3);
            grpCenterServerConfig.Size = new Size(413, 182);
            grpCenterServerConfig.TabIndex = 6;
            grpCenterServerConfig.TabStop = false;
            grpCenterServerConfig.Text = "中心服务器";
            // 
            // tableLayoutPanelCenterServer
            // 
            tableLayoutPanelCenterServer.AutoSize = true;
            tableLayoutPanelCenterServer.ColumnCount = 1;
            tableLayoutPanelCenterServer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelCenterServer.Controls.Add(chkEnableCenterServerSync, 0, 0);
            tableLayoutPanelCenterServer.Controls.Add(tlpCenterServerBaseUrl, 0, 1);
            tableLayoutPanelCenterServer.Controls.Add(tlpCenterServerSystemType, 0, 2);
            tableLayoutPanelCenterServer.Controls.Add(tlpCenterServerHeartbeat, 0, 3);
            tableLayoutPanelCenterServer.Dock = DockStyle.Fill;
            tableLayoutPanelCenterServer.Location = new Point(2, 23);
            tableLayoutPanelCenterServer.Margin = new Padding(2, 3, 2, 3);
            tableLayoutPanelCenterServer.Name = "tableLayoutPanelCenterServer";
            tableLayoutPanelCenterServer.RowCount = 4;
            tableLayoutPanelCenterServer.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelCenterServer.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelCenterServer.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelCenterServer.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelCenterServer.Size = new Size(409, 156);
            tableLayoutPanelCenterServer.TabIndex = 0;
            // 
            // chkEnableCenterServerSync
            // 
            chkEnableCenterServerSync.Dock = DockStyle.Fill;
            chkEnableCenterServerSync.Location = new Point(0, 0);
            chkEnableCenterServerSync.Margin = new Padding(0);
            chkEnableCenterServerSync.Name = "chkEnableCenterServerSync";
            chkEnableCenterServerSync.Size = new Size(409, 39);
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
            tlpCenterServerBaseUrl.Location = new Point(0, 39);
            tlpCenterServerBaseUrl.Margin = new Padding(0);
            tlpCenterServerBaseUrl.Name = "tlpCenterServerBaseUrl";
            tlpCenterServerBaseUrl.RowCount = 1;
            tlpCenterServerBaseUrl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpCenterServerBaseUrl.Size = new Size(409, 39);
            tlpCenterServerBaseUrl.TabIndex = 1;
            // 
            // lblCenterServerBaseUrl
            // 
            lblCenterServerBaseUrl.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblCenterServerBaseUrl.Dock = DockStyle.Fill;
            lblCenterServerBaseUrl.Location = new Point(0, 0);
            lblCenterServerBaseUrl.Margin = new Padding(0);
            lblCenterServerBaseUrl.Name = "lblCenterServerBaseUrl";
            lblCenterServerBaseUrl.Padding = new Padding(6, 0, 0, 0);
            lblCenterServerBaseUrl.Size = new Size(116, 39);
            lblCenterServerBaseUrl.TabIndex = 0;
            lblCenterServerBaseUrl.Text = "中心服务器地址";
            // 
            // inputCenterServerBaseUrl
            // 
            inputCenterServerBaseUrl.Dock = DockStyle.Fill;
            inputCenterServerBaseUrl.Location = new Point(116, 0);
            inputCenterServerBaseUrl.Margin = new Padding(0);
            inputCenterServerBaseUrl.Name = "inputCenterServerBaseUrl";
            inputCenterServerBaseUrl.Size = new Size(293, 39);
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
            tlpCenterServerSystemType.Location = new Point(0, 78);
            tlpCenterServerSystemType.Margin = new Padding(0);
            tlpCenterServerSystemType.Name = "tlpCenterServerSystemType";
            tlpCenterServerSystemType.RowCount = 1;
            tlpCenterServerSystemType.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpCenterServerSystemType.Size = new Size(409, 39);
            tlpCenterServerSystemType.TabIndex = 2;
            // 
            // lblCenterServerSystemType
            // 
            lblCenterServerSystemType.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblCenterServerSystemType.Dock = DockStyle.Fill;
            lblCenterServerSystemType.Location = new Point(0, 0);
            lblCenterServerSystemType.Margin = new Padding(0);
            lblCenterServerSystemType.Name = "lblCenterServerSystemType";
            lblCenterServerSystemType.Padding = new Padding(6, 0, 0, 0);
            lblCenterServerSystemType.Size = new Size(69, 39);
            lblCenterServerSystemType.TabIndex = 0;
            lblCenterServerSystemType.Text = "系统类型";
            // 
            // selectCenterServerSystemType
            // 
            selectCenterServerSystemType.Dock = DockStyle.Fill;
            selectCenterServerSystemType.Location = new Point(69, 0);
            selectCenterServerSystemType.Margin = new Padding(0);
            selectCenterServerSystemType.MaxCount = 10;
            selectCenterServerSystemType.Name = "selectCenterServerSystemType";
            selectCenterServerSystemType.Size = new Size(340, 39);
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
            tlpCenterServerHeartbeat.Location = new Point(0, 117);
            tlpCenterServerHeartbeat.Margin = new Padding(0);
            tlpCenterServerHeartbeat.Name = "tlpCenterServerHeartbeat";
            tlpCenterServerHeartbeat.RowCount = 1;
            tlpCenterServerHeartbeat.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpCenterServerHeartbeat.Size = new Size(409, 39);
            tlpCenterServerHeartbeat.TabIndex = 4;
            // 
            // lblCenterServerHeartbeatInterval
            // 
            lblCenterServerHeartbeatInterval.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblCenterServerHeartbeatInterval.Dock = DockStyle.Fill;
            lblCenterServerHeartbeatInterval.Location = new Point(0, 0);
            lblCenterServerHeartbeatInterval.Margin = new Padding(0);
            lblCenterServerHeartbeatInterval.Name = "lblCenterServerHeartbeatInterval";
            lblCenterServerHeartbeatInterval.Padding = new Padding(6, 0, 0, 0);
            lblCenterServerHeartbeatInterval.Size = new Size(85, 39);
            lblCenterServerHeartbeatInterval.TabIndex = 0;
            lblCenterServerHeartbeatInterval.Text = "心跳间隔(s)";
            // 
            // inputCenterServerHeartbeatInterval
            // 
            inputCenterServerHeartbeatInterval.Dock = DockStyle.Fill;
            inputCenterServerHeartbeatInterval.Location = new Point(85, 0);
            inputCenterServerHeartbeatInterval.Margin = new Padding(0);
            inputCenterServerHeartbeatInterval.Name = "inputCenterServerHeartbeatInterval";
            inputCenterServerHeartbeatInterval.Size = new Size(324, 39);
            inputCenterServerHeartbeatInterval.TabIndex = 1;
            inputCenterServerHeartbeatInterval.Text = "5";
            // 
            // rightSettingsColumn
            // 
            rightSettingsColumn.AutoSize = true;
            rightSettingsColumn.ColumnCount = 1;
            rightSettingsColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightSettingsColumn.Controls.Add(grpMesConfig, 0, 0);
            rightSettingsColumn.Dock = DockStyle.Top;
            rightSettingsColumn.Location = new Point(856, 3);
            rightSettingsColumn.Margin = new Padding(2, 3, 2, 3);
            rightSettingsColumn.Name = "rightSettingsColumn";
            rightSettingsColumn.RowCount = 1;
            rightSettingsColumn.RowStyles.Add(new RowStyle());
            rightSettingsColumn.Size = new Size(426, 777);
            rightSettingsColumn.TabIndex = 2;
            // 
            // grpMesConfig
            // 
            grpMesConfig.AutoSize = true;
            grpMesConfig.Controls.Add(tableLayoutPanelMesConfig);
            grpMesConfig.Dock = DockStyle.Top;
            grpMesConfig.Location = new Point(5, 5);
            grpMesConfig.Margin = new Padding(5);
            grpMesConfig.MinimumSize = new Size(0, 209);
            grpMesConfig.Name = "grpMesConfig";
            grpMesConfig.Padding = new Padding(2, 3, 2, 3);
            grpMesConfig.Size = new Size(416, 767);
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
            tableLayoutPanelMesConfig.Controls.Add(tlpCheckbox2, 0, 3);
            tableLayoutPanelMesConfig.Controls.Add(tlpProcessParameterType, 0, 0);
            tableLayoutPanelMesConfig.Controls.Add(tlpMesUserRoute, 0, 5);
            tableLayoutPanelMesConfig.Controls.Add(tlpCheckbox1, 0, 2);
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
            tableLayoutPanelMesConfig.Controls.Add(tlpMesSysRoute, 0, 16);
            tableLayoutPanelMesConfig.Controls.Add(tlpMesHeartbeat, 0, 1);
            tableLayoutPanelMesConfig.Dock = DockStyle.Top;
            tableLayoutPanelMesConfig.Location = new Point(2, 23);
            tableLayoutPanelMesConfig.Margin = new Padding(2, 3, 2, 3);
            tableLayoutPanelMesConfig.Name = "tableLayoutPanelMesConfig";
            tableLayoutPanelMesConfig.RowCount = 17;
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle());
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle());
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle());
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            tableLayoutPanelMesConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanelMesConfig.Size = new Size(412, 741);
            tableLayoutPanelMesConfig.TabIndex = 0;
            // 
            // tlpCheckbox2
            // 
            tlpCheckbox2.AutoSize = true;
            tlpCheckbox2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpCheckbox2.ColumnCount = 2;
            tlpCheckbox2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCheckbox2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCheckbox2.Controls.Add(chkEnablePostDataCustomHeader, 0, 0);
            tlpCheckbox2.Controls.Add(chkEnableWorkOrderStatusReport, 1, 0);
            tlpCheckbox2.Dock = DockStyle.Fill;
            tlpCheckbox2.Location = new Point(0, 195);
            tlpCheckbox2.Margin = new Padding(0);
            tlpCheckbox2.MinimumSize = new Size(0, 39);
            tlpCheckbox2.Name = "tlpCheckbox2";
            tlpCheckbox2.RowCount = 1;
            tlpCheckbox2.RowStyles.Add(new RowStyle());
            tlpCheckbox2.Size = new Size(412, 39);
            tlpCheckbox2.TabIndex = 7;
            // 
            // chkEnablePostDataCustomHeader
            // 
            chkEnablePostDataCustomHeader.Dock = DockStyle.Fill;
            chkEnablePostDataCustomHeader.Location = new Point(0, 0);
            chkEnablePostDataCustomHeader.Margin = new Padding(0);
            chkEnablePostDataCustomHeader.Name = "chkEnablePostDataCustomHeader";
            chkEnablePostDataCustomHeader.Padding = new Padding(6, 0, 0, 0);
            chkEnablePostDataCustomHeader.Size = new Size(206, 39);
            chkEnablePostDataCustomHeader.TabIndex = 19;
            chkEnablePostDataCustomHeader.Text = "PostData启用Header";
            // 
            // chkEnableWorkOrderStatusReport
            // 
            chkEnableWorkOrderStatusReport.Dock = DockStyle.Fill;
            chkEnableWorkOrderStatusReport.Location = new Point(206, 0);
            chkEnableWorkOrderStatusReport.Margin = new Padding(0);
            chkEnableWorkOrderStatusReport.Name = "chkEnableWorkOrderStatusReport";
            chkEnableWorkOrderStatusReport.Padding = new Padding(6, 0, 0, 0);
            chkEnableWorkOrderStatusReport.Size = new Size(206, 39);
            chkEnableWorkOrderStatusReport.TabIndex = 6;
            chkEnableWorkOrderStatusReport.Text = "启用工单状态上报";
            // 
            // tlpProcessParameterType
            // 
            tlpProcessParameterType.AutoSize = true;
            tlpProcessParameterType.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpProcessParameterType.ColumnCount = 2;
            tlpProcessParameterType.ColumnStyles.Add(new ColumnStyle());
            tlpProcessParameterType.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpProcessParameterType.Controls.Add(lblProcessParameterDeviceType, 0, 0);
            tlpProcessParameterType.Controls.Add(selectProcessParameterDeviceType, 1, 0);
            tlpProcessParameterType.Controls.Add(tlpInspectionResultSource, 0, 1);
            tlpProcessParameterType.Controls.Add(tlpRealtimePointNumberSource, 0, 2);
            tlpProcessParameterType.Dock = DockStyle.Fill;
            tlpProcessParameterType.Location = new Point(0, 0);
            tlpProcessParameterType.Margin = new Padding(0);
            tlpProcessParameterType.Name = "tlpProcessParameterType";
            tlpProcessParameterType.RowCount = 3;
            tlpProcessParameterType.RowStyles.Add(new RowStyle());
            tlpProcessParameterType.RowStyles.Add(new RowStyle());
            tlpProcessParameterType.RowStyles.Add(new RowStyle());
            tlpProcessParameterType.Size = new Size(412, 117);
            tlpProcessParameterType.TabIndex = 7;
            // 
            // lblProcessParameterDeviceType
            // 
            lblProcessParameterDeviceType.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProcessParameterDeviceType.Dock = DockStyle.Fill;
            lblProcessParameterDeviceType.Location = new Point(0, 0);
            lblProcessParameterDeviceType.Margin = new Padding(0);
            lblProcessParameterDeviceType.Name = "lblProcessParameterDeviceType";
            lblProcessParameterDeviceType.Padding = new Padding(8, 0, 0, 0);
            lblProcessParameterDeviceType.Size = new Size(134, 39);
            lblProcessParameterDeviceType.TabIndex = 2;
            lblProcessParameterDeviceType.Text = "过程参数设备类型";
            // 
            // selectProcessParameterDeviceType
            // 
            selectProcessParameterDeviceType.Dock = DockStyle.Fill;
            selectProcessParameterDeviceType.Location = new Point(134, 0);
            selectProcessParameterDeviceType.Margin = new Padding(0);
            selectProcessParameterDeviceType.MaxCount = 10;
            selectProcessParameterDeviceType.Name = "selectProcessParameterDeviceType";
            selectProcessParameterDeviceType.Size = new Size(278, 39);
            selectProcessParameterDeviceType.TabIndex = 3;
            // 
            // tlpInspectionResultSource
            // 
            tlpInspectionResultSource.ColumnCount = 2;
            tlpProcessParameterType.SetColumnSpan(tlpInspectionResultSource, 2);
            tlpInspectionResultSource.ColumnStyles.Add(new ColumnStyle());
            tlpInspectionResultSource.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpInspectionResultSource.Controls.Add(lblInspectionResultSource, 0, 0);
            tlpInspectionResultSource.Controls.Add(selectInspectionResultSource, 1, 0);
            tlpInspectionResultSource.Dock = DockStyle.Fill;
            tlpInspectionResultSource.Location = new Point(0, 39);
            tlpInspectionResultSource.Margin = new Padding(0);
            tlpInspectionResultSource.Name = "tlpInspectionResultSource";
            tlpInspectionResultSource.RowCount = 1;
            tlpInspectionResultSource.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpInspectionResultSource.Size = new Size(412, 39);
            tlpInspectionResultSource.TabIndex = 8;
            // 
            // lblInspectionResultSource
            // 
            lblInspectionResultSource.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblInspectionResultSource.Dock = DockStyle.Fill;
            lblInspectionResultSource.Location = new Point(0, 0);
            lblInspectionResultSource.Margin = new Padding(0);
            lblInspectionResultSource.Name = "lblInspectionResultSource";
            lblInspectionResultSource.Padding = new Padding(8, 0, 0, 0);
            lblInspectionResultSource.Size = new Size(103, 39);
            lblInspectionResultSource.TabIndex = 0;
            lblInspectionResultSource.Text = "检测结果来源";
            // 
            // selectInspectionResultSource
            // 
            selectInspectionResultSource.Dock = DockStyle.Fill;
            selectInspectionResultSource.Location = new Point(103, 0);
            selectInspectionResultSource.Margin = new Padding(0);
            selectInspectionResultSource.MaxCount = 10;
            selectInspectionResultSource.Name = "selectInspectionResultSource";
            selectInspectionResultSource.Size = new Size(309, 39);
            selectInspectionResultSource.TabIndex = 1;
            // 
            // tlpRealtimePointNumberSource
            // 
            tlpRealtimePointNumberSource.ColumnCount = 2;
            tlpProcessParameterType.SetColumnSpan(tlpRealtimePointNumberSource, 2);
            tlpRealtimePointNumberSource.ColumnStyles.Add(new ColumnStyle());
            tlpRealtimePointNumberSource.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRealtimePointNumberSource.Controls.Add(lblRealtimePointNumberSource, 0, 0);
            tlpRealtimePointNumberSource.Controls.Add(selectRealtimePointNumberSource, 1, 0);
            tlpRealtimePointNumberSource.Dock = DockStyle.Fill;
            tlpRealtimePointNumberSource.Location = new Point(0, 78);
            tlpRealtimePointNumberSource.Margin = new Padding(0);
            tlpRealtimePointNumberSource.Name = "tlpRealtimePointNumberSource";
            tlpRealtimePointNumberSource.RowCount = 1;
            tlpRealtimePointNumberSource.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpRealtimePointNumberSource.Size = new Size(412, 39);
            tlpRealtimePointNumberSource.TabIndex = 9;
            // 
            // lblRealtimePointNumberSource
            // 
            lblRealtimePointNumberSource.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblRealtimePointNumberSource.Dock = DockStyle.Fill;
            lblRealtimePointNumberSource.Location = new Point(0, 0);
            lblRealtimePointNumberSource.Margin = new Padding(0);
            lblRealtimePointNumberSource.Name = "lblRealtimePointNumberSource";
            lblRealtimePointNumberSource.Padding = new Padding(8, 0, 0, 0);
            lblRealtimePointNumberSource.Size = new Size(134, 39);
            lblRealtimePointNumberSource.TabIndex = 0;
            lblRealtimePointNumberSource.Text = "实时焊点编号来源";
            // 
            // selectRealtimePointNumberSource
            // 
            selectRealtimePointNumberSource.Dock = DockStyle.Fill;
            selectRealtimePointNumberSource.Location = new Point(134, 0);
            selectRealtimePointNumberSource.Margin = new Padding(0);
            selectRealtimePointNumberSource.MaxCount = 10;
            selectRealtimePointNumberSource.Name = "selectRealtimePointNumberSource";
            selectRealtimePointNumberSource.Size = new Size(278, 39);
            selectRealtimePointNumberSource.TabIndex = 1;
            // 
            // tlpMesUserRoute
            // 
            tlpMesUserRoute.ColumnCount = 2;
            tlpMesUserRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tlpMesUserRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesUserRoute.Controls.Add(lblMesUserRoute, 0, 0);
            tlpMesUserRoute.Controls.Add(inputMesUserRoute, 1, 0);
            tlpMesUserRoute.Dock = DockStyle.Fill;
            tlpMesUserRoute.Location = new Point(0, 273);
            tlpMesUserRoute.Margin = new Padding(0);
            tlpMesUserRoute.Name = "tlpMesUserRoute";
            tlpMesUserRoute.RowCount = 1;
            tlpMesUserRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesUserRoute.Size = new Size(412, 39);
            tlpMesUserRoute.TabIndex = 8;
            // 
            // lblMesUserRoute
            // 
            lblMesUserRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesUserRoute.Dock = DockStyle.Fill;
            lblMesUserRoute.Location = new Point(0, 0);
            lblMesUserRoute.Margin = new Padding(0);
            lblMesUserRoute.Name = "lblMesUserRoute";
            lblMesUserRoute.Padding = new Padding(8, 0, 0, 0);
            lblMesUserRoute.Size = new Size(103, 39);
            lblMesUserRoute.TabIndex = 0;
            lblMesUserRoute.Text = "员工信息路由";
            // 
            // inputMesUserRoute
            // 
            inputMesUserRoute.Dock = DockStyle.Fill;
            inputMesUserRoute.Location = new Point(120, 0);
            inputMesUserRoute.Margin = new Padding(0);
            inputMesUserRoute.Name = "inputMesUserRoute";
            inputMesUserRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesUserRoute.Size = new Size(292, 39);
            inputMesUserRoute.TabIndex = 1;
            // 
            // tlpCheckbox1
            // 
            tlpCheckbox1.AutoSize = true;
            tlpCheckbox1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpCheckbox1.ColumnCount = 2;
            tableLayoutPanelMesConfig.SetColumnSpan(tlpCheckbox1, 2);
            tlpCheckbox1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCheckbox1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCheckbox1.Controls.Add(chkShowTestFlagInHistory, 0, 0);
            tlpCheckbox1.Controls.Add(chkEnableDeviceStatusReport, 1, 0);
            tlpCheckbox1.Dock = DockStyle.Fill;
            tlpCheckbox1.Location = new Point(0, 156);
            tlpCheckbox1.Margin = new Padding(0);
            tlpCheckbox1.MinimumSize = new Size(0, 39);
            tlpCheckbox1.Name = "tlpCheckbox1";
            tlpCheckbox1.RowCount = 1;
            tlpCheckbox1.RowStyles.Add(new RowStyle());
            tlpCheckbox1.Size = new Size(412, 39);
            tlpCheckbox1.TabIndex = 1;
            // 
            // chkShowTestFlagInHistory
            // 
            chkShowTestFlagInHistory.Checked = true;
            chkShowTestFlagInHistory.CheckState = CheckState.Checked;
            chkShowTestFlagInHistory.Dock = DockStyle.Fill;
            chkShowTestFlagInHistory.Location = new Point(0, 0);
            chkShowTestFlagInHistory.Margin = new Padding(0);
            chkShowTestFlagInHistory.Name = "chkShowTestFlagInHistory";
            chkShowTestFlagInHistory.Padding = new Padding(6, 0, 0, 0);
            chkShowTestFlagInHistory.Size = new Size(206, 39);
            chkShowTestFlagInHistory.TabIndex = 4;
            chkShowTestFlagInHistory.Text = "产品历史显示试焊件";
            // 
            // chkEnableDeviceStatusReport
            // 
            chkEnableDeviceStatusReport.Checked = true;
            chkEnableDeviceStatusReport.CheckState = CheckState.Checked;
            chkEnableDeviceStatusReport.Dock = DockStyle.Fill;
            chkEnableDeviceStatusReport.Location = new Point(206, 0);
            chkEnableDeviceStatusReport.Margin = new Padding(0);
            chkEnableDeviceStatusReport.Name = "chkEnableDeviceStatusReport";
            chkEnableDeviceStatusReport.Padding = new Padding(6, 0, 0, 0);
            chkEnableDeviceStatusReport.Size = new Size(206, 39);
            chkEnableDeviceStatusReport.TabIndex = 5;
            chkEnableDeviceStatusReport.Text = "启用设备状态上报";
            // 
            // tlpMesWorkOrderRoute
            // 
            tlpMesWorkOrderRoute.ColumnCount = 2;
            tlpMesWorkOrderRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tlpMesWorkOrderRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesWorkOrderRoute.Controls.Add(lblMesWorkOrderRoute, 0, 0);
            tlpMesWorkOrderRoute.Controls.Add(inputMesWorkOrderRoute, 1, 0);
            tlpMesWorkOrderRoute.Dock = DockStyle.Fill;
            tlpMesWorkOrderRoute.Location = new Point(0, 312);
            tlpMesWorkOrderRoute.Margin = new Padding(0);
            tlpMesWorkOrderRoute.Name = "tlpMesWorkOrderRoute";
            tlpMesWorkOrderRoute.RowCount = 1;
            tlpMesWorkOrderRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesWorkOrderRoute.Size = new Size(412, 39);
            tlpMesWorkOrderRoute.TabIndex = 9;
            // 
            // lblMesWorkOrderRoute
            // 
            lblMesWorkOrderRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesWorkOrderRoute.Dock = DockStyle.Fill;
            lblMesWorkOrderRoute.Location = new Point(0, 0);
            lblMesWorkOrderRoute.Margin = new Padding(0);
            lblMesWorkOrderRoute.Name = "lblMesWorkOrderRoute";
            lblMesWorkOrderRoute.Padding = new Padding(8, 0, 0, 0);
            lblMesWorkOrderRoute.Size = new Size(103, 39);
            lblMesWorkOrderRoute.TabIndex = 0;
            lblMesWorkOrderRoute.Text = "工单信息路由";
            // 
            // inputMesWorkOrderRoute
            // 
            inputMesWorkOrderRoute.Dock = DockStyle.Fill;
            inputMesWorkOrderRoute.Location = new Point(120, 0);
            inputMesWorkOrderRoute.Margin = new Padding(0);
            inputMesWorkOrderRoute.Name = "inputMesWorkOrderRoute";
            inputMesWorkOrderRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesWorkOrderRoute.Size = new Size(292, 39);
            inputMesWorkOrderRoute.TabIndex = 1;
            // 
            // tlpMesServerTimeRoute
            // 
            tlpMesServerTimeRoute.ColumnCount = 2;
            tlpMesServerTimeRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tlpMesServerTimeRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesServerTimeRoute.Controls.Add(lblMesServerTimeRoute, 0, 0);
            tlpMesServerTimeRoute.Controls.Add(inputMesServerTimeRoute, 1, 0);
            tlpMesServerTimeRoute.Dock = DockStyle.Fill;
            tlpMesServerTimeRoute.Location = new Point(0, 351);
            tlpMesServerTimeRoute.Margin = new Padding(0);
            tlpMesServerTimeRoute.Name = "tlpMesServerTimeRoute";
            tlpMesServerTimeRoute.RowCount = 1;
            tlpMesServerTimeRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesServerTimeRoute.Size = new Size(412, 39);
            tlpMesServerTimeRoute.TabIndex = 10;
            // 
            // lblMesServerTimeRoute
            // 
            lblMesServerTimeRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesServerTimeRoute.Dock = DockStyle.Fill;
            lblMesServerTimeRoute.Location = new Point(0, 0);
            lblMesServerTimeRoute.Margin = new Padding(0);
            lblMesServerTimeRoute.Name = "lblMesServerTimeRoute";
            lblMesServerTimeRoute.Padding = new Padding(8, 0, 0, 0);
            lblMesServerTimeRoute.Size = new Size(103, 39);
            lblMesServerTimeRoute.TabIndex = 0;
            lblMesServerTimeRoute.Text = "设备校时路由";
            // 
            // inputMesServerTimeRoute
            // 
            inputMesServerTimeRoute.Dock = DockStyle.Fill;
            inputMesServerTimeRoute.Location = new Point(120, 0);
            inputMesServerTimeRoute.Margin = new Padding(0);
            inputMesServerTimeRoute.Name = "inputMesServerTimeRoute";
            inputMesServerTimeRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesServerTimeRoute.Size = new Size(292, 39);
            inputMesServerTimeRoute.TabIndex = 1;
            // 
            // tlpPostDataHeader
            // 
            tlpPostDataHeader.ColumnCount = 4;
            tlpPostDataHeader.ColumnStyles.Add(new ColumnStyle());
            tlpPostDataHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpPostDataHeader.ColumnStyles.Add(new ColumnStyle());
            tlpPostDataHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpPostDataHeader.Controls.Add(inputPostDataHeaderValue, 3, 0);
            tlpPostDataHeader.Controls.Add(lblPostDataHeaderValue, 2, 0);
            tlpPostDataHeader.Controls.Add(inputPostDataHeaderKey, 1, 0);
            tlpPostDataHeader.Controls.Add(lblPostDataHeaderKey, 0, 0);
            tlpPostDataHeader.Dock = DockStyle.Fill;
            tlpPostDataHeader.Location = new Point(0, 234);
            tlpPostDataHeader.Margin = new Padding(0);
            tlpPostDataHeader.Name = "tlpPostDataHeader";
            tlpPostDataHeader.RowCount = 1;
            tlpPostDataHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpPostDataHeader.Size = new Size(412, 39);
            tlpPostDataHeader.TabIndex = 20;
            // 
            // inputPostDataHeaderValue
            // 
            inputPostDataHeaderValue.Dock = DockStyle.Fill;
            inputPostDataHeaderValue.Location = new Point(297, 0);
            inputPostDataHeaderValue.Margin = new Padding(0);
            inputPostDataHeaderValue.Name = "inputPostDataHeaderValue";
            inputPostDataHeaderValue.Padding = new Padding(2, 0, 0, 0);
            inputPostDataHeaderValue.Size = new Size(115, 39);
            inputPostDataHeaderValue.TabIndex = 1;
            // 
            // lblPostDataHeaderValue
            // 
            lblPostDataHeaderValue.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPostDataHeaderValue.Dock = DockStyle.Fill;
            lblPostDataHeaderValue.Location = new Point(199, 0);
            lblPostDataHeaderValue.Margin = new Padding(0);
            lblPostDataHeaderValue.Name = "lblPostDataHeaderValue";
            lblPostDataHeaderValue.Padding = new Padding(8, 0, 0, 0);
            lblPostDataHeaderValue.Size = new Size(98, 39);
            lblPostDataHeaderValue.TabIndex = 0;
            lblPostDataHeaderValue.Text = "Header Value";
            // 
            // inputPostDataHeaderKey
            // 
            inputPostDataHeaderKey.Dock = DockStyle.Fill;
            inputPostDataHeaderKey.Location = new Point(84, 0);
            inputPostDataHeaderKey.Margin = new Padding(0);
            inputPostDataHeaderKey.Name = "inputPostDataHeaderKey";
            inputPostDataHeaderKey.Padding = new Padding(2, 0, 0, 0);
            inputPostDataHeaderKey.Size = new Size(115, 39);
            inputPostDataHeaderKey.TabIndex = 1;
            // 
            // lblPostDataHeaderKey
            // 
            lblPostDataHeaderKey.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPostDataHeaderKey.Dock = DockStyle.Fill;
            lblPostDataHeaderKey.Location = new Point(0, 0);
            lblPostDataHeaderKey.Margin = new Padding(0);
            lblPostDataHeaderKey.Name = "lblPostDataHeaderKey";
            lblPostDataHeaderKey.Padding = new Padding(8, 0, 0, 0);
            lblPostDataHeaderKey.Size = new Size(84, 39);
            lblPostDataHeaderKey.TabIndex = 0;
            lblPostDataHeaderKey.Text = "Header Key";
            // 
            // tlpMesProgramManageRoute
            // 
            tlpMesProgramManageRoute.ColumnCount = 2;
            tlpMesProgramManageRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tlpMesProgramManageRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesProgramManageRoute.Controls.Add(lblMesProgramManageRoute, 0, 0);
            tlpMesProgramManageRoute.Controls.Add(inputMesProgramManageRoute, 1, 0);
            tlpMesProgramManageRoute.Dock = DockStyle.Fill;
            tlpMesProgramManageRoute.Location = new Point(0, 390);
            tlpMesProgramManageRoute.Margin = new Padding(0);
            tlpMesProgramManageRoute.Name = "tlpMesProgramManageRoute";
            tlpMesProgramManageRoute.RowCount = 1;
            tlpMesProgramManageRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesProgramManageRoute.Size = new Size(412, 39);
            tlpMesProgramManageRoute.TabIndex = 11;
            // 
            // lblMesProgramManageRoute
            // 
            lblMesProgramManageRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesProgramManageRoute.Dock = DockStyle.Fill;
            lblMesProgramManageRoute.Location = new Point(0, 0);
            lblMesProgramManageRoute.Margin = new Padding(0);
            lblMesProgramManageRoute.Name = "lblMesProgramManageRoute";
            lblMesProgramManageRoute.Padding = new Padding(8, 0, 0, 0);
            lblMesProgramManageRoute.Size = new Size(103, 39);
            lblMesProgramManageRoute.TabIndex = 0;
            lblMesProgramManageRoute.Text = "程序管理路由";
            // 
            // inputMesProgramManageRoute
            // 
            inputMesProgramManageRoute.Dock = DockStyle.Fill;
            inputMesProgramManageRoute.Location = new Point(120, 0);
            inputMesProgramManageRoute.Margin = new Padding(0);
            inputMesProgramManageRoute.Name = "inputMesProgramManageRoute";
            inputMesProgramManageRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesProgramManageRoute.Size = new Size(292, 39);
            inputMesProgramManageRoute.TabIndex = 1;
            // 
            // tlpMesStartWorkRoute
            // 
            tlpMesStartWorkRoute.ColumnCount = 2;
            tlpMesStartWorkRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tlpMesStartWorkRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesStartWorkRoute.Controls.Add(lblMesStartWorkRoute, 0, 0);
            tlpMesStartWorkRoute.Controls.Add(inputMesStartWorkRoute, 1, 0);
            tlpMesStartWorkRoute.Dock = DockStyle.Fill;
            tlpMesStartWorkRoute.Location = new Point(0, 429);
            tlpMesStartWorkRoute.Margin = new Padding(0);
            tlpMesStartWorkRoute.Name = "tlpMesStartWorkRoute";
            tlpMesStartWorkRoute.RowCount = 1;
            tlpMesStartWorkRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesStartWorkRoute.Size = new Size(412, 39);
            tlpMesStartWorkRoute.TabIndex = 12;
            // 
            // lblMesStartWorkRoute
            // 
            lblMesStartWorkRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesStartWorkRoute.Dock = DockStyle.Fill;
            lblMesStartWorkRoute.Location = new Point(0, 0);
            lblMesStartWorkRoute.Margin = new Padding(0);
            lblMesStartWorkRoute.Name = "lblMesStartWorkRoute";
            lblMesStartWorkRoute.Padding = new Padding(8, 0, 0, 0);
            lblMesStartWorkRoute.Size = new Size(103, 39);
            lblMesStartWorkRoute.TabIndex = 0;
            lblMesStartWorkRoute.Text = "开工上报路由";
            // 
            // inputMesStartWorkRoute
            // 
            inputMesStartWorkRoute.Dock = DockStyle.Fill;
            inputMesStartWorkRoute.Location = new Point(120, 0);
            inputMesStartWorkRoute.Margin = new Padding(0);
            inputMesStartWorkRoute.Name = "inputMesStartWorkRoute";
            inputMesStartWorkRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesStartWorkRoute.Size = new Size(292, 39);
            inputMesStartWorkRoute.TabIndex = 1;
            // 
            // tlpMesWorkStatusRoute
            // 
            tlpMesWorkStatusRoute.ColumnCount = 2;
            tlpMesWorkStatusRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tlpMesWorkStatusRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesWorkStatusRoute.Controls.Add(lblMesWorkStatusRoute, 0, 0);
            tlpMesWorkStatusRoute.Controls.Add(inputMesWorkStatusRoute, 1, 0);
            tlpMesWorkStatusRoute.Dock = DockStyle.Fill;
            tlpMesWorkStatusRoute.Location = new Point(0, 468);
            tlpMesWorkStatusRoute.Margin = new Padding(0);
            tlpMesWorkStatusRoute.Name = "tlpMesWorkStatusRoute";
            tlpMesWorkStatusRoute.RowCount = 1;
            tlpMesWorkStatusRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesWorkStatusRoute.Size = new Size(412, 39);
            tlpMesWorkStatusRoute.TabIndex = 13;
            // 
            // lblMesWorkStatusRoute
            // 
            lblMesWorkStatusRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesWorkStatusRoute.Dock = DockStyle.Fill;
            lblMesWorkStatusRoute.Location = new Point(0, 0);
            lblMesWorkStatusRoute.Margin = new Padding(0);
            lblMesWorkStatusRoute.Name = "lblMesWorkStatusRoute";
            lblMesWorkStatusRoute.Padding = new Padding(8, 0, 0, 0);
            lblMesWorkStatusRoute.Size = new Size(103, 39);
            lblMesWorkStatusRoute.TabIndex = 0;
            lblMesWorkStatusRoute.Text = "工单状态路由";
            // 
            // inputMesWorkStatusRoute
            // 
            inputMesWorkStatusRoute.Dock = DockStyle.Fill;
            inputMesWorkStatusRoute.Location = new Point(120, 0);
            inputMesWorkStatusRoute.Margin = new Padding(0);
            inputMesWorkStatusRoute.Name = "inputMesWorkStatusRoute";
            inputMesWorkStatusRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesWorkStatusRoute.Size = new Size(292, 39);
            inputMesWorkStatusRoute.TabIndex = 1;
            // 
            // tlpMesEndWorkRoute
            // 
            tlpMesEndWorkRoute.ColumnCount = 2;
            tlpMesEndWorkRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tlpMesEndWorkRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesEndWorkRoute.Controls.Add(lblMesEndWorkRoute, 0, 0);
            tlpMesEndWorkRoute.Controls.Add(inputMesEndWorkRoute, 1, 0);
            tlpMesEndWorkRoute.Dock = DockStyle.Fill;
            tlpMesEndWorkRoute.Location = new Point(0, 507);
            tlpMesEndWorkRoute.Margin = new Padding(0);
            tlpMesEndWorkRoute.Name = "tlpMesEndWorkRoute";
            tlpMesEndWorkRoute.RowCount = 1;
            tlpMesEndWorkRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesEndWorkRoute.Size = new Size(412, 39);
            tlpMesEndWorkRoute.TabIndex = 14;
            // 
            // lblMesEndWorkRoute
            // 
            lblMesEndWorkRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesEndWorkRoute.Dock = DockStyle.Fill;
            lblMesEndWorkRoute.Location = new Point(0, 0);
            lblMesEndWorkRoute.Margin = new Padding(0);
            lblMesEndWorkRoute.Name = "lblMesEndWorkRoute";
            lblMesEndWorkRoute.Padding = new Padding(8, 0, 0, 0);
            lblMesEndWorkRoute.Size = new Size(103, 39);
            lblMesEndWorkRoute.TabIndex = 0;
            lblMesEndWorkRoute.Text = "完工上报路由";
            // 
            // inputMesEndWorkRoute
            // 
            inputMesEndWorkRoute.Dock = DockStyle.Fill;
            inputMesEndWorkRoute.Location = new Point(120, 0);
            inputMesEndWorkRoute.Margin = new Padding(0);
            inputMesEndWorkRoute.Name = "inputMesEndWorkRoute";
            inputMesEndWorkRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesEndWorkRoute.Size = new Size(292, 39);
            inputMesEndWorkRoute.TabIndex = 1;
            // 
            // tlpMesReportFileRoute
            // 
            tlpMesReportFileRoute.ColumnCount = 2;
            tlpMesReportFileRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tlpMesReportFileRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesReportFileRoute.Controls.Add(lblMesReportFileRoute, 0, 0);
            tlpMesReportFileRoute.Controls.Add(inputMesReportFileRoute, 1, 0);
            tlpMesReportFileRoute.Dock = DockStyle.Fill;
            tlpMesReportFileRoute.Location = new Point(0, 546);
            tlpMesReportFileRoute.Margin = new Padding(0);
            tlpMesReportFileRoute.Name = "tlpMesReportFileRoute";
            tlpMesReportFileRoute.RowCount = 1;
            tlpMesReportFileRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesReportFileRoute.Size = new Size(412, 39);
            tlpMesReportFileRoute.TabIndex = 15;
            // 
            // lblMesReportFileRoute
            // 
            lblMesReportFileRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesReportFileRoute.Dock = DockStyle.Fill;
            lblMesReportFileRoute.Location = new Point(0, 0);
            lblMesReportFileRoute.Margin = new Padding(0);
            lblMesReportFileRoute.Name = "lblMesReportFileRoute";
            lblMesReportFileRoute.Padding = new Padding(8, 0, 0, 0);
            lblMesReportFileRoute.Size = new Size(103, 39);
            lblMesReportFileRoute.TabIndex = 0;
            lblMesReportFileRoute.Text = "报告文件路由";
            // 
            // inputMesReportFileRoute
            // 
            inputMesReportFileRoute.Dock = DockStyle.Fill;
            inputMesReportFileRoute.Location = new Point(120, 0);
            inputMesReportFileRoute.Margin = new Padding(0);
            inputMesReportFileRoute.Name = "inputMesReportFileRoute";
            inputMesReportFileRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesReportFileRoute.Size = new Size(292, 39);
            inputMesReportFileRoute.TabIndex = 1;
            // 
            // tlpMesPostDataRoute
            // 
            tlpMesPostDataRoute.ColumnCount = 2;
            tlpMesPostDataRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tlpMesPostDataRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesPostDataRoute.Controls.Add(lblMesPostDataRoute, 0, 0);
            tlpMesPostDataRoute.Controls.Add(inputMesPostDataRoute, 1, 0);
            tlpMesPostDataRoute.Dock = DockStyle.Fill;
            tlpMesPostDataRoute.Location = new Point(0, 585);
            tlpMesPostDataRoute.Margin = new Padding(0);
            tlpMesPostDataRoute.Name = "tlpMesPostDataRoute";
            tlpMesPostDataRoute.RowCount = 1;
            tlpMesPostDataRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesPostDataRoute.Size = new Size(412, 39);
            tlpMesPostDataRoute.TabIndex = 16;
            // 
            // lblMesPostDataRoute
            // 
            lblMesPostDataRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesPostDataRoute.Dock = DockStyle.Fill;
            lblMesPostDataRoute.Location = new Point(0, 0);
            lblMesPostDataRoute.Margin = new Padding(0);
            lblMesPostDataRoute.Name = "lblMesPostDataRoute";
            lblMesPostDataRoute.Padding = new Padding(8, 0, 0, 0);
            lblMesPostDataRoute.Size = new Size(103, 39);
            lblMesPostDataRoute.TabIndex = 0;
            lblMesPostDataRoute.Text = "采集参数路由";
            // 
            // inputMesPostDataRoute
            // 
            inputMesPostDataRoute.Dock = DockStyle.Fill;
            inputMesPostDataRoute.Location = new Point(120, 0);
            inputMesPostDataRoute.Margin = new Padding(0);
            inputMesPostDataRoute.Name = "inputMesPostDataRoute";
            inputMesPostDataRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesPostDataRoute.Size = new Size(292, 39);
            inputMesPostDataRoute.TabIndex = 1;
            // 
            // tlpMesDeviceRoute
            // 
            tlpMesDeviceRoute.ColumnCount = 2;
            tlpMesDeviceRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tlpMesDeviceRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesDeviceRoute.Controls.Add(lblMesDeviceRoute, 0, 0);
            tlpMesDeviceRoute.Controls.Add(inputMesDeviceRoute, 1, 0);
            tlpMesDeviceRoute.Dock = DockStyle.Fill;
            tlpMesDeviceRoute.Location = new Point(0, 624);
            tlpMesDeviceRoute.Margin = new Padding(0);
            tlpMesDeviceRoute.Name = "tlpMesDeviceRoute";
            tlpMesDeviceRoute.RowCount = 1;
            tlpMesDeviceRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesDeviceRoute.Size = new Size(412, 39);
            tlpMesDeviceRoute.TabIndex = 17;
            // 
            // lblMesDeviceRoute
            // 
            lblMesDeviceRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesDeviceRoute.Dock = DockStyle.Fill;
            lblMesDeviceRoute.Location = new Point(0, 0);
            lblMesDeviceRoute.Margin = new Padding(0);
            lblMesDeviceRoute.Name = "lblMesDeviceRoute";
            lblMesDeviceRoute.Padding = new Padding(8, 0, 0, 0);
            lblMesDeviceRoute.Size = new Size(103, 39);
            lblMesDeviceRoute.TabIndex = 0;
            lblMesDeviceRoute.Text = "设备编号路由";
            // 
            // inputMesDeviceRoute
            // 
            inputMesDeviceRoute.Dock = DockStyle.Fill;
            inputMesDeviceRoute.Location = new Point(120, 0);
            inputMesDeviceRoute.Margin = new Padding(0);
            inputMesDeviceRoute.Name = "inputMesDeviceRoute";
            inputMesDeviceRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesDeviceRoute.Size = new Size(292, 39);
            inputMesDeviceRoute.TabIndex = 1;
            // 
            // tlpMesDeviceStatusRoute
            // 
            tlpMesDeviceStatusRoute.ColumnCount = 2;
            tlpMesDeviceStatusRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tlpMesDeviceStatusRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesDeviceStatusRoute.Controls.Add(lblMesDeviceStatusRoute, 0, 0);
            tlpMesDeviceStatusRoute.Controls.Add(inputMesDeviceStatusRoute, 1, 0);
            tlpMesDeviceStatusRoute.Dock = DockStyle.Top;
            tlpMesDeviceStatusRoute.Location = new Point(0, 663);
            tlpMesDeviceStatusRoute.Margin = new Padding(0);
            tlpMesDeviceStatusRoute.Name = "tlpMesDeviceStatusRoute";
            tlpMesDeviceStatusRoute.RowCount = 1;
            tlpMesDeviceStatusRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesDeviceStatusRoute.Size = new Size(412, 39);
            tlpMesDeviceStatusRoute.TabIndex = 18;
            // 
            // lblMesDeviceStatusRoute
            // 
            lblMesDeviceStatusRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesDeviceStatusRoute.Dock = DockStyle.Fill;
            lblMesDeviceStatusRoute.Location = new Point(0, 0);
            lblMesDeviceStatusRoute.Margin = new Padding(0);
            lblMesDeviceStatusRoute.Name = "lblMesDeviceStatusRoute";
            lblMesDeviceStatusRoute.Padding = new Padding(8, 0, 0, 0);
            lblMesDeviceStatusRoute.Size = new Size(103, 39);
            lblMesDeviceStatusRoute.TabIndex = 0;
            lblMesDeviceStatusRoute.Text = "设备状态路由";
            // 
            // inputMesDeviceStatusRoute
            // 
            inputMesDeviceStatusRoute.Dock = DockStyle.Fill;
            inputMesDeviceStatusRoute.Location = new Point(120, 0);
            inputMesDeviceStatusRoute.Margin = new Padding(0);
            inputMesDeviceStatusRoute.Name = "inputMesDeviceStatusRoute";
            inputMesDeviceStatusRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesDeviceStatusRoute.Size = new Size(292, 39);
            inputMesDeviceStatusRoute.TabIndex = 1;
            // 
            // tlpMesSysRoute
            // 
            tlpMesSysRoute.ColumnCount = 2;
            tlpMesSysRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tlpMesSysRoute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMesSysRoute.Controls.Add(lblMesSysRoute, 0, 0);
            tlpMesSysRoute.Controls.Add(inputMesSysRoute, 1, 0);
            tlpMesSysRoute.Dock = DockStyle.Fill;
            tlpMesSysRoute.Location = new Point(0, 702);
            tlpMesSysRoute.Margin = new Padding(0);
            tlpMesSysRoute.Name = "tlpMesSysRoute";
            tlpMesSysRoute.RowCount = 1;
            tlpMesSysRoute.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesSysRoute.Size = new Size(412, 39);
            tlpMesSysRoute.TabIndex = 22;
            // 
            // lblMesSysRoute
            // 
            lblMesSysRoute.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesSysRoute.Dock = DockStyle.Fill;
            lblMesSysRoute.Location = new Point(0, 0);
            lblMesSysRoute.Margin = new Padding(0);
            lblMesSysRoute.Name = "lblMesSysRoute";
            lblMesSysRoute.Padding = new Padding(8, 0, 0, 0);
            lblMesSysRoute.Size = new Size(103, 39);
            lblMesSysRoute.TabIndex = 0;
            lblMesSysRoute.Text = "在线检测路由";
            // 
            // inputMesSysRoute
            // 
            inputMesSysRoute.Dock = DockStyle.Fill;
            inputMesSysRoute.Location = new Point(120, 0);
            inputMesSysRoute.Margin = new Padding(0);
            inputMesSysRoute.Name = "inputMesSysRoute";
            inputMesSysRoute.Padding = new Padding(2, 0, 0, 0);
            inputMesSysRoute.Size = new Size(292, 39);
            inputMesSysRoute.TabIndex = 1;
            // 
            // tlpMesHeartbeat
            // 
            tlpMesHeartbeat.ColumnCount = 4;
            tlpMesHeartbeat.ColumnStyles.Add(new ColumnStyle());
            tlpMesHeartbeat.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpMesHeartbeat.ColumnStyles.Add(new ColumnStyle());
            tlpMesHeartbeat.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpMesHeartbeat.Controls.Add(input_MesTimeout, 3, 0);
            tlpMesHeartbeat.Controls.Add(lblMesHeartbeatInterval, 0, 0);
            tlpMesHeartbeat.Controls.Add(lblMesTimeout, 2, 0);
            tlpMesHeartbeat.Controls.Add(inputMesHeartbeatInterval, 1, 0);
            tlpMesHeartbeat.Dock = DockStyle.Fill;
            tlpMesHeartbeat.Location = new Point(0, 117);
            tlpMesHeartbeat.Margin = new Padding(0);
            tlpMesHeartbeat.Name = "tlpMesHeartbeat";
            tlpMesHeartbeat.RowCount = 1;
            tlpMesHeartbeat.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMesHeartbeat.Size = new Size(412, 39);
            tlpMesHeartbeat.TabIndex = 21;
            // 
            // input_MesTimeout
            // 
            input_MesTimeout.Dock = DockStyle.Fill;
            input_MesTimeout.Location = new Point(293, 0);
            input_MesTimeout.Margin = new Padding(0);
            input_MesTimeout.Name = "input_MesTimeout";
            input_MesTimeout.Size = new Size(119, 39);
            input_MesTimeout.TabIndex = 1;
            input_MesTimeout.Text = "0";
            // 
            // lblMesHeartbeatInterval
            // 
            lblMesHeartbeatInterval.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesHeartbeatInterval.Dock = DockStyle.Fill;
            lblMesHeartbeatInterval.Location = new Point(0, 0);
            lblMesHeartbeatInterval.Margin = new Padding(0);
            lblMesHeartbeatInterval.Name = "lblMesHeartbeatInterval";
            lblMesHeartbeatInterval.Padding = new Padding(8, 0, 0, 0);
            lblMesHeartbeatInterval.Size = new Size(87, 39);
            lblMesHeartbeatInterval.TabIndex = 0;
            lblMesHeartbeatInterval.Text = "心跳间隔(s)";
            // 
            // lblMesTimeout
            // 
            lblMesTimeout.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesTimeout.Dock = DockStyle.Fill;
            lblMesTimeout.Location = new Point(205, 0);
            lblMesTimeout.Margin = new Padding(0);
            lblMesTimeout.Name = "lblMesTimeout";
            lblMesTimeout.Padding = new Padding(8, 0, 0, 0);
            lblMesTimeout.Size = new Size(88, 39);
            lblMesTimeout.TabIndex = 0;
            lblMesTimeout.Text = "MES 超时(s)";
            // 
            // inputMesHeartbeatInterval
            // 
            inputMesHeartbeatInterval.Dock = DockStyle.Fill;
            inputMesHeartbeatInterval.Location = new Point(87, 0);
            inputMesHeartbeatInterval.Margin = new Padding(0);
            inputMesHeartbeatInterval.Name = "inputMesHeartbeatInterval";
            inputMesHeartbeatInterval.Padding = new Padding(2, 0, 0, 0);
            inputMesHeartbeatInterval.Size = new Size(118, 39);
            inputMesHeartbeatInterval.TabIndex = 1;
            inputMesHeartbeatInterval.Text = "5";
            // 
            // SystemSettingView
            // 
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(rootLayout);
            Margin = new Padding(2, 3, 2, 3);
            Name = "SystemSettingView";
            Size = new Size(1346, 1012);
            rootLayout.ResumeLayout(false);
            titleLayout.ResumeLayout(false);
            titleLayout.PerformLayout();
            tabSettingCategories.ResumeLayout(false);
            tabBasicSettings.ResumeLayout(false);
            basicSettingsViewport.ResumeLayout(false);
            basicSettingsViewport.PerformLayout();
            basicSettingsLayout.ResumeLayout(false);
            basicSettingsLayout.PerformLayout();
            leftSettingsColumn.ResumeLayout(false);
            leftSettingsColumn.PerformLayout();
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
            tlpPlcAlarmTriggerMode.ResumeLayout(false);
            tlpPlcAlarmTriggerMode.PerformLayout();
            tlpPlcStringNumericMode.ResumeLayout(false);
            tlpPlcStringNumericMode.PerformLayout();
            grpDeviceConfig.ResumeLayout(false);
            grpDeviceConfig.PerformLayout();
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
            tlpMesDeviceStatusQueryRoute.ResumeLayout(false);
            tlpMesDeviceStatusQueryRoute.PerformLayout();
            tlpMesDeviceIdSetRoute.ResumeLayout(false);
            tlpMesDeviceIdSetRoute.PerformLayout();
            middleSettingsColumn.ResumeLayout(false);
            middleSettingsColumn.PerformLayout();
            grpProductionConfig.ResumeLayout(false);
            grpProductionConfig.PerformLayout();
            tlpProductConfig.ResumeLayout(false);
            tlpProductConfig.PerformLayout();
            stationDisplayNameLayout.ResumeLayout(false);
            stationDisplayNameLayout.PerformLayout();
            tlpUploadConfig.ResumeLayout(false);
            tlpUploadConfig.PerformLayout();
            tableLayoutPanelHeartbeat.ResumeLayout(false);
            tableLayoutPanelHeartbeat.PerformLayout();
            grpAppConfig.ResumeLayout(false);
            grpAppConfig.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            tableLayoutPanel4.ResumeLayout(false);
            tlpLogPath.ResumeLayout(false);
            tlpLogPath.PerformLayout();
            tlpDataPath.ResumeLayout(false);
            tlpDataPath.PerformLayout();
            grpCenterServerConfig.ResumeLayout(false);
            grpCenterServerConfig.PerformLayout();
            tableLayoutPanelCenterServer.ResumeLayout(false);
            tlpCenterServerBaseUrl.ResumeLayout(false);
            tlpCenterServerBaseUrl.PerformLayout();
            tlpCenterServerSystemType.ResumeLayout(false);
            tlpCenterServerSystemType.PerformLayout();
            tlpCenterServerHeartbeat.ResumeLayout(false);
            tlpCenterServerHeartbeat.PerformLayout();
            rightSettingsColumn.ResumeLayout(false);
            rightSettingsColumn.PerformLayout();
            grpMesConfig.ResumeLayout(false);
            grpMesConfig.PerformLayout();
            tableLayoutPanelMesConfig.ResumeLayout(false);
            tableLayoutPanelMesConfig.PerformLayout();
            tlpCheckbox2.ResumeLayout(false);
            tlpProcessParameterType.ResumeLayout(false);
            tlpProcessParameterType.PerformLayout();
            tlpInspectionResultSource.ResumeLayout(false);
            tlpInspectionResultSource.PerformLayout();
            tlpRealtimePointNumberSource.ResumeLayout(false);
            tlpRealtimePointNumberSource.PerformLayout();
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
            tlpMesSysRoute.ResumeLayout(false);
            tlpMesSysRoute.PerformLayout();
            tlpMesHeartbeat.ResumeLayout(false);
            tlpMesHeartbeat.PerformLayout();
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
        private AntdUI.Label lblPlcHeartbeatTimeout;
        private AntdUI.Input inputPlcHeartbeatTimeout;
        private AntdUI.Label lblPlcCommunicationTimeout;
        private AntdUI.Input inputPlcCommunicationTimeout;
        private AntdUI.Button btnSaveAll;
        private TableLayoutPanel titleLayout;
        private Label lblTitle;
        private Label lblDescription;
        private TabControl tabSettingCategories;
        private TabPage tabBasicSettings;
        private Panel basicSettingsViewport;
        private TableLayoutPanel basicSettingsLayout;
        private TableLayoutPanel leftSettingsColumn;
        private TableLayoutPanel middleSettingsColumn;
        private TableLayoutPanel rightSettingsColumn;
        private GroupBox grpProductionConfig;
        private TableLayoutPanel tlpProductConfig;
        private GroupBox grpMesConfig;
        private TableLayoutPanel tableLayoutPanelMesConfig;
        private AntdUI.Checkbox chkUseOperatorInputDialog;
        private TableLayoutPanel tableLayoutPanel1;
        private GroupBox grpAppConfig;
        private AntdUI.Label lblMesTimeout;
        private AntdUI.InputNumber input_MesTimeout;
        private TableLayoutPanel tlpCheckbox1;
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
        private TableLayoutPanel tlpMesDeviceStatusQueryRoute;
        private AntdUI.Label lblMesDeviceStatusQueryRoute;
        private AntdUI.Input inputMesDeviceStatusQueryRoute;
        private TableLayoutPanel tlpMesDeviceIdSetRoute;
        private AntdUI.Label lblMesDeviceIdSetRoute;
        private AntdUI.Input inputMesDeviceIdSetRoute;
        private TableLayoutPanel tlpMesSysRoute;
        private AntdUI.Label lblMesSysRoute;
        private AntdUI.Input inputMesSysRoute;
        private TableLayoutPanel tlpMesHeartbeat;
        private AntdUI.Label lblMesHeartbeatInterval;
        private AntdUI.Input inputMesHeartbeatInterval;
        private AntdUI.Label lblProcessParameterDeviceType;
        private AntdUI.Select selectProcessParameterDeviceType;
        private TableLayoutPanel tlpInspectionResultSource;
        private AntdUI.Label lblInspectionResultSource;
        private AntdUI.Select selectInspectionResultSource;
        private TableLayoutPanel tlpRealtimePointNumberSource;
        private AntdUI.Label lblRealtimePointNumberSource;
        private AntdUI.Select selectRealtimePointNumberSource;
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
        private TableLayoutPanel tlpPlcAlarmTriggerMode;
        private AntdUI.Label lblPlcAlarmTriggerMode;
        private AntdUI.Select selectPlcAlarmTriggerMode;
        private TableLayoutPanel tlpProcessParameterType;
        private TableLayoutPanel tlpCheckbox2;
        private TableLayoutPanel tableLayoutPanel4;
        private TableLayoutPanel tlpUploadConfig;
        private TableLayoutPanel stationDisplayNameLayout;
        private AntdUI.Label lblStation1DisplayName;
        private AntdUI.Input inputStation1DisplayName;
        private AntdUI.Label lblStation2DisplayName;
        private AntdUI.Input inputStation2DisplayName;
        private TableLayoutPanel tlpMesUserRoute;
        private AntdUI.Label lblMesUserRoute;
        private AntdUI.Input inputMesUserRoute;
    }
}
