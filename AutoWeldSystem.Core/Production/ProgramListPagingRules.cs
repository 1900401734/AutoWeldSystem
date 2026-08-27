using AutoWeldSystem.Core.DTOs.DataManagement;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序管理列表的分页规则。
/// 现场设备可存放上百个加工程序，列表按产品工号分组行分页；
/// 分组序号在分页前生成，因此翻页后序号仍然全局连续。
/// </summary>
public static class ProgramListPagingRules
{
    /// <summary>
    /// 默认每页显示的产品工号分组数量。
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// 取程序列表的一页数据。
    /// </summary>
    /// <param name="groups">已排序的全部分组行。</param>
    /// <param name="requestedPageIndex">请求的页码，小于 1 或越界时会被夹到有效范围。</param>
    /// <param name="requestedPageSize">请求的每页数量，非正数时回退为默认值。</param>
    /// <param name="keepProgramId">需要保持可见的程序本地 ID；命中时直接定位到它所在页。</param>
    /// <returns>当前页的分组行以及回写分页控件所需的页码、每页数量和总数。</returns>
    public static PagedResult<ProgramProductGroupRow> GetPage(
        IReadOnlyList<ProgramProductGroupRow> groups,
        int requestedPageIndex,
        int requestedPageSize,
        int keepProgramId = 0)
    {
        ArgumentNullException.ThrowIfNull(groups);

        var pageSize = requestedPageSize > 0 ? requestedPageSize : DefaultPageSize;
        var pageTotal = Math.Max(1, (groups.Count + pageSize - 1) / pageSize);
        var pageIndex = Math.Clamp(
            ResolvePageIndex(groups, pageSize, requestedPageIndex, keepProgramId),
            1,
            pageTotal);

        return new PagedResult<ProgramProductGroupRow>
        {
            Items = groups.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = groups.Count,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 判断指定程序是否出现在给定分组行中（含展开后的子行）。
    /// </summary>
    public static bool ContainsProgram(IReadOnlyList<ProgramProductGroupRow> rows, int programId)
    {
        ArgumentNullException.ThrowIfNull(rows);

        return programId > 0 && IndexOfProgram(rows, programId) >= 0;
    }

    /// <summary>
    /// 取给定分组行中第一个真实程序的本地 ID；没有可用程序时返回 0。
    /// </summary>
    public static int ResolveFirstProgramId(IReadOnlyList<ProgramProductGroupRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        foreach (var row in rows)
        {
            if (row.ProgramId > 0)
            {
                return row.ProgramId;
            }

            // 多程序工号的父行不指向具体程序，回落到它的第一个子行。
            var firstChild = row.Programs?.FirstOrDefault(child => child.ProgramId > 0);
            if (firstChild is not null)
            {
                return firstChild.ProgramId;
            }
        }

        return 0;
    }

    private static int ResolvePageIndex(
        IReadOnlyList<ProgramProductGroupRow> groups,
        int pageSize,
        int requestedPageIndex,
        int keepProgramId)
    {
        if (keepProgramId > 0)
        {
            var rowIndex = IndexOfProgram(groups, keepProgramId);
            if (rowIndex >= 0)
            {
                return (rowIndex / pageSize) + 1;
            }
        }

        return requestedPageIndex < 1 ? 1 : requestedPageIndex;
    }

    private static int IndexOfProgram(IReadOnlyList<ProgramProductGroupRow> rows, int programId)
    {
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            if (row.ProgramId == programId)
            {
                return index;
            }

            if (row.Programs?.Any(child => child.ProgramId == programId) == true)
            {
                return index;
            }
        }

        return -1;
    }
}
