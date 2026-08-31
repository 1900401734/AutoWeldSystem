using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 生产报告文件服务。
/// 负责根据本地采集数据生成报告文件，并记录本地文件状态。
/// </summary>
public interface IProductionReportFileService
{
    BizProductionReportFile GenerateXlsxReport(BizWeldTask task);

    /// <summary>
    /// 判断当前任务是否存在可触发 MES 报表文件上传的有效 ReportEnable 角色。
    /// </summary>
    bool ShouldUploadReportFile(BizWeldTask task);

    /// <summary>
    /// 按上传报表格式导出到指定路径，并在末列附加中文上传状态。
    /// 供数据管理页手动导出使用：不创建也不更新 BizProductionReportFile 记录，
    /// 因此导出动作不会影响真实上传链路的文件与状态。
    /// </summary>
    void ExportXlsxWithUploadStatus(int taskId, string filePath);
}
