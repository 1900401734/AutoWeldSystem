using AntdUI;
using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.UI.Base;

namespace AutoWeldSystem.UI.Forms;

public partial class OperatorInputForm : BaseWindow
{
    private readonly ILocalizationService _localizer;

    public OperatorInputForm(ILocalizationService localizer)
    {
        InitializeComponent();
        _localizer = localizer;

        CancelButton = btnCancel;
        Shown += (_, _) =>
        {
            txtEmployeeNumber.Text =  GlobalContext.CurrentUser?.UserNumber ?? string.Empty;
            txtEmployeeNumber.Focus();
            txtEmployeeNumber.SelectAll();
        };
    }

    public string EmployeeNumber => txtEmployeeNumber.Text.Trim();

    /// <summary>
    /// 语言变化时刷新弹窗文本。
    /// </summary>
    protected override void OnLanguageChanged()
    {
        Text = _localizer.GetString(TextKeys.Operator.DialogTitle);
        lblEmployeeNumber.Text = _localizer.GetString(TextKeys.Operator.DialogLabel);
        btnOk.Text = _localizer.GetString(TextKeys.Common.ActionSave);
        btnCancel.Text = _localizer.GetString(TextKeys.Common.ActionCancel);
    }

    private void btnOk_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtEmployeeNumber.Text))
        {
            AntdUI.Message.warn(this, _localizer.GetString(TextKeys.Operator.EmployeeNumberRequired));
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
