using System.ComponentModel;

namespace AutoWeldSystem.Core.Enums;

public enum ApiCode
{
    [Description("设备状态上报")]
    common_001 = 1,
    [Description("开工上报")]
    common_002,
    [Description("完工上报")]
    common_003,
    [Description("设置设备编号")]
    common_004,
    [Description("工单状态变更")]
    common_005,
    [Description("新增程序")]
    common_006,
    [Description("更新程序")]
    common_007,
    [Description("下载程序")]
    common_008,
    [Description("采集参数上传")]
    EMWeldDetail_001
}
