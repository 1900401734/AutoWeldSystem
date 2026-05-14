namespace AutoWeldSystem.UI.Forms;

partial class OperatorInputForm
{
    private System.ComponentModel.IContainer components = null;
    private Label lblEmployeeNumber;
    private TextBox txtEmployeeNumber;
    private Button btnOk;
    private Button btnCancel;

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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OperatorInputForm));
        lblEmployeeNumber = new Label();
        txtEmployeeNumber = new TextBox();
        btnOk = new Button();
        btnCancel = new Button();
        pageHeader1 = new AntdUI.PageHeader();
        SuspendLayout();
        // 
        // lblEmployeeNumber
        // 
        resources.ApplyResources(lblEmployeeNumber, "lblEmployeeNumber");
        lblEmployeeNumber.Name = "lblEmployeeNumber";
        // 
        // txtEmployeeNumber
        // 
        resources.ApplyResources(txtEmployeeNumber, "txtEmployeeNumber");
        txtEmployeeNumber.Name = "txtEmployeeNumber";
        // 
        // btnOk
        // 
        resources.ApplyResources(btnOk, "btnOk");
        btnOk.Name = "btnOk";
        btnOk.UseVisualStyleBackColor = true;
        btnOk.Click += btnOk_Click;
        // 
        // btnCancel
        // 
        resources.ApplyResources(btnCancel, "btnCancel");
        btnCancel.Name = "btnCancel";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += btnCancel_Click;
        // 
        // pageHeader1
        // 
        resources.ApplyResources(pageHeader1, "pageHeader1");
        pageHeader1.Name = "pageHeader1";
        pageHeader1.ShowButton = true;
        // 
        // OperatorInputForm
        // 
        resources.ApplyResources(this, "$this");
        AutoScaleMode = AutoScaleMode.Dpi;
        Controls.Add(pageHeader1);
        Controls.Add(btnCancel);
        Controls.Add(btnOk);
        Controls.Add(txtEmployeeNumber);
        Controls.Add(lblEmployeeNumber);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "OperatorInputForm";
        ResumeLayout(false);
        PerformLayout();
    }
    private AntdUI.PageHeader pageHeader1;
}
