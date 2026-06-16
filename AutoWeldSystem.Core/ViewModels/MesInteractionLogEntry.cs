namespace AutoWeldSystem.Core.ViewModels;

/// <summary>
/// MES接口交互日志。
/// </summary>
public sealed class MesInteractionLogEntry
{
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");

    public string Purpose { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string RequestBody { get; set; } = string.Empty;

    public string ResponseBody { get; set; } = string.Empty;

    public int? HttpStatusCode { get; set; }

    public string MesStatus { get; set; } = string.Empty;

    public string MesMessage { get; set; } = string.Empty;

    public bool IsSuccess { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public DateTime SendTime { get; set; }

    public DateTime ReceiveTime { get; set; }

    public long DurationMilliseconds { get; set; }
}
