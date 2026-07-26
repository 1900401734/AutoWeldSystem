namespace AutoWeldSystem.Core.ViewModels;

/// <summary>
/// 中心服务器交互日志。
/// InteractionType 保存原始值 telemetry / product-report，与中心服务器 API 路径段一致，界面层再映射中文显示。
/// </summary>
public sealed class CenterInteractionLogEntry
{
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");

    public string InteractionType { get; set; } = string.Empty;

    public string Method { get; set; } = "POST";

    public string Url { get; set; } = string.Empty;

    public string RequestBody { get; set; } = string.Empty;

    public string ResponseBody { get; set; } = string.Empty;

    /// <summary>请求未发出（地址错误、连接失败）时为 null。</summary>
    public int? HttpStatusCode { get; set; }

    public string AckMessage { get; set; } = string.Empty;

    public DateTime? ServerTime { get; set; }

    public bool IsSuccess { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public DateTime SendTime { get; set; }

    public DateTime ReceiveTime { get; set; }

    public long DurationMilliseconds { get; set; }
}
