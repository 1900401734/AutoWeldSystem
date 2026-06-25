using AutoWeldSystem.Core.Constants;
using System.Globalization;

namespace AutoWeldSystem.Core.Plc;

/// <summary>
/// PLC String 类型数值文本格式化器。
/// 仅处理完整数值字符串，避免误改工单号、产品编号等普通文本。
/// </summary>
public static class PlcStringNumericFormatter
{
    /// <summary>
    /// 按系统设置决定是否把 PLC String 文本当作数值处理。
    /// </summary>
    public static string Format(string? value, int? decimalPlaces, bool enabled, string? mode)
    {
        var text = NormalizePlcText(value);
        if (!enabled || decimalPlaces is not >= 0)
        {
            return text;
        }

        return FormatNumericString(text, decimalPlaces.Value, mode);
    }

    /// <summary>
    /// 规范化处理方式；未知值按裁切处理，保证旧配置有稳定默认行为。
    /// </summary>
    public static string NormalizeMode(string? mode)
    {
        return string.Equals(mode?.Trim(), AppConstants.PlcStringNumericFormatModes.Round, StringComparison.OrdinalIgnoreCase)
            ? AppConstants.PlcStringNumericFormatModes.Round
            : AppConstants.PlcStringNumericFormatModes.Truncate;
    }

    private static string FormatNumericString(string text, int decimalPlaces, string? mode)
    {
        if (!TrySplitNumericString(text, out var isNegative, out var integerPart, out var fractionPart))
        {
            return text;
        }

        if (NormalizeMode(mode) == AppConstants.PlcStringNumericFormatModes.Round)
        {
            return RoundNumericString(isNegative, integerPart, fractionPart, decimalPlaces);
        }

        return TruncateNumericString(isNegative, integerPart, fractionPart, decimalPlaces);
    }

    private static string TruncateNumericString(bool isNegative, string integerPart, string fractionPart, int decimalPlaces)
    {
        var normalizedInteger = NormalizeIntegerPart(integerPart);
        var sign = ResolveSign(isNegative, normalizedInteger, fractionPart);
        if (decimalPlaces == 0)
        {
            return $"{sign}{normalizedInteger}";
        }

        var normalizedFraction = fractionPart.Length > decimalPlaces
            ? fractionPart[..decimalPlaces]
            : fractionPart.PadRight(decimalPlaces, '0');

        return $"{sign}{normalizedInteger}.{normalizedFraction}";
    }

    private static string RoundNumericString(bool isNegative, string integerPart, string fractionPart, int decimalPlaces)
    {
        var normalizedInteger = NormalizeIntegerPart(integerPart);
        var numericText = $"{(isNegative ? "-" : string.Empty)}{normalizedInteger}";
        if (fractionPart.Length > 0)
        {
            numericText = $"{numericText}.{fractionPart}";
        }

        if (!decimal.TryParse(numericText, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            return TruncateNumericString(isNegative, integerPart, fractionPart, decimalPlaces);
        }

        return value.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 拆分完整数值字符串；不接受千分位、单位和混合文本。
    /// </summary>
    private static bool TrySplitNumericString(string text, out bool isNegative, out string integerPart, out string fractionPart)
    {
        isNegative = false;
        integerPart = string.Empty;
        fractionPart = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var startIndex = 0;
        if (text[0] is '+' or '-')
        {
            isNegative = text[0] == '-';
            startIndex = 1;
        }

        if (startIndex >= text.Length)
        {
            return false;
        }

        var decimalIndex = text.IndexOf('.', startIndex);
        if (decimalIndex >= 0)
        {
            integerPart = text[startIndex..decimalIndex];
            fractionPart = text[(decimalIndex + 1)..];
            if (text.IndexOf('.', decimalIndex + 1) >= 0)
            {
                return false;
            }
        }
        else
        {
            integerPart = text[startIndex..];
        }

        var hasDigit = integerPart.Length > 0 || fractionPart.Length > 0;
        return hasDigit && IsAsciiDigits(integerPart) && IsAsciiDigits(fractionPart);
    }

    private static string NormalizeIntegerPart(string integerPart)
    {
        var normalized = integerPart.TrimStart('0');
        return normalized.Length == 0 ? "0" : normalized;
    }

    private static string ResolveSign(bool isNegative, string normalizedInteger, string fractionPart)
    {
        return isNegative && (normalizedInteger != "0" || fractionPart.Any(ch => ch != '0'))
            ? "-"
            : string.Empty;
    }

    private static bool IsAsciiDigits(string text)
    {
        foreach (var ch in text)
        {
            if (ch < '0' || ch > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizePlcText(string? value)
    {
        return value?.Trim().Trim('\0') ?? string.Empty;
    }
}
