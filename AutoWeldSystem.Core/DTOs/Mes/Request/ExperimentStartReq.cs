using System.ComponentModel;

namespace AutoWeldSystem.Core.DTOs.Mes.Request
{
    public class ExperimentStartReq
    {
        [DisplayName("任务Id")]
        public string? Id { get; set; } // 联网发送上报开工信息时，不须传值，接口返回任务Id；离线时，设备端自己生成32位GUID并上传

        [DisplayName("设备编号")]
        public string DeviceId { get; set; } = string.Empty;

        [DisplayName("流转卡号")]
        public string SN { get; set; } = string.Empty;

        [DisplayName("产品工号")]
        public string ProductNum { get; set; } = string.Empty;

        [DisplayName("产品名称")]
        public string ProductName { get; set; } = string.Empty;

        [DisplayName("图号")]
        public string DrawingNo { get; set; } = string.Empty;

        [DisplayName("批次")]
        public string Batch { get; set; } = string.Empty;

        [DisplayName("生产数量")]
        public int Qty { get; set; }

        [DisplayName("工序号")]
        public string ProcessNo { get; set; } = string.Empty;

        [DisplayName("工序名称")]
        public string ItemName { get; set; } = string.Empty;

        [DisplayName("实际数量")]
        public int ExpQty { get; set; }

        [DisplayName("开始时间")]
        public string StartTs { get; set; } = string.Empty; // 格式：yyyy-MM-dd HH:mm:ss

        [DisplayName("开始人员")]
        public string StartExperID { get; set; } = string.Empty;

        [DisplayName("工单状态")]
        public string ExpStatus { get; set; } = "0";    // 0-开工，1-完工，2-暂停

        [DisplayName("程序名称")]
        public string? ProgramName { get; set; }

        [DisplayName("工艺参数设定值")]
        public string PramaterActual { get; set; } = "{}";
    }
}
