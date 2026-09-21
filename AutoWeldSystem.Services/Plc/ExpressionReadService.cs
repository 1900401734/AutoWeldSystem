using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.Core.Production;
using System.Globalization;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// PLC 表达式读取服务。
/// 它把“表达式解析、最终地址计算、PLC 读取、显示规则转换、小数位格式化”集中在一个地方。
/// </summary>
public sealed class ExpressionReadService : IPlcExpressionReadService
{
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IAppSettingsService _settingsService;

    public ExpressionReadService(IPlcCommunicationService plcCommunicationService, IAppSettingsService settingsService)
    {
        _plcCommunicationService = plcCommunicationService;
        _settingsService = settingsService;
    }

    /// <summary>
    /// 严格解析偏移表达式；表达式无效时抛出 FormatException，供正式采集生成业务错误。
    /// </summary>
    public PlcExpressionBinding Resolve(string? baseAddress, int contextOffset, string? expressionText)
    {
        if (string.IsNullOrWhiteSpace(expressionText))
        {
            throw new FormatException("偏移表达式不能为空。");
        }

        var expression = PlcOffsetExpression.Parse(expressionText);
        if (!expression.IsAbsoluteAddress && string.IsNullOrWhiteSpace(baseAddress))
        {
            throw new FormatException("基地址不能为空。");
        }

        return new PlcExpressionBinding(
            expression.ResolveAddress(baseAddress ?? string.Empty, contextOffset),
            expression.DataType,
            expression.Rule,
            expressionText.Trim(),
            expression.DecimalPlaces,
            expression.IsAbsoluteAddress,
            expression.IsCalculated
                ? expression.ResolveSubtrahendAddress(baseAddress ?? string.Empty, contextOffset)
                : null);
    }

    /// <summary>
    /// 安全解析偏移表达式；界面预览失败时不打断整个窗口。
    /// </summary>
    public bool TryResolve(
        string? baseAddress,
        int contextOffset,
        string? expressionText,
        out PlcExpressionBinding binding,
        out string message)
    {
        try
        {
            binding = Resolve(baseAddress, contextOffset, expressionText);
            message = string.Empty;
            return true;
        }
        catch (FormatException ex)
        {
            binding = PlcExpressionBinding.Empty;
            message = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// 先解析表达式，再读取最终 PLC 地址。
    /// </summary>
    public async Task<PlcServiceResult<string>> ReadExpressionTextAsync(
        string? baseAddress,
        int contextOffset,
        string? expressionText,
        string valueRole = "PLC地址",
        int stringLength = 32,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolve(baseAddress, contextOffset, expressionText, out var binding, out var message))
        {
            return PlcServiceResult<string>.Fail($"{valueRole}偏移表达式无效：{message}");
        }

        return await ReadBindingTextAsync(binding, valueRole, stringLength, cancellationToken);
    }

    /// <summary>
    /// 读取已解析的表达式绑定，避免调用方重复拆 Address/DataType/Rule/DecimalPlaces。
    /// 计算式读两个地址并相减，普通表达式走单地址路径。
    /// </summary>
    public async Task<PlcServiceResult<string>> ReadBindingTextAsync(
        PlcExpressionBinding binding,
        string valueRole = "PLC地址",
        int stringLength = 32,
        CancellationToken cancellationToken = default)
    {
        if (binding.IsCalculated)
        {
            return await ReadCalculatedBindingTextAsync(binding, valueRole, stringLength, cancellationToken);
        }

        return await ReadResolvedAddressTextAsync(
            binding.Address,
            binding.DataType,
            binding.Rule,
            valueRole,
            stringLength,
            cancellationToken,
            binding.DecimalPlaces);
    }

    /// <summary>
    /// 计算式取值：两个操作数各自读原始值并按规则缩放，相减后只格式化一次。
    /// 不复用 ReadResolvedAddressTextAsync，因为它内部已完成小数位格式化，
    /// 先格式化再相减会引入二次舍入，与直接从原始值算差值可能差一个末位。
    /// 任一操作数读取失败或无法解析为数值时整体失败，由调用方按现有采集失败处理，
    /// 不输出空值也不把无效值当 0——后者会算出看似合理的差值，掩盖现场配置错误。
    /// </summary>
    private async Task<PlcServiceResult<string>> ReadCalculatedBindingTextAsync(
        PlcExpressionBinding binding,
        string valueRole,
        int stringLength,
        CancellationToken cancellationToken)
    {
        var minuend = await ReadScaledValueAsync(
            binding.Address,
            binding.DataType,
            binding.Rule,
            $"{valueRole}被减数",
            stringLength,
            cancellationToken);
        if (!minuend.IsSuccess)
        {
            return PlcServiceResult<string>.Fail(minuend.Message);
        }

        var subtrahend = await ReadScaledValueAsync(
            binding.SubtrahendAddress,
            binding.DataType,
            binding.Rule,
            $"{valueRole}减数",
            stringLength,
            cancellationToken);
        if (!subtrahend.IsSuccess)
        {
            return PlcServiceResult<string>.Fail(subtrahend.Message);
        }

        // 差值为负说明焊后小于焊前，是真实测量结果，照实输出以便现场发现偏移量配反等问题。
        return PlcServiceResult<string>.Success(
            FormatNumericValue(minuend.Value - subtrahend.Value, binding.DecimalPlaces));
    }

    /// <summary>
    /// 读取单个地址并按规则缩放，返回未格式化的数值，供计算式相减使用。
    /// </summary>
    private async Task<PlcServiceResult<decimal>> ReadScaledValueAsync(
        string? address,
        string? dataType,
        int rule,
        string valueRole,
        int stringLength,
        CancellationToken cancellationToken)
    {
        var plcAddress = address?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(plcAddress))
        {
            return PlcServiceResult<decimal>.Fail($"{valueRole}地址不能为空。");
        }

        return NormalizeDataType(dataType) switch
        {
            AppConstants.PlcDataTypes.Int32 => ToScaledResult(
                await _plcCommunicationService.ReadInt32Async(plcAddress, cancellationToken),
                plcAddress,
                valueRole,
                value => ApplyScaleRule(value, rule)),

            AppConstants.PlcDataTypes.Float => ToScaledResult(
                await _plcCommunicationService.ReadFloatAsync(plcAddress, cancellationToken),
                plcAddress,
                valueRole,
                value => ApplyScaleRule(Convert.ToDecimal(value, CultureInfo.InvariantCulture), rule)),

            AppConstants.PlcDataTypes.String => ToParsedStringResult(
                await _plcCommunicationService.ReadStringAsync(plcAddress, (ushort)Math.Max(1, rule > 0 ? rule : stringLength), cancellationToken),
                plcAddress,
                valueRole),

            _ => ToScaledResult(
                await _plcCommunicationService.ReadInt16Async(plcAddress, cancellationToken),
                plcAddress,
                valueRole,
                value => ApplyScaleRule(value, rule))
        };
    }

