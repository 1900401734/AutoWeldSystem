using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Decides whether the process-parameter payload should carry the IsTest field.
/// </summary>
public static class ProcessParameterIsTestRules
{
    /// <summary>
    /// Resolves the nullable numeric IsTest value for JSON serialization.
    /// </summary>
    /// <param name="recordIsTest">Product-level test-weld flag saved on the weld point record.</param>
    /// <param name="showTestFlagInHistory">Global setting that enables test-weld display and upload.</param>
    /// <param name="processParameterDeviceType">Configured process-parameter device type.</param>
    /// <returns>Null when the field should be omitted; otherwise 0=false and 1=true.</returns>
    public static int? Resolve(bool recordIsTest, bool showTestFlagInHistory, string? processParameterDeviceType)
    {
        if (!showTestFlagInHistory)
        {
            return null;
        }

        if (string.Equals(
            processParameterDeviceType?.Trim(),
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
            StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return recordIsTest ? 1 : 0;
    }
}
