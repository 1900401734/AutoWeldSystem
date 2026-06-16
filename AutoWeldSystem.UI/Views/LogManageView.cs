using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.ViewModels;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Views;

/// <summary>
/// 日志管理页面。
/// 当前先接入 MES 交互日志，其它日志分类只保留入口，后续可以用同样模式扩展。
/// </summary>
public partial class LogManageView : BaseView
{
    private const int MaxDisplayCount = 1000;
    private const string ColumnResultName = "colResult";
    private const string ColumnProductionLevelName = "colProductionLevel";
    private const string ColumnExceptionCategoryName = "colExceptionCategory";
    private const string ColumnExceptionSeverityName = "colExceptionSeverity";

    private static readonly JsonSerializerOptions PrettyJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IMesInteractionLogService _mesLogService = null!;
    private readonly IProductionFlowLogService _productionLogService = null!;
    private readonly IProgramExceptionLogService _exceptionLogService = null!;
    private readonly IDeviceStatusService _deviceStatusService = null!;
    private readonly ILocalizationService _localizer = null!;
    private readonly BindingSource _mesBindingSource = new();
    private readonly BindingSource _productionBindingSource = new();
    private readonly BindingSource _exceptionBindingSource = new();
    private readonly BindingSource _deviceStatusBindingSource = new();
    private readonly List<MesInteractionLogEntry> _mesLogs = new();
    private readonly List<ProductionFlowLogEntry> _productionLogs = new();
    private readonly List<ProgramExceptionLogEntry> _exceptionLogs = new();
    private readonly List<BizDeviceStatusLog> _deviceStatusLogs = new();
    private bool _initialized;
    private string _keyword = string.Empty;
    private string _productionKeyword = string.Empty;
    private string _exceptionKeyword = string.Empty;
    private string _deviceStatusKeyword = string.Empty;

    /// <summary>
    /// Parameterless constructor used only by the WinForms designer.
    /// Runtime instances are created by dependency injection through the service constructor.
    /// </summary>
    public LogManageView()
    {
        InitializeComponent();
    }

    public LogManageView(
        IMesInteractionLogService mesLogService,
        IProductionFlowLogService productionLogService,
        IProgramExceptionLogService exceptionLogService,
        IDeviceStatusService deviceStatusService,
        ILocalizationService localizer)
    {
        _mesLogService = mesLogService;
        _productionLogService = productionLogService;
        _exceptionLogService = exceptionLogService;
        _deviceStatusService = deviceStatusService;
        _localizer = localizer;

        InitializeComponent();
        ConfigureMesGrid();
        ConfigureProductionGrid();
        ConfigureExceptionGrid();
        ConfigureDeviceStatusGrid();
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
        dtpMesDate.Value = DateTime.Today;
        dtpProductionDate.Value = DateTime.Today;
        dtpExceptionDate.Value = DateTime.Today;
        dtpDeviceStatusDate.Value = DateTime.Today;
        LoadMesLogs();
        LoadProductionLogs();
        LoadExceptionLogs();
        LoadDeviceStatusLogs();
    }

    protected override void OnLanguageChanged()
    {
        if (IsDesignEnvironment || _localizer is null)
        {
            return;
        }

        ApplyLocalizedTexts();
        ApplyMesGridHeaders();
        ApplyProductionGridHeaders();
        ApplyExceptionGridHeaders();
        ApplyDeviceStatusGridHeaders();
        ApplyMesFilter();
        ApplyProductionFilter();
        ApplyExceptionFilter();
        ApplyDeviceStatusFilter();
    }

    private bool IsDesignEnvironment
        => DesignMode || System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime;

    /// <summary>
    /// Applies the shared runtime style and binds the MES log data source.
    /// Columns are declared in the designer so they remain visible at design time.
    /// </summary>
    private void ConfigureMesGrid()
    {
        TableStyleHelper.ApplyDataGridView(dgvMesLogs);
        dgvMesLogs.AutoGenerateColumns = false;
        dgvMesLogs.DataSource = _mesBindingSource;
        ApplyMesGridHeaders();
    }

    /// <summary>
    /// Applies the shared runtime style and binds the production log data source.
    /// </summary>
    private void ConfigureProductionGrid()
    {
        TableStyleHelper.ApplyDataGridView(dgvProductionLogs);
        dgvProductionLogs.AutoGenerateColumns = false;
        dgvProductionLogs.DataSource = _productionBindingSource;
        ApplyProductionGridHeaders();
    }

    /// <summary>
    /// Applies the shared runtime style and binds the exception log data source.
    /// </summary>
    private void ConfigureExceptionGrid()
    {
        TableStyleHelper.ApplyDataGridView(dgvExceptionLogs);
        dgvExceptionLogs.AutoGenerateColumns = false;
        dgvExceptionLogs.DataSource = _exceptionBindingSource;
        ApplyExceptionGridHeaders();
    }

    /// <summary>
    /// Applies the shared runtime style and binds the device-status log data source.
    /// </summary>
    private void ConfigureDeviceStatusGrid()
    {
        TableStyleHelper.ApplyDataGridView(dgvDeviceStatusLogs);
        dgvDeviceStatusLogs.AutoGenerateColumns = false;
        dgvDeviceStatusLogs.DataSource = _deviceStatusBindingSource;
        ApplyDeviceStatusGridHeaders();
    }

