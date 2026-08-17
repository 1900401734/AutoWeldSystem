namespace AutoWeldSystem.UI.Views
{
    partial class ProgramManageView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _operationCts?.Cancel();
                _operationCts?.Dispose();
            }

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
            btnSaveAsNew = new AntdUI.Button();
            btnDelete = new AntdUI.Button();
            btnBatchClean = new AntdUI.Button();
            btnSync = new AntdUI.Button();
            btnPullMes = new AntdUI.Button();
            btnRefresh = new AntdUI.Button();
            chkSyncNow = new CheckBox();
            txtKeyword = new TextBox();
            splitMain = new AntdUI.Splitter();
            leftLayout = new TableLayoutPanel();
            tablePrograms = new AntdUI.Table();
            rightLayout = new TableLayoutPanel();
            editorLayout = new TableLayoutPanel();
            tlpProgramId = new TableLayoutPanel();
            txtProgramId = new AntdUI.Input();
            lblProgramId = new AntdUI.Label();
            tlpRemark = new TableLayoutPanel();
            lblRemark = new AntdUI.Label();
            inputRemark = new AntdUI.Input();
            tlpRecipeCode = new TableLayoutPanel();
            lblRecipeCode = new AntdUI.Label();
            selectStation1Recipe = new AntdUI.Select();
            tlpStation2RecipeCode = new TableLayoutPanel();
            lblStation2RecipeCode = new AntdUI.Label();
            selectStation2Recipe = new AntdUI.Select();
            tlpProgramType = new TableLayoutPanel();
            lblProgramType = new AntdUI.Label();
            cmbProgramType = new AntdUI.Select();
            tlpComponentCode = new TableLayoutPanel();
            lblComponentCode = new AntdUI.Label();
            inputComponentCode = new AntdUI.Input();
            tlpSequenceNumber = new TableLayoutPanel();
            lblSequenceNumber = new AntdUI.Label();
            inputSequenceNumber = new AntdUI.Input();
            tlpProductNum = new TableLayoutPanel();
            lblProductNum = new AntdUI.Label();
            inputProductNum = new AntdUI.Input();
            lblCurrentInfo = new AntdUI.Label();
            tlpProgramName = new TableLayoutPanel();
            btnBuildName = new AntdUI.Button();
            inputProgramName = new AntdUI.Input();
            lblProgramName = new AntdUI.Label();
            tlpDescription = new TableLayoutPanel();
            lblDescription = new AntdUI.Label();
            inputDescription = new AntdUI.Input();
            programContentLayout = new TableLayoutPanel();
            lblProgramContent = new AntdUI.Label();
            tableProgramContent = new AntdUI.Table();
            rootLayout.SuspendLayout();
            toolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            leftLayout.SuspendLayout();
            rightLayout.SuspendLayout();
            editorLayout.SuspendLayout();
            tlpProgramId.SuspendLayout();
            tlpRemark.SuspendLayout();
            tlpRecipeCode.SuspendLayout();
            tlpStation2RecipeCode.SuspendLayout();
            tlpProgramType.SuspendLayout();
            tlpComponentCode.SuspendLayout();
            tlpSequenceNumber.SuspendLayout();
            tlpProductNum.SuspendLayout();
            tlpProgramName.SuspendLayout();
            tlpDescription.SuspendLayout();
            programContentLayout.SuspendLayout();
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
            toolbar.Controls.Add(btnSaveAsNew);
            toolbar.Controls.Add(btnDelete);
            toolbar.Controls.Add(btnBatchClean);
            toolbar.Controls.Add(btnSync);
            toolbar.Controls.Add(btnPullMes);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(chkSyncNow);
            toolbar.Controls.Add(txtKeyword);
            toolbar.Dock = DockStyle.Fill;
            toolbar.Location = new Point(0, 0);
            toolbar.Margin = new Padding(0);
            toolbar.Name = "toolbar";
            toolbar.Padding = new Padding(20, 12, 20, 6);
            toolbar.Size = new Size(1366, 58);
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
            // btnSaveAsNew
            //
            btnSaveAsNew.BorderWidth = 1F;
            btnSaveAsNew.IconSvg = "CopyOutlined";
            btnSaveAsNew.Location = new Point(224, 12);
            btnSaveAsNew.Margin = new Padding(0, 0, 10, 0);
            btnSaveAsNew.Name = "btnSaveAsNew";
            btnSaveAsNew.Size = new Size(142, 40);
            btnSaveAsNew.TabIndex = 2;
            btnSaveAsNew.Tag = "perm:button.program.add:enabled";
            btnSaveAsNew.Text = "另存为新程序";
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
            // btnBatchClean
            //
            btnBatchClean.BorderWidth = 1F;
            btnBatchClean.IconSvg = "ClearOutlined";
            btnBatchClean.Location = new Point(326, 12);
            btnBatchClean.Margin = new Padding(0, 0, 10, 0);
            btnBatchClean.Name = "btnBatchClean";
            btnBatchClean.Size = new Size(132, 40);
            btnBatchClean.TabIndex = 3;
            btnBatchClean.Tag = "perm:button.program.delete:enabled";
            btnBatchClean.Text = "批量清理";
            btnBatchClean.Type = AntdUI.TTypeMini.Warn;
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
            splitMain.Location = new Point(1, 59);
            splitMain.Margin = new Padding(1);
            splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(leftLayout);
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(rightLayout);
            splitMain.Size = new Size(1364, 685);
            splitMain.SplitterDistance = 911;
            splitMain.SplitterWidth = 8;
            splitMain.TabIndex = 1;
            // 
            // leftLayout
            // 
            leftLayout.ColumnCount = 1;
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftLayout.Controls.Add(tablePrograms, 0, 0);
            leftLayout.Dock = DockStyle.Fill;
            leftLayout.Location = new Point(0, 0);
            leftLayout.Name = "leftLayout";
            leftLayout.RowCount = 1;
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftLayout.Size = new Size(911, 685);
            leftLayout.TabIndex = 0;
            //
            // tablePrograms
            //
            tablePrograms.Dock = DockStyle.Fill;
            tablePrograms.Location = new Point(3, 3);
            tablePrograms.Name = "tablePrograms";
            tablePrograms.Size = new Size(905, 679);
            tablePrograms.TabIndex = 0;
            //
            // rightLayout
            // 
            rightLayout.ColumnCount = 1;
            rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightLayout.Controls.Add(editorLayout, 0, 0);
            rightLayout.Controls.Add(programContentLayout, 0, 1);
            rightLayout.Dock = DockStyle.Fill;
            rightLayout.Location = new Point(0, 0);
            rightLayout.Name = "rightLayout";
            rightLayout.RowCount = 2;
            rightLayout.RowStyles.Add(new RowStyle());
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100.000008F));
            rightLayout.Size = new Size(445, 685);
            rightLayout.TabIndex = 0;
            // 
            // editorLayout
            // 
            editorLayout.AutoSize = true;
            editorLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            editorLayout.ColumnCount = 1;
            editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            editorLayout.Controls.Add(tlpProgramId, 0, 10);
            editorLayout.Controls.Add(tlpRemark, 0, 9);
            editorLayout.Controls.Add(tlpRecipeCode, 0, 6);
            editorLayout.Controls.Add(tlpStation2RecipeCode, 0, 7);
            editorLayout.Controls.Add(tlpProgramType, 0, 8);
            editorLayout.Controls.Add(tlpComponentCode, 0, 3);
            editorLayout.Controls.Add(tlpSequenceNumber, 0, 4);
            editorLayout.Controls.Add(tlpProductNum, 0, 2);
            editorLayout.Controls.Add(lblCurrentInfo, 0, 0);
            editorLayout.Controls.Add(tlpProgramName, 0, 1);
            editorLayout.Controls.Add(tlpDescription, 0, 5);
            editorLayout.Dock = DockStyle.Fill;
            editorLayout.Location = new Point(0, 0);
            editorLayout.Margin = new Padding(0);
            editorLayout.Name = "editorLayout";
            editorLayout.RowCount = 11;
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            editorLayout.Size = new Size(445, 352);
            editorLayout.TabIndex = 1;
            // 
            // tlpProgramId
            // 
            tlpProgramId.ColumnCount = 2;
            tlpProgramId.ColumnStyles.Add(new ColumnStyle());
            tlpProgramId.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpProgramId.Controls.Add(txtProgramId, 1, 0);
            tlpProgramId.Controls.Add(lblProgramId, 0, 0);
            tlpProgramId.Dock = DockStyle.Fill;
            tlpProgramId.Location = new Point(0, 352);
            tlpProgramId.Margin = new Padding(0);
            tlpProgramId.Name = "tlpProgramId";
            tlpProgramId.RowCount = 1;
            tlpProgramId.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProgramId.Size = new Size(445, 1);
            tlpProgramId.TabIndex = 3;
            // 
            // txtProgramId
            // 
            txtProgramId.Dock = DockStyle.Fill;
            txtProgramId.Location = new Point(58, 0);
            txtProgramId.Margin = new Padding(0);
            txtProgramId.Name = "txtProgramId";
            txtProgramId.ReadOnly = true;
            txtProgramId.Size = new Size(387, 1);
            txtProgramId.TabIndex = 1;
            // 
            // lblProgramId
            // 
            lblProgramId.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProgramId.Dock = DockStyle.Fill;
            lblProgramId.Location = new Point(0, 0);
            lblProgramId.Margin = new Padding(0);
            lblProgramId.Name = "lblProgramId";
            lblProgramId.Padding = new Padding(4, 0, 0, 0);
            lblProgramId.Size = new Size(58, 1);
            lblProgramId.TabIndex = 1;
            lblProgramId.Text = "程序ID";
            // 
            // tlpRemark
            // 
            tlpRemark.ColumnCount = 2;
            tlpRemark.ColumnStyles.Add(new ColumnStyle());
            tlpRemark.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRemark.Controls.Add(lblRemark, 0, 0);
            tlpRemark.Controls.Add(inputRemark, 1, 0);
            tlpRemark.Dock = DockStyle.Fill;
            tlpRemark.Location = new Point(0, 352);
            tlpRemark.Margin = new Padding(0);
            tlpRemark.Name = "tlpRemark";
            tlpRemark.RowCount = 1;
            tlpRemark.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpRemark.Size = new Size(445, 1);
            tlpRemark.TabIndex = 5;
            // 
            // lblRemark
            // 
            lblRemark.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblRemark.Dock = DockStyle.Fill;
            lblRemark.Location = new Point(0, 0);
            lblRemark.Margin = new Padding(0);
            lblRemark.Name = "lblRemark";
            lblRemark.Padding = new Padding(4, 0, 0, 0);
            lblRemark.Size = new Size(76, 1);
            lblRemark.TabIndex = 1;
            lblRemark.Text = "MES备注";
            // 
            // inputRemark
            // 
            inputRemark.Dock = DockStyle.Fill;
            inputRemark.Location = new Point(76, 0);
            inputRemark.Margin = new Padding(0);
            inputRemark.Name = "inputRemark";
            inputRemark.Size = new Size(369, 1);
            inputRemark.TabIndex = 23;
            // 
            // tlpRecipeCode
            // 
            tlpRecipeCode.ColumnCount = 2;
            tlpRecipeCode.ColumnStyles.Add(new ColumnStyle());
            tlpRecipeCode.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRecipeCode.Controls.Add(lblRecipeCode, 0, 0);
            tlpRecipeCode.Controls.Add(selectStation1Recipe, 1, 0);
            tlpRecipeCode.Dock = DockStyle.Fill;
            tlpRecipeCode.Location = new Point(0, 308);
            tlpRecipeCode.Margin = new Padding(0);
            tlpRecipeCode.Name = "tlpRecipeCode";
            tlpRecipeCode.RowCount = 1;
            tlpRecipeCode.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpRecipeCode.Size = new Size(445, 44);
            tlpRecipeCode.TabIndex = 4;
            // 
            // lblRecipeCode
            // 
            lblRecipeCode.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblRecipeCode.Dock = DockStyle.Fill;
            lblRecipeCode.Location = new Point(0, 0);
            lblRecipeCode.Margin = new Padding(0);
            lblRecipeCode.Name = "lblRecipeCode";
            lblRecipeCode.Padding = new Padding(4, 0, 0, 0);
            lblRecipeCode.Prefix = "*";
            lblRecipeCode.PrefixColor = Color.FromArgb(255, 77, 79);
            lblRecipeCode.Size = new Size(74, 44);
            lblRecipeCode.TabIndex = 11;
            lblRecipeCode.Text = "工位1配方名称";
            // 
            // selectStation1Recipe
            // 
            selectStation1Recipe.Dock = DockStyle.Fill;
            selectStation1Recipe.List = true;
            selectStation1Recipe.Location = new Point(74, 0);
            selectStation1Recipe.Margin = new Padding(0);
            selectStation1Recipe.MaxCount = 10;
            selectStation1Recipe.Name = "selectStation1Recipe";
            selectStation1Recipe.Size = new Size(371, 44);
            selectStation1Recipe.TabIndex = 1;
            selectStation1Recipe.WheelModifyEnabled = false;
            // 
            // tlpStation2RecipeCode
            // 
            tlpStation2RecipeCode.ColumnCount = 2;
            tlpStation2RecipeCode.ColumnStyles.Add(new ColumnStyle());
            tlpStation2RecipeCode.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpStation2RecipeCode.Controls.Add(lblStation2RecipeCode, 0, 0);
            tlpStation2RecipeCode.Controls.Add(selectStation2Recipe, 1, 0);
            tlpStation2RecipeCode.Dock = DockStyle.Fill;
            tlpStation2RecipeCode.Location = new Point(0, 352);
            tlpStation2RecipeCode.Margin = new Padding(0);
            tlpStation2RecipeCode.Name = "tlpStation2RecipeCode";
            tlpStation2RecipeCode.RowCount = 1;
            tlpStation2RecipeCode.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpStation2RecipeCode.Size = new Size(445, 1);
            tlpStation2RecipeCode.TabIndex = 5;
            tlpStation2RecipeCode.Visible = false;
            // 
            // lblStation2RecipeCode
            // 
            lblStation2RecipeCode.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblStation2RecipeCode.Dock = DockStyle.Fill;
            lblStation2RecipeCode.Location = new Point(0, 0);
            lblStation2RecipeCode.Margin = new Padding(0);
            lblStation2RecipeCode.Name = "lblStation2RecipeCode";
            lblStation2RecipeCode.Padding = new Padding(4, 0, 0, 0);
            lblStation2RecipeCode.Size = new Size(88, 1);
            lblStation2RecipeCode.TabIndex = 0;
            lblStation2RecipeCode.Text = "工位2配方名称";
            // 
            // selectStation2Recipe
            // 
            selectStation2Recipe.Dock = DockStyle.Fill;
            selectStation2Recipe.List = true;
            selectStation2Recipe.Location = new Point(88, 0);
            selectStation2Recipe.Margin = new Padding(0);
            selectStation2Recipe.MaxCount = 10;
            selectStation2Recipe.Name = "selectStation2Recipe";
            selectStation2Recipe.Size = new Size(357, 1);
            selectStation2Recipe.TabIndex = 1;
            selectStation2Recipe.WheelModifyEnabled = false;
            // 
            // tlpProgramType
            // 
            tlpProgramType.ColumnCount = 2;
            tlpProgramType.ColumnStyles.Add(new ColumnStyle());
            tlpProgramType.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpProgramType.Controls.Add(lblProgramType, 0, 0);
            tlpProgramType.Controls.Add(cmbProgramType, 1, 0);
            tlpProgramType.Dock = DockStyle.Fill;
            tlpProgramType.Location = new Point(0, 352);
            tlpProgramType.Margin = new Padding(0);
            tlpProgramType.Name = "tlpProgramType";
            tlpProgramType.RowCount = 1;
            tlpProgramType.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProgramType.Size = new Size(445, 1);
            tlpProgramType.TabIndex = 3;
            tlpProgramType.Visible = false;
            // 
            // lblProgramType
            // 
            lblProgramType.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProgramType.Dock = DockStyle.Fill;
            lblProgramType.Location = new Point(0, 0);
            lblProgramType.Margin = new Padding(0);
            lblProgramType.Name = "lblProgramType";
            lblProgramType.Padding = new Padding(4, 0, 0, 0);
            lblProgramType.Size = new Size(74, 1);
            lblProgramType.TabIndex = 1;
            lblProgramType.Text = "程序类型";
            // 
            // cmbProgramType
            // 
            cmbProgramType.Dock = DockStyle.Fill;
            cmbProgramType.Location = new Point(74, 0);
            cmbProgramType.Margin = new Padding(0);
            cmbProgramType.MaxCount = 10;
            cmbProgramType.Name = "cmbProgramType";
            cmbProgramType.Size = new Size(371, 1);
            cmbProgramType.TabIndex = 13;
            // 
            // tlpComponentCode
            // 
            tlpComponentCode.ColumnCount = 2;
            tlpComponentCode.ColumnStyles.Add(new ColumnStyle());
            tlpComponentCode.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpComponentCode.Controls.Add(lblComponentCode, 0, 0);
            tlpComponentCode.Controls.Add(inputComponentCode, 1, 0);
            tlpComponentCode.Dock = DockStyle.Fill;
            tlpComponentCode.Location = new Point(0, 132);
            tlpComponentCode.Margin = new Padding(0);
            tlpComponentCode.Name = "tlpComponentCode";
            tlpComponentCode.RowCount = 1;
            tlpComponentCode.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpComponentCode.Size = new Size(445, 44);
            tlpComponentCode.TabIndex = 1;
            // 
            // lblComponentCode
            // 
            lblComponentCode.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblComponentCode.Dock = DockStyle.Fill;
            lblComponentCode.Location = new Point(0, 0);
            lblComponentCode.Margin = new Padding(0);
            lblComponentCode.Name = "lblComponentCode";
            lblComponentCode.Padding = new Padding(4, 0, 0, 0);
            lblComponentCode.Prefix = "*";
            lblComponentCode.PrefixColor = Color.FromArgb(255, 77, 79);
            lblComponentCode.Size = new Size(92, 44);
            lblComponentCode.TabIndex = 11;
            lblComponentCode.Text = "零组件代码";
            // 
            // inputComponentCode
            // 
            inputComponentCode.Dock = DockStyle.Fill;
            inputComponentCode.Location = new Point(92, 0);
            inputComponentCode.Margin = new Padding(0);
            inputComponentCode.Name = "inputComponentCode";
            inputComponentCode.Size = new Size(353, 44);
            inputComponentCode.TabIndex = 1;
            // 
            // tlpSequenceNumber
            // 
            tlpSequenceNumber.ColumnCount = 2;
            tlpSequenceNumber.ColumnStyles.Add(new ColumnStyle());
            tlpSequenceNumber.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpSequenceNumber.Controls.Add(lblSequenceNumber, 0, 0);
            tlpSequenceNumber.Controls.Add(inputSequenceNumber, 1, 0);
            tlpSequenceNumber.Dock = DockStyle.Fill;
            tlpSequenceNumber.Location = new Point(0, 176);
            tlpSequenceNumber.Margin = new Padding(0);
            tlpSequenceNumber.Name = "tlpSequenceNumber";
            tlpSequenceNumber.RowCount = 1;
            tlpSequenceNumber.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpSequenceNumber.Size = new Size(445, 44);
            tlpSequenceNumber.TabIndex = 3;
            // 
            // lblSequenceNumber
            // 
            lblSequenceNumber.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblSequenceNumber.Dock = DockStyle.Fill;
            lblSequenceNumber.Location = new Point(0, 0);
            lblSequenceNumber.Margin = new Padding(0);
            lblSequenceNumber.Name = "lblSequenceNumber";
            lblSequenceNumber.Padding = new Padding(4, 0, 0, 0);
            lblSequenceNumber.Size = new Size(57, 44);
            lblSequenceNumber.TabIndex = 11;
            lblSequenceNumber.Text = "流水号";
            // 
            // inputSequenceNumber
            // 
            inputSequenceNumber.Dock = DockStyle.Fill;
            inputSequenceNumber.Location = new Point(57, 0);
            inputSequenceNumber.Margin = new Padding(0);
            inputSequenceNumber.Name = "inputSequenceNumber";
            inputSequenceNumber.Size = new Size(388, 44);
            inputSequenceNumber.TabIndex = 1;
            // 
            // tlpProductNum
            // 
            tlpProductNum.ColumnCount = 2;
            tlpProductNum.ColumnStyles.Add(new ColumnStyle());
            tlpProductNum.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpProductNum.Controls.Add(lblProductNum, 0, 0);
            tlpProductNum.Controls.Add(inputProductNum, 1, 0);
            tlpProductNum.Dock = DockStyle.Fill;
            tlpProductNum.Location = new Point(0, 88);
            tlpProductNum.Margin = new Padding(0);
            tlpProductNum.Name = "tlpProductNum";
            tlpProductNum.RowCount = 1;
            tlpProductNum.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProductNum.Size = new Size(445, 44);
            tlpProductNum.TabIndex = 2;
            // 
            // lblProductNum
            // 
            lblProductNum.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProductNum.Dock = DockStyle.Fill;
            lblProductNum.Location = new Point(0, 0);
            lblProductNum.Margin = new Padding(0);
            lblProductNum.Name = "lblProductNum";
            lblProductNum.Padding = new Padding(4, 0, 0, 0);
            lblProductNum.Prefix = "*";
            lblProductNum.PrefixColor = Color.FromArgb(255, 77, 79);
            lblProductNum.Size = new Size(74, 44);
            lblProductNum.TabIndex = 11;
            lblProductNum.Text = "产品工号";
            // 
            // inputProductNum
            // 
            inputProductNum.Dock = DockStyle.Fill;
            inputProductNum.Location = new Point(74, 0);
            inputProductNum.Margin = new Padding(0);
            inputProductNum.Name = "inputProductNum";
            inputProductNum.Size = new Size(371, 44);
            inputProductNum.TabIndex = 1;
            // 
            // lblCurrentInfo
            // 
            lblCurrentInfo.Dock = DockStyle.Fill;
            lblCurrentInfo.Location = new Point(0, 0);
            lblCurrentInfo.Margin = new Padding(0);
            lblCurrentInfo.Name = "lblCurrentInfo";
            lblCurrentInfo.Padding = new Padding(4, 0, 0, 0);
            lblCurrentInfo.Size = new Size(445, 44);
            lblCurrentInfo.TabIndex = 1;
            lblCurrentInfo.Text = "xxx";
            // 
            // tlpProgramName
            // 
            tlpProgramName.ColumnCount = 3;
            tlpProgramName.ColumnStyles.Add(new ColumnStyle());
            tlpProgramName.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpProgramName.ColumnStyles.Add(new ColumnStyle());
            tlpProgramName.Controls.Add(btnBuildName, 2, 0);
            tlpProgramName.Controls.Add(inputProgramName, 1, 0);
            tlpProgramName.Controls.Add(lblProgramName, 0, 0);
            tlpProgramName.Dock = DockStyle.Fill;
            tlpProgramName.Location = new Point(0, 44);
            tlpProgramName.Margin = new Padding(0);
            tlpProgramName.Name = "tlpProgramName";
            tlpProgramName.RowCount = 1;
            tlpProgramName.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProgramName.Size = new Size(445, 44);
            tlpProgramName.TabIndex = 1;
            // 
            // btnBuildName
            // 
            btnBuildName.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnBuildName.BorderWidth = 1F;
            btnBuildName.Dock = DockStyle.Fill;
            btnBuildName.IconSvg = "BranchesOutlined";
            btnBuildName.Location = new Point(321, 0);
            btnBuildName.Margin = new Padding(0);
            btnBuildName.Name = "btnBuildName";
            btnBuildName.Size = new Size(124, 44);
            btnBuildName.TabIndex = 3;
            btnBuildName.Tag = "perm:button.program.build-name:enabled";
            btnBuildName.Text = "生成名称";
            // 
            // inputProgramName
            // 
            inputProgramName.Dock = DockStyle.Fill;
            inputProgramName.Location = new Point(74, 0);
            inputProgramName.Margin = new Padding(0);
            inputProgramName.Name = "inputProgramName";
            inputProgramName.Size = new Size(247, 44);
            inputProgramName.TabIndex = 12;
            // 
            // lblProgramName
            // 
            lblProgramName.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblProgramName.Dock = DockStyle.Fill;
            lblProgramName.Location = new Point(0, 0);
            lblProgramName.Margin = new Padding(0);
            lblProgramName.Name = "lblProgramName";
            lblProgramName.Padding = new Padding(4, 0, 0, 0);
            lblProgramName.Size = new Size(74, 44);
            lblProgramName.TabIndex = 11;
            lblProgramName.Text = "程序名称";
            // 
            // tlpDescription
            // 
            tlpDescription.ColumnCount = 2;
            tlpDescription.ColumnStyles.Add(new ColumnStyle());
            tlpDescription.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpDescription.Controls.Add(lblDescription, 0, 0);
            tlpDescription.Controls.Add(inputDescription, 1, 0);
            tlpDescription.Dock = DockStyle.Fill;
            tlpDescription.Location = new Point(0, 264);
            tlpDescription.Margin = new Padding(0);
            tlpDescription.Name = "tlpDescription";
            tlpDescription.RowCount = 1;
            tlpDescription.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpDescription.Size = new Size(445, 44);
            tlpDescription.TabIndex = 6;
            // 
            // lblDescription
            // 
            lblDescription.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDescription.Dock = DockStyle.Fill;
            lblDescription.Location = new Point(0, 0);
            lblDescription.Margin = new Padding(0);
            lblDescription.Name = "lblDescription";
            lblDescription.Padding = new Padding(4, 0, 0, 0);
            lblDescription.Size = new Size(39, 44);
            lblDescription.TabIndex = 1;
            lblDescription.Text = "描述";
            // 
            // inputDescription
            // 
            inputDescription.Dock = DockStyle.Fill;
            inputDescription.Location = new Point(39, 0);
            inputDescription.Margin = new Padding(0);
            inputDescription.Name = "inputDescription";
            inputDescription.Size = new Size(406, 44);
            inputDescription.TabIndex = 1;
            // 
            // programContentLayout
            // 
            programContentLayout.ColumnCount = 1;
            programContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            programContentLayout.Controls.Add(lblProgramContent, 0, 0);
            programContentLayout.Controls.Add(tableProgramContent, 0, 1);
            programContentLayout.Dock = DockStyle.Fill;
            programContentLayout.Location = new Point(0, 352);
            programContentLayout.Margin = new Padding(0);
            programContentLayout.Name = "programContentLayout";
            programContentLayout.RowCount = 2;
            programContentLayout.RowStyles.Add(new RowStyle());
            programContentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            programContentLayout.Size = new Size(445, 333);
            programContentLayout.TabIndex = 2;
            // 
            // lblProgramContent
            // 
            lblProgramContent.Dock = DockStyle.Fill;
            lblProgramContent.Location = new Point(0, 0);
            lblProgramContent.Margin = new Padding(0);
            lblProgramContent.Name = "lblProgramContent";
            lblProgramContent.Padding = new Padding(4, 0, 0, 0);
            lblProgramContent.Size = new Size(445, 50);
            lblProgramContent.TabIndex = 1;
            lblProgramContent.Text = "程序内容";
            // 
            // tableProgramContent
            // 
            tableProgramContent.Dock = DockStyle.Fill;
            tableProgramContent.Gap = 12;
            tableProgramContent.Location = new Point(0, 50);
            tableProgramContent.Margin = new Padding(0);
            tableProgramContent.Name = "tableProgramContent";
            tableProgramContent.Size = new Size(445, 283);
            tableProgramContent.TabIndex = 27;
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
            rightLayout.ResumeLayout(false);
            rightLayout.PerformLayout();
            editorLayout.ResumeLayout(false);
            tlpProgramId.ResumeLayout(false);
            tlpProgramId.PerformLayout();
            tlpRemark.ResumeLayout(false);
            tlpRemark.PerformLayout();
            tlpRecipeCode.ResumeLayout(false);
            tlpRecipeCode.PerformLayout();
            tlpStation2RecipeCode.ResumeLayout(false);
            tlpStation2RecipeCode.PerformLayout();
            tlpProgramType.ResumeLayout(false);
            tlpProgramType.PerformLayout();
            tlpComponentCode.ResumeLayout(false);
            tlpComponentCode.PerformLayout();
            tlpSequenceNumber.ResumeLayout(false);
            tlpSequenceNumber.PerformLayout();
            tlpProductNum.ResumeLayout(false);
            tlpProductNum.PerformLayout();
            tlpProgramName.ResumeLayout(false);
            tlpProgramName.PerformLayout();
            tlpDescription.ResumeLayout(false);
            tlpDescription.PerformLayout();
            programContentLayout.ResumeLayout(false);
            ResumeLayout(false);
        }

        private TableLayoutPanel rootLayout;
        private FlowLayoutPanel toolbar;
        private AntdUI.Button btnNew;
        private AntdUI.Button btnSave;
        private AntdUI.Button btnSaveAsNew;
        private AntdUI.Button btnDelete;
        private AntdUI.Button btnBatchClean;
        private AntdUI.Button btnSync;
        private AntdUI.Button btnPullMes;
        private AntdUI.Button btnRefresh;
        private CheckBox chkSyncNow;
        private TextBox txtKeyword;
        private TableLayoutPanel leftLayout;
        private AntdUI.Table tablePrograms;
        private AntdUI.Label lblProgramName;
        private AntdUI.Button btnBuildName;
        private AntdUI.Label lblProductNum;
        private AntdUI.Label lblRecipeCode;
        private AntdUI.Select cmbProgramType;
        private AntdUI.Input inputRemark;
        private AntdUI.Table tableProgramContent;
        private AntdUI.Splitter splitMain;
        private TableLayoutPanel editorLayout;
        private AntdUI.Label lblProgramId;
        private AntdUI.Label lblComponentCode;
        private AntdUI.Label lblSequenceNumber;
        private AntdUI.Input inputProgramName;
        private AntdUI.Label lblProgramType;
        private AntdUI.Label lblRemark;
        private AntdUI.Label lblDescription;
        private AntdUI.Label lblProgramContent;
        private AntdUI.Input inputProductNum;
        private TableLayoutPanel tlpProgramName;
        private AntdUI.Select selectStation1Recipe;
        private AntdUI.Select selectStation2Recipe;
        private AntdUI.Label lblStation2RecipeCode;
        private AntdUI.Input inputSequenceNumber;
        private AntdUI.Input inputComponentCode;
        private AntdUI.Input inputDescription;
        private AntdUI.Input txtProgramId;
        private TableLayoutPanel tlpComponentCode;
        private TableLayoutPanel tlpProgramId;
        private TableLayoutPanel tlpProgramType;
        private TableLayoutPanel tlpSequenceNumber;
        private TableLayoutPanel tlpRecipeCode;
        private TableLayoutPanel tlpStation2RecipeCode;
        private TableLayoutPanel tlpProductNum;
        private TableLayoutPanel tlpDescription;
        private TableLayoutPanel tlpRemark;
        private AntdUI.Label lblCurrentInfo;
        private TableLayoutPanel rightLayout;
        private TableLayoutPanel programContentLayout;
    }
}