    private static PlcServiceResult<decimal> ToScaledResult<T>(
        PlcServiceResult<T> result,
        string address,
        string valueRole,
        Func<T, decimal> scaler)
    {
        return result.IsSuccess && result.Value is not null
            ? PlcServiceResult<decimal>.Success(scaler(result.Value))
            : PlcServiceResult<decimal>.Fail($"{valueRole}地址“{address}”读取失败：{result.Message}");
    }

    /// <summary>
    /// String 操作数必须是完整数值文本；含单位或非数字字符时视为失败，不参与计算。
    /// String 类型的 rule 表示读取长度而非缩放规则，因此不套用缩放。
    /// </summary>
    private static PlcServiceResult<decimal> ToParsedStringResult(
        PlcServiceResult<string> result,
        string address,
        string valueRole)
    {
        if (!result.IsSuccess || result.Value is null)
        {
            return PlcServiceResult<decimal>.Fail($"{valueRole}地址“{address}”读取失败：{result.Message}");
        }

        var text = result.Value.Trim().Trim('\0');
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? PlcServiceResult<decimal>.Success(value)
            : PlcServiceResult<decimal>.Fail($"{valueRole}地址“{address}”的值“{text}”不是有效数值，无法参与计算。");
    }

    /// <summary>
    /// 只做规则缩放，不做小数位格式化。与 ApplyDisplayRule 的缩放口径保持一致。
    /// </summary>
    private static decimal ApplyScaleRule(decimal rawValue, int rule)
    {
        return rule switch
        {
            1 => rawValue / 10m,
            2 => rawValue / 100m,
            3 => rawValue / 1000m,
            _ => rawValue
        };
    }

    /// <summary>
    /// 读取已经解析好的 PLC 地址，并按数据类型、规则和小数位转换成界面可显示的文本。
    /// </summary>
    public async Task<PlcServiceResult<string>> ReadResolvedAddressTextAsync(
        string? address,
        string? dataType,
        int rule = 0,
        string valueRole = "PLC地址",
        int stringLength = 32,
        CancellationToken cancellationToken = default,
        int? decimalPlaces = null)
    {
        var plcAddress = address?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(plcAddress))
        {
            return PlcServiceResult<string>.Fail($"{valueRole}地址不能为空。");
        }

