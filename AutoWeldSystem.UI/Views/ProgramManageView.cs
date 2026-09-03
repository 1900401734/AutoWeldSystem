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
    private readonly List<BizProgram> _programs = new();
    private readonly List<BizProgram> _filteredPrograms = new();
    private readonly List<ProgramContentItemRow> _programContentRows = new();
    private readonly Dictionary<int, List<RecipeSelectionItem>> _recipeSelectionItems = new();
    private readonly Dictionary<int, bool> _recipeNameReadSucceeded = new();
    private int _editingId;
    private BizProgram? _editingProgram;
    private int _detailLoadVersion;
    private bool _initialized;
    private bool _programContentDictionaryAvailable;
    private int _recipeNameRefreshVersion;
    private bool _enableDualStation;
    private static readonly TimeSpan RecipeNameReadTimeout = TimeSpan.FromSeconds(10);
    private const int SuccessMessageAutoCloseSeconds = 4;
    private const int AlertMessageAutoCloseSeconds = 6;
    private readonly CancellationTokenSource _operationCts = new();
    private int _operationCtsDisposed;
    private bool _deleteInProgress;
    // 回写分页控件属性会触发 ValueChanged，用标志位避免重复绑定当前页。
    private bool _updatingProgramPagination;
    // InputQuery 按点击/回车回传关键字，不再逐字符触发，因此关键字需自己保存。
    private string _keyword = string.Empty;
    // 批量绑定控件值期间暂停自动填充，避免中间态触发多次重算。
    private bool _suppressNameAutoFill;

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

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (_initialized)
        {
            return;
        }

        _initialized = true;
        try
        {
            await ReloadProgramsAsync();
            _ = RefreshRecipeNameOptionsAsync();
            if (_programs.Count == 0)
            {
                StartNewProgram();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
    }


    private void DisposeOperationCts()
    {
        if (System.Threading.Interlocked.Exchange(ref _operationCtsDisposed, 1) != 0)
        {
            return;
        }

        _operationCts.Cancel();
        _operationCts.Dispose();
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
        // 同步状态列使用本地化文本，切换语言后按当前筛选结果重新生成。
        ApplyProgramFilter(_editingId);
    }

    private void ConfigureGrids()
    {
        TableStyleHelper.ApplyAntdTable(tablePrograms);
        // 折叠展示，避免一次铺开所有程序占满列表。
        tablePrograms.DefaultExpand = false;
        ConfigureProgramColumns();

        TableStyleHelper.ApplyAntdTable(tableProgramContent);
        // AntdUI 表格需要显式设置编辑触发方式，否则列 Editable=true 也不会进入编辑器。
        tableProgramContent.EditMode = AntdUI.TEditMode.DoubleClick;
        // 允许单元格失去焦点时提交编辑，保证最大允许值能进入保存流程。
        tableProgramContent.EditLostFocus = true;
        ConfigureProgramContentColumns(dictionaryAvailable: false);
    }

    /// <summary>
    /// 配置程序列表列。
    /// 工号列为树形列：同工号有多个程序时展开成子行，只有一个程序时不显示展开箭头。
    /// </summary>
    private void ConfigureProgramColumns()
    {
        var productNumColumn = new AntdUI.Column(
            nameof(ProgramProductGroupRow.ProductNum),
            _localizer.GetString(TextKeys.Grid.ProgramProductNum));
        productNumColumn.SetTree(nameof(ProgramProductGroupRow.Programs));

        tablePrograms.Columns = new AntdUI.ColumnCollection
        {
            new AntdUI.Column(
                nameof(ProgramProductGroupRow.SerialNumber),
                _localizer.GetString(TextKeys.Grid.ProgramSerialNumber)),
            productNumColumn,
            new AntdUI.Column(
                nameof(ProgramProductGroupRow.ProgramName),
                _localizer.GetString(TextKeys.Grid.ProgramName)),
            new AntdUI.Column(
                nameof(ProgramProductGroupRow.SyncStatus),
                _localizer.GetString(TextKeys.Grid.ProgramSyncStatus)),
            new AntdUI.Column(
                nameof(ProgramProductGroupRow.UpdatedTime),
                _localizer.GetString(TextKeys.Grid.ProgramUpdatedTime))
        };
    }

    private void WireEvents()
    {
        btnNew.Click += (_, _) => StartNewProgram();
        btnSave.Click += Save_ClickAsync;
        btnSaveAsNew.Click += SaveAsNew_ClickAsync;
        btnDelete.Click += Delete_ClickAsync;
        btnBatchClean.Click += BatchClean_ClickAsync;
        btnSync.Click += SyncSelected_ClickAsync;
        btnPullMes.Click += PullMes_ClickAsync;
        btnBuildName.Click += (_, _) => inputProgramName.Text = BuildProgramNameFromInputs();
        // InputQuery 的搜索与刷新共用一个事件：带关键字为搜索，空关键字为刷新。
        queryPrograms.QueryClick += ProgramQuery_QueryClickAsync;
        programPagination.ValueChanged += ProgramPagination_ValueChanged;
        // 父行（多程序工号）不指向具体程序，点击只展开子行，不切换编辑对象。
        tablePrograms.CellClick += (_, e) =>
        {
            if (e.Record is ProgramProductGroupRow row && row.ProgramId > 0)
            {
                BindProgramById(row.ProgramId);
            }
        };
        tableProgramContent.CellEndEdit += ProgramContentTable_CellEndEdit;

        // 名称组成字段变化时同步刷新程序名称，省去每次手点"生成名称"。
        inputProductNum.TextChanged += (_, _) => AutoFillProgramName();
        inputComponentCode.TextChanged += (_, _) => AutoFillProgramName();
        inputSequenceNumber.TextChanged += (_, _) => AutoFillProgramName();
        inputDescription.TextChanged += (_, _) => AutoFillProgramName();
    }

    private void ApplyLocalizedTexts()
    {
        btnNew.Text = _localizer.GetString(TextKeys.Common.ActionAdd);
        btnSave.Text = _localizer.GetString(TextKeys.Common.ActionSave);
        btnDelete.Text = _localizer.GetString(TextKeys.Common.ActionDelete);
        btnBatchClean.Text = _localizer.GetString(TextKeys.ProgramManage.ButtonBatchClean);
        btnSync.Text = _localizer.GetString(TextKeys.ProgramManage.ButtonSyncMes);
        btnSaveAsNew.Text = _localizer.GetString(TextKeys.ProgramManage.ButtonSaveAsNew);
        btnPullMes.Text = _localizer.GetString(TextKeys.ProgramManage.ButtonPullMes);
        btnBuildName.Text = _localizer.GetString(TextKeys.ProgramManage.ButtonBuildName);
        chkSyncNow.Text = _localizer.GetString(TextKeys.ProgramManage.CheckSyncNow);
        queryPrograms.PlaceholderText = _localizer.GetString(TextKeys.ProgramManage.PlaceholderKeyword);

        lblProgramName.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProgramName);
        lblProgramId.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProgramId);
        lblProductNum.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProductNum);
        lblRecipeCode1.Text = _localizer.GetString(TextKeys.ProgramManage.LabelStation1Recipe);
        lblRecipeCode2.Text = _localizer.GetString(TextKeys.ProgramManage.LabelStation2Recipe);
        lblComponentCode.Text = _localizer.GetString(TextKeys.ProgramManage.LabelComponentCode);
        lblSequenceNumber.Text = _localizer.GetString(TextKeys.ProgramManage.LabelSequenceNumber);
        lblProgramType.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProgramType);
        lblRemark.Text = _localizer.GetString(TextKeys.ProgramManage.LabelRemark);
        lblDescription.Text = _localizer.GetString(TextKeys.ProgramManage.LabelLocalRemark);
        lblProgramContent.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProgramContent);
    }

    private void ApplyGridHeaders()
    {
        // AntdUI 表格的列标题在构造时写入，切语言需重建列集合。
        ConfigureProgramColumns();

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

    /// <summary>
    /// 保存或删除后是否立即向 MES 同步。
    /// </summary>
    private bool SyncAfterSaveEnabled => chkSyncNow.Checked;

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


    private async Task ReloadProgramsAsync(int? selectedId = null)
    {
        var programs = await _programService.GetProgramLookupsAsync(_operationCts.Token);
        _programs.Clear();
        _programs.AddRange(programs.Select(program => program.ToEntityStub()));
        ApplyProgramFilter(selectedId);
    }

    /// <summary>
    /// 按关键字筛选程序并重新绑定列表当前页。
    /// </summary>
    /// <param name="selectedId">需要保持选中的程序本地 ID；不传时沿用正在编辑的程序。</param>
    /// <param name="resetPage">筛选条件变化时回到第一页，避免停在筛选后已不存在的页码上。</param>
    private void ApplyProgramFilter(int? selectedId = null, bool resetPage = false)
    {
        var keyword = _keyword;
        _filteredPrograms.Clear();
        _filteredPrograms.AddRange(_programs
            .Where(program => string.IsNullOrWhiteSpace(keyword)
                || Contains(program.ProgramName, keyword)
                || Contains(program.ProductNum, keyword)
                || Contains(program.ComponentCode, keyword)
                || Contains(program.Description, keyword)
                || Contains(program.SyncStatus, keyword)
                || Contains(GetSyncStatusText(program.SyncStatus), keyword)));

        BindProgramPage(
            resetPage ? 1 : programPagination.Current,
            programPagination.PageSize,
            selectedId ?? _editingId,
            rebindSelection: true);
    }

    /// <summary>
    /// 绑定筛选结果中的指定页。设备可存放上百个程序，列表按产品工号分组行分页显示。
    /// </summary>
    /// <param name="requestedPageIndex">目标页码；越界由分页规则夹到有效范围。</param>
    /// <param name="requestedPageSize">每页显示的产品工号分组数量。</param>
    /// <param name="keepProgramId">需要保持可见的程序本地 ID；命中时自动翻到它所在页。</param>
    /// <param name="rebindSelection">是否按当前页重新绑定右侧编辑区。</param>
    private void BindProgramPage(
        int requestedPageIndex,
        int requestedPageSize,
        int keepProgramId,
        bool rebindSelection)
    {
        var groups = ProgramProductGroupRules.BuildGroups(_filteredPrograms, program => GetSyncStatusText(program.SyncStatus));
        var page = ProgramListPagingRules.GetPage(groups, requestedPageIndex, requestedPageSize, keepProgramId);

        _updatingProgramPagination = true;
        try
        {
            programPagination.Total = page.TotalCount;
            programPagination.PageSize = page.PageSize;
            programPagination.Current = page.PageIndex;
        }
        finally
        {
            _updatingProgramPagination = false;
        }

        tablePrograms.DataSource = page.Items;
        if (!rebindSelection || page.Items.Count == 0)
        {
            return;
        }

        SelectProgramRow(keepProgramId, page.Items);
    }

    /// <summary>
    /// 处理 InputQuery 的搜索与刷新。
    /// 关键字为空表示点了刷新或清空了搜索框，此时重新载入程序列表和 PLC 配方名称；
    /// 关键字非空只在已加载的快照上筛选，并回到第一页。
    /// </summary>
    private async void ProgramQuery_QueryClickAsync(object? sender, string keyword)
    {
        _keyword = keyword.Trim();
        try
        {
            if (_keyword.Length == 0)
            {
                queryPrograms.Text = string.Empty;
                await ReloadProgramsAsync(_editingId);
                await RefreshRecipeNameOptionsAsync();
                return;
            }

            ApplyProgramFilter(resetPage: true);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
    }

    /// <summary>
    /// 手动翻页或改每页数量只切换可见页，不改变右侧正在编辑的程序，避免翻页丢失未保存内容。
    /// </summary>
    private void ProgramPagination_ValueChanged(object sender, AntdUI.PagePageEventArgs e)
    {
        if (_updatingProgramPagination)
        {
            return;
        }

        BindProgramPage(e.Current, e.PageSize, keepProgramId: 0, rebindSelection: false);
    }

    private static bool Contains(string? source, string keyword)
    {
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private void StartNewProgram()
    {
        _suppressNameAutoFill = true;
        // 新增状态不应继续保留列表旧行选择，否则再次点击同一行不会触发绑定。
        tablePrograms.SelectedIndex = -1;
        _editingId = 0;
        _editingProgram = null;
        Interlocked.Increment(ref _detailLoadVersion);
        txtProgramId.Clear();
        inputProgramName.Clear();
        inputProductNum.Clear();
        SetRecipeSelection(selectStation1Recipe, 1, string.Empty);
        SetRecipeSelection(selectStation2Recipe, 2, string.Empty);
        inputComponentCode.Clear();
        inputSequenceNumber.Text = "1";
        cmbProgramType.SelectedIndex = 0;
        BindRemarkText(null);
        inputDescription.Clear();
        BindProgramContentRows(null);
        lblCurrentInfo.Text = _localizer.GetString(TextKeys.ProgramManage.CurrentNew);
        _suppressNameAutoFill = false;
    }

    private async void BindProgramById(int programId)
    {
        var loadVersion = Interlocked.Increment(ref _detailLoadVersion);
        try
        {
            var program = await _programService.GetProgramAsync(programId, _operationCts.Token);
            if (program is null
                || loadVersion != Volatile.Read(ref _detailLoadVersion)
                || IsDisposed)
            {
                return;
            }

            _editingProgram = program;
            _suppressNameAutoFill = true;
            _editingId = program.Id;
            txtProgramId.Text = program.ProgramId ?? string.Empty;
            inputProgramName.Text = program.ProgramName;
            inputProductNum.Text = program.ProductNum;
            SetRecipeSelection(selectStation1Recipe, 1, program.RecipeCode, selectNotApplicable: true);
            SetRecipeSelection(selectStation2Recipe, 2, program.Station2RecipeCode, selectNotApplicable: true);
            inputComponentCode.Text = program.ComponentCode ?? string.Empty;
            inputSequenceNumber.Text = program.SequenceNumber.ToString();
            cmbProgramType.SelectedIndex = program.ProgramType == "1" ? 1 : 0;
            BindRemarkText(program.Remark);
            inputDescription.Text = program.Description ?? string.Empty;
            BindProgramContentRows(program.ProgramContent);
            SetCurrentProgramInfo(program);
            _suppressNameAutoFill = false;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
    }

    private void UpdateCurrentInfoText()
    {
        if (_editingId <= 0)
        {
            lblCurrentInfo.Text = _localizer.GetString(TextKeys.ProgramManage.CurrentNew);
            return;
        }

        var program = GetEditingProgram() ?? _programs.FirstOrDefault(item => item.Id == _editingId);
        if (program is not null)
        {
            SetCurrentProgramInfo(program);
        }
    }

    private void SetCurrentProgramInfo(BizProgram program)
    {
        lblCurrentInfo.Text = string.IsNullOrWhiteSpace(program.ProgramId)
            ? _localizer.GetString(TextKeys.ProgramManage.CurrentNotSynced)
            : _localizer.GetString(
                TextKeys.ProgramManage.CurrentSynced,
                program.ProgramId.Trim());
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

    /// <summary>
    /// 重新绑定编辑区到指定程序；传入 0 或该程序不在当前页时回落到当前页的第一个程序。
    /// </summary>
    private void SelectProgramRow(int id, IReadOnlyList<ProgramProductGroupRow> pageRows)
    {
        var programId = ProgramListPagingRules.ContainsProgram(pageRows, id)
            ? id
            : ProgramListPagingRules.ResolveFirstProgramId(pageRows);
        if (programId > 0)
        {
            BindProgramById(programId);
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
            var saveResult = await _programService.SaveWithSyncDecisionAsync(request, _operationCts.Token);
            var saved = saveResult.Program;
            var syncInBackground = SyncAfterSaveEnabled && saveResult.ShouldSyncNow;
            await ReloadProgramsAsync(saved.Id);
            ShowInfo(syncInBackground ? "程序已保存到本地，MES同步将在后台执行。" : _localizer.GetString(TextKeys.ProgramManage.SaveSuccess));
            if (syncInBackground)
            {
                _ = SyncProgramInBackgroundAsync(saved.Id);
            }
        }
        catch (OperationCanceledException)
        {
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

    /// <summary>
    /// 以当前编辑内容为基础，在同一产品工号下另存为一个新程序。
    /// 必须清空 _editingId 和 MES 程序ID，保存才会走新增；否则只会给原程序改名，
    /// 因为已有 ProgramId 的程序在同步时会把 Create 降级为 Update。
    /// </summary>
    private async void SaveAsNew_ClickAsync(object? sender, EventArgs e)
    {
        if (_editingId <= 0)
        {
            ShowWarning(TextKeys.ProgramManage.SelectDelete);
            return;
        }

        var productNum = inputProductNum.Text.Trim();
        if (string.IsNullOrWhiteSpace(productNum))
        {
            ShowWarning(TextKeys.ProgramManage.ProductNumRequired);
            return;
        }

        _editingId = 0;
        txtProgramId.Clear();
        btnSaveAsNew.Enabled = false;
        try
        {
            inputSequenceNumber.Text = (await _programService.GetNextSequenceNumberAsync(
                productNum,
                _operationCts.Token)).ToString();
            inputProgramName.Text = BuildProgramNameFromInputs();

            if (!TryBuildRequest(out var request))
            {
                return;
            }

            var saveResult = await _programService.SaveWithSyncDecisionAsync(request, _operationCts.Token);
            var saved = saveResult.Program;
            var syncInBackground = SyncAfterSaveEnabled && saveResult.ShouldSyncNow;
            await ReloadProgramsAsync(saved.Id);
            ShowInfo(syncInBackground ? "程序已保存到本地，MES同步将在后台执行。" : _localizer.GetString(TextKeys.ProgramManage.SaveSuccess));
            if (syncInBackground)
            {
                _ = SyncProgramInBackgroundAsync(saved.Id);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
        finally
        {
            btnSaveAsNew.Enabled = true;
        }
    }

    private async Task SyncProgramInBackgroundAsync(int programId)
    {
        try
        {
            await _programService.SyncProgramAsync(programId, _operationCts.Token);
            await RunOnUiThreadAsync(
                async () => await ReloadProgramsAsync(programId),
                "ProgramManageView.SyncProgram.Reload");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            RunOnUiThread(() => ShowErrorMessage(ex.Message), "ProgramManageView.SyncProgram.Error");
        }
    }

    private async void Delete_ClickAsync(object? sender, EventArgs e)
    {
        if (_programs.Count == 0)
        {
            ShowWarningMessage("当前没有可删除的加工程序。");
            return;
        }

        if (_editingId <= 0)
        {
            ShowWarning(TextKeys.ProgramManage.SelectDelete);
            return;
        }

        if (_deleteInProgress || !Confirm(TextKeys.ProgramManage.DeleteConfirm))
        {
            return;
        }

        _deleteInProgress = true;
        btnDelete.Enabled = false;
        try
        {
            var result = await _programService.DeleteLocalAsync(
                _editingId,
                ResolveEditedMesRemark(GetEditingProgram()),
                _operationCts.Token);
            var syncNow = SyncAfterSaveEnabled;
            await ReloadProgramsAsync();
            StartNewProgram();

            if (!syncNow || !result.RequiresMesSync)
            {
                return;
            }

            ShowInfo("程序已在本地删除，MES 删除将在后台执行。");
            _ = SyncDeletedProgramInBackgroundAsync(result.Id);
        }
        catch (OperationCanceledException)
        {
            ShowWarning("程序删除操作已取消。");
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
        finally
        {
            _deleteInProgress = false;
            btnDelete.Enabled = true;
        }
    }

    private async Task SyncDeletedProgramInBackgroundAsync(int programId)
    {
        try
        {
            await _programService.SyncProgramAsync(programId, _operationCts.Token);
            await RunOnUiThreadAsync(
                async () => await ReloadProgramsAsync(),
                "ProgramManageView.DeleteSync.Reload");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            RunOnUiThread(() => ShowErrorMessage($"MES 删除同步失败：{ex.Message}"), "ProgramManageView.DeleteSync.Error");
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
            await _programService.SyncProgramAsync(_editingId, _operationCts.Token);
            await ReloadProgramsAsync(_editingId);
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
            var count = await _programService.PullFromMesAsync(_operationCts.Token);
            await ReloadProgramsAsync();
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
        if (string.IsNullOrWhiteSpace(request.ProductNum))
        {
            ShowWarning(TextKeys.ProgramManage.ProductNumRequired);
            return false;
        }

        if (string.IsNullOrWhiteSpace(inputComponentCode.Text))
        {
            ShowWarning(TextKeys.ProgramManage.ComponentCodeRequired);
            return false;
        }

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
        request.Station2RecipeCode = selectStation2Recipe.Visible
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

        // 从配方下拉取出名称，注入到程序内容 JSON 最前面
        var station1RecipeName = ResolveSelectedRecipeName(selectStation1Recipe, 1);
        var station2RecipeName = ResolveSelectedRecipeName(selectStation2Recipe, 2);
        request.ProgramContentJson = ProgramContentJsonRules.MergeRecipeNamesAndContent(
            station1RecipeName,
            station2RecipeName,
            programContentJson);

        request.WeldJobName = string.Empty;
        request.RobotJobName = string.Empty;
        request.CycleTimeSeconds = 0m;
        request.MesRemark = ResolveEditedMesRemark(GetEditingProgram());
        request.LocalRemark = inputDescription.Text.Trim();
        return true;
    }

    private BizProgram? GetEditingProgram()
    {
        return _editingProgram?.Id == _editingId ? _editingProgram : null;
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
            var results = new List<(int StationNo, PlcRecipeNameReadResult Result)>();
            foreach (var stationNo in stationNumbers)
            {
                results.Add((stationNo, await ReadRecipeNameOptionsAsync(stationNo)));
            }

            if (refreshVersion != Volatile.Read(ref _recipeNameRefreshVersion))
            {
                return;
            }

            foreach (var (stationNo, result) in results)
            {
                var select = stationNo == 2 ? selectStation2Recipe : selectStation1Recipe;
                // PLC 读取期间用户可能点击新增或切换程序，必须以统一绑定时的实时编辑值为准。
                var liveRecipeCode = ResolveSelectedRecipeCode(select, stationNo);
                if (string.IsNullOrWhiteSpace(liveRecipeCode) && GetEditingProgram() is { } editingProgram)
                {
                    liveRecipeCode = stationNo == 2 ? editingProgram.Station2RecipeCode : editingProgram.RecipeCode;
                }
                BindRecipeNameOptions(select, stationNo, result, liveRecipeCode);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (refreshVersion != Volatile.Read(ref _recipeNameRefreshVersion))
            {
                return;
            }

            BindRecipeNameReadFailure(selectStation1Recipe, 1, ex);
            if (selectStation2Recipe.Visible)
            {
                BindRecipeNameReadFailure(selectStation2Recipe, 2, ex);
            }
        }
    }

    private async Task<PlcRecipeNameReadResult> ReadRecipeNameOptionsAsync(int stationNo)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_operationCts.Token);
        try
        {
            return await _recipeNameReaderService
                .ReadStationAsync(stationNo, timeoutCts.Token)
                .WaitAsync(RecipeNameReadTimeout, _operationCts.Token);
        }
        catch (TimeoutException)
        {
            timeoutCts.Cancel();
            return new PlcRecipeNameReadResult(
                stationNo,
                false,
                $"工位 {stationNo} 配方名称读取超时。",
                Array.Empty<PlcRecipeNameOption>(),
                Array.Empty<PlcRecipeNameReadFailure>());
        }
        catch (OperationCanceledException) when (!_operationCts.IsCancellationRequested)
        {
            return new PlcRecipeNameReadResult(
                stationNo,
                false,
                $"工位 {stationNo} 配方名称读取超时。",
                Array.Empty<PlcRecipeNameOption>(),
                Array.Empty<PlcRecipeNameReadFailure>());
        }
    }

    private void BindRecipeNameReadFailure(AntdUI.Select select, int stationNo, Exception exception)
    {
        _ = exception;
        var editingProgram = GetEditingProgram();
        var currentRecipeCode = stationNo == 2 ? editingProgram?.Station2RecipeCode : editingProgram?.RecipeCode;
        _recipeNameReadSucceeded[stationNo] = false;
        SetRecipeSelectorItems(select, stationNo, BuildUnavailableItems(currentRecipeCode));
        select.List = true;
        select.ReadOnly = true;
        select.PlaceholderText = _localizer.GetString(TextKeys.ProgramManage.RecipeReadFailed);
        SetRecipeSelection(select, stationNo, currentRecipeCode);
    }

    /// <summary>
    /// 单工位完全折叠工位 2 配方行，避免留下空白间距。
    /// 行高改为自适应：隐藏整行容器后行高自然归零，不写死像素值，
    /// 避免行高随字体或 DPI 变化后与其它字段行不一致。
    /// </summary>
    private void ApplyStationRecipeLayout(bool enableDualStation)
    {
        _enableDualStation = enableDualStation;
        tlpRecipe2.Visible = enableDualStation;
        editorLayout.RowStyles[7].SizeType = enableDualStation ? SizeType.AutoSize : SizeType.Absolute;
        editorLayout.RowStyles[7].Height = 0F;
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
        SetRecipeSelectorItems(select, stationNo, items);
        select.List = true;
        select.ReadOnly = !result.IsSuccess;
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

        var itemCount = items.Count;
        AddMissingRecipeOption(items, normalized);
        if (itemCount != items.Count)
        {
            RefreshRecipeSelectorItems(select, stationNo);
        }
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

    /// <summary>
    /// 从配方下拉取出选中的配方名称（不是数字配方号）。
    /// 用于注入 ProgramContent，随 MES 同步给其他设备。
    /// </summary>
    private string? ResolveSelectedRecipeName(AntdUI.Select select, int stationNo)
    {
        if (!_recipeSelectionItems.TryGetValue(stationNo, out var items)
            || select.SelectedIndex < 0
            || select.SelectedIndex >= items.Count)
        {
            return null;
        }

        var item = items[select.SelectedIndex];
        // 「不适用」和历史失效选项不写配方名
        return item.Kind == RecipeSelectionKind.PlcOption ? item.DisplayText : null;
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
            items = items.Select(item => item.Kind switch
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
            }).ToList();
            SetRecipeSelectorItems(select, stationNo, items);
        }
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

    private void SetRecipeSelectorItems(
        AntdUI.Select select,
        int stationNo,
        IReadOnlyList<RecipeSelectionItem> items)
    {
        var normalizedItems = items.ToList();
        if (_recipeSelectionItems.TryGetValue(stationNo, out var existingItems)
            && existingItems.SequenceEqual(normalizedItems))
        {
            return;
        }

        _recipeSelectionItems[stationNo] = normalizedItems;
        RefreshRecipeSelectorItems(select, stationNo);
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
            "最大允许值",
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

    /// <summary>
    /// 名称组成字段变化时刷新程序名称。
    /// 仅新增状态生效：已有程序的名称已同步给 MES，改名要走"生成名称"按钮显式确认。
    /// </summary>
    private void AutoFillProgramName()
    {
        if (_suppressNameAutoFill || _editingId > 0)
        {
            return;
        }

        inputProgramName.Text = BuildProgramNameFromInputs();
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

    private void ShowInfo(string messageKey, params object[] args)
    {
        ShowInfoMessage(_localizer.GetString(messageKey, args));
    }

    private void ShowInfoMessage(string message)
    {
        if (FindForm() is not { IsDisposed: false, Disposing: false } owner)
        {
            return;
        }

        AntdUI.Message.success(owner, message, autoClose: SuccessMessageAutoCloseSeconds);
    }

    private void ShowWarning(string messageKey, params object[] args)
    {
        ShowWarningMessage(_localizer.GetString(messageKey, args));
    }

    private void ShowWarningMessage(string message)
    {
        if (FindForm() is not { IsDisposed: false, Disposing: false } owner)
        {
            return;
        }

        AntdUI.Message.warn(owner, message, autoClose: AlertMessageAutoCloseSeconds);
    }

    private void ShowErrorMessage(string message)
    {
        if (FindForm() is not { IsDisposed: false, Disposing: false } owner)
        {
            return;
        }

        AntdUI.Message.error(owner, message, autoClose: AlertMessageAutoCloseSeconds);
    }

    private IWin32Window GetDialogOwner()
    {
        var owner = FindForm();
        return owner is null || owner.IsDisposed || owner.Disposing ? this : owner;
    }

    private async void BatchClean_ClickAsync(object? sender, EventArgs e)
    {
        var pendingIds = _programs
            .Where(p => p.SyncStatus != AppConstants.ProgramSyncStatus.Synced)
            .Select(p => p.Id)
            .ToList();

        if (pendingIds.Count == 0)
        {
            ShowWarningMessage("没有需要清理的程序。");
            return;
        }

        var confirmMessage = _localizer.GetString(TextKeys.ProgramManage.MessageConfirmBatchClean);
        var result = MessageBox.Show(
            GetDialogOwner(),
            confirmMessage,
            _localizer.GetString(TextKeys.Common.TitleWarning),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        btnBatchClean.Enabled = false;
        try
        {
            var deleteCount = await _programService.BatchDeleteLocalProgramsAsync(pendingIds, _operationCts.Token);
            await ReloadProgramsAsync();
            if (_programs.Count == 0)
            {
                StartNewProgram();
            }

            ShowInfo(TextKeys.ProgramManage.MessageBatchCleanSuccess, deleteCount);
        }
        catch (OperationCanceledException)
        {
            ShowWarningMessage("批量清理已取消。");
        }
        catch (Exception ex)
        {
            ShowErrorMessage(_localizer.GetString(TextKeys.ProgramManage.MessageBatchCleanFailed, ex.Message));
        }
        finally
        {
            btnBatchClean.Enabled = true;
        }
    }

    private bool Confirm(string messageKey, params object[] args)
    {
        var message = _localizer.GetString(messageKey, args);
        return MessageBox.Show(GetDialogOwner(), message, _localizer.GetString(TextKeys.Common.TitleConfirmDelete), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            == DialogResult.Yes;
    }
}
