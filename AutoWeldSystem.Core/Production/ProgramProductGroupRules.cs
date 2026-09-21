using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序管理列表的平铺行规则。
/// 每个程序独占一行，产品工号重复显示；排序按更新时间和本地 ID 倒序。
/// </summary>
public static class ProgramProductGroupRules
{
    /// <summary>
    /// 将本地程序转换为可直接绑定普通表格的行集合。
    /// </summary>
    /// <param name="programs">本地程序记录。</param>
    /// <param name="resolveSyncStatus">生成同步状态文本的回调。</param>
    /// <returns>按更新时间全局倒序排列的程序行；工号为空的程序不参与列表。</returns>
    public static IReadOnlyList<ProgramProductGroupRow> BuildRows(
        IEnumerable<BizProgram> programs,
        Func<BizProgram, string> resolveSyncStatus)
    {
        ArgumentNullException.ThrowIfNull(programs);
        ArgumentNullException.ThrowIfNull(resolveSyncStatus);

        return programs
            .Where(program => !string.IsNullOrWhiteSpace(program.ProductNum))
            .OrderByDescending(program => program.UpdatedTime)
            .ThenByDescending(program => program.Id)
            .Select((program, index) => new ProgramProductGroupRow
            {
                SerialNumber = index + 1,
                ProductNum = Normalize(program.ProductNum),
                ProgramId = program.Id,
                ProgramName = program.ProgramName,
                SyncStatus = resolveSyncStatus(program),
                UpdatedTime = program.UpdatedTime
            })
            .ToList();
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}

/// <summary>
/// 程序管理列表行，每个程序独占一行。
/// </summary>
public sealed class ProgramProductGroupRow
{
    /// <summary>
    /// 当前筛选结果中的程序连续序号。
    /// </summary>
    public int? SerialNumber { get; init; }

    /// <summary>
    /// 程序所属产品工号。
    /// </summary>
    public string ProductNum { get; init; } = string.Empty;

    /// <summary>
    /// 该行对应的程序本地 ID。
    /// </summary>
    public int ProgramId { get; init; }

    /// <summary>
    /// 程序名称。
    /// </summary>
    public string ProgramName { get; init; } = string.Empty;

    /// <summary>
    /// 本地化后的同步状态。
    /// </summary>
    public string SyncStatus { get; init; } = string.Empty;

    /// <summary>
    /// 该程序最近一次更新时间。
    /// </summary>
    public DateTime UpdatedTime { get; init; }
}
