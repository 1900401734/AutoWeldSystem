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
        if (string.IsNullOrWhiteSpace(baseAddress))
        {
            throw new FormatException("基地址不能为空。");
        }

        if (string.IsNullOrWhiteSpace(expressionText))
        {
            throw new FormatException("偏移表达式不能为空。");
        }

        var expression = PlcOffsetExpression.Parse(expressionText);
        return new PlcExpressionBinding(
            expression.ResolveAddress(baseAddress, contextOffset),
            expression.DataType,
            expression.Rule,
            expressionText.Trim(),
            expression.DecimalPlaces);
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
    /// </summary>
    public async Task<PlcServiceResult<string>> ReadBindingTextAsync(
        PlcExpressionBinding binding,
        string valueRole = "PLC地址",
        int stringLength = 32,
        CancellationToken cancellationToken = default)
    {
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
    private static string ApplyDisplayRule(decimal rawValue, int rule, int? decimalPlaces)
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

    private static string FormatNumericValue(decimal value, int? decimalPlaces)
    {
        if (decimalPlaces is >= 0)
        {
            return value.ToString($"F{decimalPlaces.Value}", CultureInfo.InvariantCulture);
        }

        return value % 1m == 0
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
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
