using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Forms;
using AutoWeldSystem.UI.Infrastructure;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

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
    private const int WeldPreviewMinVisibleRows = 5;
    private const int WeldPreviewMaxVisibleRows = 12;
    private const int WeldPreviewHeightPadding = 20;
    private const int WeldPreviewHeightTolerance = 2;
    // 主界面运行提示只承载摘要，完整业务细节以生产日志和本地日志为准。
    private const int RuntimeSummaryMaxLength = 56;
    private const string RuntimeSummaryOverflowSuffix = "...";
    private const int WmSetRedraw = 0x000B;
    private const string PreviewTouchNoColumn = "TouchNo";
    private const string PreviewTouchResultColumn = "TouchResult";
    private const string PreviewMessageColumn = "Message";
    private const string PreviewUpperRole = "Upper";
    private const string PreviewLowerRole = "Lower";
    private const string PreviewActualRole = "Actual";
    private const string PreviewResultRole = "Result";
    private const int StationSelectorRowIndex = 3;
    private const string VersionPrefix = "v";
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _realtimePreviewPaintTimer = new() { Interval = RealtimePreviewPaintIntervalMs };
    private readonly ILocalizationService _localizer;
    private readonly IAppSettingsService _settingsService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IMesConnectionMonitorService _mesConnectionMonitorService;
    private readonly IPlcProductionMonitorService _plcProductionMonitorService;
    private readonly IPlcWorkIdMonitorService _plcWorkIdMonitorService;
    private readonly IPlcWeldCycleMonitorService _plcWeldCycleMonitorService;
    private readonly IPlcAddressService _plcAddressService;
    private readonly IPlcExpressionReadService _plcExpressionReadService;
    private readonly IProductProcessConfigService _productProcessConfigService;
    private readonly ITestSchemeConfigService _testSchemeConfigService;
    private readonly IProductRealtimePreviewService _productRealtimePreviewService;
    private readonly IProgramManageService _programManageService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly IProductionFlowLogService _productionLogService;
    private bool _syncingLanguageSelection;
    private bool _syncingStationSelection;
    private bool _dualStationModeEnabled;
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
    private bool _refreshingSchemePreview;
    private bool _weldParameterTableBound;
    private bool _realtimePreviewApplyPosted;
    private bool _syncingWeldPreviewHorizontalScroll;
    private bool _adjustingWeldPreviewHostHeight;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public MonitorView(
        ILocalizationService localizer,
        IAppSettingsService settingsService,
        IPlcCommunicationService plcCommunicationService,
        IMesConnectionMonitorService mesConnectionMonitorService,
        IPlcProductionMonitorService plcProductionMonitorService,
        IPlcWorkIdMonitorService plcWorkIdMonitorService,
        IPlcWeldCycleMonitorService plcWeldCycleMonitorService,
        IPlcAddressService plcAddressService,
        IPlcExpressionReadService plcExpressionReadService,
        IProductProcessConfigService productProcessConfigService,
        ITestSchemeConfigService testSchemeConfigService,
        IProductRealtimePreviewService productRealtimePreviewService,
        IProgramManageService programManageService,
        IWeldTaskService weldTaskService,
        IProgramExceptionLogService exceptionLogService,
        IProductionFlowLogService productionLogService)
    {
        InitializeComponent();

        _localizer = localizer;
        _settingsService = settingsService;
        _plcCommunicationService = plcCommunicationService;
        _mesConnectionMonitorService = mesConnectionMonitorService;
        _plcProductionMonitorService = plcProductionMonitorService;
        _plcWorkIdMonitorService = plcWorkIdMonitorService;
        _plcWeldCycleMonitorService = plcWeldCycleMonitorService;
        _plcAddressService = plcAddressService;
        _plcExpressionReadService = plcExpressionReadService;
        _productProcessConfigService = productProcessConfigService;
        _testSchemeConfigService = testSchemeConfigService;
        _productRealtimePreviewService = productRealtimePreviewService;
        _programManageService = programManageService;
        _weldTaskService = weldTaskService;
        _exceptionLogService = exceptionLogService;
        _productionLogService = productionLogService;

        LoadTitleLogo();
        ConfigureHeaderLayout();
        ConfigureRuntimeMessagePanels();
        ConfigureStationResultTags();
        ApplyLocalizedTexts();
        ConfigureStationSelector();
        ConfigureTables();
        ConfigureProductionTableColumns();
        ConfigureWeldParameterTableColumns();
        WireEvents();
        BindSessionInfo();
        BindProductionRuntimeState();
        RefreshRuntimePanels();
        ApplyPlcStatus(_plcCommunicationService.Current);
        ApplyMesStatus(_mesConnectionMonitorService.Current);
        ApplyProductionStatus(GetCurrentProductionSnapshot());
        QueueRefreshSchemePreview(force: true);
        AdjustTitleFontSize();
    }

    /// <summary>
    /// 从输出目录加载标题 Logo；资源缺失时隐藏图片控件，避免界面出现空白占位图标。
    /// </summary>
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

        tlpLeftTop.AutoSize = false;
        tlpCommunicationStatus.MinimumSize = new Size(HeaderStatusCellMinWidth * 2, 0);

        GetVersion();
        ConfigureStatusTag(tagMes);
        ConfigureStatusTag(tagPLC);
        ConfigureStatusTag(tagDeviceStatus);
        ConfigureStatusTag(tagTaskStatus);
        AdjustHeaderFixedColumns();
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
    /// Report buttons keep a readable minimum width because icons consume part of the text area.
    /// </summary>
    private void ConfigureReportButton(AntdUI.Button button)
    {
        button.Font = _headerButtonFont;
        button.MinimumSize = new Size(HeaderActionMinWidth, 0);
        button.AutoEllipsis = false;
        button.Shape = AntdUI.TShape.Default;
        button.TextCenterHasIcon = true;
        button.TextMultiLine = false;
    }

    /// <summary>
    /// The title column can shrink, while status cards and action buttons reserve measured widths.
    /// </summary>
    private void AdjustHeaderFixedColumns()
    {
        if (tlpLeftTop.ColumnStyles.Count < 4)
        {
            return;
        }

        var logoWidth = picLogo.Visible ? HeaderLogoWidth : 0;
        var statusWidth = CalculateHeaderStatusWidth();
        var actionWidth = CalculateHeaderActionWidth();

        tlpLeftTop.ColumnStyles[0].SizeType = SizeType.Absolute;
        tlpLeftTop.ColumnStyles[0].Width = logoWidth;
        tlpLeftTop.ColumnStyles[1].SizeType = SizeType.Percent;
        tlpLeftTop.ColumnStyles[1].Width = 100F;
        tlpLeftTop.ColumnStyles[2].SizeType = SizeType.Absolute;
        tlpLeftTop.ColumnStyles[2].Width = statusWidth;
        tlpLeftTop.ColumnStyles[3].SizeType = SizeType.Absolute;
        tlpLeftTop.ColumnStyles[3].Width = actionWidth;

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

    /// <summary>
    /// 异常和运行状态是操作员最先看的信息，因此使用更大的加粗字体和状态色增强识别度。
    /// </summary>
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

    /// <summary>
    /// 工位结果标签用于显示最近一次采集结果。当前焊接信号只有一组，因此先启用工位 1。
    /// </summary>
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

    /// <summary>
    /// 根据系统设置决定是否显示工位选择。默认单工位模式下强制回到工位 1，避免误操作到隐藏工位。
    /// </summary>
    private void ConfigureStationSelector()
    {
        _dualStationModeEnabled = _settingsService.Get().EnableDualStationMode;
        SetStationSelectorVisible(_dualStationModeEnabled);
        UpdateStationResultLayout();

        if (!_dualStationModeEnabled && CurrentStationNo != ProductionConstants.Stations.DefaultStationNo)
        {
            _weldTaskService.SelectStation(ProductionConstants.Stations.DefaultStationNo);
        }

        BindStationSelection();
    }

    /// <summary>
    /// 单工位模式下只显示工位1并铺满；双工位模式下两个结果标签各占一半。
    /// </summary>
    private void UpdateStationResultLayout()
    {
        if (tableLayoutPanel1.ColumnStyles.Count < 2)
        {
            return;
        }

        tagStation1.Visible = true;
        tagStation2.Visible = _dualStationModeEnabled;

        tableLayoutPanel1.ColumnStyles[0].SizeType = SizeType.Percent;
        tableLayoutPanel1.ColumnStyles[0].Width = _dualStationModeEnabled ? 50F : 100F;
        tableLayoutPanel1.ColumnStyles[1].SizeType = _dualStationModeEnabled
            ? SizeType.Percent
            : SizeType.Absolute;
        tableLayoutPanel1.ColumnStyles[1].Width = _dualStationModeEnabled ? 50F : 0F;
    }

    private void SetStationSelectorVisible(bool visible)
    {
        tableLayoutPanel10.Visible = visible;

        if (TLPWorkOrderInfo.RowStyles.Count <= StationSelectorRowIndex)
        {
            return;
        }

        TLPWorkOrderInfo.RowStyles[StationSelectorRowIndex].SizeType = visible
            ? SizeType.Percent
            : SizeType.Absolute;
        TLPWorkOrderInfo.RowStyles[StationSelectorRowIndex].Height = visible ? 10F : 0F;
    }

    private void BindStationSelection()
    {
        _syncingStationSelection = true;
        try
        {
            selectStation.Items.Clear();
            selectStation.Items.AddRange(new object[]
            {
                FormatStationName(1),
                FormatStationName(2)
            });
            SyncStationSelection();
        }
        finally
        {
            _syncingStationSelection = false;
        }
    }

    private void SyncStationSelection()
    {
        if (selectStation.Items.Count == 0)
        {
            return;
        }

        var index = Math.Max(0, Math.Min(selectStation.Items.Count - 1, CurrentStationNo - 1));
        if (selectStation.SelectedIndex != index)
        {
            selectStation.SelectedIndex = index;
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
            var stationNo = _weldTaskService.CurrentState.CurrentStationNo;
            return stationNo <= 0
                ? ProductionConstants.Stations.DefaultStationNo
                : stationNo;
        }
    }

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

    /// <summary>
    /// 标题文字或容器尺寸变化后，重新计算一个尽量填满区域但不溢出的字号。
    /// </summary>
    private void TitleLayout_Changed(object? sender, EventArgs e)
    {
        AdjustHeaderFixedColumns();
        AdjustTitleFontSize();
    }

    /// <summary>
    /// 使用二分查找寻找最大可用字号，比逐级递增更稳定，也能减少频繁重绘。
    /// </summary>
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

    /// <summary>
    /// 测量单行标题在指定区域内能使用的最大字号。
    /// </summary>
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

    /// <summary>
    /// 统一绑定事件，方便后续维护。
    /// </summary>
    private void WireEvents()
    {
        Load += MonitorView_Load;
        _timer.Tick += Timer_Tick;
        _realtimePreviewPaintTimer.Tick += RealtimePreviewPaintTimer_Tick;
        tlpLeftTop.SizeChanged += TitleLayout_Changed;
        lblTitle.SizeChanged += TitleLayout_Changed;
        lblTitle.TextChanged += TitleLayout_Changed;
        btnSwitchUser.Click += SwitchUser_Click;
        btnLogout.Click += Logout_Click;
        btnChangeWO.Click += ChangeWorkOrder_Click;
        btnEditWO.Click += EditWorkOrder_Click;
        btnExpStart.Click += StartReport_Click;
        btnExpEnd.Click += FinishReport_Click;
        btnAddressPreview.Click += AddressPreview_Click;
        splitter1.SizeChanged += Splitter1_SizeChanged;
        table2.MouseEnter += Table2_MouseEnter;
        table2.MouseWheel += Table2_MouseWheel;
        table2.Scroll += Table2_Scroll;
        table2.SizeChanged += Table2_ScrollRangeChanged;
        table2.ColumnWidthChanged += Table2_ScrollRangeChanged;
        table2.ColumnAdded += Table2_ScrollRangeChanged;
        table2.ColumnRemoved += Table2_ScrollRangeChanged;
        table2HorizontalScrollBar.ValueChanged += Table2HorizontalScrollBar_ValueChanged;
        table2HorizontalScrollBar.VisibleChanged += Table2HorizontalScrollBar_VisibleChanged;
        select_Lang.SelectedIndexChanged += Language_SelectedIndexChanged;
        selectStation.SelectedIndexChanged += Station_SelectedIndexChanged;
        GlobalContext.SessionChanged += GlobalContext_SessionChanged;
        _weldTaskService.StateChanged += WeldTaskService_StateChanged;
        _plcCommunicationService.StatusChanged += PlcCommunicationService_StatusChanged;
        _mesConnectionMonitorService.StatusChanged += MesConnectionMonitorService_StatusChanged;
        _plcProductionMonitorService.StatusChanged += PlcProductionMonitorService_StatusChanged;
        _plcWorkIdMonitorService.WorkIdChanged += PlcWorkIdMonitorService_WorkIdChanged;
        _plcWeldCycleMonitorService.WeldPointCollected += PlcWeldCycleMonitorService_WeldPointCollected;
        _productRealtimePreviewService.SnapshotChanged += ProductRealtimePreviewService_SnapshotChanged;
        _productionLogService.LogWritten += ProductionLogService_LogWritten;
    }

    private void Splitter1_SizeChanged(object? sender, EventArgs e)
    {
        AdjustWeldPreviewHostHeight();
    }

    private void Table2_MouseEnter(object? sender, EventArgs e)
    {
        if (table2.CanFocus)
        {
            table2.Focus();
        }
    }

    private void Table2_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (GetWeldPreviewMaxHorizontalOffset() <= 0)
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
            table2.HorizontalScrollingOffset + direction * wheelSteps * WeldPreviewMouseWheelPixels);
    }

    private void Table2_Scroll(object? sender, ScrollEventArgs e)
    {
        if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
        {
            SyncWeldPreviewHorizontalScrollBar();
        }
    }

    private void Table2_ScrollRangeChanged(object? sender, EventArgs e)
    {
        SyncWeldPreviewHorizontalScrollBar();
        AdjustWeldPreviewHostHeight();
    }

    private void Table2HorizontalScrollBar_ValueChanged(object? sender, EventArgs e)
    {
        if (_syncingWeldPreviewHorizontalScroll)
        {
            return;
        }

        SetWeldPreviewHorizontalOffset(table2HorizontalScrollBar.Value);
    }

    private void Table2HorizontalScrollBar_VisibleChanged(object? sender, EventArgs e)
    {
        AdjustWeldPreviewHostHeight();
    }

    /// <summary>
    /// 语言变化时，只补刷新运行时动态文本。
    /// </summary>
    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ConfigureStationSelector();
        BindSessionInfo();
        BindLanguageSelection();
        BindProductionRuntimeState();
        ConfigureProductionTableColumns();
        ConfigureWeldParameterTableColumns();
        RefreshRuntimePanels();
        ApplyPlcStatus(_plcCommunicationService.Current);
        ApplyMesStatus(_mesConnectionMonitorService.Current);
        ApplyProductionStatus(GetCurrentProductionSnapshot());
        QueueRefreshSchemePreview(force: true);
        AdjustTitleFontSize();
    }

    private void MonitorView_Load(object? sender, EventArgs e)
    {
        _timer.Start();
        _realtimePreviewPaintTimer.Start();
        ApplyLocalizedTexts();
        UpdateCurrentTime();
        BindSessionInfo();
        BindLanguageSelection();
        ConfigureStationSelector();
        _weldTaskService.RestoreUnfinishedTask(CurrentStationNo);
        BindProductionRuntimeState();
        RefreshRuntimePanels();
        ApplyPlcStatus(_plcCommunicationService.Current);
        ApplyMesStatus(_mesConnectionMonitorService.Current);
        ApplyProductionStatus(GetCurrentProductionSnapshot());
        ApplyCurrentRealtimePreviewSnapshot();
        AdjustTitleFontSize();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        GlobalContext.SessionChanged -= GlobalContext_SessionChanged;
        _weldTaskService.StateChanged -= WeldTaskService_StateChanged;
        _plcCommunicationService.StatusChanged -= PlcCommunicationService_StatusChanged;
        _mesConnectionMonitorService.StatusChanged -= MesConnectionMonitorService_StatusChanged;
        _plcProductionMonitorService.StatusChanged -= PlcProductionMonitorService_StatusChanged;
        _plcWorkIdMonitorService.WorkIdChanged -= PlcWorkIdMonitorService_WorkIdChanged;
        _plcWeldCycleMonitorService.WeldPointCollected -= PlcWeldCycleMonitorService_WeldPointCollected;
        _productRealtimePreviewService.SnapshotChanged -= ProductRealtimePreviewService_SnapshotChanged;
        _productionLogService.LogWritten -= ProductionLogService_LogWritten;
        _timer.Stop();
        _realtimePreviewPaintTimer.Stop();
        _timer.Dispose();
        _realtimePreviewPaintTimer.Dispose();
        _titleFont?.Dispose();
        _headerStatusFont?.Dispose();
        _headerButtonFont?.Dispose();
        _runtimeMessageFont?.Dispose();
        _runtimeGroupFont?.Dispose();
        base.OnHandleDestroyed(e);
    }

    private void GlobalContext_SessionChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(BindSessionInfo);
            return;
        }

        BindSessionInfo();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateCurrentTime();

        if (_weldTaskService.CurrentState.CurrentWorkOrder is null)
        {
            QueueRefreshSchemePreview(force: false);
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
            BeginInvoke(RefreshProductionRuntimeState);
            return;
        }

        RefreshProductionRuntimeState();
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

    private void SwitchUser_Click(object? sender, EventArgs e)
    {
        if (!ConfirmAction(TextKeys.Monitor.Message.SwitchUserConfirm, TextKeys.Monitor.Title.SwitchUserTitle))
        {
            return;
        }

        GlobalContext.IsLogout = true;
        FindForm()?.Close();
    }

    private void Logout_Click(object? sender, EventArgs e)
    {
        if (!ConfirmAction(TextKeys.Monitor.Message.LogoutConfirm, TextKeys.Monitor.Title.LogoutTitle))
        {
            return;
        }

        GlobalContext.IsLogout = true;
        FindForm()?.Close();
    }

    private async void ChangeWorkOrder_Click(object? sender, EventArgs e)
    {
        if (_weldTaskService.RestoreUnfinishedTask(CurrentStationNo) is not null)
        {
            ShowWarning(TextKeys.Monitor.Message.StartBlockedByUnfinishedTask);
            return;
        }

        await PrepareWorkOrderAndProgramAsync(forceManualInput: true);
    }

    private async void EditWorkOrder_Click(object? sender, EventArgs e)
    {
        var state = _weldTaskService.CurrentState;
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

    private void AddressPreview_Click(object? sender, EventArgs e)
    {
        var rows = BuildCurrentAddressPreviewRows();
        using var form = new AddressPreviewForm(rows, _plcExpressionReadService, _localizer);
        form.ShowDialog(this);
    }

    private async void StartReport_Click(object? sender, EventArgs e)
    {
        var stationNo = CurrentStationNo;
        if (_weldTaskService.RestoreUnfinishedTask(stationNo) is not null)
        {
            RefreshProductionRuntimeState();
            ShowWarning(TextKeys.Monitor.Message.StartBlockedByUnfinishedTask);
            return;
        }

        if (ShouldPrepareWorkOrderBeforeStart()
            && !await PrepareWorkOrderAndProgramAsync(forceManualInput: false))
        {
            return;
        }

        var state = _weldTaskService.CurrentState;
        if (state.CurrentWorkOrder is not null
            && state.SelectedProcess is not null
            && state.SelectedProgram is null
            && !await PrepareProgramForCurrentWorkOrderAsync(stationNo))
        {
            return;
        }

        state = _weldTaskService.CurrentState;
        if (state.CurrentWorkOrder is null || state.SelectedProcess is null || state.SelectedProgram is null)
        {
            ShowWarning(TextKeys.Monitor.Message.StartPrerequisiteMissing);
            return;
        }

        if (!IsProgramContentConfirmed(state.SelectedProgram, stationNo)
            && !TryConfirmStartData(state.CurrentWorkOrder, state.SelectedProcess, state.SelectedProgram, stationNo))
        {
            return;
        }

        if (!await PrepareRecipeBeforeStartAsync(state.SelectedProgram, stationNo)
            || !await ValidateRecipeBeforeStartAsync(state.SelectedProgram, stationNo))
        {
            return;
        }

        var production = GetCurrentProductionSnapshot();
        if (!TryPromptStartActualQuantity(production, out var actualQty))
        {
            return;
        }

        var employeeNumber = await PromptValidatedOperatorAsync(stationNo);
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return;
        }

        await RunUiOperationAsync(async () =>
        {
            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.SubmittingStart);
            var task = await _weldTaskService.StartAsync(employeeNumber, actualQty, stationNo, employeeAlreadyValidated: true);
            RefreshProductionRuntimeState();
            ShowInfo(TextKeys.Monitor.Message.StartSuccess, task.ExpStartId ?? string.Empty);
        });
    }

    private async void FinishReport_Click(object? sender, EventArgs e)
    {
        var stationNo = CurrentStationNo;
        var activeTask = _weldTaskService.RestoreUnfinishedTask(stationNo);
        if (activeTask is null)
        {
            ShowWarning(TextKeys.Monitor.Message.FinishPrerequisiteMissing);
            return;
        }

        var employeeNumber = await PromptValidatedOperatorAsync(stationNo);
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return;
        }

        var production = GetCurrentProductionSnapshot();
        var defaultActual = Math.Max(1, production.TotalProduction > 0 ? production.TotalProduction : activeTask.ActualQty);
        if (!TryPromptPositiveInt(TextKeys.Monitor.Dialog.ActualQuantityTitle, TextKeys.Monitor.Dialog.ActualQuantityPrompt, defaultActual, out var actualQty)
            || !TryPromptNonNegativeInt(TextKeys.Monitor.Dialog.QualifiedQuantityTitle, TextKeys.Monitor.Dialog.QualifiedQuantityPrompt, production.AcceptedQuantity, out var qualifiedQty)
            || !TryPromptNonNegativeInt(TextKeys.Monitor.Dialog.FailedQuantityTitle, TextKeys.Monitor.Dialog.FailedQuantityPrompt, production.RejectedQuantity, out var failedQty))
        {
            return;
        }

        await RunUiOperationAsync(async () =>
        {
            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.SubmittingFinish);
            await _weldTaskService.FinishAsync(employeeNumber, actualQty, qualifiedQty, failedQty, stationNo);
            RefreshProductionRuntimeState();
            ShowInfo(TextKeys.Monitor.Message.FinishSuccess);
        });
    }

    private bool ShouldPrepareWorkOrderBeforeStart()
    {
        var state = _weldTaskService.CurrentState;
        if (state.CurrentWorkOrder is null || state.SelectedProcess is null)
        {
            return true;
        }

        var plcWorkId = GetCurrentLiveWorkId();
        return !string.IsNullOrWhiteSpace(plcWorkId)
            && !string.Equals(state.CurrentWorkOrder.SN, plcWorkId, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> PrepareWorkOrderAndProgramAsync(bool forceManualInput)
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

            if (!TrySelectProcess(workOrder.ExpItems, out var process))
            {
                return;
            }

            _weldTaskService.SelectProcess(process, stationNo);
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.LoadingPrograms);
            var programs = await _weldTaskService.LoadProgramsAsync(stationNo);
            if (programs.Count == 0)
            {
                ShowBusinessWarning(
                    "MES.GetProgramList",
                    TextKeys.Monitor.Message.ProgramListEmpty,
                    "MES返回的程序列表为空。",
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
                    "MES程序详情下载失败或返回空数据。",
                    FormatProgram(program));
                return;
            }

            if (!TryConfirmStartData(_weldTaskService.CurrentState.CurrentWorkOrder, _weldTaskService.CurrentState.SelectedProcess, detail, stationNo))
            {
                return;
            }

            RefreshProductionRuntimeState();
            ShowInfo(TextKeys.Monitor.Message.WorkOrderReady);
            isReady = true;
        });

        return isReady;
    }

    private async Task<bool> PrepareProgramForCurrentWorkOrderAsync(int stationNo)
    {
        var isReady = false;
        await RunUiOperationAsync(async () =>
        {
            var state = _weldTaskService.CurrentState;
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

            if (!TryConfirmStartData(_weldTaskService.CurrentState.CurrentWorkOrder, _weldTaskService.CurrentState.SelectedProcess, detail, stationNo))
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

    private void Language_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingLanguageSelection)
        {
            return;
        }

        var targetLanguage = select_Lang.SelectedIndex == 0
            ? AppConstants.Languages.Chinese
            : AppConstants.Languages.English;

        _localizer.SetLanguage(targetLanguage);
    }

    private void Station_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingStationSelection || !_dualStationModeEnabled)
        {
            return;
        }

        var stationNo = Math.Max(ProductionConstants.Stations.DefaultStationNo, selectStation.SelectedIndex + 1);
        if (stationNo == CurrentStationNo)
        {
            return;
        }

        _weldTaskService.SelectStation(stationNo);
        _weldTaskService.RestoreUnfinishedTask(stationNo);
        RefreshProductionRuntimeState();
        QueueRefreshSchemePreview(force: true);
        ApplyCurrentRealtimePreviewSnapshot();
        SyncStationSelection();
    }

    /// <summary>
    /// 会话信息里的“未登录”文本要随语言一起切换。
    /// </summary>
    private void BindSessionInfo()
    {
        var user = GlobalContext.CurrentUser;
        lblCurrentUser.Text = user is null
            ? _localizer.GetString(TextKeys.Common.StatusNotLoggedIn)
            : $"{user.UserName} ({user.UserNumber})";
    }

    private void BindProductionRuntimeState()
    {
        var state = _weldTaskService.CurrentState;
        var workOrder = state.CurrentWorkOrder;
        var process = state.SelectedProcess;
        var program = state.SelectedProgram;
        var liveWorkId = GetCurrentLiveWorkId();

        SyncStationSelection();
        inputSN.Text = !string.IsNullOrWhiteSpace(liveWorkId) ? liveWorkId : workOrder?.SN ?? string.Empty;
        inputProdNum.Text = workOrder?.ProdNum ?? _currentProductIdentity?.ProductNum ?? string.Empty;
        inputBatch.Text = workOrder?.Batch ?? string.Empty;
        inputProductName.Text = workOrder?.ProductName ?? string.Empty;
        inputDrawingNo.Text = workOrder?.DrawingNo ?? string.Empty;
        inputProdModel.Text = workOrder?.ProdModel ?? _currentProductIdentity?.ProductModel ?? string.Empty;
        inputSpec.Text = workOrder?.Spec ?? string.Empty;
        inputProcessNo.Text = process?.ProcessNo ?? string.Empty;
        inputItemName.Text = process?.ItemName ?? string.Empty;
        inputProgramName.Text = program?.ProgramName ?? string.Empty;
        ApplyTaskStatusTag(state);
    }

    private void ApplyTaskStatusTag(ProductionRuntimeState state)
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
        BindWeldParameterRows(record);
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
            "ProductCollectionFeedback" or
            "ProcessParameterUploadSucceeded" or
            "ProcessParameterUploadFailed" or
            "ReportFileGenerated" or
            "ReportFileUploadSucceeded" or
            "ReportFileUploadFailed";
    }

    private void ApplyProductionHint(ProductionFlowLogEntry entry)
    {
        if (entry.Level.Equals("Error", StringComparison.OrdinalIgnoreCase))
        {
            SetRuntimeErrorText(ToProductionHintText(entry));
            return;
        }

        ClearRuntimeError();
        SetRuntimeStatusText(ToProductionHintText(entry), isSuccess: true);
    }

    private static string ToProductionHintText(ProductionFlowLogEntry entry)
    {
        return entry.Step switch
        {
            "ProductDataReady" => "PLC触发采集数据",
            "ProductCollectionStart" => "正在采集产品数据",
            "ProductDataReadStart" => "正在采集产品数据",
            "ProductDataSaved" => "数据保存成功",
            "ProductCollectionFeedback" => entry.Level.Equals("Error", StringComparison.OrdinalIgnoreCase)
                ? "PLC采集反馈失败"
                : "PLC采集反馈成功",
            "ProcessParameterUploadSucceeded" => "数据上传成功",
            "ProcessParameterUploadFailed" => "数据上传失败",
            "ReportFileGenerated" => "报告生成成功",
            "ReportFileUploadSucceeded" => "报告上传成功",
            "ReportFileUploadFailed" => "报告上传失败",
            _ => entry.Summary
        };
    }

    private void ApplyStationResult(BizWeldPointRecord record)
    {
        if (record.StationNo == 2 && !_dualStationModeEnabled)
        {
            return;
        }

        var tag = record.StationNo == 2 ? tagStation2 : tagStation1;
        var resultText = NormalizeStationResultText(record.TestResultRaw ?? record.TestResult);

        UpdateStationResultLayout();
        tag.Text = $"工位{record.StationNo}{resultText}";
        tag.ForeColor = Color.White;
        tag.BackColor = string.Equals(resultText, ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase)
            ? UiColors.Status.Success
            : UiColors.Status.Danger;
    }

    /// <summary>
    /// PLC 原始值 3 表示 OK，其余有效结果统一显示为 NG。
    /// </summary>
    private static string NormalizeStationResultText(string? rawResult)
    {
        return string.Equals(rawResult?.Trim(), ProductionConstants.TestResults.OkRawValue, StringComparison.Ordinal)
            || string.Equals(rawResult?.Trim(), ProductionConstants.TestResults.Ok, StringComparison.OrdinalIgnoreCase)
            ? ProductionConstants.TestResults.Ok
            : ProductionConstants.TestResults.Ng;
    }

    /// <summary>
    /// 下拉框选项不是资源控件属性，所以这里手动刷新。
    /// </summary>
    private void BindLanguageSelection()
    {
        _syncingLanguageSelection = true;

        if (select_Lang.Items.Count > 0)
        {
            select_Lang.Items.Clear();
        }

        select_Lang.Items.AddRange(new object[]
        {
            _localizer.GetString(TextKeys.Common.LanguageChinese),
            _localizer.GetString(TextKeys.Common.LanguageEnglish)
        });

        select_Lang.SelectedIndex = GlobalContext.CurrentLanguage == AppConstants.Languages.English ? 1 : 0;

        _syncingLanguageSelection = false;
    }

    private void UpdateCurrentTime()
    {
        lblCurTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Static texts are refreshed here so designer hard-coded Chinese is not shown after language switching.
    /// </summary>
    private void ApplyLocalizedTexts()
    {
        lblTitle.Text = _localizer.GetString(TextKeys.Monitor.Title.AppTitle);
        btnExpStart.Text = _localizer.GetString(TextKeys.Monitor.Button.StartReport);
        btnExpEnd.Text = _localizer.GetString(TextKeys.Monitor.Button.FinishReport);
        btnChangeWO.Text = _localizer.GetString(TextKeys.Monitor.Button.ChangeWorkOrder);
        btnEditWO.Text = _localizer.GetString(TextKeys.Monitor.Button.EditWO);
        btnSwitchUser.Text = _localizer.GetString(TextKeys.Monitor.Button.SwitchUser);
        btnLogout.Text = _localizer.GetString(TextKeys.Monitor.Button.Logout);
        grpErrorTips.Text = _localizer.GetString(TextKeys.Monitor.Group.ExceptionTips);
        grpRunningStatus.Text = _localizer.GetString(TextKeys.Monitor.Group.RunningStatus);
        //groupStationOverview.Text = "工位产品概览";
        table1.Text = _localizer.GetString(TextKeys.Monitor.Group.ProductionMetrics);
        table2.Text = "实时测试结果";

        lblCurUser.Text = _localizer.GetString(TextKeys.Monitor.Label.CurrentUser);
        lblCurLang.Text = _localizer.GetString(TextKeys.Monitor.Label.CurrentLang);
        lblStation.Text = _localizer.GetString(TextKeys.Monitor.Label.Station);
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
        lblLiveStation.Text = "工位";
        lblLiveProductNo.Text = "产品编号";
        lblLiveProductNum.Text = "产品工号";
        lblLiveProductModel.Text = "产品型号";
        lblLiveScheme.Text = "方案";
        lblLiveTouchCount.Text = "焊点数量";
        lblLiveResult.Text = "产品结果";
        lblLiveRefreshTime.Text = "刷新时间";
        lblLiveHint.Text = "实时预览服务每 1 秒读取当前工位测试值，采集到新快照后立即刷新界面。";
        btnAddressPreview.Text = "PLC 地址预览";
        AdjustHeaderFixedColumns();
    }

    /// <summary>
    /// 将 PLC 状态快照转换成监控页右侧状态标签的文字和颜色。
    /// </summary>
    private void ApplyPlcStatus(PlcConnectionSnapshot snapshot)
    {
        tagPLC.Text = $"PLC\r\n{_localizer.GetString(GetPlcStateKey(snapshot.State))}";
        tagPLC.ForeColor = Color.White;
        tagPLC.BackColor = snapshot.State switch
        {
            PlcConnectionState.Connected => UiColors.Status.Success,
            PlcConnectionState.Connecting or PlcConnectionState.Reconnecting => UiColors.Status.Warning,
            PlcConnectionState.Stopped => UiColors.Status.Muted,
            _ => UiColors.Status.Danger
        };
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
        if (snapshot.StationNo != CurrentStationNo)
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
        var mesProductionQuantity = _weldTaskService.CurrentState.SelectedProcess?.StartAmount;
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
            new(_localizer.GetString(TextKeys.Production.MesProductionQuantity), FormatNullable(mesProductionQuantity)),
            new(_localizer.GetString(TextKeys.Production.AchievementRate), FormatRate(achievementRate))
        };

        table1.DataSource = rows;
        table1.Refresh();
    }

    private void ConfigureProductionTableColumns()
    {
        table1.Columns.Clear();
        table1.Columns.Add(new AntdUI.Column(nameof(ProductionMetricRow.Name), _localizer.GetString(TextKeys.Production.MetricName))
        {
            Ellipsis = true
        });
        table1.Columns.Add(new AntdUI.Column(nameof(ProductionMetricRow.Value), _localizer.GetString(TextKeys.Production.MetricValue))
        {
            Ellipsis = true
        });
        TableStyleHelper.ApplyAntdColumnDefaults(table1);
    }

    private void ConfigureWeldParameterTableColumns()
    {
        table2.AutoGenerateColumns = false;
        _weldParameterLayoutKey = string.Empty;
        _weldParameterPreviewSchemaKey = string.Empty;
        _weldParameterVisibleValueKey = string.Empty;
        _weldParameterTableBound = false;
        BindWeldParameterTable(forceRebind: true);
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
                row.Value = FormatNullableText(FindRecordValue(record, row, rawValues));
                row.UpperValue = FormatNullableText(FindRawValue(rawValues, $"{row.ItemKey}_upper", $"{row.ParameterName}上限"));
                row.LowerValue = FormatNullableText(FindRawValue(rawValues, $"{row.ItemKey}_lower", $"{row.ParameterName}下限"));
                row.Result = FormatTestResultText(FindRecordResult(record, row, rawValues));
                row.RecordTime = record.RecordTime.ToString("HH:mm:ss");
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
            AdjustWeldPreviewHostHeight();
            return;
        }

        var touchGroups = ResolvePreviewTouchGroups(_weldParameterRows);
        if (table2.Rows.Count != touchGroups.Count)
        {
            BindWeldParameterTable(forceRebind: true);
            return;
        }

        FillWeldPreviewRows(items, touchGroups);
        AdjustWeldPreviewHostHeight();
    }

    /// <summary>
    /// Rebuilds the unbound pivot table: one row per weld point and one column group per test item.
    /// </summary>
    private void RebuildWeldParameterPreviewTable()
    {
        var items = ResolveWeldPreviewItems(_weldParameterRows);
        SetControlRedraw(table2, enabled: false);
        table2.SuspendLayout();
        try
        {
            table2.Rows.Clear();
            table2.Columns.Clear();

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
                    AddWeldPreviewColumn(BuildPreviewColumnName(item.Index, PreviewUpperRole), $"{item.Name}上限", 118);
                    AddWeldPreviewColumn(BuildPreviewColumnName(item.Index, PreviewLowerRole), $"{item.Name}下限", 118);
                    AddWeldPreviewColumn(BuildPreviewColumnName(item.Index, PreviewActualRole), $"{item.Name}实际值", 136);
                    AddWeldPreviewColumn(BuildPreviewColumnName(item.Index, PreviewResultRole), $"{item.Name}结果", 118);
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
            table2.ResumeLayout(false);
            SetControlRedraw(table2, enabled: true);
            RedrawControl(table2);
            SyncWeldPreviewHorizontalScrollBar();
            AdjustWeldPreviewHostHeight();
        }
    }

    /// <summary>
    /// 暂停或恢复控件绘制，避免结构重建时用户看到清空过程。
    /// </summary>
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
    /// 让实时预览区域按焊点行数调整高度，但超过上限后保留纵向滚动。
    /// </summary>
    private void AdjustWeldPreviewHostHeight()
    {
        if (_adjustingWeldPreviewHostHeight || splitter1.Height <= 0)
        {
            return;
        }

        var targetHeight = CalculateWeldPreviewHostHeight();
        var minDistance = Math.Max(1, splitter1.Panel1MinSize);
        var maxDistance = Math.Max(
            minDistance,
            splitter1.Height - splitter1.SplitterWidth - splitter1.Panel2MinSize);
        var nextDistance = Math.Clamp(targetHeight, minDistance, maxDistance);
        if (Math.Abs(splitter1.SplitterDistance - nextDistance) < WeldPreviewHeightTolerance)
        {
            return;
        }

        _adjustingWeldPreviewHostHeight = true;
        try
        {
            splitter1.SplitterDistance = nextDistance;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            _exceptionLogService.Write(ex, "MonitorView.AdjustWeldPreviewHostHeight");
        }
        finally
        {
            _adjustingWeldPreviewHostHeight = false;
        }
    }

    private int CalculateWeldPreviewHostHeight()
    {
        var rowCount = Math.Clamp(
            Math.Max(table2.Rows.Count, WeldPreviewMinVisibleRows),
            WeldPreviewMinVisibleRows,
            WeldPreviewMaxVisibleRows);
        var rowHeight = ResolveWeldPreviewRowHeight();
        var headerHeight = table2.ColumnHeadersVisible ? table2.ColumnHeadersHeight : 0;
        var scrollBarHeight = table2HorizontalScrollBar.Visible ? table2HorizontalScrollBar.Height : 0;
        return headerHeight + rowCount * rowHeight + scrollBarHeight + WeldPreviewHeightPadding;
    }

    private int ResolveWeldPreviewRowHeight()
    {
        var firstVisibleRow = table2.Rows
            .Cast<DataGridViewRow>()
            .FirstOrDefault(row => row.Visible);
        if (firstVisibleRow?.Height > 0)
        {
            return firstVisibleRow.Height;
        }

        return table2.RowTemplate.Height > 0
            ? table2.RowTemplate.Height
            : 30;
    }

    private void SetWeldPreviewHorizontalOffset(int requestedOffset)
    {
        var contentWidth = GetWeldPreviewContentWidth();
        var viewportWidth = GetWeldPreviewViewportWidth();
        var maxOffset = Math.Max(0, contentWidth - viewportWidth);
        var nextOffset = Math.Clamp(requestedOffset, 0, maxOffset);

        _syncingWeldPreviewHorizontalScroll = true;
        try
        {
            if (table2.HorizontalScrollingOffset != nextOffset)
            {
                table2.HorizontalScrollingOffset = nextOffset;
            }

            table2HorizontalScrollBar.SetScrollInfo(contentWidth, viewportWidth, nextOffset);
        }
        catch (ArgumentOutOfRangeException)
        {
            table2.HorizontalScrollingOffset = 0;
            table2HorizontalScrollBar.SetScrollInfo(contentWidth, viewportWidth, 0);
        }
        finally
        {
            _syncingWeldPreviewHorizontalScroll = false;
        }
    }

    private void SyncWeldPreviewHorizontalScrollBar()
    {
        if (_syncingWeldPreviewHorizontalScroll)
        {
            return;
        }

        var contentWidth = GetWeldPreviewContentWidth();
        var viewportWidth = GetWeldPreviewViewportWidth();
        var maxOffset = Math.Max(0, contentWidth - viewportWidth);
        var offset = Math.Clamp(table2.HorizontalScrollingOffset, 0, maxOffset);

        _syncingWeldPreviewHorizontalScroll = true;
        try
        {
            if (table2.HorizontalScrollingOffset != offset)
            {
                table2.HorizontalScrollingOffset = offset;
            }

            table2HorizontalScrollBar.SetScrollInfo(contentWidth, viewportWidth, offset);
        }
        finally
        {
            _syncingWeldPreviewHorizontalScroll = false;
        }
    }

    private int GetWeldPreviewMaxHorizontalOffset()
    {
        return Math.Max(0, GetWeldPreviewContentWidth() - GetWeldPreviewViewportWidth());
    }

    private int GetWeldPreviewContentWidth()
    {
        return table2.Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Visible)
            .Sum(column => column.Width);
    }

    private int GetWeldPreviewViewportWidth()
    {
        return Math.Max(0, table2.ClientSize.Width - (table2.RowHeadersVisible ? table2.RowHeadersWidth : 0));
    }

    private void AddWeldPreviewColumn(string columnName, string headerText, int width)
    {
        table2.Columns.Add(new DataGridViewTextBoxColumn
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
        return table2.Rows.Count != _weldParameterRows.Count;
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

    private void FillWeldPreviewRows(
        IReadOnlyList<WeldPreviewItem> items,
        IReadOnlyList<IGrouping<int, WeldParameterRow>> touchGroups)
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
                SetPreviewValue(rowIndex, BuildPreviewColumnName(item.Index, PreviewUpperRole), DisplayPreviewValue(detail?.UpperValue));
                SetPreviewValue(rowIndex, BuildPreviewColumnName(item.Index, PreviewLowerRole), DisplayPreviewValue(detail?.LowerValue));
                SetPreviewValue(rowIndex, BuildPreviewColumnName(item.Index, PreviewActualRole), DisplayPreviewValue(detail?.Value));
                SetPreviewValue(rowIndex, BuildPreviewColumnName(item.Index, PreviewResultRole), DisplayPreviewValue(detail?.Result));
            }
        }
    }

    private void SetPreviewValue(int rowIndex, string columnName, string value)
    {
        if (rowIndex < 0 || rowIndex >= table2.Rows.Count || !table2.Columns.Contains(columnName))
        {
            return;
        }

        var cell = table2.Rows[rowIndex].Cells[columnName];
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
        while (table2.Rows.Count < rowCount)
        {
            table2.Rows.Add();
        }

        while (table2.Rows.Count > rowCount)
        {
            table2.Rows.RemoveAt(table2.Rows.Count - 1);
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
                Sort = group.Min(row => row.Sort % 10000)
            })
            .OrderBy(item => item.Sort)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => new WeldPreviewItem(index + 1, item.Key, item.Name, item.Sort))
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
            : string.Join("|", items.Select(item => $"{item.Index}:{item.Key}:{item.Name}"));
    }

    /// <summary>
    /// 表格布局 key 同时包含列结构和焊点行数；只有它变化时才允许重建 table2。
    /// </summary>
    private static string BuildWeldPreviewLayoutKey(IEnumerable<WeldParameterRow> rows)
    {
        var materializedRows = rows.ToList();
        var items = ResolveWeldPreviewItems(materializedRows);
        var rowCount = IsInfoPreview(items)
            ? materializedRows.Count
            : ResolvePreviewTouchGroups(materializedRows).Count;
        return $"{BuildWeldPreviewSchemaKey(items)}|rows:{rowCount}";
    }

    /// <summary>
    /// 只记录 table2 可见测试数据。刷新时间、产品编号等非表格字段变化时，不触发表格刷新。
    /// </summary>
    private static string BuildWeldPreviewVisibleValueKey(IEnumerable<WeldParameterRow> rows)
    {
        return string.Join('\u001F', rows
            .OrderBy(row => row.Sort)
            .Select(row => string.Join('\u001E',
                row.TouchIndex,
                row.TouchNo,
                row.ItemKey,
                row.ParameterName,
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

    private static string ResolvePreviewTouchNo(IEnumerable<WeldParameterRow> rows)
    {
        var first = rows.OrderBy(row => row.Sort).FirstOrDefault();
        return DisplayPreviewValue(first?.TouchNo);
    }

    private static string ResolvePreviewTouchResult(IEnumerable<WeldParameterRow> rows)
    {
        // 焊点结果列只显示焊点结果地址的值，避免与测试项结果列互相回退导致 OK/NG 抖动。
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

    /// <summary>
    /// 切换工位或页面加载时，优先显示实时预览服务已有的最新快照。
    /// </summary>
    private void ApplyCurrentRealtimePreviewSnapshot()
    {
        var snapshot = _productRealtimePreviewService.GetCurrent(CurrentStationNo);
        if (snapshot is not null)
        {
            ApplyProductRealtimePreviewSnapshot(snapshot);
        }
    }

    /// <summary>
    /// 将服务层实时采集快照映射到主界面，MonitorView 不直接读取 PLC。
    /// </summary>
    private void ApplyProductRealtimePreviewSnapshot(ProductRealtimePreviewSnapshot snapshot)
    {
        //SetControlText(groupStationOverview, $"工位 {snapshot.StationNo} 产品概览");
        SetControlText(inputLiveStation, FormatStationName(snapshot.StationNo));
        SetControlText(inputLiveProductNo, snapshot.ProductNo);
        SetControlText(inputLiveProductNum, snapshot.ProductNum);
        SetControlText(inputLiveProductModel, snapshot.ProductModel);
        SetControlText(inputLiveScheme, snapshot.SchemeId);
        SetControlText(inputLiveTouchCount, snapshot.TouchCountText);
        SetControlText(inputLiveResult, snapshot.ProductResult);
        SetControlText(inputLiveRefreshTime, snapshot.RefreshTimeText);
        SetControlText(lblLiveHint, string.IsNullOrWhiteSpace(snapshot.Message)
            ? "实时采集正常"
            : snapshot.Message);
        _currentProductIdentity = new ProductIdentity(
            snapshot.StationNo,
            snapshot.ProductNum,
            snapshot.ProductModel,
            "RealtimePreview");

        // 采集短暂失败会发布空快照；已有表格时保留旧数据，只用上方提示说明状态。
        if (snapshot.Rows.Count == 0 && table2.Rows.Count > 0)
        {
            return;
        }

        ApplyRealtimeWeldParameterRows(snapshot.Rows);
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

    /// <summary>
    /// 应用下一批预览行。布局不变时不清空表格，值不变时不触碰 table2。
    /// </summary>
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

    /// <summary>
    /// PLC 偶发读取失败返回空值时，保留上一轮有效值，避免结果或上下限列来回跳动。
    /// </summary>
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

            if (IsEmptyPreviewValue(nextRow.UpperValue) && !IsEmptyPreviewValue(previousRow.UpperValue))
            {
                nextRow.UpperValue = previousRow.UpperValue;
            }

            if (IsEmptyPreviewValue(nextRow.LowerValue) && !IsEmptyPreviewValue(previousRow.LowerValue))
            {
                nextRow.LowerValue = previousRow.LowerValue;
            }

            if (IsEmptyPreviewValue(nextRow.Result) && !IsEmptyPreviewValue(previousRow.Result))
            {
                nextRow.Result = previousRow.Result;
            }
        }
    }

    private static bool IsEmptyPreviewValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "--", StringComparison.Ordinal);
    }

    private static bool SetRowTextIfChanged(string current, string next, Action<string> apply)
    {
        if (string.Equals(current, next, StringComparison.Ordinal))
        {
            return false;
        }

        apply(next);
        return true;
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

    private ProductIdentity? ResolveCurrentPreviewIdentity(int stationNo)
    {
        if (_currentProductIdentity is not null
            && _currentProductIdentity.StationNo == stationNo
            && !string.IsNullOrWhiteSpace(_currentProductIdentity.ProductNum))
        {
            return _currentProductIdentity;
        }

        return ResolveOnlineProductIdentity(stationNo);
    }

    private IReadOnlyList<PlcAddressPreviewRow> BuildCurrentAddressPreviewRows()
    {
        var stationNo = CurrentStationNo;
        var identity = ResolveCurrentPreviewIdentity(stationNo);
        if (identity is null || string.IsNullOrWhiteSpace(identity.ProductNum))
        {
            return new[]
            {
                PlcAddressPreviewRow.Info(stationNo, "当前工位尚未识别到产品工号，无法计算地址预览。")
            };
        }

        var config = _productProcessConfigService.FindActive(identity.ProductNum, stationNo);
        if (config is null)
        {
            return new[]
            {
                PlcAddressPreviewRow.Info(stationNo, $"未找到产品工号 {identity.ProductNum} 的产品工艺配置。")
            };
        }

        var schemeItems = ResolveSchemeItems(config.SchemeId);
        var rows = new List<PlcAddressPreviewRow>();

        AddAddressPreviewRow(rows, identity, "产品头", "-", "产品编号", config.ProductBase, 0, config.ProductNoExpr);
        AddAddressPreviewRow(rows, identity, "产品头", "-", "产品结果", config.ProductBase, 0, config.ProductResultExpr);
        AddAddressPreviewRow(rows, identity, "产品头", "-", "实际焊点数", config.ProductBase, 0, config.ActualTouchCountExpr);
        AddAddressPreviewRow(rows, identity, "产品头", "-", "预设焊点数", config.ProductBase, 0, config.PresetTouchCountExpr);

        for (var touchNo = 1; touchNo <= Math.Max(1, config.TouchCount); touchNo++)
        {
            var touchContextOffset = (touchNo - 1) * config.TouchHeaderLen;
            var testContextOffset = (touchNo - 1) * config.TestAreaLen;
            var touchText = touchNo.ToString(CultureInfo.InvariantCulture);

            AddAddressPreviewRow(rows, identity, "焊点头", touchText, "焊点编号", config.TouchBase, touchContextOffset, config.TouchNoExpr);
            AddAddressPreviewRow(rows, identity, "焊点头", touchText, "焊点结果", config.TouchBase, touchContextOffset, config.TouchResultExpr);

            foreach (var schemeItem in schemeItems)
            {
                var item = schemeItem.Item;
                AddAddressPreviewRow(rows, identity, "测试项", touchText, $"{item.ItemName} 实际值", config.TestBase, testContextOffset, item.ActualExpression);
                AddAddressPreviewRow(rows, identity, "测试项", touchText, $"{item.ItemName} 上限", config.TestBase, testContextOffset, item.UpperExpression);
                AddAddressPreviewRow(rows, identity, "测试项", touchText, $"{item.ItemName} 下限", config.TestBase, testContextOffset, item.LowerExpression);
                AddAddressPreviewRow(rows, identity, "测试项", touchText, $"{item.ItemName} 结果", config.TestBase, testContextOffset, item.ResultExpression);
            }
        }

        return rows;
    }

    private void AddAddressPreviewRow(
        ICollection<PlcAddressPreviewRow> rows,
        ProductIdentity identity,
        string category,
        string touchNo,
        string valueRole,
        string baseAddress,
        int contextOffset,
        string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return;
        }

        var binding = ResolvePreviewExpressionBinding(baseAddress, contextOffset, expression);
        rows.Add(new PlcAddressPreviewRow
        {
            Station = $"工位{identity.StationNo}",
            ProductNum = identity.ProductNum,
            ProductModel = identity.ProductModel,
            Category = category,
            TouchNo = touchNo,
            ValueRole = valueRole,
            BaseAddress = baseAddress,
            ContextOffset = contextOffset,
            Expression = binding.Expression,
            DataType = binding.DataType,
            Rule = binding.Rule,
            ResolvedAddress = binding.Address
        });
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
                ?? await ReadPlcRecipeProductIdentityAsync(stationNo)
                ?? await ReadPlcProductIdentityAsync(stationNo);

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
        var state = _weldTaskService.CurrentState;
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

    private async Task<ProductIdentity> ReadPlcProductIdentityAsync(int stationNo)
    {
        var productNum = await ReadPlcAddressTextAsync(AppConstants.PlcAddressKeys.ProductNum, stationNo);
        var productModel = await ReadPlcAddressTextAsync(AppConstants.PlcAddressKeys.ProductModel, stationNo);
        return new ProductIdentity(stationNo, productNum, productModel, "PLC");
    }

    /// <summary>
    /// 离线模式下可以通过 PLC 当前配方编号反查本地程序，从而确定产品工号和型号。
    /// </summary>
    private async Task<ProductIdentity?> ReadPlcRecipeProductIdentityAsync(int stationNo)
    {
        if (!chkReadPlc.Checked)
        {
            return null;
        }

        var recipeResult = await ReadPlcAddressTextResultAsync(AppConstants.PlcAddressKeys.RecipeCode, stationNo);
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

    private async Task<string> ReadPlcAddressTextAsync(string logicalKey, int stationNo)
        => (await ReadPlcAddressTextResultAsync(logicalKey, stationNo)).Value;

    private async Task<PlcTextReadResult> ReadPlcAddressTextResultAsync(string logicalKey, int stationNo)
    {
        var address = _plcAddressService.GetByKey(logicalKey, stationNo);
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

    private async Task<PlcWriteResult> WritePlcAddressTextResultAsync(string logicalKey, int stationNo, string value)
    {
        var address = _plcAddressService.GetByKey(logicalKey, stationNo);
        if (address is null || !address.Enabled || string.IsNullOrWhiteSpace(address.Address))
        {
            return PlcWriteResult.Failed($"PLC business address \"{logicalKey}\" is not configured or disabled.");
        }

        var normalizedValue = NormalizePlcText(value);
        var plcAddress = address.Address.Trim();
        var result = address.DataType switch
        {
            AppConstants.PlcDataTypes.Bool => await WriteBoolTextAsync(plcAddress, normalizedValue),
            AppConstants.PlcDataTypes.Int32 => await WriteInt32TextAsync(plcAddress, normalizedValue),
            AppConstants.PlcDataTypes.Float => await WriteFloatTextAsync(plcAddress, normalizedValue),
            AppConstants.PlcDataTypes.String => await _plcCommunicationService.WriteStringAsync(plcAddress, normalizedValue),
            _ => await WriteInt16TextAsync(plcAddress, normalizedValue)
        };

        return result.IsSuccess
            ? PlcWriteResult.Success(plcAddress)
            : PlcWriteResult.Failed(result.Message, plcAddress);
    }

    private async Task<PlcServiceResult> WriteBoolTextAsync(string address, string value)
    {
        if (value is "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return await _plcCommunicationService.WriteBoolAsync(address, true);
        }

        if (value is "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return await _plcCommunicationService.WriteBoolAsync(address, false);
        }

        return PlcServiceResult.Fail($"配方编号“{value}”不能写入 Bool 地址，请改用 Int16/Int32/String 地址。");
    }

    private async Task<PlcServiceResult> WriteInt16TextAsync(string address, string value)
    {
        return short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? await _plcCommunicationService.WriteInt16Async(address, parsed)
            : PlcServiceResult.Fail($"配方编号“{value}”不能转换为 Int16。");
    }

    private async Task<PlcServiceResult> WriteInt32TextAsync(string address, string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? await _plcCommunicationService.WriteInt32Async(address, parsed)
            : PlcServiceResult.Fail($"配方编号“{value}”不能转换为 Int32。");
    }

    private async Task<PlcServiceResult> WriteFloatTextAsync(string address, string value)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? await _plcCommunicationService.WriteFloatAsync(address, parsed)
            : PlcServiceResult.Fail($"配方编号“{value}”不能转换为 Float。");
    }

    private void ApplySchemePreview(ProductIdentity identity, bool force)
    {
        if (identity.StationNo != CurrentStationNo)
        {
            return;
        }

        _currentProductIdentity = identity;
        if (_weldTaskService.CurrentState.CurrentWorkOrder is null)
        {
            inputProdNum.Text = identity.ProductNum;
            inputProdModel.Text = identity.ProductModel;
        }

        var previewKey = $"{identity.StationNo}|{identity.ProductNum}|{identity.ProductModel}|{identity.Source}";
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
        var nextRows = BuildSchemePreviewRows(identity, previousRows).ToList();

        _lastSchemePreviewKey = previewKey;
        ApplyWeldParameterRows(nextRows);
    }

    private IEnumerable<WeldParameterRow> BuildSchemePreviewRows(
        ProductIdentity identity,
        IReadOnlyDictionary<string, WeldParameterRow> previousRows)
    {
        if (string.IsNullOrWhiteSpace(identity.ProductNum))
        {
            return new[] { CreateInfoRow(identity, "等待产品工号", "请确认 MES 工单或 PLC 产品工号地址。") };
        }

        var config = _productProcessConfigService.FindActive(identity.ProductNum, identity.StationNo);
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
                var row = CreateSchemePreviewRow(identity, config, schemeItem.Item, touchNo, schemeItem.Sort);
                CopyLatestValues(previousRows, row);
                rows.Add(row);
            }
        }

        return rows.Count == 0
            ? new[] { CreateInfoRow(identity, "测试方案未配置", $"测试方案 {config.SchemeId} 未配置测试项。") }
            : rows;
    }

    private IReadOnlyList<SchemePreviewItem> ResolveSchemeItems(string schemeId)
    {
        var details = _testSchemeConfigService.GetDetails(schemeId)
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
                Item = allItems.FirstOrDefault(item => item.ItemId == detail.ItemId)
            })
            .Where(item => item.Item is not null)
            .Select(item => new SchemePreviewItem(item.Sort, item.Item!))
            .ToList();
    }

    private WeldParameterRow CreateSchemePreviewRow(
        ProductIdentity identity,
        BizProductProcessConfig config,
        DimTestItem item,
        int touchNo,
        int sort)
    {
        var testContextOffset = (Math.Max(1, touchNo) - 1) * config.TestAreaLen;
        var actual = ResolvePreviewExpressionBinding(config.TestBase, testContextOffset, item.ActualExpression);
        var upper = ResolvePreviewExpressionBinding(config.TestBase, testContextOffset, item.UpperExpression);
        var lower = ResolvePreviewExpressionBinding(config.TestBase, testContextOffset, item.LowerExpression);
        var result = ResolvePreviewExpressionBinding(config.TestBase, testContextOffset, item.ResultExpression);

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
            Sort = touchNo * 10000 + sort,
            ItemKey = ResolveItemKey(item),
            TestItemId = item.ItemId,
            ProcessConfigId = config.Id
        };
    }

    /// <summary>
    /// 将偏移表达式转换成实时读取所需的地址、数据类型和显示规则；表达式异常时保留原文方便排查。
    /// </summary>
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

    private static void CopyLatestValues(
        IReadOnlyDictionary<string, WeldParameterRow> previousRows,
        WeldParameterRow target)
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

    private IEnumerable<WeldParameterRow> BuildFallbackWeldParameterRows(
        BizWeldPointRecord record,
        IReadOnlyDictionary<string, string> rawValues)
    {
        var knownRows = new[]
        {
            CreateFallbackWeldParameterRow(record, "max_electric", "峰值电流", FormatNullableText(record.MaxElectric), 10),
            CreateFallbackWeldParameterRow(record, "max_voltage", "峰值电压", FormatNullableText(record.MaxVoltage), 20),
            CreateFallbackWeldParameterRow(record, "valid_power", "有效功率", FormatNullableText(record.ValidPower), 30),
            CreateFallbackWeldParameterRow(record, "displacement", "位移", FormatNullableText(record.Displacement), 40),
            CreateFallbackWeldParameterRow(record, "weld_ts", "焊接时间", FormatNullableText(record.WeldTs), 50),
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

    private WeldParameterRow CreateFallbackWeldParameterRow(
        BizWeldPointRecord record,
        string itemKey,
        string parameterName,
        string value,
        int sort)
    {
        return new WeldParameterRow
        {
            StationNo = record.StationNo,
            Station = $"工位{record.StationNo}",
            ProductNo = record.ProductNo,
            ProductNum = _currentProductIdentity?.ProductNum ?? _weldTaskService.CurrentState.ActiveTask?.ProductNum ?? string.Empty,
            ProductModel = _currentProductIdentity?.ProductModel ?? _weldTaskService.CurrentState.ActiveTask?.ProductModel ?? string.Empty,
            TouchIndex = ParsePositiveInt(record.TouchNo),
            TouchNo = record.TouchNo,
            ParameterName = parameterName,
            Value = value,
            Result = FormatNullableText(record.TestResult),
            RecordTime = record.RecordTime.ToString("HH:mm:ss"),
            Sort = ParsePositiveInt(record.TouchNo) * 10000 + sort,
            ItemKey = itemKey
        };
    }

    private static string? FindRecordValue(
        BizWeldPointRecord record,
        WeldParameterRow row,
        IReadOnlyDictionary<string, string> rawValues)
    {
        return FindRawValue(rawValues, row.ItemKey, row.ParameterName)
            ?? row.ItemKey switch
            {
                "max_electric" => record.MaxElectric,
                "max_voltage" => record.MaxVoltage,
                "valid_power" => record.ValidPower,
                "displacement" => record.Displacement,
                "weld_ts" => record.WeldTs,
                "test_result" => record.TestResult,
                _ => null
            };
    }

    private static string? FindRecordResult(
        BizWeldPointRecord record,
        WeldParameterRow row,
        IReadOnlyDictionary<string, string> rawValues)
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
        TableStyleHelper.ApplyAntdTable(table1, AntdUI.ColumnsMode.Fill);
        ApplyCompactProductionMetricTableStyle();
        ApplyWeldParameterTableStyle();
    }

    /// <summary>
    /// The metric table has only a few fixed rows, so a compact row height keeps the right panel readable.
    /// </summary>
    private void ApplyCompactProductionMetricTableStyle()
    {
        table1.RowHeight = 34;
        table1.RowHeightHeader = 36;
        table1.Gap = 4;
        table1.GapCell = 2;
        table1.Gaps = new Size(4, 4);
    }

    private void ApplyWeldParameterTableStyle()
    {
        EnableDoubleBuffering(table2);
        table2.ScrollBars = ScrollBars.Vertical;
        table2.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 10F);
        table2.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        table2.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 244, 255);
        table2.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 30, 30);
        table2.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        table2.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        table2.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
        table2.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 30, 30);
        table2.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        table2.ColumnHeadersHeight = 36;
        table2.RowTemplate.Height = 30;
        table2.GridColor = Color.FromArgb(224, 224, 224);
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

    private string FormatNullable(int? value)
    {
        return value?.ToString() ?? _localizer.GetString(TextKeys.Production.NotAvailable);
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

    private bool TrySelectProcess(IReadOnlyList<ExpItemData> processes, out ExpItemData process)
    {
        if (processes.Count == 0)
        {
            ShowWarning(TextKeys.Monitor.Message.ProcessRequired);
            process = default!;
            return false;
        }

        var columns = new[]
        {
            new SelectionDialogColumn<ExpItemData>(
                _localizer.GetString(TextKeys.Monitor.Label.SequenceNo),
                process => process.SequenceNo,
                10F,
                DataGridViewContentAlignment.MiddleCenter),
            new SelectionDialogColumn<ExpItemData>(
                _localizer.GetString(TextKeys.Monitor.Label.ProcessNo),
                process => process.ProcessNo,
                12F,
                DataGridViewContentAlignment.MiddleCenter),
            new SelectionDialogColumn<ExpItemData>(
                _localizer.GetString(TextKeys.Monitor.Label.ProcessName),
                process => process.ItemName,
                38F),
            new SelectionDialogColumn<ExpItemData>(
                _localizer.GetString(TextKeys.Monitor.Label.ProductionQuantity),
                process => process.StartAmount,
                14F,
                DataGridViewContentAlignment.MiddleRight)
        };

        return SelectionDialog.TrySelect(
            this,
            _localizer.GetString(TextKeys.Monitor.Dialog.SelectProcessTitle),
            _localizer.GetString(TextKeys.Monitor.Dialog.SelectProcessPrompt),
            processes,
            columns,
            _localizer.GetString(TextKeys.Common.ActionApply),
            _localizer.GetString(TextKeys.Common.ActionCancel),
            out process);
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
        MesWorkOrderResponse? workOrder,
        ExpItemData? process,
        MesProgramData program,
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
        var refreshedProgram = _weldTaskService.CurrentState.SelectedProgram ?? program;
        _confirmedProgramFingerprint = BuildProgramFingerprint(refreshedProgram, stationNo);
        QueueRefreshSchemePreview(force: true);
        return true;
    }

    private bool IsProgramContentConfirmed(MesProgramData program, int stationNo)
    {
        return string.Equals(
            _confirmedProgramFingerprint,
            BuildProgramFingerprint(program, stationNo),
            StringComparison.Ordinal);
    }

    private static string BuildProgramFingerprint(MesProgramData program, int stationNo)
    {
        return $"{stationNo}|{program.Id}|{program.ProgramName}|{program.ProgramContent}";
    }

    /// <summary>
    /// 根据页面勾选项，在开工上报前执行配方下发或配方读取校验。
    /// </summary>
    private async Task<bool> PrepareRecipeBeforeStartAsync(MesProgramData program, int stationNo)
    {
        var shouldSendRecipe = chkSendPlc.Checked;
        var shouldReadRecipe = chkReadPlc.Checked;
        if (!shouldSendRecipe && !shouldReadRecipe)
        {
            return true;
        }

        var localRecipeCode = ResolveExpectedRecipeCode(program);
        if (string.IsNullOrWhiteSpace(localRecipeCode))
        {
            ShowRecipeCheckFailed(
                $"Local program has no recipe code. ProgramId={program.Id}; ProgramName={program.ProgramName}; Station={stationNo}");
            return false;
        }

        if (shouldSendRecipe && !await SendRecipeCodeToPlcAsync(localRecipeCode, program, stationNo))
        {
            return false;
        }

        if (shouldReadRecipe && !await ReadAndCompareRecipeCodeAsync(localRecipeCode, program, stationNo))
        {
            return false;
        }

        return true;
    }

    private async Task<bool> SendRecipeCodeToPlcAsync(string recipeCode, MesProgramData program, int stationNo)
    {
        var writeResult = await WritePlcAddressTextResultAsync(AppConstants.PlcAddressKeys.RecipeCode, stationNo, recipeCode);
        if (!writeResult.IsSuccess)
        {
            ShowRecipeCheckFailed(
                $"PLC recipe code write failed. Station={stationNo}; RecipeCode={recipeCode}; Detail={writeResult.Detail}");
            return false;
        }

        _productionLogService.Write(
            "RecipeCodeWrite",
            "配方编号已下发PLC",
            $"RecipeCode={recipeCode}, ProgramId={program.Id}, ProgramName={program.ProgramName}",
            stationNo: stationNo,
            programId: program.Id ?? string.Empty,
            plcSignal: AppConstants.PlcAddressKeys.RecipeCode,
            plcAddress: writeResult.Address);
        SetRuntimeStatusText($"配方编号已下发：{recipeCode}", isSuccess: true);
        return true;
    }

    private async Task<bool> ReadAndCompareRecipeCodeAsync(string expectedRecipeCode, MesProgramData program, int stationNo)
    {
        var plcRecipeResult = await ReadPlcAddressTextResultAsync(AppConstants.PlcAddressKeys.RecipeCode, stationNo);
        if (!plcRecipeResult.IsSuccess || string.IsNullOrWhiteSpace(plcRecipeResult.Value))
        {
            ShowRecipeCheckFailed(
                $"PLC recipe code could not be read. Station={stationNo}; Detail={plcRecipeResult.Detail}");
            return false;
        }

        var plcRecipeCode = NormalizeRecipeCode(plcRecipeResult.Value);
        var localRecipeCode = NormalizeRecipeCode(expectedRecipeCode);
        if (!string.Equals(plcRecipeCode, localRecipeCode, StringComparison.OrdinalIgnoreCase))
        {
            ShowRecipeCheckFailed(
                $"PLC recipe code mismatch. Station={stationNo}; Local={localRecipeCode}; PLC={plcRecipeCode}; ProgramId={program.Id}; ProgramName={program.ProgramName}");
            return false;
        }

        _productionLogService.Write(
            "RecipeCodeRead",
            "PLC配方编号读取一致",
            $"RecipeCode={plcRecipeCode}, ProgramId={program.Id}, ProgramName={program.ProgramName}",
            stationNo: stationNo,
            programId: program.Id ?? string.Empty,
            plcSignal: AppConstants.PlcAddressKeys.RecipeCode);
        SetRuntimeStatusText($"PLC配方编号一致：{localRecipeCode}", isSuccess: true);
        return true;
    }

    /// <summary>
    /// When enabled, checks the PLC recipe code before MES start report to prevent using mismatched recipes.
    /// </summary>
    private async Task<bool> ValidateRecipeBeforeStartAsync(MesProgramData program, int stationNo)
    {
        if (!_settingsService.Get().ValidateRecipeBeforeStart)
        {
            return true;
        }

        var expectedRecipeCode = ResolveExpectedRecipeCode(program);
        if (string.IsNullOrWhiteSpace(expectedRecipeCode))
        {
            ShowRecipeCheckFailed(
                $"Local program has no recipe code. ProgramId={program.Id}; ProgramName={program.ProgramName}; Station={stationNo}");
            return false;
        }

        var plcRecipeResult = await ReadPlcAddressTextResultAsync(AppConstants.PlcAddressKeys.RecipeCode, stationNo);
        if (!plcRecipeResult.IsSuccess || string.IsNullOrWhiteSpace(plcRecipeResult.Value))
        {
            ShowRecipeCheckFailed(
                $"PLC recipe code could not be read. Station={stationNo}; Detail={plcRecipeResult.Detail}");
            return false;
        }

        var plcRecipeCode = NormalizeRecipeCode(plcRecipeResult.Value);
        var localRecipeCode = NormalizeRecipeCode(expectedRecipeCode);
        if (!string.Equals(plcRecipeCode, localRecipeCode, StringComparison.OrdinalIgnoreCase))
        {
            ShowRecipeCheckFailed(
                $"PLC recipe code mismatch. Station={stationNo}; Local={localRecipeCode}; PLC={plcRecipeCode}; ProgramId={program.Id}; ProgramName={program.ProgramName}");
            return false;
        }

        SetRuntimeStatusText($"配方编号校验通过：{localRecipeCode}", isSuccess: true);
        return true;
    }

    private string ResolveExpectedRecipeCode(MesProgramData program)
    {
        var localProgram = ResolveLocalProgram(program);
        return localProgram?.RecipeCode?.Trim() ?? string.Empty;
    }

    private BizProgram? ResolveLocalProgram(MesProgramData program)
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

        var settings = _settingsService.Get();
        return _programManageService.GetPrograms()
            .Where(program => string.Equals(NormalizeRecipeCode(program.RecipeCode), normalizedRecipeCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(program => SameText(program.DeviceId, settings.DeviceId))
            .ThenByDescending(program => program.UpdatedTime)
            .FirstOrDefault();
    }

    private void ShowRecipeCheckFailed(string detail)
    {
        const string message = "配方编号校验失败";
        _exceptionLogService.WriteBusiness("PLC.RecipeCodeCheck", message, detail);
        ShowWarningText(message);
    }

    private static string NormalizeRecipeCode(string? value)
    {
        return NormalizePlcText(value);
    }

    private static bool SameText(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
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
                return form.EmployeeNumber;
            }

            ShowBusinessWarning(
                "MES.ValidateOperator",
                TextKeys.Monitor.Message.OperatorValidationFailed,
                response.Msg,
                $"EmployeeNumber={form.EmployeeNumber}");
        }
    }

    private bool TryPromptStartActualQuantity(PlcProductionSnapshot production, out int actualQty)
    {
        var plcQty = Math.Max(0, production.TotalProduction);
        var defaultQty = production.IsSuccess ? plcQty : 0;
        var prompt = production.IsSuccess
            ? $"实际生产数量默认读取 PLC 加工总数：{plcQty}。如需人工修改，请确认修改原因后再应用。"
            : $"未能从 PLC 读取加工总数：{production.Message}。请人工输入实际生产数量。";

        if (!TryPromptNonNegativeIntText(
                _localizer.GetString(TextKeys.Monitor.Dialog.ActualQuantityTitle),
                prompt,
                defaultQty,
                out actualQty))
        {
            return false;
        }

        if (production.IsSuccess && actualQty != plcQty)
        {
            return ConfirmManualActualQuantityChange(plcQty, actualQty);
        }

        return true;
    }

    private bool ConfirmManualActualQuantityChange(int plcQty, int actualQty)
    {
        var result = MessageBox.Show(
            this,
            $"实际生产数量已由 PLC 加工总数 {plcQty} 修改为 {actualQty}。\r\n请确认该数量与设备实际加工数量一致。",
            "确认实际生产数量",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning);

        return result == DialogResult.OK;
    }

    private bool TryPromptPositiveInt(string titleKey, string promptKey, int defaultValue, out int value)
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

    private bool TryPromptNonNegativeIntText(string title, string prompt, int defaultValue, out int value)
    {
        if (!TryPromptIntText(title, prompt, defaultValue, out value))
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

    private void ShowInfo(string messageKey, params object[] args)
    {
        ClearRuntimeError();
        SetRuntimeStatus(messageKey, args);
        MessageBox.Show(
            this,
            _localizer.GetString(messageKey, args),
            _localizer.GetString(TextKeys.Common.TitleInfo),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
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

    /// <summary>
    /// 可预见业务失败：界面显示短提示，详细原因写入日志管理。
    /// </summary>
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

    /// <summary>
    /// 记录运行状态的资源键，语言切换时可以重新翻译当前提示。
    /// </summary>
    private void SetRuntimeStatus(string messageKey, params object[] args)
    {
        _runtimeStatusKey = messageKey;
        _runtimeStatusArgs = args;
        _runtimeStatusText = null;
        _runtimeStatusTextIsSuccess = false;
        RefreshRuntimeStatus();
    }

    private void SetRuntimeStatusText(string message, bool isSuccess = false)
    {
        _runtimeStatusKey = null;
        _runtimeStatusArgs = Array.Empty<object>();
        _runtimeStatusText = NormalizeRuntimeSummary(message);
        _runtimeStatusTextIsSuccess = isSuccess;
        RefreshRuntimeStatus();
    }

    /// <summary>
    /// 记录异常提示的资源键，避免语言切换后仍显示旧语言。
    /// </summary>
    private void SetRuntimeError(string messageKey, params object[] args)
    {
        _runtimeErrorKey = messageKey;
        _runtimeErrorArgs = args;
        _runtimeErrorText = null;
        RefreshRuntimeError();
    }

    /// <summary>
    /// 用于显示已经整理过的业务短提示，例如“开工上报失败”。
    /// </summary>
    private void SetRuntimeErrorText(string message)
    {
        _runtimeErrorKey = null;
        _runtimeErrorArgs = Array.Empty<object>();
        _runtimeErrorText = NormalizeRuntimeSummary(message);
        RefreshRuntimeError();
    }

    /// <summary>
    /// 新业务动作开始时清空旧异常，避免用户把历史错误误认为当前错误。
    /// </summary>
    private void ClearRuntimeError()
    {
        _runtimeErrorKey = null;
        _runtimeErrorArgs = Array.Empty<object>();
        _runtimeErrorText = null;
        inputErrorTips.Clear();
        ApplyRuntimeErrorTone(hasError: false);
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

    /// <summary>
    /// 运行状态使用颜色表达语义：绿色代表成功，蓝色代表处理中，灰色代表空闲。
    /// </summary>
    private void ApplyRuntimeStatusTone()
    {
        var color = _runtimeStatusTextIsSuccess
            ? UiColors.Status.Success
            : GetRuntimeStatusColor(_runtimeStatusKey);
        grpRunningStatus.ForeColor = color;
        inputRunningStatus.ForeColor = color;
    }

    /// <summary>
    /// 异常提示有内容时使用红色，无异常时弱化显示，避免用户误以为仍有故障。
    /// </summary>
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

    /// <summary>
    /// 主界面只展示业务摘要，详细错误和上下文仍以日志为准。
    /// </summary>
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
    /// 确认框统一走本地化文本，减少重复代码。
    /// </summary>
    private bool ConfirmAction(string messageKey, string titleKey)
    {
        return MessageBox.Show(
                this,
                _localizer.GetString(messageKey),
                _localizer.GetString(titleKey),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)
            == DialogResult.Yes;
    }

    /// <summary>
    /// Row model for the real-time weld parameter table. Sort is kept for display ordering only.
    /// </summary>
    private sealed record ProductIdentity(
        int StationNo,
        string ProductNum,
        string ProductModel,
        string Source);

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

    private sealed record WeldPreviewItem(int Index, string Key, string Name, int Sort);

    private sealed record SchemePreviewItem(int Sort, DimTestItem Item);
}
