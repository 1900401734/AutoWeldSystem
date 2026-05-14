namespace AutoWeldSystem.UI.Forms
{
    partial class UserEditForm
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
            pageHeader1 = new AntdUI.PageHeader();
            inputUserName = new AntdUI.Input();
            label1 = new Label();
            inputUserNumber = new AntdUI.Input();
            label2 = new Label();
            btnSave = new AntdUI.Button();
            treeRole = new AntdUI.Tree();
            lblRole = new Label();
            tableLayoutPanel1 = new TableLayoutPanel();
            inputPasscode = new AntdUI.Input();
            label4 = new Label();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // pageHeader1
            // 
            pageHeader1.Dock = DockStyle.Top;
            pageHeader1.Location = new Point(0, 0);
            pageHeader1.MaximizeBox = false;
            pageHeader1.MinimizeBox = false;
            pageHeader1.Name = "pageHeader1";
            pageHeader1.ShowButton = true;
            pageHeader1.Size = new Size(519, 29);
            pageHeader1.TabIndex = 0;
            pageHeader1.Text = "用户编辑";
            // 
            // inputUserName
            // 
            inputUserName.Location = new Point(88, 52);
            inputUserName.Name = "inputUserName";
            inputUserName.Size = new Size(153, 39);
            inputUserName.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(26, 61);
            label1.Name = "label1";
            label1.Size = new Size(46, 24);
            label1.TabIndex = 1;
            label1.Text = "姓名";
            // 
            // inputUserNumber
            // 
            inputUserNumber.Location = new Point(88, 97);
            inputUserNumber.Name = "inputUserNumber";
            inputUserNumber.Size = new Size(153, 39);
            inputUserNumber.TabIndex = 1;
            // 
            // lblProgramName
            // 
            label2.AutoSize = true;
            label2.Location = new Point(26, 106);
            label2.Name = "lblProgramName";
            label2.Size = new Size(46, 24);
            label2.TabIndex = 1;
            label2.Text = "工号";
            // 
            // btnSave
            // 
            btnSave.Location = new Point(88, 208);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(153, 45);
            btnSave.TabIndex = 2;
            btnSave.Text = "保存";
            // 
            // treeRole
            // 
            treeRole.Checkable = true;
            treeRole.Dock = DockStyle.Fill;
            treeRole.Location = new Point(3, 33);
            treeRole.Name = "treeRole";
            treeRole.Size = new Size(234, 204);
            treeRole.TabIndex = 3;
            // 
            // lblRole
            // 
            lblRole.AutoSize = true;
            lblRole.Dock = DockStyle.Left;
            lblRole.Location = new Point(3, 3);
            lblRole.Margin = new Padding(3);
            lblRole.Name = "lblRole";
            lblRole.Size = new Size(46, 24);
            lblRole.TabIndex = 1;
            lblRole.Text = "角色";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(treeRole, 0, 1);
            tableLayoutPanel1.Controls.Add(lblRole, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Right;
            tableLayoutPanel1.Location = new Point(279, 29);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(240, 240);
            tableLayoutPanel1.TabIndex = 4;
            // 
            // inputPasscode
            // 
            inputPasscode.Location = new Point(88, 142);
            inputPasscode.Name = "inputPasscode";
            inputPasscode.Size = new Size(153, 39);
            inputPasscode.TabIndex = 1;
            // 
            // lblProductNo
            // 
            label4.AutoSize = true;
            label4.Location = new Point(26, 151);
            label4.Name = "lblProductNo";
            label4.Size = new Size(46, 24);
            label4.TabIndex = 1;
            label4.Text = "密码";
            // 
            // UserEditForm
            // 
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(519, 269);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(btnSave);
            Controls.Add(label4);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(pageHeader1);
            Controls.Add(inputPasscode);
            Controls.Add(inputUserNumber);
            Controls.Add(inputUserName);
            Font = new Font("Microsoft YaHei UI", 10.5F);
            Name = "UserEditForm";
            Text = "UserEditForm";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private AntdUI.PageHeader pageHeader1;
        private AntdUI.Input inputUserName;
        private Label label1;
        private AntdUI.Input inputUserNumber;
        private Label label2;
        private AntdUI.Button btnSave;
        private AntdUI.Tree treeRole;
        private Label lblRole;
        private TableLayoutPanel tableLayoutPanel1;
        private AntdUI.Input inputPasscode;
        private Label label4;
    }
}