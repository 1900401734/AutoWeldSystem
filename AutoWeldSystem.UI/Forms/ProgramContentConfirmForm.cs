using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.UI.Base;
using System.ComponentModel;

namespace AutoWeldSystem.UI.Forms;

public partial class ProgramContentConfirmForm : BaseWindow
{
    private const string KeyWorkOrderNo = "SN";
    private const string KeyProductNum = "ProductNumber";
    private const string KeyProductModel = "ProductModel";
    private const string KeyBatch = "Batch";
    private const string KeySpec = "Spec";
    private const string KeyProductName = "ProductName";
    private const string KeyDrawingNo = "DrawingNo";
    private const string KeyProcessNo = "ProcessNo";
    private const string KeyProcessName = "ProcessName";
    private const string KeyQuantity = "Quantity";
    private const string KeyProgramName = "ProgramName";
    private const string KeyProgramType = "ProgramType";

    private readonly WorkOrderRes _sourceWorkOrder;
    private readonly ExpItemData? _sourceProcess;
    private readonly ProgramDataRes _sourceProgram;
    private readonly IReadOnlyList<BizProgram> _localPrograms;
    private readonly BindingList<StartFieldRow> _rows = [];
    private bool _isBindingRows;

    public ProgramContentConfirmForm(
        WorkOrderRes workOrder,
        ExpItemData? process,
        ProgramDataRes program,
        IReadOnlyList<BizProgram> localPrograms)
    {
        InitializeComponent();
        _sourceWorkOrder = CloneWorkOrder(workOrder);
        _sourceProcess = process is null ? null : CloneProcess(process);
        _sourceProgram = program;
        _localPrograms = localPrograms;
        ConfigureGrid();
        BindRows();
        dgvFields.DataBindingComplete += (_, _) => ApplyProductDropdownCells();
        txtProgramContent.Text = string.IsNullOrWhiteSpace(program.ProgramContent) ? "{}" : program.ProgramContent;
        Shown += (_, _) => ApplyProductDropdownCells();
    }

    public WorkOrderRes AdjustedWorkOrder { get; private set; } = new();

    public ExpItemData? AdjustedProcess { get; private set; }

    public string ProgramContent => txtProgramContent.Text.Trim();

