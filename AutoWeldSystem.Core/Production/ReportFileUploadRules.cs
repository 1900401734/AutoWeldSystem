using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// MES 报表文件上传门槛。
/// 本地 XLSX 是否生成不属于此规则；本规则只决定是否创建 MES ReportFile 上传任务。
/// </summary>
public static class ReportFileUploadRules
{
    /// <summary>
    /// 至少一个角色同时启用采集与报表输出时，才允许上传报表文件。
    /// </summary>
    public static bool ShouldUploadReportFile(IEnumerable<BizSchemeDetail> details)
    {
        ArgumentNullException.ThrowIfNull(details);
        return details.Any(detail => SchemeDetailRoleRules.AllRoles.Any(
            role => SchemeDetailRoleRules.ShouldWriteReportRole(detail, role)));
    }
}
