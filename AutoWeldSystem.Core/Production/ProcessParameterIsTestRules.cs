using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Decides whether the process-parameter payload and the XLSX report should carry the IsTest field.
/// </summary>
public static class ProcessParameterIsTestRules
{
    /// <summary>
    /// Resolves the nullable boolean IsTest value for JSON serialization.
    /// </summary>
    /// <param name="recordIsTest">Product-level test-weld flag saved on the weld point record.</param>
    /// <param name="showTestFlagInHistory">Global setting that enables test-weld display and upload.</param>
    /// <param name="processParameterDeviceType">Configured process-parameter device type.</param>
    /// <returns>Null when the field should be omitted; otherwise the product-level test-weld flag.</returns>
    public static bool? Resolve(bool recordIsTest, bool showTestFlagInHistory, string? processParameterDeviceType)
    {
        return IsEnabled(showTestFlagInHistory, processParameterDeviceType)
            ? recordIsTest
            : null;
    }

    /// <summary>
    /// 判断当前设备是否使用试焊件标志。
    /// 报表列与 MES 过程参数字段共用同一门禁：关闭全局开关表示现场不使用该概念，
    /// 整件检测设备的产品历史本就不显示该标记，报表凭空多一列会被误读成漏采。
    /// </summary>
    /// <param name="showTestFlagInHistory">Global setting that enables test-weld display and upload.</param>
    /// <param name="processParameterDeviceType">Configured process-parameter device type.</param>
    public static bool IsEnabled(bool showTestFlagInHistory, string? processParameterDeviceType)
    {
        return showTestFlagInHistory
            && !string.Equals(
                processParameterDeviceType?.Trim(),
                ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
                StringComparison.OrdinalIgnoreCase);
    }
}
