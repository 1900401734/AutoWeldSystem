using AntdUI;
using Label = System.Windows.Forms.Label;
using TabPage = System.Windows.Forms.TabPage;

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
            components = new System.ComponentModel.Container();
            tabLogCategories = new TabControl();
            tabMesLogs = new TabPage();
            mesRootLayout = new TableLayoutPanel();
            mesInteractionHeaderLayout = new TableLayoutPanel();
            tableLayoutPanel9 = new TableLayoutPanel();
            tableLayoutPanel10 = new TableLayoutPanel();
            btnOpenMesFolder = new AntdUI.Button();
            dtpMesDate = new DatePicker();
            lblMesDate = new AntdUI.Label();
            queryMesLogs = new AutoWeldSystem.UI.Controls.InputQuery(components);
            tableLayoutPanel11 = new TableLayoutPanel();
            lblMesTitle = new Label();
            lblMesDescription = new Label();
            splitterMesContent = new AntdUI.Splitter();
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
            splitterProductionContent = new AntdUI.Splitter();
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
            productionHeaderLayout = new TableLayoutPanel();
            tableLayoutPanel3 = new TableLayoutPanel();
            tlpProductionToolbar = new TableLayoutPanel();
            btnOpenProductionFolder = new AntdUI.Button();
            dtpProductionDate = new DatePicker();
            lblProductionDate = new AntdUI.Label();
            queryProductionLogs = new AutoWeldSystem.UI.Controls.InputQuery(components);
            productionTitleLayout = new TableLayoutPanel();
            lblProductionTitle = new Label();
            lblProductionDescription = new Label();
            tabExceptionLogs = new TabPage();
            exceptionRootLayout = new TableLayoutPanel();
            exceptionHeaderLayout = new TableLayoutPanel();
            tableLayoutPanel5 = new TableLayoutPanel();
            tableLayoutPanel4 = new TableLayoutPanel();
            btnOpenExceptionFolder = new AntdUI.Button();
            dtpExceptionDate = new DatePicker();
            lblExceptionDate = new AntdUI.Label();
            queryExceptionLogs = new AutoWeldSystem.UI.Controls.InputQuery(components);
            exceptionTitleLayout = new TableLayoutPanel();
            lblExceptionTitle = new Label();
            lblExceptionDescription = new Label();
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
            tabDeviceLifecycleLogs = new TabPage();
            deviceLifecycleRootLayout = new TableLayoutPanel();
            deviceLifecycleHeaderLayout = new TableLayoutPanel();
            tableLayoutPanel6 = new TableLayoutPanel();
            deviceLifecycleToolbar = new TableLayoutPanel();
            btnOpenDeviceLifecycleFolder = new AntdUI.Button();
            dtpDeviceLifecycleDate = new DatePicker();
            lblDeviceLifecycleDate = new AntdUI.Label();
            queryDeviceLifecycleLogs = new AutoWeldSystem.UI.Controls.InputQuery(components);
            deviceLifecycleTitleLayout = new TableLayoutPanel();
            lblDeviceLifecycleTitle = new Label();
            lblDeviceLifecycleDescription = new Label();
            splitDeviceLifecycleContent = new SplitContainer();
            dgvDeviceLifecycleLogs = new DataGridView();
            colLifecycleOccurredTime = new DataGridViewTextBoxColumn();
            colLifecycleLevel = new DataGridViewTextBoxColumn();
            colLifecycleEventType = new DataGridViewTextBoxColumn();
            colLifecycleStation = new DataGridViewTextBoxColumn();
            colLifecycleStatus = new DataGridViewTextBoxColumn();
            colLifecycleSummary = new DataGridViewTextBoxColumn();
            txtDeviceLifecycleDetail = new TextBox();
            tabDeviceStatusLogs = new TabPage();
            deviceStatusRootLayout = new TableLayoutPanel();
            deviceStatusHeaderLayout = new TableLayoutPanel();
            tableLayoutPanel7 = new TableLayoutPanel();
            deviceStatusToolbar = new TableLayoutPanel();
            dtpDeviceStatusDate = new DatePicker();
            lblDeviceStatusDate = new AntdUI.Label();
            queryDeviceStatusLogs = new AutoWeldSystem.UI.Controls.InputQuery(components);
            deviceStatusTitleLayout = new TableLayoutPanel();
            lblDeviceStatusTitle = new Label();
            lblDeviceStatusDescription = new Label();
            splitDeviceStatusContent = new SplitContainer();
            dgvDeviceStatusLogs = new DataGridView();
            colDeviceOccurredTime = new DataGridViewTextBoxColumn();
            colDeviceStation = new DataGridViewTextBoxColumn();
            colDeviceStatus = new DataGridViewTextBoxColumn();
            colDeviceStatusName = new DataGridViewTextBoxColumn();
            colDeviceSource = new DataGridViewTextBoxColumn();
            colDeviceReportStatus = new DataGridViewTextBoxColumn();
            colDeviceReportMessage = new DataGridViewTextBoxColumn();
            txtDeviceStatusDetail = new TextBox();
            tabLogCategories.SuspendLayout();
            tabMesLogs.SuspendLayout();
            mesRootLayout.SuspendLayout();
            mesInteractionHeaderLayout.SuspendLayout();
            tableLayoutPanel9.SuspendLayout();
            tableLayoutPanel10.SuspendLayout();
            tableLayoutPanel11.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitterMesContent).BeginInit();
            splitterMesContent.Panel1.SuspendLayout();
            splitterMesContent.Panel2.SuspendLayout();
            splitterMesContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvMesLogs).BeginInit();
            tabMesDetails.SuspendLayout();
            tabBasicInfo.SuspendLayout();
            tabRequestBody.SuspendLayout();
            tabResponseBody.SuspendLayout();
            tabProductionLogs.SuspendLayout();
            productionRootLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitterProductionContent).BeginInit();
            splitterProductionContent.Panel1.SuspendLayout();
            splitterProductionContent.Panel2.SuspendLayout();
            splitterProductionContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvProductionLogs).BeginInit();
            tabProductionDetails.SuspendLayout();
            tabProductionBasicInfo.SuspendLayout();
            tabProductionDetail.SuspendLayout();
            productionHeaderLayout.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            tlpProductionToolbar.SuspendLayout();
            productionTitleLayout.SuspendLayout();
            tabExceptionLogs.SuspendLayout();
            exceptionRootLayout.SuspendLayout();
            exceptionHeaderLayout.SuspendLayout();
            tableLayoutPanel5.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            exceptionTitleLayout.SuspendLayout();
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
            tabDeviceLifecycleLogs.SuspendLayout();
            deviceLifecycleRootLayout.SuspendLayout();
            deviceLifecycleHeaderLayout.SuspendLayout();
            tableLayoutPanel6.SuspendLayout();
            deviceLifecycleToolbar.SuspendLayout();
            deviceLifecycleTitleLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitDeviceLifecycleContent).BeginInit();
            splitDeviceLifecycleContent.Panel1.SuspendLayout();
            splitDeviceLifecycleContent.Panel2.SuspendLayout();
            splitDeviceLifecycleContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDeviceLifecycleLogs).BeginInit();
            tabDeviceStatusLogs.SuspendLayout();
            deviceStatusRootLayout.SuspendLayout();
            deviceStatusHeaderLayout.SuspendLayout();
            tableLayoutPanel7.SuspendLayout();
            deviceStatusToolbar.SuspendLayout();
            deviceStatusTitleLayout.SuspendLayout();
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
            tabLogCategories.Controls.Add(tabDeviceLifecycleLogs);
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
            tabMesLogs.Size = new Size(1358, 709);
            tabMesLogs.TabIndex = 0;
            tabMesLogs.Text = "MES Interaction";
            tabMesLogs.UseVisualStyleBackColor = true;
            // 
            // mesRootLayout
            // 
            mesRootLayout.ColumnCount = 1;
            mesRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mesRootLayout.Controls.Add(mesInteractionHeaderLayout, 0, 0);
            mesRootLayout.Controls.Add(splitterMesContent, 0, 1);
            mesRootLayout.Dock = DockStyle.Fill;
            mesRootLayout.Location = new Point(0, 0);
            mesRootLayout.Name = "mesRootLayout";
            mesRootLayout.RowCount = 2;
            mesRootLayout.RowStyles.Add(new RowStyle());
            mesRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mesRootLayout.Size = new Size(1358, 709);
            mesRootLayout.TabIndex = 0;
            // 
            // mesInteractionHeaderLayout
            // 
            mesInteractionHeaderLayout.ColumnCount = 2;
            mesInteractionHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mesInteractionHeaderLayout.ColumnStyles.Add(new ColumnStyle());
            mesInteractionHeaderLayout.Controls.Add(tableLayoutPanel9, 1, 0);
            mesInteractionHeaderLayout.Controls.Add(tableLayoutPanel11, 0, 0);
            mesInteractionHeaderLayout.Dock = DockStyle.Fill;
            mesInteractionHeaderLayout.Location = new Point(20, 14);
            mesInteractionHeaderLayout.Margin = new Padding(20, 14, 20, 8);
            mesInteractionHeaderLayout.Name = "mesInteractionHeaderLayout";
            mesInteractionHeaderLayout.RowCount = 1;
            mesInteractionHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mesInteractionHeaderLayout.Size = new Size(1318, 105);
            mesInteractionHeaderLayout.TabIndex = 4;
            // 
            // tableLayoutPanel9
            // 
            tableLayoutPanel9.AutoSize = true;
            tableLayoutPanel9.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel9.ColumnCount = 1;
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel9.Controls.Add(tableLayoutPanel10, 0, 1);
            tableLayoutPanel9.Dock = DockStyle.Fill;
            tableLayoutPanel9.Location = new Point(698, 0);
            tableLayoutPanel9.Margin = new Padding(0);
            tableLayoutPanel9.Name = "tableLayoutPanel9";
            tableLayoutPanel9.RowCount = 3;
            tableLayoutPanel9.RowStyles.Add(new RowStyle(SizeType.Percent, 22.2222214F));
            tableLayoutPanel9.RowStyles.Add(new RowStyle(SizeType.Percent, 55.5555573F));
            tableLayoutPanel9.RowStyles.Add(new RowStyle(SizeType.Percent, 22.2222214F));
            tableLayoutPanel9.Size = new Size(620, 105);
            tableLayoutPanel9.TabIndex = 1;
            // 
            // tableLayoutPanel10
            // 
            tableLayoutPanel10.AutoSize = true;
            tableLayoutPanel10.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel10.ColumnCount = 4;
            tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel10.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel10.Controls.Add(btnOpenMesFolder, 3, 0);
            tableLayoutPanel10.Controls.Add(dtpMesDate, 1, 0);
            tableLayoutPanel10.Controls.Add(lblMesDate, 0, 0);
            tableLayoutPanel10.Controls.Add(queryMesLogs, 2, 0);
            tableLayoutPanel10.Dock = DockStyle.Fill;
            tableLayoutPanel10.Location = new Point(3, 26);
            tableLayoutPanel10.Name = "tableLayoutPanel10";
            tableLayoutPanel10.RowCount = 1;
            tableLayoutPanel10.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel10.Size = new Size(614, 52);
            tableLayoutPanel10.TabIndex = 2;
            // 
            // btnOpenMesFolder
            // 
            btnOpenMesFolder.BorderWidth = 1F;
            btnOpenMesFolder.Dock = DockStyle.Fill;
            btnOpenMesFolder.IconSvg = "FolderOpenOutlined";
            btnOpenMesFolder.Location = new Point(513, 0);
            btnOpenMesFolder.Margin = new Padding(0);
            btnOpenMesFolder.Name = "btnOpenMesFolder";
            btnOpenMesFolder.Size = new Size(101, 52);
            btnOpenMesFolder.TabIndex = 5;
            btnOpenMesFolder.Tag = "perm:button.log.open-folder:enabled";
            btnOpenMesFolder.Text = "Open";
            // 
            // dtpMesDate
            // 
            dtpMesDate.Dock = DockStyle.Fill;
            dtpMesDate.Location = new Point(40, 0);
            dtpMesDate.Margin = new Padding(0);
            dtpMesDate.Name = "dtpMesDate";
            dtpMesDate.Size = new Size(150, 52);
            dtpMesDate.TabIndex = 1;
            // 
            // lblMesDate
            // 
            lblMesDate.AutoSizeMode = TAutoSize.Width;
            lblMesDate.Dock = DockStyle.Fill;
            lblMesDate.Location = new Point(0, 0);
            lblMesDate.Margin = new Padding(0);
            lblMesDate.Name = "lblMesDate";
            lblMesDate.Size = new Size(40, 52);
            lblMesDate.TabIndex = 0;
            lblMesDate.Text = "Date";
            // 
            // queryMesLogs
            // 
            queryMesLogs.AutoSize = true;
            queryMesLogs.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            queryMesLogs.Dock = DockStyle.Fill;
            queryMesLogs.Location = new Point(190, 0);
            queryMesLogs.Margin = new Padding(0);
            queryMesLogs.MinimumSize = new Size(100, 40);
            queryMesLogs.Name = "queryMesLogs";
            queryMesLogs.QueryChanged = null;
            queryMesLogs.Size = new Size(323, 52);
            queryMesLogs.TabIndex = 6;
            // 
            // tableLayoutPanel11
            // 
            tableLayoutPanel11.ColumnCount = 1;
            tableLayoutPanel11.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel11.Controls.Add(lblMesTitle, 0, 0);
            tableLayoutPanel11.Controls.Add(lblMesDescription, 0, 1);
            tableLayoutPanel11.Dock = DockStyle.Fill;
            tableLayoutPanel11.Location = new Point(0, 0);
            tableLayoutPanel11.Margin = new Padding(0);
            tableLayoutPanel11.Name = "tableLayoutPanel11";
            tableLayoutPanel11.RowCount = 2;
            tableLayoutPanel11.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tableLayoutPanel11.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel11.Size = new Size(698, 105);
            tableLayoutPanel11.TabIndex = 0;
            // 
            // lblMesTitle
            // 
            lblMesTitle.AutoSize = true;
            lblMesTitle.Dock = DockStyle.Fill;
            lblMesTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            lblMesTitle.Location = new Point(0, 0);
            lblMesTitle.Margin = new Padding(0);
            lblMesTitle.Name = "lblMesTitle";
            lblMesTitle.Size = new Size(698, 34);
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
            lblMesDescription.Size = new Size(698, 71);
            lblMesDescription.TabIndex = 1;
            lblMesDescription.Text = "MES Intraction Detail";
            lblMesDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // splitterMesContent
            // 
            splitterMesContent.Dock = DockStyle.Fill;
            splitterMesContent.Location = new Point(20, 127);
            splitterMesContent.Margin = new Padding(20, 0, 20, 18);
            splitterMesContent.Name = "splitterMesContent";
            // 
            // splitterMesContent.Panel1
            // 
            splitterMesContent.Panel1.Controls.Add(dgvMesLogs);
            splitterMesContent.Panel1.Padding = new Padding(0, 0, 12, 0);
            // 
            // splitterMesContent.Panel2
            // 
            splitterMesContent.Panel2.Controls.Add(tabMesDetails);
            splitterMesContent.Panel2.Padding = new Padding(12, 0, 0, 0);
            splitterMesContent.Size = new Size(1318, 564);
            splitterMesContent.SplitterDistance = 894;
            splitterMesContent.TabIndex = 2;
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
            dgvMesLogs.Margin = new Padding(0);
            dgvMesLogs.MultiSelect = false;
            dgvMesLogs.Name = "dgvMesLogs";
            dgvMesLogs.ReadOnly = true;
            dgvMesLogs.RowHeadersVisible = false;
            dgvMesLogs.RowHeadersWidth = 51;
            dgvMesLogs.RowTemplate.Height = 28;
            dgvMesLogs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMesLogs.Size = new Size(882, 564);
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
            tabMesDetails.Size = new Size(408, 564);
            tabMesDetails.TabIndex = 0;
            // 
            // tabBasicInfo
            // 
            tabBasicInfo.Controls.Add(txtBasicInfo);
            tabBasicInfo.Location = new Point(4, 32);
            tabBasicInfo.Name = "tabBasicInfo";
            tabBasicInfo.Padding = new Padding(3);
            tabBasicInfo.Size = new Size(400, 528);
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
            txtBasicInfo.Size = new Size(394, 522);
            txtBasicInfo.TabIndex = 0;
            txtBasicInfo.WordWrap = false;
            // 
            // tabRequestBody
            // 
            tabRequestBody.Controls.Add(txtRequestBody);
            tabRequestBody.Location = new Point(4, 29);
            tabRequestBody.Name = "tabRequestBody";
            tabRequestBody.Padding = new Padding(3);
            tabRequestBody.Size = new Size(400, 531);
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
            txtRequestBody.Size = new Size(394, 525);
            txtRequestBody.TabIndex = 0;
            txtRequestBody.WordWrap = false;
            // 
            // tabResponseBody
            // 
            tabResponseBody.Controls.Add(txtResponseBody);
            tabResponseBody.Location = new Point(4, 29);
            tabResponseBody.Name = "tabResponseBody";
            tabResponseBody.Padding = new Padding(3);
            tabResponseBody.Size = new Size(400, 531);
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
            txtResponseBody.Size = new Size(394, 525);
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
            productionRootLayout.Controls.Add(splitterProductionContent, 0, 1);
            productionRootLayout.Controls.Add(productionHeaderLayout, 0, 0);
            productionRootLayout.Dock = DockStyle.Fill;
            productionRootLayout.Location = new Point(3, 3);
            productionRootLayout.Name = "productionRootLayout";
            productionRootLayout.RowCount = 2;
            productionRootLayout.RowStyles.Add(new RowStyle());
            productionRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            productionRootLayout.Size = new Size(1352, 703);
            productionRootLayout.TabIndex = 0;
            // 
            // splitterProductionContent
            // 
            splitterProductionContent.Dock = DockStyle.Fill;
            splitterProductionContent.Location = new Point(20, 127);
            splitterProductionContent.Margin = new Padding(20, 0, 20, 18);
            splitterProductionContent.Name = "splitterProductionContent";
            // 
            // splitterProductionContent.Panel1
            // 
            splitterProductionContent.Panel1.Controls.Add(dgvProductionLogs);
            splitterProductionContent.Panel1.Padding = new Padding(0, 0, 12, 0);
            // 
            // splitterProductionContent.Panel2
            // 
            splitterProductionContent.Panel2.Controls.Add(tabProductionDetails);
            splitterProductionContent.Panel2.Padding = new Padding(12, 0, 0, 0);
            splitterProductionContent.Size = new Size(1312, 558);
            splitterProductionContent.SplitterDistance = 847;
            splitterProductionContent.TabIndex = 1;
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
            dgvProductionLogs.Name = "dgvProductionLogs";
            dgvProductionLogs.ReadOnly = true;
            dgvProductionLogs.RowHeadersVisible = false;
            dgvProductionLogs.RowHeadersWidth = 51;
            dgvProductionLogs.RowTemplate.Height = 28;
            dgvProductionLogs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProductionLogs.Size = new Size(835, 558);
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
            tabProductionDetails.Size = new Size(449, 558);
            tabProductionDetails.TabIndex = 0;
            // 
            // tabProductionBasicInfo
            // 
            tabProductionBasicInfo.Controls.Add(txtProductionBasicInfo);
            tabProductionBasicInfo.Location = new Point(4, 32);
            tabProductionBasicInfo.Name = "tabProductionBasicInfo";
            tabProductionBasicInfo.Padding = new Padding(3);
            tabProductionBasicInfo.Size = new Size(441, 522);
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
            txtProductionBasicInfo.Size = new Size(435, 516);
            txtProductionBasicInfo.TabIndex = 0;
            txtProductionBasicInfo.WordWrap = false;
            // 
            // tabProductionDetail
            // 
            tabProductionDetail.Controls.Add(txtProductionDetail);
            tabProductionDetail.Location = new Point(4, 29);
            tabProductionDetail.Name = "tabProductionDetail";
            tabProductionDetail.Padding = new Padding(3);
            tabProductionDetail.Size = new Size(441, 528);
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
            txtProductionDetail.Size = new Size(435, 522);
            txtProductionDetail.TabIndex = 0;
            txtProductionDetail.WordWrap = false;
            // 
            // productionHeaderLayout
            // 
            productionHeaderLayout.ColumnCount = 2;
            productionHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            productionHeaderLayout.ColumnStyles.Add(new ColumnStyle());
            productionHeaderLayout.Controls.Add(tableLayoutPanel3, 1, 0);
            productionHeaderLayout.Controls.Add(productionTitleLayout, 0, 0);
            productionHeaderLayout.Dock = DockStyle.Fill;
            productionHeaderLayout.Location = new Point(20, 14);
            productionHeaderLayout.Margin = new Padding(20, 14, 20, 8);
            productionHeaderLayout.Name = "productionHeaderLayout";
            productionHeaderLayout.RowCount = 1;
            productionHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            productionHeaderLayout.Size = new Size(1312, 105);
            productionHeaderLayout.TabIndex = 0;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.AutoSize = true;
            tableLayoutPanel3.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel3.ColumnCount = 1;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Controls.Add(tlpProductionToolbar, 0, 1);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(634, 0);
            tableLayoutPanel3.Margin = new Padding(0);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 3;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 22.2222214F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 55.5555573F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 22.2222214F));
            tableLayoutPanel3.Size = new Size(678, 105);
            tableLayoutPanel3.TabIndex = 2;
            // 
            // tlpProductionToolbar
            // 
            tlpProductionToolbar.AutoSize = true;
            tlpProductionToolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpProductionToolbar.ColumnCount = 4;
            tlpProductionToolbar.ColumnStyles.Add(new ColumnStyle());
            tlpProductionToolbar.ColumnStyles.Add(new ColumnStyle());
            tlpProductionToolbar.ColumnStyles.Add(new ColumnStyle());
            tlpProductionToolbar.ColumnStyles.Add(new ColumnStyle());
            tlpProductionToolbar.Controls.Add(btnOpenProductionFolder, 3, 0);
            tlpProductionToolbar.Controls.Add(dtpProductionDate, 1, 0);
            tlpProductionToolbar.Controls.Add(lblProductionDate, 0, 0);
            tlpProductionToolbar.Controls.Add(queryProductionLogs, 2, 0);
            tlpProductionToolbar.Dock = DockStyle.Fill;
            tlpProductionToolbar.Location = new Point(3, 26);
            tlpProductionToolbar.Name = "tlpProductionToolbar";
            tlpProductionToolbar.RowCount = 1;
            tlpProductionToolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProductionToolbar.Size = new Size(672, 52);
            tlpProductionToolbar.TabIndex = 1;
            // 
            // btnOpenProductionFolder
            // 
            btnOpenProductionFolder.AutoSizeMode = TAutoSize.Width;
            btnOpenProductionFolder.BorderWidth = 1F;
            btnOpenProductionFolder.Dock = DockStyle.Fill;
            btnOpenProductionFolder.IconSvg = "FolderOpenOutlined";
            btnOpenProductionFolder.Location = new Point(513, 0);
            btnOpenProductionFolder.Margin = new Padding(0);
            btnOpenProductionFolder.Name = "btnOpenProductionFolder";
            btnOpenProductionFolder.Size = new Size(159, 52);
            btnOpenProductionFolder.TabIndex = 5;
            btnOpenProductionFolder.Tag = "perm:button.log.open-folder:enabled";
            btnOpenProductionFolder.Text = "Open Folder";
            // 
            // dtpProductionDate
            // 
            dtpProductionDate.Dock = DockStyle.Fill;
            dtpProductionDate.Location = new Point(40, 0);
            dtpProductionDate.Margin = new Padding(0);
            dtpProductionDate.Name = "dtpProductionDate";
            dtpProductionDate.Size = new Size(150, 52);
            dtpProductionDate.TabIndex = 1;
            // 
            // lblProductionDate
            // 
            lblProductionDate.AutoSizeMode = TAutoSize.Width;
            lblProductionDate.Dock = DockStyle.Fill;
            lblProductionDate.Location = new Point(0, 0);
            lblProductionDate.Margin = new Padding(0);
            lblProductionDate.Name = "lblProductionDate";
            lblProductionDate.Size = new Size(40, 52);
            lblProductionDate.TabIndex = 0;
            lblProductionDate.Text = "Date";
            // 
            // queryProductionLogs
            // 
            queryProductionLogs.AutoSize = true;
            queryProductionLogs.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            queryProductionLogs.Dock = DockStyle.Fill;
            queryProductionLogs.Location = new Point(190, 0);
            queryProductionLogs.Margin = new Padding(0);
            queryProductionLogs.MinimumSize = new Size(100, 40);
            queryProductionLogs.Name = "queryProductionLogs";
            queryProductionLogs.QueryChanged = null;
            queryProductionLogs.Size = new Size(323, 52);
            queryProductionLogs.TabIndex = 6;
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
            productionTitleLayout.Size = new Size(634, 105);
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
            lblProductionTitle.Size = new Size(634, 34);
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
            lblProductionDescription.Size = new Size(634, 71);
            lblProductionDescription.TabIndex = 1;
            lblProductionDescription.Text = "Production Flow details";
            lblProductionDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tabExceptionLogs
            // 
            tabExceptionLogs.Controls.Add(exceptionRootLayout);
            tabExceptionLogs.Location = new Point(4, 32);
            tabExceptionLogs.Name = "tabExceptionLogs";
            tabExceptionLogs.Padding = new Padding(3);
            tabExceptionLogs.Size = new Size(1358, 709);
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
            exceptionRootLayout.Size = new Size(1352, 703);
            exceptionRootLayout.TabIndex = 0;
            // 
            // exceptionHeaderLayout
            // 
            exceptionHeaderLayout.ColumnCount = 2;
            exceptionHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 99.99999F));
            exceptionHeaderLayout.ColumnStyles.Add(new ColumnStyle());
            exceptionHeaderLayout.Controls.Add(tableLayoutPanel5, 1, 0);
            exceptionHeaderLayout.Controls.Add(exceptionTitleLayout, 0, 0);
            exceptionHeaderLayout.Dock = DockStyle.Fill;
            exceptionHeaderLayout.Location = new Point(20, 14);
            exceptionHeaderLayout.Margin = new Padding(20, 14, 20, 8);
            exceptionHeaderLayout.Name = "exceptionHeaderLayout";
            exceptionHeaderLayout.RowCount = 1;
            exceptionHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            exceptionHeaderLayout.Size = new Size(1312, 104);
            exceptionHeaderLayout.TabIndex = 0;
            // 
            // tableLayoutPanel5
            // 
            tableLayoutPanel5.AutoSize = true;
            tableLayoutPanel5.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel5.ColumnCount = 1;
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel5.Controls.Add(tableLayoutPanel4, 0, 1);
            tableLayoutPanel5.Dock = DockStyle.Fill;
            tableLayoutPanel5.Location = new Point(625, 0);
            tableLayoutPanel5.Margin = new Padding(0);
            tableLayoutPanel5.Name = "tableLayoutPanel5";
            tableLayoutPanel5.RowCount = 3;
            tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 22.2222214F));
            tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 55.5555573F));
            tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 22.2222214F));
            tableLayoutPanel5.Size = new Size(687, 104);
            tableLayoutPanel5.TabIndex = 2;
            // 
            // tableLayoutPanel4
            // 
            tableLayoutPanel4.AutoSize = true;
            tableLayoutPanel4.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel4.ColumnCount = 4;
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel4.Controls.Add(btnOpenExceptionFolder, 3, 0);
            tableLayoutPanel4.Controls.Add(dtpExceptionDate, 1, 0);
            tableLayoutPanel4.Controls.Add(lblExceptionDate, 0, 0);
            tableLayoutPanel4.Controls.Add(queryExceptionLogs, 2, 0);
            tableLayoutPanel4.Dock = DockStyle.Fill;
            tableLayoutPanel4.Location = new Point(3, 26);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 1;
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Size = new Size(681, 51);
            tableLayoutPanel4.TabIndex = 1;
            // 
            // btnOpenExceptionFolder
            // 
            btnOpenExceptionFolder.AutoSizeMode = TAutoSize.Width;
            btnOpenExceptionFolder.BorderWidth = 1F;
            btnOpenExceptionFolder.Dock = DockStyle.Fill;
            btnOpenExceptionFolder.IconSvg = "FolderOpenOutlined";
            btnOpenExceptionFolder.Location = new Point(522, 0);
            btnOpenExceptionFolder.Margin = new Padding(0);
            btnOpenExceptionFolder.Name = "btnOpenExceptionFolder";
            btnOpenExceptionFolder.Size = new Size(159, 51);
            btnOpenExceptionFolder.TabIndex = 5;
            btnOpenExceptionFolder.Tag = "perm:button.log.open-folder:enabled";
            btnOpenExceptionFolder.Text = "Open Folder";
            // 
            // dtpExceptionDate
            // 
            dtpExceptionDate.Dock = DockStyle.Fill;
            dtpExceptionDate.Location = new Point(40, 0);
            dtpExceptionDate.Margin = new Padding(0);
            dtpExceptionDate.Name = "dtpExceptionDate";
            dtpExceptionDate.Size = new Size(159, 51);
            dtpExceptionDate.TabIndex = 1;
            // 
            // lblExceptionDate
            // 
            lblExceptionDate.AutoSizeMode = TAutoSize.Width;
            lblExceptionDate.Dock = DockStyle.Fill;
            lblExceptionDate.Location = new Point(0, 0);
            lblExceptionDate.Margin = new Padding(0);
            lblExceptionDate.Name = "lblExceptionDate";
            lblExceptionDate.Size = new Size(40, 51);
            lblExceptionDate.TabIndex = 0;
            lblExceptionDate.Text = "Date";
            // 
            // queryExceptionLogs
            // 
            queryExceptionLogs.AutoSize = true;
            queryExceptionLogs.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            queryExceptionLogs.Dock = DockStyle.Fill;
            queryExceptionLogs.Location = new Point(199, 0);
            queryExceptionLogs.Margin = new Padding(0);
            queryExceptionLogs.MinimumSize = new Size(100, 40);
            queryExceptionLogs.Name = "queryExceptionLogs";
            queryExceptionLogs.QueryChanged = null;
            queryExceptionLogs.Size = new Size(323, 51);
            queryExceptionLogs.TabIndex = 6;
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
            exceptionTitleLayout.Size = new Size(625, 104);
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
            lblExceptionTitle.Size = new Size(625, 34);
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
            lblExceptionDescription.Size = new Size(625, 70);
            lblExceptionDescription.TabIndex = 1;
            lblExceptionDescription.Text = "Program Exceptions details";
            lblExceptionDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // splitExceptionContent
            // 
            splitExceptionContent.Dock = DockStyle.Fill;
            splitExceptionContent.Location = new Point(20, 126);
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
            splitExceptionContent.Size = new Size(1312, 559);
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
            dgvExceptionLogs.Size = new Size(748, 559);
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
            exceptionDetailsLayout.Size = new Size(535, 559);
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
            btnOpenExceptionSource.AutoSizeMode = TAutoSize.Width;
            btnOpenExceptionSource.BorderWidth = 1F;
            btnOpenExceptionSource.IconSvg = "FileSearchOutlined";
            btnOpenExceptionSource.Location = new Point(3, 3);
            btnOpenExceptionSource.Name = "btnOpenExceptionSource";
            btnOpenExceptionSource.Size = new Size(163, 40);
            btnOpenExceptionSource.TabIndex = 0;
            btnOpenExceptionSource.Tag = "perm:button.log.open-source:enabled";
            btnOpenExceptionSource.Text = "Open Source";
            // 
            // btnCopyExceptionDetails
            // 
            btnCopyExceptionDetails.AutoSizeMode = TAutoSize.Width;
            btnCopyExceptionDetails.BorderWidth = 1F;
            btnCopyExceptionDetails.IconSvg = "CopyOutlined";
            btnCopyExceptionDetails.Location = new Point(172, 3);
            btnCopyExceptionDetails.Name = "btnCopyExceptionDetails";
            btnCopyExceptionDetails.Size = new Size(98, 40);
            btnCopyExceptionDetails.TabIndex = 1;
            btnCopyExceptionDetails.Tag = "perm:button.log.copy-details:enabled";
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
            tabExceptionDetails.Size = new Size(529, 499);
            tabExceptionDetails.TabIndex = 1;
            // 
            // tabExceptionBasicInfo
            // 
            tabExceptionBasicInfo.Controls.Add(txtExceptionBasicInfo);
            tabExceptionBasicInfo.Location = new Point(4, 32);
            tabExceptionBasicInfo.Name = "tabExceptionBasicInfo";
            tabExceptionBasicInfo.Padding = new Padding(3);
            tabExceptionBasicInfo.Size = new Size(521, 463);
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
            txtExceptionBasicInfo.Size = new Size(515, 457);
            txtExceptionBasicInfo.TabIndex = 0;
            txtExceptionBasicInfo.WordWrap = false;
            // 
            // tabExceptionStackTrace
            // 
            tabExceptionStackTrace.Controls.Add(txtExceptionStackTrace);
            tabExceptionStackTrace.Location = new Point(4, 29);
            tabExceptionStackTrace.Name = "tabExceptionStackTrace";
            tabExceptionStackTrace.Padding = new Padding(3);
            tabExceptionStackTrace.Size = new Size(521, 466);
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
            txtExceptionStackTrace.Size = new Size(515, 460);
            txtExceptionStackTrace.TabIndex = 0;
            txtExceptionStackTrace.WordWrap = false;
            // 
            // tabExceptionContext
            // 
            tabExceptionContext.Controls.Add(txtExceptionContext);
            tabExceptionContext.Location = new Point(4, 29);
            tabExceptionContext.Name = "tabExceptionContext";
            tabExceptionContext.Padding = new Padding(3);
            tabExceptionContext.Size = new Size(521, 466);
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
            txtExceptionContext.Size = new Size(515, 460);
            txtExceptionContext.TabIndex = 0;
            txtExceptionContext.WordWrap = false;
            // 
            // tabDeviceLifecycleLogs
            // 
            tabDeviceLifecycleLogs.Controls.Add(deviceLifecycleRootLayout);
            tabDeviceLifecycleLogs.Location = new Point(4, 32);
            tabDeviceLifecycleLogs.Name = "tabDeviceLifecycleLogs";
            tabDeviceLifecycleLogs.Padding = new Padding(3);
            tabDeviceLifecycleLogs.Size = new Size(1358, 709);
            tabDeviceLifecycleLogs.TabIndex = 3;
            tabDeviceLifecycleLogs.Text = "Device Logs";
            tabDeviceLifecycleLogs.UseVisualStyleBackColor = true;
            // 
            // deviceLifecycleRootLayout
            // 
            deviceLifecycleRootLayout.ColumnCount = 1;
            deviceLifecycleRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            deviceLifecycleRootLayout.Controls.Add(deviceLifecycleHeaderLayout, 0, 0);
            deviceLifecycleRootLayout.Controls.Add(splitDeviceLifecycleContent, 0, 1);
            deviceLifecycleRootLayout.Dock = DockStyle.Fill;
            deviceLifecycleRootLayout.Location = new Point(3, 3);
            deviceLifecycleRootLayout.Name = "deviceLifecycleRootLayout";
            deviceLifecycleRootLayout.RowCount = 2;
            deviceLifecycleRootLayout.RowStyles.Add(new RowStyle());
            deviceLifecycleRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            deviceLifecycleRootLayout.Size = new Size(1352, 703);
            deviceLifecycleRootLayout.TabIndex = 0;
            // 
            // deviceLifecycleHeaderLayout
            // 
            deviceLifecycleHeaderLayout.ColumnCount = 2;
            deviceLifecycleHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            deviceLifecycleHeaderLayout.ColumnStyles.Add(new ColumnStyle());
            deviceLifecycleHeaderLayout.Controls.Add(tableLayoutPanel6, 1, 0);
            deviceLifecycleHeaderLayout.Controls.Add(deviceLifecycleTitleLayout, 0, 0);
            deviceLifecycleHeaderLayout.Dock = DockStyle.Fill;
            deviceLifecycleHeaderLayout.Location = new Point(20, 14);
            deviceLifecycleHeaderLayout.Margin = new Padding(20, 14, 20, 8);
            deviceLifecycleHeaderLayout.Name = "deviceLifecycleHeaderLayout";
            deviceLifecycleHeaderLayout.RowCount = 1;
            deviceLifecycleHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            deviceLifecycleHeaderLayout.Size = new Size(1312, 105);
            deviceLifecycleHeaderLayout.TabIndex = 0;
            // 
            // tableLayoutPanel6
            // 
            tableLayoutPanel6.AutoSize = true;
            tableLayoutPanel6.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel6.ColumnCount = 1;
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel6.Controls.Add(deviceLifecycleToolbar, 0, 1);
            tableLayoutPanel6.Dock = DockStyle.Fill;
            tableLayoutPanel6.Location = new Point(692, 0);
            tableLayoutPanel6.Margin = new Padding(0);
            tableLayoutPanel6.Name = "tableLayoutPanel6";
            tableLayoutPanel6.RowCount = 3;
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 22.2222214F));
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 55.5555573F));
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 22.2222214F));
            tableLayoutPanel6.Size = new Size(620, 105);
            tableLayoutPanel6.TabIndex = 1;
            // 
            // deviceLifecycleToolbar
            // 
            deviceLifecycleToolbar.AutoSize = true;
            deviceLifecycleToolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            deviceLifecycleToolbar.ColumnCount = 4;
            deviceLifecycleToolbar.ColumnStyles.Add(new ColumnStyle());
            deviceLifecycleToolbar.ColumnStyles.Add(new ColumnStyle());
            deviceLifecycleToolbar.ColumnStyles.Add(new ColumnStyle());
            deviceLifecycleToolbar.ColumnStyles.Add(new ColumnStyle());
            deviceLifecycleToolbar.Controls.Add(btnOpenDeviceLifecycleFolder, 3, 0);
            deviceLifecycleToolbar.Controls.Add(dtpDeviceLifecycleDate, 1, 0);
            deviceLifecycleToolbar.Controls.Add(lblDeviceLifecycleDate, 0, 0);
            deviceLifecycleToolbar.Controls.Add(queryDeviceLifecycleLogs, 2, 0);
            deviceLifecycleToolbar.Dock = DockStyle.Fill;
            deviceLifecycleToolbar.Location = new Point(3, 26);
            deviceLifecycleToolbar.Name = "deviceLifecycleToolbar";
            deviceLifecycleToolbar.RowCount = 1;
            deviceLifecycleToolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            deviceLifecycleToolbar.Size = new Size(614, 52);
            deviceLifecycleToolbar.TabIndex = 2;
            // 
            // btnOpenDeviceLifecycleFolder
            // 
            btnOpenDeviceLifecycleFolder.BorderWidth = 1F;
            btnOpenDeviceLifecycleFolder.Dock = DockStyle.Fill;
            btnOpenDeviceLifecycleFolder.IconSvg = "FolderOpenOutlined";
            btnOpenDeviceLifecycleFolder.Location = new Point(513, 0);
            btnOpenDeviceLifecycleFolder.Margin = new Padding(0);
            btnOpenDeviceLifecycleFolder.Name = "btnOpenDeviceLifecycleFolder";
            btnOpenDeviceLifecycleFolder.Size = new Size(101, 52);
            btnOpenDeviceLifecycleFolder.TabIndex = 5;
            btnOpenDeviceLifecycleFolder.Tag = "perm:button.log.open-folder:enabled";
            btnOpenDeviceLifecycleFolder.Text = "Open";
            // 
            // dtpDeviceLifecycleDate
            // 
            dtpDeviceLifecycleDate.Dock = DockStyle.Fill;
            dtpDeviceLifecycleDate.Location = new Point(40, 0);
            dtpDeviceLifecycleDate.Margin = new Padding(0);
            dtpDeviceLifecycleDate.Name = "dtpDeviceLifecycleDate";
            dtpDeviceLifecycleDate.Size = new Size(150, 52);
            dtpDeviceLifecycleDate.TabIndex = 1;
            // 
            // lblDeviceLifecycleDate
            // 
            lblDeviceLifecycleDate.AutoSizeMode = TAutoSize.Width;
            lblDeviceLifecycleDate.Dock = DockStyle.Fill;
            lblDeviceLifecycleDate.Location = new Point(0, 0);
            lblDeviceLifecycleDate.Margin = new Padding(0);
            lblDeviceLifecycleDate.Name = "lblDeviceLifecycleDate";
            lblDeviceLifecycleDate.Size = new Size(40, 52);
            lblDeviceLifecycleDate.TabIndex = 0;
            lblDeviceLifecycleDate.Text = "Date";
            // 
            // queryDeviceLifecycleLogs
            // 
            queryDeviceLifecycleLogs.AutoSize = true;
            queryDeviceLifecycleLogs.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            queryDeviceLifecycleLogs.Dock = DockStyle.Fill;
            queryDeviceLifecycleLogs.Location = new Point(190, 0);
            queryDeviceLifecycleLogs.Margin = new Padding(0);
            queryDeviceLifecycleLogs.MinimumSize = new Size(100, 40);
            queryDeviceLifecycleLogs.Name = "queryDeviceLifecycleLogs";
            queryDeviceLifecycleLogs.QueryChanged = null;
            queryDeviceLifecycleLogs.Size = new Size(323, 52);
            queryDeviceLifecycleLogs.TabIndex = 6;
            // 
            // deviceLifecycleTitleLayout
            // 
            deviceLifecycleTitleLayout.ColumnCount = 1;
            deviceLifecycleTitleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            deviceLifecycleTitleLayout.Controls.Add(lblDeviceLifecycleTitle, 0, 0);
            deviceLifecycleTitleLayout.Controls.Add(lblDeviceLifecycleDescription, 0, 1);
            deviceLifecycleTitleLayout.Dock = DockStyle.Fill;
            deviceLifecycleTitleLayout.Location = new Point(0, 0);
            deviceLifecycleTitleLayout.Margin = new Padding(0);
            deviceLifecycleTitleLayout.Name = "deviceLifecycleTitleLayout";
            deviceLifecycleTitleLayout.RowCount = 2;
            deviceLifecycleTitleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            deviceLifecycleTitleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            deviceLifecycleTitleLayout.Size = new Size(692, 105);
            deviceLifecycleTitleLayout.TabIndex = 0;
            // 
            // lblDeviceLifecycleTitle
            // 
            lblDeviceLifecycleTitle.AutoSize = true;
            lblDeviceLifecycleTitle.Dock = DockStyle.Fill;
            lblDeviceLifecycleTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            lblDeviceLifecycleTitle.Location = new Point(0, 0);
            lblDeviceLifecycleTitle.Margin = new Padding(0);
            lblDeviceLifecycleTitle.Name = "lblDeviceLifecycleTitle";
            lblDeviceLifecycleTitle.Size = new Size(692, 34);
            lblDeviceLifecycleTitle.TabIndex = 0;
            lblDeviceLifecycleTitle.Text = "Device Logs";
            lblDeviceLifecycleTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblDeviceLifecycleDescription
            // 
            lblDeviceLifecycleDescription.AutoEllipsis = true;
            lblDeviceLifecycleDescription.Dock = DockStyle.Fill;
            lblDeviceLifecycleDescription.ForeColor = SystemColors.GrayText;
            lblDeviceLifecycleDescription.Location = new Point(0, 34);
            lblDeviceLifecycleDescription.Margin = new Padding(0);
            lblDeviceLifecycleDescription.Name = "lblDeviceLifecycleDescription";
            lblDeviceLifecycleDescription.Size = new Size(692, 71);
            lblDeviceLifecycleDescription.TabIndex = 1;
            lblDeviceLifecycleDescription.Text = "Device lifecycle events";
            lblDeviceLifecycleDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // splitDeviceLifecycleContent
            // 
            splitDeviceLifecycleContent.Dock = DockStyle.Fill;
            splitDeviceLifecycleContent.Location = new Point(20, 127);
            splitDeviceLifecycleContent.Margin = new Padding(20, 0, 20, 18);
            splitDeviceLifecycleContent.Name = "splitDeviceLifecycleContent";
            // 
            // splitDeviceLifecycleContent.Panel1
            // 
            splitDeviceLifecycleContent.Panel1.Controls.Add(dgvDeviceLifecycleLogs);
            splitDeviceLifecycleContent.Panel1.Padding = new Padding(0, 0, 12, 0);
            // 
            // splitDeviceLifecycleContent.Panel2
            // 
            splitDeviceLifecycleContent.Panel2.Controls.Add(txtDeviceLifecycleDetail);
            splitDeviceLifecycleContent.Panel2.Padding = new Padding(12, 0, 0, 0);
            splitDeviceLifecycleContent.Size = new Size(1312, 558);
            splitDeviceLifecycleContent.SplitterDistance = 820;
            splitDeviceLifecycleContent.SplitterWidth = 5;
            splitDeviceLifecycleContent.TabIndex = 1;
            // 
            // dgvDeviceLifecycleLogs
            // 
            dgvDeviceLifecycleLogs.AllowUserToAddRows = false;
            dgvDeviceLifecycleLogs.AllowUserToDeleteRows = false;
            dgvDeviceLifecycleLogs.BackgroundColor = SystemColors.Window;
            dgvDeviceLifecycleLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDeviceLifecycleLogs.Columns.AddRange(new DataGridViewColumn[] { colLifecycleOccurredTime, colLifecycleLevel, colLifecycleEventType, colLifecycleStation, colLifecycleStatus, colLifecycleSummary });
            dgvDeviceLifecycleLogs.Dock = DockStyle.Fill;
            dgvDeviceLifecycleLogs.Location = new Point(0, 0);
            dgvDeviceLifecycleLogs.MultiSelect = false;
            dgvDeviceLifecycleLogs.Name = "dgvDeviceLifecycleLogs";
            dgvDeviceLifecycleLogs.ReadOnly = true;
            dgvDeviceLifecycleLogs.RowHeadersVisible = false;
            dgvDeviceLifecycleLogs.RowHeadersWidth = 51;
            dgvDeviceLifecycleLogs.RowTemplate.Height = 28;
            dgvDeviceLifecycleLogs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDeviceLifecycleLogs.Size = new Size(808, 558);
            dgvDeviceLifecycleLogs.TabIndex = 0;
            // 
            // colLifecycleOccurredTime
            // 
            colLifecycleOccurredTime.DataPropertyName = "OccurredTime";
            colLifecycleOccurredTime.HeaderText = "Time";
            colLifecycleOccurredTime.MinimumWidth = 150;
            colLifecycleOccurredTime.Name = "colLifecycleOccurredTime";
            colLifecycleOccurredTime.ReadOnly = true;
            colLifecycleOccurredTime.Width = 170;
            // 
            // colLifecycleLevel
            // 
            colLifecycleLevel.DataPropertyName = "Level";
            colLifecycleLevel.HeaderText = "Level";
            colLifecycleLevel.MinimumWidth = 70;
            colLifecycleLevel.Name = "colLifecycleLevel";
            colLifecycleLevel.ReadOnly = true;
            colLifecycleLevel.Width = 80;
            // 
            // colLifecycleEventType
            // 
            colLifecycleEventType.DataPropertyName = "EventType";
            colLifecycleEventType.HeaderText = "Event";
            colLifecycleEventType.MinimumWidth = 120;
            colLifecycleEventType.Name = "colLifecycleEventType";
            colLifecycleEventType.ReadOnly = true;
            colLifecycleEventType.Width = 150;
            // 
            // colLifecycleStation
            // 
            colLifecycleStation.DataPropertyName = "Station";
            colLifecycleStation.HeaderText = "Station";
            colLifecycleStation.MinimumWidth = 70;
            colLifecycleStation.Name = "colLifecycleStation";
            colLifecycleStation.ReadOnly = true;
            colLifecycleStation.Width = 80;
            // 
            // colLifecycleStatus
            // 
            colLifecycleStatus.DataPropertyName = "Status";
            colLifecycleStatus.HeaderText = "Status";
            colLifecycleStatus.MinimumWidth = 80;
            colLifecycleStatus.Name = "colLifecycleStatus";
            colLifecycleStatus.ReadOnly = true;
            colLifecycleStatus.Width = 90;
            // 
            // colLifecycleSummary
            // 
            colLifecycleSummary.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colLifecycleSummary.DataPropertyName = "Summary";
            colLifecycleSummary.HeaderText = "Summary";
            colLifecycleSummary.MinimumWidth = 160;
            colLifecycleSummary.Name = "colLifecycleSummary";
            colLifecycleSummary.ReadOnly = true;
            // 
            // txtDeviceLifecycleDetail
            // 
            txtDeviceLifecycleDetail.BackColor = SystemColors.Window;
            txtDeviceLifecycleDetail.BorderStyle = BorderStyle.FixedSingle;
            txtDeviceLifecycleDetail.Dock = DockStyle.Fill;
            txtDeviceLifecycleDetail.Font = new Font("Consolas", 10F);
            txtDeviceLifecycleDetail.Location = new Point(12, 0);
            txtDeviceLifecycleDetail.Multiline = true;
            txtDeviceLifecycleDetail.Name = "txtDeviceLifecycleDetail";
            txtDeviceLifecycleDetail.ReadOnly = true;
            txtDeviceLifecycleDetail.ScrollBars = ScrollBars.Both;
            txtDeviceLifecycleDetail.Size = new Size(475, 558);
            txtDeviceLifecycleDetail.TabIndex = 0;
            txtDeviceLifecycleDetail.WordWrap = false;
            // 
            // tabDeviceStatusLogs
            // 
            tabDeviceStatusLogs.Controls.Add(deviceStatusRootLayout);
            tabDeviceStatusLogs.Location = new Point(4, 32);
            tabDeviceStatusLogs.Name = "tabDeviceStatusLogs";
            tabDeviceStatusLogs.Padding = new Padding(3);
            tabDeviceStatusLogs.Size = new Size(1358, 709);
            tabDeviceStatusLogs.TabIndex = 4;
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
            deviceStatusRootLayout.Size = new Size(1352, 703);
            deviceStatusRootLayout.TabIndex = 0;
            // 
            // deviceStatusHeaderLayout
            // 
            deviceStatusHeaderLayout.ColumnCount = 2;
            deviceStatusHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            deviceStatusHeaderLayout.ColumnStyles.Add(new ColumnStyle());
            deviceStatusHeaderLayout.Controls.Add(tableLayoutPanel7, 1, 0);
            deviceStatusHeaderLayout.Controls.Add(deviceStatusTitleLayout, 0, 0);
            deviceStatusHeaderLayout.Dock = DockStyle.Fill;
            deviceStatusHeaderLayout.Location = new Point(20, 14);
            deviceStatusHeaderLayout.Margin = new Padding(20, 14, 20, 8);
            deviceStatusHeaderLayout.Name = "deviceStatusHeaderLayout";
            deviceStatusHeaderLayout.RowCount = 1;
            deviceStatusHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            deviceStatusHeaderLayout.Size = new Size(1312, 104);
            deviceStatusHeaderLayout.TabIndex = 0;
            // 
            // tableLayoutPanel7
            // 
            tableLayoutPanel7.AutoSize = true;
            tableLayoutPanel7.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel7.ColumnCount = 1;
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel7.Controls.Add(deviceStatusToolbar, 0, 1);
            tableLayoutPanel7.Dock = DockStyle.Fill;
            tableLayoutPanel7.Location = new Point(808, 0);
            tableLayoutPanel7.Margin = new Padding(0);
            tableLayoutPanel7.Name = "tableLayoutPanel7";
            tableLayoutPanel7.RowCount = 3;
            tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Percent, 22.2222214F));
            tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Percent, 55.5555573F));
            tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Percent, 22.2222214F));
            tableLayoutPanel7.Size = new Size(504, 104);
            tableLayoutPanel7.TabIndex = 1;
            // 
            // deviceStatusToolbar
            // 
            deviceStatusToolbar.AutoSize = true;
            deviceStatusToolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            deviceStatusToolbar.ColumnCount = 3;
            deviceStatusToolbar.ColumnStyles.Add(new ColumnStyle());
            deviceStatusToolbar.ColumnStyles.Add(new ColumnStyle());
            deviceStatusToolbar.ColumnStyles.Add(new ColumnStyle());
            deviceStatusToolbar.Controls.Add(dtpDeviceStatusDate, 1, 0);
            deviceStatusToolbar.Controls.Add(lblDeviceStatusDate, 0, 0);
            deviceStatusToolbar.Controls.Add(queryDeviceStatusLogs, 2, 0);
            deviceStatusToolbar.Dock = DockStyle.Fill;
            deviceStatusToolbar.Location = new Point(3, 26);
            deviceStatusToolbar.Name = "deviceStatusToolbar";
            deviceStatusToolbar.RowCount = 1;
            deviceStatusToolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            deviceStatusToolbar.Size = new Size(498, 51);
            deviceStatusToolbar.TabIndex = 1;
            // 
            // dtpDeviceStatusDate
            // 
            dtpDeviceStatusDate.Dock = DockStyle.Fill;
            dtpDeviceStatusDate.Location = new Point(40, 0);
            dtpDeviceStatusDate.Margin = new Padding(0);
            dtpDeviceStatusDate.Name = "dtpDeviceStatusDate";
            dtpDeviceStatusDate.Size = new Size(135, 51);
            dtpDeviceStatusDate.TabIndex = 1;
            // 
            // lblDeviceStatusDate
            // 
            lblDeviceStatusDate.AutoSizeMode = TAutoSize.Width;
            lblDeviceStatusDate.Dock = DockStyle.Fill;
            lblDeviceStatusDate.Location = new Point(0, 0);
            lblDeviceStatusDate.Margin = new Padding(0);
            lblDeviceStatusDate.Name = "lblDeviceStatusDate";
            lblDeviceStatusDate.Size = new Size(40, 51);
            lblDeviceStatusDate.TabIndex = 0;
            lblDeviceStatusDate.Text = "Date";
            // 
            // queryDeviceStatusLogs
            // 
            queryDeviceStatusLogs.AutoSize = true;
            queryDeviceStatusLogs.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            queryDeviceStatusLogs.Dock = DockStyle.Fill;
            queryDeviceStatusLogs.Location = new Point(175, 0);
            queryDeviceStatusLogs.Margin = new Padding(0);
            queryDeviceStatusLogs.MinimumSize = new Size(100, 40);
            queryDeviceStatusLogs.Name = "queryDeviceStatusLogs";
            queryDeviceStatusLogs.QueryChanged = null;
            queryDeviceStatusLogs.Size = new Size(323, 51);
            queryDeviceStatusLogs.TabIndex = 2;
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
            deviceStatusTitleLayout.Size = new Size(808, 104);
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
            lblDeviceStatusTitle.Size = new Size(808, 34);
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
            lblDeviceStatusDescription.Size = new Size(808, 70);
            lblDeviceStatusDescription.TabIndex = 1;
            lblDeviceStatusDescription.Text = "Device Status details";
            lblDeviceStatusDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // splitDeviceStatusContent
            // 
            splitDeviceStatusContent.Dock = DockStyle.Fill;
            splitDeviceStatusContent.Location = new Point(20, 126);
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
            splitDeviceStatusContent.Size = new Size(1312, 559);
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
            dgvDeviceStatusLogs.Columns.AddRange(new DataGridViewColumn[] { colDeviceOccurredTime, colDeviceStation, colDeviceStatus, colDeviceStatusName, colDeviceSource, colDeviceReportStatus, colDeviceReportMessage });
            dgvDeviceStatusLogs.Dock = DockStyle.Fill;
            dgvDeviceStatusLogs.Location = new Point(0, 0);
            dgvDeviceStatusLogs.MultiSelect = false;
            dgvDeviceStatusLogs.Name = "dgvDeviceStatusLogs";
            dgvDeviceStatusLogs.ReadOnly = true;
            dgvDeviceStatusLogs.RowHeadersVisible = false;
            dgvDeviceStatusLogs.RowHeadersWidth = 51;
            dgvDeviceStatusLogs.RowTemplate.Height = 28;
            dgvDeviceStatusLogs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDeviceStatusLogs.Size = new Size(808, 559);
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
            colDeviceStatus.DataPropertyName = "DeviceStatus";
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
            txtDeviceStatusDetail.Size = new Size(475, 559);
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
            mesInteractionHeaderLayout.ResumeLayout(false);
            mesInteractionHeaderLayout.PerformLayout();
            tableLayoutPanel9.ResumeLayout(false);
            tableLayoutPanel9.PerformLayout();
            tableLayoutPanel10.ResumeLayout(false);
            tableLayoutPanel10.PerformLayout();
            tableLayoutPanel11.ResumeLayout(false);
            tableLayoutPanel11.PerformLayout();
            splitterMesContent.Panel1.ResumeLayout(false);
            splitterMesContent.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitterMesContent).EndInit();
            splitterMesContent.ResumeLayout(false);
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
            splitterProductionContent.Panel1.ResumeLayout(false);
            splitterProductionContent.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitterProductionContent).EndInit();
            splitterProductionContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvProductionLogs).EndInit();
            tabProductionDetails.ResumeLayout(false);
            tabProductionBasicInfo.ResumeLayout(false);
            tabProductionBasicInfo.PerformLayout();
            tabProductionDetail.ResumeLayout(false);
            tabProductionDetail.PerformLayout();
            productionHeaderLayout.ResumeLayout(false);
            productionHeaderLayout.PerformLayout();
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel3.PerformLayout();
            tlpProductionToolbar.ResumeLayout(false);
            tlpProductionToolbar.PerformLayout();
            productionTitleLayout.ResumeLayout(false);
            productionTitleLayout.PerformLayout();
            tabExceptionLogs.ResumeLayout(false);
            exceptionRootLayout.ResumeLayout(false);
            exceptionHeaderLayout.ResumeLayout(false);
            exceptionHeaderLayout.PerformLayout();
            tableLayoutPanel5.ResumeLayout(false);
            tableLayoutPanel5.PerformLayout();
            tableLayoutPanel4.ResumeLayout(false);
            tableLayoutPanel4.PerformLayout();
            exceptionTitleLayout.ResumeLayout(false);
            exceptionTitleLayout.PerformLayout();
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
            tabDeviceLifecycleLogs.ResumeLayout(false);
            deviceLifecycleRootLayout.ResumeLayout(false);
            deviceLifecycleHeaderLayout.ResumeLayout(false);
            deviceLifecycleHeaderLayout.PerformLayout();
            tableLayoutPanel6.ResumeLayout(false);
            tableLayoutPanel6.PerformLayout();
            deviceLifecycleToolbar.ResumeLayout(false);
            deviceLifecycleToolbar.PerformLayout();
            deviceLifecycleTitleLayout.ResumeLayout(false);
            deviceLifecycleTitleLayout.PerformLayout();
            splitDeviceLifecycleContent.Panel1.ResumeLayout(false);
            splitDeviceLifecycleContent.Panel2.ResumeLayout(false);
            splitDeviceLifecycleContent.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitDeviceLifecycleContent).EndInit();
            splitDeviceLifecycleContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvDeviceLifecycleLogs).EndInit();
            tabDeviceStatusLogs.ResumeLayout(false);
            deviceStatusRootLayout.ResumeLayout(false);
            deviceStatusHeaderLayout.ResumeLayout(false);
            deviceStatusHeaderLayout.PerformLayout();
            tableLayoutPanel7.ResumeLayout(false);
            tableLayoutPanel7.PerformLayout();
            deviceStatusToolbar.ResumeLayout(false);
            deviceStatusToolbar.PerformLayout();
            deviceStatusTitleLayout.ResumeLayout(false);
            deviceStatusTitleLayout.PerformLayout();
            splitDeviceStatusContent.Panel1.ResumeLayout(false);
            splitDeviceStatusContent.Panel2.ResumeLayout(false);
            splitDeviceStatusContent.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitDeviceStatusContent).EndInit();
            splitDeviceStatusContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvDeviceStatusLogs).EndInit();
            ResumeLayout(false);
        }

        private TabControl tabLogCategories;
        private TabControl tabMesDetails;
        private TabControl tabProductionDetails;
        private TabControl tabExceptionDetails;

        private TabPage tabMesLogs;
        private TabPage tabProductionLogs;
        private TabPage tabExceptionLogs;
        private TabPage tabDeviceLifecycleLogs;
        private TabPage tabDeviceStatusLogs;
        private TabPage tabBasicInfo;
        private TabPage tabProductionBasicInfo;
        private TabPage tabRequestBody;
        private TabPage tabResponseBody;
        private TabPage tabProductionDetail;
        private TabPage tabExceptionBasicInfo;
        private TabPage tabExceptionStackTrace;
        private TabPage tabExceptionContext;

        private TableLayoutPanel mesRootLayout;
        private TableLayoutPanel productionRootLayout;
        private TableLayoutPanel productionHeaderLayout;
        private TableLayoutPanel productionTitleLayout;
        private TableLayoutPanel exceptionRootLayout;
        private TableLayoutPanel deviceLifecycleRootLayout;
        private TableLayoutPanel deviceLifecycleHeaderLayout;
        private TableLayoutPanel deviceLifecycleTitleLayout;
        private TableLayoutPanel tlpProductionToolbar;
        private TableLayoutPanel tableLayoutPanel3;
        private TableLayoutPanel tableLayoutPanel4;
        private TableLayoutPanel tableLayoutPanel5;
        private TableLayoutPanel exceptionHeaderLayout;
        private TableLayoutPanel exceptionTitleLayout;
        private TableLayoutPanel deviceLifecycleToolbar;
        private TableLayoutPanel tableLayoutPanel6;
        private TableLayoutPanel deviceStatusToolbar;
        private TableLayoutPanel tableLayoutPanel7;
        private TableLayoutPanel mesInteractionHeaderLayout;
        private TableLayoutPanel tableLayoutPanel9;
        private TableLayoutPanel tableLayoutPanel10;
        private TableLayoutPanel tableLayoutPanel11;
        private TableLayoutPanel deviceStatusRootLayout;
        private TableLayoutPanel deviceStatusHeaderLayout;
        private TableLayoutPanel deviceStatusTitleLayout;
        private TableLayoutPanel exceptionDetailsLayout;

        private AntdUI.Splitter splitterMesContent;
        private AntdUI.Splitter splitterProductionContent;

        private Controls.InputQuery queryProductionLogs;
        private Controls.InputQuery queryMesLogs;
        private Controls.InputQuery queryExceptionLogs;
        private Controls.InputQuery queryDeviceLifecycleLogs;
        private Controls.InputQuery queryDeviceStatusLogs;

        private DataGridView dgvDeviceStatusLogs;
        private DataGridView dgvMesLogs;
        private DataGridView dgvProductionLogs;
        private DataGridView dgvExceptionLogs;
        private DataGridView dgvDeviceLifecycleLogs;

        private AntdUI.DatePicker dtpProductionDate;
        private AntdUI.DatePicker dtpExceptionDate;
        private AntdUI.DatePicker dtpDeviceLifecycleDate;
        private AntdUI.DatePicker dtpDeviceStatusDate;
        private AntdUI.DatePicker dtpMesDate;

        private Label lblMesTitle;
        private Label lblMesDescription;
        private Label lblExceptionTitle;
        private Label lblExceptionDescription;
        private Label lblProductionTitle;
        private Label lblProductionDescription;
        private Label lblDeviceLifecycleTitle;
        private Label lblDeviceLifecycleDescription;
        private Label lblDeviceStatusTitle;
        private Label lblDeviceStatusDescription;

        private AntdUI.Label lblMesDate;
        private AntdUI.Label lblDeviceLifecycleDate;
        private AntdUI.Label lblProductionDate;
        private AntdUI.Label lblExceptionDate;
        private AntdUI.Label lblDeviceStatusDate;

        private AntdUI.Button btnOpenMesFolder;
        private AntdUI.Button btnOpenProductionFolder;
        private AntdUI.Button btnOpenExceptionFolder;
        private AntdUI.Button btnOpenDeviceLifecycleFolder;
        private AntdUI.Button btnOpenExceptionSource;
        private AntdUI.Button btnCopyExceptionDetails;

        private SplitContainer splitExceptionContent;
        private SplitContainer splitDeviceLifecycleContent;
        private SplitContainer splitDeviceStatusContent;

        private TextBox txtDeviceLifecycleDetail;
        private TextBox txtBasicInfo;
        private TextBox txtRequestBody;
        private TextBox txtResponseBody;
        private TextBox txtProductionBasicInfo;
        private TextBox txtProductionDetail;
        private TextBox txtExceptionBasicInfo;
        private TextBox txtExceptionStackTrace;
        private TextBox txtExceptionContext;
        private TextBox txtDeviceStatusDetail;
        
        private FlowLayoutPanel exceptionDetailToolbar;
        
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
        private DataGridViewTextBoxColumn colLifecycleOccurredTime;
        private DataGridViewTextBoxColumn colLifecycleLevel;
        private DataGridViewTextBoxColumn colLifecycleEventType;
        private DataGridViewTextBoxColumn colLifecycleStation;
        private DataGridViewTextBoxColumn colLifecycleStatus;
        private DataGridViewTextBoxColumn colLifecycleSummary;
        private DataGridViewTextBoxColumn colDeviceOccurredTime;
        private DataGridViewTextBoxColumn colDeviceStation;
        private DataGridViewTextBoxColumn colDeviceStatus;
        private DataGridViewTextBoxColumn colDeviceStatusName;
        private DataGridViewTextBoxColumn colDeviceSource;
        private DataGridViewTextBoxColumn colDeviceReportStatus;
        private DataGridViewTextBoxColumn colDeviceReportMessage;
    }
}
