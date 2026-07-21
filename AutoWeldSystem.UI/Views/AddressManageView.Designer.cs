namespace AutoWeldSystem.UI.Views
{
    partial class AddressManageView
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
            components = new System.ComponentModel.Container();
            rootLayout = new TableLayoutPanel();
            headerLayout = new TableLayoutPanel();
            titleLayout = new TableLayoutPanel();
            lblTitle = new Label();
            lblDescription = new Label();
            SaveLayout = new TableLayoutPanel();
            btnTest = new AntdUI.Button();
            btnRefresh = new AntdUI.Button();
            btnSave = new AntdUI.Button();
            queryAddresses = new AutoWeldSystem.UI.Controls.InputQuery(components);
            bindingFlowPanel = new FlowLayoutPanel();
            lblBindingProduct = new Label();
            lblBindingArrow1 = new Label();
            lblBindingProcess = new Label();
            lblBindingArrow2 = new Label();
            lblBindingScheme = new Label();
            lblBindingArrow3 = new Label();
            lblBindingDetail = new Label();
            lblBindingArrow4 = new Label();
            lblBindingItem = new Label();
            lblBindingArrow5 = new Label();
            lblBindingPreview = new Label();
            tabAddressCategories = new TabControl();
            tabBusinessAddresses = new TabPage();
            tableAddresses = new AntdUI.Table();
            tabRecipeNames = new TabPage();
            recipeNameLayout = new TableLayoutPanel();
            lblRecipeNameHint = new Label();
            recipeNameToolbar = new FlowLayoutPanel();
            btnPreviewRecipeNames = new AntdUI.Button();
            tableRecipeNames = new AntdUI.Table();
            lblRecipeNamePreview = new Label();
            tableRecipeNamePreview = new AntdUI.Table();
            tabAlarmAddresses = new TabPage();
            alarmAddressLayout = new TableLayoutPanel();
            lblAlarmAddressHint = new Label();
            alarmAddressToolbar = new FlowLayoutPanel();
            btnAddAlarmAddress = new AntdUI.Button();
            btnDeleteAlarmAddress = new AntdUI.Button();
            btnPasteAlarmAddresses = new AntdUI.Button();
            tableAlarmAddresses = new AntdUI.Table();
            tabTestItemAddresses = new TabPage();
            testItemAddressLayout = new TableLayoutPanel();
            lblTestItemAddressHint = new Label();
            lblProductProcessGroupHint = new Label();
            productProcessToolbar = new FlowLayoutPanel();
            btnAddProductProcess = new AntdUI.Button();
            btnDeleteProductProcess = new AntdUI.Button();
            btnPreviewProductProcessAddress = new AntdUI.Button();
            lblProductProcessSummary = new Label();
            tableProcess = new AntdUI.Table();
            tabTestSchemes = new TabPage();
            testSchemeLayout = new TableLayoutPanel();
            lblTestSchemeHint = new Label();
            testSchemeToolbar = new FlowLayoutPanel();
            btnAddScheme = new AntdUI.Button();
            btnDeleteScheme = new AntdUI.Button();
            tableTestSchemes = new AntdUI.Table();
            tabSchemeDetails = new TabPage();
            schemeDetailLayout = new TableLayoutPanel();
            lblSchemeDetailHint = new Label();
            schemeDetailToolbar = new FlowLayoutPanel();
            lblSchemeDetailScheme = new Label();
            selectSchemeDetailScheme = new AntdUI.Select();
            schemeDetailSplitContainer = new SplitContainer();
            treeSchemeDetails = new TreeView();
            schemeDetailRoleGrid = new DataGridView();
            tabTestItems = new TabPage();
            testItemLayout = new TableLayoutPanel();
            lblTestItemHint = new Label();
            testItemToolbar = new FlowLayoutPanel();
            btnAddTestItem = new AntdUI.Button();
            btnDeleteTestItem = new AntdUI.Button();
            tableTestItems = new AntdUI.Table();
            rootLayout.SuspendLayout();
            headerLayout.SuspendLayout();
            titleLayout.SuspendLayout();
            SaveLayout.SuspendLayout();
            bindingFlowPanel.SuspendLayout();
            tabAddressCategories.SuspendLayout();
            tabBusinessAddresses.SuspendLayout();
            tabRecipeNames.SuspendLayout();
            recipeNameLayout.SuspendLayout();
            recipeNameToolbar.SuspendLayout();
            tabAlarmAddresses.SuspendLayout();
            alarmAddressLayout.SuspendLayout();
            alarmAddressToolbar.SuspendLayout();
            tabTestItemAddresses.SuspendLayout();
            testItemAddressLayout.SuspendLayout();
            productProcessToolbar.SuspendLayout();
            tabTestSchemes.SuspendLayout();
            testSchemeLayout.SuspendLayout();
            testSchemeToolbar.SuspendLayout();
            tabSchemeDetails.SuspendLayout();
            schemeDetailLayout.SuspendLayout();
            schemeDetailToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)schemeDetailSplitContainer).BeginInit();
            schemeDetailSplitContainer.Panel1.SuspendLayout();
            schemeDetailSplitContainer.Panel2.SuspendLayout();
            schemeDetailSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)schemeDetailRoleGrid).BeginInit();
            tabTestItems.SuspendLayout();
            testItemLayout.SuspendLayout();
            testItemToolbar.SuspendLayout();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(headerLayout, 0, 0);
            rootLayout.Controls.Add(bindingFlowPanel, 0, 1);
            rootLayout.Controls.Add(tabAddressCategories, 0, 2);
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.RowCount = 3;
            rootLayout.RowStyles.Add(new RowStyle());
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.Size = new Size(1298, 721);
            rootLayout.TabIndex = 0;
            // 
            // headerLayout
            // 
            headerLayout.ColumnCount = 3;
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            headerLayout.Controls.Add(titleLayout, 0, 0);
            headerLayout.Controls.Add(SaveLayout, 2, 0);
            headerLayout.Controls.Add(queryAddresses, 1, 0);
            headerLayout.Dock = DockStyle.Fill;
            headerLayout.Location = new Point(24, 18);
            headerLayout.Margin = new Padding(24, 18, 24, 10);
            headerLayout.Name = "headerLayout";
            headerLayout.RowCount = 1;
            headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            headerLayout.Size = new Size(1250, 70);
            headerLayout.TabIndex = 0;
            // 
            // titleLayout
            // 
            titleLayout.ColumnCount = 1;
            titleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            titleLayout.Controls.Add(lblTitle, 0, 0);
            titleLayout.Controls.Add(lblDescription, 0, 1);
            titleLayout.Dock = DockStyle.Fill;
            titleLayout.Location = new Point(0, 0);
            titleLayout.Margin = new Padding(0);
            titleLayout.Name = "titleLayout";
            titleLayout.RowCount = 2;
            titleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            titleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            titleLayout.Size = new Size(625, 70);
            titleLayout.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(0, 0);
            lblTitle.Margin = new Padding(0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(625, 34);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "PLC 地址配置";
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblDescription
            // 
            lblDescription.AutoEllipsis = true;
            lblDescription.Dock = DockStyle.Fill;
            lblDescription.ForeColor = SystemColors.GrayText;
            lblDescription.Location = new Point(0, 34);
            lblDescription.Margin = new Padding(0);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(625, 36);
            lblDescription.TabIndex = 1;
            lblDescription.Text = "维护固定业务信号对应的 PLC 实际地址。";
            lblDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // SaveLayout
            // 
            SaveLayout.AutoSize = true;
            SaveLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            SaveLayout.ColumnCount = 3;
            SaveLayout.ColumnStyles.Add(new ColumnStyle());
            SaveLayout.ColumnStyles.Add(new ColumnStyle());
            SaveLayout.ColumnStyles.Add(new ColumnStyle());
            SaveLayout.Controls.Add(btnTest, 2, 0);
            SaveLayout.Controls.Add(btnRefresh, 1, 0);
            SaveLayout.Controls.Add(btnSave, 0, 0);
            SaveLayout.Dock = DockStyle.Fill;
            SaveLayout.Location = new Point(937, 0);
            SaveLayout.Margin = new Padding(0);
            SaveLayout.Name = "SaveLayout";
            SaveLayout.RowCount = 1;
            SaveLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            SaveLayout.Size = new Size(313, 70);
            SaveLayout.TabIndex = 1;
            // 
            // btnTest
            // 
            btnTest.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnTest.BorderWidth = 1F;
            btnTest.Dock = DockStyle.Fill;
            btnTest.IconSvg = "ApiOutlined";
            btnTest.JoinMode = AntdUI.TJoinMode.Right;
            btnTest.Location = new Point(183, 0);
            btnTest.Margin = new Padding(0);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(139, 70);
            btnTest.TabIndex = 2;
            btnTest.Tag = "perm:button.address.test:enabled";
            btnTest.Text = "测试选中地址";
            // 
            // btnRefresh
            // 
            btnRefresh.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnRefresh.BorderWidth = 1F;
            btnRefresh.Dock = DockStyle.Fill;
            btnRefresh.IconSvg = "ReloadOutlined";
            btnRefresh.JoinMode = AntdUI.TJoinMode.LR;
            btnRefresh.Location = new Point(107, 0);
            btnRefresh.Margin = new Padding(0);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(76, 70);
            btnRefresh.TabIndex = 1;
            btnRefresh.Tag = "perm:button.address.refresh:enabled";
            btnRefresh.Text = "刷新";
            // 
            // btnSave
            // 
            btnSave.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSave.BorderWidth = 1F;
            btnSave.Dock = DockStyle.Fill;
            btnSave.IconSvg = "SaveOutlined";
            btnSave.JoinMode = AntdUI.TJoinMode.Left;
            btnSave.Location = new Point(0, 0);
            btnSave.Margin = new Padding(0);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(107, 70);
            btnSave.TabIndex = 0;
            btnSave.Tag = "perm:button.address.save:enabled";
            btnSave.Text = "保存地址";
            // 
            // queryAddresses
            // 
            queryAddresses.AutoSize = true;
            queryAddresses.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            queryAddresses.Dock = DockStyle.Fill;
            queryAddresses.Location = new Point(625, 0);
            queryAddresses.Margin = new Padding(0);
            queryAddresses.MinimumSize = new Size(125, 46);
            queryAddresses.Name = "queryAddresses";
            queryAddresses.QueryChanged = null;
            queryAddresses.Size = new Size(312, 70);
            queryAddresses.TabIndex = 1;
            // 
            // bindingFlowPanel
            // 
            bindingFlowPanel.BackColor = Color.FromArgb(248, 250, 252);
            bindingFlowPanel.Controls.Add(lblBindingProduct);
            bindingFlowPanel.Controls.Add(lblBindingArrow1);
            bindingFlowPanel.Controls.Add(lblBindingProcess);
            bindingFlowPanel.Controls.Add(lblBindingArrow2);
            bindingFlowPanel.Controls.Add(lblBindingScheme);
            bindingFlowPanel.Controls.Add(lblBindingArrow3);
            bindingFlowPanel.Controls.Add(lblBindingDetail);
            bindingFlowPanel.Controls.Add(lblBindingArrow4);
            bindingFlowPanel.Controls.Add(lblBindingItem);
            bindingFlowPanel.Controls.Add(lblBindingArrow5);
            bindingFlowPanel.Controls.Add(lblBindingPreview);
            bindingFlowPanel.Dock = DockStyle.Fill;
            bindingFlowPanel.Location = new Point(24, 98);
            bindingFlowPanel.Margin = new Padding(24, 0, 24, 6);
            bindingFlowPanel.Name = "bindingFlowPanel";
            bindingFlowPanel.Padding = new Padding(12, 8, 12, 8);
            bindingFlowPanel.Size = new Size(1250, 42);
            bindingFlowPanel.TabIndex = 1;
            bindingFlowPanel.WrapContents = false;
            // 
            // lblBindingProduct
            // 
            lblBindingProduct.AutoSize = true;
            lblBindingProduct.BackColor = Color.FromArgb(232, 244, 255);
            lblBindingProduct.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            lblBindingProduct.Location = new Point(12, 8);
            lblBindingProduct.Margin = new Padding(0, 0, 6, 0);
            lblBindingProduct.Name = "lblBindingProduct";
            lblBindingProduct.Padding = new Padding(10, 2, 10, 2);
            lblBindingProduct.Size = new Size(216, 23);
            lblBindingProduct.TabIndex = 0;
            lblBindingProduct.Text = "产品工号 ProductNumber";
            // 
            // lblBindingArrow1
            // 
            lblBindingArrow1.AutoSize = true;
            lblBindingArrow1.ForeColor = SystemColors.GrayText;
            lblBindingArrow1.Location = new Point(234, 8);
            lblBindingArrow1.Margin = new Padding(0, 0, 6, 0);
            lblBindingArrow1.Name = "lblBindingArrow1";
            lblBindingArrow1.Size = new Size(25, 20);
            lblBindingArrow1.TabIndex = 1;
            lblBindingArrow1.Text = "->";
            // 
            // lblBindingProcess
            // 
            lblBindingProcess.AutoSize = true;
            lblBindingProcess.BackColor = Color.FromArgb(237, 247, 237);
            lblBindingProcess.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            lblBindingProcess.Location = new Point(265, 8);
            lblBindingProcess.Margin = new Padding(0, 0, 6, 0);
            lblBindingProcess.Name = "lblBindingProcess";
            lblBindingProcess.Padding = new Padding(10, 2, 10, 2);
            lblBindingProcess.Size = new Size(168, 23);
            lblBindingProcess.TabIndex = 2;
            lblBindingProcess.Text = "产品工艺 SchemeId";
            // 
            // lblBindingArrow2
            // 
            lblBindingArrow2.AutoSize = true;
            lblBindingArrow2.ForeColor = SystemColors.GrayText;
            lblBindingArrow2.Location = new Point(439, 8);
            lblBindingArrow2.Margin = new Padding(0, 0, 6, 0);
            lblBindingArrow2.Name = "lblBindingArrow2";
            lblBindingArrow2.Size = new Size(25, 20);
            lblBindingArrow2.TabIndex = 3;
            lblBindingArrow2.Text = "->";
            // 
            // lblBindingScheme
            // 
            lblBindingScheme.AutoSize = true;
            lblBindingScheme.BackColor = Color.FromArgb(255, 247, 230);
            lblBindingScheme.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            lblBindingScheme.Location = new Point(470, 8);
            lblBindingScheme.Margin = new Padding(0, 0, 6, 0);
            lblBindingScheme.Name = "lblBindingScheme";
            lblBindingScheme.Padding = new Padding(10, 2, 10, 2);
            lblBindingScheme.Size = new Size(89, 23);
            lblBindingScheme.TabIndex = 4;
            lblBindingScheme.Text = "测试方案";
            // 
            // lblBindingArrow3
            // 
            lblBindingArrow3.AutoSize = true;
            lblBindingArrow3.ForeColor = SystemColors.GrayText;
            lblBindingArrow3.Location = new Point(565, 8);
            lblBindingArrow3.Margin = new Padding(0, 0, 6, 0);
            lblBindingArrow3.Name = "lblBindingArrow3";
            lblBindingArrow3.Size = new Size(25, 20);
            lblBindingArrow3.TabIndex = 5;
            lblBindingArrow3.Text = "->";
            // 
            // lblBindingDetail
            // 
            lblBindingDetail.AutoSize = true;
            lblBindingDetail.BackColor = Color.FromArgb(246, 239, 255);
            lblBindingDetail.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            lblBindingDetail.Location = new Point(596, 8);
            lblBindingDetail.Margin = new Padding(0, 0, 6, 0);
            lblBindingDetail.Name = "lblBindingDetail";
            lblBindingDetail.Padding = new Padding(10, 2, 10, 2);
            lblBindingDetail.Size = new Size(143, 23);
            lblBindingDetail.TabIndex = 6;
            lblBindingDetail.Text = "方案明细 ItemId";
            // 
            // lblBindingArrow4
            // 
            lblBindingArrow4.AutoSize = true;
            lblBindingArrow4.ForeColor = SystemColors.GrayText;
            lblBindingArrow4.Location = new Point(745, 8);
            lblBindingArrow4.Margin = new Padding(0, 0, 6, 0);
            lblBindingArrow4.Name = "lblBindingArrow4";
            lblBindingArrow4.Size = new Size(25, 20);
            lblBindingArrow4.TabIndex = 7;
            lblBindingArrow4.Text = "->";
            // 
            // lblBindingItem
            // 
            lblBindingItem.AutoSize = true;
            lblBindingItem.BackColor = Color.FromArgb(255, 241, 240);
            lblBindingItem.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            lblBindingItem.Location = new Point(776, 8);
            lblBindingItem.Margin = new Padding(0, 0, 6, 0);
            lblBindingItem.Name = "lblBindingItem";
            lblBindingItem.Padding = new Padding(10, 2, 10, 2);
            lblBindingItem.Size = new Size(153, 23);
            lblBindingItem.TabIndex = 8;
            lblBindingItem.Text = "测试项字典 表达式";
            // 
            // lblBindingArrow5
            // 
            lblBindingArrow5.AutoSize = true;
            lblBindingArrow5.ForeColor = SystemColors.GrayText;
            lblBindingArrow5.Location = new Point(935, 8);
            lblBindingArrow5.Margin = new Padding(0, 0, 6, 0);
            lblBindingArrow5.Name = "lblBindingArrow5";
            lblBindingArrow5.Size = new Size(25, 20);
            lblBindingArrow5.TabIndex = 9;
            lblBindingArrow5.Text = "->";
            // 
            // lblBindingPreview
            // 
            lblBindingPreview.AutoSize = true;
            lblBindingPreview.BackColor = Color.FromArgb(238, 242, 246);
            lblBindingPreview.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            lblBindingPreview.Location = new Point(966, 8);
            lblBindingPreview.Margin = new Padding(0);
            lblBindingPreview.Name = "lblBindingPreview";
            lblBindingPreview.Padding = new Padding(10, 2, 10, 2);
            lblBindingPreview.Size = new Size(121, 23);
            lblBindingPreview.TabIndex = 10;
            lblBindingPreview.Text = "PLC 地址预览";
            // 
            // tabAddressCategories
            // 
            tabAddressCategories.Controls.Add(tabBusinessAddresses);
            tabAddressCategories.Controls.Add(tabRecipeNames);
            tabAddressCategories.Controls.Add(tabAlarmAddresses);
            tabAddressCategories.Controls.Add(tabTestItemAddresses);
            tabAddressCategories.Controls.Add(tabTestSchemes);
            tabAddressCategories.Controls.Add(tabSchemeDetails);
            tabAddressCategories.Controls.Add(tabTestItems);
            tabAddressCategories.Dock = DockStyle.Fill;
            tabAddressCategories.HotTrack = true;
            tabAddressCategories.Location = new Point(24, 152);
            tabAddressCategories.Margin = new Padding(24, 6, 24, 24);
            tabAddressCategories.Name = "tabAddressCategories";
            tabAddressCategories.SelectedIndex = 0;
            tabAddressCategories.Size = new Size(1250, 545);
            tabAddressCategories.TabIndex = 2;
            //
            // tabBusinessAddresses
            //
            tabBusinessAddresses.Controls.Add(tableAddresses);
            tabBusinessAddresses.Location = new Point(4, 29);
            tabBusinessAddresses.Name = "tabBusinessAddresses";
            tabBusinessAddresses.Padding = new Padding(3);
            tabBusinessAddresses.Size = new Size(1242, 512);
            tabBusinessAddresses.TabIndex = 0;
            tabBusinessAddresses.Text = "业务信号地址";
            tabBusinessAddresses.UseVisualStyleBackColor = true;
            //
            // tableAddresses
            //
            tableAddresses.Dock = DockStyle.Fill;
            tableAddresses.EditMode = AntdUI.TEditMode.DoubleClick;
            tableAddresses.Gap = 12;
            tableAddresses.Location = new Point(3, 3);
            tableAddresses.Margin = new Padding(0);
            tableAddresses.Name = "tableAddresses";
            tableAddresses.Size = new Size(1236, 506);
            tableAddresses.TabIndex = 0;
            tableAddresses.Text = "tableAddresses";
            // 
            // tabRecipeNames
            // 
            tabRecipeNames.Controls.Add(recipeNameLayout);
            tabRecipeNames.Location = new Point(4, 29);
            tabRecipeNames.Name = "tabRecipeNames";
            tabRecipeNames.Padding = new Padding(3);
            tabRecipeNames.Size = new Size(1242, 512);
            tabRecipeNames.TabIndex = 1;
            tabRecipeNames.Text = "配方名称地址";
            tabRecipeNames.UseVisualStyleBackColor = true;
            // 
            // recipeNameLayout
            // 
            recipeNameLayout.ColumnCount = 1;
            recipeNameLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            recipeNameLayout.Controls.Add(lblRecipeNameHint, 0, 0);
            recipeNameLayout.Controls.Add(recipeNameToolbar, 0, 1);
            recipeNameLayout.Controls.Add(tableRecipeNames, 0, 2);
            recipeNameLayout.Controls.Add(lblRecipeNamePreview, 0, 3);
            recipeNameLayout.Controls.Add(tableRecipeNamePreview, 0, 4);
            recipeNameLayout.Dock = DockStyle.Fill;
            recipeNameLayout.Location = new Point(3, 3);
            recipeNameLayout.Margin = new Padding(0);
            recipeNameLayout.Name = "recipeNameLayout";
            recipeNameLayout.RowCount = 5;
            recipeNameLayout.RowStyles.Add(new RowStyle());
            recipeNameLayout.RowStyles.Add(new RowStyle());
            recipeNameLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            recipeNameLayout.RowStyles.Add(new RowStyle());
            recipeNameLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 80F));
            recipeNameLayout.Size = new Size(1236, 506);
            recipeNameLayout.TabIndex = 0;
            // 
            // lblRecipeNameHint
            // 
            lblRecipeNameHint.Dock = DockStyle.Fill;
            lblRecipeNameHint.ForeColor = SystemColors.GrayText;
            lblRecipeNameHint.Location = new Point(0, 0);
            lblRecipeNameHint.Margin = new Padding(0);
            lblRecipeNameHint.Name = "lblRecipeNameHint";
            lblRecipeNameHint.Size = new Size(1236, 34);
            lblRecipeNameHint.TabIndex = 0;
            lblRecipeNameHint.Text = "第 N 个地址对应配方号 N，地址按基地址和固定字节偏移计算；双工位分别维护。";
            lblRecipeNameHint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // recipeNameToolbar
            // 
            recipeNameToolbar.Controls.Add(btnPreviewRecipeNames);
            recipeNameToolbar.Dock = DockStyle.Fill;
            recipeNameToolbar.Location = new Point(0, 34);
            recipeNameToolbar.Margin = new Padding(0);
            recipeNameToolbar.Name = "recipeNameToolbar";
            recipeNameToolbar.Padding = new Padding(0, 5, 0, 5);
            recipeNameToolbar.Size = new Size(1236, 44);
            recipeNameToolbar.TabIndex = 1;
            // 
            // btnPreviewRecipeNames
            // 
            btnPreviewRecipeNames.BorderWidth = 1F;
            btnPreviewRecipeNames.IconSvg = "ApiOutlined";
            btnPreviewRecipeNames.Location = new Point(0, 5);
            btnPreviewRecipeNames.Margin = new Padding(0, 0, 8, 0);
            btnPreviewRecipeNames.Name = "btnPreviewRecipeNames";
            btnPreviewRecipeNames.Size = new Size(150, 34);
            btnPreviewRecipeNames.TabIndex = 0;
            btnPreviewRecipeNames.Text = "读取配方名称";
            // 
            // tableRecipeNames
            // 
            tableRecipeNames.Dock = DockStyle.Fill;
            tableRecipeNames.EditMode = AntdUI.TEditMode.DoubleClick;
            tableRecipeNames.Gap = 12;
            tableRecipeNames.Location = new Point(0, 78);
            tableRecipeNames.Margin = new Padding(0);
            tableRecipeNames.Name = "tableRecipeNames";
            tableRecipeNames.Size = new Size(1236, 79);
            tableRecipeNames.TabIndex = 2;
            // 
            // lblRecipeNamePreview
            // 
            lblRecipeNamePreview.Dock = DockStyle.Fill;
            lblRecipeNamePreview.Location = new Point(0, 157);
            lblRecipeNamePreview.Margin = new Padding(0);
            lblRecipeNamePreview.Name = "lblRecipeNamePreview";
            lblRecipeNamePreview.Size = new Size(1236, 32);
            lblRecipeNamePreview.TabIndex = 3;
            lblRecipeNamePreview.Text = "读取结果";
            lblRecipeNamePreview.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tableRecipeNamePreview
            // 
            tableRecipeNamePreview.Dock = DockStyle.Fill;
            tableRecipeNamePreview.Gap = 12;
            tableRecipeNamePreview.Location = new Point(0, 189);
            tableRecipeNamePreview.Margin = new Padding(0);
            tableRecipeNamePreview.Name = "tableRecipeNamePreview";
            tableRecipeNamePreview.Size = new Size(1236, 317);
            tableRecipeNamePreview.TabIndex = 4;
            // 
            // tabAlarmAddresses
            // 
            tabAlarmAddresses.Controls.Add(alarmAddressLayout);
            tabAlarmAddresses.Location = new Point(4, 29);
            tabAlarmAddresses.Name = "tabAlarmAddresses";
            tabAlarmAddresses.Padding = new Padding(3);
            tabAlarmAddresses.Size = new Size(1242, 512);
            tabAlarmAddresses.TabIndex = 5;
            tabAlarmAddresses.Text = "报警地址";
            tabAlarmAddresses.UseVisualStyleBackColor = true;
            // 
            // alarmAddressLayout
            // 
            alarmAddressLayout.ColumnCount = 1;
            alarmAddressLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            alarmAddressLayout.Controls.Add(lblAlarmAddressHint, 0, 0);
            alarmAddressLayout.Controls.Add(alarmAddressToolbar, 0, 1);
            alarmAddressLayout.Controls.Add(tableAlarmAddresses, 0, 2);
            alarmAddressLayout.Dock = DockStyle.Fill;
            alarmAddressLayout.Location = new Point(3, 3);
            alarmAddressLayout.Margin = new Padding(0);
            alarmAddressLayout.Name = "alarmAddressLayout";
            alarmAddressLayout.RowCount = 3;
            alarmAddressLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            alarmAddressLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            alarmAddressLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            alarmAddressLayout.Size = new Size(1236, 506);
            alarmAddressLayout.TabIndex = 0;
            // 
            // lblAlarmAddressHint
            // 
            lblAlarmAddressHint.AutoEllipsis = true;
            lblAlarmAddressHint.Dock = DockStyle.Fill;
            lblAlarmAddressHint.ForeColor = SystemColors.GrayText;
            lblAlarmAddressHint.Location = new Point(0, 0);
            lblAlarmAddressHint.Margin = new Padding(0);
            lblAlarmAddressHint.Name = "lblAlarmAddressHint";
            lblAlarmAddressHint.Size = new Size(1236, 34);
            lblAlarmAddressHint.TabIndex = 0;
            lblAlarmAddressHint.Text = "维护 PLC Bool 报警地址。工位 0 表示共享报警点；可从 Excel 复制两列：地址 / 内容。";
            lblAlarmAddressHint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // alarmAddressToolbar
            // 
            alarmAddressToolbar.Controls.Add(btnAddAlarmAddress);
            alarmAddressToolbar.Controls.Add(btnDeleteAlarmAddress);
            alarmAddressToolbar.Controls.Add(btnPasteAlarmAddresses);
            alarmAddressToolbar.Dock = DockStyle.Fill;
            alarmAddressToolbar.Location = new Point(0, 34);
            alarmAddressToolbar.Margin = new Padding(0);
            alarmAddressToolbar.Name = "alarmAddressToolbar";
            alarmAddressToolbar.Padding = new Padding(0, 4, 0, 4);
            alarmAddressToolbar.Size = new Size(1236, 42);
            alarmAddressToolbar.TabIndex = 1;
            alarmAddressToolbar.WrapContents = false;
            // 
            // btnAddAlarmAddress
            // 
            btnAddAlarmAddress.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnAddAlarmAddress.BorderWidth = 1F;
            btnAddAlarmAddress.IconSvg = "PlusOutlined";
            btnAddAlarmAddress.Location = new Point(0, 4);
            btnAddAlarmAddress.Margin = new Padding(0, 0, 8, 0);
            btnAddAlarmAddress.Name = "btnAddAlarmAddress";
            btnAddAlarmAddress.Size = new Size(81, 34);
            btnAddAlarmAddress.TabIndex = 0;
            btnAddAlarmAddress.Tag = "perm:button.address.add-alarm:enabled";
            btnAddAlarmAddress.Text = "新增";
            // 
            // btnDeleteAlarmAddress
            // 
            btnDeleteAlarmAddress.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnDeleteAlarmAddress.BorderWidth = 1F;
            btnDeleteAlarmAddress.IconSvg = "DeleteOutlined";
            btnDeleteAlarmAddress.Location = new Point(89, 4);
            btnDeleteAlarmAddress.Margin = new Padding(0, 0, 8, 0);
            btnDeleteAlarmAddress.Name = "btnDeleteAlarmAddress";
            btnDeleteAlarmAddress.Size = new Size(81, 34);
            btnDeleteAlarmAddress.TabIndex = 1;
            btnDeleteAlarmAddress.Tag = "perm:button.address.delete-alarm:enabled";
            btnDeleteAlarmAddress.Text = "删除";
            // 
            // btnPasteAlarmAddresses
            // 
            btnPasteAlarmAddresses.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnPasteAlarmAddresses.BorderWidth = 1F;
            btnPasteAlarmAddresses.IconSvg = "SnippetsOutlined";
            btnPasteAlarmAddresses.Location = new Point(178, 4);
            btnPasteAlarmAddresses.Margin = new Padding(0, 0, 8, 0);
            btnPasteAlarmAddresses.Name = "btnPasteAlarmAddresses";
            btnPasteAlarmAddresses.Size = new Size(112, 34);
            btnPasteAlarmAddresses.TabIndex = 2;
            btnPasteAlarmAddresses.Tag = "perm:button.address.paste-alarm:enabled";
            btnPasteAlarmAddresses.Text = "粘贴导入";
            // 
            // tableAlarmAddresses
            // 
            tableAlarmAddresses.Dock = DockStyle.Fill;
            tableAlarmAddresses.EditMode = AntdUI.TEditMode.DoubleClick;
            tableAlarmAddresses.Gap = 8;
            tableAlarmAddresses.Gaps = new Size(8, 8);
            tableAlarmAddresses.Location = new Point(0, 76);
            tableAlarmAddresses.Margin = new Padding(0);
            tableAlarmAddresses.Name = "tableAlarmAddresses";
            tableAlarmAddresses.Size = new Size(1236, 430);
            tableAlarmAddresses.TabIndex = 2;
            tableAlarmAddresses.Text = "tableAlarmAddresses";
            // 
            // tabTestItemAddresses
            // 
            tabTestItemAddresses.Controls.Add(testItemAddressLayout);
            tabTestItemAddresses.Location = new Point(4, 29);
            tabTestItemAddresses.Name = "tabTestItemAddresses";
            tabTestItemAddresses.Padding = new Padding(3);
            tabTestItemAddresses.Size = new Size(1242, 512);
            tabTestItemAddresses.TabIndex = 1;
            tabTestItemAddresses.Text = "产品工艺";
            tabTestItemAddresses.UseVisualStyleBackColor = true;
            // 
            // testItemAddressLayout
            // 
            testItemAddressLayout.ColumnCount = 1;
            testItemAddressLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            testItemAddressLayout.Controls.Add(lblTestItemAddressHint, 0, 0);
            testItemAddressLayout.Controls.Add(lblProductProcessGroupHint, 0, 1);
            testItemAddressLayout.Controls.Add(productProcessToolbar, 0, 2);
            testItemAddressLayout.Controls.Add(lblProductProcessSummary, 0, 3);
            testItemAddressLayout.Controls.Add(tableProcess, 0, 4);
            testItemAddressLayout.Dock = DockStyle.Fill;
            testItemAddressLayout.Location = new Point(3, 3);
            testItemAddressLayout.Margin = new Padding(0);
            testItemAddressLayout.Name = "testItemAddressLayout";
            testItemAddressLayout.RowCount = 5;
            testItemAddressLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            testItemAddressLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            testItemAddressLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            testItemAddressLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            testItemAddressLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            testItemAddressLayout.Size = new Size(1236, 506);
            testItemAddressLayout.TabIndex = 0;
            // 
            // lblTestItemAddressHint
            // 
            lblTestItemAddressHint.AutoEllipsis = true;
            lblTestItemAddressHint.Dock = DockStyle.Fill;
            lblTestItemAddressHint.ForeColor = SystemColors.GrayText;
            lblTestItemAddressHint.Location = new Point(0, 0);
            lblTestItemAddressHint.Margin = new Padding(0);
            lblTestItemAddressHint.Name = "lblTestItemAddressHint";
            lblTestItemAddressHint.Size = new Size(1236, 30);
            lblTestItemAddressHint.TabIndex = 1;
            lblTestItemAddressHint.Text = "维护产品工号、工位、焊点数量和 PLC 数据区布局；测试方案决定采集哪些测试项";
            lblTestItemAddressHint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblProductProcessGroupHint
            // 
            lblProductProcessGroupHint.AutoEllipsis = true;
            lblProductProcessGroupHint.Dock = DockStyle.Fill;
            lblProductProcessGroupHint.ForeColor = SystemColors.GrayText;
            lblProductProcessGroupHint.Location = new Point(0, 30);
            lblProductProcessGroupHint.Margin = new Padding(0);
            lblProductProcessGroupHint.Name = "lblProductProcessGroupHint";
            lblProductProcessGroupHint.Padding = new Padding(0, 4, 0, 0);
            lblProductProcessGroupHint.Size = new Size(1236, 34);
            lblProductProcessGroupHint.TabIndex = 2;
            lblProductProcessGroupHint.Text = "分组填写：产品头保存产品级字段，焊点头按焊点头长度递增，测试项区按测试区长度递增；最终地址可通过 PLC 地址预览核对。表达式：偏移:类型-规则_小数位；数值如 14:F-0_2，数值字符串如 0:S-8_3。";
            lblProductProcessGroupHint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // productProcessToolbar
            // 
            productProcessToolbar.Controls.Add(btnAddProductProcess);
            productProcessToolbar.Controls.Add(btnDeleteProductProcess);
            productProcessToolbar.Controls.Add(btnPreviewProductProcessAddress);
            productProcessToolbar.Dock = DockStyle.Fill;
            productProcessToolbar.Location = new Point(0, 64);
            productProcessToolbar.Margin = new Padding(0);
            productProcessToolbar.Name = "productProcessToolbar";
            productProcessToolbar.Padding = new Padding(0, 4, 0, 4);
            productProcessToolbar.Size = new Size(1236, 42);
            productProcessToolbar.TabIndex = 2;
            productProcessToolbar.WrapContents = false;
            // 
            // btnAddProductProcess
            // 
            btnAddProductProcess.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnAddProductProcess.BorderWidth = 1F;
            btnAddProductProcess.IconSvg = "PlusOutlined";
            btnAddProductProcess.Location = new Point(0, 4);
            btnAddProductProcess.Margin = new Padding(0, 0, 8, 0);
            btnAddProductProcess.Name = "btnAddProductProcess";
            btnAddProductProcess.Size = new Size(81, 34);
            btnAddProductProcess.TabIndex = 0;
            btnAddProductProcess.Tag = "perm:button.address.add-product-process:enabled";
            btnAddProductProcess.Text = "新增";
            // 
            // btnDeleteProductProcess
            // 
            btnDeleteProductProcess.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnDeleteProductProcess.BorderWidth = 1F;
            btnDeleteProductProcess.IconSvg = "DeleteOutlined";
            btnDeleteProductProcess.Location = new Point(89, 4);
            btnDeleteProductProcess.Margin = new Padding(0, 0, 8, 0);
            btnDeleteProductProcess.Name = "btnDeleteProductProcess";
            btnDeleteProductProcess.Size = new Size(81, 34);
            btnDeleteProductProcess.TabIndex = 1;
            btnDeleteProductProcess.Tag = "perm:button.address.delete-product-process:enabled";
            btnDeleteProductProcess.Text = "删除";
            // 
            // btnPreviewProductProcessAddress
            // 
            btnPreviewProductProcessAddress.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnPreviewProductProcessAddress.BorderWidth = 1F;
            btnPreviewProductProcessAddress.IconSvg = "EyeOutlined";
            btnPreviewProductProcessAddress.Location = new Point(178, 4);
            btnPreviewProductProcessAddress.Margin = new Padding(0, 0, 8, 0);
            btnPreviewProductProcessAddress.Name = "btnPreviewProductProcessAddress";
            btnPreviewProductProcessAddress.Size = new Size(141, 34);
            btnPreviewProductProcessAddress.TabIndex = 2;
            btnPreviewProductProcessAddress.Tag = "perm:button.address.preview-product-process-address:enabled";
            btnPreviewProductProcessAddress.Text = "PLC 地址预览";
            // 
            // lblProductProcessSummary
            // 
            lblProductProcessSummary.AutoEllipsis = true;
            lblProductProcessSummary.BackColor = Color.FromArgb(248, 250, 252);
            lblProductProcessSummary.Dock = DockStyle.Fill;
            lblProductProcessSummary.ForeColor = Color.FromArgb(73, 80, 87);
            lblProductProcessSummary.Location = new Point(0, 106);
            lblProductProcessSummary.Margin = new Padding(0);
            lblProductProcessSummary.Name = "lblProductProcessSummary";
            lblProductProcessSummary.Padding = new Padding(10, 6, 10, 0);
            lblProductProcessSummary.Size = new Size(1236, 34);
            lblProductProcessSummary.TabIndex = 4;
            lblProductProcessSummary.Text = "选择一条产品工艺后，可查看产品 -> 焊点 -> 测试项绑定摘要，并打开 PLC 地址预览";
            lblProductProcessSummary.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tableProcess
            // 
            tableProcess.Dock = DockStyle.Fill;
            tableProcess.EditMode = AntdUI.TEditMode.DoubleClick;
            tableProcess.Gap = 8;
            tableProcess.Gaps = new Size(8, 8);
            tableProcess.Location = new Point(0, 140);
            tableProcess.Margin = new Padding(0);
            tableProcess.Name = "tableProcess";
            tableProcess.Size = new Size(1236, 366);
            tableProcess.TabIndex = 5;
            tableProcess.Text = "tableProductProcessConfigs";
            // 
            // tabTestSchemes
            // 
            tabTestSchemes.Controls.Add(testSchemeLayout);
            tabTestSchemes.Location = new Point(4, 29);
            tabTestSchemes.Name = "tabTestSchemes";
            tabTestSchemes.Padding = new Padding(3);
            tabTestSchemes.Size = new Size(1242, 512);
            tabTestSchemes.TabIndex = 2;
            tabTestSchemes.Text = "测试方案";
            tabTestSchemes.UseVisualStyleBackColor = true;
            // 
            // testSchemeLayout
            // 
            testSchemeLayout.ColumnCount = 1;
            testSchemeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            testSchemeLayout.Controls.Add(lblTestSchemeHint, 0, 0);
            testSchemeLayout.Controls.Add(testSchemeToolbar, 0, 1);
            testSchemeLayout.Controls.Add(tableTestSchemes, 0, 2);
            testSchemeLayout.Dock = DockStyle.Fill;
            testSchemeLayout.Location = new Point(3, 3);
            testSchemeLayout.Margin = new Padding(0);
            testSchemeLayout.Name = "testSchemeLayout";
            testSchemeLayout.RowCount = 3;
            testSchemeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            testSchemeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            testSchemeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            testSchemeLayout.Size = new Size(1236, 506);
            testSchemeLayout.TabIndex = 0;
            // 
            // lblTestSchemeHint
            // 
            lblTestSchemeHint.AutoEllipsis = true;
            lblTestSchemeHint.Dock = DockStyle.Fill;
            lblTestSchemeHint.ForeColor = SystemColors.GrayText;
            lblTestSchemeHint.Location = new Point(0, 0);
            lblTestSchemeHint.Margin = new Padding(0);
            lblTestSchemeHint.Name = "lblTestSchemeHint";
            lblTestSchemeHint.Size = new Size(1236, 30);
            lblTestSchemeHint.TabIndex = 0;
            lblTestSchemeHint.Text = "维护测试方案主表。方案ID用于产品工艺绑定，保存后不建议改名";
            lblTestSchemeHint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // testSchemeToolbar
            // 
            testSchemeToolbar.Controls.Add(btnAddScheme);
            testSchemeToolbar.Controls.Add(btnDeleteScheme);
            testSchemeToolbar.Dock = DockStyle.Fill;
            testSchemeToolbar.Location = new Point(0, 30);
            testSchemeToolbar.Margin = new Padding(0);
            testSchemeToolbar.Name = "testSchemeToolbar";
            testSchemeToolbar.Padding = new Padding(0, 4, 0, 4);
            testSchemeToolbar.Size = new Size(1236, 42);
            testSchemeToolbar.TabIndex = 1;
            testSchemeToolbar.WrapContents = false;
            // 
            // btnAddScheme
            // 
            btnAddScheme.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnAddScheme.BorderWidth = 1F;
            btnAddScheme.IconSvg = "PlusOutlined";
            btnAddScheme.Location = new Point(0, 4);
            btnAddScheme.Margin = new Padding(0, 0, 8, 0);
            btnAddScheme.Name = "btnAddScheme";
            btnAddScheme.Size = new Size(81, 34);
            btnAddScheme.TabIndex = 0;
            btnAddScheme.Tag = "perm:button.address.add-scheme:enabled";
            btnAddScheme.Text = "新增";
            // 
            // btnDeleteScheme
            // 
            btnDeleteScheme.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnDeleteScheme.BorderWidth = 1F;
            btnDeleteScheme.IconSvg = "DeleteOutlined";
            btnDeleteScheme.Location = new Point(89, 4);
            btnDeleteScheme.Margin = new Padding(0, 0, 8, 0);
            btnDeleteScheme.Name = "btnDeleteScheme";
            btnDeleteScheme.Size = new Size(81, 34);
            btnDeleteScheme.TabIndex = 1;
            btnDeleteScheme.Tag = "perm:button.address.delete-scheme:enabled";
            btnDeleteScheme.Text = "删除";
            // 
            // tableTestSchemes
            // 
            tableTestSchemes.Dock = DockStyle.Fill;
            tableTestSchemes.EditMode = AntdUI.TEditMode.DoubleClick;
            tableTestSchemes.Gap = 8;
            tableTestSchemes.Gaps = new Size(8, 8);
            tableTestSchemes.Location = new Point(0, 72);
            tableTestSchemes.Margin = new Padding(0);
            tableTestSchemes.Name = "tableTestSchemes";
            tableTestSchemes.Size = new Size(1236, 434);
            tableTestSchemes.TabIndex = 2;
            tableTestSchemes.Text = "tableTestSchemes";
            // 
            // tabSchemeDetails
            // 
            tabSchemeDetails.Controls.Add(schemeDetailLayout);
            tabSchemeDetails.Location = new Point(4, 29);
            tabSchemeDetails.Name = "tabSchemeDetails";
            tabSchemeDetails.Padding = new Padding(3);
            tabSchemeDetails.Size = new Size(1242, 512);
            tabSchemeDetails.TabIndex = 3;
            tabSchemeDetails.Text = "方案明细";
            tabSchemeDetails.UseVisualStyleBackColor = true;
            // 
            // schemeDetailLayout
            // 
            schemeDetailLayout.ColumnCount = 1;
            schemeDetailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            schemeDetailLayout.Controls.Add(lblSchemeDetailHint, 0, 0);
            schemeDetailLayout.Controls.Add(schemeDetailToolbar, 0, 1);
            schemeDetailLayout.Controls.Add(schemeDetailSplitContainer, 0, 2);
            schemeDetailLayout.Dock = DockStyle.Fill;
            schemeDetailLayout.Location = new Point(3, 3);
            schemeDetailLayout.Margin = new Padding(0);
            schemeDetailLayout.Name = "schemeDetailLayout";
            schemeDetailLayout.RowCount = 3;
            schemeDetailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            schemeDetailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            schemeDetailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            schemeDetailLayout.Size = new Size(1236, 506);
            schemeDetailLayout.TabIndex = 0;
            // 
            // lblSchemeDetailHint
            // 
            lblSchemeDetailHint.AutoEllipsis = true;
            lblSchemeDetailHint.Dock = DockStyle.Fill;
            lblSchemeDetailHint.ForeColor = SystemColors.GrayText;
            lblSchemeDetailHint.Location = new Point(0, 0);
            lblSchemeDetailHint.Margin = new Padding(0);
            lblSchemeDetailHint.Name = "lblSchemeDetailHint";
            lblSchemeDetailHint.Size = new Size(1236, 30);
            lblSchemeDetailHint.TabIndex = 0;
            lblSchemeDetailHint.Text = "维护测试方案包含哪些测试项。同一方案中同一测试项只能出现一次";
            lblSchemeDetailHint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // schemeDetailToolbar
            // 
            schemeDetailToolbar.Controls.Add(lblSchemeDetailScheme);
            schemeDetailToolbar.Controls.Add(selectSchemeDetailScheme);
            schemeDetailToolbar.Dock = DockStyle.Fill;
            schemeDetailToolbar.Location = new Point(0, 30);
            schemeDetailToolbar.Margin = new Padding(0);
            schemeDetailToolbar.Name = "schemeDetailToolbar";
            schemeDetailToolbar.Padding = new Padding(0, 4, 0, 4);
            schemeDetailToolbar.Size = new Size(1236, 42);
            schemeDetailToolbar.TabIndex = 1;
            schemeDetailToolbar.WrapContents = false;
            // 
            // lblSchemeDetailScheme
            // 
            lblSchemeDetailScheme.AutoSize = true;
            lblSchemeDetailScheme.Dock = DockStyle.Fill;
            lblSchemeDetailScheme.Location = new Point(0, 4);
            lblSchemeDetailScheme.Margin = new Padding(0, 0, 8, 0);
            lblSchemeDetailScheme.Name = "lblSchemeDetailScheme";
            lblSchemeDetailScheme.Size = new Size(73, 34);
            lblSchemeDetailScheme.TabIndex = 0;
            lblSchemeDetailScheme.Text = "测试方案";
            lblSchemeDetailScheme.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // selectSchemeDetailScheme
            // 
            selectSchemeDetailScheme.Location = new Point(81, 4);
            selectSchemeDetailScheme.Margin = new Padding(0, 0, 8, 0);
            selectSchemeDetailScheme.MaxCount = 10;
            selectSchemeDetailScheme.Name = "selectSchemeDetailScheme";
            selectSchemeDetailScheme.Size = new Size(260, 34);
            selectSchemeDetailScheme.TabIndex = 1;
            // 
            // schemeDetailSplitContainer
            // 
            schemeDetailSplitContainer.Dock = DockStyle.Fill;
            schemeDetailSplitContainer.FixedPanel = FixedPanel.Panel1;
            schemeDetailSplitContainer.Location = new Point(0, 72);
            schemeDetailSplitContainer.Margin = new Padding(0);
            schemeDetailSplitContainer.Name = "schemeDetailSplitContainer";
            // 
            // schemeDetailSplitContainer.Panel1
            // 
            schemeDetailSplitContainer.Panel1.Controls.Add(treeSchemeDetails);
            // 
            // schemeDetailSplitContainer.Panel2
            // 
            schemeDetailSplitContainer.Panel2.Controls.Add(schemeDetailRoleGrid);
            schemeDetailSplitContainer.Size = new Size(1236, 434);
            schemeDetailSplitContainer.SplitterDistance = 288;
            schemeDetailSplitContainer.SplitterWidth = 6;
            schemeDetailSplitContainer.TabIndex = 2;
            // 
            // treeSchemeDetails
            // 
            treeSchemeDetails.BorderStyle = BorderStyle.FixedSingle;
            treeSchemeDetails.CheckBoxes = true;
            treeSchemeDetails.Dock = DockStyle.Fill;
            treeSchemeDetails.HideSelection = false;
            treeSchemeDetails.Location = new Point(0, 0);
            treeSchemeDetails.Margin = new Padding(0);
            treeSchemeDetails.Name = "treeSchemeDetails";
            treeSchemeDetails.Size = new Size(288, 434);
            treeSchemeDetails.TabIndex = 0;
            // 
            // schemeDetailRoleGrid
            // 
            schemeDetailRoleGrid.AllowUserToAddRows = false;
            schemeDetailRoleGrid.AllowUserToDeleteRows = false;
            schemeDetailRoleGrid.BackgroundColor = Color.White;
            schemeDetailRoleGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            schemeDetailRoleGrid.Dock = DockStyle.Fill;
            schemeDetailRoleGrid.Location = new Point(0, 0);
            schemeDetailRoleGrid.Margin = new Padding(0);
            schemeDetailRoleGrid.MultiSelect = false;
            schemeDetailRoleGrid.Name = "schemeDetailRoleGrid";
            schemeDetailRoleGrid.RowHeadersVisible = false;
            schemeDetailRoleGrid.RowHeadersWidth = 51;
            schemeDetailRoleGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            schemeDetailRoleGrid.Size = new Size(942, 434);
            schemeDetailRoleGrid.TabIndex = 0;
            // 
            // tabTestItems
            // 
            tabTestItems.Controls.Add(testItemLayout);
            tabTestItems.Location = new Point(4, 29);
            tabTestItems.Name = "tabTestItems";
            tabTestItems.Padding = new Padding(3);
            tabTestItems.Size = new Size(1242, 512);
            tabTestItems.TabIndex = 4;
            tabTestItems.Text = "测试项字典";
            tabTestItems.UseVisualStyleBackColor = true;
            // 
            // testItemLayout
            // 
            testItemLayout.ColumnCount = 1;
            testItemLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            testItemLayout.Controls.Add(lblTestItemHint, 0, 0);
            testItemLayout.Controls.Add(testItemToolbar, 0, 1);
            testItemLayout.Controls.Add(tableTestItems, 0, 2);
            testItemLayout.Dock = DockStyle.Fill;
            testItemLayout.Location = new Point(3, 3);
            testItemLayout.Margin = new Padding(0);
            testItemLayout.Name = "testItemLayout";
            testItemLayout.RowCount = 3;
            testItemLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            testItemLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            testItemLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            testItemLayout.Size = new Size(1236, 506);
            testItemLayout.TabIndex = 0;
            // 
            // lblTestItemHint
            // 
            lblTestItemHint.AutoEllipsis = true;
            lblTestItemHint.Dock = DockStyle.Fill;
            lblTestItemHint.ForeColor = SystemColors.GrayText;
            lblTestItemHint.Location = new Point(0, 0);
            lblTestItemHint.Margin = new Padding(0);
            lblTestItemHint.Name = "lblTestItemHint";
            lblTestItemHint.Size = new Size(1236, 30);
            lblTestItemHint.TabIndex = 0;
            lblTestItemHint.Text = "维护测试项名称、单位和相对偏移表达式。表达式：偏移:类型-规则_小数位；类型 B/H/I/F/S；规则 0原值、1除以10、2除以100、3除以1000、4结果(2=NG、3=OK、4=焊前NG)；数值如 14:F-0_2，数值字符串如 0:S-8_3。";
            lblTestItemHint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // testItemToolbar
            // 
            testItemToolbar.Controls.Add(btnAddTestItem);
            testItemToolbar.Controls.Add(btnDeleteTestItem);
            testItemToolbar.Dock = DockStyle.Fill;
            testItemToolbar.Location = new Point(0, 30);
            testItemToolbar.Margin = new Padding(0);
            testItemToolbar.Name = "testItemToolbar";
            testItemToolbar.Padding = new Padding(0, 4, 0, 4);
            testItemToolbar.Size = new Size(1236, 42);
            testItemToolbar.TabIndex = 1;
            testItemToolbar.WrapContents = false;
            // 
            // btnAddTestItem
            // 
            btnAddTestItem.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnAddTestItem.BorderWidth = 1F;
            btnAddTestItem.IconSvg = "PlusOutlined";
            btnAddTestItem.Location = new Point(0, 4);
            btnAddTestItem.Margin = new Padding(0, 0, 8, 0);
            btnAddTestItem.Name = "btnAddTestItem";
            btnAddTestItem.Size = new Size(81, 34);
            btnAddTestItem.TabIndex = 0;
            btnAddTestItem.Tag = "perm:button.address.add-test-item:enabled";
            btnAddTestItem.Text = "新增";
            // 
            // btnDeleteTestItem
            // 
            btnDeleteTestItem.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnDeleteTestItem.BorderWidth = 1F;
            btnDeleteTestItem.IconSvg = "DeleteOutlined";
            btnDeleteTestItem.Location = new Point(89, 4);
            btnDeleteTestItem.Margin = new Padding(0, 0, 8, 0);
            btnDeleteTestItem.Name = "btnDeleteTestItem";
            btnDeleteTestItem.Size = new Size(81, 34);
            btnDeleteTestItem.TabIndex = 1;
            btnDeleteTestItem.Tag = "perm:button.address.delete-test-item:enabled";
            btnDeleteTestItem.Text = "删除";
            // 
            // tableTestItems
            // 
            tableTestItems.Dock = DockStyle.Fill;
            tableTestItems.EditMode = AntdUI.TEditMode.DoubleClick;
            tableTestItems.Gap = 8;
            tableTestItems.Gaps = new Size(8, 8);
            tableTestItems.Location = new Point(0, 72);
            tableTestItems.Margin = new Padding(0);
            tableTestItems.Name = "tableTestItems";
            tableTestItems.Size = new Size(1236, 434);
            tableTestItems.TabIndex = 2;
            tableTestItems.Text = "tableTestItems";
            // 
            // AddressManageView
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(rootLayout);
            Name = "AddressManageView";
            Size = new Size(1298, 721);
            rootLayout.ResumeLayout(false);
            headerLayout.ResumeLayout(false);
            headerLayout.PerformLayout();
            titleLayout.ResumeLayout(false);
            titleLayout.PerformLayout();
            SaveLayout.ResumeLayout(false);
            SaveLayout.PerformLayout();
            bindingFlowPanel.ResumeLayout(false);
            bindingFlowPanel.PerformLayout();
            tabAddressCategories.ResumeLayout(false);
            tabBusinessAddresses.ResumeLayout(false);
            tabRecipeNames.ResumeLayout(false);
            recipeNameLayout.ResumeLayout(false);
            recipeNameToolbar.ResumeLayout(false);
            tabAlarmAddresses.ResumeLayout(false);
            alarmAddressLayout.ResumeLayout(false);
            alarmAddressToolbar.ResumeLayout(false);
            alarmAddressToolbar.PerformLayout();
            tabTestItemAddresses.ResumeLayout(false);
            testItemAddressLayout.ResumeLayout(false);
            productProcessToolbar.ResumeLayout(false);
            productProcessToolbar.PerformLayout();
            tabTestSchemes.ResumeLayout(false);
            testSchemeLayout.ResumeLayout(false);
            testSchemeToolbar.ResumeLayout(false);
            testSchemeToolbar.PerformLayout();
            tabSchemeDetails.ResumeLayout(false);
            schemeDetailLayout.ResumeLayout(false);
            schemeDetailToolbar.ResumeLayout(false);
            schemeDetailToolbar.PerformLayout();
            schemeDetailSplitContainer.Panel1.ResumeLayout(false);
            schemeDetailSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)schemeDetailSplitContainer).EndInit();
            schemeDetailSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)schemeDetailRoleGrid).EndInit();
            tabTestItems.ResumeLayout(false);
            testItemLayout.ResumeLayout(false);
            testItemToolbar.ResumeLayout(false);
            testItemToolbar.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel rootLayout;
        private TableLayoutPanel headerLayout;
        private TableLayoutPanel titleLayout;
        private Label lblTitle;
        private Label lblDescription;
        private AntdUI.Button btnSave;
        private AntdUI.Button btnRefresh;
        private AntdUI.Button btnTest;
        private Controls.InputQuery queryAddresses;
        private FlowLayoutPanel bindingFlowPanel;
        private Label lblBindingProduct;
        private Label lblBindingArrow1;
        private Label lblBindingProcess;
        private Label lblBindingArrow2;
        private Label lblBindingScheme;
        private Label lblBindingArrow3;
        private Label lblBindingDetail;
        private Label lblBindingArrow4;
        private Label lblBindingItem;
        private Label lblBindingArrow5;
        private Label lblBindingPreview;
        private TabControl tabAddressCategories;
        private TabPage tabRecipeNames;
        private TableLayoutPanel recipeNameLayout;
        private Label lblRecipeNameHint;
        private FlowLayoutPanel recipeNameToolbar;
        private AntdUI.Button btnPreviewRecipeNames;
        private AntdUI.Table tableRecipeNames;
        private Label lblRecipeNamePreview;
        private AntdUI.Table tableRecipeNamePreview;
        private TabPage tabBusinessAddresses;
        private AntdUI.Table tableAddresses;
        private TabPage tabAlarmAddresses;
        private TableLayoutPanel alarmAddressLayout;
        private Label lblAlarmAddressHint;
        private FlowLayoutPanel alarmAddressToolbar;
        private AntdUI.Button btnAddAlarmAddress;
        private AntdUI.Button btnDeleteAlarmAddress;
        private AntdUI.Button btnPasteAlarmAddresses;
        private AntdUI.Table tableAlarmAddresses;
        private TabPage tabTestItemAddresses;
        private TableLayoutPanel testItemAddressLayout;
        private Label lblTestItemAddressHint;
        private Label lblProductProcessGroupHint;
        private FlowLayoutPanel productProcessToolbar;
        private AntdUI.Button btnAddProductProcess;
        private AntdUI.Button btnDeleteProductProcess;
        private AntdUI.Button btnPreviewProductProcessAddress;
        private Label lblProductProcessSummary;
        private AntdUI.Table tableProcess;
        private TabPage tabTestSchemes;
        private TableLayoutPanel testSchemeLayout;
        private Label lblTestSchemeHint;
        private FlowLayoutPanel testSchemeToolbar;
        private AntdUI.Button btnAddScheme;
        private AntdUI.Button btnDeleteScheme;
        private AntdUI.Table tableTestSchemes;
        private TabPage tabSchemeDetails;
        private TableLayoutPanel schemeDetailLayout;
        private Label lblSchemeDetailHint;
        private FlowLayoutPanel schemeDetailToolbar;
        private Label lblSchemeDetailScheme;
        private AntdUI.Select selectSchemeDetailScheme;
        private SplitContainer schemeDetailSplitContainer;
        private TreeView treeSchemeDetails;
        private DataGridView schemeDetailRoleGrid;
        private TabPage tabTestItems;
        private TableLayoutPanel testItemLayout;
        private Label lblTestItemHint;
        private FlowLayoutPanel testItemToolbar;
        private AntdUI.Button btnAddTestItem;
        private AntdUI.Button btnDeleteTestItem;
        private AntdUI.Table tableTestItems;
        private TableLayoutPanel SaveLayout;
    }
}
