using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 双工位显示名称的规范化和校验规则。
/// </summary>
public static class StationDisplayNameRules
{
    public const string DefaultStation1DisplayName = "左";
    public const string DefaultStation2DisplayName = "右";

    /// <summary>
    /// 只格式化展示文案；单工位、共享或未知编号沿用调用方的既有文本，不改变工位身份。
    /// </summary>
    public static string FormatForDisplay(
        AppSettings settings,
        int stationNo,
        ILocalizationService localizer,
        string fallbackText,
        bool includePhysicalNumber = false)
        => FormatCore(settings, stationNo, localizer, fallbackText, includePhysicalNumber, appendStationSuffix: true);

    /// <summary>
    /// 表格列头已说明“工位”，单元格保留保存的名称，不补充或删除用户填写的后缀。
    /// </summary>
    public static string FormatForTable(
        AppSettings settings,
        int stationNo,
        ILocalizationService localizer,
        string fallbackText,
        bool includePhysicalNumber = false)
        => FormatCore(settings, stationNo, localizer, fallbackText, includePhysicalNumber, appendStationSuffix: false);

    private static string FormatCore(
        AppSettings settings,
        int stationNo,
        ILocalizationService localizer,
        string fallbackText,
        bool includePhysicalNumber,
        bool appendStationSuffix)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(localizer);
        if (!settings.EnableDualStation || stationNo is not (1 or 2))
            return fallbackText;

        var names = NormalizeForLoad(true, settings.Station1DisplayName, settings.Station2DisplayName);
        var name = stationNo == 2 ? names.Station2 : names.Station1;
        var label = !appendStationSuffix || name.EndsWith("工位", StringComparison.Ordinal)
            || name.EndsWith(" Station", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Station", StringComparison.OrdinalIgnoreCase)
            ? name
            : localizer.GetString(TextKeys.Common.StationNameFormat, name);
        return includePhysicalNumber
            ? localizer.GetString(TextKeys.Common.StationWithNumberFormat, label,
                localizer.GetString(TextKeys.Common.StationNumberFormat, stationNo))
            : label;
    }

    /// <summary>
    /// 仅替换已知工位的业务摘要前缀；不扫描任意正文，也不修改日志、JSON 或外部返回原文。
    /// </summary>
    public static string FormatKnownMessage(
        AppSettings settings,
        int stationNo,
        ILocalizationService localizer,
        string message)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(localizer);
        if (!settings.EnableDualStation || stationNo is not (1 or 2) || string.IsNullOrEmpty(message))
            return message;

        foreach (var prefix in new[] { $"工位{stationNo}", $"工位 {stationNo}", $"Station {stationNo}", $"Station{stationNo}" })
        {
            if (message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && (message.Length == prefix.Length || !char.IsDigit(message[prefix.Length])))
            {
                var remainder = message[prefix.Length..];
                if (remainder.StartsWith("配方名称=", StringComparison.Ordinal)
                    || remainder.StartsWith("配方名称\"", StringComparison.Ordinal)
                    || remainder.StartsWith("=", StringComparison.Ordinal)
                    || remainder.StartsWith("\"", StringComparison.Ordinal))
                    return message;
                return FormatForDisplay(settings, stationNo, localizer, prefix) + remainder;
            }
        }
        return message;
    }

    /// <summary>
    /// 加载历史配置时为新增列的空值回填默认名称。
    /// </summary>
    public static StationDisplayNames NormalizeForLoad(
        bool dualStationEnabled,
        string? station1,
        string? station2)
    {
        var station1Missing = string.IsNullOrWhiteSpace(station1);
        var station2Missing = string.IsNullOrWhiteSpace(station2);
        var station1WithFallback = station1Missing
            ? DefaultStation1DisplayName
            : station1!;
        var station2WithFallback = station2Missing
            ? DefaultStation2DisplayName
            : station2!;

        var normalizedStation1 = station1WithFallback.Trim();
        var normalizedStation2 = station2WithFallback.Trim();
        if (dualStationEnabled
            && (station1Missing || station2Missing)
            && string.Equals(normalizedStation1, normalizedStation2, StringComparison.OrdinalIgnoreCase))
        {
            return new StationDisplayNames(DefaultStation1DisplayName, DefaultStation2DisplayName);
        }

        return NormalizeAndValidate(dualStationEnabled, normalizedStation1, normalizedStation2);
    }

    /// <summary>
    /// 去除名称首尾空格，并在双工位模式下校验必填和唯一性。
    /// </summary>
    public static StationDisplayNames NormalizeAndValidate(
        bool dualStationEnabled,
        string? station1,
        string? station2)
    {
        var normalizedStation1 = station1?.Trim() ?? string.Empty;
        var normalizedStation2 = station2?.Trim() ?? string.Empty;

        if (!dualStationEnabled)
        {
            return new StationDisplayNames(normalizedStation1, normalizedStation2);
        }

        if (string.IsNullOrWhiteSpace(normalizedStation1) || string.IsNullOrWhiteSpace(normalizedStation2))
        {
            throw new ArgumentException(TextKeys.SystemSetting.MessageStationDisplayNameRequired);
        }

        if (string.Equals(normalizedStation1, normalizedStation2, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(TextKeys.SystemSetting.MessageStationDisplayNameDuplicate);
        }

        return new StationDisplayNames(normalizedStation1, normalizedStation2);
    }
}

/// <summary>
/// 规范化后的双工位显示名称。
/// </summary>
public sealed record StationDisplayNames(string Station1, string Station2);
