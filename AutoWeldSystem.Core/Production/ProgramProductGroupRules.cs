using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序管理列表的产品工号分组规则。
/// 同一产品工号下可存在多个程序（靠流水号区分），列表按工号去重后一行一工号；
/// 工号下有多个程序时才展开成子行，只有一个程序时直接把该程序显示在工号行上，
/// 避免为单程序工号也摆一层空壳父节点。
/// </summary>
public static class ProgramProductGroupRules
{
    /// <summary>
    /// 按产品工号对本地程序分组，生成可直接绑定树形表格的行集合。
    /// </summary>
    /// <param name="programs">本地程序记录。</param>
    /// <param name="describeProgram">生成程序行摘要文本的回调，用于承载同步状态等需要本地化的内容。</param>
    /// <returns>按工号升序排列的分组行；工号为空的程序不参与分组。</returns>
    public static IReadOnlyList<ProgramProductGroupRow> BuildGroups(
        IEnumerable<BizProgram> programs,
        Func<BizProgram, string> describeProgram)
    {
        ArgumentNullException.ThrowIfNull(programs);
        ArgumentNullException.ThrowIfNull(describeProgram);

        return programs
            .Where(program => !string.IsNullOrWhiteSpace(program.ProductNum))
            .GroupBy(program => Normalize(program.ProductNum), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => BuildGroup(group, describeProgram))
            .ToList();
    }

    private static ProgramProductGroupRow BuildGroup(
        IGrouping<string, BizProgram> group,
        Func<BizProgram, string> describeProgram)
    {
        // 同一工号大小写不一致时取序数最小的写法，保证显示文本稳定可按 Ordinal 回查。
        var productNum = group
            .Select(program => Normalize(program.ProductNum))
            .OrderBy(value => value, StringComparer.Ordinal)
            .First();
        var ordered = group
            .OrderBy(program => program.SequenceNumber)
            .ThenBy(program => program.Id)
            .ToList();

        // 单程序工号不再多套一层父节点，直接把该程序摊平到工号行上。
        if (ordered.Count == 1)
        {
            return new ProgramProductGroupRow
            {
                ProductNum = productNum,
                ProgramId = ordered[0].Id,
                Summary = describeProgram(ordered[0]),
                UpdatedTime = ordered[0].UpdatedTime
            };
        }

        return new ProgramProductGroupRow
        {
            ProductNum = productNum,
            ProgramId = 0,
            Summary = string.Empty,
            UpdatedTime = ordered.Max(program => program.UpdatedTime),
            Programs = ordered
                .Select(program => new ProgramProductGroupRow
                {
                    ProductNum = BuildSequenceLabel(program),
                    ProgramId = program.Id,
                    Summary = describeProgram(program),
                    UpdatedTime = program.UpdatedTime
                })
                .ToList()
        };
    }

    private static string BuildSequenceLabel(BizProgram program)
        => $"#{Math.Max(1, program.SequenceNumber):000}";

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}

/// <summary>
/// 程序管理列表行。
/// 顶层行代表一个产品工号；工号下有多个程序时，子行代表其中一个程序。
/// </summary>
public sealed class ProgramProductGroupRow
{
    /// <summary>
    /// 顶层行显示产品工号，子行显示流水号标签。
    /// </summary>
    public string ProductNum { get; init; } = string.Empty;

    /// <summary>
    /// 该行对应的程序本地 ID；多程序工号的父行为 0，表示它本身不指向具体程序。
    /// </summary>
    public int ProgramId { get; init; }

    /// <summary>
    /// 程序摘要（程序名称、版本、同步状态等）；多程序工号的父行为空。
    /// </summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// 该行最近一次更新时间；父行取组内最新。
    /// </summary>
    public DateTime UpdatedTime { get; init; }

    /// <summary>
    /// 子行集合，供表格树形列绑定。单程序工号为 null，因此不会出现展开箭头。
    /// </summary>
    public List<ProgramProductGroupRow>? Programs { get; init; }
}
