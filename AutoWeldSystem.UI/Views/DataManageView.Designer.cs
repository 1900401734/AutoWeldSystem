namespace AutoWeldSystem.UI.Views
{
    partial class DataManageView
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
            datePickerRange1 = new AntdUI.DatePickerRange();
            panel1 = new AntdUI.Panel();
            splitter1 = new AntdUI.Splitter();
            table1 = new AntdUI.Table();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitter1).BeginInit();
            splitter1.Panel1.SuspendLayout();
            splitter1.SuspendLayout();
            SuspendLayout();
            // 
            // datePickerRange1
            // 
            datePickerRange1.Dock = DockStyle.Left;
            datePickerRange1.Format = "yyyy-MM-dd HH:mm:ss";
            datePickerRange1.Location = new Point(0, 0);
            datePickerRange1.Margin = new Padding(4, 3, 4, 3);
            datePickerRange1.Name = "datePickerRange1";
            datePickerRange1.Size = new Size(503, 47);
            datePickerRange1.TabIndex = 2;
            // 
            // panel1
            // 
            panel1.Controls.Add(datePickerRange1);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(4, 3, 4, 3);
            panel1.Name = "panel1";
            panel1.Size = new Size(1456, 47);
            panel1.TabIndex = 3;
            panel1.Text = "panel1";
            // 
            // splitter1
            // 
            splitter1.Dock = DockStyle.Fill;
            splitter1.Location = new Point(0, 47);
            splitter1.Margin = new Padding(4, 3, 4, 3);
            splitter1.Name = "splitter1";
            // 
            // splitter1.Panel1
            // 
            splitter1.Panel1.Controls.Add(table1);
            splitter1.Size = new Size(1456, 608);
            splitter1.SplitterDistance = 912;
            splitter1.SplitterWidth = 12;
            splitter1.TabIndex = 4;
            // 
            // table1
            // 
            table1.Dock = DockStyle.Fill;
            table1.Gap = 12;
            table1.Location = new Point(0, 0);
            table1.Margin = new Padding(4, 3, 4, 3);
            table1.Name = "table1";
            table1.Size = new Size(912, 608);
            table1.TabIndex = 0;
            table1.Text = "table1";
            // 
            // DataManageView
            // 
            AutoScaleDimensions = new SizeF(10F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitter1);
            Controls.Add(panel1);
            Font = new Font("Microsoft YaHei UI", 10.5F);
            Margin = new Padding(4, 3, 4, 3);
            Name = "DataManageView";
            Size = new Size(1456, 655);
            panel1.ResumeLayout(false);
            splitter1.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitter1).EndInit();
            splitter1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.DatePickerRange datePickerRange1;
        private AntdUI.Panel panel1;
        private AntdUI.Splitter splitter1;
        private AntdUI.Table table1;
    }
}
