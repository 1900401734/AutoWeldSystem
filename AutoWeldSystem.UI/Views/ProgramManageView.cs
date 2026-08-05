using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.PLC;
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
    private enum RecipeSelectionKind
    {
        PlcOption,
        NotApplicable,
        MissingExisting
    }

    private sealed record RecipeSelectionItem(
        string DisplayText,
        string? RecipeCode,
        RecipeSelectionKind Kind);

    private readonly IProgramManageService _programService;
    private readonly ITestSchemeConfigService _testSchemeConfigService;
    private readonly IPlcRecipeNameReaderService _recipeNameReaderService;
    private readonly IAppSettingsService _appSettingsService;
    private readonly ILocalizationService _localizer;
    private readonly BindingSource _programBindingSource = new();
    private readonly BindingSource _revisionBindingSource = new();
    private readonly List<BizProgram> _programs = new();
    private readonly List<ProgramContentItemRow> _programContentRows = new();
    private readonly Dictionary<int, List<RecipeSelectionItem>> _recipeSelectionItems = new();
    private readonly Dictionary<int, bool> _recipeNameReadSucceeded = new();
    private int _editingId;
    private bool _initialized;
    private bool _programContentDictionaryAvailable;
    private int _recipeNameRefreshVersion;
    private bool _enableDualStation;

    public ProgramManageView(
        IProgramManageService programService,
        ITestSchemeConfigService testSchemeConfigService,
        IPlcRecipeNameReaderService recipeNameReaderService,
        IAppSettingsService appSettingsService,
        ILocalizationService localizer)
    {
        _programService = programService;
        _testSchemeConfigService = testSchemeConfigService;
        _recipeNameReaderService = recipeNameReaderService;
        _appSettingsService = appSettingsService;
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
        _ = RefreshRecipeNameOptionsAsync();
        if (_programs.Count == 0)
        {
            StartNewProgram();
        }
    }

    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ApplyGridHeaders();
        ConfigureProgramContentColumns(_programContentDictionaryAvailable);
        BindProgramTypeOptions();
        BindRemarkText(inputRemark.Text);
        RefreshRecipeSelectorTexts();
        UpdateCurrentInfoText();
        dgvPrograms.Refresh();
    }

    private void ConfigureGrids()
    {
        TableStyleHelper.ApplyDataGridView(dgvPrograms);
        dgvPrograms.AutoGenerateColumns = false;
        dgvPrograms.Columns.Clear();
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
        btnRefresh.Click += async (_, _) =>
        {
            ReloadPrograms();
            await RefreshRecipeNameOptionsAsync();
        };
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
        lblRecipeCode.Text = _localizer.GetString(TextKeys.ProgramManage.LabelStation1Recipe);
        lblStation2RecipeCode.Text = _localizer.GetString(TextKeys.ProgramManage.LabelStation2Recipe);
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
                || Contains(program.ProductNum, keyword)
                || Contains(program.ComponentCode, keyword)
                || Contains(program.Description, keyword)
                || Contains(program.SyncStatus, keyword)
                || Contains(GetSyncStatusText(program.SyncStatus), keyword))
            .OrderBy(program => NormalizeSortText(program.ProductNum), StringComparer.OrdinalIgnoreCase)
            .ThenBy(program => NormalizeSortText(program.ProgramName), StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(program => program.UpdatedTime)
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
    private static string NormalizeSortText(string? value)
        => value?.Trim() ?? string.Empty;

    private void StartNewProgram()
    {
        // 新增状态不应继续保留左侧旧行选择，否则再次点击同一行不会触发 SelectionChanged。
        dgvPrograms.ClearSelection();
        dgvPrograms.CurrentCell = null;
        _editingId = 0;
        txtProgramId.Clear();
        inputProgramName.Clear();
        inputProductNum.Clear();
        SetRecipeSelection(selectStation1Recipe, 1, string.Empty);
        SetRecipeSelection(selectStation2Recipe, 2, string.Empty);
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
        SetRecipeSelection(selectStation1Recipe, 1, program.RecipeCode, selectNotApplicable: true);
        SetRecipeSelection(selectStation2Recipe, 2, program.Station2RecipeCode, selectNotApplicable: true);
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

        request.ProductNum = inputProductNum.Text.Trim();
        var editingProgram = GetEditingProgram();
        if (_editingId <= 0
            && (!_recipeNameReadSucceeded.TryGetValue(1, out var station1ReadSucceeded) || !station1ReadSucceeded
                || (_enableDualStation
                    && (!_recipeNameReadSucceeded.TryGetValue(2, out var station2ReadSucceeded) || !station2ReadSucceeded))))
        {
            ShowWarning(TextKeys.ProgramManage.RecipeReadFailed);
            return false;
        }

        request.RecipeCode = ResolveRecipeCodeForSave(selectStation1Recipe, 1, editingProgram) ?? string.Empty;
        request.Station2RecipeCode = tlpStation2RecipeCode.Visible
            ? ResolveRecipeCodeForSave(selectStation2Recipe, 2, editingProgram)
            : _editingId > 0
                ? editingProgram?.Station2RecipeCode
                : null;
        try
        {
            ProgramSaveRecipeRules.Validate(request.RecipeCode, request.Station2RecipeCode, _enableDualStation);
        }
        catch (InvalidOperationException ex)
        {
            ShowWarningMessage(ex.Message);
            return false;
        }
        request.ComponentCode = inputComponentCode.Text.Trim();
        request.SequenceNumber = sequenceNumber;
        request.ProgramName = _editingId <= 0
            ? _programService.BuildProgramName(
                request.ProductNum,
                request.ComponentCode,
                request.SequenceNumber,
                inputDescription.Text.Trim())
            : inputProgramName.Text.Trim();
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

    private string? ResolveRecipeCodeForSave(AntdUI.Select select, int stationNo, BizProgram? editingProgram)
    {
        if (_editingId > 0
            && (!_recipeNameReadSucceeded.TryGetValue(stationNo, out var readSucceeded) || !readSucceeded))
        {
            return stationNo == 2 ? editingProgram?.Station2RecipeCode : editingProgram?.RecipeCode;
        }

        return ResolveSelectedRecipeCode(select, stationNo);
    }

    /// <summary>
    /// 从 PLC 刷新配方名称列表；业务界面始终只允许选择名称，不接受手工配方号。
    /// </summary>
    private async Task RefreshRecipeNameOptionsAsync()
    {
        var refreshVersion = Interlocked.Increment(ref _recipeNameRefreshVersion);
        try
        {
            var settings = _appSettingsService.Get();
            ApplyStationRecipeLayout(settings.EnableDualStation);

            var stationNumbers = settings.EnableDualStation ? new[] { 1, 2 } : new[] { 1 };
            foreach (var stationNo in stationNumbers)
            {
                var select = stationNo == 2 ? selectStation2Recipe : selectStation1Recipe;
                var result = await _recipeNameReaderService.ReadStationAsync(stationNo);
                if (refreshVersion != Volatile.Read(ref _recipeNameRefreshVersion))
                {
                    return;
                }

                // PLC 读取期间用户可能点击新增或切换程序，必须以 await 返回后的实时编辑值为准。
                var liveRecipeCode = ResolveSelectedRecipeCode(select, stationNo);
                if (string.IsNullOrWhiteSpace(liveRecipeCode) && GetEditingProgram() is { } editingProgram)
                {
                    liveRecipeCode = stationNo == 2 ? editingProgram.Station2RecipeCode : editingProgram.RecipeCode;
                }
                BindRecipeNameOptions(select, stationNo, result, liveRecipeCode);
            }
        }
        catch (Exception ex)
        {
            if (refreshVersion != Volatile.Read(ref _recipeNameRefreshVersion))
            {
                return;
            }

            BindRecipeNameReadFailure(selectStation1Recipe, 1, ex);
            if (tlpStation2RecipeCode.Visible)
            {
                BindRecipeNameReadFailure(selectStation2Recipe, 2, ex);
            }
        }
    }

    private void BindRecipeNameReadFailure(AntdUI.Select select, int stationNo, Exception exception)
    {
        _ = exception;
        var editingProgram = GetEditingProgram();
        var currentRecipeCode = stationNo == 2 ? editingProgram?.Station2RecipeCode : editingProgram?.RecipeCode;
        _recipeNameReadSucceeded[stationNo] = false;
        _recipeSelectionItems[stationNo] = BuildUnavailableItems(currentRecipeCode);
        select.List = true;
        select.ReadOnly = true;
        RefreshRecipeSelectorItems(select, stationNo);
        select.PlaceholderText = _localizer.GetString(TextKeys.ProgramManage.RecipeReadFailed);
        SetRecipeSelection(select, stationNo, currentRecipeCode);
    }

    /// <summary>
    /// 单工位完全折叠工位 2 配方行，避免留下空白间距。
    /// </summary>
    private void ApplyStationRecipeLayout(bool enableDualStation)
    {
        _enableDualStation = enableDualStation;
        tlpStation2RecipeCode.Visible = enableDualStation;
        editorLayout.RowStyles[8].SizeType = SizeType.Absolute;
        editorLayout.RowStyles[8].Height = enableDualStation ? 44F : 0F;
    }

    private void BindRecipeNameOptions(
        AntdUI.Select select,
        int stationNo,
        PlcRecipeNameReadResult result,
        string? currentRecipeCode)
    {
        _recipeNameReadSucceeded[stationNo] = result.IsSuccess;
        var items = result.IsSuccess
            ? result.Options.Select(option => new RecipeSelectionItem(
                option.Name,
                option.RecipeCode.ToString(),
                RecipeSelectionKind.PlcOption)).ToList()
            : BuildUnavailableItems(currentRecipeCode);

        if (result.IsSuccess && _enableDualStation)
        {
            items.Add(new RecipeSelectionItem(
                _localizer.GetString(TextKeys.ProgramManage.RecipeNotApplicable),
                null,
                RecipeSelectionKind.NotApplicable));
        }

        AddMissingRecipeOption(items, currentRecipeCode);
        _recipeSelectionItems[stationNo] = items;
        select.List = true;
        select.ReadOnly = !result.IsSuccess;
        RefreshRecipeSelectorItems(select, stationNo);
        select.PlaceholderText = _localizer.GetString(result.IsSuccess
            ? TextKeys.ProgramManage.PlaceholderRecipeSelect
            : TextKeys.ProgramManage.RecipeReadFailed);
        SetRecipeSelection(
            select,
            stationNo,
            currentRecipeCode,
            selectNotApplicable: result.IsSuccess && _editingId > 0);
    }

    private List<RecipeSelectionItem> BuildUnavailableItems(string? recipeCode)
    {
        var items = new List<RecipeSelectionItem>();
        AddMissingRecipeOption(items, recipeCode);
        return items;
    }

    private void AddMissingRecipeOption(ICollection<RecipeSelectionItem> items, string? recipeCode)
    {
        var normalized = ProgramRecipeMappingRules.Normalize(recipeCode);
        if (string.IsNullOrWhiteSpace(normalized)
            || items.Any(item => string.Equals(item.RecipeCode, normalized, StringComparison.Ordinal)))
        {
            return;
        }

        items.Add(new RecipeSelectionItem(
            _localizer.GetString(TextKeys.ProgramManage.MissingRecipeOption),
            normalized,
            RecipeSelectionKind.MissingExisting));
    }

    private void SetRecipeSelection(
        AntdUI.Select select,
        int stationNo,
        string? recipeCode,
        bool selectNotApplicable = false)
    {
        var normalized = ProgramRecipeMappingRules.Normalize(recipeCode);
        if (!_recipeSelectionItems.TryGetValue(stationNo, out var items))
        {
            items = [];
            _recipeSelectionItems[stationNo] = items;
        }

        AddMissingRecipeOption(items, normalized);
        RefreshRecipeSelectorItems(select, stationNo);
        var selectedIndex = !string.IsNullOrWhiteSpace(normalized)
            ? items.FindIndex(item => string.Equals(item.RecipeCode, normalized, StringComparison.Ordinal))
            : selectNotApplicable
                ? items.FindIndex(item => item.Kind == RecipeSelectionKind.NotApplicable)
                : -1;
        select.SelectedIndex = selectedIndex;
        select.Text = selectedIndex >= 0 ? items[selectedIndex].DisplayText : string.Empty;
    }

    private string? ResolveSelectedRecipeCode(AntdUI.Select select, int stationNo)
    {
        if (!_recipeSelectionItems.TryGetValue(stationNo, out var items)
            || select.SelectedIndex < 0
            || select.SelectedIndex >= items.Count)
        {
            return null;
        }

        return items[select.SelectedIndex].RecipeCode;
    }

    private void RefreshRecipeSelectorTexts()
    {
        RefreshRecipeSelectorText(selectStation1Recipe, 1);
        RefreshRecipeSelectorText(selectStation2Recipe, 2);
    }

    private void RefreshRecipeSelectorText(AntdUI.Select select, int stationNo)
    {
        var recipeCode = ResolveSelectedRecipeCode(select, stationNo);
        var kind = _recipeSelectionItems.TryGetValue(stationNo, out var items)
            && select.SelectedIndex >= 0
            && select.SelectedIndex < items.Count
                ? items[select.SelectedIndex].Kind
                : (RecipeSelectionKind?)null;

        if (items is not null)
        {
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                items[index] = item.Kind switch
                {
                    RecipeSelectionKind.NotApplicable => item with
                    {
                        DisplayText = _localizer.GetString(TextKeys.ProgramManage.RecipeNotApplicable)
                    },
                    RecipeSelectionKind.MissingExisting => item with
                    {
                        DisplayText = _localizer.GetString(TextKeys.ProgramManage.MissingRecipeOption)
                    },
                    _ => item
                };
            }
        }

        RefreshRecipeSelectorItems(select, stationNo);
        select.PlaceholderText = _localizer.GetString(
            _recipeNameReadSucceeded.TryGetValue(stationNo, out var succeeded) && succeeded
                ? TextKeys.ProgramManage.PlaceholderRecipeSelect
                : TextKeys.ProgramManage.RecipeReadFailed);
        if (kind == RecipeSelectionKind.NotApplicable)
        {
            var notApplicableIndex = items?.FindIndex(item => item.Kind == RecipeSelectionKind.NotApplicable) ?? -1;
            select.SelectedIndex = notApplicableIndex;
            select.Text = notApplicableIndex >= 0 ? items![notApplicableIndex].DisplayText : string.Empty;
        }
        else
        {
            SetRecipeSelection(select, stationNo, recipeCode);
        }
    }

    private void RefreshRecipeSelectorItems(AntdUI.Select select, int stationNo)
    {
        if (!_recipeSelectionItems.TryGetValue(stationNo, out var items))
        {
            items = [];
            _recipeSelectionItems[stationNo] = items;
        }

        select.Items.Clear();
        select.Items.AddRange(items.Select(item => (object)item.DisplayText).ToArray());
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

        return _programService.BuildProgramName(
            inputProductNum.Text.Trim(),
            inputComponentCode.Text.Trim(),
            sequenceNumber,
            inputDescription.Text.Trim());
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
