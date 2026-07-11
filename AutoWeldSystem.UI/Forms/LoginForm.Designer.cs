namespace AutoWeldSystem.UI.Forms;

partial class LoginForm
{
    private System.ComponentModel.IContainer components = null;
    private AntdUI.PageHeader pageHeader1;
    private AntdUI.Panel panel1;
    private TableLayoutPanel tableLayoutPanel1;
    private Label lblTitle;
    private Label lblAccount;
    private Label lblPassword;
    private Label lblLanguage;
    private Label lblTip;
    private AntdUI.Input inputUserNumber;
    private AntdUI.Input inputPassword;
    private AntdUI.Select selectLang;
    private AntdUI.ButtonShadow btnCancel;
    private AntdUI.ButtonShadow btnLogin;

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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
        lblAccount = new Label();
        lblPassword = new Label();
        lblTip = new Label();
        lblLanguage = new Label();
        inputUserNumber = new AntdUI.Input();
        inputPassword = new AntdUI.Input();
        tableLayoutPanel1 = new TableLayoutPanel();
        panel1 = new AntdUI.Panel();
        btnLogin = new AntdUI.ButtonShadow();
        btnCancel = new AntdUI.ButtonShadow();
        selectLang = new AntdUI.Select();
        pageHeader1 = new AntdUI.PageHeader();
        lblTitle = new Label();
        tableLayoutPanel1.SuspendLayout();
        panel1.SuspendLayout();
        SuspendLayout();
        // 
        // lblAccount
        // 
        resources.ApplyResources(lblAccount, "lblAccount");
        lblAccount.Name = "lblAccount";
        // 
        // lblPassword
        // 
        resources.ApplyResources(lblPassword, "lblPassword");
        lblPassword.Name = "lblPassword";
        // 
        // lblTip
        // 
        resources.ApplyResources(lblTip, "lblTip");
        lblTip.Name = "lblTip";
        // 
        // lblLanguage
        // 
        resources.ApplyResources(lblLanguage, "lblLanguage");
        lblLanguage.Name = "lblLanguage";
        // 
        // inputUserNumber
        // 
        resources.ApplyResources(inputUserNumber, "inputUserNumber");
        inputUserNumber.Name = "inputUserNumber";
        // 
        // inputPassword
        // 
        resources.ApplyResources(inputPassword, "inputPassword");
        inputPassword.Name = "inputPassword";
        inputPassword.UseSystemPasswordChar = true;
        // 
        // tableLayoutPanel1
        // 
        tableLayoutPanel1.BackColor = SystemColors.ButtonHighlight;
        resources.ApplyResources(tableLayoutPanel1, "tableLayoutPanel1");
        tableLayoutPanel1.Controls.Add(panel1, 0, 2);
        tableLayoutPanel1.Controls.Add(pageHeader1, 0, 0);
        tableLayoutPanel1.Controls.Add(lblTitle, 0, 1);
        tableLayoutPanel1.Name = "tableLayoutPanel1";
        // 
        // panel1
        // 
        panel1.BackColor = SystemColors.ButtonHighlight;
        panel1.Controls.Add(btnLogin);
        panel1.Controls.Add(btnCancel);
        panel1.Controls.Add(selectLang);
        panel1.Controls.Add(inputUserNumber);
        panel1.Controls.Add(lblAccount);
        panel1.Controls.Add(inputPassword);
        panel1.Controls.Add(lblPassword);
        panel1.Controls.Add(lblTip);
        panel1.Controls.Add(lblLanguage);
        resources.ApplyResources(panel1, "panel1");
        panel1.Name = "panel1";
        // 
        // btnLogin
        // 
        resources.ApplyResources(btnLogin, "btnLogin");
        btnLogin.Name = "btnLogin";
        btnLogin.Shadow = 6;
        btnLogin.WaveSize = 6;
        btnLogin.Click += btnLogin_Click;
        // 
        // btnCancel
        // 
        resources.ApplyResources(btnCancel, "btnCancel");
        btnCancel.Name = "btnCancel";
        btnCancel.Click += btnCancel_Click;
        // 
        // selectLang
        // 
        selectLang.Items.AddRange(new object[] { "¼òÌåÖÐÎÄ", "English" });
        resources.ApplyResources(selectLang, "selectLang");
        selectLang.MaxCount = 10;
        selectLang.Name = "selectLang";
        selectLang.SelectedIndexChanged += selectLang_SelectedIndexChanged;
        // 
        // pageHeader1
        // 
        resources.ApplyResources(pageHeader1, "pageHeader1");
        pageHeader1.Name = "pageHeader1";
        pageHeader1.ShowButton = true;
        pageHeader1.ShowIcon = true;
        // 
        // lblTitle
        // 
        resources.ApplyResources(lblTitle, "lblTitle");
        lblTitle.Name = "lblTitle";
        // 
        // LoginForm
        // 
        resources.ApplyResources(this, "$this");
        AutoScaleMode = AutoScaleMode.Dpi;
        Controls.Add(tableLayoutPanel1);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "LoginForm";
        tableLayoutPanel1.ResumeLayout(false);
        tableLayoutPanel1.PerformLayout();
        panel1.ResumeLayout(false);
        panel1.PerformLayout();
        ResumeLayout(false);
    }
}
