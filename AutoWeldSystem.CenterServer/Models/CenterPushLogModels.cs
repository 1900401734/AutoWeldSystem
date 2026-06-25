using System.Text.Json.Nodes;

namespace AutoWeldSystem.CenterServer.Models;

/// <summary>
/// 中心服务器设备推送日志。
/// 每条记录对应设备端一次推送请求在某个工位维度的处理结果。
/// </summary>
public sealed class CenterPushLogRecord
{
    public DateTime ReceivedAt { get; set; } = DateTime.Now;
    public string RequestType { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty;
    public int StationNo { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
    public JsonNode? Payload { get; set; }
}

/// <summary>
/// 看板日志查询条件。
/// </summary>
public sealed class CenterPushLogQuery
{
    public string? DeviceKeyword { get; set; }
    public int? StationNo { get; set; }
    public string? RequestType { get; set; }
    public string? Keyword { get; set; }
    public bool? Success { get; set; }
    public int Limit { get; set; } = 200;
}
