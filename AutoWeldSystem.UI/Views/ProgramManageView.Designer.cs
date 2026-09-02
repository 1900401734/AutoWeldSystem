namespace AutoWeldSystem.UI.Views
{
    partial class ProgramManageView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeOperationCts();
            }

            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            rootLayout = new TableLayoutPanel();
            splitMain = new AntdUI.Splitter();
            leftLayout = new TableLayoutPanel();
            tablePrograms = new AntdUI.Table();
            programPagination = new AntdUI.Pagination();
            rightLayout = new TableLayoutPanel();
            editorLayout = new TableLayoutPanel();
            tlpProgramId = new TableLayoutPanel();
            lblProgramId = new AntdUI.Label();
            txtProgramId = new AntdUI.Input();
            lblCurrentInfo = new AntdUI.Label();
            tlpRemark = new TableLayoutPanel();
            lblRemark = new AntdUI.Label();
            inputRemark = new AntdUI.Input();
            tlpProgramName = new TableLayoutPanel();
            btnBuildName = new AntdUI.Button();
            inputProgramName = new AntdUI.Input();
            lblProgramName = new AntdUI.Label();
            tlpProgramType = new TableLayoutPanel();
            lblProgramType = new AntdUI.Label();
            cmbProgramType = new AntdUI.Select();
            tlpProductNum = new TableLayoutPanel();
            lblProductNum = new AntdUI.Label();
            inputProductNum = new AntdUI.Input();
            tlpRecipe2 = new TableLayoutPanel();
            lblRecipeCode2 = new AntdUI.Label();
            selectStation2Recipe = new AntdUI.Select();
            tlpDrawingNo = new TableLayoutPanel();
            lblComponentCode = new AntdUI.Label();
            inputComponentCode = new AntdUI.Input();
            tlpRecipe1 = new TableLayoutPanel();
            lblRecipeCode1 = new AntdUI.Label();
            selectStation1Recipe = new AntdUI.Select();
            tlpSN = new TableLayoutPanel();
            lblSequenceNumber = new AntdUI.Label();
            inputSequenceNumber = new AntdUI.Input();
            tlpDescription = new TableLayoutPanel();
            lblDescription = new AntdUI.Label();
            inputDescription = new AntdUI.Input();
            programContentLayout = new TableLayoutPanel();
            lblProgramContent = new AntdUI.Label();
            tableProgramContent = new AntdUI.Table();
            toolbarLayout = new TableLayoutPanel();
            tlpToolbar1 = new TableLayoutPanel();
            queryPrograms = new AutoWeldSystem.UI.Controls.InputQuery(components);
            btnPullMes = new AntdUI.Button();
            btnSync = new AntdUI.Button();
            btnBatchClean = new AntdUI.Button();
            btnDelete = new AntdUI.Button();
            btnSaveAsNew = new AntdUI.Button();
            tlpToolbar2 = new TableLayoutPanel();
            btnNew = new AntdUI.Button();
            chkSyncNow = new AntdUI.Checkbox();
            btnSave = new AntdUI.Button();
            rootLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            leftLayout.SuspendLayout();
            rightLayout.SuspendLayout();
            editorLayout.SuspendLayout();
            tlpProgramId.SuspendLayout();
            tlpRemark.SuspendLayout();
            tlpProgramName.SuspendLayout();
            tlpProgramType.SuspendLayout();
            tlpProductNum.SuspendLayout();
            tlpRecipe2.SuspendLayout();
            tlpDrawingNo.SuspendLayout();
            tlpRecipe1.SuspendLayout();
            tlpSN.SuspendLayout();
            tlpDescription.SuspendLayout();
            programContentLayout.SuspendLayout();
            toolbarLayout.SuspendLayout();
            tlpToolbar1.SuspendLayout();
            tlpToolbar2.SuspendLayout();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(splitMain, 0, 1);
            rootLayout.Controls.Add(toolbarLayout, 0, 0);
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Margin = new Padding(0);
            rootLayout.Name = "rootLayout";
            rootLayout.RowCount = 2;
            rootLayout.RowStyles.Add(new RowStyle());
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100.000008F));
            rootLayout.Size = new Size(1366, 745);
            rootLayout.TabIndex = 0;
            // 
            // splitMain
            // 
            splitMain.Dock = DockStyle.Fill;
            splitMain.Location = new Point(1, 68);
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
            splitMain.Size = new Size(1364, 676);
            splitMain.SplitterDistance = 911;
            splitMain.SplitterWidth = 8;
            splitMain.TabIndex = 1;
            // 
            // leftLayout
            // 
            leftLayout.ColumnCount = 1;
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftLayout.Controls.Add(tablePrograms, 0, 0);
            leftLayout.Controls.Add(programPagination, 0, 1);
            leftLayout.Dock = DockStyle.Fill;
            leftLayout.Location = new Point(0, 0);
            leftLayout.Name = "leftLayout";
            leftLayout.RowCount = 2;
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            leftLayout.Size = new Size(911, 676);
            leftLayout.TabIndex = 0;
            // 
            // tablePrograms
            // 
            tablePrograms.Dock = DockStyle.Fill;
            tablePrograms.Gap = 12;
            tablePrograms.Location = new Point(3, 3);
            tablePrograms.Name = "tablePrograms";
            tablePrograms.Size = new Size(905, 622);
            tablePrograms.TabIndex = 3;
            // 
            // programPagination
            // 
            programPagination.Dock = DockStyle.Fill;
            programPagination.Location = new Point(3, 631);
            programPagination.Name = "programPagination";
            programPagination.PageSize = 20;
            programPagination.PageSizeOptions = new int[]
    {
    20,
    50,
    100
    };
            programPagination.RecordsPerPageText = "条/页";
            programPagination.ShowSizeChanger = true;
            programPagination.Size = new Size(905, 42);
            programPagination.TabIndex = 1;
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
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rightLayout.Size = new Size(445, 676);
            rightLayout.TabIndex = 0;
            // 
            // editorLayout
            // 
            editorLayout.AutoSize = true;
            editorLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            editorLayout.ColumnCount = 1;
            editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            editorLayout.Controls.Add(tlpProgramId, 0, 10);
            editorLayout.Controls.Add(lblCurrentInfo, 0, 0);
            editorLayout.Controls.Add(tlpRemark, 0, 9);
            editorLayout.Controls.Add(tlpProgramName, 0, 1);
            editorLayout.Controls.Add(tlpProgramType, 0, 8);
            editorLayout.Controls.Add(tlpProductNum, 0, 2);
            editorLayout.Controls.Add(tlpRecipe2, 0, 7);
            editorLayout.Controls.Add(tlpDrawingNo, 0, 3);
            editorLayout.Controls.Add(tlpRecipe1, 0, 6);
            editorLayout.Controls.Add(tlpSN, 0, 4);
            editorLayout.Controls.Add(tlpDescription, 0, 5);
            editorLayout.Dock = DockStyle.Fill;
            editorLayout.Location = new Point(0, 0);
            editorLayout.Margin = new Padding(0);
            editorLayout.Name = "editorLayout";
            editorLayout.RowCount = 11;
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle());
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            editorLayout.Size = new Size(445, 424);
            editorLayout.TabIndex = 1;
            // 
            // tlpProgramId
            // 
            tlpProgramId.AutoSize = true;
            tlpProgramId.ColumnCount = 2;
            tlpProgramId.ColumnStyles.Add(new ColumnStyle());
            tlpProgramId.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpProgramId.Controls.Add(lblProgramId, 0, 0);
            tlpProgramId.Controls.Add(txtProgramId, 1, 0);
            tlpProgramId.Dock = DockStyle.Fill;
            tlpProgramId.Location = new Point(0, 424);
            tlpProgramId.Margin = new Padding(0);
            tlpProgramId.Name = "tlpProgramId";
            tlpProgramId.RowCount = 1;
            tlpProgramId.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProgramId.Size = new Size(445, 1);
            tlpProgramId.TabIndex = 5;
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
            // lblCurrentInfo
            // 
            lblCurrentInfo.Dock = DockStyle.Fill;
            lblCurrentInfo.Location = new Point(0, 0);
            lblCurrentInfo.Margin = new Padding(0);
            lblCurrentInfo.Name = "lblCurrentInfo";
            lblCurrentInfo.Padding = new Padding(8, 0, 0, 0);
            lblCurrentInfo.Size = new Size(445, 53);
            lblCurrentInfo.TabIndex = 1;
            lblCurrentInfo.Text = "xxx";
            // 
            // tlpRemark
            // 
            tlpRemark.AutoSize = true;
            tlpRemark.ColumnCount = 2;
            tlpRemark.ColumnStyles.Add(new ColumnStyle());
            tlpRemark.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRemark.Controls.Add(lblRemark, 0, 0);
            tlpRemark.Controls.Add(inputRemark, 1, 0);
            tlpRemark.Dock = DockStyle.Fill;
            tlpRemark.Location = new Point(0, 424);
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
            // tlpProgramName
            // 
            tlpProgramName.AutoSize = true;
            tlpProgramName.ColumnCount = 3;
            tlpProgramName.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            tlpProgramName.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpProgramName.ColumnStyles.Add(new ColumnStyle());
            tlpProgramName.Controls.Add(btnBuildName, 2, 0);
            tlpProgramName.Controls.Add(inputProgramName, 1, 0);
            tlpProgramName.Controls.Add(lblProgramName, 0, 0);
            tlpProgramName.Dock = DockStyle.Fill;
            tlpProgramName.Location = new Point(0, 53);
            tlpProgramName.Margin = new Padding(0);
            tlpProgramName.Name = "tlpProgramName";
            tlpProgramName.RowCount = 1;
            tlpProgramName.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProgramName.Size = new Size(445, 53);
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
            btnBuildName.Size = new Size(124, 53);
            btnBuildName.TabIndex = 3;
            btnBuildName.Tag = "perm:button.program.build-name:enabled";
            btnBuildName.Text = "生成名称";
            // 
            // inputProgramName
            // 
            inputProgramName.Dock = DockStyle.Fill;
            inputProgramName.Location = new Point(140, 0);
            inputProgramName.Margin = new Padding(0);
            inputProgramName.Name = "inputProgramName";
            inputProgramName.Size = new Size(181, 53);
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
            lblProgramName.Size = new Size(74, 53);
            lblProgramName.TabIndex = 11;
            lblProgramName.Text = "程序名称";
            // 
            // tlpProgramType
            // 
            tlpProgramType.AutoSize = true;
            tlpProgramType.ColumnCount = 2;
            tlpProgramType.ColumnStyles.Add(new ColumnStyle());
            tlpProgramType.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpProgramType.Controls.Add(lblProgramType, 0, 0);
            tlpProgramType.Controls.Add(cmbProgramType, 1, 0);
            tlpProgramType.Dock = DockStyle.Fill;
            tlpProgramType.Location = new Point(0, 424);
            tlpProgramType.Margin = new Padding(0);
            tlpProgramType.Name = "tlpProgramType";
            tlpProgramType.RowCount = 1;
            tlpProgramType.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProgramType.Size = new Size(445, 1);
            tlpProgramType.TabIndex = 5;
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
            // tlpProductNum
            // 
            tlpProductNum.AutoSize = true;
            tlpProductNum.ColumnCount = 2;
            tlpProductNum.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            tlpProductNum.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpProductNum.Controls.Add(lblProductNum, 0, 0);
            tlpProductNum.Controls.Add(inputProductNum, 1, 0);
            tlpProductNum.Dock = DockStyle.Fill;
            tlpProductNum.Location = new Point(0, 106);
            tlpProductNum.Margin = new Padding(0);
            tlpProductNum.Name = "tlpProductNum";
            tlpProductNum.RowCount = 1;
            tlpProductNum.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpProductNum.Size = new Size(445, 53);
            tlpProductNum.TabIndex = 3;
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
            lblProductNum.Size = new Size(91, 53);
            lblProductNum.TabIndex = 11;
            lblProductNum.Text = "产品工号";
            // 
            // inputProductNum
            // 
            inputProductNum.Dock = DockStyle.Fill;
            inputProductNum.Location = new Point(140, 0);
            inputProductNum.Margin = new Padding(0);
            inputProductNum.Name = "inputProductNum";
            inputProductNum.Size = new Size(305, 53);
            inputProductNum.TabIndex = 1;
            // 
            // tlpRecipe2
            // 
            tlpRecipe2.AutoSize = true;
            tlpRecipe2.ColumnCount = 2;
            tlpRecipe2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            tlpRecipe2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRecipe2.Controls.Add(lblRecipeCode2, 0, 0);
            tlpRecipe2.Controls.Add(selectStation2Recipe, 1, 0);
            tlpRecipe2.Dock = DockStyle.Fill;
            tlpRecipe2.Location = new Point(0, 371);
            tlpRecipe2.Margin = new Padding(0);
            tlpRecipe2.Name = "tlpRecipe2";
            tlpRecipe2.RowCount = 1;
            tlpRecipe2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpRecipe2.Size = new Size(445, 53);
            tlpRecipe2.TabIndex = 4;
            // 
            // lblRecipeCode2
            // 
            lblRecipeCode2.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblRecipeCode2.Dock = DockStyle.Fill;
            lblRecipeCode2.Location = new Point(0, 0);
            lblRecipeCode2.Margin = new Padding(0);
            lblRecipeCode2.Name = "lblRecipeCode2";
            lblRecipeCode2.Padding = new Padding(4, 0, 0, 0);
            lblRecipeCode2.Prefix = "*";
            lblRecipeCode2.PrefixColor = Color.FromArgb(255, 77, 79);
            lblRecipeCode2.Size = new Size(144, 53);
            lblRecipeCode2.TabIndex = 0;
            lblRecipeCode2.Text = "工位2配方名称";
            // 
            // selectStation2Recipe
            // 
            selectStation2Recipe.Dock = DockStyle.Fill;
            selectStation2Recipe.List = true;
            selectStation2Recipe.Location = new Point(140, 0);
            selectStation2Recipe.Margin = new Padding(0);
            selectStation2Recipe.MaxCount = 16;
            selectStation2Recipe.Name = "selectStation2Recipe";
            selectStation2Recipe.Size = new Size(305, 53);
            selectStation2Recipe.TabIndex = 1;
            selectStation2Recipe.WheelModifyEnabled = false;
            // 
            // tlpDrawingNo
            // 
            tlpDrawingNo.AutoSize = true;
            tlpDrawingNo.ColumnCount = 2;
            tlpDrawingNo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            tlpDrawingNo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpDrawingNo.Controls.Add(lblComponentCode, 0, 0);
            tlpDrawingNo.Controls.Add(inputComponentCode, 1, 0);
            tlpDrawingNo.Dock = DockStyle.Fill;
            tlpDrawingNo.Location = new Point(0, 159);
            tlpDrawingNo.Margin = new Padding(0);
            tlpDrawingNo.Name = "tlpDrawingNo";
            tlpDrawingNo.RowCount = 1;
            tlpDrawingNo.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpDrawingNo.Size = new Size(445, 53);
            tlpDrawingNo.TabIndex = 4;
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
            lblComponentCode.Size = new Size(91, 53);
            lblComponentCode.TabIndex = 11;
            lblComponentCode.Text = "部件图号";
            // 
            // inputComponentCode
            // 
            inputComponentCode.Dock = DockStyle.Fill;
            inputComponentCode.Location = new Point(140, 0);
            inputComponentCode.Margin = new Padding(0);
            inputComponentCode.Name = "inputComponentCode";
            inputComponentCode.Size = new Size(305, 53);
            inputComponentCode.TabIndex = 1;
            // 
            // tlpRecipe1
            // 
            tlpRecipe1.AutoSize = true;
            tlpRecipe1.ColumnCount = 2;
            tlpRecipe1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            tlpRecipe1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRecipe1.Controls.Add(lblRecipeCode1, 0, 0);
            tlpRecipe1.Controls.Add(selectStation1Recipe, 1, 0);
            tlpRecipe1.Dock = DockStyle.Fill;
            tlpRecipe1.Location = new Point(0, 318);
            tlpRecipe1.Margin = new Padding(0);
            tlpRecipe1.Name = "tlpRecipe1";
            tlpRecipe1.RowCount = 1;
            tlpRecipe1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpRecipe1.Size = new Size(445, 53);
            tlpRecipe1.TabIndex = 4;
            // 
            // lblRecipeCode1
            // 
            lblRecipeCode1.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblRecipeCode1.Dock = DockStyle.Fill;
            lblRecipeCode1.Location = new Point(0, 0);
            lblRecipeCode1.Margin = new Padding(0);
            lblRecipeCode1.Name = "lblRecipeCode1";
            lblRecipeCode1.Padding = new Padding(4, 0, 0, 0);
            lblRecipeCode1.Prefix = "*";
            lblRecipeCode1.PrefixColor = Color.FromArgb(255, 77, 79);
            lblRecipeCode1.Size = new Size(144, 53);
            lblRecipeCode1.TabIndex = 11;
            lblRecipeCode1.Text = "工位1配方名称";
            // 
            // selectStation1Recipe
            // 
            selectStation1Recipe.Dock = DockStyle.Fill;
            selectStation1Recipe.List = true;
            selectStation1Recipe.Location = new Point(140, 0);
            selectStation1Recipe.Margin = new Padding(0);
            selectStation1Recipe.MaxCount = 16;
            selectStation1Recipe.Name = "selectStation1Recipe";
            selectStation1Recipe.Size = new Size(305, 53);
            selectStation1Recipe.TabIndex = 1;
            selectStation1Recipe.WheelModifyEnabled = false;
            // 
            // tlpSN
            // 
            tlpSN.AutoSize = true;
            tlpSN.ColumnCount = 2;
            tlpSN.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            tlpSN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpSN.Controls.Add(lblSequenceNumber, 0, 0);
            tlpSN.Controls.Add(inputSequenceNumber, 1, 0);
            tlpSN.Dock = DockStyle.Fill;
            tlpSN.Location = new Point(0, 212);
            tlpSN.Margin = new Padding(0);
            tlpSN.Name = "tlpSN";
            tlpSN.RowCount = 1;
            tlpSN.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpSN.Size = new Size(445, 53);
            tlpSN.TabIndex = 4;
            // 
            // lblSequenceNumber
            // 
            lblSequenceNumber.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblSequenceNumber.Dock = DockStyle.Fill;
            lblSequenceNumber.Location = new Point(0, 0);
            lblSequenceNumber.Margin = new Padding(0);
            lblSequenceNumber.Name = "lblSequenceNumber";
            lblSequenceNumber.Padding = new Padding(4, 0, 0, 0);
            lblSequenceNumber.Size = new Size(57, 53);
            lblSequenceNumber.TabIndex = 11;
            lblSequenceNumber.Text = "流水号";
            // 
            // inputSequenceNumber
            // 
            inputSequenceNumber.Dock = DockStyle.Fill;
            inputSequenceNumber.Location = new Point(140, 0);
            inputSequenceNumber.Margin = new Padding(0);
            inputSequenceNumber.Name = "inputSequenceNumber";
            inputSequenceNumber.Size = new Size(305, 53);
            inputSequenceNumber.TabIndex = 1;
            // 
            // tlpDescription
            // 
            tlpDescription.AutoSize = true;
            tlpDescription.ColumnCount = 2;
            tlpDescription.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            tlpDescription.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpDescription.Controls.Add(lblDescription, 0, 0);
            tlpDescription.Controls.Add(inputDescription, 1, 0);
            tlpDescription.Dock = DockStyle.Fill;
            tlpDescription.Location = new Point(0, 265);
            tlpDescription.Margin = new Padding(0);
            tlpDescription.Name = "tlpDescription";
            tlpDescription.RowCount = 1;
            tlpDescription.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpDescription.Size = new Size(445, 53);
            tlpDescription.TabIndex = 4;
            // 
            // lblDescription
            // 
            lblDescription.AutoSizeMode = AntdUI.TAutoSize.Width;
            lblDescription.Dock = DockStyle.Fill;
            lblDescription.Location = new Point(0, 0);
            lblDescription.Margin = new Padding(0);
            lblDescription.Name = "lblDescription";
            lblDescription.Padding = new Padding(4, 0, 0, 0);
            lblDescription.Size = new Size(74, 53);
            lblDescription.TabIndex = 1;
            lblDescription.Text = "程序备注";
            // 
            // inputDescription
            // 
            inputDescription.Dock = DockStyle.Fill;
            inputDescription.Location = new Point(140, 0);
            inputDescription.Margin = new Padding(0);
            inputDescription.Name = "inputDescription";
            inputDescription.Size = new Size(305, 53);
            inputDescription.TabIndex = 1;
            // 
            // programContentLayout
            // 
            programContentLayout.ColumnCount = 1;
            programContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            programContentLayout.Controls.Add(lblProgramContent, 0, 0);
            programContentLayout.Controls.Add(tableProgramContent, 0, 1);
            programContentLayout.Dock = DockStyle.Fill;
            programContentLayout.Location = new Point(0, 424);
            programContentLayout.Margin = new Padding(0);
            programContentLayout.Name = "programContentLayout";
            programContentLayout.RowCount = 2;
            programContentLayout.RowStyles.Add(new RowStyle());
            programContentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            programContentLayout.Size = new Size(445, 252);
            programContentLayout.TabIndex = 2;
            // 
            // lblProgramContent
            // 
            lblProgramContent.Dock = DockStyle.Fill;
            lblProgramContent.Font = new Font("Microsoft YaHei", 10.5F);
            lblProgramContent.Location = new Point(0, 0);
            lblProgramContent.Margin = new Padding(0);
            lblProgramContent.Name = "lblProgramContent";
            lblProgramContent.Padding = new Padding(8, 0, 0, 0);
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
            tableProgramContent.Size = new Size(445, 202);
            tableProgramContent.TabIndex = 27;
            // 
            // toolbarLayout
            // 
            toolbarLayout.ColumnCount = 2;
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbarLayout.ColumnStyles.Add(new ColumnStyle());
            toolbarLayout.Controls.Add(tlpToolbar1, 0, 0);
            toolbarLayout.Controls.Add(tlpToolbar2, 1, 0);
            toolbarLayout.Dock = DockStyle.Fill;
            toolbarLayout.Location = new Point(0, 0);
            toolbarLayout.Margin = new Padding(0);
            toolbarLayout.Name = "toolbarLayout";
            toolbarLayout.Padding = new Padding(20, 12, 20, 6);
            toolbarLayout.RowCount = 1;
            toolbarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            toolbarLayout.Size = new Size(1366, 67);
            toolbarLayout.TabIndex = 0;
            // 
            // tlpToolbar1
            // 
            tlpToolbar1.AutoSize = true;
            tlpToolbar1.ColumnCount = 6;
            tlpToolbar1.ColumnStyles.Add(new ColumnStyle());
            tlpToolbar1.ColumnStyles.Add(new ColumnStyle());
            tlpToolbar1.ColumnStyles.Add(new ColumnStyle());
            tlpToolbar1.ColumnStyles.Add(new ColumnStyle());
            tlpToolbar1.ColumnStyles.Add(new ColumnStyle());
            tlpToolbar1.ColumnStyles.Add(new ColumnStyle());
            tlpToolbar1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tlpToolbar1.Controls.Add(queryPrograms, 5, 0);
            tlpToolbar1.Controls.Add(btnPullMes, 4, 0);
            tlpToolbar1.Controls.Add(btnSync, 3, 0);
            tlpToolbar1.Controls.Add(btnBatchClean, 2, 0);
            tlpToolbar1.Controls.Add(btnDelete, 1, 0);
            tlpToolbar1.Controls.Add(btnSaveAsNew, 0, 0);
            tlpToolbar1.Dock = DockStyle.Left;
            tlpToolbar1.Location = new Point(20, 12);
            tlpToolbar1.Margin = new Padding(0);
            tlpToolbar1.Name = "tlpToolbar1";
            tlpToolbar1.RowCount = 1;
            tlpToolbar1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpToolbar1.Size = new Size(965, 49);
            tlpToolbar1.TabIndex = 1;
            // 
            // queryPrograms
            // 
            queryPrograms.AutoSize = true;
            queryPrograms.Dock = DockStyle.Fill;
            queryPrograms.Location = new Point(642, 0);
            queryPrograms.Margin = new Padding(0);
            queryPrograms.MinimumSize = new Size(125, 40);
            queryPrograms.Name = "queryPrograms";
            queryPrograms.PlaceholderText = "搜索程序 / 产品工号 / 状态";
            queryPrograms.QueryChanged = null;
            queryPrograms.RefreshButtonTag = "perm:button.program.refresh:enabled";
            queryPrograms.Size = new Size(323, 49);
            queryPrograms.TabIndex = 1;
            // 
            // btnPullMes
            // 
            btnPullMes.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnPullMes.BorderWidth = 1F;
            btnPullMes.Dock = DockStyle.Fill;
            btnPullMes.IconSvg = "CloudDownloadOutlined";
            btnPullMes.Location = new Point(498, 0);
            btnPullMes.Margin = new Padding(0);
            btnPullMes.Name = "btnPullMes";
            btnPullMes.Size = new Size(144, 49);
            btnPullMes.TabIndex = 6;
            btnPullMes.Tag = "perm:button.program.pull-mes:enabled";
            btnPullMes.Text = "从MES拉取";
            // 
            // btnSync
            // 
            btnSync.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSync.BorderWidth = 1F;
            btnSync.Dock = DockStyle.Fill;
            btnSync.IconSvg = "CloudUploadOutlined";
            btnSync.Location = new Point(372, 0);
            btnSync.Margin = new Padding(0);
            btnSync.Name = "btnSync";
            btnSync.Size = new Size(126, 49);
            btnSync.TabIndex = 5;
            btnSync.Tag = "perm:button.program.sync:enabled";
            btnSync.Text = "同步MES";
            // 
            // btnBatchClean
            // 
            btnBatchClean.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnBatchClean.BorderWidth = 1F;
            btnBatchClean.Dock = DockStyle.Fill;
            btnBatchClean.IconSvg = "ClearOutlined";
            btnBatchClean.Location = new Point(248, 0);
            btnBatchClean.Margin = new Padding(0);
            btnBatchClean.Name = "btnBatchClean";
            btnBatchClean.Size = new Size(124, 49);
            btnBatchClean.TabIndex = 4;
            btnBatchClean.Tag = "perm:button.program.delete:enabled";
            btnBatchClean.Text = "批量清理";
            btnBatchClean.Type = AntdUI.TTypeMini.Warn;
            // 
            // btnDelete
            // 
            btnDelete.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnDelete.BorderWidth = 1F;
            btnDelete.Dock = DockStyle.Fill;
            btnDelete.IconSvg = "DeleteOutlined";
            btnDelete.Location = new Point(159, 0);
            btnDelete.Margin = new Padding(0);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(89, 49);
            btnDelete.TabIndex = 3;
            btnDelete.Tag = "perm:button.program.delete:enabled";
            btnDelete.Text = "删除";
            // 
            // btnSaveAsNew
            // 
            btnSaveAsNew.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSaveAsNew.BorderWidth = 1F;
            btnSaveAsNew.Dock = DockStyle.Fill;
            btnSaveAsNew.IconSvg = "CopyOutlined";
            btnSaveAsNew.Location = new Point(0, 0);
            btnSaveAsNew.Margin = new Padding(0);
            btnSaveAsNew.Name = "btnSaveAsNew";
            btnSaveAsNew.Size = new Size(159, 49);
            btnSaveAsNew.TabIndex = 2;
            btnSaveAsNew.Tag = "perm:button.program.add:enabled";
            btnSaveAsNew.Text = "另存为新程序";
            // 
            // tlpToolbar2
            // 
            tlpToolbar2.AutoSize = true;
            tlpToolbar2.ColumnCount = 3;
            tlpToolbar2.ColumnStyles.Add(new ColumnStyle());
            tlpToolbar2.ColumnStyles.Add(new ColumnStyle());
            tlpToolbar2.ColumnStyles.Add(new ColumnStyle());
            tlpToolbar2.Controls.Add(btnNew, 0, 0);
            tlpToolbar2.Controls.Add(chkSyncNow, 2, 0);
            tlpToolbar2.Controls.Add(btnSave, 1, 0);
            tlpToolbar2.Dock = DockStyle.Fill;
            tlpToolbar2.Location = new Point(999, 12);
            tlpToolbar2.Margin = new Padding(0);
            tlpToolbar2.Name = "tlpToolbar2";
            tlpToolbar2.RowCount = 1;
            tlpToolbar2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpToolbar2.Size = new Size(347, 49);
            tlpToolbar2.TabIndex = 2;
            // 
            // btnNew
            // 
            btnNew.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnNew.BorderWidth = 1F;
            btnNew.Dock = DockStyle.Fill;
            btnNew.IconSvg = "PlusOutlined";
            btnNew.Location = new Point(0, 0);
            btnNew.Margin = new Padding(0);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(89, 49);
            btnNew.TabIndex = 0;
            btnNew.Tag = "perm:button.program.add:enabled";
            btnNew.Text = "新建";
            // 
            // chkSyncNow
            // 
            chkSyncNow.AutoSizeMode = AntdUI.TAutoSize.Width;
            chkSyncNow.Checked = true;
            chkSyncNow.CheckState = CheckState.Checked;
            chkSyncNow.Dock = DockStyle.Fill;
            chkSyncNow.Location = new Point(178, 0);
            chkSyncNow.Margin = new Padding(0);
            chkSyncNow.Name = "chkSyncNow";
            chkSyncNow.Size = new Size(169, 49);
            chkSyncNow.TabIndex = 7;
            chkSyncNow.Text = "保存后立即同步";
            // 
            // btnSave
            // 
            btnSave.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSave.BorderWidth = 1F;
            btnSave.Dock = DockStyle.Fill;
            btnSave.IconSvg = "SaveOutlined";
            btnSave.Location = new Point(89, 0);
            btnSave.Margin = new Padding(0);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(89, 49);
            btnSave.TabIndex = 1;
            btnSave.Tag = "perm:button.program.edit:enabled";
            btnSave.Text = "保存";
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
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            leftLayout.ResumeLayout(false);
            rightLayout.ResumeLayout(false);
            rightLayout.PerformLayout();
            editorLayout.ResumeLayout(false);
            editorLayout.PerformLayout();
            tlpProgramId.ResumeLayout(false);
            tlpProgramId.PerformLayout();
            tlpRemark.ResumeLayout(false);
            tlpRemark.PerformLayout();
            tlpProgramName.ResumeLayout(false);
            tlpProgramName.PerformLayout();
            tlpProgramType.ResumeLayout(false);
            tlpProgramType.PerformLayout();
            tlpProductNum.ResumeLayout(false);
            tlpProductNum.PerformLayout();
            tlpRecipe2.ResumeLayout(false);
            tlpRecipe2.PerformLayout();
            tlpDrawingNo.ResumeLayout(false);
            tlpDrawingNo.PerformLayout();
            tlpRecipe1.ResumeLayout(false);
            tlpRecipe1.PerformLayout();
            tlpSN.ResumeLayout(false);
            tlpSN.PerformLayout();
            tlpDescription.ResumeLayout(false);
            tlpDescription.PerformLayout();
            programContentLayout.ResumeLayout(false);
            toolbarLayout.ResumeLayout(false);
            toolbarLayout.PerformLayout();
            tlpToolbar1.ResumeLayout(false);
            tlpToolbar1.PerformLayout();
            tlpToolbar2.ResumeLayout(false);
            tlpToolbar2.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel rootLayout;
        private TableLayoutPanel toolbarLayout;
        private AntdUI.Button btnNew;
        private AntdUI.Button btnSave;
        private AntdUI.Button btnSaveAsNew;
        private AntdUI.Button btnDelete;
        private AntdUI.Button btnBatchClean;
        private AntdUI.Button btnSync;
        private AntdUI.Button btnPullMes;
        private AntdUI.Checkbox chkSyncNow;
        private Controls.InputQuery queryPrograms;
        private TableLayoutPanel leftLayout;
        private AntdUI.Pagination programPagination;
        private AntdUI.Label lblProgramName;
        private AntdUI.Button btnBuildName;
        private AntdUI.Label lblProductNum;
        private AntdUI.Label lblRecipeCode1;
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
        private AntdUI.Label lblRecipeCode2;
        private AntdUI.Input inputSequenceNumber;
        private AntdUI.Input inputComponentCode;
        private AntdUI.Input inputDescription;
        private AntdUI.Input txtProgramId;
        private AntdUI.Label lblCurrentInfo;
        private TableLayoutPanel rightLayout;
        private TableLayoutPanel programContentLayout;
        private TableLayoutPanel tlpToolbar2;
        private TableLayoutPanel tlpToolbar1;
        private TableLayoutPanel tlpProgramId;
        private TableLayoutPanel tlpRemark;
        private TableLayoutPanel tlpProgramType;
        private TableLayoutPanel tlpRecipe2;
        private TableLayoutPanel tlpRecipe1;
        private TableLayoutPanel tlpDescription;
        private TableLayoutPanel tlpSN;
        private TableLayoutPanel tlpDrawingNo;
        private TableLayoutPanel tlpProductNum;
        private AntdUI.Table tablePrograms;
    }
}