    private void WireEvents()
    {
        btnRefreshMes.Click += (_, _) => LoadMesLogs();
        btnOpenMesFolder.Click += (_, _) => OpenMesLogFolder();
        dtpMesDate.ValueChanged += (_, _) => LoadMesLogs();
        txtMesKeyword.TextChanged += (_, _) =>
        {
            _keyword = txtMesKeyword.Text.Trim();
            ApplyMesFilter();
        };
        dgvMesLogs.SelectionChanged += (_, _) => ShowSelectedMesLogDetails();
        dgvMesLogs.CellFormatting += DgvMesLogs_CellFormatting;
        _mesLogService.LogWritten += MesLogService_LogWritten;

        btnRefreshProduction.Click += (_, _) => LoadProductionLogs();
        btnOpenProductionFolder.Click += (_, _) => OpenProductionLogFolder();
        dtpProductionDate.ValueChanged += (_, _) => LoadProductionLogs();
        txtProductionKeyword.TextChanged += (_, _) =>
        {
            _productionKeyword = txtProductionKeyword.Text.Trim();
            ApplyProductionFilter();
        };
        dgvProductionLogs.SelectionChanged += (_, _) => ShowSelectedProductionLogDetails();
        dgvProductionLogs.CellFormatting += DgvProductionLogs_CellFormatting;
        _productionLogService.LogWritten += ProductionLogService_LogWritten;
        Disposed += (_, _) => _productionLogService.LogWritten -= ProductionLogService_LogWritten;

        btnRefreshException.Click += (_, _) => LoadExceptionLogs();
        btnOpenExceptionFolder.Click += (_, _) => OpenExceptionLogFolder();
        btnOpenExceptionSource.Click += (_, _) => OpenSelectedExceptionSource();
        btnCopyExceptionDetails.Click += (_, _) => CopySelectedExceptionDetails();
        dtpExceptionDate.ValueChanged += (_, _) => LoadExceptionLogs();
        txtExceptionKeyword.TextChanged += (_, _) =>
        {
            _exceptionKeyword = txtExceptionKeyword.Text.Trim();
            ApplyExceptionFilter();
        };
        dgvExceptionLogs.SelectionChanged += (_, _) => ShowSelectedExceptionDetails();
        dgvExceptionLogs.CellFormatting += DgvExceptionLogs_CellFormatting;
        _exceptionLogService.LogWritten += ExceptionLogService_LogWritten;
        Disposed += (_, _) => _exceptionLogService.LogWritten -= ExceptionLogService_LogWritten;

        btnRefreshDeviceStatus.Click += (_, _) => LoadDeviceStatusLogs();
        dtpDeviceStatusDate.ValueChanged += (_, _) => LoadDeviceStatusLogs();
        txtDeviceStatusKeyword.TextChanged += (_, _) =>
        {
            _deviceStatusKeyword = txtDeviceStatusKeyword.Text.Trim();
            ApplyDeviceStatusFilter();
        };
        dgvDeviceStatusLogs.SelectionChanged += (_, _) => ShowSelectedDeviceStatusDetails();
        _deviceStatusService.StatusChanged += DeviceStatusService_StatusChanged;
        Disposed += (_, _) => _deviceStatusService.StatusChanged -= DeviceStatusService_StatusChanged;
    }

    private void ApplyLocalizedTexts()
    {
        tabMesLogs.Text = _localizer.GetString(TextKeys.Log.TitleMesInteraction);
        tabProductionLogs.Text = _localizer.GetString(TextKeys.Log.TabProductionFlow);
        tabExceptionLogs.Text = _localizer.GetString(TextKeys.Log.TabProgramException);
        tabDeviceStatusLogs.Text = "设备状态日志";
        lblMesTitle.Text = _localizer.GetString(TextKeys.Log.TitleMesInteraction);
        lblMesDescription.Text = _localizer.GetString(TextKeys.Log.DescriptionMesInteraction);
        lblProductionTitle.Text = _localizer.GetString(TextKeys.Log.TabProductionFlow);
        lblProductionDescription.Text = "记录PLC触发、数据采集、保存和反馈等采集流程关键步骤。";
        lblExceptionTitle.Text = _localizer.GetString(TextKeys.Log.TabProgramException);
        lblExceptionDescription.Text = _localizer.GetString(TextKeys.Log.DescriptionProgramException);
        lblDeviceStatusTitle.Text = "设备状态日志";
        lblDeviceStatusDescription.Text = "只记录设备状态变化，并显示对应MES上报状态。";
        lblMesDate.Text = _localizer.GetString(TextKeys.Log.LabelDate);
        lblProductionDate.Text = _localizer.GetString(TextKeys.Log.LabelDate);
        lblExceptionDate.Text = _localizer.GetString(TextKeys.Log.LabelDate);
        lblDeviceStatusDate.Text = _localizer.GetString(TextKeys.Log.LabelDate);
        lblMesKeyword.Text = _localizer.GetString(TextKeys.Log.LabelKeyword);
        lblProductionKeyword.Text = _localizer.GetString(TextKeys.Log.LabelKeyword);
        lblExceptionKeyword.Text = _localizer.GetString(TextKeys.Log.LabelKeyword);
        lblDeviceStatusKeyword.Text = _localizer.GetString(TextKeys.Log.LabelKeyword);
        btnRefreshMes.Text = _localizer.GetString(TextKeys.Log.ButtonRefresh);
        btnRefreshProduction.Text = _localizer.GetString(TextKeys.Log.ButtonRefresh);
        btnRefreshException.Text = _localizer.GetString(TextKeys.Log.ButtonRefresh);
        btnRefreshDeviceStatus.Text = _localizer.GetString(TextKeys.Log.ButtonRefresh);
        btnOpenMesFolder.Text = _localizer.GetString(TextKeys.Log.ButtonOpenFolder);
        btnOpenProductionFolder.Text = _localizer.GetString(TextKeys.Log.ButtonOpenFolder);
        btnOpenExceptionFolder.Text = _localizer.GetString(TextKeys.Log.ButtonOpenFolder);
        btnOpenExceptionSource.Text = _localizer.GetString(TextKeys.Log.ButtonOpenSource);
        btnCopyExceptionDetails.Text = _localizer.GetString(TextKeys.Log.ButtonCopyDetails);
        tabBasicInfo.Text = _localizer.GetString(TextKeys.Log.DetailBasicInfo);
        tabRequestBody.Text = _localizer.GetString(TextKeys.Log.DetailRequest);
        tabResponseBody.Text = _localizer.GetString(TextKeys.Log.DetailResponse);
        tabProductionBasicInfo.Text = _localizer.GetString(TextKeys.Log.DetailBasicInfo);
        tabProductionDetail.Text = _localizer.GetString(TextKeys.Log.DetailContext);
        tabExceptionBasicInfo.Text = _localizer.GetString(TextKeys.Log.DetailBasicInfo);
        tabExceptionStackTrace.Text = _localizer.GetString(TextKeys.Log.DetailStackTrace);
        tabExceptionContext.Text = _localizer.GetString(TextKeys.Log.DetailContext);
        if (dgvMesLogs.CurrentRow?.DataBoundItem is null)
        {
            ShowMesLogDetails(null);
        }

        if (dgvProductionLogs.CurrentRow?.DataBoundItem is null)
        {
            ShowProductionLogDetails(null);
        }

        if (dgvExceptionLogs.CurrentRow?.DataBoundItem is null)
        {
            ShowExceptionDetails(null);
        }

        if (dgvDeviceStatusLogs.CurrentRow?.DataBoundItem is null)
        {
            ShowDeviceStatusDetails(null);
        }
    }

