namespace AutoWeldSystem.Core.Runtime;

/// <summary>
/// 生产监控右侧区域支持的高度布局模式。
/// </summary>
public enum MonitorRightLayoutMode
{
    Compact,
    Regular
}

/// <summary>
/// 生产监控右侧区域的设备像素布局结果。
/// </summary>
public readonly record struct MonitorRightLayoutMetrics(
    MonitorRightLayoutMode Mode,
    int ContentHeight,
    int StatusPanelHeight,
    int ProductResultHeight,
    int MetricPanelHeight,
    int MetricRowHeight,
    int MetricHeaderHeight,
    bool RequiresScroll);

/// <summary>
/// 按可用高度和 DPI 统一计算生产监控右侧区域尺寸。
/// </summary>
public static class MonitorRightLayoutRules
{
    public const int BaseDpi = 96;
    public const int CompactThresholdLogicalHeight = 850;
    public const int MinimumContentLogicalHeight = 772;

    public const int CompactStatusPanelLogicalHeight = 56;
    public const int CompactProductResultLogicalHeight = 56;
    // 指标区高度按“页签头 + 表头 + 6 行指标”留出，移除工单数量行后同步收缩一行，避免表格底部留空白。
    public const int CompactMetricPanelLogicalHeight = 227;
    public const int CompactMetricRowLogicalHeight = 27;
    public const int CompactMetricHeaderLogicalHeight = 29;

    public const int RegularStatusPanelLogicalHeight = 70;
    public const int RegularProductResultLogicalHeight = 70;
    public const int RegularMetricPanelLogicalHeight = 258;
    public const int RegularMetricRowLogicalHeight = 32;
    public const int RegularMetricHeaderLogicalHeight = 34;

    public static int ToLogicalHeight(int clientHeight, int deviceDpi)
    {
        var normalizedHeight = Math.Max(0, clientHeight);
        var normalizedDpi = NormalizeDpi(deviceDpi);
        return (int)Math.Floor(normalizedHeight * (double)BaseDpi / normalizedDpi);
    }

    public static MonitorRightLayoutMetrics Resolve(int clientHeight, int deviceDpi)
    {
        var normalizedDpi = NormalizeDpi(deviceDpi);
        var logicalHeight = ToLogicalHeight(clientHeight, normalizedDpi);
        var mode = logicalHeight < CompactThresholdLogicalHeight
            ? MonitorRightLayoutMode.Compact
            : MonitorRightLayoutMode.Regular;
        var requiresScroll = logicalHeight < MinimumContentLogicalHeight;
        var contentLogicalHeight = Math.Max(logicalHeight, MinimumContentLogicalHeight);
        var contentHeight = requiresScroll
            ? ScaleToDevicePixels(contentLogicalHeight, normalizedDpi)
            : Math.Max(Math.Max(0, clientHeight), ScaleToDevicePixels(contentLogicalHeight, normalizedDpi));

        return mode == MonitorRightLayoutMode.Compact
            ? new MonitorRightLayoutMetrics(
                mode,
                contentHeight,
                ScaleToDevicePixels(CompactStatusPanelLogicalHeight, normalizedDpi),
                ScaleToDevicePixels(CompactProductResultLogicalHeight, normalizedDpi),
                ScaleToDevicePixels(CompactMetricPanelLogicalHeight, normalizedDpi),
                ScaleToDevicePixels(CompactMetricRowLogicalHeight, normalizedDpi),
                ScaleToDevicePixels(CompactMetricHeaderLogicalHeight, normalizedDpi),
                requiresScroll)
            : new MonitorRightLayoutMetrics(
                mode,
                contentHeight,
                ScaleToDevicePixels(RegularStatusPanelLogicalHeight, normalizedDpi),
                ScaleToDevicePixels(RegularProductResultLogicalHeight, normalizedDpi),
                ScaleToDevicePixels(RegularMetricPanelLogicalHeight, normalizedDpi),
                ScaleToDevicePixels(RegularMetricRowLogicalHeight, normalizedDpi),
                ScaleToDevicePixels(RegularMetricHeaderLogicalHeight, normalizedDpi),
                requiresScroll);
    }

    private static int NormalizeDpi(int deviceDpi)
        => deviceDpi > 0 ? deviceDpi : BaseDpi;

    private static int ScaleToDevicePixels(int logicalValue, int deviceDpi)
        => Math.Max(0, (int)Math.Round(logicalValue * (double)deviceDpi / BaseDpi, MidpointRounding.AwayFromZero));
}
