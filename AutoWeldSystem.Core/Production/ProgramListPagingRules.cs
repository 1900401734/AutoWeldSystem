using AutoWeldSystem.Core.DTOs.DataManagement;

namespace AutoWeldSystem.Core.Production;

/// <summary>
/// 程序管理列表的分页规则。
/// 现场设备可存放上百个加工程序，列表按程序行分页；
/// 序号在分页前生成，因此翻页后序号仍然全局连续。
/// </summary>
public static class ProgramListPagingRules
{
    /// <summary>
    /// 默认每页显示的程序行数量。
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// 取程序列表的一页数据。
    /// </summary>
    /// <param name="rows">已排序的全部程序行。</param>
    /// <param name="requestedPageIndex">请求的页码，小于 1 或越界时会被夹到有效范围。</param>
    /// <param name="requestedPageSize">请求的每页数量，非正数时回退为默认值。</param>
    /// <param name="keepProgramId">需要保持可见的程序本地 ID；命中时直接定位到它所在页。</param>
    /// <returns>当前页的程序行以及回写分页控件所需的页码、每页数量和总数。</returns>
    public static PagedResult<ProgramProductGroupRow> GetPage(
        IReadOnlyList<ProgramProductGroupRow> rows,
        int requestedPageIndex,
        int requestedPageSize,
        int keepProgramId = 0)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var pageSize = requestedPageSize > 0 ? requestedPageSize : DefaultPageSize;
        var pageTotal = Math.Max(1, (rows.Count + pageSize - 1) / pageSize);
        var pageIndex = Math.Clamp(
            ResolvePageIndex(rows, pageSize, requestedPageIndex, keepProgramId),
            1,
            pageTotal);

        return new PagedResult<ProgramProductGroupRow>
        {
            Items = rows.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = rows.Count,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 判断指定程序是否出现在给定程序行中。
    /// </summary>
    public static bool ContainsProgram(IReadOnlyList<ProgramProductGroupRow> rows, int programId)
    {
        ArgumentNullException.ThrowIfNull(rows);

        return programId > 0 && rows.Any(row => row.ProgramId == programId);
    }

    /// <summary>
    /// 取给定程序行中的第一个程序本地 ID；没有可用程序时返回 0。
    /// </summary>
    public static int ResolveFirstProgramId(IReadOnlyList<ProgramProductGroupRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        return rows.FirstOrDefault(row => row.ProgramId > 0)?.ProgramId ?? 0;
    }

    private static int ResolvePageIndex(
        IReadOnlyList<ProgramProductGroupRow> rows,
        int pageSize,
        int requestedPageIndex,
        int keepProgramId)
    {
        if (keepProgramId > 0)
        {
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                if (rows[rowIndex].ProgramId == keepProgramId)
                {
                    return (rowIndex / pageSize) + 1;
                }
            }
        }

        return requestedPageIndex < 1 ? 1 : requestedPageIndex;
    }
}
