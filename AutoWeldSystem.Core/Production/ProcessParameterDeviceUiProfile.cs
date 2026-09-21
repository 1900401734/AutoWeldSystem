using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// Resolves operator-facing terms from the process-parameter device type.
/// </summary>
public sealed record ProcessParameterDeviceUiProfile(
    string PointName,
    string PointNoHeader,
    string PointResultHeader,
    string PointCountHeader,
    string RecordName)
{
    public static ProcessParameterDeviceUiProfile Resolve(string? deviceType)
    {
        return string.Equals(
            deviceType?.Trim(),
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
            StringComparison.OrdinalIgnoreCase)
            ? new("面", "面号", "拍照结果", "面数量", "检测记录")
            : new("焊点", "焊点号", "焊点结果", "焊点数量", "焊点记录");
    }
}
