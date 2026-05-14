using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.UI.Base;

namespace AutoWeldSystem.UI.Forms;

public partial class LoginForm : BaseWindow
{
    private readonly ISysUserService _userService;
    private readonly ILocalizationService _localizer;
    private bool _syncingLanguageSelection;

    public LoginForm(ISysUserService userService, ILocalizationService localizer)
    {
        InitializeComponent();

        _userService = userService;
        _localizer = localizer;

        // 登录框里的默认账号和密码不应该写死在界面里，这里统一清空。
        inputUserNumber.Text = string.Empty;
        inputPassword.Text = string.Empty;

        WireEvents();
    }

    public SysUser? AuthenticatedUser => GlobalContext.CurrentUser;

    protected override bool ApplyDesignerResourcesOnLanguageChanged => false;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        // 窗体真正显示后再设置焦点，避免 InitializeComponent/OnLoad 过程抢走输入焦点。
        BeginInvoke(new Action(FocusUserNumberInput));
    }

    /// <summary>
    /// 这里集中绑定额外事件，避免把交互逻辑散落在多个地方。
    /// </summary>
    private void WireEvents()
    {
        inputUserNumber.KeyDown += InputControl_KeyDown;
        inputPassword.KeyDown += InputControl_KeyDown;
        selectLang.KeyDown += InputControl_KeyDown;
    }

    /// <summary>
    /// BaseWindow 在语言变化时会调用这里。
    /// 登录页的静态文本靠 resx，语言下拉项这种动态文本在这里补齐。
    /// </summary>
    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        UpdateLanguageSelection();
        ConfigureLoginTabOrder();
    }

    private void btnLogin_Click(object? sender, EventArgs e)
    {
        AttemptLogin();
    }

    private void btnCancel_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void selectLang_SelectedIndexChanged(object sender, AntdUI.IntEventArgs e)
    {
        if (_syncingLanguageSelection)
        {
            return;
        }

        // 语言切换统一交给本地化服务处理，这样会自动持久化到设置表。
        var targetLanguage = selectLang.SelectedIndex == 0
            ? AppConstants.Languages.Chinese
            : AppConstants.Languages.English;

        _localizer.SetLanguage(targetLanguage);
    }

    /// <summary>
    /// 回车键直接触发登录，减少鼠标操作。
    /// </summary>
    private void InputControl_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
        AttemptLogin();
    }

    /// <summary>
    /// 把登录流程单独收敛成一个方法，按钮点击和回车键都可以复用。
    /// </summary>
    private void AttemptLogin()
    {
        var userNumber = inputUserNumber.Text.Trim();
        var password = inputPassword.Text;

        if (string.IsNullOrWhiteSpace(userNumber) || string.IsNullOrWhiteSpace(password))
        {
            AntdUI.Message.warn(this, _localizer.GetString(TextKeys.Auth.EmptyCredentials));
            return;
        }

        var loginResult = _userService.Login(userNumber, password);
        if (!loginResult.IsSuccess || loginResult.User is null)
        {
            ShowLoginFailure(loginResult.FailureReason);
            return;
        }

        // 登录成功后，把用户和权限统一写入全局上下文。
        GlobalContext.SetCurrentUser(loginResult.User, _userService.GetPermissions(loginResult.User));
        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>
    /// 登录失败原因由服务层返回，界面只负责把它翻译成用户可读消息。
    /// </summary>
    private void ShowLoginFailure(UserLoginFailureReason failureReason)
    {
        var messageKey = failureReason switch
        {
            UserLoginFailureReason.UserDisabled => TextKeys.Auth.UserDisabled,
            UserLoginFailureReason.RoleDisabled => TextKeys.Auth.RoleDisabled,
            _ => TextKeys.Auth.InvalidCredentials
        };

        AntdUI.Message.error(this, _localizer.GetString(messageKey));
    }

    /// <summary>
    /// 语言下拉框的内容不是资源控件属性，所以需要手动同步。
    /// </summary>
    private void UpdateLanguageSelection()
    {
        _syncingLanguageSelection = true;

        selectLang.Items.Clear();
        selectLang.Items.AddRange(new object[]
        {
            _localizer.GetString(TextKeys.Common.LanguageChinese),
            _localizer.GetString(TextKeys.Common.LanguageEnglish)
        });

        selectLang.SelectedIndex = GlobalContext.CurrentLanguage == AppConstants.Languages.English ? 1 : 0;

        _syncingLanguageSelection = false;
    }

    private void ApplyLocalizedTexts()
    {
        Text = _localizer.GetString(TextKeys.Auth.LoginTitle);
        pageHeader1.Text = _localizer.GetString(TextKeys.Auth.LoginTitle);
        lblTitle.Text = _localizer.GetString(TextKeys.Auth.LoginAppTitle);
        lblAccount.Text = _localizer.GetString(TextKeys.Auth.LoginLabelAccount);
        lblPassword.Text = _localizer.GetString(TextKeys.Auth.LoginLabelPassword);
        lblLanguage.Text = _localizer.GetString(TextKeys.Auth.LoginLabelLanguage);
        lblTip.Text = _localizer.GetString(TextKeys.Auth.LoginTip);
        btnLogin.Text = _localizer.GetString(TextKeys.Common.ActionLogin);
        btnCancel.Text = _localizer.GetString(TextKeys.Common.ActionCancel);
    }

    private void ConfigureLoginTabOrder()
    {
        // 登录操作按“账户 -> 密码 -> 语言 -> 登录 -> 取消”的键盘顺序排列。
        inputUserNumber.TabIndex = 0;
        inputPassword.TabIndex = 1;
        selectLang.TabIndex = 2;
        btnLogin.TabIndex = 3;
        btnCancel.TabIndex = 4;

        lblAccount.TabStop = false;
        lblPassword.TabStop = false;
        lblLanguage.TabStop = false;
        lblTip.TabStop = false;
        lblTitle.TabStop = false;
        pageHeader1.TabStop = false;
    }

    private void FocusUserNumberInput()
    {
        ActiveControl = inputUserNumber;
        inputUserNumber.Focus();
    }
}
