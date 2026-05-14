
namespace AutoWeldSystem.Core.DTOs;

public class ReportDeviceStatusRequest
{
    /// <summary>
    /// 设备编号
    /// </summary>
    public string DeviceId { get; set; }= string.Empty;
    /// <summary>
    /// 设备状态 0=停机；1=开机；4=异常；5=异常恢复；6=程序执行开始；7=程序执行结束；
    /// </summary>
    public string DevStatus { get; set; }= string.Empty;
    /// <summary>
    /// 采集时间，格式：yyyy-MM-dd HH:mm:ss
    /// </summary>
    public string Ts { get; set; }= string.Empty;
    /// <summary>
    /// 备注说明，发生故障时描述故障原因
    /// </summary>
    public string Remark { get; set; }= string.Empty;
}
