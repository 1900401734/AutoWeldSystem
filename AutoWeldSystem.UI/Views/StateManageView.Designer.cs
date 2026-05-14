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
            btnRefresh = new AntdUI.Button();
            lblSummary = new Label();
            dgvPending = new DataGridView();
            rootLayout.SuspendLayout();
            headerLayout.SuspendLayout();
            titleLayout.SuspendLayout();
            buttonFlow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPending).BeginInit();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(headerLayout, 0, 0);
            rootLayout.Controls.Add(lblSummary, 0, 1);
            rootLayout.Controls.Add(dgvPending, 0, 2);
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.RowCount = 3;
            rootLayout.RowStyles.Add(new RowStyle());
            rootLayout.RowStyles.Add(new RowStyle());
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.Size = new Size(1366, 745);
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
            headerLayout.RowCount = 1;
            headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            headerLayout.Size = new Size(1326, 70);
            // 
            // titleLayout
            // 
            titleLayout.ColumnCount = 1;
            titleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            titleLayout.Controls.Add(lblTitle, 0, 0);
            titleLayout.Controls.Add(lblDescription, 0, 1);
            titleLayout.Dock = DockStyle.Fill;
            titleLayout.RowCount = 2;
            titleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            titleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            lblTitle.Text = "上传状态";
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblDescription
            // 
            lblDescription.AutoEllipsis = true;
            lblDescription.Dock = DockStyle.Fill;
            lblDescription.ForeColor = SystemColors.GrayText;
            lblDescription.Text = "查看尚未同步至 MES 的本地程序，支持断网恢复后手动重试。";
            lblDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // buttonFlow
            // 
            buttonFlow.AutoSize = true;
            buttonFlow.Controls.Add(btnRetrySelected);
            buttonFlow.Controls.Add(btnRetryAll);
            buttonFlow.Controls.Add(btnRefresh);
            buttonFlow.Dock = DockStyle.Right;
            buttonFlow.Padding = new Padding(0, 7, 0, 0);
            buttonFlow.WrapContents = false;
            // 
            // btnRetrySelected
            // 
            btnRetrySelected.BorderWidth = 1F;
            btnRetrySelected.IconSvg = "CloudUploadOutlined";
            btnRetrySelected.Margin = new Padding(0, 0, 10, 0);
            btnRetrySelected.Size = new Size(122, 40);
            btnRetrySelected.Text = "重试选中";
            // 
            // btnRetryAll
            // 
            btnRetryAll.BorderWidth = 1F;
            btnRetryAll.IconSvg = "UploadOutlined";
            btnRetryAll.Margin = new Padding(0, 0, 10, 0);
            btnRetryAll.Size = new Size(122, 40);
            btnRetryAll.Text = "全部重试";
            // 
            // btnRefresh
            // 
            btnRefresh.BorderWidth = 1F;
            btnRefresh.IconSvg = "ReloadOutlined";
            btnRefresh.Size = new Size(92, 40);
            btnRefresh.Text = "刷新";
            // 
            // lblSummary
            // 
            lblSummary.AutoSize = true;
            lblSummary.Dock = DockStyle.Fill;
            lblSummary.ForeColor = SystemColors.GrayText;
            lblSummary.Location = new Point(20, 94);
            lblSummary.Margin = new Padding(20, 0, 20, 8);
            lblSummary.Text = "待同步程序：0 条";
            // 
            // dgvPending
            // 
            dgvPending.AllowUserToAddRows = false;
            dgvPending.AllowUserToDeleteRows = false;
            dgvPending.BackgroundColor = SystemColors.Window;
            dgvPending.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPending.Dock = DockStyle.Fill;
            dgvPending.Location = new Point(20, 126);
            dgvPending.Margin = new Padding(20, 0, 20, 20);
            dgvPending.MultiSelect = false;
            dgvPending.ReadOnly = true;
            dgvPending.RowHeadersVisible = false;
            dgvPending.RowTemplate.Height = 28;
            dgvPending.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
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
        private AntdUI.Button btnRefresh;
        private Label lblSummary;
        private DataGridView dgvPending;
    }
}
