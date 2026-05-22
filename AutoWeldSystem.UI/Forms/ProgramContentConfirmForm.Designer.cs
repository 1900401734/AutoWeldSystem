namespace AutoWeldSystem.UI.Forms;

partial class ProgramContentConfirmForm
{
    private System.ComponentModel.IContainer components = null;
    private Label lblTitle;
    private Label lblDescription;
    private DataGridView dgvFields;
    private Label lblContent;
    private TextBox txtProgramContent;
    private FlowLayoutPanel buttonPanel;
    private Button btnApply;
    private Button btnCancel;

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
        lblContent = new Label();
        txtProgramContent = new TextBox();
        buttonPanel = new FlowLayoutPanel();
        btnCancel = new Button();
        btnApply = new Button();
        splitter1 = new AntdUI.Splitter();
        tableLayoutPanel1 = new TableLayoutPanel();
        tableLayoutPanel2 = new TableLayoutPanel();
        ((System.ComponentModel.ISupportInitialize)dgvFields).BeginInit();
        buttonPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitter1).BeginInit();
        splitter1.Panel1.SuspendLayout();
        splitter1.Panel2.SuspendLayout();
        splitter1.SuspendLayout();
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
        pageHeader1.Size = new Size(892, 29);
        pageHeader1.TabIndex = 1;
        pageHeader1.Text = "开工信息确认";
        // 
        // lblTitle
        // 
        lblTitle.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold);
        lblTitle.Location = new Point(0, 29);
        lblTitle.Margin = new Padding(0);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(892, 36);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "开工信息确认";
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
        lblDescription.Text = "请检查工单信息和程序内容。右侧调整值只用于本次开工上报和本次任务快照。";
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
        dgvFields.Size = new Size(892, 299);
        dgvFields.TabIndex = 2;
        dgvFields.CellBeginEdit += dgvFields_CellBeginEdit;
        dgvFields.CellClick += dgvFields_CellClick;
        dgvFields.CellValueChanged += dgvFields_CellValueChanged;
        dgvFields.CurrentCellDirtyStateChanged += dgvFields_CurrentCellDirtyStateChanged;
        // 
        // lblContent
        // 
        lblContent.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold);
        lblContent.Location = new Point(0, 0);
        lblContent.Margin = new Padding(0);
        lblContent.Name = "lblContent";
        lblContent.Size = new Size(892, 32);
        lblContent.TabIndex = 3;
        lblContent.Text = "程序内容";
        lblContent.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtProgramContent
        // 
        txtProgramContent.AcceptsReturn = true;
        txtProgramContent.AcceptsTab = true;
        txtProgramContent.Dock = DockStyle.Fill;
        txtProgramContent.Font = new Font("Consolas", 10F);
        txtProgramContent.Location = new Point(0, 32);
        txtProgramContent.Margin = new Padding(0);
        txtProgramContent.Multiline = true;
        txtProgramContent.Name = "txtProgramContent";
        txtProgramContent.ScrollBars = ScrollBars.Both;
        txtProgramContent.Size = new Size(892, 132);
        txtProgramContent.TabIndex = 4;
        txtProgramContent.WordWrap = false;
        // 
        // buttonPanel
        // 
        buttonPanel.Controls.Add(btnCancel);
        buttonPanel.Controls.Add(btnApply);
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Location = new Point(0, 164);
        buttonPanel.Margin = new Padding(0);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Padding = new Padding(0, 10, 0, 0);
        buttonPanel.Size = new Size(892, 52);
        buttonPanel.TabIndex = 5;
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
        // splitter1
        // 
        splitter1.Dock = DockStyle.Fill;
        splitter1.Location = new Point(16, 16);
        splitter1.Margin = new Padding(0);
        splitter1.Name = "splitter1";
        splitter1.Orientation = Orientation.Horizontal;
        // 
        // splitter1.Panel1
        // 
        splitter1.Panel1.Controls.Add(tableLayoutPanel1);
        // 
        // splitter1.Panel2
        // 
        splitter1.Panel2.Controls.Add(tableLayoutPanel2);
        splitter1.Size = new Size(892, 630);
        splitter1.SplitterDistance = 408;
        splitter1.SplitterWidth = 6;
        splitter1.TabIndex = 1;
        // 
        // tableLayoutPanel1
        // 
        tableLayoutPanel1.ColumnCount = 1;
        tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tableLayoutPanel1.Controls.Add(pageHeader1, 0, 0);
        tableLayoutPanel1.Controls.Add(dgvFields, 0, 3);
        tableLayoutPanel1.Controls.Add(lblTitle, 0, 1);
        tableLayoutPanel1.Controls.Add(lblDescription, 0, 2);
        tableLayoutPanel1.Dock = DockStyle.Fill;
        tableLayoutPanel1.Location = new Point(0, 0);
        tableLayoutPanel1.Name = "tableLayoutPanel1";
        tableLayoutPanel1.RowCount = 4;
        tableLayoutPanel1.RowStyles.Add(new RowStyle());
        tableLayoutPanel1.RowStyles.Add(new RowStyle());
        tableLayoutPanel1.RowStyles.Add(new RowStyle());
        tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tableLayoutPanel1.Size = new Size(892, 408);
        tableLayoutPanel1.TabIndex = 2;
        // 
        // tableLayoutPanel2
        // 
        tableLayoutPanel2.ColumnCount = 1;
        tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tableLayoutPanel2.Controls.Add(lblContent, 0, 0);
        tableLayoutPanel2.Controls.Add(txtProgramContent, 0, 1);
        tableLayoutPanel2.Controls.Add(buttonPanel, 0, 2);
        tableLayoutPanel2.Dock = DockStyle.Fill;
        tableLayoutPanel2.Location = new Point(0, 0);
        tableLayoutPanel2.Name = "tableLayoutPanel2";
        tableLayoutPanel2.RowCount = 3;
        tableLayoutPanel2.RowStyles.Add(new RowStyle());
        tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tableLayoutPanel2.RowStyles.Add(new RowStyle());
        tableLayoutPanel2.Size = new Size(892, 216);
        tableLayoutPanel2.TabIndex = 3;
        // 
        // ProgramContentConfirmForm
        // 
        AcceptButton = btnApply;
        AutoScaleDimensions = new SizeF(10F, 23F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(924, 662);
        Controls.Add(splitter1);
        Font = new Font("Microsoft YaHei UI", 10.5F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ProgramContentConfirmForm";
        Padding = new Padding(16);
        StartPosition = FormStartPosition.CenterParent;
        Text = "开工信息确认";
        ((System.ComponentModel.ISupportInitialize)dgvFields).EndInit();
        buttonPanel.ResumeLayout(false);
        splitter1.Panel1.ResumeLayout(false);
        splitter1.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitter1).EndInit();
        splitter1.ResumeLayout(false);
        tableLayoutPanel1.ResumeLayout(false);
        tableLayoutPanel2.ResumeLayout(false);
        tableLayoutPanel2.PerformLayout();
        ResumeLayout(false);
    }
    private AntdUI.PageHeader pageHeader1;
    private AntdUI.Splitter splitter1;
    private TableLayoutPanel tableLayoutPanel1;
    private TableLayoutPanel tableLayoutPanel2;
}
