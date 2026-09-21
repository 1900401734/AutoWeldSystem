using System.Reflection;

namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// 控件绘制优化辅助方法。
/// </summary>
public static class ControlRenderingHelper
{
    private static readonly PropertyInfo? DoubleBufferedProperty =
        typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>
    /// 启用单个控件的双缓冲。DoubleBuffered 是受保护属性，只能通过反射设置。
    /// </summary>
    /// <param name="control">目标控件。</param>
    public static void EnableDoubleBuffering(Control control)
    {
        DoubleBufferedProperty?.SetValue(control, true);
    }

    /// <summary>
    /// 递归启用控件树的双缓冲。
    /// 页面由多层 TableLayoutPanel 嵌套组成，只给根控件开启无法避免子控件逐块重绘，
    /// 因此在页面首次载入时对整棵树统一开启。
    /// </summary>
    /// <param name="root">控件树根节点。</param>
    public static void EnableDoubleBufferingRecursive(Control root)
    {
        EnableDoubleBuffering(root);
        foreach (Control child in root.Controls)
        {
            EnableDoubleBufferingRecursive(child);
        }
    }
}
