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
}
