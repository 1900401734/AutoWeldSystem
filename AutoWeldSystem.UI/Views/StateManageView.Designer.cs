namespace AutoWeldSystem.UI.Views
{
    partial class StateManageView
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
            headerLayout = new TableLayoutPanel();
            titleLayout = new TableLayoutPanel();
            lblTitle = new Label();
            lblDescription = new Label();
            buttonFlow = new FlowLayoutPanel();
            btnRetrySelected = new AntdUI.Button();
            btnRetryAll = new AntdUI.Button();
            btnDeleteSelected = new AntdUI.Button();
            btnRefresh = new AntdUI.Button();
            tabUploadCategories = new TabControl();
            tabSummary = new TabPage();
            tabProcessParameters = new TabPage();
            tabStartReports = new TabPage();
            tabFinishReports = new TabPage();
            tabWorkOrderStatuses = new TabPage();
            tabDeviceStatuses = new TabPage();
            tabReportFiles = new TabPage();
            tabProgramFiles = new TabPage();
            lblSummary = new Label();
            dgvPending = new DataGridView();
            rootLayout.SuspendLayout();
            headerLayout.SuspendLayout();
            titleLayout.SuspendLayout();
            buttonFlow.SuspendLayout();
            tabUploadCategories.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPending).BeginInit();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(headerLayout, 0, 0);
            rootLayout.Controls.Add(tabUploadCategories, 0, 1);
            rootLayout.Controls.Add(lblSummary, 0, 2);
            rootLayout.Controls.Add(dgvPending, 0, 3);
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.RowCount = 4;
            rootLayout.RowStyles.Add(new RowStyle());
            rootLayout.RowStyles.Add(new RowStyle());
            rootLayout.RowStyles.Add(new RowStyle());
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.Size = new Size(1366, 745);
            rootLayout.TabIndex = 0;
            // 
            // headerLayout
            // 
            headerLayout.ColumnCount = 2;
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.ColumnStyles.Add(new ColumnStyle());
            headerLayout.Controls.Add(titleLayout, 0, 0);
            headerLayout.Controls.Add(buttonFlow, 1, 0);
            headerLayout.Dock = DockStyle.Fill;
            headerLayout.Location = new Point(20, 16);
            headerLayout.Margin = new Padding(20, 16, 20, 8);
            headerLayout.Name = "headerLayout";
            headerLayout.RowCount = 1;
            headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            headerLayout.Size = new Size(1326, 70);
            headerLayout.TabIndex = 0;
            // 
            // titleLayout
            // 
            titleLayout.ColumnCount = 1;
            titleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            titleLayout.Controls.Add(lblTitle, 0, 0);
            titleLayout.Controls.Add(lblDescription, 0, 1);
            titleLayout.Dock = DockStyle.Fill;
            titleLayout.Location = new Point(3, 3);
            titleLayout.Name = "titleLayout";
            titleLayout.RowCount = 2;
            titleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            titleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            titleLayout.Size = new Size(952, 64);
            titleLayout.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(3, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(946, 34);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "上传状态";
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblDescription
            // 
            lblDescription.AutoEllipsis = true;
            lblDescription.Dock = DockStyle.Fill;
            lblDescription.ForeColor = SystemColors.GrayText;
            lblDescription.Location = new Point(3, 34);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(946, 30);
            lblDescription.TabIndex = 1;
            lblDescription.Text = "查看尚未同步至 MES 的本地程序，支持断网恢复后手动重试。";
            lblDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // buttonFlow
            // 
            buttonFlow.AutoSize = true;
            buttonFlow.Controls.Add(btnRetrySelected);
            buttonFlow.Controls.Add(btnRetryAll);
            buttonFlow.Controls.Add(btnDeleteSelected);
            buttonFlow.Controls.Add(btnRefresh);
            buttonFlow.Dock = DockStyle.Right;
            buttonFlow.Location = new Point(961, 3);
            buttonFlow.Name = "buttonFlow";
            buttonFlow.Padding = new Padding(0, 7, 0, 0);
            buttonFlow.Size = new Size(362, 64);
            buttonFlow.TabIndex = 1;
            buttonFlow.WrapContents = false;
            // 
            // btnRetrySelected
            // 
            btnRetrySelected.BorderWidth = 1F;
            btnRetrySelected.IconSvg = "CloudUploadOutlined";
            btnRetrySelected.Location = new Point(0, 7);
            btnRetrySelected.Margin = new Padding(0, 0, 10, 0);
            btnRetrySelected.Name = "btnRetrySelected";
            btnRetrySelected.Size = new Size(122, 40);
            btnRetrySelected.TabIndex = 0;
            btnRetrySelected.Tag = "perm:button.state.retry-selected:enabled";
            btnRetrySelected.Text = "重试选中";
            // 
            // btnRetryAll
            // 
            btnRetryAll.BorderWidth = 1F;
            btnRetryAll.IconSvg = "UploadOutlined";
            btnRetryAll.Location = new Point(132, 7);
            btnRetryAll.Margin = new Padding(0, 0, 10, 0);
            btnRetryAll.Name = "btnRetryAll";
            btnRetryAll.Size = new Size(122, 40);
            btnRetryAll.TabIndex = 1;
            btnRetryAll.Tag = "perm:button.state.retry-all:enabled";
            btnRetryAll.Text = "全部重试";
            //
            // btnDeleteSelected
            //
            btnDeleteSelected.BorderWidth = 1F;
            btnDeleteSelected.IconSvg = "DeleteOutlined";
            btnDeleteSelected.Location = new Point(264, 7);
            btnDeleteSelected.Margin = new Padding(0, 0, 10, 0);
            btnDeleteSelected.Name = "btnDeleteSelected";
            btnDeleteSelected.Size = new Size(122, 40);
            btnDeleteSelected.TabIndex = 2;
            btnDeleteSelected.Tag = "perm:button.state.delete:enabled";
            btnDeleteSelected.Text = "删除选中";
            // 
            // btnRefresh
            // 
            btnRefresh.BorderWidth = 1F;
            btnRefresh.IconSvg = "ReloadOutlined";
            btnRefresh.Location = new Point(399, 10);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(92, 40);
            btnRefresh.TabIndex = 3;
            btnRefresh.Tag = "perm:button.state.refresh:enabled";
            btnRefresh.Text = "刷新";
            // 
            // tabUploadCategories
            // 
            tabUploadCategories.Controls.Add(tabSummary);
            tabUploadCategories.Controls.Add(tabStartReports);
            tabUploadCategories.Controls.Add(tabFinishReports);
            tabUploadCategories.Controls.Add(tabProcessParameters);
            tabUploadCategories.Controls.Add(tabReportFiles);
            tabUploadCategories.Controls.Add(tabWorkOrderStatuses);
            tabUploadCategories.Controls.Add(tabDeviceStatuses);
            tabUploadCategories.Controls.Add(tabProgramFiles);
            tabUploadCategories.Dock = DockStyle.Fill;
            tabUploadCategories.Location = new Point(20, 94);
            tabUploadCategories.Margin = new Padding(20, 0, 20, 8);
            tabUploadCategories.Name = "tabUploadCategories";
            tabUploadCategories.SelectedIndex = 0;
            tabUploadCategories.Size = new Size(1326, 36);
            tabUploadCategories.TabIndex = 4;
            // 
            // tabSummary
            // 
            tabSummary.Location = new Point(4, 32);
            tabSummary.Name = "tabSummary";
            tabSummary.Padding = new Padding(3);
            tabSummary.Size = new Size(1318, 0);
            tabSummary.TabIndex = 0;
            tabSummary.Text = "上传总览";
            tabSummary.UseVisualStyleBackColor = true;
            // 
            // tabProcessParameters
            // 
            tabProcessParameters.Location = new Point(4, 32);
            tabProcessParameters.Name = "tabProcessParameters";
            tabProcessParameters.Padding = new Padding(3);
            tabProcessParameters.Size = new Size(1318, 0);
            tabProcessParameters.TabIndex = 1;
            tabProcessParameters.Text = "过程参数";
            tabProcessParameters.UseVisualStyleBackColor = true;
            // 
            // tabStartReports
            // 
            tabStartReports.Location = new Point(4, 32);
            tabStartReports.Name = "tabStartReports";
            tabStartReports.Padding = new Padding(3);
            tabStartReports.Size = new Size(1318, 0);
            tabStartReports.TabIndex = 2;
            tabStartReports.Text = "开工信息";
            tabStartReports.UseVisualStyleBackColor = true;
            // 
            // tabFinishReports
            // 
            tabFinishReports.Location = new Point(4, 32);
            tabFinishReports.Name = "tabFinishReports";
            tabFinishReports.Padding = new Padding(3);
            tabFinishReports.Size = new Size(1318, 0);
            tabFinishReports.TabIndex = 3;
            tabFinishReports.Text = "完工信息";
            tabFinishReports.UseVisualStyleBackColor = true;
            // 
            // tabWorkOrderStatuses
            // 
            tabWorkOrderStatuses.Location = new Point(4, 32);
            tabWorkOrderStatuses.Name = "tabWorkOrderStatuses";
            tabWorkOrderStatuses.Padding = new Padding(3);
            tabWorkOrderStatuses.Size = new Size(1318, 0);
            tabWorkOrderStatuses.TabIndex = 4;
            tabWorkOrderStatuses.Text = "工单状态";
            tabWorkOrderStatuses.UseVisualStyleBackColor = true;
            // 
            // tabDeviceStatuses
            // 
            tabDeviceStatuses.Location = new Point(4, 32);
            tabDeviceStatuses.Name = "tabDeviceStatuses";
            tabDeviceStatuses.Padding = new Padding(3);
            tabDeviceStatuses.Size = new Size(1318, 0);
            tabDeviceStatuses.TabIndex = 5;
            tabDeviceStatuses.Text = "设备状态";
            tabDeviceStatuses.UseVisualStyleBackColor = true;
            // 
            // tabReportFiles
            // 
            tabReportFiles.Location = new Point(4, 32);
            tabReportFiles.Name = "tabReportFiles";
            tabReportFiles.Padding = new Padding(3);
            tabReportFiles.Size = new Size(1318, 0);
            tabReportFiles.TabIndex = 6;
            tabReportFiles.Text = "报告文件";
            tabReportFiles.UseVisualStyleBackColor = true;
            // 
            // tabProgramFiles
            // 
            tabProgramFiles.Location = new Point(4, 32);
            tabProgramFiles.Name = "tabProgramFiles";
            tabProgramFiles.Padding = new Padding(3);
            tabProgramFiles.Size = new Size(1318, 0);
            tabProgramFiles.TabIndex = 7;
            tabProgramFiles.Text = "程序文件";
            tabProgramFiles.UseVisualStyleBackColor = true;
            // 
            // lblSummary
            // 
            lblSummary.AutoSize = true;
            lblSummary.Dock = DockStyle.Fill;
            lblSummary.ForeColor = SystemColors.GrayText;
            lblSummary.Location = new Point(20, 138);
            lblSummary.Margin = new Padding(20, 0, 20, 8);
            lblSummary.Name = "lblSummary";
            lblSummary.Size = new Size(1326, 24);
            lblSummary.TabIndex = 5;
            lblSummary.Text = "待同步程序：0 条";
            // 
            // dgvPending
            // 
            dgvPending.AllowUserToAddRows = false;
            dgvPending.AllowUserToDeleteRows = false;
            dgvPending.BackgroundColor = SystemColors.Window;
            dgvPending.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPending.Dock = DockStyle.Fill;
            dgvPending.Location = new Point(20, 170);
            dgvPending.Margin = new Padding(20, 0, 20, 20);
            dgvPending.MultiSelect = false;
            dgvPending.Name = "dgvPending";
            dgvPending.ReadOnly = true;
            dgvPending.RowHeadersVisible = false;
            dgvPending.RowHeadersWidth = 51;
            dgvPending.RowTemplate.Height = 28;
            dgvPending.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPending.Size = new Size(1326, 555);
            dgvPending.TabIndex = 6;
            // 
            // StateManageView
            // 
            AutoScaleDimensions = new SizeF(10F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(rootLayout);
            Font = new Font("Microsoft YaHei UI", 10.5F);
            Name = "StateManageView";
            Size = new Size(1366, 745);
            rootLayout.ResumeLayout(false);
            rootLayout.PerformLayout();
            headerLayout.ResumeLayout(false);
            headerLayout.PerformLayout();
            titleLayout.ResumeLayout(false);
            titleLayout.PerformLayout();
            buttonFlow.ResumeLayout(false);
            tabUploadCategories.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvPending).EndInit();
            ResumeLayout(false);
        }

        private TableLayoutPanel rootLayout;
        private TableLayoutPanel headerLayout;
        private TableLayoutPanel titleLayout;
        private Label lblTitle;
        private Label lblDescription;
        private FlowLayoutPanel buttonFlow;
        private AntdUI.Button btnRetrySelected;
        private AntdUI.Button btnRetryAll;
        private AntdUI.Button btnDeleteSelected;
        private AntdUI.Button btnRefresh;
        private Label lblSummary;
        private TabControl tabUploadCategories;
        private TabPage tabSummary;
        private TabPage tabProcessParameters;
        private TabPage tabStartReports;
        private TabPage tabFinishReports;
        private TabPage tabWorkOrderStatuses;
        private TabPage tabDeviceStatuses;
        private TabPage tabReportFiles;
        private TabPage tabProgramFiles;
        private DataGridView dgvPending;
    }
}
