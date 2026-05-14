using System.Runtime.InteropServices;
using AutoWeldSystem.Core;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Base;

/// <summary>
/// 所有业务 UserControl 的基础类，统一处理语言切换回调和 Tab 顺序整理。
/// </summary>
public class BaseView : UserControl
{
    private const int WmSetRedraw = 0x000B;
    private bool _tabOrderInitialized;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        GlobalContext.LanguageChanged += GlobalContext_LanguageChanged;

        if (!_tabOrderInitialized)
        {
            TabOrderHelper.Apply(this);
            _tabOrderInitialized = true;
        }

        OnLanguageChanged();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        GlobalContext.LanguageChanged -= GlobalContext_LanguageChanged;
        base.OnHandleDestroyed(e);
    }

    protected virtual void OnLanguageChanged()
    {
    }

    private void GlobalContext_LanguageChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(HandleLanguageChanged));
            return;
        }

        HandleLanguageChanged();
    }

    private void HandleLanguageChanged()
    {
        if (IsHandleCreated)
        {
            SendMessage(Handle, WmSetRedraw, IntPtr.Zero, IntPtr.Zero);
        }

        SuspendLayoutRecursive(this);
        try
        {
            OnLanguageChanged();
        }
        finally
        {
            ResumeLayoutRecursive(this);

            if (IsHandleCreated)
            {
                SendMessage(Handle, WmSetRedraw, new IntPtr(1), IntPtr.Zero);
            }

            Invalidate(true);
            Update();
        }
    }

    private static void SuspendLayoutRecursive(Control control)
    {
        control.SuspendLayout();
        foreach (Control child in control.Controls)
        {
            SuspendLayoutRecursive(child);
        }
    }

    private static void ResumeLayoutRecursive(Control control)
    {
        foreach (Control child in control.Controls)
        {
            ResumeLayoutRecursive(child);
        }

        control.ResumeLayout(false);
    }
}
