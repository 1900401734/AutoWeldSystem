using System.Globalization;
using System.Text.RegularExpressions;
using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Plc;

/// <summary>
/// PLC 地址表达式。
/// 表达式格式示例：14:F-0_2、0:S-8_3、DB97.26:F-0_2，其中相对地址以字节偏移开头，绝对地址直接以 PLC 地址开头。
/// 计算式如 (134-84):S-7_3，表示读两个相对偏移后相减，类型和规则对两个操作数同时生效。
/// </summary>
public sealed record PlcOffsetExpression(
    int Offset,
    string DataType,
    int Rule,
    int? DecimalPlaces = null,
    string? AbsoluteAddress = null,
    int? SubtrahendOffset = null)
{
    public const int MaxDecimalPlaces = 10;
    public const string RuleHint = "表达式：相对偏移或绝对地址:类型-规则_小数位；类型 B/H/I/F/S；规则 0原值、1除以10、2除以100、3除以1000、4结果(2=NG、3=OK、4=焊前NG)；相对地址如 14:F-0_2，绝对地址如 DB97.26:F-0_2，字符串如 0:S-8_3；计算式如 (134-84):S-7_3 表示两个相对偏移相减，仅支持一次减法且不支持绝对地址、Bool 和结果规则，是否按数值字符串处理及裁切/四舍五入由系统设置全局控制。";

    /// <summary>
    /// 计算式的减数是否存在。为 true 时读取两个地址并相减，仅相对偏移支持该形态。
    /// </summary>
    public bool IsCalculated => SubtrahendOffset.HasValue;

    /// <summary>
    /// 解析 PLC 地址表达式；数字开头表示相对偏移，PLC 地址开头表示绝对地址。
    /// </summary>
    public static PlcOffsetExpression Parse(string text)
    {
        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new FormatException("偏移表达式不能为空。");
        }

        var colonIndex = normalized.IndexOf(':');
        var addressPart = colonIndex >= 0 ? normalized[..colonIndex].Trim() : normalized;
        // 计算式先识别：括号形态与单地址互斥，识别失败时按原有单地址规则继续解析。
        var isCalculated = TryParseCalculation(text, addressPart, out var calculationOffsets);
        var subtrahendOffset = isCalculated ? calculationOffsets.Subtrahend : (int?)null;
        var absoluteAddress = string.Empty;
        var isAbsoluteAddress = !isCalculated
            && TryNormalizeAbsoluteAddress(addressPart, out absoluteAddress);

        var offset = isCalculated ? calculationOffsets.Minuend : 0;
        if (!isCalculated
            && !isAbsoluteAddress
            && !int.TryParse(addressPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out offset))
        {
            throw new FormatException($"表达式“{text}”必须使用数字相对偏移或有效 PLC 绝对地址。");
        }

        var dataType = AppConstants.PlcDataTypes.Int16;
        var rule = 0;
        int? decimalPlaces = null;
        if (colonIndex >= 0)
        {
            var metadata = normalized[(colonIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(metadata))
            {
                throw new FormatException($"表达式“{text}”的数据类型和规则不能为空。");
            }

            var ruleIndex = metadata.IndexOf('-');
            var dataTypeToken = ruleIndex >= 0 ? metadata[..ruleIndex].Trim() : metadata;
            if (string.IsNullOrWhiteSpace(dataTypeToken))
            {
                throw new FormatException($"表达式“{text}”的数据类型不能为空。");
            }

            dataType = NormalizeDataType(dataTypeToken);
            if (ruleIndex >= 0)
            {
                var ruleText = metadata[(ruleIndex + 1)..].Trim();
                var decimalIndex = ruleText.IndexOf('_');
                var ruleToken = decimalIndex >= 0 ? ruleText[..decimalIndex] : ruleText;
                if (string.IsNullOrWhiteSpace(ruleToken))
                {
                    throw new FormatException($"表达式“{text}”的规则不能为空。");
                }

                if (!int.TryParse(ruleToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out rule))
                {
                    throw new FormatException($"表达式“{text}”的规则“{ruleToken}”不是有效整数。");
                }

                if (decimalIndex >= 0)
                {
                    decimalPlaces = ParseDecimalPlaces(text, ruleText[(decimalIndex + 1)..], dataType, rule);
                }
            }
        }

        if (subtrahendOffset.HasValue)
        {
            EnsureCalculationSupported(text, dataType, rule);
        }

        return new PlcOffsetExpression(offset, dataType, rule, decimalPlaces, absoluteAddress, subtrahendOffset);
    }

    /// <summary>
    /// 识别 (被减数-减数) 形态的计算式。仅支持一次减法和两个整数相对偏移：
    /// 现场需求是「焊后位移量 - 焊前位移量」，多操作数和乘除没有对应场景。
    /// 非括号形态返回 false，由调用方按原有单地址规则继续解析。
    /// </summary>
    private static bool TryParseCalculation(string text, string addressPart, out (int Minuend, int Subtrahend) offsets)
    {
        offsets = default;
        var hasOpen = addressPart.StartsWith('(');
        var hasClose = addressPart.EndsWith(')');
        if (!hasOpen && !hasClose)
        {
            return false;
        }

        if (!hasOpen || !hasClose || addressPart.Length <= 2)
        {
            throw new FormatException($"表达式“{text}”的计算式括号必须配对，格式如 (134-84)。");
        }

        var body = addressPart[1..^1].Trim();
        // 负号不能作为运算符：操作数是字节偏移，负偏移没有意义，因此 - 只可能是减号。
        var parts = body.Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            throw new FormatException($"表达式“{text}”的计算式只支持一次减法，格式如 (134-84)。");
        }

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minuend)
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var subtrahend))
        {
            throw new FormatException($"表达式“{text}”的计算式操作数必须是数字相对偏移，不支持 PLC 绝对地址。");
        }

        offsets = (minuend, subtrahend);
        return true;
    }

    /// <summary>
    /// 结果规则返回 2=NG、3=OK、4=焊前NG 的枚举值，Bool 只有真假，两者相减都没有物理意义，
    /// 因此在解析阶段就拒绝，不留到读取时才报错。
    /// </summary>
    private static void EnsureCalculationSupported(string text, string dataType, int rule)
    {
        if (rule == 4 || IsBoolDataType(dataType))
        {
            throw new FormatException($"表达式“{text}”的计算式不能使用 Bool 类型或结果规则。");
        }
    }

    /// <summary>
    /// 按基地址和上下文偏移计算最终 PLC 地址。
    /// </summary>
    public bool IsAbsoluteAddress => !string.IsNullOrWhiteSpace(AbsoluteAddress);

    public string ResolveAddress(string baseAddress, int contextOffset)
    {
        if (IsAbsoluteAddress)
        {
            return AbsoluteAddress!;
        }

        return AddByteOffset(baseAddress, contextOffset + Offset);
    }

    /// <summary>
    /// 计算式减数的最终 PLC 地址；非计算式返回空串。
    /// </summary>
    public string ResolveSubtrahendAddress(string baseAddress, int contextOffset)
    {
        return SubtrahendOffset.HasValue
            ? AddByteOffset(baseAddress, contextOffset + SubtrahendOffset.Value)
            : string.Empty;
    }

    /// <summary>
    /// 尝试计算偏移地址；失败时返回 false，避免界面预览被异常打断。
    /// </summary>
    public static bool TryResolveAddress(string? baseAddress, int contextOffset, string? expressionText, out string address)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(expressionText))
            {
                address = string.Empty;
                return false;
            }

            var expression = Parse(expressionText);
            if (!expression.IsAbsoluteAddress && string.IsNullOrWhiteSpace(baseAddress))
            {
                address = string.Empty;
                return false;
            }

            address = expression.ResolveAddress(baseAddress ?? string.Empty, contextOffset);
            return true;
        }
        catch
        {
            address = string.Empty;
            return false;
        }
    }

    private static bool TryNormalizeAbsoluteAddress(string value, out string address)
    {
        address = string.Empty;
        var normalized = value.Trim();
        if (!Regex.IsMatch(
                normalized,
                @"^(?:DB\d+\.(?:\d+(?:\.\d+)?|DB[XBWD]\d+(?:\.\d+)?)|(?:M|I|Q|AI|AQ)\d+(?:\.\d+)?)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return false;
        }

        address = normalized;
        return true;
    }

    private static string NormalizeDataType(string? token)
    {
        return token?.Trim().ToUpperInvariant() switch
        {
            "B" or "BOOL" => AppConstants.PlcDataTypes.Bool,
            "I" or "INT32" or "DINT" => AppConstants.PlcDataTypes.Int32,
            "F" or "FLOAT" or "REAL" => AppConstants.PlcDataTypes.Float,
            "S" or "STRING" => AppConstants.PlcDataTypes.String,
            "H" or "INT16" or "SHORT" or "INT" => AppConstants.PlcDataTypes.Int16,
            _ => AppConstants.PlcDataTypes.Int16
        };
    }

    private static int ParseDecimalPlaces(string expressionText, string token, string dataType, int rule)
    {
        if (rule == 4 || IsBoolDataType(dataType))
        {
            throw new FormatException($"表达式“{expressionText}”的小数位不能用于 Bool 或结果规则；String 使用 _小数位表示数值字符串。");
        }

        if (!int.TryParse(token.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var decimalPlaces)
            || decimalPlaces < 0
            || decimalPlaces > MaxDecimalPlaces)
        {
            throw new FormatException($"表达式“{expressionText}”的小数位必须是 0-{MaxDecimalPlaces} 之间的整数。");
        }

        return decimalPlaces;
    }

    private static bool IsBoolDataType(string dataType)
    {
        return string.Equals(dataType, AppConstants.PlcDataTypes.Bool, StringComparison.OrdinalIgnoreCase);
    }

    public static string AddByteOffset(string baseAddress, int offset)
    {
        var normalizedBase = baseAddress.Trim();
        if (string.IsNullOrWhiteSpace(normalizedBase))
        {
            throw new FormatException("基地址不能为空。");
        }

        var dotIndex = normalizedBase.LastIndexOf('.');
        if (normalizedBase.StartsWith("DB", StringComparison.OrdinalIgnoreCase) && dotIndex > 2)
        {
            var dbPart = normalizedBase[..dotIndex];
            var bytePart = normalizedBase[(dotIndex + 1)..];
            if (int.TryParse(bytePart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var byteOffset))
            {
                return $"{dbPart}.{byteOffset + offset}";
            }
        }

        var prefixLength = 0;
        while (prefixLength < normalizedBase.Length && !char.IsDigit(normalizedBase[prefixLength]))
        {
            prefixLength++;
        }

        if (prefixLength > 0
            && prefixLength < normalizedBase.Length
            && int.TryParse(normalizedBase[prefixLength..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var start))
        {
            return $"{normalizedBase[..prefixLength]}{start + offset}";
        }

        throw new FormatException($"无法根据基地址“{normalizedBase}”计算偏移地址。");
    }
}
