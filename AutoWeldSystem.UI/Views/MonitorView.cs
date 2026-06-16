using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.Core.ViewModels;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Forms;
using AutoWeldSystem.UI.Infrastructure;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.UI.Components;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Runtime;

namespace AutoWeldSystem.UI.Views;

public partial class MonitorView : BaseView
{
    private const int TitleTextPadding = 8;
    private const int HeaderLogoWidth = 168;
    private const int HeaderActionMinWidth = 156;
    private const int HeaderStatusCellMinWidth = 140;
    private const int HeaderStatusCellPadding = 36;
    private const int HeaderButtonPadding = 62;
    private const int RealtimePreviewPaintIntervalMs = 500;
    private const int WeldPreviewMouseWheelPixels = 96;
    private const int RuntimeSummaryMaxLength = 56;
    private const string RuntimeSummaryOverflowSuffix = "...";
    private static readonly TimeSpan RecipePreparationTimeout = TimeSpan.FromSeconds(5);
    private const int PlcStatusToolTipRefreshIntervalMs = 500;
    private const int PlcStatusToolTipHoverPollIntervalMs = 100;
    private const int PlcStatusToolTipMaxWidth = 520;
    private const int PlcStatusHistoryLimit = 10;
    private const int WmSetRedraw = 0x000B;
    private const string PreviewTouchNoColumn = "TouchNo";
    private const string PreviewTouchResultColumn = "TouchResult";
    private const string PreviewMessageColumn = "Message";
    private const string PreviewUpperRole = "Upper";
    private const string PreviewLowerRole = "Lower";
    private const string PreviewActualRole = "Actual";
    private const string PreviewResultRole = "Result";
    private const int StationSelectorRowIndex = 1;
    private const string VersionPrefix = "v";
    private static readonly object StationOperationSync = new();
    private static readonly HashSet<int> BusyOperationStations = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _realtimePreviewPaintTimer = new() { Interval = RealtimePreviewPaintIntervalMs };
    private readonly System.Windows.Forms.Timer _plcStatusToolTipTimer = new() { Interval = PlcStatusToolTipHoverPollIntervalMs };
    private readonly ILocalizationService _localizer;
    private readonly IAppSettingsService _settingsService;
    private AppSettings _currentSettings;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IMesConnectionMonitor _mesConnectionMonitorService;
    private readonly IPlcProductionMonitorService _plcProductionMonitorService;
    private readonly IPlcWorkIdMonitorService _plcWorkIdMonitorService;
    private readonly IPlcWeldCycleMonitorService _plcWeldCycleMonitorService;
    private readonly IPlcAddressService _plcAddressService;
    private readonly IPlcBusinessSignalService _plcBusinessSignalService;
    private readonly IPlcExpressionReadService _plcExpressionReadService;
    private readonly IProductProcessConfigService _productProcessConfigService;
    private readonly ITestSchemeConfigService _testSchemeConfigService;
    private readonly IProductRealtimePreviewService _productRealtimePreviewService;
    private readonly IProductHistoryService _productHistoryService;
    private readonly IProgramManageService _programManageService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IUploadTaskService _uploadTaskService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly IProductionFlowLogService _productionLogService;
    private readonly IRuntimeTipStateService _runtimeTipStateService;
    private bool _syncingStationSelection;
    private bool _syncingProcessSelection;
    private bool _dualStationEnabled;
    private string? _runtimeStatusKey = TextKeys.Monitor.RuntimeStatus.Idle;
    private object[] _runtimeStatusArgs = Array.Empty<object>();
    private string? _runtimeStatusText;
    private bool _runtimeStatusTextIsSuccess;
    private string? _runtimeErrorKey;
    private object[] _runtimeErrorArgs = Array.Empty<object>();
    private string? _runtimeErrorText;
    private bool _adjustingTitleFont;
    private Font? _titleFont;
    private Font? _headerStatusFont;
    private Font? _headerButtonFont;
    private Font? _runtimeMessageFont;
    private Font? _runtimeGroupFont;
    private readonly List<WeldParameterRow> _weldParameterRows = new();
    private readonly object _realtimePreviewSync = new();
    private ProductRealtimePreviewSnapshot? _pendingRealtimePreviewSnapshot;
    private ProductIdentity? _currentProductIdentity;
    private DateTime _lastSchemePreviewRefreshTime = DateTime.MinValue;
    private string _lastSchemePreviewKey = string.Empty;
    private string _confirmedProgramFingerprint = string.Empty;
    private string _weldParameterLayoutKey = string.Empty;
    private string _weldParameterPreviewSchemaKey = string.Empty;
    private string _weldParameterVisibleValueKey = string.Empty;
    private readonly Dictionary<int, string> _productHistorySchemaKeys = new();
    private readonly int _uiThreadId = Environment.CurrentManagedThreadId;
    private bool _refreshingSchemePreview;
    private bool _refreshingProductHistoryPreview;
    private bool _productHistoryRefreshPending;
    private int _productHistoryRefreshPosted;
    private bool _weldParameterTableBound;
    private bool _realtimePreviewApplyPosted;
    private bool _syncingWeldPreviewHorizontalScroll;
    private bool _deviceModeReconcileRunning;
    private bool _workOrderStatusReconcileRunning;
    private bool _lastMesConnected;
    private bool _pendingUploadRetryRunning;
    private bool _plcStatusToolTipVisible;
    private DateTime _lastPlcStatusToolTipRefreshTime = DateTime.MinValue;
    private string _lastPlcStatusToolTipText = string.Empty;
    private Panel? _plcStatusToolTipPanel;
    private Label? _plcStatusToolTipLabel;
    private readonly Dictionary<int, PlcConnectionSnapshot> _lastPlcHistorySnapshots = new();
    private readonly List<PlcStatusHistoryEntry> _plcStatusHistory = new();
    private readonly Dictionary<int, int> _lastWorkOrderStatusSnapshots = new();
    private readonly Dictionary<int, int> _lastDeviceModeSnapshots = new();
    private readonly Dictionary<int, SemaphoreSlim> _workOrderStatusLocks = new();
    private readonly Dictionary<int, SemaphoreSlim> _deviceModeLocks = new();
    private readonly object _businessSignalLockSync = new();
    private int _viewStationNo = ProductionConstants.Stations.DefaultStationNo;
    private bool _stationViewReadOnly;
    private bool _enableBusinessSignalReconcile = true;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public MonitorView(
        ILocalizationService localizer,
        IAppSettingsService settingsService,
        IMesConnectionMonitor mesConnectionMonitorService,
        IPlcCommunicationService plcCommunicationService,
        IPlcProductionMonitorService plcProductionMonitorService,
        IPlcWorkIdMonitorService plcWorkIdMonitorService,
        IPlcWeldCycleMonitorService plcWeldCycleMonitorService,
        IPlcAddressService plcAddressService,
        IPlcBusinessSignalService plcBusinessSignalService,
        IPlcExpressionReadService plcExpressionReadService,
        IProductProcessConfigService productProcessConfigService,
        ITestSchemeConfigService testSchemeConfigService,
        IProductRealtimePreviewService productRealtimePreviewService,
        IProductHistoryService productHistoryService,
        IProgramManageService programManageService,
        IWeldTaskService weldTaskService,
        IUploadTaskService uploadTaskService,
        IProgramExceptionLogService exceptionLogService,
        IProductionFlowLogService productionLogService,
        IRuntimeTipStateService runtimeTipStateService)
    {
        InitializeComponent();

        _localizer = localizer;
        _settingsService = settingsService;
        _currentSettings = _settingsService.Get();
        _mesConnectionMonitorService = mesConnectionMonitorService;
        _plcCommunicationService = plcCommunicationService;
        _plcProductionMonitorService = plcProductionMonitorService;
        _plcWorkIdMonitorService = plcWorkIdMonitorService;
        _plcWeldCycleMonitorService = plcWeldCycleMonitorService;
        _plcAddressService = plcAddressService;
        _plcBusinessSignalService = plcBusinessSignalService;
        _plcExpressionReadService = plcExpressionReadService;
        _productProcessConfigService = productProcessConfigService;
        _testSchemeConfigService = testSchemeConfigService;
        _productRealtimePreviewService = productRealtimePreviewService;
        _productHistoryService = productHistoryService;
        _programManageService = programManageService;
        _weldTaskService = weldTaskService;
        _uploadTaskService = uploadTaskService;
        _exceptionLogService = exceptionLogService;
        _productionLogService = productionLogService;
        _runtimeTipStateService = runtimeTipStateService;

        LoadTitleLogo();
        ConfigureHeaderLayout();
        ConfigureRuntimeMessagePanels();
        ConfigureStationResultTags();
        ApplyLocalizedTexts();
        ConfigureStationSelector();
        ConfigureTables();
        ConfigureProductionTableColumns();
        ConfigureWeldParameterTableColumns();
        ConfigureProductHistoryTableColumns();
        WireEvents();
        BindProductionRuntimeState();
        RefreshRuntimePanels();
        ApplyAllStationStatuses();
        ApplyMesStatus(_mesConnectionMonitorService.Current);
        QueueRefreshSchemePreview(force: true);
        AdjustTitleFontSize();
    }

    /// <summary>
    /// Configures the station shown by this window. The station can still be changed
    /// by the window's own station tabs; it never follows another window.
    /// </summary>
    public void ConfigureStationView(int stationNo, bool readOnly, bool enableBusinessSignalReconcile = true)
    {
        _viewStationNo = NormalizePreviewStationNo(stationNo);
        _stationViewReadOnly = readOnly;
        _enableBusinessSignalReconcile = enableBusinessSignalReconcile;

        ConfigureStationSelector();
        ApplyStationViewMode();
        _weldTaskService.RestoreUnfinishedTask(CurrentStationNo);
        RefreshProductionRuntimeState();
        RestoreCurrentRuntimeTipState();
        RefreshRuntimePanels();
        ApplyAllStationStatuses();
        QueueRefreshSchemePreview(force: true);
        ApplyCurrentRealtimePreviewSnapshot();
    }

    public int ViewStationNo => CurrentStationNo;

    public void ApplyRuntimeSettingsChanged(
        AppSettings settings,
        bool readOnly,
        bool enableBusinessSignalReconcile,
        bool triggerBusinessSignalReconcile = false)
    {
        UpdateSettingsSnapshot(settings);
        _stationViewReadOnly = readOnly;
        _enableBusinessSignalReconcile = enableBusinessSignalReconcile;

        ConfigureStationSelector();
        ApplyMesStatus(_mesConnectionMonitorService.Current);
        RefreshProductionRuntimeState();
        RestoreCurrentRuntimeTipState();
        RefreshRuntimePanels();
        ApplyAllStationStatuses();
        QueueRefreshSchemePreview(force: true);
        ApplyCurrentRealtimePreviewSnapshot();
        RefreshProductHistoryPreview();

        if (triggerBusinessSignalReconcile && _enableBusinessSignalReconcile)
        {
            QueueBusinessSignalReconciliation("AppSettings.SettingsChanged");
        }
    }

