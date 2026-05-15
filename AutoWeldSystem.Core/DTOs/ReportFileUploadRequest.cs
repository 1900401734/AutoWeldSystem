namespace AutoWeldSystem.Core.DTOs;

/// <summary>
/// MES 报告文件上传请求。
/// 接口为 multipart/form-data，文件内容通过 FilePath 读取。
/// </summary>
public sealed class ReportFileUploadRequest
{
    public string ExpStartId { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public string SN { get; set; } = string.Empty;

    public string ProcessNo { get; set; } = string.Empty;

    public int FileType { get; set; }

    public string FilePath { get; set; } = string.Empty;
}
