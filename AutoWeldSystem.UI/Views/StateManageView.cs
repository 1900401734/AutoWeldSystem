using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.Upload;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.ViewModels;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Views;

/// <summary>
/// 上传状态页面。
/// 总览页只管理当前任务补传链路；程序文件同步保留为独立页签，避免和工单任务混在一起。
/// </summary>
public partial class StateManageView : BaseView
{
    private readonly IProgramManageService _programService;
    private readonly IUploadTaskService _uploadTaskService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IDeviceStatusService _deviceStatusService;
    private readonly IUploadStatusSummaryService _summaryService;
    private readonly ILocalizationService _localizer;
    private readonly IMesConnectionMonitor _mesConnectionMonitor;
    private readonly IReadOnlyList<StateUploadTabDefinition> _tabDefinitions;
    private readonly BindingSource _bindingSource = new();
    private readonly Dictionary<string, BizDeviceStatusLog> _deviceStatusLogsByRecordKey =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<DataGridViewTextBoxColumn> _gridColumns = new();
    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private CancellationTokenSource? _reloadCancellation;
    private int _reloadVersion;
    private bool _initialized;
    private bool _applyingTabPermissions;
    private bool _isReloading;

    public StateManageView(
        IProgramManageService programService,
        IUploadTaskService uploadTaskService,
        IWeldTaskService weldTaskService,
        IDeviceStatusService deviceStatusService,
        IUploadStatusSummaryService summaryService,
        ILocalizationService localizer,
        IMesConnectionMonitor mesConnectionMonitor)
    {
        _programService = programService;
        _uploadTaskService = uploadTaskService;
        _weldTaskService = weldTaskService;
        _deviceStatusService = deviceStatusService;
        _summaryService = summaryService;
        _localizer = localizer;
        _mesConnectionMonitor = mesConnectionMonitor;

        InitializeComponent();
        _tabDefinitions = BuildTabDefinitions();
        ConfigureGrid();
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
        ApplyLocalizedTexts();
        ApplyTabPermissions();
        RequestReloadActiveTasks();
    }

    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ApplyTabPermissions();
        ConfigureActiveGridColumns();
        dgvPending.Refresh();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        GlobalContext.SessionChanged -= GlobalContext_SessionChanged;
        _mesConnectionMonitor.StatusChanged -= MesConnectionMonitor_StatusChanged;
        _deviceStatusService.LogsChanged -= DeviceStatusService_LogsChanged;
        _reloadCancellation?.Cancel();
        base.OnHandleDestroyed(e);
    }

    private IReadOnlyList<StateUploadTabDefinition> BuildTabDefinitions()
    {
        return
        [
            new(tabSummary, PermissionCodes.Tabs.State.WorkOrderInfo),
            new(tabStartReports, PermissionCodes.Tabs.State.StartReport),
            new(tabFinishReports, PermissionCodes.Tabs.State.FinishReport),
            new(tabProcessParameters, PermissionCodes.Tabs.State.ProcessParameter),
            new(tabReportFiles, PermissionCodes.Tabs.State.ReportFile),
            new(tabWorkOrderStatuses, PermissionCodes.Tabs.State.WorkOrderStatus),
            new(tabDeviceStatuses, PermissionCodes.Tabs.State.DeviceStatus),
            new(tabProgramFiles, PermissionCodes.Tabs.State.ProgramFile)
        ];
    }

    private void ApplyTabPermissions()
    {
        var previousSelectedTab = tabUploadCategories.SelectedTab;

        _applyingTabPermissions = true;
        tabUploadCategories.SuspendLayout();
        try
        {
            tabUploadCategories.TabPages.Clear();
            foreach (var definition in _tabDefinitions)
            {
                if (GlobalContext.HasPermission(definition.PermissionCode))
                {
                    tabUploadCategories.TabPages.Add(definition.Page);
                }
            }

            if (tabUploadCategories.TabPages.Count == 0)
            {
                SetNoVisibleTabState();
                return;
            }

            tabUploadCategories.SelectedTab = previousSelectedTab is not null
                && tabUploadCategories.TabPages.Contains(previousSelectedTab)
                    ? previousSelectedTab
                    : tabUploadCategories.TabPages[0];
        }
        finally
        {
            tabUploadCategories.ResumeLayout();
            _applyingTabPermissions = false;
        }

        ConfigureActiveGridColumns();
    }

    private void SetNoVisibleTabState()
    {
        BindRows(Array.Empty<object>());
        foreach (var column in _gridColumns)
        {
            column.Visible = false;
        }

        lblSummary.Text = _localizer.GetString(TextKeys.StateManage.MessageNoVisibleTabs);
        btnRetrySelected.Enabled = false;
        btnRetryAll.Enabled = false;
        btnDeleteSelected.Enabled = false;
    }

    private void ConfigureGrid()
    {
        TableStyleHelper.ApplyDataGridView(dgvPending);
        dgvPending.AutoGenerateColumns = false;
        InitializeGridColumns();
        dgvPending.DataSource = _bindingSource;
        ConfigureActiveGridColumns();
    }

    private void InitializeGridColumns()
    {
        if (_gridColumns.Count > 0)
        {
            return;
        }

        for (var index = 0; index < 10; index++)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = $"stateColumn{index}",
                Visible = false
            };
            _gridColumns.Add(column);
            dgvPending.Columns.Add(column);
        }
    }

    private void ConfigureActiveGridColumns()
    {
        var definitions = GetActiveColumnDefinitions();
        dgvPending.SuspendLayout();
        try
        {
            foreach (var column in _gridColumns)
            {
                column.Visible = false;
            }

            for (var index = 0; index < definitions.Count; index++)
            {
                var column = _gridColumns[index];
                var definition = definitions[index];
                column.DataPropertyName = definition.PropertyName;
                column.HeaderText = definition.HeaderText;
                column.FillWeight = definition.FillWeight;
                column.DefaultCellStyle.Format = definition.Format ?? string.Empty;
            }

            for (var index = 0; index < definitions.Count; index++)
            {
                _gridColumns[index].Visible = true;
            }
        }
        finally
        {
            dgvPending.ResumeLayout(false);
        }
    }

    private List<(string PropertyName, string HeaderText, float FillWeight, string? Format)> GetActiveColumnDefinitions()
    {
        if (IsSummaryTab())
        {
            return
            [
                (nameof(UploadPendingSummaryRow.SequenceNo), "序号", 6, null),
                (nameof(UploadPendingSummaryRow.TaskIdentity), "任务ID", 22, null),
                (nameof(UploadPendingSummaryRow.WorkOrderId), "工单号", 14, null),
                (nameof(UploadPendingSummaryRow.StationNo), "工位", 6, null),
                (nameof(UploadPendingSummaryRow.StartReportStatus), "开工上报", 10, null),
                (nameof(UploadPendingSummaryRow.ProcessParameterStatus), "过程参数", 10, null),
                (nameof(UploadPendingSummaryRow.ReportFileStatus), "xlsx报表", 10, null),
                (nameof(UploadPendingSummaryRow.FinishReportStatus), "完工上报", 10, null),
                (nameof(UploadPendingSummaryRow.PendingCount), "待处理数", 8, null),
                (nameof(UploadPendingSummaryRow.UpdatedTime), "更新时间", 14, "yyyy-MM-dd HH:mm:ss")
            ];
        }

        if (IsProgramFileTab())
        {
            return
            [
                (nameof(ProgramSyncSummary.ProgramName), "程序名称", 24, null),
                (nameof(ProgramSyncSummary.ProductNum), "产品工号", 12, null),
                (nameof(ProgramSyncSummary.SyncStatus), "同步状态", 12, null),
                (nameof(ProgramSyncSummary.SyncAction), "动作", 10, null),
                (nameof(ProgramSyncSummary.SyncMessage), "同步消息", 30, null),
                (nameof(ProgramSyncSummary.LastSyncTime), "最后同步时间", 16, "yyyy-MM-dd HH:mm:ss")
            ];
        }

        var definitions = new List<(string, string, float, string?)>
        {
            (nameof(UploadTaskSummary.TaskIdentity), IsDeviceStatusTab() ? "状态标识" : "任务ID", 16, null)
        };
        if (IsProcessParameterTab())
        {
            definitions.Add((nameof(UploadTaskSummary.StationNo), "工位", 6, null));
            definitions.Add((nameof(UploadTaskSummary.ProductNo), "产品编号", 12, null));
        }

        definitions.Add((nameof(UploadTaskSummary.Target), "目标平台", 10, null));
        definitions.Add((nameof(UploadTaskSummary.Status), "上传状态", 12, null));
        definitions.Add((nameof(UploadTaskSummary.RetryCount), "重试次数", 9, null));
        definitions.Add((nameof(UploadTaskSummary.MaxRetryCount), "最大重试", 9, null));
        if (IsReportFileTab())
        {
            definitions.Add((nameof(UploadTaskSummary.FilePath), "文件路径", 24, null));
        }

        definitions.Add((nameof(UploadTaskSummary.DisplayMessage), "处理消息", 30, null));
        definitions.Add((nameof(UploadTaskSummary.UpdatedTime), "更新时间", 14, "yyyy-MM-dd HH:mm:ss"));
        return definitions;
    }

    private void WireEvents()
    {
        btnRefresh.Click += (_, _) => RequestReloadActiveTasks();
        btnRetrySelected.Click += RetrySelected_ClickAsync;
        btnRetryAll.Click += RetryAll_ClickAsync;
        btnDeleteSelected.Click += DeleteSelected_Click;
        tabUploadCategories.SelectedIndexChanged += (_, _) => SwitchUploadCategory();
        dgvPending.CellFormatting += DgvPending_CellFormatting;
        dgvPending.DataError += DgvPending_DataError;
        dgvPending.KeyDown += DgvPending_KeyDown;
        GlobalContext.SessionChanged += GlobalContext_SessionChanged;
        _mesConnectionMonitor.StatusChanged += MesConnectionMonitor_StatusChanged;
        _deviceStatusService.LogsChanged += DeviceStatusService_LogsChanged;
        dgvPending.SelectionChanged += (_, _) =>
        {
            ApplyRetrySelectedPermissionForActiveTab();
            ApplyDeletePermissionForActiveTab();
        };
    }

    private void ApplyLocalizedTexts()
    {
        lblTitle.Text = _localizer.GetString(TextKeys.StateManage.Title);
        lblDescription.Text = "查看开工、过程参数、xlsx报表和完工上报的本地待上传状态，支持断网恢复后人工补传。";
        btnRetrySelected.Text = _localizer.GetString(TextKeys.StateManage.ButtonRetrySelected);
        btnRetryAll.Text = IsSummaryTab() ? "一键上传" : _localizer.GetString(TextKeys.StateManage.ButtonRetryAll);
        btnDeleteSelected.Text = "删除选中";
        ApplyRetryAllPermissionForActiveTab();
        ApplyDeletePermissionForActiveTab();
        btnRefresh.Text = _localizer.GetString(TextKeys.Common.ActionRefresh);
        tabSummary.Text = "工单信息";
        tabStartReports.Text = "开工信息";
        tabFinishReports.Text = "完工信息";
        tabProcessParameters.Text = "过程参数";
        tabReportFiles.Text = "报告文件";
        tabWorkOrderStatuses.Text = "工单状态";
        tabDeviceStatuses.Text = "设备状态";
        tabProgramFiles.Text = "程序文件";
        SetSummary(GetPendingCount());
    }

    private void SwitchUploadCategory()
    {
        if (_applyingTabPermissions || !HasVisibleTabs())
        {
            return;
        }

        btnRetryAll.Text = IsSummaryTab() ? "一键上传" : _localizer.GetString(TextKeys.StateManage.ButtonRetryAll);
        ApplyRetryAllPermissionForActiveTab();
        ApplyDeletePermissionForActiveTab();
        ConfigureActiveGridColumns();
        RequestReloadActiveTasks(clearCurrentRows: true);
    }

    private void MesConnectionMonitor_StatusChanged(object? sender, MesConnectionSnapshot e)
    {
        if (IsDisposed)
        {
            return;
        }

        RunOnUiThread(
            () =>
            {
                dgvPending.Invalidate();
            },
            "StateManageView.MesConnectionChanged");
    }

    private void GlobalContext_SessionChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        RunOnUiThread(
            () =>
            {
                ApplyLocalizedTexts();
                ApplyTabPermissions();
                RequestReloadActiveTasks();
            },
            "StateManageView.SessionChanged");
    }

    /// <summary>
    /// Refreshes the pending device-status projection after source logs change.
    /// </summary>
    private void DeviceStatusService_LogsChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        RunOnUiThread(
            () =>
            {
                if (IsDeviceStatusTab())
                {
                    RequestReloadActiveTasks();
                }
            },
            "StateManageView.DeviceStatusLogsChanged");
    }

    private void ApplyRetryAllPermissionForActiveTab()
    {
        if (!HasVisibleTabs())
        {
            btnRetryAll.Enabled = false;
            return;
        }

        var permissionCode = IsSummaryTab()
            ? PermissionCodes.Buttons.State.UploadAll
            : PermissionCodes.Buttons.State.RetryAll;
        btnRetryAll.Tag = $"perm:{permissionCode}:enabled";
        btnRetryAll.Enabled = GlobalContext.HasPermission(permissionCode);
    }

    private void ApplyRetrySelectedPermissionForActiveTab()
    {
        if (!HasVisibleTabs())
        {
            btnRetrySelected.Enabled = false;
            return;
        }

        if (IsSummaryTab())
        {
            btnRetrySelected.Enabled = false;
            return;
        }

        if (IsProgramFileTab())
        {
            btnRetrySelected.Enabled = dgvPending.CurrentRow?.DataBoundItem is ProgramSyncSummary
                && GlobalContext.HasPermission(PermissionCodes.Buttons.State.RetrySelected);
            return;
        }

        btnRetrySelected.Enabled = dgvPending.CurrentRow?.DataBoundItem is UploadTaskSummary { CanRetry: true }
            && GlobalContext.HasPermission(PermissionCodes.Buttons.State.RetrySelected);
    }

    private void ApplyDeletePermissionForActiveTab()
    {
        if (!HasVisibleTabs())
        {
            btnDeleteSelected.Enabled = false;
            return;
        }

        if (IsProgramFileTab())
        {
            btnDeleteSelected.Enabled = GetSelectedProgramSummaries().Count > 0
                && GlobalContext.HasPermission(PermissionCodes.Buttons.State.Delete);
            return;
        }

        if (IsDeviceStatusTab())
        {
            var selectedTasks = GetSelectedUploadTasks();
            btnDeleteSelected.Enabled = selectedTasks.Count > 0
                && selectedTasks.All(task => task.CanDelete)
                && GlobalContext.HasPermission(PermissionCodes.Buttons.State.Delete);
            return;
        }

        var selectedItems = dgvPending.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem)
            .ToList();
        var canDelete = selectedItems.Count > 0
            && selectedItems.All(item => item is UploadPendingSummaryRow or UploadTaskSummary { CanDelete: true });

        btnDeleteSelected.Enabled = canDelete
            && GlobalContext.HasPermission(PermissionCodes.Buttons.State.Delete);
    }

    private int GetPendingCount()
    {
        return _bindingSource.DataSource switch
        {
            ICollection<UploadPendingSummaryRow> summaries => summaries.Count(row => row.PendingCount > 0),
            ICollection<ProgramSyncSummary> programs => programs.Count,
            ICollection<UploadTaskSummary> tasks => tasks.Count,
            _ => 0
        };
    }

    private void SetSummary(int count)
    {
        lblSummary.Text = $"{GetActiveCategoryText()}待处理：{count} 条";
    }

    private void RequestReloadActiveTasks(bool clearCurrentRows = false)
    {
        if (IsDisposed)
        {
            return;
        }

        if (!HasVisibleTabs())
        {
            SetNoVisibleTabState();
            return;
        }

        var activeTab = GetActiveTabKind();
        var version = Interlocked.Increment(ref _reloadVersion);
        _reloadCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        _reloadCancellation = cancellation;

        if (clearCurrentRows)
        {
            BindRows(Array.Empty<object>());
        }

        lblSummary.Text = $"{GetActiveCategoryText()}正在加载...";
        SetReloadingState(true);
        _ = ReloadActiveTasksAsync(activeTab, version, cancellation);
    }

    private async Task ReloadActiveTasksAsync(
        StateUploadTabKind activeTab,
        int version,
        CancellationTokenSource cancellation)
    {
        var gateEntered = false;
        try
        {
            await _reloadGate.WaitAsync(cancellation.Token);
            gateEntered = true;
            var result = await Task.Run(
                () => LoadActiveTasks(activeTab),
                cancellation.Token);

            cancellation.Token.ThrowIfCancellationRequested();
            if (IsDisposed
                || version != Volatile.Read(ref _reloadVersion)
                || GetActiveTabKind() != activeTab)
            {
                return;
            }

            if (result.DeviceStatusLogs is not null)
            {
                RefreshDeviceStatusLogIndex(result.DeviceStatusLogs);
            }

            BindRows(result.Rows);
            SetSummary(result.PendingCount);
            ApplyRetrySelectedPermissionForActiveTab();
            ApplyDeletePermissionForActiveTab();
        }
        catch (OperationCanceledException)
        {
            // 页签快速切换或窗体销毁时，忽略已经过期的加载结果。
        }
        catch (Exception ex)
        {
            if (!IsDisposed
                && version == Volatile.Read(ref _reloadVersion))
            {
                SetSummary(GetPendingCount());
                ShowErrorMessage($"加载{GetActiveCategoryText()}失败：{ex.Message}");
            }
        }
        finally
        {
            if (gateEntered)
            {
                _reloadGate.Release();
            }

            if (!IsDisposed && version == Volatile.Read(ref _reloadVersion))
            {
                SetReloadingState(false);
            }

            if (ReferenceEquals(_reloadCancellation, cancellation))
            {
                _reloadCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private StateUploadLoadResult LoadActiveTasks(StateUploadTabKind activeTab)
    {
        return activeTab switch
        {
            StateUploadTabKind.Summary => CreateSummaryLoadResult(_summaryService.GetSummary().ToList()),
            StateUploadTabKind.ProgramFile => CreateProgramLoadResult(_programService.GetPendingSyncPrograms().ToList()),
            StateUploadTabKind.ProcessParameter => CreateTaskLoadResult(_uploadTaskService.GetProcessParameterRows().ToList()),
            StateUploadTabKind.StartReport => CreateTaskLoadResult(_uploadTaskService.GetTasks(ProductionConstants.UploadTaskTypes.StartReport).ToList()),
            StateUploadTabKind.FinishReport => CreateTaskLoadResult(_uploadTaskService.GetTasks(ProductionConstants.UploadTaskTypes.FinishReport).ToList()),
            StateUploadTabKind.ReportFile => CreateTaskLoadResult(_uploadTaskService.GetTasks(ProductionConstants.UploadTaskTypes.ReportFile).ToList()),
            StateUploadTabKind.WorkOrderStatus => CreateTaskLoadResult(_uploadTaskService.GetTasks(ProductionConstants.UploadTaskTypes.WorkOrderStatus).ToList()),
            StateUploadTabKind.DeviceStatus => CreateDeviceStatusLoadResult(),
            _ => throw new ArgumentOutOfRangeException(nameof(activeTab), activeTab, null)
        };
    }

    private StateUploadLoadResult CreateDeviceStatusLoadResult()
    {
        var logs = _deviceStatusService.GetPendingLogs().ToList();
        var tasks = _uploadTaskService.GetTasks(ProductionConstants.UploadTaskTypes.DeviceStatus).ToList();
        return new StateUploadLoadResult(tasks, tasks.Count, logs);
    }

    private static StateUploadLoadResult CreateSummaryLoadResult(IReadOnlyList<UploadPendingSummaryRow> rows)
        => new(rows, rows.Count(row => row.PendingCount > 0), null);

    private static StateUploadLoadResult CreateProgramLoadResult(IReadOnlyList<ProgramSyncSummary> rows)
        => new(rows, rows.Count, null);

    private static StateUploadLoadResult CreateTaskLoadResult(IReadOnlyList<UploadTaskSummary> rows)
        => new(rows, rows.Count, null);

    private void BindRows(object rows)
    {
        dgvPending.SuspendLayout();
        try
        {
            _bindingSource.RaiseListChangedEvents = false;
            try
            {
                _bindingSource.DataSource = rows;
            }
            finally
            {
                _bindingSource.RaiseListChangedEvents = true;
            }

            _bindingSource.ResetBindings(false);
        }
        finally
        {
            dgvPending.ResumeLayout(false);
        }
    }

    private void SetReloadingState(bool isReloading)
    {
        _isReloading = isReloading;
        btnRefresh.Enabled = !isReloading;
        if (isReloading)
        {
            btnRetrySelected.Enabled = false;
            btnRetryAll.Enabled = false;
            btnDeleteSelected.Enabled = false;
            return;
        }

        ApplyRetrySelectedPermissionForActiveTab();
        ApplyRetryAllPermissionForActiveTab();
        ApplyDeletePermissionForActiveTab();
    }

    private async void RetrySelected_ClickAsync(object? sender, EventArgs e)
    {
        if (IsSummaryTab())
        {
            ShowWarning("工单信息请使用一键上传。");
            return;
        }

        if (IsProgramFileTab())
        {
            await RetrySelectedProgramAsync();
            return;
        }

        if (dgvPending.CurrentRow?.DataBoundItem is not UploadTaskSummary task)
        {
            ShowWarning(_localizer.GetString(TextKeys.StateManage.MessageSelectPending));
            return;
        }

        if (!task.CanRetry)
        {
            ShowWarning("该过程参数行来自产品历史，需等待达到批次数量后自动上传，不能手动重试。");
            return;
        }

        try
        {
            await _uploadTaskService.ExecuteAsync(task.Id);
            RequestReloadActiveTasks();
            ShowInfo("已执行选中的上传任务。");
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
    }

    private async Task RetrySelectedProgramAsync()
    {
        if (dgvPending.CurrentRow?.DataBoundItem is not ProgramSyncSummary item)
        {
            ShowWarning(_localizer.GetString(TextKeys.StateManage.MessageSelectPending));
            return;
        }

        btnRetrySelected.Enabled = false;
        try
        {
            await _programService.SyncProgramAsync(item.Id);
            RequestReloadActiveTasks();
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
        finally
        {
            ApplyRetrySelectedPermissionForActiveTab();
        }
    }

    private async void RetryAll_ClickAsync(object? sender, EventArgs e)
    {
        btnRetryAll.Enabled = false;
        try
        {
            if (IsSummaryTab())
            {
                var count = await ExecuteAllPendingUploadsAsync();
                ShowInfo($"已按开工、过程参数、xlsx报表、完工顺序执行 {count} 条待上传任务。");
            }
            else if (IsProgramFileTab())
            {
                await _programService.SyncAllPendingAsync();
            }
            else
            {
                var count = await _uploadTaskService.ExecuteAllPendingAsync(GetActiveUploadTaskType());
                ShowInfo($"已执行 {count} 条上传任务。");
            }

            RequestReloadActiveTasks();
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
        finally
        {
            ApplyRetryAllPermissionForActiveTab();
        }
    }

    private void DeleteSelected_Click(object? sender, EventArgs e)
    {
        if (IsDeviceStatusTab())
        {
            DeleteSelectedDeviceStatusTasks();
            return;
        }

        if (IsProgramFileTab())
        {
            DeleteSelectedProgramSyncRecords();
            return;
        }

        DeleteSelectedUploadRecords();
    }

    private void DeleteSelectedUploadRecords()
    {
        var selectedSummaries = dgvPending.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem)
            .OfType<UploadPendingSummaryRow>()
            .ToList();
        var selectedTasks = GetSelectedUploadTasks();
        if (selectedSummaries.Count + selectedTasks.Count == 0)
        {
            ShowWarning(_localizer.GetString(TextKeys.StateManage.MessageSelectPending));
            return;
        }

        if (selectedTasks.Any(task => !task.CanDelete))
        {
            ShowWarning("所选记录中包含不可删除的任务。");
            return;
        }

        var message = selectedSummaries.Count > 0
            ? $"确定删除选中的 {selectedSummaries.Count} 条工单信息和 {selectedTasks.Count} 条上传记录吗？\n\n警告：删除工单将同时删除其关联采集记录、上传任务和报表文件，删除后无法恢复！"
            : $"确定删除选中的 {selectedTasks.Count} 条上传记录吗？\n\n虚拟过程参数行会同时删除对应产品的采集记录，删除后无法恢复！";
        if (MessageBox.Show(
                this,
                message,
                _localizer.GetString(TextKeys.Common.TitleConfirmDelete),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        foreach (var summary in selectedSummaries)
        {
            _weldTaskService.DeleteWeldTask(summary.WeldTaskId);
        }

        foreach (var task in selectedTasks.Where(task => task.IsVirtual))
        {
            var parts = task.BusinessId.Split(':');
            if (parts.Length >= 4 && int.TryParse(parts[1], out var weldTaskId))
            {
                _uploadTaskService.DeleteProcessParameterVirtualRow(weldTaskId, task.StationNo, task.ProductNo);
            }
        }

        foreach (var task in selectedTasks.Where(task => !task.IsVirtual))
        {
            _uploadTaskService.DeleteTask(task.Id);
        }

        RequestReloadActiveTasks();
        dgvPending.ClearSelection();
        ShowInfo($"已删除选中的 {selectedSummaries.Count + selectedTasks.Count} 条记录。");
    }

    /// <summary>
    /// Returns the selected upload task rows from the current grid.
    /// </summary>
    private IReadOnlyList<UploadTaskSummary> GetSelectedUploadTasks()
    {
        return dgvPending.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem)
            .OfType<UploadTaskSummary>()
            .ToList();
    }

    /// <summary>
    /// 返回程序文件页签当前选中的同步记录。
    /// </summary>
    private IReadOnlyList<ProgramSyncSummary> GetSelectedProgramSummaries()
    {
        return dgvPending.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem)
            .OfType<ProgramSyncSummary>()
            .ToList();
    }

    /// <summary>
    /// 清理选中的程序同步记录。
    /// 只做本地软删除，不回调 MES：这些记录多半是设备编号变更或 MES 侧已删除导致的死单，
    /// 再次调用远程删除仍会失败，只能在本地终结。
    /// </summary>
    private async void DeleteSelectedProgramSyncRecords()
    {
        var selected = GetSelectedProgramSummaries();
        if (selected.Count == 0)
        {
            ShowWarning(_localizer.GetString(TextKeys.StateManage.MessageSelectPending));
            return;
        }

        var message = $"确定清理选中的 {selected.Count} 条程序同步记录吗？\n\n仅删除本地待同步状态，不会通知 MES，适用于 MES 侧已不存在的程序。";
        var result = MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleConfirmDelete),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        btnDeleteSelected.Enabled = false;
        try
        {
            var deletedCount = await _programService.BatchDeleteLocalProgramsAsync(selected.Select(item => item.Id));
            RequestReloadActiveTasks();
            ShowInfo($"已清理 {deletedCount} 条程序同步记录。");
        }
        catch (Exception ex)
        {
            ShowWarning($"清理程序同步记录失败：{ex.Message}");
        }
        finally
        {
            ApplyDeletePermissionForActiveTab();
        }
    }

    /// <summary>
    /// Deletes all selected device-status upload tasks after an explicit confirmation.
    /// </summary>
    private void DeleteSelectedDeviceStatusTasks()
    {
        var selectedTasks = GetSelectedUploadTasks();
        if (selectedTasks.Count == 0)
        {
            ShowWarning(_localizer.GetString(TextKeys.StateManage.MessageSelectPending));
            return;
        }

        if (selectedTasks.Any(task => !task.CanDelete))
        {
            ShowWarning("所选设备状态记录中包含不可删除的任务。");
            return;
        }

        var message = $"确定删除选中的 {selectedTasks.Count} 条设备状态上传记录吗？\n\n删除后不可恢复，并会清理关联的上传任务。";
        var result = MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleConfirmDelete),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        var selectedLogs = selectedTasks
            .Where(task => !string.IsNullOrWhiteSpace(task.DeviceStatusRecordKey))
            .Select(task => _deviceStatusLogsByRecordKey.TryGetValue(task.DeviceStatusRecordKey, out var log) ? log : null)
            .Where(log => log is not null)
            .Cast<BizDeviceStatusLog>()
            .ToList();
        var selectedRecordKeys = selectedLogs
            .Select(DeviceStatusRecordIdentityRules.GetRecordKey)
            .Where(recordKey => recordKey is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        int deletedCount;
        try
        {
            deletedCount = _deviceStatusService.DeleteLogs(selectedLogs);
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
            return;
        }

        var orphanTasks = selectedTasks
            .Where(task => string.IsNullOrWhiteSpace(task.DeviceStatusRecordKey)
                || !selectedRecordKeys.Contains(task.DeviceStatusRecordKey))
            .ToList();
        foreach (var task in orphanTasks)
        {
            _uploadTaskService.DeleteTask(task.Id);
        }

        RequestReloadActiveTasks();
        dgvPending.ClearSelection();
        ShowInfo($"已删除选中的 {deletedCount + orphanTasks.Count} 条设备状态上传记录。");
    }

    /// <summary>
    /// Caches the same pending/failed log objects used to reconcile device-status tasks.
    /// </summary>
    private void RefreshDeviceStatusLogIndex(IEnumerable<BizDeviceStatusLog> logs)
    {
        _deviceStatusLogsByRecordKey.Clear();
        foreach (var log in logs)
        {
            var recordKey = DeviceStatusRecordIdentityRules.GetRecordKey(log);
            if (recordKey is not null)
            {
                _deviceStatusLogsByRecordKey[recordKey] = log;
            }
        }
    }

    /// <summary>
    /// Provides an explicit Ctrl+A shortcut for selecting every device-status row.
    /// </summary>
    private void DgvPending_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsDeviceStatusTab() || !e.Control || e.KeyCode != Keys.A)
        {
            return;
        }

        dgvPending.SelectAll();
        e.Handled = true;
        e.SuppressKeyPress = true;
        ApplyDeletePermissionForActiveTab();
    }

    private async Task<int> ExecuteAllPendingUploadsAsync()
    {
        var count = 0;
        var taskTypes = new[]
        {
            ProductionConstants.UploadTaskTypes.StartReport,
            ProductionConstants.UploadTaskTypes.ProcessParameter,
            ProductionConstants.UploadTaskTypes.ReportFile,
            ProductionConstants.UploadTaskTypes.FinishReport
        };

        foreach (var taskType in taskTypes)
        {
            count += await _uploadTaskService.ExecuteAllPendingAsync(taskType);
        }

        return count;
    }

    /// <summary>
    /// 数据源重置过渡期内，DataGridView 内部仍按旧行数取值校验会抛越界异常；
    /// 吞掉该异常避免弹出默认错误对话框，其余异常保留默认处理以免掩盖真实问题。
    /// </summary>
    private void DgvPending_DataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        if (e.Exception is IndexOutOfRangeException)
        {
            e.ThrowException = false;
        }
    }

    private void DgvPending_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0
            || e.RowIndex >= dgvPending.Rows.Count
            || e.ColumnIndex < 0
            || e.ColumnIndex >= dgvPending.Columns.Count
            || e.CellStyle is null)
        {
            return;
        }

        object? item;
        try
        {
            item = dgvPending.Rows[e.RowIndex].DataBoundItem;
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }
        catch (InvalidOperationException)
        {
            return;
        }
        if (item is UploadPendingSummaryRow summary)
        {
            FormatSummaryCell(summary, e);
            return;
        }

        if (item is ProgramSyncSummary program)
        {
            FormatProgramCell(program, e);
            return;
        }

        if (item is UploadTaskSummary task)
        {
            FormatUploadTaskCell(task, e);
        }
    }

    private void FormatSummaryCell(UploadPendingSummaryRow item, DataGridViewCellFormattingEventArgs e)
    {
        if (e.CellStyle is null)
        {
            return;
        }

        var column = dgvPending.Columns[e.ColumnIndex];
        var rawText = Convert.ToString(e.Value);
        if (column.DataPropertyName.EndsWith("Status", StringComparison.Ordinal))
        {
            e.Value = GetUploadStatusText(rawText);
            e.FormattingApplied = true;
        }

        e.CellStyle.ForeColor = rawText switch
        {
            ProductionConstants.UploadStatuses.Failed => Color.Firebrick,
            ProductionConstants.UploadStatuses.Uploading => Color.RoyalBlue,
            ProductionConstants.UploadStatuses.Pending => Color.DarkOrange,
            ProductionConstants.UploadStatuses.Retrying => Color.DarkOrange,
            ProductionConstants.UploadStatuses.Uploaded => Color.SeaGreen,
            _ => item.PendingCount > 0 ? Color.DarkOrange : Color.FromArgb(36, 36, 36)
        };
    }

    private void FormatProgramCell(ProgramSyncSummary item, DataGridViewCellFormattingEventArgs e)
    {
        if (e.CellStyle is null)
        {
            return;
        }

        var column = dgvPending.Columns[e.ColumnIndex];
        if (string.Equals(column.DataPropertyName, nameof(ProgramSyncSummary.SyncStatus), StringComparison.Ordinal))
        {
            e.Value = GetProgramSyncStatusText(Convert.ToString(e.Value));
            e.FormattingApplied = true;
        }
        else if (string.Equals(column.DataPropertyName, nameof(ProgramSyncSummary.SyncAction), StringComparison.Ordinal))
        {
            e.Value = GetProgramSyncActionText(Convert.ToString(e.Value));
            e.FormattingApplied = true;
        }

        e.CellStyle.ForeColor = string.Equals(item.SyncStatus, AppConstants.ProgramSyncStatus.Failed, StringComparison.OrdinalIgnoreCase)
            ? Color.Firebrick
            : Color.DarkOrange;
    }

    private void FormatUploadTaskCell(UploadTaskSummary item, DataGridViewCellFormattingEventArgs e)
    {
        if (e.CellStyle is null)
        {
            return;
        }

        var column = dgvPending.Columns[e.ColumnIndex];
        if (string.Equals(column.DataPropertyName, nameof(UploadTaskSummary.Status), StringComparison.Ordinal))
        {
            e.Value = GetUploadStatusText(Convert.ToString(e.Value));
            e.FormattingApplied = true;
        }

        e.CellStyle.ForeColor = item.Status switch
        {
            ProductionConstants.UploadStatuses.Failed => Color.Firebrick,
            ProductionConstants.UploadStatuses.Uploaded => Color.SeaGreen,
            ProductionConstants.UploadStatuses.Retrying => Color.DarkOrange,
            _ => Color.FromArgb(36, 36, 36)
        };
    }

    private string GetActiveUploadTaskType()
    {
        if (tabUploadCategories.SelectedTab == tabReportFiles)
        {
            return ProductionConstants.UploadTaskTypes.ReportFile;
        }

        if (tabUploadCategories.SelectedTab == tabStartReports)
        {
            return ProductionConstants.UploadTaskTypes.StartReport;
        }

        if (tabUploadCategories.SelectedTab == tabFinishReports)
        {
            return ProductionConstants.UploadTaskTypes.FinishReport;
        }

        if (tabUploadCategories.SelectedTab == tabWorkOrderStatuses)
        {
            return ProductionConstants.UploadTaskTypes.WorkOrderStatus;
        }

        if (tabUploadCategories.SelectedTab == tabDeviceStatuses)
        {
            return ProductionConstants.UploadTaskTypes.DeviceStatus;
        }

        return ProductionConstants.UploadTaskTypes.ProcessParameter;
    }

    private string GetActiveCategoryText()
    {
        if (IsSummaryTab())
        {
            return "工单信息";
        }

        if (tabUploadCategories.SelectedTab == tabReportFiles)
        {
            return "报告文件";
        }

        if (tabUploadCategories.SelectedTab == tabStartReports)
        {
            return "开工信息";
        }

        if (tabUploadCategories.SelectedTab == tabFinishReports)
        {
            return "完工信息";
        }

        if (tabUploadCategories.SelectedTab == tabWorkOrderStatuses)
        {
            return "工单状态";
        }

        if (tabUploadCategories.SelectedTab == tabDeviceStatuses)
        {
            return "设备状态";
        }

        if (tabUploadCategories.SelectedTab == tabProgramFiles)
        {
            return "程序文件";
        }

        return "过程参数";
    }

    private bool IsProgramFileTab()
    {
        return tabUploadCategories.SelectedTab == tabProgramFiles;
    }

    private bool IsStartReportTab()
    {
        return tabUploadCategories.SelectedTab == tabStartReports;
    }

    private bool IsReportFileTab()
    {
        return tabUploadCategories.SelectedTab == tabReportFiles;
    }

    private bool IsFinishReportTab()
    {
        return tabUploadCategories.SelectedTab == tabFinishReports;
    }

    private bool IsProcessParameterTab()
    {
        return tabUploadCategories.SelectedTab == tabProcessParameters;
    }

    private bool IsDeviceStatusTab()
    {
        return tabUploadCategories.SelectedTab == tabDeviceStatuses;
    }

    private bool IsSummaryTab()
    {
        return tabUploadCategories.SelectedTab == tabSummary;
    }

    private bool HasVisibleTabs()
    {
        return tabUploadCategories.TabPages.Count > 0;
    }

    private string GetProgramSyncStatusText(string? status)
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

    private string GetProgramSyncActionText(string? action)
    {
        return action switch
        {
            AppConstants.ProgramSyncActions.Create => _localizer.GetString(TextKeys.ProgramManage.ActionCreate),
            AppConstants.ProgramSyncActions.Update => _localizer.GetString(TextKeys.ProgramManage.ActionUpdate),
            AppConstants.ProgramSyncActions.Delete => _localizer.GetString(TextKeys.ProgramManage.ActionDelete),
            _ => action ?? string.Empty
        };
    }

    private string GetUploadStatusText(string? status)
    {
        return UploadStatusDisplayRules.GetDisplayText(status, _mesConnectionMonitor.Current.IsConnected);
    }

    private void ShowInfo(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleInfo), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleWarning), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ShowErrorMessage(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleError), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private StateUploadTabKind GetActiveTabKind()
    {
        if (IsSummaryTab()) return StateUploadTabKind.Summary;
        if (IsProgramFileTab()) return StateUploadTabKind.ProgramFile;
        if (IsProcessParameterTab()) return StateUploadTabKind.ProcessParameter;
        if (IsStartReportTab()) return StateUploadTabKind.StartReport;
        if (IsFinishReportTab()) return StateUploadTabKind.FinishReport;
        if (IsReportFileTab()) return StateUploadTabKind.ReportFile;
        if (IsWorkOrderStatusTab()) return StateUploadTabKind.WorkOrderStatus;
        if (IsDeviceStatusTab()) return StateUploadTabKind.DeviceStatus;
        throw new InvalidOperationException("当前没有可用的上传页签。");
    }

    private bool IsWorkOrderStatusTab()
        => tabUploadCategories.SelectedTab == tabWorkOrderStatuses;

    private enum StateUploadTabKind
    {
        Summary,
        StartReport,
        FinishReport,
        ProcessParameter,
        ReportFile,
        WorkOrderStatus,
        DeviceStatus,
        ProgramFile
    }

    private sealed record StateUploadLoadResult(
        object Rows,
        int PendingCount,
        IReadOnlyList<BizDeviceStatusLog>? DeviceStatusLogs);

    private sealed record StateUploadTabDefinition(TabPage Page, string PermissionCode);
}
