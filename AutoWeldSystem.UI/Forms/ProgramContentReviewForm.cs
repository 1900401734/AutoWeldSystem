using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.UI.Base;
using System.ComponentModel;

namespace AutoWeldSystem.UI.Forms;

/// <summary>
/// 开工前程序内容预览/微调弹窗。
/// 表格展示测试项名称、设定上限和设定下限；修改只对本次开工生效、不落库。
/// </summary>
public partial class ProgramContentReviewForm : BaseWindow
{
    private readonly ProgramDataRes _program;
    private readonly BindingList<ProgramContentReviewRow> _rows = [];
    private bool _isBindingRows;

    private readonly string? _station1RecipeName;
    private readonly string? _station2RecipeName;
    private readonly int? _touchCount;
    private readonly string _touchCountLabel;
    private readonly string? _deviceType;
    private readonly string _originalContent;

    public ProgramContentReviewForm(
        ProgramDataRes program,
        IReadOnlyList<DimTestItem> dictionaryItems,
        bool enableDualStation = false,
        string? processParameterDeviceType = null)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(dictionaryItems);
        _program = program;
        _deviceType = processParameterDeviceType;
        _originalContent = ProgramContentJsonRules.NormalizeContent(program.ProgramContent, _deviceType);
        MergedContentJson = _originalContent;

        // 配方名称来自程序内容保留键，只读展示，改配方仍走程序管理。
        (_station1RecipeName, _station2RecipeName) =
            ProgramContentJsonRules.ExtractRecipeNames(program.ProgramContent);
        _touchCount = ProgramContentJsonRules.TryGetTouchCount(program.ProgramContent, out var touchCount)
            ? touchCount
            : null;
        _touchCountLabel = ProcessParameterDeviceUiProfile.Resolve(processParameterDeviceType).PointCountHeader;

        InitializeComponent();
        ConfigureGrid();
        BindRecipeNames(enableDualStation);
        BindRows(dictionaryItems);
        btnApply.Enabled = _touchCount.HasValue;
    }

    /// <summary>
    /// 用户确认后合并得到的 ProgramContent JSON 字符串；未确认或取消时为程序原始内容。
    /// </summary>
    public string MergedContentJson { get; private set; } = "{}";

    /// <summary>
    /// 在测试项表格上方只读显示本次开工使用的配方名称。
    /// 旧程序内容没有该字段时显示「未指定」，不阻挡确认。
    /// </summary>
    private void BindRecipeNames(bool enableDualStation)
    {
        const string notSpecified = "未指定";
        var lines = new List<string>
        {
            $"工位1配方名称：{(string.IsNullOrWhiteSpace(_station1RecipeName) ? notSpecified : _station1RecipeName)}"
        };

        if (enableDualStation || !string.IsNullOrWhiteSpace(_station2RecipeName))
        {
            lines.Add($"工位2配方名称：{(string.IsNullOrWhiteSpace(_station2RecipeName) ? notSpecified : _station2RecipeName)}");
        }

        lines.Add(_touchCount.HasValue
            ? $"{_touchCountLabel}：{_touchCount.Value}"
            : $"{_touchCountLabel}：未配置（请先在程序管理中补齐）");

        lblRecipeNamesSection.Text = string.Join(Environment.NewLine, lines);
        lblRecipeNamesSection.AutoSize = true;
        lblRecipeNamesSection.Visible = true;
    }

    private void ConfigureGrid()
    {
        dgvFields.DataSource = _rows;
    }

    private void BindRows(IReadOnlyList<DimTestItem> dictionaryItems)
    {
        _isBindingRows = true;
        try
        {
            _rows.Clear();
            foreach (var row in ProgramContentJsonRules.BuildReviewRows(dictionaryItems, _program.ProgramContent))
            {
                _rows.Add(row);
            }
        }
        finally
        {
            _isBindingRows = false;
        }
    }

    private void dgvFields_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        if (e.RowIndex < 0 || dgvFields.Rows[e.RowIndex].DataBoundItem is not ProgramContentReviewRow row)
        {
            return;
        }

        // 测试项名称固定，两侧限值都允许编辑。
        e.Cancel = e.ColumnIndex == 0;
    }

    private void dgvFields_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || _isBindingRows)
        {
            return;
        }

        if (dgvFields.Rows[e.RowIndex].DataBoundItem is ProgramContentReviewRow row)
        {
            dgvFields.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = string.Empty;
            var value = dgvFields.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;
            if (e.ColumnIndex == 1) row.UpperLimit = value;
            if (e.ColumnIndex == 2) row.LowerLimit = value;
        }
    }

    private void dgvFields_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (dgvFields.IsCurrentCellDirty)
        {
            dgvFields.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void btnApply_Click(object? sender, EventArgs e)
    {
        if (!dgvFields.EndEdit()) return;
        BindingContext?[_rows]?.EndCurrentEdit();
        if (!ProgramContentJsonRules.TryMergeReviewRowsToJson(_rows, out var json, out var message, _deviceType))
        {
            FocusInvalidLimit(message);
            MessageBox.Show(this, message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        MergedContentJson = ProgramContentJsonRules.ReplaceLimits(_originalContent, json, _deviceType);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void FocusInvalidLimit(string message)
    {
        for (var index = 0; index < _rows.Count; index++)
        {
            if (string.IsNullOrEmpty(_rows[index].ItemName) || !message.Contains($"“{_rows[index].ItemName}”", StringComparison.Ordinal)) continue;
            var column = message.Contains("设定下限", StringComparison.Ordinal) ? 2 : 1;
            dgvFields.CurrentCell = dgvFields.Rows[index].Cells[column];
            dgvFields.Rows[index].Cells[column].ErrorText = message;
            dgvFields.Focus();
            break;
        }
    }

    private void btnCancel_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
