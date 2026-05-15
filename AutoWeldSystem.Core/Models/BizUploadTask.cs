using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// Generic upload/outbox task.
/// This keeps MES uploads, report files, program files, and future forwarding tasks in one retry model.
/// </summary>
[SugarTable("Biz_UploadTask", TableDescription = "上传任务表")]
public class BizUploadTask
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "上传类型")]
    public string TaskType { get; set; } = ProductionConstants.UploadTaskTypes.ProcessParameter;

    [SugarColumn(Length = 50, ColumnDescription = "目标平台")]
    public string Target { get; set; } = ProductionConstants.UploadTargets.Mes;

    [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "业务ID")]
    public string? BusinessId { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "焊接任务ID")]
    public int? WeldTaskId { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "请求内容")]
    public string? PayloadJson { get; set; }

    [SugarColumn(Length = 500, IsNullable = true, ColumnDescription = "文件路径")]
    public string? FilePath { get; set; }

    [SugarColumn(Length = 20, ColumnDescription = "上传状态")]
    public string Status { get; set; } = ProductionConstants.UploadStatuses.Pending;

    [SugarColumn(ColumnDescription = "重试次数")]
    public int RetryCount { get; set; }

    [SugarColumn(ColumnDescription = "最大重试次数")]
    public int MaxRetryCount { get; set; } = 3;

    [SugarColumn(IsNullable = true, ColumnDescription = "下次重试时间")]
    public DateTime? NextRetryTime { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "上次尝试时间")]
    public DateTime? LastAttemptTime { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "完成时间")]
    public DateTime? CompletedTime { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "处理消息")]
    public string? Message { get; set; }

    [SugarColumn(ColumnDescription = "创建时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}
