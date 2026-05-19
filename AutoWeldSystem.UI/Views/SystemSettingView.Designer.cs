namespace AutoWeldSystem.UI.Views
{
    partial class SystemSettingView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            grpMasterConfig = new GroupBox();
            tableLayoutPanel2 = new TableLayoutPanel();
            btnConnectMasterController = new AntdUI.Button();
            lblMasterIp = new AntdUI.Label();
            lblMasterPort = new AntdUI.Label();
            input_MasterIp = new AntdUI.Input();
            input_MasterPort = new AntdUI.Input();
            grpPlcConfig = new GroupBox();
            tlpPlcConfig = new TableLayoutPanel();
            btnConnectPlc = new AntdUI.Button();
            lblPlcIp = new AntdUI.Label();
            lblPlcPort = new AntdUI.Label();
            lblPlcType = new AntdUI.Label();
            input_PlcIp = new AntdUI.Input();
            input_PlcPort = new AntdUI.Input();
            select_PlcType = new AntdUI.Select();
            grpAppConfig = new GroupBox();
            tableLayoutPanel3 = new TableLayoutPanel();
            flowPanel2 = new AntdUI.FlowPanel();
            btnOpenDataPath = new AntdUI.Button();
            btnChangeDataPath = new AntdUI.Button();
            flowPanel1 = new AntdUI.FlowPanel();
            btnOpenLogPath = new AntdUI.Button();
            btnChangeLogPath = new AntdUI.Button();
            lblDeviceId = new AntdUI.Label();
            input_DeviceID = new AntdUI.Input();
            btnSyncDevice = new AntdUI.Button();
            lblDeviceName = new AntdUI.Label();
            lblDataPath = new AntdUI.Label();
            lblLogPath = new AntdUI.Label();
            lblMesUrl = new AntdUI.Label();
            input_DeviceName = new AntdUI.Input();
            input_LogsPath = new AntdUI.Input();
            input_DataPath = new AntdUI.Input();
            input_BaseUrl = new AntdUI.Input();
            btnTestConnection = new AntdUI.Button();
            lblDeviceUrl = new AntdUI.Label();
            input_DeviceUrl = new AntdUI.Input();
            chkEnableDualStationMode = new AntdUI.Checkbox();
            panel2 = new AntdUI.Panel();
            btnSaveAll = new AntdUI.Button();
            headerLayout = new TableLayoutPanel();
            titleLayout = new TableLayoutPanel();
            lblTitle = new Label();
            lblDescription = new Label();
            tableLayoutPanel4 = new TableLayoutPanel();
            panel3 = new AntdUI.Panel();
            tabSettingCategories = new TabControl();
            tabBasicSettings = new TabPage();
            panelBasicSettings = new AntdUI.Panel();
            groupBox1 = new GroupBox();
            tableLayoutPanel5 = new TableLayoutPanel();
            chkUseProductNumberFilter = new AntdUI.Checkbox();
            tableLayoutPanel1 = new TableLayoutPanel();
            tabProductProcess = new TabPage();
            productProcessLayout = new TableLayoutPanel();
            productProcessHeaderLayout = new TableLayoutPanel();
            lblProductProcessTitle = new Label();
            lblProductProcessDescription = new Label();
            productProcessToolbar = new FlowLayoutPanel();
            btnRefreshProductProcesses = new AntdUI.Button();
            btnDeleteProductProcess = new AntdUI.Button();
            btnSaveProductProcesses = new AntdUI.Button();
            btnAddProductProcess = new AntdUI.Button();
            tableProductProcesses = new AntdUI.Table();
            tabTestItemTemplates = new TabPage();
            testTemplateLayout = new TableLayoutPanel();
            testTemplateHeaderLayout = new TableLayoutPanel();
            lblTestTemplateTitle = new Label();
            lblTestTemplateDescription = new Label();
            testTemplateBodyLayout = new TableLayoutPanel();
            testTemplateListLayout = new TableLayoutPanel();
            lblTestTemplateListTitle = new Label();
            testTemplateToolbar = new FlowLayoutPanel();
            btnRefreshTestTemplates = new AntdUI.Button();
            btnDeleteTestTemplate = new AntdUI.Button();
            btnSaveTestTemplates = new AntdUI.Button();
            btnAddTestTemplate = new AntdUI.Button();
            tableTestTemplates = new AntdUI.Table();
            grpMasterConfig.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            grpPlcConfig.SuspendLayout();
            tlpPlcConfig.SuspendLayout();
            grpAppConfig.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            flowPanel2.SuspendLayout();
            flowPanel1.SuspendLayout();
            panel2.SuspendLayout();
            headerLayout.SuspendLayout();
            titleLayout.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            panel3.SuspendLayout();
            tabSettingCategories.SuspendLayout();
            tabBasicSettings.SuspendLayout();
            panelBasicSettings.SuspendLayout();
            groupBox1.SuspendLayout();
            tableLayoutPanel5.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            tabProductProcess.SuspendLayout();
            productProcessLayout.SuspendLayout();
            productProcessHeaderLayout.SuspendLayout();
            productProcessToolbar.SuspendLayout();
            tabTestItemTemplates.SuspendLayout();
            testTemplateLayout.SuspendLayout();
            testTemplateHeaderLayout.SuspendLayout();
            testTemplateBodyLayout.SuspendLayout();
            testTemplateListLayout.SuspendLayout();
            testTemplateToolbar.SuspendLayout();
            SuspendLayout();
            //
            // grpMasterConfig
            //
            grpMasterConfig.AutoSize = true;
            grpMasterConfig.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grpMasterConfig.Controls.Add(tableLayoutPanel2);
            grpMasterConfig.Dock = DockStyle.Top;
            grpMasterConfig.Location = new Point(3, 214);
            grpMasterConfig.Name = "grpMasterConfig";
            grpMasterConfig.Size = new Size(358, 153);
            grpMasterConfig.TabIndex = 2;
            grpMasterConfig.TabStop = false;
            grpMasterConfig.Text = "总控参数设定";
            //
            // tableLayoutPanel2
            //
            tableLayoutPanel2.ColumnCount = 3;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel2.Controls.Add(btnConnectMasterController, 2, 0);
            tableLayoutPanel2.Controls.Add(lblMasterIp, 0, 0);
            tableLayoutPanel2.Controls.Add(lblMasterPort, 0, 1);
            tableLayoutPanel2.Controls.Add(input_MasterIp, 1, 0);
            tableLayoutPanel2.Controls.Add(input_MasterPort, 1, 1);
            tableLayoutPanel2.Location = new Point(11, 40);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Size = new Size(341, 84);
            tableLayoutPanel2.TabIndex = 0;
            //
            // btnConnectMasterController
            //
            btnConnectMasterController.BorderWidth = 1F;
            btnConnectMasterController.Dock = DockStyle.Fill;
            btnConnectMasterController.IconSvg = "ApiOutlined";
            btnConnectMasterController.Location = new Point(244, 3);
            btnConnectMasterController.Name = "btnConnectMasterController";
            btnConnectMasterController.Size = new Size(94, 36);
            btnConnectMasterController.TabIndex = 1;
            btnConnectMasterController.Text = "连接";
            //
            // lblMasterIp
            //
            lblMasterIp.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMasterIp.Dock = DockStyle.Fill;
            lblMasterIp.Location = new Point(3, 3);
            lblMasterIp.Name = "lblMasterIp";
            lblMasterIp.Size = new Size(16, 36);
            lblMasterIp.TabIndex = 0;
            lblMasterIp.Text = "IP";
            //
            // lblMasterPort
            //
            lblMasterPort.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMasterPort.Dock = DockStyle.Fill;
            lblMasterPort.Location = new Point(3, 45);
            lblMasterPort.Name = "lblMasterPort";
            lblMasterPort.Size = new Size(35, 36);
            lblMasterPort.TabIndex = 1;
            lblMasterPort.Text = "端口";
            //
            // input_MasterIp
            //
            input_MasterIp.Dock = DockStyle.Fill;
            input_MasterIp.Location = new Point(44, 3);
            input_MasterIp.Name = "input_MasterIp";
            input_MasterIp.Size = new Size(194, 36);
            input_MasterIp.TabIndex = 2;
            input_MasterIp.Text = "127.0.0.1";
            //
            // input_MasterPort
            //
            input_MasterPort.Dock = DockStyle.Fill;
            input_MasterPort.Location = new Point(44, 45);
            input_MasterPort.Name = "input_MasterPort";
            input_MasterPort.Size = new Size(194, 36);
            input_MasterPort.TabIndex = 2;
            input_MasterPort.Text = "6000";
            //
            // grpPlcConfig
            //
            grpPlcConfig.AutoSize = true;
            grpPlcConfig.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grpPlcConfig.Controls.Add(tlpPlcConfig);
            grpPlcConfig.Dock = DockStyle.Fill;
            grpPlcConfig.Location = new Point(3, 3);
            grpPlcConfig.Name = "grpPlcConfig";
            grpPlcConfig.Size = new Size(358, 205);
            grpPlcConfig.TabIndex = 1;
            grpPlcConfig.TabStop = false;
            grpPlcConfig.Text = "PLC参数设定";
            //
            // tlpPlcConfig
            //
            tlpPlcConfig.AutoSize = true;
            tlpPlcConfig.ColumnCount = 3;
            tlpPlcConfig.ColumnStyles.Add(new ColumnStyle());
            tlpPlcConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpPlcConfig.ColumnStyles.Add(new ColumnStyle());
            tlpPlcConfig.Controls.Add(btnConnectPlc, 2, 0);
            tlpPlcConfig.Controls.Add(lblPlcIp, 0, 0);
            tlpPlcConfig.Controls.Add(lblPlcPort, 0, 1);
            tlpPlcConfig.Controls.Add(lblPlcType, 0, 2);
            tlpPlcConfig.Controls.Add(input_PlcIp, 1, 0);
            tlpPlcConfig.Controls.Add(input_PlcPort, 1, 1);
            tlpPlcConfig.Controls.Add(select_PlcType, 1, 2);
            tlpPlcConfig.Location = new Point(11, 50);
            tlpPlcConfig.Name = "tlpPlcConfig";
            tlpPlcConfig.RowCount = 3;
            tlpPlcConfig.RowStyles.Add(new RowStyle());
            tlpPlcConfig.RowStyles.Add(new RowStyle());
            tlpPlcConfig.RowStyles.Add(new RowStyle());
            tlpPlcConfig.Size = new Size(341, 126);
            tlpPlcConfig.TabIndex = 0;
            //
            // btnConnectPlc
            //
            btnConnectPlc.BorderWidth = 1F;
            btnConnectPlc.Dock = DockStyle.Fill;
            btnConnectPlc.IconSvg = "ApiOutlined";
            btnConnectPlc.Location = new Point(244, 3);
            btnConnectPlc.Name = "btnConnectPlc";
            btnConnectPlc.Size = new Size(94, 36);
            btnConnectPlc.TabIndex = 1;
            btnConnectPlc.Text = "连接";
            //
            // lblPlcIp
            //
            lblPlcIp.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcIp.Dock = DockStyle.Fill;
            lblPlcIp.Location = new Point(3, 3);
            lblPlcIp.Name = "lblPlcIp";
            lblPlcIp.Size = new Size(16, 36);
            lblPlcIp.TabIndex = 0;
            lblPlcIp.Text = "IP";
            //
            // lblPlcPort
            //
            lblPlcPort.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcPort.Dock = DockStyle.Fill;
            lblPlcPort.Location = new Point(3, 45);
            lblPlcPort.Name = "lblPlcPort";
            lblPlcPort.Size = new Size(35, 36);
            lblPlcPort.TabIndex = 1;
            lblPlcPort.Text = "端口";
            //
            // lblPlcType
            //
            lblPlcType.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPlcType.Dock = DockStyle.Fill;
            lblPlcType.Location = new Point(3, 87);
            lblPlcType.Name = "lblPlcType";
            lblPlcType.Size = new Size(35, 36);
            lblPlcType.TabIndex = 1;
            lblPlcType.Text = "类型";
            //
            // input_PlcIp
            //
            input_PlcIp.Dock = DockStyle.Fill;
            input_PlcIp.Location = new Point(44, 3);
            input_PlcIp.Name = "input_PlcIp";
            input_PlcIp.Size = new Size(194, 36);
            input_PlcIp.TabIndex = 2;
            input_PlcIp.Text = "127.0.0.1";
            //
            // input_PlcPort
            //
            input_PlcPort.Dock = DockStyle.Fill;
            input_PlcPort.Location = new Point(44, 45);
            input_PlcPort.Name = "input_PlcPort";
            input_PlcPort.Size = new Size(194, 36);
            input_PlcPort.TabIndex = 2;
            input_PlcPort.Text = "6000";
            //
            // select_PlcType
            //
            select_PlcType.Dock = DockStyle.Fill;
            select_PlcType.Location = new Point(44, 87);
            select_PlcType.Name = "select_PlcType";
            select_PlcType.Size = new Size(194, 36);
            select_PlcType.TabIndex = 3;
            //
            // grpAppConfig
            //
            grpAppConfig.AutoSize = true;
            grpAppConfig.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grpAppConfig.Controls.Add(tableLayoutPanel3);
            grpAppConfig.Location = new Point(367, 0);
            grpAppConfig.Name = "grpAppConfig";
            grpAppConfig.Size = new Size(743, 386);
            grpAppConfig.TabIndex = 1;
            grpAppConfig.TabStop = false;
            grpAppConfig.Text = "应用参数设定";
            //
            // tableLayoutPanel3
            //
            tableLayoutPanel3.ColumnCount = 3;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel3.Controls.Add(flowPanel2, 2, 3);
            tableLayoutPanel3.Controls.Add(flowPanel1, 2, 2);
            tableLayoutPanel3.Controls.Add(lblDeviceId, 0, 0);
            tableLayoutPanel3.Controls.Add(input_DeviceID, 1, 0);
            tableLayoutPanel3.Controls.Add(btnSyncDevice, 2, 0);
            tableLayoutPanel3.Controls.Add(lblDeviceName, 0, 1);
            tableLayoutPanel3.Controls.Add(lblDataPath, 0, 3);
            tableLayoutPanel3.Controls.Add(lblLogPath, 0, 2);
            tableLayoutPanel3.Controls.Add(lblMesUrl, 0, 4);
            tableLayoutPanel3.Controls.Add(input_DeviceName, 1, 1);
            tableLayoutPanel3.Controls.Add(input_LogsPath, 1, 2);
            tableLayoutPanel3.Controls.Add(input_DataPath, 1, 3);
            tableLayoutPanel3.Controls.Add(input_BaseUrl, 1, 4);
            tableLayoutPanel3.Controls.Add(btnTestConnection, 2, 4);
            tableLayoutPanel3.Controls.Add(lblDeviceUrl, 0, 5);
            tableLayoutPanel3.Controls.Add(input_DeviceUrl, 1, 5);
            tableLayoutPanel3.Location = new Point(6, 38);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 7;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 14.2857113F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 14.2857151F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 14.2857151F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 14.2857151F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 14.2857151F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 14.2857151F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 14.2857151F));
            tableLayoutPanel3.Size = new Size(731, 319);
            tableLayoutPanel3.TabIndex = 3;
            //
            // flowPanel2
            //
            flowPanel2.Controls.Add(btnOpenDataPath);
            flowPanel2.Controls.Add(btnChangeDataPath);
            flowPanel2.Dock = DockStyle.Fill;
            flowPanel2.Location = new Point(383, 135);
            flowPanel2.Margin = new Padding(0);
            flowPanel2.Name = "flowPanel2";
            flowPanel2.Size = new Size(348, 45);
            flowPanel2.TabIndex = 6;
            flowPanel2.Text = "flowPanel2";
            //
            // btnOpenDataPath
            //
            btnOpenDataPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnOpenDataPath.BorderWidth = 1F;
            btnOpenDataPath.Dock = DockStyle.Right;
            btnOpenDataPath.IconSvg = "FolderOpenOutlined";
            btnOpenDataPath.JoinMode = AntdUI.TJoinMode.Right;
            btnOpenDataPath.Location = new Point(128, 3);
            btnOpenDataPath.Name = "btnOpenDataPath";
            btnOpenDataPath.Size = new Size(189, 39);
            btnOpenDataPath.TabIndex = 6;
            btnOpenDataPath.Text = "打开文件所在位置";
            //
            // btnChangeDataPath
            //
            btnChangeDataPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnChangeDataPath.BorderWidth = 1F;
            btnChangeDataPath.Dock = DockStyle.Left;
            btnChangeDataPath.IconSvg = "EditOutlined";
            btnChangeDataPath.JoinMode = AntdUI.TJoinMode.Left;
            btnChangeDataPath.Location = new Point(3, 3);
            btnChangeDataPath.Name = "btnChangeDataPath";
            btnChangeDataPath.Size = new Size(119, 39);
            btnChangeDataPath.TabIndex = 4;
            btnChangeDataPath.Text = "变更路径";
            //
            // flowPanel1
            //
            flowPanel1.Controls.Add(btnOpenLogPath);
            flowPanel1.Controls.Add(btnChangeLogPath);
            flowPanel1.Dock = DockStyle.Fill;
            flowPanel1.Location = new Point(383, 90);
            flowPanel1.Margin = new Padding(0);
            flowPanel1.Name = "flowPanel1";
            flowPanel1.Size = new Size(348, 45);
            flowPanel1.TabIndex = 6;
            flowPanel1.Text = "flowPanel1";
            //
            // btnOpenLogPath
            //
            btnOpenLogPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnOpenLogPath.BorderWidth = 1F;
            btnOpenLogPath.Dock = DockStyle.Right;
            btnOpenLogPath.IconSvg = "FolderOpenOutlined";
            btnOpenLogPath.JoinMode = AntdUI.TJoinMode.Right;
            btnOpenLogPath.Location = new Point(128, 3);
            btnOpenLogPath.Name = "btnOpenLogPath";
            btnOpenLogPath.Size = new Size(189, 39);
            btnOpenLogPath.TabIndex = 5;
            btnOpenLogPath.Text = "打开文件所在位置";
            //
            // btnChangeLogPath
            //
            btnChangeLogPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnChangeLogPath.BorderWidth = 1F;
            btnChangeLogPath.Dock = DockStyle.Left;
            btnChangeLogPath.IconSvg = "EditOutlined";
            btnChangeLogPath.JoinMode = AntdUI.TJoinMode.Left;
            btnChangeLogPath.Location = new Point(3, 3);
            btnChangeLogPath.Name = "btnChangeLogPath";
            btnChangeLogPath.Size = new Size(119, 39);
            btnChangeLogPath.TabIndex = 4;
            btnChangeLogPath.Text = "变更路径";
            //
            // lblDeviceId
            //
            lblDeviceId.AutoEllipsis = true;
            lblDeviceId.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceId.Dock = DockStyle.Fill;
            lblDeviceId.Location = new Point(3, 3);
            lblDeviceId.Name = "lblDeviceId";
            lblDeviceId.Size = new Size(70, 39);
            lblDeviceId.TabIndex = 0;
            lblDeviceId.Text = "设备编号";
            //
            // input_DeviceID
            //
            input_DeviceID.Dock = DockStyle.Fill;
            input_DeviceID.Location = new Point(88, 3);
            input_DeviceID.Name = "input_DeviceID";
            input_DeviceID.Size = new Size(292, 39);
            input_DeviceID.TabIndex = 2;
            //
            // btnSyncDevice
            //
            btnSyncDevice.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSyncDevice.BorderWidth = 1F;
            btnSyncDevice.Dock = DockStyle.Fill;
            btnSyncDevice.IconSvg = "CloudUploadOutlined";
            btnSyncDevice.Location = new Point(386, 3);
            btnSyncDevice.Name = "btnSyncDevice";
            btnSyncDevice.Size = new Size(144, 39);
            btnSyncDevice.TabIndex = 9;
            btnSyncDevice.Text = "同步到MES";
            //
            // lblDeviceName
            //
            lblDeviceName.AutoEllipsis = true;
            lblDeviceName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceName.Dock = DockStyle.Fill;
            lblDeviceName.Location = new Point(3, 48);
            lblDeviceName.Name = "lblDeviceName";
            lblDeviceName.Size = new Size(79, 39);
            lblDeviceName.TabIndex = 0;
            lblDeviceName.Text = "设备名称\r\n";
            //
            // lblDataPath
            //
            lblDataPath.AutoEllipsis = true;
            lblDataPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDataPath.Dock = DockStyle.Fill;
            lblDataPath.Location = new Point(3, 138);
            lblDataPath.Name = "lblDataPath";
            lblDataPath.Size = new Size(70, 39);
            lblDataPath.TabIndex = 0;
            lblDataPath.Text = "数据路径";
            //
            // lblLogPath
            //
            lblLogPath.AutoEllipsis = true;
            lblLogPath.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblLogPath.Dock = DockStyle.Fill;
            lblLogPath.Location = new Point(3, 93);
            lblLogPath.Name = "lblLogPath";
            lblLogPath.Size = new Size(70, 39);
            lblLogPath.TabIndex = 0;
            lblLogPath.Text = "日志路径";
            //
            // lblMesUrl
            //
            lblMesUrl.AutoEllipsis = true;
            lblMesUrl.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblMesUrl.Dock = DockStyle.Fill;
            lblMesUrl.Location = new Point(3, 183);
            lblMesUrl.Name = "lblMesUrl";
            lblMesUrl.Size = new Size(72, 39);
            lblMesUrl.TabIndex = 0;
            lblMesUrl.Text = "MES地址";
            //
            // input_DeviceName
            //
            input_DeviceName.Dock = DockStyle.Fill;
            input_DeviceName.Location = new Point(88, 48);
            input_DeviceName.Name = "input_DeviceName";
            input_DeviceName.Size = new Size(292, 39);
            input_DeviceName.TabIndex = 2;
            input_DeviceName.Text = "单稳态型电磁系统自动点焊设备";
            //
            // input_LogsPath
            //
            input_LogsPath.Dock = DockStyle.Fill;
            input_LogsPath.Location = new Point(88, 93);
            input_LogsPath.Name = "input_LogsPath";
            input_LogsPath.Size = new Size(292, 39);
            input_LogsPath.TabIndex = 2;
            //
            // input_DataPath
            //
            input_DataPath.Dock = DockStyle.Fill;
            input_DataPath.Location = new Point(88, 138);
            input_DataPath.Name = "input_DataPath";
            input_DataPath.Size = new Size(292, 39);
            input_DataPath.TabIndex = 2;
            //
            // input_BaseUrl
            //
            input_BaseUrl.Dock = DockStyle.Fill;
            input_BaseUrl.Location = new Point(88, 183);
            input_BaseUrl.Name = "input_BaseUrl";
            input_BaseUrl.Size = new Size(292, 39);
            input_BaseUrl.TabIndex = 2;
            //
            // btnTestConnection
            //
            btnTestConnection.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnTestConnection.BorderWidth = 1F;
            btnTestConnection.Dock = DockStyle.Fill;
            btnTestConnection.IconSvg = "ApiOutlined";
            btnTestConnection.Location = new Point(386, 183);
            btnTestConnection.Name = "btnTestConnection";
            btnTestConnection.Size = new Size(142, 39);
            btnTestConnection.TabIndex = 3;
            btnTestConnection.Text = "连通性测试";
            //
            // lblDeviceUrl
            //
            lblDeviceUrl.AutoEllipsis = true;
            lblDeviceUrl.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeviceUrl.Dock = DockStyle.Fill;
            lblDeviceUrl.Location = new Point(3, 228);
            lblDeviceUrl.Name = "lblDeviceUrl";
            lblDeviceUrl.Size = new Size(70, 39);
            lblDeviceUrl.TabIndex = 7;
            lblDeviceUrl.Text = "设备地址";
            //
            // input_DeviceUrl
            //
            input_DeviceUrl.Dock = DockStyle.Fill;
            input_DeviceUrl.Location = new Point(88, 228);
            input_DeviceUrl.Name = "input_DeviceUrl";
            input_DeviceUrl.Size = new Size(292, 39);
            input_DeviceUrl.TabIndex = 8;
            //
            // chkEnableDualStationMode
            //
            chkEnableDualStationMode.AutoSizeMode = AntdUI.TAutoSize.Width;
            chkEnableDualStationMode.Dock = DockStyle.Fill;
            chkEnableDualStationMode.Location = new Point(3, 3);
            chkEnableDualStationMode.Name = "chkEnableDualStationMode";
            chkEnableDualStationMode.Size = new Size(221, 40);
            chkEnableDualStationMode.TabIndex = 10;
            chkEnableDualStationMode.Text = "启用双工位双工单模式";
            //
            // panel2
            //
            panel2.Controls.Add(btnSaveAll);
            panel2.Location = new Point(1258, 0);
            panel2.Margin = new Padding(0);
            panel2.Name = "panel2";
            panel2.Size = new Size(135, 70);
            panel2.TabIndex = 2;
            panel2.Text = "panel2";
            //
            // btnSaveAll
            //
            btnSaveAll.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSaveAll.BorderWidth = 1F;
            btnSaveAll.Dock = DockStyle.Fill;
            btnSaveAll.IconSvg = "SaveOutlined";
            btnSaveAll.Location = new Point(0, 0);
            btnSaveAll.Margin = new Padding(0);
            btnSaveAll.Name = "btnSaveAll";
            btnSaveAll.Size = new Size(124, 70);
            btnSaveAll.TabIndex = 0;
            btnSaveAll.Text = "应用全部";
            //
            // headerLayout
            //
            headerLayout.ColumnCount = 2;
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.ColumnStyles.Add(new ColumnStyle());
            headerLayout.Controls.Add(titleLayout, 0, 0);
            headerLayout.Controls.Add(panel2, 1, 0);
            headerLayout.Dock = DockStyle.Fill;
            headerLayout.Location = new Point(24, 18);
            headerLayout.Margin = new Padding(24, 18, 24, 10);
            headerLayout.Name = "headerLayout";
            headerLayout.RowCount = 1;
            headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            headerLayout.Size = new Size(1393, 70);
            headerLayout.TabIndex = 4;
            //
            // titleLayout
            //
            titleLayout.ColumnCount = 1;
            titleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            titleLayout.Controls.Add(lblTitle, 0, 0);
            titleLayout.Controls.Add(lblDescription, 0, 1);
            titleLayout.Dock = DockStyle.Fill;
            titleLayout.Location = new Point(0, 0);
            titleLayout.Margin = new Padding(0);
            titleLayout.Name = "titleLayout";
            titleLayout.RowCount = 2;
            titleLayout.RowStyles.Add(new RowStyle());
            titleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            titleLayout.Size = new Size(1258, 70);
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
            lblTitle.Size = new Size(1258, 31);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "系统设置";
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblDescription
            //
            lblDescription.AutoEllipsis = true;
            lblDescription.Dock = DockStyle.Fill;
            lblDescription.ForeColor = SystemColors.GrayText;
            lblDescription.Location = new Point(0, 31);
            lblDescription.Margin = new Padding(0);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(1258, 39);
            lblDescription.TabIndex = 1;
            lblDescription.Text = "配置 PLC、MES、总控、设备信息以及本地数据和日志存储路径。";
            lblDescription.TextAlign = ContentAlignment.MiddleLeft;
            //
            // tableLayoutPanel4
            //
            tableLayoutPanel4.ColumnCount = 1;
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Controls.Add(headerLayout, 0, 0);
            tableLayoutPanel4.Controls.Add(panel3, 0, 1);
            tableLayoutPanel4.Dock = DockStyle.Fill;
            tableLayoutPanel4.Location = new Point(0, 0);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 2;
            tableLayoutPanel4.RowStyles.Add(new RowStyle());
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Size = new Size(1441, 802);
            tableLayoutPanel4.TabIndex = 3;
            //
            // panel3
            //
            panel3.Controls.Add(tabSettingCategories);
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new Point(3, 101);
            panel3.Name = "panel3";
            panel3.Size = new Size(1435, 698);
            panel3.TabIndex = 3;
            panel3.Text = "panel3";
            //
            // tabSettingCategories
            //
            tabSettingCategories.Controls.Add(tabBasicSettings);
            tabSettingCategories.Controls.Add(tabProductProcess);
            tabSettingCategories.Controls.Add(tabTestItemTemplates);
            tabSettingCategories.Dock = DockStyle.Fill;
            tabSettingCategories.Location = new Point(0, 0);
            tabSettingCategories.Name = "tabSettingCategories";
            tabSettingCategories.SelectedIndex = 0;
            tabSettingCategories.Size = new Size(1435, 698);
            tabSettingCategories.TabIndex = 4;
            //
            // tabBasicSettings
            //
            tabBasicSettings.Controls.Add(panelBasicSettings);
            tabBasicSettings.Location = new Point(4, 32);
            tabBasicSettings.Name = "tabBasicSettings";
            tabBasicSettings.Padding = new Padding(3);
            tabBasicSettings.Size = new Size(1427, 662);
            tabBasicSettings.TabIndex = 0;
            tabBasicSettings.Text = "基础设置";
            tabBasicSettings.UseVisualStyleBackColor = true;
            //
            // panelBasicSettings
            //
            panelBasicSettings.Controls.Add(groupBox1);
            panelBasicSettings.Controls.Add(tableLayoutPanel1);
            panelBasicSettings.Controls.Add(grpAppConfig);
            panelBasicSettings.Dock = DockStyle.Fill;
            panelBasicSettings.Location = new Point(3, 3);
            panelBasicSettings.Name = "panelBasicSettings";
            panelBasicSettings.Size = new Size(1421, 656);
            panelBasicSettings.TabIndex = 0;
            panelBasicSettings.Text = "panelBasicSettings";
            //
            // groupBox1
            //
            groupBox1.Controls.Add(tableLayoutPanel5);
            groupBox1.Location = new Point(367, 392);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(743, 264);
            groupBox1.TabIndex = 5;
            groupBox1.TabStop = false;
            groupBox1.Text = "生产配置";
            //
            // tableLayoutPanel5
            //
            tableLayoutPanel5.ColumnCount = 3;
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel5.Controls.Add(chkUseProductNumberFilter, 1, 0);
            tableLayoutPanel5.Controls.Add(chkEnableDualStationMode, 0, 0);
            tableLayoutPanel5.Location = new Point(9, 41);
            tableLayoutPanel5.Name = "tableLayoutPanel5";
            tableLayoutPanel5.RowCount = 2;
            tableLayoutPanel5.RowStyles.Add(new RowStyle());
            tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel5.Size = new Size(725, 217);
            tableLayoutPanel5.TabIndex = 4;
            //
            // chkUseProductNumberFilter
            //
            chkUseProductNumberFilter.AutoSizeMode = AntdUI.TAutoSize.Width;
            chkUseProductNumberFilter.Dock = DockStyle.Fill;
            chkUseProductNumberFilter.Location = new Point(244, 3);
            chkUseProductNumberFilter.Name = "chkUseProductNumberFilter";
            chkUseProductNumberFilter.Size = new Size(204, 40);
            chkUseProductNumberFilter.TabIndex = 11;
            chkUseProductNumberFilter.Text = "按产品工号筛选程序";
            //
            // tableLayoutPanel1
            //
            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.Controls.Add(grpPlcConfig, 0, 0);
            tableLayoutPanel1.Controls.Add(grpMasterConfig, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Left;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(364, 656);
            tableLayoutPanel1.TabIndex = 3;
            //
            // tabProductProcess
            //
            tabProductProcess.Controls.Add(productProcessLayout);
            tabProductProcess.Location = new Point(4, 29);
            tabProductProcess.Name = "tabProductProcess";
            tabProductProcess.Padding = new Padding(3);
            tabProductProcess.Size = new Size(1427, 665);
            tabProductProcess.TabIndex = 1;
            tabProductProcess.Text = "产品工艺配置";
            tabProductProcess.UseVisualStyleBackColor = true;
            //
            // productProcessLayout
            //
            productProcessLayout.ColumnCount = 1;
            productProcessLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            productProcessLayout.Controls.Add(productProcessHeaderLayout, 0, 0);
            productProcessLayout.Controls.Add(productProcessToolbar, 0, 1);
            productProcessLayout.Controls.Add(tableProductProcesses, 0, 2);
            productProcessLayout.Dock = DockStyle.Fill;
            productProcessLayout.Location = new Point(3, 3);
            productProcessLayout.Name = "productProcessLayout";
            productProcessLayout.RowCount = 3;
            productProcessLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            productProcessLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            productProcessLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            productProcessLayout.Size = new Size(1421, 659);
            productProcessLayout.TabIndex = 0;
            //
            // productProcessHeaderLayout
            //
            productProcessHeaderLayout.ColumnCount = 1;
            productProcessHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            productProcessHeaderLayout.Controls.Add(lblProductProcessTitle, 0, 0);
            productProcessHeaderLayout.Controls.Add(lblProductProcessDescription, 0, 1);
            productProcessHeaderLayout.Dock = DockStyle.Fill;
            productProcessHeaderLayout.Location = new Point(0, 0);
            productProcessHeaderLayout.Margin = new Padding(0, 0, 0, 6);
            productProcessHeaderLayout.Name = "productProcessHeaderLayout";
            productProcessHeaderLayout.RowCount = 2;
            productProcessHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            productProcessHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            productProcessHeaderLayout.Size = new Size(1421, 58);
            productProcessHeaderLayout.TabIndex = 0;
            //
            // lblProductProcessTitle
            //
            lblProductProcessTitle.AutoSize = true;
            lblProductProcessTitle.Dock = DockStyle.Fill;
            lblProductProcessTitle.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            lblProductProcessTitle.Location = new Point(0, 0);
            lblProductProcessTitle.Margin = new Padding(0);
            lblProductProcessTitle.Name = "lblProductProcessTitle";
            lblProductProcessTitle.Size = new Size(1421, 32);
            lblProductProcessTitle.TabIndex = 0;
            lblProductProcessTitle.Text = "产品工艺配置";
            lblProductProcessTitle.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblProductProcessDescription
            //
            lblProductProcessDescription.AutoEllipsis = true;
            lblProductProcessDescription.Dock = DockStyle.Fill;
            lblProductProcessDescription.ForeColor = SystemColors.GrayText;
            lblProductProcessDescription.Location = new Point(0, 32);
            lblProductProcessDescription.Margin = new Padding(0);
            lblProductProcessDescription.Name = "lblProductProcessDescription";
            lblProductProcessDescription.Size = new Size(1421, 26);
            lblProductProcessDescription.TabIndex = 1;
            lblProductProcessDescription.Text = "维护工位、产品型号、工序号、每件焊点数量和采集参数组。工位 0 表示所有工位共享配置。";
            lblProductProcessDescription.TextAlign = ContentAlignment.MiddleLeft;
            //
            // productProcessToolbar
            //
            productProcessToolbar.Controls.Add(btnRefreshProductProcesses);
            productProcessToolbar.Controls.Add(btnDeleteProductProcess);
            productProcessToolbar.Controls.Add(btnSaveProductProcesses);
            productProcessToolbar.Controls.Add(btnAddProductProcess);
            productProcessToolbar.Dock = DockStyle.Fill;
            productProcessToolbar.FlowDirection = FlowDirection.RightToLeft;
            productProcessToolbar.Location = new Point(0, 64);
            productProcessToolbar.Margin = new Padding(0, 0, 0, 8);
            productProcessToolbar.Name = "productProcessToolbar";
            productProcessToolbar.Size = new Size(1421, 42);
            productProcessToolbar.TabIndex = 1;
            productProcessToolbar.WrapContents = false;
            //
            // btnRefreshProductProcesses
            //
            btnRefreshProductProcesses.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnRefreshProductProcesses.BorderWidth = 1F;
            btnRefreshProductProcesses.IconSvg = "ReloadOutlined";
            btnRefreshProductProcesses.Location = new Point(1329, 3);
            btnRefreshProductProcesses.Name = "btnRefreshProductProcesses";
            btnRefreshProductProcesses.Size = new Size(89, 36);
            btnRefreshProductProcesses.TabIndex = 3;
            btnRefreshProductProcesses.Text = "刷新";
            //
            // btnDeleteProductProcess
            //
            btnDeleteProductProcess.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnDeleteProductProcess.BorderWidth = 1F;
            btnDeleteProductProcess.IconSvg = "DeleteOutlined";
            btnDeleteProductProcess.Location = new Point(1199, 3);
            btnDeleteProductProcess.Name = "btnDeleteProductProcess";
            btnDeleteProductProcess.Size = new Size(124, 36);
            btnDeleteProductProcess.TabIndex = 2;
            btnDeleteProductProcess.Text = "删除选中";
            //
            // btnSaveProductProcesses
            //
            btnSaveProductProcesses.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSaveProductProcesses.BorderWidth = 1F;
            btnSaveProductProcesses.IconSvg = "SaveOutlined";
            btnSaveProductProcesses.Location = new Point(1104, 3);
            btnSaveProductProcesses.Name = "btnSaveProductProcesses";
            btnSaveProductProcesses.Size = new Size(89, 36);
            btnSaveProductProcesses.TabIndex = 1;
            btnSaveProductProcesses.Text = "保存";
            //
            // btnAddProductProcess
            //
            btnAddProductProcess.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnAddProductProcess.BorderWidth = 1F;
            btnAddProductProcess.IconSvg = "PlusOutlined";
            btnAddProductProcess.Location = new Point(1009, 3);
            btnAddProductProcess.Name = "btnAddProductProcess";
            btnAddProductProcess.Size = new Size(89, 36);
            btnAddProductProcess.TabIndex = 0;
            btnAddProductProcess.Text = "新增";
            //
            // tableProductProcesses
            //
            tableProductProcesses.Dock = DockStyle.Fill;
            tableProductProcesses.EditMode = AntdUI.TEditMode.DoubleClick;
            tableProductProcesses.Gap = 8;
            tableProductProcesses.Gaps = new Size(8, 8);
            tableProductProcesses.Location = new Point(0, 114);
            tableProductProcesses.Margin = new Padding(0);
            tableProductProcesses.Name = "tableProductProcesses";
            tableProductProcesses.Size = new Size(1421, 545);
            tableProductProcesses.TabIndex = 2;
            tableProductProcesses.Text = "tableProductProcesses";
            //
            // tabTestItemTemplates
            //
            tabTestItemTemplates.Controls.Add(testTemplateLayout);
            tabTestItemTemplates.Location = new Point(4, 29);
            tabTestItemTemplates.Name = "tabTestItemTemplates";
            tabTestItemTemplates.Padding = new Padding(3);
            tabTestItemTemplates.Size = new Size(1427, 665);
            tabTestItemTemplates.TabIndex = 2;
            tabTestItemTemplates.Text = "测试项目模板";
            tabTestItemTemplates.UseVisualStyleBackColor = true;
            //
            // testTemplateLayout
            //
            testTemplateLayout.ColumnCount = 1;
            testTemplateLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            testTemplateLayout.Controls.Add(testTemplateHeaderLayout, 0, 0);
            testTemplateLayout.Controls.Add(testTemplateBodyLayout, 0, 1);
            testTemplateLayout.Dock = DockStyle.Fill;
            testTemplateLayout.Location = new Point(3, 3);
            testTemplateLayout.Name = "testTemplateLayout";
            testTemplateLayout.RowCount = 2;
            testTemplateLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            testTemplateLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            testTemplateLayout.Size = new Size(1421, 659);
            testTemplateLayout.TabIndex = 0;
            //
            // testTemplateHeaderLayout
            //
            testTemplateHeaderLayout.ColumnCount = 1;
            testTemplateHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            testTemplateHeaderLayout.Controls.Add(lblTestTemplateTitle, 0, 0);
            testTemplateHeaderLayout.Controls.Add(lblTestTemplateDescription, 0, 1);
            testTemplateHeaderLayout.Dock = DockStyle.Fill;
            testTemplateHeaderLayout.Location = new Point(0, 0);
            testTemplateHeaderLayout.Margin = new Padding(0, 0, 0, 6);
            testTemplateHeaderLayout.Name = "testTemplateHeaderLayout";
            testTemplateHeaderLayout.RowCount = 2;
            testTemplateHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            testTemplateHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            testTemplateHeaderLayout.Size = new Size(1421, 58);
            testTemplateHeaderLayout.TabIndex = 0;
            //
            // lblTestTemplateTitle
            //
            lblTestTemplateTitle.AutoSize = true;
            lblTestTemplateTitle.Dock = DockStyle.Fill;
            lblTestTemplateTitle.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            lblTestTemplateTitle.Location = new Point(0, 0);
            lblTestTemplateTitle.Margin = new Padding(0);
            lblTestTemplateTitle.Name = "lblTestTemplateTitle";
            lblTestTemplateTitle.Size = new Size(1421, 32);
            lblTestTemplateTitle.TabIndex = 0;
            lblTestTemplateTitle.Text = "测试项目模板";
            lblTestTemplateTitle.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblTestTemplateDescription
            //
            lblTestTemplateDescription.AutoEllipsis = true;
            lblTestTemplateDescription.Dock = DockStyle.Fill;
            lblTestTemplateDescription.ForeColor = SystemColors.GrayText;
            lblTestTemplateDescription.Location = new Point(0, 32);
            lblTestTemplateDescription.Margin = new Padding(0);
            lblTestTemplateDescription.Name = "lblTestTemplateDescription";
            lblTestTemplateDescription.Size = new Size(1421, 26);
            lblTestTemplateDescription.TabIndex = 1;
            lblTestTemplateDescription.Text = "维护可复用的测试项目模板。产品工艺配置绑定模板后，采集时按模板中的焊点、工位和地址读取实际值、上下限和结果。";
            lblTestTemplateDescription.TextAlign = ContentAlignment.MiddleLeft;
            //
            // testTemplateBodyLayout
            //
            testTemplateBodyLayout.ColumnCount = 1;
            testTemplateBodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            testTemplateBodyLayout.Controls.Add(testTemplateListLayout, 0, 0);
            testTemplateBodyLayout.Dock = DockStyle.Fill;
            testTemplateBodyLayout.Location = new Point(0, 64);
            testTemplateBodyLayout.Margin = new Padding(0);
            testTemplateBodyLayout.Name = "testTemplateBodyLayout";
            testTemplateBodyLayout.RowCount = 1;
            testTemplateBodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            testTemplateBodyLayout.Size = new Size(1421, 595);
            testTemplateBodyLayout.TabIndex = 1;
            //
            // testTemplateListLayout
            //
            testTemplateListLayout.ColumnCount = 1;
            testTemplateListLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            testTemplateListLayout.Controls.Add(lblTestTemplateListTitle, 0, 0);
            testTemplateListLayout.Controls.Add(testTemplateToolbar, 0, 1);
            testTemplateListLayout.Controls.Add(tableTestTemplates, 0, 2);
            testTemplateListLayout.Dock = DockStyle.Fill;
            testTemplateListLayout.Location = new Point(0, 0);
            testTemplateListLayout.Margin = new Padding(0, 0, 8, 0);
            testTemplateListLayout.Name = "testTemplateListLayout";
            testTemplateListLayout.RowCount = 3;
            testTemplateListLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            testTemplateListLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            testTemplateListLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            testTemplateListLayout.Size = new Size(1421, 595);
            testTemplateListLayout.TabIndex = 0;
            //
            // lblTestTemplateListTitle
            //
            lblTestTemplateListTitle.Dock = DockStyle.Fill;
            lblTestTemplateListTitle.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold);
            lblTestTemplateListTitle.Location = new Point(0, 0);
            lblTestTemplateListTitle.Margin = new Padding(0);
            lblTestTemplateListTitle.Name = "lblTestTemplateListTitle";
            lblTestTemplateListTitle.Size = new Size(1421, 36);
            lblTestTemplateListTitle.TabIndex = 0;
            lblTestTemplateListTitle.Text = "模板列表";
            lblTestTemplateListTitle.TextAlign = ContentAlignment.MiddleLeft;
            //
            // testTemplateToolbar
            //
            testTemplateToolbar.Controls.Add(btnRefreshTestTemplates);
            testTemplateToolbar.Controls.Add(btnDeleteTestTemplate);
            testTemplateToolbar.Controls.Add(btnSaveTestTemplates);
            testTemplateToolbar.Controls.Add(btnAddTestTemplate);
            testTemplateToolbar.Dock = DockStyle.Fill;
            testTemplateToolbar.FlowDirection = FlowDirection.RightToLeft;
            testTemplateToolbar.Location = new Point(0, 36);
            testTemplateToolbar.Margin = new Padding(0, 0, 0, 8);
            testTemplateToolbar.Name = "testTemplateToolbar";
            testTemplateToolbar.Size = new Size(1421, 42);
            testTemplateToolbar.TabIndex = 1;
            testTemplateToolbar.WrapContents = false;
            //
            // btnRefreshTestTemplates
            //
            btnRefreshTestTemplates.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnRefreshTestTemplates.BorderWidth = 1F;
            btnRefreshTestTemplates.IconSvg = "ReloadOutlined";
            btnRefreshTestTemplates.Location = new Point(383, 3);
            btnRefreshTestTemplates.Name = "btnRefreshTestTemplates";
            btnRefreshTestTemplates.Size = new Size(89, 36);
            btnRefreshTestTemplates.TabIndex = 3;
            btnRefreshTestTemplates.Text = "刷新";
            //
            // btnDeleteTestTemplate
            //
            btnDeleteTestTemplate.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnDeleteTestTemplate.BorderWidth = 1F;
            btnDeleteTestTemplate.IconSvg = "DeleteOutlined";
            btnDeleteTestTemplate.Location = new Point(253, 3);
            btnDeleteTestTemplate.Name = "btnDeleteTestTemplate";
            btnDeleteTestTemplate.Size = new Size(124, 36);
            btnDeleteTestTemplate.TabIndex = 2;
            btnDeleteTestTemplate.Text = "删除选中";
            //
            // btnSaveTestTemplates
            //
            btnSaveTestTemplates.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSaveTestTemplates.BorderWidth = 1F;
            btnSaveTestTemplates.IconSvg = "SaveOutlined";
            btnSaveTestTemplates.Location = new Point(158, 3);
            btnSaveTestTemplates.Name = "btnSaveTestTemplates";
            btnSaveTestTemplates.Size = new Size(89, 36);
            btnSaveTestTemplates.TabIndex = 1;
            btnSaveTestTemplates.Text = "保存";
            //
            // btnAddTestTemplate
            //
            btnAddTestTemplate.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnAddTestTemplate.BorderWidth = 1F;
            btnAddTestTemplate.IconSvg = "PlusOutlined";
            btnAddTestTemplate.Location = new Point(63, 3);
            btnAddTestTemplate.Name = "btnAddTestTemplate";
            btnAddTestTemplate.Size = new Size(89, 36);
            btnAddTestTemplate.TabIndex = 0;
            btnAddTestTemplate.Text = "新增";
            //
            // tableTestTemplates
            //
            tableTestTemplates.Dock = DockStyle.Fill;
            tableTestTemplates.EditMode = AntdUI.TEditMode.DoubleClick;
            tableTestTemplates.Gap = 8;
            tableTestTemplates.Gaps = new Size(8, 8);
            tableTestTemplates.Location = new Point(0, 86);
            tableTestTemplates.Margin = new Padding(0);
            tableTestTemplates.Name = "tableTestTemplates";
            tableTestTemplates.Size = new Size(1421, 509);
            tableTestTemplates.TabIndex = 2;
            tableTestTemplates.Text = "tableTestTemplates";
            // SystemSettingView
            //
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(tableLayoutPanel4);
            Font = new Font("Microsoft YaHei UI", 10.5F);
            Margin = new Padding(45, 24, 45, 24);
            Name = "SystemSettingView";
            Size = new Size(1441, 802);
            grpMasterConfig.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            grpPlcConfig.ResumeLayout(false);
            grpPlcConfig.PerformLayout();
            tlpPlcConfig.ResumeLayout(false);
            tlpPlcConfig.PerformLayout();
            grpAppConfig.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel3.PerformLayout();
            flowPanel2.ResumeLayout(false);
            flowPanel2.PerformLayout();
            flowPanel1.ResumeLayout(false);
            flowPanel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            headerLayout.ResumeLayout(false);
            titleLayout.ResumeLayout(false);
            titleLayout.PerformLayout();
            tableLayoutPanel4.ResumeLayout(false);
            panel3.ResumeLayout(false);
            tabSettingCategories.ResumeLayout(false);
            tabBasicSettings.ResumeLayout(false);
            panelBasicSettings.ResumeLayout(false);
            panelBasicSettings.PerformLayout();
            groupBox1.ResumeLayout(false);
            tableLayoutPanel5.ResumeLayout(false);
            tableLayoutPanel5.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            tabProductProcess.ResumeLayout(false);
            productProcessLayout.ResumeLayout(false);
            productProcessHeaderLayout.ResumeLayout(false);
            productProcessHeaderLayout.PerformLayout();
            productProcessToolbar.ResumeLayout(false);
            productProcessToolbar.PerformLayout();
            tabTestItemTemplates.ResumeLayout(false);
            testTemplateLayout.ResumeLayout(false);
            testTemplateHeaderLayout.ResumeLayout(false);
            testTemplateHeaderLayout.PerformLayout();
            testTemplateBodyLayout.ResumeLayout(false);
            testTemplateListLayout.ResumeLayout(false);
            testTemplateToolbar.ResumeLayout(false);
            testTemplateToolbar.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private GroupBox grpPlcConfig;
        private TableLayoutPanel tlpPlcConfig;
        private AntdUI.Label lblPlcIp;
        private AntdUI.Label lblPlcPort;
        private AntdUI.Label lblPlcType;
        private GroupBox grpMasterConfig;
        private TableLayoutPanel tableLayoutPanel2;
        private AntdUI.Button btnConnectMasterController;
        private AntdUI.Label lblMasterIp;
        private AntdUI.Label lblMasterPort;
        private AntdUI.Input input_MasterIp;
        private AntdUI.Input input_MasterPort;
        private AntdUI.Button btnConnectPlc;
        private AntdUI.Input input_PlcIp;
        private AntdUI.Input input_PlcPort;
        private AntdUI.Select select_PlcType;
        private GroupBox grpAppConfig;
        private AntdUI.Label lblDeviceId;
        private AntdUI.Label lblDeviceName;
        private AntdUI.Label lblLogPath;
        private AntdUI.Label lblDataPath;
        private AntdUI.Input input_DeviceID;
        private AntdUI.Button btnSyncDevice;
        private TableLayoutPanel tableLayoutPanel3;
        private AntdUI.Label lblMesUrl;
        private AntdUI.Input input_DeviceName;
        private AntdUI.Input input_LogsPath;
        private AntdUI.Input input_DataPath;
        private AntdUI.Input input_BaseUrl;
        private AntdUI.Button btnTestConnection;
        private AntdUI.Button btnChangeLogPath;
        private AntdUI.Button btnChangeDataPath;
        private AntdUI.Button btnOpenLogPath;
        private AntdUI.Button btnOpenDataPath;
        private AntdUI.Panel panel2;
        private TableLayoutPanel headerLayout;
        private TableLayoutPanel titleLayout;
        private Label lblTitle;
        private Label lblDescription;
        private TableLayoutPanel tableLayoutPanel4;
        private AntdUI.Panel panel3;
        private TabControl tabSettingCategories;
        private TabPage tabBasicSettings;
        private AntdUI.Panel panelBasicSettings;
        private TabPage tabProductProcess;
        private TableLayoutPanel productProcessLayout;
        private TableLayoutPanel productProcessHeaderLayout;
        private Label lblProductProcessTitle;
        private Label lblProductProcessDescription;
        private FlowLayoutPanel productProcessToolbar;
        private AntdUI.Button btnRefreshProductProcesses;
        private AntdUI.Button btnDeleteProductProcess;
        private AntdUI.Button btnSaveProductProcesses;
        private AntdUI.Button btnAddProductProcess;
        private AntdUI.Table tableProductProcesses;
        private TabPage tabTestItemTemplates;
        private TableLayoutPanel testTemplateLayout;
        private TableLayoutPanel testTemplateHeaderLayout;
        private Label lblTestTemplateTitle;
        private Label lblTestTemplateDescription;
        private TableLayoutPanel testTemplateBodyLayout;
        private TableLayoutPanel testTemplateListLayout;
        private Label lblTestTemplateListTitle;
        private FlowLayoutPanel testTemplateToolbar;
        private AntdUI.Button btnRefreshTestTemplates;
        private AntdUI.Button btnDeleteTestTemplate;
        private AntdUI.Button btnSaveTestTemplates;
        private AntdUI.Button btnAddTestTemplate;
        private AntdUI.Table tableTestTemplates;
        private AntdUI.Button btnSaveAll;
        private AntdUI.Label lblDeviceUrl;
        private AntdUI.Input input_DeviceUrl;
        private TableLayoutPanel tableLayoutPanel1;
        private AntdUI.Checkbox chkEnableDualStationMode;
        private GroupBox groupBox1;
        private TableLayoutPanel tableLayoutPanel5;
        private AntdUI.FlowPanel flowPanel2;
        private AntdUI.FlowPanel flowPanel1;
        private AntdUI.Checkbox chkUseProductNumberFilter;
    }
}
