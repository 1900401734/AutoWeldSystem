using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Plc;

/// <summary>
/// PLC 调试写入的纯规则。
/// UI 窗口和业务入口共用这里的判断，避免各处重复维护类型和布尔值解析规则。
/// </summary>
public static class PlcDebugWriteRules
{
    /// <summary>
    /// 将界面或配置传入的数据类型归一化为系统支持的 PLC 数据类型。
    /// 未识别的类型统一按 Int16 处理，保持历史默认行为。
    /// </summary>
    /// <param name="dataType">待归一化的数据类型文本。</param>
    /// <returns>系统支持的 PLC 数据类型。</returns>
    public static string NormalizeDataType(string? dataType)
    {
        var normalized = dataType?.Trim() ?? string.Empty;
        return AppConstants.PlcDataTypes.All.Contains(normalized, StringComparer.OrdinalIgnoreCase)
            ? AppConstants.PlcDataTypes.All.First(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase))
            : AppConstants.PlcDataTypes.Int16;
    }

    /// <summary>
    /// 尝试把调试窗口输入的文本解析为 Bool 写入值。
    /// 为了降低误写风险，只接受明确的 1/0/true/false。
    /// </summary>
    /// <param name="valueText">用户输入的写入值。</param>
    /// <param name="value">解析出的布尔值。</param>
    /// <returns>输入明确可解析时返回 true。</returns>
    public static bool TryParseBool(string? valueText, out bool value)
    {
        var normalized = valueText?.Trim() ?? string.Empty;
        if (normalized is "1" || normalized.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (normalized is "0" || normalized.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }
}
