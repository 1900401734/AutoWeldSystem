namespace AutoWeldSystem.UI.Forms;

partial class LocalWorkOrderForm
{
    private System.ComponentModel.IContainer components = null;
    private TableLayoutPanel layoutRoot;
    private Label lblWorkOrderId;
    private Label lblProgram;
    private Label lblBatch;
    private Label lblSpec;
    private Label lblProcessNo;
    private Label lblProcessName;
    private Label lblPlannedQty;
    private Label lblProductNum;
    private Label lblProductModel;
    private Label lblProgramName;
    private Label lblRecipeCode;
    private TextBox txtWorkOrderId;
    private ComboBox cmbProgram;
    private TextBox txtBatch;
    private TextBox txtSpec;
    private TextBox txtProcessNo;
    private TextBox txtProcessName;
    private NumericUpDown numPlannedQty;
    private TextBox txtProductNum;
    private TextBox txtProductModel;
    private TextBox txtProgramName;
    private TextBox txtRecipeCode;
    private FlowLayoutPanel buttonPanel;
    private Button btnOk;
    private Button btnCancel;

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
        layoutRoot = new TableLayoutPanel();
        lblWorkOrderId = new Label();
        lblProgram = new Label();
        lblBatch = new Label();
        lblSpec = new Label();
        lblProcessNo = new Label();
        lblProcessName = new Label();
        lblPlannedQty = new Label();
        lblProductNum = new Label();
        lblProductModel = new Label();
        lblProgramName = new Label();
        lblRecipeCode = new Label();
        txtWorkOrderId = new TextBox();
        cmbProgram = new ComboBox();
        txtBatch = new TextBox();
        txtSpec = new TextBox();
        txtProcessNo = new TextBox();
        txtProcessName = new TextBox();
        numPlannedQty = new NumericUpDown();
        txtProductNum = new TextBox();
        txtProductModel = new TextBox();
        txtProgramName = new TextBox();
        txtRecipeCode = new TextBox();
        buttonPanel = new FlowLayoutPanel();
        btnOk = new Button();
        btnCancel = new Button();
        layoutRoot.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numPlannedQty).BeginInit();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        // 
        // layoutRoot
        // 
        layoutRoot.ColumnCount = 2;
        layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutRoot.Controls.Add(lblWorkOrderId, 0, 0);
        layoutRoot.Controls.Add(txtWorkOrderId, 1, 0);
        layoutRoot.Controls.Add(lblProgram, 0, 1);
        layoutRoot.Controls.Add(cmbProgram, 1, 1);
        layoutRoot.Controls.Add(lblBatch, 0, 2);
        layoutRoot.Controls.Add(txtBatch, 1, 2);
        layoutRoot.Controls.Add(lblSpec, 0, 3);
        layoutRoot.Controls.Add(txtSpec, 1, 3);
        layoutRoot.Controls.Add(lblProcessNo, 0, 4);
        layoutRoot.Controls.Add(txtProcessNo, 1, 4);
        layoutRoot.Controls.Add(lblProcessName, 0, 5);
        layoutRoot.Controls.Add(txtProcessName, 1, 5);
        layoutRoot.Controls.Add(lblPlannedQty, 0, 6);
        layoutRoot.Controls.Add(numPlannedQty, 1, 6);
        layoutRoot.Controls.Add(lblProductNum, 0, 7);
        layoutRoot.Controls.Add(txtProductNum, 1, 7);
        layoutRoot.Controls.Add(lblProductModel, 0, 8);
        layoutRoot.Controls.Add(txtProductModel, 1, 8);
        layoutRoot.Controls.Add(lblProgramName, 0, 9);
        layoutRoot.Controls.Add(txtProgramName, 1, 9);
        layoutRoot.Controls.Add(lblRecipeCode, 0, 10);
        layoutRoot.Controls.Add(txtRecipeCode, 1, 10);
        layoutRoot.Controls.Add(buttonPanel, 1, 11);
        layoutRoot.Dock = DockStyle.Fill;
        layoutRoot.Location = new Point(0, 0);
        layoutRoot.Name = "layoutRoot";
        layoutRoot.Padding = new Padding(16);
        layoutRoot.RowCount = 12;
        for (var i = 0; i < 11; i++)
        {
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        }
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        layoutRoot.Size = new Size(560, 460);
        layoutRoot.TabIndex = 0;
        // 
        // labels
        // 
        lblWorkOrderId.Text = "工单号*";
        lblProgram.Text = "产品工号/配方*";
        lblBatch.Text = "批次";
        lblSpec.Text = "规格";
        lblProcessNo.Text = "工序号";
        lblProcessName.Text = "工序名称";
        lblPlannedQty.Text = "计划数量";
        lblProductNum.Text = "产品工号";
        lblProductModel.Text = "产品型号";
        lblProgramName.Text = "程序名称";
        lblRecipeCode.Text = "配方编号";
        foreach (var label in new[] { lblWorkOrderId, lblProgram, lblBatch, lblSpec, lblProcessNo, lblProcessName, lblPlannedQty, lblProductNum, lblProductModel, lblProgramName, lblRecipeCode })
        {
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
        }
        // 
        // inputs
        // 
        foreach (var input in new[] { txtWorkOrderId, txtBatch, txtSpec, txtProcessNo, txtProcessName, txtProductNum, txtProductModel, txtProgramName, txtRecipeCode })
        {
            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(0, 4, 0, 4);
        }
        cmbProgram.Dock = DockStyle.Fill;
        cmbProgram.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbProgram.Margin = new Padding(0, 4, 0, 4);
        numPlannedQty.Dock = DockStyle.Left;
        numPlannedQty.Margin = new Padding(0, 4, 0, 4);
        numPlannedQty.Maximum = 1000000;
        numPlannedQty.Minimum = 1;
        numPlannedQty.Size = new Size(160, 27);
        txtProductNum.ReadOnly = true;
        txtProductModel.ReadOnly = true;
        txtProgramName.ReadOnly = true;
        txtRecipeCode.ReadOnly = true;
        // 
        // buttonPanel
        // 
        buttonPanel.Controls.Add(btnOk);
        buttonPanel.Controls.Add(btnCancel);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Location = new Point(136, 412);
        buttonPanel.Margin = new Padding(0);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Size = new Size(408, 42);
        buttonPanel.TabIndex = 22;
        // 
        // btnOk
        // 
        btnOk.Location = new Point(315, 8);
        btnOk.Margin = new Padding(8);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(85, 30);
        btnOk.TabIndex = 0;
        btnOk.Text = "本地开工";
        btnOk.UseVisualStyleBackColor = true;
        btnOk.Click += btnOk_Click;
        // 
        // btnCancel
        // 
        btnCancel.Location = new Point(214, 8);
        btnCancel.Margin = new Padding(8);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(85, 30);
        btnCancel.TabIndex = 1;
        btnCancel.Text = "取消";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += btnCancel_Click;
        // 
        // LocalWorkOrderForm
        // 
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(560, 460);
        Controls.Add(layoutRoot);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "LocalWorkOrderForm";
        StartPosition = FormStartPosition.CenterParent;
        layoutRoot.ResumeLayout(false);
        layoutRoot.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numPlannedQty).EndInit();
        buttonPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}
