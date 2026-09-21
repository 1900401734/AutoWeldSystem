namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 测试项单位在报表和 MES 过程参数中的统一格式。
/// </summary>
public static class TestItemUnitFormatRules
{
    public static string FormatHeader(string? header, string? unit, SchemeDetailValueRole role)
    {
        var normalizedHeader = header?.Trim() ?? string.Empty;
        var normalizedUnit = ResolveUnit(unit, role);
        return string.IsNullOrEmpty(normalizedHeader) || string.IsNullOrEmpty(normalizedUnit)
            ? normalizedHeader
            : $"{normalizedHeader} ({normalizedUnit})";
    }

    public static string FormatValue(string? value, string? unit, SchemeDetailValueRole role)
    {
        var normalizedValue = value?.Trim() ?? string.Empty;
        var normalizedUnit = ResolveUnit(unit, role);
        return string.IsNullOrEmpty(normalizedValue) || string.IsNullOrEmpty(normalizedUnit)
            ? normalizedValue
            : $"{normalizedValue} {normalizedUnit}";
    }

    private static string ResolveUnit(string? unit, SchemeDetailValueRole role)
    {
        return role is SchemeDetailValueRole.Actual or SchemeDetailValueRole.Upper or SchemeDetailValueRole.Lower
            ? unit?.Trim() ?? string.Empty
            : string.Empty;
    }
}
