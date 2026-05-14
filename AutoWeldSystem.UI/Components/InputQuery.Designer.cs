namespace AutoWeldSystem.UI.Components
{
    partial class InputQuery
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
            btnRefresh = new AntdUI.Button();
            btnQuery = new AntdUI.Button();
            input1 = new AntdUI.Input();
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // btnRefresh
            // 
            btnRefresh.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnRefresh.BorderWidth = 1F;
            btnRefresh.Dock = DockStyle.Left;
            btnRefresh.IconSvg = "ReloadOutlined";
            btnRefresh.JoinMode = AntdUI.TJoinMode.Left;
            btnRefresh.Location = new Point(0, 0);
            btnRefresh.Margin = new Padding(0);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(76, 40);
            btnRefresh.TabIndex = 0;
            btnRefresh.Text = "刷新";
            // 
            // btnQuery
            // 
            btnQuery.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnQuery.BorderWidth = 1F;
            btnQuery.Dock = DockStyle.Right;
            btnQuery.IconSvg = "SearchOutlined";
            btnQuery.JoinMode = AntdUI.TJoinMode.Right;
            btnQuery.Location = new Point(231, 0);
            btnQuery.Margin = new Padding(0);
            btnQuery.Name = "btnQuery";
            btnQuery.Size = new Size(76, 40);
            btnQuery.TabIndex = 1;
            btnQuery.Text = "搜索";
            // 
            // input1
            // 
            input1.Dock = DockStyle.Fill;
            input1.JoinMode = AntdUI.TJoinMode.LR;
            input1.Location = new Point(76, 0);
            input1.Margin = new Padding(0);
            input1.Name = "input1";
            input1.Size = new Size(155, 40);
            input1.TabIndex = 2;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.Controls.Add(btnRefresh, 0, 0);
            tableLayoutPanel1.Controls.Add(btnQuery, 2, 0);
            tableLayoutPanel1.Controls.Add(input1, 1, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(307, 40);
            tableLayoutPanel1.TabIndex = 3;
            // 
            // InputQuery
            // 
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Controls.Add(tableLayoutPanel1);
            MinimumSize = new Size(100, 40);
            Name = "InputQuery";
            Size = new Size(307, 40);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private AntdUI.Button btnRefresh;
        private AntdUI.Button btnQuery;
        private AntdUI.Input input1;
        private TableLayoutPanel tableLayoutPanel1;
    }
}
