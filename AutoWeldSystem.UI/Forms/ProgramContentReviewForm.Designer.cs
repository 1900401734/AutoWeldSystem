namespace AutoWeldSystem.UI.Forms;

partial class ProgramContentReviewForm
{
    private System.ComponentModel.IContainer components = null;
    private AntdUI.PageHeader pageHeader1;
    private Label lblTitle;
    private Label lblDescription;
    private DataGridView dgvFields;
    private FlowLayoutPanel buttonPanel;
    private Button btnApply;
    private Button btnCancel;
    private TableLayoutPanel tableLayoutPanel1;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        pageHeader1 = new AntdUI.PageHeader();
        lblTitle = new Label();
        lblDescription = new Label();
        dgvFields = new DataGridView();
        buttonPanel = new FlowLayoutPanel();
        btnCancel = new Button();
        btnApply = new Button();
        tableLayoutPanel1 = new TableLayoutPanel();
        ((System.ComponentModel.ISupportInitialize)dgvFields).BeginInit();
        buttonPanel.SuspendLayout();
        tableLayoutPanel1.SuspendLayout();
        SuspendLayout();
        //
        // pageHeader1
        //
        pageHeader1.Dock = DockStyle.Fill;
        pageHeader1.Location = new Point(0, 0);
        pageHeader1.Margin = new Padding(0);
        pageHeader1.MaximizeBox = false;
        pageHeader1.MinimizeBox = false;
        pageHeader1.Name = "pageHeader1";
        pageHeader1.ShowButton = true;
        pageHeader1.Size = new Size(892, 29);
        pageHeader1.TabIndex = 1;
        pageHeader1.Text = "程序内容确认";
        //
        // lblTitle
        //
        lblTitle.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold);
        lblTitle.Location = new Point(0, 29);
        lblTitle.Margin = new Padding(0);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(892, 36);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "程序内容确认";
        lblTitle.TextAlign = ContentAlignment.MiddleLeft;
        //
        // lblDescription
        //
        lblDescription.ForeColor = Color.DimGray;
        lblDescription.Location = new Point(0, 65);
        lblDescription.Margin = new Padding(0);
        lblDescription.Name = "lblDescription";
        lblDescription.Size = new Size(892, 36);
        lblDescription.TabIndex = 1;
        lblDescription.Text = "请在“修改值”列填写本次开工临时取值，留空将沿用设定值/标准值。修改只对本次开工生效、不落库。";
        lblDescription.TextAlign = ContentAlignment.MiddleLeft;
        //
        // dgvFields
        //
        dgvFields.AllowUserToAddRows = false;
        dgvFields.AllowUserToDeleteRows = false;
        dgvFields.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvFields.BackgroundColor = Color.White;
        dgvFields.ColumnHeadersHeight = 36;
        dgvFields.Dock = DockStyle.Fill;
        dgvFields.Location = new Point(0, 101);
        dgvFields.Margin = new Padding(0, 0, 0, 8);
        dgvFields.MultiSelect = false;
        dgvFields.Name = "dgvFields";
        dgvFields.RowHeadersVisible = false;
        dgvFields.RowHeadersWidth = 51;
        dgvFields.RowTemplate.Height = 32;
        dgvFields.SelectionMode = DataGridViewSelectionMode.CellSelect;
        dgvFields.Size = new Size(892, 360);
        dgvFields.TabIndex = 2;
        dgvFields.CellBeginEdit += dgvFields_CellBeginEdit;
        dgvFields.CellValueChanged += dgvFields_CellValueChanged;
        dgvFields.CurrentCellDirtyStateChanged += dgvFields_CurrentCellDirtyStateChanged;
        //
        // buttonPanel
        //
        buttonPanel.Controls.Add(btnCancel);
        buttonPanel.Controls.Add(btnApply);
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Location = new Point(0, 0);
        buttonPanel.Margin = new Padding(0);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Padding = new Padding(0, 10, 0, 0);
        buttonPanel.Size = new Size(892, 52);
        buttonPanel.TabIndex = 3;
        //
        // btnCancel
        //
        btnCancel.Location = new Point(790, 10);
        btnCancel.Margin = new Padding(10, 0, 0, 0);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(102, 34);
        btnCancel.TabIndex = 1;
        btnCancel.Text = "取消";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += btnCancel_Click;
        //
        // btnApply
        //
        btnApply.Location = new Point(676, 10);
        btnApply.Margin = new Padding(10, 0, 0, 0);
        btnApply.Name = "btnApply";
        btnApply.Size = new Size(104, 34);
        btnApply.TabIndex = 0;
        btnApply.Text = "应用本次";
        btnApply.UseVisualStyleBackColor = true;
        btnApply.Click += btnApply_Click;
        //
        // tableLayoutPanel1
        //
        tableLayoutPanel1.ColumnCount = 1;
        tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tableLayoutPanel1.Controls.Add(pageHeader1, 0, 0);
        tableLayoutPanel1.Controls.Add(lblTitle, 0, 1);
        tableLayoutPanel1.Controls.Add(lblDescription, 0, 2);
        tableLayoutPanel1.Controls.Add(dgvFields, 0, 3);
        tableLayoutPanel1.Controls.Add(buttonPanel, 0, 4);
        tableLayoutPanel1.Dock = DockStyle.Fill;
        tableLayoutPanel1.Location = new Point(0, 0);
        tableLayoutPanel1.Name = "tableLayoutPanel1";
        tableLayoutPanel1.RowCount = 5;
        tableLayoutPanel1.RowStyles.Add(new RowStyle());
        tableLayoutPanel1.RowStyles.Add(new RowStyle());
        tableLayoutPanel1.RowStyles.Add(new RowStyle());
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tableLayoutPanel1.RowStyles.Add(new RowStyle());
        tableLayoutPanel1.Size = new Size(892, 540);
        tableLayoutPanel1.TabIndex = 4;
        //
        // ProgramContentReviewForm
        //
        AcceptButton = btnApply;
        AutoScaleDimensions = new SizeF(10F, 23F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(924, 572);
        Controls.Add(tableLayoutPanel1);
        Font = new Font("Microsoft YaHei UI", 10.5F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ProgramContentReviewForm";
        Padding = new Padding(16);
        StartPosition = FormStartPosition.CenterParent;
        Text = "程序内容确认";
        ((System.ComponentModel.ISupportInitialize)dgvFields).EndInit();
        buttonPanel.ResumeLayout(false);
        tableLayoutPanel1.ResumeLayout(false);
        tableLayoutPanel1.PerformLayout();
        ResumeLayout(false);
    }
}