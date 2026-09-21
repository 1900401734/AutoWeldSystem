namespace AutoWeldSystem.Core.Production;

/// <summary>
/// AntdUI 表格选中行序号换算规则。
/// AntdUI 把表头也当成一行并占用序号 0，数据行序号从 1 开始（Ctrl+A 也按 1 起写入），
/// 因此选中序号必须减一才能落到绑定数据源的下标上，直接当下标用会整体错位到下一行，
/// 表现为“删除选中行时删掉了下一条记录”。
/// </summary>
public static class AntdTableRowIndexRules
{
    /// <summary>
    /// AntdUI 表格中表头占用的行序号。
    /// </summary>
    public const int HeaderRowIndex = 0;

    /// <summary>
    /// 把 AntdUI 选中行序号换算为绑定数据源下标。
    /// 表头序号和越界序号一并丢弃，其余按传入顺序去重，保留用户的点选顺序。
    /// </summary>
    /// <param name="selectedRowIndexes">AntdUI 选中行序号（含表头的 1 起序号）。</param>
    /// <param name="rowCount">当前数据源行数。</param>
    /// <returns>可直接用于数据源索引的 0 起下标。</returns>
    public static IReadOnlyList<int> ToDataSourceIndexes(IEnumerable<int>? selectedRowIndexes, int rowCount)
    {
        if (selectedRowIndexes is null || rowCount <= 0)
        {
            return Array.Empty<int>();
        }

        return selectedRowIndexes
            .Where(index => index > HeaderRowIndex && index <= rowCount)
            .Select(index => index - 1)
            .Distinct()
            .ToList();
    }
}
