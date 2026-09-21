using AutoWeldSystem.UI.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// 统一打开 PLC 写入调试窗口。
/// 这样各个表格只需要提供地址和类型，不需要直接依赖窗口创建细节。
/// </summary>
public sealed class PlcWriteDebugLauncher(IServiceProvider serviceProvider)
{
    /// <summary>
    /// 打开 PLC 写入调试窗口，并把当前行的地址信息预填进去。
    /// </summary>
    /// <param name="owner">窗口拥有者，用于保证弹窗在当前界面前方。</param>
    /// <param name="preset">待预填的 PLC 地址信息。</param>
    public void Show(IWin32Window? owner, PlcWriteDebugPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        using var form = serviceProvider.GetRequiredService<PlcWriteDebugForm>();
        form.ApplyPreset(preset);

        if (owner is null)
        {
            form.ShowDialog();
            return;
        }

        form.ShowDialog(owner);
    }
}
