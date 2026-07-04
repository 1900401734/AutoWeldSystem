using System.Globalization;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 日志管理界面时间列的统一格式化规则。
/// 默认只显示时间；用户勾选“显示日期”后再显示完整年月日。
/// </summary>
public static class LogTimestampDisplayRules
{
    private const string TimeOnlyFormat = "HH:mm:ss.fff";
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>
    /// 根据界面开关决定日志表格时间文本。
    /// 使用 InvariantCulture 可以避免不同系统区域设置影响日志显示格式。
    /// </summary>
    public static string Format(DateTime value, bool showDate)
        => value.ToString(showDate ? DateTimeFormat : TimeOnlyFormat, CultureInfo.InvariantCulture);
}
