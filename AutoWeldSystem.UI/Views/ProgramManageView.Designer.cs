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
            splitMain = new AntdUI.Splitter();
            leftLayout = new TableLayoutPanel();
            dgvPrograms = new DataGridView();
            grpRevisions = new GroupBox();
            dgvRevisions = new DataGridView();
            editorLayout = new TableLayoutPanel();
            cmbRemark = new ComboBox();
            fileLayout = new TableLayoutPanel();
            txtProgramFile = new TextBox();
            btnBrowseFile = new AntdUI.Button();
            txtProgramId = new TextBox();
            lblProgramId = new Label();
            lblProgramFile = new Label();
            lblCommitMessage = new Label();
            txtLocalRemark = new TextBox();
            lblLocalRemark = new Label();
            lblProgramContent = new Label();
            cmbProgramType = new ComboBox();
            txtSequenceNumber = new TextBox();
            txtComponentCode = new TextBox();
            txtRecipeCode = new TextBox();
            txtProductModel = new TextBox();
            lblRecipeCode = new Label();
            lblProductModel = new Label();
            lblComponentCode = new Label();
            lblSequenceNumber = new Label();
            lblProgramType = new Label();
            txtProductNum = new TextBox();
            lblProductNum = new Label();
            lblProgramName = new Label();
            lblCurrentInfo = new Label();
            LayoutProgramName = new TableLayoutPanel();
            txtProgramName = new TextBox();
            btnBuildName = new AntdUI.Button();
            txtProgramContent = new TextBox();
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
            rootLayout.Margin = new Padding(0);
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
            btnNew.Tag = "perm:button.program.add:enabled";
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
            btnSave.Tag = "perm:button.program.edit:enabled";
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
            btnDelete.Tag = "perm:button.program.delete:enabled";
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
            btnSync.Tag = "perm:button.program.sync:enabled";
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
            btnPullMes.Tag = "perm:button.program.pull-mes:enabled";
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
            btnRefresh.Tag = "perm:button.program.refresh:enabled";
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
            splitMain.Location = new Point(0, 64);
            splitMain.Margin = new Padding(0);
            splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(leftLayout);
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(editorLayout);
            splitMain.Size = new Size(1366, 681);
            splitMain.SplitterDistance = 731;
            splitMain.SplitterWidth = 8;
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
            leftLayout.Size = new Size(731, 681);
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
            dgvPrograms.Size = new Size(725, 457);
            dgvPrograms.TabIndex = 0;
            // 
            // grpRevisions
            // 
            grpRevisions.Controls.Add(dgvRevisions);
            grpRevisions.Dock = DockStyle.Fill;
            grpRevisions.Location = new Point(3, 466);
            grpRevisions.Name = "grpRevisions";
            grpRevisions.Size = new Size(725, 212);
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
            dgvRevisions.Size = new Size(719, 183);
            dgvRevisions.TabIndex = 0;
            // 
            // editorLayout
            // 
            editorLayout.ColumnCount = 2;
            editorLayout.ColumnStyles.Add(new ColumnStyle());
            editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            editorLayout.Controls.Add(cmbRemark, 1, 10);
            editorLayout.Controls.Add(fileLayout, 1, 9);
            editorLayout.Controls.Add(txtProgramId, 1, 8);
            editorLayout.Controls.Add(lblProgramId, 0, 8);
            editorLayout.Controls.Add(lblProgramFile, 0, 9);
            editorLayout.Controls.Add(lblCommitMessage, 0, 10);
            editorLayout.Controls.Add(txtLocalRemark, 1, 11);
            editorLayout.Controls.Add(lblLocalRemark, 0, 11);
            editorLayout.Controls.Add(lblProgramContent, 0, 12);
            editorLayout.Controls.Add(cmbProgramType, 1, 7);
            editorLayout.Controls.Add(txtSequenceNumber, 1, 6);
            editorLayout.Controls.Add(txtComponentCode, 1, 5);
            editorLayout.Controls.Add(txtRecipeCode, 1, 4);
            editorLayout.Controls.Add(txtProductModel, 1, 3);
            editorLayout.Controls.Add(lblRecipeCode, 0, 4);
            editorLayout.Controls.Add(lblProductModel, 0, 3);
            editorLayout.Controls.Add(lblComponentCode, 0, 5);
            editorLayout.Controls.Add(lblSequenceNumber, 0, 6);
            editorLayout.Controls.Add(lblProgramType, 0, 7);
            editorLayout.Controls.Add(txtProductNum, 1, 2);
            editorLayout.Controls.Add(lblProductNum, 0, 2);
            editorLayout.Controls.Add(lblProgramName, 0, 1);
            editorLayout.Controls.Add(lblCurrentInfo, 0, 0);
            editorLayout.Controls.Add(LayoutProgramName, 1, 1);
            editorLayout.Controls.Add(txtProgramContent, 0, 13);
            editorLayout.Dock = DockStyle.Fill;
            editorLayout.Location = new Point(0, 0);
            editorLayout.Margin = new Padding(0);
            editorLayout.Name = "editorLayout";
            editorLayout.RowCount = 14;
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            editorLayout.Size = new Size(627, 681);
            editorLayout.TabIndex = 1;
            // 
            // cmbRemark
            // 
            cmbRemark.Dock = DockStyle.Fill;
            cmbRemark.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRemark.Enabled = false;
            cmbRemark.Location = new Point(118, 400);
            cmbRemark.Margin = new Padding(0);
            cmbRemark.Name = "cmbRemark";
            cmbRemark.Size = new Size(509, 31);
            cmbRemark.TabIndex = 23;
            cmbRemark.TabStop = false;
            cmbRemark.Visible = false;
            // 
            // fileLayout
            // 
            fileLayout.ColumnCount = 2;
            fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            fileLayout.ColumnStyles.Add(new ColumnStyle());
            fileLayout.Controls.Add(txtProgramFile, 0, 0);
            fileLayout.Controls.Add(btnBrowseFile, 1, 0);
            fileLayout.Dock = DockStyle.Fill;
            fileLayout.Location = new Point(118, 360);
            fileLayout.Margin = new Padding(0);
            fileLayout.Name = "fileLayout";
            fileLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            fileLayout.Size = new Size(509, 40);
            fileLayout.TabIndex = 21;
            // 
            // txtProgramFile
            // 
            txtProgramFile.Dock = DockStyle.Fill;
            txtProgramFile.Location = new Point(0, 0);
            txtProgramFile.Margin = new Padding(0);
            txtProgramFile.Name = "txtProgramFile";
            txtProgramFile.Size = new Size(413, 30);
            txtProgramFile.TabIndex = 0;
            txtProgramFile.Visible = false;
            // 
            // btnBrowseFile
            // 
            btnBrowseFile.BorderWidth = 1F;
            btnBrowseFile.Dock = DockStyle.Fill;
            btnBrowseFile.IconSvg = "FolderOpenOutlined";
            btnBrowseFile.Location = new Point(413, 0);
            btnBrowseFile.Margin = new Padding(0);
            btnBrowseFile.Name = "btnBrowseFile";
            btnBrowseFile.Size = new Size(96, 40);
            btnBrowseFile.TabIndex = 1;
            btnBrowseFile.Tag = "perm:button.program.browse-file:enabled";
            btnBrowseFile.Text = "选择";
            btnBrowseFile.Visible = false;
            // 
            // txtProgramId
            // 
            txtProgramId.Dock = DockStyle.Fill;
            txtProgramId.Location = new Point(118, 320);
            txtProgramId.Margin = new Padding(0);
            txtProgramId.Name = "txtProgramId";
            txtProgramId.ReadOnly = true;
            txtProgramId.Size = new Size(509, 30);
            txtProgramId.TabIndex = 15;
            txtProgramId.Visible = false;
            // 
            // lblProgramId
            // 
            lblProgramId.Dock = DockStyle.Fill;
            lblProgramId.Location = new Point(0, 320);
            lblProgramId.Margin = new Padding(0);
            lblProgramId.Name = "lblProgramId";
            lblProgramId.Size = new Size(118, 40);
            lblProgramId.TabIndex = 14;
            lblProgramId.Text = "MES程序ID";
            lblProgramId.TextAlign = ContentAlignment.MiddleLeft;
            lblProgramId.Visible = false;
            // 
            // lblProgramFile
            // 
            lblProgramFile.Dock = DockStyle.Fill;
            lblProgramFile.Location = new Point(0, 360);
            lblProgramFile.Margin = new Padding(0);
            lblProgramFile.Name = "lblProgramFile";
            lblProgramFile.Size = new Size(118, 40);
            lblProgramFile.TabIndex = 20;
            lblProgramFile.Text = "程序文件";
            lblProgramFile.TextAlign = ContentAlignment.MiddleLeft;
            lblProgramFile.Visible = false;
            // 
            // lblCommitMessage
            // 
            lblCommitMessage.Dock = DockStyle.Fill;
            lblCommitMessage.Location = new Point(0, 400);
            lblCommitMessage.Margin = new Padding(0);
            lblCommitMessage.Name = "lblCommitMessage";
            lblCommitMessage.Size = new Size(118, 40);
            lblCommitMessage.TabIndex = 22;
            lblCommitMessage.Text = "MES备注";
            lblCommitMessage.TextAlign = ContentAlignment.MiddleLeft;
            lblCommitMessage.Visible = false;
            // 
            // txtLocalRemark
            // 
            txtLocalRemark.Dock = DockStyle.Fill;
            txtLocalRemark.Location = new Point(118, 440);
            txtLocalRemark.Margin = new Padding(0);
            txtLocalRemark.Name = "txtLocalRemark";
            txtLocalRemark.Size = new Size(509, 30);
            txtLocalRemark.TabIndex = 24;
            // 
            // lblLocalRemark
            // 
            lblLocalRemark.Dock = DockStyle.Fill;
            lblLocalRemark.Location = new Point(0, 440);
            lblLocalRemark.Margin = new Padding(0);
            lblLocalRemark.Name = "lblLocalRemark";
            lblLocalRemark.Size = new Size(118, 40);
            lblLocalRemark.TabIndex = 25;
            lblLocalRemark.Text = "本地备注";
            lblLocalRemark.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblProgramContent
            // 
            editorLayout.SetColumnSpan(lblProgramContent, 2);
            lblProgramContent.Dock = DockStyle.Fill;
            lblProgramContent.Location = new Point(0, 480);
            lblProgramContent.Margin = new Padding(0);
            lblProgramContent.Name = "lblProgramContent";
            lblProgramContent.Size = new Size(627, 40);
            lblProgramContent.TabIndex = 26;
            lblProgramContent.Text = "程序内容";
            lblProgramContent.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbProgramType
            // 
            cmbProgramType.Dock = DockStyle.Fill;
            cmbProgramType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProgramType.Items.AddRange(new object[] { "0 - 参数字符串", "1 - 文件" });
            cmbProgramType.Location = new Point(118, 280);
            cmbProgramType.Margin = new Padding(0);
            cmbProgramType.Name = "cmbProgramType";
            cmbProgramType.Size = new Size(509, 31);
            cmbProgramType.TabIndex = 13;
            cmbProgramType.Visible = false;
            // 
            // txtSequenceNumber
            // 
            txtSequenceNumber.Dock = DockStyle.Fill;
            txtSequenceNumber.Location = new Point(118, 240);
            txtSequenceNumber.Margin = new Padding(0);
            txtSequenceNumber.Name = "txtSequenceNumber";
            txtSequenceNumber.Size = new Size(509, 30);
            txtSequenceNumber.TabIndex = 11;
            // 
            // txtComponentCode
            // 
            txtComponentCode.Dock = DockStyle.Fill;
            txtComponentCode.Location = new Point(118, 200);
            txtComponentCode.Margin = new Padding(0);
            txtComponentCode.Name = "txtComponentCode";
            txtComponentCode.Size = new Size(509, 30);
            txtComponentCode.TabIndex = 9;
            // 
            // txtRecipeCode
            // 
            txtRecipeCode.Dock = DockStyle.Fill;
            txtRecipeCode.Location = new Point(118, 160);
            txtRecipeCode.Margin = new Padding(0);
            txtRecipeCode.Name = "txtRecipeCode";
            txtRecipeCode.Size = new Size(509, 30);
            txtRecipeCode.TabIndex = 8;
            // 
            // txtProductModel
            // 
            txtProductModel.Dock = DockStyle.Fill;
            txtProductModel.Location = new Point(118, 120);
            txtProductModel.Margin = new Padding(0);
            txtProductModel.Name = "txtProductModel";
            txtProductModel.Size = new Size(509, 30);
            txtProductModel.TabIndex = 7;
            // 
            // lblRecipeCode
            // 
            lblRecipeCode.Dock = DockStyle.Fill;
            lblRecipeCode.Location = new Point(0, 160);
            lblRecipeCode.Margin = new Padding(0);
            lblRecipeCode.Name = "lblRecipeCode";
            lblRecipeCode.Size = new Size(118, 40);
            lblRecipeCode.TabIndex = 9;
            lblRecipeCode.Text = "配方编号";
            lblRecipeCode.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblProductModel
            // 
            lblProductModel.Dock = DockStyle.Fill;
            lblProductModel.Location = new Point(0, 120);
            lblProductModel.Margin = new Padding(0);
            lblProductModel.Name = "lblProductModel";
            lblProductModel.Size = new Size(118, 40);
            lblProductModel.TabIndex = 6;
            lblProductModel.Text = "产品型号";
            lblProductModel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblComponentCode
            // 
            lblComponentCode.Dock = DockStyle.Fill;
            lblComponentCode.Location = new Point(0, 200);
            lblComponentCode.Margin = new Padding(0);
            lblComponentCode.Name = "lblComponentCode";
            lblComponentCode.Size = new Size(118, 40);
            lblComponentCode.TabIndex = 8;
            lblComponentCode.Text = "零组件代码";
            lblComponentCode.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblSequenceNumber
            // 
            lblSequenceNumber.Dock = DockStyle.Fill;
            lblSequenceNumber.Location = new Point(0, 240);
            lblSequenceNumber.Margin = new Padding(0);
            lblSequenceNumber.Name = "lblSequenceNumber";
            lblSequenceNumber.Size = new Size(118, 40);
            lblSequenceNumber.TabIndex = 10;
            lblSequenceNumber.Text = "流水号";
            lblSequenceNumber.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblProgramType
            // 
            lblProgramType.Dock = DockStyle.Fill;
            lblProgramType.Location = new Point(0, 280);
            lblProgramType.Margin = new Padding(0);
            lblProgramType.Name = "lblProgramType";
            lblProgramType.Size = new Size(118, 40);
            lblProgramType.TabIndex = 12;
            lblProgramType.Text = "程序类型";
            lblProgramType.TextAlign = ContentAlignment.MiddleLeft;
            lblProgramType.Visible = false;
            // 
            // txtProductNum
            // 
            txtProductNum.Dock = DockStyle.Fill;
            txtProductNum.Location = new Point(118, 80);
            txtProductNum.Margin = new Padding(0);
            txtProductNum.Name = "txtProductNum";
            txtProductNum.Size = new Size(509, 30);
            txtProductNum.TabIndex = 5;
            // 
            // lblProductNum
            // 
            lblProductNum.Dock = DockStyle.Fill;
            lblProductNum.Location = new Point(0, 80);
            lblProductNum.Margin = new Padding(0);
            lblProductNum.Name = "lblProductNum";
            lblProductNum.Size = new Size(118, 40);
            lblProductNum.TabIndex = 4;
            lblProductNum.Text = "产品工号";
            lblProductNum.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblProgramName
            // 
            lblProgramName.Dock = DockStyle.Fill;
            lblProgramName.Location = new Point(0, 40);
            lblProgramName.Margin = new Padding(0);
            lblProgramName.Name = "lblProgramName";
            lblProgramName.Size = new Size(118, 40);
            lblProgramName.TabIndex = 1;
            lblProgramName.Text = "程序名称";
            lblProgramName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblCurrentInfo
            // 
            lblCurrentInfo.AutoEllipsis = true;
            editorLayout.SetColumnSpan(lblCurrentInfo, 2);
            lblCurrentInfo.Dock = DockStyle.Fill;
            lblCurrentInfo.ForeColor = SystemColors.GrayText;
            lblCurrentInfo.Location = new Point(0, 0);
            lblCurrentInfo.Margin = new Padding(0);
            lblCurrentInfo.Name = "lblCurrentInfo";
            lblCurrentInfo.Size = new Size(627, 40);
            lblCurrentInfo.TabIndex = 0;
            lblCurrentInfo.Text = "xxx";
            // 
            // LayoutProgramName
            // 
            LayoutProgramName.ColumnCount = 2;
            LayoutProgramName.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            LayoutProgramName.ColumnStyles.Add(new ColumnStyle());
            LayoutProgramName.Controls.Add(txtProgramName, 0, 0);
            LayoutProgramName.Controls.Add(btnBuildName, 1, 0);
            LayoutProgramName.Dock = DockStyle.Fill;
            LayoutProgramName.Location = new Point(118, 40);
            LayoutProgramName.Margin = new Padding(0);
            LayoutProgramName.Name = "LayoutProgramName";
            LayoutProgramName.RowCount = 1;
            LayoutProgramName.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            LayoutProgramName.Size = new Size(509, 40);
            LayoutProgramName.TabIndex = 28;
            // 
            // txtProgramName
            // 
            txtProgramName.Dock = DockStyle.Fill;
            txtProgramName.Location = new Point(0, 0);
            txtProgramName.Margin = new Padding(0);
            txtProgramName.Name = "txtProgramName";
            txtProgramName.Size = new Size(391, 30);
            txtProgramName.TabIndex = 2;
            // 
            // btnBuildName
            // 
            btnBuildName.BorderWidth = 1F;
            btnBuildName.Dock = DockStyle.Fill;
            btnBuildName.IconSvg = "BranchesOutlined";
            btnBuildName.Location = new Point(391, 0);
            btnBuildName.Margin = new Padding(0);
            btnBuildName.Name = "btnBuildName";
            btnBuildName.Size = new Size(118, 40);
            btnBuildName.TabIndex = 3;
            btnBuildName.Tag = "perm:button.program.build-name:enabled";
            btnBuildName.Text = "生成名称";
            // 
            // txtProgramContent
            // 
            txtProgramContent.AcceptsReturn = true;
            txtProgramContent.AcceptsTab = true;
            editorLayout.SetColumnSpan(txtProgramContent, 2);
            txtProgramContent.Dock = DockStyle.Fill;
            txtProgramContent.Font = new Font("Consolas", 10F);
            txtProgramContent.Location = new Point(0, 520);
            txtProgramContent.Margin = new Padding(0);
            txtProgramContent.Multiline = true;
            txtProgramContent.Name = "txtProgramContent";
            txtProgramContent.ScrollBars = ScrollBars.Both;
            txtProgramContent.Size = new Size(627, 161);
            txtProgramContent.TabIndex = 27;
            txtProgramContent.WordWrap = false;
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
        private TableLayoutPanel leftLayout;
        private DataGridView dgvPrograms;
        private GroupBox grpRevisions;
        private DataGridView dgvRevisions;
        private Label lblCurrentInfo;
        private Label lblProgramName;
        private TextBox txtProgramName;
        private AntdUI.Button btnBuildName;
        private Label lblProgramId;
        private TextBox txtProgramId;
        private Label lblProductNum;
        private TextBox txtProductNum;
        private Label lblProductModel;
        private TextBox txtProductModel;
        private Label lblRecipeCode;
        private TextBox txtRecipeCode;
        private Label lblComponentCode;
        private TextBox txtComponentCode;
        private Label lblSequenceNumber;
        private TextBox txtSequenceNumber;
        private Label lblProgramType;
        private ComboBox cmbProgramType;
        private Label lblProgramFile;
        private TableLayoutPanel fileLayout;
        private TextBox txtProgramFile;
        private AntdUI.Button btnBrowseFile;
        private Label lblCommitMessage;
        private ComboBox cmbRemark;
        private Label lblLocalRemark;
        private TextBox txtLocalRemark;
        private Label lblProgramContent;
        private TextBox txtProgramContent;
        private TableLayoutPanel LayoutProgramName;
        private AntdUI.Splitter splitMain;
        private TableLayoutPanel editorLayout;
    }
}
