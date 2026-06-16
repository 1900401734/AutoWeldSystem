namespace AutoWeldSystem.Core.DTOs.Upload;

/// <summary>
/// MES process parameter upload row.
/// One row represents one collected weld point under a ProductNo.
/// </summary>
public sealed class ProcessParameterUploadItem
{
    /// <summary>
    /// 开工任务ID。在线时MES返回，离线时本地生成。
    /// </summary>
    public string ExpStartId { get; set; } = string.Empty;

    /// <summary>
    /// 设备编号
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 工单号/流转卡号
    /// </summary>
    public string SN { get; set; } = string.Empty;

    /// <summary>
    /// 工序号
    /// </summary>
    public string ProcessNo { get; set; } = string.Empty;

    /// <summary>
    /// 产品编号。在产品周期采集中从PLC读取，
    /// </summary>
    public string ProductNo { get; set; } = string.Empty;

    /// <summary>
    /// 焊点编号。在产品周期采集中从PLC读取，
    /// </summary>
    public string TouchNo { get; set; } = string.Empty;

    /// <summary>
    /// 类型。用于区分接触系统、整件、电磁设备的点焊参数。字典值：[TS, 接触系统], [WP, 整件]，[EM, 电磁]
    /// </summary>
    public string Type { get; set; } = "EM";

    /// <summary>
    /// 采集时间
    /// </summary>
    public string Ts { get; set; } = string.Empty;

    /// <summary>
    /// True when the operator marked the whole product as a local test weld part before upload.
    /// </summary>
    public bool IsTest { get; set; }
}
