namespace AutoWeldSystem.UI.Views
{
    partial class LogManageView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                _mesLogService.LogWritten -= MesLogService_LogWritten;
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            tabLogCategories = new TabControl();
            tabMesLogs = new TabPage();
            mesRootLayout = new TableLayoutPanel();
            mesHeaderLayout = new TableLayoutPanel();
            mesTitleLayout = new TableLayoutPanel();
            lblMesTitle = new Label();
            lblMesDescription = new Label();
            mesToolbar = new FlowLayoutPanel();
            lblMesDate = new Label();
            dtpMesDate = new DateTimePicker();
            lblMesKeyword = new Label();
            txtMesKeyword = new TextBox();
            btnRefreshMes = new AntdUI.Button();
            btnOpenMesFolder = new AntdUI.Button();
            splitMesContent = new SplitContainer();
            dgvMesLogs = new DataGridView();
            tabMesDetails = new TabControl();
            tabBasicInfo = new TabPage();
            txtBasicInfo = new TextBox();
            tabRequestBody = new TabPage();
            txtRequestBody = new TextBox();
            tabResponseBody = new TabPage();
            txtResponseBody = new TextBox();
            tabProductionLogs = new TabPage();
            lblProductionReserved = new Label();
            tabExceptionLogs = new TabPage();
            lblExceptionReserved = new Label();
            tabLogCategories.SuspendLayout();
            tabMesLogs.SuspendLayout();
            mesRootLayout.SuspendLayout();
            mesHeaderLayout.SuspendLayout();
            mesTitleLayout.SuspendLayout();
            mesToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMesContent).BeginInit();
            splitMesContent.Panel1.SuspendLayout();
            splitMesContent.Panel2.SuspendLayout();
            splitMesContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvMesLogs).BeginInit();
            tabMesDetails.SuspendLayout();
            tabBasicInfo.SuspendLayout();
            tabRequestBody.SuspendLayout();
            tabResponseBody.SuspendLayout();
            tabProductionLogs.SuspendLayout();
            tabExceptionLogs.SuspendLayout();
            SuspendLayout();
            // 
            // tabLogCategories
            // 
            tabLogCategories.Controls.Add(tabMesLogs);
            tabLogCategories.Controls.Add(tabProductionLogs);
            tabLogCategories.Controls.Add(tabExceptionLogs);
            tabLogCategories.Dock = DockStyle.Fill;
            tabLogCategories.HotTrack = true;
            tabLogCategories.Location = new Point(0, 0);
            tabLogCategories.Name = "tabLogCategories";
            tabLogCategories.SelectedIndex = 0;
            tabLogCategories.Size = new Size(1366, 745);
            tabLogCategories.TabIndex = 0;
            // 
            // tabMesLogs
            // 
            tabMesLogs.Controls.Add(mesRootLayout);
            tabMesLogs.Location = new Point(4, 32);
            tabMesLogs.Name = "tabMesLogs";
            tabMesLogs.Padding = new Padding(3);
            tabMesLogs.Size = new Size(1358, 709);
            tabMesLogs.TabIndex = 0;
            tabMesLogs.Text = "MES交互日志";
            tabMesLogs.UseVisualStyleBackColor = true;
            // 
            // mesRootLayout
            // 
            mesRootLayout.ColumnCount = 1;
            mesRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mesRootLayout.Controls.Add(mesHeaderLayout, 0, 0);
            mesRootLayout.Controls.Add(splitMesContent, 0, 1);
            mesRootLayout.Dock = DockStyle.Fill;
            mesRootLayout.Location = new Point(3, 3);
            mesRootLayout.Name = "mesRootLayout";
            mesRootLayout.RowCount = 2;
            mesRootLayout.RowStyles.Add(new RowStyle());
            mesRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mesRootLayout.Size = new Size(1352, 703);
            mesRootLayout.TabIndex = 0;
            // 
            // mesHeaderLayout
            // 
            mesHeaderLayout.ColumnCount = 2;
            mesHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mesHeaderLayout.ColumnStyles.Add(new ColumnStyle());
            mesHeaderLayout.Controls.Add(mesTitleLayout, 0, 0);
            mesHeaderLayout.Controls.Add(mesToolbar, 1, 0);
            mesHeaderLayout.Dock = DockStyle.Fill;
            mesHeaderLayout.Location = new Point(20, 14);
            mesHeaderLayout.Margin = new Padding(20, 14, 20, 8);
            mesHeaderLayout.Name = "mesHeaderLayout";
            mesHeaderLayout.RowCount = 1;
            mesHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mesHeaderLayout.Size = new Size(1312, 70);
            mesHeaderLayout.TabIndex = 0;
            // 
            // mesTitleLayout
            // 
            mesTitleLayout.ColumnCount = 1;
            mesTitleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mesTitleLayout.Controls.Add(lblMesTitle, 0, 0);
            mesTitleLayout.Controls.Add(lblMesDescription, 0, 1);
            mesTitleLayout.Dock = DockStyle.Fill;
            mesTitleLayout.Location = new Point(0, 0);
            mesTitleLayout.Margin = new Padding(0);
            mesTitleLayout.Name = "mesTitleLayout";
            mesTitleLayout.RowCount = 2;
            mesTitleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            mesTitleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mesTitleLayout.Size = new Size(561, 70);
            mesTitleLayout.TabIndex = 0;
            // 
            // lblMesTitle
            // 
            lblMesTitle.AutoSize = true;
            lblMesTitle.Dock = DockStyle.Fill;
            lblMesTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            lblMesTitle.Location = new Point(0, 0);
            lblMesTitle.Margin = new Padding(0);
            lblMesTitle.Name = "lblMesTitle";
            lblMesTitle.Size = new Size(561, 34);
            lblMesTitle.TabIndex = 0;
            lblMesTitle.Text = "MES交互日志";
            lblMesTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblMesDescription
            // 
            lblMesDescription.AutoEllipsis = true;
            lblMesDescription.Dock = DockStyle.Fill;
            lblMesDescription.ForeColor = SystemColors.GrayText;
            lblMesDescription.Location = new Point(0, 34);
            lblMesDescription.Margin = new Padding(0);
            lblMesDescription.Name = "lblMesDescription";
            lblMesDescription.Size = new Size(561, 36);
            lblMesDescription.TabIndex = 1;
            lblMesDescription.Text = "实时显示本机 MES 请求、响应和异常，日志目录跟随系统设置。";
            lblMesDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // mesToolbar
            // 
            mesToolbar.AutoSize = true;
            mesToolbar.Controls.Add(lblMesDate);
            mesToolbar.Controls.Add(dtpMesDate);
            mesToolbar.Controls.Add(lblMesKeyword);
            mesToolbar.Controls.Add(txtMesKeyword);
            mesToolbar.Controls.Add(btnRefreshMes);
            mesToolbar.Controls.Add(btnOpenMesFolder);
            mesToolbar.Dock = DockStyle.Right;
            mesToolbar.Location = new Point(561, 0);
            mesToolbar.Margin = new Padding(0);
            mesToolbar.Name = "mesToolbar";
            mesToolbar.Padding = new Padding(0, 6, 0, 0);
            mesToolbar.Size = new Size(751, 70);
            mesToolbar.TabIndex = 1;
            mesToolbar.WrapContents = false;
            // 
            // lblMesDate
            // 
            lblMesDate.AutoSize = true;
            lblMesDate.Location = new Point(0, 15);
            lblMesDate.Margin = new Padding(0, 9, 8, 0);
            lblMesDate.Name = "lblMesDate";
            lblMesDate.Size = new Size(82, 24);
            lblMesDate.TabIndex = 0;
            lblMesDate.Text = "日志日期";
            // 
            // dtpMesDate
            // 
            dtpMesDate.CustomFormat = "yyyy-MM-dd";
            dtpMesDate.Format = DateTimePickerFormat.Custom;
            dtpMesDate.Location = new Point(90, 8);
            dtpMesDate.Margin = new Padding(0, 2, 16, 0);
            dtpMesDate.Name = "dtpMesDate";
            dtpMesDate.Size = new Size(150, 30);
            dtpMesDate.TabIndex = 1;
            // 
            // lblMesKeyword
            // 
            lblMesKeyword.AutoSize = true;
            lblMesKeyword.Location = new Point(256, 15);
            lblMesKeyword.Margin = new Padding(0, 9, 8, 0);
            lblMesKeyword.Name = "lblMesKeyword";
            lblMesKeyword.Size = new Size(64, 24);
            lblMesKeyword.TabIndex = 2;
            lblMesKeyword.Text = "关键词";
            // 
            // txtMesKeyword
            // 
            txtMesKeyword.Location = new Point(328, 8);
            txtMesKeyword.Margin = new Padding(0, 2, 16, 0);
            txtMesKeyword.Name = "txtMesKeyword";
            txtMesKeyword.PlaceholderText = "URL / MES / TraceId";
            txtMesKeyword.Size = new Size(190, 30);
            txtMesKeyword.TabIndex = 3;
            // 
            // btnRefreshMes
            // 
            btnRefreshMes.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnRefreshMes.BorderWidth = 1F;
            btnRefreshMes.IconSvg = "ReloadOutlined";
            btnRefreshMes.Location = new Point(534, 6);
            btnRefreshMes.Margin = new Padding(0, 0, 10, 0);
            btnRefreshMes.Name = "btnRefreshMes";
            btnRefreshMes.Size = new Size(89, 40);
            btnRefreshMes.TabIndex = 4;
            btnRefreshMes.Text = "刷新";
            // 
            // btnOpenMesFolder
            // 
            btnOpenMesFolder.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnOpenMesFolder.BorderWidth = 1F;
            btnOpenMesFolder.IconSvg = "FolderOpenOutlined";
            btnOpenMesFolder.Location = new Point(633, 6);
            btnOpenMesFolder.Margin = new Padding(0);
            btnOpenMesFolder.Name = "btnOpenMesFolder";
            btnOpenMesFolder.Size = new Size(118, 40);
            btnOpenMesFolder.TabIndex = 5;
            btnOpenMesFolder.Text = "打开目录";
            // 
            // splitMesContent
            // 
            splitMesContent.Dock = DockStyle.Fill;
            splitMesContent.Location = new Point(20, 92);
            splitMesContent.Margin = new Padding(20, 0, 20, 18);
            splitMesContent.Name = "splitMesContent";
            // 
            // splitMesContent.Panel1
            // 
            splitMesContent.Panel1.Controls.Add(dgvMesLogs);
            splitMesContent.Panel1.Padding = new Padding(0, 0, 12, 0);
            // 
            // splitMesContent.Panel2
            // 
            splitMesContent.Panel2.Controls.Add(tabMesDetails);
            splitMesContent.Panel2.Padding = new Padding(12, 0, 0, 0);
            splitMesContent.Size = new Size(1312, 593);
            splitMesContent.SplitterDistance = 820;
            splitMesContent.SplitterWidth = 5;
            splitMesContent.TabIndex = 1;
            // 
            // dgvMesLogs
            // 
            dgvMesLogs.AllowUserToAddRows = false;
            dgvMesLogs.AllowUserToDeleteRows = false;
            dgvMesLogs.BackgroundColor = SystemColors.Window;
            dgvMesLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvMesLogs.Dock = DockStyle.Fill;
            dgvMesLogs.Location = new Point(0, 0);
            dgvMesLogs.MultiSelect = false;
            dgvMesLogs.Name = "dgvMesLogs";
            dgvMesLogs.ReadOnly = true;
            dgvMesLogs.RowHeadersVisible = false;
            dgvMesLogs.RowHeadersWidth = 51;
            dgvMesLogs.RowTemplate.Height = 28;
            dgvMesLogs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMesLogs.Size = new Size(808, 593);
            dgvMesLogs.TabIndex = 0;
            // 
            // tabMesDetails
            // 
            tabMesDetails.Controls.Add(tabBasicInfo);
            tabMesDetails.Controls.Add(tabRequestBody);
            tabMesDetails.Controls.Add(tabResponseBody);
            tabMesDetails.Dock = DockStyle.Fill;
            tabMesDetails.Location = new Point(12, 0);
            tabMesDetails.Name = "tabMesDetails";
            tabMesDetails.SelectedIndex = 0;
            tabMesDetails.Size = new Size(475, 593);
            tabMesDetails.TabIndex = 0;
            // 
            // tabBasicInfo
            // 
            tabBasicInfo.Controls.Add(txtBasicInfo);
            tabBasicInfo.Location = new Point(4, 32);
            tabBasicInfo.Name = "tabBasicInfo";
            tabBasicInfo.Padding = new Padding(3);
            tabBasicInfo.Size = new Size(467, 557);
            tabBasicInfo.TabIndex = 0;
            tabBasicInfo.Text = "基础信息";
            tabBasicInfo.UseVisualStyleBackColor = true;
            // 
            // txtBasicInfo
            // 
            txtBasicInfo.BackColor = SystemColors.Window;
            txtBasicInfo.BorderStyle = BorderStyle.FixedSingle;
            txtBasicInfo.Dock = DockStyle.Fill;
            txtBasicInfo.Font = new Font("Consolas", 10F);
            txtBasicInfo.Location = new Point(3, 3);
            txtBasicInfo.Multiline = true;
            txtBasicInfo.Name = "txtBasicInfo";
            txtBasicInfo.ReadOnly = true;
            txtBasicInfo.ScrollBars = ScrollBars.Both;
            txtBasicInfo.Size = new Size(461, 551);
            txtBasicInfo.TabIndex = 0;
            txtBasicInfo.WordWrap = false;
            // 
            // tabRequestBody
            // 
            tabRequestBody.Controls.Add(txtRequestBody);
            tabRequestBody.Location = new Point(4, 32);
            tabRequestBody.Name = "tabRequestBody";
            tabRequestBody.Padding = new Padding(3);
            tabRequestBody.Size = new Size(467, 557);
            tabRequestBody.TabIndex = 1;
            tabRequestBody.Text = "请求报文";
            tabRequestBody.UseVisualStyleBackColor = true;
            // 
            // txtRequestBody
            // 
            txtRequestBody.BackColor = SystemColors.Window;
            txtRequestBody.BorderStyle = BorderStyle.FixedSingle;
            txtRequestBody.Dock = DockStyle.Fill;
            txtRequestBody.Font = new Font("Consolas", 10F);
            txtRequestBody.Location = new Point(3, 3);
            txtRequestBody.Multiline = true;
            txtRequestBody.Name = "txtRequestBody";
            txtRequestBody.ReadOnly = true;
            txtRequestBody.ScrollBars = ScrollBars.Both;
            txtRequestBody.Size = new Size(461, 551);
            txtRequestBody.TabIndex = 0;
            txtRequestBody.WordWrap = false;
            // 
            // tabResponseBody
            // 
            tabResponseBody.Controls.Add(txtResponseBody);
            tabResponseBody.Location = new Point(4, 32);
            tabResponseBody.Name = "tabResponseBody";
            tabResponseBody.Padding = new Padding(3);
            tabResponseBody.Size = new Size(467, 557);
            tabResponseBody.TabIndex = 2;
            tabResponseBody.Text = "响应报文";
            tabResponseBody.UseVisualStyleBackColor = true;
            // 
            // txtResponseBody
            // 
            txtResponseBody.BackColor = SystemColors.Window;
            txtResponseBody.BorderStyle = BorderStyle.FixedSingle;
            txtResponseBody.Dock = DockStyle.Fill;
            txtResponseBody.Font = new Font("Consolas", 10F);
            txtResponseBody.Location = new Point(3, 3);
            txtResponseBody.Multiline = true;
            txtResponseBody.Name = "txtResponseBody";
            txtResponseBody.ReadOnly = true;
            txtResponseBody.ScrollBars = ScrollBars.Both;
            txtResponseBody.Size = new Size(461, 551);
            txtResponseBody.TabIndex = 0;
            txtResponseBody.WordWrap = false;
            // 
            // tabProductionLogs
            // 
            tabProductionLogs.Controls.Add(lblProductionReserved);
            tabProductionLogs.Location = new Point(4, 32);
            tabProductionLogs.Name = "tabProductionLogs";
            tabProductionLogs.Padding = new Padding(3);
            tabProductionLogs.Size = new Size(1358, 709);
            tabProductionLogs.TabIndex = 1;
            tabProductionLogs.Text = "生产流程日志";
            tabProductionLogs.UseVisualStyleBackColor = true;
            // 
            // lblProductionReserved
            // 
            lblProductionReserved.Dock = DockStyle.Fill;
            lblProductionReserved.ForeColor = SystemColors.GrayText;
            lblProductionReserved.Location = new Point(3, 3);
            lblProductionReserved.Name = "lblProductionReserved";
            lblProductionReserved.Size = new Size(1352, 703);
            lblProductionReserved.TabIndex = 0;
            lblProductionReserved.Text = "当前分类已预留，后续接入对应日志服务后显示。";
            lblProductionReserved.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tabExceptionLogs
            // 
            tabExceptionLogs.Controls.Add(lblExceptionReserved);
            tabExceptionLogs.Location = new Point(4, 32);
            tabExceptionLogs.Name = "tabExceptionLogs";
            tabExceptionLogs.Padding = new Padding(3);
            tabExceptionLogs.Size = new Size(1358, 709);
            tabExceptionLogs.TabIndex = 2;
            tabExceptionLogs.Text = "程序异常日志";
            tabExceptionLogs.UseVisualStyleBackColor = true;
            // 
            // lblExceptionReserved
            // 
            lblExceptionReserved.Dock = DockStyle.Fill;
            lblExceptionReserved.ForeColor = SystemColors.GrayText;
            lblExceptionReserved.Location = new Point(3, 3);
            lblExceptionReserved.Name = "lblExceptionReserved";
            lblExceptionReserved.Size = new Size(1352, 703);
            lblExceptionReserved.TabIndex = 0;
            lblExceptionReserved.Text = "当前分类已预留，后续接入对应日志服务后显示。";
            lblExceptionReserved.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // LogManageView
            // 
            AutoScaleDimensions = new SizeF(10F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tabLogCategories);
            Font = new Font("Microsoft YaHei UI", 10.5F);
            Margin = new Padding(4, 3, 4, 3);
            Name = "LogManageView";
            Size = new Size(1366, 745);
            tabLogCategories.ResumeLayout(false);
            tabMesLogs.ResumeLayout(false);
            mesRootLayout.ResumeLayout(false);
            mesHeaderLayout.ResumeLayout(false);
            mesHeaderLayout.PerformLayout();
            mesTitleLayout.ResumeLayout(false);
            mesTitleLayout.PerformLayout();
            mesToolbar.ResumeLayout(false);
            mesToolbar.PerformLayout();
            splitMesContent.Panel1.ResumeLayout(false);
            splitMesContent.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMesContent).EndInit();
            splitMesContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvMesLogs).EndInit();
            tabMesDetails.ResumeLayout(false);
            tabBasicInfo.ResumeLayout(false);
            tabBasicInfo.PerformLayout();
            tabRequestBody.ResumeLayout(false);
            tabRequestBody.PerformLayout();
            tabResponseBody.ResumeLayout(false);
            tabResponseBody.PerformLayout();
            tabProductionLogs.ResumeLayout(false);
            tabExceptionLogs.ResumeLayout(false);
            ResumeLayout(false);
        }

        private TabControl tabLogCategories;
        private TabPage tabMesLogs;
        private TableLayoutPanel mesRootLayout;
        private TableLayoutPanel mesHeaderLayout;
        private TableLayoutPanel mesTitleLayout;
        private Label lblMesTitle;
        private Label lblMesDescription;
        private FlowLayoutPanel mesToolbar;
        private Label lblMesDate;
        private DateTimePicker dtpMesDate;
        private Label lblMesKeyword;
        private TextBox txtMesKeyword;
        private AntdUI.Button btnRefreshMes;
        private AntdUI.Button btnOpenMesFolder;
        private SplitContainer splitMesContent;
        private DataGridView dgvMesLogs;
        private TabControl tabMesDetails;
        private TabPage tabBasicInfo;
        private TextBox txtBasicInfo;
        private TabPage tabRequestBody;
        private TextBox txtRequestBody;
        private TabPage tabResponseBody;
        private TextBox txtResponseBody;
        private TabPage tabProductionLogs;
        private Label lblProductionReserved;
        private TabPage tabExceptionLogs;
        private Label lblExceptionReserved;
    }
}
