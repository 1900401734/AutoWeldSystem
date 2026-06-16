using System.ComponentModel;
using System.Runtime.InteropServices;
using AntdUI;
using AutoWeldSystem.Core;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Base;

/// <summary>
/// 所有 AntdUI 窗体的基础类，统一处理语言刷新和 Tab 顺序初始化。
/// </summary>
public class BaseWindow : Window
{
    private const int WmSetRedraw = 0x000B;
    private bool _tabOrderInitialized;

    public BaseWindow()
    {
        Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(GlobalContext.CurrentLanguage);
        AppAssets.ApplyWindowIcon(this);
    }

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

    public void ApplyLanguage()
    {
        var resourceManager = new ComponentResourceManager(GetType());
        resourceManager.ApplyResources(this, "$this");
        ApplyControlResource(resourceManager, this);
    }

    protected virtual void OnLanguageChanged()
    {
    }

    protected virtual bool ApplyDesignerResourcesOnLanguageChanged => true;

    /// <summary>
    /// Runs UI updates safely even when the caller is a background service event.
    /// </summary>
    protected bool RunOnUiThread(Action action, string source, bool requireHandle = true)
    {
        return UiThreadDispatcherProvider.Current.TryRun(this, action, source, requireHandle);
    }

    /// <summary>
    /// Runs async UI work safely even when the caller is a background service event.
    /// </summary>
    protected Task<bool> RunOnUiThreadAsync(Func<Task> action, string source, bool requireHandle = true)
    {
        return UiThreadDispatcherProvider.Current.TryRunAsync(this, action, source, requireHandle);
    }

    private void GlobalContext_LanguageChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        RunOnUiThread(HandleLanguageChanged, "BaseWindow.LanguageChanged");
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
            if (ApplyDesignerResourcesOnLanguageChanged)
            {
                ApplyLanguage();
            }

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

    private static void ApplyControlResource(ComponentResourceManager resourceManager, Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            resourceManager.ApplyResources(control, control.Name);
            if (control.Controls.Count > 0)
            {
                ApplyControlResource(resourceManager, control);
            }
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
