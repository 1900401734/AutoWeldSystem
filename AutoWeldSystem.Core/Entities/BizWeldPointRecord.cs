using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// Weld point collection record.
/// One row represents one weld start/end cycle for a specific ProductNo and TouchNo.
/// </summary>
[SugarTable("Biz_WeldPointRecord", TableDescription = "焊点采集记录表")]
public class BizWeldPointRecord
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    #region MES字段

    /// <summary>
    /// MES start task id returned by ExpStart.
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "开工任务ID")]
    public string ExpStartId { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "设备编号")]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Work order number / flow card number.
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "工单号/流转卡号")]
    public string SN { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "工序号")]
    public string ProcessNo { get; set; } = string.Empty;

    /// <summary>
    /// Unique product number under the current work order.
    /// In product-cycle collection this value is read from PLC; legacy weld-point collection can still generate it locally.
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "产品编号")]
    public string ProductNo { get; set; } = string.Empty;

    /// <summary>
    /// Weld point number under one ProductNo.
    /// </summary>
    [SugarColumn(Length = 50, ColumnDescription = "焊点编号")]
    public string TouchNo { get; set; } = string.Empty;

    /// <summary>
    /// 用于区分接触系统、整件、电磁设备的点焊参数。字典值：[TS, 接触系统], [WP, 整件]，[EM, 电磁]
    /// </summary>
    [SugarColumn(Length = 20, ColumnDescription = "分类")]
    public string Type { get; set; } = "EM";

    /// <summary>
    /// 格式：yyyy-MM-dd HH:mm:ss
    /// </summary>
    [SugarColumn(ColumnDescription = "采集时间")]
    public DateTime Ts { get; set; } = DateTime.Now;

    /// <summary>
    /// Whether this completed product is a local test weld part.
    /// The flag is stored on every weld point row so process-parameter makeup upload can read it directly.
    /// </summary>
    [SugarColumn(ColumnDescription = "是否试焊件")]
    public bool IsTest { get; set; }

    #endregion

    [SugarColumn(ColumnDescription = "工位号")]
    public int StationNo { get; set; }

    /// <summary>
    /// Local weld task id.
    /// </summary>
    [SugarColumn(ColumnDescription = "焊接任务ID")]
    public int TaskId { get; set; }

    [SugarColumn(ColumnDescription = "采集序号")]
    public int SequenceNo { get; set; }

    [SugarColumn(Length = 20, ColumnDescription = "测试结果")]
    public string TestResult { get; set; } = ProductionConstants.TestResults.Unknown;

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "操作员工号")]
    public string? OperatorNo { get; set; }

    /// <summary>
    /// Whether all weld points for this ProductNo have been collected.
    /// </summary>
    [SugarColumn(ColumnDescription = "产品是否采集完成")]
    public bool ProductCompleted { get; set; }

    [SugarColumn(Length = 20, ColumnDescription = "上传状态")]
    public string UploadStatus { get; set; } = ProductionConstants.UploadStatuses.Pending;

    [SugarColumn(IsNullable = true, ColumnDescription = "上传时间")]
    public DateTime? UploadTime { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "上传消息")]
    public string? UploadMessage { get; set; }

    [SugarColumn(ColumnDescription = "重试次数")]
    public int RetryCount { get; set; }

    /// <summary>
    /// Raw collection values serialized as JSON for later troubleshooting and dynamic report columns.
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "原始采集JSON")]
    public string? RawDataJson { get; set; }
}
