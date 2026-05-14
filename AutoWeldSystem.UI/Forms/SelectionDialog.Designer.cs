namespace AutoWeldSystem.UI.Forms
{
    partial class SelectionDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components is not null)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            lblPrompt = new Label();
            tableItems = new DataGridView();
            buttonPanel = new FlowLayoutPanel();
            btnCancel = new Button();
            btnOk = new Button();
            pageHeader1 = new AntdUI.PageHeader();
            rootLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)tableItems).BeginInit();
            buttonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(pageHeader1, 0, 0);
            rootLayout.Controls.Add(lblPrompt, 0, 1);
            rootLayout.Controls.Add(tableItems, 0, 2);
            rootLayout.Controls.Add(buttonPanel, 0, 3);
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.Padding = new Padding(16);
            rootLayout.RowCount = 4;
            rootLayout.RowStyles.Add(new RowStyle());
            rootLayout.RowStyles.Add(new RowStyle());
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle());
            rootLayout.Size = new Size(760, 500);
            rootLayout.TabIndex = 0;
            // 
            // lblPrompt
            // 
            lblPrompt.AutoEllipsis = true;
            lblPrompt.Dock = DockStyle.Fill;
            lblPrompt.Location = new Point(16, 48);
            lblPrompt.Margin = new Padding(0);
            lblPrompt.Name = "lblPrompt";
            lblPrompt.Size = new Size(728, 34);
            lblPrompt.TabIndex = 0;
            lblPrompt.Text = "请选择一条记录。";
            lblPrompt.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tableItems
            // 
            tableItems.AllowUserToAddRows = false;
            tableItems.AllowUserToDeleteRows = false;
            tableItems.AllowUserToResizeRows = false;
            tableItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tableItems.BackgroundColor = SystemColors.Window;
            tableItems.BorderStyle = BorderStyle.FixedSingle;
            tableItems.ColumnHeadersHeight = 38;
            tableItems.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            tableItems.Dock = DockStyle.Fill;
            tableItems.Location = new Point(16, 82);
            tableItems.Margin = new Padding(0);
            tableItems.MultiSelect = false;
            tableItems.Name = "tableItems";
            tableItems.ReadOnly = true;
            tableItems.RowHeadersVisible = false;
            tableItems.RowHeadersWidth = 51;
            tableItems.RowTemplate.Height = 36;
            tableItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tableItems.Size = new Size(728, 358);
            tableItems.TabIndex = 1;
            // 
            // buttonPanel
            // 
            buttonPanel.AutoSize = true;
            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnOk);
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Location = new Point(16, 440);
            buttonPanel.Margin = new Padding(0);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Padding = new Padding(0, 12, 0, 0);
            buttonPanel.Size = new Size(728, 44);
            buttonPanel.TabIndex = 2;
            buttonPanel.WrapContents = false;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(640, 12);
            btnCancel.Margin = new Padding(10, 0, 0, 0);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(88, 32);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "取消";
            // 
            // btnOk
            // 
            btnOk.Location = new Point(542, 12);
            btnOk.Margin = new Padding(10, 0, 0, 0);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(88, 32);
            btnOk.TabIndex = 0;
            btnOk.Text = "应用";
            // 
            // pageHeader1
            // 
            pageHeader1.Dock = DockStyle.Fill;
            pageHeader1.Location = new Point(16, 16);
            pageHeader1.Margin = new Padding(0);
            pageHeader1.MaximizeBox = false;
            pageHeader1.MinimizeBox = false;
            pageHeader1.Name = "pageHeader1";
            pageHeader1.ShowButton = true;
            pageHeader1.Size = new Size(728, 32);
            pageHeader1.TabIndex = 1;
            pageHeader1.Text = "工序选择";
            // 
            // SelectionDialog
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(10F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(760, 500);
            Controls.Add(rootLayout);
            Font = new Font("Microsoft YaHei UI", 10.5F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SelectionDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "选择";
            rootLayout.ResumeLayout(false);
            rootLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)tableItems).EndInit();
            buttonPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        private TableLayoutPanel rootLayout;
        private Label lblPrompt;
        private DataGridView tableItems;
        private FlowLayoutPanel buttonPanel;
        private Button btnCancel;
        private Button btnOk;
        private AntdUI.PageHeader pageHeader1;
    }
}