    private void ConfigureGrid()
    {
        dgvFields.AutoGenerateColumns = false;
        dgvFields.Columns.Clear();
        dgvFields.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(StartFieldRow.FieldName),
            HeaderText = "字段",
            ReadOnly = true,
            FillWeight = 22F
        });
        dgvFields.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(StartFieldRow.OriginalValue),
            HeaderText = "原始值",
            ReadOnly = true,
            FillWeight = 39F
        });
        dgvFields.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(StartFieldRow.AdjustedValue),
            HeaderText = "调整值",
            FillWeight = 39F
        });
        dgvFields.DataSource = _rows;
    }

    private void BindRows()
    {
        _isBindingRows = true;
        try
        {
            _rows.Clear();
        AddRow(KeyWorkOrderNo, "工单号", _sourceWorkOrder.SN);
        AddRow(KeyProductNum, "产品工号", _sourceWorkOrder.ProdNum);
        AddRow(KeyProductModel, "产品型号", _sourceWorkOrder.ProdModel);
        AddRow(KeyBatch, "批次", _sourceWorkOrder.Batch);
        AddRow(KeySpec, "规格", _sourceWorkOrder.Spec);
        AddRow(KeyProductName, "部件名称", _sourceWorkOrder.ProductName);
        AddRow(KeyDrawingNo, "零件图号", _sourceWorkOrder.DrawingNo);
        AddRow(KeyProcessNo, "工序号", _sourceProcess?.ProcessNo ?? string.Empty);
        AddRow(KeyProcessName, "工序名称", _sourceProcess?.ItemName ?? string.Empty);
        AddRow(KeyQuantity, "生产数量", _sourceProcess?.StartAmount.ToString() ?? string.Empty);
        AddRow(KeyProgramName, "程序名称", _sourceProgram.ProgramName, editable: false);
        AddRow(KeyProgramType, "程序类型", _sourceProgram.ProgramType, editable: false);
        }
        finally
        {
            _isBindingRows = false;
        }

        ApplyProductDropdownCells();
    }

    private void AddRow(string key, string fieldName, string value, bool editable = true)
    {
        _rows.Add(new StartFieldRow(key, fieldName, value, value, editable));
    }

    private void dgvFields_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        if (e.RowIndex < 0 || dgvFields.Rows[e.RowIndex].DataBoundItem is not StartFieldRow row)
        {
            return;
        }

        e.Cancel = !row.Editable || e.ColumnIndex != 2;
    }

    private void dgvFields_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || dgvFields.Rows[e.RowIndex].DataBoundItem is not StartFieldRow row)
        {
            return;
        }

        if (e.ColumnIndex == 2)
        {
            row.AdjustedValue = dgvFields.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;
        }

        if (row.Key == KeyProductNum)
        {
            RefreshProductModelDropdown();
        }
    }

    private void dgvFields_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (dgvFields.IsCurrentCellDirty)
        {
            dgvFields.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void dgvFields_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != 2)
        {
            return;
        }

        if (dgvFields.Rows[e.RowIndex].Cells[e.ColumnIndex] is not DataGridViewComboBoxCell)
        {
            return;
        }

        dgvFields.BeginEdit(true);
        if (dgvFields.EditingControl is ComboBox comboBox)
        {
            comboBox.DroppedDown = true;
        }
    }

    private void ApplyProductDropdownCells()
    {
        if (_isBindingRows)
        {
            return;
        }

        SetComboBoxCell(KeyProductNum, GetProductNumOptions());
        SetComboBoxCell(KeyProductModel, GetProductModelOptions(GetValue(KeyProductNum)));
    }

    private void RefreshProductModelDropdown()
    {
        var productModelOptions = GetProductModelOptions(GetValue(KeyProductNum));
        var currentModel = GetValue(KeyProductModel);
        SetComboBoxCell(KeyProductModel, productModelOptions);

        if (string.IsNullOrWhiteSpace(currentModel) && productModelOptions.Count == 1)
        {
            SetValue(KeyProductModel, productModelOptions[0]);
        }
    }

    private void SetComboBoxCell(string key, IReadOnlyList<string> options)
    {
        var rowIndex = FindRowIndex(key);
        if (rowIndex < 0)
        {
            return;
        }

        var currentValue = GetValue(key);
        var values = options
            .Append(currentValue)
            .Append(string.Empty)
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToArray();

        var cell = new DataGridViewComboBoxCell
        {
            FlatStyle = FlatStyle.Flat
        };
        cell.Items.AddRange(values);
        dgvFields.Rows[rowIndex].Cells[2] = cell;
        dgvFields.Rows[rowIndex].Cells[2].Value = currentValue;
    }

    private IReadOnlyList<string> GetProductNumOptions()
    {
        return _localPrograms
            .Select(program => program.ProductNum)
            .Append(_sourceProgram.ProductNum)
            .Append(_sourceWorkOrder.ProdNum)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();
    }

    private IReadOnlyList<string> GetProductModelOptions(string productNum)
    {
        var relatedModels = string.IsNullOrWhiteSpace(productNum)
            ? Enumerable.Empty<string?>()
            : _localPrograms
                .Where(program => string.Equals(
                    program.ProductNum?.Trim(),
                    productNum.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .Select(program => program.ProductModel);

        return relatedModels
            .Concat(_localPrograms.Select(program => program.ProductModel))
            .Append(_sourceWorkOrder.ProdModel)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();
    }

    private void btnApply_Click(object? sender, EventArgs e)
    {
        dgvFields.EndEdit();
        if (!TryBuildResult(out var message))
        {
            MessageBox.Show(this, message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCancel_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private bool TryBuildResult(out string message)
    {
        AdjustedWorkOrder = CloneWorkOrder(_sourceWorkOrder);
        AdjustedWorkOrder.SN = GetValue(KeyWorkOrderNo);
        AdjustedWorkOrder.ProdNum = GetValue(KeyProductNum);
        AdjustedWorkOrder.ProdModel = GetValue(KeyProductModel);
        AdjustedWorkOrder.Batch = GetValue(KeyBatch);
        AdjustedWorkOrder.Spec = GetValue(KeySpec);
        AdjustedWorkOrder.ProductName = GetValue(KeyProductName);
        AdjustedWorkOrder.DrawingNo = GetValue(KeyDrawingNo);
        AdjustedProcess = BuildAdjustedProcess();

        if (string.IsNullOrWhiteSpace(AdjustedWorkOrder.SN))
        {
            message = "工单号不能为空。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(AdjustedWorkOrder.ProdNum))
        {
            message = "产品工号不能为空。";
            return false;
        }

        if (AdjustedProcess is not null && AdjustedProcess.StartAmount <= 0)
        {
            message = "生产数量必须大于 0。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ProgramContent))
        {
            message = "程序内容不能为空。";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private ExpItemData? BuildAdjustedProcess()
    {
        if (_sourceProcess is null)
        {
            return null;
        }

        var process = CloneProcess(_sourceProcess);
        process.ProcessNo = GetValue(KeyProcessNo);
        process.ItemName = GetValue(KeyProcessName);
        process.StartAmount = int.TryParse(GetValue(KeyQuantity), out var quantity) ? quantity : 0;
        return process;
    }

    private string GetValue(string key)
    {
        return _rows.FirstOrDefault(row => row.Key == key)?.AdjustedValue.Trim() ?? string.Empty;
    }

    private void SetValue(string key, string value)
    {
        _rows.First(row => row.Key == key).AdjustedValue = value;
        var rowIndex = FindRowIndex(key);
        if (rowIndex >= 0)
        {
            dgvFields.Rows[rowIndex].Cells[2].Value = value;
        }
    }

    private int FindRowIndex(string key)
    {
        for (var index = 0; index < dgvFields.Rows.Count; index++)
        {
            if (dgvFields.Rows[index].DataBoundItem is StartFieldRow row && row.Key == key)
            {
                return index;
            }
        }

        return -1;
    }

    private static WorkOrderRes CloneWorkOrder(WorkOrderRes source)
    {
        return new WorkOrderRes
        {
            SN = source.SN,
            ProdNum = source.ProdNum,
            ProdModel = source.ProdModel,
            Spec = source.Spec,
            Batch = source.Batch,
            ProductName = source.ProductName,
            DrawingNo = source.DrawingNo,
            ProjectFrom = source.ProjectFrom,
            ExpItems = (source.ExpItems ?? []).Select(CloneProcess).ToList()
        };
    }

    private static ExpItemData CloneProcess(ExpItemData source)
    {
        return new ExpItemData
        {
            ItemId = source.ItemId,
            ItemTitle = source.ItemTitle,
            ItemCont = source.ItemCont,
            SequenceNo = source.SequenceNo,
            ItemName = source.ItemName,
            ProcessNo = source.ProcessNo,
            StartAmount = source.StartAmount
        };
    }

    private sealed class StartFieldRow
    {
        public StartFieldRow(string key, string fieldName, string originalValue, string adjustedValue, bool editable)
        {
            Key = key;
            FieldName = fieldName;
            OriginalValue = originalValue;
            AdjustedValue = adjustedValue;
            Editable = editable;
        }

        public string Key { get; }

        public string FieldName { get; }

        public string OriginalValue { get; }

        public string AdjustedValue { get; set; }

        public bool Editable { get; }
    }
}
