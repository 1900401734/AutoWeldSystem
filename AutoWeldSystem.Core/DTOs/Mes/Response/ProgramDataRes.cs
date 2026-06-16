namespace AutoWeldSystem.Core.DTOs.Mes.Response;

public class ProgramDataRes
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
    /// 程序内容, JSON字符串
    /// </summary>
    public string ProgramContent { get; set; } = string.Empty;

    /// <summary>
    /// 程序类型，0:字符串
    /// </summary>
    public string ProgramType { get; set; } = string.Empty;

    /// <summary>
    /// 产品工号
    /// </summary>
    public string? ProductNum { get; set; } = string.Empty;

    /// <summary>
    /// 程序文件，base64字符串文件流，客户端需解析存为文件
    /// </summary>
    public string? ProgramFile { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; } = string.Empty;

    /// <summary>
    /// 本地程序维护的配方编号。
    /// </summary>
    public string RecipeCode { get; set; } = string.Empty;
}
