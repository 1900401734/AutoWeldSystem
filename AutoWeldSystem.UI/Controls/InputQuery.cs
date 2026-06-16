using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;

namespace AutoWeldSystem.UI.Components;

/// <summary>
/// 搜索按钮
/// </summary>
[ToolboxItem(true)]
[DefaultEvent(nameof(QueryClick))]
public partial class InputQuery : UserControl
{
    #region 公开属性

    [Description("是否显示搜索按钮")]
    [Category("外观")]
    [DefaultValue(true)]
    public bool IsShowQueryButton { get => btnQuery.Visible; set => btnQuery.Visible = value; }

    [Description("是否显示刷新按钮")]
    [Category("外观")]
    [DefaultValue(true)]
    public bool IsShowRefreshButton { get => btnRefresh.Visible; set => btnRefresh.Visible = value; }

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
