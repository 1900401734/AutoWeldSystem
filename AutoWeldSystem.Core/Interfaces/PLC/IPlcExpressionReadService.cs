using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Plc;

namespace AutoWeldSystem.Core.Interfaces.PLC;

/// <summary>
/// 统一处理 PLC 偏移表达式解析和实际地址读取。
/// 生产采集、实时预览和调试窗口都通过该服务复用同一套读取规则。
/// </summary>
public interface IPlcExpressionReadService
{
    PlcExpressionBinding Resolve(string? baseAddress, int contextOffset, string? expressionText);

    bool TryResolve(
        string? baseAddress,
        int contextOffset,
        string? expressionText,
        out PlcExpressionBinding binding,
        out string message);

    Task<PlcServiceResult<string>> ReadExpressionTextAsync(
        string? baseAddress,
        int contextOffset,
        string? expressionText,
        string valueRole = "PLC地址",
        int stringLength = 32,
        CancellationToken cancellationToken = default);

    Task<PlcServiceResult<string>> ReadResolvedAddressTextAsync(
        string? address,
        string? dataType,
        int rule = 0,
        string valueRole = "PLC地址",
        int stringLength = 32,
        CancellationToken cancellationToken = default);
}
