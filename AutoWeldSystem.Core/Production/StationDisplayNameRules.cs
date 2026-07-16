using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 双工位显示名称的规范化和校验规则。
/// </summary>
public static class StationDisplayNameRules
{
    public const string DefaultStation1DisplayName = "左";
    public const string DefaultStation2DisplayName = "右";

    /// <summary>
    /// 加载历史配置时为新增列的空值回填默认名称。
    /// </summary>
    public static StationDisplayNames NormalizeForLoad(
        bool dualStationEnabled,
        string? station1,
        string? station2)
    {
        var station1WithFallback = string.IsNullOrWhiteSpace(station1)
            ? DefaultStation1DisplayName
            : station1;
        var station2WithFallback = string.IsNullOrWhiteSpace(station2)
            ? DefaultStation2DisplayName
            : station2;

        var normalizedStation1 = station1WithFallback.Trim();
        var normalizedStation2 = station2WithFallback.Trim();
        if (dualStationEnabled && string.Equals(normalizedStation1, normalizedStation2, StringComparison.OrdinalIgnoreCase))
        {
            return new StationDisplayNames(DefaultStation1DisplayName, DefaultStation2DisplayName);
        }

        return new StationDisplayNames(normalizedStation1, normalizedStation2);
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
