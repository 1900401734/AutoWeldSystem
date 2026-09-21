using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Plc;

namespace AutoWeldSystem.Core.Interfaces.PLC;

/// <summary>
/// 统一处理 PLC 偏移表达式解析和实际地址读取。
/// 生产采集、实时预览和调试窗口都通过该服务复用同一套读取规则。
/// </summary>
public interface IPlcExpressionReadService
{
    /// <summary>
    /// 解析偏移表达式，并按基地址和上下文偏移计算最终 PLC 地址。
    /// </summary>
    PlcExpressionBinding Resolve(string? baseAddress, int contextOffset, string? expressionText);

    /// <summary>
    /// 尝试解析偏移表达式；失败时返回错误消息，不抛出异常。
    /// </summary>
    bool TryResolve(
        string? baseAddress,
        int contextOffset,
        string? expressionText,
        out PlcExpressionBinding binding,
        out string message);

    /// <summary>
    /// 先解析偏移表达式，再读取最终 PLC 地址文本。
    /// </summary>
    Task<PlcServiceResult<string>> ReadExpressionTextAsync(
        string? baseAddress,
        int contextOffset,
        string? expressionText,
        string valueRole = "PLC地址",
        int stringLength = 32,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取已解析的 PLC 表达式绑定，并统一应用数据类型、规则和小数位格式。
    /// </summary>
    Task<PlcServiceResult<string>> ReadBindingTextAsync(
        PlcExpressionBinding binding,
        string valueRole = "PLC地址",
        int stringLength = 32,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取已解析好的 PLC 地址，并按数据类型、规则和小数位转换成界面可显示文本。
    /// </summary>
    Task<PlcServiceResult<string>> ReadResolvedAddressTextAsync(
        string? address,
        string? dataType,
        int rule = 0,
        string valueRole = "PLC地址",
        int stringLength = 32,
        CancellationToken cancellationToken = default,
        int? decimalPlaces = null);
}
