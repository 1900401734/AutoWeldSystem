using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.UI.Forms
{
    partial class AddressPreviewForm
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
        /// Required method for Designer support.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            pageHeader = new AntdUI.PageHeader();
            mainLayout = new TableLayoutPanel();
            lblHint = new Label();
            inputQuery = new AutoWeldSystem.UI.Components.InputQuery(components);
            tableAddressPreview = new AntdUI.Table();
            bottomPanel = new FlowLayoutPanel();
            btnClose = new AntdUI.Button();
            btnTestSelected = new AntdUI.Button();
            mainLayout.SuspendLayout();
            bottomPanel.SuspendLayout();
            SuspendLayout();
            // 
            // pageHeader
            // 
            pageHeader.Dock = DockStyle.Top;
            pageHeader.Location = new Point(0, 0);
            pageHeader.MaximizeBox = false;
            pageHeader.MinimizeBox = false;
            pageHeader.Name = "pageHeader";
            pageHeader.ShowButton = true;
            pageHeader.Size = new Size(1180, 34);
            pageHeader.TabIndex = 0;
            pageHeader.Text = "PLC 地址预览";
            // 
            // mainLayout
            // 
            mainLayout.ColumnCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(lblHint, 0, 0);
            mainLayout.Controls.Add(inputQuery, 0, 1);
            mainLayout.Controls.Add(tableAddressPreview, 0, 2);
            mainLayout.Controls.Add(bottomPanel, 0, 3);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(0, 34);
            mainLayout.Margin = new Padding(0);
            mainLayout.Name = "mainLayout";
            mainLayout.RowCount = 4;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            mainLayout.Size = new Size(1180, 646);
            mainLayout.TabIndex = 1;
            // 
            // lblHint
            // 
            lblHint.Dock = DockStyle.Fill;
            lblHint.ForeColor = SystemColors.GrayText;
            lblHint.Location = new Point(12, 0);
            lblHint.Margin = new Padding(12, 0, 12, 0);
            lblHint.Name = "lblHint";
            lblHint.Size = new Size(1156, 38);
            lblHint.TabIndex = 0;
            lblHint.Text = "按产品工艺配置计算最终 PLC 地址。表达式：偏移:类型-规则_小数位；数值如 14:F-0_2，数值字符串如 0:S-8_3。";
            lblHint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // inputQuery
            // 
            inputQuery.AutoSize = true;
            inputQuery.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            inputQuery.Dock = DockStyle.Fill;
            inputQuery.Location = new Point(12, 38);
            inputQuery.Margin = new Padding(12, 0, 12, 6);
            inputQuery.MinimumSize = new Size(125, 40);
            inputQuery.Name = "inputQuery";
            inputQuery.QueryChanged = null;
            inputQuery.Size = new Size(1156, 40);
            inputQuery.TabIndex = 1;
            // 
            // tableAddressPreview
            // 
            tableAddressPreview.Dock = DockStyle.Fill;
            tableAddressPreview.Gap = 8;
            tableAddressPreview.Gaps = new Size(8, 8);
            tableAddressPreview.Location = new Point(12, 84);
            tableAddressPreview.Margin = new Padding(12, 0, 12, 0);
            tableAddressPreview.Name = "tableAddressPreview";
            tableAddressPreview.Size = new Size(1156, 510);
            tableAddressPreview.TabIndex = 2;
            tableAddressPreview.Text = "tableAddressPreview";
            // 
            // bottomPanel
            // 
            bottomPanel.Controls.Add(btnClose);
            bottomPanel.Controls.Add(btnTestSelected);
            bottomPanel.Dock = DockStyle.Fill;
            bottomPanel.FlowDirection = FlowDirection.RightToLeft;
            bottomPanel.Location = new Point(12, 600);
            bottomPanel.Margin = new Padding(12, 6, 12, 8);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Size = new Size(1156, 40);
            bottomPanel.TabIndex = 3;
            // 
            // btnClose
            // 
            btnClose.BorderWidth = 1F;
            btnClose.IconSvg = "CloseOutlined";
            btnClose.Location = new Point(1056, 0);
            btnClose.Margin = new Padding(8, 0, 0, 0);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(100, 36);
            btnClose.TabIndex = 0;
            // 
            // btnTestSelected
            // 
            btnTestSelected.BorderWidth = 1F;
            btnTestSelected.IconSvg = "ApiOutlined";
            btnTestSelected.Location = new Point(893, 0);
            btnTestSelected.Margin = new Padding(8, 0, 0, 0);
            btnTestSelected.Name = "btnTestSelected";
            btnTestSelected.Size = new Size(155, 36);
            btnTestSelected.TabIndex = 1;
            btnTestSelected.Text = "测试选中地址";
            btnClose.Text = "关闭";
            // 
            // AddressPreviewForm
            // 
            AutoScaleDimensions = new SizeF(10F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1180, 680);
            Controls.Add(mainLayout);
            Controls.Add(pageHeader);
            Font = new Font("Microsoft YaHei UI", 10.5F);
            MinimumSize = new Size(960, 560);
            Name = "AddressPreviewForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "PLC 地址预览";
            mainLayout.ResumeLayout(false);
            bottomPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        /// <summary>
        /// 地址预览列属于窗口界面结构，集中放在 Designer 文件中，避免与业务代码混在一起。
        /// </summary>
        private void ConfigureTable()
        {
            tableAddressPreview.Columns.Clear();
            tableAddressPreview.Columns.Add(new AntdUI.Column(nameof(PlcAddressPreviewRow.Station), "工位") { Ellipsis = true });
            tableAddressPreview.Columns.Add(new AntdUI.Column(nameof(PlcAddressPreviewRow.ProductNum), "产品工号") { Ellipsis = true });
            tableAddressPreview.Columns.Add(new AntdUI.Column(nameof(PlcAddressPreviewRow.ProductModel), "产品型号") { Ellipsis = true });
            tableAddressPreview.Columns.Add(new AntdUI.Column(nameof(PlcAddressPreviewRow.Category), "区域") { Ellipsis = true });
            tableAddressPreview.Columns.Add(new AntdUI.Column(nameof(PlcAddressPreviewRow.TouchNo), "焊点") { Ellipsis = true });
            tableAddressPreview.Columns.Add(new AntdUI.Column(nameof(PlcAddressPreviewRow.ValueRole), "字段") { Ellipsis = true });
            tableAddressPreview.Columns.Add(new AntdUI.Column(nameof(PlcAddressPreviewRow.BaseAddress), "基地址") { Ellipsis = true });
            tableAddressPreview.Columns.Add(new AntdUI.Column(nameof(PlcAddressPreviewRow.ContextOffset), "上下文偏移") { Ellipsis = true });
            tableAddressPreview.Columns.Add(new AntdUI.Column(nameof(PlcAddressPreviewRow.Expression), "偏移表达式") { Ellipsis = true });
            tableAddressPreview.Columns.Add(new AntdUI.Column(nameof(PlcAddressPreviewRow.DataType), "类型") { Ellipsis = true });
            tableAddressPreview.Columns.Add(new AntdUI.Column(nameof(PlcAddressPreviewRow.Rule), "规则") { Ellipsis = true });
            tableAddressPreview.Columns.Add(new AntdUI.Column(nameof(PlcAddressPreviewRow.DecimalPlaces), "小数位") { Ellipsis = true });
            tableAddressPreview.Columns.Add(new AntdUI.Column(nameof(PlcAddressPreviewRow.ResolvedAddress), "最终地址") { Ellipsis = true });
            AutoWeldSystem.UI.Infrastructure.TableStyleHelper.ApplyAntdTable(tableAddressPreview, AntdUI.ColumnsMode.Fill);
            AutoWeldSystem.UI.Infrastructure.TableStyleHelper.ApplyAntdColumnDefaults(tableAddressPreview);
        }

        #endregion

        private AntdUI.PageHeader pageHeader;
        private TableLayoutPanel mainLayout;
        private Label lblHint;
        private AutoWeldSystem.UI.Components.InputQuery inputQuery;
        private AntdUI.Table tableAddressPreview;
        private FlowLayoutPanel bottomPanel;
        private AntdUI.Button btnClose;
        private AntdUI.Button btnTestSelected;
    }
}
