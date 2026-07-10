namespace AutoWeldSystem.UI.Views
{
    partial class DataManageView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                BeginDispose();
                _detailQueryCancellation?.Dispose();
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            workOrderBindingSource = new BindingSource(components);
            parameterBindingSource = new BindingSource(components);
            collectionBindingSource = new BindingSource(components);
            reportBindingSource = new BindingSource(components);
            rootLayout = new TableLayoutPanel();
            filterPanel = new Panel();
            filterLayout = new TableLayoutPanel();
            lblProductNum = new Label();
            txtProductNum = new AntdUI.Input();
            lblBatch = new Label();
            txtBatch = new AntdUI.Input();
            lblWorkOrder = new Label();
            txtWorkOrder = new AntdUI.Input();
            lblDateRange = new Label();
            dateRange = new AntdUI.DatePickerRange();
            btnQuery = new AntdUI.Button();
            btnReset = new AntdUI.Button();
            mainSplitter = new SplitContainer();
            workOrderLayout = new TableLayoutPanel();
            lblWorkOrderSummary = new Label();
            dgvWorkOrders = new DataGridView();
            workOrderPagination = new AntdUI.Pagination();
            detailTabs = new TabControl();
            tabWeldParameters = new TabPage();
            parameterLayout = new TableLayoutPanel();
            lblParameterSummary = new Label();
            dgvWeldParameters = new DataGridView();
            tabCollectionData = new TabPage();
            collectionSplitter = new SplitContainer();
            collectionLayout = new TableLayoutPanel();
            lblCollectionSummary = new Label();
            dgvCollectionRecords = new DataGridView();
            collectionPagination = new AntdUI.Pagination();
            rawDataLayout = new TableLayoutPanel();
            lblRawData = new Label();
            txtRawData = new TextBox();
            tabReportFiles = new TabPage();
            reportLayout = new TableLayoutPanel();
            reportToolbar = new FlowLayoutPanel();
            btnOpenReport = new AntdUI.Button();
            btnOpenReportFolder = new AntdUI.Button();
            lblReportSummary = new Label();
            dgvReportFiles = new DataGridView();
            colTaskStation = new DataGridViewTextBoxColumn();
            colTaskWorkOrder = new DataGridViewTextBoxColumn();
            colTaskProductNum = new DataGridViewTextBoxColumn();
            colTaskBatch = new DataGridViewTextBoxColumn();
            colTaskProductName = new DataGridViewTextBoxColumn();
            colTaskProcess = new DataGridViewTextBoxColumn();
            colTaskRecipe = new DataGridViewTextBoxColumn();
            colTaskPlannedQty = new DataGridViewTextBoxColumn();
            colTaskActualQty = new DataGridViewTextBoxColumn();
            colTaskQualifiedQty = new DataGridViewTextBoxColumn();
            colTaskFailedQty = new DataGridViewTextBoxColumn();
            colTaskOperator = new DataGridViewTextBoxColumn();
            colTaskStartTime = new DataGridViewTextBoxColumn();
            colTaskEndTime = new DataGridViewTextBoxColumn();
            colTaskStatus = new DataGridViewTextBoxColumn();
            colTaskUploadStatus = new DataGridViewTextBoxColumn();
            colParameterStation = new DataGridViewTextBoxColumn();
            colParameterProductNo = new DataGridViewTextBoxColumn();
            colParameterTouchNo = new DataGridViewTextBoxColumn();
            colParameterResult = new DataGridViewTextBoxColumn();
            colParameterRecordTime = new DataGridViewTextBoxColumn();
            colCollectionSequence = new DataGridViewTextBoxColumn();
            colCollectionStation = new DataGridViewTextBoxColumn();
            colCollectionProductNo = new DataGridViewTextBoxColumn();
            colCollectionTouchNo = new DataGridViewTextBoxColumn();
            colCollectionResult = new DataGridViewTextBoxColumn();
            colCollectionIsTest = new DataGridViewCheckBoxColumn();
            colCollectionCompleted = new DataGridViewCheckBoxColumn();
            colCollectionUploadStatus = new DataGridViewTextBoxColumn();
            colCollectionOperator = new DataGridViewTextBoxColumn();
            colCollectionRecordTime = new DataGridViewTextBoxColumn();
            colReportFileName = new DataGridViewTextBoxColumn();
            colReportFormat = new DataGridViewTextBoxColumn();
            colReportPath = new DataGridViewTextBoxColumn();
            colReportUploadStatus = new DataGridViewTextBoxColumn();
            colReportCreatedTime = new DataGridViewTextBoxColumn();
            colReportUpdatedTime = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)workOrderBindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)parameterBindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)collectionBindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)reportBindingSource).BeginInit();
            rootLayout.SuspendLayout();
            filterPanel.SuspendLayout();
            filterLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplitter).BeginInit();
            mainSplitter.Panel1.SuspendLayout();
            mainSplitter.Panel2.SuspendLayout();
            mainSplitter.SuspendLayout();
            workOrderLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvWorkOrders).BeginInit();
            detailTabs.SuspendLayout();
            tabWeldParameters.SuspendLayout();
            parameterLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvWeldParameters).BeginInit();
            tabCollectionData.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)collectionSplitter).BeginInit();
            collectionSplitter.Panel1.SuspendLayout();
            collectionSplitter.Panel2.SuspendLayout();
            collectionSplitter.SuspendLayout();
            collectionLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvCollectionRecords).BeginInit();
            rawDataLayout.SuspendLayout();
            tabReportFiles.SuspendLayout();
            reportLayout.SuspendLayout();
            reportToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvReportFiles).BeginInit();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(filterPanel, 0, 0);
            rootLayout.Controls.Add(mainSplitter, 0, 1);
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.RowCount = 2;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.Size = new Size(1456, 760);
            rootLayout.TabIndex = 0;
            // 
            // filterPanel
            // 
            filterPanel.Controls.Add(filterLayout);
            filterPanel.Dock = DockStyle.Fill;
            filterPanel.Location = new Point(16, 12);
            filterPanel.Margin = new Padding(16, 12, 16, 8);
            filterPanel.Name = "filterPanel";
            filterPanel.Size = new Size(1424, 62);
            filterPanel.TabIndex = 0;
            // 
            // filterLayout
            // 
            filterLayout.ColumnCount = 10;
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.0526314F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18.4210529F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.0526314F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39.4736824F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            filterLayout.Controls.Add(lblProductNum, 0, 0);
            filterLayout.Controls.Add(txtProductNum, 1, 0);
            filterLayout.Controls.Add(lblBatch, 2, 0);
            filterLayout.Controls.Add(txtBatch, 3, 0);
            filterLayout.Controls.Add(lblWorkOrder, 4, 0);
            filterLayout.Controls.Add(txtWorkOrder, 5, 0);
            filterLayout.Controls.Add(lblDateRange, 6, 0);
            filterLayout.Controls.Add(dateRange, 7, 0);
            filterLayout.Controls.Add(btnQuery, 8, 0);
            filterLayout.Controls.Add(btnReset, 9, 0);
            filterLayout.Dock = DockStyle.Fill;
            filterLayout.Location = new Point(0, 0);
            filterLayout.Name = "filterLayout";
            filterLayout.RowCount = 1;
            filterLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            filterLayout.Size = new Size(1424, 62);
            filterLayout.TabIndex = 0;
            // 
            // lblProductNum
            // 
            lblProductNum.Dock = DockStyle.Fill;
            lblProductNum.Location = new Point(3, 0);
            lblProductNum.Name = "lblProductNum";
            lblProductNum.Size = new Size(84, 62);
            lblProductNum.TabIndex = 0;
            lblProductNum.Text = "产品工号";
            lblProductNum.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtProductNum
            // 
            txtProductNum.Dock = DockStyle.Fill;
            txtProductNum.Location = new Point(93, 10);
            txtProductNum.Margin = new Padding(3, 10, 8, 10);
            txtProductNum.Name = "txtProductNum";
            txtProductNum.PlaceholderText = "模糊查询";
            txtProductNum.Size = new Size(180, 42);
            txtProductNum.TabIndex = 1;
            // 
            // lblBatch
            // 
            lblBatch.Dock = DockStyle.Fill;
            lblBatch.Location = new Point(284, 0);
            lblBatch.Name = "lblBatch";
            lblBatch.Size = new Size(60, 62);
            lblBatch.TabIndex = 2;
            lblBatch.Text = "批次";
            lblBatch.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtBatch
            // 
            txtBatch.Dock = DockStyle.Fill;
            txtBatch.Location = new Point(350, 10);
            txtBatch.Margin = new Padding(3, 10, 8, 10);
            txtBatch.Name = "txtBatch";
            txtBatch.PlaceholderText = "模糊查询";
            txtBatch.Size = new Size(156, 42);
            txtBatch.TabIndex = 3;
            // 
            // lblWorkOrder
            // 
            lblWorkOrder.Dock = DockStyle.Fill;
            lblWorkOrder.Location = new Point(517, 0);
            lblWorkOrder.Name = "lblWorkOrder";
            lblWorkOrder.Size = new Size(80, 62);
            lblWorkOrder.TabIndex = 4;
            lblWorkOrder.Text = "工单号";
            lblWorkOrder.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtWorkOrder
            // 
            txtWorkOrder.Dock = DockStyle.Fill;
            txtWorkOrder.Location = new Point(603, 10);
            txtWorkOrder.Margin = new Padding(3, 10, 8, 10);
            txtWorkOrder.Name = "txtWorkOrder";
            txtWorkOrder.PlaceholderText = "流转卡号";
            txtWorkOrder.Size = new Size(180, 42);
            txtWorkOrder.TabIndex = 5;
            // 
            // lblDateRange
            // 
            lblDateRange.Dock = DockStyle.Fill;
            lblDateRange.Location = new Point(794, 0);
            lblDateRange.Name = "lblDateRange";
            lblDateRange.Size = new Size(74, 62);
            lblDateRange.TabIndex = 6;
            lblDateRange.Text = "日期";
            lblDateRange.TextAlign = ContentAlignment.MiddleRight;
            // 
            // dateRange
            // 
            dateRange.Dock = DockStyle.Fill;
            dateRange.Location = new Point(874, 10);
            dateRange.Margin = new Padding(3, 10, 8, 10);
            dateRange.Name = "dateRange";
            dateRange.Size = new Size(348, 42);
            dateRange.TabIndex = 7;
            // 
            // btnQuery
            // 
            btnQuery.BorderWidth = 1F;
            btnQuery.Dock = DockStyle.Fill;
            btnQuery.IconSvg = "SearchOutlined";
            btnQuery.Location = new Point(1230, 10);
            btnQuery.Margin = new Padding(0, 10, 8, 10);
            btnQuery.Name = "btnQuery";
            btnQuery.Size = new Size(88, 42);
            btnQuery.TabIndex = 8;
            btnQuery.Tag = "perm:button.data.query:enabled";
            btnQuery.Text = "查询";
            btnQuery.Type = AntdUI.TTypeMini.Primary;
            // 
            // btnReset
            // 
            btnReset.BorderWidth = 1F;
            btnReset.Dock = DockStyle.Fill;
            btnReset.IconSvg = "ClearOutlined";
            btnReset.Location = new Point(1326, 10);
            btnReset.Margin = new Padding(0, 10, 0, 10);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(98, 42);
            btnReset.TabIndex = 9;
            btnReset.Tag = "perm:button.data.reset:enabled";
            btnReset.Text = "重置";
            // 
            // mainSplitter
            // 
            mainSplitter.Dock = DockStyle.Fill;
            mainSplitter.Location = new Point(16, 82);
            mainSplitter.Margin = new Padding(16, 0, 16, 16);
            mainSplitter.Name = "mainSplitter";
            mainSplitter.Orientation = Orientation.Horizontal;
            // 
            // mainSplitter.Panel1
            // 
            mainSplitter.Panel1.Controls.Add(workOrderLayout);
            mainSplitter.Panel1MinSize = 220;
            // 
            // mainSplitter.Panel2
            // 
            mainSplitter.Panel2.Controls.Add(detailTabs);
            mainSplitter.Panel2MinSize = 220;
            mainSplitter.Size = new Size(1424, 662);
            mainSplitter.SplitterDistance = 300;
            mainSplitter.SplitterWidth = 6;
            mainSplitter.TabIndex = 1;
            // 
            // workOrderLayout
            // 
            workOrderLayout.ColumnCount = 1;
            workOrderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            workOrderLayout.Controls.Add(lblWorkOrderSummary, 0, 0);
            workOrderLayout.Controls.Add(dgvWorkOrders, 0, 1);
            workOrderLayout.Controls.Add(workOrderPagination, 0, 2);
            workOrderLayout.Dock = DockStyle.Fill;
            workOrderLayout.Location = new Point(0, 0);
            workOrderLayout.Name = "workOrderLayout";
            workOrderLayout.RowCount = 3;
            workOrderLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            workOrderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            workOrderLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            workOrderLayout.Size = new Size(1424, 300);
            workOrderLayout.TabIndex = 0;
            // 
            // lblWorkOrderSummary
            // 
            lblWorkOrderSummary.Dock = DockStyle.Fill;
            lblWorkOrderSummary.ForeColor = SystemColors.GrayText;
            lblWorkOrderSummary.Location = new Point(3, 0);
            lblWorkOrderSummary.Name = "lblWorkOrderSummary";
            lblWorkOrderSummary.Size = new Size(1418, 30);
            lblWorkOrderSummary.TabIndex = 0;
            lblWorkOrderSummary.Text = "历史工单：0 条";
            lblWorkOrderSummary.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // dgvWorkOrders
            // 
            dgvWorkOrders.AllowUserToAddRows = false;
            dgvWorkOrders.AllowUserToDeleteRows = false;
            dgvWorkOrders.AutoGenerateColumns = false;
            dgvWorkOrders.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvWorkOrders.DataSource = workOrderBindingSource;
            dgvWorkOrders.Dock = DockStyle.Fill;
            dgvWorkOrders.Location = new Point(0, 30);
            dgvWorkOrders.Margin = new Padding(0);
            dgvWorkOrders.MultiSelect = false;
            dgvWorkOrders.Name = "dgvWorkOrders";
            dgvWorkOrders.ReadOnly = true;
            dgvWorkOrders.RowHeadersVisible = false;
            dgvWorkOrders.RowHeadersWidth = 51;
            dgvWorkOrders.RowTemplate.Height = 28;
            dgvWorkOrders.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvWorkOrders.Size = new Size(1424, 222);
            dgvWorkOrders.TabIndex = 1;
            // 
            // workOrderPagination
            // 
            workOrderPagination.Dock = DockStyle.Fill;
            workOrderPagination.Location = new Point(3, 255);
            workOrderPagination.Name = "workOrderPagination";
            workOrderPagination.PageSize = 20;
            workOrderPagination.PageSizeOptions = new int[]
    {
    20,
    50,
    100
    };
            workOrderPagination.RecordsPerPageText = "条/页";
            workOrderPagination.ShowSizeChanger = true;
            workOrderPagination.Size = new Size(1418, 42);
            workOrderPagination.TabIndex = 2;
            // 
            // detailTabs
            // 
            detailTabs.Controls.Add(tabWeldParameters);
            detailTabs.Controls.Add(tabCollectionData);
            detailTabs.Controls.Add(tabReportFiles);
            detailTabs.Dock = DockStyle.Fill;
            detailTabs.Location = new Point(0, 0);
            detailTabs.Name = "detailTabs";
            detailTabs.SelectedIndex = 0;
            detailTabs.Size = new Size(1424, 356);
            detailTabs.TabIndex = 0;
            // 
            // tabWeldParameters
            // 
            tabWeldParameters.Controls.Add(parameterLayout);
            tabWeldParameters.Location = new Point(4, 32);
            tabWeldParameters.Name = "tabWeldParameters";
            tabWeldParameters.Padding = new Padding(3);
            tabWeldParameters.Size = new Size(1416, 320);
            tabWeldParameters.TabIndex = 0;
            tabWeldParameters.Text = "焊接参数";
            tabWeldParameters.UseVisualStyleBackColor = true;
            // 
            // parameterLayout
            // 
            parameterLayout.ColumnCount = 1;
            parameterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            parameterLayout.Controls.Add(lblParameterSummary, 0, 0);
            parameterLayout.Controls.Add(dgvWeldParameters, 0, 1);
            parameterLayout.Dock = DockStyle.Fill;
            parameterLayout.Location = new Point(3, 3);
            parameterLayout.Name = "parameterLayout";
            parameterLayout.RowCount = 2;
            parameterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            parameterLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            parameterLayout.Size = new Size(1410, 314);
            parameterLayout.TabIndex = 0;
            // 
            // lblParameterSummary
            // 
            lblParameterSummary.Dock = DockStyle.Fill;
            lblParameterSummary.ForeColor = SystemColors.GrayText;
            lblParameterSummary.Location = new Point(3, 0);
            lblParameterSummary.Name = "lblParameterSummary";
            lblParameterSummary.Size = new Size(1404, 30);
            lblParameterSummary.TabIndex = 0;
            lblParameterSummary.Text = "请选择历史工单";
            lblParameterSummary.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // dgvWeldParameters
            // 
            dgvWeldParameters.AllowUserToAddRows = false;
            dgvWeldParameters.AllowUserToDeleteRows = false;
            dgvWeldParameters.AutoGenerateColumns = false;
            dgvWeldParameters.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvWeldParameters.DataSource = parameterBindingSource;
            dgvWeldParameters.Dock = DockStyle.Fill;
            dgvWeldParameters.Location = new Point(0, 30);
            dgvWeldParameters.Margin = new Padding(0);
            dgvWeldParameters.Name = "dgvWeldParameters";
            dgvWeldParameters.ReadOnly = true;
            dgvWeldParameters.RowHeadersVisible = false;
            dgvWeldParameters.RowHeadersWidth = 51;
            dgvWeldParameters.RowTemplate.Height = 28;
            dgvWeldParameters.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvWeldParameters.Size = new Size(1410, 284);
            dgvWeldParameters.TabIndex = 1;
            // 
            // tabCollectionData
            // 
            tabCollectionData.Controls.Add(collectionSplitter);
            tabCollectionData.Location = new Point(4, 29);
            tabCollectionData.Name = "tabCollectionData";
            tabCollectionData.Padding = new Padding(3);
            tabCollectionData.Size = new Size(1416, 323);
            tabCollectionData.TabIndex = 1;
            tabCollectionData.Text = "采集数据";
            tabCollectionData.UseVisualStyleBackColor = true;
            // 
            // collectionSplitter
            // 
            collectionSplitter.Dock = DockStyle.Fill;
            collectionSplitter.Location = new Point(3, 3);
            collectionSplitter.Name = "collectionSplitter";
            collectionSplitter.Orientation = Orientation.Horizontal;
            // 
            // collectionSplitter.Panel1
            // 
            collectionSplitter.Panel1.Controls.Add(collectionLayout);
            collectionSplitter.Panel1MinSize = 140;
            // 
            // collectionSplitter.Panel2
            // 
            collectionSplitter.Panel2.Controls.Add(rawDataLayout);
            collectionSplitter.Panel2MinSize = 80;
            collectionSplitter.Size = new Size(1410, 317);
            collectionSplitter.SplitterDistance = 205;
            collectionSplitter.SplitterWidth = 5;
            collectionSplitter.TabIndex = 0;
            // 
            // collectionLayout
            // 
            collectionLayout.ColumnCount = 1;
            collectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            collectionLayout.Controls.Add(lblCollectionSummary, 0, 0);
            collectionLayout.Controls.Add(dgvCollectionRecords, 0, 1);
            collectionLayout.Controls.Add(collectionPagination, 0, 2);
            collectionLayout.Dock = DockStyle.Fill;
            collectionLayout.Location = new Point(0, 0);
            collectionLayout.Name = "collectionLayout";
            collectionLayout.RowCount = 3;
            collectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            collectionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            collectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            collectionLayout.Size = new Size(1410, 205);
            collectionLayout.TabIndex = 0;
            // 
            // lblCollectionSummary
            // 
            lblCollectionSummary.Dock = DockStyle.Fill;
            lblCollectionSummary.ForeColor = SystemColors.GrayText;
            lblCollectionSummary.Location = new Point(3, 0);
            lblCollectionSummary.Name = "lblCollectionSummary";
            lblCollectionSummary.Size = new Size(1404, 30);
            lblCollectionSummary.TabIndex = 0;
            lblCollectionSummary.Text = "请选择历史工单";
            lblCollectionSummary.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // dgvCollectionRecords
            // 
            dgvCollectionRecords.AllowUserToAddRows = false;
            dgvCollectionRecords.AllowUserToDeleteRows = false;
            dgvCollectionRecords.AutoGenerateColumns = false;
            dgvCollectionRecords.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvCollectionRecords.DataSource = collectionBindingSource;
            dgvCollectionRecords.Dock = DockStyle.Fill;
            dgvCollectionRecords.Location = new Point(0, 30);
            dgvCollectionRecords.Margin = new Padding(0);
            dgvCollectionRecords.MultiSelect = false;
            dgvCollectionRecords.Name = "dgvCollectionRecords";
            dgvCollectionRecords.ReadOnly = true;
            dgvCollectionRecords.RowHeadersVisible = false;
            dgvCollectionRecords.RowHeadersWidth = 51;
            dgvCollectionRecords.RowTemplate.Height = 28;
            dgvCollectionRecords.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCollectionRecords.Size = new Size(1410, 131);
            dgvCollectionRecords.TabIndex = 1;
            // 
            // collectionPagination
            // 
            collectionPagination.Dock = DockStyle.Fill;
            collectionPagination.Location = new Point(3, 164);
            collectionPagination.Name = "collectionPagination";
            collectionPagination.PageSize = 50;
            collectionPagination.PageSizeOptions = new int[]
    {
    50,
    100,
    200
    };
            collectionPagination.RecordsPerPageText = "条/页";
            collectionPagination.ShowSizeChanger = true;
            collectionPagination.Size = new Size(1404, 38);
            collectionPagination.TabIndex = 2;
            // 
            // rawDataLayout
            // 
            rawDataLayout.ColumnCount = 1;
            rawDataLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rawDataLayout.Controls.Add(lblRawData, 0, 0);
            rawDataLayout.Controls.Add(txtRawData, 0, 1);
            rawDataLayout.Dock = DockStyle.Fill;
            rawDataLayout.Location = new Point(0, 0);
            rawDataLayout.Name = "rawDataLayout";
            rawDataLayout.RowCount = 2;
            rawDataLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            rawDataLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rawDataLayout.Size = new Size(1410, 107);
            rawDataLayout.TabIndex = 0;
            // 
            // lblRawData
            // 
            lblRawData.Dock = DockStyle.Fill;
            lblRawData.Location = new Point(3, 0);
            lblRawData.Name = "lblRawData";
            lblRawData.Size = new Size(1404, 28);
            lblRawData.TabIndex = 0;
            lblRawData.Text = "原始采集数据";
            lblRawData.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // txtRawData
            // 
            txtRawData.Dock = DockStyle.Fill;
            txtRawData.Font = new Font("Consolas", 9F);
            txtRawData.Location = new Point(0, 28);
            txtRawData.Margin = new Padding(0);
            txtRawData.Multiline = true;
            txtRawData.Name = "txtRawData";
            txtRawData.ReadOnly = true;
            txtRawData.ScrollBars = ScrollBars.Both;
            txtRawData.Size = new Size(1410, 79);
            txtRawData.TabIndex = 1;
            txtRawData.WordWrap = false;
            // 
            // tabReportFiles
            // 
            tabReportFiles.Controls.Add(reportLayout);
            tabReportFiles.Location = new Point(4, 32);
            tabReportFiles.Name = "tabReportFiles";
            tabReportFiles.Padding = new Padding(3);
            tabReportFiles.Size = new Size(1416, 320);
            tabReportFiles.TabIndex = 2;
            tabReportFiles.Text = "报告文件";
            tabReportFiles.UseVisualStyleBackColor = true;
            // 
            // reportLayout
            // 
            reportLayout.ColumnCount = 1;
            reportLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            reportLayout.Controls.Add(reportToolbar, 0, 0);
            reportLayout.Controls.Add(lblReportSummary, 0, 1);
            reportLayout.Controls.Add(dgvReportFiles, 0, 2);
            reportLayout.Dock = DockStyle.Fill;
            reportLayout.Location = new Point(3, 3);
            reportLayout.Name = "reportLayout";
            reportLayout.RowCount = 3;
            reportLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            reportLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            reportLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            reportLayout.Size = new Size(1410, 314);
            reportLayout.TabIndex = 0;
            // 
            // reportToolbar
            // 
            reportToolbar.Controls.Add(btnOpenReport);
            reportToolbar.Controls.Add(btnOpenReportFolder);
            reportToolbar.Dock = DockStyle.Fill;
            reportToolbar.Location = new Point(0, 0);
            reportToolbar.Margin = new Padding(0);
            reportToolbar.Name = "reportToolbar";
            reportToolbar.Padding = new Padding(0, 4, 0, 0);
            reportToolbar.Size = new Size(1410, 48);
            reportToolbar.TabIndex = 0;
            // 
            // btnOpenReport
            // 
            btnOpenReport.BorderWidth = 1F;
            btnOpenReport.IconSvg = "FileExcelOutlined";
            btnOpenReport.Location = new Point(0, 4);
            btnOpenReport.Margin = new Padding(0, 0, 10, 0);
            btnOpenReport.Name = "btnOpenReport";
            btnOpenReport.Size = new Size(118, 40);
            btnOpenReport.TabIndex = 0;
            btnOpenReport.Tag = "perm:button.data.open-report:enabled";
            btnOpenReport.Text = "打开报告";
            // 
            // btnOpenReportFolder
            // 
            btnOpenReportFolder.BorderWidth = 1F;
            btnOpenReportFolder.IconSvg = "FolderOpenOutlined";
            btnOpenReportFolder.Location = new Point(128, 4);
            btnOpenReportFolder.Margin = new Padding(0);
            btnOpenReportFolder.Name = "btnOpenReportFolder";
            btnOpenReportFolder.Size = new Size(136, 40);
            btnOpenReportFolder.TabIndex = 1;
            btnOpenReportFolder.Tag = "perm:button.data.open-report-folder:enabled";
            btnOpenReportFolder.Text = "打开所在目录";
            // 
            // lblReportSummary
            // 
            lblReportSummary.Dock = DockStyle.Fill;
            lblReportSummary.ForeColor = SystemColors.GrayText;
            lblReportSummary.Location = new Point(3, 48);
            lblReportSummary.Name = "lblReportSummary";
            lblReportSummary.Size = new Size(1404, 30);
            lblReportSummary.TabIndex = 1;
            lblReportSummary.Text = "请选择历史工单";
            lblReportSummary.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // dgvReportFiles
            // 
            dgvReportFiles.AllowUserToAddRows = false;
            dgvReportFiles.AllowUserToDeleteRows = false;
            dgvReportFiles.AutoGenerateColumns = false;
            dgvReportFiles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvReportFiles.DataSource = reportBindingSource;
            dgvReportFiles.Dock = DockStyle.Fill;
            dgvReportFiles.Location = new Point(0, 78);
            dgvReportFiles.Margin = new Padding(0);
            dgvReportFiles.MultiSelect = false;
            dgvReportFiles.Name = "dgvReportFiles";
            dgvReportFiles.ReadOnly = true;
            dgvReportFiles.RowHeadersVisible = false;
            dgvReportFiles.RowHeadersWidth = 51;
            dgvReportFiles.RowTemplate.Height = 28;
            dgvReportFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReportFiles.Size = new Size(1410, 236);
            dgvReportFiles.TabIndex = 2;
            // 
            // colTaskStation
            // 
            colTaskStation.MinimumWidth = 6;
            colTaskStation.Name = "colTaskStation";
            colTaskStation.Width = 125;
            // 
            // colTaskWorkOrder
            // 
            colTaskWorkOrder.MinimumWidth = 6;
            colTaskWorkOrder.Name = "colTaskWorkOrder";
            colTaskWorkOrder.Width = 125;
            // 
            // colTaskProductNum
            // 
            colTaskProductNum.MinimumWidth = 6;
            colTaskProductNum.Name = "colTaskProductNum";
            colTaskProductNum.Width = 125;
            // 
            // colTaskBatch
            // 
            colTaskBatch.MinimumWidth = 6;
            colTaskBatch.Name = "colTaskBatch";
            colTaskBatch.Width = 125;
            // 
            // colTaskProductName
            // 
            colTaskProductName.MinimumWidth = 6;
            colTaskProductName.Name = "colTaskProductName";
            colTaskProductName.Width = 125;
            // 
            // colTaskProcess
            // 
            colTaskProcess.MinimumWidth = 6;
            colTaskProcess.Name = "colTaskProcess";
            colTaskProcess.Width = 125;
            // 
            // colTaskRecipe
            // 
            colTaskRecipe.MinimumWidth = 6;
            colTaskRecipe.Name = "colTaskRecipe";
            colTaskRecipe.Width = 125;
            // 
            // colTaskPlannedQty
            // 
            colTaskPlannedQty.MinimumWidth = 6;
            colTaskPlannedQty.Name = "colTaskPlannedQty";
            colTaskPlannedQty.Width = 125;
            // 
            // colTaskActualQty
            // 
            colTaskActualQty.MinimumWidth = 6;
            colTaskActualQty.Name = "colTaskActualQty";
            colTaskActualQty.Width = 125;
            // 
            // colTaskQualifiedQty
            // 
            colTaskQualifiedQty.MinimumWidth = 6;
            colTaskQualifiedQty.Name = "colTaskQualifiedQty";
            colTaskQualifiedQty.Width = 125;
            // 
            // colTaskFailedQty
            // 
            colTaskFailedQty.MinimumWidth = 6;
            colTaskFailedQty.Name = "colTaskFailedQty";
            colTaskFailedQty.Width = 125;
            // 
            // colTaskOperator
            // 
            colTaskOperator.MinimumWidth = 6;
            colTaskOperator.Name = "colTaskOperator";
            colTaskOperator.Width = 125;
            // 
            // colTaskStartTime
            // 
            colTaskStartTime.MinimumWidth = 6;
            colTaskStartTime.Name = "colTaskStartTime";
            colTaskStartTime.Width = 125;
            // 
            // colTaskEndTime
            // 
            colTaskEndTime.MinimumWidth = 6;
            colTaskEndTime.Name = "colTaskEndTime";
            colTaskEndTime.Width = 125;
            // 
            // colTaskStatus
            // 
            colTaskStatus.MinimumWidth = 6;
            colTaskStatus.Name = "colTaskStatus";
            colTaskStatus.Width = 125;
            // 
            // colTaskUploadStatus
            // 
            colTaskUploadStatus.MinimumWidth = 6;
            colTaskUploadStatus.Name = "colTaskUploadStatus";
            colTaskUploadStatus.Width = 125;
            // 
            // colParameterStation
            // 
            colParameterStation.MinimumWidth = 6;
            colParameterStation.Name = "colParameterStation";
            colParameterStation.Width = 125;
            // 
            // colParameterProductNo
            // 
            colParameterProductNo.MinimumWidth = 6;
            colParameterProductNo.Name = "colParameterProductNo";
            colParameterProductNo.Width = 125;
            // 
            // colParameterTouchNo
            // 
            colParameterTouchNo.MinimumWidth = 6;
            colParameterTouchNo.Name = "colParameterTouchNo";
            colParameterTouchNo.Width = 125;
            // 
            // colParameterResult
            // 
            colParameterResult.MinimumWidth = 6;
            colParameterResult.Name = "colParameterResult";
            colParameterResult.Width = 125;
            // 
            // colParameterRecordTime
            // 
            colParameterRecordTime.MinimumWidth = 6;
            colParameterRecordTime.Name = "colParameterRecordTime";
            colParameterRecordTime.Width = 125;
            // 
            // colCollectionSequence
            // 
            colCollectionSequence.MinimumWidth = 6;
            colCollectionSequence.Name = "colCollectionSequence";
            colCollectionSequence.Width = 125;
            // 
            // colCollectionStation
            // 
            colCollectionStation.MinimumWidth = 6;
            colCollectionStation.Name = "colCollectionStation";
            colCollectionStation.Width = 125;
            // 
            // colCollectionProductNo
            // 
            colCollectionProductNo.MinimumWidth = 6;
            colCollectionProductNo.Name = "colCollectionProductNo";
            colCollectionProductNo.Width = 125;
            // 
            // colCollectionTouchNo
            // 
            colCollectionTouchNo.MinimumWidth = 6;
            colCollectionTouchNo.Name = "colCollectionTouchNo";
            colCollectionTouchNo.Width = 125;
            // 
            // colCollectionResult
            // 
            colCollectionResult.MinimumWidth = 6;
            colCollectionResult.Name = "colCollectionResult";
            colCollectionResult.Width = 125;
            // 
            // colCollectionIsTest
            // 
            colCollectionIsTest.MinimumWidth = 6;
            colCollectionIsTest.Name = "colCollectionIsTest";
            colCollectionIsTest.Width = 125;
            // 
            // colCollectionCompleted
            // 
            colCollectionCompleted.MinimumWidth = 6;
            colCollectionCompleted.Name = "colCollectionCompleted";
            colCollectionCompleted.Width = 125;
            // 
            // colCollectionUploadStatus
            // 
            colCollectionUploadStatus.MinimumWidth = 6;
            colCollectionUploadStatus.Name = "colCollectionUploadStatus";
            colCollectionUploadStatus.Width = 125;
            // 
            // colCollectionOperator
            // 
            colCollectionOperator.MinimumWidth = 6;
            colCollectionOperator.Name = "colCollectionOperator";
            colCollectionOperator.Width = 125;
            // 
            // colCollectionRecordTime
            // 
            colCollectionRecordTime.MinimumWidth = 6;
            colCollectionRecordTime.Name = "colCollectionRecordTime";
            colCollectionRecordTime.Width = 125;
            // 
            // colReportFileName
            // 
            colReportFileName.MinimumWidth = 6;
            colReportFileName.Name = "colReportFileName";
            colReportFileName.Width = 125;
            // 
            // colReportFormat
            // 
            colReportFormat.MinimumWidth = 6;
            colReportFormat.Name = "colReportFormat";
            colReportFormat.Width = 125;
            // 
            // colReportPath
            // 
            colReportPath.MinimumWidth = 6;
            colReportPath.Name = "colReportPath";
            colReportPath.Width = 125;
            // 
            // colReportUploadStatus
            // 
            colReportUploadStatus.MinimumWidth = 6;
            colReportUploadStatus.Name = "colReportUploadStatus";
            colReportUploadStatus.Width = 125;
            // 
            // colReportCreatedTime
            // 
            colReportCreatedTime.MinimumWidth = 6;
            colReportCreatedTime.Name = "colReportCreatedTime";
            colReportCreatedTime.Width = 125;
            // 
            // colReportUpdatedTime
            // 
            colReportUpdatedTime.MinimumWidth = 6;
            colReportUpdatedTime.Name = "colReportUpdatedTime";
            colReportUpdatedTime.Width = 125;
            // 
            // DataManageView
            // 
            AutoScaleDimensions = new SizeF(10F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(rootLayout);
            Font = new Font("Microsoft YaHei UI", 10.5F);
            Name = "DataManageView";
            Size = new Size(1456, 760);
            ((System.ComponentModel.ISupportInitialize)workOrderBindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)parameterBindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)collectionBindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)reportBindingSource).EndInit();
            rootLayout.ResumeLayout(false);
            filterPanel.ResumeLayout(false);
            filterLayout.ResumeLayout(false);
            mainSplitter.Panel1.ResumeLayout(false);
            mainSplitter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mainSplitter).EndInit();
            mainSplitter.ResumeLayout(false);
            workOrderLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvWorkOrders).EndInit();
            detailTabs.ResumeLayout(false);
            tabWeldParameters.ResumeLayout(false);
            parameterLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvWeldParameters).EndInit();
            tabCollectionData.ResumeLayout(false);
            collectionSplitter.Panel1.ResumeLayout(false);
            collectionSplitter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)collectionSplitter).EndInit();
            collectionSplitter.ResumeLayout(false);
            collectionLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvCollectionRecords).EndInit();
            rawDataLayout.ResumeLayout(false);
            rawDataLayout.PerformLayout();
            tabReportFiles.ResumeLayout(false);
            reportLayout.ResumeLayout(false);
            reportToolbar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvReportFiles).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private BindingSource workOrderBindingSource;
        private BindingSource parameterBindingSource;
        private BindingSource collectionBindingSource;
        private BindingSource reportBindingSource;
        private TableLayoutPanel rootLayout;
        private Panel filterPanel;
        private TableLayoutPanel filterLayout;
        private Label lblProductNum;
        private AntdUI.Input txtProductNum;
        private Label lblBatch;
        private AntdUI.Input txtBatch;
        private Label lblWorkOrder;
        private AntdUI.Input txtWorkOrder;
        private Label lblDateRange;
        private AntdUI.DatePickerRange dateRange;
        private AntdUI.Button btnQuery;
        private AntdUI.Button btnReset;
        private SplitContainer mainSplitter;
        private TableLayoutPanel workOrderLayout;
        private Label lblWorkOrderSummary;
        private DataGridView dgvWorkOrders;
        private AntdUI.Pagination workOrderPagination;
        private TabControl detailTabs;
        private TabPage tabWeldParameters;
        private TableLayoutPanel parameterLayout;
        private Label lblParameterSummary;
        private DataGridView dgvWeldParameters;
        private TabPage tabCollectionData;
        private SplitContainer collectionSplitter;
        private TableLayoutPanel collectionLayout;
        private Label lblCollectionSummary;
        private DataGridView dgvCollectionRecords;
        private AntdUI.Pagination collectionPagination;
        private TableLayoutPanel rawDataLayout;
        private Label lblRawData;
        private TextBox txtRawData;
        private TabPage tabReportFiles;
        private TableLayoutPanel reportLayout;
        private FlowLayoutPanel reportToolbar;
        private AntdUI.Button btnOpenReport;
        private AntdUI.Button btnOpenReportFolder;
        private Label lblReportSummary;
        private DataGridView dgvReportFiles;
        private DataGridViewTextBoxColumn colTaskStation;
        private DataGridViewTextBoxColumn colTaskWorkOrder;
        private DataGridViewTextBoxColumn colTaskProductNum;
        private DataGridViewTextBoxColumn colTaskBatch;
        private DataGridViewTextBoxColumn colTaskProductName;
        private DataGridViewTextBoxColumn colTaskProcess;
        private DataGridViewTextBoxColumn colTaskRecipe;
        private DataGridViewTextBoxColumn colTaskPlannedQty;
        private DataGridViewTextBoxColumn colTaskActualQty;
        private DataGridViewTextBoxColumn colTaskQualifiedQty;
        private DataGridViewTextBoxColumn colTaskFailedQty;
        private DataGridViewTextBoxColumn colTaskOperator;
        private DataGridViewTextBoxColumn colTaskStartTime;
        private DataGridViewTextBoxColumn colTaskEndTime;
        private DataGridViewTextBoxColumn colTaskStatus;
        private DataGridViewTextBoxColumn colTaskUploadStatus;
        private DataGridViewTextBoxColumn colParameterStation;
        private DataGridViewTextBoxColumn colParameterProductNo;
        private DataGridViewTextBoxColumn colParameterTouchNo;
        private DataGridViewTextBoxColumn colParameterResult;
        private DataGridViewTextBoxColumn colParameterRecordTime;
        private DataGridViewTextBoxColumn colCollectionSequence;
        private DataGridViewTextBoxColumn colCollectionStation;
        private DataGridViewTextBoxColumn colCollectionProductNo;
        private DataGridViewTextBoxColumn colCollectionTouchNo;
        private DataGridViewTextBoxColumn colCollectionResult;
        private DataGridViewCheckBoxColumn colCollectionIsTest;
        private DataGridViewCheckBoxColumn colCollectionCompleted;
        private DataGridViewTextBoxColumn colCollectionUploadStatus;
        private DataGridViewTextBoxColumn colCollectionOperator;
        private DataGridViewTextBoxColumn colCollectionRecordTime;
        private DataGridViewTextBoxColumn colReportFileName;
        private DataGridViewTextBoxColumn colReportFormat;
        private DataGridViewTextBoxColumn colReportPath;
        private DataGridViewTextBoxColumn colReportUploadStatus;
        private DataGridViewTextBoxColumn colReportCreatedTime;
        private DataGridViewTextBoxColumn colReportUpdatedTime;
    }
}