        return NormalizeDataType(dataType) switch
        {
            AppConstants.PlcDataTypes.Bool => ToTextResult(
                await _plcCommunicationService.ReadBoolAsync(plcAddress, cancellationToken),
                plcAddress,
                valueRole,
                value => ApplyDisplayRule(value ? 1m : 0m, rule, null)),

            AppConstants.PlcDataTypes.Int32 => ToTextResult(
                await _plcCommunicationService.ReadInt32Async(plcAddress, cancellationToken),
                plcAddress,
                valueRole,
                value => ApplyDisplayRule(value, rule, decimalPlaces)),

            AppConstants.PlcDataTypes.Float => ToTextResult(
                await _plcCommunicationService.ReadFloatAsync(plcAddress, cancellationToken),
                plcAddress,
                valueRole,
                value => ApplyDisplayRule(Convert.ToDecimal(value, CultureInfo.InvariantCulture), rule, decimalPlaces)),

            AppConstants.PlcDataTypes.String => ToTextResult(
                await _plcCommunicationService.ReadStringAsync(plcAddress, (ushort)Math.Max(1, rule > 0 ? rule : stringLength), cancellationToken),
                plcAddress,
                valueRole,
                value => NormalizeStringValue(value, decimalPlaces)),

            _ => ToTextResult(
                await _plcCommunicationService.ReadInt16Async(plcAddress, cancellationToken),
                plcAddress,
                valueRole,
                value => ApplyDisplayRule(value, rule, decimalPlaces))
        };
    }

    private static PlcServiceResult<string> ToTextResult<T>(
        PlcServiceResult<T> result,
        string address,
        string valueRole,
        Func<T, string> formatter)
    {
        return result.IsSuccess && result.Value is not null
            ? PlcServiceResult<string>.Success(formatter(result.Value))
            : PlcServiceResult<string>.Fail($"{valueRole}地址“{address}”读取失败：{result.Message}");
    }

    /// <summary>
    /// 先应用旧规则缩放，再应用新小数位格式，保证历史配置行为不变。
    /// </summary>
    private string ApplyDisplayRule(decimal rawValue, int rule, int? decimalPlaces)
    {
        if (rule == 4)
        {
            return FormatResult(rawValue.ToString("0", CultureInfo.InvariantCulture));
        }

        var value = rule switch
        {
            1 => rawValue / 10m,
            2 => rawValue / 100m,
            3 => rawValue / 1000m,
            _ => rawValue
        };

        return FormatNumericValue(value, decimalPlaces);
    }

    /// <summary>
    /// 数值类型与 String 类型使用同一套小数位处理：小数位来自偏移量表达式始终生效，
    /// 全局设置只决定截断还是四舍五入，保证界面与 MES 上传、报表口径一致。
    /// </summary>
    private string FormatNumericValue(decimal value, int? decimalPlaces)
    {
        if (decimalPlaces is not >= 0)
        {
            return value % 1m == 0
                ? value.ToString("0", CultureInfo.InvariantCulture)
                : value.ToString(CultureInfo.InvariantCulture);
        }

        var settings = _settingsService.Get();
        // 关闭全局数值处理时按四舍五入，与本次修复前的 F{n} 行为等价。
        var mode = settings.EnablePlcStringNumericFormatting ?? true
            ? settings.PlcStringNumericFormatMode
            : AppConstants.PlcStringNumericFormatModes.Round;

        return PlcStringNumericFormatter.Format(
            value.ToString(CultureInfo.InvariantCulture),
            decimalPlaces,
            enabled: true,
            mode);
    }

    /// <summary>
    /// String 默认只清理 PLC 填充字符；配置 _小数位 且系统设置启用后，才按全局模式处理数值字符串。
    /// </summary>
    private string NormalizeStringValue(string? value, int? decimalPlaces)
    {
        var settings = _settingsService.Get();
        return PlcStringNumericFormatter.Format(
            value,
            decimalPlaces,
            settings.EnablePlcStringNumericFormatting ?? true,
            settings.PlcStringNumericFormatMode);
    }

    private static string FormatResult(string? value)
        => TestResultRules.ToDisplayText(value);

    private static string NormalizeDataType(string? dataType)
    {
        return dataType?.Trim().ToUpperInvariant() switch
        {
            "B" or "BOOL" => AppConstants.PlcDataTypes.Bool,
            "I" or "INT32" or "DINT" => AppConstants.PlcDataTypes.Int32,
            "F" or "FLOAT" or "REAL" => AppConstants.PlcDataTypes.Float,
            "S" or "STRING" => AppConstants.PlcDataTypes.String,
            "H" or "INT16" or "SHORT" or "INT" => AppConstants.PlcDataTypes.Int16,
            _ => AppConstants.PlcDataTypes.Int16
        };
    }

}
