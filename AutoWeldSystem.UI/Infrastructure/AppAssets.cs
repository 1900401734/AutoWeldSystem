using System.Drawing;

namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// 集中管理 UI 项目中的静态资源路径，避免窗体中散落硬编码文件名。
/// </summary>
internal static class AppAssets
{
    private const string AssetsFolderName = "Assets";

    /// <summary>
    /// 软件窗口和可执行文件使用的图标路径。
    /// </summary>
    public static string IconPath => Path.Combine(AppContext.BaseDirectory, AssetsFolderName, "icon.ico");

    /// <summary>
    /// 监控主页标题左侧展示的 Logo 路径。
    /// </summary>
    public static string LogoPath => Path.Combine(AppContext.BaseDirectory, AssetsFolderName, "logo.png");

    /// <summary>
    /// 尝试给窗体应用统一图标；资源缺失时保持默认图标，避免影响设计器和启动流程。
    /// </summary>
    public static void ApplyWindowIcon(Form form)
    {
        if (!File.Exists(IconPath))
        {
            return;
        }

        try
        {
            form.Icon = new Icon(IconPath);
        }
        catch
        {
            // 图标文件损坏或被占用时不阻塞主程序启动。
        }
    }
}
