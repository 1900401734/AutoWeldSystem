using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// Weld point collection record.
/// One row represents one weld start/end cycle for a specific ProductNumber and TouchNo.
/// </summary>
/// <remarks>
/// 自然键唯一索引：程序自算产品编号依赖“取库中最大值+1”，没有索引兜底时撞号会被静默跳过丢件，
/// 因此在数据库层强制唯一。升级前需确认旧库无重复行，否则 CodeFirst 建索引会失败。
/// </remarks>
[SugarTable("Biz_WeldPointRecord", TableDescription = "焊点采集记录表")]
[SugarIndex("unique_weldpointrecord_naturalkey",
    nameof(TaskId), OrderByType.Asc,
    nameof(StationNo), OrderByType.Asc,
    nameof(ProductNo), OrderByType.Asc,
    nameof(TouchNo), OrderByType.Asc,
    true)]
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
    /// Weld point number under one ProductNumber.
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

    /// <summary>
    /// 产品级软删标记：已删除产品不再进入上传、报表和产量统计，但保留行以便撤销且不回收产品编号。
    /// 与 <see cref="IsTest"/> 同模式，冗余存到每条焊点行，读侧用 Any 聚合。
    /// </summary>
    [SugarColumn(ColumnDescription = "产品已删除")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 产品级重焊/重测预约标记：下一次采集覆盖该产品而非追加新品，覆盖后自动清除。
    /// 同任务同工位最多只有一件待覆盖产品。
    /// </summary>
    [SugarColumn(ColumnDescription = "待重焊覆盖")]
    public bool IsReweldPending { get; set; }

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

    /// <summary>
    /// PLC product-level result. This value is independent from the weld-point <see cref="TestResult"/>.
    /// Legacy rows can keep this column null and fall back to RawDataJson when displayed.
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = true, ColumnDescription = "产品结果")]
    public string? ProductResult { get; set; }

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "操作员工号")]
    public string? OperatorNo { get; set; }

    /// <summary>
    /// Whether all weld points for this ProductNumber have been collected.
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
