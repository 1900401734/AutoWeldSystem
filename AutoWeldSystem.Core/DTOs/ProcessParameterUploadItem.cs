namespace AutoWeldSystem.Core.DTOs;

/// <summary>
/// MES process parameter upload row.
/// One row represents one collected weld point under a ProductNo.
/// </summary>
public sealed class ProcessParameterUploadItem
{
    public string ExpStartId { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public string SN { get; set; } = string.Empty;

    public string ProcessNo { get; set; } = string.Empty;

    public string ProductNo { get; set; } = string.Empty;

    public string TouchNo { get; set; } = string.Empty;

    public string MaxElectric { get; set; } = string.Empty;

    public string MaxVoltage { get; set; } = string.Empty;

    public string ValidPower { get; set; } = string.Empty;

    public string Displacement { get; set; } = string.Empty;

    public string WeldTs { get; set; } = string.Empty;

    public string Type { get; set; } = "EM";

    public string Ts { get; set; } = string.Empty;
}
