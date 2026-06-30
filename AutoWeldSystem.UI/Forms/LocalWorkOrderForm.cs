using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.UI.Base;

namespace AutoWeldSystem.UI.Forms;

/// <summary>
/// Local work-order editor used when MES is disconnected.
/// The selected local program provides product identity and recipe code.
/// </summary>
public partial class LocalWorkOrderForm : BaseWindow
{
    private readonly IReadOnlyList<BizProgram> _programs;
    private readonly int _stationNo;
    private readonly IPlcWorkIdMonitorService? _workIdMonitorService;
    private bool _workOrderEditedByUser;
    private bool _syncingWorkOrderText;

    public LocalWorkOrderForm(
        IReadOnlyList<BizProgram> programs,
        int stationNo,
        IPlcWorkIdMonitorService? workIdMonitorService = null)
    {
        InitializeComponent();
        _programs = programs;
        _stationNo = stationNo <= 0 ? ProductionConstants.Stations.DefaultStationNo : stationNo;
        _workIdMonitorService = workIdMonitorService;

        Text = "本地工单";
        AcceptButton = btnOk;
        CancelButton = btnCancel;
        BindPrograms();
        BindDefaults();
        txtWorkOrderId.TextChanged += TxtWorkOrderId_TextChanged;

        if (_workIdMonitorService is not null)
        {
            _workIdMonitorService.WorkIdChanged += WorkIdMonitorService_WorkIdChanged;
        }
    }

    public OfflineExperimentStartReq Request { get; private set; } = new();

    private void BindPrograms()
    {
        var items = OfflineStartInputRules.BuildProgramNameOptions(_programs)
            .Select(option => new LocalProgramItem(option))
            .ToList();

        cmbProgram.DisplayMember = nameof(LocalProgramItem.DisplayText);
        cmbProgram.ValueMember = nameof(LocalProgramItem.Program);
        cmbProgram.DataSource = items;
        cmbProgram.SelectedIndexChanged += (_, _) => BindSelectedProgram();
        BindSelectedProgram();
    }

    private void BindDefaults()
    {
        SetWorkOrderText(ResolveInitialWorkOrderId());
        txtProcessNo.Text = "OP10";
        txtProcessName.Text = "离线焊接";
        numPlannedQty.Value = 1;
    }

    private void BindSelectedProgram()
    {
        var program = SelectedProgram;
        txtProductNum.Text = program?.ProductNum ?? string.Empty;
        txtProgramName.Text = program?.ProgramName ?? string.Empty;
        txtRecipeCode.Text = program?.RecipeCode ?? string.Empty;
    }

    private string ResolveInitialWorkOrderId()
    {
        var plcWorkId = _workIdMonitorService?.GetCurrent(_stationNo).WorkId?.Trim();
        return string.IsNullOrWhiteSpace(plcWorkId)
            ? $"LOCAL-{_stationNo}-{DateTime.Now:yyyyMMddHHmmss}"
            : plcWorkId;
    }

    private void TxtWorkOrderId_TextChanged(object? sender, EventArgs e)
    {
        if (!_syncingWorkOrderText)
        {
            _workOrderEditedByUser = true;
        }
    }

    private void WorkIdMonitorService_WorkIdChanged(object? sender, PlcWorkIdSnapshot snapshot)
    {
        if (snapshot.StationNo != _stationNo || _workOrderEditedByUser || string.IsNullOrWhiteSpace(snapshot.WorkId))
        {
            return;
        }

        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        BeginInvoke(new Action(() =>
        {
            if (!_workOrderEditedByUser)
            {
                SetWorkOrderText(snapshot.WorkId.Trim());
            }
        }));
    }

    private void SetWorkOrderText(string value)
    {
        _syncingWorkOrderText = true;
        try
        {
            txtWorkOrderId.Text = value;
        }
        finally
        {
            _syncingWorkOrderText = false;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (_workIdMonitorService is not null)
        {
            _workIdMonitorService.WorkIdChanged -= WorkIdMonitorService_WorkIdChanged;
        }

        base.OnFormClosed(e);
    }

    private BizProgram? SelectedProgram
        => cmbProgram.SelectedItem is LocalProgramItem item ? item.Program : null;

    private void btnOk_Click(object sender, EventArgs e)
    {
        var program = SelectedProgram;
        if (program is null)
        {
            MessageBox.Show(this, "请先选择本地加工程序。", "本地工单", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(program.RecipeCode))
        {
            MessageBox.Show(this, "所选本地程序缺少配方编号，无法离线开工。", "本地工单", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtWorkOrderId.Text))
        {
            MessageBox.Show(this, "工单号不能为空。", "本地工单", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtProductName.Text))
        {
            MessageBox.Show(this, "产品名称不能为空。", "本地工单", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtDrawingNo.Text))
        {
            MessageBox.Show(this, "图号不能为空。", "本地工单", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Request = new OfflineExperimentStartReq
        {
            StationNo = _stationNo,
            WorkOrderId = txtWorkOrderId.Text.Trim(),
            Batch = txtBatch.Text.Trim(),
            Spec = txtSpec.Text.Trim(),
            ProcessNo = string.IsNullOrWhiteSpace(txtProcessNo.Text) ? "OP10" : txtProcessNo.Text.Trim(),
            ProcessName = string.IsNullOrWhiteSpace(txtProcessName.Text) ? "离线焊接" : txtProcessName.Text.Trim(),
            PlannedQty = (int)numPlannedQty.Value,
            ProgramLocalId = program.Id,
            ProgramId = string.IsNullOrWhiteSpace(program.ProgramId) ? $"local-{program.Id}" : program.ProgramId.Trim(),
            ProgramName = program.ProgramName.Trim(),
            ProgramType = program.ProgramType.Trim(),
            ProgramContent = string.IsNullOrWhiteSpace(program.ProgramContent) ? "{}" : program.ProgramContent.Trim(),
            ProductNum = program.ProductNum.Trim(),
            ProductModel = program.ProductModel?.Trim() ?? string.Empty,
            ProductName = txtProductName.Text.Trim(),
            DrawingNo = txtDrawingNo.Text.Trim(),
            RecipeCode = program.RecipeCode.Trim()
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private sealed class LocalProgramItem
    {
        public LocalProgramItem(OfflineProgramNameOption option)
        {
            Option = option;
        }

        public OfflineProgramNameOption Option { get; }

        public BizProgram Program => Option.Program;

        public string DisplayText => Option.DisplayText;
    }
}
