using AutoWeldSystem.UI.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// 捕获主程序内的右键双击，用于打开 PLC 写入调试窗口。
/// 该入口只在 MainForm 生命周期内安装，避免未登录时也能打开调试工具。
/// </summary>
public sealed class PlcWriteDebugMessageFilter : IMessageFilter
{
    private const int WmRightButtonDoubleClick = 0x0206;

    private readonly IServiceProvider _serviceProvider;
    private readonly Form _mainForm;
    private bool _opening;

    public PlcWriteDebugMessageFilter(IServiceProvider serviceProvider, Form mainForm)
    {
        _serviceProvider = serviceProvider;
        _mainForm = mainForm;
    }

    public bool PreFilterMessage(ref Message m)
    {
        if (m.Msg != WmRightButtonDoubleClick || _opening || _mainForm.IsDisposed)
        {
            return false;
        }

        var target = Control.FromHandle(m.HWnd);
        if (IsInsideDebugForm(target))
        {
            return false;
        }

        _opening = true;
        _mainForm.BeginInvoke(() => ShowDebugForm(target?.FindForm()));
        return false;
    }

    private static bool IsInsideDebugForm(Control? control)
    {
        return control?.FindForm() is PlcWriteDebugForm;
    }

    private void ShowDebugForm(Form? owner)
    {
        try
        {
            if (_mainForm.IsDisposed || HasOpenDebugForm())
            {
                return;
            }

            using var form = _serviceProvider.GetRequiredService<PlcWriteDebugForm>();
            form.ShowDialog(owner is { IsDisposed: false } ? owner : _mainForm);
        }
        finally
        {
            _opening = false;
        }
    }

    private static bool HasOpenDebugForm()
    {
        return Application.OpenForms.Cast<Form>().Any(form => form is PlcWriteDebugForm);
    }
}
