using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Forms;
using AutoWeldSystem.UI.Infrastructure;
using System.Reflection;
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
    private const int StationSelectorRowIndex = 3;
    private const string VersionPrefix = "v";
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private readonly ILocalizationService _localizer;
    private readonly IAppSettingsService _settingsService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IMesConnectionMonitorService _mesConnectionMonitorService;
    private readonly IPlcProductionMonitorService _plcProductionMonitorService;
    private readonly IPlcWorkIdMonitorService _plcWorkIdMonitorService;
    private readonly IPlcWeldCycleMonitorService _plcWeldCycleMonitorService;
    private readonly IPlcAddressService _plcAddressService;
    private readonly IProductProcessConfigService _productProcessConfigService;
    private readonly ITestItemTemplateService _testItemTemplateService;
    private readonly IProgramManageService _programManageService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IProgramExceptionLogService _exceptionLogService;
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
    private Font? _versionFont;
    private Font? _runtimeMessageFont;
    private Font? _runtimeGroupFont;
    private Label? _lblVersion;
    private TableLayoutPanel? _titleLayout;
    private readonly List<WeldParameterRow> _weldParameterRows = new();
    private ProductIdentity? _currentProductIdentity;
    private DateTime _lastProductTemplateRefreshTime = DateTime.MinValue;
    private string _lastProductTemplatePreviewKey = string.Empty;
    private string _confirmedProgramFingerprint = string.Empty;
    private bool _refreshingProductTemplatePreview;

    public MonitorView(
        ILocalizationService localizer,
        IAppSettingsService settingsService,
        IPlcCommunicationService plcCommunicationService,
        IMesConnectionMonitorService mesConnectionMonitorService,
        IPlcProductionMonitorService plcProductionMonitorService,
        IPlcWorkIdMonitorService plcWorkIdMonitorService,
        IPlcWeldCycleMonitorService plcWeldCycleMonitorService,
        IPlcAddressService plcAddressService,
        IProductProcessConfigService productProcessConfigService,
        ITestItemTemplateService testItemTemplateService,
        IProgramManageService programManageService,
        IWeldTaskService weldTaskService,
        IProgramExceptionLogService exceptionLogService)
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
        _productProcessConfigService = productProcessConfigService;
        _testItemTemplateService = testItemTemplateService;
        _programManageService = programManageService;
        _weldTaskService = weldTaskService;
        _exceptionLogService = exceptionLogService;

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
        QueueRefreshProductTemplatePreview(force: true);
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
        _versionFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);

        tlpLeftTop.AutoSize = false;
        tlpCommunicationStatus.MinimumSize = new Size(HeaderStatusCellMinWidth * 2, 0);

        ConfigureTitleVersionLayout();
        ConfigureStatusTag(tagMes);
        ConfigureStatusTag(tagPLC);
        ConfigureStatusTag(tagDeviceStatus);
        ConfigureStatusTag(tagTaskStatus);
        //ConfigureReportButton(btnExpStart);
        //ConfigureReportButton(btnExpEnd);
        AdjustHeaderFixedColumns();
    }

    /// <summary>
    /// Splits the header title area into app title and version, keeping the version visible without crowding the title.
    /// </summary>
    private void ConfigureTitleVersionLayout()
    {
        if (_titleLayout is not null)
        {
            return;
        }

        tlpLeftTop.Controls.Remove(lblTitle);
        lblTitle.Dock = DockStyle.Fill;
        lblTitle.Margin = new Padding(0);
        lblTitle.TextAlign = ContentAlignment.BottomCenter;

        _lblVersion = new Label
        {
            Dock = DockStyle.Fill,
            Font = _versionFont,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0),
            Name = "lblVersion",
            Text = BuildVersionText(),
            TextAlign = ContentAlignment.TopCenter
        };

        _titleLayout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Name = "tlpTitleInfo",
            RowCount = 2
        };
        _titleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _titleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 72F));
        _titleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 28F));
        _titleLayout.Controls.Add(lblTitle, 0, 0);
        _titleLayout.Controls.Add(_lblVersion, 0, 1);

        tlpLeftTop.Controls.Add(_titleLayout, 1, 0);
    }

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

        groupBox1.Font = _runtimeGroupFont;
        groupBox2.Font = _runtimeGroupFont;
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
        ConfigureStationResultTag(tag1, "工位1--", UiColors.Status.Muted);
        ConfigureStationResultTag(tag2, "工位2--", UiColors.Status.Muted);
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

        tag1.Visible = true;
        tag2.Visible = _dualStationModeEnabled;

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
        tlpLeftTop.SizeChanged += TitleLayout_Changed;
        lblTitle.SizeChanged += TitleLayout_Changed;
        lblTitle.TextChanged += TitleLayout_Changed;
        btnSwitchUser.Click += SwitchUser_Click;
        btnLogout.Click += Logout_Click;
        btnChangeWO.Click += ChangeWorkOrder_Click;
        btnEditWO.Click += EditWorkOrder_Click;
        btnExpStart.Click += StartReport_Click;
        btnExpEnd.Click += FinishReport_Click;
        select_Lang.SelectedIndexChanged += Language_SelectedIndexChanged;
        selectStation.SelectedIndexChanged += Station_SelectedIndexChanged;
        GlobalContext.SessionChanged += GlobalContext_SessionChanged;
        _weldTaskService.StateChanged += WeldTaskService_StateChanged;
        _plcCommunicationService.StatusChanged += PlcCommunicationService_StatusChanged;
        _mesConnectionMonitorService.StatusChanged += MesConnectionMonitorService_StatusChanged;
        _plcProductionMonitorService.StatusChanged += PlcProductionMonitorService_StatusChanged;
        _plcWorkIdMonitorService.WorkIdChanged += PlcWorkIdMonitorService_WorkIdChanged;
        _plcWeldCycleMonitorService.WeldPointCollected += PlcWeldCycleMonitorService_WeldPointCollected;
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
        QueueRefreshProductTemplatePreview(force: true);
        AdjustTitleFontSize();
    }

    private void MonitorView_Load(object? sender, EventArgs e)
    {
        _timer.Start();
        ApplyLocalizedTexts();
        UpdateCurrentTime();
        BindSessionInfo();
        BindLanguageSelection();
        ConfigureStationSelector();
        BindProductionRuntimeState();
        RefreshRuntimePanels();
        ApplyPlcStatus(_plcCommunicationService.Current);
        ApplyMesStatus(_mesConnectionMonitorService.Current);
        ApplyProductionStatus(GetCurrentProductionSnapshot());
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
        _timer.Stop();
        _timer.Dispose();
        _titleFont?.Dispose();
        _headerStatusFont?.Dispose();
        _headerButtonFont?.Dispose();
        _versionFont?.Dispose();
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
            QueueRefreshProductTemplatePreview(force: false);
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
            ShowWarningText("当前工位已经开工，不能再调整加工程序。");
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

    private async void StartReport_Click(object? sender, EventArgs e)
    {
        var stationNo = CurrentStationNo;
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

        if (!await ValidateRecipeBeforeStartAsync(state.SelectedProgram, stationNo))
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
        var state = _weldTaskService.CurrentState;
        if (state.ActiveTask is null)
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
        var defaultActual = Math.Max(1, production.TotalProduction > 0 ? production.TotalProduction : state.ActiveTask.ActualQty);
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
        RefreshProductionRuntimeState();
        QueueRefreshProductTemplatePreview(force: true);
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
        QueueRefreshProductTemplatePreview(force: false);
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
            QueueRefreshProductTemplatePreview(force: true);
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
            $"焊点采集完成：工位{record.StationNo} 产品{record.ProductNo} 焊点{record.TouchNo} {record.TestResult}",
            isSuccess: true);
    }

    private void ApplyStationResult(BizWeldPointRecord record)
    {
        if (record.StationNo == 2 && !_dualStationModeEnabled)
        {
            return;
        }

        var tag = record.StationNo == 2 ? tag2 : tag1;
        var resultText = NormalizeStationResultText(record.TestResultRaw ?? record.TestResult);

        UpdateStationResultLayout();
        tag.Text = $"工位{record.StationNo}\r\n{resultText}";
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
        groupBox1.Text = _localizer.GetString(TextKeys.Monitor.Group.ExceptionTips);
        groupBox2.Text = _localizer.GetString(TextKeys.Monitor.Group.RunningStatus);
        table1.Text = _localizer.GetString(TextKeys.Monitor.Group.ProductionMetrics);

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
            new(_localizer.GetString(TextKeys.Production.MesProductionQuantity), FormatNullable(mesProductionQuantity)),
            new(_localizer.GetString(TextKeys.Production.AcceptedQuantity), snapshot.AcceptedQuantity.ToString()),
            new(_localizer.GetString(TextKeys.Production.RejectedQuantity), snapshot.RejectedQuantity.ToString()),
            new(_localizer.GetString(TextKeys.Production.AcceptedRate), FormatRate(acceptedRate)),
            new(_localizer.GetString(TextKeys.Production.RejectedRate), FormatRate(rejectedRate)),
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
        table2.Columns.Clear();
        table2.Columns.Add(new AntdUI.Column(nameof(WeldParameterRow.Station), "工位") { Ellipsis = true });
        table2.Columns.Add(new AntdUI.Column(nameof(WeldParameterRow.ProductNum), "产品工号") { Ellipsis = true });
        table2.Columns.Add(new AntdUI.Column(nameof(WeldParameterRow.ProductModel), "产品型号") { Ellipsis = true });
        table2.Columns.Add(new AntdUI.Column(nameof(WeldParameterRow.TouchNo), "焊点") { Ellipsis = true });
        table2.Columns.Add(new AntdUI.Column(nameof(WeldParameterRow.ParameterName), "测试项目") { Ellipsis = true });
        table2.Columns.Add(new AntdUI.Column(nameof(WeldParameterRow.Unit), "单位") { Ellipsis = true });
        table2.Columns.Add(new AntdUI.Column(nameof(WeldParameterRow.ActualAddress), "实际值地址") { Ellipsis = true });
        table2.Columns.Add(new AntdUI.Column(nameof(WeldParameterRow.UpperAddress), "上限地址") { Ellipsis = true });
        table2.Columns.Add(new AntdUI.Column(nameof(WeldParameterRow.LowerAddress), "下限地址") { Ellipsis = true });
        table2.Columns.Add(new AntdUI.Column(nameof(WeldParameterRow.ResultAddress), "结果地址") { Ellipsis = true });
        table2.Columns.Add(new AntdUI.Column(nameof(WeldParameterRow.Value), "最新值") { Ellipsis = true });
        table2.Columns.Add(new AntdUI.Column(nameof(WeldParameterRow.Result), "最新结果") { Ellipsis = true });
        table2.Columns.Add(new AntdUI.Column(nameof(WeldParameterRow.RecordTime), "采集时间") { Ellipsis = true });
        TableStyleHelper.ApplyAntdColumnDefaults(table2);
        BindWeldParameterTable();
    }

    private void BindWeldParameterRows(BizWeldPointRecord record)
    {
        var rawValues = ParseRawWeldValues(record.RawDataJson);
        var touchIndex = ParsePositiveInt(record.TouchNo);
        var matchedRows = _weldParameterRows
            .Where(row => row.StationNo == record.StationNo && row.TouchIndex == touchIndex)
            .ToList();

        if (matchedRows.Count == 0)
        {
            _weldParameterRows.AddRange(BuildFallbackWeldParameterRows(record, rawValues));
        }
        else
        {
            foreach (var row in matchedRows)
            {
                row.Value = FormatNullableText(FindRecordValue(record, row, rawValues));
                row.Result = FormatTestResultText(FindRecordResult(record, row, rawValues));
                row.RecordTime = record.RecordTime.ToString("HH:mm:ss");
            }
        }

        SortWeldParameterRows();
        BindWeldParameterTable();
    }

    private void BindWeldParameterTable()
    {
        table2.DataSource = _weldParameterRows.ToList();
        table2.Refresh();
    }

    private void QueueRefreshProductTemplatePreview(bool force)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (!force && DateTime.Now - _lastProductTemplateRefreshTime < TimeSpan.FromSeconds(2))
        {
            return;
        }

        _lastProductTemplateRefreshTime = DateTime.Now;
        _ = RefreshProductTemplatePreviewAsync(force);
    }

    private async Task RefreshProductTemplatePreviewAsync(bool force)
    {
        if (_refreshingProductTemplatePreview)
        {
            return;
        }

        _refreshingProductTemplatePreview = true;
        try
        {
            var stationNo = CurrentStationNo;
            var identity = ResolveOnlineProductIdentity(stationNo)
                ?? await ReadPlcProductIdentityAsync(stationNo);

            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(() => ApplyProductTemplatePreview(identity, force));
                return;
            }

            ApplyProductTemplatePreview(identity, force);
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.RefreshProductTemplatePreviewAsync");
        }
        finally
        {
            _refreshingProductTemplatePreview = false;
        }
    }

    private ProductIdentity? ResolveOnlineProductIdentity(int stationNo)
    {
        var state = _weldTaskService.CurrentState;
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

    private void ApplyProductTemplatePreview(ProductIdentity identity, bool force)
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
            && string.Equals(previewKey, _lastProductTemplatePreviewKey, StringComparison.Ordinal)
            && _weldParameterRows.Count > 0)
        {
            return;
        }

        var previousRows = _weldParameterRows
            .Where(row => !string.IsNullOrWhiteSpace(row.ItemKey))
            .GroupBy(row => row.UniqueKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        _lastProductTemplatePreviewKey = previewKey;
        _weldParameterRows.Clear();
        _weldParameterRows.AddRange(BuildProductTemplatePreviewRows(identity, previousRows));
        SortWeldParameterRows();
        BindWeldParameterTable();
    }

    private IEnumerable<WeldParameterRow> BuildProductTemplatePreviewRows(
        ProductIdentity identity,
        IReadOnlyDictionary<string, WeldParameterRow> previousRows)
    {
        if (string.IsNullOrWhiteSpace(identity.ProductNum))
        {
            return new[] { CreateInfoRow(identity, "等待产品工号", "请确认 MES 工单或 PLC 产品工号地址。") };
        }

        var config = _productProcessConfigService.FindActive(identity.ProductNum, identity.ProductModel, identity.StationNo);
        if (config is null)
        {
            return new[] { CreateInfoRow(identity, "未找到产品工艺配置", $"产品工号：{identity.ProductNum}，产品型号：{identity.ProductModel}") };
        }

        if (config.TemplateId <= 0)
        {
            return new[] { CreateInfoRow(identity, "未绑定测试项目模板", $"产品工号：{identity.ProductNum}") };
        }

        var allItems = _testItemTemplateService.GetItems(config.TemplateId)
            .Where(item => item.Enabled)
            .ToList();
        var rows = new List<WeldParameterRow>();

        for (var touchNo = 1; touchNo <= Math.Max(1, config.WeldPointCount); touchNo++)
        {
            var items = SelectTemplateItemsForTouch(allItems, identity.StationNo, touchNo);
            foreach (var item in items)
            {
                var row = CreateTemplatePreviewRow(identity, config, item, touchNo);
                CopyLatestValues(previousRows, row);
                rows.Add(row);
            }
        }

        return rows.Count == 0
            ? new[] { CreateInfoRow(identity, "测试项目模板无可用明细", $"模板ID：{config.TemplateId}") }
            : rows;
    }

    private static IReadOnlyList<BizTestItemTemplateItem> SelectTemplateItemsForTouch(
        IReadOnlyList<BizTestItemTemplateItem> allItems,
        int stationNo,
        int touchNo)
    {
        return allItems
            .Where(item => IsTemplateItemMatched(item, stationNo, touchNo))
            .GroupBy(item => item.ItemKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(item => item.StationNo == stationNo)
                .ThenByDescending(item => item.TouchNo == touchNo)
                .ThenBy(item => item.Sort)
                .First())
            .OrderBy(item => item.Sort)
            .ThenBy(item => item.ItemName)
            .ToList();
    }

    private static bool IsTemplateItemMatched(BizTestItemTemplateItem item, int stationNo, int touchNo)
    {
        var stationMatched = item.StationNo == ProductionConstants.Stations.SharedStationNo
            || item.StationNo == stationNo;
        var touchMatched = item.TouchNo == 0 || item.TouchNo == touchNo;
        return stationMatched && touchMatched;
    }

    private static WeldParameterRow CreateTemplatePreviewRow(
        ProductIdentity identity,
        BizProductProcessConfig config,
        BizTestItemTemplateItem item,
        int touchNo)
    {
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
            ActualAddress = item.ActualAddress ?? string.Empty,
            UpperAddress = item.UpperAddress ?? string.Empty,
            LowerAddress = item.LowerAddress ?? string.Empty,
            ResultAddress = item.ResultAddress ?? string.Empty,
            Value = "--",
            Result = "--",
            RecordTime = string.Empty,
            Sort = touchNo * 10000 + item.Sort,
            ItemKey = item.ItemKey,
            MesFieldPrefix = item.MesFieldPrefix,
            TemplateItemId = item.Id,
            ProcessConfigId = config.Id
        };
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
        return FindRawValue(rawValues, row.ItemKey, row.MesFieldPrefix)
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
        return FindRawValue(rawValues, $"{row.ItemKey}_result", $"{row.MesFieldPrefix}Result", $"{row.MesFieldPrefix}_result")
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
        TableStyleHelper.ApplyAntdTable(table2, AntdUI.ColumnsMode.Fill);
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
        table2.RowHeight = 38;
        table2.RowHeightHeader = 40;
        table2.Gap = 6;
        table2.GapCell = 3;
        table2.Gaps = new Size(6, 6);
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
        if (string.IsNullOrWhiteSpace(value))
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
        QueueRefreshProductTemplatePreview(force: true);
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
            var byMesProgramId = localPrograms.FirstOrDefault(item =>
                string.Equals(item.ProgramId?.Trim(), programId, StringComparison.OrdinalIgnoreCase));
            if (byMesProgramId is not null)
            {
                return byMesProgramId;
            }
        }

        return localPrograms.FirstOrDefault(item =>
            string.Equals(item.ProgramName?.Trim(), program.ProgramName?.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.ProductNum?.Trim(), program.ProductNum?.Trim(), StringComparison.OrdinalIgnoreCase));
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
        _runtimeStatusText = NormalizePanelMessage(message);
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
        _runtimeErrorText = NormalizePanelMessage(message);
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
        groupBox2.ForeColor = color;
        inputRunningStatus.ForeColor = color;
    }

    /// <summary>
    /// 异常提示有内容时使用红色，无异常时弱化显示，避免用户误以为仍有故障。
    /// </summary>
    private void ApplyRuntimeErrorTone(bool hasError)
    {
        var color = hasError ? UiColors.Status.Danger : UiColors.Status.Muted;
        groupBox1.ForeColor = color;
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
        return NormalizePanelMessage(_localizer.GetString(messageKey, args));
    }

    private static string NormalizePanelMessage(string? message)
    {
        return string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
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

    private sealed class WeldParameterRow
    {
        public int StationNo { get; init; }

        public string Station { get; init; } = string.Empty;

        public string ProductNum { get; init; } = string.Empty;

        public string ProductModel { get; init; } = string.Empty;

        public int TouchIndex { get; init; }

        public string TouchNo { get; init; } = string.Empty;

        public string ParameterName { get; init; } = string.Empty;

        public string Unit { get; init; } = string.Empty;

        public string ActualAddress { get; init; } = string.Empty;

        public string UpperAddress { get; init; } = string.Empty;

        public string LowerAddress { get; init; } = string.Empty;

        public string ResultAddress { get; init; } = string.Empty;

        public string Value { get; set; } = "--";

        public string Result { get; set; } = "--";

        public string RecordTime { get; set; } = string.Empty;

        public int Sort { get; init; }

        public string ItemKey { get; init; } = string.Empty;

        public string? MesFieldPrefix { get; init; }

        public int TemplateItemId { get; init; }

        public int ProcessConfigId { get; init; }

        public string UniqueKey => $"{StationNo}|{ProductNum}|{ProductModel}|{TouchIndex}|{ItemKey}";
    }

    private sealed record ProductionMetricRow(string Name, string Value);
}