    private void ApplyMesGridHeaders()
    {
        colMesSendTime.HeaderText = _localizer.GetString(TextKeys.Log.ColumnSendTime);
        colMesPurpose.HeaderText = _localizer.GetString(TextKeys.Log.ColumnPurpose);
        colMesMethod.HeaderText = _localizer.GetString(TextKeys.Log.ColumnMethod);
        colMesHttpStatus.HeaderText = _localizer.GetString(TextKeys.Log.ColumnHttpStatus);
        colMesStatus.HeaderText = _localizer.GetString(TextKeys.Log.ColumnMesStatus);
        colResult.HeaderText = _localizer.GetString(TextKeys.Log.ColumnSuccess);
        colMesDuration.HeaderText = _localizer.GetString(TextKeys.Log.ColumnDuration);
    }

    private void ApplyProductionGridHeaders()
    {
        colProductionOccurredTime.HeaderText = "时间";
        colProductionLevel.HeaderText = "级别";
        colProductionStep.HeaderText = "步骤";
        colProductionSummary.HeaderText = "摘要";
        colProductionStation.HeaderText = "工位";
        colProductionWorkOrder.HeaderText = "工单号";
        colProductionProductNo.HeaderText = "产品编号";
        colProductionPlcSignal.HeaderText = "PLC信号";
    }

    private void ApplyExceptionGridHeaders()
    {
        colExceptionOccurredTime.HeaderText = _localizer.GetString(TextKeys.Log.ColumnOccurredTime);
        colExceptionCategory.HeaderText = _localizer.GetString(TextKeys.Log.ColumnCategory);
        colExceptionSeverity.HeaderText = _localizer.GetString(TextKeys.Log.ColumnSeverity);
        colExceptionType.HeaderText = _localizer.GetString(TextKeys.Log.ColumnExceptionType);
        colExceptionMessage.HeaderText = _localizer.GetString(TextKeys.Log.ColumnMessage);
        colExceptionSource.HeaderText = _localizer.GetString(TextKeys.Log.ColumnSource);
        colExceptionSourceLocation.HeaderText = _localizer.GetString(TextKeys.Log.ColumnSourceLine);
    }

    private void ApplyDeviceStatusGridHeaders()
    {
        colDeviceOccurredTime.HeaderText = "时间";
        colDeviceStation.HeaderText = "工位";
        colDeviceStatus.HeaderText = "状态码";
        colDeviceStatusName.HeaderText = "状态名称";
        colDeviceWorkOrder.HeaderText = "工单号";
        colDeviceSource.HeaderText = "来源";
        colDeviceReportStatus.HeaderText = "上传状态";
        colDeviceReportMessage.HeaderText = "上传消息";
    }

