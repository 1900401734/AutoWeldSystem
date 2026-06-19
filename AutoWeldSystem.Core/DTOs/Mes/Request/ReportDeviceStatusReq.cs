
namespace AutoWeldSystem.Core.DTOs.Mes.Request;

public class ReportDeviceStatusReq
{
    /// <summary>
    /// 设备编号
    /// </summary>
    public string DeviceId { get; set; }= string.Empty;
    /// <summary>
    /// PLC设备状态：1=运行；2=暂停/空闲；3=停止；4=报警。
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
