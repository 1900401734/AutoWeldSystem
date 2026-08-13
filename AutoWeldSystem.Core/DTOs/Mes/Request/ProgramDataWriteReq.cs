namespace AutoWeldSystem.Core.DTOs.Mes.Request;

/// <summary>
/// MES 新增或更新程序接口的请求报文。
/// 该 DTO 只描述远端接口需要的字段，不承载界面保存、本地配方号等辅助信息。
/// </summary>
public sealed class ProgramDataWriteReq
{
    /// <summary>
    /// MES 程序 ID；新增时为空，更新时必须有值。
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 程序名称。
    /// </summary>
    public string ProgramName { get; set; } = string.Empty;

    /// <summary>
    /// 设备编号。
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 程序内容，通常为 JSON 字符串。
    /// </summary>
    public string ProgramContent { get; set; } = string.Empty;

    /// <summary>
    /// 程序类型；0 表示参数字符串，1 表示程序文件。
    /// </summary>
    public string ProgramType { get; set; } = string.Empty;

    /// <summary>
    /// 产品工号。
    /// </summary>
    public string ProductNum { get; set; } = string.Empty;

    /// <summary>
    /// MES 备注；客户接口用该字段区分新增、修改和删除。
    /// </summary>
    public string Remark { get; set; } = string.Empty;
}