    private void LoadMesLogs()
    {
        try
        {
            _mesLogs.Clear();
            _mesLogs.AddRange(_mesLogService
                .GetByDate(dtpMesDate.Value.Date, MaxDisplayCount)
                .Where(ShouldShowMesLog));
            ApplyMesFilter();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void LoadProductionLogs()
    {
        try
        {
            _productionLogs.Clear();
            _productionLogs.AddRange(_productionLogService.GetByDate(dtpProductionDate.Value.Date, MaxDisplayCount));
            ApplyProductionFilter();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void LoadExceptionLogs()
    {
        try
        {
            _exceptionLogs.Clear();
            _exceptionLogs.AddRange(_exceptionLogService.GetByDate(dtpExceptionDate.Value.Date, MaxDisplayCount));
            ApplyExceptionFilter();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void LoadDeviceStatusLogs()
    {
        try
        {
            var date = dtpDeviceStatusDate.Value.Date;
            _deviceStatusLogs.Clear();
            _deviceStatusLogs.AddRange(_deviceStatusService.GetLogs(date, date.AddDays(1).AddTicks(-1), MaxDisplayCount));
            ApplyDeviceStatusFilter();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ApplyMesFilter()
    {
        var rows = _mesLogs
            .Where(entry => IsMesLogMatched(entry, _keyword))
            .Select(CreateMesLogRow)
            .ToList();

        _mesBindingSource.DataSource = rows;
        if (rows.Count == 0)
        {
            ShowMesLogDetails(null);
            return;
        }

        SelectFirstRowIfNeeded(dgvMesLogs);

        ShowSelectedMesLogDetails();
    }

    private void ApplyProductionFilter()
    {
        var rows = _productionLogs
            .Where(entry => IsProductionLogMatched(entry, _productionKeyword))
            .Select(entry => new ProductionLogRow(entry))
            .ToList();

        _productionBindingSource.DataSource = rows;
        if (rows.Count == 0)
        {
            ShowProductionLogDetails(null);
            return;
        }

        SelectFirstRowIfNeeded(dgvProductionLogs);

        ShowSelectedProductionLogDetails();
    }

    private void ApplyExceptionFilter()
    {
        var rows = _exceptionLogs
            .Where(entry => IsExceptionLogMatched(entry, _exceptionKeyword))
            .Select(CreateExceptionLogRow)
            .ToList();

        _exceptionBindingSource.DataSource = rows;
        if (rows.Count == 0)
        {
            ShowExceptionDetails(null);
            return;
        }

        SelectFirstRowIfNeeded(dgvExceptionLogs);

        ShowSelectedExceptionDetails();
    }

    private void ApplyDeviceStatusFilter()
    {
        var rows = _deviceStatusLogs
            .Where(entry => IsDeviceStatusLogMatched(entry, _deviceStatusKeyword))
            .Select(entry => new DeviceStatusLogRow(entry))
            .ToList();

        _deviceStatusBindingSource.DataSource = rows;
        if (rows.Count == 0)
        {
            ShowDeviceStatusDetails(null);
            return;
        }

        SelectFirstRowIfNeeded(dgvDeviceStatusLogs);

        ShowSelectedDeviceStatusDetails();
    }

    /// <summary>
    /// Selects the first visible data row only when both a row and a column exist.
    /// This protects the page from incomplete designer column definitions.
    /// </summary>
    private static void SelectFirstRowIfNeeded(DataGridView grid)
    {
        if (grid.CurrentRow is not null || grid.Rows.Count == 0 || grid.Columns.Count == 0)
        {
            return;
        }

        var firstRow = grid.Rows[0];
        firstRow.Selected = true;
        grid.CurrentCell = firstRow.Cells[0];
    }

    private MesLogRow CreateMesLogRow(MesInteractionLogEntry entry)
    {
        return new MesLogRow(
            entry,
            entry.IsSuccess
                ? _localizer.GetString(TextKeys.Log.ValueSuccess)
                : _localizer.GetString(TextKeys.Log.ValueFailed));
    }

    private ExceptionLogRow CreateExceptionLogRow(ProgramExceptionLogEntry entry)
    {
        return new ExceptionLogRow(entry, GetExceptionCategoryText(entry.Category));
    }

    private string GetExceptionCategoryText(string category)
    {
        return string.Equals(category, AppConstants.ExceptionLogCategories.Business, StringComparison.OrdinalIgnoreCase)
            ? _localizer.GetString(TextKeys.Log.ValueBusinessException)
            : _localizer.GetString(TextKeys.Log.ValueProgramException);
    }

    private static bool IsMesLogMatched(MesInteractionLogEntry entry, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return true;
        }

        return Contains(entry.Purpose, keyword)
            || Contains(entry.Method, keyword)
            || Contains(entry.Url, keyword)
            || Contains(entry.RequestBody, keyword)
            || Contains(entry.ResponseBody, keyword)
            || Contains(entry.MesStatus, keyword)
            || Contains(entry.MesMessage, keyword)
            || Contains(entry.ErrorMessage, keyword)
            || Contains(entry.TraceId, keyword);
    }

    private static bool IsProductionLogMatched(ProductionFlowLogEntry entry, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return true;
        }

        return Contains(entry.TraceId, keyword)
            || Contains(entry.Level, keyword)
            || Contains(entry.Step, keyword)
            || Contains(entry.Summary, keyword)
            || Contains(entry.Detail, keyword)
            || Contains(entry.WorkOrder, keyword)
            || Contains(entry.ProductNo, keyword)
            || Contains(entry.ProgramId, keyword)
            || Contains(entry.PlcSignal, keyword)
            || Contains(entry.PlcAddress, keyword);
    }

    private static bool IsExceptionLogMatched(ProgramExceptionLogEntry entry, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return true;
        }

        return Contains(entry.TraceId, keyword)
            || Contains(entry.Category, keyword)
            || Contains(entry.Severity, keyword)
            || Contains(entry.Source, keyword)
            || Contains(entry.ExceptionType, keyword)
            || Contains(entry.Message, keyword)
            || Contains(entry.SourceFilePath, keyword)
            || Contains(entry.SourceMemberName, keyword)
            || Contains(entry.TargetSite, keyword)
            || Contains(entry.Context, keyword)
            || Contains(entry.StackTrace, keyword)
            || Contains(entry.InnerException, keyword);
    }

    private static bool IsDeviceStatusLogMatched(BizDeviceStatusLog entry, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return true;
        }

        return entry.StationNo.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || Contains(entry.DeviceId, keyword)
            || Contains(entry.DeviceStatus, keyword)
            || Contains(entry.StatusName, keyword)
            || Contains(entry.Source, keyword)
            || Contains(entry.WorkOrderId, keyword)
            || Contains(entry.ReportStatus, keyword)
            || Contains(entry.ReportMessage, keyword)
            || Contains(entry.Remark, keyword);
    }

    private static bool ShouldShowMesLog(MesInteractionLogEntry entry)
    {
        return !string.Equals(
            entry.Purpose,
            AppConstants.MesLogPurposes.GetServerTime,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contains(string? source, string keyword)
    {
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private void MesLogService_LogWritten(object? sender, MesInteractionLogEntry entry)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AddLiveMesLog(entry)));
            return;
        }

        AddLiveMesLog(entry);
    }

    private void ProductionLogService_LogWritten(object? sender, ProductionFlowLogEntry entry)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AddLiveProductionLog(entry)));
            return;
        }

