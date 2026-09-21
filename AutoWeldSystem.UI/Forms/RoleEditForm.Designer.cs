namespace AutoWeldSystem.UI.Forms
{
    partial class RoleEditForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            switchEnabled = new CheckBox();
            btnSave = new Button();
            btnCancel = new Button();
            pageHeader1 = new AntdUI.PageHeader();
            tableLayoutPanel1 = new TableLayoutPanel();
            label2 = new AntdUI.Label();
            inputRoleName = new AntdUI.Input();
            inputRoleCode = new AntdUI.Input();
            inputDescription = new AntdUI.Input();
            label1 = new AntdUI.Label();
            label3 = new AntdUI.Label();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // switchEnabled
            // 
            switchEnabled.AutoSize = true;
            switchEnabled.Location = new Point(24, 38);
            switchEnabled.Name = "switchEnabled";
            switchEnabled.Size = new Size(104, 28);
            switchEnabled.TabIndex = 2;
            switchEnabled.Text = "是否启用";
            switchEnabled.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(21, 240);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(94, 29);
            btnSave.TabIndex = 3;
            btnSave.Text = "保存";
            btnSave.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(164, 240);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(94, 29);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "取消";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // pageHeader1
            // 
            pageHeader1.Dock = DockStyle.Top;
            pageHeader1.Location = new Point(0, 0);
            pageHeader1.MaximizeBox = false;
            pageHeader1.MinimizeBox = false;
            pageHeader1.Name = "pageHeader1";
            pageHeader1.ShowButton = true;
            pageHeader1.Size = new Size(340, 29);
            pageHeader1.TabIndex = 4;
            pageHeader1.Text = "角色编辑";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(label2, 0, 0);
            tableLayoutPanel1.Controls.Add(inputRoleName, 1, 1);
            tableLayoutPanel1.Controls.Add(inputRoleCode, 1, 0);
            tableLayoutPanel1.Controls.Add(inputDescription, 1, 2);
            tableLayoutPanel1.Controls.Add(label1, 0, 1);
            tableLayoutPanel1.Controls.Add(label3, 0, 2);
            tableLayoutPanel1.Location = new Point(21, 70);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.Size = new Size(250, 146);
            tableLayoutPanel1.TabIndex = 5;
            // 
            // label2
            // 
            label2.AutoSizeMode = AntdUI.TAutoSize.Width;
            label2.Dock = DockStyle.Fill;
            label2.Location = new Point(3, 3);
            label2.Name = "label2";
            label2.Size = new Size(70, 42);
            label2.TabIndex = 6;
            label2.Text = "角色编码";
            label2.TextAlign = ContentAlignment.MiddleRight;
            // 
            // inputRoleName
            // 
            inputRoleName.Dock = DockStyle.Fill;
            inputRoleName.Location = new Point(79, 51);
            inputRoleName.Name = "inputRoleName";
            inputRoleName.Size = new Size(168, 42);
            inputRoleName.TabIndex = 4;
            // 
            // inputRoleCode
            // 
            inputRoleCode.Dock = DockStyle.Fill;
            inputRoleCode.Location = new Point(79, 3);
            inputRoleCode.Name = "inputRoleCode";
            inputRoleCode.Size = new Size(168, 42);
            inputRoleCode.TabIndex = 4;
            // 
            // inputDescription
            // 
            inputDescription.Dock = DockStyle.Fill;
            inputDescription.Location = new Point(79, 99);
            inputDescription.Name = "inputDescription";
            inputDescription.Size = new Size(168, 44);
            inputDescription.TabIndex = 5;
            // 
            // label1
            // 
            label1.AutoSizeMode = AntdUI.TAutoSize.Width;
            label1.Dock = DockStyle.Fill;
            label1.Location = new Point(3, 51);
            label1.Name = "label1";
            label1.Size = new Size(70, 42);
            label1.TabIndex = 6;
            label1.Text = "角色名称";
            label1.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            label3.AutoSizeMode = AntdUI.TAutoSize.Width;
            label3.Dock = DockStyle.Fill;
            label3.Location = new Point(3, 99);
            label3.Name = "label3";
            label3.Size = new Size(35, 44);
            label3.TabIndex = 6;
            label3.Text = "描述";
            label3.TextAlign = ContentAlignment.MiddleRight;
            // 
            // RoleEditForm
            // 
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(340, 346);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(switchEnabled);
            Controls.Add(pageHeader1);
            Font = new Font("Microsoft YaHei UI", 10.5F);
            Name = "RoleEditForm";
            Text = "RoleEditForm";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private CheckBox switchEnabled;
        private Button btnSave;
        private Button btnCancel;
        private AntdUI.PageHeader pageHeader1;
        private TableLayoutPanel tableLayoutPanel1;
        private AntdUI.Label label2;
        private AntdUI.Input inputRoleName;
        private AntdUI.Input inputRoleCode;
        private AntdUI.Input inputDescription;
        private AntdUI.Label label1;
        private AntdUI.Label label3;
    }
}