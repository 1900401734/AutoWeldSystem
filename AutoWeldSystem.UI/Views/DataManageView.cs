using System.Diagnostics;
using System.Text.Json;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.DataManagement;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Views;

/// <summary>
/// Provides local work-order history and task-level production details.
/// Static controls and fixed columns are declared in the designer file.
/// </summary>
public partial class DataManageView : BaseView
{
    private const string DynamicColumnTagPrefix = "data-history-dynamic:";

    private readonly IDataHistoryQueryService _historyQueryService = null!;
    private readonly ILocalizationService _localizer = null!;
    private CancellationTokenSource? _workOrderQueryCancellation;
    private CancellationTokenSource? _detailQueryCancellation;
    private bool _initialized;
    private bool _suppressWorkOrderSelection;
    private bool _updatingWorkOrderPagination;
    private bool _updatingCollectionPagination;
    private int _selectedTaskId;

    /// <summary>
    /// Constructor used only by the WinForms designer.
    /// </summary>
    public DataManageView()
    {
        InitializeComponent();
    }

    public DataManageView(
        IDataHistoryQueryService historyQueryService,
        ILocalizationService localizer)
    {
        _historyQueryService = historyQueryService;
        _localizer = localizer;

        InitializeComponent();
        ConfigureGrids();
        WireEvents();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (IsDesignEnvironment || _initialized)
        {
            return;
        }

        _initialized = true;
        SetDefaultDateRange();
        ApplyLocalizedTexts();
        _ = QueryWorkOrdersAsync(resetPage: true);
    }

    protected override void OnLanguageChanged()
    {
        if (IsDesignEnvironment || _localizer is null)
        {
            return;
        }

        ApplyLocalizedTexts();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _workOrderQueryCancellation?.Cancel();
        _detailQueryCancellation?.Cancel();
        base.OnHandleDestroyed(e);
    }

    private bool IsDesignEnvironment
        => _historyQueryService is null
            || _localizer is null
            || DesignMode
            || System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime;

    private void ConfigureGrids()
    {
        ConfigureGrid(dgvWorkOrders, DataGridViewAutoSizeColumnsMode.DisplayedCells);
        ConfigureGrid(dgvWeldParameters, DataGridViewAutoSizeColumnsMode.DisplayedCells);
        ConfigureGrid(dgvCollectionRecords, DataGridViewAutoSizeColumnsMode.DisplayedCells);
        ConfigureGrid(dgvReportFiles, DataGridViewAutoSizeColumnsMode.Fill);
    }

    private static void ConfigureGrid(DataGridView grid, DataGridViewAutoSizeColumnsMode autoSizeMode)
    {
        TableStyleHelper.ApplyDataGridView(grid);
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = autoSizeMode;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        // The shared helper uses Fill mode. History tables can contain many columns,
        // therefore each column must size by content and use horizontal scrolling.
        foreach (DataGridViewColumn column in grid.Columns)
        {
            column.AutoSizeMode = autoSizeMode == DataGridViewAutoSizeColumnsMode.Fill
                ? DataGridViewAutoSizeColumnMode.Fill
                : DataGridViewAutoSizeColumnMode.DisplayedCells;
        }
    }

