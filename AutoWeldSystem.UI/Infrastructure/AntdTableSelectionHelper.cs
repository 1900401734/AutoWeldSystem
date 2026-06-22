using AntdUI;

namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// Provides reusable selection helpers for AntdUI tables.
/// </summary>
public static class AntdTableSelectionHelper
{
    /// <summary>
    /// Enables multi-row selection without clearing the selection when focus leaves the table.
    /// </summary>
    public static void EnableMultiRowSelection(Table table)
    {
        ArgumentNullException.ThrowIfNull(table);

        table.MultipleRows = true;
        table.LostFocusClearSelection = false;
    }

    /// <summary>
    /// Gets the real selected row records and filters them to the requested row type.
    /// </summary>
    public static IReadOnlyList<T> GetSelectedRows<T>(Table table)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(table);

        return (table.SelectedsReal() ?? Array.Empty<object>())
            .OfType<T>()
            .ToList();
    }

    /// <summary>
    /// Clears the current selection and restores rows that still match after a table refresh.
    /// </summary>
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
