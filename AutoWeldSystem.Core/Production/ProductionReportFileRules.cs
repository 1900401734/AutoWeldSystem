using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 生产报表文件的纯业务规则。
/// 将数据库读取和文件选择判断从服务编排中拆出，便于使用真实实体做回归测试。
/// </summary>
public static class ProductionReportFileRules
{
    /// <summary>
    /// 已保存任务按 TaskId 读取最新快照；读取不到时保留调用方传入对象。
    /// </summary>
    public static BizWeldTask ResolveLatestTask(
        BizWeldTask suppliedTask,
        Func<int, BizWeldTask?> loadById)
    {
        ArgumentNullException.ThrowIfNull(suppliedTask);
        ArgumentNullException.ThrowIfNull(loadById);

        return suppliedTask.Id > 0
            ? loadById(suppliedTask.Id) ?? suppliedTask
            : suppliedTask;
    }

    /// <summary>
    /// 从指定任务的文件记录中选择最新、可上传的设备端 XLSX 生产报表。
    /// </summary>
    public static string? SelectLatestUploadFilePath(
        IEnumerable<BizProductionReportFile> reports,
        int taskId)
    {
        ArgumentNullException.ThrowIfNull(reports);

        return reports
            .Where(report => report.TaskId == taskId
                && report.FileCode == ProductionConstants.ReportFileCodes.Spreadsheet
                && string.Equals(report.FileFormat, "XLSX", StringComparison.OrdinalIgnoreCase)
                && report.MesFileType == ProductionConstants.MesFileTypes.ReportFile
                && !string.IsNullOrWhiteSpace(report.FilePath))
            .OrderByDescending(report => report.UpdatedTime)
            .ThenByDescending(report => report.Id)
            .Select(report => report.FilePath.Trim())
            .FirstOrDefault();
    }
}
