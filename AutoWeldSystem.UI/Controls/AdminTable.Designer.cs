namespace AutoWeldSystem.UI.Controls
{
    partial class AdminTable
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
            components = new System.ComponentModel.Container();
            pagination1 = new AntdUI.Pagination();
            panel1 = new Panel();
            inputQuery = new AutoWeldSystem.UI.Components.InputQuery(components);
            btnExport = new AntdUI.Button();
            panel2 = new Panel();
            btnDelete = new AntdUI.Button();
            btnEdit = new AntdUI.Button();
            btnAdd = new AntdUI.Button();
            table1 = new AntdUI.Table();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // pagination1
            // 
            pagination1.Dock = DockStyle.Bottom;
            pagination1.Location = new Point(0, 498);
            pagination1.Name = "pagination1";
            pagination1.RightToLeft = RightToLeft.Yes;
            pagination1.ShowSizeChanger = true;
            pagination1.Size = new Size(979, 33);
            pagination1.TabIndex = 0;
            pagination1.Text = "pagination1";
            // 
            // panel1
            // 
            panel1.Controls.Add(inputQuery);
            panel1.Controls.Add(btnExport);
            panel1.Controls.Add(panel2);
            panel1.Controls.Add(btnDelete);
            panel1.Controls.Add(btnEdit);
            panel1.Controls.Add(btnAdd);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(979, 42);
            panel1.TabIndex = 1;
            // 
            // inputQuery
            // 
            inputQuery.AutoSize = true;
            inputQuery.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            inputQuery.Dock = DockStyle.Right;
            inputQuery.Location = new Point(532, 0);
            inputQuery.MinimumSize = new Size(100, 40);
            inputQuery.Name = "inputQuery";
            inputQuery.QueryChanged = null;
            inputQuery.Size = new Size(323, 42);
            inputQuery.TabIndex = 9;
            // 
            // btnExport
            // 
            btnExport.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnExport.Dock = DockStyle.Right;
            btnExport.IconSvg = "FileExcelOutlined";
            btnExport.Location = new Point(855, 0);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(124, 42);
            btnExport.TabIndex = 8;
            btnExport.Text = "导出表格";
            // 
            // panel2
            // 
            panel2.Dock = DockStyle.Left;
            panel2.Location = new Point(267, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(263, 42);
            panel2.TabIndex = 6;
            // 
            // btnDelete
            // 
            btnDelete.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnDelete.BorderWidth = 1F;
            btnDelete.Dock = DockStyle.Left;
            btnDelete.IconSvg = "DeleteOutlined";
            btnDelete.Location = new Point(178, 0);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(89, 42);
            btnDelete.TabIndex = 5;
            btnDelete.Text = "删除";
            // 
            // btnEdit
            // 
            btnEdit.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnEdit.BorderWidth = 1F;
            btnEdit.Dock = DockStyle.Left;
            btnEdit.IconSvg = "FormOutlined";
            btnEdit.Location = new Point(89, 0);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new Size(89, 42);
            btnEdit.TabIndex = 4;
            btnEdit.Text = "编辑";
            // 
            // btnAdd
            // 
            btnAdd.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnAdd.BorderWidth = 1F;
            btnAdd.Dock = DockStyle.Left;
            btnAdd.IconSvg = "PlusOutlined";
            btnAdd.Location = new Point(0, 0);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(89, 42);
            btnAdd.TabIndex = 3;
            btnAdd.Text = "新增";
            // 
            // table1
            // 
            table1.Dock = DockStyle.Fill;
            table1.Gap = 12;
            table1.Location = new Point(0, 42);
            table1.Name = "table1";
            table1.Size = new Size(979, 456);
            table1.TabIndex = 2;
            table1.Text = "table1";
            // 
            // AdminTable
            // 
            AutoScaleDimensions = new SizeF(10F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(table1);
            Controls.Add(panel1);
            Controls.Add(pagination1);
            Font = new Font("Microsoft YaHei UI", 10.5F);
            Name = "AdminTable";
            Size = new Size(979, 531);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.Pagination pagination1;
        private Panel panel1;
        private Panel panel2;
        private AntdUI.Button btnDelete;
        private AntdUI.Button btnEdit;
        private AntdUI.Button btnAdd;
        private AntdUI.Table table1;
        private AntdUI.Button btnExport;
        private AutoWeldSystem.UI.Components.InputQuery inputQuery;
    }
}
