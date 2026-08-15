using System.Text.Json.Serialization;

namespace AutoWeldSystem.Core.DTOs.Upload;

/// <summary>
/// MES process-parameter upload row.
/// One row represents one collected weld point or inspection point under a product.
/// </summary>
public sealed class ProcessParameterUploadItem
{
    /// <summary>
    /// Start-report task id. Online tasks use MES ExpStartId; offline tasks use the local task id before MES returns one.
    /// </summary>
    public string ExpStartId { get; set; } = string.Empty;

    /// <summary>
    /// Device id.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Work order or routing card number.
    /// </summary>
    public string SN { get; set; } = string.Empty;

    /// <summary>
    /// Process number.
    /// </summary>
    public string ProcessNo { get; set; } = string.Empty;

    /// <summary>
    /// Product number read from PLC.
    /// </summary>
    public string ProductNo { get; set; } = string.Empty;

    /// <summary>
    /// Weld point number. Whole-piece inspection devices use SideNo instead.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TouchNo { get; set; }

    /// <summary>
    /// Inspection side number for whole-piece inspection devices.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SideNo { get; set; }

    /// <summary>
    /// Device family marker. Electromagnetic systems use EM; whole-piece welding systems use WP.
    /// Whole-piece inspection requests omit this field.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }

    /// <summary>
    /// Photograph result for the current side of a whole-piece inspection.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Result { get; set; }

    /// <summary>
    /// Actual collection time.
    /// </summary>
    public string Ts { get; set; } = string.Empty;

    /// <summary>
    /// Product-level test-weld flag for MES. Null means the field is intentionally omitted; 0=false, 1=true.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? IsTest { get; set; }

    /// <summary>
    /// MES dynamic process fields. Field names come from scheme detail MesFieldName and are flattened into this JSON object.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?> DynamicFields { get; } = new(StringComparer.OrdinalIgnoreCase);
}
