using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Production;
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
    private readonly ITestSchemeConfigService _testSchemeConfigService;
    private readonly ILocalizationService _localizer;
    private readonly BindingSource _programBindingSource = new();
    private readonly BindingSource _revisionBindingSource = new();
    private readonly List<BizProgram> _programs = new();
    private readonly List<ProgramContentItemRow> _programContentRows = new();
    private int _editingId;
    private bool _initialized;
    private bool _programContentDictionaryAvailable;

    public ProgramManageView(
        IProgramManageService programService,
        ITestSchemeConfigService testSchemeConfigService,
        ILocalizationService localizer)
    {
        _programService = programService;
        _testSchemeConfigService = testSchemeConfigService;
        _localizer = localizer;

        InitializeComponent();
        ConfigureGrids();
        BindRemarkText(null);
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
        ConfigureProgramContentColumns(_programContentDictionaryAvailable);
        BindProgramTypeOptions();
        BindRemarkText(inputRemark.Text);
        UpdateCurrentInfoText();
        dgvPrograms.Refresh();
    }

    private void ConfigureGrids()
    {
        TableStyleHelper.ApplyDataGridView(dgvPrograms);
        dgvPrograms.AutoGenerateColumns = false;
        dgvPrograms.Columns.Clear();
        dgvPrograms.Columns.Add(CreateTextColumn(nameof(BizProgram.RecipeCode), 14));
        dgvPrograms.Columns.Add(CreateTextColumn(nameof(BizProgram.ProductNum), 18));
        dgvPrograms.Columns.Add(CreateTextColumn(nameof(BizProgram.Description), 18));
        dgvPrograms.Columns.Add(CreateTextColumn(nameof(BizProgram.VersionNumber), 8));
        dgvPrograms.Columns.Add(CreateTextColumn(nameof(BizProgram.SyncStatus), 13));
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

        TableStyleHelper.ApplyAntdTable(tableProgramContent);
        // AntdUI 表格需要显式设置编辑触发方式，否则列 Editable=true 也不会进入编辑器。
        tableProgramContent.EditMode = AntdUI.TEditMode.DoubleClick;
        // 允许单元格失去焦点时提交编辑，保证设定值能进入保存流程。
        tableProgramContent.EditLostFocus = true;
        ConfigureProgramContentColumns(dictionaryAvailable: false);
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
        btnBuildName.Click += (_, _) => inputProgramName.Text = BuildProgramNameFromInputs();
        btnBrowseFile.Click += (_, _) => BrowseProgramFile();
        btnRefresh.Click += (_, _) => ReloadPrograms();
        txtKeyword.TextChanged += (_, _) => ApplyProgramFilter();
        dgvPrograms.SelectionChanged += (_, _) => BindSelectedProgram();
        dgvPrograms.CellFormatting += DgvPrograms_CellFormatting;
        tableProgramContent.CellEndEdit += ProgramContentTable_CellEndEdit;
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
        lblProgramId.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProgramId);
        lblProductNum.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProductNum);
        lblRecipeCode.Text = _localizer.GetString(TextKeys.ProgramManage.LabelRecipeCode);
        lblComponentCode.Text = _localizer.GetString(TextKeys.ProgramManage.LabelComponentCode);
        lblSequenceNumber.Text = _localizer.GetString(TextKeys.ProgramManage.LabelSequenceNumber);
        lblProgramType.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProgramType);
        lblProgramFile.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProgramFile);
        lblRemark.Text = _localizer.GetString(TextKeys.ProgramManage.LabelRemark);
        lblDescription.Text = _localizer.GetString(TextKeys.ProgramManage.LabelLocalRemark);
        lblProgramContent.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProgramContent);
    }

    private void ApplyGridHeaders()
    {
        SetColumnHeader(dgvPrograms, nameof(BizProgram.RecipeCode), TextKeys.Grid.ProgramRecipeCode);
        SetColumnHeader(dgvPrograms, nameof(BizProgram.ProductNum), TextKeys.Grid.ProgramProductNum);
        SetColumnHeader(dgvPrograms, nameof(BizProgram.Description), TextKeys.Grid.ProgramLocalRemark);
        SetColumnHeader(dgvPrograms, nameof(BizProgram.VersionNumber), TextKeys.Grid.ProgramVersionNumber);
        SetColumnHeader(dgvPrograms, nameof(BizProgram.SyncStatus), TextKeys.Grid.ProgramSyncStatus);
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

    private void BindRemarkText(string? remark)
    {
        inputRemark.Text = remark?.Trim() ?? string.Empty;
    }

    private void BindProgramContentRows(string? programContentJson)
    {
        var dictionaryItems = _testSchemeConfigService.GetItems();
        _programContentDictionaryAvailable = dictionaryItems.Any(item => !string.IsNullOrWhiteSpace(item.ItemName));
        _programContentRows.Clear();
        _programContentRows.AddRange(ProgramContentJsonRules.BuildRows(dictionaryItems, programContentJson));
        EnsureManualProgramContentRow();
        ConfigureProgramContentColumns(_programContentDictionaryAvailable);
        RefreshProgramContentTable();
    }

    private bool ProgramContentTable_CellEndEdit(object sender, AntdUI.TableEndEditEventArgs e)
    {
        if (EnsureManualProgramContentRow())
        {
            RefreshProgramContentTable();
        }

        return true;
    }

    private bool EnsureManualProgramContentRow()
    {
        if (_programContentDictionaryAvailable)
        {
            return false;
        }

        if (_programContentRows.Any(IsBlankProgramContentRow))
        {
            return false;
        }

        _programContentRows.Add(new ProgramContentItemRow());
        return true;
    }

    private void RefreshProgramContentTable()
    {
        tableProgramContent.DataSource = null;
        tableProgramContent.DataSource = _programContentRows;
    }

    private static bool IsBlankProgramContentRow(ProgramContentItemRow row)
        => string.IsNullOrWhiteSpace(row.ItemName) && string.IsNullOrWhiteSpace(row.StandardValue);

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
                || Contains(program.RecipeCode, keyword)
                || Contains(program.ProductNum, keyword)
                || Contains(program.ComponentCode, keyword)
                || Contains(program.Description, keyword)
                || Contains(program.SyncStatus, keyword)
                || Contains(GetSyncStatusText(program.SyncStatus), keyword))
            .OrderBy(GetRecipeSortBucket)
            .ThenBy(GetRecipeSortNumber)
            .ThenBy(program => NormalizeSortText(program.RecipeCode), StringComparer.OrdinalIgnoreCase)
            .ThenBy(program => NormalizeSortText(program.ProductNum), StringComparer.OrdinalIgnoreCase)
            .ThenBy(program => NormalizeSortText(program.ProgramName), StringComparer.OrdinalIgnoreCase)
            .ToList();

        _programBindingSource.DataSource = filtered;
        dgvPrograms.Invalidate();
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

    /// <summary>
    /// 配方编号通常是数字。这里把数字编号排在前面，文本编号排在后面，空值放到最后。
    /// </summary>
    private static int GetRecipeSortBucket(BizProgram program)
    {
        var recipeCode = program.RecipeCode?.Trim();
        if (string.IsNullOrWhiteSpace(recipeCode))
        {
            return 2;
        }

        return int.TryParse(recipeCode, out _) ? 0 : 1;
    }

    private static int GetRecipeSortNumber(BizProgram program)
        => int.TryParse(program.RecipeCode?.Trim(), out var recipeNumber) ? recipeNumber : 0;

    private static string NormalizeSortText(string? value)
        => value?.Trim() ?? string.Empty;

    private void StartNewProgram()
    {
        _editingId = 0;
        txtProgramId.Clear();
        inputProgramName.Clear();
        inputProductNum.Clear();
        inputRecipeCode.Text = string.Empty;
        inputComponentCode.Clear();
        inputSequenceNumber.Text = "1";
        cmbProgramType.SelectedIndex = 0;
        txtProgramFile.Clear();
        BindRemarkText(null);
        inputDescription.Clear();
        BindProgramContentRows(null);
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
        txtProgramId.Text = program.ProgramId ?? string.Empty;
        inputProgramName.Text = program.ProgramName;
        inputProductNum.Text = program.ProductNum;
        inputRecipeCode.Text = program.RecipeCode ?? string.Empty;
        inputComponentCode.Text = program.ComponentCode ?? string.Empty;
        inputSequenceNumber.Text = program.SequenceNumber.ToString();
        cmbProgramType.SelectedIndex = program.ProgramType == "1" ? 1 : 0;
        txtProgramFile.Text = program.ProgramFileName ?? string.Empty;
        BindRemarkText(program.Remark);
        inputDescription.Text = program.Description ?? string.Empty;
        BindProgramContentRows(program.ProgramContent);
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
        var currentText = _localizer.GetString(
            TextKeys.ProgramManage.CurrentSelected,
            program.VersionNumber,
            GetSyncStatusText(program.SyncStatus),
            program.CommitId ?? string.Empty);
        var programId = string.IsNullOrWhiteSpace(program.ProgramId) ? "--" : program.ProgramId;
        //lblCurrentInfo.Text = $"{currentText}，MES程序ID：{programId}";
        lblCurrentInfo.Text = $"{currentText}";
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
            var saveResult = await _programService.SaveWithSyncDecisionAsync(request);
            var saved = saveResult.Program;
            var syncInBackground = chkSyncNow.Checked && saveResult.ShouldSyncNow;
            ReloadPrograms(saved.Id);
            ShowInfo(syncInBackground ? "程序已保存到本地，MES同步将在后台执行。" : _localizer.GetString(TextKeys.ProgramManage.SaveSuccess));
            if (syncInBackground)
            {
                _ = SyncProgramInBackgroundAsync(saved.Id);
            }
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

    private async Task SyncProgramInBackgroundAsync(int programId)
    {
        try
        {
            await _programService.SyncProgramAsync(programId);
            RunOnUiThread(() => ReloadPrograms(programId), "ProgramManageView.SyncProgram.Reload");
        }
        catch (Exception ex)
        {
            RunOnUiThread(() => ShowErrorMessage(ex.Message), "ProgramManageView.SyncProgram.Error");
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
            await _programService.DeleteAsync(_editingId, chkSyncNow.Checked, ResolveEditedMesRemark(GetEditingProgram()));
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
            var count = await _programService.PullFromMesAsync();
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

    private bool TryBuildRequest(out SaveProgramReq request)
    {
        request = new SaveProgramReq { Id = _editingId };

        if (!int.TryParse(inputSequenceNumber.Text.Trim(), out var sequenceNumber) || sequenceNumber <= 0)
        {
            ShowWarning(TextKeys.ProgramManage.SequenceInvalid);
            return false;
        }

        request.ProgramName = inputProgramName.Text.Trim();
        request.ProductNum = inputProductNum.Text.Trim();
        request.RecipeCode = ResolveRecipeCodeForSave(GetEditingProgram());
        request.ComponentCode = inputComponentCode.Text.Trim();
        request.SequenceNumber = sequenceNumber;
        request.ProgramType = cmbProgramType.SelectedIndex == 1 ? "1" : "0";
        tableProgramContent.EditModeClose();
        if (!ProgramContentJsonRules.TryToJson(_programContentRows, out var programContentJson, out var errorMessage))
        {
            ShowWarningMessage(errorMessage);
            return false;
        }

        request.ProgramContentJson = programContentJson;
        request.ProgramFilePath = File.Exists(txtProgramFile.Text.Trim()) ? txtProgramFile.Text.Trim() : string.Empty;
        request.WeldJobName = string.Empty;
        request.RobotJobName = string.Empty;
        request.CycleTimeSeconds = 0m;
        request.MesRemark = ResolveEditedMesRemark(GetEditingProgram());
        request.LocalRemark = inputDescription.Text.Trim();
        return true;
    }

    private BizProgram? GetEditingProgram()
    {
        return _programs.FirstOrDefault(program => program.Id == _editingId);
    }

    private string ResolveEditedMesRemark(BizProgram? editingProgram)
    {
        var current = inputRemark.Text.Trim();
        var original = editingProgram?.Remark?.Trim() ?? string.Empty;
        return string.Equals(current, original, StringComparison.Ordinal)
            ? string.Empty
            : current;
    }

    private string ResolveRecipeCodeForSave(BizProgram? editingProgram)
    {
        var current = inputRecipeCode.Text.Trim();
        if (!string.IsNullOrWhiteSpace(current))
        {
            return current;
        }

        var original = editingProgram?.RecipeCode?.Trim();
        return !string.IsNullOrWhiteSpace(original) && !int.TryParse(original, out _)
            ? original
            : string.Empty;
    }

    private static string GetAutoRemarkAction(BizProgram? program)
    {
        if (program is null || program.Id <= 0)
        {
            return AppConstants.ProgramRemarkActions.Create;
        }

        return program.SyncAction switch
        {
            AppConstants.ProgramSyncActions.Create => AppConstants.ProgramRemarkActions.Create,
            AppConstants.ProgramSyncActions.Delete => AppConstants.ProgramRemarkActions.Delete,
            _ when string.IsNullOrWhiteSpace(program.ProgramId) => AppConstants.ProgramRemarkActions.Create,
            _ => AppConstants.ProgramRemarkActions.Update
        };
    }

    private void ConfigureProgramContentColumns(bool dictionaryAvailable)
    {
        tableProgramContent.Columns.Clear();
        tableProgramContent.Columns.Add(CreateProgramContentColumn(
            nameof(ProgramContentItemRow.ItemName),
            "测试项名称",
            readOnly: dictionaryAvailable));
        tableProgramContent.Columns.Add(CreateProgramContentColumn(
            nameof(ProgramContentItemRow.StandardValue),
            "设定值/标准值",
            readOnly: false));
        TableStyleHelper.ApplyAntdColumnDefaults(tableProgramContent);
    }

    private static AntdUI.Column CreateProgramContentColumn(string key, string title, bool readOnly)
    {
        return new AntdUI.Column(key, title)
        {
            Align = AntdUI.ColumnAlign.Center,
            ColAlign = AntdUI.ColumnAlign.Center,
            ReadOnly = readOnly,
            Editable = !readOnly,
            Ellipsis = true
        };
    }

    private string BuildProgramNameFromInputs()
    {
        if (!int.TryParse(inputSequenceNumber.Text.Trim(), out var sequenceNumber))
        {
            sequenceNumber = 1;
        }

        return _programService.BuildProgramName(inputProductNum.Text.Trim(), inputComponentCode.Text.Trim(), sequenceNumber);
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
