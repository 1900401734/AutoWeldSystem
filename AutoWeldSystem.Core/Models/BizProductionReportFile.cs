using AutoWeldSystem.Core.Constants;
using SqlSugar;

namespace AutoWeldSystem.Core.Models;

/// <summary>
/// Local production report file record.
/// It tracks generated Excel/CSV files and their MES upload state.
/// </summary>
[SugarTable("Biz_ProductionReportFile", TableDescription = "生产报告文件表")]
public class BizProductionReportFile
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(ColumnDescription = "焊接任务ID")]
    public int TaskId { get; set; }

    [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "开工任务ID")]
    public string? ExpStartId { get; set; }

    [SugarColumn(Length = 50, ColumnDescription = "设备编号")]
    public string DeviceId { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "工单号")]
    public string SN { get; set; } = string.Empty;

    [SugarColumn(Length = 50, ColumnDescription = "工序号")]
    public string ProcessNo { get; set; } = string.Empty;

    [SugarColumn(Length = 10, ColumnDescription = "文件代码")]
    public string FileCode { get; set; } = ProductionConstants.ReportFileCodes.Spreadsheet;

    [SugarColumn(ColumnDescription = "MES文件类型")]
    public int MesFileType { get; set; } = ProductionConstants.MesFileTypes.ReportFile;

    [SugarColumn(Length = 20, ColumnDescription = "文件格式")]
    public string FileFormat { get; set; } = "Excel";

    [SugarColumn(Length = 260, ColumnDescription = "文件名")]
    public string FileName { get; set; } = string.Empty;

    [SugarColumn(Length = 500, ColumnDescription = "文件路径")]
    public string FilePath { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "三位流水号")]
    public int SequenceNo { get; set; } = 1;

    [SugarColumn(Length = 20, ColumnDescription = "上传状态")]
    public string UploadStatus { get; set; } = ProductionConstants.UploadStatuses.Pending;

    [SugarColumn(IsNullable = true, ColumnDescription = "上传时间")]
    public DateTime? UploadTime { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true, ColumnDescription = "上传消息")]
    public string? UploadMessage { get; set; }

    [SugarColumn(ColumnDescription = "创建时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}
