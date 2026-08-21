namespace AutoWeldSystem.UI.Forms;

partial class ProgramContentReviewForm
{
    private System.ComponentModel.IContainer components = null;
    private AntdUI.PageHeader pageHeader1;
    private Label lblDescription;
    private DataGridView dgvFields;
    private AntdUI.Button btnCancel;
    private AntdUI.Button btnApply;
    private TableLayoutPanel tableLayoutPanel1;
    private TableLayoutPanel tableLayoutPanel2;

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
        lblDescription = new Label();
        dgvFields = new DataGridView();
        btnCancel = new AntdUI.Button();
        btnApply = new AntdUI.Button();
        tableLayoutPanel1 = new TableLayoutPanel();
        tableLayoutPanel2 = new TableLayoutPanel();
        ((System.ComponentModel.ISupportInitialize)dgvFields).BeginInit();
        tableLayoutPanel1.SuspendLayout();
        tableLayoutPanel2.SuspendLayout();
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
        pageHeader1.Size = new Size(746, 29);
        pageHeader1.TabIndex = 1;
        pageHeader1.Text = "程序内容确认";
        //
        // lblDescription
        //
        lblDescription.ForeColor = Color.DimGray;
        lblDescription.Location = new Point(0, 29);
        lblDescription.Margin = new Padding(0);
        lblDescription.Name = "lblDescription";
        lblDescription.Size = new Size(746, 36);
        lblDescription.TabIndex = 1;
        lblDescription.Text = "如需调整本次开工取值，请直接修改“最大允许值”列。修改只对本次开工生效、不落库。";
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
        dgvFields.Location = new Point(0, 65);
        dgvFields.Margin = new Padding(0, 0, 0, 8);
        dgvFields.MultiSelect = false;
        dgvFields.Name = "dgvFields";
        dgvFields.RowHeadersVisible = false;
        dgvFields.RowHeadersWidth = 51;
        dgvFields.RowTemplate.Height = 32;
        dgvFields.SelectionMode = DataGridViewSelectionMode.CellSelect;
        dgvFields.Size = new Size(746, 416);
        dgvFields.TabIndex = 2;
        dgvFields.CellBeginEdit += dgvFields_CellBeginEdit;
        dgvFields.CellValueChanged += dgvFields_CellValueChanged;
        dgvFields.CurrentCellDirtyStateChanged += dgvFields_CurrentCellDirtyStateChanged;
        //
        // btnCancel
        //
        btnCancel.AutoSizeMode = AntdUI.TAutoSize.Width;
        btnCancel.Dock = DockStyle.Fill;
        btnCancel.Location = new Point(103, 0);
        btnCancel.Margin = new Padding(0);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(68, 51);
        btnCancel.TabIndex = 2;
        btnCancel.Text = "取消";
        btnCancel.Click += btnCancel_Click;
        //
        // btnApply
        //
        btnApply.AutoSizeMode = AntdUI.TAutoSize.Width;
        btnApply.Dock = DockStyle.Right;
        btnApply.Location = new Point(0, 0);
        btnApply.Margin = new Padding(0);
        btnApply.Name = "btnApply";
        btnApply.Size = new Size(103, 51);
        btnApply.TabIndex = 2;
        btnApply.Text = "应用本次";
        btnApply.Click += btnApply_Click;
        //
        // tableLayoutPanel1
        //
        tableLayoutPanel1.ColumnCount = 1;
        tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tableLayoutPanel1.Controls.Add(pageHeader1, 0, 0);
        tableLayoutPanel1.Controls.Add(lblDescription, 0, 1);
        tableLayoutPanel1.Controls.Add(dgvFields, 0, 2);
        tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 3);
        tableLayoutPanel1.Dock = DockStyle.Fill;
        tableLayoutPanel1.Location = new Point(16, 16);
        tableLayoutPanel1.Name = "tableLayoutPanel1";
        tableLayoutPanel1.RowCount = 4;
        tableLayoutPanel1.RowStyles.Add(new RowStyle());
        tableLayoutPanel1.RowStyles.Add(new RowStyle());
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tableLayoutPanel1.RowStyles.Add(new RowStyle());
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        tableLayoutPanel1.Size = new Size(746, 540);
        tableLayoutPanel1.TabIndex = 4;
        //
        // tableLayoutPanel2
        //
        tableLayoutPanel2.AutoSize = true;
        tableLayoutPanel2.ColumnCount = 2;
        tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle());
        tableLayoutPanel2.Controls.Add(btnCancel, 1, 0);
        tableLayoutPanel2.Controls.Add(btnApply, 0, 0);
        tableLayoutPanel2.Dock = DockStyle.Right;
        tableLayoutPanel2.Location = new Point(575, 489);
        tableLayoutPanel2.Margin = new Padding(0);
        tableLayoutPanel2.Name = "tableLayoutPanel2";
        tableLayoutPanel2.RowCount = 1;
        tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tableLayoutPanel2.Size = new Size(171, 51);
        tableLayoutPanel2.TabIndex = 5;
        //
        // ProgramContentReviewForm
        //
        AutoScaleDimensions = new SizeF(10F, 23F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(778, 572);
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
        tableLayoutPanel1.ResumeLayout(false);
        tableLayoutPanel1.PerformLayout();
        tableLayoutPanel2.ResumeLayout(false);
        tableLayoutPanel2.PerformLayout();
        ResumeLayout(false);
    }
}