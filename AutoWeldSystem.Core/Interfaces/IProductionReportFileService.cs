using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// 生产报告文件服务。
/// 负责根据本地采集数据生成报告文件，并记录本地文件状态。
/// </summary>
public interface IProductionReportFileService
{
    BizProductionReportFile GenerateXlsxReport(BizWeldTask task);
}
