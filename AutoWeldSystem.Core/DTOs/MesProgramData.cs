namespace AutoWeldSystem.Core.DTOs;

public class MesProgramData
{
    /// <summary>
    /// 程序ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 程序名称
    /// </summary>
    public string ProgramName { get; set; } = string.Empty;

    /// <summary>
    /// 设备编号
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 程序内容
    /// </summary>
    public string ProgramContent { get; set; } = string.Empty;

    /// <summary>
    /// 程序类型
    /// </summary>
    public string ProgramType { get; set; } = string.Empty;

    /// <summary>
    /// 产品工号
    /// </summary>
    public string ProductNum { get; set; } = string.Empty;

    /// <summary>
    /// 程序文件
    /// </summary>
    public string ProgramFile { get; set; } = string.Empty;

    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; } = string.Empty;
}
