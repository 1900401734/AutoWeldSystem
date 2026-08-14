using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.UI.Base;
using System.ComponentModel;

namespace AutoWeldSystem.UI.Forms;

/// <summary>
/// 开工前程序内容预览/微调弹窗。
/// 表格展示测试项名称、设定值/标准值和本次临时修改值；修改只对本次开工生效、不落库。
/// </summary>
public partial class ProgramContentReviewForm : BaseWindow
{
    private readonly ProgramDataRes _program;
    private readonly BindingList<ProgramContentReviewRow> _rows = [];
    private bool _isBindingRows;

    public ProgramContentReviewForm(
        ProgramDataRes program,
        IReadOnlyList<DimTestItem> dictionaryItems)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(dictionaryItems);
        _program = program;

        InitializeComponent();
        ConfigureGrid();
        BindRows(dictionaryItems);
    }

    /// <summary>
    /// 用户确认后合并得到的 ProgramContent JSON 字符串；未确认或取消时为程序原始内容。
    /// </summary>
    public string MergedContentJson { get; private set; } = "{}";

    private void ConfigureGrid()
    {
        dgvFields.AutoGenerateColumns = false;
        dgvFields.Columns.Clear();
        dgvFields.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(ProgramContentReviewRow.ItemName),
            HeaderText = "测试项名称",
            ReadOnly = true,
            FillWeight = 34F
        });
        dgvFields.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(ProgramContentReviewRow.StandardValue),
            HeaderText = "设定值/标准值",
            ReadOnly = false,
            FillWeight = 66F
        });
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

        // 仅“设定值/标准值”列允许编辑，测试项名称固定。
        e.Cancel = e.ColumnIndex != 1;
    }

    private void dgvFields_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _isBindingRows)
        {
            return;
        }

        if (e.ColumnIndex != 1)
        {
            return;
        }

        if (dgvFields.Rows[e.RowIndex].DataBoundItem is ProgramContentReviewRow row)
        {
            row.StandardValue = dgvFields.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;
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
        dgvFields.EndEdit();
        if (!ProgramContentJsonRules.TryMergeReviewRowsToJson(_rows, out var json, out var message))
        {
            MessageBox.Show(this, message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        MergedContentJson = json;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCancel_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}