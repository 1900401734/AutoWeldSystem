using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 双工位显示名称的规范化和校验规则。
/// </summary>
public static class StationDisplayNameRules
{
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
