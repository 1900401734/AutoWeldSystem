namespace AutoWeldSystem.UI.Views
{
    partial class LogManageView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                if (_mesLogService is not null)
                {
                    _mesLogService.LogWritten -= MesLogService_LogWritten;
                }

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
            colMesSendTime = new DataGridViewTextBoxColumn();
            colMesPurpose = new DataGridViewTextBoxColumn();
            colMesMethod = new DataGridViewTextBoxColumn();
            colMesHttpStatus = new DataGridViewTextBoxColumn();
            colMesStatus = new DataGridViewTextBoxColumn();
            colResult = new DataGridViewTextBoxColumn();
            colMesDuration = new DataGridViewTextBoxColumn();
            tabMesDetails = new TabControl();
            tabBasicInfo = new TabPage();
            txtBasicInfo = new TextBox();
            tabRequestBody = new TabPage();
            txtRequestBody = new TextBox();
            tabResponseBody = new TabPage();
            txtResponseBody = new TextBox();
            tabProductionLogs = new TabPage();
            productionRootLayout = new TableLayoutPanel();
            productionHeaderLayout = new TableLayoutPanel();
            productionTitleLayout = new TableLayoutPanel();
            lblProductionTitle = new Label();
            lblProductionDescription = new Label();
            productionToolbar = new FlowLayoutPanel();
            lblProductionDate = new Label();
            dtpProductionDate = new DateTimePicker();
            lblProductionKeyword = new Label();
            txtProductionKeyword = new TextBox();
            btnRefreshProduction = new AntdUI.Button();
            btnOpenProductionFolder = new AntdUI.Button();
            splitProductionContent = new SplitContainer();
            dgvProductionLogs = new DataGridView();
            colProductionOccurredTime = new DataGridViewTextBoxColumn();
            colProductionLevel = new DataGridViewTextBoxColumn();
            colProductionStep = new DataGridViewTextBoxColumn();
            colProductionSummary = new DataGridViewTextBoxColumn();
            colProductionStation = new DataGridViewTextBoxColumn();
            colProductionPlcSignal = new DataGridViewTextBoxColumn();
            tabProductionDetails = new TabControl();
            tabProductionBasicInfo = new TabPage();
            txtProductionBasicInfo = new TextBox();
            tabProductionDetail = new TabPage();
            txtProductionDetail = new TextBox();
            tabExceptionLogs = new TabPage();
            exceptionRootLayout = new TableLayoutPanel();
            exceptionHeaderLayout = new TableLayoutPanel();
            exceptionTitleLayout = new TableLayoutPanel();
            lblExceptionTitle = new Label();
            lblExceptionDescription = new Label();
            exceptionToolbar = new FlowLayoutPanel();
            lblExceptionDate = new Label();
            dtpExceptionDate = new DateTimePicker();
            lblExceptionKeyword = new Label();
            txtExceptionKeyword = new TextBox();
            btnRefreshException = new AntdUI.Button();
            btnOpenExceptionFolder = new AntdUI.Button();
            splitExceptionContent = new SplitContainer();
            dgvExceptionLogs = new DataGridView();
            colExceptionOccurredTime = new DataGridViewTextBoxColumn();
            colExceptionCategory = new DataGridViewTextBoxColumn();
            colExceptionSeverity = new DataGridViewTextBoxColumn();
            colExceptionType = new DataGridViewTextBoxColumn();
            colExceptionMessage = new DataGridViewTextBoxColumn();
            colExceptionSource = new DataGridViewTextBoxColumn();
            colExceptionSourceLocation = new DataGridViewTextBoxColumn();
            exceptionDetailsLayout = new TableLayoutPanel();
            exceptionDetailToolbar = new FlowLayoutPanel();
            btnOpenExceptionSource = new AntdUI.Button();
            btnCopyExceptionDetails = new AntdUI.Button();
            tabExceptionDetails = new TabControl();
            tabExceptionBasicInfo = new TabPage();
            txtExceptionBasicInfo = new TextBox();
            tabExceptionStackTrace = new TabPage();
            txtExceptionStackTrace = new TextBox();
            tabExceptionContext = new TabPage();
            txtExceptionContext = new TextBox();
            tabDeviceStatusLogs = new TabPage();
            deviceStatusRootLayout = new TableLayoutPanel();
            deviceStatusHeaderLayout = new TableLayoutPanel();
            deviceStatusTitleLayout = new TableLayoutPanel();
            lblDeviceStatusTitle = new Label();
            lblDeviceStatusDescription = new Label();
            deviceStatusToolbar = new FlowLayoutPanel();
            lblDeviceStatusDate = new Label();
            dtpDeviceStatusDate = new DateTimePicker();
            lblDeviceStatusKeyword = new Label();
            txtDeviceStatusKeyword = new TextBox();
            btnRefreshDeviceStatus = new AntdUI.Button();
            splitDeviceStatusContent = new SplitContainer();
            dgvDeviceStatusLogs = new DataGridView();
            colDeviceOccurredTime = new DataGridViewTextBoxColumn();
            colDeviceStation = new DataGridViewTextBoxColumn();
            colDeviceStatus = new DataGridViewTextBoxColumn();
            colDeviceStatusName = new DataGridViewTextBoxColumn();
            colDeviceWorkOrder = new DataGridViewTextBoxColumn();
            colDeviceSource = new DataGridViewTextBoxColumn();
            colDeviceReportStatus = new DataGridViewTextBoxColumn();
            colDeviceReportMessage = new DataGridViewTextBoxColumn();
            txtDeviceStatusDetail = new TextBox();
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
            productionRootLayout.SuspendLayout();
            productionHeaderLayout.SuspendLayout();
            productionTitleLayout.SuspendLayout();
            productionToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitProductionContent).BeginInit();
            splitProductionContent.Panel1.SuspendLayout();
            splitProductionContent.Panel2.SuspendLayout();
            splitProductionContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvProductionLogs).BeginInit();
            tabProductionDetails.SuspendLayout();
            tabProductionBasicInfo.SuspendLayout();
            tabProductionDetail.SuspendLayout();
            tabExceptionLogs.SuspendLayout();
            exceptionRootLayout.SuspendLayout();
            exceptionHeaderLayout.SuspendLayout();
            exceptionTitleLayout.SuspendLayout();
            exceptionToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitExceptionContent).BeginInit();
            splitExceptionContent.Panel1.SuspendLayout();
            splitExceptionContent.Panel2.SuspendLayout();
            splitExceptionContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvExceptionLogs).BeginInit();
            exceptionDetailsLayout.SuspendLayout();
            exceptionDetailToolbar.SuspendLayout();
            tabExceptionDetails.SuspendLayout();
            tabExceptionBasicInfo.SuspendLayout();
            tabExceptionStackTrace.SuspendLayout();
            tabExceptionContext.SuspendLayout();
            tabDeviceStatusLogs.SuspendLayout();
            deviceStatusRootLayout.SuspendLayout();
            deviceStatusHeaderLayout.SuspendLayout();
            deviceStatusTitleLayout.SuspendLayout();
            deviceStatusToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitDeviceStatusContent).BeginInit();
            splitDeviceStatusContent.Panel1.SuspendLayout();
            splitDeviceStatusContent.Panel2.SuspendLayout();
            splitDeviceStatusContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDeviceStatusLogs).BeginInit();
            SuspendLayout();
            // 
            // tabLogCategories
            // 
            tabLogCategories.Controls.Add(tabMesLogs);
            tabLogCategories.Controls.Add(tabProductionLogs);
            tabLogCategories.Controls.Add(tabExceptionLogs);
            tabLogCategories.Controls.Add(tabDeviceStatusLogs);
            tabLogCategories.Dock = DockStyle.Fill;
            tabLogCategories.HotTrack = true;
            tabLogCategories.Location = new Point(0, 0);
            tabLogCategories.Margin = new Padding(0);
            tabLogCategories.Name = "tabLogCategories";
            tabLogCategories.Padding = new Point(0, 0);
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
            tabMesLogs.Text = "MES Interaction";
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
            mesHeaderLayout.Size = new Size(1312, 100);
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
            mesTitleLayout.Size = new Size(500, 100);
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
            lblMesTitle.Size = new Size(500, 34);
            lblMesTitle.TabIndex = 0;
            lblMesTitle.Text = "MES Interaction";
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
            lblMesDescription.Size = new Size(500, 66);
            lblMesDescription.TabIndex = 1;
            lblMesDescription.Text = "MES Interaction details";
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
            mesToolbar.Dock = DockStyle.Top;
            mesToolbar.Location = new Point(500, 0);
            mesToolbar.Margin = new Padding(0);
            mesToolbar.Name = "mesToolbar";
            mesToolbar.Padding = new Padding(0, 6, 0, 0);
            mesToolbar.Size = new Size(812, 52);
            mesToolbar.TabIndex = 1;
            mesToolbar.WrapContents = false;
            // 
            // lblMesDate
            // 
            lblMesDate.AutoSize = true;
            lblMesDate.Location = new Point(0, 15);
            lblMesDate.Margin = new Padding(0, 9, 8, 0);
            lblMesDate.Name = "lblMesDate";
            lblMesDate.Size = new Size(51, 24);
            lblMesDate.TabIndex = 0;
            lblMesDate.Text = "Date";
            // 
            // dtpMesDate
            // 
            dtpMesDate.CustomFormat = "yyyy-MM-dd";
            dtpMesDate.Format = DateTimePickerFormat.Custom;
            dtpMesDate.Location = new Point(59, 8);
            dtpMesDate.Margin = new Padding(0, 2, 16, 0);
            dtpMesDate.Name = "dtpMesDate";
            dtpMesDate.Size = new Size(150, 30);
            dtpMesDate.TabIndex = 1;
            // 
            // lblMesKeyword
            // 
            lblMesKeyword.AutoSize = true;
            lblMesKeyword.Location = new Point(225, 15);
            lblMesKeyword.Margin = new Padding(0, 9, 8, 0);
            lblMesKeyword.Name = "lblMesKeyword";
            lblMesKeyword.Size = new Size(85, 24);
            lblMesKeyword.TabIndex = 2;
            lblMesKeyword.Text = "Keyword";
            // 
            // txtMesKeyword
            // 
            txtMesKeyword.Location = new Point(318, 8);
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
            btnRefreshMes.Location = new Point(527, 9);
            btnRefreshMes.Name = "btnRefreshMes";
            btnRefreshMes.Size = new Size(117, 40);
            btnRefreshMes.TabIndex = 4;
            btnRefreshMes.Tag = "perm:button.log.refresh:visible";
            btnRefreshMes.Text = "Refresh";
            // 
            // btnOpenMesFolder
            // 
            btnOpenMesFolder.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnOpenMesFolder.BorderWidth = 1F;
            btnOpenMesFolder.IconSvg = "FolderOpenOutlined";
            btnOpenMesFolder.Location = new Point(650, 9);
            btnOpenMesFolder.Name = "btnOpenMesFolder";
            btnOpenMesFolder.Size = new Size(159, 40);
            btnOpenMesFolder.TabIndex = 5;
            btnOpenMesFolder.Tag = "perm:button.log.open-folder:visible";
            btnOpenMesFolder.Text = "Open Folder";
            // 
            // splitMesContent
            // 
            splitMesContent.Dock = DockStyle.Fill;
            splitMesContent.Location = new Point(20, 122);
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
            splitMesContent.Size = new Size(1312, 563);
            splitMesContent.SplitterDistance = 885;
            splitMesContent.SplitterWidth = 5;
            splitMesContent.TabIndex = 1;
            // 
            // dgvMesLogs
            // 
            dgvMesLogs.AllowUserToAddRows = false;
            dgvMesLogs.AllowUserToDeleteRows = false;
            dgvMesLogs.BackgroundColor = SystemColors.Window;
            dgvMesLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvMesLogs.Columns.AddRange(new DataGridViewColumn[] { colMesSendTime, colMesPurpose, colMesMethod, colMesHttpStatus, colMesStatus, colResult, colMesDuration });
            dgvMesLogs.Dock = DockStyle.Fill;
            dgvMesLogs.Location = new Point(0, 0);
            dgvMesLogs.MultiSelect = false;
            dgvMesLogs.Name = "dgvMesLogs";
            dgvMesLogs.ReadOnly = true;
            dgvMesLogs.RowHeadersVisible = false;
            dgvMesLogs.RowHeadersWidth = 51;
            dgvMesLogs.RowTemplate.Height = 28;
            dgvMesLogs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMesLogs.Size = new Size(873, 563);
            dgvMesLogs.TabIndex = 0;
            // 
            // colMesSendTime
            // 
            colMesSendTime.DataPropertyName = "SendTime";
            colMesSendTime.FillWeight = 18F;
            colMesSendTime.HeaderText = "Send Time";
            colMesSendTime.MinimumWidth = 88;
            colMesSendTime.Name = "colMesSendTime";
            colMesSendTime.ReadOnly = true;
            colMesSendTime.Width = 125;
            // 
            // colMesPurpose
            // 
            colMesPurpose.DataPropertyName = "Purpose";
            colMesPurpose.FillWeight = 18F;
            colMesPurpose.HeaderText = "Purpose";
            colMesPurpose.MinimumWidth = 88;
            colMesPurpose.Name = "colMesPurpose";
            colMesPurpose.ReadOnly = true;
            colMesPurpose.Width = 125;
            // 
            // colMesMethod
            // 
            colMesMethod.DataPropertyName = "Method";
            colMesMethod.FillWeight = 9F;
            colMesMethod.HeaderText = "Method";
            colMesMethod.MinimumWidth = 88;
            colMesMethod.Name = "colMesMethod";
            colMesMethod.ReadOnly = true;
            colMesMethod.Width = 125;
            // 
            // colMesHttpStatus
            // 
            colMesHttpStatus.DataPropertyName = "HttpStatus";
            colMesHttpStatus.FillWeight = 9F;
            colMesHttpStatus.HeaderText = "HTTP";
            colMesHttpStatus.MinimumWidth = 88;
            colMesHttpStatus.Name = "colMesHttpStatus";
            colMesHttpStatus.ReadOnly = true;
            colMesHttpStatus.Width = 125;
            // 
            // colMesStatus
            // 
            colMesStatus.DataPropertyName = "MesStatus";
            colMesStatus.FillWeight = 8F;
            colMesStatus.HeaderText = "MES Status";
            colMesStatus.MinimumWidth = 88;
            colMesStatus.Name = "colMesStatus";
            colMesStatus.ReadOnly = true;
            colMesStatus.Width = 125;
            // 
            // colResult
            // 
            colResult.DataPropertyName = "Result";
            colResult.FillWeight = 10F;
            colResult.HeaderText = "Result";
            colResult.MinimumWidth = 88;
            colResult.Name = "colResult";
            colResult.ReadOnly = true;
            colResult.Width = 125;
            // 
            // colMesDuration
            // 
            colMesDuration.DataPropertyName = "Duration";
            colMesDuration.FillWeight = 10F;
            colMesDuration.HeaderText = "Duration";
            colMesDuration.MinimumWidth = 88;
            colMesDuration.Name = "colMesDuration";
            colMesDuration.ReadOnly = true;
            colMesDuration.Width = 125;
            // 
            // tabMesDetails
            // 
            tabMesDetails.Controls.Add(tabBasicInfo);
            tabMesDetails.Controls.Add(tabRequestBody);
            tabMesDetails.Controls.Add(tabResponseBody);
            tabMesDetails.Dock = DockStyle.Fill;
            tabMesDetails.Location = new Point(12, 0);
            tabMesDetails.Margin = new Padding(0);
            tabMesDetails.Name = "tabMesDetails";
            tabMesDetails.Padding = new Point(0, 0);
            tabMesDetails.SelectedIndex = 0;
            tabMesDetails.Size = new Size(410, 563);
            tabMesDetails.TabIndex = 0;
            // 
            // tabBasicInfo
            // 
            tabBasicInfo.Controls.Add(txtBasicInfo);
            tabBasicInfo.Location = new Point(4, 32);
            tabBasicInfo.Name = "tabBasicInfo";
            tabBasicInfo.Padding = new Padding(3);
            tabBasicInfo.Size = new Size(402, 527);
            tabBasicInfo.TabIndex = 0;
            tabBasicInfo.Text = "Basic";
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
            txtBasicInfo.Size = new Size(396, 521);
            txtBasicInfo.TabIndex = 0;
            txtBasicInfo.WordWrap = false;
            // 
            // tabRequestBody
            // 
            tabRequestBody.Controls.Add(txtRequestBody);
            tabRequestBody.Location = new Point(4, 29);
            tabRequestBody.Name = "tabRequestBody";
            tabRequestBody.Padding = new Padding(3);
            tabRequestBody.Size = new Size(402, 530);
            tabRequestBody.TabIndex = 1;
            tabRequestBody.Text = "Request";
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
            txtRequestBody.Size = new Size(396, 524);
            txtRequestBody.TabIndex = 0;
            txtRequestBody.WordWrap = false;
            // 
            // tabResponseBody
            // 
            tabResponseBody.Controls.Add(txtResponseBody);
            tabResponseBody.Location = new Point(4, 29);
            tabResponseBody.Name = "tabResponseBody";
            tabResponseBody.Padding = new Padding(3);
            tabResponseBody.Size = new Size(402, 530);
            tabResponseBody.TabIndex = 2;
            tabResponseBody.Text = "Response";
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
            txtResponseBody.Size = new Size(396, 524);
            txtResponseBody.TabIndex = 0;
            txtResponseBody.WordWrap = false;
            // 
            // tabProductionLogs
            // 
            tabProductionLogs.Controls.Add(productionRootLayout);
            tabProductionLogs.Location = new Point(4, 32);
            tabProductionLogs.Name = "tabProductionLogs";
            tabProductionLogs.Padding = new Padding(3);
            tabProductionLogs.Size = new Size(1358, 709);
            tabProductionLogs.TabIndex = 1;
            tabProductionLogs.Text = "Production Flow";
            tabProductionLogs.UseVisualStyleBackColor = true;
            // 
            // productionRootLayout
            // 
            productionRootLayout.ColumnCount = 1;
            productionRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            productionRootLayout.Controls.Add(productionHeaderLayout, 0, 0);
            productionRootLayout.Controls.Add(splitProductionContent, 0, 1);
            productionRootLayout.Dock = DockStyle.Fill;
            productionRootLayout.Location = new Point(3, 3);
            productionRootLayout.Name = "productionRootLayout";
            productionRootLayout.RowCount = 2;
            productionRootLayout.RowStyles.Add(new RowStyle());
            productionRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            productionRootLayout.Size = new Size(1352, 703);
            productionRootLayout.TabIndex = 0;
            // 
            // productionHeaderLayout
            // 
            productionHeaderLayout.ColumnCount = 2;
            productionHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            productionHeaderLayout.ColumnStyles.Add(new ColumnStyle());
            productionHeaderLayout.Controls.Add(productionTitleLayout, 0, 0);
            productionHeaderLayout.Controls.Add(productionToolbar, 1, 0);
            productionHeaderLayout.Dock = DockStyle.Fill;
            productionHeaderLayout.Location = new Point(20, 14);
            productionHeaderLayout.Margin = new Padding(20, 14, 20, 8);
            productionHeaderLayout.Name = "productionHeaderLayout";
            productionHeaderLayout.RowCount = 1;
            productionHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            productionHeaderLayout.Size = new Size(1312, 100);
            productionHeaderLayout.TabIndex = 0;
            // 
            // productionTitleLayout
            // 
            productionTitleLayout.ColumnCount = 1;
            productionTitleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            productionTitleLayout.Controls.Add(lblProductionTitle, 0, 0);
            productionTitleLayout.Controls.Add(lblProductionDescription, 0, 1);
            productionTitleLayout.Dock = DockStyle.Fill;
            productionTitleLayout.Location = new Point(0, 0);
            productionTitleLayout.Margin = new Padding(0);
            productionTitleLayout.Name = "productionTitleLayout";
            productionTitleLayout.RowCount = 2;
            productionTitleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            productionTitleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            productionTitleLayout.Size = new Size(460, 100);
            productionTitleLayout.TabIndex = 0;
            // 
            // lblProductionTitle
            // 
            lblProductionTitle.AutoSize = true;
            lblProductionTitle.Dock = DockStyle.Fill;
            lblProductionTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            lblProductionTitle.Location = new Point(0, 0);
            lblProductionTitle.Margin = new Padding(0);
            lblProductionTitle.Name = "lblProductionTitle";
            lblProductionTitle.Size = new Size(460, 34);
            lblProductionTitle.TabIndex = 0;
            lblProductionTitle.Text = "Production Flow";
            lblProductionTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblProductionDescription
            // 
            lblProductionDescription.AutoEllipsis = true;
            lblProductionDescription.Dock = DockStyle.Fill;
            lblProductionDescription.ForeColor = SystemColors.GrayText;
            lblProductionDescription.Location = new Point(0, 34);
            lblProductionDescription.Margin = new Padding(0);
            lblProductionDescription.Name = "lblProductionDescription";
            lblProductionDescription.Size = new Size(460, 66);
            lblProductionDescription.TabIndex = 1;
            lblProductionDescription.Text = "Production Flow details";
            lblProductionDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // productionToolbar
            // 
            productionToolbar.AutoSize = true;
            productionToolbar.Controls.Add(lblProductionDate);
            productionToolbar.Controls.Add(dtpProductionDate);
            productionToolbar.Controls.Add(lblProductionKeyword);
            productionToolbar.Controls.Add(txtProductionKeyword);
            productionToolbar.Controls.Add(btnRefreshProduction);
            productionToolbar.Controls.Add(btnOpenProductionFolder);
            productionToolbar.Dock = DockStyle.Right;
            productionToolbar.Location = new Point(460, 0);
            productionToolbar.Margin = new Padding(0);
            productionToolbar.Name = "productionToolbar";
            productionToolbar.Padding = new Padding(0, 6, 0, 0);
            productionToolbar.Size = new Size(852, 100);
            productionToolbar.TabIndex = 1;
            productionToolbar.WrapContents = false;
            // 
            // lblProductionDate
            // 
            lblProductionDate.AutoSize = true;
            lblProductionDate.Location = new Point(0, 15);
            lblProductionDate.Margin = new Padding(0, 9, 8, 0);
            lblProductionDate.Name = "lblProductionDate";
            lblProductionDate.Size = new Size(51, 24);
            lblProductionDate.TabIndex = 0;
            lblProductionDate.Text = "Date";
            // 
            // dtpProductionDate
            // 
            dtpProductionDate.CustomFormat = "yyyy-MM-dd";
            dtpProductionDate.Format = DateTimePickerFormat.Custom;
            dtpProductionDate.Location = new Point(59, 8);
            dtpProductionDate.Margin = new Padding(0, 2, 16, 0);
            dtpProductionDate.Name = "dtpProductionDate";
            dtpProductionDate.Size = new Size(150, 30);
            dtpProductionDate.TabIndex = 1;
            // 
            // lblProductionKeyword
            // 
            lblProductionKeyword.AutoSize = true;
            lblProductionKeyword.Location = new Point(225, 15);
            lblProductionKeyword.Margin = new Padding(0, 9, 8, 0);
            lblProductionKeyword.Name = "lblProductionKeyword";
            lblProductionKeyword.Size = new Size(85, 24);
            lblProductionKeyword.TabIndex = 2;
            lblProductionKeyword.Text = "Keyword";
            // 
            // txtProductionKeyword
            // 
            txtProductionKeyword.Location = new Point(318, 8);
            txtProductionKeyword.Margin = new Padding(0, 2, 16, 0);
            txtProductionKeyword.Name = "txtProductionKeyword";
            txtProductionKeyword.PlaceholderText = "Step / WorkOrder / ProductNumber / PLC";
            txtProductionKeyword.Size = new Size(230, 30);
            txtProductionKeyword.TabIndex = 3;
            // 
            // btnRefreshProduction
            // 
            btnRefreshProduction.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnRefreshProduction.BorderWidth = 1F;
            btnRefreshProduction.IconSvg = "ReloadOutlined";
            btnRefreshProduction.Location = new Point(567, 9);
            btnRefreshProduction.Name = "btnRefreshProduction";
            btnRefreshProduction.Size = new Size(117, 40);
            btnRefreshProduction.TabIndex = 4;
            btnRefreshProduction.Tag = "perm:button.log.refresh:visible";
            btnRefreshProduction.Text = "Refresh";
            // 
            // btnOpenProductionFolder
            // 
            btnOpenProductionFolder.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnOpenProductionFolder.BorderWidth = 1F;
            btnOpenProductionFolder.IconSvg = "FolderOpenOutlined";
            btnOpenProductionFolder.Location = new Point(690, 9);
            btnOpenProductionFolder.Name = "btnOpenProductionFolder";
            btnOpenProductionFolder.Size = new Size(159, 40);
            btnOpenProductionFolder.TabIndex = 5;
            btnOpenProductionFolder.Tag = "perm:button.log.open-folder:visible";
            btnOpenProductionFolder.Text = "Open Folder";
            // 
            // splitProductionContent
            // 
            splitProductionContent.Dock = DockStyle.Fill;
            splitProductionContent.Location = new Point(20, 122);
            splitProductionContent.Margin = new Padding(20, 0, 20, 18);
            splitProductionContent.Name = "splitProductionContent";
            // 
            // splitProductionContent.Panel1
            // 
            splitProductionContent.Panel1.Controls.Add(dgvProductionLogs);
            splitProductionContent.Panel1.Padding = new Padding(0, 0, 12, 0);
            // 
            // splitProductionContent.Panel2
            // 
            splitProductionContent.Panel2.Controls.Add(tabProductionDetails);
            splitProductionContent.Panel2.Padding = new Padding(12, 0, 0, 0);
            splitProductionContent.Size = new Size(1312, 563);
            splitProductionContent.SplitterDistance = 820;
            splitProductionContent.SplitterWidth = 5;
            splitProductionContent.TabIndex = 1;
            // 
            // dgvProductionLogs
            // 
            dgvProductionLogs.AllowUserToAddRows = false;
            dgvProductionLogs.AllowUserToDeleteRows = false;
            dgvProductionLogs.BackgroundColor = SystemColors.Window;
            dgvProductionLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvProductionLogs.Columns.AddRange(new DataGridViewColumn[] { colProductionOccurredTime, colProductionLevel, colProductionStep, colProductionSummary, colProductionStation, colProductionPlcSignal });
            dgvProductionLogs.Dock = DockStyle.Fill;
            dgvProductionLogs.Location = new Point(0, 0);
            dgvProductionLogs.MultiSelect = false;
            dgvProductionLogs.Name = "dgvProductionLogs";
            dgvProductionLogs.ReadOnly = true;
            dgvProductionLogs.RowHeadersVisible = false;
            dgvProductionLogs.RowHeadersWidth = 51;
            dgvProductionLogs.RowTemplate.Height = 28;
            dgvProductionLogs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProductionLogs.Size = new Size(808, 563);
            dgvProductionLogs.TabIndex = 0;
            // 
            // colProductionOccurredTime
            // 
            colProductionOccurredTime.DataPropertyName = "OccurredTime";
            colProductionOccurredTime.FillWeight = 14F;
            colProductionOccurredTime.HeaderText = "Time";
            colProductionOccurredTime.MinimumWidth = 88;
            colProductionOccurredTime.Name = "colProductionOccurredTime";
            colProductionOccurredTime.ReadOnly = true;
            colProductionOccurredTime.Width = 125;
            // 
            // colProductionLevel
            // 
            colProductionLevel.DataPropertyName = "Level";
            colProductionLevel.FillWeight = 8F;
            colProductionLevel.HeaderText = "Level";
            colProductionLevel.MinimumWidth = 88;
            colProductionLevel.Name = "colProductionLevel";
            colProductionLevel.ReadOnly = true;
            colProductionLevel.Width = 125;
            // 
            // colProductionStep
            // 
            colProductionStep.DataPropertyName = "Step";
            colProductionStep.FillWeight = 16F;
            colProductionStep.HeaderText = "Step";
            colProductionStep.MinimumWidth = 88;
            colProductionStep.Name = "colProductionStep";
            colProductionStep.ReadOnly = true;
            colProductionStep.Width = 125;
            // 
            // colProductionSummary
            // 
            colProductionSummary.DataPropertyName = "Summary";
            colProductionSummary.FillWeight = 28F;
            colProductionSummary.HeaderText = "Summary";
            colProductionSummary.MinimumWidth = 88;
            colProductionSummary.Name = "colProductionSummary";
            colProductionSummary.ReadOnly = true;
            colProductionSummary.Width = 125;
            // 
            // colProductionStation
            // 
            colProductionStation.DataPropertyName = "Station";
            colProductionStation.FillWeight = 8F;
            colProductionStation.HeaderText = "Station";
            colProductionStation.MinimumWidth = 88;
            colProductionStation.Name = "colProductionStation";
            colProductionStation.ReadOnly = true;
            colProductionStation.Width = 125;
            // 
            // colProductionPlcSignal
            // 
            colProductionPlcSignal.DataPropertyName = "PlcSignal";
            colProductionPlcSignal.FillWeight = 13F;
            colProductionPlcSignal.HeaderText = "PLC Signal";
            colProductionPlcSignal.MinimumWidth = 88;
            colProductionPlcSignal.Name = "colProductionPlcSignal";
            colProductionPlcSignal.ReadOnly = true;
            colProductionPlcSignal.Width = 125;
            // 
            // tabProductionDetails
            // 
            tabProductionDetails.Controls.Add(tabProductionBasicInfo);
            tabProductionDetails.Controls.Add(tabProductionDetail);
            tabProductionDetails.Dock = DockStyle.Fill;
            tabProductionDetails.Location = new Point(12, 0);
            tabProductionDetails.Name = "tabProductionDetails";
            tabProductionDetails.SelectedIndex = 0;
            tabProductionDetails.Size = new Size(475, 563);
            tabProductionDetails.TabIndex = 0;
            // 
            // tabProductionBasicInfo
            // 
            tabProductionBasicInfo.Controls.Add(txtProductionBasicInfo);
            tabProductionBasicInfo.Location = new Point(4, 32);
            tabProductionBasicInfo.Name = "tabProductionBasicInfo";
            tabProductionBasicInfo.Padding = new Padding(3);
            tabProductionBasicInfo.Size = new Size(467, 527);
            tabProductionBasicInfo.TabIndex = 0;
            tabProductionBasicInfo.Text = "Basic";
            tabProductionBasicInfo.UseVisualStyleBackColor = true;
            // 
            // txtProductionBasicInfo
            // 
            txtProductionBasicInfo.BackColor = SystemColors.Window;
            txtProductionBasicInfo.BorderStyle = BorderStyle.FixedSingle;
            txtProductionBasicInfo.Dock = DockStyle.Fill;
            txtProductionBasicInfo.Font = new Font("Consolas", 10F);
            txtProductionBasicInfo.Location = new Point(3, 3);
            txtProductionBasicInfo.Multiline = true;
            txtProductionBasicInfo.Name = "txtProductionBasicInfo";
            txtProductionBasicInfo.ReadOnly = true;
            txtProductionBasicInfo.ScrollBars = ScrollBars.Both;
            txtProductionBasicInfo.Size = new Size(461, 521);
            txtProductionBasicInfo.TabIndex = 0;
            txtProductionBasicInfo.WordWrap = false;
            // 
            // tabProductionDetail
            // 
            tabProductionDetail.Controls.Add(txtProductionDetail);
            tabProductionDetail.Location = new Point(4, 29);
            tabProductionDetail.Name = "tabProductionDetail";
            tabProductionDetail.Padding = new Padding(3);
            tabProductionDetail.Size = new Size(467, 530);
            tabProductionDetail.TabIndex = 1;
            tabProductionDetail.Text = "Detail";
            tabProductionDetail.UseVisualStyleBackColor = true;
            // 
            // txtProductionDetail
            // 
            txtProductionDetail.BackColor = SystemColors.Window;
            txtProductionDetail.BorderStyle = BorderStyle.FixedSingle;
            txtProductionDetail.Dock = DockStyle.Fill;
            txtProductionDetail.Font = new Font("Consolas", 10F);
            txtProductionDetail.Location = new Point(3, 3);
            txtProductionDetail.Multiline = true;
            txtProductionDetail.Name = "txtProductionDetail";
            txtProductionDetail.ReadOnly = true;
            txtProductionDetail.ScrollBars = ScrollBars.Both;
            txtProductionDetail.Size = new Size(461, 524);
            txtProductionDetail.TabIndex = 0;
            txtProductionDetail.WordWrap = false;
            // 
            // tabExceptionLogs
            // 
            tabExceptionLogs.Controls.Add(exceptionRootLayout);
            tabExceptionLogs.Location = new Point(4, 29);
            tabExceptionLogs.Name = "tabExceptionLogs";
            tabExceptionLogs.Padding = new Padding(3);
            tabExceptionLogs.Size = new Size(1358, 712);
            tabExceptionLogs.TabIndex = 2;
            tabExceptionLogs.Text = "Program Exceptions";
            tabExceptionLogs.UseVisualStyleBackColor = true;
            // 
            // exceptionRootLayout
            // 
            exceptionRootLayout.ColumnCount = 1;
            exceptionRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            exceptionRootLayout.Controls.Add(exceptionHeaderLayout, 0, 0);
            exceptionRootLayout.Controls.Add(splitExceptionContent, 0, 1);
            exceptionRootLayout.Dock = DockStyle.Fill;
            exceptionRootLayout.Location = new Point(3, 3);
            exceptionRootLayout.Name = "exceptionRootLayout";
            exceptionRootLayout.RowCount = 2;
            exceptionRootLayout.RowStyles.Add(new RowStyle());
            exceptionRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            exceptionRootLayout.Size = new Size(1352, 706);
            exceptionRootLayout.TabIndex = 0;
            // 
            // exceptionHeaderLayout
            // 
            exceptionHeaderLayout.ColumnCount = 2;
            exceptionHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            exceptionHeaderLayout.ColumnStyles.Add(new ColumnStyle());
            exceptionHeaderLayout.Controls.Add(exceptionTitleLayout, 0, 0);
            exceptionHeaderLayout.Controls.Add(exceptionToolbar, 1, 0);
            exceptionHeaderLayout.Dock = DockStyle.Fill;
            exceptionHeaderLayout.Location = new Point(20, 14);
            exceptionHeaderLayout.Margin = new Padding(20, 14, 20, 8);
            exceptionHeaderLayout.Name = "exceptionHeaderLayout";
            exceptionHeaderLayout.RowCount = 1;
            exceptionHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            exceptionHeaderLayout.Size = new Size(1312, 100);
            exceptionHeaderLayout.TabIndex = 0;
            // 
            // exceptionTitleLayout
            // 
            exceptionTitleLayout.ColumnCount = 1;
            exceptionTitleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            exceptionTitleLayout.Controls.Add(lblExceptionTitle, 0, 0);
            exceptionTitleLayout.Controls.Add(lblExceptionDescription, 0, 1);
            exceptionTitleLayout.Dock = DockStyle.Fill;
            exceptionTitleLayout.Location = new Point(0, 0);
            exceptionTitleLayout.Margin = new Padding(0);
            exceptionTitleLayout.Name = "exceptionTitleLayout";
            exceptionTitleLayout.RowCount = 2;
            exceptionTitleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            exceptionTitleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            exceptionTitleLayout.Size = new Size(500, 100);
            exceptionTitleLayout.TabIndex = 0;
            // 
            // lblExceptionTitle
            // 
            lblExceptionTitle.AutoSize = true;
            lblExceptionTitle.Dock = DockStyle.Fill;
            lblExceptionTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            lblExceptionTitle.Location = new Point(0, 0);
            lblExceptionTitle.Margin = new Padding(0);
            lblExceptionTitle.Name = "lblExceptionTitle";
            lblExceptionTitle.Size = new Size(500, 34);
            lblExceptionTitle.TabIndex = 0;
            lblExceptionTitle.Text = "Program Exceptions";
            lblExceptionTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblExceptionDescription
            // 
            lblExceptionDescription.AutoEllipsis = true;
            lblExceptionDescription.Dock = DockStyle.Fill;
            lblExceptionDescription.ForeColor = SystemColors.GrayText;
            lblExceptionDescription.Location = new Point(0, 34);
            lblExceptionDescription.Margin = new Padding(0);
            lblExceptionDescription.Name = "lblExceptionDescription";
            lblExceptionDescription.Size = new Size(500, 66);
            lblExceptionDescription.TabIndex = 1;
            lblExceptionDescription.Text = "Program Exceptions details";
            lblExceptionDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // exceptionToolbar
            // 
            exceptionToolbar.AutoSize = true;
            exceptionToolbar.Controls.Add(lblExceptionDate);
            exceptionToolbar.Controls.Add(dtpExceptionDate);
            exceptionToolbar.Controls.Add(lblExceptionKeyword);
            exceptionToolbar.Controls.Add(txtExceptionKeyword);
            exceptionToolbar.Controls.Add(btnRefreshException);
            exceptionToolbar.Controls.Add(btnOpenExceptionFolder);
            exceptionToolbar.Dock = DockStyle.Right;
            exceptionToolbar.Location = new Point(500, 0);
            exceptionToolbar.Margin = new Padding(0);
            exceptionToolbar.Name = "exceptionToolbar";
            exceptionToolbar.Padding = new Padding(0, 6, 0, 0);
            exceptionToolbar.Size = new Size(812, 100);
            exceptionToolbar.TabIndex = 1;
            exceptionToolbar.WrapContents = false;
            // 
            // lblExceptionDate
            // 
            lblExceptionDate.AutoSize = true;
            lblExceptionDate.Location = new Point(0, 15);
            lblExceptionDate.Margin = new Padding(0, 9, 8, 0);
            lblExceptionDate.Name = "lblExceptionDate";
            lblExceptionDate.Size = new Size(51, 24);
            lblExceptionDate.TabIndex = 0;
            lblExceptionDate.Text = "Date";
            // 
            // dtpExceptionDate
            // 
            dtpExceptionDate.CustomFormat = "yyyy-MM-dd";
            dtpExceptionDate.Format = DateTimePickerFormat.Custom;
            dtpExceptionDate.Location = new Point(59, 8);
            dtpExceptionDate.Margin = new Padding(0, 2, 16, 0);
            dtpExceptionDate.Name = "dtpExceptionDate";
            dtpExceptionDate.Size = new Size(150, 30);
            dtpExceptionDate.TabIndex = 1;
            // 
            // lblExceptionKeyword
            // 
            lblExceptionKeyword.AutoSize = true;
            lblExceptionKeyword.Location = new Point(225, 15);
            lblExceptionKeyword.Margin = new Padding(0, 9, 8, 0);
            lblExceptionKeyword.Name = "lblExceptionKeyword";
            lblExceptionKeyword.Size = new Size(85, 24);
            lblExceptionKeyword.TabIndex = 2;
            lblExceptionKeyword.Text = "Keyword";
            // 
            // txtExceptionKeyword
            // 
            txtExceptionKeyword.Location = new Point(318, 8);
            txtExceptionKeyword.Margin = new Padding(0, 2, 16, 0);
            txtExceptionKeyword.Name = "txtExceptionKeyword";
            txtExceptionKeyword.PlaceholderText = "Type / Message / Source";
            txtExceptionKeyword.Size = new Size(190, 30);
            txtExceptionKeyword.TabIndex = 3;
            // 
            // btnRefreshException
            // 
            btnRefreshException.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnRefreshException.BorderWidth = 1F;
            btnRefreshException.IconSvg = "ReloadOutlined";
            btnRefreshException.Location = new Point(527, 9);
            btnRefreshException.Name = "btnRefreshException";
            btnRefreshException.Size = new Size(117, 40);
            btnRefreshException.TabIndex = 4;
            btnRefreshException.Tag = "perm:button.log.refresh:visible";
            btnRefreshException.Text = "Refresh";
            // 
            // btnOpenExceptionFolder
            // 
            btnOpenExceptionFolder.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnOpenExceptionFolder.BorderWidth = 1F;
            btnOpenExceptionFolder.IconSvg = "FolderOpenOutlined";
            btnOpenExceptionFolder.Location = new Point(650, 9);
            btnOpenExceptionFolder.Name = "btnOpenExceptionFolder";
            btnOpenExceptionFolder.Size = new Size(159, 40);
            btnOpenExceptionFolder.TabIndex = 5;
            btnOpenExceptionFolder.Tag = "perm:button.log.open-folder:visible";
            btnOpenExceptionFolder.Text = "Open Folder";
            // 
            // splitExceptionContent
            // 
            splitExceptionContent.Dock = DockStyle.Fill;
            splitExceptionContent.Location = new Point(20, 122);
            splitExceptionContent.Margin = new Padding(20, 0, 20, 18);
            splitExceptionContent.Name = "splitExceptionContent";
            // 
            // splitExceptionContent.Panel1
            // 
            splitExceptionContent.Panel1.Controls.Add(dgvExceptionLogs);
            splitExceptionContent.Panel1.Padding = new Padding(0, 0, 12, 0);
            // 
            // splitExceptionContent.Panel2
            // 
            splitExceptionContent.Panel2.Controls.Add(exceptionDetailsLayout);
            splitExceptionContent.Panel2.Padding = new Padding(12, 0, 0, 0);
            splitExceptionContent.Size = new Size(1312, 566);
            splitExceptionContent.SplitterDistance = 760;
            splitExceptionContent.SplitterWidth = 5;
            splitExceptionContent.TabIndex = 1;
            // 
            // dgvExceptionLogs
            // 
            dgvExceptionLogs.AllowUserToAddRows = false;
            dgvExceptionLogs.AllowUserToDeleteRows = false;
            dgvExceptionLogs.BackgroundColor = SystemColors.Window;
            dgvExceptionLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvExceptionLogs.Columns.AddRange(new DataGridViewColumn[] { colExceptionOccurredTime, colExceptionCategory, colExceptionSeverity, colExceptionType, colExceptionMessage, colExceptionSource, colExceptionSourceLocation });
            dgvExceptionLogs.Dock = DockStyle.Fill;
            dgvExceptionLogs.Location = new Point(0, 0);
            dgvExceptionLogs.MultiSelect = false;
            dgvExceptionLogs.Name = "dgvExceptionLogs";
            dgvExceptionLogs.ReadOnly = true;
            dgvExceptionLogs.RowHeadersVisible = false;
            dgvExceptionLogs.RowHeadersWidth = 51;
            dgvExceptionLogs.RowTemplate.Height = 28;
            dgvExceptionLogs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvExceptionLogs.Size = new Size(748, 566);
            dgvExceptionLogs.TabIndex = 0;
            // 
            // colExceptionOccurredTime
            // 
            colExceptionOccurredTime.DataPropertyName = "OccurredTime";
            colExceptionOccurredTime.FillWeight = 15F;
            colExceptionOccurredTime.HeaderText = "Time";
            colExceptionOccurredTime.MinimumWidth = 88;
            colExceptionOccurredTime.Name = "colExceptionOccurredTime";
            colExceptionOccurredTime.ReadOnly = true;
            colExceptionOccurredTime.Width = 125;
            // 
            // colExceptionCategory
            // 
            colExceptionCategory.DataPropertyName = "Category";
            colExceptionCategory.FillWeight = 10F;
            colExceptionCategory.HeaderText = "Category";
            colExceptionCategory.MinimumWidth = 88;
            colExceptionCategory.Name = "colExceptionCategory";
            colExceptionCategory.ReadOnly = true;
            colExceptionCategory.Width = 125;
            // 
            // colExceptionSeverity
            // 
            colExceptionSeverity.DataPropertyName = "Severity";
            colExceptionSeverity.FillWeight = 10F;
            colExceptionSeverity.HeaderText = "Severity";
            colExceptionSeverity.MinimumWidth = 88;
            colExceptionSeverity.Name = "colExceptionSeverity";
            colExceptionSeverity.ReadOnly = true;
            colExceptionSeverity.Width = 125;
            // 
            // colExceptionType
            // 
            colExceptionType.DataPropertyName = "ExceptionType";
            colExceptionType.FillWeight = 16F;
            colExceptionType.HeaderText = "Exception Type";
            colExceptionType.MinimumWidth = 88;
            colExceptionType.Name = "colExceptionType";
            colExceptionType.ReadOnly = true;
            colExceptionType.Width = 125;
            // 
            // colExceptionMessage
            // 
            colExceptionMessage.DataPropertyName = "Message";
            colExceptionMessage.FillWeight = 32F;
            colExceptionMessage.HeaderText = "Message";
            colExceptionMessage.MinimumWidth = 88;
            colExceptionMessage.Name = "colExceptionMessage";
            colExceptionMessage.ReadOnly = true;
            colExceptionMessage.Width = 125;
            // 
            // colExceptionSource
            // 
            colExceptionSource.DataPropertyName = "Source";
            colExceptionSource.FillWeight = 16F;
            colExceptionSource.HeaderText = "Source";
            colExceptionSource.MinimumWidth = 88;
            colExceptionSource.Name = "colExceptionSource";
            colExceptionSource.ReadOnly = true;
            colExceptionSource.Width = 125;
            // 
            // colExceptionSourceLocation
            // 
            colExceptionSourceLocation.DataPropertyName = "SourceLocation";
            colExceptionSourceLocation.FillWeight = 22F;
            colExceptionSourceLocation.HeaderText = "Location";
            colExceptionSourceLocation.MinimumWidth = 88;
            colExceptionSourceLocation.Name = "colExceptionSourceLocation";
            colExceptionSourceLocation.ReadOnly = true;
            colExceptionSourceLocation.Width = 125;
            // 
            // exceptionDetailsLayout
            // 
            exceptionDetailsLayout.ColumnCount = 1;
            exceptionDetailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            exceptionDetailsLayout.Controls.Add(exceptionDetailToolbar, 0, 0);
            exceptionDetailsLayout.Controls.Add(tabExceptionDetails, 0, 1);
            exceptionDetailsLayout.Dock = DockStyle.Fill;
            exceptionDetailsLayout.Location = new Point(12, 0);
            exceptionDetailsLayout.Name = "exceptionDetailsLayout";
            exceptionDetailsLayout.RowCount = 2;
            exceptionDetailsLayout.RowStyles.Add(new RowStyle());
            exceptionDetailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            exceptionDetailsLayout.Size = new Size(535, 566);
            exceptionDetailsLayout.TabIndex = 0;
            // 
            // exceptionDetailToolbar
            // 
            exceptionDetailToolbar.AutoSize = true;
            exceptionDetailToolbar.Controls.Add(btnOpenExceptionSource);
            exceptionDetailToolbar.Controls.Add(btnCopyExceptionDetails);
            exceptionDetailToolbar.Dock = DockStyle.Fill;
            exceptionDetailToolbar.Location = new Point(0, 0);
            exceptionDetailToolbar.Margin = new Padding(0, 0, 0, 8);
            exceptionDetailToolbar.Name = "exceptionDetailToolbar";
            exceptionDetailToolbar.Size = new Size(535, 46);
            exceptionDetailToolbar.TabIndex = 0;
            exceptionDetailToolbar.WrapContents = false;
            // 
            // btnOpenExceptionSource
            // 
            btnOpenExceptionSource.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnOpenExceptionSource.BorderWidth = 1F;
            btnOpenExceptionSource.IconSvg = "FileSearchOutlined";
            btnOpenExceptionSource.Location = new Point(3, 3);
            btnOpenExceptionSource.Name = "btnOpenExceptionSource";
            btnOpenExceptionSource.Size = new Size(163, 40);
            btnOpenExceptionSource.TabIndex = 0;
            btnOpenExceptionSource.Tag = "perm:button.log.open-source:visible";
            btnOpenExceptionSource.Text = "Open Source";
            // 
            // btnCopyExceptionDetails
            // 
            btnCopyExceptionDetails.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnCopyExceptionDetails.BorderWidth = 1F;
            btnCopyExceptionDetails.IconSvg = "CopyOutlined";
            btnCopyExceptionDetails.Location = new Point(172, 3);
            btnCopyExceptionDetails.Name = "btnCopyExceptionDetails";
            btnCopyExceptionDetails.Size = new Size(98, 40);
            btnCopyExceptionDetails.TabIndex = 1;
            btnCopyExceptionDetails.Tag = "perm:button.log.copy-details:visible";
            btnCopyExceptionDetails.Text = "Copy";
            // 
            // tabExceptionDetails
            // 
            tabExceptionDetails.Controls.Add(tabExceptionBasicInfo);
            tabExceptionDetails.Controls.Add(tabExceptionStackTrace);
            tabExceptionDetails.Controls.Add(tabExceptionContext);
            tabExceptionDetails.Dock = DockStyle.Fill;
            tabExceptionDetails.Location = new Point(3, 57);
            tabExceptionDetails.Name = "tabExceptionDetails";
            tabExceptionDetails.SelectedIndex = 0;
            tabExceptionDetails.Size = new Size(529, 506);
            tabExceptionDetails.TabIndex = 1;
            // 
            // tabExceptionBasicInfo
            // 
            tabExceptionBasicInfo.Controls.Add(txtExceptionBasicInfo);
            tabExceptionBasicInfo.Location = new Point(4, 32);
            tabExceptionBasicInfo.Name = "tabExceptionBasicInfo";
            tabExceptionBasicInfo.Padding = new Padding(3);
            tabExceptionBasicInfo.Size = new Size(521, 470);
            tabExceptionBasicInfo.TabIndex = 0;
            tabExceptionBasicInfo.Text = "Basic";
            tabExceptionBasicInfo.UseVisualStyleBackColor = true;
            // 
            // txtExceptionBasicInfo
            // 
            txtExceptionBasicInfo.BackColor = SystemColors.Window;
            txtExceptionBasicInfo.BorderStyle = BorderStyle.FixedSingle;
            txtExceptionBasicInfo.Dock = DockStyle.Fill;
            txtExceptionBasicInfo.Font = new Font("Consolas", 10F);
            txtExceptionBasicInfo.Location = new Point(3, 3);
            txtExceptionBasicInfo.Multiline = true;
            txtExceptionBasicInfo.Name = "txtExceptionBasicInfo";
            txtExceptionBasicInfo.ReadOnly = true;
            txtExceptionBasicInfo.ScrollBars = ScrollBars.Both;
            txtExceptionBasicInfo.Size = new Size(515, 464);
            txtExceptionBasicInfo.TabIndex = 0;
            txtExceptionBasicInfo.WordWrap = false;
            // 
            // tabExceptionStackTrace
            // 
            tabExceptionStackTrace.Controls.Add(txtExceptionStackTrace);
            tabExceptionStackTrace.Location = new Point(4, 29);
            tabExceptionStackTrace.Name = "tabExceptionStackTrace";
            tabExceptionStackTrace.Padding = new Padding(3);
            tabExceptionStackTrace.Size = new Size(521, 473);
            tabExceptionStackTrace.TabIndex = 1;
            tabExceptionStackTrace.Text = "Stack Trace";
            tabExceptionStackTrace.UseVisualStyleBackColor = true;
            // 
            // txtExceptionStackTrace
            // 
            txtExceptionStackTrace.BackColor = SystemColors.Window;
            txtExceptionStackTrace.BorderStyle = BorderStyle.FixedSingle;
            txtExceptionStackTrace.Dock = DockStyle.Fill;
            txtExceptionStackTrace.Font = new Font("Consolas", 10F);
            txtExceptionStackTrace.Location = new Point(3, 3);
            txtExceptionStackTrace.Multiline = true;
            txtExceptionStackTrace.Name = "txtExceptionStackTrace";
            txtExceptionStackTrace.ReadOnly = true;
            txtExceptionStackTrace.ScrollBars = ScrollBars.Both;
            txtExceptionStackTrace.Size = new Size(515, 467);
            txtExceptionStackTrace.TabIndex = 0;
            txtExceptionStackTrace.WordWrap = false;
            // 
            // tabExceptionContext
            // 
            tabExceptionContext.Controls.Add(txtExceptionContext);
            tabExceptionContext.Location = new Point(4, 29);
            tabExceptionContext.Name = "tabExceptionContext";
            tabExceptionContext.Padding = new Padding(3);
            tabExceptionContext.Size = new Size(521, 473);
            tabExceptionContext.TabIndex = 2;
            tabExceptionContext.Text = "Context";
            tabExceptionContext.UseVisualStyleBackColor = true;
            // 
            // txtExceptionContext
            // 
            txtExceptionContext.BackColor = SystemColors.Window;
            txtExceptionContext.BorderStyle = BorderStyle.FixedSingle;
            txtExceptionContext.Dock = DockStyle.Fill;
            txtExceptionContext.Font = new Font("Consolas", 10F);
            txtExceptionContext.Location = new Point(3, 3);
            txtExceptionContext.Multiline = true;
            txtExceptionContext.Name = "txtExceptionContext";
            txtExceptionContext.ReadOnly = true;
            txtExceptionContext.ScrollBars = ScrollBars.Both;
            txtExceptionContext.Size = new Size(515, 467);
            txtExceptionContext.TabIndex = 0;
            txtExceptionContext.WordWrap = false;
            // 
            // tabDeviceStatusLogs
            // 
            tabDeviceStatusLogs.Controls.Add(deviceStatusRootLayout);
            tabDeviceStatusLogs.Location = new Point(4, 29);
            tabDeviceStatusLogs.Name = "tabDeviceStatusLogs";
            tabDeviceStatusLogs.Padding = new Padding(3);
            tabDeviceStatusLogs.Size = new Size(1358, 712);
            tabDeviceStatusLogs.TabIndex = 3;
            tabDeviceStatusLogs.Text = "Device Status";
            tabDeviceStatusLogs.UseVisualStyleBackColor = true;
            // 
            // deviceStatusRootLayout
            // 
            deviceStatusRootLayout.ColumnCount = 1;
            deviceStatusRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            deviceStatusRootLayout.Controls.Add(deviceStatusHeaderLayout, 0, 0);
            deviceStatusRootLayout.Controls.Add(splitDeviceStatusContent, 0, 1);
            deviceStatusRootLayout.Dock = DockStyle.Fill;
            deviceStatusRootLayout.Location = new Point(3, 3);
            deviceStatusRootLayout.Name = "deviceStatusRootLayout";
            deviceStatusRootLayout.RowCount = 2;
            deviceStatusRootLayout.RowStyles.Add(new RowStyle());
            deviceStatusRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            deviceStatusRootLayout.Size = new Size(1352, 706);
            deviceStatusRootLayout.TabIndex = 0;
            // 
            // deviceStatusHeaderLayout
            // 
            deviceStatusHeaderLayout.ColumnCount = 2;
            deviceStatusHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            deviceStatusHeaderLayout.ColumnStyles.Add(new ColumnStyle());
            deviceStatusHeaderLayout.Controls.Add(deviceStatusTitleLayout, 0, 0);
            deviceStatusHeaderLayout.Controls.Add(deviceStatusToolbar, 1, 0);
            deviceStatusHeaderLayout.Dock = DockStyle.Fill;
            deviceStatusHeaderLayout.Location = new Point(20, 14);
            deviceStatusHeaderLayout.Margin = new Padding(20, 14, 20, 8);
            deviceStatusHeaderLayout.Name = "deviceStatusHeaderLayout";
            deviceStatusHeaderLayout.RowCount = 1;
            deviceStatusHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            deviceStatusHeaderLayout.Size = new Size(1312, 100);
            deviceStatusHeaderLayout.TabIndex = 0;
            // 
            // deviceStatusTitleLayout
            // 
            deviceStatusTitleLayout.ColumnCount = 1;
            deviceStatusTitleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            deviceStatusTitleLayout.Controls.Add(lblDeviceStatusTitle, 0, 0);
            deviceStatusTitleLayout.Controls.Add(lblDeviceStatusDescription, 0, 1);
            deviceStatusTitleLayout.Dock = DockStyle.Fill;
            deviceStatusTitleLayout.Location = new Point(0, 0);
            deviceStatusTitleLayout.Margin = new Padding(0);
            deviceStatusTitleLayout.Name = "deviceStatusTitleLayout";
            deviceStatusTitleLayout.RowCount = 2;
            deviceStatusTitleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            deviceStatusTitleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            deviceStatusTitleLayout.Size = new Size(605, 100);
            deviceStatusTitleLayout.TabIndex = 0;
            // 
            // lblDeviceStatusTitle
            // 
            lblDeviceStatusTitle.AutoSize = true;
            lblDeviceStatusTitle.Dock = DockStyle.Fill;
            lblDeviceStatusTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            lblDeviceStatusTitle.Location = new Point(0, 0);
            lblDeviceStatusTitle.Margin = new Padding(0);
            lblDeviceStatusTitle.Name = "lblDeviceStatusTitle";
            lblDeviceStatusTitle.Size = new Size(605, 34);
            lblDeviceStatusTitle.TabIndex = 0;
            lblDeviceStatusTitle.Text = "Device Status";
            lblDeviceStatusTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblDeviceStatusDescription
            // 
            lblDeviceStatusDescription.AutoEllipsis = true;
            lblDeviceStatusDescription.Dock = DockStyle.Fill;
            lblDeviceStatusDescription.ForeColor = SystemColors.GrayText;
            lblDeviceStatusDescription.Location = new Point(0, 34);
            lblDeviceStatusDescription.Margin = new Padding(0);
            lblDeviceStatusDescription.Name = "lblDeviceStatusDescription";
            lblDeviceStatusDescription.Size = new Size(605, 66);
            lblDeviceStatusDescription.TabIndex = 1;
            lblDeviceStatusDescription.Text = "Device Status details";
            lblDeviceStatusDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // deviceStatusToolbar
            // 
            deviceStatusToolbar.AutoSize = true;
            deviceStatusToolbar.Controls.Add(lblDeviceStatusDate);
            deviceStatusToolbar.Controls.Add(dtpDeviceStatusDate);
            deviceStatusToolbar.Controls.Add(lblDeviceStatusKeyword);
            deviceStatusToolbar.Controls.Add(txtDeviceStatusKeyword);
            deviceStatusToolbar.Controls.Add(btnRefreshDeviceStatus);
            deviceStatusToolbar.Dock = DockStyle.Right;
            deviceStatusToolbar.Location = new Point(605, 0);
            deviceStatusToolbar.Margin = new Padding(0);
            deviceStatusToolbar.Name = "deviceStatusToolbar";
            deviceStatusToolbar.Padding = new Padding(0, 6, 0, 0);
            deviceStatusToolbar.Size = new Size(707, 100);
            deviceStatusToolbar.TabIndex = 1;
            deviceStatusToolbar.WrapContents = false;
            // 
            // lblDeviceStatusDate
            // 
            lblDeviceStatusDate.AutoSize = true;
            lblDeviceStatusDate.Location = new Point(0, 15);
            lblDeviceStatusDate.Margin = new Padding(0, 9, 8, 0);
            lblDeviceStatusDate.Name = "lblDeviceStatusDate";
            lblDeviceStatusDate.Size = new Size(51, 24);
            lblDeviceStatusDate.TabIndex = 0;
            lblDeviceStatusDate.Text = "Date";
            // 
            // dtpDeviceStatusDate
            // 
            dtpDeviceStatusDate.CustomFormat = "yyyy-MM-dd";
            dtpDeviceStatusDate.Format = DateTimePickerFormat.Custom;
            dtpDeviceStatusDate.Location = new Point(59, 8);
            dtpDeviceStatusDate.Margin = new Padding(0, 2, 16, 0);
            dtpDeviceStatusDate.Name = "dtpDeviceStatusDate";
            dtpDeviceStatusDate.Size = new Size(150, 30);
            dtpDeviceStatusDate.TabIndex = 1;
            // 
            // lblDeviceStatusKeyword
            // 
            lblDeviceStatusKeyword.AutoSize = true;
            lblDeviceStatusKeyword.Location = new Point(225, 15);
            lblDeviceStatusKeyword.Margin = new Padding(0, 9, 8, 0);
            lblDeviceStatusKeyword.Name = "lblDeviceStatusKeyword";
            lblDeviceStatusKeyword.Size = new Size(85, 24);
            lblDeviceStatusKeyword.TabIndex = 2;
            lblDeviceStatusKeyword.Text = "Keyword";
            // 
            // txtDeviceStatusKeyword
            // 
            txtDeviceStatusKeyword.Location = new Point(318, 8);
            txtDeviceStatusKeyword.Margin = new Padding(0, 2, 16, 0);
            txtDeviceStatusKeyword.Name = "txtDeviceStatusKeyword";
            txtDeviceStatusKeyword.PlaceholderText = "Station / Status / WorkOrder / Source";
            txtDeviceStatusKeyword.Size = new Size(250, 30);
            txtDeviceStatusKeyword.TabIndex = 3;
            // 
            // btnRefreshDeviceStatus
            // 
            btnRefreshDeviceStatus.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnRefreshDeviceStatus.BorderWidth = 1F;
            btnRefreshDeviceStatus.IconSvg = "ReloadOutlined";
            btnRefreshDeviceStatus.Location = new Point(587, 9);
            btnRefreshDeviceStatus.Name = "btnRefreshDeviceStatus";
            btnRefreshDeviceStatus.Size = new Size(117, 40);
            btnRefreshDeviceStatus.TabIndex = 4;
            btnRefreshDeviceStatus.Tag = "perm:button.log.refresh:visible";
            btnRefreshDeviceStatus.Text = "Refresh";
            // 
            // splitDeviceStatusContent
            // 
            splitDeviceStatusContent.Dock = DockStyle.Fill;
            splitDeviceStatusContent.Location = new Point(20, 122);
            splitDeviceStatusContent.Margin = new Padding(20, 0, 20, 18);
            splitDeviceStatusContent.Name = "splitDeviceStatusContent";
            // 
            // splitDeviceStatusContent.Panel1
            // 
            splitDeviceStatusContent.Panel1.Controls.Add(dgvDeviceStatusLogs);
            splitDeviceStatusContent.Panel1.Padding = new Padding(0, 0, 12, 0);
            // 
            // splitDeviceStatusContent.Panel2
            // 
            splitDeviceStatusContent.Panel2.Controls.Add(txtDeviceStatusDetail);
            splitDeviceStatusContent.Panel2.Padding = new Padding(12, 0, 0, 0);
            splitDeviceStatusContent.Size = new Size(1312, 566);
            splitDeviceStatusContent.SplitterDistance = 820;
            splitDeviceStatusContent.SplitterWidth = 5;
            splitDeviceStatusContent.TabIndex = 1;
            // 
            // dgvDeviceStatusLogs
            // 
            dgvDeviceStatusLogs.AllowUserToAddRows = false;
            dgvDeviceStatusLogs.AllowUserToDeleteRows = false;
            dgvDeviceStatusLogs.BackgroundColor = SystemColors.Window;
            dgvDeviceStatusLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDeviceStatusLogs.Columns.AddRange(new DataGridViewColumn[] { colDeviceOccurredTime, colDeviceStation, colDeviceStatus, colDeviceStatusName, colDeviceWorkOrder, colDeviceSource, colDeviceReportStatus, colDeviceReportMessage });
            dgvDeviceStatusLogs.Dock = DockStyle.Fill;
            dgvDeviceStatusLogs.Location = new Point(0, 0);
            dgvDeviceStatusLogs.MultiSelect = false;
            dgvDeviceStatusLogs.Name = "dgvDeviceStatusLogs";
            dgvDeviceStatusLogs.ReadOnly = true;
            dgvDeviceStatusLogs.RowHeadersVisible = false;
            dgvDeviceStatusLogs.RowHeadersWidth = 51;
            dgvDeviceStatusLogs.RowTemplate.Height = 28;
            dgvDeviceStatusLogs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDeviceStatusLogs.Size = new Size(808, 566);
            dgvDeviceStatusLogs.TabIndex = 0;
            // 
            // colDeviceOccurredTime
            // 
            colDeviceOccurredTime.DataPropertyName = "OccurredTime";
            colDeviceOccurredTime.FillWeight = 14F;
            colDeviceOccurredTime.HeaderText = "Time";
            colDeviceOccurredTime.MinimumWidth = 88;
            colDeviceOccurredTime.Name = "colDeviceOccurredTime";
            colDeviceOccurredTime.ReadOnly = true;
            colDeviceOccurredTime.Width = 125;
            // 
            // colDeviceStation
            // 
            colDeviceStation.DataPropertyName = "Station";
            colDeviceStation.FillWeight = 8F;
            colDeviceStation.HeaderText = "Station";
            colDeviceStation.MinimumWidth = 88;
            colDeviceStation.Name = "colDeviceStation";
            colDeviceStation.ReadOnly = true;
            colDeviceStation.Width = 125;
            // 
            // colDeviceStatus
            // 
            colDeviceStatus.DataPropertyName = "DeviceState";
            colDeviceStatus.FillWeight = 10F;
            colDeviceStatus.HeaderText = "Status Code";
            colDeviceStatus.MinimumWidth = 88;
            colDeviceStatus.Name = "colDeviceStatus";
            colDeviceStatus.ReadOnly = true;
            colDeviceStatus.Width = 125;
            // 
            // colDeviceStatusName
            // 
            colDeviceStatusName.DataPropertyName = "StatusName";
            colDeviceStatusName.FillWeight = 14F;
            colDeviceStatusName.HeaderText = "Status";
            colDeviceStatusName.MinimumWidth = 88;
            colDeviceStatusName.Name = "colDeviceStatusName";
            colDeviceStatusName.ReadOnly = true;
            colDeviceStatusName.Width = 125;
            // 
            // colDeviceWorkOrder
            // 
            colDeviceWorkOrder.DataPropertyName = "SN";
            colDeviceWorkOrder.FillWeight = 16F;
            colDeviceWorkOrder.HeaderText = "Work Order";
            colDeviceWorkOrder.MinimumWidth = 88;
            colDeviceWorkOrder.Name = "colDeviceWorkOrder";
            colDeviceWorkOrder.ReadOnly = true;
            colDeviceWorkOrder.Width = 125;
            // 
            // colDeviceSource
            // 
            colDeviceSource.DataPropertyName = "Source";
            colDeviceSource.FillWeight = 13F;
            colDeviceSource.HeaderText = "Source";
            colDeviceSource.MinimumWidth = 88;
            colDeviceSource.Name = "colDeviceSource";
            colDeviceSource.ReadOnly = true;
            colDeviceSource.Width = 125;
            // 
            // colDeviceReportStatus
            // 
            colDeviceReportStatus.DataPropertyName = "ReportStatus";
            colDeviceReportStatus.FillWeight = 13F;
            colDeviceReportStatus.HeaderText = "Upload Status";
            colDeviceReportStatus.MinimumWidth = 88;
            colDeviceReportStatus.Name = "colDeviceReportStatus";
            colDeviceReportStatus.ReadOnly = true;
            colDeviceReportStatus.Width = 125;
            // 
            // colDeviceReportMessage
            // 
            colDeviceReportMessage.DataPropertyName = "ReportMessage";
            colDeviceReportMessage.FillWeight = 22F;
            colDeviceReportMessage.HeaderText = "Upload Message";
            colDeviceReportMessage.MinimumWidth = 88;
            colDeviceReportMessage.Name = "colDeviceReportMessage";
            colDeviceReportMessage.ReadOnly = true;
            colDeviceReportMessage.Width = 125;
            // 
            // txtDeviceStatusDetail
            // 
            txtDeviceStatusDetail.BackColor = SystemColors.Window;
            txtDeviceStatusDetail.BorderStyle = BorderStyle.FixedSingle;
            txtDeviceStatusDetail.Dock = DockStyle.Fill;
            txtDeviceStatusDetail.Font = new Font("Consolas", 10F);
            txtDeviceStatusDetail.Location = new Point(12, 0);
            txtDeviceStatusDetail.Multiline = true;
            txtDeviceStatusDetail.Name = "txtDeviceStatusDetail";
            txtDeviceStatusDetail.ReadOnly = true;
            txtDeviceStatusDetail.ScrollBars = ScrollBars.Both;
            txtDeviceStatusDetail.Size = new Size(475, 566);
            txtDeviceStatusDetail.TabIndex = 0;
            txtDeviceStatusDetail.WordWrap = false;
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
            productionRootLayout.ResumeLayout(false);
            productionHeaderLayout.ResumeLayout(false);
            productionHeaderLayout.PerformLayout();
            productionTitleLayout.ResumeLayout(false);
            productionTitleLayout.PerformLayout();
            productionToolbar.ResumeLayout(false);
            productionToolbar.PerformLayout();
            splitProductionContent.Panel1.ResumeLayout(false);
            splitProductionContent.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitProductionContent).EndInit();
            splitProductionContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvProductionLogs).EndInit();
            tabProductionDetails.ResumeLayout(false);
            tabProductionBasicInfo.ResumeLayout(false);
            tabProductionBasicInfo.PerformLayout();
            tabProductionDetail.ResumeLayout(false);
            tabProductionDetail.PerformLayout();
            tabExceptionLogs.ResumeLayout(false);
            exceptionRootLayout.ResumeLayout(false);
            exceptionHeaderLayout.ResumeLayout(false);
            exceptionHeaderLayout.PerformLayout();
            exceptionTitleLayout.ResumeLayout(false);
            exceptionTitleLayout.PerformLayout();
            exceptionToolbar.ResumeLayout(false);
            exceptionToolbar.PerformLayout();
            splitExceptionContent.Panel1.ResumeLayout(false);
            splitExceptionContent.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitExceptionContent).EndInit();
            splitExceptionContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvExceptionLogs).EndInit();
            exceptionDetailsLayout.ResumeLayout(false);
            exceptionDetailsLayout.PerformLayout();
            exceptionDetailToolbar.ResumeLayout(false);
            exceptionDetailToolbar.PerformLayout();
            tabExceptionDetails.ResumeLayout(false);
            tabExceptionBasicInfo.ResumeLayout(false);
            tabExceptionBasicInfo.PerformLayout();
            tabExceptionStackTrace.ResumeLayout(false);
            tabExceptionStackTrace.PerformLayout();
            tabExceptionContext.ResumeLayout(false);
            tabExceptionContext.PerformLayout();
            tabDeviceStatusLogs.ResumeLayout(false);
            deviceStatusRootLayout.ResumeLayout(false);
            deviceStatusHeaderLayout.ResumeLayout(false);
            deviceStatusHeaderLayout.PerformLayout();
            deviceStatusTitleLayout.ResumeLayout(false);
            deviceStatusTitleLayout.PerformLayout();
            deviceStatusToolbar.ResumeLayout(false);
            deviceStatusToolbar.PerformLayout();
            splitDeviceStatusContent.Panel1.ResumeLayout(false);
            splitDeviceStatusContent.Panel2.ResumeLayout(false);
            splitDeviceStatusContent.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitDeviceStatusContent).EndInit();
            splitDeviceStatusContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvDeviceStatusLogs).EndInit();
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
        private TabPage tabProductionLogs;
        private TableLayoutPanel productionRootLayout;
        private TableLayoutPanel productionHeaderLayout;
        private TableLayoutPanel productionTitleLayout;
        private Label lblProductionTitle;
        private Label lblProductionDescription;
        private FlowLayoutPanel productionToolbar;
        private Label lblProductionDate;
        private DateTimePicker dtpProductionDate;
        private Label lblProductionKeyword;
        private TextBox txtProductionKeyword;
        private AntdUI.Button btnRefreshProduction;
        private AntdUI.Button btnOpenProductionFolder;
        private SplitContainer splitProductionContent;
        private DataGridView dgvProductionLogs;
        private TabPage tabExceptionLogs;
        private TableLayoutPanel exceptionRootLayout;
        private TableLayoutPanel exceptionHeaderLayout;
        private TableLayoutPanel exceptionTitleLayout;
        private Label lblExceptionTitle;
        private Label lblExceptionDescription;
        private FlowLayoutPanel exceptionToolbar;
        private Label lblExceptionDate;
        private DateTimePicker dtpExceptionDate;
        private Label lblExceptionKeyword;
        private TextBox txtExceptionKeyword;
        private AntdUI.Button btnRefreshException;
        private AntdUI.Button btnOpenExceptionFolder;
        private SplitContainer splitExceptionContent;
        private DataGridView dgvExceptionLogs;
        private TabPage tabDeviceStatusLogs;
        private TableLayoutPanel deviceStatusRootLayout;
        private TableLayoutPanel deviceStatusHeaderLayout;
        private TableLayoutPanel deviceStatusTitleLayout;
        private Label lblDeviceStatusTitle;
        private Label lblDeviceStatusDescription;
        private FlowLayoutPanel deviceStatusToolbar;
        private Label lblDeviceStatusDate;
        private DateTimePicker dtpDeviceStatusDate;
        private Label lblDeviceStatusKeyword;
        private TextBox txtDeviceStatusKeyword;
        private AntdUI.Button btnRefreshDeviceStatus;
        private SplitContainer splitDeviceStatusContent;
        private DataGridView dgvDeviceStatusLogs;
        private TabControl tabMesDetails;
        private TabPage tabBasicInfo;
        private TextBox txtBasicInfo;
        private TabPage tabRequestBody;
        private TextBox txtRequestBody;
        private TabPage tabResponseBody;
        private TextBox txtResponseBody;
        private TabControl tabProductionDetails;
        private TabPage tabProductionBasicInfo;
        private TextBox txtProductionBasicInfo;
        private TabPage tabProductionDetail;
        private TextBox txtProductionDetail;
        private TableLayoutPanel exceptionDetailsLayout;
        private FlowLayoutPanel exceptionDetailToolbar;
        private AntdUI.Button btnOpenExceptionSource;
        private AntdUI.Button btnCopyExceptionDetails;
        private TabControl tabExceptionDetails;
        private TabPage tabExceptionBasicInfo;
        private TextBox txtExceptionBasicInfo;
        private TabPage tabExceptionStackTrace;
        private TextBox txtExceptionStackTrace;
        private TabPage tabExceptionContext;
        private TextBox txtExceptionContext;
        private TextBox txtDeviceStatusDetail;
        private DataGridViewTextBoxColumn colMesSendTime;
        private DataGridViewTextBoxColumn colMesPurpose;
        private DataGridViewTextBoxColumn colMesMethod;
        private DataGridViewTextBoxColumn colMesHttpStatus;
        private DataGridViewTextBoxColumn colMesStatus;
        private DataGridViewTextBoxColumn colResult;
        private DataGridViewTextBoxColumn colMesDuration;
        private DataGridViewTextBoxColumn colProductionOccurredTime;
        private DataGridViewTextBoxColumn colProductionLevel;
        private DataGridViewTextBoxColumn colProductionStep;
        private DataGridViewTextBoxColumn colProductionSummary;
        private DataGridViewTextBoxColumn colProductionStation;
        private DataGridViewTextBoxColumn colProductionPlcSignal;
        private DataGridViewTextBoxColumn colExceptionOccurredTime;
        private DataGridViewTextBoxColumn colExceptionCategory;
        private DataGridViewTextBoxColumn colExceptionSeverity;
        private DataGridViewTextBoxColumn colExceptionType;
        private DataGridViewTextBoxColumn colExceptionMessage;
        private DataGridViewTextBoxColumn colExceptionSource;
        private DataGridViewTextBoxColumn colExceptionSourceLocation;
        private DataGridViewTextBoxColumn colDeviceOccurredTime;
        private DataGridViewTextBoxColumn colDeviceStation;
        private DataGridViewTextBoxColumn colDeviceStatus;
        private DataGridViewTextBoxColumn colDeviceStatusName;
        private DataGridViewTextBoxColumn colDeviceWorkOrder;
        private DataGridViewTextBoxColumn colDeviceSource;
        private DataGridViewTextBoxColumn colDeviceReportStatus;
        private DataGridViewTextBoxColumn colDeviceReportMessage;
    }
}
