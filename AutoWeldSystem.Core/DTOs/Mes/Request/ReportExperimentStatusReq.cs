using System.ComponentModel;

namespace AutoWeldSystem.Core.DTOs.Mes.Request;

public class ReportExperimentStatusReq
{
    [DisplayName("工单任务Id")]
    public string ExpStartId { get; set; } = string.Empty;

    [DisplayName("设备编号")]
    public string DeviceId { get; set; } = string.Empty;

    [DisplayName("工单状态")]
    public string ExpStatus { get; set; } = string.Empty;   // -1：异常， 0：开工，1：完工，2：暂停

    [DisplayName("采集时间")]
    public string Ts { get; set; } = string.Empty;  // 格式：yyyy-MM-dd HH:mm:ss
}
