namespace AutoWeldSystem.Core.DTOs;

/// <summary>
/// 生产流程日志。
/// 用于记录 PLC 信号监听、采集、保存、上传、转发等关键业务步骤，便于现场复盘完整生产链路。
/// </summary>
public sealed class ProductionFlowLogEntry
{
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime OccurredTime { get; set; } = DateTime.Now;

    public string Level { get; set; } = "Info";

    public string Step { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public int StationNo { get; set; }

    public string WorkOrderId { get; set; } = string.Empty;

    public string ProductNo { get; set; } = string.Empty;

    public string ProgramId { get; set; } = string.Empty;

    public string PlcSignal { get; set; } = string.Empty;

    public string PlcAddress { get; set; } = string.Empty;

    public long? DurationMilliseconds { get; set; }
}
