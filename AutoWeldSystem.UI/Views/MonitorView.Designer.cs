using AutoWeldSystem.UI.Components;

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
            AntdUI.Tabs.StyleLine styleLine1 = new AntdUI.Tabs.StyleLine();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            AntdUI.SegmentedItem segmentedItem1 = new AntdUI.SegmentedItem();
            AntdUI.SegmentedItem segmentedItem2 = new AntdUI.SegmentedItem();
            AntdUI.Tabs.StyleLine styleLine2 = new AntdUI.Tabs.StyleLine();
            VerticalSplitter = new AntdUI.Splitter();
            tlpLeft = new TableLayoutPanel();
            LeftTopLayout = new TableLayoutPanel();
            lblTitle = new Label();
            tlpCommunicationStatus = new TableLayoutPanel();
            tagDeviceStatus = new AntdUI.Tag();
            tagTaskStatus = new AntdUI.Tag();
            tagMes = new AntdUI.Tag();
            tagPLC = new AntdUI.Tag();
            picLogo = new PictureBox();
            tabsPreview = new AntdUI.Tabs();
            tabsPreview1 = new AntdUI.TabPage();
            PreviewLayout1 = new TableLayoutPanel();
            tlpStationOverview1 = new TableLayoutPanel();
            lblLiveHint1 = new AntdUI.Label();
            lblLiveProductNo1 = new AntdUI.Label();
            tagLiveResult1 = new AntdUI.Tag();
            lblLiveTouchNo1 = new AntdUI.Label();
            HorizontalSplitter1 = new AntdUI.Splitter();
            HorizontalScrollBar1 = new SlimHorizontalScrollBar();
            dgvPreview1 = new DataGridView();
            panelHistory1 = new Panel();
            tableHistory1 = new AntdUI.Table();
            tabsPreview2 = new AntdUI.TabPage();
            previewLayout2 = new TableLayoutPanel();
            HorizontalSplitter2 = new AntdUI.Splitter();
            panelPreview2 = new Panel();
            dgvPreview2 = new DataGridView();
            HorizontalScrollBar2 = new SlimHorizontalScrollBar();
            panelHistory2 = new Panel();
            tableHistory2 = new AntdUI.Table();
            tlpStationOverview2 = new TableLayoutPanel();
            lblLiveHint2 = new AntdUI.Label();
            lblLiveProductNo2 = new AntdUI.Label();
            tagLiveResult2 = new AntdUI.Tag();
            lblLiveTouchNo2 = new AntdUI.Label();
            tlpRight = new TableLayoutPanel();
            grpProductResult = new GroupBox();
            tlpStationResult = new TableLayoutPanel();
            tagStationResult2 = new AntdUI.Tag();
            tagStationResult1 = new AntdUI.Tag();
            tlpWorkOrderInfo = new TableLayoutPanel();
            tlpUserInfo2 = new TableLayoutPanel();
            TeamName = new AntdUI.Input();
            lblTeamName = new AntdUI.Label();
            lblDeptName = new AntdUI.Label();
            inputDeptName = new AntdUI.Input();
            tlpButton = new TableLayoutPanel();
            btnExpEnd = new AntdUI.Button();
            btnExpStart = new AntdUI.Button();
            btnLocalWorkOrder = new AntdUI.Button();
            btnGetWO = new AntdUI.Button();
            tlpStation = new TableLayoutPanel();
            segmentedStationSwitch = new AntdUI.Segmented();
            tlpProductNameAndDrawingNo = new TableLayoutPanel();
            lblPartName = new AntdUI.Label();
            inputProductName = new AntdUI.Input();
            lblDrawingNo = new AntdUI.Label();
            inputDrawingNo = new AntdUI.Input();
            tlpProgramName = new TableLayoutPanel();
            inputProgramName = new AntdUI.Input();
            lblProgramName = new AntdUI.Label();
            panelTimeAndVersion = new Panel();
            lblVersion = new AntdUI.Label();
            lblCurTime = new Label();
            tlpProcessInfo = new TableLayoutPanel();
            lblProcessName = new AntdUI.Label();
            inputProcessNo = new AntdUI.Input();
            selectItemName = new AntdUI.Select();
            lblProcessNo = new AntdUI.Label();
            lblStartAmount = new AntdUI.Label();
            inputStartAmount = new AntdUI.Input();
            tlpSpecAndBatch = new TableLayoutPanel();
            inputBatch = new AntdUI.Input();
            lblSpec = new AntdUI.Label();
            lblBatchNo = new AntdUI.Label();
            inputSpec = new AntdUI.Input();
            tlpStationInfo = new TableLayoutPanel();
            lblWorkOrder = new AntdUI.Label();
            btnChangeWO = new AntdUI.Button();
            inputSN = new AntdUI.Input();
            btnEditWO = new AntdUI.Button();
            tlpProductModel = new TableLayoutPanel();
            lblProdModel = new AntdUI.Label();
            inputProdModel = new AntdUI.Input();
            tlpProductNum = new TableLayoutPanel();
            inputProdNum = new AntdUI.Input();
            lblProductNo = new AntdUI.Label();
            selectRecipeCode = new AntdUI.Select();
            tlpUserInfo1 = new TableLayoutPanel();
            MesUserNumber = new AntdUI.Input();
            lblUserNumber = new AntdUI.Label();
            lblUserName = new AntdUI.Label();
            MesUserName = new AntdUI.Input();
            grpErrorTips = new GroupBox();
            inputErrorTips = new AntdUI.Input();
            grpRunningStatus = new GroupBox();
            inputRunningStatus = new AntdUI.Input();
            tabsMetrics = new AntdUI.Tabs();
            tabsMetrics1 = new AntdUI.TabPage();
            tableMetric1 = new AntdUI.Table();
            tabsMetrics2 = new AntdUI.TabPage();
            tableMetric2 = new AntdUI.Table();
            lblLiveResult = new AntdUI.Label();
            lblLiveTouchCount = new AntdUI.Label();
            label7 = new AntdUI.Label();
            label8 = new AntdUI.Label();
            ((System.ComponentModel.ISupportInitialize)VerticalSplitter).BeginInit();
            VerticalSplitter.Panel1.SuspendLayout();
            VerticalSplitter.Panel2.SuspendLayout();
            VerticalSplitter.SuspendLayout();
            tlpLeft.SuspendLayout();
            LeftTopLayout.SuspendLayout();
            tlpCommunicationStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
            tabsPreview.SuspendLayout();
            tabsPreview1.SuspendLayout();
            PreviewLayout1.SuspendLayout();
            tlpStationOverview1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)HorizontalSplitter1).BeginInit();
            HorizontalSplitter1.Panel1.SuspendLayout();
            HorizontalSplitter1.Panel2.SuspendLayout();
            HorizontalSplitter1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPreview1).BeginInit();
            panelHistory1.SuspendLayout();
            tabsPreview2.SuspendLayout();
            previewLayout2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)HorizontalSplitter2).BeginInit();
            HorizontalSplitter2.Panel1.SuspendLayout();
            HorizontalSplitter2.Panel2.SuspendLayout();
            HorizontalSplitter2.SuspendLayout();
            panelPreview2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPreview2).BeginInit();
            panelHistory2.SuspendLayout();
            tlpStationOverview2.SuspendLayout();
            tlpRight.SuspendLayout();
            grpProductResult.SuspendLayout();
            tlpStationResult.SuspendLayout();
            tlpWorkOrderInfo.SuspendLayout();
            tlpUserInfo2.SuspendLayout();
            tlpButton.SuspendLayout();
            tlpStation.SuspendLayout();
            tlpProductNameAndDrawingNo.SuspendLayout();
            tlpProgramName.SuspendLayout();
            panelTimeAndVersion.SuspendLayout();
            tlpProcessInfo.SuspendLayout();
            tlpSpecAndBatch.SuspendLayout();
            tlpStationInfo.SuspendLayout();
            tlpProductModel.SuspendLayout();
            tlpProductNum.SuspendLayout();
            tlpUserInfo1.SuspendLayout();
            grpErrorTips.SuspendLayout();
            grpRunningStatus.SuspendLayout();
            tabsMetrics.SuspendLayout();
            tabsMetrics1.SuspendLayout();
            tabsMetrics2.SuspendLayout();
            SuspendLayout();
            // 
            // VerticalSplitter
            // 
            VerticalSplitter.BackColor = SystemColors.Control;
            VerticalSplitter.Dock = DockStyle.Fill;
            VerticalSplitter.Location = new Point(0, 0);
            VerticalSplitter.Margin = new Padding(0);
            VerticalSplitter.Name = "VerticalSplitter";
            // 
            // VerticalSplitter.Panel1
            // 
            VerticalSplitter.Panel1.Controls.Add(tlpLeft);
            VerticalSplitter.Panel1MinSize = 500;
            // 
            // VerticalSplitter.Panel2
            // 
            VerticalSplitter.Panel2.Controls.Add(tlpRight);
            VerticalSplitter.Panel2MinSize = 400;
            VerticalSplitter.Size = new Size(1564, 932);
            VerticalSplitter.SplitterDistance = 1150;
            VerticalSplitter.SplitterWidth = 5;
            VerticalSplitter.TabIndex = 3;
            // 
            // tlpLeft
            // 
            tlpLeft.AutoSize = true;
            tlpLeft.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpLeft.ColumnCount = 1;
            tlpLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpLeft.Controls.Add(LeftTopLayout, 0, 0);
            tlpLeft.Controls.Add(tabsPreview, 0, 1);
            tlpLeft.Dock = DockStyle.Fill;
            tlpLeft.Location = new Point(0, 0);
            tlpLeft.Margin = new Padding(0);
            tlpLeft.Name = "tlpLeft";
            tlpLeft.RowCount = 2;
            tlpLeft.RowStyles.Add(new RowStyle());
            tlpLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpLeft.Size = new Size(1150, 932);
            tlpLeft.TabIndex = 0;
            // 
            // LeftTopLayout
            // 
            LeftTopLayout.AutoSize = true;
            LeftTopLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            LeftTopLayout.ColumnCount = 3;
            LeftTopLayout.ColumnStyles.Add(new ColumnStyle());
            LeftTopLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));
            LeftTopLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            LeftTopLayout.Controls.Add(lblTitle, 1, 0);
            LeftTopLayout.Controls.Add(tlpCommunicationStatus, 2, 0);
            LeftTopLayout.Controls.Add(picLogo, 0, 0);
            LeftTopLayout.Dock = DockStyle.Fill;
            LeftTopLayout.Location = new Point(0, 1);
            LeftTopLayout.Margin = new Padding(0, 1, 1, 0);
            LeftTopLayout.Name = "LeftTopLayout";
            LeftTopLayout.RowCount = 1;
            LeftTopLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            LeftTopLayout.Size = new Size(1149, 102);
            LeftTopLayout.TabIndex = 4;
            // 
            // lblTitle
            // 
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.Font = new Font("Microsoft YaHei UI", 36F, FontStyle.Bold);
            lblTitle.ImeMode = ImeMode.NoControl;
            lblTitle.Location = new Point(131, 0);
            lblTitle.Margin = new Padding(0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(763, 102);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "单稳态型自动点焊系统";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpCommunicationStatus
            // 
            tlpCommunicationStatus.ColumnCount = 2;
            tlpCommunicationStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 49.99999F));
            tlpCommunicationStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50.0000076F));
            tlpCommunicationStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tlpCommunicationStatus.Controls.Add(tagDeviceStatus, 0, 1);
            tlpCommunicationStatus.Controls.Add(tagTaskStatus, 1, 1);
            tlpCommunicationStatus.Controls.Add(tagMes, 0, 0);
            tlpCommunicationStatus.Controls.Add(tagPLC, 1, 0);
            tlpCommunicationStatus.Dock = DockStyle.Fill;
            tlpCommunicationStatus.Location = new Point(894, 0);
            tlpCommunicationStatus.Margin = new Padding(0);
            tlpCommunicationStatus.Name = "tlpCommunicationStatus";
            tlpCommunicationStatus.RowCount = 2;
            tlpCommunicationStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCommunicationStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCommunicationStatus.Size = new Size(255, 102);
            tlpCommunicationStatus.TabIndex = 2;
            // 
            // tagDeviceStatus
            // 
            tagDeviceStatus.Dock = DockStyle.Fill;
            tagDeviceStatus.Location = new Point(0, 51);
            tagDeviceStatus.Margin = new Padding(0);
            tagDeviceStatus.Name = "tagDeviceStatus";
            tagDeviceStatus.Padding = new Padding(0, 0, 2, 0);
            tagDeviceStatus.Size = new Size(127, 51);
            tagDeviceStatus.TabIndex = 2;
            tagDeviceStatus.Text = "RUN";
            tagDeviceStatus.TextMultiLine = true;
            // 
            // tagTaskStatus
            // 
            tagTaskStatus.Dock = DockStyle.Fill;
            tagTaskStatus.Location = new Point(127, 51);
            tagTaskStatus.Margin = new Padding(0);
            tagTaskStatus.Name = "tagTaskStatus";
            tagTaskStatus.Padding = new Padding(2, 0, 0, 0);
            tagTaskStatus.Size = new Size(128, 51);
            tagTaskStatus.TabIndex = 3;
            tagTaskStatus.Text = "未开工";
            tagTaskStatus.TextMultiLine = true;
            // 
            // tagMes
            // 
            tagMes.Dock = DockStyle.Fill;
            tagMes.Location = new Point(0, 0);
            tagMes.Margin = new Padding(0);
            tagMes.Name = "tagMes";
            tagMes.Padding = new Padding(0, 0, 2, 0);
            tagMes.Size = new Size(127, 51);
            tagMes.TabIndex = 0;
            tagMes.Text = "MES";
            tagMes.TextMultiLine = true;
            // 
            // tagPLC
            // 
            tagPLC.Dock = DockStyle.Fill;
            tagPLC.Location = new Point(127, 0);
            tagPLC.Margin = new Padding(0);
            tagPLC.Name = "tagPLC";
            tagPLC.Padding = new Padding(2, 0, 0, 0);
            tagPLC.Size = new Size(128, 51);
            tagPLC.TabIndex = 0;
            tagPLC.Text = "PLC";
            tagPLC.TextMultiLine = true;
            // 
            // picLogo
            // 
            picLogo.Dock = DockStyle.Fill;
            picLogo.Image = (Image)resources.GetObject("picLogo.Image");
            picLogo.Location = new Point(0, 0);
            picLogo.Margin = new Padding(0);
            picLogo.Name = "picLogo";
            picLogo.Size = new Size(131, 102);
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLogo.TabIndex = 1;
            picLogo.TabStop = false;
            // 
            // tabsPreview
            // 
            tabsPreview.Controls.Add(tabsPreview1);
            tabsPreview.Controls.Add(tabsPreview2);
            tabsPreview.Dock = DockStyle.Fill;
            tabsPreview.Location = new Point(1, 104);
            tabsPreview.Margin = new Padding(1);
            tabsPreview.Name = "tabsPreview";
            tabsPreview.Pages.Add(tabsPreview1);
            tabsPreview.Pages.Add(tabsPreview2);
            tabsPreview.SelectedIndex = 1;
            tabsPreview.Size = new Size(1148, 827);
            tabsPreview.Style = styleLine1;
            tabsPreview.TabIndex = 6;
            tabsPreview.Text = "tabs1";
            // 
            // tabsPreview1
            // 
            tabsPreview1.Controls.Add(PreviewLayout1);
            tabsPreview1.Location = new Point(-2296, -1584);
            tabsPreview1.Name = "tabsPreview1";
            tabsPreview1.Size = new Size(1148, 792);
            tabsPreview1.TabIndex = 0;
            tabsPreview1.Text = "工位1";
            // 
            // PreviewLayout1
            // 
            PreviewLayout1.ColumnCount = 1;
            PreviewLayout1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            PreviewLayout1.Controls.Add(tlpStationOverview1, 0, 0);
            PreviewLayout1.Controls.Add(HorizontalSplitter1, 0, 1);
            PreviewLayout1.Dock = DockStyle.Fill;
            PreviewLayout1.Location = new Point(0, 0);
            PreviewLayout1.Name = "PreviewLayout1";
            PreviewLayout1.RowCount = 2;
            PreviewLayout1.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            PreviewLayout1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            PreviewLayout1.Size = new Size(1148, 792);
            PreviewLayout1.TabIndex = 1;
            // 
            // tlpStationOverview1
            // 
            tlpStationOverview1.ColumnCount = 4;
            tlpStationOverview1.ColumnStyles.Add(new ColumnStyle());
            tlpStationOverview1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpStationOverview1.ColumnStyles.Add(new ColumnStyle());
            tlpStationOverview1.ColumnStyles.Add(new ColumnStyle());
            tlpStationOverview1.Controls.Add(lblLiveHint1, 0, 0);
            tlpStationOverview1.Controls.Add(lblLiveProductNo1, 1, 0);
            tlpStationOverview1.Controls.Add(tagLiveResult1, 2, 0);
            tlpStationOverview1.Controls.Add(lblLiveTouchNo1, 3, 0);
            tlpStationOverview1.Dock = DockStyle.Fill;
            tlpStationOverview1.Location = new Point(0, 0);
            tlpStationOverview1.Margin = new Padding(0);
            tlpStationOverview1.Name = "tlpStationOverview1";
            tlpStationOverview1.RowCount = 1;
            tlpStationOverview1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpStationOverview1.Size = new Size(1148, 36);
            tlpStationOverview1.TabIndex = 0;
            // 
            // lblLiveHint1
            // 
            lblLiveHint1.AutoEllipsis = true;
            lblLiveHint1.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblLiveHint1.Dock = DockStyle.Fill;
            lblLiveHint1.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            lblLiveHint1.Location = new Point(0, 0);
            lblLiveHint1.Margin = new Padding(0);
            lblLiveHint1.Name = "lblLiveHint1";
            lblLiveHint1.Padding = new Padding(10, 0, 20, 0);
            lblLiveHint1.Size = new Size(130, 36);
            lblLiveHint1.TabIndex = 2;
            lblLiveHint1.Text = "实时采集正常";
            // 
            // lblLiveProductNo1
            // 
            lblLiveProductNo1.AutoEllipsis = true;
            lblLiveProductNo1.Dock = DockStyle.Fill;
            lblLiveProductNo1.Location = new Point(130, 0);
            lblLiveProductNo1.Margin = new Padding(0);
            lblLiveProductNo1.Name = "lblLiveProductNo1";
            lblLiveProductNo1.Size = new Size(790, 36);
            lblLiveProductNo1.TabIndex = 3;
            lblLiveProductNo1.Text = "产品编号：--";
            // 
            // tagLiveResult1
            // 
            tagLiveResult1.Dock = DockStyle.Fill;
            tagLiveResult1.Location = new Point(920, 0);
            tagLiveResult1.Margin = new Padding(0);
            tagLiveResult1.Name = "tagLiveResult1";
            tagLiveResult1.Padding = new Padding(0, 0, 20, 0);
            tagLiveResult1.Size = new Size(150, 36);
            tagLiveResult1.TabIndex = 13;
            tagLiveResult1.Text = "产品结果：--";
            // 
            // lblLiveTouchNo1
            // 
            lblLiveTouchNo1.AutoEllipsis = true;
            lblLiveTouchNo1.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblLiveTouchNo1.Dock = DockStyle.Fill;
            lblLiveTouchNo1.Location = new Point(1070, 0);
            lblLiveTouchNo1.Margin = new Padding(0);
            lblLiveTouchNo1.Name = "lblLiveTouchNo1";
            lblLiveTouchNo1.Padding = new Padding(0, 0, 10, 0);
            lblLiveTouchNo1.Size = new Size(78, 36);
            lblLiveTouchNo1.TabIndex = 11;
            lblLiveTouchNo1.Text = "焊点：--";
            // 
            // HorizontalSplitter1
            // 
            HorizontalSplitter1.Dock = DockStyle.Fill;
            HorizontalSplitter1.Location = new Point(0, 36);
            HorizontalSplitter1.Margin = new Padding(0);
            HorizontalSplitter1.Name = "HorizontalSplitter1";
            HorizontalSplitter1.Orientation = Orientation.Horizontal;
            // 
            // HorizontalSplitter1.Panel1
            // 
            HorizontalSplitter1.Panel1.Controls.Add(HorizontalScrollBar1);
            HorizontalSplitter1.Panel1.Controls.Add(dgvPreview1);
            // 
            // HorizontalSplitter1.Panel2
            // 
            HorizontalSplitter1.Panel2.Controls.Add(panelHistory1);
            HorizontalSplitter1.Size = new Size(1148, 756);
            HorizontalSplitter1.SplitterDistance = 258;
            HorizontalSplitter1.SplitterWidth = 3;
            HorizontalSplitter1.TabIndex = 0;
            // 
            // HorizontalScrollBar1
            // 
            HorizontalScrollBar1.BackColor = Color.White;
            HorizontalScrollBar1.Dock = DockStyle.Bottom;
            HorizontalScrollBar1.Location = new Point(0, 246);
            HorizontalScrollBar1.Margin = new Padding(1, 0, 1, 0);
            HorizontalScrollBar1.MinimumSize = new Size(0, 12);
            HorizontalScrollBar1.Name = "HorizontalScrollBar1";
            HorizontalScrollBar1.Size = new Size(1148, 12);
            HorizontalScrollBar1.TabIndex = 3;
            HorizontalScrollBar1.TabStop = false;
            HorizontalScrollBar1.Value = 0;
            HorizontalScrollBar1.Visible = false;
            // 
            // dgvPreview1
            // 
            dgvPreview1.AllowUserToAddRows = false;
            dgvPreview1.AllowUserToDeleteRows = false;
            dgvPreview1.AllowUserToResizeRows = false;
            dgvPreview1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPreview1.BackgroundColor = SystemColors.Control;
            dgvPreview1.BorderStyle = BorderStyle.None;
            dgvPreview1.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = SystemColors.Control;
            dataGridViewCellStyle1.Font = new Font("Microsoft YaHei UI", 10.5F);
            dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.ActiveCaption;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dgvPreview1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvPreview1.ColumnHeadersHeight = 29;
            dgvPreview1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = SystemColors.Control;
            dataGridViewCellStyle2.Font = new Font("Microsoft YaHei UI", 10.5F);
            dataGridViewCellStyle2.ForeColor = SystemColors.GrayText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            dgvPreview1.DefaultCellStyle = dataGridViewCellStyle2;
            dgvPreview1.Dock = DockStyle.Fill;
            dgvPreview1.EnableHeadersVisualStyles = false;
            dgvPreview1.ImeMode = ImeMode.Disable;
            dgvPreview1.Location = new Point(0, 0);
            dgvPreview1.Margin = new Padding(0);
            dgvPreview1.Name = "dgvPreview1";
            dgvPreview1.ReadOnly = true;
            dgvPreview1.RowHeadersVisible = false;
            dgvPreview1.RowHeadersWidth = 51;
            dgvPreview1.ScrollBars = ScrollBars.None;
            dgvPreview1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPreview1.Size = new Size(1148, 258);
            dgvPreview1.TabIndex = 2;
            dgvPreview1.Text = "实时测试结果";
            // 
            // panelHistory1
            // 
            panelHistory1.Controls.Add(tableHistory1);
            panelHistory1.Dock = DockStyle.Fill;
            panelHistory1.Location = new Point(0, 0);
            panelHistory1.Margin = new Padding(0);
            panelHistory1.Name = "panelHistory1";
            panelHistory1.Size = new Size(1148, 495);
            panelHistory1.TabIndex = 0;
            // 
            // tableHistory1
            // 
            tableHistory1.Dock = DockStyle.Fill;
            tableHistory1.Gap = 6;
            tableHistory1.GapCell = 3;
            tableHistory1.Gaps = new Size(6, 6);
            tableHistory1.Location = new Point(0, 0);
            tableHistory1.Margin = new Padding(0);
            tableHistory1.Name = "tableHistory1";
            tableHistory1.RowHeight = 36;
            tableHistory1.RowHeightHeader = 38;
            tableHistory1.Size = new Size(1148, 495);
            tableHistory1.TabIndex = 0;
            tableHistory1.Text = "table2";
            tableHistory1.TreeButtonSize = 18;
            // 
            // tabsPreview2
            // 
            tabsPreview2.Controls.Add(previewLayout2);
            tabsPreview2.Location = new Point(0, 35);
            tabsPreview2.Name = "tabsPreview2";
            tabsPreview2.Size = new Size(1148, 792);
            tabsPreview2.TabIndex = 1;
            tabsPreview2.Text = "工位2";
            // 
            // previewLayout2
            // 
            previewLayout2.ColumnCount = 1;
            previewLayout2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            previewLayout2.Controls.Add(HorizontalSplitter2, 0, 1);
            previewLayout2.Controls.Add(tlpStationOverview2, 0, 0);
            previewLayout2.Dock = DockStyle.Fill;
            previewLayout2.Location = new Point(0, 0);
            previewLayout2.Margin = new Padding(0);
            previewLayout2.Name = "previewLayout2";
            previewLayout2.RowCount = 2;
            previewLayout2.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            previewLayout2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            previewLayout2.Size = new Size(1148, 792);
            previewLayout2.TabIndex = 2;
            // 
            // HorizontalSplitter2
            // 
            HorizontalSplitter2.Dock = DockStyle.Fill;
            HorizontalSplitter2.Location = new Point(0, 36);
            HorizontalSplitter2.Margin = new Padding(0);
            HorizontalSplitter2.Name = "HorizontalSplitter2";
            HorizontalSplitter2.Orientation = Orientation.Horizontal;
            // 
            // HorizontalSplitter2.Panel1
            // 
            HorizontalSplitter2.Panel1.Controls.Add(panelPreview2);
            // 
            // HorizontalSplitter2.Panel2
            // 
            HorizontalSplitter2.Panel2.Controls.Add(panelHistory2);
            HorizontalSplitter2.Size = new Size(1148, 756);
            HorizontalSplitter2.SplitterDistance = 243;
            HorizontalSplitter2.SplitterWidth = 3;
            HorizontalSplitter2.TabIndex = 0;
            // 
            // panelPreview2
            // 
            panelPreview2.BackColor = Color.White;
            panelPreview2.Controls.Add(dgvPreview2);
            panelPreview2.Controls.Add(HorizontalScrollBar2);
            panelPreview2.Dock = DockStyle.Fill;
            panelPreview2.Location = new Point(0, 0);
            panelPreview2.Margin = new Padding(0);
            panelPreview2.Name = "panelPreview2";
            panelPreview2.Size = new Size(1148, 243);
            panelPreview2.TabIndex = 3;
            // 
            // dgvPreview2
            // 
            dgvPreview2.AllowUserToAddRows = false;
            dgvPreview2.AllowUserToDeleteRows = false;
            dgvPreview2.AllowUserToResizeRows = false;
            dgvPreview2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPreview2.BackgroundColor = SystemColors.Control;
            dgvPreview2.BorderStyle = BorderStyle.None;
            dgvPreview2.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = SystemColors.Control;
            dataGridViewCellStyle3.Font = new Font("Microsoft YaHei UI", 10.5F);
            dataGridViewCellStyle3.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = SystemColors.ActiveCaption;
            dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
            dgvPreview2.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgvPreview2.ColumnHeadersHeight = 29;
            dgvPreview2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle4.BackColor = SystemColors.Control;
            dataGridViewCellStyle4.Font = new Font("Microsoft YaHei UI", 10.5F);
            dataGridViewCellStyle4.ForeColor = SystemColors.GrayText;
            dataGridViewCellStyle4.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
            dgvPreview2.DefaultCellStyle = dataGridViewCellStyle4;
            dgvPreview2.Dock = DockStyle.Fill;
            dgvPreview2.EnableHeadersVisualStyles = false;
            dgvPreview2.ImeMode = ImeMode.Disable;
            dgvPreview2.Location = new Point(0, 0);
            dgvPreview2.Margin = new Padding(0);
            dgvPreview2.Name = "dgvPreview2";
            dgvPreview2.ReadOnly = true;
            dgvPreview2.RowHeadersVisible = false;
            dgvPreview2.RowHeadersWidth = 51;
            dgvPreview2.ScrollBars = ScrollBars.None;
            dgvPreview2.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPreview2.Size = new Size(1148, 231);
            dgvPreview2.TabIndex = 2;
            dgvPreview2.Text = "实时测试结果";
            // 
            // HorizontalScrollBar2
            // 
            HorizontalScrollBar2.BackColor = Color.White;
            HorizontalScrollBar2.Dock = DockStyle.Bottom;
            HorizontalScrollBar2.Location = new Point(0, 231);
            HorizontalScrollBar2.Margin = new Padding(1, 0, 1, 0);
            HorizontalScrollBar2.MinimumSize = new Size(0, 12);
            HorizontalScrollBar2.Name = "HorizontalScrollBar2";
            HorizontalScrollBar2.Size = new Size(1148, 12);
            HorizontalScrollBar2.TabIndex = 3;
            HorizontalScrollBar2.TabStop = false;
            HorizontalScrollBar2.Value = 0;
            HorizontalScrollBar2.Visible = false;
            // 
            // panelHistory2
            // 
            panelHistory2.Controls.Add(tableHistory2);
            panelHistory2.Dock = DockStyle.Fill;
            panelHistory2.Location = new Point(0, 0);
            panelHistory2.Margin = new Padding(0);
            panelHistory2.Name = "panelHistory2";
            panelHistory2.Size = new Size(1148, 510);
            panelHistory2.TabIndex = 0;
            // 
            // tableHistory2
            // 
            tableHistory2.Dock = DockStyle.Fill;
            tableHistory2.Gap = 6;
            tableHistory2.GapCell = 3;
            tableHistory2.Gaps = new Size(6, 6);
            tableHistory2.Location = new Point(0, 0);
            tableHistory2.Margin = new Padding(0);
            tableHistory2.Name = "tableHistory2";
            tableHistory2.RowHeight = 36;
            tableHistory2.RowHeightHeader = 38;
            tableHistory2.Size = new Size(1148, 510);
            tableHistory2.TabIndex = 1;
            tableHistory2.Text = "table2";
            tableHistory2.TreeButtonSize = 18;
            // 
            // tlpStationOverview2
            // 
            tlpStationOverview2.ColumnCount = 4;
            tlpStationOverview2.ColumnStyles.Add(new ColumnStyle());
            tlpStationOverview2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpStationOverview2.ColumnStyles.Add(new ColumnStyle());
            tlpStationOverview2.ColumnStyles.Add(new ColumnStyle());
            tlpStationOverview2.Controls.Add(lblLiveHint2, 0, 0);
            tlpStationOverview2.Controls.Add(lblLiveProductNo2, 1, 0);
            tlpStationOverview2.Controls.Add(tagLiveResult2, 2, 0);
            tlpStationOverview2.Controls.Add(lblLiveTouchNo2, 3, 0);
            tlpStationOverview2.Dock = DockStyle.Fill;
            tlpStationOverview2.Location = new Point(0, 0);
            tlpStationOverview2.Margin = new Padding(0);
            tlpStationOverview2.Name = "tlpStationOverview2";
            tlpStationOverview2.RowCount = 1;
            tlpStationOverview2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpStationOverview2.Size = new Size(1148, 36);
            tlpStationOverview2.TabIndex = 0;
            // 
            // lblLiveHint2
            // 
            lblLiveHint2.AutoEllipsis = true;
            lblLiveHint2.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblLiveHint2.Dock = DockStyle.Fill;
            lblLiveHint2.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            lblLiveHint2.Location = new Point(0, 0);
            lblLiveHint2.Margin = new Padding(0);
            lblLiveHint2.Name = "lblLiveHint2";
            lblLiveHint2.Padding = new Padding(10, 0, 20, 0);
            lblLiveHint2.Size = new Size(130, 36);
            lblLiveHint2.TabIndex = 2;
            lblLiveHint2.Text = "实时采集正常";
            // 
            // lblLiveProductNo2
            // 
            lblLiveProductNo2.AutoEllipsis = true;
            lblLiveProductNo2.Dock = DockStyle.Fill;
            lblLiveProductNo2.Location = new Point(130, 0);
            lblLiveProductNo2.Margin = new Padding(0);
            lblLiveProductNo2.Name = "lblLiveProductNo2";
            lblLiveProductNo2.Size = new Size(790, 36);
            lblLiveProductNo2.TabIndex = 3;
            lblLiveProductNo2.Text = "产品编号：--";
            // 
            // tagLiveResult2
            // 
            tagLiveResult2.Dock = DockStyle.Fill;
            tagLiveResult2.Location = new Point(920, 0);
            tagLiveResult2.Margin = new Padding(0);
            tagLiveResult2.Name = "tagLiveResult2";
            tagLiveResult2.Padding = new Padding(0, 0, 20, 0);
            tagLiveResult2.Size = new Size(150, 36);
            tagLiveResult2.TabIndex = 13;
            tagLiveResult2.Text = "产品结果：--";
            // 
            // lblLiveTouchNo2
            // 
            lblLiveTouchNo2.AutoEllipsis = true;
            lblLiveTouchNo2.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblLiveTouchNo2.Dock = DockStyle.Fill;
            lblLiveTouchNo2.Location = new Point(1070, 0);
            lblLiveTouchNo2.Margin = new Padding(0);
            lblLiveTouchNo2.Name = "lblLiveTouchNo2";
            lblLiveTouchNo2.Padding = new Padding(0, 0, 10, 0);
            lblLiveTouchNo2.Size = new Size(78, 36);
            lblLiveTouchNo2.TabIndex = 11;
            lblLiveTouchNo2.Text = "焊点：--";
            // 
            // tlpRight
            // 
            tlpRight.ColumnCount = 1;
            tlpRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRight.Controls.Add(grpProductResult, 0, 3);
            tlpRight.Controls.Add(tlpWorkOrderInfo, 0, 0);
            tlpRight.Controls.Add(grpErrorTips, 0, 1);
            tlpRight.Controls.Add(grpRunningStatus, 0, 2);
            tlpRight.Controls.Add(tabsMetrics, 0, 4);
            tlpRight.Dock = DockStyle.Fill;
            tlpRight.Location = new Point(0, 0);
            tlpRight.Margin = new Padding(0);
            tlpRight.Name = "tlpRight";
            tlpRight.RowCount = 5;
            tlpRight.RowStyles.Add(new RowStyle());
            tlpRight.RowStyles.Add(new RowStyle());
            tlpRight.RowStyles.Add(new RowStyle());
            tlpRight.RowStyles.Add(new RowStyle());
            tlpRight.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpRight.Size = new Size(409, 932);
            tlpRight.TabIndex = 0;
            // 
            // grpProductResult
            // 
            grpProductResult.Controls.Add(tlpStationResult);
            grpProductResult.Dock = DockStyle.Fill;
            grpProductResult.Location = new Point(1, 625);
            grpProductResult.Margin = new Padding(1, 0, 1, 0);
            grpProductResult.Name = "grpProductResult";
            grpProductResult.Size = new Size(407, 88);
            grpProductResult.TabIndex = 0;
            grpProductResult.TabStop = false;
            grpProductResult.Text = "产品结果";
            // 
            // tlpStationResult
            // 
            tlpStationResult.ColumnCount = 2;
            tlpStationResult.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpStationResult.ColumnStyles.Add(new ColumnStyle());
            tlpStationResult.Controls.Add(tagStationResult2, 1, 0);
            tlpStationResult.Controls.Add(tagStationResult1, 0, 0);
            tlpStationResult.Dock = DockStyle.Fill;
            tlpStationResult.Location = new Point(3, 26);
            tlpStationResult.Margin = new Padding(0);
            tlpStationResult.Name = "tlpStationResult";
            tlpStationResult.RowCount = 1;
            tlpStationResult.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpStationResult.Size = new Size(401, 59);
            tlpStationResult.TabIndex = 1;
            // 
            // tagStationResult2
            // 
            tagStationResult2.BackColor = Color.FromArgb(108, 117, 125);
            tagStationResult2.Dock = DockStyle.Fill;
            tagStationResult2.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold);
            tagStationResult2.ForeColor = Color.White;
            tagStationResult2.Location = new Point(200, 0);
            tagStationResult2.Margin = new Padding(0);
            tagStationResult2.Name = "tagStationResult2";
            tagStationResult2.Size = new Size(201, 59);
            tagStationResult2.TabIndex = 1;
            tagStationResult2.Text = "工位2--";
            tagStationResult2.Visible = false;
            // 
            // tagStationResult1
            // 
            tagStationResult1.BackColor = Color.FromArgb(108, 117, 125);
            tagStationResult1.Dock = DockStyle.Fill;
            tagStationResult1.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold);
            tagStationResult1.ForeColor = Color.White;
            tagStationResult1.Location = new Point(0, 0);
            tagStationResult1.Margin = new Padding(0);
            tagStationResult1.Name = "tagStationResult1";
            tagStationResult1.Size = new Size(200, 59);
            tagStationResult1.TabIndex = 0;
            tagStationResult1.Text = "工位1--";
            // 
            // tlpWorkOrderInfo
            // 
            tlpWorkOrderInfo.ColumnCount = 1;
            tlpWorkOrderInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpWorkOrderInfo.Controls.Add(tlpUserInfo2, 0, 3);
            tlpWorkOrderInfo.Controls.Add(tlpButton, 0, 11);
            tlpWorkOrderInfo.Controls.Add(tlpStation, 0, 1);
            tlpWorkOrderInfo.Controls.Add(tlpProductNameAndDrawingNo, 0, 8);
            tlpWorkOrderInfo.Controls.Add(tlpProgramName, 0, 10);
            tlpWorkOrderInfo.Controls.Add(panelTimeAndVersion, 0, 0);
            tlpWorkOrderInfo.Controls.Add(tlpProcessInfo, 0, 9);
            tlpWorkOrderInfo.Controls.Add(tlpSpecAndBatch, 0, 7);
            tlpWorkOrderInfo.Controls.Add(tlpStationInfo, 0, 4);
            tlpWorkOrderInfo.Controls.Add(tlpProductModel, 0, 6);
            tlpWorkOrderInfo.Controls.Add(tlpProductNum, 0, 5);
            tlpWorkOrderInfo.Controls.Add(tlpUserInfo1, 0, 2);
            tlpWorkOrderInfo.Dock = DockStyle.Fill;
            tlpWorkOrderInfo.Location = new Point(4, 3);
            tlpWorkOrderInfo.Margin = new Padding(4, 3, 4, 3);
            tlpWorkOrderInfo.Name = "tlpWorkOrderInfo";
            tlpWorkOrderInfo.RowCount = 12;
            tlpWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 9.090909F));
            tlpWorkOrderInfo.RowStyles.Add(new RowStyle());
            tlpWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 9.090909F));
            tlpWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 9.090909F));
            tlpWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 9.090909F));
            tlpWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 9.090909F));
            tlpWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 9.090909F));
            tlpWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 9.090909F));
            tlpWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 9.090909F));
            tlpWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 9.090909F));
            tlpWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 9.090909F));
            tlpWorkOrderInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 9.090909F));
            tlpWorkOrderInfo.Size = new Size(401, 443);
            tlpWorkOrderInfo.TabIndex = 0;
            // 
            // tlpUserInfo2
            // 
            tlpUserInfo2.ColumnCount = 4;
            tlpUserInfo2.ColumnStyles.Add(new ColumnStyle());
            tlpUserInfo2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpUserInfo2.ColumnStyles.Add(new ColumnStyle());
            tlpUserInfo2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpUserInfo2.Controls.Add(TeamName, 3, 0);
            tlpUserInfo2.Controls.Add(lblTeamName, 2, 0);
            tlpUserInfo2.Controls.Add(lblDeptName, 0, 0);
            tlpUserInfo2.Controls.Add(inputDeptName, 1, 0);
            tlpUserInfo2.Dock = DockStyle.Fill;
            tlpUserInfo2.Location = new Point(0, 108);
            tlpUserInfo2.Margin = new Padding(0);
            tlpUserInfo2.Name = "tlpUserInfo2";
            tlpUserInfo2.RowCount = 1;
            tlpUserInfo2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpUserInfo2.Size = new Size(401, 36);
            tlpUserInfo2.TabIndex = 19;
            // 
            // TeamName
            // 
            TeamName.Dock = DockStyle.Fill;
            TeamName.Location = new Point(270, 0);
            TeamName.Margin = new Padding(0);
            TeamName.Name = "TeamName";
            TeamName.ReadOnly = true;
            TeamName.Size = new Size(131, 36);
            TeamName.TabIndex = 3;
            // 
            // lblTeamName
            // 
            lblTeamName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblTeamName.Dock = DockStyle.Fill;
            lblTeamName.Location = new Point(200, 0);
            lblTeamName.Margin = new Padding(0);
            lblTeamName.Name = "lblTeamName";
            lblTeamName.Size = new Size(70, 36);
            lblTeamName.TabIndex = 2;
            lblTeamName.Text = "班组名称";
            // 
            // lblDeptName
            // 
            lblDeptName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDeptName.Dock = DockStyle.Fill;
            lblDeptName.Location = new Point(0, 0);
            lblDeptName.Margin = new Padding(0);
            lblDeptName.Name = "lblDeptName";
            lblDeptName.Size = new Size(70, 36);
            lblDeptName.TabIndex = 0;
            lblDeptName.Text = "部门名称";
            // 
            // inputDeptName
            // 
            inputDeptName.Dock = DockStyle.Fill;
            inputDeptName.Location = new Point(70, 0);
            inputDeptName.Margin = new Padding(0);
            inputDeptName.Name = "inputDeptName";
            inputDeptName.ReadOnly = true;
            inputDeptName.Size = new Size(130, 36);
            inputDeptName.TabIndex = 1;
            // 
            // tlpButton
            // 
            tlpButton.ColumnCount = 4;
            tlpButton.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpButton.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpButton.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpButton.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpButton.Controls.Add(btnExpEnd, 3, 0);
            tlpButton.Controls.Add(btnExpStart, 2, 0);
            tlpButton.Controls.Add(btnLocalWorkOrder, 1, 0);
            tlpButton.Controls.Add(btnGetWO, 0, 0);
            tlpButton.Dock = DockStyle.Fill;
            tlpButton.Location = new Point(0, 396);
            tlpButton.Margin = new Padding(0);
            tlpButton.Name = "tlpButton";
            tlpButton.RowCount = 1;
            tlpButton.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpButton.Size = new Size(401, 47);
            tlpButton.TabIndex = 18;
            // 
            // btnExpEnd
            // 
            btnExpEnd.BorderWidth = 1F;
            btnExpEnd.Dock = DockStyle.Fill;
            btnExpEnd.IconGap = 0.2F;
            btnExpEnd.IconSvg = "CheckCircleOutlined";
            btnExpEnd.Location = new Point(300, 0);
            btnExpEnd.Margin = new Padding(0);
            btnExpEnd.Name = "btnExpEnd";
            btnExpEnd.Size = new Size(101, 47);
            btnExpEnd.TabIndex = 3;
            btnExpEnd.Tag = "perm:button.monitor.finish-report:visible";
            btnExpEnd.Text = "完工上报";
            // 
            // btnExpStart
            // 
            btnExpStart.BorderWidth = 1F;
            btnExpStart.Dock = DockStyle.Fill;
            btnExpStart.IconGap = 0.2F;
            btnExpStart.IconSvg = "PlayCircleOutlined";
            btnExpStart.Location = new Point(200, 0);
            btnExpStart.Margin = new Padding(0);
            btnExpStart.Name = "btnExpStart";
            btnExpStart.Size = new Size(100, 47);
            btnExpStart.TabIndex = 3;
            btnExpStart.Tag = "perm:button.monitor.start-report:visible";
            btnExpStart.Text = "开工上报";
            // 
            // btnLocalWorkOrder
            // 
            btnLocalWorkOrder.BorderWidth = 1F;
            btnLocalWorkOrder.Dock = DockStyle.Fill;
            btnLocalWorkOrder.IconGap = 0.2F;
            btnLocalWorkOrder.IconSvg = "FileAddOutlined";
            btnLocalWorkOrder.Location = new Point(100, 0);
            btnLocalWorkOrder.Margin = new Padding(0);
            btnLocalWorkOrder.Name = "btnLocalWorkOrder";
            btnLocalWorkOrder.Size = new Size(100, 47);
            btnLocalWorkOrder.TabIndex = 5;
            btnLocalWorkOrder.Tag = "perm:button.monitor.local-work-order:visible";
            btnLocalWorkOrder.Text = "离线开工";
            // 
            // btnGetWO
            // 
            btnGetWO.Dock = DockStyle.Fill;
            btnGetWO.IconSvg = "CloudDownloadOutlined";
            btnGetWO.Location = new Point(0, 0);
            btnGetWO.Margin = new Padding(0);
            btnGetWO.Name = "btnGetWO";
            btnGetWO.Size = new Size(100, 47);
            btnGetWO.TabIndex = 4;
            btnGetWO.Tag = "perm:button.monitor.get-work-order:visible";
            btnGetWO.Text = "获取工单";
            // 
            // tlpStation
            // 
            tlpStation.ColumnCount = 1;
            tlpStation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpStation.Controls.Add(segmentedStationSwitch, 0, 0);
            tlpStation.Dock = DockStyle.Fill;
            tlpStation.Location = new Point(0, 36);
            tlpStation.Margin = new Padding(0);
            tlpStation.Name = "tlpStation";
            tlpStation.RowCount = 1;
            tlpStation.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpStation.Size = new Size(401, 36);
            tlpStation.TabIndex = 9;
            // 
            // segmentedStationSwitch
            // 
            segmentedStationSwitch.BackActive = SystemColors.Window;
            segmentedStationSwitch.Dock = DockStyle.Fill;
            segmentedStationSwitch.ForeActive = Color.RoyalBlue;
            segmentedStationSwitch.Full = true;
            segmentedItem1.LocalizationText = "";
            segmentedItem1.Text = "工位1";
            segmentedItem2.Text = "工位2";
            segmentedStationSwitch.Items.Add(segmentedItem1);
            segmentedStationSwitch.Items.Add(segmentedItem2);
            segmentedStationSwitch.Location = new Point(0, 0);
            segmentedStationSwitch.Margin = new Padding(0);
            segmentedStationSwitch.Name = "segmentedStationSwitch";
            segmentedStationSwitch.Size = new Size(401, 36);
            segmentedStationSwitch.TabIndex = 8;
            // 
            // tlpProductNameAndDrawingNo
            // 
            tlpProductNameAndDrawingNo.AutoSize = true;
            tlpProductNameAndDrawingNo.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpProductNameAndDrawingNo.ColumnCount = 4;
            tlpProductNameAndDrawingNo.ColumnStyles.Add(new ColumnStyle());
            tlpProductNameAndDrawingNo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpProductNameAndDrawingNo.ColumnStyles.Add(new ColumnStyle());
            tlpProductNameAndDrawingNo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpProductNameAndDrawingNo.Controls.Add(lblPartName, 0, 0);
            tlpProductNameAndDrawingNo.Controls.Add(inputProductName, 1, 0);
            tlpProductNameAndDrawingNo.Controls.Add(lblDrawingNo, 2, 0);
            tlpProductNameAndDrawingNo.Controls.Add(inputDrawingNo, 3, 0);
            tlpProductNameAndDrawingNo.Dock = DockStyle.Fill;
            tlpProductNameAndDrawingNo.Location = new Point(0, 288);
            tlpProductNameAndDrawingNo.Margin = new Padding(0);
            tlpProductNameAndDrawingNo.Name = "tlpProductNameAndDrawingNo";
            tlpProductNameAndDrawingNo.RowCount = 1;
            tlpProductNameAndDrawingNo.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProductNameAndDrawingNo.Size = new Size(401, 36);
            tlpProductNameAndDrawingNo.TabIndex = 0;
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
            lblPartName.Size = new Size(70, 36);
            lblPartName.TabIndex = 29;
            lblPartName.Text = "部件名称";
            // 
            // inputProductName
            // 
            inputProductName.Dock = DockStyle.Fill;
            inputProductName.ImeMode = ImeMode.Inherit;
            inputProductName.Location = new Point(70, 0);
            inputProductName.Margin = new Padding(0);
            inputProductName.Name = "inputProductName";
            inputProductName.ReadOnly = true;
            inputProductName.Size = new Size(130, 36);
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
            lblDrawingNo.Size = new Size(70, 36);
            lblDrawingNo.TabIndex = 29;
            lblDrawingNo.Text = "零件图号";
            // 
            // inputDrawingNo
            // 
            inputDrawingNo.Dock = DockStyle.Fill;
            inputDrawingNo.ImeMode = ImeMode.Inherit;
            inputDrawingNo.Location = new Point(270, 0);
            inputDrawingNo.Margin = new Padding(0);
            inputDrawingNo.Name = "inputDrawingNo";
            inputDrawingNo.ReadOnly = true;
            inputDrawingNo.Size = new Size(131, 36);
            inputDrawingNo.TabIndex = 4;
            // 
            // tlpProgramName
            // 
            tlpProgramName.ColumnCount = 2;
            tlpProgramName.ColumnStyles.Add(new ColumnStyle());
            tlpProgramName.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpProgramName.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tlpProgramName.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tlpProgramName.Controls.Add(inputProgramName, 1, 0);
            tlpProgramName.Controls.Add(lblProgramName, 0, 0);
            tlpProgramName.Dock = DockStyle.Fill;
            tlpProgramName.Location = new Point(0, 360);
            tlpProgramName.Margin = new Padding(0);
            tlpProgramName.Name = "tlpProgramName";
            tlpProgramName.RowCount = 1;
            tlpProgramName.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProgramName.Size = new Size(401, 36);
            tlpProgramName.TabIndex = 8;
            // 
            // inputProgramName
            // 
            inputProgramName.Dock = DockStyle.Fill;
            inputProgramName.ImeMode = ImeMode.Inherit;
            inputProgramName.Location = new Point(70, 0);
            inputProgramName.Margin = new Padding(0);
            inputProgramName.Name = "inputProgramName";
            inputProgramName.ReadOnly = true;
            inputProgramName.Size = new Size(331, 36);
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
            lblProgramName.Size = new Size(70, 36);
            lblProgramName.TabIndex = 23;
            lblProgramName.Text = "程序名称";
            // 
            // panelTimeAndVersion
            // 
            panelTimeAndVersion.Controls.Add(lblVersion);
            panelTimeAndVersion.Controls.Add(lblCurTime);
            panelTimeAndVersion.Dock = DockStyle.Fill;
            panelTimeAndVersion.Location = new Point(0, 0);
            panelTimeAndVersion.Margin = new Padding(0);
            panelTimeAndVersion.Name = "panelTimeAndVersion";
            panelTimeAndVersion.Size = new Size(401, 36);
            panelTimeAndVersion.TabIndex = 0;
            // 
            // lblVersion
            // 
            lblVersion.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblVersion.Dock = DockStyle.Right;
            lblVersion.Font = new Font("Microsoft YaHei UI", 9F);
            lblVersion.Location = new Point(331, 0);
            lblVersion.Name = "lblVersion";
            lblVersion.Prefix = "v";
            lblVersion.Size = new Size(70, 36);
            lblVersion.TabIndex = 2;
            lblVersion.Text = "Version";
            lblVersion.TextAlign = ContentAlignment.BottomRight;
            // 
            // lblCurTime
            // 
            lblCurTime.Dock = DockStyle.Fill;
            lblCurTime.Font = new Font("Segoe UI", 15.7F);
            lblCurTime.ImeMode = ImeMode.NoControl;
            lblCurTime.Location = new Point(0, 0);
            lblCurTime.Margin = new Padding(0);
            lblCurTime.Name = "lblCurTime";
            lblCurTime.Size = new Size(401, 36);
            lblCurTime.TabIndex = 6;
            lblCurTime.Text = "当前时间";
            lblCurTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpProcessInfo
            // 
            tlpProcessInfo.ColumnCount = 6;
            tlpProcessInfo.ColumnStyles.Add(new ColumnStyle());
            tlpProcessInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpProcessInfo.ColumnStyles.Add(new ColumnStyle());
            tlpProcessInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpProcessInfo.ColumnStyles.Add(new ColumnStyle());
            tlpProcessInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpProcessInfo.Controls.Add(lblProcessName, 0, 0);
            tlpProcessInfo.Controls.Add(inputProcessNo, 3, 0);
            tlpProcessInfo.Controls.Add(selectItemName, 1, 0);
            tlpProcessInfo.Controls.Add(lblProcessNo, 2, 0);
            tlpProcessInfo.Controls.Add(lblStartAmount, 4, 0);
            tlpProcessInfo.Controls.Add(inputStartAmount, 5, 0);
            tlpProcessInfo.Dock = DockStyle.Fill;
            tlpProcessInfo.Location = new Point(0, 324);
            tlpProcessInfo.Margin = new Padding(0);
            tlpProcessInfo.Name = "tlpProcessInfo";
            tlpProcessInfo.RowCount = 1;
            tlpProcessInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProcessInfo.Size = new Size(401, 36);
            tlpProcessInfo.TabIndex = 2;
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
            lblProcessName.Size = new Size(70, 36);
            lblProcessName.TabIndex = 30;
            lblProcessName.Text = "工序名称";
            // 
            // inputProcessNo
            // 
            inputProcessNo.Dock = DockStyle.Fill;
            inputProcessNo.ImeMode = ImeMode.Inherit;
            inputProcessNo.Location = new Point(227, 0);
            inputProcessNo.Margin = new Padding(0);
            inputProcessNo.Name = "inputProcessNo";
            inputProcessNo.ReadOnly = true;
            inputProcessNo.Size = new Size(52, 36);
            inputProcessNo.TabIndex = 4;
            // 
            // selectItemName
            // 
            selectItemName.Dock = DockStyle.Fill;
            selectItemName.Location = new Point(70, 0);
            selectItemName.Margin = new Padding(0);
            selectItemName.MaxCount = 10;
            selectItemName.Name = "selectItemName";
            selectItemName.Size = new Size(104, 36);
            selectItemName.TabIndex = 4;
            // 
            // lblProcessNo
            // 
            lblProcessNo.AutoEllipsis = true;
            lblProcessNo.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProcessNo.AutoSizePadding = true;
            lblProcessNo.Dock = DockStyle.Fill;
            lblProcessNo.Location = new Point(174, 0);
            lblProcessNo.Margin = new Padding(0);
            lblProcessNo.Name = "lblProcessNo";
            lblProcessNo.Size = new Size(53, 36);
            lblProcessNo.TabIndex = 27;
            lblProcessNo.Text = "工序号";
            // 
            // lblStartAmount
            // 
            lblStartAmount.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblStartAmount.Dock = DockStyle.Fill;
            lblStartAmount.Location = new Point(279, 0);
            lblStartAmount.Margin = new Padding(0);
            lblStartAmount.Name = "lblStartAmount";
            lblStartAmount.Size = new Size(70, 36);
            lblStartAmount.TabIndex = 31;
            lblStartAmount.Text = "生产数量";
            // 
            // inputStartAmount
            // 
            inputStartAmount.Dock = DockStyle.Fill;
            inputStartAmount.Location = new Point(349, 0);
            inputStartAmount.Margin = new Padding(0);
            inputStartAmount.Name = "inputStartAmount";
            inputStartAmount.ReadOnly = true;
            inputStartAmount.Size = new Size(52, 36);
            inputStartAmount.TabIndex = 32;
            // 
            // tlpSpecAndBatch
            // 
            tlpSpecAndBatch.ColumnCount = 4;
            tlpSpecAndBatch.ColumnStyles.Add(new ColumnStyle());
            tlpSpecAndBatch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpSpecAndBatch.ColumnStyles.Add(new ColumnStyle());
            tlpSpecAndBatch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpSpecAndBatch.Controls.Add(inputBatch, 3, 0);
            tlpSpecAndBatch.Controls.Add(lblSpec, 0, 0);
            tlpSpecAndBatch.Controls.Add(lblBatchNo, 2, 0);
            tlpSpecAndBatch.Controls.Add(inputSpec, 1, 0);
            tlpSpecAndBatch.Dock = DockStyle.Fill;
            tlpSpecAndBatch.Location = new Point(0, 252);
            tlpSpecAndBatch.Margin = new Padding(0);
            tlpSpecAndBatch.Name = "tlpSpecAndBatch";
            tlpSpecAndBatch.RowCount = 1;
            tlpSpecAndBatch.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpSpecAndBatch.Size = new Size(401, 36);
            tlpSpecAndBatch.TabIndex = 0;
            // 
            // inputBatch
            // 
            inputBatch.Dock = DockStyle.Fill;
            inputBatch.ImeMode = ImeMode.Inherit;
            inputBatch.Location = new Point(235, 0);
            inputBatch.Margin = new Padding(0);
            inputBatch.Name = "inputBatch";
            inputBatch.ReadOnly = true;
            inputBatch.Size = new Size(166, 36);
            inputBatch.TabIndex = 4;
            // 
            // lblSpec
            // 
            lblSpec.AutoEllipsis = true;
            lblSpec.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblSpec.AutoSizePadding = true;
            lblSpec.Dock = DockStyle.Fill;
            lblSpec.Location = new Point(0, 0);
            lblSpec.Margin = new Padding(0);
            lblSpec.Name = "lblSpec";
            lblSpec.Size = new Size(35, 36);
            lblSpec.TabIndex = 29;
            lblSpec.Text = "规格";
            // 
            // lblBatchNo
            // 
            lblBatchNo.AutoEllipsis = true;
            lblBatchNo.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblBatchNo.AutoSizePadding = true;
            lblBatchNo.Dock = DockStyle.Fill;
            lblBatchNo.Location = new Point(200, 0);
            lblBatchNo.Margin = new Padding(0);
            lblBatchNo.Name = "lblBatchNo";
            lblBatchNo.Size = new Size(35, 36);
            lblBatchNo.TabIndex = 29;
            lblBatchNo.Text = "批次";
            // 
            // inputSpec
            // 
            inputSpec.Dock = DockStyle.Fill;
            inputSpec.ImeMode = ImeMode.Inherit;
            inputSpec.Location = new Point(35, 0);
            inputSpec.Margin = new Padding(0);
            inputSpec.Name = "inputSpec";
            inputSpec.ReadOnly = true;
            inputSpec.Size = new Size(165, 36);
            inputSpec.TabIndex = 4;
            // 
            // tlpStationInfo
            // 
            tlpStationInfo.ColumnCount = 4;
            tlpStationInfo.ColumnStyles.Add(new ColumnStyle());
            tlpStationInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpStationInfo.ColumnStyles.Add(new ColumnStyle());
            tlpStationInfo.ColumnStyles.Add(new ColumnStyle());
            tlpStationInfo.Controls.Add(lblWorkOrder, 0, 0);
            tlpStationInfo.Controls.Add(btnChangeWO, 2, 0);
            tlpStationInfo.Controls.Add(inputSN, 1, 0);
            tlpStationInfo.Controls.Add(btnEditWO, 3, 0);
            tlpStationInfo.Dock = DockStyle.Fill;
            tlpStationInfo.Location = new Point(0, 144);
            tlpStationInfo.Margin = new Padding(0);
            tlpStationInfo.Name = "tlpStationInfo";
            tlpStationInfo.RowCount = 1;
            tlpStationInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpStationInfo.Size = new Size(401, 36);
            tlpStationInfo.TabIndex = 7;
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
            lblWorkOrder.Size = new Size(53, 36);
            lblWorkOrder.TabIndex = 22;
            lblWorkOrder.Text = "工单号";
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
            btnChangeWO.Size = new Size(84, 36);
            btnChangeWO.TabIndex = 3;
            btnChangeWO.Tag = "perm:button.monitor.change-work-order:visible";
            btnChangeWO.Text = "变更";
            // 
            // inputSN
            // 
            inputSN.Dock = DockStyle.Fill;
            inputSN.ImeMode = ImeMode.Inherit;
            inputSN.Location = new Point(53, 0);
            inputSN.Margin = new Padding(0);
            inputSN.Name = "inputSN";
            inputSN.ReadOnly = true;
            inputSN.Size = new Size(180, 36);
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
            btnEditWO.Size = new Size(84, 36);
            btnEditWO.TabIndex = 3;
            btnEditWO.Tag = "perm:button.monitor.edit-work-order:visible";
            btnEditWO.Text = "微调";
            // 
            // tlpProductModel
            // 
            tlpProductModel.ColumnCount = 2;
            tlpProductModel.ColumnStyles.Add(new ColumnStyle());
            tlpProductModel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpProductModel.Controls.Add(lblProdModel, 0, 0);
            tlpProductModel.Controls.Add(inputProdModel, 1, 0);
            tlpProductModel.Dock = DockStyle.Fill;
            tlpProductModel.Location = new Point(0, 216);
            tlpProductModel.Margin = new Padding(0);
            tlpProductModel.Name = "tlpProductModel";
            tlpProductModel.RowCount = 1;
            tlpProductModel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProductModel.Size = new Size(401, 36);
            tlpProductModel.TabIndex = 1;
            // 
            // lblProdModel
            // 
            lblProdModel.AutoEllipsis = true;
            lblProdModel.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProdModel.AutoSizePadding = true;
            lblProdModel.Dock = DockStyle.Fill;
            lblProdModel.Location = new Point(0, 0);
            lblProdModel.Margin = new Padding(0);
            lblProdModel.Name = "lblProdModel";
            lblProdModel.Size = new Size(70, 36);
            lblProdModel.TabIndex = 23;
            lblProdModel.Text = "产品型号";
            // 
            // inputProdModel
            // 
            inputProdModel.Dock = DockStyle.Fill;
            inputProdModel.ImeMode = ImeMode.Inherit;
            inputProdModel.Location = new Point(70, 0);
            inputProdModel.Margin = new Padding(0);
            inputProdModel.Name = "inputProdModel";
            inputProdModel.ReadOnly = true;
            inputProdModel.Size = new Size(331, 36);
            inputProdModel.TabIndex = 4;
            // 
            // tlpProductNum
            // 
            tlpProductNum.ColumnCount = 3;
            tlpProductNum.ColumnStyles.Add(new ColumnStyle());
            tlpProductNum.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpProductNum.ColumnStyles.Add(new ColumnStyle());
            tlpProductNum.Controls.Add(inputProdNum, 1, 0);
            tlpProductNum.Controls.Add(lblProductNo, 0, 0);
            tlpProductNum.Controls.Add(selectRecipeCode, 2, 0);
            tlpProductNum.Dock = DockStyle.Fill;
            tlpProductNum.Location = new Point(0, 180);
            tlpProductNum.Margin = new Padding(0);
            tlpProductNum.Name = "tlpProductNum";
            tlpProductNum.RowCount = 1;
            tlpProductNum.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProductNum.Size = new Size(401, 36);
            tlpProductNum.TabIndex = 10;
            // 
            // inputProdNum
            // 
            inputProdNum.Dock = DockStyle.Fill;
            inputProdNum.ImeMode = ImeMode.Inherit;
            inputProdNum.Location = new Point(70, 0);
            inputProdNum.Margin = new Padding(0);
            inputProdNum.Name = "inputProdNum";
            inputProdNum.ReadOnly = true;
            inputProdNum.Size = new Size(163, 36);
            inputProdNum.TabIndex = 4;
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
            lblProductNo.Size = new Size(70, 36);
            lblProductNo.TabIndex = 24;
            lblProductNo.Text = "产品工号";
            // 
            // selectRecipeCode
            // 
            selectRecipeCode.Dock = DockStyle.Fill;
            selectRecipeCode.Location = new Point(233, 0);
            selectRecipeCode.Margin = new Padding(0);
            selectRecipeCode.Name = "selectRecipeCode";
            selectRecipeCode.PrefixText = "配方号：";
            selectRecipeCode.ReadOnly = true;
            selectRecipeCode.Size = new Size(168, 36);
            selectRecipeCode.TabIndex = 25;
            selectRecipeCode.Text = "--";
            // 
            // tlpUserInfo1
            // 
            tlpUserInfo1.ColumnCount = 4;
            tlpUserInfo1.ColumnStyles.Add(new ColumnStyle());
            tlpUserInfo1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpUserInfo1.ColumnStyles.Add(new ColumnStyle());
            tlpUserInfo1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpUserInfo1.Controls.Add(MesUserNumber, 3, 0);
            tlpUserInfo1.Controls.Add(lblUserNumber, 2, 0);
            tlpUserInfo1.Controls.Add(lblUserName, 0, 0);
            tlpUserInfo1.Controls.Add(MesUserName, 1, 0);
            tlpUserInfo1.Dock = DockStyle.Fill;
            tlpUserInfo1.Location = new Point(0, 72);
            tlpUserInfo1.Margin = new Padding(0);
            tlpUserInfo1.Name = "tlpUserInfo1";
            tlpUserInfo1.RowCount = 1;
            tlpUserInfo1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpUserInfo1.Size = new Size(401, 36);
            tlpUserInfo1.TabIndex = 18;
            // 
            // MesUserNumber
            // 
            MesUserNumber.Dock = DockStyle.Fill;
            MesUserNumber.Location = new Point(244, 0);
            MesUserNumber.Margin = new Padding(0);
            MesUserNumber.Name = "MesUserNumber";
            MesUserNumber.ReadOnly = true;
            MesUserNumber.Size = new Size(157, 36);
            MesUserNumber.TabIndex = 3;
            // 
            // lblUserNumber
            // 
            lblUserNumber.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblUserNumber.Dock = DockStyle.Fill;
            lblUserNumber.Location = new Point(191, 0);
            lblUserNumber.Margin = new Padding(0);
            lblUserNumber.Name = "lblUserNumber";
            lblUserNumber.Size = new Size(53, 36);
            lblUserNumber.TabIndex = 2;
            lblUserNumber.Text = "员工号";
            // 
            // lblUserName
            // 
            lblUserName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblUserName.Dock = DockStyle.Fill;
            lblUserName.Location = new Point(0, 0);
            lblUserName.Margin = new Padding(0);
            lblUserName.Name = "lblUserName";
            lblUserName.Size = new Size(35, 36);
            lblUserName.TabIndex = 0;
            lblUserName.Text = "姓名";
            // 
            // MesUserName
            // 
            MesUserName.Dock = DockStyle.Fill;
            MesUserName.Location = new Point(35, 0);
            MesUserName.Margin = new Padding(0);
            MesUserName.Name = "MesUserName";
            MesUserName.ReadOnly = true;
            MesUserName.Size = new Size(156, 36);
            MesUserName.TabIndex = 1;
            // 
            // grpErrorTips
            // 
            grpErrorTips.Controls.Add(inputErrorTips);
            grpErrorTips.Dock = DockStyle.Fill;
            grpErrorTips.ForeColor = SystemColors.ActiveCaptionText;
            grpErrorTips.Location = new Point(1, 449);
            grpErrorTips.Margin = new Padding(1, 0, 1, 0);
            grpErrorTips.MaximumSize = new Size(0, 109);
            grpErrorTips.MinimumSize = new Size(0, 88);
            grpErrorTips.Name = "grpErrorTips";
            grpErrorTips.Size = new Size(407, 88);
            grpErrorTips.TabIndex = 4;
            grpErrorTips.TabStop = false;
            grpErrorTips.Text = "异常提示：";
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
            inputErrorTips.Size = new Size(401, 59);
            inputErrorTips.TabIndex = 1;
            // 
            // grpRunningStatus
            // 
            grpRunningStatus.Controls.Add(inputRunningStatus);
            grpRunningStatus.Dock = DockStyle.Fill;
            grpRunningStatus.ForeColor = SystemColors.ActiveCaptionText;
            grpRunningStatus.Location = new Point(1, 537);
            grpRunningStatus.Margin = new Padding(1, 0, 1, 0);
            grpRunningStatus.MaximumSize = new Size(0, 109);
            grpRunningStatus.MinimumSize = new Size(0, 88);
            grpRunningStatus.Name = "grpRunningStatus";
            grpRunningStatus.Size = new Size(407, 88);
            grpRunningStatus.TabIndex = 3;
            grpRunningStatus.TabStop = false;
            grpRunningStatus.Text = "运行状态：";
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
            inputRunningStatus.Size = new Size(401, 59);
            inputRunningStatus.TabIndex = 1;
            // 
            // tabsMetrics
            // 
            tabsMetrics.Controls.Add(tabsMetrics1);
            tabsMetrics.Controls.Add(tabsMetrics2);
            tabsMetrics.Dock = DockStyle.Fill;
            tabsMetrics.Location = new Point(1, 713);
            tabsMetrics.Margin = new Padding(1, 0, 1, 1);
            tabsMetrics.Name = "tabsMetrics";
            tabsMetrics.Pages.Add(tabsMetrics1);
            tabsMetrics.Pages.Add(tabsMetrics2);
            tabsMetrics.Size = new Size(407, 218);
            tabsMetrics.Style = styleLine2;
            tabsMetrics.TabIndex = 5;
            tabsMetrics.Text = "tabsMetrics";
            // 
            // tabsMetrics1
            // 
            tabsMetrics1.Controls.Add(tableMetric1);
            tabsMetrics1.Location = new Point(0, 35);
            tabsMetrics1.Name = "tabsMetrics1";
            tabsMetrics1.Size = new Size(407, 183);
            tabsMetrics1.TabIndex = 0;
            tabsMetrics1.Text = "工位1";
            // 
            // tableMetric1
            // 
            tableMetric1.AutoSizeColumnsMode = AntdUI.ColumnsMode.Fill;
            tableMetric1.BorderRenderMode = AntdUI.TableBorderMode.High;
            tableMetric1.Dock = DockStyle.Fill;
            tableMetric1.Gap = 8;
            tableMetric1.GapCell = 5;
            tableMetric1.Gaps = new Size(8, 8);
            tableMetric1.Location = new Point(0, 0);
            tableMetric1.Margin = new Padding(1, 0, 1, 1);
            tableMetric1.MultipleRows = true;
            tableMetric1.Name = "tableMetric1";
            tableMetric1.Radius = 10;
            tableMetric1.Size = new Size(407, 183);
            tableMetric1.TabIndex = 1;
            tableMetric1.Text = "生产指标";
            // 
            // tabsMetrics2
            // 
            tabsMetrics2.Controls.Add(tableMetric2);
            tabsMetrics2.Location = new Point(-814, -366);
            tabsMetrics2.Name = "tabsMetrics2";
            tabsMetrics2.Size = new Size(407, 183);
            tabsMetrics2.TabIndex = 1;
            tabsMetrics2.Text = "工位2";
            // 
            // tableMetric2
            // 
            tableMetric2.AutoSizeColumnsMode = AntdUI.ColumnsMode.Fill;
            tableMetric2.BorderRenderMode = AntdUI.TableBorderMode.High;
            tableMetric2.Dock = DockStyle.Fill;
            tableMetric2.Gap = 8;
            tableMetric2.GapCell = 5;
            tableMetric2.Gaps = new Size(8, 8);
            tableMetric2.Location = new Point(0, 0);
            tableMetric2.Margin = new Padding(1, 0, 1, 1);
            tableMetric2.MultipleRows = true;
            tableMetric2.Name = "tableMetric2";
            tableMetric2.Radius = 10;
            tableMetric2.Size = new Size(407, 183);
            tableMetric2.TabIndex = 2;
            tableMetric2.Text = "生产指标";
            // 
            // lblLiveResult
            // 
            lblLiveResult.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblLiveResult.Dock = DockStyle.Fill;
            lblLiveResult.Location = new Point(385, 0);
            lblLiveResult.Margin = new Padding(0);
            lblLiveResult.Name = "lblLiveResult";
            lblLiveResult.Size = new Size(63, 62);
            lblLiveResult.TabIndex = 12;
            lblLiveResult.Text = "产品结果";
            lblLiveResult.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblLiveTouchCount
            // 
            lblLiveTouchCount.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblLiveTouchCount.Dock = DockStyle.Fill;
            lblLiveTouchCount.Location = new Point(762, 0);
            lblLiveTouchCount.Margin = new Padding(0);
            lblLiveTouchCount.Name = "lblLiveTouchCount";
            lblLiveTouchCount.Size = new Size(63, 62);
            lblLiveTouchCount.TabIndex = 10;
            lblLiveTouchCount.Text = "焊点数量";
            lblLiveTouchCount.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            label7.AutoSizeMode = AntdUI.TAutoSize.Width;
            label7.Dock = DockStyle.Fill;
            label7.Location = new Point(385, 0);
            label7.Margin = new Padding(0);
            label7.Name = "label7";
            label7.Size = new Size(63, 62);
            label7.TabIndex = 12;
            label7.Text = "产品结果";
            label7.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label8
            // 
            label8.AutoSizeMode = AntdUI.TAutoSize.Width;
            label8.Dock = DockStyle.Fill;
            label8.Location = new Point(762, 0);
            label8.Margin = new Padding(0);
            label8.Name = "label8";
            label8.Size = new Size(63, 62);
            label8.TabIndex = 10;
            label8.Text = "焊点数量";
            label8.TextAlign = ContentAlignment.MiddleRight;
            // 
            // MonitorView
            // 
            AutoScaleDimensions = new SizeF(10F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            Controls.Add(VerticalSplitter);
            Font = new Font("Microsoft YaHei UI", 10.5F);
            Margin = new Padding(6, 3, 6, 3);
            Name = "MonitorView";
            Size = new Size(1564, 932);
            VerticalSplitter.Panel1.ResumeLayout(false);
            VerticalSplitter.Panel1.PerformLayout();
            VerticalSplitter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)VerticalSplitter).EndInit();
            VerticalSplitter.ResumeLayout(false);
            tlpLeft.ResumeLayout(false);
            tlpLeft.PerformLayout();
            LeftTopLayout.ResumeLayout(false);
            tlpCommunicationStatus.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
            tabsPreview.ResumeLayout(false);
            tabsPreview1.ResumeLayout(false);
            PreviewLayout1.ResumeLayout(false);
            tlpStationOverview1.ResumeLayout(false);
            tlpStationOverview1.PerformLayout();
            HorizontalSplitter1.Panel1.ResumeLayout(false);
            HorizontalSplitter1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)HorizontalSplitter1).EndInit();
            HorizontalSplitter1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvPreview1).EndInit();
            panelHistory1.ResumeLayout(false);
            tabsPreview2.ResumeLayout(false);
            previewLayout2.ResumeLayout(false);
            HorizontalSplitter2.Panel1.ResumeLayout(false);
            HorizontalSplitter2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)HorizontalSplitter2).EndInit();
            HorizontalSplitter2.ResumeLayout(false);
            panelPreview2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvPreview2).EndInit();
            panelHistory2.ResumeLayout(false);
            tlpStationOverview2.ResumeLayout(false);
            tlpStationOverview2.PerformLayout();
            tlpRight.ResumeLayout(false);
            grpProductResult.ResumeLayout(false);
            tlpStationResult.ResumeLayout(false);
            tlpWorkOrderInfo.ResumeLayout(false);
            tlpWorkOrderInfo.PerformLayout();
            tlpUserInfo2.ResumeLayout(false);
            tlpUserInfo2.PerformLayout();
            tlpButton.ResumeLayout(false);
            tlpStation.ResumeLayout(false);
            tlpProductNameAndDrawingNo.ResumeLayout(false);
            tlpProductNameAndDrawingNo.PerformLayout();
            tlpProgramName.ResumeLayout(false);
            tlpProgramName.PerformLayout();
            panelTimeAndVersion.ResumeLayout(false);
            panelTimeAndVersion.PerformLayout();
            tlpProcessInfo.ResumeLayout(false);
            tlpProcessInfo.PerformLayout();
            tlpSpecAndBatch.ResumeLayout(false);
            tlpSpecAndBatch.PerformLayout();
            tlpStationInfo.ResumeLayout(false);
            tlpStationInfo.PerformLayout();
            tlpProductModel.ResumeLayout(false);
            tlpProductModel.PerformLayout();
            tlpProductNum.ResumeLayout(false);
            tlpProductNum.PerformLayout();
            tlpUserInfo1.ResumeLayout(false);
            tlpUserInfo1.PerformLayout();
            grpErrorTips.ResumeLayout(false);
            grpRunningStatus.ResumeLayout(false);
            tabsMetrics.ResumeLayout(false);
            tabsMetrics1.ResumeLayout(false);
            tabsMetrics2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.Splitter VerticalSplitter;
        private TableLayoutPanel tlpLeft;
        private AntdUI.Button btnExpStart;
        private AntdUI.Button btnExpEnd;
        private AntdUI.Button btnLocalWorkOrder;
        private TableLayoutPanel tlpCommunicationStatus;
        private AntdUI.Tag tagMes;
        private AntdUI.Tag tagPLC;
        private PictureBox picLogo;
        private Label lblTitle;
        private TableLayoutPanel tlpRight;
        private TableLayoutPanel tlpWorkOrderInfo;
        private AntdUI.Input inputProgramName;
        private AntdUI.Button btnChangeWO;
        private AntdUI.Input inputSN;
        private Label lblCurTime;
        private AntdUI.Input inputProcessNo;
        private AntdUI.Select selectItemName;
        private AntdUI.Input inputBatch;
        private AntdUI.Tag tagDeviceStatus;
        private AntdUI.Tag tagTaskStatus;
        private AntdUI.Label lblProcessName;
        private AntdUI.Label lblBatchNo;
        private AntdUI.Label lblProcessNo;
        private AntdUI.Label lblProductNo;
        private AntdUI.Label lblProgramName;
        private AntdUI.Label lblWorkOrder;
        private GroupBox grpErrorTips;
        private AntdUI.Input inputErrorTips;
        private AntdUI.Table tableMetric1;
        private GroupBox grpRunningStatus;
        private AntdUI.Input inputRunningStatus;
        private TableLayoutPanel tlpStationOverview1;
        private DataGridView dgvPreview1;
        private SlimHorizontalScrollBar HorizontalScrollBar1;
        private TableLayoutPanel tlpProcessInfo;
        private TableLayoutPanel tlpProgramName;
        private AntdUI.Label lblProdModel;
        private AntdUI.Input inputProdModel;
        private TableLayoutPanel tlpProductModel;
        private TableLayoutPanel tlpStationInfo;
        private TableLayoutPanel tlpSpecAndBatch;
        private AntdUI.Label lblSpec;
        private AntdUI.Input inputSpec;
        private TableLayoutPanel tlpProductNameAndDrawingNo;
        private AntdUI.Label lblPartName;
        private AntdUI.Input inputProductName;
        private AntdUI.Label lblDrawingNo;
        private AntdUI.Input inputDrawingNo;
        private TableLayoutPanel LeftTopLayout;
        private TableLayoutPanel tlpStationResult;
        private AntdUI.Tag tagStationResult2;
        private AntdUI.Tag tagStationResult1;
        private TableLayoutPanel tlpStation;
        private AntdUI.Segmented segmentedStationSwitch;
        private AntdUI.Button btnEditWO;
        private GroupBox grpProductResult;
        private TableLayoutPanel tlpProductNum;
        private AntdUI.Splitter HorizontalSplitter1;
        private Panel panelTimeAndVersion;
        private Panel panelHistory1;
        private TableLayoutPanel PreviewLayout1;
        private TableLayoutPanel tlpButton;
        private AntdUI.Label lblStartAmount;
        private AntdUI.Input inputStartAmount;
        private AntdUI.Tabs tabsMetrics;
        private AntdUI.TabPage tabsMetrics1;
        private AntdUI.Tabs tabsPreview;
        private AntdUI.TabPage tabsPreview1;
        private AntdUI.TabPage tabsPreview2;
        private AntdUI.Label lblLiveHint1;
        private AntdUI.Label lblLiveProductNo1;
        private AntdUI.Label lblLiveResult;
        private AntdUI.Tag tagLiveResult1;
        private AntdUI.Label lblLiveTouchCount;
        private AntdUI.Label lblLiveTouchNo1;
        private AntdUI.Button btnGetWO;
        private TableLayoutPanel tlpUserInfo1;
        private AntdUI.Input MesUserNumber;
        private AntdUI.Label lblUserNumber;
        private AntdUI.Label lblUserName;
        private AntdUI.Input MesUserName;
        private TableLayoutPanel tlpUserInfo2;
        private AntdUI.Input TeamName;
        private AntdUI.Label lblTeamName;
        private AntdUI.Label lblDeptName;
        private AntdUI.Input inputDeptName;
        private TableLayoutPanel previewLayout2;
        private TableLayoutPanel tlpStationOverview2;
        private AntdUI.Label lblLiveHint2;
        private AntdUI.Label lblLiveProductNo2;
        private AntdUI.Label label7;
        private AntdUI.Tag tagLiveResult2;
        private AntdUI.Label label8;
        private AntdUI.Label lblLiveTouchNo2;
        private AntdUI.Splitter HorizontalSplitter2;
        private Panel panelPreview2;
        private DataGridView dgvPreview2;
        private SlimHorizontalScrollBar HorizontalScrollBar2;
        private Panel panelHistory2;
        private AntdUI.Table tableHistory1;
        private AntdUI.Input inputProdNum;
        private AntdUI.Select selectRecipeCode;
        private AntdUI.TabPage tabsMetrics2;
        private AntdUI.Table tableMetric2;
        private AntdUI.Table tableHistory2;
        private AntdUI.Label lblVersion;
    }
}
