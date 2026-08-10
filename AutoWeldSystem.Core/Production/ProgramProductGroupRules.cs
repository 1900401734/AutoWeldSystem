using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序管理列表的产品工号分组规则。
/// 同一产品工号下可存在多个程序（靠流水号区分），列表按工号去重后一行一工号，
/// 具体程序改由右侧树展开，避免同工号多行在界面上无法区分归属。
/// </summary>
public static class ProgramProductGroupRules
{
    /// <summary>
    /// 按产品工号对本地程序分组。
    /// </summary>
    /// <param name="programs">本地程序记录。</param>
    /// <returns>按工号升序排列的分组行；工号为空的程序不参与分组。</returns>
    public static IReadOnlyList<ProgramProductGroupRow> BuildGroups(IEnumerable<BizProgram> programs)
    {
        ArgumentNullException.ThrowIfNull(programs);

        return programs
            .Where(program => !string.IsNullOrWhiteSpace(program.ProductNum))
            .GroupBy(program => Normalize(program.ProductNum), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProgramProductGroupRow
            {
                // 同一工号大小写不一致时取序数最小的写法，保证显示文本稳定可按 Ordinal 回查。
                ProductNum = group
                    .Select(program => Normalize(program.ProductNum))
                    .OrderBy(productNum => productNum, StringComparer.Ordinal)
                    .First(),
                ProgramCount = group.Count(),
                UpdatedTime = group.Max(program => program.UpdatedTime)
            })
            .ToList();
    }

    /// <summary>
    /// 取出某个产品工号下的全部程序，按流水号升序排列。
    /// </summary>
    /// <param name="programs">本地程序记录。</param>
    /// <param name="productNum">目标产品工号；大小写和首尾空白不敏感。</param>
    /// <returns>该工号下的程序；工号为空时返回空集合。</returns>
    public static IReadOnlyList<BizProgram> FilterByProductNum(
        IEnumerable<BizProgram> programs,
        string? productNum)
    {
        ArgumentNullException.ThrowIfNull(programs);

        var normalized = Normalize(productNum);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<BizProgram>();
        }

        return programs
            .Where(program => string.Equals(
                Normalize(program.ProductNum),
                normalized,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(program => program.SequenceNumber)
            .ThenBy(program => program.Id)
            .ToList();
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}

/// <summary>
/// 程序管理列表行，一行代表一个产品工号。
/// </summary>
public sealed class ProgramProductGroupRow
{
    /// <summary>
    /// 产品工号，用于回查该工号下的全部程序。
    /// </summary>
    public string ProductNum { get; init; } = string.Empty;

    /// <summary>
    /// 该工号下的程序数量。
    /// </summary>
    public int ProgramCount { get; init; }

    /// <summary>
    /// 该工号下最近一次更新时间。
    /// </summary>
    public DateTime UpdatedTime { get; init; }
}
