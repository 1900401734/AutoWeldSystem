using System.Diagnostics;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.DataManagement;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Production;
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
    private const string DateTimeDisplayFormat = "yyyy-MM-dd HH:mm:ss";

    private readonly IDataHistoryQueryService _historyQueryService = null!;
    private readonly ILocalizationService _localizer = null!;
    private readonly IAppSettingsService _appSettingsService = null!;
    private CancellationTokenSource? _workOrderQueryCancellation;
    private CancellationTokenSource? _detailQueryCancellation;
    private bool _initialized;
    private bool _suppressWorkOrderSelection;
    private bool _updatingWorkOrderPagination;
    private bool _disposing;
    private int _selectedTaskId;
    private IReadOnlyList<DataHistoryDynamicColumn> _testDataDynamicColumns = [];
    private IReadOnlyList<DataHistoryTestDataRow> _testDataRows = [];
    private IReadOnlyList<DataHistoryTestDataRow> _visibleTestDataRows = [];
    private string _productResultFilter = DataHistoryTestDataRules.AllResults;
    private bool _suppressProductResultFilter;
    private string? _testDataSortColumnKey;
    private bool _testDataSortDescending;

    /// <summary>
    /// Constructor used only by the WinForms designer.
    /// </summary>
    public DataManageView()
    {
        InitializeComponent();
    }

    public DataManageView(
        IDataHistoryQueryService historyQueryService,
        ILocalizationService localizer,
        IAppSettingsService appSettingsService)
    {
        _historyQueryService = historyQueryService;
        _localizer = localizer;
        _appSettingsService = appSettingsService;

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
        ApplyDefaultSplitterLayout();
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
        ConfigureStaticGridColumns();
        ConfigureGrid(dgvWorkOrders, DataGridViewAutoSizeColumnsMode.DisplayedCells);
        ConfigureGrid(dgvReportFiles, DataGridViewAutoSizeColumnsMode.Fill);
        TableStyleHelper.ApplyAntdTable(tableTestData);
        tableTestData.DefaultExpand = true;
        ConfigureTestDataColumns([]);
    }

    private void ConfigureTestDataColumns(IReadOnlyList<DataHistoryDynamicColumn> dynamicColumns)
    {
        tableTestData.Columns.Clear();

        var nodeColumn = CreateTestDataColumn(
            nameof(DataHistoryTestDataRow.NodeText),
            T(TextKeys.DataManage.ColumnTestNode));
        nodeColumn.Align = AntdUI.ColumnAlign.Left;
        nodeColumn.SetTree(nameof(DataHistoryTestDataRow.Children));
        tableTestData.Columns.Add(nodeColumn);
        tableTestData.Columns.Add(CreateTestDataColumn(
            nameof(DataHistoryTestDataRow.StationNo),
            T(TextKeys.DataManage.ColumnStation)));
        tableTestData.Columns.Add(new AntdUI.Column(
            nameof(DataHistoryTestDataRow.TestResult),
            T(TextKeys.DataManage.ColumnTouchResult))
        {
            Align = AntdUI.ColumnAlign.Center,
            ColAlign = AntdUI.ColumnAlign.Center,
            Ellipsis = true,
            ReadOnly = true,
            Render = (_, record, _) => record is DataHistoryTestDataRow row
                ? TestResultRules.ToDisplayText(row.IsProductRow ? row.ProductResult : row.TestResult)
                : string.Empty
        });
        tableTestData.Columns.Add(new AntdUI.Column(
            nameof(DataHistoryTestDataRow.UploadStatus),
            T(TextKeys.DataManage.ColumnUploadStatus))
        {
            Align = AntdUI.ColumnAlign.Center,
            ColAlign = AntdUI.ColumnAlign.Center,
            Ellipsis = true,
            ReadOnly = true,
            Render = (_, record, _) => record is DataHistoryTestDataRow row
                ? UploadStatusDisplayRules.GetDisplayText(row.UploadStatus)
                : string.Empty
        });
        tableTestData.Columns.Add(new AntdUI.Column(
            nameof(DataHistoryTestDataRow.TestCount),
            T(TextKeys.DataManage.ColumnTestCount))
        {
            Align = AntdUI.ColumnAlign.Center,
            ColAlign = AntdUI.ColumnAlign.Center,
            ReadOnly = true,
            Render = (_, record, _) => record is DataHistoryTestDataRow { IsProductRow: true } row
                ? row.TestCount
                : string.Empty
        });
        tableTestData.Columns.Add(new AntdUI.Column(
            nameof(DataHistoryTestDataRow.RecordTime),
            T(TextKeys.DataManage.ColumnLastRecordTime))
        {
            Align = AntdUI.ColumnAlign.Center,
            ColAlign = AntdUI.ColumnAlign.Center,
            Ellipsis = true,
            ReadOnly = true,
            Render = (_, record, _) => record is DataHistoryTestDataRow row && row.RecordTime.HasValue
                ? row.RecordTime.Value.ToString(DateTimeDisplayFormat)
                : string.Empty
        });

        foreach (var dynamicColumn in dynamicColumns)
        {
            tableTestData.Columns.Add(new AntdUI.Column(dynamicColumn.Key, dynamicColumn.HeaderText)
            {
                SortOrder = true,
                SortMode = string.Equals(_testDataSortColumnKey, dynamicColumn.Key, StringComparison.OrdinalIgnoreCase)
                    ? (_testDataSortDescending ? AntdUI.SortMode.DESC : AntdUI.SortMode.ASC)
                    : AntdUI.SortMode.NONE,
                Align = AntdUI.ColumnAlign.Center,
                ColAlign = AntdUI.ColumnAlign.Center,
                Ellipsis = true,
                ReadOnly = true,
                Render = (_, record, _) => record is DataHistoryTestDataRow row
                    && row.DynamicValues.TryGetValue(dynamicColumn.Key, out var value)
                        ? value
                        : string.Empty
            });
        }

        TableStyleHelper.ApplyAntdColumnDefaults(tableTestData);
        nodeColumn.Align = AntdUI.ColumnAlign.Left;
    }

    private static AntdUI.Column CreateTestDataColumn(string key, string title)
    {
        return new AntdUI.Column(key, title)
        {
            Align = AntdUI.ColumnAlign.Center,
            ColAlign = AntdUI.ColumnAlign.Center,
            Ellipsis = true,
            ReadOnly = true
        };
    }

    /// <summary>
    /// Configures fixed DataGridView columns that cannot be auto-generated.
    /// </summary>
    private void ConfigureStaticGridColumns()
    {
        ConfigureColumn(colTaskStation, nameof(DataHistoryWorkOrderRow.StationNo));
        ConfigureColumn(colTaskWorkOrder, nameof(DataHistoryWorkOrderRow.WorkOrderId));
        ConfigureColumn(colTaskProductNum, nameof(DataHistoryWorkOrderRow.ProductNum));
        ConfigureColumn(colTaskBatch, nameof(DataHistoryWorkOrderRow.Batch));
        ConfigureColumn(colTaskProductName, nameof(DataHistoryWorkOrderRow.ProductName));
        ConfigureColumn(colTaskProcess, nameof(DataHistoryWorkOrderRow.ProcessDisplay));
        ConfigureColumn(colTaskRecipe, nameof(DataHistoryWorkOrderRow.RecipeCode));
        ConfigureColumn(colTaskPlannedQty, nameof(DataHistoryWorkOrderRow.PlannedQty));
        ConfigureColumn(colTaskActualQty, nameof(DataHistoryWorkOrderRow.ActualQty));
        ConfigureColumn(colTaskQualifiedQty, nameof(DataHistoryWorkOrderRow.QualifiedQty));
        ConfigureColumn(colTaskFailedQty, nameof(DataHistoryWorkOrderRow.FailedQty));
        ConfigureColumn(colTaskOperator, nameof(DataHistoryWorkOrderRow.OperatorNumber));
        ConfigureColumn(colTaskStartTime, nameof(DataHistoryWorkOrderRow.StartTime), DateTimeDisplayFormat);
        ConfigureColumn(colTaskEndTime, nameof(DataHistoryWorkOrderRow.EndTime), DateTimeDisplayFormat);
        ConfigureColumn(colTaskStatus, nameof(DataHistoryWorkOrderRow.TaskStatus));
        ConfigureColumn(colTaskUploadStatus, nameof(DataHistoryWorkOrderRow.UploadStatus));
        if (dgvWorkOrders.Columns.Count == 0)
        {
            dgvWorkOrders.Columns.AddRange(new DataGridViewColumn[]
            {
                colTaskStation,
                colTaskWorkOrder,
                colTaskProductNum,
                colTaskBatch,
                colTaskProductName,
                colTaskProcess,
                colTaskRecipe,
                colTaskPlannedQty,
                colTaskActualQty,
                colTaskQualifiedQty,
                colTaskFailedQty,
                colTaskOperator,
                colTaskStartTime,
                colTaskEndTime,
                colTaskStatus,
                colTaskUploadStatus
            });
        }

        ConfigureColumn(colParameterStation, nameof(DataHistoryWeldParameterRow.StationNo));
        ConfigureColumn(colParameterProductNo, nameof(DataHistoryWeldParameterRow.ProductNo));
        ConfigureColumn(colParameterTouchNo, nameof(DataHistoryWeldParameterRow.TouchNo));
        ConfigureColumn(colParameterResult, nameof(DataHistoryWeldParameterRow.TestResult));
        ConfigureColumn(colParameterRecordTime, nameof(DataHistoryWeldParameterRow.RecordTime), DateTimeDisplayFormat);
        if (dgvWeldParameters.Columns.Count == 0)
        {
            dgvWeldParameters.Columns.AddRange(new DataGridViewColumn[]
            {
                colParameterStation,
                colParameterProductNo,
                colParameterTouchNo,
                colParameterResult,
                colParameterRecordTime
            });
        }

        ConfigureColumn(colCollectionSequence, nameof(DataHistoryCollectionRow.SequenceNo));
        ConfigureColumn(colCollectionStation, nameof(DataHistoryCollectionRow.StationNo));
        ConfigureColumn(colCollectionProductNo, nameof(DataHistoryCollectionRow.ProductNo));
        ConfigureColumn(colCollectionTouchNo, nameof(DataHistoryCollectionRow.TouchNo));
        ConfigureColumn(colCollectionResult, nameof(DataHistoryCollectionRow.TestResult));
        ConfigureColumn(colCollectionIsTest, nameof(DataHistoryCollectionRow.IsTest));
        ConfigureColumn(colCollectionCompleted, nameof(DataHistoryCollectionRow.ProductCompleted));
        ConfigureColumn(colCollectionUploadStatus, nameof(DataHistoryCollectionRow.UploadStatus));
        ConfigureColumn(colCollectionOperator, nameof(DataHistoryCollectionRow.OperatorNo));
        ConfigureColumn(colCollectionRecordTime, nameof(DataHistoryCollectionRow.RecordTime), DateTimeDisplayFormat);
        if (dgvCollectionRecords.Columns.Count == 0)
        {
            dgvCollectionRecords.Columns.AddRange(new DataGridViewColumn[]
            {
                colCollectionSequence,
                colCollectionStation,
                colCollectionProductNo,
                colCollectionTouchNo,
                colCollectionResult,
                colCollectionIsTest,
                colCollectionCompleted,
                colCollectionUploadStatus,
                colCollectionOperator,
                colCollectionRecordTime
            });
        }

        ConfigureColumn(colReportFileName, nameof(DataHistoryReportFileRow.FileName));
        ConfigureColumn(colReportFormat, nameof(DataHistoryReportFileRow.FileFormat));
        ConfigureColumn(colReportPath, nameof(DataHistoryReportFileRow.FilePath));
        ConfigureColumn(colReportUploadStatus, nameof(DataHistoryReportFileRow.UploadStatus));
        ConfigureColumn(colReportCreatedTime, nameof(DataHistoryReportFileRow.CreatedTime), DateTimeDisplayFormat);
        ConfigureColumn(colReportUpdatedTime, nameof(DataHistoryReportFileRow.UpdatedTime), DateTimeDisplayFormat);
        if (dgvReportFiles.Columns.Count == 0)
        {
            dgvReportFiles.Columns.AddRange(new DataGridViewColumn[]
            {
                colReportFileName,
                colReportFormat,
                colReportPath,
                colReportUploadStatus,
                colReportCreatedTime,
                colReportUpdatedTime
            });
        }
    }

    /// <summary>
    /// Binds one static grid column to one DTO property.
    /// </summary>
    private static void ConfigureColumn(
        DataGridViewColumn column,
        string propertyName,
        string? displayFormat = null)
    {
        column.DataPropertyName = propertyName;
        column.ReadOnly = true;
        if (!string.IsNullOrWhiteSpace(displayFormat))
        {
            column.DefaultCellStyle.Format = displayFormat;
        }
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
        dgvWorkOrders.SelectionChanged += WorkOrders_SelectionChanged;
        dgvWorkOrders.CellFormatting += Status_CellFormatting;
        dgvReportFiles.CellFormatting += Status_CellFormatting;
        dgvReportFiles.SelectionChanged += ReportFiles_SelectionChanged;
        dgvReportFiles.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                OpenSelectedReport();
            }
        };
        btnOpenReport.Click += (_, _) => OpenSelectedReport();
        btnOpenReportFolder.Click += (_, _) => OpenSelectedReportFolder();
        selectProductResult.SelectedIndexChanged += ProductResultFilter_SelectedIndexChanged;
        tableTestData.SortModeChanged += TestData_SortModeChanged;
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

    private async void WorkOrders_SelectionChanged(object? sender, EventArgs e)
    {
        if (_disposing || IsDisposed || Disposing || _suppressWorkOrderSelection)
        {
            return;
        }

        var row = GetSelectedWorkOrder();
        if (row is null)
        {
            return;
        }

        await LoadTaskDetailsAsync(row.TaskId);
    }

    /// <summary>
    /// Safely reads the selected work-order row from the BindingSource.
    /// During Dispose, DataGridView can still raise SelectionChanged while its row
    /// collection is being cleared, so event handlers must not read CurrentRow.
    /// </summary>
    private DataHistoryWorkOrderRow? GetSelectedWorkOrder()
    {
        if (_disposing || workOrderBindingSource.Count <= 0)
        {
            return null;
        }

        var position = workOrderBindingSource.Position;
        if (position < 0 || position >= workOrderBindingSource.Count)
        {
            return null;
        }

        return workOrderBindingSource.Current as DataHistoryWorkOrderRow;
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
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

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
        SetDetailBusy(true);
        try
        {
            var testDataTask = _historyQueryService.QueryTestDataAsync(taskId, cancellationToken);
            var reportTask = _historyQueryService.QueryReportFilesAsync(taskId, cancellationToken);
            await Task.WhenAll(testDataTask, reportTask);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            BindTestData(await testDataTask);
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

    private void BindTestData(DataHistoryTestDataResult result)
    {
        _testDataDynamicColumns = result.DynamicColumns;
        _testDataRows = result.Rows;
        _testDataSortColumnKey = null;
        _testDataSortDescending = false;
        ResetProductResultFilter();
        ConfigureTestDataColumns(_testDataDynamicColumns);
        ApplyTestDataView();
    }

    private void ProductResultFilter_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_suppressProductResultFilter)
        {
            return;
        }

        _productResultFilter = e.Value switch
        {
            1 => ProductionConstants.TestResults.Ok,
            2 => ProductionConstants.TestResults.Ng,
            _ => DataHistoryTestDataRules.AllResults
        };
        ApplyTestDataView();
    }

    private void ApplyTestDataView()
    {
        _visibleTestDataRows = DataHistoryTestDataRules.Apply(
            _testDataRows,
            _productResultFilter,
            _testDataSortColumnKey,
            _testDataSortDescending);
        tableTestData.DataSource = null;
        tableTestData.DataSource = _visibleTestDataRows.ToList();
        lblParameterSummary.Text = _localizer.GetString(
            TextKeys.DataManage.ParameterSummary,
            _visibleTestDataRows.Count,
            CountVisibleTestRecords(),
            _testDataDynamicColumns.Count);
    }

    private bool TestData_SortModeChanged(object sender, AntdUI.TableSortModeEventArgs e)
    {
        if (!_testDataDynamicColumns.Any(column => string.Equals(column.Key, e.Column.Key, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        _testDataSortColumnKey = e.SortMode == AntdUI.SortMode.NONE ? null : e.Column.Key;
        _testDataSortDescending = e.SortMode == AntdUI.SortMode.DESC;
        ApplyTestDataView();
        return true;
    }

    private int CountVisibleTestRecords()
        => _visibleTestDataRows.Sum(row => row.Children.Count > 0 ? row.Children.Count : row.RecordId > 0 ? 1 : 0);

    private void ResetProductResultFilter()
    {
        _productResultFilter = DataHistoryTestDataRules.AllResults;
        _suppressProductResultFilter = true;
        try
        {
            selectProductResult.SelectedIndex = 0;
        }
        finally
        {
            _suppressProductResultFilter = false;
        }
    }

    private void BindReportFiles(IReadOnlyList<DataHistoryReportFileRow> rows)
    {
        reportBindingSource.DataSource = rows.ToList();
        lblReportSummary.Text = _localizer.GetString(TextKeys.DataManage.ReportSummary, rows.Count);
        UpdateReportButtons();
    }

    private void Status_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.Value is not string status)
        {
            return;
        }

        var columnName = ((DataGridView)sender!).Columns[e.ColumnIndex].DataPropertyName;
        e.Value = string.Equals(columnName, nameof(DataHistoryWorkOrderRow.TaskStatus), StringComparison.Ordinal)
            ? TaskStatusDisplayRules.GetDisplayText(status)
            : UploadStatusDisplayRules.GetDisplayText(status);
        e.FormattingApplied = true;
    }

    /// <summary>
    /// Handles report-file selection changes. WinForms also raises this event while
    /// disposing the grid, so shutdown paths must not query CurrentRow here.
    /// </summary>
    private void ReportFiles_SelectionChanged(object? sender, EventArgs e)
    {
        if (_disposing || IsDisposed || Disposing)
        {
            return;
        }

        UpdateReportButtons();
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
        if (_disposing || IsDisposed || Disposing)
        {
            return;
        }

        var report = GetSelectedReport();
        btnOpenReport.Enabled = report is not null && File.Exists(report.FilePath);
        var directory = report is null ? null : Path.GetDirectoryName(report.FilePath);
        btnOpenReportFolder.Enabled = !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory);
    }

    private DataHistoryReportFileRow? GetSelectedReport()
    {
        if (_disposing || reportBindingSource.Count <= 0)
        {
            return null;
        }

        var position = reportBindingSource.Position;
        if (position < 0 || position >= reportBindingSource.Count)
        {
            return null;
        }

        return reportBindingSource.Current as DataHistoryReportFileRow;
    }

    /// <summary>
    /// Marks the view as disposing before the component container clears DataGridView
    /// bindings. This prevents shutdown selection events from reading stale rows.
    /// </summary>
    private void BeginDispose()
    {
        if (_disposing)
        {
            return;
        }

        _disposing = true;
        CancelAndDispose(ref _workOrderQueryCancellation);
        CancelAndDispose(ref _detailQueryCancellation);
        dgvWorkOrders.SelectionChanged -= WorkOrders_SelectionChanged;
        dgvReportFiles.SelectionChanged -= ReportFiles_SelectionChanged;
    }

    private void ClearTaskDetails()
    {
        CancelAndDispose(ref _detailQueryCancellation);
        _selectedTaskId = 0;
        RemoveDynamicParameterColumns();
        parameterBindingSource.DataSource = Array.Empty<DataHistoryWeldParameterRow>();
        _testDataDynamicColumns = [];
        _testDataRows = [];
        _visibleTestDataRows = [];
        ResetProductResultFilter();
        ConfigureTestDataColumns(_testDataDynamicColumns);
        tableTestData.DataSource = Array.Empty<DataHistoryTestDataRow>();
        collectionBindingSource.DataSource = Array.Empty<DataHistoryCollectionRow>();
        reportBindingSource.DataSource = Array.Empty<DataHistoryReportFileRow>();
        lblParameterSummary.Text = _localizer.GetString(TextKeys.DataManage.SelectWorkOrder);
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

    private void ApplyDefaultSplitterLayout()
    {
        var mainHeight = mainSplitter.ClientSize.Height - mainSplitter.SplitterWidth;
        if (mainHeight > mainSplitter.Panel1MinSize + mainSplitter.Panel2MinSize)
        {
            mainSplitter.SplitterDistance = Math.Clamp(
                (int)Math.Round(mainHeight * 0.25),
                mainSplitter.Panel1MinSize,
                mainHeight - mainSplitter.Panel2MinSize);
        }
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
        lblProductResultFilter.Text = _localizer.GetString(TextKeys.DataManage.ProductResultFilter);
        BindProductResultFilterOptions();
        ConfigureTestDataColumns(_testDataDynamicColumns);
        tabReportFiles.Text = _localizer.GetString(TextKeys.DataManage.TabReportFiles);
        btnOpenReport.Text = _localizer.GetString(TextKeys.DataManage.OpenReport);
        btnOpenReportFolder.Text = _localizer.GetString(TextKeys.DataManage.OpenReportFolder);
        ApplyColumnHeaders();
    }


    private void BindProductResultFilterOptions()
    {
        var selectedIndex = selectProductResult.SelectedIndex < 0 ? 0 : selectProductResult.SelectedIndex;
        _suppressProductResultFilter = true;
        try
        {
            selectProductResult.Items.Clear();
            selectProductResult.Items.AddRange(
            [
                _localizer.GetString(TextKeys.DataManage.ProductResultAll),
                _localizer.GetString(TextKeys.DataManage.ProductResultOk),
                _localizer.GetString(TextKeys.DataManage.ProductResultNg)
            ]);
            selectProductResult.SelectedIndex = Math.Min(selectedIndex, 2);
        }
        finally
        {
            _suppressProductResultFilter = false;
        }
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

        var profile = ProcessParameterDeviceUiProfile.Resolve(GetProcessParameterDeviceType());
        colParameterStation.HeaderText = T(TextKeys.DataManage.ColumnStation);
        colParameterProductNo.HeaderText = T(TextKeys.DataManage.ColumnProductNo);
        colParameterTouchNo.HeaderText = profile.PointNoHeader;
        colParameterResult.HeaderText = profile.PointResultHeader;
        colParameterRecordTime.HeaderText = T(TextKeys.DataManage.ColumnRecordTime);

        colCollectionSequence.HeaderText = T(TextKeys.DataManage.ColumnSequence);
        colCollectionStation.HeaderText = T(TextKeys.DataManage.ColumnStation);
        colCollectionProductNo.HeaderText = T(TextKeys.DataManage.ColumnProductNo);
        colCollectionTouchNo.HeaderText = profile.PointNoHeader;
        colCollectionResult.HeaderText = profile.PointResultHeader;
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

    private string GetProcessParameterDeviceType()
        => _appSettingsService.Get().ProcessParameterDeviceType;

    private string T(string key)
        => _localizer.GetString(key);

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
