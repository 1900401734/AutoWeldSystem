using AntdUI;
using System.Collections;

namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// AntdUI 表格选择辅助方法。
/// 统一处理多选行、刷新后恢复选择和旧版单选回退，避免各个页面重复写选择逻辑。
/// </summary>
public static class AntdTableSelectionHelper
{
    /// <summary>
    /// 启用多行选择，并避免表格失焦时清空选中行。
    /// </summary>
    /// <param name="table">需要启用多选的 AntdUI 表格。</param>
    public static void EnableMultiRowSelection(Table table)
    {
        ArgumentNullException.ThrowIfNull(table);

        table.MultipleRows = true;
        table.LostFocusClearSelection = false;
    }

    /// <summary>
    /// 读取表格真实选中行，并按调用方需要的行类型过滤。
    /// </summary>
    /// <typeparam name="T">表格绑定行类型。</typeparam>
    /// <param name="table">AntdUI 表格。</param>
    /// <returns>当前真实选中的行。</returns>
    public static IReadOnlyList<T> GetSelectedRows<T>(Table table)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(table);

        var selectedRows = (table.SelectedsReal() ?? Array.Empty<object>())
            .OfType<T>()
            .ToList();
        if (selectedRows.Count > 0)
        {
            return selectedRows;
        }

        return GetSelectedRowsFromIndexes<T>(table);
    }

    /// <summary>
    /// Reads selected rows from SelectedIndexs when AntdUI only records selected row indexes.
    /// Ctrl+A selection can hit this path, so delete buttons must not rely only on SelectedsReal().
    /// </summary>
    private static IReadOnlyList<T> GetSelectedRowsFromIndexes<T>(Table table)
        where T : class
    {
        var selectedIndexes = table.SelectedIndexs;
        if (selectedIndexes is null || selectedIndexes.Length == 0)
        {
            return Array.Empty<T>();
        }

        var rows = EnumerateDataSource(table.DataSource)
            .OfType<T>()
            .ToList();
        if (rows.Count == 0)
        {
            return Array.Empty<T>();
        }

        return selectedIndexes
            .Where(index => index >= 0 && index < rows.Count)
            .Distinct()
            .Select(index => rows[index])
            .ToList();
    }

    /// <summary>
    /// Converts the current table data source to an object sequence while keeping string cells out.
    /// </summary>
    private static IEnumerable<object> EnumerateDataSource(object? dataSource)
    {
        if (dataSource is null or string)
        {
            return Array.Empty<object>();
        }

        return dataSource is IEnumerable enumerable
            ? enumerable.Cast<object>()
            : Array.Empty<object>();
    }

    /// <summary>
    /// 读取选中行；如果 AntdUI 当前没有多选记录，则回退到页面维护的当前行。
    /// 该方法用于兼容旧版“单击一行后点删除”的操作习惯。
    /// </summary>
    /// <typeparam name="T">表格绑定行类型。</typeparam>
    /// <param name="table">AntdUI 表格。</param>
    /// <param name="fallbackRow">页面当前记录的单选行。</param>
    /// <returns>选中行或单行回退结果。</returns>
    public static IReadOnlyList<T> GetSelectedRowsOrFallback<T>(Table table, T? fallbackRow)
        where T : class
    {
        var selectedRows = GetSelectedRows<T>(table);
        if (selectedRows.Count > 0)
        {
            return selectedRows;
        }

        return fallbackRow is null
            ? Array.Empty<T>()
            : new[] { fallbackRow };
    }

    /// <summary>
    /// 清空当前选择，并在表格刷新后恢复仍然匹配的行。
    /// </summary>
    /// <typeparam name="T">表格绑定行类型。</typeparam>
    /// <param name="table">AntdUI 表格。</param>
    /// <param name="rows">刷新后的可见行。</param>
    /// <param name="match">判断某行是否需要恢复选中的匹配规则。</param>
    public static void RestoreSelection<T>(Table table, IEnumerable<T> rows, Func<T, bool> match)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(match);

        table.SelectedIndexs = Array.Empty<int>();

        foreach (var row in rows)
        {
            if (match(row))
            {
                table.SetSelected(row, true);
            }
        }
    }
}
