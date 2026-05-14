using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Views;

/// <summary>
/// 程序管理页。
/// 支持本地程序编辑、版本提交、MES 同步和从 MES 拉取程序。
/// </summary>
public partial class ProgramManageView : BaseView
{
    private readonly IProgramManageService _programService;
    private readonly ILocalizationService _localizer;
    private readonly BindingSource _programBindingSource = new();
    private readonly BindingSource _revisionBindingSource = new();
    private readonly List<BizProgram> _programs = new();
    private int _editingId;
    private bool _initialized;

    public ProgramManageView(IProgramManageService programService, ILocalizationService localizer)
    {
        _programService = programService;
        _localizer = localizer;

        InitializeComponent();
        ConfigureGrids();
        WireEvents();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (_initialized)
        {
            return;
        }

        _initialized = true;
        ReloadPrograms();
        StartNewProgram();
    }

    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ApplyGridHeaders();
        BindProgramTypeOptions();
        UpdateCurrentInfoText();
        dgvPrograms.Refresh();
    }

    private void ConfigureGrids()
    {
        TableStyleHelper.ApplyDataGridView(dgvPrograms);
        dgvPrograms.AutoGenerateColumns = false;
        dgvPrograms.Columns.Clear();
        dgvPrograms.Columns.Add(CreateTextColumn(nameof(BizProgram.ProgramName), 28));
        dgvPrograms.Columns.Add(CreateTextColumn(nameof(BizProgram.ProductNum), 14));
        dgvPrograms.Columns.Add(CreateTextColumn(nameof(BizProgram.VersionNumber), 8));
        dgvPrograms.Columns.Add(CreateTextColumn(nameof(BizProgram.SyncStatus), 13));
        dgvPrograms.Columns.Add(CreateTextColumn(nameof(BizProgram.CommitId), 12));
        dgvPrograms.Columns.Add(CreateTextColumn(nameof(BizProgram.UpdatedTime), 18));
        dgvPrograms.DataSource = _programBindingSource;

        TableStyleHelper.ApplyDataGridView(dgvRevisions);
        dgvRevisions.AutoGenerateColumns = false;
        dgvRevisions.Columns.Clear();
        dgvRevisions.Columns.Add(CreateTextColumn(nameof(BizProgramRevision.VersionNumber), 8));
        dgvRevisions.Columns.Add(CreateTextColumn(nameof(BizProgramRevision.CommitId), 14));
        dgvRevisions.Columns.Add(CreateTextColumn(nameof(BizProgramRevision.CommitMessage), 22));
        dgvRevisions.Columns.Add(CreateTextColumn(nameof(BizProgramRevision.UserName), 12));
        dgvRevisions.Columns.Add(CreateTextColumn(nameof(BizProgramRevision.CreatedTime), 20));
        dgvRevisions.DataSource = _revisionBindingSource;
    }

    private static DataGridViewTextBoxColumn CreateTextColumn(string propertyName, float fillWeight)
    {
        return new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            FillWeight = fillWeight
        };
    }

    private void WireEvents()
    {
        btnNew.Click += (_, _) => StartNewProgram();
        btnSave.Click += Save_ClickAsync;
        btnDelete.Click += Delete_ClickAsync;
        btnSync.Click += SyncSelected_ClickAsync;
        btnPullMes.Click += PullMes_ClickAsync;
        btnBuildName.Click += (_, _) => txtProgramName.Text = BuildProgramNameFromInputs();
        btnBrowseFile.Click += (_, _) => BrowseProgramFile();
        btnRefresh.Click += (_, _) => ReloadPrograms();
        txtKeyword.TextChanged += (_, _) => ApplyProgramFilter();
        dgvPrograms.SelectionChanged += (_, _) => BindSelectedProgram();
        dgvPrograms.CellFormatting += DgvPrograms_CellFormatting;
    }

    private void ApplyLocalizedTexts()
    {
        btnNew.Text = _localizer.GetString(TextKeys.Common.ActionAdd);
        btnSave.Text = _localizer.GetString(TextKeys.Common.ActionSave);
        btnDelete.Text = _localizer.GetString(TextKeys.Common.ActionDelete);
        btnSync.Text = _localizer.GetString(TextKeys.ProgramManage.ButtonSyncMes);
        btnPullMes.Text = _localizer.GetString(TextKeys.ProgramManage.ButtonPullMes);
        btnRefresh.Text = _localizer.GetString(TextKeys.Common.ActionRefresh);
        btnBuildName.Text = _localizer.GetString(TextKeys.ProgramManage.ButtonBuildName);
        btnBrowseFile.Text = _localizer.GetString(TextKeys.ProgramManage.ButtonBrowseFile);
        chkSyncNow.Text = _localizer.GetString(TextKeys.ProgramManage.CheckSyncNow);
        txtKeyword.PlaceholderText = _localizer.GetString(TextKeys.ProgramManage.PlaceholderKeyword);
        grpRevisions.Text = _localizer.GetString(TextKeys.ProgramManage.GroupRevisions);

        lblProgramName.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProgramName);
        lblProductNum.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProductNum);
        lblProductModel.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProductModel);
        lblComponentCode.Text = _localizer.GetString(TextKeys.ProgramManage.LabelComponentCode);
        lblSequenceNumber.Text = _localizer.GetString(TextKeys.ProgramManage.LabelSequenceNumber);
        lblProgramType.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProgramType);
        lblWeldJobName.Text = _localizer.GetString(TextKeys.ProgramManage.LabelWeldJobName);
        lblRobotJobName.Text = _localizer.GetString(TextKeys.ProgramManage.LabelRobotJobName);
        lblCycleTime.Text = _localizer.GetString(TextKeys.ProgramManage.LabelCycleTime);
        lblProgramFile.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProgramFile);
        lblCommitMessage.Text = _localizer.GetString(TextKeys.ProgramManage.LabelCommitMessage);
        lblRemark.Text = _localizer.GetString(TextKeys.ProgramManage.LabelRemark);
        lblProgramContent.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProgramContent);
    }

    private void ApplyGridHeaders()
    {
        SetColumnHeader(dgvPrograms, nameof(BizProgram.ProgramName), TextKeys.Grid.ProgramName);
        SetColumnHeader(dgvPrograms, nameof(BizProgram.ProductNum), TextKeys.Grid.ProgramProductNum);
        SetColumnHeader(dgvPrograms, nameof(BizProgram.VersionNumber), TextKeys.Grid.ProgramVersionNumber);
        SetColumnHeader(dgvPrograms, nameof(BizProgram.SyncStatus), TextKeys.Grid.ProgramSyncStatus);
        SetColumnHeader(dgvPrograms, nameof(BizProgram.CommitId), TextKeys.Grid.ProgramCommitId);
        SetColumnHeader(dgvPrograms, nameof(BizProgram.UpdatedTime), TextKeys.Grid.ProgramUpdatedTime);

        SetColumnHeader(dgvRevisions, nameof(BizProgramRevision.VersionNumber), TextKeys.Grid.ProgramVersionNumber);
        SetColumnHeader(dgvRevisions, nameof(BizProgramRevision.CommitId), TextKeys.Grid.ProgramCommitId);
        SetColumnHeader(dgvRevisions, nameof(BizProgramRevision.CommitMessage), TextKeys.Grid.ProgramCommitMessage);
        SetColumnHeader(dgvRevisions, nameof(BizProgramRevision.UserName), TextKeys.Grid.ProgramCommitUser);
        SetColumnHeader(dgvRevisions, nameof(BizProgramRevision.CreatedTime), TextKeys.Grid.ProgramCommitTime);
    }

    private void BindProgramTypeOptions()
    {
        var selectedIndex = cmbProgramType.SelectedIndex;

        cmbProgramType.Items.Clear();
        cmbProgramType.Items.Add(_localizer.GetString(TextKeys.ProgramManage.OptionParameterString));
        cmbProgramType.Items.Add(_localizer.GetString(TextKeys.ProgramManage.OptionFile));

        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        cmbProgramType.SelectedIndex = Math.Min(selectedIndex, cmbProgramType.Items.Count - 1);
    }

    private void SetColumnHeader(DataGridView grid, string propertyName, string headerKey)
    {
        foreach (DataGridViewColumn column in grid.Columns)
        {
            if (string.Equals(column.DataPropertyName, propertyName, StringComparison.Ordinal))
            {
                column.HeaderText = _localizer.GetString(headerKey);
                return;
            }
        }
    }

    private void ReloadPrograms(int? selectedId = null)
    {
        _programs.Clear();
        _programs.AddRange(_programService.GetPrograms());
        ApplyProgramFilter(selectedId);
    }

    private void ApplyProgramFilter(int? selectedId = null)
    {
        var keyword = txtKeyword.Text.Trim();
        var filtered = _programs
            .Where(program => string.IsNullOrWhiteSpace(keyword)
                || Contains(program.ProgramName, keyword)
                || Contains(program.ProductNum, keyword)
                || Contains(program.ProductModel, keyword)
                || Contains(program.ComponentCode, keyword)
                || Contains(program.SyncStatus, keyword)
                || Contains(GetSyncStatusText(program.SyncStatus), keyword))
            .ToList();

        _programBindingSource.DataSource = filtered;
        if (filtered.Count == 0)
        {
            _revisionBindingSource.DataSource = Array.Empty<BizProgramRevision>();
            return;
        }

        SelectProgramRow(selectedId ?? _editingId);
        BindSelectedProgram();
    }

    private static bool Contains(string? source, string keyword)
    {
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private void StartNewProgram()
    {
        _editingId = 0;
        txtProgramName.Clear();
        txtProductNum.Clear();
        txtProductModel.Clear();
        txtComponentCode.Clear();
        txtSequenceNumber.Text = "1";
        cmbProgramType.SelectedIndex = 0;
        txtWeldJobName.Clear();
        txtRobotJobName.Clear();
        txtCycleTime.Text = "0";
        txtProgramFile.Clear();
        txtRemark.Clear();
        txtCommitMessage.Text = _localizer.GetString(TextKeys.ProgramManage.CommitCreate);
        txtProgramContent.Text = "{\r\n}";
        lblCurrentInfo.Text = _localizer.GetString(TextKeys.ProgramManage.CurrentNew);
        _revisionBindingSource.DataSource = Array.Empty<BizProgramRevision>();
    }

    private void BindSelectedProgram()
    {
        if (dgvPrograms.CurrentRow?.DataBoundItem is not BizProgram program)
        {
            return;
        }

        _editingId = program.Id;
        txtProgramName.Text = program.ProgramName;
        txtProductNum.Text = program.ProductNum;
        txtProductModel.Text = program.ProductModel ?? string.Empty;
        txtComponentCode.Text = program.ComponentCode ?? string.Empty;
        txtSequenceNumber.Text = program.SequenceNumber.ToString();
        cmbProgramType.SelectedIndex = program.ProgramType == "1" ? 1 : 0;
        txtWeldJobName.Text = program.WeldJobName ?? string.Empty;
        txtRobotJobName.Text = program.RobotJobName ?? string.Empty;
        txtCycleTime.Text = program.CycleTimeSeconds.ToString("0.###");
        txtProgramFile.Text = program.ProgramFileName ?? string.Empty;
        txtRemark.Text = program.Remark ?? string.Empty;
        txtCommitMessage.Text = _localizer.GetString(TextKeys.ProgramManage.CommitUpdate);
        txtProgramContent.Text = string.IsNullOrWhiteSpace(program.ProgramContentJson)
            ? "{\r\n}"
            : program.ProgramContentJson;
        SetCurrentProgramInfo(program);
        _revisionBindingSource.DataSource = _programService.GetRevisions(program.Id).ToList();
    }

    private void UpdateCurrentInfoText()
    {
        if (_editingId <= 0)
        {
            lblCurrentInfo.Text = _localizer.GetString(TextKeys.ProgramManage.CurrentNew);
            return;
        }

        if (dgvPrograms.CurrentRow?.DataBoundItem is BizProgram program)
        {
            SetCurrentProgramInfo(program);
        }
    }

    private void SetCurrentProgramInfo(BizProgram program)
    {
        lblCurrentInfo.Text = _localizer.GetString(
            TextKeys.ProgramManage.CurrentSelected,
            program.VersionNumber,
            GetSyncStatusText(program.SyncStatus),
            program.CommitId ?? string.Empty);
    }

    private void DgvPrograms_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        var column = dgvPrograms.Columns[e.ColumnIndex];
        if (string.Equals(column.DataPropertyName, nameof(BizProgram.SyncStatus), StringComparison.Ordinal))
        {
            e.Value = GetSyncStatusText(Convert.ToString(e.Value));
            e.FormattingApplied = true;
        }
    }

    private string GetSyncStatusText(string? status)
    {
        return status switch
        {
            AppConstants.ProgramSyncStatus.PendingCreate => _localizer.GetString(TextKeys.ProgramManage.StatusPendingCreate),
            AppConstants.ProgramSyncStatus.PendingUpdate => _localizer.GetString(TextKeys.ProgramManage.StatusPendingUpdate),
            AppConstants.ProgramSyncStatus.PendingDelete => _localizer.GetString(TextKeys.ProgramManage.StatusPendingDelete),
            AppConstants.ProgramSyncStatus.Synced => _localizer.GetString(TextKeys.ProgramManage.StatusSynced),
            AppConstants.ProgramSyncStatus.Failed => _localizer.GetString(TextKeys.ProgramManage.StatusFailed),
            AppConstants.ProgramSyncStatus.Deleted => _localizer.GetString(TextKeys.ProgramManage.StatusDeleted),
            _ => status ?? string.Empty
        };
    }

    private void SelectProgramRow(int id)
    {
        if (id <= 0 || dgvPrograms.Rows.Count == 0)
        {
            return;
        }

        foreach (DataGridViewRow row in dgvPrograms.Rows)
        {
            if (row.DataBoundItem is BizProgram program && program.Id == id)
            {
                row.Selected = true;
                dgvPrograms.CurrentCell = row.Cells[0];
                return;
            }
        }
    }

    private async void Save_ClickAsync(object? sender, EventArgs e)
    {
        if (!TryBuildRequest(out var request))
        {
            return;
        }

        btnSave.Enabled = false;
        try
        {
            var saved = await _programService.SaveAsync(request, chkSyncNow.Checked);
            ReloadPrograms(saved.Id);
            ShowInfo(TextKeys.ProgramManage.SaveSuccess);
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
        finally
        {
            btnSave.Enabled = true;
        }
    }

    private async void Delete_ClickAsync(object? sender, EventArgs e)
    {
        if (_editingId <= 0)
        {
            ShowWarning(TextKeys.ProgramManage.SelectDelete);
            return;
        }

        if (!Confirm(TextKeys.ProgramManage.DeleteConfirm))
        {
            return;
        }

        try
        {
            await _programService.DeleteAsync(_editingId, chkSyncNow.Checked);
            ReloadPrograms();
            StartNewProgram();
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
    }

    private async void SyncSelected_ClickAsync(object? sender, EventArgs e)
    {
        if (_editingId <= 0)
        {
            ShowWarning(TextKeys.ProgramManage.SelectSync);
            return;
        }

        btnSync.Enabled = false;
        try
        {
            await _programService.SyncProgramAsync(_editingId);
            ReloadPrograms(_editingId);
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
        finally
        {
            btnSync.Enabled = true;
        }
    }

    private async void PullMes_ClickAsync(object? sender, EventArgs e)
    {
        btnPullMes.Enabled = false;
        try
        {
            var count = await _programService.PullFromMesAsync(string.IsNullOrWhiteSpace(txtProductNum.Text) ? null : txtProductNum.Text.Trim());
            ReloadPrograms();
            ShowInfo(TextKeys.ProgramManage.PullSuccess, count);
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
        finally
        {
            btnPullMes.Enabled = true;
        }
    }

    private bool TryBuildRequest(out ProgramSaveRequest request)
    {
        request = new ProgramSaveRequest { Id = _editingId };

        if (!int.TryParse(txtSequenceNumber.Text.Trim(), out var sequenceNumber) || sequenceNumber <= 0)
        {
            ShowWarning(TextKeys.ProgramManage.SequenceInvalid);
            return false;
        }

        if (!decimal.TryParse(txtCycleTime.Text.Trim(), out var cycleTime) || cycleTime < 0)
        {
            ShowWarning(TextKeys.ProgramManage.CycleTimeInvalid);
            return false;
        }

        request.ProgramName = txtProgramName.Text.Trim();
        request.ProductNum = txtProductNum.Text.Trim();
        request.ProductModel = txtProductModel.Text.Trim();
        request.ComponentCode = txtComponentCode.Text.Trim();
        request.SequenceNumber = sequenceNumber;
        request.ProgramType = cmbProgramType.SelectedIndex == 1 ? "1" : "0";
        request.ProgramContentJson = txtProgramContent.Text.Trim();
        request.ProgramFilePath = File.Exists(txtProgramFile.Text.Trim()) ? txtProgramFile.Text.Trim() : string.Empty;
        request.WeldJobName = txtWeldJobName.Text.Trim();
        request.RobotJobName = txtRobotJobName.Text.Trim();
        request.CycleTimeSeconds = cycleTime;
        request.Remark = txtRemark.Text.Trim();
        request.CommitMessage = txtCommitMessage.Text.Trim();
        return true;
    }

    private string BuildProgramNameFromInputs()
    {
        if (!int.TryParse(txtSequenceNumber.Text.Trim(), out var sequenceNumber))
        {
            sequenceNumber = 1;
        }

        return _programService.BuildProgramName(txtProductNum.Text.Trim(), txtComponentCode.Text.Trim(), sequenceNumber);
    }

    private void BrowseProgramFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = _localizer.GetString(TextKeys.ProgramManage.DialogSelectFile),
            Filter = _localizer.GetString(TextKeys.ProgramManage.DialogFileFilterAll)
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtProgramFile.Text = dialog.FileName;
            cmbProgramType.SelectedIndex = 1;
        }
    }

    private void ShowInfo(string messageKey, params object[] args)
    {
        ShowInfoMessage(_localizer.GetString(messageKey, args));
    }

    private void ShowInfoMessage(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleInfo), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowWarning(string messageKey, params object[] args)
    {
        ShowWarningMessage(_localizer.GetString(messageKey, args));
    }

    private void ShowWarningMessage(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleWarning), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ShowErrorMessage(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleError), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private bool Confirm(string messageKey, params object[] args)
    {
        var message = _localizer.GetString(messageKey, args);
        return MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleConfirmDelete), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            == DialogResult.Yes;
    }
}
