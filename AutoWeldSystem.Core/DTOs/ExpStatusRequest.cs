namespace AutoWeldSystem.Core.DTOs;

public class ExpStatusRequest
{
    public string ExpStartId { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public string ExpStatus { get; set; } = string.Empty;

    public string Ts { get; set; } = string.Empty;
}
