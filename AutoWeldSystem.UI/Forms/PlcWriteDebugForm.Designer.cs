namespace AutoWeldSystem.UI.Forms;

partial class PlcWriteDebugForm
{
    private System.ComponentModel.IContainer components = null;
    private AntdUI.PageHeader pageHeader;
    private TableLayoutPanel mainLayout;
    private Label lblHint;
    private Label lblAddress;
    private Label lblDataType;
    private AntdUI.Select selectDataType;
    private Label lblValue;
    private Label lblResult;
    private AntdUI.Input inputValue;
    private AntdUI.Input inputAddress;
    private AntdUI.Button btnWrite;
    private AntdUI.Button btnClose;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        pageHeader = new AntdUI.PageHeader();
        mainLayout = new TableLayoutPanel();
        inputValue = new AntdUI.Input();
        selectDataType = new AntdUI.Select();
        inputAddress = new AntdUI.Input();
        lblHint = new Label();
        lblAddress = new Label();
        lblDataType = new Label();
        lblValue = new Label();
        lblResult = new Label();
        tableLayoutPanel1 = new TableLayoutPanel();
        btnClose = new AntdUI.Button();
        btnWrite = new AntdUI.Button();
        mainLayout.SuspendLayout();
        tableLayoutPanel1.SuspendLayout();
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
        pageHeader.Size = new Size(640, 34);
        pageHeader.TabIndex = 0;
        pageHeader.Text = "PLC 地址写入调试";
        // 
        // mainLayout
        // 
        mainLayout.ColumnCount = 2;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8.059211F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 91.94079F));
        mainLayout.Controls.Add(inputValue, 1, 3);
        mainLayout.Controls.Add(selectDataType, 1, 2);
        mainLayout.Controls.Add(inputAddress, 1, 1);
        mainLayout.Controls.Add(lblHint, 0, 0);
        mainLayout.Controls.Add(lblAddress, 0, 1);
        mainLayout.Controls.Add(lblDataType, 0, 2);
        mainLayout.Controls.Add(lblValue, 0, 3);
        mainLayout.Controls.Add(lblResult, 0, 4);
        mainLayout.Controls.Add(tableLayoutPanel1, 1, 5);
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Location = new Point(0, 34);
        mainLayout.Name = "mainLayout";
        mainLayout.Padding = new Padding(16, 12, 16, 12);
        mainLayout.RowCount = 6;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666679F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666679F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666679F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666679F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
        mainLayout.Size = new Size(640, 306);
        mainLayout.TabIndex = 1;
        // 
        // inputValue
        // 
        inputValue.Dock = DockStyle.Fill;
        inputValue.Location = new Point(65, 152);
        inputValue.Margin = new Padding(0);
        inputValue.Name = "inputValue";
        inputValue.Size = new Size(559, 47);
        inputValue.TabIndex = 5;
        // 
        // selectDataType
        // 
        selectDataType.Dock = DockStyle.Fill;
        selectDataType.Location = new Point(65, 105);
        selectDataType.Margin = new Padding(0);
        selectDataType.Name = "selectDataType";
        selectDataType.Size = new Size(559, 47);
        selectDataType.TabIndex = 0;
        selectDataType.Text = "select1";
        // 
        // inputAddress
        // 
        inputAddress.Dock = DockStyle.Fill;
        inputAddress.Location = new Point(65, 58);
        inputAddress.Margin = new Padding(0);
        inputAddress.Name = "inputAddress";
        inputAddress.Size = new Size(559, 47);
        inputAddress.TabIndex = 3;
        // 
        // lblHint
        // 
        mainLayout.SetColumnSpan(lblHint, 2);
        lblHint.Dock = DockStyle.Fill;
        lblHint.ForeColor = SystemColors.GrayText;
        lblHint.Location = new Point(16, 12);
        lblHint.Margin = new Padding(0, 0, 0, 6);
        lblHint.Name = "lblHint";
        lblHint.Size = new Size(608, 40);
        lblHint.TabIndex = 0;
        lblHint.Text = "用于现场调试时直接向 PLC 地址写入值。写入前请确认地址、类型和值。";
        lblHint.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblAddress
        // 
        lblAddress.Dock = DockStyle.Fill;
        lblAddress.Location = new Point(16, 58);
        lblAddress.Margin = new Padding(0);
        lblAddress.Name = "lblAddress";
        lblAddress.Size = new Size(49, 47);
        lblAddress.TabIndex = 1;
        lblAddress.Text = "地址";
        lblAddress.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblDataType
        // 
        lblDataType.Dock = DockStyle.Fill;
        lblDataType.Location = new Point(16, 105);
        lblDataType.Margin = new Padding(0);
        lblDataType.Name = "lblDataType";
        lblDataType.Size = new Size(49, 47);
        lblDataType.TabIndex = 3;
        lblDataType.Text = "类型";
        lblDataType.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblValue
        // 
        lblValue.Dock = DockStyle.Fill;
        lblValue.Location = new Point(16, 152);
        lblValue.Margin = new Padding(0);
        lblValue.Name = "lblValue";
        lblValue.Size = new Size(49, 47);
        lblValue.TabIndex = 5;
        lblValue.Text = "内容";
        lblValue.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblResult
        // 
        lblResult.BackColor = SystemColors.ControlLightLight;
        lblResult.BorderStyle = BorderStyle.FixedSingle;
        mainLayout.SetColumnSpan(lblResult, 2);
        lblResult.Dock = DockStyle.Fill;
        lblResult.ForeColor = SystemColors.GrayText;
        lblResult.Location = new Point(21, 204);
        lblResult.Margin = new Padding(5);
        lblResult.Name = "lblResult";
        lblResult.Padding = new Padding(8, 0, 8, 0);
        lblResult.Size = new Size(598, 37);
        lblResult.TabIndex = 7;
        lblResult.Text = "等待写入。";
        lblResult.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // tableLayoutPanel1
        // 
        tableLayoutPanel1.ColumnCount = 2;
        tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tableLayoutPanel1.Controls.Add(btnClose, 1, 0);
        tableLayoutPanel1.Controls.Add(btnWrite, 0, 0);
        tableLayoutPanel1.Dock = DockStyle.Right;
        tableLayoutPanel1.Location = new Point(398, 246);
        tableLayoutPanel1.Margin = new Padding(0);
        tableLayoutPanel1.Name = "tableLayoutPanel1";
        tableLayoutPanel1.RowCount = 1;
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tableLayoutPanel1.Size = new Size(226, 48);
        tableLayoutPanel1.TabIndex = 6;
        // 
        // btnClose
        // 
        btnClose.AutoSizeMode = AntdUI.TAutoSize.Width;
        btnClose.DialogResult = DialogResult.Cancel;
        btnClose.Dock = DockStyle.Fill;
        btnClose.Location = new Point(113, 0);
        btnClose.Margin = new Padding(0);
        btnClose.MinimumSize = new Size(100, 0);
        btnClose.Name = "btnClose";
        btnClose.Size = new Size(100, 48);
        btnClose.TabIndex = 5;
        btnClose.Text = "关闭";
        // 
        // btnWrite
        // 
        btnWrite.AutoSizeMode = AntdUI.TAutoSize.Width;
        btnWrite.Dock = DockStyle.Fill;
        btnWrite.Location = new Point(0, 0);
        btnWrite.Margin = new Padding(0);
        btnWrite.MinimumSize = new Size(100, 0);
        btnWrite.Name = "btnWrite";
        btnWrite.Size = new Size(100, 48);
        btnWrite.TabIndex = 5;
        btnWrite.Text = "写入";
        // 
        // PlcWriteDebugForm
        // 
        AutoScaleDimensions = new SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(640, 340);
        Controls.Add(mainLayout);
        Controls.Add(pageHeader);
        Font = new Font("Microsoft YaHei UI", 10.5F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(640, 340);
        Name = "PlcWriteDebugForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "PLC 地址写入调试";
        mainLayout.ResumeLayout(false);
        tableLayoutPanel1.ResumeLayout(false);
        tableLayoutPanel1.PerformLayout();
        ResumeLayout(false);
    }
    private TableLayoutPanel tableLayoutPanel1;
}
