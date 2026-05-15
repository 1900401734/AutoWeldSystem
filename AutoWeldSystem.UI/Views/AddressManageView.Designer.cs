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
            queryAddresses = new AutoWeldSystem.UI.Components.InputQuery(components);
            buttonFlow = new FlowLayoutPanel();
            btnSave = new AntdUI.Button();
            btnRefresh = new AntdUI.Button();
            btnTest = new AntdUI.Button();
            tabAddressCategories = new TabControl();
            tabBusinessAddresses = new TabPage();
            tableAddresses = new AntdUI.Table();
            tabCollectionParameters = new TabPage();
            tableCollectionParameters = new AntdUI.Table();
            rootLayout.SuspendLayout();
            headerLayout.SuspendLayout();
            titleLayout.SuspendLayout();
            buttonFlow.SuspendLayout();
            tabAddressCategories.SuspendLayout();
            tabBusinessAddresses.SuspendLayout();
            tabCollectionParameters.SuspendLayout();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(headerLayout, 0, 0);
            rootLayout.Controls.Add(tabAddressCategories, 0, 1);
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.RowCount = 2;
            rootLayout.RowStyles.Add(new RowStyle());
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.Size = new Size(1298, 721);
            rootLayout.TabIndex = 0;
            // 
            // headerLayout
            // 
            headerLayout.ColumnCount = 3;
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.ColumnStyles.Add(new ColumnStyle());
            headerLayout.ColumnStyles.Add(new ColumnStyle());
            headerLayout.Controls.Add(titleLayout, 0, 0);
            headerLayout.Controls.Add(queryAddresses, 1, 0);
            headerLayout.Controls.Add(buttonFlow, 2, 0);
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
            titleLayout.Size = new Size(601, 70);
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
            lblTitle.Size = new Size(601, 34);
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
            lblDescription.Size = new Size(601, 36);
            lblDescription.TabIndex = 1;
            lblDescription.Text = "维护固定业务信号对应的 PLC 实际地址。";
            lblDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // queryAddresses
            // 
            queryAddresses.AutoSize = true;
            queryAddresses.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            queryAddresses.Location = new Point(601, 0);
            queryAddresses.Margin = new Padding(0);
            queryAddresses.MinimumSize = new Size(125, 46);
            queryAddresses.Name = "queryAddresses";
            queryAddresses.QueryChanged = null;
            queryAddresses.Size = new Size(307, 46);
            queryAddresses.TabIndex = 1;
            // 
            // buttonFlow
            // 
            buttonFlow.AutoSize = true;
            buttonFlow.Controls.Add(btnSave);
            buttonFlow.Controls.Add(btnRefresh);
            buttonFlow.Controls.Add(btnTest);
            buttonFlow.Dock = DockStyle.Right;
            buttonFlow.Location = new Point(908, 0);
            buttonFlow.Margin = new Padding(0);
            buttonFlow.Name = "buttonFlow";
            buttonFlow.Size = new Size(342, 70);
            buttonFlow.TabIndex = 1;
            buttonFlow.WrapContents = false;
            // 
            // btnSave
            // 
            btnSave.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnSave.BorderWidth = 1F;
            btnSave.IconSvg = "SaveOutlined";
            btnSave.JoinMode = AntdUI.TJoinMode.Left;
            btnSave.Location = new Point(0, 0);
            btnSave.Margin = new Padding(0, 0, 10, 0);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(107, 45);
            btnSave.TabIndex = 0;
            btnSave.Text = "保存地址";
            // 
            // btnRefresh
            // 
            btnRefresh.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnRefresh.BorderWidth = 1F;
            btnRefresh.IconSvg = "ReloadOutlined";
            btnRefresh.JoinMode = AntdUI.TJoinMode.LR;
            btnRefresh.Location = new Point(117, 0);
            btnRefresh.Margin = new Padding(0, 0, 10, 0);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(76, 44);
            btnRefresh.TabIndex = 1;
            btnRefresh.Text = "刷新";
            // 
            // btnTest
            // 
            btnTest.AutoSizeMode = AntdUI.TAutoSize.Width;
            btnTest.BorderWidth = 1F;
            btnTest.Dock = DockStyle.Right;
            btnTest.IconSvg = "ApiOutlined";
            btnTest.JoinMode = AntdUI.TJoinMode.Right;
            btnTest.Location = new Point(203, 0);
            btnTest.Margin = new Padding(0);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(139, 45);
            btnTest.TabIndex = 2;
            btnTest.Text = "测试选中地址";
            //
            // tabAddressCategories
            //
            tabAddressCategories.Controls.Add(tabBusinessAddresses);
            tabAddressCategories.Controls.Add(tabCollectionParameters);
            tabAddressCategories.Dock = DockStyle.Fill;
            tabAddressCategories.HotTrack = true;
            tabAddressCategories.Location = new Point(24, 104);
            tabAddressCategories.Margin = new Padding(24, 6, 24, 24);
            tabAddressCategories.Name = "tabAddressCategories";
            tabAddressCategories.SelectedIndex = 0;
            tabAddressCategories.Size = new Size(1250, 593);
            tabAddressCategories.TabIndex = 1;
            //
            // tabBusinessAddresses
            //
            tabBusinessAddresses.Controls.Add(tableAddresses);
            tabBusinessAddresses.Location = new Point(4, 29);
            tabBusinessAddresses.Name = "tabBusinessAddresses";
            tabBusinessAddresses.Padding = new Padding(3);
            tabBusinessAddresses.Size = new Size(1242, 560);
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
            tableAddresses.Size = new Size(1236, 554);
            tableAddresses.TabIndex = 0;
            tableAddresses.Text = "tableAddresses";
            //
            // tabCollectionParameters
            //
            tabCollectionParameters.Controls.Add(tableCollectionParameters);
            tabCollectionParameters.Location = new Point(4, 29);
            tabCollectionParameters.Name = "tabCollectionParameters";
            tabCollectionParameters.Padding = new Padding(3);
            tabCollectionParameters.Size = new Size(1242, 560);
            tabCollectionParameters.TabIndex = 1;
            tabCollectionParameters.Text = "采集参数地址";
            tabCollectionParameters.UseVisualStyleBackColor = true;
            //
            // tableCollectionParameters
            //
            tableCollectionParameters.Dock = DockStyle.Fill;
            tableCollectionParameters.EditMode = AntdUI.TEditMode.DoubleClick;
            tableCollectionParameters.Gap = 8;
            tableCollectionParameters.Location = new Point(3, 3);
            tableCollectionParameters.Margin = new Padding(0);
            tableCollectionParameters.Name = "tableCollectionParameters";
            tableCollectionParameters.Size = new Size(1236, 554);
            tableCollectionParameters.TabIndex = 0;
            tableCollectionParameters.Text = "tableCollectionParameters";
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
            buttonFlow.ResumeLayout(false);
            buttonFlow.PerformLayout();
            tabCollectionParameters.ResumeLayout(false);
            tabBusinessAddresses.ResumeLayout(false);
            tabAddressCategories.ResumeLayout(false);
            ResumeLayout(false);
        }

        private TableLayoutPanel rootLayout;
        private TableLayoutPanel headerLayout;
        private TableLayoutPanel titleLayout;
        private Label lblTitle;
        private Label lblDescription;
        private FlowLayoutPanel buttonFlow;
        private AntdUI.Button btnSave;
        private AntdUI.Button btnRefresh;
        private AntdUI.Button btnTest;
        private Components.InputQuery queryAddresses;
        private TabControl tabAddressCategories;
        private TabPage tabBusinessAddresses;
        private AntdUI.Table tableAddresses;
        private TabPage tabCollectionParameters;
        private AntdUI.Table tableCollectionParameters;
    }
}
