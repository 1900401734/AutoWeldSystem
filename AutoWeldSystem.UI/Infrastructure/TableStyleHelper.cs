using System.Drawing;
using System.Windows.Forms;

namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// Centralizes table appearance so every page keeps a consistent visual rhythm.
/// </summary>
public static class TableStyleHelper
{
    /// <summary>
    /// Applies a restrained, dense style to AntdUI tables used by dashboard pages.
    /// </summary>
    public static void ApplyAntdTable(AntdUI.Table table, AntdUI.ColumnsMode columnsMode = AntdUI.ColumnsMode.Fill)
    {
        table.AutoSizeColumnsMode = columnsMode;
        table.Bordered = true;
        table.EnableHeaderResizing = true;
        table.FixedHeader = true;
        table.Gap = 8;
        table.RowHeight = 42;
        table.RowHeightHeader = 42;
        table.ScrollBarAvoidHeader = true;
        table.ShowTip = true;
    }

    /// <summary>
    /// Centers AntdUI table headers and cells, and lets each column size itself by content.
    /// </summary>
    public static void ApplyAntdColumnDefaults(AntdUI.Table table)
    {
        foreach (var column in table.Columns)
        {
            column.Align = AntdUI.ColumnAlign.Center;
            column.ColAlign = AntdUI.ColumnAlign.Center;
            column.Ellipsis = true;
        }
    }

    /// <summary>
    /// Applies a clean DataGridView style without changing the bound data or columns.
    /// </summary>
    public static void ApplyDataGridView(DataGridView grid)
    {
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersHeight = 40;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.EnableHeadersVisualStyles = false;
        grid.GridColor = UiColors.Table.GridLineColor;
        grid.RowHeadersVisible = false;
        grid.RowTemplate.Height = 36;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        grid.ColumnAdded -= Grid_ColumnAdded;
        grid.ColumnAdded += Grid_ColumnAdded;

        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            Alignment = DataGridViewContentAlignment.MiddleCenter,
            BackColor = UiColors.Table.HeaderBackColor,
            ForeColor = UiColors.Table.HeaderForeColor,
            Font = new Font(grid.Font, FontStyle.Bold),
            Padding = new Padding(8, 0, 8, 0),
            SelectionBackColor = UiColors.Table.HeaderBackColor,
            SelectionForeColor = UiColors.Table.HeaderForeColor,
            WrapMode = DataGridViewTriState.False
        };

        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            Alignment = DataGridViewContentAlignment.MiddleCenter,
            BackColor = Color.White,
            ForeColor = UiColors.Table.TextColor,
            Padding = new Padding(8, 0, 8, 0),
            SelectionBackColor = UiColors.Table.SelectionBackColor,
            SelectionForeColor = UiColors.Table.SelectionForeColor,
            WrapMode = DataGridViewTriState.False
        };

        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            Alignment = DataGridViewContentAlignment.MiddleCenter,
            BackColor = UiColors.Table.AlternateRowColor,
            ForeColor = UiColors.Table.TextColor,
            Padding = new Padding(8, 0, 8, 0),
            SelectionBackColor = UiColors.Table.SelectionBackColor,
            SelectionForeColor = UiColors.Table.SelectionForeColor,
            WrapMode = DataGridViewTriState.False
        };

        ApplyDataGridViewColumnDefaults(grid);
    }

    /// <summary>
    /// Applies centered headers and cells to columns already added to the grid.
    /// </summary>
    public static void ApplyDataGridViewColumnDefaults(DataGridView grid)
    {
        foreach (DataGridViewColumn column in grid.Columns)
        {
            ApplyDataGridViewColumn(column);
        }
    }

    private static void Grid_ColumnAdded(object? sender, DataGridViewColumnEventArgs e)
    {
        ApplyDataGridViewColumn(e.Column);
    }

    private static void ApplyDataGridViewColumn(DataGridViewColumn column)
    {
        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        column.MinimumWidth = 88;
        column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        column.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
    }
}
