namespace AutoWeldSystem.Core.DTOs.CenterServer;

/// <summary>
/// One completed product forwarded from an equipment client to the center server.
/// </summary>
public sealed class CenterProductReportRequest
{
    /// <summary>
    /// Stable device id configured on the equipment client.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Human readable device name configured on the equipment client.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Logical system type used by the center dashboard for grouping.
    /// </summary>
    public string SystemType { get; set; } = string.Empty;

    /// <summary>
    /// Station that produced this product.
    /// </summary>
    public int StationNo { get; set; } = 1;

    /// <summary>
    /// Work order number of the product.
    /// </summary>
    public string WorkOrder { get; set; } = string.Empty;

    /// <summary>
    /// Product job number configured in the local task.
    /// </summary>
    public string ProductJobNo { get; set; } = string.Empty;

    /// <summary>
    /// PLC-collected product number.
    /// </summary>
    public string ProductNo { get; set; } = string.Empty;

    /// <summary>
    /// Optional product model.
    /// </summary>
    public string ProductModel { get; set; } = string.Empty;

    /// <summary>
    /// Product-level result resolved by the equipment client.
    /// </summary>
    public string ProductResult { get; set; } = string.Empty;

    /// <summary>
    /// Time when the product was completed on the equipment client.
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Collected point rows that belong to this completed product.
    /// </summary>
    public List<CenterProductReportPointDto> Points { get; set; } = new();
}

/// <summary>
/// One point row inside a completed product report forwarded to the center server.
/// </summary>
public sealed class CenterProductReportPointDto
{
    /// <summary>
    /// Local collection sequence number.
    /// </summary>
    public int SequenceNo { get; set; }

    /// <summary>
    /// Weld point, camera, or inspection point number read from PLC.
    /// </summary>
    public string TouchNo { get; set; } = string.Empty;

    /// <summary>
    /// Point-level result.
    /// </summary>
    public string TestResult { get; set; } = string.Empty;

    /// <summary>
    /// Point collection time.
    /// </summary>
    public DateTime CollectedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Dynamic collected values serialized by the equipment client.
    /// </summary>
    public string RawDataJson { get; set; } = string.Empty;
}