        AddLiveProductionLog(entry);
    }

    private void ExceptionLogService_LogWritten(object? sender, ProgramExceptionLogEntry entry)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AddLiveExceptionLog(entry)));
            return;
        }

        AddLiveExceptionLog(entry);
    }

    private void DeviceStatusService_StatusChanged(object? sender, BizDeviceStatusLog entry)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AddLiveDeviceStatusLog(entry)));
            return;
        }

        AddLiveDeviceStatusLog(entry);
    }

    private void AddLiveMesLog(MesInteractionLogEntry entry)
    {
        if (entry.SendTime.Date != dtpMesDate.Value.Date)
        {
            return;
        }

        if (!ShouldShowMesLog(entry))
        {
            return;
        }

        _mesLogs.Insert(0, entry);
        if (_mesLogs.Count > MaxDisplayCount)
        {
            _mesLogs.RemoveRange(MaxDisplayCount, _mesLogs.Count - MaxDisplayCount);
        }

        ApplyMesFilter();
    }

    private void AddLiveProductionLog(ProductionFlowLogEntry entry)
    {
        if (entry.OccurredTime.Date != dtpProductionDate.Value.Date)
        {
            return;
        }

        _productionLogs.Insert(0, entry);
        if (_productionLogs.Count > MaxDisplayCount)
        {
            _productionLogs.RemoveRange(MaxDisplayCount, _productionLogs.Count - MaxDisplayCount);
        }

        ApplyProductionFilter();
    }

    private void AddLiveExceptionLog(ProgramExceptionLogEntry entry)
    {
        if (entry.OccurredTime.Date != dtpExceptionDate.Value.Date)
        {
            return;
        }

        _exceptionLogs.Insert(0, entry);
        if (_exceptionLogs.Count > MaxDisplayCount)
        {
            _exceptionLogs.RemoveRange(MaxDisplayCount, _exceptionLogs.Count - MaxDisplayCount);
        }

        ApplyExceptionFilter();
    }

    private void AddLiveDeviceStatusLog(BizDeviceStatusLog entry)
    {
        if (entry.OccurredTime.Date != dtpDeviceStatusDate.Value.Date)
        {
            return;
        }

        _deviceStatusLogs.Insert(0, entry);
        if (_deviceStatusLogs.Count > MaxDisplayCount)
        {
            _deviceStatusLogs.RemoveRange(MaxDisplayCount, _deviceStatusLogs.Count - MaxDisplayCount);
        }

        ApplyDeviceStatusFilter();
    }

    private void ShowSelectedMesLogDetails()
    {
        var row = dgvMesLogs.CurrentRow?.DataBoundItem as MesLogRow;
        ShowMesLogDetails(row?.Entry);
    }

    private void ShowSelectedProductionLogDetails()
    {
        var row = dgvProductionLogs.CurrentRow?.DataBoundItem as ProductionLogRow;
        ShowProductionLogDetails(row?.Entry);
    }

    private void ShowSelectedExceptionDetails()
    {
        ShowExceptionDetails(GetSelectedExceptionEntry());
    }

    private void ShowSelectedDeviceStatusDetails()
    {
        var row = dgvDeviceStatusLogs.CurrentRow?.DataBoundItem as DeviceStatusLogRow;
        ShowDeviceStatusDetails(row?.Entry);
    }

    private void ShowMesLogDetails(MesInteractionLogEntry? entry)
    {
        if (entry is null)
        {
            txtBasicInfo.Text = _localizer.GetString(TextKeys.Log.DetailNoSelection);
            txtRequestBody.Clear();
            txtResponseBody.Clear();
            return;
        }

        txtBasicInfo.Text = BuildBasicInfo(entry);
        txtRequestBody.Text = PrettyPrintJson(entry.RequestBody);
        txtResponseBody.Text = PrettyPrintJson(entry.ResponseBody);
    }

    private void ShowProductionLogDetails(ProductionFlowLogEntry? entry)
    {
        if (entry is null)
        {
            txtProductionBasicInfo.Text = _localizer.GetString(TextKeys.Log.DetailNoSelection);
            txtProductionDetail.Clear();
            return;
        }

        txtProductionBasicInfo.Text = BuildProductionBasicInfo(entry);
        txtProductionDetail.Text = FormatProductionDetail(entry.Detail);
    }

    /// <summary>
    /// Formats the production detail string for better readability.
    /// Converts semicolon-separated key-value pairs into newline-separated format.
    /// </summary>
    private string FormatProductionDetail(string detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
        {
            return string.Empty;
        }

        // Replace semicolon separators with newlines for better readability
        return detail.Replace("; ", Environment.NewLine);
    }

    private void ShowExceptionDetails(ProgramExceptionLogEntry? entry)
    {
        var hasSourceFile = entry is not null
            && !string.IsNullOrWhiteSpace(entry.SourceFilePath)
            && File.Exists(entry.SourceFilePath);

        btnOpenExceptionSource.Enabled = hasSourceFile;
        btnCopyExceptionDetails.Enabled = entry is not null;

        if (entry is null)
        {
            txtExceptionBasicInfo.Text = _localizer.GetString(TextKeys.Log.DetailNoExceptionSelection);
            txtExceptionStackTrace.Clear();
            txtExceptionContext.Clear();
            return;
        }

        txtExceptionBasicInfo.Text = BuildExceptionBasicInfo(entry);
        txtExceptionStackTrace.Text = entry.StackTrace;
        txtExceptionContext.Text = BuildExceptionContext(entry);
    }

    private void ShowDeviceStatusDetails(BizDeviceStatusLog? entry)
    {
        txtDeviceStatusDetail.Text = entry is null
            ? _localizer.GetString(TextKeys.Log.DetailNoSelection)
            : BuildDeviceStatusBasicInfo(entry);
    }

    private ProgramExceptionLogEntry? GetSelectedExceptionEntry()
    {
        return (dgvExceptionLogs.CurrentRow?.DataBoundItem as ExceptionLogRow)?.Entry;
    }

    private static string BuildBasicInfo(MesInteractionLogEntry entry)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"TraceId: {entry.TraceId}");
        builder.AppendLine($"Purpose: {entry.Purpose}");
        builder.AppendLine($"Method: {entry.Method}");
        builder.AppendLine($"Url: {entry.Url}");
        builder.AppendLine($"SendTime: {entry.SendTime:yyyy-MM-dd HH:mm:ss.fff}");
        builder.AppendLine($"ReceiveTime: {entry.ReceiveTime:yyyy-MM-dd HH:mm:ss.fff}");
        builder.AppendLine($"Duration: {entry.DurationMilliseconds} ms");
        builder.AppendLine($"HTTP: {entry.HttpStatusCode?.ToString() ?? "-"}");
        builder.AppendLine($"MES Status: {entry.MesStatus}");
        builder.AppendLine($"MES Message: {entry.MesMessage}");
        builder.AppendLine($"Success: {entry.IsSuccess}");

        if (!string.IsNullOrWhiteSpace(entry.ErrorMessage))
        {
            builder.AppendLine($"Error: {entry.ErrorMessage}");
        }

        return builder.ToString();
    }

    private static string BuildProductionBasicInfo(ProductionFlowLogEntry entry)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"TraceId: {entry.TraceId}");
        builder.AppendLine($"Time: {entry.OccurredTime:yyyy-MM-dd HH:mm:ss.fff}");
        builder.AppendLine($"Level: {entry.Level}");
        builder.AppendLine($"Step: {entry.Step}");
        builder.AppendLine($"Summary: {entry.Summary}");
        builder.AppendLine($"Station: {entry.StationNo}");
        builder.AppendLine($"WorkOrder: {entry.WorkOrder}");
        builder.AppendLine($"ProductNo: {entry.ProductNo}");
        builder.AppendLine($"ProgramId: {entry.ProgramId}");
        builder.AppendLine($"PLC Signal: {entry.PlcSignal}");
        builder.AppendLine($"PLC Address: {entry.PlcAddress}");
        builder.AppendLine($"Duration: {entry.DurationMilliseconds?.ToString() ?? "-"} ms");
        return builder.ToString();
    }

    private static string BuildExceptionBasicInfo(ProgramExceptionLogEntry entry)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"TraceId: {entry.TraceId}");
        builder.AppendLine($"Category: {entry.Category}");
        builder.AppendLine($"Severity: {entry.Severity}");
        builder.AppendLine($"Source: {entry.Source}");
        builder.AppendLine($"ExceptionType: {entry.ExceptionType}");
        builder.AppendLine($"Message: {entry.Message}");
        builder.AppendLine($"OccurredTime: {entry.OccurredTime:yyyy-MM-dd HH:mm:ss.fff}");
        builder.AppendLine($"SourceFile: {GetSourceLocation(entry)}");
        builder.AppendLine($"SourceMember: {entry.SourceMemberName}");
        builder.AppendLine($"TargetSite: {entry.TargetSite}");
        builder.AppendLine($"Thread: {entry.ThreadId} {entry.ThreadName}".TrimEnd());
        builder.AppendLine($"User: {entry.MachineName}\\{entry.UserName}");
        builder.AppendLine($"AppVersion: {entry.ApplicationVersion}");
        return builder.ToString();
    }

    private static string BuildDeviceStatusBasicInfo(BizDeviceStatusLog entry)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Id: {entry.Id}");
        builder.AppendLine($"DeviceId: {entry.DeviceId}");
        builder.AppendLine($"Station: {entry.StationNo}");
        builder.AppendLine($"TaskId: {entry.WeldTaskId?.ToString() ?? "-"}");
        builder.AppendLine($"WorkOrder: {entry.WorkOrderId ?? "-"}");
        builder.AppendLine($"DeviceStatus: {entry.DeviceStatus}");
        builder.AppendLine($"StatusName: {entry.StatusName}");
        builder.AppendLine($"Source: {entry.Source}");
        builder.AppendLine($"OccurredTime: {entry.OccurredTime:yyyy-MM-dd HH:mm:ss.fff}");
        builder.AppendLine($"ReportStatus: {entry.ReportStatus}");
        builder.AppendLine($"ReportTime: {entry.ReportTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-"}");
        builder.AppendLine($"ReportMessage: {entry.ReportMessage ?? "-"}");
        builder.AppendLine($"Remark: {entry.Remark ?? "-"}");
        return builder.ToString();
    }

    private static string BuildExceptionContext(ProgramExceptionLogEntry entry)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(entry.Context))
        {
            builder.AppendLine("Context:");
            builder.AppendLine(entry.Context);
            builder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(entry.InnerException))
        {
            builder.AppendLine("InnerException:");
            builder.AppendLine(entry.InnerException);
        }

        return builder.ToString();
    }

    private static string BuildExceptionFullDetails(ProgramExceptionLogEntry entry)
    {
        var builder = new StringBuilder();
        builder.AppendLine(BuildExceptionBasicInfo(entry));
        builder.AppendLine("StackTrace:");
        builder.AppendLine(entry.StackTrace);

        var context = BuildExceptionContext(entry);
        if (!string.IsNullOrWhiteSpace(context))
        {
            builder.AppendLine();
            builder.AppendLine(context);
        }

        return builder.ToString();
    }

    private static string GetSourceLocation(ProgramExceptionLogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.SourceFilePath))
        {
            return string.IsNullOrWhiteSpace(entry.SourceMemberName)
                ? "-"
                : entry.SourceMemberName;
        }

        return entry.SourceLineNumber > 0
            ? $"{entry.SourceFilePath}:{entry.SourceLineNumber}"
            : entry.SourceFilePath;
    }

    private static string PrettyPrintJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            return JsonSerializer.Serialize(document.RootElement, PrettyJsonOptions);
        }
        catch
        {
            return value;
        }
    }

    private void DgvMesLogs_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (!IsValidCell(dgvMesLogs, e)
            || dgvMesLogs.Rows[e.RowIndex].DataBoundItem is not MesLogRow row)
        {
            return;
        }

        if (e.CellStyle is not null && dgvMesLogs.Columns[e.ColumnIndex].Name == ColumnResultName)
        {
            e.CellStyle.ForeColor = row.Entry.IsSuccess ? Color.ForestGreen : Color.Firebrick;
            e.CellStyle.Font = new Font(dgvMesLogs.Font, FontStyle.Bold);
        }
    }

    private void DgvProductionLogs_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (!IsValidCell(dgvProductionLogs, e)
            || dgvProductionLogs.Rows[e.RowIndex].DataBoundItem is not ProductionLogRow row)
        {
            return;
        }

        if (e.CellStyle is null || dgvProductionLogs.Columns[e.ColumnIndex].Name != ColumnProductionLevelName)
        {
            return;
        }

        e.CellStyle.ForeColor = row.Entry.Level.Equals("Error", StringComparison.OrdinalIgnoreCase)
            ? UiColors.Status.Danger
            : UiColors.Status.Success;
        e.CellStyle.Font = new Font(dgvProductionLogs.Font, FontStyle.Bold);
    }

    private void DgvExceptionLogs_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (!IsValidCell(dgvExceptionLogs, e)
            || dgvExceptionLogs.Rows[e.RowIndex].DataBoundItem is not ExceptionLogRow row)
        {
            return;
        }

        if (e.CellStyle is null)
        {
            return;
        }

        if (dgvExceptionLogs.Columns[e.ColumnIndex].Name == ColumnExceptionCategoryName
            || dgvExceptionLogs.Columns[e.ColumnIndex].Name == ColumnExceptionSeverityName)
        {
            e.CellStyle.ForeColor = IsBusinessException(row.Entry)
                ? UiColors.Status.Business
                : UiColors.Status.Danger;
            e.CellStyle.Font = new Font(dgvExceptionLogs.Font, FontStyle.Bold);
        }
    }

    /// <summary>
    /// Validates row and column indexes supplied by DataGridView formatting events.
    /// </summary>
    private static bool IsValidCell(DataGridView grid, DataGridViewCellFormattingEventArgs e)
    {
        return e.RowIndex >= 0
            && e.RowIndex < grid.Rows.Count
            && e.ColumnIndex >= 0
            && e.ColumnIndex < grid.Columns.Count;
    }

    private static bool IsBusinessException(ProgramExceptionLogEntry entry)
    {
        return string.Equals(entry.Category, AppConstants.ExceptionLogCategories.Business, StringComparison.OrdinalIgnoreCase);
    }

    private void OpenMesLogFolder()
    {
        try
        {
            var folder = _mesLogService.GetLogDirectory();
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OpenProductionLogFolder()
    {
        try
        {
            var folder = _productionLogService.GetLogDirectory();
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OpenExceptionLogFolder()
    {
        try
        {
            var folder = _exceptionLogService.GetLogDirectory();
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OpenSelectedExceptionSource()
    {
        var entry = GetSelectedExceptionEntry();
        if (entry is null || string.IsNullOrWhiteSpace(entry.SourceFilePath) || !File.Exists(entry.SourceFilePath))
        {
            ShowWarning(_localizer.GetString(TextKeys.Log.MessageSourceMissing));
            return;
        }

        try
        {
            Clipboard.SetText(GetSourceLocation(entry));
            Process.Start(new ProcessStartInfo
            {
                FileName = entry.SourceFilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void CopySelectedExceptionDetails()
    {
        var entry = GetSelectedExceptionEntry();
        if (entry is null)
        {
            ShowWarning(_localizer.GetString(TextKeys.Log.DetailNoExceptionSelection));
            return;
        }

        try
        {
            Clipboard.SetText(BuildExceptionFullDetails(entry));
            ShowInfo(_localizer.GetString(TextKeys.Log.MessageDetailsCopied));
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ShowInfo(string message)
    {
        MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleInfo),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleWarning),
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private void ShowError(string message)
    {
        MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleError),
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private sealed class MesLogRow
    {
        public MesLogRow(MesInteractionLogEntry entry, string result)
        {
            Entry = entry;
            Result = result;
        }

        public MesInteractionLogEntry Entry { get; }

        public string SendTime => Entry.SendTime.ToString("yyyy/MM/dd HH:mm:ss.fff");

        public string Purpose => Entry.Purpose;

        public string Method => Entry.Method;

        public string HttpStatus => Entry.HttpStatusCode?.ToString() ?? "-";

        public string MesStatus => string.IsNullOrWhiteSpace(Entry.MesStatus) ? "-" : Entry.MesStatus;

        public string Result { get; }

        public string Duration => Entry.DurationMilliseconds.ToString();

    }

    private sealed class ProductionLogRow
    {
        public ProductionLogRow(ProductionFlowLogEntry entry)
        {
            Entry = entry;
        }

        public ProductionFlowLogEntry Entry { get; }

        public string OccurredTime => Entry.OccurredTime.ToString("HH:mm:ss.fff");

        public string Level => Entry.Level;

        public string Step => Entry.Step;

        public string Summary => Entry.Summary;

        public string Station => Entry.StationNo <= 0 ? "-" : Entry.StationNo.ToString();

        public string WorkOrderId => string.IsNullOrWhiteSpace(Entry.WorkOrder) ? "-" : Entry.WorkOrder;

        public string ProductNo => string.IsNullOrWhiteSpace(Entry.ProductNo) ? "-" : Entry.ProductNo;

        public string PlcSignal => string.IsNullOrWhiteSpace(Entry.PlcSignal) ? "-" : Entry.PlcSignal;
    }

    private sealed class DeviceStatusLogRow
    {
        public DeviceStatusLogRow(BizDeviceStatusLog entry)
        {
            Entry = entry;
        }

        public BizDeviceStatusLog Entry { get; }

        public string OccurredTime => Entry.OccurredTime.ToString("HH:mm:ss.fff");

        public string Station => Entry.StationNo <= 0 ? "-" : Entry.StationNo.ToString();

        public string DeviceStatus => Entry.DeviceStatus;

        public string StatusName => string.IsNullOrWhiteSpace(Entry.StatusName) ? "-" : Entry.StatusName;

        public string WorkOrderId => string.IsNullOrWhiteSpace(Entry.WorkOrderId) ? "-" : Entry.WorkOrderId;

        public string Source => string.IsNullOrWhiteSpace(Entry.Source) ? "-" : Entry.Source;

        public string ReportStatus => string.IsNullOrWhiteSpace(Entry.ReportStatus) ? "-" : Entry.ReportStatus;

        public string ReportMessage => string.IsNullOrWhiteSpace(Entry.ReportMessage) ? "-" : Entry.ReportMessage;
    }

    private sealed class ExceptionLogRow
    {
        public ExceptionLogRow(ProgramExceptionLogEntry entry, string category)
        {
            Entry = entry;
            Category = category;
        }

        public ProgramExceptionLogEntry Entry { get; }

        public string OccurredTime => Entry.OccurredTime.ToString("HH:mm:ss.fff");

        public string Category { get; }

        public string Severity => Entry.Severity;

        public string ExceptionType => GetShortTypeName(Entry.ExceptionType);

        public string Message => Entry.Message;

        public string Source => Entry.Source;

        public string SourceLocation
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Entry.SourceFilePath))
                {
                    var fileName = Path.GetFileName(Entry.SourceFilePath);
                    return Entry.SourceLineNumber > 0
                        ? $"{fileName}:{Entry.SourceLineNumber}"
                        : fileName;
                }

                return string.IsNullOrWhiteSpace(Entry.SourceMemberName)
                    ? "-"
                    : Entry.SourceMemberName;
            }
        }

        private static string GetShortTypeName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return "-";
            }

            var lastDotIndex = typeName.LastIndexOf('.');
            return lastDotIndex >= 0 && lastDotIndex < typeName.Length - 1
                ? typeName[(lastDotIndex + 1)..]
                : typeName;
        }
    }
}
