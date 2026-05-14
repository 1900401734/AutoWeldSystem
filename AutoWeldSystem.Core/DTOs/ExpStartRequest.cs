using System.ComponentModel;

namespace AutoWeldSystem.Core.DTOs
{
    public class ExpStartRequest
    {
        public string Id { get; set; } = string.Empty;

        [DisplayName("设备编号")]
        public string DeviceId { get; set; } = string.Empty;

        [DisplayName("流转卡号")]
        public string SN { get; set; } = string.Empty;

        public string ProductNum { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string DrawingNo { get; set; } = string.Empty;

        public string Batch { get; set; } = string.Empty;

        public int Qty { get; set; }

        public string ProcessNo { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public int ExpQty { get; set; }

        public string StartTs { get; set; } = string.Empty;

        public string StartExperID { get; set; } = string.Empty;

        public string ExpStatus { get; set; } = "0";

        public string ProgramName { get; set; } = string.Empty;

        public string PramaterActual { get; set; } = "{}";
    }
}