    /// <summary>
    /// Receives the latest persisted settings without performing database access.
    /// UI reconfiguration for dual-station mode is coordinated by MainForm.
    /// </summary>
    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        UpdateSettingsSnapshot(e.CurrentSettings);
    }

    /// <summary>
    /// Atomically replaces the read-only settings snapshot used by this view.
    /// The supplied settings object is never modified by MonitorView.
    /// </summary>
    private void UpdateSettingsSnapshot(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Interlocked.Exchange(ref _currentSettings, settings);
    }

    private void LoadTitleLogo()
    {
        if (!File.Exists(AppAssets.LogoPath))
        {
            picLogo.Visible = false;
            return;
        }

        picLogo.Visible = true;
        picLogo.ImageLocation = AppAssets.LogoPath;
    }

    /// <summary>
    /// Keeps the header readable when English labels are longer than Chinese labels.
    /// </summary>
    private void ConfigureHeaderLayout()
    {
        _headerStatusFont = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular);
        _headerButtonFont = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular);

        LeftTopLayout.AutoSize = false;
        tlpCommunicationStatus.MinimumSize = new Size(HeaderStatusCellMinWidth * 2, 0);

        GetVersion();
        ConfigureStatusTag(tagMes);
        ConfigureStatusTag(tagPLC);
        ConfigureStatusTag(tagDeviceStatus);
        ConfigureStatusTag(tagTaskStatus);
        ConfigureCommunicationStatusLayout();
        AdjustHeaderFixedColumns();
    }

    private void ConfigureCommunicationStatusLayout()
    {
        tlpCommunicationStatus.SuspendLayout();
        try
        {
            tlpCommunicationStatus.Controls.Clear();
            tlpCommunicationStatus.ColumnStyles.Clear();
            tlpCommunicationStatus.RowStyles.Clear();
            tlpCommunicationStatus.RowCount = 2;
            tlpCommunicationStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCommunicationStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            tlpCommunicationStatus.ColumnCount = 2;
            tlpCommunicationStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCommunicationStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCommunicationStatus.Controls.Add(tagMes, 0, 0);
            tlpCommunicationStatus.Controls.Add(tagPLC, 1, 0);
            tlpCommunicationStatus.Controls.Add(tagDeviceStatus, 0, 1);
            tlpCommunicationStatus.Controls.Add(tagTaskStatus, 1, 1);
        }
        finally
        {
            tlpCommunicationStatus.ResumeLayout();
        }
    }

    private void GetVersion() => lblVersion.Text = BuildVersionText();

    /// <summary>
    /// Status tags use compact bold text and a small margin so rounded corners do not cut into text.
    /// </summary>
    private void ConfigureStatusTag(AntdUI.Tag tag)
    {
        tag.Font = _headerStatusFont;
        tag.Margin = new Padding(4, 0, 4, 0);
        tag.AutoEllipsis = false;
        tag.TextMultiLine = true;
    }

    /// <summary>
    /// The title column can shrink, while status cards and action buttons reserve measured widths.
    /// </summary>
    private void AdjustHeaderFixedColumns()
    {
        if (LeftTopLayout.ColumnStyles.Count < 4)
        {
            return;
        }

        var logoWidth = picLogo.Visible ? HeaderLogoWidth : 0;
        var statusWidth = CalculateHeaderStatusWidth();
        var actionWidth = CalculateHeaderActionWidth();

        LeftTopLayout.ColumnStyles[0].SizeType = SizeType.Absolute;
        LeftTopLayout.ColumnStyles[0].Width = logoWidth;
        LeftTopLayout.ColumnStyles[1].SizeType = SizeType.Percent;
        LeftTopLayout.ColumnStyles[1].Width = 100F;
        LeftTopLayout.ColumnStyles[2].SizeType = SizeType.Absolute;
        LeftTopLayout.ColumnStyles[2].Width = statusWidth;
        LeftTopLayout.ColumnStyles[3].SizeType = SizeType.Absolute;
        LeftTopLayout.ColumnStyles[3].Width = actionWidth;

        tlpCommunicationStatus.MinimumSize = new Size(statusWidth, 0);
    }

    /// <summary>
    /// Measures possible status words so every status card can show the longest translated value.
    /// </summary>
    private int CalculateHeaderStatusWidth()
    {
        var statusFont = _headerStatusFont ?? tagMes.Font;
        var statusTexts = new[]
        {
            "MES",
            "PLC",
            _localizer.GetString(TextKeys.Monitor.Label.DeviceStatus),
            _localizer.GetString(TextKeys.Mes.StateChecking),
            _localizer.GetString(TextKeys.Mes.StateConnected),
            _localizer.GetString(TextKeys.Mes.StateDisconnected),
            _localizer.GetString(TextKeys.Plc.StateStopped),
            _localizer.GetString(TextKeys.Plc.StateConnecting),
            _localizer.GetString(TextKeys.Plc.StateConnected),
            _localizer.GetString(TextKeys.Plc.StateReconnecting),
            _localizer.GetString(TextKeys.Plc.StateDisconnected),
            _localizer.GetString(TextKeys.Plc.StateFaulted),
            _localizer.GetString(TextKeys.DeviceStatus.Running),
            _localizer.GetString(TextKeys.DeviceStatus.Paused),
            _localizer.GetString(TextKeys.DeviceStatus.Stopped),
            _localizer.GetString(TextKeys.DeviceStatus.Alarm),
            _localizer.GetString(TextKeys.DeviceStatus.Unknown),
            "工位状态",
            "未开工",
            "待开工",
            "已开工",
            "已暂停",
            "已完工"
        };

        var maxTextWidth = statusTexts.Max(text => MeasureTextWidth(text, statusFont));
        var cellWidth = Math.Max(HeaderStatusCellMinWidth, maxTextWidth + HeaderStatusCellPadding);
        return cellWidth * 2;
    }

    /// <summary>
    /// Measures localized report button text and leaves extra room for the icon.
    /// </summary>
    private int CalculateHeaderActionWidth()
    {
        var buttonFont = _headerButtonFont ?? btnExpStart.Font;
        var startWidth = MeasureTextWidth(btnExpStart.Text ?? string.Empty, buttonFont);
        var finishWidth = MeasureTextWidth(btnExpEnd.Text ?? string.Empty, buttonFont);
        return Math.Max(HeaderActionMinWidth, Math.Max(startWidth, finishWidth) + HeaderButtonPadding);
    }

    /// <summary>
    /// Centralized text measurement avoids scattered magic width values in the header layout.
    /// </summary>
    private static int MeasureTextWidth(string text, Font font)
    {
        return TextRenderer.MeasureText(
            text,
            font,
            new Size(10000, 10000),
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
    }

    /// <summary>
    /// Builds the compact version text shown below the monitor title.
    /// </summary>
    private static string BuildVersionText()
    {
        var version = GetApplicationVersion();
        return string.IsNullOrWhiteSpace(version)
            ? string.Empty
            : $"{VersionPrefix}{version}";
    }

    /// <summary>
    /// Reads the product version from assembly metadata so the UI always follows Directory.Build.props.
    /// </summary>
    private static string GetApplicationVersion()
    {
        var assembly = typeof(MonitorView).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        var version = string.IsNullOrWhiteSpace(informationalVersion)
            ? assembly.GetName().Version?.ToString(3)
            : informationalVersion;

        // InformationalVersion may contain source metadata after '+', but operators only need the release version.
        return version?.Split('+')[0] ?? string.Empty;
    }

    private void ConfigureRuntimeMessagePanels()
    {
        _runtimeMessageFont = new Font("Microsoft YaHei UI", 12.5F, FontStyle.Bold);
        _runtimeGroupFont = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold);

        grpErrorTips.Font = _runtimeGroupFont;
        grpRunningStatus.Font = _runtimeGroupFont;
        inputErrorTips.Font = _runtimeMessageFont;
        inputRunningStatus.Font = _runtimeMessageFont;

        ApplyRuntimeErrorTone(hasError: false);
        ApplyRuntimeStatusTone();
    }

    private void ConfigureStationResultTags()
    {
        tableLayoutPanel1.AutoSize = false;
        ConfigureStationResultTag(tagStation1, "工位1--", UiColors.Status.Muted);
        ConfigureStationResultTag(tagStation2, "工位2--", UiColors.Status.Muted);
        UpdateStationResultLayout();
    }

    private static void ConfigureStationResultTag(AntdUI.Tag tag, string text, Color backColor)
    {
        tag.Text = text;
        tag.Dock = DockStyle.Fill;
        tag.ForeColor = Color.White;
        tag.BackColor = backColor;
        tag.TextMultiLine = true;
    }

    private void ConfigureStationSelector()
    {
        _dualStationEnabled = _currentSettings.EnableDualStation;
        SetStationSelectorVisible(_dualStationEnabled);
        SetStationPreviewPagesVisible(_dualStationEnabled);
        ConfigureCommunicationStatusLayout();
        UpdateStationResultLayout();
        AdjustHeaderFixedColumns();

        if (!_dualStationEnabled && CurrentStationNo != ProductionConstants.Stations.DefaultStationNo)
        {
            _viewStationNo = ProductionConstants.Stations.DefaultStationNo;
        }

        if (!_dualStationEnabled
            && _weldTaskService.CurrentState.CurrentStationNo != ProductionConstants.Stations.DefaultStationNo)
        {
            _weldTaskService.SelectStation(ProductionConstants.Stations.DefaultStationNo);
        }

        BindStationSelection();
        ApplyStationViewMode();
    }

    private void ApplyStationViewMode()
    {
        ApplyOperationMode();
    }

    private void ApplyOperationMode()
    {
        var canOperate = !_stationViewReadOnly;
        btnGetWO.Visible = canOperate;
        btnLocalWorkOrder.Visible = canOperate;
        btnChangeWO.Visible = canOperate;
        btnEditWO.Visible = canOperate;
        btnExpStart.Visible = canOperate;
        btnExpEnd.Visible = canOperate;

        btnGetWO.Enabled = canOperate;
        btnLocalWorkOrder.Enabled = canOperate;
        btnChangeWO.Enabled = canOperate;
        btnEditWO.Enabled = canOperate;
        btnExpStart.Enabled = canOperate;
        btnExpEnd.Enabled = canOperate;
    }

    private bool IsReadOnlyOperationBlocked(string actionName)
    {
        if (!_stationViewReadOnly)
        {
            return false;
        }

        SetRuntimeErrorText($"工位{CurrentStationNo}{actionName}已禁用，当前窗口为只读看板。");
        return true;
    }

    private void SetStationPreviewPagesVisible(bool visible)
    {
        tabStation2.Visible = visible;
        tabPage1.Visible = visible;

        if (!visible)
        {
            tabsStationView.SelectedIndex = 0;
            tabsMetrics.SelectedIndex = 0;
        }
    }

    private void UpdateStationResultLayout()
    {
        if (tableLayoutPanel1.ColumnStyles.Count < 2)
        {
            return;
        }

        tagStation1.Visible = true;
        tagStation2.Visible = _dualStationEnabled;

        tableLayoutPanel1.ColumnStyles[0].SizeType = SizeType.Percent;
        tableLayoutPanel1.ColumnStyles[0].Width = _dualStationEnabled ? 50F : 100F;
        tableLayoutPanel1.ColumnStyles[1].SizeType = _dualStationEnabled
            ? SizeType.Percent
            : SizeType.Absolute;
        tableLayoutPanel1.ColumnStyles[1].Width = _dualStationEnabled ? 50F : 0F;
    }

    private void SetStationSelectorVisible(bool visible)
    {
        tlpCurStation.Visible = visible;

        if (TLPWorkOrderInfo.RowStyles.Count <= StationSelectorRowIndex)
        {
            return;
        }

        TLPWorkOrderInfo.RowStyles[StationSelectorRowIndex].SizeType = visible ? SizeType.Percent : SizeType.Absolute;
        TLPWorkOrderInfo.RowStyles[StationSelectorRowIndex].Height = visible ? 10F : 0F;
    }

    private void BindStationSelection()
    {
        _syncingStationSelection = true;
        try
        {
            segmentedStationSwitch.Items.Clear();
            segmentedStationSwitch.Items.Add(new AntdUI.SegmentedItem { Text = FormatStationName(1) });
            segmentedStationSwitch.Items.Add(new AntdUI.SegmentedItem { Text = FormatStationName(2) });
            SyncStationSelection();
        }
        finally
        {
            _syncingStationSelection = false;
        }
    }

    private void SyncStationSelection()
    {
        var previousSyncing = _syncingStationSelection;
        _syncingStationSelection = true;
        try
        {
            var index = Math.Max(0, Math.Min(1, CurrentStationNo - 1));
            if (segmentedStationSwitch.Items.Count > 0 && segmentedStationSwitch.SelectIndex != index)
            {
                segmentedStationSwitch.SelectIndex = index;
            }

            if (tabsStationView.Pages.Count > index && tabsStationView.SelectedIndex != index)
            {
                tabsStationView.SelectedIndex = index;
            }

            if (tabsMetrics.Pages.Count > index && tabsMetrics.SelectedIndex != index)
            {
                tabsMetrics.SelectedIndex = index;
            }
        }
        finally
        {
            _syncingStationSelection = previousSyncing;
        }
    }

    private string FormatStationName(int stationNo)
    {
        return $"{_localizer.GetString(TextKeys.Monitor.Label.Station)} {stationNo}";
    }

    private int CurrentStationNo
    {
        get
        {
            return NormalizePreviewStationNo(_viewStationNo);
        }
    }

    private ProductionStationRuntimeState GetCurrentStationState()
    {
        return _weldTaskService.CurrentState.GetOrCreateStation(CurrentStationNo);
    }

    /// <summary>
    /// Work-order level PLC signals are mirrored in dual-station/same-work-order mode.
    /// </summary>
    private IReadOnlyList<int> ResolveWorkOrderSignalStations(int stationNo)
    {
        var settings = _currentSettings;
        if (settings.EnableDualStation && !settings.EnableDualWorkOrder)
        {
            return [1, 2];
        }

        return [NormalizeStatusStationNo(stationNo)];
    }

    private DataGridView CurrentWeldPreviewGrid => GetWeldPreviewGrid(CurrentStationNo);

    private SlimHorizontalScrollBar CurrentWeldPreviewScrollBar
        => GetWeldPreviewScrollBar(CurrentStationNo);

    private DataGridView GetWeldPreviewGrid(int stationNo)
        => NormalizePreviewStationNo(stationNo) == 2 ? dgvPreview2 : dgvPreview1;

    private SlimHorizontalScrollBar GetWeldPreviewScrollBar(int stationNo)
        => NormalizePreviewStationNo(stationNo) == 2 ? HorizontalScrollBar2 : HorizontalScrollBar1;

    private int ResolveWeldPreviewStationNo(Control control)
    {
        if (ReferenceEquals(control, dgvPreview2)
            || ReferenceEquals(control, HorizontalScrollBar2))
        {
            return 2;
        }

        return 1;
    }

    private static int NormalizePreviewStationNo(int stationNo)
        => stationNo == 2 ? 2 : 1;

    private AntdUI.Table CurrentMetricTable => CurrentStationNo == 2 ? tableMetric2 : tableMetric1;

    private AntdUI.Table CurrentProductHistoryTable => CurrentStationNo == 2 ? tableProductHistoryPreview2 : tableProductHistoryPreview1;

    private AntdUI.Label CurrentLivePreviewStatusLabel => CurrentStationNo == 2 ? lblLiveHint2 : lblLiveHint1;

    private AntdUI.Label CurrentLiveProductNoLabel => CurrentStationNo == 2 ? lblLiveProductNo2 : lblLiveProductNo1;

    private AntdUI.Tag CurrentLiveResultTag => CurrentStationNo == 2 ? tagLiveResult2 : tagLiveResult1;

    private AntdUI.Label CurrentLiveTouchCountLabel => CurrentStationNo == 2 ? lblLiveTouchNo2 : lblLiveTouchNo1;

    private PlcWorkIdSnapshot GetCurrentWorkIdSnapshot()
    {
        return _plcWorkIdMonitorService.GetCurrent(CurrentStationNo);
    }

    private string GetCurrentLiveWorkId()
    {
        var snapshot = GetCurrentWorkIdSnapshot();
        return snapshot.IsSuccess
            ? snapshot.WorkId.Trim()
            : string.Empty;
    }

    private PlcProductionSnapshot GetCurrentProductionSnapshot()
    {
        return _plcProductionMonitorService.GetCurrent(CurrentStationNo);
    }

    private void TitleLayout_Changed(object? sender, EventArgs e)
    {
        AdjustHeaderFixedColumns();
        AdjustTitleFontSize();
    }

    private void AdjustTitleFontSize()
    {
        if (_adjustingTitleFont || string.IsNullOrWhiteSpace(lblTitle.Text))
        {
            return;
        }

        var availableSize = new Size(
            Math.Max(1, lblTitle.ClientSize.Width - TitleTextPadding),
            Math.Max(1, lblTitle.ClientSize.Height - TitleTextPadding));

        if (availableSize.Width <= 1 || availableSize.Height <= 1)
        {
            return;
        }

        _adjustingTitleFont = true;
        try
        {
            var bestSize = FindBestTitleFontSize(lblTitle.Text, lblTitle.Font, availableSize);
            if (Math.Abs(lblTitle.Font.Size - bestSize) < 0.25F)
            {
                return;
            }

            var oldFont = _titleFont;
            _titleFont = new Font(lblTitle.Font.FontFamily, bestSize, lblTitle.Font.Style, lblTitle.Font.Unit);
            lblTitle.Font = _titleFont;
            oldFont?.Dispose();
        }
        finally
        {
            _adjustingTitleFont = false;
        }
    }

    private static float FindBestTitleFontSize(string text, Font baseFont, Size availableSize)
    {
        const float MinTitleFontSize = 12F;
        const float MaxTitleFontSize = 68F;

        var low = MinTitleFontSize;
        var high = MaxTitleFontSize;
        var best = MinTitleFontSize;
        const TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

        for (var i = 0; i < 12; i++)
        {
            var mid = (low + high) / 2F;
            using var testFont = new Font(baseFont.FontFamily, mid, baseFont.Style, baseFont.Unit);
            var measuredSize = TextRenderer.MeasureText(text, testFont, new Size(10000, 10000), flags);

            if (measuredSize.Width <= availableSize.Width && measuredSize.Height <= availableSize.Height)
            {
                best = mid;
                low = mid;
                continue;
            }

            high = mid;
        }

        return best;
    }

    private void WireEvents()
    {
        Load += MonitorView_Load;

        btnGetWO.Click += GetWorkOrder_Click;
        btnLocalWorkOrder.Click += LocalWorkOrder_Click;
        btnChangeWO.Click += ChangeWorkOrder_Click;
        btnEditWO.Click += EditWorkOrder_Click;
        btnExpStart.Click += StartReport_Click;
        btnExpEnd.Click += FinishReport_Click;

        _timer.Tick += Timer_Tick;
        _realtimePreviewPaintTimer.Tick += RealtimePreviewPaintTimer_Tick;
        _plcStatusToolTipTimer.Tick += PlcStatusToolTipTimer_Tick;

        LeftTopLayout.SizeChanged += TitleLayout_Changed;
        lblTitle.SizeChanged += TitleLayout_Changed;
        lblTitle.TextChanged += TitleLayout_Changed;
        tagPLC.MouseEnter += TagPLC_MouseEnter;
        tagPLC.MouseLeave += TagPLC_MouseLeave;

        HorizontalScrollBar2.ValueChanged += Table2HorizontalScrollBar_ValueChanged;
        HorizontalScrollBar1.ValueChanged += Table2HorizontalScrollBar_ValueChanged;

        WireWeldPreviewGridEvents(dgvPreview1);
        WireWeldPreviewGridEvents(dgvPreview2);

        tableProductHistoryPreview1.CellClick += ProductHistoryTable_CellClick;
        tableProductHistoryPreview2.CellClick += ProductHistoryTable_CellClick;

        segmentedStationSwitch.SelectIndexChanged += Station_SelectedIndexChanged;
        selectItemName.SelectedIndexChanged += ProcessSelection_SelectedIndexChanged;
        tabsStationView.SelectedIndexChanged += (_, _) => StationTab_SelectedIndexChanged(tabsStationView.SelectedIndex + 1);
        tabsMetrics.SelectedIndexChanged += (_, _) => StationTab_SelectedIndexChanged(tabsMetrics.SelectedIndex + 1);

        _weldTaskService.StateChanged += WeldTaskService_StateChanged;
        _plcCommunicationService.StatusChanged += PlcCommunicationService_StatusChanged;
        _mesConnectionMonitorService.StatusChanged += MesConnectionMonitorService_StatusChanged;
        _plcProductionMonitorService.StatusChanged += PlcProductionMonitorService_StatusChanged;
        _plcWorkIdMonitorService.WorkIdChanged += PlcWorkIdMonitorService_WorkIdChanged;
        _plcWeldCycleMonitorService.WeldPointCollected += PlcWeldCycleMonitorService_WeldPointCollected;
        _productRealtimePreviewService.SnapshotChanged += ProductRealtimePreviewService_SnapshotChanged;
        _productionLogService.LogWritten += ProductionLogService_LogWritten;
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
    }

    private void WireWeldPreviewGridEvents(DataGridView grid)
    {
        grid.MouseEnter += Table2_MouseEnter;
        grid.MouseWheel += Table2_MouseWheel;
        grid.Scroll += Table2_Scroll;
        grid.SizeChanged += Table2_ScrollRangeChanged;
        grid.ColumnWidthChanged += Table2_ScrollRangeChanged;
        grid.ColumnAdded += Table2_ScrollRangeChanged;
        grid.ColumnRemoved += Table2_ScrollRangeChanged;
    }

    private void UnwireWeldPreviewGridEvents(DataGridView grid)
    {
        grid.MouseEnter -= Table2_MouseEnter;
        grid.MouseWheel -= Table2_MouseWheel;
        grid.Scroll -= Table2_Scroll;
        grid.SizeChanged -= Table2_ScrollRangeChanged;
        grid.ColumnWidthChanged -= Table2_ScrollRangeChanged;
        grid.ColumnAdded -= Table2_ScrollRangeChanged;
        grid.ColumnRemoved -= Table2_ScrollRangeChanged;
    }

    private void Table2_MouseEnter(object? sender, EventArgs e)
    {
        var grid = CurrentWeldPreviewGrid;
        if (grid.CanFocus)
        {
            grid.Focus();
        }
    }

    private void Table2_MouseWheel(object? sender, MouseEventArgs e)
    {
        var stationNo = sender is DataGridView grid
            ? ResolveWeldPreviewStationNo(grid)
            : CurrentStationNo;
        if (GetWeldPreviewMaxHorizontalOffset(stationNo) <= 0)
        {
            return;
        }

        if (e is HandledMouseEventArgs handled)
        {
            handled.Handled = true;
        }

        var wheelSteps = Math.Max(1, Math.Abs(e.Delta) / SystemInformation.MouseWheelScrollDelta);
        var direction = e.Delta < 0 ? 1 : -1;
        SetWeldPreviewHorizontalOffset(
            stationNo,
            GetWeldPreviewGrid(stationNo).HorizontalScrollingOffset + direction * wheelSteps * WeldPreviewMouseWheelPixels);
    }

    private void Table2_Scroll(object? sender, ScrollEventArgs e)
    {
        if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
        {
            SyncWeldPreviewHorizontalScrollBar(sender as DataGridView);
        }
    }

    private void Table2_ScrollRangeChanged(object? sender, EventArgs e)
    {
        var grid = sender as DataGridView;
        SyncWeldPreviewHorizontalScrollBar(grid);
    }

    private void Table2HorizontalScrollBar_ValueChanged(object? sender, EventArgs e)
    {
        if (_syncingWeldPreviewHorizontalScroll)
        {
            return;
        }

        if (sender is SlimHorizontalScrollBar scrollBar)
        {
            SetWeldPreviewHorizontalOffset(ResolveWeldPreviewStationNo(scrollBar), scrollBar.Value);
            return;
        }

        SetWeldPreviewHorizontalOffset(CurrentStationNo, CurrentWeldPreviewScrollBar.Value);
    }

    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ConfigureStationSelector();
        BindProductionRuntimeState();
        ConfigureProductionTableColumns();
        ConfigureWeldParameterTableColumns();
        RefreshRuntimePanels();
        ApplyAllStationStatuses();
        ApplyMesStatus(_mesConnectionMonitorService.Current);
        QueueRefreshSchemePreview(force: true);
        AdjustTitleFontSize();
    }

    private void MonitorView_Load(object? sender, EventArgs e)
    {
        _timer.Start();
        _realtimePreviewPaintTimer.Start();
        ApplyLocalizedTexts();
        UpdateCurrentTime();
        ConfigureStationSelector();
        _weldTaskService.RestoreUnfinishedTask(CurrentStationNo);
        BindProductionRuntimeState();
        RestoreCurrentRuntimeTipState();
        RefreshRuntimePanels();
        ApplyAllStationStatuses();
        ApplyMesStatus(_mesConnectionMonitorService.Current);
        ApplyCurrentRealtimePreviewSnapshot();
        RefreshProductHistoryPreview();
        if (_enableBusinessSignalReconcile)
        {
            QueueBusinessSignalReconciliation("MonitorView.Load");
        }
        AdjustTitleFontSize();
        SetVerticalSplitterPanel2ToMinWidth();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _settingsService.SettingsChanged -= SettingsService_SettingsChanged;
        _weldTaskService.StateChanged -= WeldTaskService_StateChanged;
        _plcCommunicationService.StatusChanged -= PlcCommunicationService_StatusChanged;
        _mesConnectionMonitorService.StatusChanged -= MesConnectionMonitorService_StatusChanged;
        _plcProductionMonitorService.StatusChanged -= PlcProductionMonitorService_StatusChanged;
        _plcWorkIdMonitorService.WorkIdChanged -= PlcWorkIdMonitorService_WorkIdChanged;
        _plcWeldCycleMonitorService.WeldPointCollected -= PlcWeldCycleMonitorService_WeldPointCollected;
        _productRealtimePreviewService.SnapshotChanged -= ProductRealtimePreviewService_SnapshotChanged;
        _productionLogService.LogWritten -= ProductionLogService_LogWritten;
        tableProductHistoryPreview1.CellClick -= ProductHistoryTable_CellClick;
        tableProductHistoryPreview2.CellClick -= ProductHistoryTable_CellClick;
        UnwireWeldPreviewGridEvents(dgvPreview1);
        UnwireWeldPreviewGridEvents(dgvPreview2);
        HorizontalScrollBar1.ValueChanged -= Table2HorizontalScrollBar_ValueChanged;
        HorizontalScrollBar2.ValueChanged -= Table2HorizontalScrollBar_ValueChanged;
        tagPLC.MouseEnter -= TagPLC_MouseEnter;
        tagPLC.MouseLeave -= TagPLC_MouseLeave;
        _timer.Stop();
        _realtimePreviewPaintTimer.Stop();
        _plcStatusToolTipTimer.Stop();
        _timer.Dispose();
        _realtimePreviewPaintTimer.Dispose();
        _plcStatusToolTipTimer.Dispose();
        DisposePlcStatusToolTipPopup();
        _titleFont?.Dispose();
        _headerStatusFont?.Dispose();
        _headerButtonFont?.Dispose();
        _runtimeMessageFont?.Dispose();
        _runtimeGroupFont?.Dispose();
        base.OnHandleDestroyed(e);
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateCurrentTime();

        if (GetCurrentStationState().CurrentWorkOrder is null)
        {
            QueueRefreshSchemePreview(force: false);
        }

        if (_enableBusinessSignalReconcile && _plcCommunicationService.Current.IsConnected)
        {
            QueueBusinessSignalReconciliation("MonitorView.Timer");
        }
    }

    private void WeldTaskService_StateChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() =>
            {
                RefreshProductionRuntimeState();
                QueueRefreshSchemePreview(force: true);
                if (_enableBusinessSignalReconcile)
                {
                    QueueBusinessSignalReconciliation("WeldTaskService.StateChanged", includeDeviceMode: false);
                }
            });
            return;
        }

        RefreshProductionRuntimeState();
        QueueRefreshSchemePreview(force: true);
        if (_enableBusinessSignalReconcile)
        {
            QueueBusinessSignalReconciliation("WeldTaskService.StateChanged", includeDeviceMode: false);
        }
    }

    private void PlcCommunicationService_StatusChanged(object? sender, PlcConnectionSnapshot e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyPlcStatus(e));
            return;
        }

        ApplyPlcStatus(e);
    }

    private void ApplyAllStationStatuses()
    {
        if (IsDisposed)
        {
            return;
        }

        ApplyPlcStatus(_plcCommunicationService.GetCurrent(CurrentStationNo));
        ApplyProductionStatus(_plcProductionMonitorService.GetCurrent(CurrentStationNo));
    }

    private void TagPLC_MouseEnter(object? sender, EventArgs e)
    {
        _plcStatusToolTipVisible = true;
        _lastPlcStatusToolTipRefreshTime = DateTime.MinValue;
        RefreshPlcStatusToolTip();
        _plcStatusToolTipTimer.Start();
    }

    private void TagPLC_MouseLeave(object? sender, EventArgs e)
    {
        ClosePlcStatusToolTip();
    }

    private void PlcStatusToolTipTimer_Tick(object? sender, EventArgs e)
    {
        if (!_plcStatusToolTipVisible)
        {
            return;
        }

        if (!IsMouseOverTagPlc())
        {
            ClosePlcStatusToolTip();
            return;
        }

        if (DateTime.Now - _lastPlcStatusToolTipRefreshTime < TimeSpan.FromMilliseconds(PlcStatusToolTipRefreshIntervalMs))
        {
            return;
        }

        RefreshPlcStatusToolTip();
    }

    /// <summary>
    /// Reads the latest PLC snapshot on demand, so tooltip details can follow heartbeat/message changes.
    /// </summary>
    private void RefreshPlcStatusToolTip()
    {
        if (IsDisposed || !IsHandleCreated || !tagPLC.IsHandleCreated)
        {
            return;
        }

        if (_plcStatusToolTipVisible && !IsMouseOverTagPlc())
        {
            ClosePlcStatusToolTip();
            return;
        }

        _lastPlcStatusToolTipRefreshTime = DateTime.Now;
        var text = BuildPlcStatusToolTipText(_plcCommunicationService.GetCurrent(CurrentStationNo));
        UpdatePlcStatusToolTipText(text);

        if (!_plcStatusToolTipVisible)
        {
            return;
        }

        ShowPlcStatusToolTipPopup();
    }

    private bool IsMouseOverTagPlc()
    {
        if (IsDisposed || !tagPLC.IsHandleCreated)
        {
            return false;
        }

        var bounds = tagPLC.RectangleToScreen(tagPLC.ClientRectangle);
        return bounds.Contains(Cursor.Position);
    }

    private static int NormalizeStatusStationNo(int stationNo)
    {
        return stationNo == 2 ? 2 : ProductionConstants.Stations.DefaultStationNo;
    }

    private void ClosePlcStatusToolTip()
    {
        _plcStatusToolTipVisible = false;
        _plcStatusToolTipTimer.Stop();
        HidePlcStatusToolTipPopup();
    }

    private void EnsurePlcStatusToolTipPopup()
    {
        if (_plcStatusToolTipPanel is not null)
        {
            return;
        }

        _plcStatusToolTipLabel = new Label
        {
            AutoSize = true,
            BackColor = SystemColors.Info,
            ForeColor = SystemColors.InfoText,
            Font = tagPLC.Font,
            MaximumSize = new Size(PlcStatusToolTipMaxWidth, 0),
            Padding = new Padding(10),
            TextAlign = ContentAlignment.TopLeft
        };

        _plcStatusToolTipPanel = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = SystemColors.Info,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Visible = false
        };
        _plcStatusToolTipPanel.Controls.Add(_plcStatusToolTipLabel);
        Controls.Add(_plcStatusToolTipPanel);
    }

    private void UpdatePlcStatusToolTipText(string text)
    {
        EnsurePlcStatusToolTipPopup();

        if (_plcStatusToolTipLabel is null
            || _plcStatusToolTipPanel is null
            || string.Equals(_lastPlcStatusToolTipText, text, StringComparison.Ordinal))
        {
            return;
        }

        _lastPlcStatusToolTipText = text;
        _plcStatusToolTipLabel.Text = text;

        // Update the existing tooltip text without recreating the native tooltip window every 500 ms.
        var preferredSize = _plcStatusToolTipLabel.GetPreferredSize(new Size(PlcStatusToolTipMaxWidth, 0));
        _plcStatusToolTipLabel.Size = preferredSize;
        _plcStatusToolTipPanel.Size = new Size(preferredSize.Width + 2, preferredSize.Height + 2);
    }

    private void ShowPlcStatusToolTipPopup()
    {
        EnsurePlcStatusToolTipPopup();

        if (_plcStatusToolTipPanel is null)
        {
            return;
        }

        _plcStatusToolTipPanel.Location = PointToClient(tagPLC.PointToScreen(new Point(0, tagPLC.Height + 4)));
        _plcStatusToolTipPanel.Visible = true;
        _plcStatusToolTipPanel.BringToFront();
    }

    private void HidePlcStatusToolTipPopup()
    {
        if (_plcStatusToolTipPanel is not null)
        {
            _plcStatusToolTipPanel.Visible = false;
        }
    }

    private void DisposePlcStatusToolTipPopup()
    {
        _plcStatusToolTipPanel?.Dispose();
        _plcStatusToolTipPanel = null;
        _plcStatusToolTipLabel = null;
        _lastPlcStatusToolTipText = string.Empty;
    }

    private string BuildPlcStatusToolTipText(PlcConnectionSnapshot snapshot)
    {
        var history = _plcStatusHistory
            .Where(entry => entry.StationNo == NormalizeStatusStationNo(snapshot.StationNo))
            .Take(PlcStatusHistoryLimit)
            .ToList();
        var builder = new StringBuilder();
        builder.AppendLine($"Station: {NormalizeStatusStationNo(snapshot.StationNo)}");
        builder.AppendLine("PLC 当前详情");
        builder.AppendLine($"当前状态：{GetLocalizedPlcStateText(snapshot.State)} ({snapshot.State})");
        builder.AppendLine($"是否连接：{FormatYesNo(snapshot.IsConnected)}");
        builder.AppendLine($"端点：{FormatToolTipValue(snapshot.Endpoint)}");
        builder.AppendLine($"最近连接时间：{FormatOptionalTime(snapshot.LastConnectedTime)}");
        builder.AppendLine($"最近心跳时间：{FormatOptionalTime(snapshot.LastHeartbeatTime)}");
        builder.AppendLine($"当前消息：{FormatToolTipValue(snapshot.Message)}");
        builder.AppendLine($"当前读取时间：{FormatTime(DateTime.Now)}");
        builder.AppendLine();
        builder.AppendLine("最近 10 条状态变化：");

        if (history.Count == 0)
        {
            builder.AppendLine("暂无记录");
            return builder.ToString();
        }

        for (var i = 0; i < history.Count; i++)
        {
            var entry = history[i];
            builder.AppendLine(
                $"{i + 1}. {FormatTime(entry.ChangedTime)} | {GetLocalizedPlcStateText(entry.State)} ({entry.State}) | 连接：{FormatYesNo(entry.IsConnected)}");
            builder.AppendLine($"   消息：{FormatToolTipValue(entry.Message)}");
        }

        return builder.ToString();
    }

    private void RecordPlcStatusChange(PlcConnectionSnapshot snapshot)
    {
        var stationNo = NormalizeStatusStationNo(snapshot.StationNo);
        if (_lastPlcHistorySnapshots.TryGetValue(stationNo, out var lastSnapshot)
            && lastSnapshot.State == snapshot.State
            && lastSnapshot.IsConnected == snapshot.IsConnected)
        {
            return;
        }

        _lastPlcHistorySnapshots[stationNo] = snapshot;
        _plcStatusHistory.Insert(0, new PlcStatusHistoryEntry(
            stationNo,
            DateTime.Now,
            snapshot.State,
            snapshot.IsConnected,
            snapshot.Message));

        var extraHistory = _plcStatusHistory
            .Where(entry => entry.StationNo == stationNo)
            .Skip(PlcStatusHistoryLimit)
            .ToList();
        foreach (var entry in extraHistory)
        {
            _plcStatusHistory.Remove(entry);
        }
    }

    private string GetLocalizedPlcStateText(PlcConnectionState state)
    {
        return _localizer.GetString(GetPlcStateKey(state));
    }

    private static string FormatYesNo(bool value)
    {
        return value ? "是" : "否";
    }

    private static string FormatOptionalTime(DateTime? value)
    {
        return value.HasValue ? FormatTime(value.Value) : "--";
    }

    private static string FormatTime(DateTime value)
    {
        return value.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);
    }

    private static string FormatToolTipValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "--";
        }

        return value.Trim()
            .Replace("\r\n", " / ", StringComparison.Ordinal)
            .Replace("\n", " / ", StringComparison.Ordinal);
    }

    private void MesConnectionMonitorService_StatusChanged(object? sender, MesConnectionSnapshot e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyMesStatus(e));
            return;
        }

        ApplyMesStatus(e);
    }

    private void PlcProductionMonitorService_StatusChanged(object? sender, PlcProductionSnapshot e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyProductionStatus(e));
            return;
        }

        ApplyProductionStatus(e);
    }

    private void PlcWorkIdMonitorService_WorkIdChanged(object? sender, PlcWorkIdSnapshot e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyWorkIdSnapshot(e));
            return;
        }

        ApplyWorkIdSnapshot(e);
    }

    private void PlcWeldCycleMonitorService_WeldPointCollected(object? sender, BizWeldPointRecord e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyLatestWeldPointRecord(e));
            return;
        }

        ApplyLatestWeldPointRecord(e);
    }

    private void ProductRealtimePreviewService_SnapshotChanged(object? sender, ProductRealtimePreviewSnapshot e)
    {
        if (IsDisposed || !IsHandleCreated || e.StationNo != CurrentStationNo)
        {
            return;
        }

        lock (_realtimePreviewSync)
        {
            _pendingRealtimePreviewSnapshot = e;
            if (_realtimePreviewApplyPosted)
            {
                return;
            }

            _realtimePreviewApplyPosted = true;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(ApplyPendingRealtimePreviewSnapshot));
            return;
        }

        ApplyPendingRealtimePreviewSnapshot();
    }

    /// <summary>
    /// Fallback check for a pending realtime snapshot; normal snapshots post to the UI immediately.
    /// </summary>
    private void RealtimePreviewPaintTimer_Tick(object? sender, EventArgs e)
    {
        ApplyPendingRealtimePreviewSnapshot();
    }

    /// <summary>
    /// Consumes one cached realtime snapshot. Older snapshots are overwritten before this method runs.
    /// </summary>
    private void ApplyPendingRealtimePreviewSnapshot()
    {
        ProductRealtimePreviewSnapshot? snapshot;
        lock (_realtimePreviewSync)
        {
            snapshot = _pendingRealtimePreviewSnapshot;
            _pendingRealtimePreviewSnapshot = null;
            _realtimePreviewApplyPosted = false;
        }

        if (snapshot is null || IsDisposed || snapshot.StationNo != CurrentStationNo)
        {
            return;
        }

        ApplyProductRealtimePreviewSnapshot(snapshot);
    }

    private void ProductionLogService_LogWritten(object? sender, ProductionFlowLogEntry e)
    {
        if (IsDisposed || !ShouldShowProductionHint(e))
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyProductionHint(e));
            return;
        }

        ApplyProductionHint(e);
    }

    private async void GetWorkOrder_Click(object? sender, EventArgs e)
    {
        if (IsReadOnlyOperationBlocked("获取工单"))
        {
            return;
        }

        var stationNo = CurrentStationNo;

        SelectStationForOperation(stationNo);
        if (_weldTaskService.RestoreUnfinishedTask(stationNo) is not null)
        {
            ShowWarning(TextKeys.Monitor.Message.StartBlockedByUnfinishedTask);
            return;
        }

        await PrepareWorkOrderAsync(forceManualInput: false);
    }

    private async void ChangeWorkOrder_Click(object? sender, EventArgs e)
    {
        if (IsReadOnlyOperationBlocked("变更工单"))
        {
            return;
        }

        var stationNo = CurrentStationNo;
        SelectStationForOperation(stationNo);
        if (_weldTaskService.RestoreUnfinishedTask(stationNo) is not null)
        {
            ShowWarning(TextKeys.Monitor.Message.StartBlockedByUnfinishedTask);
            return;
        }

        await PrepareWorkOrderAsync(forceManualInput: true);
    }

    private async void EditWorkOrder_Click(object? sender, EventArgs e)
    {
        if (IsReadOnlyOperationBlocked("微调工单"))
        {
            return;
        }

        SelectStationForOperation(CurrentStationNo);
        var state = GetCurrentStationState();
        if (state.CurrentWorkOrder is null)
        {
            ShowWarningText("请先获取工单信息后再确认加工程序。");
            return;
        }

        if (state.ActiveTask is not null)
        {
            ShowWarningText("处于开工状态，禁止调整加工程序。");
            return;
        }

        if (state.SelectedProcess is null)
        {
            ShowWarning(TextKeys.Monitor.Message.ProcessRequired);
            return;
        }

        if (state.SelectedProgram is null)
        {
            await PrepareProgramForCurrentWorkOrderAsync(CurrentStationNo);
            return;
        }

        if (TryConfirmStartData(state.CurrentWorkOrder, state.SelectedProcess, state.SelectedProgram, CurrentStationNo))
        {
            RefreshProductionRuntimeState();
            ClearRuntimeError();
            SetRuntimeStatusText("加工程序已确认，本次开工将使用当前程序内容。", isSuccess: true);
        }
    }

    private async void LocalWorkOrder_Click(object? sender, EventArgs e)
    {
        if (IsReadOnlyOperationBlocked("本地工单"))
        {
            return;
        }

        var stationNo = CurrentStationNo;
        SelectStationForOperation(stationNo);
        var activeTask = _weldTaskService.RestoreUnfinishedTask(stationNo);
        if (activeTask is { IsOfflineCreated: true, EndTime: null })
        {
            await FinishLocalWorkOrderAsync(stationNo);
            return;
        }

        if (activeTask is not null && activeTask.EndTime is null)
        {
            SetStationReportFailure(stationNo, "本地工单", "当前工位已有在线任务未完工，不能创建本地工单。");
            return;
        }

        var programs = _programManageService.GetPrograms()
            .Where(program => !program.IsDeleted)
            .Where(program => !string.IsNullOrWhiteSpace(program.RecipeCode))
            .ToList();
        if (programs.Count == 0)
        {
            ShowWarningText("没有可用的本地程序，或本地程序缺少配方编号。");
            return;
        }

        using var form = new LocalWorkOrderForm(programs, stationNo, _plcWorkIdMonitorService);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var localProgram = new ProgramDataRes
        {
            Id = form.Request.ProgramId,
            ProgramName = form.Request.ProgramName,
            ProductNum = form.Request.ProductNum,
            RecipeCode = form.Request.RecipeCode,
            ProgramType = form.Request.ProgramType,
            ProgramContent = form.Request.ProgramContent
        };

        BindLocalOperatorInfo();
        await RunReportOperationAsync(stationNo, "本地开工", async () =>
        {
            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.SubmittingStart);
            selectRecipeCode.Text = form.Request.RecipeCode;
            await _weldTaskService.StartLocalAsync(form.Request, ResolveLocalOperatorNumber(), 0);
            RefreshProductionRuntimeState();
            QueueRefreshSchemePreview(force: true);
            SetRuntimeStatusText(BuildStationReportSuccessText(stationNo, "本地开工"), isSuccess: true);
        });

        // Write business signals independently - failures won't affect the start success status
        await SafeWriteStartBusinessSignalsAsync(localProgram, stationNo);
    }

    private async Task FinishLocalWorkOrderAsync(int stationNo)
    {
        if (!TryResolveFinishQuantities(stationNo, out var actualQty, out var qualifiedQty, out var failedQty))
        {
            return;
        }

        BindLocalOperatorInfo();
        await RunReportOperationAsync(stationNo, "本地完工", async () =>
        {
            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.SubmittingFinish);
            await _weldTaskService.FinishLocalAsync(
                ResolveLocalOperatorNumber(),
                actualQty,
                qualifiedQty,
                failedQty,
                stationNo);
            await WriteFinishBusinessSignalsAsync(stationNo);
            RefreshProductionRuntimeState();
            SetRuntimeStatusText(BuildStationReportSuccessText(stationNo, "本地完工"), isSuccess: true);
        });
    }

    private void BindLocalOperatorInfo()
    {
        var user = GlobalContext.CurrentUser;
        MesUserName.Text = user?.UserName ?? Environment.UserName;
        MesUserNumber.Text = ResolveLocalOperatorNumber();
        DeptName.Text = string.Empty;
        TeamName.Text = string.Empty;
    }

    private static string ResolveLocalOperatorNumber()
    {
        if (!string.IsNullOrWhiteSpace(GlobalContext.CurrentUser?.UserNumber))
        {
            return GlobalContext.CurrentUser.UserNumber.Trim();
        }

        return string.IsNullOrWhiteSpace(Environment.UserName)
            ? "local"
            : Environment.UserName.Trim();
    }

    private async void StartReport_Click(object? sender, EventArgs e)
    {
        if (IsReadOnlyOperationBlocked("开工上报"))
        {
            return;
        }

        var stationNo = CurrentStationNo;
        SelectStationForOperation(stationNo);
        if (_weldTaskService.RestoreUnfinishedTask(stationNo) is not null)
        {
            RefreshProductionRuntimeState();
            SetStationReportFailure(stationNo, "开工上报", BuildLocalizedMessage(TextKeys.Monitor.Message.StartBlockedByUnfinishedTask));
            return;
        }

        var state = GetCurrentStationState();
        if (state.CurrentWorkOrder is null)
        {
            SetStationReportFailure(stationNo, "开工上报", "请先点击获取工单，获取工单信息后再开工上报。");
            return;
        }

        if (state.SelectedProcess is null)
        {
            SetStationReportFailure(stationNo, "开工上报", BuildLocalizedMessage(TextKeys.Monitor.Message.ProcessRequired));
            return;
        }

        if (state.CurrentWorkOrder is not null
            && state.SelectedProcess is not null
            && state.SelectedProgram is null
            && !await PrepareProgramForCurrentWorkOrderAsync(stationNo))
        {
            return;
        }

        state = GetCurrentStationState();
        if (state.CurrentWorkOrder is null || state.SelectedProcess is null || state.SelectedProgram is null)
        {
            SetStationReportFailure(stationNo, "开工上报", BuildLocalizedMessage(TextKeys.Monitor.Message.StartPrerequisiteMissing));
            return;
        }

        if (!IsProgramContentConfirmed(state.SelectedProgram, stationNo)
            && !TryConfirmStartData(state.CurrentWorkOrder, state.SelectedProcess, state.SelectedProgram, stationNo))
        {
            return;
        }

        var actualQty = 0;

        var employeeNumber = await PromptValidatedOperatorAsync(stationNo);
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return;
        }

        await RunReportOperationAsync(stationNo, "开工上报", async () =>
        {
            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.SubmittingStart);
            await _weldTaskService.StartAsync(employeeNumber, actualQty, stationNo, employeeAlreadyValidated: true);
            RefreshProductionRuntimeState();
            QueueRefreshSchemePreview(force: true);
            SetRuntimeStatusText(BuildStationReportSuccessText(stationNo, "开工上报"), isSuccess: true);
        });

        // Write business signals independently - failures won't affect the start success status
        await SafeWriteStartBusinessSignalsAsync(state.SelectedProgram, stationNo);
    }

    private async void FinishReport_Click(object? sender, EventArgs e)
    {
        if (IsReadOnlyOperationBlocked("完工上报"))
        {
            return;
        }

        var stationNo = CurrentStationNo;
        SelectStationForOperation(stationNo);
        var activeTask = _weldTaskService.RestoreUnfinishedTask(stationNo);
        if (activeTask is null)
        {
            SetStationReportFailure(stationNo, "完工上报", BuildLocalizedMessage(TextKeys.Monitor.Message.FinishPrerequisiteMissing));
            return;
        }

        var employeeNumber = await PromptValidatedOperatorAsync(stationNo);
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return;
        }

        if (!TryResolveFinishQuantities(stationNo, out var actualQty, out var qualifiedQty, out var failedQty))
        {
            return;
        }

        await RunReportOperationAsync(stationNo, "完工上报", async () =>
        {
            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.SubmittingFinish);
            await _weldTaskService.FinishAsync(employeeNumber, actualQty, qualifiedQty, failedQty, stationNo);
            await WriteFinishBusinessSignalsAsync(stationNo);
            RefreshProductionRuntimeState();
            SetRuntimeStatusText(BuildStationReportSuccessText(stationNo, "完工上报"), isSuccess: true);
        });
    }

    private bool TryResolveFinishQuantities(int stationNo, out int actualQty, out int qualifiedQty, out int failedQty)
    {
        var settings = _currentSettings;
        var production = GetCurrentProductionSnapshot();
        return settings.EnableFinishExpQtyPrompt
            ? TryResolveFinishQuantitiesWithPrompt(production, out actualQty, out qualifiedQty, out failedQty)
            : TryResolveFinishQuantitiesFromPlc(stationNo, production, out actualQty, out qualifiedQty, out failedQty);
    }

    private bool TryResolveFinishQuantitiesFromPlc(
        int stationNo,
        PlcProductionSnapshot production,
        out int actualQty,
        out int qualifiedQty,
        out int failedQty)
    {
        actualQty = production.TotalProduction;
        qualifiedQty = production.AcceptedQuantity;
        failedQty = production.RejectedQuantity;

        if (production.IsSuccess && production.ProductionQuantitiesReadSuccess)
        {
            return true;
        }

        SetStationReportFailure(stationNo, "完工上报", BuildFinishQuantityReadFailureText(production));
        return false;
    }

    private bool TryResolveFinishQuantitiesWithPrompt(
        PlcProductionSnapshot production,
        out int actualQty,
        out int qualifiedQty,
        out int failedQty)
    {
        actualQty = 0;
        qualifiedQty = 0;
        failedQty = 0;

        var defaultActual = production.TotalProductionReadSuccess
            ? Math.Max(0, production.TotalProduction)
            : 0;
        if (!TryPromptNonNegativeInt(
                TextKeys.Monitor.Dialog.ActualQuantityTitle,
                TextKeys.Monitor.Dialog.ActualQuantityPrompt,
                defaultActual,
                out actualQty))
        {
            return false;
        }

        if (production.AcceptedQuantityReadSuccess)
        {
            qualifiedQty = production.AcceptedQuantity;
        }
        else if (!TryPromptNonNegativeInt(
                     TextKeys.Monitor.Dialog.QualifiedQuantityTitle,
                     TextKeys.Monitor.Dialog.QualifiedQuantityPrompt,
                     0,
                     out qualifiedQty))
        {
            return false;
        }

        if (production.RejectedQuantityReadSuccess)
        {
            failedQty = production.RejectedQuantity;
            return true;
        }

        return TryPromptNonNegativeInt(
            TextKeys.Monitor.Dialog.FailedQuantityTitle,
            TextKeys.Monitor.Dialog.FailedQuantityPrompt,
            0,
            out failedQty);
    }

    private static string BuildFinishQuantityReadFailureText(PlcProductionSnapshot production)
    {
        var details = new[]
            {
                production.IsSuccess ? string.Empty : production.Message,
                production.TotalProductionReadSuccess ? string.Empty : production.TotalProductionReadMessage,
                production.AcceptedQuantityReadSuccess ? string.Empty : production.AcceptedQuantityReadMessage,
                production.RejectedQuantityReadSuccess ? string.Empty : production.RejectedQuantityReadMessage
            }
            .Where(detail => !string.IsNullOrWhiteSpace(detail))
            .Select(detail => detail.Trim())
            .Distinct()
            .ToList();

        var suffix = details.Count > 0
            ? string.Join("；", details)
            : "请确认 PLC 连接和产量地址配置。";
        return $"PLC 完工数量读取失败，已阻止完工上报：{suffix}";
    }

    private async Task<bool> PrepareWorkOrderAsync(bool forceManualInput)
    {
        var stationNo = CurrentStationNo;
        if (!TryResolveWorkId(forceManualInput, out var workId))
        {
            return false;
        }

        var isReady = false;
        await RunUiOperationAsync(async () =>
        {
            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.LoadingWorkOrder);
            var workOrder = await _weldTaskService.GetWorkOrderInfoAsync(workId, stationNo);
            if (workOrder is null)
            {
                ShowBusinessWarning(
                    "MES.GetWorkOrderInfo",
                    TextKeys.Monitor.Message.WorkOrderLoadFailed,
                    _weldTaskService.CurrentState.LastServerSyncMessage ?? string.Empty,
                    $"WorkId={workId}");
                return;
            }

            var defaultProcess = workOrder.ExpItems.FirstOrDefault();
            if (defaultProcess is null)
            {
                RefreshProductionRuntimeState();
                ClearMesOperatorInfo();
                ShowWarning(TextKeys.Monitor.Message.ProcessRequired);
                return;
            }

            // Work-order retrieval only binds the process; program download is deferred to start report.
            _weldTaskService.SelectProcess(defaultProcess, stationNo);
            RefreshProductionRuntimeState();
            ClearMesOperatorInfo();
            SetRuntimeStatusText("工单信息已获取，请确认工序后点击开工上报。", isSuccess: true);
            isReady = true;
        });

        return isReady;
    }

    private async Task<bool> PrepareProgramForCurrentWorkOrderAsync(int stationNo)
    {
        var isReady = false;
        await RunUiOperationAsync(async () =>
        {
            var state = GetCurrentStationState();
            var workOrder = state.CurrentWorkOrder;
            if (workOrder is null)
            {
                return;
            }

            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.LoadingPrograms);
            var programs = await _weldTaskService.LoadProgramsAsync(stationNo);
            if (programs.Count == 0)
            {
                ShowBusinessWarning(
                    "MES.GetProgramList",
                    TextKeys.Monitor.Message.ProgramListEmpty,
                    "MES 返回的程序列表为空。",
                    $"WorkId={workOrder.SN}; ProductNum={workOrder.ProdNum}");
                return;
            }

            if (!TrySelectProgram(programs, out var program))
            {
                return;
            }

            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.DownloadingProgram);
            var detail = await _weldTaskService.DownloadProgramAsync(program, stationNo);
            if (detail is null)
            {
                ShowBusinessWarning(
                    "MES.DownloadProgram",
                    TextKeys.Monitor.Message.ProgramDownloadFailed,
                    "MES 程序详情下载失败或返回空数据。",
                    FormatProgram(program));
                return;
            }

            var refreshedState = GetCurrentStationState();
            if (!TryConfirmStartData(refreshedState.CurrentWorkOrder, refreshedState.SelectedProcess, detail, stationNo))
            {
                return;
            }

            RefreshProductionRuntimeState();
            isReady = true;
        });

        return isReady;
    }

    private bool TryResolveWorkId(bool forceManualInput, out string workId)
    {
        var stationSnapshot = GetCurrentWorkIdSnapshot();
        var plcWorkId = stationSnapshot.WorkId.Trim();
        if (!forceManualInput
            && stationSnapshot.IsSuccess
            && !string.IsNullOrWhiteSpace(plcWorkId))
        {
            workId = plcWorkId;
            return true;
        }

        if (!PromptInputForm.TryShow(
                this,
                _localizer.GetString(TextKeys.Monitor.Dialog.ScanWorkIdTitle),
                _localizer.GetString(TextKeys.Monitor.Dialog.ScanWorkIdPrompt),
                plcWorkId,
                _localizer.GetString(TextKeys.Common.ActionApply),
                _localizer.GetString(TextKeys.Common.ActionCancel),
                out var input))
        {
            workId = string.Empty;
            return false;
        }

        workId = input.Trim();
        if (!string.IsNullOrWhiteSpace(workId))
        {
            return true;
        }

        ShowWarning(TextKeys.Monitor.Message.WorkIdRequired);
        return false;
    }

    private void Station_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingStationSelection || !_dualStationEnabled)
        {
            return;
        }

        var stationNo = Math.Clamp(e.Value + 1, 1, 2);
        SwitchStationFromUi(stationNo);
    }

    private void StationTab_SelectedIndexChanged(int stationNo)
    {
        if (_syncingStationSelection || !_dualStationEnabled)
        {
            return;
        }

        SwitchStationFromUi(stationNo);
    }

    private void ProcessSelection_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingProcessSelection)
        {
            return;
        }

        var state = GetCurrentStationState();
        var processes = state.CurrentWorkOrder?.ExpItems ?? [];
        // AntdUI raises SelectedIndexChanged before every control property is stable,
        // so use the event value as the source of truth for the user's new selection.
        var selectedIndex = e.Value;
        if (selectedIndex < 0 || selectedIndex >= processes.Count)
        {
            ClearProcessSelectionDisplay();
            inputProcessNo.Text = string.Empty;
            input1.Text = string.Empty;
            return;
        }

        var process = processes[selectedIndex];
        SelectStationForOperation(CurrentStationNo);
        _weldTaskService.SelectProcess(process, CurrentStationNo);
        selectItemName.Text = GetProcessDisplayName(process);
        inputProcessNo.Text = process.ProcessNo ?? string.Empty;
        input1.Text = process.StartAmount.ToString(CultureInfo.InvariantCulture);
        ClearRuntimeError();
        SetRuntimeStatusText($"已选择工序：{process.ItemName}", isSuccess: true);
    }

    private void SwitchStationFromUi(int stationNo)
    {
        var normalizedStationNo = Math.Clamp(stationNo, 1, 2);
        if (normalizedStationNo != CurrentStationNo)
        {
            _viewStationNo = normalizedStationNo;
            _weldTaskService.RestoreUnfinishedTask(normalizedStationNo);
        }

        RefreshProductionRuntimeState();
        RestoreCurrentRuntimeTipState();
        ApplyAllStationStatuses();
        QueueRefreshSchemePreview(force: true);
        ApplyCurrentRealtimePreviewSnapshot();
        SyncStationSelection();
    }

    private void SelectStationForOperation(int stationNo)
    {
        var normalizedStationNo = NormalizePreviewStationNo(stationNo);
        if (_weldTaskService.CurrentState.CurrentStationNo != normalizedStationNo)
        {
            _weldTaskService.SelectStation(normalizedStationNo);
        }
    }

    private void BindProductionRuntimeState()
    {
        var state = GetCurrentStationState();
        var workOrder = state.CurrentWorkOrder;
        var process = state.SelectedProcess;
        var program = state.SelectedProgram;
        var activeTask = state.ActiveTask;
        var liveWorkId = GetCurrentLiveWorkId();
        var currentIdentity = _currentProductIdentity?.StationNo == CurrentStationNo
            ? _currentProductIdentity
            : null;

        SyncStationSelection();
        inputSN.Text = activeTask is not null
            ? activeTask.SN
            : !string.IsNullOrWhiteSpace(liveWorkId) ? liveWorkId : workOrder?.SN ?? string.Empty;
        inputProdNum.Text = workOrder?.ProdNum ?? currentIdentity?.ProductNum ?? string.Empty;
        inputBatch.Text = workOrder?.Batch ?? string.Empty;
        inputProductName.Text = workOrder?.ProductName ?? string.Empty;
        inputDrawingNo.Text = workOrder?.DrawingNo ?? string.Empty;
        inputProdModel.Text = workOrder?.ProdModel ?? currentIdentity?.ProductModel ?? string.Empty;
        inputSpec.Text = workOrder?.Spec ?? string.Empty;
        BindProcessSelection(workOrder, process, activeTask is not null);
        inputProcessNo.Text = process?.ProcessNo ?? string.Empty;
        input1.Text = process is null ? string.Empty : process.StartAmount.ToString(CultureInfo.InvariantCulture);
        inputProgramName.Text = program?.ProgramName ?? string.Empty;
        selectRecipeCode.Text = ResolveRecipeCodeForDisplay(activeTask, program);
        BindRuntimeOperatorInfo(state, activeTask);
        ApplyTaskStatusTag(state);
        btnLocalWorkOrder.Text = activeTask is { IsOfflineCreated: true, EndTime: null }
            ? "本地完工"
            : _localizer.GetString(TextKeys.Monitor.Button.LocalWorkOrder);
    }

    /// <summary>
    /// <summary>Bind MES process items and immediately reflect the selected process back to runtime fields.</summary>
    /// </summary>
    private void BindProcessSelection(WorkOrderRes? workOrder, ExpItemData? selectedProcess, bool bindSelectedOnly)
    {
        _syncingProcessSelection = true;
        try
        {
            IReadOnlyList<ExpItemData> processes = bindSelectedOnly && selectedProcess is not null
                ? new[] { selectedProcess }
                : workOrder?.ExpItems is null
                    ? Array.Empty<ExpItemData>()
                    : workOrder.ExpItems;
            selectItemName.Items.Clear();
            selectItemName.Items.AddRange(processes
                .Select(GetProcessDisplayName)
                .Cast<object>()
                .ToArray());

            var selectedIndex = ResolveSelectedProcessIndex(processes, selectedProcess);
            if (selectedIndex >= 0)
            {
                selectItemName.SelectedIndex = selectedIndex;
                selectItemName.Text = GetProcessDisplayName(processes[selectedIndex]);
            }
            else
            {
                ClearProcessSelectionDisplay();
            }
        }
        finally
        {
            _syncingProcessSelection = false;
        }
    }

    private void ClearProcessSelectionDisplay()
    {
        if (selectItemName.SelectedIndex != -1)
        {
            selectItemName.SelectedIndex = -1;
        }

        selectItemName.Text = string.Empty;
    }

    private static string GetProcessDisplayName(ExpItemData process)
    {
        return string.IsNullOrWhiteSpace(process.ItemName)
            ? process.ProcessNo.Trim()
            : process.ItemName.Trim();
    }

    private static int ResolveSelectedProcessIndex(IReadOnlyList<ExpItemData> processes, ExpItemData? selectedProcess)
    {
        if (selectedProcess is null)
        {
            return -1;
        }

        var itemIdIndex = selectedProcess.ItemID > 0
            ? processes.ToList().FindIndex(process => process.ItemID == selectedProcess.ItemID)
            : -1;
        if (itemIdIndex >= 0)
        {
            return itemIdIndex;
        }

        // MES sometimes returns duplicate or blank process names/numbers.
        // Match by a combined signature so a duplicated field does not snap the selection back to index 0.
        return processes.ToList().FindIndex(process =>
            SameText(process.ProcessNo, selectedProcess.ProcessNo)
            && SameText(process.ItemName, selectedProcess.ItemName)
            && process.SequenceNo == selectedProcess.SequenceNo);
    }

    private void ApplyTaskStatusTag(ProductionStationRuntimeState state)
    {
        var activeTask = state.ActiveTask;
        if (activeTask is null)
        {
            var isReadyToStart = state.CurrentWorkOrder is not null
                && state.SelectedProcess is not null
                && state.SelectedProgram is not null;
            ApplyTaskStatusTag(
                isReadyToStart ? "待开工" : "未开工",
                isReadyToStart ? UiColors.Status.Primary : UiColors.Status.Muted);
            return;
        }

        var statusText = activeTask.TaskStatus switch
        {
            "Completed" => "已完工",
            "Paused" => "已暂停",
            "Running" => "已开工",
            _ => string.IsNullOrWhiteSpace(activeTask.ExpStartId) ? "未开工" : "已开工"
        };
        var statusColor = activeTask.TaskStatus switch
        {
            "Completed" => UiColors.Status.Success,
            "Paused" => UiColors.Status.Warning,
            "Running" => UiColors.Status.Success,
            _ => string.IsNullOrWhiteSpace(activeTask.ExpStartId) ? UiColors.Status.Muted : UiColors.Status.Success
        };

        ApplyTaskStatusTag(statusText, statusColor);
    }

    private void ApplyTaskStatusTag(string statusText, Color backColor)
    {
        tagTaskStatus.Text = $"{statusText}\r\n工位状态";
        tagTaskStatus.ForeColor = backColor.ToArgb() == UiColors.Status.Warning.ToArgb()
            ? Color.Black
            : Color.White;
        tagTaskStatus.BackColor = backColor;
    }

    /// <summary>
    /// Refreshes work-order fields and metrics that depend on the selected MES process.
    /// </summary>
    private void RefreshProductionRuntimeState()
    {
        BindProductionRuntimeState();
        BindProductionMetrics(GetCurrentProductionSnapshot());
        RefreshProductHistoryPreview();
        QueueRefreshSchemePreview(force: false);
    }

    private void ApplyWorkIdSnapshot(PlcWorkIdSnapshot snapshot)
    {
        if (snapshot.StationNo != CurrentStationNo)
        {
            return;
        }

        if (snapshot.IsSuccess)
        {
            BindProductionRuntimeState();
            QueueRefreshSchemePreview(force: true);
        }

        if (!snapshot.IsSuccess && !string.IsNullOrWhiteSpace(snapshot.Message))
        {
            SetRuntimeError(TextKeys.Monitor.RuntimeError.WorkIdReadFailed);
        }
    }

    private void ApplyLatestWeldPointRecord(BizWeldPointRecord record)
    {
        ApplyStationResult(record);
        if (record.StationNo > 0 && record.StationNo != CurrentStationNo)
        {
            return;
        }

        BindWeldParameterRows(record);
        if (record.ProductCompleted)
        {
            RefreshProductHistoryPreview();
        }

        ClearRuntimeError();
        SetRuntimeStatusText(
            $"数据采集完成：焊点{record.TouchNo} {record.TestResult}",
            isSuccess: true);
    }

    private bool ShouldShowProductionHint(ProductionFlowLogEntry entry)
    {
        if (entry.StationNo > 0 && entry.StationNo != CurrentStationNo)
        {
            return false;
        }

        return entry.Step is
            "ProductDataReady" or
            "ProductCollectionStart" or
            "ProductDataReadStart" or
            "ProductDataSaved" or
            "ProductDataSaveFailed" or
            "ProductCollectionFeedback" or
            "RecipeCodeWriteSucceeded" or
            "RecipeCodeWriteFailed" or
            "RecipeCodeValidationSucceeded" or
            "RecipeCodeValidationFailed" or
            "BusinessSignalWrite" or
            "WorkOrderFinishedCountReset";
    }

    private void ApplyProductionHint(ProductionFlowLogEntry entry)
    {
        if (ShouldRefreshProductHistoryFromLog(entry))
        {
            RefreshProductHistoryPreview();
        }

        if (entry.Level.Equals("Error", StringComparison.OrdinalIgnoreCase))
        {
            SetRuntimeErrorText(ToProductionHintText(entry));
            return;
        }

        ClearRuntimeError();
        SetRuntimeStatusText(ToProductionHintText(entry), isSuccess: true);
    }

    private static bool ShouldRefreshProductHistoryFromLog(ProductionFlowLogEntry entry)
    {
        return false;
    }

    private string ToProductionHintText(ProductionFlowLogEntry entry)
    {
        return entry.Step switch
        {
            "ProductDataReady" => _localizer.GetString(TextKeys.Monitor.ProductionHint.ProductDataReady),
            "ProductCollectionStart" => _localizer.GetString(TextKeys.Monitor.ProductionHint.ProductCollectionStart),
            "ProductDataReadStart" => _localizer.GetString(TextKeys.Monitor.ProductionHint.ProductDataReadStart),
            "ProductDataSaved" => _localizer.GetString(TextKeys.Monitor.ProductionHint.ProductDataSaved),
            "ProductDataSaveFailed" => _localizer.GetString(TextKeys.Monitor.ProductionHint.ProductDataSaveFailed),
            "ProductCollectionFeedback" => entry.Level.Equals("Error", StringComparison.OrdinalIgnoreCase)
                ? _localizer.GetString(TextKeys.Monitor.ProductionHint.ProductCollectionFeedbackFailed)
                : _localizer.GetString(TextKeys.Monitor.ProductionHint.ProductCollectionFeedbackSucceeded),
            "RecipeCodeWriteSucceeded" => _localizer.GetString(TextKeys.Monitor.ProductionHint.RecipeCodeWriteSucceeded),
            "RecipeCodeWriteFailed" => _localizer.GetString(TextKeys.Monitor.ProductionHint.RecipeCodeWriteFailed),
            "RecipeCodeValidationSucceeded" => _localizer.GetString(TextKeys.Monitor.ProductionHint.RecipeCodeValidationSucceeded),
            "RecipeCodeValidationFailed" => _localizer.GetString(TextKeys.Monitor.ProductionHint.RecipeCodeValidationFailed),
            "BusinessSignalWrite" => entry.Level.Equals("Error", StringComparison.OrdinalIgnoreCase)
                ? _localizer.GetString(TextKeys.Monitor.ProductionHint.BusinessSignalWriteFailed)
                : _localizer.GetString(TextKeys.Monitor.ProductionHint.BusinessSignalWriteSucceeded),
            _ => entry.Summary
        };
    }

    private void ApplyStationResult(BizWeldPointRecord record)
    {
        if (record.StationNo == 2 && !_dualStationEnabled)
        {
            return;
        }

        var resultText = ResolveStationProductResultText(record);
        var tag = record.StationNo == 2 ? tagStation2 : tagStation1;

        UpdateStationResultLayout();
        tag.Text = $"工位{record.StationNo}{resultText}";
        tag.ForeColor = Color.White;
        tag.BackColor = ResolveStationResultColor(resultText);
    }

    private static string ResolveStationProductResultText(BizWeldPointRecord record)
    {
        var rawValues = ParseRawWeldValues(record.RawDataJson);
        var productResult = FindRawValue(rawValues, "product_result");
        if (string.IsNullOrWhiteSpace(productResult)
            || string.Equals(productResult.Trim(), ProductionConstants.TestResults.Unknown, StringComparison.OrdinalIgnoreCase)
            || string.Equals(productResult.Trim(), "--", StringComparison.Ordinal))
        {
            return "--";
        }

        return NormalizeStationResultText(productResult);
    }

    private static Color ResolveStationResultColor(string resultText)
    {
        if (string.Equals(resultText, ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase))
        {
            return UiColors.Status.Success;
        }

        return string.Equals(resultText, ProductionConstants.TestResults.Ng, StringComparison.OrdinalIgnoreCase)
            ? UiColors.Status.Danger
            : UiColors.Status.Muted;
    }

    private static string NormalizeStationResultText(string? rawResult)
    {
        return string.Equals(rawResult?.Trim(), ProductionConstants.TestResults.OkRawValue, StringComparison.Ordinal)
            || string.Equals(rawResult?.Trim(), ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase)
            ? ProductionConstants.TestResults.Ok
            : ProductionConstants.TestResults.Ng;
    }

    private void UpdateCurrentTime()
    {
        lblCurTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void ApplyLocalizedTexts()
    {
        lblTitle.Text = _localizer.GetString(TextKeys.Monitor.Title.AppTitle);
        lblWorkOrder.Text = _localizer.GetString(TextKeys.Monitor.Label.WorkOrderNo);
        lblProgramName.Text = _localizer.GetString(TextKeys.Monitor.Label.ProgramName);
        lblProductNo.Text = _localizer.GetString(TextKeys.Monitor.Label.ProductNo);
        lblProdModel.Text = _localizer.GetString(TextKeys.Monitor.Label.ProductModel);
        lblBatchNo.Text = _localizer.GetString(TextKeys.Monitor.Label.BatchNo);
        lblSpec.Text = _localizer.GetString(TextKeys.Monitor.Label.Spec);
        lblPartName.Text = _localizer.GetString(TextKeys.Monitor.Label.PartName);
        lblDrawingNo.Text = _localizer.GetString(TextKeys.Monitor.Label.DrawingNo);
        lblProcessNo.Text = _localizer.GetString(TextKeys.Monitor.Label.ProcessNo);
        lblProcessName.Text = _localizer.GetString(TextKeys.Monitor.Label.ProcessName);


        lblLiveHint1.Text = "实时采集正常";
        lblLiveHint2.Text = "实时采集正常";
        lblLiveProductNo1.Text = "产品编号：--";
        lblLiveProductNo2.Text = "产品编号：--";
        tagLiveResult1.Text = "产品结果：--";
        tagLiveResult2.Text = "产品结果：--";
        lblLiveTouchNo1.Text = "焊点：--";
        lblLiveTouchNo2.Text = "焊点：--";
        lblLiveHint1.ForeColor = UiColors.Status.Success;
        lblLiveHint2.ForeColor = UiColors.Status.Success;

        btnExpStart.Text = _localizer.GetString(TextKeys.Monitor.Button.StartReport);
        btnExpEnd.Text = _localizer.GetString(TextKeys.Monitor.Button.FinishReport);
        btnLocalWorkOrder.Text = _localizer.GetString(TextKeys.Monitor.Button.LocalWorkOrder);
        btnChangeWO.Text = _localizer.GetString(TextKeys.Monitor.Button.ChangeWorkOrder);
        btnEditWO.Text = _localizer.GetString(TextKeys.Monitor.Button.EditWO);

        grpErrorTips.Text = _localizer.GetString(TextKeys.Monitor.Group.ExceptionTips);
        grpRunningStatus.Text = _localizer.GetString(TextKeys.Monitor.Group.RunningStatus);
        tableMetric1.Text = _localizer.GetString(TextKeys.Monitor.Group.ProductionMetrics);

        dgvPreview1.Text = "实时测试结果";
        dgvPreview2.Text = "实时测试结果";

        SetLiveResultTagColor(tagLiveResult1, UiColors.Status.Muted, Color.White);
        SetLiveResultTagColor(tagLiveResult2, UiColors.Status.Muted, Color.White);
        AdjustHeaderFixedColumns();
    }

    private void ApplyPlcStatus(PlcConnectionSnapshot snapshot)
    {
        RecordPlcStatusChange(snapshot);

        var stationNo = NormalizeStatusStationNo(snapshot.StationNo);
        if (stationNo != CurrentStationNo)
        {
            return;
        }

        tagPLC.Text = $"PLC\r\n{GetLocalizedPlcStateText(snapshot.State)}";
        tagPLC.ForeColor = Color.White;
        tagPLC.BackColor = snapshot.State switch
        {
            PlcConnectionState.Connected => UiColors.Status.Success,
            PlcConnectionState.Connecting => UiColors.Status.Primary,
            PlcConnectionState.Reconnecting => UiColors.Status.Business,
            PlcConnectionState.Disconnected => UiColors.Status.Warning,
            PlcConnectionState.Faulted => UiColors.Status.Danger,
            PlcConnectionState.Stopped => UiColors.Status.Muted,
            _ => UiColors.Status.Danger
        };

        if (!snapshot.IsConnected)
        {
            _lastWorkOrderStatusSnapshots.Remove(stationNo);
            _lastDeviceModeSnapshots.Remove(stationNo);
        }

        if (snapshot.IsConnected && _enableBusinessSignalReconcile)
        {
            QueueBusinessSignalReconciliation("PLC.StatusChanged");
        }

        if (_plcStatusToolTipVisible)
        {
            RefreshPlcStatusToolTip();
        }
    }

    private void QueueBusinessSignalReconciliation(
        string source,
        bool includeDeviceMode = true,
        bool includeWorkOrderStatus = true)
    {
        if (includeDeviceMode)
        {
            _ = ReconcileDeviceModeAsync(source);
        }

        if (includeWorkOrderStatus)
        {
            _ = ReconcileWorkOrderStatusAsync(source);
        }
    }

    private async Task ReconcileDeviceModeAsync(string source)
    {
        if (_deviceModeReconcileRunning)
        {
            return;
        }

        _deviceModeReconcileRunning = true;
        try
        {
            var deviceMode = ResolvePlcDeviceMode();
            foreach (var stationNo in ResolveBusinessSignalReconcileStations())
            {
                if (!_plcCommunicationService.GetCurrent(stationNo).IsConnected)
                {
                    continue;
                }

                await EnsureDeviceModeAsync(
                    stationNo,
                    deviceMode,
                    "PLC.DeviceMode.Reconcile",
                    "Device mode reconcile failed.",
                    source,
                    writeOnReadFailure: false);
            }
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.ReconcileDeviceModeAsync");
        }
        finally
        {
            _deviceModeReconcileRunning = false;
        }
    }

    private async Task ReconcileWorkOrderStatusAsync(string source)
    {
        if (_workOrderStatusReconcileRunning)
        {
            return;
        }

        _workOrderStatusReconcileRunning = true;
        try
        {
            foreach (var stationNo in ResolveBusinessSignalReconcileStations())
            {
                if (!_plcCommunicationService.GetCurrent(stationNo).IsConnected)
                {
                    continue;
                }

                await EnsureWorkOrderStatusAsync(
                    stationNo,
                    ResolveExpectedPlcWorkOrderStatus(stationNo),
                    "PLC.WorkOrderStatus.Reconcile",
                    "Work order status reconcile failed.",
                    source,
                    writeOnReadFailure: false,
                    mirrorWorkOrderStations: false);
            }
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.ReconcileWorkOrderStatusAsync");
        }
        finally
        {
            _workOrderStatusReconcileRunning = false;
        }
    }

    private IReadOnlyList<int> ResolveBusinessSignalReconcileStations()
    {
        var settings = _currentSettings;
        return settings.EnableDualStation
            ? [1, 2]
            : [ProductionConstants.Stations.DefaultStationNo];
    }

    private int ResolveExpectedPlcWorkOrderStatus(int stationNo)
    {
        return _weldTaskService.GetUnfinishedTask(stationNo) is null
            ? ProductionConstants.PlcWorkOrderStatuses.FinishedForbidProduction
            : ProductionConstants.PlcWorkOrderStatuses.StartedAllowProduction;
    }

    private static string GetPlcStateKey(PlcConnectionState state)
    {
        return state switch
        {
            PlcConnectionState.Connecting => TextKeys.Plc.StateConnecting,
            PlcConnectionState.Connected => TextKeys.Plc.StateConnected,
            PlcConnectionState.Reconnecting => TextKeys.Plc.StateReconnecting,
            PlcConnectionState.Disconnected => TextKeys.Plc.StateDisconnected,
            PlcConnectionState.Faulted => TextKeys.Plc.StateFaulted,
            _ => TextKeys.Plc.StateStopped
        };
    }

    /// <summary>
    /// MES connectivity is judged by the MES monitor service; the view only maps it to color and text.
    /// </summary>
    private void ApplyMesStatus(MesConnectionSnapshot snapshot)
    {
        tagMes.Text = $"MES\r\n{_localizer.GetString(GetMesStateKey(snapshot))}";
        tagMes.ForeColor = Color.White;
        tagMes.BackColor = snapshot.UpdatedTime == default
            ? UiColors.Status.Warning
            : snapshot.IsConnected
                ? UiColors.Status.Success
                : UiColors.Status.Danger;
        ApplyMesDependentButtonState(snapshot);
        if (snapshot.IsConnected && !_lastMesConnected && _enableBusinessSignalReconcile)
        {
            QueuePendingUploadRetry();
        }

        _lastMesConnected = snapshot.IsConnected;
    }

    private void ApplyMesDependentButtonState(MesConnectionSnapshot snapshot)
    {
        if (_stationViewReadOnly)
        {
            ApplyOperationMode();
            return;
        }

        var isOnline = snapshot.IsConnected;
        btnGetWO.Enabled = isOnline;
        btnExpStart.Enabled = isOnline;
        btnExpEnd.Enabled = isOnline;
        btnLocalWorkOrder.Enabled = !isOnline;
    }

    private void QueuePendingUploadRetry()
    {
        if (_pendingUploadRetryRunning)
        {
            return;
        }

        _pendingUploadRetryRunning = true;
        _ = Task.Run(async () =>
        {
            try
            {
                await _weldTaskService.RetryPendingUploadsAsync();
            }
            catch (Exception ex)
            {
                _exceptionLogService.Write(ex, "MonitorView.QueuePendingUploadRetry");
            }
            finally
            {
                _pendingUploadRetryRunning = false;
            }
        });
    }

    private static string GetMesStateKey(MesConnectionSnapshot snapshot)
    {
        if (snapshot.UpdatedTime == default)
        {
            return TextKeys.Mes.StateChecking;
        }

        return snapshot.IsConnected
            ? TextKeys.Mes.StateConnected
            : TextKeys.Mes.StateDisconnected;
    }

    /// <summary>
    /// Refreshes device state and production metrics from the latest PLC production snapshot.
    /// </summary>
    private void ApplyProductionStatus(PlcProductionSnapshot snapshot)
    {
        if (NormalizeStatusStationNo(snapshot.StationNo) != CurrentStationNo)
        {
            return;
        }

        ApplyDeviceStatus(snapshot);
        BindProductionMetrics(snapshot);
    }

    private void ApplyDeviceStatus(PlcProductionSnapshot snapshot)
    {
        var stateKey = GetDeviceStatusKey(snapshot.DeviceStatusCode);
        var stateText = _localizer.GetString(stateKey);

        // The dynamic state is placed first so it stays visible even if the Tag only paints one line.
        tagDeviceStatus.Text = $"{stateText}\r\n{_localizer.GetString(TextKeys.Monitor.Label.DeviceStatus)}";
        tagDeviceStatus.ForeColor = Color.White;
        tagDeviceStatus.BackColor = GetDeviceStatusColor(snapshot.DeviceStatusCode, snapshot.IsSuccess);

        if (!snapshot.IsSuccess && !string.IsNullOrWhiteSpace(snapshot.Message))
        {
            SetRuntimeError(TextKeys.Monitor.RuntimeError.ProductionCollectFailed);
        }
    }

    private void BindProductionMetrics(PlcProductionSnapshot snapshot)
    {
        var mesProductionQuantity = GetCurrentStationState().SelectedProcess?.StartAmount;
        var acceptedRate = CalculateRate(snapshot.AcceptedQuantity, snapshot.TotalProduction);
        var rejectedRate = CalculateRate(snapshot.RejectedQuantity, snapshot.TotalProduction);
        var achievementRate = mesProductionQuantity.GetValueOrDefault() > 0
            ? CalculateRate(snapshot.TotalProduction, mesProductionQuantity!.Value)
            : null;

        var rows = new List<ProductionMetricRow>
        {
            new(_localizer.GetString(TextKeys.Production.TotalProduction), snapshot.TotalProduction.ToString()),
            new(_localizer.GetString(TextKeys.Production.AcceptedQuantity), snapshot.AcceptedQuantity.ToString()),
            new(_localizer.GetString(TextKeys.Production.RejectedQuantity), snapshot.RejectedQuantity.ToString()),
            new(_localizer.GetString(TextKeys.Production.AcceptedRate), FormatRate(acceptedRate)),
            new(_localizer.GetString(TextKeys.Production.RejectedRate), FormatRate(rejectedRate)),
            //new(_localizer.GetString(TextKeys.Production.MesProductionQuantity), FormatNullable(mesProductionQuantity)),
            new(_localizer.GetString(TextKeys.Production.AchievementRate), FormatRate(achievementRate))
        };

        var metricTable = CurrentMetricTable;
        metricTable.DataSource = rows;
        metricTable.Refresh();
    }

    private void ConfigureProductionTableColumns()
    {
        ConfigureProductionTableColumns(tableMetric1);
        ConfigureProductionTableColumns(tableMetric2);
    }

    private void ConfigureProductionTableColumns(AntdUI.Table table)
    {
        table.Columns.Clear();
        table.Columns.Add(new AntdUI.Column(nameof(ProductionMetricRow.Name), _localizer.GetString(TextKeys.Production.MetricName))
        {
            Ellipsis = true
        });
        table.Columns.Add(new AntdUI.Column(nameof(ProductionMetricRow.Value), _localizer.GetString(TextKeys.Production.MetricValue))
        {
            Ellipsis = true
        });
        TableStyleHelper.ApplyAntdColumnDefaults(table);
    }

    private void ConfigureProductHistoryTableColumns()
    {
        ConfigureProductHistoryTableColumns(tableProductHistoryPreview1, [], 1);
        ConfigureProductHistoryTableColumns(tableProductHistoryPreview2, [], 2);
    }

    private void ConfigureProductHistoryTableColumns(
        AntdUI.Table table,
        IReadOnlyList<ProductHistoryDynamicColumn> dynamicColumns,
        int stationNo)
    {
        var schemaKey = BuildProductHistorySchemaKey(dynamicColumns);
        if (_productHistorySchemaKeys.TryGetValue(stationNo, out var existingSchemaKey)
            && string.Equals(existingSchemaKey, schemaKey, StringComparison.Ordinal)
            && table.Columns.Count > 0)
        {
            return;
        }

        table.Columns.Clear();

        var nodeColumn = new AntdUI.Column(nameof(ProductHistoryTableRow.NodeText), "产品/焊点")
        {
            Align = AntdUI.ColumnAlign.Left,
            ColAlign = AntdUI.ColumnAlign.Center,
            Ellipsis = true
        };
        nodeColumn.SetTree(nameof(ProductHistoryTableRow.Children));

        table.Columns.Add(nodeColumn);
        table.Columns.Add(CreateProductHistoryColumn(nameof(ProductHistoryTableRow.ProductNo), "产品编号"));
        table.Columns.Add(CreateProductHistoryColumn(nameof(ProductHistoryTableRow.TouchNo), "焊点"));
        table.Columns.Add(CreateProductHistoryColumn(nameof(ProductHistoryTableRow.ResultText), "结果"));
        table.Columns.Add(CreateProductHistoryColumn(nameof(ProductHistoryTableRow.UploadStatusText), "上传状态"));
        table.Columns.Add(CreateProductHistoryColumn(nameof(ProductHistoryTableRow.IsTestText), "试焊件"));
        table.Columns.Add(CreateProductHistoryColumn(nameof(ProductHistoryTableRow.TouchCountText), "焊点数"));
        table.Columns.Add(CreateProductHistoryColumn(nameof(ProductHistoryTableRow.RecordTimeText), "采集时间"));
        foreach (var dynamicColumn in dynamicColumns)
        {
            table.Columns.Add(CreateProductHistoryDynamicColumn(dynamicColumn));
        }

        TableStyleHelper.ApplyAntdColumnDefaults(table);
        nodeColumn.Align = AntdUI.ColumnAlign.Left;
        _productHistorySchemaKeys[stationNo] = schemaKey;
    }

    private static AntdUI.Column CreateProductHistoryColumn(string key, string title)
    {
        return new AntdUI.Column(key, title)
        {
            Align = AntdUI.ColumnAlign.Center,
            ColAlign = AntdUI.ColumnAlign.Center,
            Ellipsis = true,
            ReadOnly = true
        };
    }

    private static AntdUI.Column CreateProductHistoryDynamicColumn(ProductHistoryDynamicColumn dynamicColumn)
    {
        var key = dynamicColumn.Key;
        return new AntdUI.Column(key, dynamicColumn.Title)
        {
            Align = AntdUI.ColumnAlign.Center,
            ColAlign = AntdUI.ColumnAlign.Center,
            Ellipsis = true,
            ReadOnly = true,
            Render = (value, record, rowIndex) => record is ProductHistoryTableRow row
                && row.DynamicValues.TryGetValue(key, out var dynamicValue)
                    ? dynamicValue
                    : string.Empty
        };
    }

    private void ConfigureWeldParameterTableColumns()
    {
        dgvPreview1.AutoGenerateColumns = false;
        dgvPreview2.AutoGenerateColumns = false;
        _weldParameterLayoutKey = string.Empty;
        _weldParameterPreviewSchemaKey = string.Empty;
        _weldParameterVisibleValueKey = string.Empty;
        _weldParameterTableBound = false;
        BindWeldParameterTable(forceRebind: true);
    }

    private void RefreshProductHistoryPreview()
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        // InvokeRequired can return false before a handle is created. Comparing the
        // creating thread explicitly prevents background services from mutating the
        // AntdUI column collection while the control is being initialized or painted.
        if (Environment.CurrentManagedThreadId != _uiThreadId)
        {
            PostProductHistoryRefreshToUiThread();
            return;
        }

        if (_refreshingProductHistoryPreview)
        {
            _productHistoryRefreshPending = true;
            return;
        }

        do
        {
            _productHistoryRefreshPending = false;
            _refreshingProductHistoryPreview = true;
            try
            {
                RefreshProductHistoryPreviewCore();
            }
            finally
            {
                _refreshingProductHistoryPreview = false;
            }
        }
        while (_productHistoryRefreshPending && !IsDisposed && !Disposing);
    }

    /// <summary>
    /// Coalesces product-history refresh requests raised by background services.
    /// </summary>
    private void PostProductHistoryRefreshToUiThread()
    {
        if (!IsHandleCreated || Interlocked.Exchange(ref _productHistoryRefreshPosted, 1) == 1)
        {
            return;
        }

        try
        {
            BeginInvoke(new Action(() =>
            {
                Interlocked.Exchange(ref _productHistoryRefreshPosted, 0);
                RefreshProductHistoryPreview();
            }));
        }
        catch (InvalidOperationException)
        {
            Interlocked.Exchange(ref _productHistoryRefreshPosted, 0);
        }
    }

    /// <summary>
    /// Loads and binds the current station history. This method runs only on the UI thread.
    /// </summary>
    private void RefreshProductHistoryPreviewCore()
    {
        try
        {
            var activeTask = GetCurrentStationState().ActiveTask;
            if (activeTask is null)
            {
                ConfigureProductHistoryTableColumns(CurrentProductHistoryTable, [], CurrentStationNo);
                BindProductHistoryRows(CurrentProductHistoryTable, []);
                return;
            }

            var snapshot = _productHistoryService.GetSnapshot(activeTask.Id, CurrentStationNo);
            BindProductHistorySnapshot(snapshot, activeTask);
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.RefreshProductHistoryPreview");
        }
    }

    private void BindProductHistorySnapshot(ProductHistorySnapshot snapshot, BizWeldTask activeTask)
    {
        var table = GetProductHistoryTable(snapshot.StationNo);
        var dynamicColumns = ResolveProductHistoryDynamicColumns(activeTask, snapshot);
        ConfigureProductHistoryTableColumns(table, dynamicColumns, snapshot.StationNo);
        var rows = snapshot.Products
            .Select(product => ToProductHistoryRow(product, dynamicColumns))
            .ToList();

        BindProductHistoryRows(table, rows);
    }

    private void BindProductHistoryRows(AntdUI.Table table, IReadOnlyList<ProductHistoryTableRow> rows)
    {
        table.DataSource = rows;
        table.ExpandAll(false);
        table.Invalidate();
    }

    private AntdUI.Table GetProductHistoryTable(int stationNo)
    {
        return stationNo == 2 ? tableProductHistoryPreview2 : tableProductHistoryPreview1;
    }

    private void ProductHistoryTable_CellClick(object sender, AntdUI.TableClickEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.Record is not ProductHistoryTableRow row)
        {
            return;
        }

        ShowProductHistoryContextMenu((Control)sender, row);
    }

    private void ShowProductHistoryContextMenu(Control target, ProductHistoryTableRow row)
    {
        if (_stationViewReadOnly)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(row.ProductNo))
        {
            return;
        }

        var targetFlag = !row.IsTest;
        var menuItem = new AntdUI.ContextMenuStripItem
        {
            ID = targetFlag ? "mark-test-product" : "unmark-test-product",
            Text = targetFlag ? "标记为试焊件" : "取消试焊件",
            SubText = row.CanMarkTest ? string.Empty : row.MarkDisabledReason,
            Enabled = row.CanMarkTest,
            Tag = row
        };

        AntdUI.ContextMenuStrip.open(
            target,
            item =>
            {
                if (item.Tag is ProductHistoryTableRow selectedRow)
                {
                    SetProductHistoryTestFlag(selectedRow, targetFlag);
                }
            },
            new AntdUI.IContextMenuStripItem[] { menuItem },
            0);
    }

    private void SetProductHistoryTestFlag(ProductHistoryTableRow row, bool isTest)
    {
        try
        {
            var result = _productHistoryService.SetProductTestFlag(row.TaskId, row.StationNo, row.ProductNo, isTest);
            RefreshProductHistoryPreview();

            if (!result.IsSuccess)
            {
                ShowWarningText(result.Message);
                return;
            }

            ClearRuntimeError();
            SetRuntimeStatusText(result.Message, isSuccess: true);
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.SetProductHistoryTestFlag");
            ShowWarningText("试焊件标记失败，请查看异常日志。");
        }
    }

    private ProductHistoryTableRow ToProductHistoryRow(
        ProductHistoryProduct product,
        IReadOnlyList<ProductHistoryDynamicColumn> dynamicColumns)
    {
        return new ProductHistoryTableRow
        {
            IsProductRow = true,
            TaskId = product.TaskId,
            StationNo = product.StationNo,
            ProductNo = product.ProductNo,
            NodeText = $"产品 {product.ProductNo}",
            ResultText = FormatHistoryResult(product.Result),
            UploadStatusText = FormatHistoryUploadStatus(product.UploadStatus),
            IsTest = product.IsTest,
            IsTestText = FormatHistoryTestFlag(product.IsTest),
            TouchCountText = product.TouchCount.ToString(CultureInfo.InvariantCulture),
            RecordTimeText = FormatHistoryTime(product.LastRecordTime),
            CanMarkTest = product.CanMarkTest,
            MarkDisabledReason = product.MarkDisabledReason,
            Children = product.Points.Select(point => ToProductHistoryPointRow(product, point, dynamicColumns)).ToList()
        };
    }

    private ProductHistoryTableRow ToProductHistoryPointRow(
        ProductHistoryProduct product,
        ProductHistoryPoint point,
        IReadOnlyList<ProductHistoryDynamicColumn> dynamicColumns)
    {
        return new ProductHistoryTableRow
        {
            IsProductRow = false,
            TaskId = product.TaskId,
            StationNo = product.StationNo,
            ProductNo = product.ProductNo,
            TouchNo = point.TouchNo,
            NodeText = $"焊点 {point.TouchNo}",
            ResultText = FormatHistoryResult(point.Result),
            UploadStatusText = FormatHistoryUploadStatus(point.UploadStatus),
            IsTest = point.IsTest,
            IsTestText = FormatHistoryTestFlag(point.IsTest),
            RecordTimeText = FormatHistoryTime(point.RecordTime),
            DynamicValues = BuildProductHistoryDynamicValues(point, dynamicColumns),
            CanMarkTest = product.CanMarkTest,
            MarkDisabledReason = product.MarkDisabledReason
        };
    }

    private Dictionary<string, string> BuildProductHistoryDynamicValues(
        ProductHistoryPoint point,
        IReadOnlyList<ProductHistoryDynamicColumn> dynamicColumns)
    {
        var rawValues = ParseRawWeldValues(point.RawDataJson);
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in dynamicColumns)
        {
            var rawValue = FindProductHistoryDynamicRawValueForHistory(point, rawValues, column);
            values[column.Key] = column.Role == PreviewResultRole
                ? FormatTestResultText(rawValue)
                : DisplayPreviewValue(rawValue);
        }

        return values;
    }

    private static string? FindProductHistoryDynamicRawValueForHistory(
        ProductHistoryPoint point,
        IReadOnlyDictionary<string, string> rawValues,
        ProductHistoryDynamicColumn column)
    {
        var rawValue = column.Role switch
        {
            PreviewUpperRole => FindRawValue(rawValues, $"{column.ItemKey}_upper", $"{column.ItemName}上限"),
            PreviewLowerRole => FindRawValue(rawValues, $"{column.ItemKey}_lower", $"{column.ItemName}下限"),
            PreviewActualRole => FindRawValue(rawValues, column.ItemKey, column.ItemName),
            PreviewResultRole => FindRawValue(rawValues, $"{column.ItemKey}_result", $"{column.ItemName}结果"),
            _ => null
        };

        return rawValue ?? FindProductHistoryFixedValue(point, column);
    }

    private static string? FindProductHistoryFixedValue(ProductHistoryPoint point, ProductHistoryDynamicColumn column)
    {
        var itemKey = ResolveProductHistoryKnownItemKeyForHistory(column.ItemKey, column.ItemName);
        if (column.Role == PreviewResultRole)
        {
            return point.Result;
        }

        if (column.Role != PreviewActualRole)
        {
            return null;
        }

        return itemKey switch
        {
            "max_electric" => point.MaxElectric,
            "max_voltage" => point.MaxVoltage,
            "valid_power" => point.ValidPower,
            "displacement" => point.Displacement,
            "weld_ts" => point.WeldTs,
            _ => null
        };
    }

    private IReadOnlyList<ProductHistoryDynamicColumn> ResolveProductHistoryDynamicColumns(
        BizWeldTask activeTask,
        ProductHistorySnapshot snapshot)
    {
        var stationNo = snapshot.StationNo;
        var config = ResolveProductHistoryProcessConfig(activeTask, stationNo);
        var schemeColumns = config is null
            ? Array.Empty<ProductHistoryDynamicColumn>()
            : ResolveSchemeItems(config.SchemeId)
                .SelectMany(CreateProductHistoryDynamicColumnsFromScheme)
                .OrderBy(column => column.Sort)
                .ThenBy(column => column.Title, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (schemeColumns.Length > 0)
        {
            return schemeColumns;
        }

        var realtimeColumns = ResolveProductHistoryDynamicColumnsFromRealtimePreview(stationNo);
        return realtimeColumns.Count > 0
            ? realtimeColumns
            : ResolveProductHistoryDynamicColumnsFromSnapshot(snapshot);
    }

    private BizProductProcessConfig? ResolveProductHistoryProcessConfig(BizWeldTask activeTask, int stationNo)
    {
        var config = _productProcessConfigService.FindActiveForTask(activeTask, stationNo);
        if (config is not null)
        {
            return config;
        }

        var currentIdentity = _currentProductIdentity;
        if (currentIdentity is null
            || currentIdentity.StationNo != stationNo
            || string.IsNullOrWhiteSpace(currentIdentity.ProductNum))
        {
            return null;
        }

        return _productProcessConfigService.FindActive(currentIdentity.ProductNum, stationNo);
    }

    private IReadOnlyList<ProductHistoryDynamicColumn> ResolveProductHistoryDynamicColumnsFromRealtimePreview(int stationNo)
    {
        if (CurrentStationNo != stationNo)
        {
            return Array.Empty<ProductHistoryDynamicColumn>();
        }

        return ResolveWeldPreviewItems(_weldParameterRows.Where(row => row.StationNo == stationNo))
            .SelectMany(CreateProductHistoryDynamicColumns)
            .OrderBy(column => column.Sort)
            .ThenBy(column => column.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<ProductHistoryDynamicColumn> ResolveProductHistoryDynamicColumnsFromSnapshot(ProductHistorySnapshot snapshot)
    {
        var candidates = new Dictionary<string, ProductHistoryRawColumnCandidate>(StringComparer.OrdinalIgnoreCase);
        var sort = 0;
        foreach (var point in snapshot.Products.SelectMany(product => product.Points))
        {
            var rawValues = ParseRawWeldValues(point.RawDataJson);
            foreach (var key in rawValues.Keys)
            {
                if (!TryResolveProductHistoryRawColumnForHistory(key, out var itemKey, out var itemName, out var role))
                {
                    continue;
                }

                if (!candidates.TryGetValue(itemKey, out var candidate))
                {
                    candidate = new ProductHistoryRawColumnCandidate(itemKey, itemName, sort += 10);
                    candidates[itemKey] = candidate;
                }

                candidate.EnableRole(role);
            }

        }

        return candidates.Values
            .OrderBy(candidate => candidate.Sort)
            .ThenBy(candidate => candidate.ItemName, StringComparer.OrdinalIgnoreCase)
            .SelectMany(CreateProductHistoryDynamicColumns)
            .ToList();
    }

    private static bool TryResolveProductHistoryRawColumnForHistory(
        string rawKey,
        out string itemKey,
        out string itemName,
        out string role)
    {
        itemKey = string.Empty;
        itemName = string.Empty;
        role = PreviewActualRole;
        var key = rawKey.Trim();
        if (string.IsNullOrWhiteSpace(key) || IsProductHistoryRawKeyIgnored(key))
        {
            return false;
        }

        var baseName = key;
        if (key.EndsWith("_upper", StringComparison.OrdinalIgnoreCase))
        {
            baseName = key[..^"_upper".Length];
            role = PreviewUpperRole;
        }
        else if (key.EndsWith("_lower", StringComparison.OrdinalIgnoreCase))
        {
            baseName = key[..^"_lower".Length];
            role = PreviewLowerRole;
        }
        else if (key.EndsWith("_result", StringComparison.OrdinalIgnoreCase))
        {
            baseName = key[..^"_result".Length];
            role = PreviewResultRole;
        }
        else if (key.EndsWith("上限", StringComparison.Ordinal))
        {
            baseName = key[..^"上限".Length];
            role = PreviewUpperRole;
        }
        else if (key.EndsWith("下限", StringComparison.Ordinal))
        {
            baseName = key[..^"下限".Length];
            role = PreviewLowerRole;
        }
        else if (key.EndsWith("结果", StringComparison.Ordinal))
        {
            baseName = key[..^"结果".Length];
            role = PreviewResultRole;
        }

        if (string.IsNullOrWhiteSpace(baseName) || IsProductHistoryRawKeyIgnored(baseName))
        {
            return false;
        }

        itemKey = ResolveProductHistoryKnownItemKeyForHistory(baseName, baseName);
        itemName = ResolveProductHistoryItemNameForHistory(itemKey, baseName);
        return true;
    }

    private static bool IsProductHistoryRawKeyIgnored(string key)
    {
        return string.Equals(key, "product_result", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "test_result", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "test_result_raw", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "TestResult", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "Result", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveProductHistoryKnownItemKeyForHistory(string itemKey, string? itemName)
    {
        var normalized = (itemKey ?? string.Empty).Trim();
        if (IsKnownProductHistoryItemKey(normalized))
        {
            return normalized;
        }

        return (itemName ?? string.Empty).Trim() switch
        {
            "峰值电流" => "max_electric",
            "峰值电压" => "max_voltage",
            "有效功率" => "valid_power",
            "位移" => "displacement",
            "焊接时间" => "weld_ts",
            _ => normalized
        };
    }

    private static string ResolveProductHistoryItemNameForHistory(string itemKey, string fallbackName)
    {
        return itemKey switch
        {
            "max_electric" => "峰值电流",
            "max_voltage" => "峰值电压",
            "valid_power" => "有效功率",
            "displacement" => "位移",
            "weld_ts" => "焊接时间",
            _ => fallbackName.Trim()
        };
    }

    private static string ResolveProductHistoryKnownItemKey(string itemKey, string? itemName)
    {
        var normalized = (itemKey ?? string.Empty).Trim();
        if (IsKnownProductHistoryItemKey(normalized))
        {
            return normalized;
        }

        return (itemName ?? string.Empty).Trim() switch
        {
            "峰值电流" => "max_electric",
            "峰值电压" => "max_voltage",
            "有效功率" => "valid_power",
            "位移" => "displacement",
            "焊接时间" => "weld_ts",
            _ => normalized
        };
    }

    private static bool IsKnownProductHistoryItemKey(string itemKey)
    {
        return string.Equals(itemKey, "max_electric", StringComparison.OrdinalIgnoreCase)
            || string.Equals(itemKey, "max_voltage", StringComparison.OrdinalIgnoreCase)
            || string.Equals(itemKey, "valid_power", StringComparison.OrdinalIgnoreCase)
            || string.Equals(itemKey, "displacement", StringComparison.OrdinalIgnoreCase)
            || string.Equals(itemKey, "weld_ts", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveProductHistoryItemName(string itemKey, string fallbackName)
    {
        return itemKey switch
        {
            "max_electric" => "峰值电流",
            "max_voltage" => "峰值电压",
            "valid_power" => "有效功率",
            "displacement" => "位移",
            "weld_ts" => "焊接时间",
            _ => fallbackName.Trim()
        };
    }

    private static IEnumerable<ProductHistoryDynamicColumn> CreateProductHistoryDynamicColumns(ProductHistoryRawColumnCandidate candidate)
    {
        if (candidate.EnableUpper)
        {
            yield return CreateProductHistoryDynamicColumn(
                candidate.ItemKey,
                candidate.ItemName,
                PreviewUpperRole,
                $"{candidate.ItemName}上限",
                candidate.Sort + 1);
        }

        if (candidate.EnableLower)
        {
            yield return CreateProductHistoryDynamicColumn(
                candidate.ItemKey,
                candidate.ItemName,
                PreviewLowerRole,
                $"{candidate.ItemName}下限",
                candidate.Sort + 2);
        }

        if (candidate.EnableActual)
        {
            yield return CreateProductHistoryDynamicColumn(
                candidate.ItemKey,
                candidate.ItemName,
                PreviewActualRole,
                $"{candidate.ItemName}实际值",
                candidate.Sort + 3);
        }

        if (candidate.EnableResult)
        {
            yield return CreateProductHistoryDynamicColumn(
                candidate.ItemKey,
                candidate.ItemName,
                PreviewResultRole,
                $"{candidate.ItemName}结果",
                candidate.Sort + 4);
        }
    }

    private static IEnumerable<ProductHistoryDynamicColumn> CreateProductHistoryDynamicColumns(WeldPreviewItem previewItem)
    {
        if (previewItem.EnableUpper)
        {
            yield return CreateProductHistoryDynamicColumn(
                previewItem.Key,
                previewItem.Name,
                PreviewUpperRole,
                $"{previewItem.Name}上限",
                previewItem.Sort + 1);
        }

        if (previewItem.EnableLower)
        {
            yield return CreateProductHistoryDynamicColumn(
                previewItem.Key,
                previewItem.Name,
                PreviewLowerRole,
                $"{previewItem.Name}下限",
                previewItem.Sort + 2);
        }

        if (previewItem.EnableActual)
        {
            yield return CreateProductHistoryDynamicColumn(
                previewItem.Key,
                previewItem.Name,
                PreviewActualRole,
                $"{previewItem.Name}实际值",
                previewItem.Sort + 3);
        }

        if (previewItem.EnableResult)
        {
            yield return CreateProductHistoryDynamicColumn(
                previewItem.Key,
                previewItem.Name,
                PreviewResultRole,
                $"{previewItem.Name}结果",
                previewItem.Sort + 4);
        }
    }

    private static IEnumerable<ProductHistoryDynamicColumn> CreateProductHistoryDynamicColumnsFromScheme(SchemePreviewItem schemeItem)
    {
        var item = schemeItem.Item;
        var detail = schemeItem.Detail;
        var itemKey = ResolveItemKey(item);
        var itemName = item.ItemName?.Trim() ?? itemKey;

        if (detail.EnableUpper)
        {
            yield return CreateProductHistoryDynamicColumn(itemKey, itemName, PreviewUpperRole, $"{itemName}上限", schemeItem.Sort + 1);
        }

        if (detail.EnableLower)
        {
            yield return CreateProductHistoryDynamicColumn(itemKey, itemName, PreviewLowerRole, $"{itemName}下限", schemeItem.Sort + 2);
        }

        if (detail.EnableActual)
        {
            yield return CreateProductHistoryDynamicColumn(itemKey, itemName, PreviewActualRole, $"{itemName}实际值", schemeItem.Sort + 3);
        }

        if (detail.EnableResult)
        {
            yield return CreateProductHistoryDynamicColumn(itemKey, itemName, PreviewResultRole, $"{itemName}结果", schemeItem.Sort + 4);
        }
    }

    private static ProductHistoryDynamicColumn CreateProductHistoryDynamicColumn(
        string itemKey,
        string itemName,
        string role,
        string title,
        int sort)
    {
        return new ProductHistoryDynamicColumn(
            $"{itemKey}_{role}",
            title,
            itemKey,
            itemName,
            role,
            sort);
    }

    private static string BuildProductHistorySchemaKey(IReadOnlyList<ProductHistoryDynamicColumn> dynamicColumns)
    {
        return dynamicColumns.Count == 0
            ? "base"
            : string.Join("|", dynamicColumns.Select(column => $"{column.Key}:{column.Title}:{column.Role}:{column.Sort}"));
    }

    private static string FormatHistoryUploadStatus(string status)
    {
        return status switch
        {
            ProductionConstants.UploadStatuses.Pending => "待上传",
            ProductionConstants.UploadStatuses.Uploading => "上传中",
            ProductionConstants.UploadStatuses.Uploaded => "已上传",
            ProductionConstants.UploadStatuses.Failed => "上传失败",
            ProductionConstants.UploadStatuses.Retrying => "重试中",
            ProductionConstants.UploadStatuses.Skipped => "已跳过",
            _ => string.IsNullOrWhiteSpace(status) ? "--" : status
        };
    }

    private static string FormatHistoryResult(string result)
    {
        return string.IsNullOrWhiteSpace(result) || string.Equals(result, ProductionConstants.TestResults.Unknown, StringComparison.OrdinalIgnoreCase)
            ? "--"
            : result;
    }

    private static string FormatHistoryTestFlag(bool isTest)
    {
        return isTest ? "试焊件" : "--";
    }

    private static string FormatHistoryTime(DateTime? time)
    {
        return time?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "--";
    }

    private void BindWeldParameterRows(BizWeldPointRecord record)
    {
        var rawValues = ParseRawWeldValues(record.RawDataJson);
        var touchIndex = ParsePositiveInt(record.TouchNo);
        var structureChanged = false;
        var matchedRows = _weldParameterRows
            .Where(row => row.StationNo == record.StationNo && row.TouchIndex == touchIndex)
            .ToList();

        if (matchedRows.Count == 0)
        {
            _weldParameterRows.AddRange(BuildFallbackWeldParameterRows(record, rawValues));
            structureChanged = true;
        }
        else
        {
            foreach (var row in matchedRows)
            {
                row.ProductNo = record.ProductNo;
                row.TouchResult = FormatTestResultText(record.TestResult);
                row.UpperValue = FormatNullableText(FindRawValue(rawValues, $"{row.ItemKey}_upper", $"{row.ParameterName}上限"));
                row.LowerValue = FormatNullableText(FindRawValue(rawValues, $"{row.ItemKey}_lower", $"{row.ParameterName}下限"));
                row.Result = FormatTestResultText(FindRecordResult(record, row, rawValues));
                row.RecordTime = record.Ts.ToString("HH:mm:ss");
            }
        }

        SortWeldParameterRows();
        BindWeldParameterTable(forceRebind: structureChanged);
    }

    private void BindWeldParameterTable(bool forceRebind = false)
    {
        if (forceRebind || !_weldParameterTableBound)
        {
            RebuildWeldParameterPreviewTable();
            return;
        }

        RefreshWeldParameterTable();
    }

    /// <summary>
    /// Updates table2 values without rebuilding columns or rebinding data.
    /// </summary>
    private void RefreshWeldParameterTable()
    {
        RefreshWeldParameterRows();
        _weldParameterLayoutKey = BuildWeldPreviewLayoutKey(_weldParameterRows);
        _weldParameterVisibleValueKey = BuildWeldPreviewVisibleValueKey(_weldParameterRows);
    }

    /// <summary>
    /// Updates the unbound DataGridView preview from the cached realtime rows.
    /// The preview is pivoted by weld point, so changed detail rows are merged into the same visible grid.
    /// </summary>
    private void RefreshWeldParameterRows()
    {
        if (!_weldParameterTableBound)
        {
            BindWeldParameterTable(forceRebind: true);
            return;
        }

        var items = ResolveWeldPreviewItems(_weldParameterRows);
        var layoutKey = BuildWeldPreviewLayoutKey(_weldParameterRows);
        if (!string.Equals(layoutKey, _weldParameterLayoutKey, StringComparison.Ordinal))
        {
            BindWeldParameterTable(forceRebind: true);
            return;
        }

        if (IsInfoPreview(items))
        {
            if (EnsureInfoPreviewRows())
            {
                BindWeldParameterTable(forceRebind: true);
                return;
            }

            FillInfoPreviewRows();
            return;
        }

        var touchGroups = ResolvePreviewTouchGroups(_weldParameterRows);
        if (CurrentWeldPreviewGrid.Rows.Count != touchGroups.Count)
        {
            BindWeldParameterTable(forceRebind: true);
            return;
        }

        FillWeldPreviewRows(items, touchGroups);
    }

    /// <summary>
    /// Rebuilds the unbound pivot table: one row per weld point and one column group per test item.
    /// </summary>
    private void RebuildWeldParameterPreviewTable()
    {
        var items = ResolveWeldPreviewItems(_weldParameterRows);
        var grid = CurrentWeldPreviewGrid;
        SetControlRedraw(grid, enabled: false);
        grid.SuspendLayout();
        try
        {
            grid.Rows.Clear();
            grid.Columns.Clear();

            AddWeldPreviewColumn(PreviewTouchNoColumn, "焊点序号", 86);
            AddWeldPreviewColumn(PreviewTouchResultColumn, "焊点结果", 86);
            if (IsInfoPreview(items))
            {
                AddWeldPreviewColumn(PreviewMessageColumn, "提示", 360);
                FillInfoPreviewRows();
            }
            else
            {
                foreach (var item in items)
                {
                    if (item.EnableUpper)
                    {
                        AddWeldPreviewColumn(BuildPreviewColumnName(item.Index, PreviewUpperRole), $"{item.Name}上限", 118);
                    }

                    if (item.EnableLower)
                    {
                        AddWeldPreviewColumn(BuildPreviewColumnName(item.Index, PreviewLowerRole), $"{item.Name}下限", 118);
                    }

                    if (item.EnableActual)
                    {
                        AddWeldPreviewColumn(BuildPreviewColumnName(item.Index, PreviewActualRole), $"{item.Name}实际值", 136);
                    }

                    if (item.EnableResult)
                    {
                        AddWeldPreviewColumn(BuildPreviewColumnName(item.Index, PreviewResultRole), $"{item.Name}结果", 118);
                    }
                }

                FillWeldPreviewRows(items, ResolvePreviewTouchGroups(_weldParameterRows));
            }

            _weldParameterLayoutKey = BuildWeldPreviewLayoutKey(_weldParameterRows);
            _weldParameterPreviewSchemaKey = BuildWeldPreviewSchemaKey(items);
            _weldParameterVisibleValueKey = BuildWeldPreviewVisibleValueKey(_weldParameterRows);
            _weldParameterTableBound = true;
        }
        finally
        {
            grid.ResumeLayout(false);
            SetControlRedraw(grid, enabled: true);
            RedrawControl(grid);
            SyncWeldPreviewHorizontalScrollBar();
        }
    }

    private static void SetControlRedraw(Control control, bool enabled)
    {
        if (!control.IsHandleCreated)
        {
            return;
        }

        SendMessage(control.Handle, WmSetRedraw, enabled ? new IntPtr(1) : IntPtr.Zero, IntPtr.Zero);
    }

    private static void RedrawControl(Control control)
    {
        control.Invalidate();
        control.Update();
    }

    /// <summary>
    /// Sets the right panel to its configured minimum width before the first visible paint.
    /// This runs once during load so later manual splitter adjustments are preserved.
    /// </summary>
    private void SetVerticalSplitterPanel2ToMinWidth()
    {
        if (IsDisposed || !VerticalSplitter.IsHandleCreated || VerticalSplitter.Width <= 0)
        {
            return;
        }

        var minDistance = Math.Max(0, VerticalSplitter.Panel1MinSize);
        var maxDistance = Math.Max(0, VerticalSplitter.Width - VerticalSplitter.SplitterWidth - VerticalSplitter.Panel2MinSize);
        var targetDistance = VerticalSplitter.Width - VerticalSplitter.SplitterWidth - VerticalSplitter.Panel2MinSize;
        var nextDistance = maxDistance < minDistance
            ? Math.Max(0, maxDistance)
            : Math.Clamp(targetDistance, minDistance, maxDistance);
        if (VerticalSplitter.SplitterDistance == nextDistance)
        {
            return;
        }

        try
        {
            VerticalSplitter.SplitterDistance = nextDistance;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            _exceptionLogService.Write(ex, "MonitorView.SetVerticalSplitterPanel2ToMinWidth");
        }
    }

    private void SetWeldPreviewHorizontalOffset(int stationNo, int requestedOffset)
    {
        var grid = GetWeldPreviewGrid(stationNo);
        var scrollBar = GetWeldPreviewScrollBar(stationNo);
        var contentWidth = GetWeldPreviewContentWidth(stationNo);
        var viewportWidth = GetWeldPreviewViewportWidth(stationNo);
        var maxOffset = Math.Max(0, contentWidth - viewportWidth);
        var nextOffset = Math.Clamp(requestedOffset, 0, maxOffset);

        _syncingWeldPreviewHorizontalScroll = true;
        try
        {
            if (grid.HorizontalScrollingOffset != nextOffset)
            {
                grid.HorizontalScrollingOffset = nextOffset;
            }

            scrollBar.SetScrollInfo(contentWidth, viewportWidth, nextOffset);
        }
        catch (ArgumentOutOfRangeException)
        {
            grid.HorizontalScrollingOffset = 0;
            scrollBar.SetScrollInfo(contentWidth, viewportWidth, 0);
        }
        finally
        {
            _syncingWeldPreviewHorizontalScroll = false;
        }
    }

    private void SyncWeldPreviewHorizontalScrollBar()
        => SyncWeldPreviewHorizontalScrollBar(CurrentWeldPreviewGrid);

    private void SyncWeldPreviewHorizontalScrollBar(DataGridView? sourceGrid)
    {
        if (_syncingWeldPreviewHorizontalScroll)
        {
            return;
        }

        var stationNo = sourceGrid is null ? CurrentStationNo : ResolveWeldPreviewStationNo(sourceGrid);
        var grid = GetWeldPreviewGrid(stationNo);
        var scrollBar = GetWeldPreviewScrollBar(stationNo);
        var contentWidth = GetWeldPreviewContentWidth(stationNo);
        var viewportWidth = GetWeldPreviewViewportWidth(stationNo);
        var maxOffset = Math.Max(0, contentWidth - viewportWidth);
        var offset = Math.Clamp(grid.HorizontalScrollingOffset, 0, maxOffset);

        _syncingWeldPreviewHorizontalScroll = true;
        try
        {
            if (grid.HorizontalScrollingOffset != offset)
            {
                grid.HorizontalScrollingOffset = offset;
            }

            scrollBar.SetScrollInfo(contentWidth, viewportWidth, offset);
        }
        finally
        {
            _syncingWeldPreviewHorizontalScroll = false;
        }
    }

    private int GetWeldPreviewMaxHorizontalOffset(int stationNo)
    {
        return Math.Max(0, GetWeldPreviewContentWidth(stationNo) - GetWeldPreviewViewportWidth(stationNo));
    }

    private int GetWeldPreviewContentWidth(int stationNo)
    {
        return GetWeldPreviewGrid(stationNo).Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Visible)
            .Sum(column => column.Width);
    }

    private int GetWeldPreviewViewportWidth(int stationNo)
    {
        var grid = GetWeldPreviewGrid(stationNo);
        return Math.Max(0, grid.ClientSize.Width - (grid.RowHeadersVisible ? grid.RowHeadersWidth : 0));
    }

    private void AddWeldPreviewColumn(string columnName, string headerText, int width)
    {
        CurrentWeldPreviewGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = headerText,
            MinimumWidth = width,
            Width = width,
            Name = columnName,
            //SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private bool EnsureInfoPreviewRows()
    {
        return CurrentWeldPreviewGrid.Rows.Count != _weldParameterRows.Count;
    }

    private void FillInfoPreviewRows()
    {
        var rows = _weldParameterRows.OrderBy(item => item.Sort).ToList();
        EnsurePreviewRowCount(rows.Count);

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var sourceRow = rows[rowIndex];
            SetPreviewValue(rowIndex, PreviewTouchNoColumn, DisplayPreviewValue(sourceRow.TouchNo));
            SetPreviewValue(rowIndex, PreviewTouchResultColumn, DisplayPreviewValue(sourceRow.ParameterName));
            SetPreviewValue(rowIndex, PreviewMessageColumn, DisplayPreviewValue(sourceRow.Value));
        }
    }

    private void FillWeldPreviewRows(IReadOnlyList<WeldPreviewItem> items, IReadOnlyList<IGrouping<int, WeldParameterRow>> touchGroups)
    {
        EnsurePreviewRowCount(touchGroups.Count);

        for (var rowIndex = 0; rowIndex < touchGroups.Count; rowIndex++)
        {
            var touchGroup = touchGroups[rowIndex];
            SetPreviewValue(rowIndex, PreviewTouchNoColumn, ResolvePreviewTouchNo(touchGroup));
            SetPreviewValue(rowIndex, PreviewTouchResultColumn, ResolvePreviewTouchResult(touchGroup));

            foreach (var item in items)
            {
                var detail = touchGroup.FirstOrDefault(row => SamePreviewItem(row, item));
                if (item.EnableUpper)
                {
                    SetPreviewValue(rowIndex, BuildPreviewColumnName(item.Index, PreviewUpperRole), DisplayPreviewValue(detail?.UpperValue));
                }

                if (item.EnableLower)
                {
                    SetPreviewValue(rowIndex, BuildPreviewColumnName(item.Index, PreviewLowerRole), DisplayPreviewValue(detail?.LowerValue));
                }

                if (item.EnableActual)
                {
                    SetPreviewValue(rowIndex, BuildPreviewColumnName(item.Index, PreviewActualRole), DisplayPreviewValue(detail?.Value));
                }

                if (item.EnableResult)
                {
                    SetPreviewValue(rowIndex, BuildPreviewColumnName(item.Index, PreviewResultRole), DisplayPreviewValue(detail?.Result));
                }
            }
        }
    }

    private void SetPreviewValue(int rowIndex, string columnName, string value)
    {
        var grid = CurrentWeldPreviewGrid;
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count || !grid.Columns.Contains(columnName))
        {
            return;
        }

        var cell = grid.Rows[rowIndex].Cells[columnName];
        var current = cell.Value as string ?? string.Empty;
        if (!string.Equals(current, value, StringComparison.Ordinal))
        {
            cell.Value = value;
        }

        ApplyPreviewResultCellStyle(cell, columnName, value);
    }

    /// <summary>
    /// Marks weld result cells with stable OK/NG colors without repainting the whole table.
    /// </summary>
    private static void ApplyPreviewResultCellStyle(DataGridViewCell cell, string columnName, string value)
    {
        if (!IsPreviewResultColumn(columnName))
        {
            return;
        }

        var normalizedValue = value.Trim();
        if (string.Equals(normalizedValue, ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase))
        {
            SetPreviewResultCellColor(cell, UiColors.Status.Success);
            return;
        }

        if (string.Equals(normalizedValue, ProductionConstants.TestResults.Ng, StringComparison.OrdinalIgnoreCase))
        {
            SetPreviewResultCellColor(cell, UiColors.Status.Danger);
            return;
        }

        ResetPreviewCellStyle(cell);
    }

    /// <summary>
    /// Only weld-point result cells need the strong OK/NG background color.
    /// </summary>
    private static bool IsPreviewResultColumn(string columnName)
    {
        return string.Equals(columnName, PreviewTouchResultColumn, StringComparison.Ordinal);
    }

    /// <summary>
    /// Applies the same foreground/background color in normal and selected states.
    /// </summary>
    private static void SetPreviewResultCellColor(DataGridViewCell cell, Color backColor)
    {
        cell.Style.BackColor = backColor;
        cell.Style.ForeColor = Color.White;
        cell.Style.SelectionBackColor = backColor;
        cell.Style.SelectionForeColor = Color.White;
    }

    /// <summary>
    /// Returns ordinary cells to the table default style when their result is blank or unknown.
    /// </summary>
    private static void ResetPreviewCellStyle(DataGridViewCell cell)
    {
        cell.Style.BackColor = Color.Empty;
        cell.Style.ForeColor = Color.Empty;
        cell.Style.SelectionBackColor = Color.Empty;
        cell.Style.SelectionForeColor = Color.Empty;
    }

    private void EnsurePreviewRowCount(int rowCount)
    {
        var grid = CurrentWeldPreviewGrid;
        while (grid.Rows.Count < rowCount)
        {
            grid.Rows.Add();
        }

        while (grid.Rows.Count > rowCount)
        {
            grid.Rows.RemoveAt(grid.Rows.Count - 1);
        }
    }

    private static IReadOnlyList<WeldPreviewItem> ResolveWeldPreviewItems(IEnumerable<WeldParameterRow> rows)
    {
        var items = rows
            .Where(row => row.TouchIndex > 0)
            .Where(row => !string.IsNullOrWhiteSpace(row.ItemKey))
            .Where(row => !IsTouchResultRow(row))
            .GroupBy(row => row.ItemKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Name = group.First().ParameterName,
                Key = group.Key,
                Sort = group.Min(row => row.Sort % 10000),
                EnableActual = group.Any(row => row.EnableActual),
                EnableUpper = group.Any(row => row.EnableUpper),
                EnableLower = group.Any(row => row.EnableLower),
                EnableResult = group.Any(row => row.EnableResult)
            })
            .Where(item => item.EnableActual || item.EnableUpper || item.EnableLower || item.EnableResult)
            .OrderBy(item => item.Sort)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => new WeldPreviewItem(
                index + 1,
                item.Key,
                item.Name,
                item.Sort,
                item.EnableActual,
                item.EnableUpper,
                item.EnableLower,
                item.EnableResult))
            .ToList();

        return items.Count == 0
            ? Array.Empty<WeldPreviewItem>()
            : items;
    }

    private static IReadOnlyList<IGrouping<int, WeldParameterRow>> ResolvePreviewTouchGroups(IEnumerable<WeldParameterRow> rows)
    {
        return rows
            .Where(row => row.TouchIndex > 0)
            .Where(row => !string.IsNullOrWhiteSpace(row.ItemKey))
            .GroupBy(row => row.TouchIndex)
            .OrderBy(group => group.Key)
            .ToList();
    }

    private static bool IsInfoPreview(IReadOnlyList<WeldPreviewItem> items)
    {
        return items.Count == 0;
    }

    private static string BuildWeldPreviewSchemaKey(IReadOnlyList<WeldPreviewItem> items)
    {
        return items.Count == 0
            ? "info"
            : string.Join("|", items.Select(item =>
                $"{item.Index}:{item.Key}:{item.Name}:{item.EnableActual}:{item.EnableUpper}:{item.EnableLower}:{item.EnableResult}"));
    }

    private static string BuildWeldPreviewLayoutKey(IEnumerable<WeldParameterRow> rows)
    {
        var materializedRows = rows.ToList();
        var items = ResolveWeldPreviewItems(materializedRows);
        var rowCount = IsInfoPreview(items)
            ? materializedRows.Count
            : ResolvePreviewTouchGroups(materializedRows).Count;
        return $"{BuildWeldPreviewSchemaKey(items)}|rows:{rowCount}";
    }

    private static string BuildWeldPreviewVisibleValueKey(IEnumerable<WeldParameterRow> rows)
    {
        return string.Join('\u001F', rows
            .OrderBy(row => row.Sort)
            .Select(row => string.Join('\u001E',
                row.StationNo,
                row.ProductNum,
                row.ProductModel,
                row.TouchIndex,
                row.TouchNo,
                row.ItemKey,
                row.ParameterName,
                row.EnableActual,
                row.EnableUpper,
                row.EnableLower,
                row.EnableResult,
                row.TouchResult,
                row.Value,
                row.UpperValue,
                row.LowerValue,
                row.Result)));
    }

    private static string BuildPreviewColumnName(int itemIndex, string role)
    {
        return $"Item{itemIndex}_{role}";
    }

    private static bool SamePreviewItem(WeldParameterRow row, WeldPreviewItem item)
    {
        return string.Equals(row.ItemKey, item.Key, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTouchResultRow(WeldParameterRow row)
    {
        return string.Equals(row.ItemKey, "test_result", StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.ParameterName, "焊点结果", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasAnyEnabledRole(BizSchemeDetail detail)
    {
        return detail.EnableActual || detail.EnableUpper || detail.EnableLower || detail.EnableResult;
    }

    private static BizSchemeDetail NormalizeLegacyDetailRoles(BizSchemeDetail detail)
    {
        if (HasAnyEnabledRole(detail))
        {
            return detail;
        }

        detail.EnableActual = true;
        detail.EnableUpper = true;
        detail.EnableLower = true;
        detail.EnableResult = true;
        return detail;
    }

    private static string ResolvePreviewTouchNo(IEnumerable<WeldParameterRow> rows)
    {
        var first = rows.OrderBy(row => row.Sort).FirstOrDefault();
        return DisplayPreviewValue(first?.TouchNo);
    }

    private static string ResolvePreviewTouchResult(IEnumerable<WeldParameterRow> rows)
    {
        var explicitResult = rows
            .Select(row => row.TouchResult)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value) && value != "--");
        return DisplayPreviewValue(explicitResult);
    }

    private static string DisplayPreviewValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || value == "--"
            ? string.Empty
            : value.Trim();
    }

    private void ApplyCurrentRealtimePreviewSnapshot()
    {
        var snapshot = _productRealtimePreviewService.GetCurrent(CurrentStationNo);
        if (snapshot is not null)
        {
            ApplyProductRealtimePreviewSnapshot(snapshot);
        }
    }

    private void ApplyProductRealtimePreviewSnapshot(ProductRealtimePreviewSnapshot snapshot)
    {
        ApplyLivePreviewSummary(snapshot);
        _currentProductIdentity = new ProductIdentity(snapshot.StationNo, snapshot.ProductNum, snapshot.ProductModel, "RealtimePreview");

        if (snapshot.Rows.Count == 0 && CurrentWeldPreviewGrid.Rows.Count > 0)
        {
            return;
        }

        ApplyRealtimeWeldParameterRows(snapshot.Rows);
    }

    private void ApplyLivePreviewSummary(ProductRealtimePreviewSnapshot snapshot)
    {
        var hasErrorMessage = !string.IsNullOrWhiteSpace(snapshot.Message);
        var statusLabel = CurrentLivePreviewStatusLabel;
        SetControlText(statusLabel, hasErrorMessage ? "实时采集异常" : "实时采集正常");
        statusLabel.ForeColor = hasErrorMessage ? UiColors.Status.Danger : UiColors.Status.Success;

        SetControlText(CurrentLiveProductNoLabel, $"产品编号：{FormatLiveSummaryValue(snapshot.ProductNo)}");
        SetControlText(CurrentLiveTouchCountLabel, $"焊点：{FormatLiveSummaryValue(snapshot.TouchCountText)}");
        ApplyLiveResultTag(snapshot.ProductResult);
    }

    private void ApplyLiveResultTag(string? productResult)
    {
        var resultText = FormatLiveSummaryValue(productResult);
        var tag = CurrentLiveResultTag;
        SetControlText(tag, $"产品结果：{resultText}");

        if (string.Equals(resultText, ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase))
        {
            SetLiveResultTagColor(tag, UiColors.Status.Success, Color.White);
            return;
        }

        if (string.Equals(resultText, ProductionConstants.TestResults.Ng, StringComparison.OrdinalIgnoreCase))
        {
            SetLiveResultTagColor(tag, UiColors.Status.Danger, Color.White);
            return;
        }

        SetLiveResultTagColor(tag, UiColors.Status.Muted, Color.White);
    }

    private static void SetLiveResultTagColor(AntdUI.Tag tag, Color backColor, Color foreColor)
    {
        tag.BackColor = backColor;
        tag.ForeColor = foreColor;
    }

    private static string FormatLiveSummaryValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();
    }

    /// <summary>
    /// Updates realtime rows in place when the row structure is unchanged.
    /// Rebinding is reserved for product/scheme/touch changes because rebuilding the table causes visible flicker.
    /// </summary>
    private void ApplyRealtimeWeldParameterRows(IReadOnlyList<ProductRealtimePreviewRow> snapshotRows)
    {
        var nextRows = snapshotRows
            .OrderBy(row => row.Sort)
            .Select(ToWeldParameterRow)
            .ToList();
        ApplyWeldParameterRows(nextRows);
    }

    private void ApplyWeldParameterRows(IReadOnlyList<WeldParameterRow> nextRows)
    {
        PreserveStablePreviewValues(nextRows);
        var nextLayoutKey = BuildWeldPreviewLayoutKey(nextRows);
        var nextVisibleValueKey = BuildWeldPreviewVisibleValueKey(nextRows);
        var layoutChanged = !_weldParameterTableBound
            || !string.Equals(nextLayoutKey, _weldParameterLayoutKey, StringComparison.Ordinal);

        ReplaceWeldParameterRows(nextRows);
        if (layoutChanged)
        {
            BindWeldParameterTable(forceRebind: true);
            return;
        }

        if (string.Equals(nextVisibleValueKey, _weldParameterVisibleValueKey, StringComparison.Ordinal))
        {
            return;
        }

        _weldParameterVisibleValueKey = nextVisibleValueKey;
        RefreshWeldParameterRows();
    }

    private void ReplaceWeldParameterRows(IEnumerable<WeldParameterRow> rows)
    {
        _weldParameterRows.Clear();
        _weldParameterRows.AddRange(rows);
        SortWeldParameterRows();
    }

    private void PreserveStablePreviewValues(IEnumerable<WeldParameterRow> nextRows)
    {
        var previousRows = _weldParameterRows
            .Where(row => !string.IsNullOrWhiteSpace(row.ItemKey))
            .GroupBy(row => row.UniqueKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var nextRow in nextRows)
        {
            if (!previousRows.TryGetValue(nextRow.UniqueKey, out var previousRow))
            {
                continue;
            }

            if (IsEmptyPreviewValue(nextRow.TouchResult) && !IsEmptyPreviewValue(previousRow.TouchResult))
            {
                nextRow.TouchResult = previousRow.TouchResult;
            }

            if (nextRow.EnableUpper && IsEmptyPreviewValue(nextRow.UpperValue) && !IsEmptyPreviewValue(previousRow.UpperValue))
            {
                nextRow.UpperValue = previousRow.UpperValue;
            }

            if (nextRow.EnableLower && IsEmptyPreviewValue(nextRow.LowerValue) && !IsEmptyPreviewValue(previousRow.LowerValue))
            {
                nextRow.LowerValue = previousRow.LowerValue;
            }

            if (nextRow.EnableResult && IsEmptyPreviewValue(nextRow.Result) && !IsEmptyPreviewValue(previousRow.Result))
            {
                nextRow.Result = previousRow.Result;
            }
        }
    }

    private static bool IsEmptyPreviewValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "--", StringComparison.Ordinal);
    }

    private static void SetControlText(Control control, string? text)
    {
        var value = text ?? string.Empty;
        if (!string.Equals(control.Text, value, StringComparison.Ordinal))
        {
            control.Text = value;
        }
    }

    private static WeldParameterRow ToWeldParameterRow(ProductRealtimePreviewRow row)
    {
        return new WeldParameterRow
        {
            StationNo = row.StationNo,
            Station = row.Station,
            ProductNo = row.ProductNo,
            ProductNum = row.ProductNum,
            ProductModel = row.ProductModel,
            TouchIndex = row.TouchIndex,
            TouchNo = row.TouchNo,
            TouchResult = row.TouchResult,
            ParameterName = row.ItemName,
            Unit = row.Unit,
            EnableActual = row.EnableActual,
            EnableUpper = row.EnableUpper,
            EnableLower = row.EnableLower,
            EnableResult = row.EnableResult,
            ActualAddress = row.ActualAddress,
            UpperAddress = row.UpperAddress,
            LowerAddress = row.LowerAddress,
            ResultAddress = row.ResultAddress,
            Value = row.ActualValue,
            UpperValue = row.UpperValue,
            LowerValue = row.LowerValue,
            Result = row.Result,
            RecordTime = row.RefreshTimeText,
            Sort = row.Sort,
            ItemKey = ResolveItemKey(row.ItemId, row.ItemName),
            TestItemId = row.ItemId
        };
    }

    private PlcExpressionBinding ResolvePreviewExpressionBinding(string baseAddress, int contextOffset, string? expression)
    {
        if (_plcExpressionReadService.TryResolve(baseAddress, contextOffset, expression, out var binding, out _))
        {
            return binding;
        }

        var expressionText = expression?.Trim() ?? string.Empty;
        return new PlcExpressionBinding(expressionText, AppConstants.PlcDataTypes.Int16, 0, expressionText);
    }

    private void QueueRefreshSchemePreview(bool force)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (!force && DateTime.Now - _lastSchemePreviewRefreshTime < TimeSpan.FromSeconds(2))
        {
            return;
        }

        _lastSchemePreviewRefreshTime = DateTime.Now;
        _ = RefreshSchemePreviewAsync(force);
    }

    private async Task RefreshSchemePreviewAsync(bool force)
    {
        if (_refreshingSchemePreview)
        {
            return;
        }

        _refreshingSchemePreview = true;
        try
        {
            var stationNo = CurrentStationNo;
            var identity = ResolveOnlineProductIdentity(stationNo)
                ?? await ReadPlcRecipeProductIdentityAsync(stationNo);
            if (identity is null)
            {
                return;
            }

            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(() => ApplySchemePreview(identity, force));
                return;
            }

            ApplySchemePreview(identity, force);
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.RefreshSchemePreviewAsync");
        }
        finally
        {
            _refreshingSchemePreview = false;
        }
    }

    private ProductIdentity? ResolveOnlineProductIdentity(int stationNo)
    {
        var state = GetCurrentStationState();
        var localProgram = state.SelectedProgram is not null
            ? ResolveLocalProgram(state.SelectedProgram)
            : ResolveLocalProgramById(state.ActiveTask?.ProgramId, state.ActiveTask?.DeviceId);
        if (!string.IsNullOrWhiteSpace(localProgram?.ProductNum))
        {
            return new ProductIdentity(
                stationNo,
                localProgram.ProductNum.Trim(),
                localProgram.ProductModel?.Trim() ?? string.Empty,
                "LocalProgram");
        }

        if (!string.IsNullOrWhiteSpace(state.CurrentWorkOrder?.ProdNum))
        {
            return new ProductIdentity(
                stationNo,
                state.CurrentWorkOrder.ProdNum.Trim(),
                state.CurrentWorkOrder.ProdModel?.Trim() ?? string.Empty,
                "MES");
        }

        if (!string.IsNullOrWhiteSpace(state.ActiveTask?.ProductNum))
        {
            return new ProductIdentity(
                stationNo,
                state.ActiveTask.ProductNum.Trim(),
                state.ActiveTask.ProductModel?.Trim() ?? string.Empty,
                "Task");
        }

        return null;
    }

    private async Task<ProductIdentity?> ReadPlcRecipeProductIdentityAsync(int stationNo)
    {
        var recipeResult = await ReadPlcAddressTextResultAsync(AppConstants.PlcLogicalKeys.PlcRecipeCode, stationNo);
        if (!recipeResult.IsSuccess || string.IsNullOrWhiteSpace(recipeResult.Value))
        {
            return null;
        }

        var localProgram = ResolveLocalProgramByRecipeCode(recipeResult.Value, stationNo);
        if (localProgram is null)
        {
            return null;
        }

        return new ProductIdentity(
            stationNo,
            localProgram.ProductNum.Trim(),
            localProgram.ProductModel?.Trim() ?? string.Empty,
            "PLCRecipe");
    }

    private async Task<PlcTextReadResult> ReadPlcAddressTextResultAsync(string logicalKey, int stationNo)
    {
        var address = _plcAddressService.GetAddress(logicalKey, stationNo);
        if (address is null || !address.Enabled || string.IsNullOrWhiteSpace(address.Address))
        {
            return PlcTextReadResult.Failed($"PLC business address \"{logicalKey}\" is not configured or disabled.");
        }

        var plcAddress = address.Address.Trim();
        switch (address.DataType)
        {
            case AppConstants.PlcDataTypes.Int32:
                var int32Result = await _plcCommunicationService.ReadInt32Async(plcAddress);
                return int32Result.IsSuccess
                    ? PlcTextReadResult.Success(NormalizePlcText(int32Result.Value.ToString()))
                    : PlcTextReadResult.Failed(int32Result.Message);
            case AppConstants.PlcDataTypes.Float:
                var floatResult = await _plcCommunicationService.ReadFloatAsync(plcAddress);
                return floatResult.IsSuccess
                    ? PlcTextReadResult.Success(NormalizePlcText(floatResult.Value.ToString()))
                    : PlcTextReadResult.Failed(floatResult.Message);
            case AppConstants.PlcDataTypes.Bool:
                var boolResult = await _plcCommunicationService.ReadBoolAsync(plcAddress);
                return boolResult.IsSuccess
                    ? PlcTextReadResult.Success(NormalizePlcText(boolResult.Value == true ? "1" : "0"))
                    : PlcTextReadResult.Failed(boolResult.Message);
            case AppConstants.PlcDataTypes.String:
                var stringResult = await _plcCommunicationService.ReadStringAsync(plcAddress, (ushort)Math.Max(1, address.DataLength));
                return stringResult.IsSuccess
                    ? PlcTextReadResult.Success(NormalizePlcText(stringResult.Value))
                    : PlcTextReadResult.Failed(stringResult.Message);
            default:
                var int16Result = await _plcCommunicationService.ReadInt16Async(plcAddress);
                return int16Result.IsSuccess
                    ? PlcTextReadResult.Success(NormalizePlcText(int16Result.Value.ToString()))
                    : PlcTextReadResult.Failed(int16Result.Message);
        }
    }

    private void ApplySchemePreview(ProductIdentity identity, bool force)
    {
        if (identity.StationNo != CurrentStationNo)
        {
            return;
        }

        _currentProductIdentity = identity;
        if (GetCurrentStationState().CurrentWorkOrder is null)
        {
            inputProdNum.Text = identity.ProductNum;
            inputProdModel.Text = identity.ProductModel;
        }

        var processConfig = ResolveRealtimePreviewProcessConfig(identity);
        var activeTaskId = GetCurrentStationState().ActiveTask?.Id ?? 0;
        var previewKey = $"{identity.StationNo}|{identity.ProductNum}|{identity.ProductModel}|{identity.Source}|{activeTaskId}|{processConfig?.Id}|{processConfig?.SchemeId}";
        if (!force
            && string.Equals(previewKey, _lastSchemePreviewKey, StringComparison.Ordinal)
            && _weldParameterRows.Count > 0)
        {
            return;
        }

        var previousRows = _weldParameterRows
            .Where(row => !string.IsNullOrWhiteSpace(row.ItemKey))
            .GroupBy(row => row.UniqueKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var nextRows = BuildSchemePreviewRows(identity, processConfig, previousRows).ToList();

        _lastSchemePreviewKey = previewKey;
        ApplyWeldParameterRows(nextRows);
    }

    private IEnumerable<WeldParameterRow> BuildSchemePreviewRows(
        ProductIdentity identity,
        BizProductProcessConfig? config,
        IReadOnlyDictionary<string, WeldParameterRow> previousRows)
    {
        if (string.IsNullOrWhiteSpace(identity.ProductNum))
        {
            return new[] { CreateInfoRow(identity, "等待产品工号", "请确认 MES 工单或 PLC 产品工号地址。") };
        }

        if (config is null)
        {
            return new[] { CreateInfoRow(identity, "未找到产品工艺配置", "请在地址维护中维护当前产品的产品工艺。") };
        }

        var schemeItems = ResolveSchemeItems(config.SchemeId);
        if (schemeItems.Count == 0)
        {
            return new[] { CreateInfoRow(identity, "测试方案未配置", $"测试方案 {config.SchemeId} 未配置测试项。") };
        }

        var rows = new List<WeldParameterRow>();

        for (var touchNo = 1; touchNo <= Math.Max(1, config.TouchCount); touchNo++)
        {
            foreach (var schemeItem in schemeItems)
            {
                var row = CreateSchemePreviewRow(identity, config, schemeItem, touchNo);
                CopyLatestValues(previousRows, row);
                rows.Add(row);
            }
        }

        return rows.Count == 0
            ? new[] { CreateInfoRow(identity, "测试方案未配置", $"测试方案 {config.SchemeId} 未配置测试项。") }
            : rows;
    }

    /// <summary>
    /// Uses the started task as the authoritative source after start, then falls back
    /// to the product number while the operator is preparing the work order.
    /// </summary>
    private BizProductProcessConfig? ResolveRealtimePreviewProcessConfig(ProductIdentity identity)
    {
        var activeTask = GetCurrentStationState().ActiveTask;
        if (activeTask is not null)
        {
            var taskConfig = _productProcessConfigService.FindActiveForTask(activeTask, identity.StationNo);
            if (taskConfig is not null)
            {
                return taskConfig;
            }
        }

        return _productProcessConfigService.FindActive(identity.ProductNum, identity.StationNo);
    }

    private IReadOnlyList<SchemePreviewItem> ResolveSchemeItems(string schemeId)
    {
        var details = _testSchemeConfigService.GetDetails(schemeId)
            .Select(NormalizeLegacyDetailRoles)
            .OrderBy(detail => detail.DetailId)
            .ToList();
        if (details.Count == 0)
        {
            return Array.Empty<SchemePreviewItem>();
        }

        var allItems = _testSchemeConfigService.GetItems();
        return details
            .Select((detail, index) => new
            {
                Sort = (index + 1) * 10,
                Item = allItems.FirstOrDefault(item => item.ItemId == detail.ItemId),
                Detail = detail
            })
            .Where(item => item.Item is not null)
            .Where(item => HasAnyEnabledRole(item.Detail))
            .Select(item => new SchemePreviewItem(item.Sort, item.Item!, item.Detail))
            .ToList();
    }

    private WeldParameterRow CreateSchemePreviewRow(ProductIdentity identity, BizProductProcessConfig config, SchemePreviewItem schemeItem, int touchNo)
    {
        var item = schemeItem.Item;
        var detail = schemeItem.Detail;
        var testContextOffset = (Math.Max(1, touchNo) - 1) * config.TestAreaLen;
        var actual = detail.EnableActual
            ? ResolvePreviewExpressionBinding(config.TestBase, testContextOffset, item.ActualExpression)
            : PlcExpressionBinding.Empty;
        var upper = detail.EnableUpper
            ? ResolvePreviewExpressionBinding(config.TestBase, testContextOffset, item.UpperExpression)
            : PlcExpressionBinding.Empty;
        var lower = detail.EnableLower
            ? ResolvePreviewExpressionBinding(config.TestBase, testContextOffset, item.LowerExpression)
            : PlcExpressionBinding.Empty;
        var result = detail.EnableResult
            ? ResolvePreviewExpressionBinding(config.TestBase, testContextOffset, item.ResultExpression)
            : PlcExpressionBinding.Empty;

        return new WeldParameterRow
        {
            StationNo = identity.StationNo,
            Station = $"工位{identity.StationNo}",
            ProductNum = identity.ProductNum,
            ProductModel = identity.ProductModel,
            TouchIndex = touchNo,
            TouchNo = touchNo.ToString(),
            ParameterName = item.ItemName,
            Unit = item.Unit ?? string.Empty,
            EnableActual = detail.EnableActual,
            EnableUpper = detail.EnableUpper,
            EnableLower = detail.EnableLower,
            EnableResult = detail.EnableResult,
            ActualAddress = actual.Address,
            UpperAddress = upper.Address,
            LowerAddress = lower.Address,
            ResultAddress = result.Address,
            ActualDataType = actual.DataType,
            ActualRule = actual.Rule,
            UpperDataType = upper.DataType,
            UpperRule = upper.Rule,
            LowerDataType = lower.DataType,
            LowerRule = lower.Rule,
            ResultDataType = result.DataType,
            ResultRule = result.Rule,
            Value = "--",
            Result = "--",
            RecordTime = string.Empty,
            Sort = touchNo * 10000 + schemeItem.Sort,
            ItemKey = ResolveItemKey(item),
            TestItemId = item.ItemId,
            ProcessConfigId = config.Id
        };
    }

    private static string ResolveItemKey(DimTestItem item)
    {
        return ResolveItemKey(item.ItemId, item.ItemName);
    }

    private static string ResolveItemKey(int itemId, string? itemName)
    {
        if (itemId > 0)
        {
            return $"item_{itemId}";
        }

        return itemName?.Trim() ?? string.Empty;
    }

    private static WeldParameterRow CreateInfoRow(ProductIdentity identity, string title, string detail)
    {
        return new WeldParameterRow
        {
            StationNo = identity.StationNo,
            Station = $"工位{identity.StationNo}",
            ProductNum = identity.ProductNum,
            ProductModel = identity.ProductModel,
            TouchNo = "-",
            ParameterName = title,
            Value = detail,
            Result = "--",
            Sort = 0,
            ItemKey = string.Empty
        };
    }

    private static void CopyLatestValues(IReadOnlyDictionary<string, WeldParameterRow> previousRows, WeldParameterRow target)
    {
        if (!previousRows.TryGetValue(target.UniqueKey, out var previous))
        {
            return;
        }

        target.Value = previous.Value;
        target.TouchResult = previous.TouchResult;
        target.UpperValue = previous.UpperValue;
        target.LowerValue = previous.LowerValue;
        target.Result = previous.Result;
        target.RecordTime = previous.RecordTime;
    }

    private IEnumerable<WeldParameterRow> BuildFallbackWeldParameterRows(BizWeldPointRecord record, IReadOnlyDictionary<string, string> rawValues)
    {
        var knownRows = new[]
        {
            CreateFallbackWeldParameterRow(record, "test_result", "焊点结果", FormatNullableText(record.TestResult), 90)
        };

        var knownKeys = new HashSet<string>(knownRows.Select(row => row.ItemKey), StringComparer.OrdinalIgnoreCase)
        {
            "test_result_raw",
            "TestResult",
            "Result"
        };
        var dynamicRows = rawValues
            .Where(item => !knownKeys.Contains(item.Key) && !item.Key.EndsWith("_upper", StringComparison.OrdinalIgnoreCase)
                && !item.Key.EndsWith("_lower", StringComparison.OrdinalIgnoreCase)
                && !item.Key.EndsWith("_result", StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => CreateFallbackWeldParameterRow(record, item.Key, item.Key, FormatNullableText(item.Value), 100 + index));

        return knownRows.Concat(dynamicRows);
    }

    private WeldParameterRow CreateFallbackWeldParameterRow(BizWeldPointRecord record, string itemKey, string parameterName, string value, int sort)
    {
        return new WeldParameterRow
        {
            StationNo = record.StationNo,
            Station = $"工位{record.StationNo}",
            ProductNo = record.ProductNo,
            ProductNum = _currentProductIdentity?.ProductNum ?? GetCurrentStationState().ActiveTask?.ProductNum ?? string.Empty,
            ProductModel = _currentProductIdentity?.ProductModel ?? GetCurrentStationState().ActiveTask?.ProductModel ?? string.Empty,
            TouchIndex = ParsePositiveInt(record.TouchNo),
            TouchNo = record.TouchNo,
            ParameterName = parameterName,
            Value = value,
            Result = FormatNullableText(record.TestResult),
            RecordTime = record.Ts.ToString("HH:mm:ss"),
            Sort = ParsePositiveInt(record.TouchNo) * 10000 + sort,
            ItemKey = itemKey
        };
    }

    private static string? FindRecordResult(BizWeldPointRecord record, WeldParameterRow row, IReadOnlyDictionary<string, string> rawValues)
    {
        return FindRawValue(rawValues, $"{row.ItemKey}_result", $"{row.ParameterName}结果")
            ?? record.TestResult;
    }

    private static string? FindRawValue(IReadOnlyDictionary<string, string> rawValues, params string?[] keys)
    {
        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key) && rawValues.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static int ParsePositiveInt(string? value)
    {
        return int.TryParse(value, out var result) && result > 0 ? result : 0;
    }

    private static string NormalizePlcText(string? value)
    {
        return value?.Trim().Trim('\0') ?? string.Empty;
    }

    private void SortWeldParameterRows()
    {
        _weldParameterRows.Sort((left, right) =>
        {
            var stationCompare = left.StationNo.CompareTo(right.StationNo);
            return stationCompare != 0
                ? stationCompare
                : left.Sort.CompareTo(right.Sort);
        });
    }

    private static Dictionary<string, string> ParseRawWeldValues(string? rawDataJson)
    {
        if (string.IsNullOrWhiteSpace(rawDataJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(rawDataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return document.RootElement.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => property.Value.ToString(),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Keeps monitor tables visually aligned with other management pages.
    /// </summary>
    private void ConfigureTables()
    {
        TableStyleHelper.ApplyAntdTable(tableMetric1, AntdUI.ColumnsMode.Fill);
        TableStyleHelper.ApplyAntdTable(tableMetric2, AntdUI.ColumnsMode.Fill);
        TableStyleHelper.ApplyAntdTable(tableProductHistoryPreview1, AntdUI.ColumnsMode.Fill);
        TableStyleHelper.ApplyAntdTable(tableProductHistoryPreview2, AntdUI.ColumnsMode.Fill);
        ApplyProductHistoryTableStyle(tableProductHistoryPreview1);
        ApplyProductHistoryTableStyle(tableProductHistoryPreview2);
        ApplyCompactProductionMetricTableStyle();
        ApplyWeldParameterTableStyle();
    }

    private static void ApplyProductHistoryTableStyle(AntdUI.Table table)
    {
        table.DefaultExpand = false;
        table.TreeButtonSize = 18;
        table.RowHeight = 36;
        table.RowHeightHeader = 38;
        table.Gap = 6;
        table.GapCell = 3;
        table.Gaps = new Size(6, 4);
    }

    /// <summary>
    /// The metric table has only a few fixed rows, so a compact row height keeps the right panel readable.
    /// </summary>
    private void ApplyCompactProductionMetricTableStyle()
    {
        ApplyCompactProductionMetricTableStyle(tableMetric1);
        ApplyCompactProductionMetricTableStyle(tableMetric2);
    }

    private static void ApplyCompactProductionMetricTableStyle(AntdUI.Table table)
    {
        table.RowHeight = 34;
        table.RowHeightHeader = 36;
        table.Gap = 4;
        table.GapCell = 2;
        table.Gaps = new Size(4, 4);
    }

    private void ApplyWeldParameterTableStyle()
    {
        ApplyWeldParameterTableStyle(dgvPreview1);
        ApplyWeldParameterTableStyle(dgvPreview2);
    }

    private static void ApplyWeldParameterTableStyle(DataGridView grid)
    {
        EnableDoubleBuffering(grid);
        grid.ScrollBars = ScrollBars.Vertical;
        grid.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 10F);
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 244, 255);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 30, 30);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 30, 30);
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.ColumnHeadersHeight = 36;
        grid.RowTemplate.Height = 30;
        grid.GridColor = Color.FromArgb(224, 224, 224);
    }

    /// <summary>
    /// Enables buffered painting for high-frequency monitor tables.
    /// </summary>
    private static void EnableDoubleBuffering(Control control)
    {
        typeof(Control)
            .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(control, true);
    }

    private static string GetDeviceStatusKey(short? statusCode)
    {
        return statusCode switch
        {
            1 => TextKeys.DeviceStatus.Running,
            2 => TextKeys.DeviceStatus.Paused,
            3 => TextKeys.DeviceStatus.Stopped,
            4 => TextKeys.DeviceStatus.Alarm,
            _ => TextKeys.DeviceStatus.Unknown
        };
    }

    private static Color GetDeviceStatusColor(short? statusCode, bool isSuccess)
    {
        if (!isSuccess)
        {
            return UiColors.Status.Danger;
        }

        return statusCode switch
        {
            1 => UiColors.Status.Success,
            2 => UiColors.Status.Warning,
            3 => UiColors.Status.Muted,
            4 => UiColors.Status.Danger,
            _ => UiColors.Status.Muted
        };
    }

    private string FormatNullableText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? _localizer.GetString(TextKeys.Production.NotAvailable)
            : value.Trim();
    }

    private string FormatTestResultText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "--", StringComparison.Ordinal))
        {
            return _localizer.GetString(TextKeys.Production.NotAvailable);
        }

        var resultText = value.Trim();
        if (string.Equals(resultText, ProductionConstants.TestResults.OkRawValue, StringComparison.Ordinal)
            || string.Equals(resultText, ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase))
        {
            return ProductionConstants.TestResults.Ok;
        }

        return string.Equals(resultText, _localizer.GetString(TextKeys.Production.NotAvailable), StringComparison.Ordinal)
            ? resultText
            : ProductionConstants.TestResults.Ng;
    }

    private static double? CalculateRate(int numerator, int denominator)
    {
        return denominator > 0
            ? (double)numerator / denominator
            : null;
    }

    private string FormatRate(double? value)
    {
        return value.HasValue
            ? value.Value.ToString("P2")
            : _localizer.GetString(TextKeys.Production.NotAvailable);
    }

    private bool TrySelectProgram(IReadOnlyList<MesProgramListItemData> programs, out MesProgramListItemData program)
    {
        var columns = new[]
        {
            new SelectionDialogColumn<MesProgramListItemData>(
                "程序名称",
                program => program.ProgramName,
                58F),
            new SelectionDialogColumn<MesProgramListItemData>(
                "产品工号",
                program => program.ProductNum,
                24F,
                DataGridViewContentAlignment.MiddleCenter),
            new SelectionDialogColumn<MesProgramListItemData>(
                "程序类型",
                program => program.ProgramType,
                18F,
                DataGridViewContentAlignment.MiddleCenter)
        };

        return SelectionDialog.TrySelect(
            this,
            _localizer.GetString(TextKeys.Monitor.Dialog.SelectProgramTitle),
            _localizer.GetString(TextKeys.Monitor.Dialog.SelectProgramPrompt),
            programs,
            columns,
            _localizer.GetString(TextKeys.Common.ActionApply),
            _localizer.GetString(TextKeys.Common.ActionCancel),
            out program);
    }

    private bool TryConfirmStartData(
        WorkOrderRes? workOrder,
        ExpItemData? process,
        ProgramDataRes program,
        int stationNo)
    {
        if (workOrder is null)
        {
            ShowWarningText("请先获取工单信息后再确认开工信息。");
            return false;
        }

        using var form = new ProgramContentConfirmForm(
            workOrder,
            process,
            program,
            _programManageService.GetPrograms());
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return false;
        }

        _weldTaskService.ApplyStartAdjustment(form.AdjustedWorkOrder, form.AdjustedProcess, form.ProgramContent, stationNo);
        var refreshedProgram = GetCurrentStationState().SelectedProgram ?? program;
        _confirmedProgramFingerprint = BuildProgramFingerprint(refreshedProgram, stationNo);
        QueueRefreshSchemePreview(force: true);
        return true;
    }

    private bool IsProgramContentConfirmed(ProgramDataRes program, int stationNo)
    {
        return string.Equals(
            _confirmedProgramFingerprint,
            BuildProgramFingerprint(program, stationNo),
            StringComparison.Ordinal);
    }

    private static string BuildProgramFingerprint(ProgramDataRes program, int stationNo)
    {
        return $"{stationNo}|{program.Id}|{program.ProgramName}|{program.ProgramContent}";
    }

    /// <summary>
    /// Resolves the recipe code after a task has started.
    /// Online tasks use ProgramId; offline tasks use ProductNum.
    /// </summary>
    private RecipeCodeResolution ResolveRecipeCodeForStartedTask(BizWeldTask task, ProgramDataRes? selectedProgram)
    {
        if (task.IsOfflineCreated)
        {
            var productNum = FirstNonEmpty(task.ProductNum, selectedProgram?.ProductNum);
            var localProgram = ResolveLocalProgramByProductNum(productNum);
            return new RecipeCodeResolution(
                FirstNonEmpty(localProgram?.RecipeCode, task.RecipeCode),
                "ProductNum",
                $"ProductNum={productNum}; LocalProgramMatched={localProgram is not null}; LocalProgramId={localProgram?.Id}; RecipeCodePresent={!string.IsNullOrWhiteSpace(localProgram?.RecipeCode)}; TaskGuid={task.LocalExpStartId}");
        }

        var programId = FirstNonEmpty(selectedProgram?.Id, task.ProgramId);
        var localProgramById = ResolveLocalProgramByProgramId(programId);
        return new RecipeCodeResolution(
            FirstNonEmpty(localProgramById?.RecipeCode, task.RecipeCode, selectedProgram?.RecipeCode),
            "ProgramId",
            $"ProgramId={programId}; LocalProgramMatched={localProgramById is not null}; LocalProgramId={localProgramById?.Id}; RecipeCodePresent={!string.IsNullOrWhiteSpace(localProgramById?.RecipeCode)}; ExpStartId={task.ExpStartId}");
    }

    private BizProgram? ResolveLocalProgramByProgramId(string? mesProgramId)
    {
        var normalizedProgramId = mesProgramId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedProgramId))
        {
            return null;
        }

        var settings = _currentSettings;
        return _programManageService.GetPrograms()
            .Where(program => string.Equals(program.ProgramId?.Trim(), normalizedProgramId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(program => SameText(program.DeviceId, settings.DeviceId))
            .ThenByDescending(program => program.UpdatedTime)
            .FirstOrDefault();
    }

    private BizProgram? ResolveLocalProgramByProductNum(string? productNum)
    {
        var normalizedProductNum = productNum?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedProductNum))
        {
            return null;
        }

        var settings = _currentSettings;
        return _programManageService.GetPrograms()
            .Where(program => string.Equals(program.ProductNum?.Trim(), normalizedProductNum, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(program => SameText(program.DeviceId, settings.DeviceId))
            .ThenByDescending(program => program.UpdatedTime)
            .FirstOrDefault();
    }

    /// <summary>
    /// Dispatches the recipe after start succeeds. PC recipe is always written;
    /// PLC recipe is checked only when enabled in system settings.
    /// </summary>
    private async Task DispatchRecipeCodeAfterStartAsync(BizWeldTask task, ProgramDataRes? selectedProgram, int stationNo)
    {
        var resolution = ResolveRecipeCodeForStartedTask(task, selectedProgram);
        var recipeCode = NormalizeRecipeCode(resolution.RecipeCode);
        if (string.IsNullOrWhiteSpace(recipeCode))
        {
            WriteRecipeFlowLog(
                "RecipeCodeResolveFailed",
                "配方编号解析失败",
                $"{resolution.Source}; {resolution.Detail}",
                stationNo,
                "Error");
            throw new BusinessOperationException(
                "PLC.RecipeCode",
                "配方编号解析失败",
                BuildRecipeResolveFailureDetail(task, resolution));
        }

        task.RecipeCode = recipeCode;
        selectRecipeCode.Text = recipeCode;
        var validateRecipe = _currentSettings.ValidateRecipeAfterStart;
        foreach (var targetStationNo in ResolveWorkOrderSignalStations(stationNo))
        {
            WriteRecipeFlowLog(
                "RecipeCodeWriteStarted",
                "配方编号准备下发",
                $"{resolution.Source}; {resolution.Detail}; RecipeCode={recipeCode}",
                targetStationNo,
                plcSignal: AppConstants.PlcLogicalKeys.PcRecipeCode);

            if (!validateRecipe)
            {
                var writeResult = await _plcBusinessSignalService.WriteTextAsync(
                    AppConstants.PlcLogicalKeys.PcRecipeCode,
                    targetStationNo,
                    recipeCode);
                if (!writeResult.IsSuccess)
                {
                    WriteRecipeFlowLog(
                        "RecipeCodeWriteFailed",
                        "配方编号下发失败",
                        $"{resolution.Source}; {resolution.Detail}; RecipeCode={recipeCode}; Detail={writeResult.Message}",
                        targetStationNo,
                        "Error",
                        AppConstants.PlcLogicalKeys.PcRecipeCode,
                        writeResult.Address);
                    throw new BusinessOperationException(
                        "PLC.RecipeCode",
                        "配方编号下发失败",
                        $"Station={targetStationNo}; RecipeCode={recipeCode}; Detail={writeResult.Message}");
                }

                WriteRecipeFlowLog(
                    "RecipeCodeWriteSucceeded",
                    "配方编号已下发",
                    $"{resolution.Source}; {resolution.Detail}; RecipeCode={recipeCode}; ValidateRecipe=false",
                    targetStationNo,
                    plcSignal: AppConstants.PlcLogicalKeys.PcRecipeCode,
                    plcAddress: writeResult.Address);
                continue;
            }

            var syncResult = await _plcBusinessSignalService.SyncRecipeCodeAsync(
                targetStationNo,
                recipeCode,
                RecipePreparationTimeout);
            if (!syncResult.IsSuccess)
            {
                WriteRecipeFlowLog(
                    "RecipeCodeValidationFailed",
                    "配方编号校验失败",
                    $"{resolution.Source}; {resolution.Detail}; PC={syncResult.PcRecipeCode}; PLC={syncResult.PlcRecipeCode}; Detail={syncResult.Message}",
                    targetStationNo,
                    "Error",
                    AppConstants.PlcLogicalKeys.PlcRecipeCode);
                throw new BusinessOperationException(
                    "PLC.RecipeCodeCheck",
                    "配方编号校验失败",
                    $"Station={targetStationNo}; PC={syncResult.PcRecipeCode}; PLC={syncResult.PlcRecipeCode}; Detail={syncResult.Message}");
            }

            WriteRecipeFlowLog(
                "RecipeCodeValidationSucceeded",
                "配方编号校验通过",
                $"{resolution.Source}; {resolution.Detail}; RecipeCode={syncResult.PcRecipeCode}; PLC={syncResult.PlcRecipeCode}",
                targetStationNo,
                plcSignal: AppConstants.PlcLogicalKeys.PlcRecipeCode);
        }

        SetRuntimeStatusText(validateRecipe
            ? $"配方编号校验通过：{recipeCode}"
            : $"配方编号已下发：{recipeCode}",
            isSuccess: true);
    }

    private static string BuildRecipeResolveFailureDetail(BizWeldTask task, RecipeCodeResolution resolution)
    {
        var lookupHint = task.IsOfflineCreated
            ? "离线任务需要按 ProductNum 匹配本地程序并读取 RecipeCode。"
            : "在线任务需要按 ProgramId 匹配本地程序并读取 RecipeCode。";
        return $"{lookupHint} {resolution.Detail}";
    }

    private async Task WriteStartBusinessSignalsAsync(ProgramDataRes program, int stationNo)
    {
        await WriteStartBusinessSignalsAfterStartAsync(program, stationNo);
    }

    /// <summary>
    /// Safely writes start business signals (recipe code, work order status) after start succeeds.
    /// Catches and handles exceptions independently without affecting the start success status.
    /// </summary>
    private async Task SafeWriteStartBusinessSignalsAsync(ProgramDataRes program, int stationNo)
    {
        try
        {
            await WriteStartBusinessSignalsAsync(program, stationNo);
        }
        catch (BusinessOperationException ex) when (ex.SourceName?.Contains("Recipe") == true)
        {
            // Recipe-related errors should not mask the start success
            SetRuntimeErrorText("配方编号校验失败");
            _exceptionLogService.WriteBusiness(ex.SourceName, ex.Message, ex.Detail);
        }
        catch (BusinessOperationException ex)
        {
            // Other business signal errors
            SetRuntimeErrorText($"业务信号写入失败：{ex.Message}");
            _exceptionLogService.WriteBusiness(ex.SourceName, ex.Message, ex.Detail);
        }
        catch (Exception ex)
        {
            // Unexpected errors
            SetRuntimeErrorText("业务信号写入失败");
            _exceptionLogService.Write(ex, "MonitorView.SafeWriteStartBusinessSignals");
        }
    }

    /// <summary>
    /// Runs PLC writes after local or online start succeeds.
    /// Work-order status must be written before the recipe code.
    /// </summary>
    private async Task<bool> WriteStartBusinessSignalsAfterStartAsync(ProgramDataRes program, int stationNo)
    {
        var task = GetCurrentStationState().ActiveTask ?? _weldTaskService.RestoreUnfinishedTask(stationNo);

        if (task is null || task.EndTime is not null)
        {
            throw new BusinessOperationException(
                "PLC.RecipeCode",
                "配方编号下发失败",
                $"No started task exists for station {stationNo}.");
        }

        await RequireWorkOrderStatusWriteAsync(
            stationNo,
            ProductionConstants.PlcWorkOrderStatuses.StartedAllowProduction,
            "PLC.WorkOrderStatus.Start",
            "Work order status write failed.",
            writeOnReadFailure: true,
            mirrorWorkOrderStations: true);

        await DispatchRecipeCodeAfterStartAsync(task, program, stationNo);
        return true;
    }

    private async Task WriteFinishBusinessSignalsAsync(int stationNo)
    {
        await RequireWorkOrderStatusWriteAsync(
            stationNo,
            ProductionConstants.PlcWorkOrderStatuses.FinishedForbidProduction,
            "PLC.WorkOrderStatus.Finish",
            "Work order status write failed.",
            writeOnReadFailure: true,
            mirrorWorkOrderStations: true);
    }

    private async Task RequireWorkOrderStatusWriteAsync(
        int stationNo,
        int status,
        string source,
        string summary,
        bool writeOnReadFailure,
        bool mirrorWorkOrderStations)
    {
        var targetStations = mirrorWorkOrderStations
            ? ResolveWorkOrderSignalStations(stationNo)
            : [NormalizeStatusStationNo(stationNo)];
        foreach (var targetStationNo in targetStations)
        {
            await EnsureWorkOrderStatusAsync(
                targetStationNo,
                status,
                source,
                summary,
                context: source,
                writeOnReadFailure,
                mirrorWorkOrderStations: false);
        }
    }

    private async Task EnsureWorkOrderStatusAsync(
        int stationNo,
        int expectedStatus,
        string source,
        string summary,
        string context,
        bool writeOnReadFailure,
        bool mirrorWorkOrderStations)
    {
        var targetStations = mirrorWorkOrderStations
            ? ResolveWorkOrderSignalStations(stationNo)
            : [NormalizeStatusStationNo(stationNo)];
        foreach (var targetStationNo in targetStations)
        {
            await EnsureIntegerBusinessSignalAsync(
                targetStationNo,
                AppConstants.PlcLogicalKeys.WorkOrderStatus,
                expectedStatus,
                $"{source}.S{targetStationNo}",
                summary,
                context,
                writeOnReadFailure,
                _lastWorkOrderStatusSnapshots,
                GetWorkOrderStatusLock(targetStationNo),
                (target, value) => _plcBusinessSignalService.WriteWorkOrderStatusAsync(target, value));
        }
    }

    private Task EnsureDeviceModeAsync(
        int stationNo,
        int expectedMode,
        string source,
        string summary,
        string context,
        bool writeOnReadFailure)
    {
        var targetStationNo = NormalizeStatusStationNo(stationNo);
        return EnsureIntegerBusinessSignalAsync(
            targetStationNo,
            AppConstants.PlcLogicalKeys.DeviceMode,
            expectedMode,
            $"{source}.S{targetStationNo}",
            summary,
            context,
            writeOnReadFailure,
            _lastDeviceModeSnapshots,
            GetDeviceModeLock(targetStationNo),
            (target, value) => _plcBusinessSignalService.WriteDeviceModeAsync(target, value));
    }

    private async Task EnsureIntegerBusinessSignalAsync(
        int stationNo,
        string logicalKey,
        int expectedValue,
        string source,
        string summary,
        string context,
        bool writeOnReadFailure,
        IDictionary<int, int> lastSuccessCache,
        SemaphoreSlim signalLock,
        Func<int, int, Task<PlcBusinessSignalResult>> writeAsync)
    {
        var targetStationNo = NormalizeStatusStationNo(stationNo);
        await signalLock.WaitAsync();
        try
        {
            var readResult = await _plcBusinessSignalService.ReadTextAsync(logicalKey, targetStationNo);
            var readValueParsed = TryParsePlcSignalInt(readResult.Value, out var currentValue);
            var shouldWrite = !readResult.IsSuccess || !readValueParsed || currentValue != expectedValue;
            PlcBusinessSignalResult? writeResult = null;

            if (!readResult.IsSuccess && !writeOnReadFailure)
            {
                WriteBusinessSignalReconcileFlowLog(
                    readResult,
                    writeResult,
                    source,
                    summary,
                    targetStationNo,
                    logicalKey,
                    expectedValue,
                    shouldWrite: false,
                    context);
                return;
            }

            if (!shouldWrite)
            {
                lastSuccessCache[targetStationNo] = expectedValue;
                return;
            }

            writeResult = await writeAsync(targetStationNo, expectedValue);
            WriteBusinessSignalReconcileFlowLog(
                readResult,
                writeResult,
                source,
                summary,
                targetStationNo,
                logicalKey,
                expectedValue,
                shouldWrite,
                context);

            if (writeResult is { IsSuccess: false } && writeOnReadFailure)
            {
                throw new BusinessOperationException(source, summary, writeResult.Message);
            }

            if (writeResult is { IsSuccess: true })
            {
                lastSuccessCache[targetStationNo] = expectedValue;
            }
        }
        finally
        {
            signalLock.Release();
        }
    }

    private SemaphoreSlim GetWorkOrderStatusLock(int stationNo)
        => GetBusinessSignalLock(_workOrderStatusLocks, stationNo);

    private SemaphoreSlim GetDeviceModeLock(int stationNo)
        => GetBusinessSignalLock(_deviceModeLocks, stationNo);

    private SemaphoreSlim GetBusinessSignalLock(Dictionary<int, SemaphoreSlim> locks, int stationNo)
    {
        var targetStationNo = NormalizeStatusStationNo(stationNo);
        lock (_businessSignalLockSync)
        {
            if (!locks.TryGetValue(targetStationNo, out var semaphore))
            {
                semaphore = new SemaphoreSlim(1, 1);
                locks[targetStationNo] = semaphore;
            }

            return semaphore;
        }
    }

    private static bool TryParsePlcSignalInt(string? value, out int number)
    {
        return int.TryParse((value ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
    }

    private void WriteBusinessSignalReconcileFlowLog(
        PlcBusinessSignalResult readResult,
        PlcBusinessSignalResult? writeResult,
        string source,
        string summary,
        int stationNo,
        string plcSignal,
        int expectedValue,
        bool shouldWrite,
        string context)
    {
        var state = _weldTaskService.CurrentState.GetOrCreateStation(NormalizeStatusStationNo(stationNo));
        var actionSummary = BuildBusinessSignalReconcileSummary(plcSignal, readResult, writeResult, summary);
        var address = FirstNonEmpty(writeResult?.Address, readResult.Address);
        var detail = new StringBuilder()
            .Append("Source=").Append(source).Append("\r\n")
            .Append("Expected=").Append(expectedValue.ToString(CultureInfo.InvariantCulture)).Append("\r\n")
            .Append("PlcValue=").Append(readResult.Value).Append("\r\n")
            .Append("ReadSuccess=").Append(readResult.IsSuccess.ToString(CultureInfo.InvariantCulture)).Append("\r\n")
            .Append("ReadMessage=").Append(readResult.Message).Append("\r\n")
            .Append("ShouldWrite=").Append(shouldWrite.ToString(CultureInfo.InvariantCulture)).Append("\r\n")
            .Append("WriteSuccess=").Append(writeResult?.IsSuccess.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append("\r\n")
            .Append("WriteMessage=").Append(writeResult?.Message ?? string.Empty).Append("\r\n")
            .Append("Address=").Append(address).Append("\r\n")
            .Append("Context=").Append(context).Append("\r\n")
            .ToString();

        _productionLogService.Write(
            "BusinessSignalReconcile",
            actionSummary,
            detail,
            ResolveBusinessSignalReconcileSeverity(readResult, writeResult),
            stationNo <= 0 ? CurrentStationNo : stationNo,
            state.ActiveTask?.SN ?? inputSN.Text,
            productNo: string.Empty,
            programId: state.SelectedProgram?.Id ?? string.Empty,
            plcSignal: plcSignal,
            plcAddress: address);
    }

    private static string BuildBusinessSignalReconcileSummary(
        string plcSignal,
        PlcBusinessSignalResult readResult,
        PlcBusinessSignalResult? writeResult,
        string failureSummary)
    {
        if (!readResult.IsSuccess && writeResult is null)
        {
            return $"{plcSignal}读取失败，未执行调和写入";
        }

        if (writeResult is { IsSuccess: true })
        {
            return $"{plcSignal}调和写入成功";
        }

        return failureSummary;
    }

    private static string ResolveBusinessSignalReconcileSeverity(
        PlcBusinessSignalResult readResult,
        PlcBusinessSignalResult? writeResult)
    {
        return !readResult.IsSuccess || writeResult is { IsSuccess: false }
            ? "Error"
            : "Info";
    }

    private int ResolvePlcDeviceMode()
    {
        var settings = _currentSettings;
        return settings.EnableDualStation && settings.EnableDualWorkOrder
            ? ProductionConstants.PlcDeviceModes.DualStationDualWorkOrder
            : ProductionConstants.PlcDeviceModes.SingleOrDualSameWorkOrder;
    }

    private void WriteRecipeFlowLog(
        string step,
        string summary,
        string detail,
        int stationNo,
        string level = "Info",
        string plcSignal = "",
        string plcAddress = "")
    {
        var state = GetCurrentStationState();
        _productionLogService.Write(
            step,
            summary,
            detail,
            level,
            stationNo,
            state.ActiveTask?.SN ?? inputSN.Text,
            productNo: string.Empty,
            programId: state.SelectedProgram?.Id ?? string.Empty,
            plcSignal: string.IsNullOrWhiteSpace(plcSignal) ? AppConstants.PlcLogicalKeys.PcRecipeCode : plcSignal,
            plcAddress: plcAddress);
    }

    /// <summary>
    /// Displays the recipe snapshot without using task ids for recipe lookup.
    /// </summary>
    private string ResolveRecipeCodeForDisplay(BizWeldTask? activeTask, ProgramDataRes? program)
    {
        if (activeTask is not null)
        {
            return FirstNonEmpty(activeTask.RecipeCode, program?.RecipeCode);
        }

        if (program is null)
        {
            return string.Empty;
        }

        var localProgram = ResolveLocalProgramById(program.Id);
        return FirstNonEmpty(localProgram?.RecipeCode, program.RecipeCode);
    }

    private BizProgram? ResolveLocalProgram(ProgramDataRes program)
    {
        var localPrograms = _programManageService.GetPrograms();
        var programId = program.Id?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(programId))
        {
            var byMesProgramId = ResolveLocalProgramById(programId);
            if (byMesProgramId is not null)
            {
                return byMesProgramId;
            }
        }

        return localPrograms.FirstOrDefault(item =>
            string.Equals(item.ProgramName?.Trim(), program.ProgramName?.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.ProductNum?.Trim(), program.ProductNum?.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private BizProgram? ResolveLocalProgramById(string? programId, string? deviceId = null)
    {
        var normalizedProgramId = programId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedProgramId))
        {
            return null;
        }

        return _programManageService.GetPrograms()
            .Where(program => string.Equals(program.ProgramId?.Trim(), normalizedProgramId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(program => SameText(program.DeviceId, deviceId))
            .ThenByDescending(program => program.UpdatedTime)
            .FirstOrDefault();
    }

    private BizProgram? ResolveLocalProgramByRecipeCode(string? recipeCode, int stationNo)
    {
        var normalizedRecipeCode = NormalizeRecipeCode(recipeCode);
        if (string.IsNullOrWhiteSpace(normalizedRecipeCode))
        {
            return null;
        }

        var settings = _currentSettings;
        return _programManageService.GetPrograms()
            .Where(program => string.Equals(NormalizeRecipeCode(program.RecipeCode), normalizedRecipeCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(program => SameText(program.DeviceId, settings.DeviceId))
            .ThenByDescending(program => program.UpdatedTime)
            .FirstOrDefault();
    }

    private static string NormalizeRecipeCode(string? value)
    {
        return NormalizePlcText(value);
    }

    private static bool SameText(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    private static string FormatProgram(MesProgramListItemData program)
    {
        return $"{program.ProgramName} | {program.ProgramType} | {program.ProductNum} | {program.Id}";
    }

    private async Task<string> PromptValidatedOperatorAsync(int stationNo)
    {
        while (true)
        {
            using var form = new OperatorInputForm(_localizer);
            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return string.Empty;
            }

            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.ValidatingOperator);
            var response = await _weldTaskService.ValidateMesOperatorAsync(form.EmployeeNumber, stationNo);
            if (response.IsSuccess)
            {
                BindMesOperatorInfo(response.Data, form.EmployeeNumber);
                return string.IsNullOrWhiteSpace(response.Data?.UserNumber)
                    ? form.EmployeeNumber
                    : response.Data.UserNumber.Trim();
            }

            ShowBusinessWarning(
                "MES.ValidateOperator",
                TextKeys.Monitor.Message.OperatorValidationFailed,
                response.Msg,
                $"EmployeeNumber={form.EmployeeNumber}");
        }
    }

    private void BindMesOperatorInfo(UserInfoRes? userInfo, string fallbackEmployeeNumber)
    {
        MesUserName.Text = userInfo?.UserName?.Trim() ?? string.Empty;
        MesUserNumber.Text = string.IsNullOrWhiteSpace(userInfo?.UserNumber)
            ? fallbackEmployeeNumber.Trim()
            : userInfo.UserNumber.Trim();
        DeptName.Text = userInfo?.DeptName?.Trim() ?? string.Empty;
        TeamName.Text = userInfo?.TeamName?.Trim() ?? string.Empty;
    }

    private void BindRuntimeOperatorInfo(ProductionStationRuntimeState state, BizWeldTask? activeTask)
    {
        var taskOperator = CreateTaskOperatorInfo(activeTask);
        if (taskOperator is not null)
        {
            BindMesOperatorInfo(taskOperator, taskOperator.UserNumber);
            return;
        }

        if (state.MesOperatorInfo is not null)
        {
            BindMesOperatorInfo(state.MesOperatorInfo, state.MesOperatorNumber);
            return;
        }

        if (!string.IsNullOrWhiteSpace(state.MesOperatorNumber))
        {
            BindMesOperatorInfo(null, state.MesOperatorNumber);
            return;
        }

        ClearMesOperatorInfo();
    }

    private static UserInfoRes? CreateTaskOperatorInfo(BizWeldTask? task)
    {
        if (task is null)
        {
            return null;
        }

        var userNumber = FirstNonEmpty(task.UserNumber);
        var userName = task.UserName?.Trim() ?? string.Empty;
        var deptName = task.DeptName?.Trim() ?? string.Empty;
        var teamName = task.TeamName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userNumber)
            && string.IsNullOrWhiteSpace(userName)
            && string.IsNullOrWhiteSpace(deptName)
            && string.IsNullOrWhiteSpace(teamName))
        {
            return null;
        }

        return new UserInfoRes
        {
            UserNumber = userNumber,
            UserName = userName,
            DeptName = deptName,
            TeamName = teamName
        };
    }

    private void ClearMesOperatorInfo()
    {
        MesUserName.Text = string.Empty;
        MesUserNumber.Text = string.Empty;
        DeptName.Text = string.Empty;
        TeamName.Text = string.Empty;
    }

    private bool TryPromptNonNegativeInt(string titleKey, string promptKey, int defaultValue, out int value)
    {
        if (!TryPromptInt(titleKey, promptKey, defaultValue, out value))
        {
            return false;
        }

        if (value >= 0)
        {
            return true;
        }

        ShowWarning(TextKeys.Monitor.Message.QuantityInvalid);
        return false;
    }

    private bool TryPromptInt(string titleKey, string promptKey, int defaultValue, out int value)
    {
        return TryPromptIntText(
            _localizer.GetString(titleKey),
            _localizer.GetString(promptKey),
            defaultValue,
            out value);
    }

    private bool TryPromptIntText(string title, string prompt, int defaultValue, out int value)
    {
        if (!PromptInputForm.TryShow(
                this,
                title,
                prompt,
                Math.Max(0, defaultValue).ToString(),
                _localizer.GetString(TextKeys.Common.ActionApply),
                _localizer.GetString(TextKeys.Common.ActionCancel),
                out var text))
        {
            value = 0;
            return false;
        }

        if (int.TryParse(text, out value))
        {
            return true;
        }

        ShowWarning(TextKeys.Monitor.Message.QuantityInvalid);
        return false;
    }

    private async Task RunUiOperationAsync(Func<Task> action)
    {
        try
        {
            UseWaitCursor = true;
            await action();
        }
        catch (BusinessOperationException ex)
        {
            _exceptionLogService.WriteBusiness(ex.SourceName, ex.Message, ex.Detail);
            SetRuntimeErrorText(ex.Message);
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.RunUiOperationAsync");
            var message = BuildLocalizedMessage(TextKeys.Monitor.RuntimeError.OperationFailed);
            SetRuntimeError(TextKeys.Monitor.RuntimeError.OperationFailed);
            ShowError(message);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private async Task RunReportOperationAsync(int stationNo, string actionName, Func<Task> action)
    {
        stationNo = NormalizePreviewStationNo(stationNo);
        if (!TryEnterStationOperation(stationNo))
        {
            SetRuntimeErrorText($"工位{stationNo}{actionName}正在执行中，请稍后再试。");
            return;
        }

        try
        {
            UseWaitCursor = true;
            SelectStationForOperation(stationNo);
            await action();
        }
        catch (BusinessOperationException ex)
        {
            _exceptionLogService.WriteBusiness(ex.SourceName, ex.Message, ex.Detail);
            SetRuntimeErrorText(BuildStationReportFailureText(stationNo, actionName, ex.Message));
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, $"MonitorView.{actionName}");
            SetRuntimeErrorText(BuildStationReportFailureText(stationNo, actionName, ex.Message));
        }
        finally
        {
            UseWaitCursor = false;
            ExitStationOperation(stationNo);
        }
    }

    private static bool TryEnterStationOperation(int stationNo)
    {
        lock (StationOperationSync)
        {
            if (BusyOperationStations.Contains(stationNo))
            {
                return false;
            }

            BusyOperationStations.Add(stationNo);
            return true;
        }
    }

    private static void ExitStationOperation(int stationNo)
    {
        lock (StationOperationSync)
        {
            BusyOperationStations.Remove(stationNo);
        }
    }

    private static string BuildStationReportSuccessText(int stationNo, string actionName)
    {
        return $"工位{stationNo}{actionName}成功";
    }

    private void SetStationReportFailure(int stationNo, string actionName, string detail)
    {
        SetRuntimeErrorText(BuildStationReportFailureText(stationNo, actionName, detail));
    }

    private static string BuildStationReportFailureText(int stationNo, string actionName, string detail)
    {
        return string.IsNullOrWhiteSpace(detail)
            ? $"工位{stationNo}{actionName}失败"
            : $"工位{stationNo}{actionName}失败：{detail}";
    }

    private void ShowWarning(string messageKey, params object[] args)
    {
        SetRuntimeError(messageKey, args);
        MessageBox.Show(
            this,
            _localizer.GetString(messageKey, args),
            _localizer.GetString(TextKeys.Common.TitleWarning),
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private void ShowWarningText(string message)
    {
        SetRuntimeErrorText(message);
        MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleWarning),
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private void ShowBusinessWarning(string source, string messageKey, string detail, string? context = null)
    {
        var message = _localizer.GetString(messageKey);
        _exceptionLogService.WriteBusiness(source, message, detail, context);
        SetRuntimeError(messageKey);
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

    private void SetRuntimeStatus(string messageKey, params object[] args)
    {
        _runtimeStatusKey = messageKey;
        _runtimeStatusArgs = args;
        _runtimeStatusText = null;
        _runtimeStatusTextIsSuccess = false;
        PersistCurrentRuntimeTipState();
        RefreshRuntimeStatus();
    }

    private void SetRuntimeStatusText(string message, bool isSuccess = false)
    {
        _runtimeStatusKey = null;
        _runtimeStatusArgs = Array.Empty<object>();
        _runtimeStatusText = NormalizeRuntimeSummary(message);
        _runtimeStatusTextIsSuccess = isSuccess;
        PersistCurrentRuntimeTipState();
        RefreshRuntimeStatus();
    }

    private void SetRuntimeError(string messageKey, params object[] args)
    {
        _runtimeErrorKey = messageKey;
        _runtimeErrorArgs = args;
        _runtimeErrorText = null;
        PersistCurrentRuntimeTipState();
        RefreshRuntimeError();
    }

    private void SetRuntimeErrorText(string message)
    {
        _runtimeErrorKey = null;
        _runtimeErrorArgs = Array.Empty<object>();
        _runtimeErrorText = NormalizeRuntimeSummary(message);
        PersistCurrentRuntimeTipState();
        RefreshRuntimeError();
    }

    private void ClearRuntimeError()
    {
        _runtimeErrorKey = null;
        _runtimeErrorArgs = Array.Empty<object>();
        _runtimeErrorText = null;
        inputErrorTips.Clear();
        ApplyRuntimeErrorTone(hasError: false);
        PersistCurrentRuntimeTipState();
    }

    private void RestoreCurrentRuntimeTipState()
    {
        try
        {
            var state = _runtimeTipStateService.Get(CurrentStationNo);
            _runtimeStatusKey = string.IsNullOrWhiteSpace(state.RuntimeStatusKey)
                ? TextKeys.Monitor.RuntimeStatus.Idle
                : state.RuntimeStatusKey;
            _runtimeStatusArgs = DeserializeRuntimeArgs(state.RuntimeStatusArgsJson);
            _runtimeStatusText = state.RuntimeStatusText;
            _runtimeStatusTextIsSuccess = state.RuntimeStatusTextIsSuccess;
            _runtimeErrorKey = string.IsNullOrWhiteSpace(state.RuntimeErrorKey)
                ? null
                : state.RuntimeErrorKey;
            _runtimeErrorArgs = DeserializeRuntimeArgs(state.RuntimeErrorArgsJson);
            _runtimeErrorText = state.RuntimeErrorText;
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.RestoreCurrentRuntimeTipState");
        }
    }

    private void PersistCurrentRuntimeTipState()
    {
        try
        {
            _runtimeTipStateService.Save(new BizRuntimeTipState
            {
                StationNo = CurrentStationNo,
                RuntimeStatusKey = _runtimeStatusKey,
                RuntimeStatusArgsJson = SerializeRuntimeArgs(_runtimeStatusArgs),
                RuntimeStatusText = _runtimeStatusText,
                RuntimeStatusTextIsSuccess = _runtimeStatusTextIsSuccess,
                RuntimeErrorKey = _runtimeErrorKey,
                RuntimeErrorArgsJson = SerializeRuntimeArgs(_runtimeErrorArgs),
                RuntimeErrorText = _runtimeErrorText
            });
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.PersistCurrentRuntimeTipState");
        }
    }

    private static string? SerializeRuntimeArgs(object[] args)
    {
        if (args.Length == 0)
        {
            return null;
        }

        var values = args.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).ToArray();
        return JsonSerializer.Serialize(values);
    }

    private static object[] DeserializeRuntimeArgs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<object>();
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json)?.Cast<object>().ToArray() ?? Array.Empty<object>();
        }
        catch
        {
            return Array.Empty<object>();
        }
    }

    private void RefreshRuntimePanels()
    {
        RefreshRuntimeStatus();
        RefreshRuntimeError();
    }

    private void RefreshRuntimeStatus()
    {
        inputRunningStatus.Text = _runtimeStatusKey is null
            ? _runtimeStatusText ?? string.Empty
            : BuildLocalizedMessage(_runtimeStatusKey, _runtimeStatusArgs);
        ApplyRuntimeStatusTone();
    }

    private void RefreshRuntimeError()
    {
        inputErrorTips.Text = _runtimeErrorKey is null
            ? _runtimeErrorText ?? string.Empty
            : BuildLocalizedMessage(_runtimeErrorKey, _runtimeErrorArgs);
        ApplyRuntimeErrorTone(!string.IsNullOrWhiteSpace(inputErrorTips.Text));
    }

    private void ApplyRuntimeStatusTone()
    {
        var color = _runtimeStatusTextIsSuccess
            ? UiColors.Status.Success
            : GetRuntimeStatusColor(_runtimeStatusKey);
        grpRunningStatus.ForeColor = color;
        inputRunningStatus.ForeColor = color;
    }

    private void ApplyRuntimeErrorTone(bool hasError)
    {
        var color = hasError ? UiColors.Status.Danger : UiColors.Status.Muted;
        grpErrorTips.ForeColor = color;
        inputErrorTips.ForeColor = color;
    }

    private static Color GetRuntimeStatusColor(string? messageKey)
    {
        return messageKey switch
        {
            TextKeys.Monitor.Message.WorkOrderReady
                or TextKeys.Monitor.Message.StartSuccess
                or TextKeys.Monitor.Message.FinishSuccess => UiColors.Status.Success,
            TextKeys.Monitor.RuntimeStatus.Idle or null => UiColors.Status.Muted,
            _ => UiColors.Status.Primary
        };
    }

    private string BuildLocalizedMessage(string messageKey, params object[] args)
    {
        return NormalizeRuntimeSummary(_localizer.GetString(messageKey, args));
    }

    private static string NormalizeRuntimeSummary(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(message.Length);
        var previousWasWhiteSpace = false;
        foreach (var current in message.Trim())
        {
            if (char.IsWhiteSpace(current))
            {
                if (!previousWasWhiteSpace)
                {
                    builder.Append(' ');
                    previousWasWhiteSpace = true;
                }

                continue;
            }

            builder.Append(current);
            previousWasWhiteSpace = false;
        }

        var summary = builder.ToString().Trim();
        if (summary.Length <= RuntimeSummaryMaxLength)
        {
            return summary;
        }

        var keepLength = Math.Max(0, RuntimeSummaryMaxLength - RuntimeSummaryOverflowSuffix.Length);
        return summary[..keepLength] + RuntimeSummaryOverflowSuffix;
    }

    /// <summary>
    /// Identifies the active product used by the real-time preview.
    /// </summary>
    private sealed record ProductIdentity(
        int StationNo,
        string ProductNum,
        string ProductModel,
        string Source);

    /// <summary>
    /// Row model used by AntdUI.Table for the product history tree.
    /// </summary>
    private sealed class ProductHistoryTableRow
    {
        public bool IsProductRow { get; init; }

        public int TaskId { get; init; }

        public int StationNo { get; init; }

        public string ProductNo { get; init; } = string.Empty;

        public string TouchNo { get; init; } = string.Empty;

        public string NodeText { get; init; } = string.Empty;

        public string ResultText { get; init; } = string.Empty;

        public string UploadStatusText { get; init; } = string.Empty;

        public bool IsTest { get; init; }

        public string IsTestText { get; init; } = string.Empty;

        public string TouchCountText { get; init; } = string.Empty;

        public string RecordTimeText { get; init; } = string.Empty;

        public Dictionary<string, string> DynamicValues { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        public bool CanMarkTest { get; init; }

        public string MarkDisabledReason { get; init; } = string.Empty;

        public List<ProductHistoryTableRow> Children { get; init; } = [];
    }

    private sealed record ProductHistoryDynamicColumn(
        string Key,
        string Title,
        string ItemKey,
        string ItemName,
        string Role,
        int Sort);

    private sealed class ProductHistoryRawColumnCandidate
    {
        public ProductHistoryRawColumnCandidate(string itemKey, string itemName, int sort)
        {
            ItemKey = itemKey;
            ItemName = itemName;
            Sort = sort;
        }

        public string ItemKey { get; }

        public string ItemName { get; }

        public int Sort { get; }

        public bool EnableActual { get; private set; }

        public bool EnableUpper { get; private set; }

        public bool EnableLower { get; private set; }

        public bool EnableResult { get; private set; }

        public void EnableRole(string role)
        {
            switch (role)
            {
                case PreviewUpperRole:
                    EnableUpper = true;
                    break;
                case PreviewLowerRole:
                    EnableLower = true;
                    break;
                case PreviewResultRole:
                    EnableResult = true;
                    break;
                default:
                    EnableActual = true;
                    break;
            }
        }
    }

    private sealed record PlcTextReadResult(bool IsSuccess, string Value, string Detail)
    {
        public static PlcTextReadResult Success(string value) => new(true, value, string.Empty);

        public static PlcTextReadResult Failed(string detail) => new(false, string.Empty, detail);
    }

    private sealed record PlcWriteResult(bool IsSuccess, string Detail, string Address)
    {
        public static PlcWriteResult Success(string address) => new(true, string.Empty, address);

        public static PlcWriteResult Failed(string detail, string address = "") => new(false, detail, address);
    }

    private sealed class WeldParameterRow
    {
        public int StationNo { get; init; }

        public string Station { get; init; } = string.Empty;

        public string ProductNo { get; set; } = string.Empty;

        public string ProductNum { get; init; } = string.Empty;

        public string ProductModel { get; init; } = string.Empty;

        public int TouchIndex { get; init; }

        public string TouchNo { get; init; } = string.Empty;

        public string TouchResult { get; set; } = "--";

        public string ParameterName { get; init; } = string.Empty;

        public string Unit { get; init; } = string.Empty;

        public bool EnableActual { get; init; } = true;

        public bool EnableUpper { get; init; } = true;

        public bool EnableLower { get; init; } = true;

        public bool EnableResult { get; init; } = true;

        public string ActualAddress { get; init; } = string.Empty;

        public string UpperAddress { get; init; } = string.Empty;

        public string LowerAddress { get; init; } = string.Empty;

        public string ResultAddress { get; init; } = string.Empty;

        public string ActualDataType { get; init; } = AppConstants.PlcDataTypes.Int16;

        public int ActualRule { get; init; }

        public string UpperDataType { get; init; } = AppConstants.PlcDataTypes.Int16;

        public int UpperRule { get; init; }

        public string LowerDataType { get; init; } = AppConstants.PlcDataTypes.Int16;

        public int LowerRule { get; init; }

        public string ResultDataType { get; init; } = AppConstants.PlcDataTypes.Int16;

        public int ResultRule { get; init; }

        public string Value { get; set; } = "--";

        public string UpperValue { get; set; } = "--";

        public string LowerValue { get; set; } = "--";

        public string Result { get; set; } = "--";

        public string RecordTime { get; set; } = string.Empty;

        public int Sort { get; init; }

        public string ItemKey { get; init; } = string.Empty;

        public int TestItemId { get; init; }

        public int ProcessConfigId { get; init; }

        public string UniqueKey => $"{StationNo}|{ProductNum}|{ProductModel}|{TouchIndex}|{ItemKey}";
    }

    private sealed record ProductionMetricRow(string Name, string Value);

    /// <summary>
    /// Keeps only the fields needed to explain when and why the PLC connection state changed.
    /// </summary>
    private sealed record PlcStatusHistoryEntry(
        int StationNo,
        DateTime ChangedTime,
        PlcConnectionState State,
        bool IsConnected,
        string Message);

    private sealed record WeldPreviewItem(
        int Index,
        string Key,
        string Name,
        int Sort,
        bool EnableActual,
        bool EnableUpper,
        bool EnableLower,
        bool EnableResult);

    private sealed record SchemePreviewItem(int Sort, DimTestItem Item, BizSchemeDetail Detail);

    private sealed record RecipeCodeResolution(string RecipeCode, string Source, string Detail);
}
