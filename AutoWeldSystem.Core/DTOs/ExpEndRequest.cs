namespace AutoWeldSystem.Core.DTOs;

public class ExpEndRequest
{
    public string ExpStartId { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public string SN { get; set; } = string.Empty;

    public string ProcessNo { get; set; } = string.Empty;

    public string EndTs { get; set; } = string.Empty;

    public string EndExperID { get; set; } = string.Empty;

    public string ExpStatus { get; set; } = "1";

    public decimal WorkHour { get; set; }

    public int ExpQty { get; set; }

    public int QualifyNumber { get; set; }

    public int FailureNumber { get; set; }
}
