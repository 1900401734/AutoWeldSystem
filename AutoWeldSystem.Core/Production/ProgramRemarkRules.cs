using AutoWeldSystem.Core.Constants;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 解析程序同步时写入 MES 的备注。
/// 用户填写内容优先；为空时按真实同步动作补齐接口约定值。
/// </summary>
public static class ProgramRemarkRules
{
    private const string LegacyUpdateRemark = "修改";

    private static readonly string[] SystemActionRemarks =
    [
        AppConstants.ProgramRemarkActions.Create,
        LegacyUpdateRemark,
        AppConstants.ProgramRemarkActions.Update,
        AppConstants.ProgramRemarkActions.Delete
    ];

    /// <summary>
    /// 根据用户输入和同步动作解析 MES 备注。
    /// </summary>
    /// <param name="userRemark">用户在程序管理页填写的 MES 备注。</param>
    /// <param name="syncAction">本次准备执行的 MES 同步动作。</param>
    /// <returns>最终写入 MES 的备注。</returns>
    public static string ResolveForAction(string? userRemark, string? syncAction)
    {
        var normalizedRemark = userRemark?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedRemark) && !IsSystemActionRemark(normalizedRemark))
        {
            return normalizedRemark;
        }

        return syncAction switch
        {
            AppConstants.ProgramSyncActions.Create => AppConstants.ProgramRemarkActions.Create,
            AppConstants.ProgramSyncActions.Delete => AppConstants.ProgramRemarkActions.Delete,
            _ => AppConstants.ProgramRemarkActions.Update
        };
    }

    private static bool IsSystemActionRemark(string remark)
    {
        // 历史版本会把“新增/修改”等动作字样写入 Remark；它们不是用户自定义备注。
        return SystemActionRemarks.Any(action => string.Equals(action, remark, StringComparison.Ordinal));
    }
}
