namespace AutoWeldSystem.UI.Views
{
    partial class ProgramManageView
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
            toolbar = new FlowLayoutPanel();
            btnNew = new AntdUI.Button();
            btnSave = new AntdUI.Button();
            btnDelete = new AntdUI.Button();
            btnSync = new AntdUI.Button();
            btnPullMes = new AntdUI.Button();
            btnRefresh = new AntdUI.Button();
            chkSyncNow = new CheckBox();
            txtKeyword = new TextBox();
            splitMain = new SplitContainer();
            leftLayout = new TableLayoutPanel();
            dgvPrograms = new DataGridView();
            grpRevisions = new GroupBox();
            dgvRevisions = new DataGridView();
            editorLayout = new TableLayoutPanel();
            lblCurrentInfo = new Label();
            lblProgramName = new Label();
            lblProductNum = new Label();
            txtProductNum = new TextBox();
            lblProductModel = new Label();
            txtProductModel = new TextBox();
            lblComponentCode = new Label();
            txtComponentCode = new TextBox();
            lblSequenceNumber = new Label();
            txtSequenceNumber = new TextBox();
            lblProgramType = new Label();
            cmbProgramType = new ComboBox();
            lblWeldJobName = new Label();
            txtWeldJobName = new TextBox();
            lblRobotJobName = new Label();
            txtRobotJobName = new TextBox();
            lblCycleTime = new Label();
            txtCycleTime = new TextBox();
            lblProgramFile = new Label();
            fileLayout = new TableLayoutPanel();
            txtProgramFile = new TextBox();
            btnBrowseFile = new AntdUI.Button();
            lblCommitMessage = new Label();
            txtCommitMessage = new TextBox();
            lblRemark = new Label();
            txtRemark = new TextBox();
            lblProgramContent = new Label();
            txtProgramContent = new TextBox();
            LayoutProgramName = new TableLayoutPanel();
            txtProgramName = new TextBox();
            btnBuildName = new AntdUI.Button();
            rootLayout.SuspendLayout();
            toolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            leftLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPrograms).BeginInit();
            grpRevisions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRevisions).BeginInit();
            editorLayout.SuspendLayout();
            fileLayout.SuspendLayout();
            LayoutProgramName.SuspendLayout();
            SuspendLayout();
            //
            // rootLayout
            //
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(toolbar, 0, 0);
            rootLayout.Controls.Add(splitMain, 0, 1);
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.RowCount = 2;
            rootLayout.RowStyles.Add(new RowStyle());
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.Size = new Size(1366, 745);
            rootLayout.TabIndex = 0;
            //
            // toolbar
            //
            toolbar.AutoSize = true;
            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnSave);
            toolbar.Controls.Add(btnDelete);
            toolbar.Controls.Add(btnSync);
            toolbar.Controls.Add(btnPullMes);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(chkSyncNow);
            toolbar.Controls.Add(txtKeyword);
            toolbar.Dock = DockStyle.Fill;
            toolbar.Location = new Point(3, 3);
            toolbar.Name = "toolbar";
            toolbar.Padding = new Padding(20, 12, 20, 6);
            toolbar.Size = new Size(1360, 58);
            toolbar.TabIndex = 0;
            toolbar.WrapContents = false;
            //
            // btnNew
            //
            btnNew.BorderWidth = 1F;
            btnNew.IconSvg = "PlusOutlined";
            btnNew.Location = new Point(20, 12);
            btnNew.Margin = new Padding(0, 0, 10, 0);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(92, 40);
            btnNew.TabIndex = 0;
            btnNew.Tag = "perm:button.program.add:visible";
            btnNew.Text = "新建";
            //
            // btnSave
            //
            btnSave.BorderWidth = 1F;
            btnSave.IconSvg = "SaveOutlined";
            btnSave.Location = new Point(122, 12);
            btnSave.Margin = new Padding(0, 0, 10, 0);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 40);
            btnSave.TabIndex = 1;
            btnSave.Tag = "perm:button.program.edit:visible";
            btnSave.Text = "保存";
            //
            // btnDelete
            //
            btnDelete.BorderWidth = 1F;
            btnDelete.IconSvg = "DeleteOutlined";
            btnDelete.Location = new Point(224, 12);
            btnDelete.Margin = new Padding(0, 0, 10, 0);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(92, 40);
            btnDelete.TabIndex = 2;
            btnDelete.Tag = "perm:button.program.delete:visible";
            btnDelete.Text = "删除";
            //
            // btnSync
            //
            btnSync.BorderWidth = 1F;
            btnSync.IconSvg = "CloudUploadOutlined";
            btnSync.Location = new Point(326, 12);
            btnSync.Margin = new Padding(0, 0, 10, 0);
            btnSync.Name = "btnSync";
            btnSync.Size = new Size(118, 40);
            btnSync.TabIndex = 3;
            btnSync.Text = "同步MES";
            //
            // btnPullMes
            //
            btnPullMes.BorderWidth = 1F;
            btnPullMes.IconSvg = "CloudDownloadOutlined";
            btnPullMes.Location = new Point(454, 12);
            btnPullMes.Margin = new Padding(0, 0, 10, 0);
            btnPullMes.Name = "btnPullMes";
            btnPullMes.Size = new Size(132, 40);
            btnPullMes.TabIndex = 4;
            btnPullMes.Text = "从MES拉取";
            //
            // btnRefresh
            //
            btnRefresh.BorderWidth = 1F;
            btnRefresh.IconSvg = "ReloadOutlined";
            btnRefresh.Location = new Point(596, 12);
            btnRefresh.Margin = new Padding(0, 0, 18, 0);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(92, 40);
            btnRefresh.TabIndex = 5;
            btnRefresh.Text = "刷新";
            //
            // chkSyncNow
            //
            chkSyncNow.AutoSize = true;
            chkSyncNow.Checked = true;
            chkSyncNow.CheckState = CheckState.Checked;
            chkSyncNow.Location = new Point(706, 20);
            chkSyncNow.Margin = new Padding(0, 8, 20, 0);
            chkSyncNow.Name = "chkSyncNow";
            chkSyncNow.Size = new Size(158, 28);
            chkSyncNow.TabIndex = 6;
            chkSyncNow.Text = "保存后立即同步";
            //
            // txtKeyword
            //
            txtKeyword.Location = new Point(884, 17);
            txtKeyword.Margin = new Padding(0, 5, 0, 0);
            txtKeyword.Name = "txtKeyword";
            txtKeyword.PlaceholderText = "搜索程序 / 产品工号 / 状态";
            txtKeyword.Size = new Size(250, 30);
            txtKeyword.TabIndex = 7;
            //
            // splitMain
            //
            splitMain.Dock = DockStyle.Fill;
            splitMain.Location = new Point(20, 64);
            splitMain.Margin = new Padding(20, 0, 20, 18);
            splitMain.Name = "splitMain";
            //
            // splitMain.Panel1
            //
            splitMain.Panel1.Controls.Add(leftLayout);
            //
            // splitMain.Panel2
            //
            splitMain.Panel2.Controls.Add(editorLayout);
            splitMain.Panel2.Padding = new Padding(12, 0, 0, 0);
            splitMain.Size = new Size(1326, 663);
            splitMain.SplitterDistance = 650;
            splitMain.SplitterWidth = 10;
            splitMain.TabIndex = 1;
            //
            // leftLayout
            //
            leftLayout.ColumnCount = 1;
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftLayout.Controls.Add(dgvPrograms, 0, 0);
            leftLayout.Controls.Add(grpRevisions, 0, 1);
            leftLayout.Dock = DockStyle.Fill;
            leftLayout.Location = new Point(0, 0);
            leftLayout.Name = "leftLayout";
            leftLayout.RowCount = 2;
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 68F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));
            leftLayout.Size = new Size(650, 663);
            leftLayout.TabIndex = 0;
            //
            // dgvPrograms
            //
            dgvPrograms.AllowUserToAddRows = false;
            dgvPrograms.AllowUserToDeleteRows = false;
            dgvPrograms.BackgroundColor = SystemColors.Window;
            dgvPrograms.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPrograms.Dock = DockStyle.Fill;
            dgvPrograms.Location = new Point(3, 3);
            dgvPrograms.MultiSelect = false;
            dgvPrograms.Name = "dgvPrograms";
            dgvPrograms.ReadOnly = true;
            dgvPrograms.RowHeadersVisible = false;
            dgvPrograms.RowHeadersWidth = 51;
            dgvPrograms.RowTemplate.Height = 28;
            dgvPrograms.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPrograms.Size = new Size(644, 444);
            dgvPrograms.TabIndex = 0;
            //
            // grpRevisions
            //
            grpRevisions.Controls.Add(dgvRevisions);
            grpRevisions.Dock = DockStyle.Fill;
            grpRevisions.Location = new Point(3, 453);
            grpRevisions.Name = "grpRevisions";
            grpRevisions.Size = new Size(644, 207);
            grpRevisions.TabIndex = 1;
            grpRevisions.TabStop = false;
            grpRevisions.Text = "版本提交历史";
            //
            // dgvRevisions
            //
            dgvRevisions.AllowUserToAddRows = false;
            dgvRevisions.AllowUserToDeleteRows = false;
            dgvRevisions.BackgroundColor = SystemColors.Window;
            dgvRevisions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvRevisions.Dock = DockStyle.Fill;
            dgvRevisions.Location = new Point(3, 26);
            dgvRevisions.MultiSelect = false;
            dgvRevisions.Name = "dgvRevisions";
            dgvRevisions.ReadOnly = true;
            dgvRevisions.RowHeadersVisible = false;
            dgvRevisions.RowHeadersWidth = 51;
            dgvRevisions.RowTemplate.Height = 26;
            dgvRevisions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvRevisions.Size = new Size(638, 178);
            dgvRevisions.TabIndex = 0;
            //
            // editorLayout
            //
            editorLayout.ColumnCount = 2;
            editorLayout.ColumnStyles.Add(new ColumnStyle());
            editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            editorLayout.Controls.Add(lblCurrentInfo, 0, 0);
            editorLayout.Controls.Add(lblProgramName, 0, 1);
            editorLayout.Controls.Add(lblProductNum, 0, 2);
            editorLayout.Controls.Add(txtProductNum, 1, 2);
            editorLayout.Controls.Add(lblProductModel, 0, 3);
            editorLayout.Controls.Add(txtProductModel, 1, 3);
            editorLayout.Controls.Add(lblComponentCode, 0, 4);
            editorLayout.Controls.Add(txtComponentCode, 1, 4);
            editorLayout.Controls.Add(lblSequenceNumber, 0, 5);
            editorLayout.Controls.Add(txtSequenceNumber, 1, 5);
            editorLayout.Controls.Add(lblProgramType, 0, 6);
            editorLayout.Controls.Add(cmbProgramType, 1, 6);
            editorLayout.Controls.Add(lblWeldJobName, 0, 7);
            editorLayout.Controls.Add(txtWeldJobName, 1, 7);
            editorLayout.Controls.Add(lblRobotJobName, 0, 8);
            editorLayout.Controls.Add(txtRobotJobName, 1, 8);
            editorLayout.Controls.Add(lblCycleTime, 0, 9);
            editorLayout.Controls.Add(txtCycleTime, 1, 9);
            editorLayout.Controls.Add(lblProgramFile, 0, 10);
            editorLayout.Controls.Add(fileLayout, 1, 10);
            editorLayout.Controls.Add(lblCommitMessage, 0, 11);
            editorLayout.Controls.Add(txtCommitMessage, 1, 11);
            editorLayout.Controls.Add(lblRemark, 0, 12);
            editorLayout.Controls.Add(txtRemark, 1, 12);
            editorLayout.Controls.Add(lblProgramContent, 0, 13);
            editorLayout.Controls.Add(txtProgramContent, 0, 14);
            editorLayout.Controls.Add(LayoutProgramName, 1, 1);
            editorLayout.Dock = DockStyle.Fill;
            editorLayout.Location = new Point(12, 0);
            editorLayout.Name = "editorLayout";
            editorLayout.RowCount = 15;
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            editorLayout.Size = new Size(654, 663);
            editorLayout.TabIndex = 0;
            //
            // lblCurrentInfo
            //
            lblCurrentInfo.AutoEllipsis = true;
            editorLayout.SetColumnSpan(lblCurrentInfo, 2);
            lblCurrentInfo.Dock = DockStyle.Fill;
            lblCurrentInfo.ForeColor = SystemColors.GrayText;
            lblCurrentInfo.Location = new Point(3, 0);
            lblCurrentInfo.Name = "lblCurrentInfo";
            lblCurrentInfo.Size = new Size(648, 32);
            lblCurrentInfo.TabIndex = 0;
            lblCurrentInfo.Text = "xxx";
            //
            // lblProgramName
            //
            lblProgramName.Dock = DockStyle.Fill;
            lblProgramName.Location = new Point(3, 32);
            lblProgramName.Name = "lblProgramName";
            lblProgramName.Size = new Size(118, 39);
            lblProgramName.TabIndex = 1;
            lblProgramName.Text = "程序名称";
            lblProgramName.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblProductNum
            //
            lblProductNum.Dock = DockStyle.Fill;
            lblProductNum.Location = new Point(3, 71);
            lblProductNum.Name = "lblProductNum";
            lblProductNum.Size = new Size(118, 39);
            lblProductNum.TabIndex = 4;
            lblProductNum.Text = "产品工号";
            lblProductNum.TextAlign = ContentAlignment.MiddleLeft;
            //
            // txtProductNum
            //
            txtProductNum.Dock = DockStyle.Fill;
            txtProductNum.Location = new Point(124, 75);
            txtProductNum.Margin = new Padding(0, 4, 8, 4);
            txtProductNum.Name = "txtProductNum";
            txtProductNum.Size = new Size(522, 30);
            txtProductNum.TabIndex = 5;
            //
            // lblProductModel
            //
            lblProductModel.Dock = DockStyle.Fill;
            lblProductModel.Location = new Point(3, 110);
            lblProductModel.Name = "lblProductModel";
            lblProductModel.Size = new Size(118, 39);
            lblProductModel.TabIndex = 6;
            lblProductModel.Text = "产品型号";
            lblProductModel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // txtProductModel
            //
            txtProductModel.Dock = DockStyle.Fill;
            txtProductModel.Location = new Point(124, 114);
            txtProductModel.Margin = new Padding(0, 4, 8, 4);
            txtProductModel.Name = "txtProductModel";
            txtProductModel.Size = new Size(522, 30);
            txtProductModel.TabIndex = 7;
            //
            // lblComponentCode
            //
            lblComponentCode.Dock = DockStyle.Fill;
            lblComponentCode.Location = new Point(3, 149);
            lblComponentCode.Name = "lblComponentCode";
            lblComponentCode.Size = new Size(118, 39);
            lblComponentCode.TabIndex = 8;
            lblComponentCode.Text = "零组件代码";
            lblComponentCode.TextAlign = ContentAlignment.MiddleLeft;
            //
            // txtComponentCode
            //
            txtComponentCode.Dock = DockStyle.Fill;
            txtComponentCode.Location = new Point(124, 153);
            txtComponentCode.Margin = new Padding(0, 4, 8, 4);
            txtComponentCode.Name = "txtComponentCode";
            txtComponentCode.Size = new Size(522, 30);
            txtComponentCode.TabIndex = 9;
            //
            // lblSequenceNumber
            //
            lblSequenceNumber.Dock = DockStyle.Fill;
            lblSequenceNumber.Location = new Point(3, 188);
            lblSequenceNumber.Name = "lblSequenceNumber";
            lblSequenceNumber.Size = new Size(118, 39);
            lblSequenceNumber.TabIndex = 10;
            lblSequenceNumber.Text = "流水号";
            lblSequenceNumber.TextAlign = ContentAlignment.MiddleLeft;
            //
            // txtSequenceNumber
            //
            txtSequenceNumber.Dock = DockStyle.Fill;
            txtSequenceNumber.Location = new Point(124, 192);
            txtSequenceNumber.Margin = new Padding(0, 4, 8, 4);
            txtSequenceNumber.Name = "txtSequenceNumber";
            txtSequenceNumber.Size = new Size(522, 30);
            txtSequenceNumber.TabIndex = 11;
            //
            // lblProgramType
            //
            lblProgramType.Dock = DockStyle.Fill;
            lblProgramType.Location = new Point(3, 227);
            lblProgramType.Name = "lblProgramType";
            lblProgramType.Size = new Size(118, 39);
            lblProgramType.TabIndex = 12;
            lblProgramType.Text = "程序类型";
            lblProgramType.TextAlign = ContentAlignment.MiddleLeft;
            //
            // cmbProgramType
            //
            cmbProgramType.Dock = DockStyle.Fill;
            cmbProgramType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProgramType.Items.AddRange(new object[] { "0 - 参数字符串", "1 - 文件" });
            cmbProgramType.Location = new Point(127, 230);
            cmbProgramType.Name = "cmbProgramType";
            cmbProgramType.Size = new Size(524, 31);
            cmbProgramType.TabIndex = 13;
            //
            // lblWeldJobName
            //
            lblWeldJobName.Dock = DockStyle.Fill;
            lblWeldJobName.Location = new Point(3, 266);
            lblWeldJobName.Name = "lblWeldJobName";
            lblWeldJobName.Size = new Size(118, 39);
            lblWeldJobName.TabIndex = 14;
            lblWeldJobName.Text = "焊接作业";
            lblWeldJobName.TextAlign = ContentAlignment.MiddleLeft;
            //
            // txtWeldJobName
            //
            txtWeldJobName.Dock = DockStyle.Fill;
            txtWeldJobName.Location = new Point(124, 270);
            txtWeldJobName.Margin = new Padding(0, 4, 8, 4);
            txtWeldJobName.Name = "txtWeldJobName";
            txtWeldJobName.Size = new Size(522, 30);
            txtWeldJobName.TabIndex = 15;
            //
            // lblRobotJobName
            //
            lblRobotJobName.Dock = DockStyle.Fill;
            lblRobotJobName.Location = new Point(3, 305);
            lblRobotJobName.Name = "lblRobotJobName";
            lblRobotJobName.Size = new Size(118, 39);
            lblRobotJobName.TabIndex = 16;
            lblRobotJobName.Text = "机器作业";
            lblRobotJobName.TextAlign = ContentAlignment.MiddleLeft;
            //
            // txtRobotJobName
            //
            txtRobotJobName.Dock = DockStyle.Fill;
            txtRobotJobName.Location = new Point(124, 309);
            txtRobotJobName.Margin = new Padding(0, 4, 8, 4);
            txtRobotJobName.Name = "txtRobotJobName";
            txtRobotJobName.Size = new Size(522, 30);
            txtRobotJobName.TabIndex = 17;
            //
            // lblCycleTime
            //
            lblCycleTime.Dock = DockStyle.Fill;
            lblCycleTime.Location = new Point(3, 344);
            lblCycleTime.Name = "lblCycleTime";
            lblCycleTime.Size = new Size(118, 39);
            lblCycleTime.TabIndex = 18;
            lblCycleTime.Text = "节拍秒";
            lblCycleTime.TextAlign = ContentAlignment.MiddleLeft;
            //
            // txtCycleTime
            //
            txtCycleTime.Dock = DockStyle.Fill;
            txtCycleTime.Location = new Point(124, 348);
            txtCycleTime.Margin = new Padding(0, 4, 8, 4);
            txtCycleTime.Name = "txtCycleTime";
            txtCycleTime.Size = new Size(522, 30);
            txtCycleTime.TabIndex = 19;
            //
            // lblProgramFile
            //
            lblProgramFile.Dock = DockStyle.Fill;
            lblProgramFile.Location = new Point(3, 383);
            lblProgramFile.Name = "lblProgramFile";
            lblProgramFile.Size = new Size(118, 39);
            lblProgramFile.TabIndex = 20;
            lblProgramFile.Text = "程序文件";
            lblProgramFile.TextAlign = ContentAlignment.MiddleLeft;
            //
            // fileLayout
            //
            fileLayout.ColumnCount = 2;
            fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            fileLayout.ColumnStyles.Add(new ColumnStyle());
            fileLayout.Controls.Add(txtProgramFile, 0, 0);
            fileLayout.Controls.Add(btnBrowseFile, 1, 0);
            fileLayout.Dock = DockStyle.Fill;
            fileLayout.Location = new Point(127, 386);
            fileLayout.Name = "fileLayout";
            fileLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            fileLayout.Size = new Size(524, 33);
            fileLayout.TabIndex = 21;
            //
            // txtProgramFile
            //
            txtProgramFile.Dock = DockStyle.Fill;
            txtProgramFile.Location = new Point(0, 4);
            txtProgramFile.Margin = new Padding(0, 4, 8, 4);
            txtProgramFile.Name = "txtProgramFile";
            txtProgramFile.Size = new Size(420, 30);
            txtProgramFile.TabIndex = 0;
            //
            // btnBrowseFile
            //
            btnBrowseFile.BorderWidth = 1F;
            btnBrowseFile.Dock = DockStyle.Fill;
            btnBrowseFile.IconSvg = "FolderOpenOutlined";
            btnBrowseFile.Location = new Point(428, 3);
            btnBrowseFile.Margin = new Padding(0, 3, 0, 3);
            btnBrowseFile.Name = "btnBrowseFile";
            btnBrowseFile.Size = new Size(96, 27);
            btnBrowseFile.TabIndex = 1;
            btnBrowseFile.Text = "选择";
            //
            // lblCommitMessage
            //
            lblCommitMessage.Dock = DockStyle.Fill;
            lblCommitMessage.Location = new Point(3, 422);
            lblCommitMessage.Name = "lblCommitMessage";
            lblCommitMessage.Size = new Size(118, 39);
            lblCommitMessage.TabIndex = 22;
            lblCommitMessage.Text = "提交说明";
            lblCommitMessage.TextAlign = ContentAlignment.MiddleLeft;
            //
            // txtCommitMessage
            //
            txtCommitMessage.Dock = DockStyle.Fill;
            txtCommitMessage.Location = new Point(124, 426);
            txtCommitMessage.Margin = new Padding(0, 4, 8, 4);
            txtCommitMessage.Name = "txtCommitMessage";
            txtCommitMessage.Size = new Size(522, 30);
            txtCommitMessage.TabIndex = 23;
            //
            // lblRemark
            //
            lblRemark.Dock = DockStyle.Fill;
            lblRemark.Location = new Point(3, 461);
            lblRemark.Name = "lblRemark";
            lblRemark.Size = new Size(118, 39);
            lblRemark.TabIndex = 24;
            lblRemark.Text = "备注";
            lblRemark.TextAlign = ContentAlignment.MiddleLeft;
            //
            // txtRemark
            //
            txtRemark.Dock = DockStyle.Fill;
            txtRemark.Location = new Point(124, 465);
            txtRemark.Margin = new Padding(0, 4, 8, 4);
            txtRemark.Name = "txtRemark";
            txtRemark.Size = new Size(522, 30);
            txtRemark.TabIndex = 25;
            //
            // lblProgramContent
            //
            lblProgramContent.Dock = DockStyle.Fill;
            lblProgramContent.Location = new Point(3, 500);
            lblProgramContent.Name = "lblProgramContent";
            lblProgramContent.Size = new Size(118, 28);
            lblProgramContent.TabIndex = 26;
            lblProgramContent.Text = "工艺参数JSON";
            lblProgramContent.TextAlign = ContentAlignment.MiddleLeft;
            //
            // txtProgramContent
            //
            txtProgramContent.AcceptsReturn = true;
            txtProgramContent.AcceptsTab = true;
            editorLayout.SetColumnSpan(txtProgramContent, 2);
            txtProgramContent.Dock = DockStyle.Fill;
            txtProgramContent.Font = new Font("Consolas", 10F);
            txtProgramContent.Location = new Point(3, 531);
            txtProgramContent.Multiline = true;
            txtProgramContent.Name = "txtProgramContent";
            txtProgramContent.ScrollBars = ScrollBars.Both;
            txtProgramContent.Size = new Size(648, 129);
            txtProgramContent.TabIndex = 27;
            txtProgramContent.WordWrap = false;
            //
            // LayoutProgramName
            //
            LayoutProgramName.ColumnCount = 2;
            LayoutProgramName.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            LayoutProgramName.ColumnStyles.Add(new ColumnStyle());
            LayoutProgramName.Controls.Add(txtProgramName, 0, 0);
            LayoutProgramName.Controls.Add(btnBuildName, 1, 0);
            LayoutProgramName.Dock = DockStyle.Fill;
            LayoutProgramName.Location = new Point(127, 35);
            LayoutProgramName.Name = "LayoutProgramName";
            LayoutProgramName.RowCount = 1;
            LayoutProgramName.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            LayoutProgramName.Size = new Size(524, 33);
            LayoutProgramName.TabIndex = 28;
            //
            // txtProgramName
            //
            txtProgramName.Dock = DockStyle.Fill;
            txtProgramName.Location = new Point(0, 4);
            txtProgramName.Margin = new Padding(0, 4, 8, 4);
            txtProgramName.Name = "txtProgramName";
            txtProgramName.Size = new Size(398, 30);
            txtProgramName.TabIndex = 2;
            //
            // btnBuildName
            //
            btnBuildName.BorderWidth = 1F;
            btnBuildName.Dock = DockStyle.Fill;
            btnBuildName.IconSvg = "BranchesOutlined";
            btnBuildName.Location = new Point(406, 3);
            btnBuildName.Margin = new Padding(0, 3, 0, 3);
            btnBuildName.Name = "btnBuildName";
            btnBuildName.Size = new Size(118, 27);
            btnBuildName.TabIndex = 3;
            btnBuildName.Text = "生成名称";
            //
            // ProgramManageView
            //
            AutoScaleDimensions = new SizeF(10F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(rootLayout);
            Font = new Font("Microsoft YaHei UI", 10.5F);
            Name = "ProgramManageView";
            Size = new Size(1366, 745);
            rootLayout.ResumeLayout(false);
            rootLayout.PerformLayout();
            toolbar.ResumeLayout(false);
            toolbar.PerformLayout();
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            leftLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvPrograms).EndInit();
            grpRevisions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvRevisions).EndInit();
            editorLayout.ResumeLayout(false);
            editorLayout.PerformLayout();
            fileLayout.ResumeLayout(false);
            fileLayout.PerformLayout();
            LayoutProgramName.ResumeLayout(false);
            LayoutProgramName.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel rootLayout;
        private FlowLayoutPanel toolbar;
        private AntdUI.Button btnNew;
        private AntdUI.Button btnSave;
        private AntdUI.Button btnDelete;
        private AntdUI.Button btnSync;
        private AntdUI.Button btnPullMes;
        private AntdUI.Button btnRefresh;
        private CheckBox chkSyncNow;
        private TextBox txtKeyword;
        private SplitContainer splitMain;
        private TableLayoutPanel leftLayout;
        private DataGridView dgvPrograms;
        private GroupBox grpRevisions;
        private DataGridView dgvRevisions;
        private TableLayoutPanel editorLayout;
        private Label lblCurrentInfo;
        private Label lblProgramName;
        private TextBox txtProgramName;
        private AntdUI.Button btnBuildName;
        private Label lblProductNum;
        private TextBox txtProductNum;
        private Label lblProductModel;
        private TextBox txtProductModel;
        private Label lblComponentCode;
        private TextBox txtComponentCode;
        private Label lblSequenceNumber;
        private TextBox txtSequenceNumber;
        private Label lblProgramType;
        private ComboBox cmbProgramType;
        private Label lblWeldJobName;
        private TextBox txtWeldJobName;
        private Label lblRobotJobName;
        private TextBox txtRobotJobName;
        private Label lblCycleTime;
        private TextBox txtCycleTime;
        private Label lblProgramFile;
        private TableLayoutPanel fileLayout;
        private TextBox txtProgramFile;
        private AntdUI.Button btnBrowseFile;
        private Label lblCommitMessage;
        private TextBox txtCommitMessage;
        private Label lblRemark;
        private TextBox txtRemark;
        private Label lblProgramContent;
        private TextBox txtProgramContent;
        private TableLayoutPanel LayoutProgramName;
    }
}
