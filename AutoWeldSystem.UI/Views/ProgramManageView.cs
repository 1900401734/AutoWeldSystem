using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;
using System.Globalization;

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
    private string _editingContent = "{}";
    private int _detailLoadVersion;
    private bool _initialized;
    private bool _wasVisible;
    private bool _detailLoading;
    private string? _contentError;
    private bool _programOperationInProgress;
    private bool _recipeNameRefreshing;
    private bool _programContentDictionaryAvailable;
    private int _recipeNameRefreshVersion;
    private bool _enableDualStation;
    private static readonly TimeSpan RecipeNameReadTimeout = TimeSpan.FromSeconds(10);
    private const int SuccessMessageAutoCloseSeconds = 4;
    private const int AlertMessageAutoCloseSeconds = 6;
    private readonly CancellationTokenSource _operationCts = new();
    private int _operationCtsDisposed;
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
        _wasVisible = Visible;
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

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        var becameVisible = Visible && !_wasVisible;
        _wasVisible = Visible;
        if (_initialized && becameVisible && !IsDisposed && !Disposing)
        {
            // 主窗体缓存页面；返回时只刷新配方，不重绑未保存的程序内容。
            _ = RefreshRecipeNameOptionsAsync();
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
        ConfigureProgramColumns();

        TableStyleHelper.ApplyAntdTable(tableProgramContent);
        ConfigureProgramContentColumns(dictionaryAvailable: false);
    }

    /// <summary>
    /// 配置程序列表列。
    /// 每个程序独占一行，产品工号重复显示。
    /// </summary>
    private void ConfigureProgramColumns()
    {
        var productNumColumn = new AntdUI.Column(
            nameof(ProgramProductGroupRow.ProductNum),
            _localizer.GetString(TextKeys.Grid.ProgramProductNum));

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
        btnNew.Click += (_, _) =>
        {
            if (!_programOperationInProgress) StartNewProgram();
        };
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
        tablePrograms.CellClick += (_, e) =>
        {
            if (!_programOperationInProgress && e.Record is ProgramProductGroupRow row)
            {
                BindProgramById(row.ProgramId);
            }
        };
        tableProgramContent.CellEndEdit += ProgramContentTable_CellEndEdit;
        inputTouchCount.TextChanged += (_, _) => inputTouchCount.Status = AntdUI.TType.None;

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
        var profile = ProcessParameterDeviceUiProfile.Resolve(_appSettingsService.Get().ProcessParameterDeviceType);
        lblTouchCount.Text = profile.PointName == "面"
            ? _localizer.GetString(TextKeys.ProgramManage.LabelFaceCount)
            : _localizer.GetString(TextKeys.ProgramManage.LabelTouchCount);
        inputTouchCount.PlaceholderText = _localizer.GetString(TextKeys.ProgramManage.PlaceholderTouchCount);
        lblRecipeCode1.Text = _localizer.GetString(TextKeys.ProgramManage.LabelStation1Recipe);
        lblRecipeCode2.Text = _localizer.GetString(TextKeys.ProgramManage.LabelStation2Recipe);
        lblComponentCode.Text = _localizer.GetString(TextKeys.ProgramManage.LabelComponentCode);
        lblSequenceNumber.Text = _localizer.GetString(TextKeys.ProgramManage.LabelSequenceNumber);
        lblProgramType.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProgramType);
        lblRemark.Text = _localizer.GetString(TextKeys.ProgramManage.LabelRemark);
        lblDescription.Text = _localizer.GetString(TextKeys.ProgramManage.LabelLocalRemark);
        grpProgramContent.Text = _localizer.GetString(TextKeys.ProgramManage.LabelProgramContent);
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
        var rows = ProgramContentJsonRules.BuildRows(dictionaryItems, programContentJson);
        BindProgramContentRows(rows, dictionaryItems.Any(item => !string.IsNullOrWhiteSpace(item.ItemName)));
    }

    private void BindProgramContentRows(IReadOnlyList<ProgramContentItemRow> rows, bool dictionaryAvailable)
    {
        _programContentDictionaryAvailable = dictionaryAvailable;
        _programContentRows.Clear();
        _programContentRows.AddRange(rows);
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
        => string.IsNullOrWhiteSpace(row.ItemName) && string.IsNullOrWhiteSpace(row.UpperLimit) && string.IsNullOrWhiteSpace(row.LowerLimit);


    private async Task ReloadProgramsAsync(int? selectedId = null)
    {
        var programs = await _programService.GetProgramLookupsAsync(_operationCts.Token);
        if (IsDisposed || Disposing) return;
        // 新列表取代旧详情请求，避免清理后迟到的详情把已删程序重新绑定回来。
        Interlocked.Increment(ref _detailLoadVersion);
        _detailLoading = false;
        _programs.Clear();
        _programs.AddRange(programs.Select(program => program.ToEntityStub()));
        if (_editingId > 0 && _programs.All(program => program.Id != _editingId))
        {
            StartNewProgram();
        }
        ApplyProgramFilter(selectedId);
        UpdateProgramActions();
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
    /// 绑定筛选结果中的指定页。设备可存放上百个程序，列表按程序行分页显示。
    /// </summary>
    /// <param name="requestedPageIndex">目标页码；越界由分页规则夹到有效范围。</param>
    /// <param name="requestedPageSize">每页显示的程序行数量。</param>
    /// <param name="keepProgramId">需要保持可见的程序本地 ID；命中时自动翻到它所在页。</param>
    /// <param name="rebindSelection">是否按当前页重新绑定右侧编辑区。</param>
    private void BindProgramPage(
        int requestedPageIndex,
        int requestedPageSize,
        int keepProgramId,
        bool rebindSelection)
    {
        var rows = ProgramProductGroupRules.BuildRows(_filteredPrograms, program => GetSyncStatusText(program.SyncStatus));
        var page = ProgramListPagingRules.GetPage(rows, requestedPageIndex, requestedPageSize, keepProgramId);

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
        if (_programOperationInProgress) return;
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
        _editingContent = "{}";
        _contentError = null;
        _detailLoading = false;
        Interlocked.Increment(ref _detailLoadVersion);
        txtProgramId.Clear();
        inputProgramName.Clear();
        inputProductNum.Clear();
        SetRecipeSelection(selectStation1Recipe, 1, string.Empty);
        SetRecipeSelection(selectStation2Recipe, 2, string.Empty);
        inputComponentCode.Clear();
        inputSequenceNumber.Text = "1";
        inputTouchCount.Clear();
        inputTouchCount.Status = AntdUI.TType.None;
        cmbProgramType.SelectedIndex = 0;
        BindRemarkText(null);
        inputDescription.Clear();
        BindProgramContentRows(null);
        lblCurrentInfo.Text = _localizer.GetString(TextKeys.ProgramManage.CurrentNew);
        _suppressNameAutoFill = false;
        UpdateProgramActions();
    }

    private void UpdateProgramActions()
    {
        if (IsDisposed || Disposing) return;
        var idle = !_programOperationInProgress;
        var editable = idle && !_detailLoading && _contentError is null;
        editorLayout.Enabled = editable;
        grpProgramContent.Enabled = editable;
        btnSave.Enabled = editable && !_recipeNameRefreshing;
        btnSaveAsNew.Enabled = editable && !_recipeNameRefreshing;
        btnSync.Enabled = editable;
        btnDelete.Enabled = idle && !_detailLoading;
        btnBatchClean.Enabled = idle;
        btnNew.Enabled = idle;
        tablePrograms.Enabled = idle;
        btnPullMes.Enabled = idle;
        queryPrograms.Enabled = idle;
    }

    private bool CanEditProgram()
    {
        if (_detailLoading)
        {
            ShowWarning(TextKeys.ProgramManage.DetailLoading);
            return false;
        }
        if (_contentError is not null)
        {
            ShowWarning(TextKeys.ProgramManage.ContentInvalid, _contentError);
            return false;
        }
        return !_programOperationInProgress;
    }

    private async void BindProgramById(int programId)
    {
        var loadVersion = Interlocked.Increment(ref _detailLoadVersion);
        _detailLoading = true;
        UpdateProgramActions();
        try
        {
            var program = await _programService.GetProgramAsync(programId, _operationCts.Token);
            if (loadVersion != Volatile.Read(ref _detailLoadVersion) || IsDisposed || Disposing)
            {
                return;
            }
            if (program is null || program.IsDeleted)
            {
                StartNewProgram();
                return;
            }

            var dictionaryItems = _testSchemeConfigService.GetItems();
            var content = program.ProgramContent;
            string? contentError = null;
            IReadOnlyList<ProgramContentItemRow> rows = Array.Empty<ProgramContentItemRow>();
            try
            {
                _ = ProgramContentJsonRules.NormalizeContent(content, _appSettingsService.Get().ProcessParameterDeviceType, requireTouchCount: false);
                rows = ProgramContentJsonRules.BuildRows(dictionaryItems, content);
            }
            catch (InvalidOperationException ex)
            {
                if (ProgramContentJsonRules.TryCreateReconfigurationContent(content, out var metadata)
                    && MessageBox.Show(GetDialogOwner(), $"{ex.Message}\n\n是否重新配置此程序？保留程序身份和有效配方元数据，旧限值不预填；保存前不修改数据库。",
                        "重新配置", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    content = metadata;
                    rows = ProgramContentJsonRules.BuildRows(dictionaryItems, metadata);
                }
                else
                {
                    contentError = ex.Message;
                }
            }

            if (loadVersion != Volatile.Read(ref _detailLoadVersion) || IsDisposed || Disposing) return;
            // 内容无效也必须绑定当前身份供删除；不能回退到上一程序或用空限值覆盖原内容。
            _editingProgram = program;
            _editingContent = content ?? "{}";
            _contentError = contentError;
            _suppressNameAutoFill = true;
            _editingId = program.Id;
            txtProgramId.Text = program.ProgramId ?? string.Empty;
            inputProgramName.Text = program.ProgramName;
            inputProductNum.Text = program.ProductNum;
            SetRecipeSelection(selectStation1Recipe, 1, program.RecipeCode, selectNotApplicable: true);
            SetRecipeSelection(selectStation2Recipe, 2, program.Station2RecipeCode, selectNotApplicable: true);
            inputComponentCode.Text = program.ComponentCode ?? string.Empty;
            inputSequenceNumber.Text = program.SequenceNumber.ToString();
            inputTouchCount.Text = ProgramContentJsonRules.TryGetTouchCount(content, out var touchCount)
                ? touchCount.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
            inputTouchCount.Status = AntdUI.TType.None;
            cmbProgramType.SelectedIndex = program.ProgramType == "1" ? 1 : 0;
            BindRemarkText(program.Remark);
            inputDescription.Text = program.Description ?? string.Empty;
            if (contentError is null)
            {
                BindProgramContentRows(rows, dictionaryItems.Any(item => !string.IsNullOrWhiteSpace(item.ItemName)));
            }
            else
            {
                _programContentRows.Clear();
                RefreshProgramContentTable();
                ShowWarning(TextKeys.ProgramManage.ContentInvalid, contentError);
            }
            UpdateCurrentInfoText();
            RestoreEditingSelection();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (loadVersion != Volatile.Read(ref _detailLoadVersion) || IsDisposed || Disposing) return;
            StartNewProgram();
            ShowErrorMessage(ex.Message);
        }
        finally
        {
            if (loadVersion == Volatile.Read(ref _detailLoadVersion))
            {
                _detailLoading = false;
                _suppressNameAutoFill = false;
                UpdateProgramActions();
            }
        }
    }

    private void RestoreEditingSelection()
    {
        if (tablePrograms.DataSource is IReadOnlyList<ProgramProductGroupRow> rows)
        {
            var index = rows.ToList().FindIndex(row => row.ProgramId == _editingId);
            tablePrograms.SelectedIndex = index < 0 ? -1 : index + 1;
        }
    }

    private void UpdateCurrentInfoText()
    {
        if (_contentError is not null)
        {
            lblCurrentInfo.Text = _localizer.GetString(TextKeys.ProgramManage.CurrentInvalid);
            return;
        }
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

        _programOperationInProgress = true;
        UpdateProgramActions();
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
            _programOperationInProgress = false;
            UpdateProgramActions();
        }
    }

    /// <summary>
    /// 以当前编辑内容为基础，在同一产品工号下另存为一个新程序。
    /// 新请求清空本地 ID；保存失败前不改变当前编辑身份，避免误把异常程序当新增保存。
    /// </summary>
    private async void SaveAsNew_ClickAsync(object? sender, EventArgs e)
    {
        if (!CanEditProgram()) return;
        if (_editingId <= 0)
        {
            ShowWarning(TextKeys.ProgramManage.SelectDelete);
            return;
        }
        if (!TryBuildRequest(out var request, asNew: true)) return;

        _programOperationInProgress = true;
        UpdateProgramActions();
        try
        {
            request.SequenceNumber = await _programService.GetNextSequenceNumberAsync(
                request.ProductNum,
                _operationCts.Token);
            request.ProgramName = _programService.BuildProgramName(
                request.ProductNum, request.ComponentCode, request.SequenceNumber, request.LocalRemark);
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
            _programOperationInProgress = false;
            UpdateProgramActions();
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
        if (_programOperationInProgress) return;
        if (_detailLoading)
        {
            ShowWarning(TextKeys.ProgramManage.DetailLoading);
            return;
        }
        if (_programs.Count == 0)
        {
            ShowWarningMessage("当前没有可删除的加工程序。");
            return;
        }

        var program = GetEditingProgram();
        if (program is null)
        {
            ShowWarning(TextKeys.ProgramManage.SelectDelete);
            return;
        }

        // 确认框期间异步回调仍可运行，删除始终使用确认前捕获的身份与备注。
        var programId = program.Id;
        var remark = _contentError is null ? ResolveEditedMesRemark(program) : string.Empty;
        var syncNow = SyncAfterSaveEnabled;
        _programOperationInProgress = true;
        UpdateProgramActions();
        try
        {
            if (!Confirm(TextKeys.ProgramManage.DeleteConfirm, program.ProgramName, programId)) return;
            var result = await _programService.DeleteLocalAsync(programId, remark, _operationCts.Token);
            await ReloadProgramsAsync();
            StartNewProgram();

            if (!syncNow || !result.RequiresMesSync)
            {
                ShowInfo(TextKeys.ProgramManage.DeleteSuccess);
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
            _programOperationInProgress = false;
            UpdateProgramActions();
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
        if (!CanEditProgram()) return;
        if (_editingId <= 0)
        {
            ShowWarning(TextKeys.ProgramManage.SelectSync);
            return;
        }

        var programId = _editingId;
        _programOperationInProgress = true;
        UpdateProgramActions();
        try
        {
            await _programService.SyncProgramAsync(programId, _operationCts.Token);
            await ReloadProgramsAsync(programId);
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
        finally
        {
            _programOperationInProgress = false;
            UpdateProgramActions();
        }
    }

    private async void PullMes_ClickAsync(object? sender, EventArgs e)
    {
        if (_programOperationInProgress) return;
        _programOperationInProgress = true;
        UpdateProgramActions();
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
            try { await ReloadProgramsAsync(_editingId); }
            catch (Exception ex) { ShowErrorMessage(ex.Message); }
            _programOperationInProgress = false;
            UpdateProgramActions();
        }
    }

    private bool TryBuildRequest(out SaveProgramReq request, bool asNew = false)
    {
        request = new SaveProgramReq { Id = asNew ? 0 : _editingId };
        if (!CanEditProgram()) return false;
        if (_recipeNameRefreshing)
        {
            ShowWarning(TextKeys.ProgramManage.RecipeRefreshing);
            return false;
        }

        var sequenceNumber = 1;
        if (!asNew && (!int.TryParse(inputSequenceNumber.Text.Trim(), out sequenceNumber) || sequenceNumber <= 0))
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

        if (!int.TryParse(
                inputTouchCount.Text.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var touchCount)
            || touchCount <= 0)
        {
            inputTouchCount.Status = AntdUI.TType.Error;
            inputTouchCount.Focus();
            ShowWarning(TextKeys.ProgramManage.TouchCountInvalid, lblTouchCount.Text ?? string.Empty);
            return false;
        }

        var editingProgram = GetEditingProgram();
        if ((_editingId <= 0 || asNew)
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
            : _editingId > 0 && !asNew
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
                inputDescription.Text)
            : inputProgramName.Text;
        request.ProgramType = cmbProgramType.SelectedIndex == 1 ? "1" : "0";
        tableProgramContent.EditModeClose();
        if (!ProgramContentJsonRules.TryToJson(_programContentRows, out var programContentJson, out var errorMessage,
                _appSettingsService.Get().ProcessParameterDeviceType))
        {
            var invalidRow = _programContentRows.FirstOrDefault(row => !string.IsNullOrEmpty(row.ItemName)
                && errorMessage.Contains($"“{row.ItemName}”", StringComparison.Ordinal));
            if (invalidRow is not null)
            {
                tableProgramContent.SelectedIndex = _programContentRows.IndexOf(invalidRow) + 1;
                tableProgramContent.ScrollLine(invalidRow, true);
            }
            tableProgramContent.Focus();
            ShowWarningMessage(errorMessage);
            return false;
        }

        // 从配方下拉取出名称，注入到程序内容 JSON 最前面
        var station1RecipeName = ResolveSelectedRecipeName(selectStation1Recipe, 1);
        var station2RecipeName = selectStation2Recipe.Visible
            ? ResolveSelectedRecipeName(selectStation2Recipe, 2)
            : ProgramContentJsonRules.ExtractRecipeNames(_editingContent).Station2RecipeName;
        request.ProgramContentJson = ProgramContentJsonRules.MergeRecipeNamesAndContent(
            station1RecipeName,
            station2RecipeName,
            programContentJson,
            touchCount);

        request.WeldJobName = string.Empty;
        request.RobotJobName = string.Empty;
        request.CycleTimeSeconds = 0m;
        request.MesRemark = ResolveEditedMesRemark(GetEditingProgram());
        request.LocalRemark = inputDescription.Text;
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
        if (IsDisposed || Disposing || _operationCts.IsCancellationRequested) return;
        var refreshVersion = Interlocked.Increment(ref _recipeNameRefreshVersion);
        _recipeNameRefreshing = true;
        UpdateProgramActions();
        try
        {
            var settings = _appSettingsService.Get();
            ApplyStationRecipeLayout(settings.EnableDualStation);

            var stationNumbers = settings.EnableDualStation ? new[] { 1, 2 } : new[] { 1 };
            foreach (var stationNo in stationNumbers)
            {
                _recipeNameReadSucceeded[stationNo] = false;
                var select = stationNo == 2 ? selectStation2Recipe : selectStation1Recipe;
                select.ExpandDrop = false;
                select.ReadOnly = true;
            }
            var results = new List<(int StationNo, PlcRecipeNameReadResult Result)>();
            foreach (var stationNo in stationNumbers)
            {
                results.Add((stationNo, await ReadRecipeNameOptionsAsync(stationNo)));
            }

            if (refreshVersion != Volatile.Read(ref _recipeNameRefreshVersion) || IsDisposed || Disposing)
            {
                return;
            }

            foreach (var (stationNo, result) in results)
            {
                var select = stationNo == 2 ? selectStation2Recipe : selectStation1Recipe;
                // PLC 读取期间用户可能点击新增或切换程序，必须以统一绑定时的实时编辑值为准。
                var selection = ResolveSelectedRecipeItem(select, stationNo);
                var liveRecipeCode = selection?.RecipeCode;
                if (selection is null && GetEditingProgram() is { } editingProgram)
                {
                    liveRecipeCode = stationNo == 2 ? editingProgram.Station2RecipeCode : editingProgram.RecipeCode;
                }
                BindRecipeNameOptions(select, stationNo, result, liveRecipeCode,
                    selectNotApplicable: selection?.Kind == RecipeSelectionKind.NotApplicable || _editingId > 0);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (refreshVersion != Volatile.Read(ref _recipeNameRefreshVersion) || IsDisposed || Disposing)
            {
                return;
            }

            BindRecipeNameReadFailure(selectStation1Recipe, 1, ex);
            if (selectStation2Recipe.Visible)
            {
                BindRecipeNameReadFailure(selectStation2Recipe, 2, ex);
            }
        }
        finally
        {
            if (refreshVersion == Volatile.Read(ref _recipeNameRefreshVersion))
            {
                _recipeNameRefreshing = false;
                UpdateProgramActions();
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
        tlpRecipe1.Visible = true;
        tlpRecipe2.Visible = enableDualStation;
        // 配方行移动容器后，必须在实际父容器中折叠，避免压缩编辑区同索引的产品工号行。
        var recipeLayout = (TableLayoutPanel)tlpRecipe2.Parent!;
        var station2RecipeRow = recipeLayout.GetRow(tlpRecipe2);
        recipeLayout.RowStyles[station2RecipeRow].SizeType = enableDualStation ? SizeType.AutoSize : SizeType.Absolute;
        recipeLayout.RowStyles[station2RecipeRow].Height = 0F;
        recipeLayout.PerformLayout();
    }

    private void BindRecipeNameOptions(
        AntdUI.Select select,
        int stationNo,
        PlcRecipeNameReadResult result,
        string? currentRecipeCode,
        bool selectNotApplicable = false)
    {
        _recipeNameReadSucceeded[stationNo] = result.IsSuccess;
        var items = result.IsSuccess
            ? result.Options.Select(option => new RecipeSelectionItem(
                option.Name,
                option.RecipeCode.ToString(),
                RecipeSelectionKind.PlcOption)).ToList()
            : BuildUnavailableItems(currentRecipeCode);

        if (_enableDualStation && (result.IsSuccess || (selectNotApplicable && string.IsNullOrWhiteSpace(currentRecipeCode))))
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
            selectNotApplicable: selectNotApplicable);
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
        // Items 重建不会复位 AntdUI 内部索引；先归位，避免同索引对应的新名称仍显示旧值。
        select.SelectedIndex = -1;
        select.SelectedIndex = selectedIndex;
        if (selectedIndex < 0) select.Text = string.Empty;
    }

    private string? ResolveSelectedRecipeCode(AntdUI.Select select, int stationNo)
        => ResolveSelectedRecipeItem(select, stationNo)?.RecipeCode;

    private RecipeSelectionItem? ResolveSelectedRecipeItem(AntdUI.Select select, int stationNo)
    {
        if (!_recipeSelectionItems.TryGetValue(stationNo, out var items)) return null;
        var index = SelectListRules.ResolveSelectedIndex(
            items.Select(item => (string?)item.DisplayText).ToArray(),
            select.SelectedValue as string ?? select.Text,
            select.SelectedIndex);
        return index >= 0 ? items[index] : null;
    }

    /// <summary>
    /// 从配方下拉取出选中的配方名称（不是数字配方号）。
    /// 用于注入 ProgramContent，随 MES 同步给其他设备。
    /// </summary>
    private string? ResolveSelectedRecipeName(AntdUI.Select select, int stationNo)
    {
        var item = ResolveSelectedRecipeItem(select, stationNo);
        if (_recipeNameReadSucceeded.TryGetValue(stationNo, out var succeeded) && succeeded)
        {
            if (item?.Kind == RecipeSelectionKind.PlcOption) return item.DisplayText;
            if (item?.Kind == RecipeSelectionKind.NotApplicable) return null;
        }

        // 读取失败只允许非配方编辑，名称与配方号必须一起保留，不能混用未保存的选择。
        var existing = ProgramContentJsonRules.ExtractRecipeNames(_editingContent);
        return stationNo == 2 ? existing.Station2RecipeName : existing.Station1RecipeName;
    }

    private void RefreshRecipeSelectorTexts()
    {
        RefreshRecipeSelectorText(selectStation1Recipe, 1);
        RefreshRecipeSelectorText(selectStation2Recipe, 2);
    }

    private void RefreshRecipeSelectorText(AntdUI.Select select, int stationNo)
    {
        var selection = ResolveSelectedRecipeItem(select, stationNo);
        var recipeCode = selection?.RecipeCode;
        var kind = selection?.Kind;

        if (_recipeSelectionItems.TryGetValue(stationNo, out var items))
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
        SetRecipeSelection(select, stationNo, recipeCode, selectNotApplicable: kind == RecipeSelectionKind.NotApplicable);
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

        select.ExpandDrop = false;
        select.SelectedIndex = -1;
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
        columnContentName.ReadOnly = dictionaryAvailable;
        columnContentName.Editable = !dictionaryAvailable;
        TableStyleHelper.ApplyAntdColumnDefaults(tableProgramContent);
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
            inputDescription.Text);
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
        if (_programOperationInProgress) return;
        _programOperationInProgress = true;
        UpdateProgramActions();
        try
        {
            // 待处理查询包含已在列表隐藏的删除失败记录，不受分页和搜索快照影响。
            var pending = await _programService.GetPendingSyncProgramsAsync(_operationCts.Token);
            var pendingIds = pending.Select(program => program.Id).ToList();
            if (pendingIds.Count == 0)
            {
                ShowWarning(TextKeys.ProgramManage.MessageBatchCleanEmpty);
                return;
            }

            var confirmMessage = _localizer.GetString(TextKeys.ProgramManage.MessageConfirmBatchClean, pendingIds.Count);
            var result = MessageBox.Show(
                GetDialogOwner(),
                confirmMessage,
                _localizer.GetString(TextKeys.Common.TitleWarning),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

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
            ShowWarning(TextKeys.ProgramManage.MessageBatchCleanCanceled);
        }
        catch (Exception ex)
        {
            ShowErrorMessage(_localizer.GetString(TextKeys.ProgramManage.MessageBatchCleanFailed, ex.Message));
        }
        finally
        {
            _programOperationInProgress = false;
            UpdateProgramActions();
        }
    }

    private bool Confirm(string messageKey, params object[] args)
    {
        var message = _localizer.GetString(messageKey, args);
        return MessageBox.Show(GetDialogOwner(), message, _localizer.GetString(TextKeys.Common.TitleConfirmDelete), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            == DialogResult.Yes;
    }
}
