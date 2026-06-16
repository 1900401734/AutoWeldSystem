using System.Globalization;
using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Plc;

/// <summary>
/// PLC 偏移表达式。
/// 表达式格式示例：0:F-0、12:H-4，其中 0/12 是字节偏移，F/H 是数据类型，- 后是显示或结果规则。
/// </summary>
public sealed record PlcOffsetExpression(int Offset, string DataType, int Rule)
{
    /// <summary>
    /// 解析偏移表达式；表达式必须以数字偏移开头，不能直接填写绝对地址。
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
        if (!int.TryParse(addressPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var offset))
        {
            throw new FormatException($"表达式“{text}”必须使用相对偏移，不能填写绝对地址。");
        }

        var dataType = AppConstants.PlcDataTypes.Int16;
        var rule = 0;
        if (colonIndex >= 0)
        {
            var metadata = normalized[(colonIndex + 1)..].Trim();
            var ruleIndex = metadata.IndexOf('-');
            var dataTypeToken = ruleIndex >= 0 ? metadata[..ruleIndex] : metadata;
            dataType = NormalizeDataType(dataTypeToken);
            if (ruleIndex >= 0 && int.TryParse(metadata[(ruleIndex + 1)..], out var parsedRule))
            {
                rule = parsedRule;
            }
        }

        return new PlcOffsetExpression(offset, dataType, rule);
    }

    /// <summary>
    /// 按基地址和上下文偏移计算最终 PLC 地址。
    /// </summary>
    public string ResolveAddress(string baseAddress, int contextOffset)
    {
        return AddByteOffset(baseAddress, contextOffset + Offset);
    }

    /// <summary>
    /// 尝试计算偏移地址；失败时返回 false，避免界面预览被异常打断。
    /// </summary>
    public static bool TryResolveAddress(string? baseAddress, int contextOffset, string? expressionText, out string address)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(baseAddress) || string.IsNullOrWhiteSpace(expressionText))
            {
                address = string.Empty;
                return false;
            }

            address = Parse(expressionText).ResolveAddress(baseAddress, contextOffset);
            return true;
        }
        catch
        {
            address = string.Empty;
            return false;
        }
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
