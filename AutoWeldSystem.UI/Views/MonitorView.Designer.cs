namespace AutoWeldSystem.UI.Views
{
    partial class MonitorView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MonitorView));
            左右分隔面板 = new AntdUI.Splitter();
            tlpLeft = new TableLayoutPanel();
            tlpLeftTop = new TableLayoutPanel();
            lblTitle = new Label();
            tlpCommunicationStatus = new TableLayoutPanel();
            btnExpEnd = new AntdUI.Button();
            tagDeviceStatus = new AntdUI.Tag();
            btnExpStart = new AntdUI.Button();
            tagTaskStatus = new AntdUI.Tag();
            tagMes = new AntdUI.Tag();
            tagPLC = new AntdUI.Tag();
            picLogo = new PictureBox();
            table2 = new AntdUI.Table();
            panel1 = new Panel();
            TPLRight = new TableLayoutPanel();
            groupBox3 = new GroupBox();
            tableLayoutPanel1 = new TableLayoutPanel();
            tag2 = new AntdUI.Tag();
            tag1 = new AntdUI.Tag();
            TLPWorkOrderInfo = new TableLayoutPanel();
            tableLayoutPanel9 = new TableLayoutPanel();
            lblPartName = new AntdUI.Label();
            inputProductName = new AntdUI.Input();
            lblDrawingNo = new AntdUI.Label();
            inputDrawingNo = new AntdUI.Input();
            tableLayoutPanel3 = new TableLayoutPanel();
            lblProcessName = new AntdUI.Label();
            inputProcessNo = new AntdUI.Input();
            inputItemName = new AntdUI.Input();
            lblProcessNo = new AntdUI.Label();
            tableLayoutPanel7 = new TableLayoutPanel();
            inputProgramName = new AntdUI.Input();
            lblProgramName = new AntdUI.Label();
            lblCurTime = new Label();
            tableLayoutPanel8 = new TableLayoutPanel();
            lblBatchNo = new AntdUI.Label();
            inputBatch = new AntdUI.Input();
            lblSpec = new AntdUI.Label();
            inputSpec = new AntdUI.Input();
            tableLayoutPanel6 = new TableLayoutPanel();
            lblWorkOrder = new AntdUI.Label();
            btnChangeWO = new AntdUI.Button();
            inputSN = new AntdUI.Input();
            btnEditWO = new AntdUI.Button();
            tableLayoutPanel2 = new TableLayoutPanel();
            lblProductNo = new AntdUI.Label();
            inputProdNum = new AntdUI.Input();
            inputProdModel = new AntdUI.Input();
            lblProdModel = new AntdUI.Label();
            tableLayoutPanel4 = new TableLayoutPanel();
            lblCurUser = new AntdUI.Label();
            lblCurrentUser = new Label();
            btnSwitchUser = new AntdUI.Button();
            tableLayoutPanel5 = new TableLayoutPanel();
            lblCurLang = new AntdUI.Label();
            select_Lang = new AntdUI.Select();
            btnLogout = new AntdUI.Button();
            tableLayoutPanel10 = new TableLayoutPanel();
            lblStation = new AntdUI.Label();
            selectStation = new AntdUI.Select();
            groupBox1 = new GroupBox();
            inputErrorTips = new AntdUI.Input();
            table1 = new AntdUI.Table();
            groupBox2 = new GroupBox();
            inputRunningStatus = new AntdUI.Input();
            ((System.ComponentModel.ISupportInitialize)左右分隔面板).BeginInit();
            左右分隔面板.Panel1.SuspendLayout();
            左右分隔面板.Panel2.SuspendLayout();
            左右分隔面板.SuspendLayout();
            tlpLeft.SuspendLayout();
            tlpLeftTop.SuspendLayout();
            tlpCommunicationStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
            TPLRight.SuspendLayout();
            groupBox3.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            TLPWorkOrderInfo.SuspendLayout();
            tableLayoutPanel9.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            tableLayoutPanel7.SuspendLayout();
            tableLayoutPanel8.SuspendLayout();
            tableLayoutPanel6.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            tableLayoutPanel5.SuspendLayout();
            tableLayoutPanel10.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // 左右分隔面板
            // 
            左右分隔面板.BackColor = SystemColors.Control;
            左右分隔面板.Dock = DockStyle.Fill;
            左右分隔面板.Location = new Point(0, 0);
            左右分隔面板.Margin = new Padding(0);
            左右分隔面板.Name = "左右分隔面板";
            // 
            // 左右分隔面板.Panel1
            // 
            左右分隔面板.Panel1.Controls.Add(tlpLeft);
            左右分隔面板.Panel1MinSize = 500;
            // 
            // 左右分隔面板.Panel2
            // 
            左右分隔面板.Panel2.Controls.Add(TPLRight);
            左右分隔面板.Panel2MinSize = 400;
            左右分隔面板.Size = new Size(1564, 932);
            左右分隔面板.SplitterDistance = 1150;
            左右分隔面板.SplitterWidth = 5;
            左右分隔面板.TabIndex = 3;
            // 
            // tlpLeft
            // 
            tlpLeft.AutoSize = true;
            tlpLeft.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpLeft.ColumnCount = 1;
            tlpLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpLeft.Controls.Add(tlpLeftTop, 0, 0);
            tlpLeft.Controls.Add(table2, 0, 1);
            tlpLeft.Controls.Add(panel1, 0, 2);
            tlpLeft.Dock = DockStyle.Fill;
            tlpLeft.Location = new Point(0, 0);
            tlpLeft.Margin = new Padding(4, 3, 4, 3);
            tlpLeft.Name = "tlpLeft";
            tlpLeft.RowCount = 3;
            tlpLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 11.5879831F));
            tlpLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 51.28755F));
            tlpLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 37.1244621F));
            tlpLeft.Size = new Size(1150, 932);
            tlpLeft.TabIndex = 0;
            // 
            // tlpLeftTop
            // 
            tlpLeftTop.AutoSize = true;
            tlpLeftTop.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpLeftTop.ColumnCount = 3;
            tlpLeftTop.ColumnStyles.Add(new ColumnStyle());
            tlpLeftTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            tlpLeftTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tlpLeftTop.Controls.Add(lblTitle, 1, 0);
            tlpLeftTop.Controls.Add(tlpCommunicationStatus, 2, 0);
            tlpLeftTop.Controls.Add(picLogo, 0, 0);
            tlpLeftTop.Dock = DockStyle.Fill;
            tlpLeftTop.Location = new Point(3, 3);
            tlpLeftTop.Name = "tlpLeftTop";
            tlpLeftTop.RowCount = 1;
            tlpLeftTop.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpLeftTop.Size = new Size(1144, 102);
            tlpLeftTop.TabIndex = 4;
            // 
            // lblTitle
            // 
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.Font = new Font("Microsoft YaHei UI", 28F, FontStyle.Bold);
            lblTitle.ImeMode = ImeMode.NoControl;
            lblTitle.Location = new Point(168, 0);
            lblTitle.Margin = new Padding(0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(634, 102);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "单稳态型自动点焊系统";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpCommunicationStatus
            // 
            tlpCommunicationStatus.ColumnCount = 3;
            tlpCommunicationStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tlpCommunicationStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333359F));
            tlpCommunicationStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333359F));
            tlpCommunicationStatus.Controls.Add(btnExpEnd, 2, 1);
            tlpCommunicationStatus.Controls.Add(tagDeviceStatus, 0, 1);
            tlpCommunicationStatus.Controls.Add(btnExpStart, 2, 0);
            tlpCommunicationStatus.Controls.Add(tagTaskStatus, 1, 1);
            tlpCommunicationStatus.Controls.Add(tagMes, 0, 0);
            tlpCommunicationStatus.Controls.Add(tagPLC, 1, 0);
            tlpCommunicationStatus.Dock = DockStyle.Fill;
            tlpCommunicationStatus.Location = new Point(802, 0);
            tlpCommunicationStatus.Margin = new Padding(0);
            tlpCommunicationStatus.Name = "tlpCommunicationStatus";
            tlpCommunicationStatus.RowCount = 2;
            tlpCommunicationStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCommunicationStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCommunicationStatus.Size = new Size(342, 102);
            tlpCommunicationStatus.TabIndex = 2;
            // 
            // btnExpEnd
            // 
            btnExpEnd.BorderWidth = 1F;
            btnExpEnd.Dock = DockStyle.Fill;
            btnExpEnd.IconGap = 0.2F;
            btnExpEnd.IconSvg = "CheckCircleOutlined";
            btnExpEnd.Location = new Point(227, 51);
            btnExpEnd.Margin = new Padding(0);
            btnExpEnd.Name = "btnExpEnd";
            btnExpEnd.Size = new Size(115, 51);
            btnExpEnd.TabIndex = 3;
            btnExpEnd.Tag = "perm:button.monitor.finish-report:visible";
            btnExpEnd.Text = "完工上报";
            // 
            // tagDeviceStatus
            // 
            tagDeviceStatus.Dock = DockStyle.Fill;
            tagDeviceStatus.Location = new Point(0, 51);
            tagDeviceStatus.Margin = new Padding(0);
            tagDeviceStatus.Name = "tagDeviceStatus";
            tagDeviceStatus.Size = new Size(113, 51);
            tagDeviceStatus.TabIndex = 2;
            tagDeviceStatus.Text = "RUN";
            // 
            // btnExpStart
            // 
            btnExpStart.BorderWidth = 1F;
            btnExpStart.Dock = DockStyle.Fill;
            btnExpStart.IconGap = 0.2F;
            btnExpStart.IconSvg = "PlayCircleOutlined";
            btnExpStart.Location = new Point(227, 0);
            btnExpStart.Margin = new Padding(0);
            btnExpStart.Name = "btnExpStart";
            btnExpStart.Size = new Size(115, 51);
            btnExpStart.TabIndex = 3;
            btnExpStart.Tag = "perm:button.monitor.start-report:visible";
            btnExpStart.Text = "开工上报";
            // 
            // tagTaskStatus
            // 
            tagTaskStatus.Dock = DockStyle.Fill;
            tagTaskStatus.Location = new Point(113, 51);
            tagTaskStatus.Margin = new Padding(0);
            tagTaskStatus.Name = "tagTaskStatus";
            tagTaskStatus.Size = new Size(114, 51);
            tagTaskStatus.TabIndex = 3;
            tagTaskStatus.Text = "未开工";
            // 
            // tagMes
            // 
            tagMes.Dock = DockStyle.Fill;
            tagMes.Location = new Point(0, 0);
            tagMes.Margin = new Padding(0);
            tagMes.Name = "tagMes";
            tagMes.Size = new Size(113, 51);
            tagMes.TabIndex = 0;
            tagMes.Text = "MES";
            // 
            // tagPLC
            // 
            tagPLC.Dock = DockStyle.Fill;
            tagPLC.Location = new Point(113, 0);
            tagPLC.Margin = new Padding(0);
            tagPLC.Name = "tagPLC";
            tagPLC.Size = new Size(114, 51);
            tagPLC.TabIndex = 0;
            tagPLC.Text = "PLC";
            // 
            // picLogo
            // 
            picLogo.Dock = DockStyle.Fill;
            picLogo.Image = (Image)resources.GetObject("picLogo.Image");
            picLogo.Location = new Point(0, 0);
            picLogo.Margin = new Padding(0);
            picLogo.Name = "picLogo";
            picLogo.Size = new Size(168, 102);
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLogo.TabIndex = 1;
            picLogo.TabStop = false;
            // 
            // table2
            // 
            table2.Dock = DockStyle.Fill;
            table2.Gap = 12;
            table2.Location = new Point(1, 109);
            table2.Margin = new Padding(1);
            table2.Name = "table2";
            table2.Size = new Size(1148, 476);
            table2.TabIndex = 2;
            table2.Text = "table2";
            // 
            // panel1
            // 
            panel1.Location = new Point(3, 589);
            panel1.Name = "panel1";
            panel1.Size = new Size(1144, 297);
            panel1.TabIndex = 5;
            // 
            // TPLRight
            // 
            TPLRight.ColumnCount = 1;
            TPLRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TPLRight.Controls.Add(groupBox3, 0, 3);
            TPLRight.Controls.Add(TLPWorkOrderInfo, 0, 0);
            TPLRight.Controls.Add(groupBox1, 0, 1);
            TPLRight.Controls.Add(table1, 0, 4);
            TPLRight.Controls.Add(groupBox2, 0, 2);
            TPLRight.Dock = DockStyle.Fill;
            TPLRight.Location = new Point(0, 0);
            TPLRight.Margin = new Padding(4, 3, 4, 3);
            TPLRight.Name = "TPLRight";
            TPLRight.RowCount = 5;
            TPLRight.RowStyles.Add(new RowStyle());
            TPLRight.RowStyles.Add(new RowStyle());
            TPLRight.RowStyles.Add(new RowStyle());
            TPLRight.RowStyles.Add(new RowStyle());
            TPLRight.RowStyles.Add(new RowStyle());
            TPLRight.Size = new Size(409, 932);
            TPLRight.TabIndex = 0;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(tableLayoutPanel1);
            groupBox3.Dock = DockStyle.Fill;
            groupBox3.Location = new Point(3, 572);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(403, 82);
            groupBox3.TabIndex = 0;
            groupBox3.TabStop = false;
            groupBox3.Text = "产品结果";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(tag2, 1, 0);
            tableLayoutPanel1.Controls.Add(tag1, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(3, 26);
            tableLayoutPanel1.Margin = new Padding(0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(397, 53);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // tag2
            // 
            tag2.Dock = DockStyle.Fill;
            tag2.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold);
            tag2.Location = new Point(201, 3);
            tag2.Name = "tag2";
            tag2.Size = new Size(193, 47);
            tag2.TabIndex = 1;
            tag2.Text = "工位2";
            tag2.Visible = false;
            // 
            // tag1
            // 
            tag1.Dock = DockStyle.Fill;
            tag1.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold);
            tag1.Location = new Point(3, 3);
            tag1.Name = "tag1";
            tag1.Size = new Size(192, 47);
            tag1.TabIndex = 0;
            tag1.Text = "工位1";
            // 
            // TLPWorkOrderInfo
            // 
            TLPWorkOrderInfo.ColumnCount = 1;
            TLPWorkOrderInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TLPWorkOrderInfo.Controls.Add(tableLayoutPanel9, 0, 8);
            TLPWorkOrderInfo.Controls.Add(tableLayoutPanel3, 0, 9);
            TLPWorkOrderInfo.Controls.Add(tableLayoutPanel7, 0, 5);
            TLPWorkOrderInfo.Controls.Add(lblCurTime, 0, 0);
            TLPWorkOrderInfo.Controls.Add(tableLayoutPanel8, 0, 7);
            TLPWorkOrderInfo.Controls.Add(tableLayoutPanel6, 0, 4);
            TLPWorkOrderInfo.Controls.Add(tableLayoutPanel2, 0, 6);
            TLPWorkOrderInfo.Controls.Add(tableLayoutPanel4, 0, 1);
            TLPWorkOrderInfo.Controls.Add(tableLayoutPanel5, 0, 2);
            TLPWorkOrderInfo.Controls.Add(tableLayoutPanel10, 0, 3);
            TLPWorkOrderInfo.Dock = DockStyle.Fill;
            TLPWorkOrderInfo.Location = new Point(4, 3);
            TLPWorkOrderInfo.Margin = new Padding(4, 3, 4, 3);
            TLPWorkOrderInfo.Name = "TLPWorkOrderInfo";
            TLPWorkOrderInfo.RowCount = 10;
            TLPWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            TLPWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            TLPWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            TLPWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            TLPWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            TLPWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            TLPWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            TLPWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            TLPWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            TLPWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            TLPWorkOrderInfo.Size = new Size(401, 387);
            TLPWorkOrderInfo.TabIndex = 0;
            // 
            // tableLayoutPanel9
            // 
            tableLayoutPanel9.AutoSize = true;
            tableLayoutPanel9.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel9.ColumnCount = 4;
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel9.Controls.Add(lblPartName, 0, 0);
            tableLayoutPanel9.Controls.Add(inputProductName, 1, 0);
            tableLayoutPanel9.Controls.Add(lblDrawingNo, 2, 0);
            tableLayoutPanel9.Controls.Add(inputDrawingNo, 3, 0);
            tableLayoutPanel9.Dock = DockStyle.Fill;
            tableLayoutPanel9.Location = new Point(0, 304);
            tableLayoutPanel9.Margin = new Padding(0);
            tableLayoutPanel9.Name = "tableLayoutPanel9";
            tableLayoutPanel9.RowCount = 1;
            tableLayoutPanel9.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel9.Size = new Size(401, 38);
            tableLayoutPanel9.TabIndex = 0;
            // 
            // lblPartName
            // 
            lblPartName.AutoEllipsis = true;
            lblPartName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblPartName.AutoSizePadding = true;
            lblPartName.Dock = DockStyle.Fill;
            lblPartName.Location = new Point(0, 0);
            lblPartName.Margin = new Padding(0);
            lblPartName.Name = "lblPartName";
            lblPartName.Size = new Size(79, 38);
            lblPartName.TabIndex = 29;
            lblPartName.Text = "部件名称";
            lblPartName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // inputProductName
            // 
            inputProductName.Dock = DockStyle.Fill;
            inputProductName.ImeMode = ImeMode.Inherit;
            inputProductName.Location = new Point(79, 0);
            inputProductName.Margin = new Padding(0);
            inputProductName.Name = "inputProductName";
            inputProductName.ReadOnly = true;
            inputProductName.Size = new Size(121, 38);
            inputProductName.TabIndex = 4;
            // 
            // lblDrawingNo
            // 
            lblDrawingNo.AutoEllipsis = true;
            lblDrawingNo.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDrawingNo.AutoSizePadding = true;
            lblDrawingNo.Dock = DockStyle.Fill;
            lblDrawingNo.Location = new Point(200, 0);
            lblDrawingNo.Margin = new Padding(0);
            lblDrawingNo.Name = "lblDrawingNo";
            lblDrawingNo.Size = new Size(79, 38);
            lblDrawingNo.TabIndex = 29;
            lblDrawingNo.Text = "零件图号";
            lblDrawingNo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // inputDrawingNo
            // 
            inputDrawingNo.Dock = DockStyle.Fill;
            inputDrawingNo.ImeMode = ImeMode.Inherit;
            inputDrawingNo.Location = new Point(279, 0);
            inputDrawingNo.Margin = new Padding(0);
            inputDrawingNo.Name = "inputDrawingNo";
            inputDrawingNo.ReadOnly = true;
            inputDrawingNo.Size = new Size(122, 38);
            inputDrawingNo.TabIndex = 4;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 4;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.Controls.Add(lblProcessName, 0, 0);
            tableLayoutPanel3.Controls.Add(inputProcessNo, 3, 0);
            tableLayoutPanel3.Controls.Add(inputItemName, 1, 0);
            tableLayoutPanel3.Controls.Add(lblProcessNo, 2, 0);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(0, 342);
            tableLayoutPanel3.Margin = new Padding(0);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 1;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Size = new Size(401, 45);
            tableLayoutPanel3.TabIndex = 2;
            // 
            // lblProcessName
            // 
            lblProcessName.AutoEllipsis = true;
            lblProcessName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProcessName.AutoSizePadding = true;
            lblProcessName.Dock = DockStyle.Fill;
            lblProcessName.Location = new Point(0, 0);
            lblProcessName.Margin = new Padding(0);
            lblProcessName.Name = "lblProcessName";
            lblProcessName.Size = new Size(79, 45);
            lblProcessName.TabIndex = 30;
            lblProcessName.Text = "工序名称";
            lblProcessName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // inputProcessNo
            // 
            inputProcessNo.Dock = DockStyle.Fill;
            inputProcessNo.ImeMode = ImeMode.Inherit;
            inputProcessNo.Location = new Point(270, 0);
            inputProcessNo.Margin = new Padding(0);
            inputProcessNo.Name = "inputProcessNo";
            inputProcessNo.ReadOnly = true;
            inputProcessNo.Size = new Size(131, 45);
            inputProcessNo.TabIndex = 4;
            // 
            // inputItemName
            // 
            inputItemName.Dock = DockStyle.Fill;
            inputItemName.ImeMode = ImeMode.Inherit;
            inputItemName.Location = new Point(79, 0);
            inputItemName.Margin = new Padding(0);
            inputItemName.Name = "inputItemName";
            inputItemName.ReadOnly = true;
            inputItemName.Size = new Size(131, 45);
            inputItemName.TabIndex = 4;
            // 
            // lblProcessNo
            // 
            lblProcessNo.AutoEllipsis = true;
            lblProcessNo.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProcessNo.AutoSizePadding = true;
            lblProcessNo.Dock = DockStyle.Fill;
            lblProcessNo.Location = new Point(210, 0);
            lblProcessNo.Margin = new Padding(0);
            lblProcessNo.Name = "lblProcessNo";
            lblProcessNo.Size = new Size(60, 45);
            lblProcessNo.TabIndex = 27;
            lblProcessNo.Text = "工序号";
            lblProcessNo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel7
            // 
            tableLayoutPanel7.ColumnCount = 2;
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel7.Controls.Add(inputProgramName, 1, 0);
            tableLayoutPanel7.Controls.Add(lblProgramName, 0, 0);
            tableLayoutPanel7.Dock = DockStyle.Fill;
            tableLayoutPanel7.Location = new Point(0, 190);
            tableLayoutPanel7.Margin = new Padding(0);
            tableLayoutPanel7.Name = "tableLayoutPanel7";
            tableLayoutPanel7.RowCount = 1;
            tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel7.Size = new Size(401, 38);
            tableLayoutPanel7.TabIndex = 8;
            // 
            // inputProgramName
            // 
            inputProgramName.Dock = DockStyle.Fill;
            inputProgramName.ImeMode = ImeMode.Inherit;
            inputProgramName.Location = new Point(79, 0);
            inputProgramName.Margin = new Padding(0);
            inputProgramName.Name = "inputProgramName";
            inputProgramName.ReadOnly = true;
            inputProgramName.Size = new Size(322, 38);
            inputProgramName.TabIndex = 4;
            // 
            // lblProgramName
            // 
            lblProgramName.AutoEllipsis = true;
            lblProgramName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProgramName.AutoSizePadding = true;
            lblProgramName.Dock = DockStyle.Fill;
            lblProgramName.Location = new Point(0, 0);
            lblProgramName.Margin = new Padding(0);
            lblProgramName.Name = "lblProgramName";
            lblProgramName.Size = new Size(79, 38);
            lblProgramName.TabIndex = 23;
            lblProgramName.Text = "程序名称";
            lblProgramName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblCurTime
            // 
            lblCurTime.AutoSize = true;
            TLPWorkOrderInfo.SetColumnSpan(lblCurTime, 4);
            lblCurTime.Dock = DockStyle.Fill;
            lblCurTime.Font = new Font("Segoe UI", 15.7F);
            lblCurTime.ImeMode = ImeMode.NoControl;
            lblCurTime.Location = new Point(0, 0);
            lblCurTime.Margin = new Padding(0);
            lblCurTime.Name = "lblCurTime";
            lblCurTime.Size = new Size(401, 38);
            lblCurTime.TabIndex = 6;
            lblCurTime.Text = "当前时间";
            lblCurTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel8
            // 
            tableLayoutPanel8.ColumnCount = 4;
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel8.Controls.Add(lblBatchNo, 0, 0);
            tableLayoutPanel8.Controls.Add(inputBatch, 1, 0);
            tableLayoutPanel8.Controls.Add(lblSpec, 2, 0);
            tableLayoutPanel8.Controls.Add(inputSpec, 3, 0);
            tableLayoutPanel8.Dock = DockStyle.Fill;
            tableLayoutPanel8.Location = new Point(0, 266);
            tableLayoutPanel8.Margin = new Padding(0);
            tableLayoutPanel8.Name = "tableLayoutPanel8";
            tableLayoutPanel8.RowCount = 1;
            tableLayoutPanel8.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel8.Size = new Size(401, 38);
            tableLayoutPanel8.TabIndex = 0;
            // 
            // lblBatchNo
            // 
            lblBatchNo.AutoEllipsis = true;
            lblBatchNo.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblBatchNo.AutoSizePadding = true;
            lblBatchNo.Dock = DockStyle.Fill;
            lblBatchNo.Location = new Point(0, 0);
            lblBatchNo.Margin = new Padding(0);
            lblBatchNo.Name = "lblBatchNo";
            lblBatchNo.Size = new Size(40, 38);
            lblBatchNo.TabIndex = 29;
            lblBatchNo.Text = "批次";
            lblBatchNo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // inputBatch
            // 
            inputBatch.Dock = DockStyle.Fill;
            inputBatch.ImeMode = ImeMode.Inherit;
            inputBatch.Location = new Point(40, 0);
            inputBatch.Margin = new Padding(0);
            inputBatch.Name = "inputBatch";
            inputBatch.ReadOnly = true;
            inputBatch.Size = new Size(160, 38);
            inputBatch.TabIndex = 4;
            // 
            // lblSpec
            // 
            lblSpec.AutoEllipsis = true;
            lblSpec.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblSpec.AutoSizePadding = true;
            lblSpec.Dock = DockStyle.Fill;
            lblSpec.Location = new Point(200, 0);
            lblSpec.Margin = new Padding(0);
            lblSpec.Name = "lblSpec";
            lblSpec.Size = new Size(40, 38);
            lblSpec.TabIndex = 29;
            lblSpec.Text = "规格";
            lblSpec.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // inputSpec
            // 
            inputSpec.Dock = DockStyle.Fill;
            inputSpec.ImeMode = ImeMode.Inherit;
            inputSpec.Location = new Point(240, 0);
            inputSpec.Margin = new Padding(0);
            inputSpec.Name = "inputSpec";
            inputSpec.ReadOnly = true;
            inputSpec.Size = new Size(161, 38);
            inputSpec.TabIndex = 4;
            // 
            // tableLayoutPanel6
            // 
            tableLayoutPanel6.ColumnCount = 4;
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel6.Controls.Add(lblWorkOrder, 0, 0);
            tableLayoutPanel6.Controls.Add(btnChangeWO, 2, 0);
            tableLayoutPanel6.Controls.Add(inputSN, 1, 0);
            tableLayoutPanel6.Controls.Add(btnEditWO, 3, 0);
            tableLayoutPanel6.Dock = DockStyle.Fill;
            tableLayoutPanel6.Location = new Point(0, 152);
            tableLayoutPanel6.Margin = new Padding(0);
            tableLayoutPanel6.Name = "tableLayoutPanel6";
            tableLayoutPanel6.RowCount = 1;
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel6.Size = new Size(401, 38);
            tableLayoutPanel6.TabIndex = 7;
            // 
            // lblWorkOrder
            // 
            lblWorkOrder.AutoEllipsis = true;
            lblWorkOrder.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblWorkOrder.AutoSizePadding = true;
            lblWorkOrder.Dock = DockStyle.Fill;
            lblWorkOrder.Location = new Point(0, 0);
            lblWorkOrder.Margin = new Padding(0);
            lblWorkOrder.Name = "lblWorkOrder";
            lblWorkOrder.Size = new Size(60, 38);
            lblWorkOrder.TabIndex = 22;
            lblWorkOrder.Text = "工单号";
            lblWorkOrder.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnChangeWO
            // 
            btnChangeWO.AutoEllipsis = true;
            btnChangeWO.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnChangeWO.BorderWidth = 1F;
            btnChangeWO.Dock = DockStyle.Right;
            btnChangeWO.IconSvg = "SwapOutlined";
            btnChangeWO.JoinMode = AntdUI.TJoinMode.Left;
            btnChangeWO.Location = new Point(233, 0);
            btnChangeWO.Margin = new Padding(0);
            btnChangeWO.Name = "btnChangeWO";
            btnChangeWO.Shape = AntdUI.TShape.Round;
            btnChangeWO.Size = new Size(84, 38);
            btnChangeWO.TabIndex = 3;
            btnChangeWO.Tag = "perm:button.monitor.change-work-order:visible";
            btnChangeWO.Text = "变更";
            // 
            // inputSN
            // 
            inputSN.Dock = DockStyle.Fill;
            inputSN.ImeMode = ImeMode.Inherit;
            inputSN.Location = new Point(60, 0);
            inputSN.Margin = new Padding(0);
            inputSN.Name = "inputSN";
            inputSN.ReadOnly = true;
            inputSN.Size = new Size(173, 38);
            inputSN.TabIndex = 4;
            // 
            // btnEditWO
            // 
            btnEditWO.AutoEllipsis = true;
            btnEditWO.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnEditWO.BorderWidth = 1F;
            btnEditWO.Dock = DockStyle.Left;
            btnEditWO.IconSvg = "EditOutlined";
            btnEditWO.JoinMode = AntdUI.TJoinMode.Right;
            btnEditWO.Location = new Point(317, 0);
            btnEditWO.Margin = new Padding(0);
            btnEditWO.Name = "btnEditWO";
            btnEditWO.Shape = AntdUI.TShape.Round;
            btnEditWO.Size = new Size(84, 38);
            btnEditWO.TabIndex = 3;
            btnEditWO.Tag = "";
            btnEditWO.Text = "微调";
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 4;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Controls.Add(lblProductNo, 0, 0);
            tableLayoutPanel2.Controls.Add(inputProdNum, 1, 0);
            tableLayoutPanel2.Controls.Add(inputProdModel, 3, 0);
            tableLayoutPanel2.Controls.Add(lblProdModel, 2, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(0, 228);
            tableLayoutPanel2.Margin = new Padding(0);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(401, 38);
            tableLayoutPanel2.TabIndex = 1;
            // 
            // lblProductNo
            // 
            lblProductNo.AutoEllipsis = true;
            lblProductNo.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProductNo.AutoSizePadding = true;
            lblProductNo.Dock = DockStyle.Fill;
            lblProductNo.Location = new Point(0, 0);
            lblProductNo.Margin = new Padding(0);
            lblProductNo.Name = "lblProductNo";
            lblProductNo.Size = new Size(79, 38);
            lblProductNo.TabIndex = 24;
            lblProductNo.Text = "产品工号";
            lblProductNo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // inputProdNum
            // 
            inputProdNum.Dock = DockStyle.Fill;
            inputProdNum.ImeMode = ImeMode.Inherit;
            inputProdNum.Location = new Point(79, 0);
            inputProdNum.Margin = new Padding(0);
            inputProdNum.Name = "inputProdNum";
            inputProdNum.ReadOnly = true;
            inputProdNum.Size = new Size(121, 38);
            inputProdNum.TabIndex = 4;
            // 
            // inputProdModel
            // 
            inputProdModel.Dock = DockStyle.Fill;
            inputProdModel.ImeMode = ImeMode.Inherit;
            inputProdModel.Location = new Point(279, 0);
            inputProdModel.Margin = new Padding(0);
            inputProdModel.Name = "inputProdModel";
            inputProdModel.ReadOnly = true;
            inputProdModel.Size = new Size(122, 38);
            inputProdModel.TabIndex = 4;
            // 
            // lblProdModel
            // 
            lblProdModel.AutoEllipsis = true;
            lblProdModel.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProdModel.AutoSizePadding = true;
            lblProdModel.Dock = DockStyle.Fill;
            lblProdModel.Location = new Point(200, 0);
            lblProdModel.Margin = new Padding(0);
            lblProdModel.Name = "lblProdModel";
            lblProdModel.Size = new Size(79, 38);
            lblProdModel.TabIndex = 23;
            lblProdModel.Text = "产品型号";
            lblProdModel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel4
            // 
            tableLayoutPanel4.ColumnCount = 3;
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel4.Controls.Add(lblCurUser, 0, 0);
            tableLayoutPanel4.Controls.Add(lblCurrentUser, 1, 0);
            tableLayoutPanel4.Controls.Add(btnSwitchUser, 2, 0);
            tableLayoutPanel4.Dock = DockStyle.Fill;
            tableLayoutPanel4.Location = new Point(0, 38);
            tableLayoutPanel4.Margin = new Padding(0);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 1;
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Size = new Size(401, 38);
            tableLayoutPanel4.TabIndex = 3;
            // 
            // lblCurUser
            // 
            lblCurUser.AutoEllipsis = true;
            lblCurUser.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblCurUser.AutoSizePadding = true;
            lblCurUser.Dock = DockStyle.Fill;
            lblCurUser.Location = new Point(0, 0);
            lblCurUser.Margin = new Padding(0);
            lblCurUser.Name = "lblCurUser";
            lblCurUser.Size = new Size(79, 38);
            lblCurUser.TabIndex = 18;
            lblCurUser.Text = "当前用户";
            lblCurUser.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblCurrentUser
            // 
            lblCurrentUser.AutoSize = true;
            lblCurrentUser.Dock = DockStyle.Fill;
            lblCurrentUser.ImeMode = ImeMode.NoControl;
            lblCurrentUser.Location = new Point(79, 0);
            lblCurrentUser.Margin = new Padding(0);
            lblCurrentUser.Name = "lblCurrentUser";
            lblCurrentUser.Size = new Size(198, 38);
            lblCurrentUser.TabIndex = 1;
            lblCurrentUser.Text = "当前用户";
            lblCurrentUser.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnSwitchUser
            // 
            btnSwitchUser.AutoEllipsis = true;
            btnSwitchUser.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSwitchUser.BorderWidth = 1F;
            btnSwitchUser.Dock = DockStyle.Fill;
            btnSwitchUser.IconSvg = "UserSwitchOutlined";
            btnSwitchUser.Location = new Point(277, 0);
            btnSwitchUser.Margin = new Padding(0);
            btnSwitchUser.Name = "btnSwitchUser";
            btnSwitchUser.Shape = AntdUI.TShape.Round;
            btnSwitchUser.Size = new Size(124, 38);
            btnSwitchUser.TabIndex = 3;
            btnSwitchUser.Tag = "perm:button.auth.switch-user:visible";
            btnSwitchUser.Text = "切换用户";
            // 
            // tableLayoutPanel5
            // 
            tableLayoutPanel5.ColumnCount = 3;
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel5.Controls.Add(lblCurLang, 0, 0);
            tableLayoutPanel5.Controls.Add(select_Lang, 1, 0);
            tableLayoutPanel5.Controls.Add(btnLogout, 2, 0);
            tableLayoutPanel5.Dock = DockStyle.Fill;
            tableLayoutPanel5.Location = new Point(0, 76);
            tableLayoutPanel5.Margin = new Padding(0);
            tableLayoutPanel5.Name = "tableLayoutPanel5";
            tableLayoutPanel5.RowCount = 1;
            tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel5.Size = new Size(401, 38);
            tableLayoutPanel5.TabIndex = 4;
            // 
            // lblCurLang
            // 
            lblCurLang.AutoEllipsis = true;
            lblCurLang.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblCurLang.AutoSizePadding = true;
            lblCurLang.Dock = DockStyle.Fill;
            lblCurLang.Location = new Point(0, 0);
            lblCurLang.Margin = new Padding(0);
            lblCurLang.Name = "lblCurLang";
            lblCurLang.Size = new Size(79, 38);
            lblCurLang.TabIndex = 21;
            lblCurLang.Text = "当前语言";
            lblCurLang.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // select_Lang
            // 
            select_Lang.Dock = DockStyle.Fill;
            select_Lang.Location = new Point(79, 0);
            select_Lang.Margin = new Padding(0);
            select_Lang.Name = "select_Lang";
            select_Lang.PrefixText = "";
            select_Lang.Size = new Size(198, 38);
            select_Lang.TabIndex = 7;
            // 
            // btnLogout
            // 
            btnLogout.AutoEllipsis = true;
            btnLogout.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnLogout.BorderWidth = 1F;
            btnLogout.Dock = DockStyle.Fill;
            btnLogout.IconSvg = "LogoutOutlined";
            btnLogout.Location = new Point(277, 0);
            btnLogout.Margin = new Padding(0);
            btnLogout.Name = "btnLogout";
            btnLogout.Shape = AntdUI.TShape.Round;
            btnLogout.Size = new Size(124, 38);
            btnLogout.TabIndex = 3;
            btnLogout.Tag = "perm:button.auth.logout:visible";
            btnLogout.Text = "退出登录";
            // 
            // tableLayoutPanel10
            // 
            tableLayoutPanel10.ColumnCount = 2;
            tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel10.Controls.Add(lblStation, 0, 0);
            tableLayoutPanel10.Controls.Add(selectStation, 1, 0);
            tableLayoutPanel10.Dock = DockStyle.Fill;
            tableLayoutPanel10.Location = new Point(0, 114);
            tableLayoutPanel10.Margin = new Padding(0);
            tableLayoutPanel10.Name = "tableLayoutPanel10";
            tableLayoutPanel10.RowCount = 1;
            tableLayoutPanel10.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel10.Size = new Size(401, 38);
            tableLayoutPanel10.TabIndex = 9;
            // 
            // lblStation
            // 
            lblStation.AutoEllipsis = true;
            lblStation.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblStation.AutoSizePadding = true;
            lblStation.Dock = DockStyle.Fill;
            lblStation.Location = new Point(0, 0);
            lblStation.Margin = new Padding(0);
            lblStation.Name = "lblStation";
            lblStation.Size = new Size(79, 38);
            lblStation.TabIndex = 31;
            lblStation.Text = "当前工位";
            lblStation.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // selectStation
            // 
            selectStation.Dock = DockStyle.Fill;
            selectStation.Location = new Point(79, 0);
            selectStation.Margin = new Padding(0);
            selectStation.Name = "selectStation";
            selectStation.Size = new Size(322, 38);
            selectStation.TabIndex = 8;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(inputErrorTips);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.ForeColor = SystemColors.ActiveCaptionText;
            groupBox1.Location = new Point(3, 396);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(403, 82);
            groupBox1.TabIndex = 4;
            groupBox1.TabStop = false;
            groupBox1.Text = "异常提示：";
            // 
            // inputErrorTips
            // 
            inputErrorTips.BackColor = Color.Transparent;
            inputErrorTips.BorderActive = Color.Transparent;
            inputErrorTips.BorderColor = Color.Transparent;
            inputErrorTips.BorderHover = Color.Transparent;
            inputErrorTips.BorderWidth = 0F;
            inputErrorTips.Dock = DockStyle.Fill;
            inputErrorTips.Location = new Point(3, 26);
            inputErrorTips.Margin = new Padding(0);
            inputErrorTips.Name = "inputErrorTips";
            inputErrorTips.ReadOnly = true;
            inputErrorTips.SelectionColor = SystemColors.ActiveCaption;
            inputErrorTips.Size = new Size(397, 53);
            inputErrorTips.TabIndex = 1;
            // 
            // table1
            // 
            table1.AutoSizeColumnsMode = AntdUI.ColumnsMode.Fill;
            table1.BorderRenderMode = AntdUI.TableBorderMode.High;
            table1.Dock = DockStyle.Fill;
            table1.Gap = 8;
            table1.GapCell = 5;
            table1.Gaps = new Size(8, 8);
            table1.Location = new Point(3, 660);
            table1.Name = "table1";
            table1.Size = new Size(403, 269);
            table1.TabIndex = 1;
            table1.Text = "生产指标";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(inputRunningStatus);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.ForeColor = SystemColors.ActiveCaptionText;
            groupBox2.Location = new Point(3, 484);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(403, 82);
            groupBox2.TabIndex = 3;
            groupBox2.TabStop = false;
            groupBox2.Text = "运行状态：";
            // 
            // inputRunningStatus
            // 
            inputRunningStatus.BackColor = Color.Transparent;
            inputRunningStatus.BorderActive = Color.Transparent;
            inputRunningStatus.BorderColor = Color.Transparent;
            inputRunningStatus.BorderHover = Color.Transparent;
            inputRunningStatus.BorderWidth = 0F;
            inputRunningStatus.Dock = DockStyle.Fill;
            inputRunningStatus.Location = new Point(3, 26);
            inputRunningStatus.Margin = new Padding(0);
            inputRunningStatus.Name = "inputRunningStatus";
            inputRunningStatus.ReadOnly = true;
            inputRunningStatus.SelectionColor = SystemColors.ActiveCaption;
            inputRunningStatus.Size = new Size(397, 53);
            inputRunningStatus.TabIndex = 1;
            // 
            // MonitorView
            // 
            AutoScaleDimensions = new SizeF(10F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            Controls.Add(左右分隔面板);
            Font = new Font("Microsoft YaHei UI", 10.5F);
            Margin = new Padding(4, 3, 4, 3);
            Name = "MonitorView";
            Size = new Size(1564, 932);
            左右分隔面板.Panel1.ResumeLayout(false);
            左右分隔面板.Panel1.PerformLayout();
            左右分隔面板.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)左右分隔面板).EndInit();
            左右分隔面板.ResumeLayout(false);
            tlpLeft.ResumeLayout(false);
            tlpLeft.PerformLayout();
            tlpLeftTop.ResumeLayout(false);
            tlpCommunicationStatus.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
            TPLRight.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            TLPWorkOrderInfo.ResumeLayout(false);
            TLPWorkOrderInfo.PerformLayout();
            tableLayoutPanel9.ResumeLayout(false);
            tableLayoutPanel9.PerformLayout();
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel3.PerformLayout();
            tableLayoutPanel7.ResumeLayout(false);
            tableLayoutPanel7.PerformLayout();
            tableLayoutPanel8.ResumeLayout(false);
            tableLayoutPanel8.PerformLayout();
            tableLayoutPanel6.ResumeLayout(false);
            tableLayoutPanel6.PerformLayout();
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            tableLayoutPanel4.ResumeLayout(false);
            tableLayoutPanel4.PerformLayout();
            tableLayoutPanel5.ResumeLayout(false);
            tableLayoutPanel5.PerformLayout();
            tableLayoutPanel10.ResumeLayout(false);
            tableLayoutPanel10.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.Splitter 左右分隔面板;
        private TableLayoutPanel tlpLeft;
        private AntdUI.Button btnExpStart;
        private AntdUI.Button btnExpEnd;
        private TableLayoutPanel tlpCommunicationStatus;
        private AntdUI.Tag tagMes;
        private AntdUI.Tag tagPLC;
        private PictureBox picLogo;
        private Label lblTitle;
        private TableLayoutPanel TPLRight;
        private TableLayoutPanel TLPWorkOrderInfo;
        private Label lblCurrentUser;
        private AntdUI.Input inputProgramName;
        private AntdUI.Button btnChangeWO;
        private AntdUI.Input inputSN;
        private Label lblCurTime;
        private AntdUI.Button btnSwitchUser;
        private AntdUI.Button btnLogout;
        private AntdUI.Input inputProcessNo;
        private AntdUI.Input inputItemName;
        private AntdUI.Input inputProdNum;
        private AntdUI.Input inputBatch;
        private AntdUI.Select select_Lang;
        private AntdUI.Label lblCurUser;
        private AntdUI.Tag tagDeviceStatus;
        private AntdUI.Tag tagTaskStatus;
        private AntdUI.Label lblProcessName;
        private AntdUI.Label lblBatchNo;
        private AntdUI.Label lblProcessNo;
        private AntdUI.Label lblProductNo;
        private AntdUI.Label lblProgramName;
        private AntdUI.Label lblWorkOrder;
        private AntdUI.Label lblCurLang;
        private GroupBox groupBox1;
        private AntdUI.Input inputErrorTips;
        private AntdUI.Table table1;
        private GroupBox groupBox2;
        private AntdUI.Input inputRunningStatus;
        private AntdUI.Table table2;
        private TableLayoutPanel tableLayoutPanel3;
        private TableLayoutPanel tableLayoutPanel7;
        private AntdUI.Label lblProdModel;
        private AntdUI.Input inputProdModel;
        private TableLayoutPanel tableLayoutPanel2;
        private TableLayoutPanel tableLayoutPanel6;
        private TableLayoutPanel tableLayoutPanel4;
        private TableLayoutPanel tableLayoutPanel5;
        private TableLayoutPanel tableLayoutPanel8;
        private AntdUI.Label lblSpec;
        private AntdUI.Input inputSpec;
        private TableLayoutPanel tableLayoutPanel9;
        private AntdUI.Label lblPartName;
        private AntdUI.Input inputProductName;
        private AntdUI.Label lblDrawingNo;
        private AntdUI.Input inputDrawingNo;
        private TableLayoutPanel tlpLeftTop;
        private TableLayoutPanel tableLayoutPanel1;
        private AntdUI.Tag tag2;
        private AntdUI.Tag tag1;
        private TableLayoutPanel tableLayoutPanel10;
        private AntdUI.Label lblStation;
        private AntdUI.Select selectStation;
        private AntdUI.Button btnEditWO;
        private Panel panel1;
        private GroupBox groupBox3;
    }
}
