using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Forms;

/// <summary>
/// 通用选择弹窗。
/// 窗体本身保持非泛型，方便 WinForms 设计器正常打开；泛型选择逻辑放在静态方法里。
/// </summary>
public sealed partial class SelectionDialog : BaseWindow
{
    private const int MaxColumnCount = 10;

    private readonly List<SelectionDialogRow> _rows = new();
    private SelectionDialogRow? _selectedRow;

    public SelectionDialog()
    {
        InitializeComponent();
        ConfigureTable();
        WireEvents();
    }

    private SelectionDialog(
        string title,
        string prompt,
        IReadOnlyList<SelectionDialogRow> rows,
        IReadOnlyList<SelectionDialogColumnDefinition> columns,
        string okText,
        string cancelText)
        : this()
    {
        Text = title;
        pageHeader1.Text = title;
        lblPrompt.Text = prompt;
        btnOk.Text = okText;
        btnCancel.Text = cancelText;
        ConfigureColumns(columns);
        BindRows(rows);
    }

    private object? SelectedValue => _selectedRow?.Value;

    public static bool TrySelect<T>(
        IWin32Window owner,
        string title,
        string prompt,
        IReadOnlyList<T> items,
        Func<T, string> displaySelector,
        string okText,
        string cancelText,
        out T selected)
    {
        return TrySelect(
            owner,
            title,
            prompt,
            items,
            [new SelectionDialogColumn<T>(string.Empty, displaySelector)],
            okText,
            cancelText,
            out selected);
    }

    public static bool TrySelect<T>(
        IWin32Window owner,
        string title,
        string prompt,
        IReadOnlyList<T> items,
        IReadOnlyList<SelectionDialogColumn<T>> columns,
        string okText,
        string cancelText,
        out T selected)
    {
        if (items.Count == 0)
        {
            selected = default!;
            return false;
        }

        var normalizedColumns = NormalizeColumns(columns);
        var rows = items
            .Select(item => new SelectionDialogRow(
                item!,
                columns.Select(column => column.ValueSelector(item)?.ToString() ?? string.Empty).ToArray()))
            .ToList();

        using var form = new SelectionDialog(title, prompt, rows, normalizedColumns, okText, cancelText);
        if (form.ShowDialog(owner) == DialogResult.OK && form.SelectedValue is T value)
        {
            selected = value;
            return true;
        }

        selected = default!;
        return false;
    }

    private static List<SelectionDialogColumnDefinition> NormalizeColumns<T>(IReadOnlyList<SelectionDialogColumn<T>> columns)
    {
        if (columns.Count == 0)
        {
            throw new InvalidOperationException("Selection dialog must have at least one column.");
        }

        if (columns.Count > MaxColumnCount)
        {
            throw new InvalidOperationException($"Selection dialog supports at most {MaxColumnCount} columns.");
        }

        return columns
            .Select((column, index) => new SelectionDialogColumnDefinition(
                GetColumnKey(index),
                column.HeaderText,
                column.FillWeight,
                column.Alignment))
            .ToList();
    }

    private void ConfigureTable()
    {
        TableStyleHelper.ApplyDataGridView(tableItems);
        tableItems.AutoGenerateColumns = false;
        tableItems.MultiSelect = false;
        tableItems.ReadOnly = true;
        tableItems.StandardTab = true;
    }

    private void ConfigureColumns(IReadOnlyList<SelectionDialogColumnDefinition> columns)
    {
        tableItems.Columns.Clear();
        tableItems.ColumnHeadersVisible = columns.Any(column => !string.IsNullOrWhiteSpace(column.HeaderText));

        foreach (var column in columns)
        {
            tableItems.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = column.Key,
                HeaderText = column.HeaderText,
                FillWeight = column.FillWeight,
                DefaultCellStyle = { Alignment = column.Alignment }
            });
        }
    }

    private void BindRows(IReadOnlyList<SelectionDialogRow> rows)
    {
        _rows.Clear();
        _rows.AddRange(rows);
        tableItems.DataSource = null;
        tableItems.DataSource = _rows;

        _selectedRow = _rows.FirstOrDefault();
        if (_selectedRow is not null && tableItems.Rows.Count > 0)
        {
            tableItems.Rows[0].Selected = true;
            tableItems.CurrentCell = tableItems.Rows[0].Cells[0];
        }
    }

    private void WireEvents()
    {
        tableItems.CellClick += (_, _) => UpdateSelectedRow();
        tableItems.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                UpdateSelectedRow();
                AcceptSelection();
            }
        };
        tableItems.KeyDown += TableItems_KeyDown;
        tableItems.SelectionChanged += (_, _) => UpdateSelectedRow();
        btnOk.Click += (_, _) => AcceptSelection();
        btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
    }

    private void TableItems_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.Handled = true;
        AcceptSelection();
    }

    private void UpdateSelectedRow()
    {
        if (tableItems.CurrentRow?.DataBoundItem is SelectionDialogRow row)
        {
            _selectedRow = row;
        }
    }

    private void AcceptSelection()
    {
        UpdateSelectedRow();

        if (_selectedRow is null)
        {
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private static string GetColumnKey(int index)
    {
        return index switch
        {
            0 => nameof(SelectionDialogRow.Value0),
            1 => nameof(SelectionDialogRow.Value1),
            2 => nameof(SelectionDialogRow.Value2),
            3 => nameof(SelectionDialogRow.Value3),
            4 => nameof(SelectionDialogRow.Value4),
            5 => nameof(SelectionDialogRow.Value5),
            6 => nameof(SelectionDialogRow.Value6),
            7 => nameof(SelectionDialogRow.Value7),
            8 => nameof(SelectionDialogRow.Value8),
            9 => nameof(SelectionDialogRow.Value9),
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
        };
    }

    private sealed class SelectionDialogRow
    {
        public SelectionDialogRow(object value, string[] values)
        {
            Value = value;
            Value0 = GetValue(values, 0);
            Value1 = GetValue(values, 1);
            Value2 = GetValue(values, 2);
            Value3 = GetValue(values, 3);
            Value4 = GetValue(values, 4);
            Value5 = GetValue(values, 5);
            Value6 = GetValue(values, 6);
            Value7 = GetValue(values, 7);
            Value8 = GetValue(values, 8);
            Value9 = GetValue(values, 9);
        }

        public object Value { get; }

        public string Value0 { get; set; }

        public string Value1 { get; set; }

        public string Value2 { get; set; }

        public string Value3 { get; set; }

        public string Value4 { get; set; }

        public string Value5 { get; set; }

        public string Value6 { get; set; }

        public string Value7 { get; set; }

        public string Value8 { get; set; }

        public string Value9 { get; set; }

        private static string GetValue(string[] values, int index)
        {
            return index >= 0 && index < values.Length ? values[index] : string.Empty;
        }
    }

    private sealed record SelectionDialogColumnDefinition(
        string Key,
        string HeaderText,
        float FillWeight,
        DataGridViewContentAlignment Alignment);
}

/// <summary>
/// 通用选择弹窗的表格列定义。
/// 调用方只描述“显示什么”，选择结果仍然返回原始业务对象。
/// </summary>
public sealed record SelectionDialogColumn<T>(
    string HeaderText,
    Func<T, object?> ValueSelector,
    float FillWeight = 1F,
    DataGridViewContentAlignment Alignment = DataGridViewContentAlignment.MiddleLeft);
