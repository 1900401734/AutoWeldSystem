using System.ComponentModel;

namespace AutoWeldSystem.Core.DTOs.Mes.Request;

public class ExperimentEndReq
{
    [DisplayName("任务Id")]
    public string ExpStartId { get; set; } = string.Empty;  // 开工上报接口返回的Id

    [DisplayName("设备编号")]
    public string DeviceId { get; set; } = string.Empty;

    [DisplayName("流转卡号/工单号")]
    public string SN { get; set; } = string.Empty;

    [DisplayName("工序号")]
    public string ProcessNo { get; set; } = string.Empty;

    [DisplayName("结束时间")]
    public string EndTs { get; set; } = string.Empty;

    [DisplayName("结束人员")]
    public string EndExperID { get; set; } = string.Empty;  // 与开工人员一致

    [DisplayName("工单状态")]  
    public string ExpStatus { get; set; } = "1";    // -1：异常， 0：开工，1：完工，2：暂停

    [DisplayName("实际工作时长")]
    public decimal WorkHour { get; set; }   // 以小时为单位

    [DisplayName("实际数量")]
    public int ExpQty { get; set; }

    [DisplayName("合格数量")]
    public int QualifyNumber { get; set; }

    [DisplayName("失效数量")]
    public int FailureNumber { get; set; }
}
