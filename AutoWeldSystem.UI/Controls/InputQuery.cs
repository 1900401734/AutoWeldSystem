using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;

namespace AutoWeldSystem.UI.Controls;

/// <summary>
/// 搜索按钮
/// </summary>
[ToolboxItem(true)]
[DefaultEvent(nameof(QueryClick))]
public partial class InputQuery : UserControl
{
    private bool _isShowQueryButton = true;
    private bool _isShowRefreshButton = true;

    #region 公开属性

    [Description("是否显示搜索按钮")]
    [Category("外观")]
    [DefaultValue(true)]
    public bool IsShowQueryButton
    {
        get => _isShowQueryButton;
        set
        {
            _isShowQueryButton = value;
            btnQuery.Visible = value;
        }
    }

    [Description("是否显示刷新按钮")]
    [Category("外观")]
    [DefaultValue(true)]
    public bool IsShowRefreshButton
    {
        get => _isShowRefreshButton;
        set
        {
            _isShowRefreshButton = value;
            btnRefresh.Visible = value;
        }
    }

    [Description("刷新按钮的权限标记，交给 PermissionUiBinder 按 Tag 控制可用状态")]
    [Category("行为")]
    [DefaultValue("")]
    public string RefreshButtonTag
    {
        // 刷新按钮被封装在本控件内部，外部页面无法直接给它挂权限 Tag，这里开放一个入口。
        get => btnRefresh.Tag as string ?? string.Empty;
        set => btnRefresh.Tag = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    [Description("输入框占位提示文本")]
    [Category("外观")]
    [DefaultValue("")]
    public string PlaceholderText
    {
        get => input1.PlaceholderText ?? string.Empty;
        set => input1.PlaceholderText = value;
    }

    /// <summary>
    /// 搜索按钮点击事件 订阅
    /// </summary>
    public event EventHandler<string>? QueryClick;

    /// <summary>
    /// 按钮点击后事件 委托
    /// </summary>
    public Func<string, Task>? QueryChanged { get; set; }

    #endregion

    #region 重写属性

    [Description("文本")]
    [Category("外观")]
    [DefaultValue("")]
    [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
    public new string Text { get => input1.Text; set => input1.Text = value; }

    #endregion

    public InputQuery()
    {
        InitializeComponent();
        InitializeBehavior();
    }

    public InputQuery(IContainer container)
    {
        container.Add(this);

        InitializeComponent();
        InitializeBehavior();
    }

    public virtual void OnQueryClick(string text = "")
    {
        QueryClick?.Invoke(this, text);
        QueryChanged?.Invoke(text);
    }

    /// <summary>
    /// 两个构造函数都需要绑定交互事件，所以单独提取成一个方法。
    /// </summary>
    private void InitializeBehavior()
    {
        input1.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                OnQueryClick(input1.Text);
            }
        };

        btnRefresh.Click += (s, e) => OnQueryClick();
        btnQuery.Click += (s, e) => OnQueryClick(input1.Text);
    }
}