    private void WireEvents()
    {
        btnQuery.Click += async (_, _) => await QueryWorkOrdersAsync(resetPage: true);
        btnReset.Click += async (_, _) => await ResetQueryAsync();
        txtProductNum.KeyDown += FilterInput_KeyDown;
        txtBatch.KeyDown += FilterInput_KeyDown;
        txtWorkOrder.KeyDown += FilterInput_KeyDown;
        workOrderPagination.ValueChanged += WorkOrderPagination_ValueChanged;
        collectionPagination.ValueChanged += CollectionPagination_ValueChanged;
        dgvWorkOrders.SelectionChanged += WorkOrders_SelectionChanged;
        dgvWeldParameters.CellFormatting += WeldParameters_CellFormatting;
        dgvCollectionRecords.SelectionChanged += CollectionRecords_SelectionChanged;
        dgvReportFiles.SelectionChanged += (_, _) => UpdateReportButtons();
        dgvReportFiles.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                OpenSelectedReport();
            }
        };
        btnOpenReport.Click += (_, _) => OpenSelectedReport();
        btnOpenReportFolder.Click += (_, _) => OpenSelectedReportFolder();
    }

    private async void FilterInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        await QueryWorkOrdersAsync(resetPage: true);
    }

    private async void WorkOrderPagination_ValueChanged(object sender, AntdUI.PagePageEventArgs e)
    {
        if (_updatingWorkOrderPagination)
        {
            return;
        }

        await QueryWorkOrdersAsync(resetPage: false, e.Current, e.PageSize);
    }

    private async void CollectionPagination_ValueChanged(object sender, AntdUI.PagePageEventArgs e)
    {
        if (!_updatingCollectionPagination && _selectedTaskId > 0)
        {
            await LoadCollectionRecordsAsync(_selectedTaskId, e.Current, e.PageSize, GetDetailToken());
        }
    }

    private async void WorkOrders_SelectionChanged(object? sender, EventArgs e)
    {
        if (_suppressWorkOrderSelection
            || dgvWorkOrders.CurrentRow?.DataBoundItem is not DataHistoryWorkOrderRow row)
        {
            return;
        }

        await LoadTaskDetailsAsync(row.TaskId);
    }

    private async Task QueryWorkOrdersAsync(
        bool resetPage,
        int? requestedPage = null,
        int? requestedPageSize = null)
    {
        CancelAndDispose(ref _workOrderQueryCancellation);
        _workOrderQueryCancellation = new CancellationTokenSource();
        var cancellationToken = _workOrderQueryCancellation.Token;
        var pageIndex = resetPage ? 1 : Math.Max(1, requestedPage ?? workOrderPagination.Current);
        var pageSize = Math.Max(1, requestedPageSize ?? workOrderPagination.PageSize);

        SetQueryBusy(true);
        try
        {
            var result = await _historyQueryService.QueryWorkOrdersAsync(
                BuildCriteria(),
                pageIndex,
                pageSize,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _suppressWorkOrderSelection = true;
            try
            {
                workOrderBindingSource.DataSource = result.Items.ToList();
                _updatingWorkOrderPagination = true;
                try
                {
                    workOrderPagination.Total = result.TotalCount;
                    workOrderPagination.PageSize = result.PageSize;
                    workOrderPagination.Current = result.PageIndex;
                }
                finally
                {
                    _updatingWorkOrderPagination = false;
                }

                lblWorkOrderSummary.Text = _localizer.GetString(
                    TextKeys.DataManage.WorkOrderSummary,
                    result.TotalCount);
            }
            finally
            {
                _suppressWorkOrderSelection = false;
            }

            if (result.Items.Count == 0)
            {
                ClearTaskDetails();
                return;
            }

            _suppressWorkOrderSelection = true;
            try
            {
                workOrderBindingSource.Position = 0;

                // DataGridView rows and cells can be generated after the BindingSource
                // has already received its data. Selection is optional, so only touch
                // the visual row when both collections are ready.
                if (dgvWorkOrders.Rows.Count > 0 && dgvWorkOrders.Rows[0].Cells.Count > 0)
                {
                    dgvWorkOrders.ClearSelection();
                    dgvWorkOrders.Rows[0].Selected = true;
                    dgvWorkOrders.CurrentCell = dgvWorkOrders.Rows[0].Cells[0];
                }
            }
            finally
            {
                _suppressWorkOrderSelection = false;
            }

            await LoadTaskDetailsAsync(result.Items[0].TaskId);
        }
        catch (OperationCanceledException)
        {
            // A newer query superseded this request.
        }
        catch (Exception ex)
        {
            ShowError(_localizer.GetString(TextKeys.DataManage.QueryFailed, ex.Message));
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                SetQueryBusy(false);
            }
        }
    }

    private async Task ResetQueryAsync()
    {
        txtProductNum.Text = string.Empty;
        txtBatch.Text = string.Empty;
        txtWorkOrder.Text = string.Empty;
        SetDefaultDateRange();
        await QueryWorkOrdersAsync(resetPage: true);
    }

    private async Task LoadTaskDetailsAsync(int taskId)
    {
        CancelAndDispose(ref _detailQueryCancellation);
        _detailQueryCancellation = new CancellationTokenSource();
        var cancellationToken = _detailQueryCancellation.Token;
        _selectedTaskId = taskId;
        _updatingCollectionPagination = true;
        try
        {
            collectionPagination.Current = 1;
        }
        finally
        {
            _updatingCollectionPagination = false;
        }

        SetDetailBusy(true);
        try
        {
            var parameterTask = _historyQueryService.QueryWeldParametersAsync(taskId, cancellationToken);
            var collectionTask = _historyQueryService.QueryCollectionRecordsAsync(
                taskId,
                1,
                collectionPagination.PageSize,
                cancellationToken);
            var reportTask = _historyQueryService.QueryReportFilesAsync(taskId, cancellationToken);
            await Task.WhenAll(parameterTask, collectionTask, reportTask);
            cancellationToken.ThrowIfCancellationRequested();

            BindWeldParameters(await parameterTask);
            BindCollectionRecords(await collectionTask);
            BindReportFiles(await reportTask);
        }
        catch (OperationCanceledException)
        {
            // The user selected another task before this detail query completed.
        }
        catch (Exception ex)
        {
            ShowError(_localizer.GetString(TextKeys.DataManage.DetailQueryFailed, ex.Message));
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                SetDetailBusy(false);
            }
        }
    }

    private async Task LoadCollectionRecordsAsync(
        int taskId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _historyQueryService.QueryCollectionRecordsAsync(
                taskId,
                pageIndex,
                pageSize,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            BindCollectionRecords(result);
        }
        catch (OperationCanceledException)
        {
            // The selected work order changed.
        }
        catch (Exception ex)
        {
            ShowError(_localizer.GetString(TextKeys.DataManage.DetailQueryFailed, ex.Message));
        }
    }

    private void BindWeldParameters(DataHistoryWeldParameterResult result)
    {
        RemoveDynamicParameterColumns();
        foreach (var definition in result.DynamicColumns)
        {
            dgvWeldParameters.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = definition.HeaderText,
                MinimumWidth = 110,
                Name = $"dynamic_{definition.Key}",
                ReadOnly = true,
                Tag = $"{DynamicColumnTagPrefix}{definition.Key}",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells
            });
        }

        parameterBindingSource.DataSource = result.Rows.ToList();
        lblParameterSummary.Text = _localizer.GetString(
            TextKeys.DataManage.ParameterSummary,
            result.Rows.Count,
            result.DynamicColumns.Count);
    }

    private void BindCollectionRecords(PagedResult<DataHistoryCollectionRow> result)
    {
        collectionBindingSource.DataSource = result.Items.ToList();
        _updatingCollectionPagination = true;
        try
        {
            collectionPagination.Total = result.TotalCount;
            collectionPagination.PageSize = result.PageSize;
            collectionPagination.Current = result.PageIndex;
        }
        finally
        {
            _updatingCollectionPagination = false;
        }
        lblCollectionSummary.Text = _localizer.GetString(
            TextKeys.DataManage.CollectionSummary,
            result.TotalCount);
        ShowSelectedRawData();
    }

    private void BindReportFiles(IReadOnlyList<DataHistoryReportFileRow> rows)
    {
        reportBindingSource.DataSource = rows.ToList();
        lblReportSummary.Text = _localizer.GetString(TextKeys.DataManage.ReportSummary, rows.Count);
        UpdateReportButtons();
    }

    private void WeldParameters_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0
            || dgvWeldParameters.Columns[e.ColumnIndex].Tag is not string tag
            || !tag.StartsWith(DynamicColumnTagPrefix, StringComparison.Ordinal)
            || dgvWeldParameters.Rows[e.RowIndex].DataBoundItem is not DataHistoryWeldParameterRow row)
        {
            return;
        }

        var key = tag[DynamicColumnTagPrefix.Length..];
        e.Value = row.DynamicValues.TryGetValue(key, out var value) ? value : string.Empty;
        e.FormattingApplied = true;
    }

    private void CollectionRecords_SelectionChanged(object? sender, EventArgs e)
    {
        ShowSelectedRawData();
    }

    private void ShowSelectedRawData()
    {
        var json = (dgvCollectionRecords.CurrentRow?.DataBoundItem as DataHistoryCollectionRow)?.RawDataJson;
        txtRawData.Text = FormatJsonOrOriginal(json);
    }

    private void RemoveDynamicParameterColumns()
    {
        var dynamicColumns = dgvWeldParameters.Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Tag is string tag
                && tag.StartsWith(DynamicColumnTagPrefix, StringComparison.Ordinal))
            .ToList();
        foreach (var column in dynamicColumns)
        {
            dgvWeldParameters.Columns.Remove(column);
            column.Dispose();
        }
    }

    private void OpenSelectedReport()
    {
        var report = GetSelectedReport();
        if (report is null)
        {
            ShowWarning(_localizer.GetString(TextKeys.DataManage.SelectReport));
            return;
        }

        if (!File.Exists(report.FilePath))
        {
            ShowWarning(_localizer.GetString(TextKeys.DataManage.ReportFileMissing, report.FilePath));
            UpdateReportButtons();
            return;
        }

        OpenShellPath(report.FilePath);
    }

    private void OpenSelectedReportFolder()
    {
        var report = GetSelectedReport();
        if (report is null)
        {
            ShowWarning(_localizer.GetString(TextKeys.DataManage.SelectReport));
            return;
        }

        var directory = Path.GetDirectoryName(report.FilePath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            ShowWarning(_localizer.GetString(TextKeys.DataManage.ReportDirectoryMissing, directory ?? report.FilePath));
            UpdateReportButtons();
            return;
        }

        OpenShellPath(directory);
    }

    private void UpdateReportButtons()
    {
        var report = GetSelectedReport();
        btnOpenReport.Enabled = report is not null && File.Exists(report.FilePath);
        var directory = report is null ? null : Path.GetDirectoryName(report.FilePath);
        btnOpenReportFolder.Enabled = !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory);
    }

    private DataHistoryReportFileRow? GetSelectedReport()
    {
        return dgvReportFiles.CurrentRow?.DataBoundItem as DataHistoryReportFileRow;
    }

    private void ClearTaskDetails()
    {
        CancelAndDispose(ref _detailQueryCancellation);
        _selectedTaskId = 0;
        RemoveDynamicParameterColumns();
        parameterBindingSource.DataSource = Array.Empty<DataHistoryWeldParameterRow>();
        collectionBindingSource.DataSource = Array.Empty<DataHistoryCollectionRow>();
        reportBindingSource.DataSource = Array.Empty<DataHistoryReportFileRow>();
        _updatingCollectionPagination = true;
        try
        {
            collectionPagination.Total = 0;
            collectionPagination.Current = 1;
        }
        finally
        {
            _updatingCollectionPagination = false;
        }
        txtRawData.Clear();
        lblParameterSummary.Text = _localizer.GetString(TextKeys.DataManage.SelectWorkOrder);
        lblCollectionSummary.Text = _localizer.GetString(TextKeys.DataManage.SelectWorkOrder);
        lblReportSummary.Text = _localizer.GetString(TextKeys.DataManage.SelectWorkOrder);
        UpdateReportButtons();
    }

    private DataHistoryQueryCriteria BuildCriteria()
    {
        var selectedRange = dateRange.Value ?? Array.Empty<DateTime>();
        var startDate = selectedRange.Length > 0 ? selectedRange[0].Date : DateTime.Today;
        var endDate = selectedRange.Length > 1 ? selectedRange[1].Date : startDate;
        return new DataHistoryQueryCriteria
        {
            ProductNum = txtProductNum.Text,
            Batch = txtBatch.Text,
            SN = txtWorkOrder.Text,
            StartTime = startDate,
            EndTime = endDate.AddDays(1).AddTicks(-1)
        };
    }

    private void SetDefaultDateRange()
    {
        var today = DateTime.Today;
        dateRange.Value = new[] { new DateTime(today.Year, today.Month, 1), today };
    }

    private CancellationToken GetDetailToken()
    {
        return _detailQueryCancellation?.Token ?? CancellationToken.None;
    }

    private void SetQueryBusy(bool busy)
    {
        btnQuery.Loading = busy;
        btnQuery.Enabled = !busy;
        btnReset.Enabled = !busy;
        workOrderPagination.Enabled = !busy;
    }

    private void SetDetailBusy(bool busy)
    {
        detailTabs.Enabled = !busy;
        if (busy)
        {
            lblParameterSummary.Text = _localizer.GetString(TextKeys.DataManage.Loading);
            lblCollectionSummary.Text = _localizer.GetString(TextKeys.DataManage.Loading);
            lblReportSummary.Text = _localizer.GetString(TextKeys.DataManage.Loading);
        }
    }

    private void ApplyLocalizedTexts()
    {
        lblProductNum.Text = _localizer.GetString(TextKeys.DataManage.ProductNum);
        lblBatch.Text = _localizer.GetString(TextKeys.DataManage.Batch);
        lblWorkOrder.Text = _localizer.GetString(TextKeys.DataManage.WorkOrderId);
        lblDateRange.Text = _localizer.GetString(TextKeys.DataManage.DateRange);
        txtProductNum.PlaceholderText = _localizer.GetString(TextKeys.DataManage.FuzzySearch);
        txtBatch.PlaceholderText = _localizer.GetString(TextKeys.DataManage.FuzzySearch);
        txtWorkOrder.PlaceholderText = _localizer.GetString(TextKeys.DataManage.WorkOrderPlaceholder);
        btnQuery.Text = _localizer.GetString(TextKeys.DataManage.Query);
        btnReset.Text = _localizer.GetString(TextKeys.DataManage.Reset);
        tabWeldParameters.Text = _localizer.GetString(TextKeys.DataManage.TabWeldParameters);
        tabCollectionData.Text = _localizer.GetString(TextKeys.DataManage.TabCollectionData);
        tabReportFiles.Text = _localizer.GetString(TextKeys.DataManage.TabReportFiles);
        lblRawData.Text = _localizer.GetString(TextKeys.DataManage.RawData);
        btnOpenReport.Text = _localizer.GetString(TextKeys.DataManage.OpenReport);
        btnOpenReportFolder.Text = _localizer.GetString(TextKeys.DataManage.OpenReportFolder);
        ApplyColumnHeaders();
    }

    private void ApplyColumnHeaders()
    {
        colTaskStation.HeaderText = T(TextKeys.DataManage.ColumnStation);
        colTaskWorkOrder.HeaderText = T(TextKeys.DataManage.ColumnWorkOrderId);
        colTaskProductNum.HeaderText = T(TextKeys.DataManage.ColumnProductNum);
        colTaskBatch.HeaderText = T(TextKeys.DataManage.ColumnBatch);
        colTaskProductName.HeaderText = T(TextKeys.DataManage.ColumnProductName);
        colTaskProcess.HeaderText = T(TextKeys.DataManage.ColumnProcess);
        colTaskRecipe.HeaderText = T(TextKeys.DataManage.ColumnRecipe);
        colTaskPlannedQty.HeaderText = T(TextKeys.DataManage.ColumnPlannedQty);
        colTaskActualQty.HeaderText = T(TextKeys.DataManage.ColumnActualQty);
        colTaskQualifiedQty.HeaderText = T(TextKeys.DataManage.ColumnQualifiedQty);
        colTaskFailedQty.HeaderText = T(TextKeys.DataManage.ColumnFailedQty);
        colTaskOperator.HeaderText = T(TextKeys.DataManage.ColumnOperator);
        colTaskStartTime.HeaderText = T(TextKeys.DataManage.ColumnStartTime);
        colTaskEndTime.HeaderText = T(TextKeys.DataManage.ColumnEndTime);
        colTaskStatus.HeaderText = T(TextKeys.DataManage.ColumnTaskStatus);
        colTaskUploadStatus.HeaderText = T(TextKeys.DataManage.ColumnUploadStatus);

        colParameterStation.HeaderText = T(TextKeys.DataManage.ColumnStation);
        colParameterProductNo.HeaderText = T(TextKeys.DataManage.ColumnProductNo);
        colParameterTouchNo.HeaderText = T(TextKeys.DataManage.ColumnTouchNo);
        colParameterResult.HeaderText = T(TextKeys.DataManage.ColumnTouchResult);
        colParameterRecordTime.HeaderText = T(TextKeys.DataManage.ColumnRecordTime);

        colCollectionSequence.HeaderText = T(TextKeys.DataManage.ColumnSequence);
        colCollectionStation.HeaderText = T(TextKeys.DataManage.ColumnStation);
        colCollectionProductNo.HeaderText = T(TextKeys.DataManage.ColumnProductNo);
        colCollectionTouchNo.HeaderText = T(TextKeys.DataManage.ColumnTouchNo);
        colCollectionResult.HeaderText = T(TextKeys.DataManage.ColumnTouchResult);
        colCollectionIsTest.HeaderText = T(TextKeys.DataManage.ColumnIsTest);
        colCollectionCompleted.HeaderText = T(TextKeys.DataManage.ColumnProductCompleted);
        colCollectionUploadStatus.HeaderText = T(TextKeys.DataManage.ColumnUploadStatus);
        colCollectionOperator.HeaderText = T(TextKeys.DataManage.ColumnOperator);
        colCollectionRecordTime.HeaderText = T(TextKeys.DataManage.ColumnRecordTime);

        colReportFileName.HeaderText = T(TextKeys.DataManage.ColumnFileName);
        colReportFormat.HeaderText = T(TextKeys.DataManage.ColumnFileFormat);
        colReportPath.HeaderText = T(TextKeys.DataManage.ColumnFilePath);
        colReportUploadStatus.HeaderText = T(TextKeys.DataManage.ColumnUploadStatus);
        colReportCreatedTime.HeaderText = T(TextKeys.DataManage.ColumnCreatedTime);
        colReportUpdatedTime.HeaderText = T(TextKeys.DataManage.ColumnUpdatedTime);
    }

    private string T(string key)
    {
        return _localizer.GetString(key);
    }

    private static string FormatJsonOrOriginal(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private void OpenShellPath(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowError(_localizer.GetString(TextKeys.DataManage.OpenPathFailed, ex.Message));
        }
    }

    private static void CancelAndDispose(ref CancellationTokenSource? source)
    {
        source?.Cancel();
        source?.Dispose();
        source = null;
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(this, message, T(TextKeys.Common.TitleWarning), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ShowError(string message)
    {
        MessageBox.Show(this, message, T(TextKeys.Common.TitleError), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
