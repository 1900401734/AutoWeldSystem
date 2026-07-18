namespace AutoWeldSystem.Core.Runtime;

/// <summary>
/// 系统设置页支持的响应式列模式。
/// </summary>
public enum SystemSettingLayoutMode
{
    SingleColumn,
    TwoColumns,
    ThreeColumns
}

/// <summary>
/// 将设备像素换算为逻辑宽度，并集中决定系统设置页的响应式模式。
/// </summary>
public static class SystemSettingLayoutRules
{
    public const int BaseDpi = 96;
    public const int TwoColumnMinimumLogicalWidth = 760;
    public const int ThreeColumnMinimumLogicalWidth = 1200;

    public static int ToLogicalWidth(int clientWidth, int deviceDpi)
    {
        var normalizedWidth = Math.Max(0, clientWidth);
        var normalizedDpi = deviceDpi > 0 ? deviceDpi : BaseDpi;
        return (int)Math.Floor(normalizedWidth * (double)BaseDpi / normalizedDpi);
    }

    public static SystemSettingLayoutMode ResolveMode(int clientWidth, int deviceDpi)
    {
        var logicalWidth = ToLogicalWidth(clientWidth, deviceDpi);
        if (logicalWidth >= ThreeColumnMinimumLogicalWidth)
        {
            return SystemSettingLayoutMode.ThreeColumns;
        }

        return logicalWidth >= TwoColumnMinimumLogicalWidth
            ? SystemSettingLayoutMode.TwoColumns
            : SystemSettingLayoutMode.SingleColumn;
    }
}
