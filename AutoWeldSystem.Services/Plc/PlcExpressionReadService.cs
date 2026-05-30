using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Plc;
using System.Globalization;

namespace AutoWeldSystem.Services.Plc;

/// <summary>
/// PLC 表达式读取服务。
/// 它把“表达式解析、最终地址计算、PLC 读取、显示规则转换”集中在一个地方。
/// </summary>
public sealed class PlcExpressionReadService : IPlcExpressionReadService
{
    private readonly IPlcCommunicationService _plcCommunicationService;

    public PlcExpressionReadService(IPlcCommunicationService plcCommunicationService)
    {
        _plcCommunicationService = plcCommunicationService;
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
            expressionText.Trim());
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

        return await ReadResolvedAddressTextAsync(
            binding.Address,
            binding.DataType,
            binding.Rule,
            valueRole,
            stringLength,
            cancellationToken);
    }

    /// <summary>
    /// 读取已经解析好的 PLC 地址，并按数据类型和规则转换成界面可显示的文本。
    /// </summary>
    public async Task<PlcServiceResult<string>> ReadResolvedAddressTextAsync(
        string? address,
        string? dataType,
        int rule = 0,
        string valueRole = "PLC地址",
        int stringLength = 32,
        CancellationToken cancellationToken = default)
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
                value => ApplyDisplayRule(value ? 1m : 0m, rule)),

            AppConstants.PlcDataTypes.Int32 => ToTextResult(
                await _plcCommunicationService.ReadInt32Async(plcAddress, cancellationToken),
                plcAddress,
                valueRole,
                value => ApplyDisplayRule(value, rule)),

            AppConstants.PlcDataTypes.Float => ToTextResult(
                await _plcCommunicationService.ReadFloatAsync(plcAddress, cancellationToken),
                plcAddress,
                valueRole,
                value => ApplyDisplayRule(Convert.ToDecimal(value, CultureInfo.InvariantCulture), rule)),

            AppConstants.PlcDataTypes.String => ToTextResult(
                await _plcCommunicationService.ReadStringAsync(plcAddress, (ushort)Math.Max(1, stringLength), cancellationToken),
                plcAddress,
                valueRole,
                NormalizePlcText),

            _ => ToTextResult(
                await _plcCommunicationService.ReadInt16Async(plcAddress, cancellationToken),
                plcAddress,
                valueRole,
                value => ApplyDisplayRule(value, rule))
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

    private static string ApplyDisplayRule(decimal rawValue, int rule)
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

        return value % 1m == 0
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatResult(string? value)
    {
        var result = NormalizePlcText(value);
        if (string.IsNullOrWhiteSpace(result))
        {
            return ProductionConstants.TestResults.Unknown;
        }

        return string.Equals(result, ProductionConstants.TestResults.OkRawValue, StringComparison.Ordinal)
            || string.Equals(result, ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase)
            ? ProductionConstants.TestResults.Ok
            : ProductionConstants.TestResults.Ng;
    }

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

    private static string NormalizePlcText(string? value)
    {
        return value?.Trim().Trim('\0') ?? string.Empty;
    }
}
