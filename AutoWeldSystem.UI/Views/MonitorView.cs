using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.DTOs.Upload;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Exceptions;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Core.ViewModels;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Controls;
using AutoWeldSystem.UI.Forms;
using AutoWeldSystem.UI.Infrastructure;
using AutoWeldSystem.UI.ViewModels;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace AutoWeldSystem.UI.Views;

public partial class MonitorView : BaseView
{
    #region 常量配置

    private const int TitleTextPadding = 8;
    private const int RealtimePreviewPaintIntervalMs = 500;
    private const int WeldPreviewMouseWheelPixels = 96;
    private const int RuntimeSummaryMaxLength = 56;
    private const int PlcStatusToolTipRefreshIntervalMs = 500;
    private const int PlcStatusToolTipHoverPollIntervalMs = 100;

    private const int PlcStatusToolTipMaxWidth = 480;
    private const int PlcStatusHistoryLimit = 5;
    private const int PlcStatusToolTipPadding = 10;
    private const int PlcStatusToolTipRadius = 8;
    private const int PlcStatusToolTipShadow = 6;
    private const int PlcStatusToolTipGap = 4;
    private const float PlcStatusToolTipFontSize = 9F;
    private const int WmSetRedraw = 0x000B;

    private static readonly TimeSpan RecipePreparationTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan FinishRecipeReadFailureLogInterval = TimeSpan.FromSeconds(30);
    private const string RuntimeSummaryOverflowSuffix = "...";
    // 报警属于整台设备，只使用一个通知 ID，双工位不再各弹一张卡片。
    private const string PlcAlarmNotificationId = "monitor-plc-alarm";
    private const string RuntimeErrorSourceDeviceAlarm = "DeviceAlarm";
    private const string PreviewTouchNoColumn = "TouchNo";
    private const string PreviewTouchResultColumn = "TouchResult";
    private const string PreviewMessageColumn = "Message";
    private const string PreviewUpperRole = "Upper";
    private const string PreviewLowerRole = "Lower";
    private const string PreviewActualRole = "Actual";
    private const string PreviewResultRole = "Result";

    #endregion

    #region 工位操作静态状态

    private static readonly object StationOperationSync = new();
    private static readonly HashSet<int> BusyOperationStations = new();

    #endregion

    #region 定时器

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _realtimePreviewPaintTimer = new() { Interval = RealtimePreviewPaintIntervalMs };
    private readonly System.Windows.Forms.Timer _plcStatusToolTipTimer = new() { Interval = PlcStatusToolTipHoverPollIntervalMs };


    #endregion

    #region 注入服务

    private AppSettings _currentSettings;

    private readonly ILocalizationService _localizer;
    private readonly PermissionUiBinder _permissionUiBinder;
    private readonly IAppSettingsService _settingsService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IMesConnectionMonitor _mesConnectionMonitorService;
    private readonly IPlcProductionMonitorService _plcProductionMonitorService;
    private readonly IPlcWorkIdMonitorService _plcWorkIdMonitorService;
    private readonly IPlcWeldCycleMonitorService _plcWeldCycleMonitorService;
    private readonly IPlcAddressService _plcAddressService;
    private readonly IPlcBusinessSignalService _plcBusinessSignalService;
    private readonly IPlcRecipeReconcileMonitorService _plcRecipeReconcileMonitorService;
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

    #endregion

    #region 运行状态

    private string? _runtimeStatusKey = TextKeys.Monitor.RuntimeStatus.Idle;
    private object[] _runtimeStatusArgs = Array.Empty<object>();
    private string? _runtimeStatusText;
    private bool _runtimeStatusTextIsSuccess;
    private string? _runtimeErrorKey;
    private object[] _runtimeErrorArgs = Array.Empty<object>();
    private string? _runtimeErrorText;
    private string? _runtimeErrorSource;
    private string? _deviceAlarmRuntimeErrorText;
    private bool _deviceAlarmPendingConfirmation;
    // 报警地址属于整台设备，通知与已读状态按设备维护单份，不再按工位拆分。
    private string? _plcAlarmNotificationSignature;
    private string? _plcAlarmNotificationDismissedSignature;
    private string? _plcAlarmSummaryDismissedSignature;
    private readonly Dictionary<int, DateTime> _finishRecipeReadFailureLogTimes = new();
    private readonly Dictionary<int, string> _lastAutoQueriedWorkIds = new();
    private readonly HashSet<int> _workOrderBaselines = new();
    // Stores the value that may be used for start; typing changes are drafts until Enter.
    private readonly Dictionary<int, string> _confirmedWorkOrderInputs = new();
    // One request per station is current. A PLC scan replaces a pending manual query immediately.
    private readonly Dictionary<int, CancellationTokenSource> _workOrderLoadCancellationTokens = new();
    private readonly List<OfflineProgramNameOption> _offlineProgramNameOptions = new();
    private IReadOnlyList<BizProgram> _localProgramSnapshot = Array.Empty<BizProgram>();
    private int _programSnapshotRefreshVersion;
    private readonly List<OfflineProductNumOption> _offlineProductNumOptions = new();
    // 操作员离线态录入或选择的产品工号；跨 1Hz 重绑定保留，按工位隔离。无条目表示操作员尚未录入，工号保持为空。
    private readonly Dictionary<int, string> _userSelectedOfflineProductNums = new();

    private bool _syncingStationSelection;
    private bool _syncingProcessSelection;
    private bool _syncingOfflineProgramSelection;
    private bool _syncingOfflineProductNumSelection;
    private bool _syncingOnlineProgramSelection;
    private bool _syncingOfflineInputs;
    private bool _syncingWorkOrderInput;
    private bool _syncingOperatorInput;
    private bool _syncingDualWorkOrderToggle;
    private bool _syncingProductNumberFilterToggle;
    private bool _syncingMergedDisplayToggle;
    private bool _syncingFaceResultDisplayToggle;
    private string? _validatedOperatorNumber;
    private string? _pendingOnlineProgramName;
    private string? _pendingOnlineProgramWorkOrderKey;
    private string _pendingOnlineProgramRecipeCode = string.Empty;
    private bool _offlineWorkOrderEditedByUser;
    private bool _offlineInputModeActive;
    // 记录操作员已显式选择离线程序或配方的工位。
    private readonly HashSet<int> _offlineProgramSelectedByUserStations = new();
    private bool _manualWorkOrderEditedByUser;
    private string? _lastBoundOnlineWorkOrderKey;
    private bool _dualStationEnabled;
    private bool _adjustingTitleFont;
    private Font? _titleFont;
    private Font? _runtimeMessageFont;
    private Font? _runtimeGroupFont;

    #endregion

    #region 预览状态

    private readonly List<WeldParameterRow> _weldParameterRows = new();
    private readonly object _realtimePreviewSync = new();

    /// <summary>
    /// 四面整件检测的合并显示列与取值，来自实时预览快照，四面未采集齐时值为空。
    /// </summary>
    private IReadOnlyList<WholePieceMergedColumn> _mergedPreviewColumns = Array.Empty<WholePieceMergedColumn>();
    private IReadOnlyDictionary<string, string> _mergedPreviewValues =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<WholePieceAbValueDefinition> _mergedPreviewDefinitions =
        Array.Empty<WholePieceAbValueDefinition>();

    /// <summary>
    /// 合并显示中超出程序设定值的列名，用于把 NG 定位到具体列。PLC 读取模式下为空。
    /// </summary>
    private IReadOnlyList<string> _mergedFailedColumns = Array.Empty<string>();
    private ProductRealtimePreviewSnapshot? _pendingRealtimePreviewSnapshot;
    private ProductIdentity? _currentProductIdentity;
    private DateTime _lastSchemePreviewRefreshTime = DateTime.MinValue;
    private string _lastSchemePreviewKey = string.Empty;
    private string _weldParameterLayoutKey = string.Empty;
    private string _weldParameterPreviewSchemaKey = string.Empty;
    private string _weldParameterVisibleValueKey = string.Empty;
    private readonly Dictionary<int, string> _productHistorySchemaKeys = new();
    private readonly Dictionary<int, string> _lastRealtimeProductNumbers = new();
    private readonly int _uiThreadId = Environment.CurrentManagedThreadId;

    private bool _refreshingSchemePreview;
    private bool _refreshingProductHistoryPreview;
    private bool _productHistoryRefreshPending;
    private int _productHistoryRefreshPosted;
    private bool _weldParameterTableBound;
    private bool _realtimePreviewApplyPosted;
    private bool _syncingWeldPreviewHorizontalScroll;
    private CancellationTokenSource? _businessSignalReconcileCancellation = new();
    private bool _deviceModeReconcileRunning;
    private bool _workOrderStatusReconcileRunning;
    private bool _lastMesConnected;
    private CancellationTokenSource? _pendingUploadRetryCancellation = new();
    private Task? _pendingUploadRetryTask;
    private int _pendingUploadRetryRunning;
    private bool _plcStatusToolTipVisible;
    private MonitorRightLayoutMode? _lastRightLayoutMode;
    private int _lastRightLayoutViewportHeight = -1;
    private int _lastRightLayoutDpi = -1;

    #endregion

    #region PLC 状态悬浮提示状态

    private DateTime _lastPlcStatusToolTipRefreshTime = DateTime.MinValue;
    private string _lastPlcStatusToolTipText = string.Empty;
    private int _lastPlcStatusToolTipClientWidth = -1;
    private int _lastPlcStatusToolTipDpi = -1;
    private AntdUI.Panel? _plcStatusToolTipPanel;
    private Label? _plcStatusToolTipLabel;
    private Font? _plcStatusToolTipFont;

    private readonly Dictionary<int, PlcConnectionSnapshot> _lastPlcHistorySnapshots = new();
    private readonly List<PlcStatusHistoryEntry> _plcStatusHistory = new();

    #endregion

    #region 业务信号调和状态

    private readonly Dictionary<int, int> _lastWorkOrderStatusSnapshots = new();
    private readonly Dictionary<int, int> _lastDeviceModeSnapshots = new();
    private readonly Dictionary<int, SemaphoreSlim> _workOrderStatusLocks = new();
    private readonly Dictionary<int, SemaphoreSlim> _deviceModeLocks = new();
    private readonly object _businessSignalLockSync = new();

    private int _viewStationNo = ProductionConstants.Stations.DefaultStationNo;
    private bool _stationViewReadOnly;
    private bool _enableBusinessSignalReconcile = true;

    #endregion

    #region 系统调用

    /// <summary>
    /// 调用 Win32 SendMessage 接口向指定窗口发送消息。
    /// </summary>
    /// <param name="hWnd">hWnd。</param>
    /// <param name="msg">窗口消息编号。</param>
    /// <param name="wParam">消息的第一个附加参数。</param>
    /// <param name="lParam">消息的第二个附加参数。</param>
    /// <returns>Win32 API 返回值。</returns>
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化监控视图，完成服务注入、界面配置、事件绑定和初始数据刷新。
    /// </summary>
    /// <param name="localizer">界面文本本地化服务。</param>
    /// <param name="permissionUiBinder">权限 UI 绑定器。</param>
    /// <param name="settingsService">系统设置读取与变更通知服务。</param>
    /// <param name="mesConnectionMonitorService">MES 连接状态监控服务。</param>
    /// <param name="plcCommunicationService">PLC 通讯服务。</param>
    /// <param name="plcProductionMonitorService">PLC 生产数据监控服务。</param>
    /// <param name="plcWorkIdMonitorService">PLC 工单号监控服务。</param>
    /// <param name="plcWeldCycleMonitorService">PLC 焊接节拍监控服务。</param>
    /// <param name="plcAddressService">PLC 地址配置服务。</param>
    /// <param name="plcBusinessSignalService">PLC 业务信号读写服务。</param>
    /// <param name="plcExpressionReadService">PLC 表达式解析读取服务。</param>
    /// <param name="productProcessConfigService">产品工艺配置服务。</param>
    /// <param name="testSchemeConfigService">测试方案配置服务。</param>
    /// <param name="productRealtimePreviewService">产品实时预览服务。</param>
    /// <param name="productHistoryService">产品历史查询与标记服务。</param>
    /// <param name="programManageService">本地程序管理服务。</param>
    /// <param name="weldTaskService">焊接任务业务服务。</param>
    /// <param name="exceptionLogService">程序异常与业务异常日志服务。</param>
    /// <param name="productionLogService">生产流程日志服务。</param>
    /// <param name="runtimeTipStateService">运行提示状态持久化服务。</param>
    public MonitorView(
        ILocalizationService localizer,
        PermissionUiBinder permissionUiBinder,
        IAppSettingsService settingsService,
        IMesConnectionMonitor mesConnectionMonitorService,
        IPlcCommunicationService plcCommunicationService,
        IPlcProductionMonitorService plcProductionMonitorService,
        IPlcWorkIdMonitorService plcWorkIdMonitorService,
        IPlcWeldCycleMonitorService plcWeldCycleMonitorService,
        IPlcAddressService plcAddressService,
        IPlcBusinessSignalService plcBusinessSignalService,
        IPlcRecipeReconcileMonitorService plcRecipeReconcileMonitorService,
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
        _permissionUiBinder = permissionUiBinder;
        _settingsService = settingsService;
        _currentSettings = _settingsService.Get();
        _mesConnectionMonitorService = mesConnectionMonitorService;
        _plcCommunicationService = plcCommunicationService;
        _plcProductionMonitorService = plcProductionMonitorService;
        _plcWorkIdMonitorService = plcWorkIdMonitorService;
        _plcWeldCycleMonitorService = plcWeldCycleMonitorService;
        _plcAddressService = plcAddressService;
        _plcBusinessSignalService = plcBusinessSignalService;
        _plcRecipeReconcileMonitorService = plcRecipeReconcileMonitorService;
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

        GetVersion();
        ConfigureRuntimeMessagePanels();
        ConfigureReportButtonLayout();
        ApplyLocalizedTexts();
        ConfigureDeviceMode();
        SyncProductNumberFilterToggle(_currentSettings.UseProductNumberFilter);
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

    #endregion

    #region 公开视图配置

    /// <summary>
    /// 配置当前窗口展示的工位、只读模式和业务信号调和开关。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <param name="readOnly">是否以只读模式显示。</param>
    /// <param name="enableBusinessSignalReconcile">是否启用 PLC 业务信号调和。</param>
    public void ConfigureStationView(int stationNo, bool readOnly, bool enableBusinessSignalReconcile = true)
    {
        _viewStationNo = NormalizeStationNo(stationNo);
        _stationViewReadOnly = readOnly;
        _enableBusinessSignalReconcile = enableBusinessSignalReconcile;

        ConfigureDeviceMode();
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

    /// <summary>
    /// 应用运行时设置变更，并刷新当前监控界面状态。
    /// </summary>
    /// <param name="settings">应用设置快照。</param>
    /// <param name="readOnly">是否以只读模式显示。</param>
    /// <param name="enableBusinessSignalReconcile">是否启用 PLC 业务信号调和。</param>
    /// <param name="triggerBusinessSignalReconcile">设置变更后是否立即触发业务信号调和。</param>
    public void ApplyRuntimeSettingsChanged(
        AppSettings settings,
        bool readOnly,
        bool enableBusinessSignalReconcile,
        bool triggerBusinessSignalReconcile = false)
    {
        UpdateSettingsSnapshot(settings);
        _stationViewReadOnly = readOnly;
        _enableBusinessSignalReconcile = enableBusinessSignalReconcile;

        ConfigureDeviceMode();
          SyncProductNumberFilterToggle(_currentSettings.UseProductNumberFilter);
      ApplyMesStatus(_mesConnectionMonitorService.Current);
        ApplyDeviceIdText();
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

    #endregion

    #region 工位选择与视图模式

    /// <summary>
    /// 配置设备模式。
    /// </summary>
    private void ConfigureDeviceMode()
    {
        _dualStationEnabled = _currentSettings.EnableDualStation;
        SyncDualWorkOrderAvailability();

        tlpStation.Visible = _dualStationEnabled;
        tabsPreview2.Visible = _dualStationEnabled;
        tabsMetrics2.Visible = _dualStationEnabled;
        tagResult2.Visible = _dualStationEnabled;

        if (!_dualStationEnabled)
        {
            tabsPreview.SelectedIndex = 0;
            tabsMetrics.SelectedIndex = 0;

            // Column1：100F
            tlpResult.ColumnStyles[0].SizeType = SizeType.Percent;
            tlpResult.ColumnStyles[0].Width = 100F;
            // Column2：0F
            tlpResult.ColumnStyles[1].SizeType = SizeType.Absolute;
            tlpResult.ColumnStyles[1].Width = 0F;
        }

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

    /// <summary>
    /// 绑定工位选择。
    /// </summary>
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

    /// <summary>
    /// 应用工位视图模式。
    /// </summary>
    private void ApplyStationViewMode()
    {
        ApplyOperationMode();
    }

    /// <summary>
    /// 将离线按钮和在线上报按钮固定为左右各半。
    /// </summary>
    private void ConfigureReportButtonLayout()
    {
        // 运行时强制两列布局，避免设计器历史列配置影响按钮比例。
        tlpButton.ColumnCount = 2;
        tlpButton.ColumnStyles.Clear();
        tlpButton.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tlpButton.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        tlpButton.ColumnStyles[0].SizeType = SizeType.Percent;
        tlpButton.ColumnStyles[0].Width = 50F;
        tlpButton.ColumnStyles[1].SizeType = SizeType.Percent;
        tlpButton.ColumnStyles[1].Width = 50F;

        tlpButton.SetColumn(btnLocalWorkOrder, 0);
        tlpButton.SetColumnSpan(btnLocalWorkOrder, 1);
        tlpButton.SetColumn(btnOnlineReport, 1);
        tlpButton.SetColumnSpan(btnOnlineReport, 1);
    }

    /// <summary>
    /// 应用操作模式。
    /// </summary>
    private void ApplyOperationMode()
    {
        var canOperate = !_stationViewReadOnly;

        btnLocalWorkOrder.Visible = canOperate;
        btnOnlineReport.Visible = canOperate;

        btnLocalWorkOrder.Enabled = canOperate;
        btnOnlineReport.Enabled = canOperate;
        ApplyReportButtonState();
    }

    /// <summary>
    /// 根据 MES 连接和当前任务状态刷新在线上报按钮及离线按钮。
    /// </summary>
    private void ApplyReportButtonState()
    {
        var activeTask = _weldTaskService.RestoreUnfinishedTask(CurrentStationNo)
            ?? GetCurrentStationState().ActiveTask;
        var hasOnlineRunningTask = activeTask is { IsOfflineCreated: false, EndTime: null };
        var hasOfflineRunningTask = activeTask is { IsOfflineCreated: true, EndTime: null };
        var decision = MonitorReportButtonRules.Decide(
            _stationViewReadOnly,
            _mesConnectionMonitorService.Current.IsConnected,
            ArePlcStationsConnected(CurrentStationNo),
            hasOnlineRunningTask,
            hasOfflineRunningTask);

        var isFinishAction = decision.OnlineReportAction == MonitorOnlineReportAction.Finish;
        var permissionCode = PermissionCodes.Buttons.Monitor.OnlineReport;

        btnOnlineReport.Text = _localizer.GetString(isFinishAction
            ? TextKeys.Monitor.Button.FinishReport
            : TextKeys.Monitor.Button.StartReport);
        btnOnlineReport.IconSvg = isFinishAction
            ? "CheckCircleOutlined"
            : "PlayCircleOutlined";
        btnOnlineReport.Visible = decision.ShowOnlineReportButton;
        _permissionUiBinder.ApplyEnabled(btnOnlineReport, permissionCode);
        btnOnlineReport.Enabled = btnOnlineReport.Enabled && decision.OnlineReportEnabled;
        btnLocalWorkOrder.Enabled = decision.LocalWorkOrderEnabled;
    }

    /// <summary>
    /// 判断读取OnlyOperationBlocked。
    /// </summary>
    /// <param name="actionName">操作名称，用于提示和日志。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private bool IsReadOnlyOperationBlocked(string actionName)
    {
        if (!_stationViewReadOnly)
        {
            return false;
        }

        SetRuntimeError(TextKeys.Monitor.RuntimeError.ReadOnlyOperationBlocked);
        return true;
    }

    /// <summary>
    /// 处理Sync工位选择。
    /// </summary>
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

            if (tabsPreview.Pages.Count > index && tabsPreview.SelectedIndex != index)
            {
                tabsPreview.SelectedIndex = index;
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

    /// <summary>
    /// 格式化工位名称。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>处理后的文本。</returns>
    private string FormatStationName(int stationNo)
    {
        return $"{_localizer.GetString(TextKeys.Monitor.Label.Station)} {stationNo}";
    }

    private int CurrentStationNo => NormalizeStationNo(_viewStationNo);

    /// <summary>
    /// 获取当前工位状态。
    /// </summary>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private ProductionStationRuntimeState GetCurrentStationState()
    {
        return _weldTaskService.CurrentState.GetOrCreateStation(CurrentStationNo);
    }

    /// <summary>
    /// 解析工单信号Stations。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析后的集合。</returns>
    private IReadOnlyList<int> ResolveWorkOrderSignalStations(int stationNo)
    {
        var settings = _currentSettings;
        return RecipeStationScopeRules.ResolveSharedRecipeStations(
            settings.EnableDualStation,
            settings.EnableDualWorkOrder,
            NormalizeStatusStationNo(stationNo));
    }

    /// <summary>
    /// 判断两个工位是否共享同一个生产任务；共享任务不能把单一 RecipeCode 当作各工位配方真值。
    /// </summary>
    private bool SharesRecipeTaskAcrossStations()
    {
        var settings = _currentSettings;
        return settings.EnableDualStation && !settings.EnableDualWorkOrder;
    }

    private DataGridView CurrentWeldPreviewGrid => GetWeldPreviewGrid(CurrentStationNo);

    private SlimHorizontalScrollBar CurrentWeldPreviewScrollBar
        => GetWeldPreviewScrollBar(CurrentStationNo);

    /// <summary>
    /// 获取焊接预览Grid。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private DataGridView GetWeldPreviewGrid(int stationNo)
        => NormalizeStationNo(stationNo) == 2 ? dgvPreview2 : dgvPreview1;

    /// <summary>
    /// 获取焊接预览滚动Bar。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private SlimHorizontalScrollBar GetWeldPreviewScrollBar(int stationNo)
        => NormalizeStationNo(stationNo) == 2 ? HorizontalScrollBar2 : HorizontalScrollBar1;

    /// <summary>
    /// 解析焊接预览工位号。
    /// </summary>
    /// <param name="control">目标控件。</param>
    /// <returns>解析或计算后的数值。</returns>
    private int ResolveWeldPreviewStationNo(Control control)
    {
        if (ReferenceEquals(control, dgvPreview2)
            || ReferenceEquals(control, HorizontalScrollBar2))
        {
            return 2;
        }

        return 1;
    }

    /// <summary>
    /// 规范化预览工位号。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析或计算后的数值。</returns>
    private static int NormalizeStationNo(int stationNo) => stationNo == 2 ? 2 : 1;

    private AntdUI.Table CurrentMetricTable => CurrentStationNo == 2 ? tableMetric2 : tableMetric1;

    private AntdUI.Table CurrentProductHistoryTable => CurrentStationNo == 2 ? tableHistory2 : tableHistory1;

    private AntdUI.Label CurrentLivePreviewStatusLabel => CurrentStationNo == 2 ? lblLiveHint2 : lblLiveHint1;

    private AntdUI.Label CurrentLiveProductNoLabel => CurrentStationNo == 2 ? lblLiveProductNo2 : lblLiveProductNo1;

    private AntdUI.Label CurrentLiveTouchCountLabel => CurrentStationNo == 2 ? lblLiveTouchNo2 : lblLiveTouchNo1;

    /// <summary>
    /// 获取当前工单号快照。
    /// </summary>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private PlcWorkIdSnapshot GetCurrentWorkIdSnapshot()
    {
        return _plcWorkIdMonitorService.GetCurrent(CurrentStationNo);
    }

    /// <summary>
    /// 获取当前LiveWorkId。
    /// </summary>
    /// <returns>处理后的文本。</returns>
    private string GetCurrentLiveWorkId()
    {
        var snapshot = GetCurrentWorkIdSnapshot();
        return snapshot.IsSuccess
            ? snapshot.WorkId.Trim()
            : string.Empty;
    }

    /// <summary>
    /// 获取当前生产快照。
    /// </summary>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private PlcProductionSnapshot GetCurrentProductionSnapshot()
    {
        return _plcProductionMonitorService.GetCurrent(CurrentStationNo);
    }

    #endregion

    #region 设置快照

    /// <summary>
    /// 更新Settings快照。
    /// </summary>
    /// <param name="settings">应用设置快照。</param>
    private void UpdateSettingsSnapshot(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Interlocked.Exchange(ref _currentSettings, settings);
    }

    #endregion

    #region 事件订阅

    /// <summary>
    /// 订阅监控视图需要的界面事件和后台服务事件。
    /// </summary>
    private void WireEvents()
    {
        Load += MonitorView_Load;
        GlobalContext.SessionChanged += GlobalContext_SessionChanged;

        btnLocalWorkOrder.Click += LocalWorkOrder_Click;
        btnOnlineReport.Click += OnlineReport_Click;
        btnClearErrorTips.Click += RuntimeErrorClearButton_Click;
        chkEnableDualWorkOrder.CheckedChanged += DualWorkOrder_CheckedChanged;
        chkFilterByProductNumber.CheckedChanged += FilterByProductNumber_CheckedChanged;
        chkMergedDisplay1.CheckedChanged += MergedDisplay_CheckedChanged;
        chkFaceResultDisplay1.CheckedChanged += FaceResultDisplay_CheckedChanged;
        inputSN.TextChanged += WorkOrderInput_TextChanged;
        inputSN.KeyDown += WorkOrderInput_KeyDown;
        selectProgramName.SelectedIndexChanged += ProgramNameSelection_SelectedIndexChanged;
        selectProdNum.SelectedIndexChanged += ProductNumSelection_SelectedIndexChanged;
        // 产品工号允许手工录入现场工号，选择事件覆盖不到键盘输入，需要额外监听文本变化。
        selectProdNum.TextChanged += ProductNumInput_TextChanged;
        // 滚轮换选会静默改变程序/配方/工序并触发下载或工序重载，禁用避免误操作。
        selectProgramName.WheelModifyEnabled = false;
        selectProdNum.WheelModifyEnabled = false;
        selectItemName.WheelModifyEnabled = false;
        MesUserNumber.KeyDown += OperatorInput_KeyDown;
        MesUserNumber.TextChanged += OperatorInput_TextChanged;

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

        tableHistory1.CellClick += ProductHistoryTable_CellClick;
        tableHistory2.CellClick += ProductHistoryTable_CellClick;

        segmentedStationSwitch.SelectIndexChanged += Station_SelectedIndexChanged;
        selectItemName.SelectedIndexChanged += ProcessSelection_SelectedIndexChanged;
        tabsPreview.SelectedIndexChanged += (_, _) => StationTab_SelectedIndexChanged(tabsPreview.SelectedIndex + 1);
        tabsMetrics.SelectedIndexChanged += (_, _) => StationTab_SelectedIndexChanged(tabsMetrics.SelectedIndex + 1);

        _weldTaskService.StateChanged += WeldTaskService_StateChanged;
        _plcCommunicationService.StatusChanged += PlcCommunicationService_StatusChanged;
        _mesConnectionMonitorService.StatusChanged += MesConnectionMonitorService_StatusChanged;
        _plcProductionMonitorService.StatusChanged += PlcProductionMonitorService_StatusChanged;
        _plcWorkIdMonitorService.WorkIdChanged += PlcWorkIdMonitorService_WorkIdChanged;
        _plcWeldCycleMonitorService.WeldPointCollected += PlcWeldCycleMonitorService_WeldPointCollected;
        _plcRecipeReconcileMonitorService.RecipeCodeChanged += PlcRecipeReconcileMonitorService_RecipeCodeChanged;
        _productRealtimePreviewService.SnapshotChanged += ProductRealtimePreviewService_SnapshotChanged;
        _productionLogService.LogWritten += ProductionLogService_LogWritten;
        _uploadTaskService.TaskStatusChanged += UploadTaskService_TaskStatusChanged;
        _programManageService.ProgramLookupsChanged += ProgramManageService_ProgramLookupsChanged;
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    /// <summary>
    /// 订阅焊接预览表格的鼠标、滚动和列变化事件。
    /// </summary>
    /// <param name="grid">目标表格控件。</param>
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

    /// <summary>
    /// 取消订阅焊接预览表格事件，避免控件释放后仍触发回调。
    /// </summary>
    /// <param name="grid">目标表格控件。</param>
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

    #endregion

    #region 按钮事件

    /// <summary>
    /// 异步下载下拉框当前选中的在线程序详情，并在下载成功后弹程序内容预览/微调窗。
    /// 失败统一记录业务日志并在提示区报错，不弹业务警告窗。
    /// </summary>
    private async Task DownloadSelectedOnlineProgramAsync(MesProgramListItemData programListItem, int stationNo)
    {
        await RunUiOperationAsync(async () =>
        {
            var state = GetCurrentStationState();
            if (!IsOnlineStartInputEditable(state))
            {
                return;
            }

            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.DownloadingProgram);
            var detail = await _weldTaskService.DownloadProgramAsync(programListItem, stationNo);
            if (detail is null)
            {
                _exceptionLogService.WriteBusiness(
                    "MES.DownloadProgram",
                    _localizer.GetString(TextKeys.Monitor.Message.ProgramDownloadFailed),
                    "MES 程序详情下载失败或返回空数据。",
                    FormatProgram(programListItem));
                SetRuntimeError(TextKeys.Monitor.Message.ProgramDownloadFailed);
                return;
            }

            ClearPendingOnlineProgramSelection();

            // 程序内容预览/微调窗：OK 时把合并后的内容写回选中程序（只对本次开工生效）。
            using var form = new ProgramContentReviewForm(detail, _testSchemeConfigService.GetItems());
            if (form.ShowDialog(this) != DialogResult.OK)
            {
                // 取消则保留下载的默认内容，不做任何修改。
                RefreshProductionRuntimeState();
                SyncOnlineProgramSelectionAfterDownload(detail);
                return;
            }

            detail.ProgramContent = form.MergedContentJson;
            _weldTaskService.ApplyStartAdjustment(
                state.CurrentWorkOrder!,
                state.SelectedProcess,
                detail,
                stationNo);
            RefreshProductionRuntimeState();
            SyncOnlineProgramSelectionAfterDownload(detail);
            ClearRuntimeError();
            SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.ProgramConfirmed);
        });
    }

    /// <summary>
    /// 从当前工位已加载的程序列表解析下拉框选中的程序项。
    /// </summary>
    private MesProgramListItemData? ResolveSelectedOnlineProgramListItem()
    {
        return ResolveOnlineProgramListItemByName(GetProgramNameSelectionText());
    }

    /// <summary>
    /// 获取程序名称下拉当前选中文本。
    /// AntdUI 筛选态下拉的事件索引指向筛选后的子列表，选中文本才是跨状态稳定的唯一键。
    /// </summary>
    private string GetProgramNameSelectionText()
    {
        return (selectProgramName.SelectedValue as string ?? selectProgramName.Text)?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// 获取配方号下拉当前选中文本。
    /// </summary>
    /// <returns>当前配方号文本。</returns>
    /// <summary>
    /// 按程序名称解析在线程序列表项。
    /// </summary>
    /// <param name="selectedName">下拉选中的程序名称。</param>
    /// <returns>解析到的程序；名称为空或列表中不存在时返回 null。</returns>
    private MesProgramListItemData? ResolveOnlineProgramListItemByName(string? selectedName)
    {
        var name = selectedName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return GetCurrentStationState().AvailablePrograms.FirstOrDefault(program =>
            string.Equals(program.ProgramName?.Trim(), name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 按配方号解析在线程序列表项，用于配方号下拉反向联动程序名称。
    /// MES 列表项不带配方号，因此必须使用本地同步程序表解析。
    /// </summary>
    /// <param name="selectedRecipeCode">下拉选中的配方号。</param>
    /// <returns>解析到的 MES 程序列表项；不存在时返回 null。</returns>
    /// <summary>
    /// 预览在线程序选择，避免后台 StateChanged 在详情下载前清空下拉框和配方号。
    /// 程序列表恰在重载（工单加载/工序切换后列表短暂为空）时保留名称预览，
    /// 等列表就绪后由 <see cref="BindOnlineProgramNameOptions"/> 恢复选中，开工前再补下载。
    /// </summary>
    /// <param name="programListItem">按名称解析到的在线程序列表项；列表重载期间可为 null。</param>
    /// <param name="selectedName">当前下拉选中的程序名称。</param>
    private void ApplyOnlineProgramSelectionPreview(MesProgramListItemData? programListItem, string? selectedName)
    {
        var name = programListItem?.ProgramName?.Trim() ?? selectedName?.Trim();
        if (string.IsNullOrWhiteSpace(name) || !IsOnlineStartInputEditable(GetCurrentStationState()))
        {
            ClearPendingOnlineProgramSelection();
            return;
        }

        _pendingOnlineProgramName = name;
        _pendingOnlineProgramWorkOrderKey = GetCurrentStationState().CurrentWorkOrder?.SN?.Trim();
        _pendingOnlineProgramRecipeCode = programListItem is not null
            ? ResolveRecipeCodeForPendingProgram(programListItem)
            : string.Empty;

        SyncProgramNameSelectionDisplay(name);
    }

    /// <summary>
    /// 清空尚未完成下载确认的在线程序预览状态。
    /// </summary>
    private void ClearPendingOnlineProgramSelection()
    {
        _pendingOnlineProgramName = null;
        _pendingOnlineProgramWorkOrderKey = null;
        _pendingOnlineProgramRecipeCode = string.Empty;
    }

    /// <summary>
    /// 下载/微调流程结束后保持下拉选中该程序并同步配方号。
    /// AntdUI 下拉重复点击同一项仍会触发 SelectedIndexChanged，无需释放选中索引即可再次下载。
    /// </summary>
    /// <param name="program">本次下载得到的程序详情。</param>
    private void SyncOnlineProgramSelectionAfterDownload(ProgramDataRes program)
    {
        SyncProgramNameSelectionDisplay(program.ProgramName?.Trim() ?? string.Empty);
    }

    /// <summary>
    /// 以程序名称同步下拉选中态和配方号显示，始终在同步旗标内执行，不触发用户选择逻辑。
    /// </summary>
    private void SyncProgramNameSelectionDisplay(string programName)
    {
        _syncingOnlineProgramSelection = true;
        try
        {
            ForceProgramNameSelection(FindProgramNameItemIndex(programName), programName);
        }
        finally
        {
            _syncingOnlineProgramSelection = false;
        }
    }

    /// <summary>
    /// 在程序名称下拉选项中定位指定名称。
    /// </summary>
    /// <returns>选项索引；不存在时返回 -1。</returns>
    private int FindProgramNameItemIndex(string programName)
    {
        for (var i = 0; i < selectProgramName.Items.Count; i++)
        {
            if (string.Equals(selectProgramName.Items[i]?.ToString()?.Trim(), programName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 在配方号下拉选项中定位指定配方。
    /// </summary>
    /// <param name="recipeCode">配方号。</param>
    /// <returns>选项索引；不存在时返回 -1。</returns>
    /// <summary>
    /// 强制程序名称下拉选中态与目标一致。
    /// AntdUI 重建 Items 不会复位内部索引，直接赋相同索引会被短路导致 Text 与 Items 脱节，
    /// 因此先归位 -1 再赋目标索引；索引赋值内部同步 Text、不触发筛选，并顺带清空残留筛选文本。
    /// 名称不在选项中时仅保留文本显示（如列表重载间隙或任务恢复的历史程序）。
    /// </summary>
    private void ForceProgramNameSelection(int index, string text)
    {
        if (selectProgramName.SelectedIndex != -1)
        {
            selectProgramName.SelectedIndex = -1;
        }

        if (index >= 0)
        {
            selectProgramName.SelectedIndex = index;
            return;
        }

        if (!string.Equals(selectProgramName.Text, text, StringComparison.Ordinal))
        {
            selectProgramName.Text = text;
        }
    }

    /// <summary>
    /// 强制产品工号下拉选中态与目标一致，复用程序名称下拉的 -1 归位规避 AntdUI 索引短路。
    /// 工号不在选项中时仅保留文本显示（如在线工单工号未建本地程序）。
    /// </summary>
    private void ForceProductNumSelection(int index, string text)
    {
        if (selectProdNum.SelectedIndex != -1)
        {
            selectProdNum.SelectedIndex = -1;
        }

        if (index >= 0)
        {
            selectProdNum.SelectedIndex = index;
            return;
        }

        if (!string.Equals(selectProdNum.Text, text, StringComparison.Ordinal))
        {
            selectProdNum.Text = text;
        }
    }

    /// <summary>
    /// 由程序同步产品工号下拉显示文本，避免触发工号联动。
    /// </summary>
    /// <param name="productNum">需要显示的产品工号。</param>
    private void SetProductNumSelectionText(string productNum)
    {
        _syncingOfflineProductNumSelection = true;
        try
        {
            var index = _offlineProductNumOptions.FindIndex(
                option => string.Equals(option.DisplayText, productNum, StringComparison.Ordinal));
            ForceProductNumSelection(index, productNum);
        }
        finally
        {
            _syncingOfflineProductNumSelection = false;
        }
    }

    /// <summary>
    /// 强制配方号下拉选中态与目标一致，用于程序名联动、PLC 离线回填和列表刷新。
    /// </summary>
    /// <param name="index">目标索引；不存在时传 -1。</param>
    /// <param name="text">需要显示的配方号。</param>
    /// <summary>
    /// 从主界面控件构造本次开工的工单快照（可空项允许空串）。
    /// 产品工号取自 selectProdNum 控件的实际选中值；产品型号优先使用手工输入，否则使用 MES 工单值。
    /// </summary>
    private WorkOrderRes BuildAdjustedWorkOrderFromInputs(WorkOrderRes source)
    {
        return new WorkOrderRes
        {
            SN = inputSN.Text.Trim(),
            ProdNum = FirstNonEmpty(GetProductNumInputText(), source.ProdNum),
            ProdModel = FirstNonEmpty(inputProdModel.Text, source.ProdModel),
            Spec = inputSpec.Text.Trim(),
            Batch = inputBatch.Text.Trim(),
            ProductName = inputProductName.Text.Trim(),
            DrawingNo = inputDrawingNo.Text.Trim(),
            ProjectFrom = source.ProjectFrom,
            ExpItems = source.ExpItems?.Select(CloneExpItem).ToList() ?? []
        };
    }

    /// <summary>
    /// 从主界面控件构造本次开工的工序快照；StartAmount 解析失败时保留原值。
    /// </summary>
    private ExpItemData BuildAdjustedProcessFromInputs(ExpItemData source)
    {
        var process = CloneExpItem(source);
        process.ProcessNo = inputProcessNo.Text.Trim();
        process.ItemName = string.IsNullOrWhiteSpace(selectItemName.Text)
            ? source.ItemName
            : selectItemName.Text.Trim();
        process.StartAmount = int.TryParse(inputStartAmount.Text.Trim(), out var quantity) && quantity > 0
            ? quantity
            : source.StartAmount;
        return process;
    }

    private static ExpItemData CloneExpItem(ExpItemData source)
    {
        return new ExpItemData
        {
            ItemId = source.ItemId,
            ItemTitle = source.ItemTitle,
            ItemCont = source.ItemCont,
            SequenceNo = source.SequenceNo,
            ItemName = source.ItemName,
            ProcessNo = source.ProcessNo,
            StartAmount = source.StartAmount
        };
    }

    /// <summary>
    /// 处理本地工单按钮点击事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private async void LocalWorkOrder_Click(object? sender, EventArgs e)
    {
        if (IsReadOnlyOperationBlocked("本地工单"))
        {
            return;
        }

        var stationNo = CurrentStationNo;
        SelectStationForOperation(stationNo);
        var activeTask = _weldTaskService.RestoreUnfinishedTask(stationNo);
        // 本地任务未完工时，此按钮复用为“本地完工”，减少离线流程入口数量。
        if (activeTask is { IsOfflineCreated: true, EndTime: null })
        {
            await FinishLocalWorkOrderAsync(stationNo);
            return;
        }

        if (activeTask is not null && activeTask.EndTime is null)
        {
            SetRuntimeError(TextKeys.Monitor.Message.StartBlockedByUnfinishedTask);
            return;
        }

        if (!EnsurePlcConnectedForStart())
        {
            return;
        }

        if (!TryBuildOfflineStartRequest(stationNo, out var request, out var selectedProgram))
        {
            return;
        }

        // 离线员工号由操作员在界面录入，不再用登录账号兜底，因此开工前必须校验非空；
        // MES 离线无法校验身份，只按录入值原样上报和落库。
        var employeeNumber = MesUserNumber.Text.Trim();
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            SetRuntimeError(TextKeys.Monitor.RuntimeError.OperatorNumberRequired);
            return;
        }

        var fullProgram = await _programManageService.GetProgramAsync(selectedProgram!.Program.Id);
        if (fullProgram is null)
        {
            SetRuntimeError(TextKeys.Monitor.RuntimeError.ProgramNameRequired);
            return;
        }

        request.ProgramId = string.IsNullOrWhiteSpace(fullProgram.ProgramId) ? $"local-{fullProgram.Id}" : fullProgram.ProgramId;
        request.ProgramName = fullProgram.ProgramName;
        request.ProgramType = string.IsNullOrWhiteSpace(fullProgram.ProgramType) ? "0" : fullProgram.ProgramType;
        request.ProgramContent = string.IsNullOrWhiteSpace(fullProgram.ProgramContent) ? "{}" : fullProgram.ProgramContent;
        // 工号以界面录入值为准（BuildRequest 已在留空时回退程序工号），不能用完整程序把操作员改写刷回去。
        request.ProductNum = FirstNonEmpty(GetProductNumInputText(), fullProgram.ProductNum);
        request.ProductModel = inputProdModel.Text.Trim();
        request.RecipeCode = ProgramRecipeMappingRules.Resolve(fullProgram, stationNo);

        var localProgram = new ProgramDataRes
        {
            Id = request.ProgramId,
            ProgramName = request.ProgramName,
            ProductNum = request.ProductNum,
            RecipeCode = request.RecipeCode,
            ProgramType = request.ProgramType,
            ProgramContent = request.ProgramContent
        };

        await RunReportOperationAsync(stationNo, "本地开工", async () =>
        {
            ClearRuntimeError();
            await _weldTaskService.StartLocalAsync(request, employeeNumber, 0);
            _offlineWorkOrderEditedByUser = false;
            ApplyOfflineProgramNameOption(selectedProgram, syncProgramFields: false);
            RefreshProductionRuntimeState();
            QueueRefreshSchemePreview(force: true);
            SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.LocalStartSucceeded);
        });

        // PLC 业务信号独立写入；失败只提示和记录日志，不回滚已经成功的本地开工。
        await SafeWriteStartBusinessSignalsAsync(localProgram, stationNo);
    }

    /// <summary>
    /// 处理在线上报按钮点击，根据当前任务状态执行开工或完工。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private async void OnlineReport_Click(object? sender, EventArgs e)
    {
        var stationNo = CurrentStationNo;
        var activeTask = _weldTaskService.RestoreUnfinishedTask(stationNo)
            ?? GetCurrentStationState().ActiveTask;

        if (activeTask is { IsOfflineCreated: false, EndTime: null })
        {
            await RunFinishReportAsync();
            return;
        }

        await RunStartReportAsync();
    }

    /// <summary>
    /// 开工前确认本次操作涉及的全部 PLC 工位均已建立连接。
    /// </summary>
    private bool EnsurePlcConnectedForStart()
    {
        if (ArePlcStationsConnected(CurrentStationNo))
        {
            return true;
        }

        SetRuntimeError(TextKeys.Monitor.Message.PlcDisconnected);
        return false;
    }

    /// <summary>
    /// 双工位共用工单时同时检查两个工位；独立工单只检查当前工位。
    /// </summary>
    private bool ArePlcStationsConnected(int stationNo)
        => ResolveWorkOrderSignalStations(stationNo)
            .All(targetStationNo => _plcCommunicationService.GetCurrent(targetStationNo).IsConnected);

    /// <summary>
    /// 执行在线开工上报流程。
    /// </summary>
    private async Task RunStartReportAsync()
    {
        if (!EnsurePlcConnectedForStart())
        {
            return;
        }

        if (IsReadOnlyOperationBlocked("开工上报"))
        {
            return;
        }

        var stationNo = CurrentStationNo;
        SelectStationForOperation(stationNo);
        // 开工前先恢复持久化任务，软件重启后也能拦截未完工任务。
        if (_weldTaskService.RestoreUnfinishedTask(stationNo) is not null)
        {
            RefreshProductionRuntimeState();
            SetRuntimeError(TextKeys.Monitor.Message.StartBlockedByUnfinishedTask);
            return;
        }

        var state = GetCurrentStationState();
        if (state.CurrentWorkOrder is null)
        {
            SetRuntimeError(TextKeys.Monitor.RuntimeError.WorkOrderRequired);
            return;
        }

        if (!WorkOrderInputConfirmationRules.IsConfirmed(inputSN.Text, GetConfirmedWorkOrderInput(stationNo)))
        {
            SetRuntimeError(TextKeys.Monitor.RuntimeError.WorkOrderRequired);
            return;
        }

        if (state.SelectedProcess is null)
        {
            SetRuntimeError(TextKeys.Monitor.Message.ProcessRequired);
            return;
        }

        // 下拉已选定程序但详情尚未下载时，先内联补一次下载（下载失败不弹窗，仅提示区报错）。
        if (state.SelectedProgram is null)
        {
            var programListItem = ResolveSelectedOnlineProgramListItem();
            if (programListItem is not null)
            {
                await DownloadSelectedOnlineProgramAsync(programListItem, stationNo);
            }

            state = GetCurrentStationState();
        }

        if (state.SelectedProgram is null)
        {
            SetRuntimeError(TextKeys.Monitor.RuntimeError.ProgramSelectionRequired);
            return;
        }

        // 工序号在主界面控件中编辑，开工前必须非空。
        var processNo = inputProcessNo.Text.Trim();
        if (string.IsNullOrWhiteSpace(processNo))
        {
            SetRuntimeError(TextKeys.Monitor.Message.ProcessRequired);
            return;
        }

        // 从控件构造本次开工的工单/工序快照，可空项允许空串，应用为内存态（只对本次生效，不落库）。
        var adjustedWorkOrder = BuildAdjustedWorkOrderFromInputs(state.CurrentWorkOrder!);
        var adjustedProcess = BuildAdjustedProcessFromInputs(state.SelectedProcess!);
        var adjustedProgram = state.SelectedProgram!;
        _weldTaskService.ApplyStartAdjustment(adjustedWorkOrder, adjustedProcess, adjustedProgram, stationNo);

        var actualQty = 0;

        var settings = _currentSettings;
        var useOperatorDialog = settings.UseOperatorInputDialog ?? true;
        string employeeNumber;
        if (useOperatorDialog)
        {
            employeeNumber = await PromptValidatedOperatorAsync(stationNo);
            if (string.IsNullOrWhiteSpace(employeeNumber))
            {
                return;
            }
        }
        else
        {
            employeeNumber = MesUserNumber.Text.Trim();
            if (string.IsNullOrWhiteSpace(employeeNumber))
            {
                SetRuntimeError(TextKeys.Monitor.RuntimeError.OperatorNumberRequired);
                return;
            }

            // 员工号已输入但尚未按回车完成内联身份校验（或校验后又修改了内容）。
            if (!IsInlineOperatorValidated(employeeNumber))
            {
                SetRuntimeError(TextKeys.Monitor.RuntimeError.OperatorValidationRequired);
                return;
            }

            // 已通过身份校验；WeldTaskService 已存储本次校验的操作员信息，直接使用缓存结果。
        }

        await RunReportOperationAsync(stationNo, "开工上报", async () =>
        {
            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.SubmittingStart);
            await _weldTaskService.StartAsync(employeeNumber, actualQty, stationNo, employeeAlreadyValidated: true);
            RefreshProductionRuntimeState();
            QueueRefreshSchemePreview(force: true);
            SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.OnlineStartSucceeded);
        });

        // PLC 业务信号独立写入；失败只提示和记录日志，不回滚已经成功的在线开工。
        await SafeWriteStartBusinessSignalsAsync(adjustedProgram, stationNo);
    }

    /// <summary>
    /// 执行在线完工上报流程。
    /// </summary>
    private async Task RunFinishReportAsync()
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
            SetRuntimeError(TextKeys.Monitor.Message.FinishPrerequisiteMissing);
            return;
        }

        // 完工不再二次弹窗校验员工，直接沿用在线开工时写入任务的员工号。
        var employeeNumber = activeTask.UserNumber?.Trim() ?? string.Empty;

        // 完工数量优先来自 PLC；配置允许时才通过弹窗补录，避免上报数量与设备数据不一致。
        if (!TryResolveFinishQuantities(stationNo, out var actualQty, out var qualifiedQty, out var failedQty))
        {
            return;
        }

        await RunReportOperationAsync(stationNo, "完工上报", async () =>
        {
            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.SubmittingFinish);
            await RefreshRecipeCodeFromPlcBeforeFinishAsync(activeTask, stationNo);
            await _weldTaskService.FinishAsync(employeeNumber, actualQty, qualifiedQty, failedQty, stationNo);
            ClearFinishedProductIdentity(stationNo);
            // 完工后立即禁止 PLC 继续生产，防止操作员未重新开工时设备继续采集。
            await WriteFinishBusinessSignalsAsync(stationNo);
            RefreshProductionRuntimeState();
            SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.OnlineFinishSucceeded);
        });
    }

    #endregion

    #region WinForms 生命周期事件

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible)
        {
            ApplyResponsiveRightLayout();
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyResponsiveRightLayout();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ApplyResponsiveRightLayout(force: true);
    }

    /// <summary>
    /// 处理MonitorView加载。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void MonitorView_Load(object? sender, EventArgs e)
    {
        _timer.Start();
        _realtimePreviewPaintTimer.Start();
        _ = RefreshLocalProgramSnapshotAsync(rebindOptions: true);
        ApplyLocalizedTexts();
        SyncDualWorkOrderToggle(_currentSettings.EnableDualWorkOrder);
        SyncMergedDisplayToggle(_currentSettings.IsWholePieceMergedDisplayEnabled);
        SyncFaceResultDisplayToggle(_currentSettings.IsWholePieceFaceResultDisplayEnabled);
        UpdateCurrentTime();
        ConfigureDeviceMode();
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
        ApplyResponsiveRightLayout(force: true);
    }

    /// <summary>
    /// 根据右侧视口高度和 DPI 分配工单、状态、结果及生产指标区域。
    /// </summary>
    private void ApplyResponsiveRightLayout(bool force = false)
    {
        if (IsDisposed
            || VerticalSplitter is null
            || tlpRight is null
            || tlpRight.RowStyles.Count < 5)
        {
            return;
        }

        var viewportHeight = VerticalSplitter.Panel2.ClientSize.Height;
        if (viewportHeight <= 0)
        {
            return;
        }

        var dpi = DeviceDpi > 0 ? DeviceDpi : MonitorRightLayoutRules.BaseDpi;
        var layout = MonitorRightLayoutRules.Resolve(viewportHeight, dpi);
        if (!force
            && _lastRightLayoutMode == layout.Mode
            && _lastRightLayoutViewportHeight == viewportHeight
            && _lastRightLayoutDpi == dpi)
        {
            return;
        }

        VerticalSplitter.Panel2.SuspendLayout();
        tlpRight.SuspendLayout();
        try
        {
            _lastRightLayoutMode = layout.Mode;
            _lastRightLayoutViewportHeight = viewportHeight;
            _lastRightLayoutDpi = dpi;

            tlpRight.RowStyles[0].SizeType = SizeType.Percent;
            tlpRight.RowStyles[0].Height = 100F;
            SetAbsoluteRowHeight(tlpRight.RowStyles[1], layout.StatusPanelHeight);
            SetAbsoluteRowHeight(tlpRight.RowStyles[2], layout.StatusPanelHeight);
            SetAbsoluteRowHeight(tlpRight.RowStyles[3], layout.ProductResultHeight);
            SetAbsoluteRowHeight(tlpRight.RowStyles[4], layout.MetricPanelHeight);

            SetFixedHeight(grpErrorTips, layout.StatusPanelHeight);
            SetFixedHeight(grpRunningStatus, layout.StatusPanelHeight);
            SetFixedHeight(grpProductResult, layout.ProductResultHeight);
            ApplyProductionMetricTableStyle(layout.MetricRowHeight, layout.MetricHeaderHeight);

            tlpRight.Height = layout.ContentHeight;
            VerticalSplitter.Panel2.AutoScrollMinSize = layout.RequiresScroll
                ? new Size(0, layout.ContentHeight)
                : Size.Empty;
            if (!layout.RequiresScroll)
            {
                VerticalSplitter.Panel2.AutoScrollPosition = Point.Empty;
            }
        }
        finally
        {
            tlpRight.ResumeLayout(true);
            VerticalSplitter.Panel2.ResumeLayout(true);
        }
    }

    private static void SetAbsoluteRowHeight(RowStyle rowStyle, int height)
    {
        rowStyle.SizeType = SizeType.Absolute;
        rowStyle.Height = Math.Max(0, height);
    }

    private static void SetFixedHeight(Control control, int height)
    {
        var normalizedHeight = Math.Max(0, height);
        control.MinimumSize = new Size(0, normalizedHeight);
        control.MaximumSize = new Size(0, normalizedHeight);
    }

    /// <summary>
    /// 当前登录用户变化后，重新计算在线按钮对应的开工/完工权限，
    /// 以及两个写全局设置的显示开关是否对该角色可见。
    /// </summary>
    private void GlobalContext_SessionChanged(object? sender, EventArgs e)
    {
        RunOnUiThread(
            () =>
            {
                ApplyReportButtonState();
                SyncMergedDisplayToggle(_currentSettings.IsWholePieceMergedDisplayEnabled);
            },
            "MonitorView.SessionChanged",
            requireHandle: false);
    }

    /// <summary>
    /// 处理Timer定时触发事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
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

    /// <summary>
    /// 处理Language变更。
    /// </summary>
    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ConfigureDeviceMode();
        BindProductionRuntimeState();
        ConfigureProductionTableColumns();
        ConfigureWeldParameterTableColumns();
        RefreshRuntimePanels();
        ApplyAllStationStatuses();
        ApplyMesStatus(_mesConnectionMonitorService.Current);
        QueueRefreshSchemePreview(force: true);
        AdjustTitleFontSize();
    }

    /// <summary>
    /// 处理HandleDestroyed。
    /// </summary>
    /// <param name="e">事件参数。</param>
    protected override void OnHandleDestroyed(EventArgs e)
    {
        CancelPendingUploadRetry();
        CancelAndDispose(ref _businessSignalReconcileCancellation);
        GlobalContext.SessionChanged -= GlobalContext_SessionChanged;
        _settingsService.SettingsChanged -= OnSettingsChanged;
        _weldTaskService.StateChanged -= WeldTaskService_StateChanged;
        _plcCommunicationService.StatusChanged -= PlcCommunicationService_StatusChanged;
        _mesConnectionMonitorService.StatusChanged -= MesConnectionMonitorService_StatusChanged;
        _plcProductionMonitorService.StatusChanged -= PlcProductionMonitorService_StatusChanged;
        _plcWorkIdMonitorService.WorkIdChanged -= PlcWorkIdMonitorService_WorkIdChanged;
        _plcWeldCycleMonitorService.WeldPointCollected -= PlcWeldCycleMonitorService_WeldPointCollected;
        _plcRecipeReconcileMonitorService.RecipeCodeChanged -= PlcRecipeReconcileMonitorService_RecipeCodeChanged;
        _productRealtimePreviewService.SnapshotChanged -= ProductRealtimePreviewService_SnapshotChanged;
        _productionLogService.LogWritten -= ProductionLogService_LogWritten;
        _uploadTaskService.TaskStatusChanged -= UploadTaskService_TaskStatusChanged;
        _programManageService.ProgramLookupsChanged -= ProgramManageService_ProgramLookupsChanged;
        inputSN.TextChanged -= WorkOrderInput_TextChanged;
        inputSN.KeyDown -= WorkOrderInput_KeyDown;
        chkEnableDualWorkOrder.CheckedChanged -= DualWorkOrder_CheckedChanged;
        chkFilterByProductNumber.CheckedChanged -= FilterByProductNumber_CheckedChanged;
        chkMergedDisplay1.CheckedChanged -= MergedDisplay_CheckedChanged;
        chkFaceResultDisplay1.CheckedChanged -= FaceResultDisplay_CheckedChanged;
        selectProgramName.SelectedIndexChanged -= ProgramNameSelection_SelectedIndexChanged;
        selectProdNum.SelectedIndexChanged -= ProductNumSelection_SelectedIndexChanged;
        selectProdNum.TextChanged -= ProductNumInput_TextChanged;
        MesUserNumber.KeyDown -= OperatorInput_KeyDown;
        MesUserNumber.TextChanged -= OperatorInput_TextChanged;
        tableHistory1.CellClick -= ProductHistoryTable_CellClick;
        tableHistory2.CellClick -= ProductHistoryTable_CellClick;
        UnwireWeldPreviewGridEvents(dgvPreview1);
        UnwireWeldPreviewGridEvents(dgvPreview2);
        HorizontalScrollBar1.ValueChanged -= Table2HorizontalScrollBar_ValueChanged;
        HorizontalScrollBar2.ValueChanged -= Table2HorizontalScrollBar_ValueChanged;
        tagPLC.MouseEnter -= TagPLC_MouseEnter;
        tagPLC.MouseLeave -= TagPLC_MouseLeave;
        foreach (var tokenSource in _workOrderLoadCancellationTokens.Values)
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }
        _workOrderLoadCancellationTokens.Clear();

        _timer.Stop();
        _realtimePreviewPaintTimer.Stop();
        _plcStatusToolTipTimer.Stop();
        _timer.Dispose();
        _realtimePreviewPaintTimer.Dispose();
        _plcStatusToolTipTimer.Dispose();
        CloseAllPlcAlarmNotifications();
        DisposePlcStatusToolTipPopup();
        _titleFont?.Dispose();
        _runtimeMessageFont?.Dispose();
        _runtimeGroupFont?.Dispose();
        base.OnHandleDestroyed(e);
    }

    #endregion

    #region 工位与工序事件

    /// <summary>
    /// 处理工位选择变化事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void Station_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingStationSelection || !_dualStationEnabled)
        {
            return;
        }

        var stationNo = Math.Clamp(e.Value + 1, 1, 2);
        SwitchStationFromUi(stationNo);
    }

    /// <summary>
    /// 处理工位Tab选择变化事件。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    private void StationTab_SelectedIndexChanged(int stationNo)
    {
        if (_syncingStationSelection || !_dualStationEnabled)
        {
            return;
        }

        SwitchStationFromUi(stationNo);
    }

    /// <summary>
    /// 处理工序选择选择变化事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private async void ProcessSelection_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingProcessSelection)
        {
            return;
        }

        if (IsOfflineInputEditable(GetCurrentStationState()))
        {
            return;
        }

        var state = GetCurrentStationState();
        var processes = state.CurrentWorkOrder?.ExpItems ?? [];
        // AntdUI 筛选态下拉的事件索引指向筛选后的子列表，按显示文本回查真实工序，
        // 事件索引仅在工序显示名重复时用于消歧。
        var selectedIndex = SelectListRules.ResolveSelectedIndex(
            processes.Select(GetProcessDisplayName).ToList(),
            selectItemName.SelectedValue as string ?? selectItemName.Text,
            e.Value);
        if (selectedIndex < 0 || selectedIndex >= processes.Count)
        {
            ClearProcessSelectionDisplay();
            inputProcessNo.Text = string.Empty;
            inputStartAmount.Text = string.Empty;
            return;
        }

        var process = processes[selectedIndex];
        SelectStationForOperation(CurrentStationNo);
        ClearPendingOnlineProgramSelection();
        _weldTaskService.SelectProcess(process, CurrentStationNo);
        ApplySelectedProcessInputs(process);
        ClearRuntimeError();
        if (await ReloadProgramsAfterProcessSelectionAsync(CurrentStationNo))
        {
            SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.ProcessSelected);
        }
    }

    /// <summary>
    /// 在线切换工序后重新加载可选程序，避免服务层清空程序列表后下拉无法再次下载。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>程序列表加载成功且存在可选程序返回 true。</returns>
    private async Task<bool> ReloadProgramsAfterProcessSelectionAsync(int stationNo)
    {
        await LoadProgramListForWorkOrderAsync(stationNo);
        return GetCurrentStationState().AvailablePrograms.Count > 0;
    }

    /// <summary>
    /// 处理工单号输入变化；离线时只记录人工修改，在线空闲时自动排队查询 MES 工单。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void WorkOrderInput_TextChanged(object? sender, EventArgs e)
    {
        if (_syncingOfflineInputs || _syncingWorkOrderInput)
        {
            return;
        }

        var stationNo = CurrentStationNo;
        var state = GetCurrentStationState();
        ClearConfirmedWorkOrderInput(stationNo);
        if (IsOfflineInputEditable(state))
        {
            _offlineWorkOrderEditedByUser = true;
            return;
        }

        if (!IsManualOnlineWorkOrderInputEditable(state))
        {
            return;
        }

        if (!string.Equals(inputSN.Text?.Trim(), state.CurrentWorkOrder?.SN?.Trim(), StringComparison.Ordinal))
        {
            ClearPendingOnlineProgramSelection();
        }

        // Manual typing is only a draft. The MES query starts after Enter confirms this value.
        _manualWorkOrderEditedByUser = true;
    }

    /// <summary>
    /// Confirms a manually entered work order. Online confirmation loads MES data; offline confirmation enables local start.
    /// </summary>
    private void WorkOrderInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        var stationNo = CurrentStationNo;
        var state = GetCurrentStationState();
        if (!IsOfflineInputEditable(state) && !IsManualOnlineWorkOrderInputEditable(state))
        {
            return;
        }

        if (!ConfirmManualWorkOrderInput(stationNo))
        {
            return;
        }

        if (IsManualOnlineWorkOrderInputEditable(state))
        {
            _ = StartWorkOrderLoadAsync(GetConfirmedWorkOrderInput(stationNo), stationNo, showDialogOnFailure: true);
        }
    }

    /// <summary>
    /// Stores the current input as the work order confirmed by manual Enter.
    /// </summary>
    private bool ConfirmManualWorkOrderInput(int stationNo)
    {
        var workId = WorkOrderInputConfirmationRules.Normalize(inputSN.Text);
        if (string.IsNullOrWhiteSpace(workId))
        {
            ClearConfirmedWorkOrderInput(stationNo);
            SetRuntimeError(TextKeys.Monitor.RuntimeError.WorkOrderRequired);
            return false;
        }

        SetWorkOrderInputText(workId);
        _confirmedWorkOrderInputs[NormalizeStationNo(stationNo)] = workId;
        return true;
    }

    /// <summary>
    /// 处理 PLC 成功清空工单号：立即释放扫码去重状态，空闲时只清空流转卡号及其确认状态。
    /// 运行任务期间保留任务工单显示，但去重状态仍需释放，确保完工后同一工单可直接重新扫码。
    /// </summary>
    private bool ApplyClearedPlcWorkOrderInput(PlcWorkIdSnapshot snapshot)
    {
        var stationNo = NormalizeStationNo(snapshot.StationNo);
        if (!WorkOrderAutoQueryRules.ShouldResetAfterPlcClear(snapshot.IsSuccess, snapshot.WorkId))
        {
            return false;
        }

        _lastAutoQueriedWorkIds.Remove(stationNo);
        // 空值证明 PLC 已完成一次清空，后续首个非空值应按新扫码处理，而不是启动残留。
        _workOrderBaselines.Add(stationNo);
        var state = _weldTaskService.CurrentState.GetOrCreateStation(stationNo);
        var stationIsIdle = !IsRunningWeldTask(state.ActiveTask)
            && _weldTaskService.GetUnfinishedTask(stationNo) is null;
        CancelWorkOrderLoad(stationNo);
        ClearConfirmedWorkOrderInput(stationNo);
        if (!stationIsIdle)
        {
            return true;
        }
        if (stationNo == CurrentStationNo)
        {
            _manualWorkOrderEditedByUser = false;
            // 标记为操作员草稿，使离线待开工信息在后续刷新周期中不被当成无准备状态清除。
            _offlineWorkOrderEditedByUser = IsOfflineInputEditable(state);
            SetWorkOrderInputText(string.Empty);
        }

        return true;
    }

    /// <summary>
    /// Applies an idle-station PLC work order with higher priority than an unconfirmed manual draft.
    /// </summary>
    private bool ApplyPlcWorkOrderInput(PlcWorkIdSnapshot snapshot)
    {
        var stationNo = NormalizeStationNo(snapshot.StationNo);
        var state = _weldTaskService.CurrentState.GetOrCreateStation(stationNo);
        var stationIsIdle = !IsRunningWeldTask(state.ActiveTask)
            && _weldTaskService.GetUnfinishedTask(stationNo) is null;
        if (!WorkOrderInputConfirmationRules.ShouldApplyPlcSnapshot(stationIsIdle, snapshot.IsSuccess, snapshot.WorkId))
        {
            return false;
        }

        // 启动后首个读数只记基准：PLC 侧持续驱动该寄存器，残留条码不能当作新扫码使用。
        if (WorkOrderAutoQueryRules.ShouldCaptureBaselineOnly(
                _workOrderBaselines.Contains(stationNo),
                snapshot.IsSuccess,
                snapshot.WorkId))
        {
            _workOrderBaselines.Add(stationNo);
            _lastAutoQueriedWorkIds[stationNo] = WorkOrderInputConfirmationRules.Normalize(snapshot.WorkId);
            return false;
        }

        var workId = WorkOrderInputConfirmationRules.Normalize(snapshot.WorkId);
        var hasManualDraft = _offlineWorkOrderEditedByUser || _manualWorkOrderEditedByUser;
        var isAlreadyApplied = !hasManualDraft
            && WorkOrderInputConfirmationRules.IsConfirmed(inputSN.Text, GetConfirmedWorkOrderInput(stationNo))
            && string.Equals(GetConfirmedWorkOrderInput(stationNo), workId, StringComparison.OrdinalIgnoreCase);
        if (isAlreadyApplied)
        {
            return false;
        }

        _confirmedWorkOrderInputs[stationNo] = workId;
        _offlineWorkOrderEditedByUser = false;
        _manualWorkOrderEditedByUser = false;
        ClearPendingOnlineProgramSelection();
        SetWorkOrderInputText(workId);
        return true;
    }

    /// <summary>
    /// Clears the confirmed value when the operator changes the input after confirmation.
    /// </summary>
    private void ClearConfirmedWorkOrderInput(int stationNo)
    {
        _confirmedWorkOrderInputs.Remove(NormalizeStationNo(stationNo));
    }

    private void CancelWorkOrderLoad(int stationNo)
    {
        if (!_workOrderLoadCancellationTokens.Remove(NormalizeStationNo(stationNo), out var tokenSource))
        {
            return;
        }

        tokenSource.Cancel();
        tokenSource.Dispose();
    }

    /// <summary>
    /// Gets the work order currently confirmed for the specified station.
    /// </summary>
    private string GetConfirmedWorkOrderInput(int stationNo)
    {
        return _confirmedWorkOrderInputs.TryGetValue(NormalizeStationNo(stationNo), out var workId)
            ? workId
            : string.Empty;
    }

    /// <summary>
    /// Starts the latest MES work-order request for a station and cancels a superseded request.
    /// </summary>
    private async Task StartWorkOrderLoadAsync(string workId, int stationNo, bool showDialogOnFailure)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var normalizedWorkId = WorkOrderInputConfirmationRules.Normalize(workId);
        if (string.IsNullOrWhiteSpace(normalizedWorkId))
        {
            return;
        }

        CancelWorkOrderLoad(normalizedStationNo);

        using var tokenSource = new CancellationTokenSource();
        _workOrderLoadCancellationTokens[normalizedStationNo] = tokenSource;
        try
        {
            await LoadWorkOrderInfoAsync(normalizedWorkId, normalizedStationNo, showDialogOnFailure, tokenSource.Token);
        }
        catch (OperationCanceledException) when (tokenSource.IsCancellationRequested)
        {
            // A newer manual confirmation or PLC scan replaced this request.
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.StartWorkOrderLoad");
            if (!tokenSource.IsCancellationRequested)
            {
                SetRuntimeError(TextKeys.Monitor.Message.WorkOrderLoadFailed);
            }
        }
        finally
        {
            if (_workOrderLoadCancellationTokens.TryGetValue(normalizedStationNo, out var currentTokenSource)
                && ReferenceEquals(currentTokenSource, tokenSource))
            {
                _workOrderLoadCancellationTokens.Remove(normalizedStationNo);
            }
        }
    }

    /// <summary>
    /// 处理员工号输入框回车键，触发内联身份校验。
    /// 仅在"操作员弹窗输入"关闭且在线空闲时有效，其余状态忽略。
    /// </summary>
    private void OperatorInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        _ = ValidateOperatorInlineAsync(CurrentStationNo);
    }

    /// <summary>
    /// 员工号输入框内容变化时，清除已校验状态并清空关联显示字段。
    /// 程序性赋值（BindMesOperatorInfo / ClearMesOperatorInfo）通过 _syncingOperatorInput 旗标绕过此逻辑。
    /// </summary>
    private void OperatorInput_TextChanged(object? sender, EventArgs e)
    {
        if (_syncingOperatorInput || _validatedOperatorNumber is null)
        {
            return;
        }

        _validatedOperatorNumber = null;
        ClearMesOperatorDisplayInfo();
    }

    /// <summary>
    /// 手动输入停顿达到防抖时间后自动查询工单。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    /// <summary>
    /// 选中产品工号后定位到该工号的程序。
    /// 启用“按产品工号筛选程序”时程序列表随之收窄；未启用时列表保持全量，仅跳转选中。
    /// 仅离线可编辑态生效；在线态工号跟随工单只读展示。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void ProductNumSelection_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingOfflineProductNumSelection)
        {
            return;
        }

        if (!IsOfflineInputEditable(GetCurrentStationState()))
        {
            return;
        }

        var productNum = GetSelectedOfflineProductNum();
        if (string.IsNullOrWhiteSpace(productNum))
        {
            return;
        }

        MarkOfflineProgramSelectionByUser(CurrentStationNo);
        RememberProductNumInput(productNum);
        BindOfflineProgramNameOptions();
        // 未启用筛选时程序列表是全量的，重绑定会保留原程序并把工号回写成原值，
        // 因此必须显式跳到该工号的首个程序。
        SelectFirstOfflineProgramForProductNum(productNum);
        ApplyOfflineProgramNameOption(GetSelectedOfflineProgramNameOption(), syncProgramFields: true);
        QueueRefreshSchemePreview(force: true);
    }

    /// <summary>
    /// 操作员手工录入产品工号时记住输入值。
    /// 手输不会触发 SelectedIndexChanged，若不在此记住，1Hz 运行态重绑定会把输入抹掉。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void ProductNumInput_TextChanged(object? sender, EventArgs e)
    {
        if (_syncingOfflineProductNumSelection || _syncingOfflineInputs)
        {
            return;
        }

        if (!IsOfflineInputEditable(GetCurrentStationState()))
        {
            return;
        }

        var productNum = selectProdNum.Text?.Trim() ?? string.Empty;
        RememberProductNumInput(productNum);
        if (productNum.Length == 0)
        {
            // 工号被清空时同步清掉与之关联的程序名称，两者保持同一空值状态，避免只剩程序名称显示着上一个工号的程序。
            ClearOfflineProgramSelectionByUser(CurrentStationNo);
            ClearOfflineProgramNameSelection();
        }
    }

    /// <summary>
    /// 记住当前工位操作员录入或选中的产品工号；清空输入时同时清除记忆，让工号回到空值。
    /// </summary>
    /// <param name="productNum">操作员录入或选中的产品工号。</param>
    private void RememberProductNumInput(string? productNum)
    {
        var stationKey = NormalizeStationNo(CurrentStationNo);
        var normalized = productNum?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(normalized))
        {
            _userSelectedOfflineProductNums.Remove(stationKey);
            return;
        }

        _userSelectedOfflineProductNums[stationKey] = normalized;
    }

    /// <summary>
    /// 在当前程序名称选项中定位指定产品工号的首个程序并选中。
    /// </summary>
    /// <param name="productNum">操作员选中的产品工号。</param>
    private void SelectFirstOfflineProgramForProductNum(string productNum)
    {
        var index = _offlineProgramNameOptions.FindIndex(option => string.Equals(
            option.Program.ProductNum?.Trim(),
            productNum,
            StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return;
        }

        _syncingOfflineProgramSelection = true;
        try
        {
            ForceProgramNameSelection(index, _offlineProgramNameOptions[index].DisplayText);
        }
        finally
        {
            _syncingOfflineProgramSelection = false;
        }
    }

    /// <summary>
    /// 清空离线程序名称下拉的选中态，供工号清空和转入离线时与工号保持同一空值状态。
    /// 走同步守卫，避免程序化清空被当成操作员选择而触发联动回填。
    /// </summary>
    private void ClearOfflineProgramNameSelection()
    {
        _syncingOfflineProgramSelection = true;
        try
        {
            ForceProgramNameSelection(-1, string.Empty);
        }
        finally
        {
            _syncingOfflineProgramSelection = false;
        }
    }

    /// <summary>
    /// 同步离线程序名称下拉选中项关联的产品工号、产品型号和配方号。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void ProgramNameSelection_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        // 离线可编辑态走本地程序联动；在线空闲态走 MES 下载与程序内容预览。
        var state = GetCurrentStationState();
        if (IsOfflineInputEditable(state))
        {
            if (_syncingOfflineProgramSelection)
            {
                return;
            }

            var option = GetSelectedOfflineProgramNameOption();
            if (option is not null)
            {
                MarkOfflineProgramSelectionByUser(CurrentStationNo);
            }
            ApplyOfflineProgramNameOption(option, syncProgramFields: true);
            if (option is not null)
            {
                QueueRefreshSchemePreview(force: true);
            }

            return;
        }

        if (_syncingOnlineProgramSelection)
        {
            return;
        }

        // AntdUI 筛选态下拉的事件索引指向筛选后的子列表，统一按选中文本解析程序。
        var selectedName = GetProgramNameSelectionText();
        var programListItem = ResolveOnlineProgramListItemByName(selectedName);
        ApplyOnlineProgramSelectionPreview(programListItem, selectedName);
        if (programListItem is null)
        {
            return;
        }

        // 在线选定程序后立即下载详情并弹程序内容预览窗，不自动选中以避免 PLC 扫码自动加载触发模态窗。
        _ = DownloadSelectedOnlineProgramAsync(programListItem, CurrentStationNo);
    }

    /// <summary>
    /// 同步配方号下拉选中项关联的程序名称。
    /// 在线开工时选择配方号等价于选择对应 MES 程序，会继续下载并弹出程序内容微调窗。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    #endregion

    #region 服务事件处理

    /// <summary>
    /// 处理实时预览PaintTimer定时触发事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void RealtimePreviewPaintTimer_Tick(object? sender, EventArgs e)
    {
        ApplyPendingRealtimePreviewSnapshot();
    }

    /// <summary>
    /// 处理Weld任务Service状态变更。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void WeldTaskService_StateChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        RunOnUiThread(ApplyWeldTaskStateChanged, "MonitorView.WeldTaskStateChanged");
    }

    /// <summary>
    /// 处理PlcCommunicationService状态变化事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void PlcCommunicationService_StatusChanged(object? sender, PlcConnectionSnapshot e)
    {
        if (IsDisposed)
        {
            return;
        }

        RunOnUiThread(() => ApplyPlcStatus(e), "MonitorView.PlcStatusChanged");
    }

    /// <summary>
    /// 处理MesConnectionMonitorService状态变化事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void MesConnectionMonitorService_StatusChanged(object? sender, MesConnectionSnapshot e)
    {
        if (IsDisposed)
        {
            return;
        }

        RunOnUiThread(() =>
        {
            ApplyMesStatus(e);
            BindProductionRuntimeState();
        }, "MonitorView.MesStatusChanged");
    }

    /// <summary>
    /// 处理Plc生产MonitorService状态变化事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void PlcProductionMonitorService_StatusChanged(object? sender, PlcProductionSnapshot e)
    {
        if (IsDisposed)
        {
            return;
        }

        RunOnUiThread(() => ApplyProductionStatus(e), "MonitorView.ProductionStatusChanged");
    }

    /// <summary>
    /// 处理PlcWorkIdMonitorService工单号变化事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void PlcWorkIdMonitorService_WorkIdChanged(object? sender, PlcWorkIdSnapshot e)
    {
        if (IsDisposed)
        {
            return;
        }

        RunOnUiThread(() =>
        {
            ApplyWorkIdSnapshot(e);
            QueueAutoWorkOrderQuery(e);
        }, "MonitorView.WorkIdChanged");
    }

    /// <summary>
    /// 处理PlcWeldCycleMonitorService焊点采集完成事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void PlcWeldCycleMonitorService_WeldPointCollected(object? sender, BizWeldPointRecord e)
    {
        if (IsDisposed)
        {
            return;
        }

        RunOnUiThread(() => ApplyLatestWeldPointRecord(e), "MonitorView.WeldPointCollected");
    }

    /// <summary>
    /// 处理产品实时预览Service快照变化事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    /// <summary>
    /// Handles PLC recipe readback snapshot changes for idle station display.
    /// </summary>
    private void PlcRecipeReconcileMonitorService_RecipeCodeChanged(object? sender, PlcRecipeCodeSnapshot e)
    {
        if (IsDisposed || !IsHandleCreated || e.StationNo != CurrentStationNo)
        {
            return;
        }

        RunOnUiThread(() => ApplyIdleRecipeCodeSnapshot(e), "MonitorView.RecipeCodeChanged");
    }

    /// <summary>
    /// Applies the latest PLC recipe readback to the recipe selector only when the station is idle.
    /// </summary>
    private void ApplyIdleRecipeCodeSnapshot(PlcRecipeCodeSnapshot snapshot)
    {
        if (snapshot.StationNo != CurrentStationNo || !snapshot.IsSuccess)
        {
            return;
        }

        var state = GetCurrentStationState();
        if (!IsRunningWeldTask(state.ActiveTask) && IsOfflineInputEditable(state))
        {
            QueueRefreshSchemePreview(force: true);
        }
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

        if (!RunOnUiThread(ApplyPendingRealtimePreviewSnapshot, "MonitorView.RealtimePreviewSnapshot"))
        {
            lock (_realtimePreviewSync)
            {
                _realtimePreviewApplyPosted = false;
            }
        }
    }

    /// <summary>
    /// 处理生产LogService日志写入事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    /// <summary>
    /// 过程参数上传完成后刷新产品历史，避免上传状态列停留在上一轮显示值。
    /// </summary>
    private void UploadTaskService_TaskStatusChanged(object? sender, UploadTaskStatusChangedEventArgs e)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (!string.Equals(e.TaskType, ProductionConstants.UploadTaskTypes.ProcessParameter, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var activeTask = GetCurrentStationState().ActiveTask;
        if (activeTask is null || e.WeldTaskId is not null && e.WeldTaskId.Value != activeTask.Id)
        {
            return;
        }

        RefreshProductHistoryPreview();
    }

    private void ProductionLogService_LogWritten(object? sender, ProductionFlowLogEntry e)
    {
        if (IsDisposed || !ShouldShowProductionHint(e))
        {
            return;
        }

        RunOnUiThread(() => ApplyProductionHint(e), "MonitorView.ProductionLogWritten");
    }

    /// <summary>
    /// 应用Weld任务状态变更。
    /// </summary>
    private void ApplyWeldTaskStateChanged()
    {
        RefreshProductionRuntimeState();
        QueueRefreshSchemePreview(force: true);
        if (_enableBusinessSignalReconcile)
        {
            QueueBusinessSignalReconciliation("WeldTaskService.StateChanged", includeDeviceMode: false);
        }
    }

    /// <summary>
    /// 处理Settings变更。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void OnSettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        var previousShowTestFlag = _currentSettings.ShowTestFlagInHistory != false;
        var previousMergedDisplay = _currentSettings.IsWholePieceMergedDisplayEnabled;
        var previousFaceResultDisplay = _currentSettings.IsWholePieceFaceResultDisplayEnabled;
        UpdateSettingsSnapshot(e.CurrentSettings);
        RunOnUiThread(() =>
        {
            lblTitle.Text = _currentSettings.DeviceName;
            ApplyDeviceIdText();
            SyncProductNumberFilterToggle(_currentSettings.UseProductNumberFilter);
            SyncDualWorkOrderAvailability();
            SyncDualWorkOrderToggle(_currentSettings.EnableDualWorkOrder);
            SyncMergedDisplayToggle(_currentSettings.IsWholePieceMergedDisplayEnabled);
            SyncFaceResultDisplayToggle(_currentSettings.IsWholePieceFaceResultDisplayEnabled);
            ApplyProgramLimitsDisplay();
        }, "MonitorView.SettingsChanged.DeviceIdentity");
        var currentShowTestFlag = e.CurrentSettings.ShowTestFlagInHistory != false;
        if (previousShowTestFlag != currentShowTestFlag)
        {
            RunOnUiThread(RefreshProductHistoryPreview, "MonitorView.SettingsChanged.ShowTestFlag");
        }

        // 系统设置页也能改合并显示和逐面结果显示，这里同步重建界面，避免两个入口结果不一致。
        var currentMergedDisplay = e.CurrentSettings.IsWholePieceMergedDisplayEnabled;
        var currentFaceResultDisplay = e.CurrentSettings.IsWholePieceFaceResultDisplayEnabled;
        if (previousMergedDisplay != currentMergedDisplay || previousFaceResultDisplay != currentFaceResultDisplay)
        {
            RunOnUiThread(RefreshMergedDisplayViews, "MonitorView.SettingsChanged.MergedDisplay");
        }
    }

    /// <summary>
    /// 处理监控页按产品工号筛选程序开关。
    /// </summary>
    private void FilterByProductNumber_CheckedChanged(object? sender, AntdUI.BoolEventArgs e)
    {
        if (_syncingProductNumberFilterToggle)
        {
            return;
        }

        var settings = _currentSettings.Clone();
        settings.UseProductNumberFilter = e.Value;
        var savedSettings = _settingsService.Save(settings);
        UpdateSettingsSnapshot(savedSettings);

        if (IsOfflineInputEditable(GetCurrentStationState()))
        {
            BindOfflineProgramNameOptions();
            return;
        }

        if (GetCurrentStationState().CurrentWorkOrder is not null)
        {
            _ = LoadProgramListForWorkOrderAsync(CurrentStationNo);
        }
        else
        {
            BindOnlineProgramNameOptions();
        }
    }

    /// <summary>
    /// 同步产品工号筛选复选框状态，避免程序性赋值再次保存。
    /// </summary>
    private void SyncProductNumberFilterToggle(bool useProductNumberFilter)
    {
        _syncingProductNumberFilterToggle = true;
        try
        {
            chkFilterByProductNumber.Checked = useProductNumberFilter;
        }
        finally
        {
            _syncingProductNumberFilterToggle = false;
        }
    }

    /// <summary>
    /// 单工位没有双工单语义，禁用并清除历史残留的双工单配置。
    /// </summary>
    private void SyncDualWorkOrderAvailability()
    {
        var available = _currentSettings.EnableDualStation;
        chkEnableDualWorkOrder.Enabled = available;
        if (available || !_currentSettings.EnableDualWorkOrder)
        {
            return;
        }

        var settings = _currentSettings.Clone();
        settings.EnableDualWorkOrder = false;
        var savedSettings = _settingsService.Save(settings);
        UpdateSettingsSnapshot(savedSettings);
        SyncDualWorkOrderToggle(false);
    }

    /// <summary>
    /// 处理监控页双工单快捷开关。
    /// </summary>
    private void DualWorkOrder_CheckedChanged(object? sender, AntdUI.BoolEventArgs e)
    {
        if (_syncingDualWorkOrderToggle)
        {
            return;
        }

        SaveDualWorkOrderMode(e.Value);
    }

    /// <summary>
    /// 保存双工单模式。勾选双工单时沿用系统设置页旧逻辑：自动启用双工位。
    /// </summary>
    private void SaveDualWorkOrderMode(bool enableDualWorkOrder)
    {
        var previousSettings = _currentSettings;
        var settings = previousSettings.Clone();
        settings.EnableDualWorkOrder = enableDualWorkOrder;
        if (enableDualWorkOrder)
        {
            settings.EnableDualStation = true;
        }

        if (!CanSaveDualModeChange(previousSettings, settings))
        {
            SyncDualWorkOrderToggle(previousSettings.EnableDualWorkOrder);
            ShowWarningText("存在未完工任务，不能切换双工位/双工单模式，请先完工后再调整。");
            return;
        }

        var savedSettings = _settingsService.Save(settings);
        UpdateSettingsSnapshot(savedSettings);
        SyncDualWorkOrderToggle(savedSettings.EnableDualWorkOrder);
    }

    /// <summary>
    /// 处理监控页合并显示快捷开关。合并显示只影响界面，切换后立即重建实时预览和产品历史。
    /// </summary>
    private void MergedDisplay_CheckedChanged(object? sender, AntdUI.BoolEventArgs e)
    {
        if (_syncingMergedDisplayToggle)
        {
            return;
        }

        var settings = _currentSettings.Clone();
        settings.EnableWholePieceMergedDisplay = e.Value;
        var savedSettings = _settingsService.Save(settings);
        UpdateSettingsSnapshot(savedSettings);
        // 合并视图没有面结果列，切换后要立即更新面结果开关的可见性。
        SyncFaceResultDisplayVisibility();
        RefreshMergedDisplayViews();
    }

    /// <summary>
    /// 同步合并显示复选框状态，避免程序性赋值再次触发保存。
    /// 合并显示只对整件检测有意义，其他设备类型隐藏开关；
    /// 该开关写的是全局设置，因此还要求当前角色具备对应权限。
    /// </summary>
    private void SyncMergedDisplayToggle(bool enableMergedDisplay)
    {
        _syncingMergedDisplayToggle = true;
        try
        {
            chkMergedDisplay1.Checked = enableMergedDisplay;
        }
        finally
        {
            _syncingMergedDisplayToggle = false;
        }

        chkMergedDisplay1.Visible = IsWholePieceInspectionDevice()
            && GlobalContext.HasPermission(PermissionCodes.Buttons.Monitor.MergedDisplay);
        // 合并模式没有面结果列，此时隐藏面结果开关，避免出现一个不起作用的勾选框。
        SyncFaceResultDisplayVisibility();
    }

    /// <summary>
    /// 处理监控页面结果显示快捷开关。只隐藏“面结果”列，面号和逐面实测值保留。
    /// </summary>
    private void FaceResultDisplay_CheckedChanged(object? sender, AntdUI.BoolEventArgs e)
    {
        if (_syncingFaceResultDisplayToggle)
        {
            return;
        }

        var settings = _currentSettings.Clone();
        settings.EnableWholePieceFaceResultDisplay = e.Value;
        var savedSettings = _settingsService.Save(settings);
        UpdateSettingsSnapshot(savedSettings);
        RefreshMergedDisplayViews();
    }

    /// <summary>
    /// 同步面结果显示复选框状态，避免程序性赋值再次触发保存。
    /// </summary>
    private void SyncFaceResultDisplayToggle(bool enableFaceResultDisplay)
    {
        _syncingFaceResultDisplayToggle = true;
        try
        {
            chkFaceResultDisplay1.Checked = enableFaceResultDisplay;
        }
        finally
        {
            _syncingFaceResultDisplayToggle = false;
        }

        SyncFaceResultDisplayVisibility();
    }

    /// <summary>
    /// 面结果开关只在整件检测的逐面模式下有意义，合并视图本身就没有面结果列。
    /// 该开关写的是全局设置，因此还要求当前角色具备对应权限。
    /// </summary>
    private void SyncFaceResultDisplayVisibility()
    {
        chkFaceResultDisplay1.Visible = IsWholePieceInspectionDevice()
            && !_currentSettings.IsWholePieceMergedDisplayEnabled
            && GlobalContext.HasPermission(PermissionCodes.Buttons.Monitor.FaceResultDisplay);
    }

    /// <summary>
    /// 当前过程参数设备类型是否为整件检测。合并显示和面结果开关都只对该设备类型有意义。
    /// </summary>
    private bool IsWholePieceInspectionDevice()
        => string.Equals(
            _currentSettings.ProcessParameterDeviceType?.Trim(),
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 合并显示切换后立即重建两个表格，不等下一帧 PLC 快照。
    /// </summary>
    private void RefreshMergedDisplayViews()
    {
        BindWeldParameterTable(forceRebind: true);
        RefreshProductHistoryPreview();
    }

    /// <summary>
    /// 同步双工单复选框状态，避免程序性赋值再次触发保存。
    /// </summary>
    private void SyncDualWorkOrderToggle(bool enableDualWorkOrder)
    {
        _syncingDualWorkOrderToggle = true;
        try
        {
            chkEnableDualWorkOrder.Checked = enableDualWorkOrder;
        }
        finally
        {
            _syncingDualWorkOrderToggle = false;
        }
    }

    /// <summary>
    /// 判断运行模式是否允许变更。存在未完工任务时禁止切换双工位/双工单模式。
    /// </summary>
    private bool CanSaveDualModeChange(AppSettings previousSettings, AppSettings newSettings)
    {
        if (previousSettings.EnableDualStation == newSettings.EnableDualStation
            && previousSettings.EnableDualWorkOrder == newSettings.EnableDualWorkOrder)
        {
            return true;
        }

        return !HasAnyUnfinishedTask();
    }

    /// <summary>
    /// 检查任一工位是否存在未完工任务。
    /// </summary>
    private bool HasAnyUnfinishedTask()
    {
        return _weldTaskService.GetUnfinishedTask(1) is not null
            || _weldTaskService.GetUnfinishedTask(2) is not null;
    }

    #endregion

    #region 表格与鼠标事件

    /// <summary>
    /// 处理产品历史表格Cell点击。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void ProductHistoryTable_CellClick(object sender, AntdUI.TableClickEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.Record is not ProductHistoryTableRow row)
        {
            return;
        }

        ShowProductHistoryContextMenu((Control)sender, row);
    }

    #endregion

    #region 版本信息

    /// <summary>
    /// 获取Version。
    /// </summary>
    private void GetVersion() => lblVersion.Text = BuildVersionText();

    /// <summary>
    /// 构建Version文本。
    /// </summary>
    /// <returns>处理后的文本。</returns>
    private static string BuildVersionText()
    {
        var assembly = typeof(MonitorView).Assembly;

        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        var version = string.IsNullOrWhiteSpace(informationalVersion) ? assembly.GetName().Version?.ToString(3) : informationalVersion;

        // InformationalVersion may contain source metadata after '+', but operators only need the release version.
        return version?.Split('+')[0] ?? string.Empty;
    }

    #endregion

    #region 运行提示面板

    /// <summary>
    /// 配置运行时MessagePanels。
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

    #endregion

    #region 标题布局调整

    /// <summary>
    /// 处理标题布局变更。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void TitleLayout_Changed(object? sender, EventArgs e)
    {
        AdjustTitleFontSize();
    }

    /// <summary>
    /// 处理Adjust标题字体尺寸。
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
    /// 查找最佳标题字体尺寸。
    /// </summary>
    /// <param name="text">显示文本。</param>
    /// <param name="baseFont">基础字体。</param>
    /// <param name="availableSize">可用显示尺寸。</param>
    /// <returns>解析或计算后的数值。</returns>
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

    #endregion

    #region 预览表格鼠标与滚动事件

    /// <summary>
    /// 处理表格2鼠标进入事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void Table2_MouseEnter(object? sender, EventArgs e)
    {
        // 工单输入区仍在编辑时不抢占键盘焦点，鼠标离开输入框后可继续录入。
        if (tlpWorkOrderInfo.ContainsFocus)
        {
            return;
        }

        var grid = CurrentWeldPreviewGrid;
        if (grid.CanFocus)
        {
            grid.Focus();
        }
    }

    /// <summary>
    /// 处理表格2鼠标滚轮事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
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

    /// <summary>
    /// 处理表格2滚动事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void Table2_Scroll(object? sender, ScrollEventArgs e)
    {
        if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
        {
            SyncWeldPreviewHorizontalScrollBar(sender as DataGridView);
        }
    }

    /// <summary>
    /// 处理表格2滚动范围变化事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void Table2_ScrollRangeChanged(object? sender, EventArgs e)
    {
        var grid = sender as DataGridView;
        SyncWeldPreviewHorizontalScrollBar(grid);
    }

    /// <summary>
    /// 处理表格2水平滚动Bar值变更。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
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

    #endregion

    #region PLC 状态悬浮提示

    /// <summary>
    /// 应用全部工位Statuses。
    /// </summary>
    private void ApplyAllStationStatuses()
    {
        if (IsDisposed)
        {
            return;
        }

        SyncPlcAlarmNotification();

        ApplyPlcStatus(_plcCommunicationService.GetCurrent(CurrentStationNo));
        ApplyProductionStatus(_plcProductionMonitorService.GetCurrent(CurrentStationNo));
    }

    /// <summary>
    /// 处理TagPLC鼠标进入事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void TagPLC_MouseEnter(object? sender, EventArgs e)
    {
        _plcStatusToolTipVisible = true;
        _lastPlcStatusToolTipRefreshTime = DateTime.MinValue;
        RefreshPlcStatusToolTip();
        _plcStatusToolTipTimer.Start();
    }

    /// <summary>
    /// 处理TagPLC鼠标离开事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
    private void TagPLC_MouseLeave(object? sender, EventArgs e)
    {
        ClosePlcStatusToolTip();
    }

    /// <summary>
    /// 处理PLC 状态ToolTipTimer定时触发事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">事件参数。</param>
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
    /// 刷新PLC 状态ToolTip。
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

    /// <summary>
    /// 判断鼠标OverTagPlc。
    /// </summary>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private bool IsMouseOverTagPlc()
    {
        if (IsDisposed || !tagPLC.IsHandleCreated)
        {
            return false;
        }

        var bounds = tagPLC.RectangleToScreen(tagPLC.ClientRectangle);
        return bounds.Contains(Cursor.Position);
    }

    /// <summary>
    /// 规范化状态工位号。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析或计算后的数值。</returns>
    private static int NormalizeStatusStationNo(int stationNo)
    {
        return stationNo == 2 ? 2 : ProductionConstants.Stations.DefaultStationNo;
    }

    /// <summary>
    /// 关闭PLC 状态ToolTip。
    /// </summary>
    private void ClosePlcStatusToolTip()
    {
        _plcStatusToolTipVisible = false;
        _plcStatusToolTipTimer.Stop();
        HidePlcStatusToolTipPopup();
    }

    /// <summary>
    /// 确保PLC 状态ToolTip弹窗。
    /// </summary>
    private void EnsurePlcStatusToolTipPopup()
    {
        if (_plcStatusToolTipPanel is not null)
        {
            return;
        }

        var backgroundColor = UiColors.Table.HeaderBackColor;
        _plcStatusToolTipFont = new Font(
            tagPLC.Font.FontFamily,
            PlcStatusToolTipFontSize,
            FontStyle.Regular,
            GraphicsUnit.Point);
        _plcStatusToolTipLabel = new Label
        {
            AutoSize = false,
            BackColor = backgroundColor,
            ForeColor = UiColors.Table.TextColor,
            Font = _plcStatusToolTipFont,
            MaximumSize = new Size(ScalePlcStatusToolTipMetric(PlcStatusToolTipMaxWidth), 0),
            Padding = Padding.Empty,
            TextAlign = ContentAlignment.TopLeft
        };

        _plcStatusToolTipPanel = new AntdUI.Panel
        {
            AutoSize = false,
            Back = backgroundColor,
            BackColor = backgroundColor,
            BorderColor = UiColors.Table.GridLineColor,
            BorderWidth = 1F,
            Radius = PlcStatusToolTipRadius,
            Shadow = PlcStatusToolTipShadow,
            ShadowColor = Color.FromArgb(15, 23, 42),
            ShadowOpacity = 0.18F,
            ShadowOpacityAnimation = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Visible = false
        };
        _plcStatusToolTipPanel.Controls.Add(_plcStatusToolTipLabel);
        Controls.Add(_plcStatusToolTipPanel);
    }

    /// <summary>
    /// 更新PLC 状态ToolTip文本。
    /// </summary>
    /// <param name="text">显示文本。</param>
    private void UpdatePlcStatusToolTipText(string text)
    {
        EnsurePlcStatusToolTipPopup();

        if (_plcStatusToolTipLabel is null || _plcStatusToolTipPanel is null)
        {
            return;
        }

        var currentDpi = DeviceDpi <= 0 ? 96 : DeviceDpi;
        var currentClientWidth = ClientSize.Width;
        if (string.Equals(_lastPlcStatusToolTipText, text, StringComparison.Ordinal)
            && _lastPlcStatusToolTipClientWidth == currentClientWidth
            && _lastPlcStatusToolTipDpi == currentDpi)
        {
            return;
        }

        _lastPlcStatusToolTipText = text;
        _lastPlcStatusToolTipClientWidth = currentClientWidth;
        _lastPlcStatusToolTipDpi = currentDpi;
        _plcStatusToolTipLabel.Text = text;

        // Reuse the card unless its text, available width, or DPI requires a new measurement.
        var cardInset = ScalePlcStatusToolTipMetric(PlcStatusToolTipPadding + PlcStatusToolTipShadow);
        var availableWidth = Math.Max(1, ClientSize.Width - cardInset * 2);
        var maxCardWidth = Math.Min(
            ScalePlcStatusToolTipMetric(PlcStatusToolTipMaxWidth),
            availableWidth);
        var maxLabelWidth = Math.Max(1, maxCardWidth - cardInset * 2);
        _plcStatusToolTipLabel.MaximumSize = new Size(maxLabelWidth, 0);
        var preferredSize = _plcStatusToolTipLabel.GetPreferredSize(new Size(maxLabelWidth, 0));
        _plcStatusToolTipLabel.Location = new Point(cardInset, cardInset);
        _plcStatusToolTipLabel.Size = preferredSize;
        _plcStatusToolTipPanel.Size = new Size(
            preferredSize.Width + cardInset * 2,
            preferredSize.Height + cardInset * 2);
    }

    /// <summary>
    /// 显示PLC 状态ToolTip弹窗。
    /// </summary>
    private void ShowPlcStatusToolTipPopup()
    {
        EnsurePlcStatusToolTipPopup();

        if (_plcStatusToolTipPanel is null)
        {
            return;
        }

        var gap = ScalePlcStatusToolTipMetric(PlcStatusToolTipGap);
        var popupSize = _plcStatusToolTipPanel.Size;
        var workingArea = Screen.FromControl(tagPLC).WorkingArea;
        var tagTopLeft = tagPLC.PointToScreen(Point.Empty);
        var anchor = tagPLC.PointToScreen(new Point(0, tagPLC.Height + gap));
        var x = Math.Clamp(
            anchor.X,
            workingArea.Left,
            Math.Max(workingArea.Left, workingArea.Right - popupSize.Width));
        var y = anchor.Y + popupSize.Height <= workingArea.Bottom
            ? anchor.Y
            : Math.Max(workingArea.Top, tagTopLeft.Y - popupSize.Height - gap);
        var clientLocation = PointToClient(new Point(x, y));
        clientLocation.X = Math.Clamp(
            clientLocation.X,
            0,
            Math.Max(0, ClientSize.Width - popupSize.Width));
        clientLocation.Y = Math.Clamp(
            clientLocation.Y,
            0,
            Math.Max(0, ClientSize.Height - popupSize.Height));
        _plcStatusToolTipPanel.Location = clientLocation;
        _plcStatusToolTipPanel.Visible = true;
        _plcStatusToolTipPanel.BringToFront();
    }

    /// <summary>
    /// 将悬浮面板逻辑尺寸换算为当前 DPI 下的控件尺寸。
    /// </summary>
    /// <param name="logicalValue">96 DPI 下的逻辑尺寸。</param>
    /// <returns>当前 DPI 下的尺寸。</returns>
    private int ScalePlcStatusToolTipMetric(int logicalValue)
    {
        var dpi = DeviceDpi <= 0 ? 96 : DeviceDpi;
        return Math.Max(1, (int)Math.Round(logicalValue * dpi / 96F));
    }

    /// <summary>
    /// 隐藏PLC 状态ToolTip弹窗。
    /// </summary>
    private void HidePlcStatusToolTipPopup()
    {
        if (_plcStatusToolTipPanel is not null)
        {
            _plcStatusToolTipPanel.Visible = false;
        }
    }

    /// <summary>
    /// 释放PLC 状态ToolTip弹窗。
    /// </summary>
    private void DisposePlcStatusToolTipPopup()
    {
        _plcStatusToolTipPanel?.Dispose();
        _plcStatusToolTipFont?.Dispose();
        _plcStatusToolTipPanel = null;
        _plcStatusToolTipLabel = null;
        _plcStatusToolTipFont = null;
        _lastPlcStatusToolTipText = string.Empty;
        _lastPlcStatusToolTipClientWidth = -1;
        _lastPlcStatusToolTipDpi = -1;
    }

    /// <summary>
    /// 构建PLC 状态ToolTip文本。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
    /// <returns>处理后的文本。</returns>
    private string BuildPlcStatusToolTipText(PlcConnectionSnapshot snapshot)
    {
        var history = _plcStatusHistory
            .Where(entry => entry.StationNo == NormalizeStatusStationNo(snapshot.StationNo))
            .Take(PlcStatusHistoryLimit)
            .ToList();
        var builder = new StringBuilder();
        builder.AppendLine(_localizer.GetString(TextKeys.Monitor.PlcToolTip.Title));
        builder.AppendLine(_localizer.GetString(
            TextKeys.Monitor.PlcToolTip.Station,
            NormalizeStatusStationNo(snapshot.StationNo)));
        builder.AppendLine(_localizer.GetString(
            TextKeys.Monitor.PlcToolTip.CurrentState,
            GetLocalizedPlcStateText(snapshot.State)));
        builder.AppendLine(_localizer.GetString(
            TextKeys.Monitor.PlcToolTip.Connected,
            FormatYesNo(snapshot.IsConnected)));
        builder.AppendLine(_localizer.GetString(
            TextKeys.Monitor.PlcToolTip.Endpoint,
            FormatToolTipValue(snapshot.Endpoint)));
        builder.AppendLine(_localizer.GetString(
            TextKeys.Monitor.PlcToolTip.LastConnected,
            FormatOptionalTime(snapshot.LastConnectedTime)));
        builder.AppendLine(_localizer.GetString(
            TextKeys.Monitor.PlcToolTip.LastHeartbeat,
            FormatOptionalTime(snapshot.LastHeartbeatTime)));
        builder.AppendLine(_localizer.GetString(
            TextKeys.Monitor.PlcToolTip.CurrentMessage,
            FormatToolTipValue(snapshot.Message)));
        builder.AppendLine();
        builder.AppendLine(_localizer.GetString(TextKeys.Monitor.PlcToolTip.RecentHistory));

        if (history.Count == 0)
        {
            builder.AppendLine(_localizer.GetString(TextKeys.Monitor.PlcToolTip.NoHistory));
            return builder.ToString();
        }

        foreach (var entry in history)
        {
            builder.AppendLine(FormatCompactPlcStatusHistoryEntry(entry));
        }

        return builder.ToString();
    }

    /// <summary>
    /// 格式化紧凑的 PLC 状态历史项，保留原始诊断消息但限制其长度。
    /// </summary>
    /// <param name="entry">状态历史项。</param>
    /// <returns>本地化后的单行历史文本。</returns>
    private string FormatCompactPlcStatusHistoryEntry(PlcStatusHistoryEntry entry)
    {
        var message = NormalizeRuntimeSummary(entry.Message);
        return _localizer.GetString(
            TextKeys.Monitor.PlcToolTip.HistoryEntry,
            entry.ChangedTime.ToString("HH:mm:ss", CultureInfo.CurrentCulture),
            GetLocalizedPlcStateText(entry.State),
            string.IsNullOrWhiteSpace(message) ? "--" : message);
    }

    /// <summary>
    /// 记录PLC 状态变更。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
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

    /// <summary>
    /// 获取LocalizedPLC 状态文本。
    /// </summary>
    /// <param name="state">工位运行状态。</param>
    /// <returns>处理后的文本。</returns>
    private string GetLocalizedPlcStateText(PlcConnectionState state)
    {
        return _localizer.GetString(GetPlcStateKey(state));
    }

    /// <summary>
    /// 格式化Yes号。
    /// </summary>
    /// <param name="value">待处理值。</param>
    /// <returns>处理后的文本。</returns>
    private string FormatYesNo(bool value)
    {
        return _localizer.GetString(value
            ? TextKeys.Monitor.PlcToolTip.Yes
            : TextKeys.Monitor.PlcToolTip.No);
    }

    /// <summary>
    /// 格式化可选时间。
    /// </summary>
    /// <param name="value">待处理值。</param>
    /// <returns>处理后的文本。</returns>
    private static string FormatOptionalTime(DateTime? value)
    {
        return value.HasValue ? FormatTime(value.Value) : "--";
    }

    /// <summary>
    /// 格式化时间。
    /// </summary>
    /// <param name="value">待处理值。</param>
    /// <returns>处理后的文本。</returns>
    private static string FormatTime(DateTime value)
    {
        return value.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// 格式化ToolTip值。
    /// </summary>
    /// <param name="value">待处理值。</param>
    /// <returns>处理后的文本。</returns>
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

    #endregion

    #region 实时预览调度

    /// <summary>
    /// 应用待处理实时预览快照。
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

    #endregion

    #region 完工上报流程

    /// <summary>
    /// 异步完工Local工单。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>表示异步操作的任务。</returns>
    private async Task FinishLocalWorkOrderAsync(int stationNo)
    {
        if (!TryResolveFinishQuantities(stationNo, out var actualQty, out var qualifiedQty, out var failedQty))
        {
            return;
        }

        await RunReportOperationAsync(stationNo, "本地完工", async () =>
        {
            ClearRuntimeError();
            var activeTask = _weldTaskService.RestoreUnfinishedTask(stationNo);
            await RefreshRecipeCodeFromPlcBeforeFinishAsync(activeTask, stationNo);
            // 完工不再回填登录账号，直接沿用离线开工时操作员录入并写入任务的员工号。
            await _weldTaskService.FinishLocalAsync(
                activeTask?.UserNumber?.Trim() ?? string.Empty,
                actualQty,
                qualifiedQty,
                failedQty,
                stationNo);
            ClearFinishedProductIdentity(stationNo);
            await WriteFinishBusinessSignalsAsync(stationNo);
            RefreshProductionRuntimeState();
            SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.LocalFinishSucceeded);
        });
    }

    /// <summary>
    /// Reads the final PLC recipe code before finish and updates the local task when a valid value is available.
    /// </summary>
    /// <param name="activeTask">Current unfinished task.</param>
    /// <param name="stationNo">Station number used for PLC addressing.</param>
    /// <returns>Asynchronous operation.</returns>
    private async Task RefreshRecipeCodeFromPlcBeforeFinishAsync(BizWeldTask? activeTask, int stationNo)
    {
        if (activeTask is null || activeTask.Id <= 0)
        {
            return;
        }

        PlcBusinessSignalResult readResult;
        try
        {
            readResult = await _plcBusinessSignalService.ReadTextAsync(
                AppConstants.PlcLogicalKeys.PlcRecipeCode,
                stationNo);
        }
        catch (Exception ex)
        {
            WriteFinishRecipeReadFailureLog(stationNo, activeTask, ex.Message);
            return;
        }

        if (!readResult.IsSuccess)
        {
            WriteFinishRecipeReadFailureLog(stationNo, activeTask, readResult.Message);
            return;
        }

        var plcRecipeCode = NormalizeRecipeCode(readResult.Value);
        if (string.IsNullOrWhiteSpace(plcRecipeCode))
        {
            WriteFinishRecipeReadFailureLog(stationNo, activeTask, "PLC recipe code is empty.");
            return;
        }

        if (!SharesRecipeTaskAcrossStations())
        {
            if (!_weldTaskService.TryUpdateRecipeCode(activeTask.Id, plcRecipeCode, stationNo))
            {
                WriteFinishRecipeReadFailureLog(stationNo, activeTask, "Local task recipe update failed.");
                return;
            }

            activeTask.RecipeCode = plcRecipeCode;
        }
}

    /// <summary>
    /// Writes a throttled log entry when the finish recipe read cannot produce a usable value.
    /// </summary>
    /// <param name="stationNo">Station number.</param>
    /// <param name="task">Current task.</param>
    /// <param name="detail">Failure detail.</param>
    private void WriteFinishRecipeReadFailureLog(int stationNo, BizWeldTask task, string? detail)
    {
        var normalizedStationNo = NormalizeStationNo(stationNo);
        var now = DateTime.Now;
        if (_finishRecipeReadFailureLogTimes.TryGetValue(normalizedStationNo, out var lastWriteTime)
            && now - lastWriteTime < FinishRecipeReadFailureLogInterval)
        {
            return;
        }

        _finishRecipeReadFailureLogTimes[normalizedStationNo] = now;
        _exceptionLogService.WriteBusiness(
            "PLC.RecipeCode.FinishRead",
            "Finish recipe code read failed.",
            $"Station={normalizedStationNo}; TaskId={task.Id}; WorkOrder={task.SN}; Detail={detail}",
            "Finish reads PLC recipe code before closing the task.");
    }

    /// <summary>
    /// 尝试解析完工上报需要的产量、合格数和不合格数。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <param name="actualQty">输出实际生产数量。</param>
    /// <param name="qualifiedQty">输出合格数量。</param>
    /// <param name="failedQty">输出不合格数量。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private bool TryResolveFinishQuantities(int stationNo, out int actualQty, out int qualifiedQty, out int failedQty)
    {
        var settings = _currentSettings;
        var production = GetCurrentProductionSnapshot();
        if (TryResolveFinishQuantitiesFromPlc(stationNo, production, out actualQty, out qualifiedQty, out failedQty))
        {
            return true;
        }

        // PLC 数量读取失败时，只有启用系统设置才允许人工补录；默认仍保持弹窗关闭。
        return settings.EnableFinishExpQtyPrompt
            && TryResolveFinishQuantitiesWithPrompt(production, out actualQty, out qualifiedQty, out failedQty);
    }

    /// <summary>
    /// 尝试直接从 PLC 生产快照读取完工数量。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <param name="production">PLC 生产数据快照。</param>
    /// <param name="actualQty">输出实际生产数量。</param>
    /// <param name="qualifiedQty">输出合格数量。</param>
    /// <param name="failedQty">输出不合格数量。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private bool TryResolveFinishQuantitiesFromPlc(int stationNo, PlcProductionSnapshot production,
        out int actualQty, out int qualifiedQty, out int failedQty)
    {
        actualQty = production.TotalProduction;
        qualifiedQty = production.AcceptedQuantity;
        failedQty = production.RejectedQuantity;

        if (production.IsSuccess && production.ProductionQuantitiesReadSuccess)
        {
            return true;
        }

        var detail = BuildFinishQuantityReadFailureText(production);
        _exceptionLogService.WriteBusiness(
            "PLC.FinishQuantity",
            _localizer.GetString(TextKeys.Monitor.RuntimeError.FinishQuantityReadFailed),
            detail,
            $"Station={stationNo}");
        SetRuntimeError(TextKeys.Monitor.RuntimeError.FinishQuantityReadFailed);
        return false;
    }

    /// <summary>
    /// 在 PLC 数量不完整时通过人工输入补齐完工数量。
    /// </summary>
    /// <param name="production">PLC 生产数据快照。</param>
    /// <param name="actualQty">输出实际生产数量。</param>
    /// <param name="qualifiedQty">输出合格数量。</param>
    /// <param name="failedQty">输出不合格数量。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private bool TryResolveFinishQuantitiesWithPrompt(PlcProductionSnapshot production,
        out int actualQty, out int qualifiedQty, out int failedQty)
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

    /// <summary>
    /// 构建完工数量读取失败文本。
    /// </summary>
    /// <param name="production">PLC 生产数据快照。</param>
    /// <returns>处理后的文本。</returns>
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

    #endregion

    #region 工单准备流程

    /// <summary>
    /// 按指定工单号加载 MES 工单信息，并绑定默认工序。
    /// </summary>
    /// <param name="workId">工单号。</param>
    /// <param name="stationNo">工位号。</param>
    /// <param name="showDialogOnFailure">失败时是否弹窗提示；自动扫码查询使用 false。</param>
    /// <returns>加载成功返回 true；否则返回 false。</returns>
    private async Task<bool> LoadWorkOrderInfoAsync(
        string workId,
        int stationNo,
        bool showDialogOnFailure,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var isReady = false;
        await RunUiOperationAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.LoadingWorkOrder);
            var workOrder = await _weldTaskService.GetWorkOrderInfoAsync(workId, stationNo, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (workOrder is null)
            {
                HandleWorkOrderLoadFailure(workId, showDialogOnFailure);
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

            _weldTaskService.SelectProcess(defaultProcess, stationNo);
            if (stationNo == CurrentStationNo)
            {
                ClearPendingOnlineProgramSelection();
                ApplySelectedProcessInputs(defaultProcess);
                _manualWorkOrderEditedByUser = false;
            }

            await LoadProgramListForWorkOrderAsync(stationNo, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            RefreshProductionRuntimeState();
            ClearMesOperatorInfo();
            SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.WorkOrderLoaded);
            isReady = true;
        });

        return isReady;
    }
    /// <summary>
    /// 统一处理工单加载失败提示，避免自动扫码查询频繁弹窗。
    /// </summary>
    /// <param name="workId">工单号。</param>
    /// <param name="showDialogOnFailure">是否弹窗提示。</param>
    private void HandleWorkOrderLoadFailure(string workId, bool showDialogOnFailure)
    {
        var detail = _weldTaskService.CurrentState.LastServerSyncMessage ?? string.Empty;
        if (showDialogOnFailure)
        {
            ShowBusinessWarning(
                "MES.GetWorkOrderInfo",
                TextKeys.Monitor.Message.WorkOrderLoadFailed,
                detail,
                $"WorkId={workId}");
            return;
        }

        var message = _localizer.GetString(TextKeys.Monitor.Message.WorkOrderLoadFailed);
        _exceptionLogService.WriteBusiness("MES.GetWorkOrderInfo", message, detail, $"WorkId={workId}");
        SetRuntimeError(TextKeys.Monitor.Message.WorkOrderLoadFailed);
    }

    /// <summary>
    /// 异步加载并确认当前工单的加工程序。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>异步操作成功返回 true，否则返回 false。</returns>
    private async Task LoadProgramListForWorkOrderAsync(int stationNo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await RunUiOperationAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var state = GetCurrentStationState();
            var workOrder = state.CurrentWorkOrder;
            if (workOrder is null)
            {
                return;
            }

            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.LoadingPrograms);
            var programs = await _weldTaskService.LoadProgramsAsync(stationNo, cancellationToken);
            await RefreshLocalProgramSnapshotAsync(rebindOptions: false, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (programs.Count == 0)
            {
                // 列表为空不弹窗，按自动查询失败模式记录业务日志并在提示区报错。
                var detail = "MES 返回的程序列表为空，或按产品工号筛选后无匹配程序。";
                _exceptionLogService.WriteBusiness(
                    "MES.GetProgramList",
                    _localizer.GetString(TextKeys.Monitor.Message.ProgramListEmpty),
                    detail,
                    $"WorkId={workOrder.SN}; ProductNumber={workOrder.ProdNum}");
                SetRuntimeError(TextKeys.Monitor.Message.ProgramListEmpty);
                BindOnlineProgramNameOptions();
                return;
            }

            BindOnlineProgramNameOptions();
        });
    }

    private void ProgramManageService_ProgramLookupsChanged(object? sender, EventArgs e)
    {
        _ = RefreshLocalProgramSnapshotAsync(rebindOptions: true);
    }

    private async Task RefreshLocalProgramSnapshotAsync(
        bool rebindOptions,
        CancellationToken cancellationToken = default)
    {
        var refreshVersion = Interlocked.Increment(ref _programSnapshotRefreshVersion);
        try
        {
            var lookups = await _programManageService.GetProgramLookupsAsync(cancellationToken);
            if (refreshVersion != Volatile.Read(ref _programSnapshotRefreshVersion)
                || IsDisposed
                || !IsHandleCreated)
            {
                return;
            }

            var programs = lookups.Select(lookup => lookup.ToEntityStub()).ToArray();
            await RunOnUiThreadAsync(() =>
            {
                _localProgramSnapshot = programs;
                if (rebindOptions)
                {
                    if (IsOfflineInputEditable(GetCurrentStationState()))
                    {
                        BindOfflineProductNumOptions();
                        BindOfflineProgramNameOptions();
                    }
                    else if (GetCurrentStationState().AvailablePrograms.Count > 0)
                    {
                        BindOnlineProgramNameOptions();
                    }
                }

                return Task.CompletedTask;
            }, "MonitorView.ProgramLookupSnapshot");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.ProgramLookupSnapshot");
        }
    }

    /// <summary>
    /// 用当前工位已加载的程序列表填充在线程序名称下拉框（镜像离线版绑定，但不自动选中）。
    /// </summary>
    private void BindOnlineProgramNameOptions()
    {
        var state = GetCurrentStationState();
        var localPrograms = _localProgramSnapshot;
        var requireBothStations = _currentSettings.EnableDualStation && !_currentSettings.EnableDualWorkOrder;
        var programs = state.AvailablePrograms
            .Where(program =>
            {
                var localProgram = ResolveLocalProgramByProgramId(program.Id)
                    ?? localPrograms.FirstOrDefault(item =>
                        SameText(item.ProgramName, program.ProgramName)
                        && SameText(item.ProductNum, program.ProductNum));
                return !string.IsNullOrWhiteSpace(ProgramRecipeMappingRules.Resolve(localProgram, CurrentStationNo))
                    && (!requireBothStations
                        || (!string.IsNullOrWhiteSpace(ProgramRecipeMappingRules.Resolve(localProgram, 1))
                            && !string.IsNullOrWhiteSpace(ProgramRecipeMappingRules.Resolve(localProgram, 2))));
            })
            .ToList();
        var programNames = programs
            .Select(program => program.ProgramName?.Trim() ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _syncingOnlineProgramSelection = true;
        try
        {
            selectProgramName.Items.Clear();
            selectProgramName.Items.AddRange(programNames.Cast<object>().ToArray());
            var currentProgramName = state.SelectedProgram?.ProgramName?.Trim();
            if (string.IsNullOrWhiteSpace(currentProgramName) && HasPendingOnlineProgramSelection(state))
            {
                currentProgramName = _pendingOnlineProgramName;
            }

            var selectedIndex = string.IsNullOrWhiteSpace(currentProgramName)
                ? -1
                : programNames.FindIndex(name => string.Equals(name, currentProgramName, StringComparison.OrdinalIgnoreCase));
            if (selectedIndex < 0 && programNames.Count > 0 && HasPendingOnlineProgramSelection(state))
            {
                ClearPendingOnlineProgramSelection();
                currentProgramName = state.SelectedProgram?.ProgramName?.Trim();
                selectedIndex = string.IsNullOrWhiteSpace(currentProgramName)
                    ? -1
                    : programNames.FindIndex(name => string.Equals(name, currentProgramName, StringComparison.OrdinalIgnoreCase));
            }

            ForceProgramNameSelection(
                selectedIndex,
                selectedIndex >= 0 ? programNames[selectedIndex] : currentProgramName ?? string.Empty);
        }
        finally
        {
            _syncingOnlineProgramSelection = false;
        }
    }

    /// <summary>
    /// 用当前在线 MES 程序列表填充配方号下拉框。
    /// MES 程序列表项不包含配方号，配方号从本地同步程序维护表中解析。
    /// </summary>
    /// <param name="programs">当前工单可选的 MES 程序列表。</param>
    /// <param name="currentRecipeCode">需要保持显示的当前配方号。</param>
    #endregion

    #region 工位运行状态绑定

    /// <summary>
    /// 从界面切换当前展示的工位。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    private void SwitchStationFromUi(int stationNo)
    {
        var normalizedStationNo = Math.Clamp(stationNo, 1, 2);
        if (normalizedStationNo != CurrentStationNo)
        {
            ClearOfflineProgramSelectionByUser(CurrentStationNo);
        _offlineInputModeActive = false;
            ClearOfflineProgramSelectionByUser(normalizedStationNo);
            _viewStationNo = normalizedStationNo;
            ClearPendingOnlineProgramSelection();
            _offlineWorkOrderEditedByUser = false;
            _manualWorkOrderEditedByUser = false;
            _validatedOperatorNumber = null;
                _weldTaskService.RestoreUnfinishedTask(normalizedStationNo);
        }

        RefreshProductionRuntimeState();
        RestoreCurrentRuntimeTipState();
        ApplyAllStationStatuses();
        QueueRefreshSchemePreview(force: true);
        ApplyCurrentRealtimePreviewSnapshot();
        SyncStationSelection();
    }

    /// <summary>
    /// 选择后续业务操作要作用的工位。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    private void SelectStationForOperation(int stationNo)
    {
        if (_weldTaskService.CurrentState.CurrentStationNo != stationNo)
        {
            _weldTaskService.SelectStation(stationNo);
        }
    }

    /// <summary>
    /// 绑定生产运行状态。
    /// </summary>
    private void BindProductionRuntimeState()
    {
        var state = GetCurrentStationState();
        var workOrder = state.CurrentWorkOrder;
        var process = state.SelectedProcess;
        var program = state.SelectedProgram;
        var activeTask = state.ActiveTask;
        var liveWorkId = GetCurrentLiveWorkId();
        var hasRunningTask = IsRunningWeldTask(activeTask);
        var hasPreparedWorkOrder = HasPreparedWorkOrderInfo(state, liveWorkId);
        var currentIdentity = hasPreparedWorkOrder ? ResolveDisplayProductIdentity(state) : null;

        if (!hasRunningTask)
        {
            ClearIdleProductionDataDisplay(clearWorkOrderInfo: !hasPreparedWorkOrder);
        }

        SyncStationSelection();
        if (IsOfflineInputEditable(state))
        {
            BindOfflineEditableRuntimeState(liveWorkId);
            // 离线员工号是操作员的待开工录入项，1Hz 重绑定必须保留正在输入的内容，否则边输入边被清空。
            BindOfflineOperatorInfo(activeTask);
            ApplyTaskStatusTag(state);
            btnLocalWorkOrder.Text = _localizer.GetString(TextKeys.Monitor.Button.LocalWorkOrder);
            ApplyReportButtonState();
            return;
        }

        ClearOfflineProgramSelectionByUser(CurrentStationNo);
        _offlineInputModeActive = false;
        var canEditOnlineWorkOrder = IsManualOnlineWorkOrderInputEditable(state);
        var onlineEditable = IsOnlineStartInputEditable(state);
        ApplyOfflineInputReadOnly(readOnly: true);
        inputSN.ReadOnly = !canEditOnlineWorkOrder;
        ApplyOnlineStartInputReadOnly(onlineEditable);
        _offlineWorkOrderEditedByUser = false;
        if (!canEditOnlineWorkOrder)
        {
            _manualWorkOrderEditedByUser = false;
            }

        var workOrderText = activeTask is not null
            ? activeTask.SN
            : !string.IsNullOrWhiteSpace(liveWorkId) ? liveWorkId : workOrder?.SN ?? string.Empty;
        if (!_manualWorkOrderEditedByUser || !canEditOnlineWorkOrder)
        {
            SetWorkOrderInputText(workOrderText);
        }

        // 在线可编辑字段仅在工作单变化（或首次绑定）时用工单值刷新，避免刷新周期覆盖操作员手改值。
        var workOrderKey = workOrder?.SN ?? string.Empty;
        var workOrderChanged = !string.Equals(workOrderKey, _lastBoundOnlineWorkOrderKey, StringComparison.Ordinal);
        if (workOrderChanged)
        {
            _lastBoundOnlineWorkOrderKey = string.IsNullOrEmpty(workOrderKey) ? null : workOrderKey;
        }

        // 产品工号与其他在线可编辑字段同规则：仅在工单变化、有运行任务或不可编辑时用工单值覆盖，
        // 否则保留操作员的改写值，开工按改写后的工号上报。
        if (!onlineEditable || activeTask is not null || workOrderChanged)
        {
            SetProductNumSelectionText(workOrder?.ProdNum ?? currentIdentity?.ProductNum ?? string.Empty);
            inputBatch.Text = workOrder?.Batch ?? string.Empty;
            inputProductName.Text = workOrder?.ProductName ?? string.Empty;
            inputDrawingNo.Text = workOrder?.DrawingNo ?? string.Empty;
            inputSpec.Text = workOrder?.Spec ?? string.Empty;
            inputProcessNo.Text = process?.ProcessNo ?? string.Empty;
            inputStartAmount.Text = process is null ? string.Empty : process.StartAmount.ToString(CultureInfo.InvariantCulture);
            inputProdModel.Text = workOrder?.ProdModel ?? string.Empty;
        }
        BindProcessSelection(workOrder, process, activeTask is not null);
        var usePendingProgram = program is null && activeTask is null && HasPendingOnlineProgramSelection(state);
        selectProgramName.Text = usePendingProgram
            ? _pendingOnlineProgramName ?? string.Empty
            : program?.ProgramName ?? string.Empty;
BindRuntimeOperatorInfo(state, activeTask, ShouldPreserveDraftOperatorNumber(state, activeTask, onlineEditable));
        ApplyTaskStatusTag(state);
        btnLocalWorkOrder.Text = activeTask is { IsOfflineCreated: true, EndTime: null }
            ? "本地完工"
            : _localizer.GetString(TextKeys.Monitor.Button.LocalWorkOrder);
        ApplyReportButtonState();
    }

    /// <summary>
    /// 判断未开工工位是否已有需要保留的待开工信息。
    /// </summary>
    private bool HasPreparedWorkOrderInfo(ProductionStationRuntimeState state, string liveWorkId)
    {
        return state.CurrentWorkOrder is not null
            || IsNewLiveWorkOrder(liveWorkId)
            || !string.IsNullOrWhiteSpace(GetConfirmedWorkOrderInput(CurrentStationNo))
            || _manualWorkOrderEditedByUser
            || _offlineWorkOrderEditedByUser
            // 离线模式的主界面字段本身就是待开工草稿，不能在刷新周期中清除。
            || IsOfflineInputEditable(state);
    }

    private bool IsNewLiveWorkOrder(string liveWorkId)
    {
        if (string.IsNullOrWhiteSpace(liveWorkId))
        {
            return false;
        }

        var stationNo = NormalizeStationNo(CurrentStationNo);
        return !_lastAutoQueriedWorkIds.TryGetValue(stationNo, out var previousWorkId)
            || !string.Equals(previousWorkId, liveWorkId.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 清空未开工工位的生产数据；待开工草稿存在时只清运行数据。
    /// </summary>
    private void ClearIdleProductionDataDisplay(bool clearWorkOrderInfo)
    {
        ClearCurrentRealtimePreviewDisplay();
        ClearCurrentProductHistoryDisplay();
        if (clearWorkOrderInfo)
        {
            ClearUnpreparedWorkOrderInfoDisplay();
        }
    }

    private void ClearUnpreparedWorkOrderInfoDisplay()
    {
        _syncingOfflineInputs = true;
        try
        {
            SetWorkOrderInputText(string.Empty);
            SetProductNumSelectionText(string.Empty);
            inputBatch.Text = string.Empty;
            inputProductName.Text = string.Empty;
            inputDrawingNo.Text = string.Empty;
            inputSpec.Text = string.Empty;
            inputProcessNo.Text = string.Empty;
            inputStartAmount.Text = string.Empty;
            inputProdModel.Text = string.Empty;
            selectItemName.Text = string.Empty;
            ForceProgramNameSelection(-1, string.Empty);
            BindProcessSelection(null, null, bindSelectedOnly: false);
            ClearMesOperatorInfo();
            _lastBoundOnlineWorkOrderKey = null;
        }
        finally
        {
            _syncingOfflineInputs = false;
        }
    }

    /// <summary>
    /// 判断当前工位是否允许在主界面直接编辑离线开工信息。
    /// </summary>
    /// <param name="state">当前工位运行态。</param>
    /// <returns>允许离线编辑返回 true；否则返回 false。</returns>
    private bool IsOfflineInputEditable(ProductionStationRuntimeState state)
    {
        return !_stationViewReadOnly
            && !_mesConnectionMonitorService.Current.IsConnected
            && !IsRunningWeldTask(state.ActiveTask);
    }

    /// <summary>
    /// 判断在线空闲时是否允许直接在工单号输入框录入并自动查询 MES 工单。
    /// </summary>
    /// <param name="state">当前工位运行态。</param>
    /// <returns>允许在线手输返回 true；否则返回 false。</returns>
    private bool IsManualOnlineWorkOrderInputEditable(ProductionStationRuntimeState state)
    {
        return !_stationViewReadOnly
            && _mesConnectionMonitorService.Current.IsConnected
            && !IsRunningWeldTask(state.ActiveTask)
            && _weldTaskService.GetUnfinishedTask(CurrentStationNo) is null;
    }

    /// <summary>
    /// 判断在线空闲且工单已加载时，是否允许在主界面直接编辑本次开工的中段字段并选定程序。
    /// </summary>
    private bool IsOnlineStartInputEditable(ProductionStationRuntimeState state)
    {
        return IsManualOnlineWorkOrderInputEditable(state)
            && state.CurrentWorkOrder is not null;
    }

    /// <summary>
    /// 解析当前是否允许用缓存的产品身份回填产品工号/型号控件。
    /// 在线完工后运行态已经清空，此时不能再用上一帧预览缓存把控件刷回旧产品。
    /// </summary>
    /// <param name="state">当前工位运行态。</param>
    /// <returns>可用于显示的产品身份；不允许显示时返回 null。</returns>
    private ProductIdentity? ResolveDisplayProductIdentity(ProductionStationRuntimeState state)
    {
        var currentIdentity = _currentProductIdentity;
        if (currentIdentity is null
            || currentIdentity.StationNo != CurrentStationNo
            || string.IsNullOrWhiteSpace(currentIdentity.ProductNum))
        {
            return null;
        }

        if (state.ActiveTask is not null || state.CurrentWorkOrder is not null)
        {
            return currentIdentity;
        }

        return IsOfflineInputEditable(state)
            ? currentIdentity
            : null;
    }

    /// <summary>
    /// 完工后清除当前工位的产品身份缓存，避免运行态刷新继续显示已完工产品。
    /// </summary>
    /// <param name="stationNo">已完工工位。</param>
    private void ClearFinishedProductIdentity(int stationNo)
    {
        if (_currentProductIdentity?.StationNo == stationNo)
        {
            _currentProductIdentity = null;
        }

        ClearConfirmedWorkOrderInput(stationNo);
        if (NormalizeStationNo(stationNo) == CurrentStationNo)
        {
            _manualWorkOrderEditedByUser = false;
            _offlineWorkOrderEditedByUser = false;
            _offlineInputModeActive = false;
        }

        _lastSchemePreviewKey = string.Empty;
    }

    /// <summary>
    /// 设置在线开工输入控件的只读状态。
    /// 产品工号和产品型号由 MES 工单回填后仍允许操作员改写，开工按改写值上报；员工号依“操作员弹窗输入”设置。
    /// </summary>
    private void ApplyOnlineStartInputReadOnly(bool editable)
    {
        var fieldReadOnly = !editable;
        inputBatch.ReadOnly = fieldReadOnly;
        inputSpec.ReadOnly = fieldReadOnly;
        inputProductName.ReadOnly = fieldReadOnly;
        inputDrawingNo.ReadOnly = fieldReadOnly;
        inputProcessNo.ReadOnly = fieldReadOnly;
        inputStartAmount.ReadOnly = fieldReadOnly;
        selectProgramName.ReadOnly = fieldReadOnly;
        selectItemName.ReadOnly = fieldReadOnly;

        selectProdNum.ReadOnly = fieldReadOnly;
        inputProdModel.ReadOnly = fieldReadOnly;

        var useOperatorDialog = _currentSettings.UseOperatorInputDialog ?? true;
        MesUserNumber.ReadOnly = fieldReadOnly || useOperatorDialog;
    }

    /// <summary>
    /// 绑定离线空闲状态下的可编辑开工信息。
    /// </summary>
    /// <param name="liveWorkId">PLC 当前扫码工单号。</param>
    private void BindOfflineEditableRuntimeState(string liveWorkId)
    {
        _syncingOfflineInputs = true;
        try
        {
            var enteringOfflineInputMode = !_offlineInputModeActive;
            _offlineInputModeActive = true;
            ApplyOfflineInputReadOnly(readOnly: false);
            if (enteringOfflineInputMode)
            {
                // 刚转入离线：工号和与之关联的程序名称都是操作员待录入项，
                // 先清掉上一在线工单残留值，两者一起保持为空。
                RememberProductNumInput(null);
                SetProductNumSelectionText(string.Empty);
                ClearOfflineProgramNameSelection();
            }

            BindOfflineProductNumOptions();
            BindOfflineProgramNameOptions();

            if (!_offlineWorkOrderEditedByUser && !string.IsNullOrWhiteSpace(liveWorkId))
            {
                // 流转卡号只接受 PLC 扫码值或操作员录入，不再为空值生成 LOCAL 占位编号：
                // 占位编号会被当成真实工单写入任务和上报数据，且掩盖“未扫码”这一状态。
                inputSN.Text = liveWorkId;
            }

            if (enteringOfflineInputMode)
            {
                inputProdModel.Text = string.Empty;
                // 工序号、工序名称和工单数量同样只接受操作员录入，不预填 OP10/离线焊接/1：
                // 预填值会被当成真实工序和计划数量写入任务、报表和 MES 开工上报，且掩盖“未录入”这一状态。
                // 工序号在开工时校验非空并提示；工序名称和工单数量允许留空。
                inputProcessNo.Text = string.Empty;
                selectItemName.Text = string.Empty;
                inputStartAmount.Text = string.Empty;
                // 员工号同样是操作员待录入项：清掉上一在线工单校验得到的员工信息和校验标记，
                // 避免离线开工把在线阶段校验过的他人工号当成本次操作员上报。
                ClearMesOperatorInfo();
            }

            ApplyOfflineProgramNameOption(GetSelectedOfflineProgramNameOption(), syncProgramFields: false);
        }
        finally
        {
            _syncingOfflineInputs = false;
        }
    }

    /// <summary>
    /// 设置离线开工输入控件的只读状态。
    /// </summary>
    /// <param name="readOnly">是否只读。</param>
    private void ApplyOfflineInputReadOnly(bool readOnly)
    {
        inputSN.ReadOnly = readOnly;
        inputBatch.ReadOnly = readOnly;
        inputSpec.ReadOnly = readOnly;
        inputProductName.ReadOnly = readOnly;
        inputDrawingNo.ReadOnly = readOnly;
        inputProcessNo.ReadOnly = readOnly;
        inputStartAmount.ReadOnly = readOnly;
        selectProgramName.ReadOnly = readOnly;
        selectItemName.ReadOnly = readOnly;
        selectProdNum.ReadOnly = readOnly;
        inputProdModel.ReadOnly = readOnly;
        // 离线无法向 MES 校验身份，员工号只能由现场操作员录入，因此不受“操作员弹窗输入”设置影响，始终随离线可编辑态开放。
        MesUserNumber.ReadOnly = readOnly;
    }

    /// <summary>
    /// 由程序同步工单号输入框文本，避免触发手动输入自动查询。
    /// </summary>
    /// <param name="workId">需要显示的工单号。</param>
    private void SetWorkOrderInputText(string workId)
    {
        if (string.Equals(inputSN.Text, workId, StringComparison.Ordinal))
        {
            return;
        }

        _syncingWorkOrderInput = true;
        try
        {
            inputSN.Text = workId;
        }
        finally
        {
            _syncingWorkOrderInput = false;
        }
    }

    /// <summary>
    /// 从本地程序库刷新“程序名称”下拉选项。
    /// </summary>
    private void BindOfflineProgramNameOptions()
    {
        var previousProgramId = GetSelectedOfflineProgramNameOption()?.Program.Id;
        var previousText = selectProgramName.Text?.Trim() ?? string.Empty;
        var requireBothStations = _currentSettings.EnableDualStation && !_currentSettings.EnableDualWorkOrder;
        // 与在线 ProgramListFilterRules 同语义：未启用“按产品工号筛选程序”时列出全部程序，
        // 以支持一款产品借用另一款工号的程序生产。
        var productNumFilter = _currentSettings.UseProductNumberFilter
            ? ResolveOfflineProductNumFilter()
            : null;
        var options = OfflineStartInputRules.BuildProgramNameOptions(
            _localProgramSnapshot,
            CurrentStationNo,
            requireBothStations,
            productNumFilter).ToList();

        _syncingOfflineProgramSelection = true;
        try
        {
            _offlineProgramNameOptions.Clear();
            _offlineProgramNameOptions.AddRange(options);
            selectProgramName.Items.Clear();
            selectProgramName.Items.AddRange(options.Select(option => option.DisplayText).Cast<object>().ToArray());
            var selectedIndex = previousProgramId.HasValue
                ? options.FindIndex(option => option.Program.Id == previousProgramId.Value)
                : -1;
            if (selectedIndex < 0 && !string.IsNullOrWhiteSpace(previousText))
            {
                selectedIndex = options.FindIndex(option => string.Equals(option.DisplayText, previousText, StringComparison.Ordinal));
            }

            // 程序名称与产品工号是一组相互关联的操作员录入项：未选择就保持为空，不默认选中第一项，
            // 避免工号还空着界面却显示一个未确认的程序，被误当成已选好而直接开工。
            ForceProgramNameSelection(
                selectedIndex,
                selectedIndex >= 0 ? options[selectedIndex].DisplayText : string.Empty);
        }
        finally
        {
            _syncingOfflineProgramSelection = false;
        }
    }

    /// <summary>
    /// 从本地程序库刷新“产品工号”下拉选项，同一工号只保留一项。
    /// </summary>
    private void BindOfflineProductNumOptions()
    {
        var previousText = GetProductNumInputText();
        var requireBothStations = _currentSettings.EnableDualStation && !_currentSettings.EnableDualWorkOrder;
        var options = OfflineStartInputRules.BuildProductNumOptions(
            _localProgramSnapshot,
            CurrentStationNo,
            requireBothStations).ToList();

        _syncingOfflineProductNumSelection = true;
        try
        {
            _offlineProductNumOptions.Clear();
            _offlineProductNumOptions.AddRange(options);
            selectProdNum.Items.Clear();
            selectProdNum.Items.AddRange(options.Select(option => option.DisplayText).Cast<object>().ToArray());

            var stationKey = NormalizeStationNo(CurrentStationNo);
            // 离线态工号是操作员自己的录入项：未录入就保持为空，不默认选中第一项，避免误按未确认的工号开工。
            var desired = _userSelectedOfflineProductNums.TryGetValue(stationKey, out var remembered)
                ? remembered
                : previousText;
            var selectedIndex = string.IsNullOrWhiteSpace(desired)
                ? -1
                : options.FindIndex(option => string.Equals(option.DisplayText, desired, StringComparison.Ordinal));

            // 手工录入的现场工号不在程序库选项中，此时保留文本而不是清空记忆。
            ForceProductNumSelection(selectedIndex, desired);
        }
        finally
        {
            _syncingOfflineProductNumSelection = false;
        }
    }

    /// <summary>
    /// 取当前应用于程序名称筛选的产品工号；无选项或无选中时返回空表示不筛选。
    /// 返回值仅在启用“按产品工号筛选程序”时被采用，是否筛选由调用方门控。
    /// </summary>
    private string? ResolveOfflineProductNumFilter()
    {
        var productNum = GetSelectedOfflineProductNum();
        return string.IsNullOrWhiteSpace(productNum) ? null : productNum;
    }

    /// <summary>
    /// 取产品工号下拉当前选中的工号。
    /// AntdUI 筛选态下拉的 SelectedIndex 指向筛选后的子列表，因此按显示文本回查完整选项。
    /// </summary>
    private string GetSelectedOfflineProductNum()
    {
        var selectedIndex = SelectListRules.ResolveSelectedIndex(
            _offlineProductNumOptions.Select(option => option.DisplayText).ToList(),
            selectProdNum.SelectedValue as string ?? selectProdNum.Text,
            selectProdNum.SelectedIndex);
        return selectedIndex >= 0 && selectedIndex < _offlineProductNumOptions.Count
            ? _offlineProductNumOptions[selectedIndex].ProductNum
            : string.Empty;
    }

    /// <summary>
    /// 取产品工号控件当前显示的工号，用于开工上报和运行态记忆。
    /// 只按控件文本解析：手工输入不会更新 AntdUI 的 SelectedValue，采信 SelectedValue 会拿到改写前的旧工号。
    /// 文本命中下拉选项时返回该选项的规范工号，统一同一工号的大小写写法；未命中时按现场手输工号原样返回。
    /// </summary>
    private string GetProductNumInputText()
    {
        var text = selectProdNum.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var index = _offlineProductNumOptions.FindIndex(
            option => string.Equals(option.DisplayText, text, StringComparison.Ordinal));
        return index >= 0 ? _offlineProductNumOptions[index].ProductNum : text;
    }

    /// <summary>
    /// 用本地程序列表填充离线程序号下拉框。
    /// </summary>
    /// <param name="options">离线程序名称选项。</param>
    /// <param name="currentRecipeCode">需要保持显示的当前配方号。</param>
    /// <summary>
    /// 将选中的程序名称关联信息同步到产品工号和部件图号。
    /// </summary>
    /// <param name="option">选中的本地程序；为空时清空联动字段。</param>
    /// <param name="syncProgramFields">
    /// 是否用程序值回填产品工号和部件图号；仅操作员显式选择时传 true。
    /// 后台 1Hz 重绑定传 false，否则会把操作员改写的工号和图号刷回程序原值。
    /// </param>
    private void ApplyOfflineProgramNameOption(OfflineProgramNameOption? option, bool syncProgramFields)
    {
        if (syncProgramFields)
        {
            SetProductNumSelectionText(option?.Program.ProductNum ?? string.Empty);
            RememberProductNumInput(option?.Program.ProductNum);
            inputDrawingNo.Text = option?.Program.ComponentCode?.Trim() ?? string.Empty;
        }

        if (option is not null && IsOfflineInputEditable(GetCurrentStationState()))
        {
            selectProgramName.Text = option.DisplayText;
        }
    }

    /// <summary>
    /// 按配方号反向联动离线程序名称、产品工号和产品型号。
    /// </summary>
    /// <param name="recipeCode">配方号。</param>
    /// <returns>找到本地程序并完成联动返回 true。</returns>
    /// <summary>
    /// 获取当前程序名称下拉选中的本地程序。
    /// AntdUI 筛选态下拉的 SelectedIndex 指向筛选后的子列表，因此按显示文本回查完整选项。
    /// </summary>
    /// <returns>选中的程序；未选中时返回 null。</returns>
    private OfflineProgramNameOption? GetSelectedOfflineProgramNameOption()
    {
        var selectedIndex = SelectListRules.ResolveSelectedIndex(
            _offlineProgramNameOptions.Select(option => option.DisplayText).ToList(),
            selectProgramName.SelectedValue as string ?? selectProgramName.Text,
            selectProgramName.SelectedIndex);
        return selectedIndex >= 0 && selectedIndex < _offlineProgramNameOptions.Count
            ? _offlineProgramNameOptions[selectedIndex]
            : null;
    }

    /// <summary>
    /// 从主界面离线输入控件构造本地开工请求。
    /// </summary>
    /// <param name="stationNo">当前工位号。</param>
    /// <param name="request">构造出的离线开工请求。</param>
    /// <param name="selectedProgram">选中的程序名称选项。</param>
    /// <returns>构造成功返回 true；校验失败返回 false。</returns>
    private bool TryBuildOfflineStartRequest(int stationNo, out OfflineExperimentStartReq request, out OfflineProgramNameOption? selectedProgram)
    {
        request = new OfflineExperimentStartReq();
        selectedProgram = GetSelectedOfflineProgramNameOption();
        if (selectedProgram is null)
        {
            ShowWarning(TextKeys.Monitor.RuntimeError.ProgramNameRequired);
            return false;
        }

        try
        {
            // 逐项具名，避免同类型文本字段顺序错位后仍能编译通过。
            request = OfflineStartInputRules.BuildRequest(
                new OfflineStartInput(
                    StationNo: stationNo,
                    WorkOrderId: GetConfirmedWorkOrderInput(stationNo),
                    Batch: inputBatch.Text,
                    Spec: inputSpec.Text,
                    ProcessNo: inputProcessNo.Text,
                    ProcessName: selectItemName.Text,
                    PlannedQtyText: inputStartAmount.Text,
                    ProductModel: inputProdModel.Text,
                    ProductName: inputProductName.Text,
                    DrawingNo: inputDrawingNo.Text,
                    ProductNum: GetProductNumInputText()),
                selectedProgram);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            ShowWarningText(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 绑定工序选择。
    /// </summary>
    /// <param name="workOrder">MES 工单数据。</param>
    /// <param name="selectedProcess">当前选中的工序。</param>
    /// <param name="bindSelectedOnly">是否只绑定当前选中工序。</param>
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

    /// <summary>
    /// 将当前选中工序同步到工序名称、工序号和生产数量控件。
    /// </summary>
    /// <param name="process">当前选中的工序。</param>
    private void ApplySelectedProcessInputs(ExpItemData process)
    {
        selectItemName.Text = GetProcessDisplayName(process);
        inputProcessNo.Text = process.ProcessNo ?? string.Empty;
        inputStartAmount.Text = process.StartAmount.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 清空工序选择控件的显示内容。
    /// </summary>
    private void ClearProcessSelectionDisplay()
    {
        if (selectItemName.SelectedIndex != -1)
        {
            selectItemName.SelectedIndex = -1;
        }

        selectItemName.Text = string.Empty;
    }

    /// <summary>
    /// 获取工序在下拉框中的显示名称。
    /// </summary>
    /// <param name="process">工序数据。</param>
    /// <returns>处理后的文本。</returns>
    private static string GetProcessDisplayName(ExpItemData process)
    {
        return string.IsNullOrWhiteSpace(process.ItemName)
            ? process.ProcessNo.Trim()
            : process.ItemName.Trim();
    }

    /// <summary>
    /// 解析选中工序索引。
    /// </summary>
    /// <param name="processes">工序集合。</param>
    /// <param name="selectedProcess">当前选中的工序。</param>
    /// <returns>解析或计算后的数值。</returns>
    private static int ResolveSelectedProcessIndex(IReadOnlyList<ExpItemData> processes, ExpItemData? selectedProcess)
    {
        if (selectedProcess is null)
        {
            return -1;
        }

        var itemIdIndex = selectedProcess.ItemId > 0
            ? processes.ToList().FindIndex(process => process.ItemId == selectedProcess.ItemId)
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

    /// <summary>
    /// 应用任务状态Tag。
    /// </summary>
    /// <param name="state">工位运行状态。</param>
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

    /// <summary>
    /// 应用任务状态Tag。
    /// </summary>
    /// <param name="statusText">状态显示文本。</param>
    /// <param name="backColor">背景颜色。</param>
    private void ApplyTaskStatusTag(string statusText, Color backColor)
    {
        tagTaskStatus.Text = $"{statusText}\r\n工位状态";
        tagTaskStatus.ForeColor = backColor.ToArgb() == UiColors.Status.Warning.ToArgb()
            ? Color.Black
            : Color.White;
        tagTaskStatus.BackColor = backColor;
    }

    /// <summary>
    /// 刷新生产运行状态。
    /// </summary>
    private void RefreshProductionRuntimeState()
    {
        BindProductionRuntimeState();
        BindProductionMetrics(GetCurrentProductionSnapshot());
        RefreshProductHistoryPreview();
        QueueRefreshSchemePreview(force: false);
    }

    /// <summary>
    /// 应用WorkId快照。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
    private void ApplyWorkIdSnapshot(PlcWorkIdSnapshot snapshot)
    {
        if (ApplyClearedPlcWorkOrderInput(snapshot))
        {
            return;
        }

        if (snapshot.StationNo != CurrentStationNo)
        {
            return;
        }

        if (ApplyPlcWorkOrderInput(snapshot))
        {
            var stationNo = NormalizeStationNo(snapshot.StationNo);
            var workId = WorkOrderInputConfirmationRules.Normalize(snapshot.WorkId);
            var isNewPlcWorkOrder = !_lastAutoQueriedWorkIds.TryGetValue(stationNo, out var lastWorkId)
                || !string.Equals(lastWorkId, workId, StringComparison.OrdinalIgnoreCase);
            BindProductionRuntimeState();
            QueueRefreshSchemePreview(force: true);
            if (isNewPlcWorkOrder && _mesConnectionMonitorService.Current.IsConnected)
            {
                _lastAutoQueriedWorkIds[stationNo] = workId;
                _ = StartWorkOrderLoadAsync(workId, stationNo, showDialogOnFailure: false);
            }
        }

        if (!snapshot.IsSuccess && !string.IsNullOrWhiteSpace(snapshot.Message))
        {
            SetRuntimeError(TextKeys.Monitor.RuntimeError.WorkIdReadFailed);
        }
    }

    /// <summary>
    /// Queues an automatic MES work-order query when PLC scans a new work order on an idle online station.
    /// </summary>
    /// <param name="snapshot">PLC work-order snapshot.</param>
    private void QueueAutoWorkOrderQuery(PlcWorkIdSnapshot snapshot)
    {
        var stationNo = NormalizeStationNo(snapshot.StationNo);
        var state = _weldTaskService.CurrentState.GetOrCreateStation(stationNo);
        var workId = WorkOrderInputConfirmationRules.Normalize(snapshot.WorkId);
        _lastAutoQueriedWorkIds.TryGetValue(stationNo, out var lastWorkId);
        var hasRunningTask = IsRunningWeldTask(state.ActiveTask) || _weldTaskService.GetUnfinishedTask(stationNo) is not null;

        if (!WorkOrderAutoQueryRules.ShouldAutoQuery(
                _mesConnectionMonitorService.Current.IsConnected,
                hasRunningTask,
                snapshot.IsSuccess,
                workId,
                lastWorkId,
                queryInProgress: false))
        {
            return;
        }

        _lastAutoQueriedWorkIds[stationNo] = workId;
        _ = AutoLoadWorkOrderInfoAsync(stationNo, workId);
    }

    /// <summary>
    /// Loads MES work-order information for an automatic PLC scan without blocking the monitor event.
    /// </summary>
    /// <param name="stationNo">Station number.</param>
    /// <param name="workId">Work order read from PLC.</param>
    /// <returns>Asynchronous operation.</returns>
    private Task AutoLoadWorkOrderInfoAsync(int stationNo, string workId)
    {
        return StartWorkOrderLoadAsync(workId, stationNo, showDialogOnFailure: false);
    }

    private void ApplyLatestWeldPointRecord(BizWeldPointRecord record)
    {
        ApplyStationResult(record);
        if (record.StationNo > 0 && record.StationNo != CurrentStationNo)
        {
            return;
        }

        // 未开工或已完工时不得把采集记录写回实时预览，否则完工上报后表格会重新建列。
        if (IsRunningWeldTask(GetCurrentStationState().ActiveTask))
        {
            BindWeldParameterRows(record);
        }

        if (record.ProductCompleted)
        {
            RefreshProductHistoryPreview();
        }

        ClearRuntimeError();
        SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.ProductDataCollected);
    }

    /// <summary>
    /// 判断是否显示生产提示。
    /// </summary>
    /// <param name="entry">生产流程日志条目。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
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
            "RecipeCodeChangedDetected" or
            "RecipeCodeReconcileSucceeded" or
            "RecipeCodeReconcileFailed" or
            "BusinessSignalWrite" or
            "WorkOrderFinishedCountReset";
    }

    /// <summary>
    /// 应用生产提示。
    /// </summary>
    /// <param name="entry">生产流程日志条目。</param>
    private void ApplyProductionHint(ProductionFlowLogEntry entry)
    {
        if (ShouldRefreshProductHistoryFromLog(entry))
        {
            RefreshProductHistoryPreview();
        }

        var hint = ResolveProductionHint(entry);
        if (entry.Level.Equals("Error", StringComparison.OrdinalIgnoreCase))
        {
            SetRuntimeError(hint.MessageKey, hint.Args);
            return;
        }

        ClearRuntimeError();
        SetRuntimeStatusSuccess(hint.MessageKey, hint.Args);
    }

    /// <summary>
    /// 判断是否刷新产品历史从Log。
    /// </summary>
    /// <param name="entry">生产流程日志条目。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private static bool ShouldRefreshProductHistoryFromLog(ProductionFlowLogEntry entry)
    {
        return false;
    }

    /// <summary>
    /// 解析生产提示文本键和参数。
    /// </summary>
    /// <param name="entry">生产流程日志条目。</param>
    /// <returns>资源键和参数。</returns>
    private (string MessageKey, object[] Args) ResolveProductionHint(ProductionFlowLogEntry entry)
    {
        return entry.Step switch
        {
            "ProductDataReady" => RuntimeTip(ProductionFlowLogTexts.ResourceKeys.ProductDataReady),
            "ProductCollectionStart" => RuntimeTip(ProductionFlowLogTexts.ResourceKeys.ProductCollectionStart),
            "ProductDataReadStart" => RuntimeTip(ProductionFlowLogTexts.ResourceKeys.ProductDataReadStart),
            "ProductDataSaved" => RuntimeTip(ProductionFlowLogTexts.ResourceKeys.ProductDataSaved),
            "ProductDataSaveFailed" => RuntimeTip(ProductionFlowLogTexts.ResourceKeys.ProductDataSaveFailed),
            "ProductCollectionFeedback" => entry.Level.Equals("Error", StringComparison.OrdinalIgnoreCase)
                ? RuntimeTip(ProductionFlowLogTexts.ResourceKeys.ProductCollectionFeedbackFailed)
                : RuntimeTip(ProductionFlowLogTexts.ResourceKeys.ProductCollectionFeedbackSucceeded),
            "RecipeCodeWriteSucceeded" => RuntimeTip(ProductionFlowLogTexts.ResourceKeys.RecipeCodeWriteSucceeded),
            "RecipeCodeWriteFailed" => RuntimeTip(ProductionFlowLogTexts.ResourceKeys.RecipeCodeWriteFailed),
            "RecipeCodeValidationSucceeded" => RuntimeTip(ProductionFlowLogTexts.ResourceKeys.RecipeCodeValidationSucceeded),
            "RecipeCodeValidationFailed" => RuntimeTip(ProductionFlowLogTexts.ResourceKeys.RecipeCodeValidationFailed),
            "RecipeCodeChangedDetected" => RuntimeTip(
                ProductionFlowLogTexts.ResourceKeys.RecipeCodeChangedDetected,
                GetProductionLogDetailValue(entry, "PlcRecipeCode")),
            "RecipeCodeReconcileSucceeded" => RuntimeTip(
                ProductionFlowLogTexts.ResourceKeys.RecipeCodeReconcileSucceeded,
                GetProductionLogDetailValue(entry, "ExpectedRecipeCode")),
            "RecipeCodeReconcileFailed" => RuntimeTip(
                ProductionFlowLogTexts.ResourceKeys.RecipeCodeReconcileFailed,
                GetProductionLogDetailValue(entry, "ExpectedRecipeCode"),
                GetProductionLogDetailValue(entry, "PlcRecipeCode")),
            "BusinessSignalWrite" => entry.Level.Equals("Error", StringComparison.OrdinalIgnoreCase)
                ? RuntimeTip(ProductionFlowLogTexts.ResourceKeys.BusinessSignalWriteFailed)
                : RuntimeTip(ProductionFlowLogTexts.ResourceKeys.BusinessSignalWriteSucceeded),
            _ => RuntimeTip(TextKeys.Monitor.RuntimeStatus.ProductDataCollected)
        };
    }

    /// <summary>
    /// 组合运行提示资源键和参数。
    /// </summary>
    /// <param name="messageKey">本地化文本键。</param>
    /// <param name="args">本地化参数。</param>
    /// <returns>资源键和参数。</returns>
    private static (string MessageKey, object[] Args) RuntimeTip(string messageKey, params object[] args)
        => (messageKey, args);

    /// <summary>
    /// 从生产流程日志详情中读取 key=value 形式的值，供本地化提示拼接动态参数。
    /// </summary>
    /// <param name="entry">生产流程日志条目。</param>
    /// <param name="key">详情字段名。</param>
    /// <returns>字段值；未找到时返回日志摘要，避免界面显示空白。</returns>
    private static string GetProductionLogDetailValue(ProductionFlowLogEntry entry, string key)
    {
        var separators = new[] { "\r\n", "\n", ";" };
        foreach (var part in entry.Detail.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = part.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                continue;
            }

            var fieldName = part[..separatorIndex].Trim();
            if (string.Equals(fieldName, key, StringComparison.OrdinalIgnoreCase))
            {
                return part[(separatorIndex + 1)..].Trim();
            }
        }

        return entry.Summary;
    }

    private void ApplyStationResult(BizWeldPointRecord record)
    {
        if (!record.ProductCompleted)
        {
            return;
        }

        if (record.StationNo == 2 && !_dualStationEnabled)
        {
            return;
        }

        var resultText = ResolveStationProductResultText(record);
        ApplyProductResultToGroup(record.StationNo, resultText);
    }

    private static string ResolveStationProductResultText(BizWeldPointRecord record)
    {
        // 程序计算模式会把正式结果写入实体字段，RawDataJson.product_result 保留 PLC 原始值用于追溯。
        var productResult = record.ProductResult;
        if (string.IsNullOrWhiteSpace(productResult))
        {
            var rawValues = ParseRawWeldValues(record.RawDataJson);
            productResult = FindRawValue(rawValues, "product_result");
        }

        if (string.IsNullOrWhiteSpace(productResult)
            || string.Equals(productResult.Trim(), ProductionConstants.TestResults.Unknown, StringComparison.OrdinalIgnoreCase)
            || string.Equals(productResult.Trim(), "--", StringComparison.Ordinal))
        {
            return "--";
        }

        return TestResultRules.ToDisplayText(productResult);
    }

    /// <summary>
    /// 解析工位结果颜色。
    /// </summary>
    /// <param name="resultText">结果文本。</param>
    /// <returns>用于界面显示的颜色。</returns>
    private static Color ResolveStationResultColor(string resultText)
    {
        if (TestResultRules.IsOk(resultText))
        {
            return UiColors.Status.Success;
        }

        return TestResultRules.IsFailed(resultText)
            ? UiColors.Status.Danger
            : UiColors.Status.Muted;
    }

    /// <summary>
    /// 规范化工位结果文本。
    /// </summary>
    /// <param name="rawResult">原始结果。</param>
    /// <returns>处理后的文本。</returns>
    private static string NormalizeStationResultText(string? rawResult)
        => TestResultRules.ToDisplayText(rawResult);

    /// <summary>
    /// 更新当前时间。
    /// </summary>
    private void UpdateCurrentTime()
    {
        lblCurTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 应用本地化文本。
    /// </summary>
    private void ApplyLocalizedTexts()
    {
        lblTitle.Text = _currentSettings.DeviceName;
        ApplyDeviceIdText();
        lblWorkOrder.Text = _localizer.GetString(TextKeys.Monitor.Label.WorkOrderNo);
        chkEnableDualWorkOrder.Text = _localizer.GetString(TextKeys.Monitor.Checkbox.EnableDualWorkOrder);
        chkFilterByProductNumber.Text = _localizer.GetString(TextKeys.Monitor.Checkbox.FilterByProductNumber);
        SyncProductNumberFilterToggle(_currentSettings.UseProductNumberFilter);
        tooltipComponent.SetTip(
            chkFilterByProductNumber,
            _localizer.GetString(TextKeys.Monitor.Tooltip.FilterByProductNumber));
        tooltipComponent.SetTip(
            chkEnableDualWorkOrder,
            _localizer.GetString(TextKeys.Monitor.Tooltip.EnableDualWorkOrder));
        chkMergedDisplay1.Text = _localizer.GetString(TextKeys.Monitor.Checkbox.MergedDisplay);
        SyncMergedDisplayToggle(_currentSettings.IsWholePieceMergedDisplayEnabled);
        chkFaceResultDisplay1.Text = _localizer.GetString(TextKeys.Monitor.Checkbox.FaceResultDisplay);
        SyncFaceResultDisplayToggle(_currentSettings.IsWholePieceFaceResultDisplayEnabled);
        tooltipComponent.SetTip(
            chkFaceResultDisplay1,
            _localizer.GetString(TextKeys.Monitor.Tooltip.FaceResultDisplay));
        ApplyProgramLimitsDisplay();
        tooltipComponent.SetTip(
            lblLiveProgramLimits1,
            _localizer.GetString(TextKeys.Monitor.Tooltip.ProgramLimits));
        tooltipComponent.SetTip(
            chkMergedDisplay1,
            _localizer.GetString(TextKeys.Monitor.Tooltip.MergedDisplay));
        lblProgramName.Text = _localizer.GetString(TextKeys.Monitor.Label.ProgramName);
        lblProductNo.Text = _localizer.GetString(TextKeys.Monitor.Label.ProductNumber);
        lblProdModel.Text = _localizer.GetString(TextKeys.Monitor.Label.ProductModel);
        lblBatchNo.Text = _localizer.GetString(TextKeys.Monitor.Label.Batch);
        lblSpec.Text = _localizer.GetString(TextKeys.Monitor.Label.Spec);
        lblPartName.Text = _localizer.GetString(TextKeys.Monitor.Label.PartName);
        lblDrawingNo.Text = _localizer.GetString(TextKeys.Monitor.Label.DrawingNo);
        lblProcessNo.Text = _localizer.GetString(TextKeys.Monitor.Label.ProcessNo);
        lblProcessName.Text = _localizer.GetString(TextKeys.Monitor.Label.ProcessName);


        lblLiveHint1.Text = "实时采集正常";
        lblLiveHint2.Text = "实时采集正常";
        lblLiveProductNo1.Text = "产品编号：--";
        lblLiveProductNo2.Text = "产品编号：--";
        tagResult1.Text = "工位1--";
        tagResult2.Text = "工位2--";
        lblLiveTouchNo1.Text = "焊点：--";
        lblLiveTouchNo2.Text = "焊点：--";
        lblLiveHint1.ForeColor = UiColors.Status.Success;
        lblLiveHint2.ForeColor = UiColors.Status.Success;

        btnOnlineReport.Text = _localizer.GetString(TextKeys.Monitor.Button.StartReport);
        btnLocalWorkOrder.Text = _localizer.GetString(TextKeys.Monitor.Button.LocalWorkOrder);
        btnClearErrorTips.Text = _localizer.GetString(TextKeys.Monitor.Button.ClearErrorTips);

        grpErrorTips.Text = _localizer.GetString(TextKeys.Monitor.Group.ExceptionTips);
        grpRunningStatus.Text = _localizer.GetString(TextKeys.Monitor.Group.RunningStatus);
        tableMetric1.Text = _localizer.GetString(TextKeys.Monitor.Group.ProductionMetrics);

        dgvPreview1.Text = "实时测试结果";
        dgvPreview2.Text = "实时测试结果";

        ApplyProductResultToGroup(ProductionConstants.Stations.DefaultStationNo, ProductionConstants.TestResults.NotAvailable);
        ApplyProductResultToGroup(2, ProductionConstants.TestResults.NotAvailable);
        RefreshRuntimePanels();
    }

    /// <summary>
    /// Displays the configured device id on the monitor header.
    /// Empty values are shown as "--" so operators can immediately spot incomplete settings.
    /// </summary>
    private void ApplyDeviceIdText()
    {
        var deviceId = string.IsNullOrWhiteSpace(_currentSettings.DeviceId)
            ? "--"
            : _currentSettings.DeviceId.Trim();
        lblDeviceId.Text = $"{_localizer.GetString(TextKeys.SystemSetting.LabelDeviceId)}：{deviceId}";
    }

    #endregion

    #region PLC 与 MES 状态展示

    /// <summary>
    /// 应用 PLC 连接状态快照，并在需要时触发业务信号调和。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
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
            PlcConnectionState.Unverified => UiColors.Status.Warning,
            PlcConnectionState.Disconnected => UiColors.Status.Warning,
            PlcConnectionState.Faulted => UiColors.Status.Danger,
            PlcConnectionState.Stopped => UiColors.Status.Muted,
            _ => UiColors.Status.Danger
        };

        if (!snapshot.IsConnected)
        {
            // PLC 断开后清空上次成功快照，重连后必须重新读取确认，不能相信旧状态。
            _lastWorkOrderStatusSnapshots.Remove(stationNo);
            _lastDeviceModeSnapshots.Remove(stationNo);
        }

        if (snapshot.IsConnected && _enableBusinessSignalReconcile)
        {
            // PLC 重连后主动调和业务信号，避免设备端状态停留在断线前的旧值。
            QueueBusinessSignalReconciliation("PLC.StatusChanged");
        }

        if (_plcStatusToolTipVisible)
        {
            RefreshPlcStatusToolTip();
        }
    }

    #endregion

    #region PLC 业务信号调和

    /// <summary>
    /// 排队触发 PLC 设备模式和工单状态的业务信号调和。
    /// </summary>
    /// <param name="source">触发来源或日志来源。</param>
    /// <param name="includeDeviceMode">是否调和设备模式。</param>
    /// <param name="includeWorkOrderStatus">是否调和工单状态。</param>
    private void QueueBusinessSignalReconciliation(string source, bool includeDeviceMode = true, bool includeWorkOrderStatus = true)
    {
        var cancellationSource = Volatile.Read(ref _businessSignalReconcileCancellation);
        if (cancellationSource is null || cancellationSource.IsCancellationRequested)
        {
            return;
        }

        CancellationToken cancellationToken;
        try
        {
            cancellationToken = cancellationSource.Token;
        }
        catch (ObjectDisposedException)
        {
            // 页面可能在状态事件到达的同时被销毁，此时无需再启动调和任务。
            return;
        }

        if (includeDeviceMode)
        {
            // 调和任务允许后台执行；方法内部有运行标记，避免重复并发。
            _ = ReconcileDeviceModeAsync(source, cancellationToken);
        }

        if (includeWorkOrderStatus)
        {
            // 工单状态调和同样后台执行，避免阻塞 PLC 状态事件回调。
            _ = ReconcileWorkOrderStatusAsync(source, cancellationToken);
        }
    }

    /// <summary>
    /// 原子移除并释放页面生命周期令牌源，确保重复销毁不会再次访问已释放对象。
    /// </summary>
    /// <param name="source">待取消并释放的令牌源。</param>
    private static void CancelAndDispose(ref CancellationTokenSource? source)
    {
        var cancellationSource = Interlocked.Exchange(ref source, null);
        if (cancellationSource is null)
        {
            return;
        }

        cancellationSource.Cancel();
        cancellationSource.Dispose();
    }

    /// <summary>
    /// 页面关闭时取消 MES 重连补传；最终停机状态由应用生命周期在同一个 MES 超时窗口内处理。
    /// </summary>
    private void CancelPendingUploadRetry()
    {
        var cancellationSource = Interlocked.Exchange(ref _pendingUploadRetryCancellation, null);
        var retryTask = Interlocked.Exchange(ref _pendingUploadRetryTask, null);
        cancellationSource?.Cancel();
        if (cancellationSource is null)
        {
            return;
        }

        if (retryTask is null)
        {
            cancellationSource.Dispose();
            return;
        }

        _ = retryTask.ContinueWith(
            _ => cancellationSource.Dispose(),
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);
    }

    /// <summary>
    /// 异步调和 PLC 设备模式，确保 PLC 与当前软件设置一致。
    /// </summary>
    /// <param name="source">触发来源或日志来源。</param>
    /// <param name="cancellationToken">页面生命周期取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    private async Task ReconcileDeviceModeAsync(string source, CancellationToken cancellationToken)
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
                cancellationToken.ThrowIfCancellationRequested();
                if (!_plcCommunicationService.GetCurrent(stationNo).IsConnected)
                {
                    // 未连接工位不写 PLC，避免把通讯异常误判为业务写入失败。
                    continue;
                }

                await EnsureDeviceModeAsync(
                    stationNo,
                    deviceMode,
                    "PLC.DeviceMode.Reconcile",
                    ProductionFlowLogTexts.Summaries.DeviceModeReconcileFailed,
                    source,
                    writeOnReadFailure: false,
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
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

    /// <summary>
    /// 异步调和 PLC 工单状态，确保 PLC 与当前任务状态一致。
    /// </summary>
    /// <param name="source">触发来源或日志来源。</param>
    /// <param name="cancellationToken">页面生命周期取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    private async Task ReconcileWorkOrderStatusAsync(string source, CancellationToken cancellationToken)
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
                cancellationToken.ThrowIfCancellationRequested();
                if (!_plcCommunicationService.GetCurrent(stationNo).IsConnected)
                {
                    // 工位离线时跳过调和，等待下一次连接恢复事件再处理。
                    continue;
                }

                await EnsureWorkOrderStatusAsync(
                    stationNo,
                    ResolveExpectedPlcWorkOrderStatus(stationNo),
                    "PLC.WorkOrderStatus.Reconcile",
                    ProductionFlowLogTexts.Summaries.WorkOrderStatusReconcileFailed,
                    source,
                    writeOnReadFailure: false,
                    mirrorWorkOrderStations: false,
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
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

    /// <summary>
    /// 解析业务信号调和Stations。
    /// </summary>
    /// <returns>解析后的集合。</returns>
    private IReadOnlyList<int> ResolveBusinessSignalReconcileStations()
    {
        var settings = _currentSettings;
        return settings.EnableDualStation
            ? [1, 2]
            : [ProductionConstants.Stations.DefaultStationNo];
    }

    /// <summary>
    /// 解析ExpectedPlc工单状态。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析或计算后的数值。</returns>
    private int ResolveExpectedPlcWorkOrderStatus(int stationNo)
    {
        return _weldTaskService.GetUnfinishedTask(stationNo) is null
            ? ProductionConstants.PlcWorkOrderStatuses.FinishedForbidProduction
            : ProductionConstants.PlcWorkOrderStatuses.StartedAllowProduction;
    }

    #endregion

    #region PLC 与 MES 状态展示

    /// <summary>
    /// 获取PLC 状态键。
    /// </summary>
    /// <param name="state">工位运行状态。</param>
    /// <returns>处理后的文本。</returns>
    private static string GetPlcStateKey(PlcConnectionState state)
    {
        return state switch
        {
            PlcConnectionState.Connecting => TextKeys.Plc.StateConnecting,
            PlcConnectionState.Connected => TextKeys.Plc.StateConnected,
            PlcConnectionState.Reconnecting => TextKeys.Plc.StateReconnecting,
            PlcConnectionState.Unverified => TextKeys.Plc.StateUnverified,
            PlcConnectionState.Disconnected => TextKeys.Plc.StateDisconnected,
            PlcConnectionState.Faulted => TextKeys.Plc.StateFaulted,
            _ => TextKeys.Plc.StateStopped
        };
    }

    /// <summary>
    /// 应用 MES 连接状态，并根据在线状态调整可用操作。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
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
        if (snapshot.IsConnected && !_lastMesConnected)
        {
            // MES 从离线变为在线后，立即重试断线期间积压的上传任务；该行为不依赖 PLC 业务信号调和开关。
            QueuePendingUploadRetry();
        }

        _lastMesConnected = snapshot.IsConnected;
    }

    /// <summary>
    /// 应用MesDependentButton状态。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
    private void ApplyMesDependentButtonState(MesConnectionSnapshot snapshot)
    {
        if (_stationViewReadOnly)
        {
            ApplyOperationMode();
            return;
        }

        ApplyReportButtonState();
    }

    /// <summary>
    /// 在 MES 恢复连接后排队重试待上传数据。
    /// </summary>
    private void QueuePendingUploadRetry()
    {
        if (Interlocked.CompareExchange(ref _pendingUploadRetryRunning, 1, 0) != 0)
        {
            return;
        }

        var cancellationSource = Volatile.Read(ref _pendingUploadRetryCancellation);
        if (cancellationSource is null)
        {
            Interlocked.Exchange(ref _pendingUploadRetryRunning, 0);
            return;
        }

        CancellationToken cancellationToken;
        try
        {
            cancellationToken = cancellationSource.Token;
        }
        catch (ObjectDisposedException)
        {
            Interlocked.Exchange(ref _pendingUploadRetryRunning, 0);
            return;
        }

        _pendingUploadRetryTask = Task.Run(async () =>
        {
            try
            {
                await _weldTaskService.RetryPendingUploadsAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 页面关闭会取消重连补传，最终停机状态由应用生命周期统一处理。
            }
            catch (Exception ex)
            {
                _exceptionLogService.Write(ex, "MonitorView.QueuePendingUploadRetry");
            }
            finally
            {
                Interlocked.Exchange(ref _pendingUploadRetryRunning, 0);
            }
        });
    }

    /// <summary>
    /// 获取MES 状态键。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
    /// <returns>处理后的文本。</returns>
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
    /// 应用生产状态。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
    private void ApplyProductionStatus(PlcProductionSnapshot snapshot)
    {
        // 通知按整台设备聚合，任一工位的快照变化都要重算，不能只在当前工位刷新。
        SyncPlcAlarmNotification();

        if (NormalizeStatusStationNo(snapshot.StationNo) != CurrentStationNo)
        {
            return;
        }

        ApplyDeviceStatus(snapshot);
        BindProductionMetrics(snapshot);
    }

    /// <summary>
    /// 应用设备状态。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
    private void ApplyDeviceStatus(PlcProductionSnapshot snapshot)
    {
        var stateKey = snapshot.IsSoftwareAlarmActive
            ? TextKeys.DeviceStatus.Alarm
            : snapshot.IsAlarmPendingConfirmation
                ? TextKeys.DeviceStatus.AlarmPendingConfirmation
                : snapshot.IsRawAlarmUnconfirmed
                    ? TextKeys.DeviceStatus.Unknown
                    : GetDeviceStatusKey(snapshot.DeviceStatusCode);
        var stateText = _localizer.GetString(stateKey);

        // The dynamic state is placed first so it stays visible even if the Tag only paints one line.
        tagDeviceStatus.Text = $"{stateText}\r\n{_localizer.GetString(TextKeys.Monitor.Label.DeviceState)}";
        tagDeviceStatus.ForeColor = Color.White;
        tagDeviceStatus.BackColor = snapshot.IsSoftwareAlarmActive
            ? UiColors.Status.Danger
            : snapshot.IsAlarmPendingConfirmation
                ? UiColors.Status.Warning
                : snapshot.IsRawAlarmUnconfirmed
                    ? UiColors.Status.Muted
                    : GetDeviceStatusColor(snapshot.DeviceStatusCode, snapshot.IsSuccess);

        if (PlcAlarmNotificationRules.IsActive(
                snapshot.IsSoftwareAlarmActive,
                snapshot.IsAlarmPendingConfirmation,
                snapshot.IsRawAlarmUnconfirmed))
        {
            var alarmMessages = PlcAlarmNotificationRules.SplitMessages(snapshot.SoftwareAlarmMessage);
            if (alarmMessages.Count == 0)
            {
                alarmMessages = [PlcSoftwareAlarmRules.GenericAlarmMessage];
            }

            _deviceAlarmRuntimeErrorText = string.Join("；", alarmMessages);
            var pendingConfirmation = !snapshot.IsSoftwareAlarmActive;
            _deviceAlarmPendingConfirmation = pendingConfirmation;
            var signature = PlcAlarmNotificationRules.CreateSignature(alarmMessages, pendingConfirmation);
            var isDismissed = string.Equals(_plcAlarmSummaryDismissedSignature, signature, StringComparison.Ordinal);
            if (!isDismissed)
            {
                SetRuntimeErrorDetailText(
                    _deviceAlarmRuntimeErrorText,
                    snapshot.IsSoftwareAlarmActive
                        ? TextKeys.Monitor.RuntimeError.DeviceAlarmSummary
                        : TextKeys.Monitor.RuntimeError.DeviceAlarmPending,
                    RuntimeErrorSourceDeviceAlarm,
                    alarmMessages.Count);
            }
            else if (string.Equals(_runtimeErrorSource, RuntimeErrorSourceDeviceAlarm, StringComparison.Ordinal))
            {
                inputErrorTips.Text = BuildLocalizedMessage(
                    pendingConfirmation
                        ? TextKeys.Monitor.RuntimeError.DeviceAlarmPending
                        : TextKeys.Monitor.RuntimeError.DeviceAlarmSummary,
                    alarmMessages.Count);
                ApplyRuntimeErrorTone(hasError: true);
            }

            return;
        }

        ClearDeviceAlarmRuntimeErrorIfCurrent();

        if (!snapshot.IsSuccess && !string.IsNullOrWhiteSpace(snapshot.Message))
        {
            SetRuntimeError(TextKeys.Monitor.RuntimeError.ProductionCollectFailed);
        }
    }

    /// <summary>
    /// 处理 PLC 报警通知的打开、更新和恢复关闭。
    /// 报警地址属于整台设备，所有工位聚合为唯一一张通知卡片。
    /// </summary>
    private void SyncPlcAlarmNotification()
    {
        if (!tabsPreview.IsHandleCreated)
        {
            return;
        }

        var state = BuildCurrentPlcAlarmNotificationState();
        if (!state.IsActive)
        {
            ResetPlcAlarmNotificationState();
            return;
        }

        var signature = state.Signature!;
        if (string.Equals(_plcAlarmNotificationSignature, signature, StringComparison.Ordinal))
        {
            return;
        }

        _plcAlarmNotificationSignature = signature;
        ClosePlcAlarmNotificationIfPresent(PlcAlarmNotificationId);
        if (string.Equals(_plcAlarmNotificationDismissedSignature, signature, StringComparison.Ordinal))
        {
            return;
        }

        var notification = new AntdUI.Notification.Config(
            new AntdUI.Target(this),
            _localizer.GetString(TextKeys.Monitor.Notification.PlcAlarmTitle),
            PlcAlarmNotificationRules.BuildDisplayText(state.Messages),
            state.PendingConfirmation ? AntdUI.TType.Warn : AntdUI.TType.Error,
            AntdUI.TAlignFrom.BL,
            _runtimeMessageFont,
            0)
        {
            ID = PlcAlarmNotificationId,
            AutoClose = 0,
            ClickClose = false,
            CloseIcon = true,
            TopMost = false,
            ShowInWindow = true,
            EnableSound = false,
            OnClose = () =>
            {
                if (string.Equals(_plcAlarmNotificationSignature, signature, StringComparison.Ordinal))
                {
                    _plcAlarmNotificationDismissedSignature = signature;
                }
            }
        };
        AntdUI.Notification.open(notification);
    }

    /// <summary>
    /// 按当前设备模式聚合参与报警的工位快照；关闭双工位时工位 2 不参与。
    /// </summary>
    private PlcAlarmNotificationState BuildCurrentPlcAlarmNotificationState()
    {
        var stationNumbers = _currentSettings.EnableDualStation
            ? new[] { 1, 2 }
            : [ProductionConstants.Stations.DefaultStationNo];
        var inputs = stationNumbers
            .Select(stationNo => _plcProductionMonitorService.GetCurrent(stationNo))
            .Select(snapshot => new PlcAlarmNotificationInput(
                snapshot.IsSoftwareAlarmActive,
                snapshot.IsAlarmPendingConfirmation,
                snapshot.IsRawAlarmUnconfirmed,
                snapshot.SoftwareAlarmMessage));

        return PlcAlarmNotificationRules.Aggregate(inputs);
    }

    /// <summary>
    /// 关闭 PLC 报警通知并标记当前报警为已读，右侧摘要不受影响。
    /// </summary>
    private void DismissPlcAlarmNotification()
    {
        if (_plcAlarmNotificationSignature is { } signature)
        {
            _plcAlarmNotificationDismissedSignature = signature;
        }

        ClosePlcAlarmNotificationIfPresent(PlcAlarmNotificationId);
    }

    /// <summary>
    /// 报警恢复或工位配置变化时清空全部通知状态，使下一次报警可以重新弹出。
    /// </summary>
    private void ResetPlcAlarmNotificationState()
    {
        _plcAlarmNotificationSignature = null;
        _plcAlarmNotificationDismissedSignature = null;
        _plcAlarmSummaryDismissedSignature = null;
        ClosePlcAlarmNotificationIfPresent(PlcAlarmNotificationId);
    }

    /// <summary>
    /// 仅在通知已进入 AntdUI 队列或已经显示时关闭，避免 close_id 的 volley 机制抵消随后打开的同 ID 通知。
    /// </summary>
    private static void ClosePlcAlarmNotificationIfPresent(string notificationId)
    {
        if (AntdUI.Notification.contains(notificationId))
        {
            AntdUI.Notification.close_id(notificationId);
        }
    }

    private void CloseAllPlcAlarmNotifications()
    {
        ResetPlcAlarmNotificationState();
    }

    /// <summary>
    /// 由用户清除右侧异常摘要；设备报警仅在本机标记为已读，不写入 PLC。
    /// </summary>
    private void RuntimeErrorClearButton_Click(object? sender, EventArgs e)
    {
        if (string.Equals(_runtimeErrorSource, RuntimeErrorSourceDeviceAlarm, StringComparison.Ordinal))
        {
            // 摘要与通知的报警签名可能不同步（例如通知先于摘要更新），
            // 因此按当前设备聚合状态重算签名，保证摘要和通知一起标记为已读。
            var signature = BuildCurrentPlcAlarmNotificationState().Signature
                ?? _plcAlarmNotificationSignature;
            if (signature is not null)
    /// 清除按钮必须同时关闭报警通知卡片，否则通知会一直留在屏幕上无法清除。
            {
                _plcAlarmSummaryDismissedSignature = signature;
                _plcAlarmNotificationSignature = signature;
            }

            DismissPlcAlarmNotification();
        }

        ClearRuntimeError();
    }

    /// <summary>
    /// 清除由 PLC 报警写入的异常提示，避免报警恢复后覆盖其它业务异常。
    /// </summary>
    private void ClearDeviceAlarmRuntimeErrorIfCurrent()
    {
        var shouldClear = string.Equals(_runtimeErrorSource, RuntimeErrorSourceDeviceAlarm, StringComparison.Ordinal)
            || (!string.IsNullOrWhiteSpace(_deviceAlarmRuntimeErrorText)
                && _runtimeErrorKey is null
                && string.Equals(_runtimeErrorText, _deviceAlarmRuntimeErrorText, StringComparison.Ordinal));
        _deviceAlarmRuntimeErrorText = null;
        _deviceAlarmPendingConfirmation = false;
        if (shouldClear)
        {
            ClearRuntimeError();
        }
    }

    #endregion

    #region 生产指标表格

    /// <summary>
    /// 绑定生产指标。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
    private void BindProductionMetrics(PlcProductionSnapshot snapshot)
    {
        // 工单数量只作为达成率分母参与计算，不再单独占一行：工单信息区已经展示同一数值。
        var mesProductionQuantity = GetCurrentStationState().SelectedProcess?.StartAmount;
        var acceptedRate = CalculateRate(snapshot.AcceptedQuantity, snapshot.TotalProduction);
        var rejectedRate = CalculateRate(snapshot.RejectedQuantity, snapshot.TotalProduction);
        // 达成率口径为合格数/工单数量，与中心看板保持一致，便于两端数值对账。
        var achievementRate = mesProductionQuantity.GetValueOrDefault() > 0
            ? CalculateRate(snapshot.AcceptedQuantity, mesProductionQuantity!.Value)
            : null;

        var rows = new List<ProductionMetricRow>
        {
            new(_localizer.GetString(TextKeys.Production.TotalProduction), snapshot.TotalProduction.ToString()),
            new(_localizer.GetString(TextKeys.Production.AcceptedQuantity), snapshot.AcceptedQuantity.ToString()),
            new(_localizer.GetString(TextKeys.Production.RejectedQuantity), snapshot.RejectedQuantity.ToString()),
            new(_localizer.GetString(TextKeys.Production.AcceptedRate), FormatRate(acceptedRate)),
            new(_localizer.GetString(TextKeys.Production.RejectedRate), FormatRate(rejectedRate)),
            new(_localizer.GetString(TextKeys.Production.AchievementRate), FormatRate(achievementRate))
        };

        var metricTable = CurrentMetricTable;
        metricTable.DataSource = rows;
        metricTable.Refresh();
    }

    #endregion

    #region 表格列配置

    /// <summary>
    /// 配置生产表格Columns。
    /// </summary>
    private void ConfigureProductionTableColumns()
    {
        ConfigureProductionTableColumns(tableMetric1);
        ConfigureProductionTableColumns(tableMetric2);
    }

    /// <summary>
    /// 配置生产表格Columns。
    /// </summary>
    /// <param name="table">目标表格控件。</param>
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

    /// <summary>
    /// 配置产品历史表格Columns。
    /// </summary>
    private void ConfigureProductHistoryTableColumns()
    {
        var displayOptions = ProductHistoryDisplayOptions.Default with
        {
            ShowTestFlagInHistory = _currentSettings.ShowTestFlagInHistory != false
        };
        ConfigureProductHistoryTableColumns(tableHistory1, [], 1, displayOptions);
        ConfigureProductHistoryTableColumns(tableHistory2, [], 2, displayOptions);
    }

    /// <summary>
    /// 配置产品历史表格Columns。
    /// </summary>
    /// <param name="table">目标表格控件。</param>
    /// <param name="dynamicColumns">动态列集合。</param>
    /// <param name="stationNo">工位编号。</param>
    private void ConfigureProductHistoryTableColumns(
        AntdUI.Table table,
        IReadOnlyList<ProductHistoryDynamicColumn> dynamicColumns,
        int stationNo,
        ProductHistoryDisplayOptions displayOptions)
    {
        var schemaKey = BuildProductHistorySchemaKey(dynamicColumns, displayOptions);

        if (_productHistorySchemaKeys.TryGetValue(stationNo, out var existingSchemaKey)
            && string.Equals(existingSchemaKey, schemaKey, StringComparison.Ordinal)
            && table.Columns.Count > 0)
        {
            return;
        }

        table.Columns.Clear();

        var nodeColumn = new AntdUI.Column(nameof(ProductHistoryTableRow.NodeText), $"产品/{displayOptions.PointName}")
        {
            Align = AntdUI.ColumnAlign.Left,
            ColAlign = AntdUI.ColumnAlign.Center,
            Ellipsis = true
        };
        nodeColumn.SetTree(nameof(ProductHistoryTableRow.Children));

        table.Columns.Add(nodeColumn);
        table.Columns.Add(CreateProductHistoryColumn(nameof(ProductHistoryTableRow.ProductNo), "产品编号"));
        table.Columns.Add(CreateProductHistoryColumn(nameof(ProductHistoryTableRow.TouchNo), displayOptions.PointName));
        table.Columns.Add(CreateProductHistoryColumn(nameof(ProductHistoryTableRow.ResultText), displayOptions.PointResultHeader));
        table.Columns.Add(CreateProductHistoryColumn(nameof(ProductHistoryTableRow.UploadStatusText), "上传状态"));
        if (displayOptions.ShowTestFlagInHistory)
        {
            table.Columns.Add(CreateProductHistoryColumn(nameof(ProductHistoryTableRow.IsTestText), "试焊件"));
        }

        table.Columns.Add(CreateProductHistoryColumn(nameof(ProductHistoryTableRow.TouchCountText), displayOptions.PointCountHeader));
        table.Columns.Add(CreateProductHistoryColumn(nameof(ProductHistoryTableRow.RecordTimeText), "采集时间"));
        foreach (var dynamicColumn in dynamicColumns)
        {
            table.Columns.Add(CreateProductHistoryDynamicColumn(dynamicColumn));
        }

        TableStyleHelper.ApplyAntdColumnDefaults(table);
        nodeColumn.Align = AntdUI.ColumnAlign.Left;
        _productHistorySchemaKeys[stationNo] = schemaKey;
    }

    /// <summary>
    /// 创建产品历史列。
    /// </summary>
    /// <param name="key">键。</param>
    /// <param name="title">标题文本。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
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

    /// <summary>
    /// 创建产品历史动态列。
    /// </summary>
    /// <param name="dynamicColumn">动态列定义。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
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

    /// <summary>
    /// 配置焊接参数表格Columns。
    /// </summary>
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

    #endregion

    #region 产品历史预览

    /// <summary>
    /// 刷新当前工位的产品历史预览，并确保实际刷新在 UI 线程执行。
    /// </summary>
    private void RefreshProductHistoryPreview()
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        // 产品历史会配置 AntdUI 动态列，必须回到 UI 线程，避免后台线程改列集合导致控件异常。
        if (Environment.CurrentManagedThreadId != _uiThreadId)
        {
            PostProductHistoryRefreshToUiThread();
            return;
        }

        if (_refreshingProductHistoryPreview)
        {
            // 刷新中收到的新请求只打标记，当前刷新结束后再补一次，避免递归重入。
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
    /// 将产品历史刷新请求合并投递到 UI 线程。
    /// </summary>
    private void PostProductHistoryRefreshToUiThread()
    {
        if (!IsHandleCreated || Interlocked.Exchange(ref _productHistoryRefreshPosted, 1) == 1)
        {
            return;
        }

        try
        {
            if (!RunOnUiThread(() =>
            {
                // 投递成功后先清掉标记，这样执行期间的新请求可以再次入队。
                Interlocked.Exchange(ref _productHistoryRefreshPosted, 0);
                RefreshProductHistoryPreview();
            }, "MonitorView.ProductHistoryRefresh"))
            {
                Interlocked.Exchange(ref _productHistoryRefreshPosted, 0);
            }
        }
        catch (InvalidOperationException)
        {
            Interlocked.Exchange(ref _productHistoryRefreshPosted, 0);
        }
    }

    /// <summary>
    /// 在 UI 线程加载并绑定当前工位的产品历史数据。
    /// </summary>
    private void RefreshProductHistoryPreviewCore()
    {
        try
        {
            var activeTask = GetCurrentStationState().ActiveTask;
            if (activeTask is null || !IsRunningWeldTask(activeTask))
            {
                // 无当前任务时仍重置列和数据，避免界面保留上一个任务的历史记录。
                ConfigureProductHistoryTableColumns(
                    CurrentProductHistoryTable,
                    [],
                    CurrentStationNo,
                    ProductHistoryDisplayOptions.Default with { ShowTestFlagInHistory = _currentSettings.ShowTestFlagInHistory != false });
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

    /// <summary>
    /// 根据产品历史快照配置动态列并绑定产品历史行。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
    /// <param name="activeTask">active任务。</param>
    private void BindProductHistorySnapshot(ProductHistorySnapshot snapshot, BizWeldTask activeTask)
    {
        var table = GetProductHistoryTable(snapshot.StationNo);
        var displayOptions = ResolveProductHistoryDisplayOptions(activeTask, snapshot.StationNo);
        var dynamicColumns = ResolveProductHistoryDynamicColumns(activeTask, snapshot);
        ConfigureProductHistoryTableColumns(table, dynamicColumns, snapshot.StationNo, displayOptions);
        var rows = ProductHistoryPreviewSortRules.OrderProductsLatestFirst(snapshot.Products)
            .Select(product => ToProductHistoryRow(product, dynamicColumns, displayOptions))
            .ToList();

        BindProductHistoryRows(table, rows);
    }

    /// <summary>
    /// 绑定产品历史行。
    /// </summary>
    /// <param name="table">目标表格控件。</param>
    /// <param name="rows">行数据集合。</param>
    private void BindProductHistoryRows(AntdUI.Table table, IReadOnlyList<ProductHistoryTableRow> rows)
    {
        table.DataSource = rows;
        table.ExpandAll(false);
        table.Invalidate();
    }

    /// <summary>
    /// 获取产品历史表格。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private AntdUI.Table GetProductHistoryTable(int stationNo)
    {
        return stationNo == 2 ? tableHistory2 : tableHistory1;
    }


    /// <summary>
    /// 显示产品历史上下文菜单。
    /// </summary>
    /// <param name="target">目标对象。</param>
    /// <param name="row">表格行数据。</param>
    private void ShowProductHistoryContextMenu(Control target, ProductHistoryTableRow row)
    {
        if (_stationViewReadOnly || !row.ShowTestFlag)
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

    /// <summary>
    /// 设置产品历史中的试焊件标记，并刷新界面状态。
    /// </summary>
    /// <param name="row">表格行数据。</param>
    /// <param name="isTest">是否标记为试焊件。</param>
    private void SetProductHistoryTestFlag(ProductHistoryTableRow row, bool isTest)
    {
        try
        {
            var result = _productHistoryService.SetProductTestFlag(row.TaskId, row.StationNo, row.ProductNo, isTest);
            // 不论服务返回成功或失败，都先刷新一次，保证界面与服务层最终状态一致。
            RefreshProductHistoryPreview();

            if (!result.IsSuccess)
            {
                ShowWarning(TextKeys.Monitor.RuntimeError.TestFlagUpdateFailed);
                return;
            }

            ClearRuntimeError();
            SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.TestFlagUpdated);
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.SetProductHistoryTestFlag");
            ShowWarning(TextKeys.Monitor.RuntimeError.TestFlagUpdateFailed);
        }
    }

    /// <summary>
    /// 处理到产品历史行。
    /// </summary>
    /// <param name="product">产品历史数据。</param>
    /// <param name="dynamicColumns">动态列集合。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private ProductHistoryTableRow ToProductHistoryRow(
        ProductHistoryProduct product,
        IReadOnlyList<ProductHistoryDynamicColumn> dynamicColumns,
        ProductHistoryDisplayOptions displayOptions)
    {
        if (IsWholePieceMergedPreview())
        {
            return ToMergedProductHistoryRow(product, displayOptions);
        }

        var children = product.Points
            .Select(point => ToProductHistoryPointRow(product, point, dynamicColumns, displayOptions))
            .ToList();
        var productRow = new ProductHistoryTableRow
        {
            IsProductRow = true,
            TaskId = product.TaskId,
            StationNo = product.StationNo,
            ProductNo = product.ProductNo,
            NodeText = $"产品 {product.ProductNo}",
            ResultText = FormatHistoryResult(product.Result),
            UploadStatusText = FormatHistoryUploadStatus(product.UploadStatus),
            ShowTestFlag = displayOptions.ShowTestFlagInHistory,
            IsTest = product.IsTest,
            IsTestText = FormatHistoryTestFlag(product.IsTest),
            TouchCountText = product.TouchCount.ToString(CultureInfo.InvariantCulture),
            RecordTimeText = FormatHistoryTime(product.LastRecordTime),
            CanMarkTest = product.CanMarkTest,
            MarkDisabledReason = product.MarkDisabledReason,
            Children = children
        };

        return ProductHistoryDisplayRules.ShouldFlattenSinglePoint(displayOptions.TouchCount, children.Count)
            ? FlattenSinglePointProductRow(productRow, children[0])
            : productRow;
    }

    /// <summary>
    /// 合并视图下每个产品只显示一行，不再展开各检测面；四面数据按 A/B 聚合后填入动态列。
    /// </summary>
    private ProductHistoryTableRow ToMergedProductHistoryRow(
        ProductHistoryProduct product,
        ProductHistoryDisplayOptions displayOptions)
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
            ShowTestFlag = displayOptions.ShowTestFlagInHistory,
            IsTest = product.IsTest,
            IsTestText = FormatHistoryTestFlag(product.IsTest),
            TouchCountText = product.TouchCount.ToString(CultureInfo.InvariantCulture),
            RecordTimeText = FormatHistoryTime(product.LastRecordTime),
            DynamicValues = BuildMergedHistoryDynamicValues(product),
            CanMarkTest = product.CanMarkTest,
            MarkDisabledReason = product.MarkDisabledReason,
            Children = new List<ProductHistoryTableRow>()
        };
    }

    /// <summary>
    /// 按合并列聚合一个产品的四面历史数据。面数不足或数据异常时各列留空。
    /// </summary>
    private Dictionary<string, string> BuildMergedHistoryDynamicValues(ProductHistoryProduct product)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in _mergedPreviewColumns)
        {
            values[column.ColumnName] = string.Empty;
        }

        var sideItemValues = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var sideResults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var point in product.Points)
        {
            var side = point.TouchNo?.Trim() ?? string.Empty;
            if (side.Length == 0)
            {
                continue;
            }

            sideItemValues[side] = ParseRawWeldValues(point.RawDataJson);
            sideResults[side] = point.Result;
        }

        var aggregation = WholePieceAbAggregationRules.AggregatePreview(
            sideItemValues,
            sideResults,
            _mergedPreviewDefinitions,
            _currentSettings.PairedAggregationMode,
            _currentSettings.EnablePlcStringNumericFormatting ?? true,
            _currentSettings.PlcStringNumericFormatMode);
        if (!aggregation.IsSuccess)
        {
            return values;
        }

        foreach (var pair in WholePieceMergedDisplayRules.BuildValues(_mergedPreviewColumns, aggregation.Rows))
        {
            values[pair.Key] = pair.Value;
        }

        return values;
    }

    private static ProductHistoryTableRow FlattenSinglePointProductRow(
        ProductHistoryTableRow productRow,
        ProductHistoryTableRow pointRow)
    {
        return new ProductHistoryTableRow
        {
            IsProductRow = true,
            TaskId = productRow.TaskId,
            StationNo = productRow.StationNo,
            ProductNo = productRow.ProductNo,
            TouchNo = pointRow.TouchNo,
            NodeText = productRow.NodeText,
            ResultText = productRow.ResultText,
            UploadStatusText = productRow.UploadStatusText,
            ShowTestFlag = productRow.ShowTestFlag,
            IsTest = productRow.IsTest,
            IsTestText = productRow.IsTestText,
            TouchCountText = productRow.TouchCountText,
            RecordTimeText = pointRow.RecordTimeText,
            DynamicValues = pointRow.DynamicValues,
            CanMarkTest = productRow.CanMarkTest,
            MarkDisabledReason = productRow.MarkDisabledReason
        };
    }

    /// <summary>
    /// 处理到产品历史Point行。
    /// </summary>
    /// <param name="product">产品历史数据。</param>
    /// <param name="point">焊点历史数据。</param>
    /// <param name="dynamicColumns">动态列集合。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private ProductHistoryTableRow ToProductHistoryPointRow(ProductHistoryProduct product,
        ProductHistoryPoint point,
        IReadOnlyList<ProductHistoryDynamicColumn> dynamicColumns,
        ProductHistoryDisplayOptions displayOptions)
    {
        return new ProductHistoryTableRow
        {
            IsProductRow = false,
            TaskId = product.TaskId,
            StationNo = product.StationNo,
            ProductNo = product.ProductNo,
            TouchNo = point.TouchNo,
            NodeText = $"{displayOptions.PointName} {point.TouchNo}",
            ResultText = FormatHistoryResult(point.Result),
            UploadStatusText = FormatHistoryUploadStatus(point.UploadStatus),
            ShowTestFlag = displayOptions.ShowTestFlagInHistory,
            IsTest = point.IsTest,
            IsTestText = FormatHistoryTestFlag(point.IsTest),
            RecordTimeText = FormatHistoryTime(point.RecordTime),
            DynamicValues = BuildProductHistoryDynamicValues(point, dynamicColumns),
            CanMarkTest = product.CanMarkTest,
            MarkDisabledReason = product.MarkDisabledReason
        };
    }

    /// <summary>
    /// 构建产品历史动态值。
    /// </summary>
    /// <param name="point">焊点历史数据。</param>
    /// <param name="dynamicColumns">动态列集合。</param>
    /// <returns>解析后的集合。</returns>
    private Dictionary<string, string> BuildProductHistoryDynamicValues(ProductHistoryPoint point, IReadOnlyList<ProductHistoryDynamicColumn> dynamicColumns)
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

    /// <summary>
    /// 查找产品历史动态原始值用于历史。
    /// </summary>
    /// <param name="point">焊点历史数据。</param>
    /// <param name="rawValues">原始采集值集合。</param>
    /// <param name="column">动态列定义。</param>
    /// <returns>处理后的文本。</returns>
    private static string? FindProductHistoryDynamicRawValueForHistory(ProductHistoryPoint point, IReadOnlyDictionary<string, string> rawValues,
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

    /// <summary>
    /// 查找产品历史固定值。
    /// </summary>
    /// <param name="point">焊点历史数据。</param>
    /// <param name="column">动态列定义。</param>
    /// <returns>处理后的文本。</returns>
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

    /// <summary>
    /// 解析产品历史动态Columns。
    /// </summary>
    /// <param name="activeTask">active任务。</param>
    /// <param name="snapshot">状态快照。</param>
    /// <returns>解析后的集合。</returns>
    private IReadOnlyList<ProductHistoryDynamicColumn> ResolveProductHistoryDynamicColumns(BizWeldTask activeTask, ProductHistorySnapshot snapshot)
    {
        if (IsWholePieceMergedPreview())
        {
            // 合并视图下历史表与实时预览使用同一组列，保证界面口径一致。
            return _mergedPreviewColumns
                .Select((column, index) => new ProductHistoryDynamicColumn(
                    column.ColumnName,
                    column.ColumnName,
                    column.ColumnName,
                    column.ItemName,
                    PreviewActualRole,
                    (index + 1) * 10))
                .ToList();
        }

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

    /// <summary>
    /// 解析产品历史工序Config。
    /// </summary>
    /// <param name="activeTask">active任务。</param>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
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

    private ProductHistoryDisplayOptions ResolveProductHistoryDisplayOptions(BizWeldTask activeTask, int stationNo)
    {
        var config = ResolveProductHistoryProcessConfig(activeTask, stationNo);
        var showTestFlagInHistory = _currentSettings.ShowTestFlagInHistory != false;
        return config is null
            ? ProductHistoryDisplayOptions.Default with { ShowTestFlagInHistory = showTestFlagInHistory }
            : ProductHistoryDisplayOptions.FromConfig(config, showTestFlagInHistory);
    }

    /// <summary>
    /// 解析产品历史动态Columns从实时预览。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析后的集合。</returns>
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

    /// <summary>
    /// 解析产品历史动态Columns从快照。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
    /// <returns>解析后的集合。</returns>
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

    /// <summary>
    /// 尝试解析产品历史原始列用于历史。
    /// </summary>
    /// <param name="rawKey">原始字段键。</param>
    /// <param name="itemKey">测试项键。</param>
    /// <param name="itemName">测试项名称。</param>
    /// <param name="role">字段角色。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private static bool TryResolveProductHistoryRawColumnForHistory(string rawKey, out string itemKey, out string itemName, out string role)
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

    /// <summary>
    /// 判断产品历史原始键Ignored。
    /// </summary>
    /// <param name="key">键。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private static bool IsProductHistoryRawKeyIgnored(string key)
    {
        return string.Equals(key, "product_result", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "test_result", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "test_result_raw", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "TestResult", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "Result", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 解析产品历史已知Item键用于历史。
    /// </summary>
    /// <param name="itemKey">测试项键。</param>
    /// <param name="itemName">测试项名称。</param>
    /// <returns>处理后的文本。</returns>
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

    /// <summary>
    /// 解析产品历史Item名称用于历史。
    /// </summary>
    /// <param name="itemKey">测试项键。</param>
    /// <param name="fallbackName">兜底显示名称。</param>
    /// <returns>处理后的文本。</returns>
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

    /// <summary>
    /// 判断已知产品历史Item键。
    /// </summary>
    /// <param name="itemKey">测试项键。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private static bool IsKnownProductHistoryItemKey(string itemKey)
    {
        return string.Equals(itemKey, "max_electric", StringComparison.OrdinalIgnoreCase)
            || string.Equals(itemKey, "max_voltage", StringComparison.OrdinalIgnoreCase)
            || string.Equals(itemKey, "valid_power", StringComparison.OrdinalIgnoreCase)
            || string.Equals(itemKey, "displacement", StringComparison.OrdinalIgnoreCase)
            || string.Equals(itemKey, "weld_ts", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 创建产品历史动态Columns。
    /// </summary>
    /// <param name="candidate">动态列候选项。</param>
    /// <returns>解析后的集合。</returns>
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
                SchemeDetailRoleRules.GetDefaultHeader(candidate.ItemName, SchemeDetailValueRole.Actual),
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

    /// <summary>
    /// 创建产品历史动态Columns。
    /// </summary>
    /// <param name="previewItem">预览Item。</param>
    /// <returns>解析后的集合。</returns>
    private static IEnumerable<ProductHistoryDynamicColumn> CreateProductHistoryDynamicColumns(WeldPreviewItem previewItem)
    {
        if (previewItem.EnableUpper)
        {
            yield return CreateProductHistoryDynamicColumn(
                previewItem.Key,
                previewItem.Name,
                PreviewUpperRole,
                NormalizeDisplayText(previewItem.UpperHeader, $"{previewItem.Name}上限"),
                previewItem.Sort + 1);
        }

        if (previewItem.EnableLower)
        {
            yield return CreateProductHistoryDynamicColumn(
                previewItem.Key,
                previewItem.Name,
                PreviewLowerRole,
                NormalizeDisplayText(previewItem.LowerHeader, $"{previewItem.Name}下限"),
                previewItem.Sort + 2);
        }

        if (previewItem.EnableActual)
        {
            yield return CreateProductHistoryDynamicColumn(
                previewItem.Key,
                previewItem.Name,
                PreviewActualRole,
                SchemeDetailRoleRules.ResolveHeader(previewItem.ActualHeader, previewItem.Name, SchemeDetailValueRole.Actual),
                previewItem.Sort + 3);
        }

        if (previewItem.EnableResult)
        {
            yield return CreateProductHistoryDynamicColumn(
                previewItem.Key,
                previewItem.Name,
                PreviewResultRole,
                NormalizeDisplayText(previewItem.ResultHeader, $"{previewItem.Name}结果"),
                previewItem.Sort + 4);
        }
    }

    /// <summary>
    /// 创建产品历史动态Columns从Scheme。
    /// </summary>
    /// <param name="schemeItem">测试方案项。</param>
    /// <returns>解析后的集合。</returns>
    private static IEnumerable<ProductHistoryDynamicColumn> CreateProductHistoryDynamicColumnsFromScheme(SchemePreviewItem schemeItem)
    {
        var item = schemeItem.Item;
        var detail = schemeItem.Detail;
        var itemKey = ResolveItemKey(item);
        var itemName = item.ItemName?.Trim() ?? itemKey;

        if (SchemeDetailRoleRules.ShouldShowHistoryRole(detail, SchemeDetailValueRole.Upper))
        {
            yield return CreateProductHistoryDynamicColumn(itemKey, itemName, PreviewUpperRole, SchemeDetailRoleRules.ResolveHeader(detail, item, SchemeDetailValueRole.Upper), schemeItem.Sort + 1);
        }

        if (SchemeDetailRoleRules.ShouldShowHistoryRole(detail, SchemeDetailValueRole.Lower))
        {
            yield return CreateProductHistoryDynamicColumn(itemKey, itemName, PreviewLowerRole, SchemeDetailRoleRules.ResolveHeader(detail, item, SchemeDetailValueRole.Lower), schemeItem.Sort + 2);
        }

        if (SchemeDetailRoleRules.ShouldShowHistoryRole(detail, SchemeDetailValueRole.Actual))
        {
            yield return CreateProductHistoryDynamicColumn(itemKey, itemName, PreviewActualRole, SchemeDetailRoleRules.ResolveHeader(detail, item, SchemeDetailValueRole.Actual), schemeItem.Sort + 3);
        }

        if (SchemeDetailRoleRules.ShouldShowHistoryRole(detail, SchemeDetailValueRole.Result))
        {
            yield return CreateProductHistoryDynamicColumn(itemKey, itemName, PreviewResultRole, SchemeDetailRoleRules.ResolveHeader(detail, item, SchemeDetailValueRole.Result), schemeItem.Sort + 4);
        }
    }

    /// <summary>
    /// 创建产品历史动态列。
    /// </summary>
    /// <param name="itemKey">测试项键。</param>
    /// <param name="itemName">测试项名称。</param>
    /// <param name="role">字段角色。</param>
    /// <param name="title">标题文本。</param>
    /// <param name="sort">排序。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private static ProductHistoryDynamicColumn CreateProductHistoryDynamicColumn(string itemKey, string itemName, string role, string title, int sort)
    {
        return new ProductHistoryDynamicColumn(
            $"{itemKey}_{role}",
            title,
            itemKey,
            itemName,
            role,
            sort);
    }

    /// <summary>
    /// 构建产品历史结构键。
    /// </summary>
    /// <param name="dynamicColumns">动态列集合。</param>
    /// <returns>处理后的文本。</returns>
    private static string BuildProductHistorySchemaKey(
        IReadOnlyList<ProductHistoryDynamicColumn> dynamicColumns,
        ProductHistoryDisplayOptions displayOptions)
    {
        var displayKey = string.Join('\u001E',
            displayOptions.PointName,
            displayOptions.PointNoHeader,
            displayOptions.PointResultHeader,
            displayOptions.PointCountHeader,
            displayOptions.ShowTestFlagInHistory);
        var dynamicKey = dynamicColumns.Count == 0
            ? "base"
            : string.Join("|", dynamicColumns.Select(column => $"{column.Key}:{column.Title}:{column.Role}:{column.Sort}"));
        return $"{displayKey}|{dynamicKey}";
    }

    /// <summary>
    /// 格式化历史上传状态。
    /// </summary>
    /// <param name="status">目标状态值。</param>
    /// <returns>处理后的文本。</returns>
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

    /// <summary>
    /// 格式化历史结果。
    /// </summary>
    /// <param name="result">结果。</param>
    /// <returns>处理后的文本。</returns>
    private static string FormatHistoryResult(string result)
    {
        return string.IsNullOrWhiteSpace(result) || string.Equals(result, ProductionConstants.TestResults.Unknown, StringComparison.OrdinalIgnoreCase)
            ? "--"
            : result;
    }

    /// <summary>
    /// 格式化历史测试标记。
    /// </summary>
    /// <param name="isTest">是否标记为试焊件。</param>
    /// <returns>处理后的文本。</returns>
    private static string FormatHistoryTestFlag(bool isTest)
    {
        return isTest ? "试焊件" : "--";
    }

    /// <summary>
    /// 格式化历史时间。
    /// </summary>
    /// <param name="time">时间。</param>
    /// <returns>处理后的文本。</returns>
    private static string FormatHistoryTime(DateTime? time)
    {
        return time?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "--";
    }

    #endregion

    #region 焊接参数预览

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

    /// <summary>
    /// 绑定焊接参数表格。
    /// </summary>
    /// <param name="forceRebind">forceRebind。</param>
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
    /// 刷新焊接参数表格。
    /// </summary>
    private void RefreshWeldParameterTable()
    {
        RefreshWeldParameterRows();
        _weldParameterLayoutKey = BuildPreviewLayoutKey(_weldParameterRows);
        _weldParameterVisibleValueKey = BuildPreviewValueKey(_weldParameterRows);
    }

    /// <summary>
    /// 刷新焊接参数行。
    /// </summary>
    private void RefreshWeldParameterRows()
    {
        if (!_weldParameterTableBound)
        {
            BindWeldParameterTable(forceRebind: true);
            return;
        }

        var items = ResolveWeldPreviewItems(_weldParameterRows);
        var layoutKey = BuildPreviewLayoutKey(_weldParameterRows);
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

        // 合并视图只有一行，行数与四面分组对不上，不走下面的逐面填充，否则每帧都会整表重建。
        if (IsWholePieceMergedPreview())
        {
            FillMergedPreviewRow();
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
    /// 处理Rebuild焊接参数预览表格。
    /// </summary>
    private void RebuildWeldParameterPreviewTable()
    {
        var grid = CurrentWeldPreviewGrid;
        if (_weldParameterRows.Count == 0 && !IsWholePieceMergedPreview())
        {
            // 没有任何预览行时不建列，否则未开工和完工上报后会残留空表头。
            ClearWeldPreviewGrid(grid);
            _weldParameterLayoutKey = string.Empty;
            _weldParameterPreviewSchemaKey = string.Empty;
            _weldParameterVisibleValueKey = string.Empty;
            _weldParameterTableBound = false;
            return;
        }

        var items = ResolveWeldPreviewItems(_weldParameterRows);
        var displayOptions = ResolveWeldPreviewDisplayOptions(_weldParameterRows);
        SetControlRedraw(grid, enabled: false);
        grid.SuspendLayout();
        try
        {
            grid.Rows.Clear();
            grid.Columns.Clear();

            if (IsWholePieceMergedPreview())
            {
                // 合并视图不再有面号和面结果，只显示 A/B 聚合后的一行。
                for (var index = 0; index < _mergedPreviewColumns.Count; index++)
                {
                    AddWeldPreviewColumn(BuildMergedPreviewColumnName(index), _mergedPreviewColumns[index].ColumnName, 136);
                }

                FillMergedPreviewRow();
            }
            else
            {
                AddWeldPreviewColumn(PreviewTouchNoColumn, displayOptions.PointNoHeader, 86);
                if (ShouldShowFaceResultColumn())
                {
                    AddWeldPreviewColumn(PreviewTouchResultColumn, displayOptions.PointResultHeader, 86);
                }
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
                            AddWeldPreviewColumn(BuildPreviewColumnName(item.Index, PreviewUpperRole), ResolvePreviewColumnHeader(item, SchemeDetailValueRole.Upper), 118);
                        }

                        if (item.EnableLower)
                        {
                            AddWeldPreviewColumn(BuildPreviewColumnName(item.Index, PreviewLowerRole), ResolvePreviewColumnHeader(item, SchemeDetailValueRole.Lower), 118);
                        }

                        if (item.EnableActual)
                        {
                            AddWeldPreviewColumn(BuildPreviewColumnName(item.Index, PreviewActualRole), ResolvePreviewColumnHeader(item, SchemeDetailValueRole.Actual), 136);
                        }

                        if (item.EnableResult)
                        {
                            AddWeldPreviewColumn(BuildPreviewColumnName(item.Index, PreviewResultRole), ResolvePreviewColumnHeader(item, SchemeDetailValueRole.Result), 118);
                        }
                    }

                    FillWeldPreviewRows(items, ResolvePreviewTouchGroups(_weldParameterRows));
                }
            }

            _weldParameterLayoutKey = BuildPreviewLayoutKey(_weldParameterRows);
            _weldParameterPreviewSchemaKey = BuildWeldPreviewSchemaKey(items);
            _weldParameterVisibleValueKey = BuildPreviewValueKey(_weldParameterRows);
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

    /// <summary>
    /// 清空实时预览表格的行与列，并同步隐藏水平滚动条。
    /// </summary>
    /// <param name="grid">目标表格控件。</param>
    private void ClearWeldPreviewGrid(DataGridView grid)
    {
        if (grid.Rows.Count > 0)
        {
            grid.Rows.Clear();
        }

        if (grid.Columns.Count > 0)
        {
            grid.Columns.Clear();
        }

        SyncWeldPreviewHorizontalScrollBar(grid);
    }

    /// <summary>
    /// 设置控件重绘。
    /// </summary>
    /// <param name="control">目标控件。</param>
    /// <param name="enabled">是否启用。</param>
    private static void SetControlRedraw(Control control, bool enabled)
    {
        if (!control.IsHandleCreated)
        {
            return;
        }

        SendMessage(control.Handle, WmSetRedraw, enabled ? new IntPtr(1) : IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>
    /// 处理RedrawControl。
    /// </summary>
    /// <param name="control">目标控件。</param>
    private static void RedrawControl(Control control)
    {
        control.Invalidate();
        control.Update();
    }

    /// <summary>
    /// 设置垂直分隔条Panel2到Min宽度。
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

    /// <summary>
    /// 设置焊接预览水平偏移。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <param name="requestedOffset">请求设置的水平偏移量。</param>
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

    /// <summary>
    /// 处理Sync焊接预览水平滚动Bar。
    /// </summary>
    private void SyncWeldPreviewHorizontalScrollBar()
        => SyncWeldPreviewHorizontalScrollBar(CurrentWeldPreviewGrid);

    /// <summary>
    /// 处理Sync焊接预览水平滚动Bar。
    /// </summary>
    /// <param name="sourceGrid">触发同步的源表格。</param>
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

    /// <summary>
    /// 获取焊接预览Max水平偏移。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析或计算后的数值。</returns>
    private int GetWeldPreviewMaxHorizontalOffset(int stationNo)
    {
        return Math.Max(0, GetWeldPreviewContentWidth(stationNo) - GetWeldPreviewViewportWidth(stationNo));
    }

    /// <summary>
    /// 获取焊接预览内容宽度。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析或计算后的数值。</returns>
    private int GetWeldPreviewContentWidth(int stationNo)
    {
        return GetWeldPreviewGrid(stationNo).Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Visible)
            .Sum(column => column.Width);
    }

    /// <summary>
    /// 获取焊接预览视口宽度。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析或计算后的数值。</returns>
    private int GetWeldPreviewViewportWidth(int stationNo)
    {
        var grid = GetWeldPreviewGrid(stationNo);
        return Math.Max(0, grid.ClientSize.Width - (grid.RowHeadersVisible ? grid.RowHeadersWidth : 0));
    }

    /// <summary>
    /// 添加焊接预览列。
    /// </summary>
    /// <param name="columnName">列名。</param>
    /// <param name="headerText">列标题。</param>
    /// <param name="width">列宽度。</param>
    /// <summary>
    /// 解析实时预览列标题，并按统一规则追加测试项单位。
    /// </summary>
    /// <param name="item">预览项。</param>
    /// <param name="role">字段角色。</param>
    /// <returns>处理后的文本。</returns>
    private static string ResolvePreviewColumnHeader(WeldPreviewItem item, SchemeDetailValueRole role)
    {
        var header = role switch
        {
            SchemeDetailValueRole.Upper => item.UpperHeader,
            SchemeDetailValueRole.Lower => item.LowerHeader,
            SchemeDetailValueRole.Result => item.ResultHeader,
            _ => item.ActualHeader
        };
        return TestItemUnitFormatRules.FormatHeader(
            SchemeDetailRoleRules.ResolveHeader(header, item.Name, role),
            item.Unit,
            role);
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

    /// <summary>
    /// 确保信息预览行。
    /// </summary>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private bool EnsureInfoPreviewRows()
    {
        return CurrentWeldPreviewGrid.Rows.Count != _weldParameterRows.Count;
    }

    /// <summary>
    /// 填充信息预览行。
    /// </summary>
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

    /// <summary>
    /// 填充焊接预览行。
    /// </summary>
    /// <param name="items">预览项集合。</param>
    /// <param name="touchGroups">按焊点分组的行集合。</param>
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

    /// <summary>
    /// 面结果列只在整件检测的逐面模式下允许隐藏；其他设备类型始终显示该列。
    /// </summary>
    private bool ShouldShowFaceResultColumn()
        => !string.Equals(
               _currentSettings.ProcessParameterDeviceType?.Trim(),
               ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
               StringComparison.OrdinalIgnoreCase)
           || _currentSettings.IsWholePieceFaceResultDisplayEnabled;

    /// <summary>
    /// 设置预览值。
    /// </summary>
    /// <param name="rowIndex">行索引。</param>
    /// <param name="columnName">列名。</param>
    /// <param name="value">待处理值。</param>
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
    /// 应用预览结果CellStyle。
    /// </summary>
    /// <param name="cell">目标单元格。</param>
    /// <param name="columnName">列名。</param>
    /// <param name="value">待处理值。</param>
    private static void ApplyPreviewResultCellStyle(DataGridViewCell cell, string columnName, string value)
    {
        if (!IsPreviewResultColumn(columnName))
        {
            return;
        }

        var normalizedValue = TestResultRules.Normalize(value);
        if (TestResultRules.IsOk(normalizedValue))
        {
            SetPreviewResultCellColor(cell, UiColors.Status.Success);
            return;
        }

        if (TestResultRules.IsFailed(normalizedValue))
        {
            SetPreviewResultCellColor(cell, UiColors.Status.Danger);
            return;
        }

        ResetPreviewCellStyle(cell);
    }

    /// <summary>
    /// 判断预览结果列。
    /// </summary>
    /// <param name="columnName">列名。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private static bool IsPreviewResultColumn(string columnName)
    {
        return string.Equals(columnName, PreviewTouchResultColumn, StringComparison.Ordinal);
    }

    /// <summary>
    /// 设置预览结果Cell颜色。
    /// </summary>
    /// <param name="cell">目标单元格。</param>
    /// <param name="backColor">背景颜色。</param>
    private static void SetPreviewResultCellColor(DataGridViewCell cell, Color backColor)
    {
        cell.Style.BackColor = backColor;
        cell.Style.ForeColor = Color.White;
        cell.Style.SelectionBackColor = backColor;
        cell.Style.SelectionForeColor = Color.White;
    }

    /// <summary>
    /// 处理Reset预览单元格Style。
    /// </summary>
    /// <param name="cell">目标单元格。</param>
    private static void ResetPreviewCellStyle(DataGridViewCell cell)
    {
        cell.Style.BackColor = Color.Empty;
        cell.Style.ForeColor = Color.Empty;
        cell.Style.SelectionBackColor = Color.Empty;
        cell.Style.SelectionForeColor = Color.Empty;
    }

    /// <summary>
    /// 确保预览行Count。
    /// </summary>
    /// <param name="rowCount">目标行数。</param>
    /// <summary>
    /// 是否按四面整件检测的合并视图显示实时预览。
    /// 列结构由实时预览快照提供，开关关闭或非四面整件检测时保持逐面显示。
    /// </summary>
    private bool IsWholePieceMergedPreview()
        => _currentSettings.IsWholePieceMergedDisplayEnabled && _mergedPreviewColumns.Count > 0;

    private static string BuildMergedPreviewColumnName(int index)
        => $"merged_{index.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    /// 填充合并视图的唯一一行。四面未采集齐时快照没有值，此时留空行。
    /// 超出程序设定值的列标红，让操作员直接看出是哪一项 NG。
    /// </summary>
    private void FillMergedPreviewRow()
    {
        EnsurePreviewRowCount(1);
        var grid = CurrentWeldPreviewGrid;
        for (var index = 0; index < _mergedPreviewColumns.Count; index++)
        {
            var column = _mergedPreviewColumns[index];
            _mergedPreviewValues.TryGetValue(column.ColumnName, out var value);
            var columnName = BuildMergedPreviewColumnName(index);
            SetPreviewValue(0, columnName, value ?? string.Empty);
            if (grid.Rows.Count == 0 || !grid.Columns.Contains(columnName))
            {
                continue;
            }

            // 必须显式还原，否则上一件产品的红色会留在格子里。
            var cell = grid.Rows[0].Cells[columnName];
            if (IsMergedFailedColumn(column.ColumnName))
            {
                SetPreviewResultCellColor(cell, UiColors.Status.Danger);
            }
            else
            {
                ResetPreviewCellStyle(cell);
            }
        }
    }

    private bool IsMergedFailedColumn(string columnName)
    {
        for (var index = 0; index < _mergedFailedColumns.Count; index++)
        {
            if (string.Equals(_mergedFailedColumns[index], columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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

    /// <summary>
    /// 解析焊接预览Items。
    /// </summary>
    /// <param name="rows">行数据集合。</param>
    /// <returns>解析后的集合。</returns>
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
                EnableResult = group.Any(row => row.EnableResult),
                ActualHeader = group.Select(row => row.ActualHeader).FirstOrDefault(header => !string.IsNullOrWhiteSpace(header)) ?? string.Empty,
                UpperHeader = group.Select(row => row.UpperHeader).FirstOrDefault(header => !string.IsNullOrWhiteSpace(header)) ?? string.Empty,
                LowerHeader = group.Select(row => row.LowerHeader).FirstOrDefault(header => !string.IsNullOrWhiteSpace(header)) ?? string.Empty,
                ResultHeader = group.Select(row => row.ResultHeader).FirstOrDefault(header => !string.IsNullOrWhiteSpace(header)) ?? string.Empty,
                Unit = group.Select(row => row.Unit).FirstOrDefault(unit => !string.IsNullOrWhiteSpace(unit)) ?? string.Empty
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
                item.EnableResult,
                item.ActualHeader,
                item.UpperHeader,
                item.LowerHeader,
                item.ResultHeader,
                item.Unit))
            .ToList();

        return items.Count == 0
            ? Array.Empty<WeldPreviewItem>()
            : items;
    }

    /// <summary>
    /// 解析预览焊点分组。
    /// </summary>
    /// <param name="rows">行数据集合。</param>
    /// <returns>解析后的集合。</returns>
    private static IReadOnlyList<IGrouping<int, WeldParameterRow>> ResolvePreviewTouchGroups(IEnumerable<WeldParameterRow> rows)
    {
        return rows
            .Where(row => row.TouchIndex > 0)
            .Where(row => !string.IsNullOrWhiteSpace(row.ItemKey))
            .GroupBy(row => row.TouchIndex)
            .OrderBy(group => group.Key)
            .ToList();
    }

    /// <summary>
    /// 判断信息预览。
    /// </summary>
    /// <param name="items">预览项集合。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private static bool IsInfoPreview(IReadOnlyList<WeldPreviewItem> items)
    {
        return items.Count == 0;
    }

    /// <summary>
    /// 构建焊接预览结构键。
    /// </summary>
    /// <param name="items">预览项集合。</param>
    /// <returns>处理后的文本。</returns>
    private static string BuildWeldPreviewSchemaKey(IReadOnlyList<WeldPreviewItem> items)
    {
        return items.Count == 0
            ? "info"
            : string.Join("|", items.Select(item =>
                $"{item.Index}:{item.Key}:{item.Name}:{item.EnableActual}:{item.EnableUpper}:{item.EnableLower}:{item.EnableResult}:{item.ActualHeader}:{item.UpperHeader}:{item.LowerHeader}:{item.ResultHeader}:{item.Unit}"));
    }

    /// <summary>
    /// 预览布局键。合并视图的列结构也要参与比较，否则每帧都会误判为布局变化而重建表格。
    /// </summary>
    private string BuildPreviewLayoutKey(IEnumerable<WeldParameterRow> rows)
        => BuildWeldPreviewLayoutKey(rows) + BuildMergedPreviewLayoutKey();

    /// <summary>
    /// 预览取值键，合并视图的取值同样参与比较。
    /// </summary>
    private string BuildPreviewValueKey(IEnumerable<WeldParameterRow> rows)
        => BuildWeldPreviewVisibleValueKey(rows) + BuildMergedPreviewValueKey();

    /// <summary>
    /// 合并视图的列结构指纹。未启用合并显示时返回空串，逐面显示的刷新判断保持原样。
    /// </summary>
    private string BuildMergedPreviewLayoutKey()
        => IsWholePieceMergedPreview()
            ? "|merged:" + string.Join('', _mergedPreviewColumns.Select(column => column.ColumnName))
            : string.Empty;

    private string BuildMergedPreviewValueKey()
        => IsWholePieceMergedPreview()
            ? "|merged:" + string.Join('', _mergedPreviewColumns.Select(column =>
                _mergedPreviewValues.TryGetValue(column.ColumnName, out var value) ? value : string.Empty))
                + "|ng:" + string.Join("|", _mergedFailedColumns)
            : string.Empty;

    /// <summary>
    /// 构建焊接预览布局键。
    /// </summary>
    /// <param name="rows">行数据集合。</param>
    /// <returns>处理后的文本。</returns>
    private static string BuildWeldPreviewLayoutKey(IEnumerable<WeldParameterRow> rows)
    {
        var materializedRows = rows.ToList();
        var items = ResolveWeldPreviewItems(materializedRows);
        var displayOptions = ResolveWeldPreviewDisplayOptions(materializedRows);
        var rowCount = IsInfoPreview(items)
            ? materializedRows.Count
            : ResolvePreviewTouchGroups(materializedRows).Count;
        return $"{displayOptions.PointNoHeader}:{displayOptions.PointResultHeader}|{BuildWeldPreviewSchemaKey(items)}|rows:{rowCount}";
    }

    private static ProductHistoryDisplayOptions ResolveWeldPreviewDisplayOptions(IEnumerable<WeldParameterRow> rows)
    {
        var row = rows.FirstOrDefault(item =>
            !string.IsNullOrWhiteSpace(item.PointName)
            || !string.IsNullOrWhiteSpace(item.PointNoHeader)
            || !string.IsNullOrWhiteSpace(item.PointResultHeader)
            || !string.IsNullOrWhiteSpace(item.PointCountHeader));
        if (row is null)
        {
            return ProductHistoryDisplayOptions.Default;
        }

        var pointName = NormalizeDisplayText(row.PointName, ProductHistoryDisplayOptions.Default.PointName);
        return new ProductHistoryDisplayOptions(
            pointName,
            NormalizeDisplayText(row.PointNoHeader, $"{pointName}序号"),
            NormalizeDisplayText(row.PointResultHeader, $"{pointName}结果"),
            NormalizeDisplayText(row.PointCountHeader, $"{pointName}数"),
            0,
            true);
    }

    /// <summary>
    /// 构建焊接预览可见值键。
    /// </summary>
    /// <param name="rows">行数据集合。</param>
    /// <returns>处理后的文本。</returns>
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
                row.PointName,
                row.PointNoHeader,
                row.PointResultHeader,
                row.PointCountHeader,
                row.ItemKey,
                row.ParameterName,
                row.ActualHeader,
                row.UpperHeader,
                row.LowerHeader,
                row.ResultHeader,
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

    /// <summary>
    /// 构建预览列名称。
    /// </summary>
    /// <param name="itemIndex">item索引。</param>
    /// <param name="role">字段角色。</param>
    /// <returns>处理后的文本。</returns>
    private static string BuildPreviewColumnName(int itemIndex, string role)
    {
        return $"Item{itemIndex}_{role}";
    }

    /// <summary>
    /// 比较预览Item。
    /// </summary>
    /// <param name="row">表格行数据。</param>
    /// <param name="item">测试项。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private static bool SamePreviewItem(WeldParameterRow row, WeldPreviewItem item)
    {
        return string.Equals(row.ItemKey, item.Key, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 判断焊点结果行。
    /// </summary>
    /// <param name="row">表格行数据。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private static bool IsTouchResultRow(WeldParameterRow row)
    {
        return string.Equals(row.ItemKey, "test_result", StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.ParameterName, "焊点结果", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 判断方案明细是否至少启用一个实时预览角色。
    /// </summary>
    /// <param name="detail">详情。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private static bool HasAnyPreviewEnabled(BizSchemeDetail detail)
    {
        return SchemeDetailRoleRules.HasAnyPreviewEnabled(detail);
    }

    /// <summary>
    /// 解析预览焊点号。
    /// </summary>
    /// <param name="rows">行数据集合。</param>
    /// <returns>处理后的文本。</returns>
    private static string ResolvePreviewTouchNo(IEnumerable<WeldParameterRow> rows)
    {
        var first = rows.OrderBy(row => row.Sort).FirstOrDefault();
        return DisplayPreviewValue(first?.TouchNo);
    }

    /// <summary>
    /// 解析预览焊点结果。
    /// </summary>
    /// <param name="rows">行数据集合。</param>
    /// <returns>处理后的文本。</returns>
    private static string ResolvePreviewTouchResult(IEnumerable<WeldParameterRow> rows)
    {
        var explicitResult = rows
            .Select(row => row.TouchResult)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value) && value != "--");
        return DisplayPreviewValue(explicitResult);
    }

    /// <summary>
    /// 处理Display预览值。
    /// </summary>
    /// <param name="value">待处理值。</param>
    /// <returns>处理后的文本。</returns>
    private static string DisplayPreviewValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || value == "--"
            ? string.Empty
            : value.Trim();
    }

    #endregion

    #region 实时预览

    /// <summary>
    /// 应用当前工位缓存的实时预览快照。
    /// </summary>
    private void ApplyCurrentRealtimePreviewSnapshot()
    {
        var snapshot = _productRealtimePreviewService.GetCurrent(CurrentStationNo);
        if (snapshot is null || !CanDisplayRealtimePreviewSnapshot(snapshot))
        {
            ClearCurrentRealtimePreviewDisplay();
            return;
        }

        ApplyProductRealtimePreviewSnapshot(snapshot);
    }

    /// <summary>
    /// 应用产品实时预览快照，刷新摘要和焊接参数行。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
    private void ApplyProductRealtimePreviewSnapshot(ProductRealtimePreviewSnapshot snapshot)
    {
        if (!CanDisplayRealtimePreviewSnapshot(snapshot))
        {
            ClearCurrentRealtimePreviewDisplay();
            return;
        }

        var productChanged = HasRealtimeProductChanged(snapshot);
        ApplyLivePreviewSummary(snapshot, productChanged);
        _currentProductIdentity = new ProductIdentity(snapshot.StationNo, snapshot.ProductNum, snapshot.ProductModel, "RealtimePreview");
        _mergedPreviewColumns = snapshot.MergedColumns;
        _mergedPreviewValues = snapshot.MergedValues;
        _mergedPreviewDefinitions = snapshot.MergedDefinitions;
        _mergedFailedColumns = snapshot.MergedFailedColumns;

        if (snapshot.Rows.Count == 0 && CurrentWeldPreviewGrid.Rows.Count > 0)
        {
            // 后台短暂读空时保留上一帧明细，避免实时表格被瞬间清空造成闪烁。
            return;
        }

        ApplyRealtimeWeldParameterRows(snapshot.Rows);
    }

    private bool CanDisplayRealtimePreviewSnapshot(ProductRealtimePreviewSnapshot snapshot)
    {
        var activeTask = GetCurrentStationState().ActiveTask;
        return snapshot.StationNo == CurrentStationNo
            && IsRunningWeldTask(activeTask)
            && snapshot.RefreshTime >= activeTask!.StartTime;
    }

    private void ClearCurrentRealtimePreviewDisplay()
    {
        lock (_realtimePreviewSync)
        {
            _pendingRealtimePreviewSnapshot = null;
            _realtimePreviewApplyPosted = false;
        }

        _currentProductIdentity = null;
        _lastRealtimeProductNumbers.Remove(CurrentStationNo);
        _lastSchemePreviewKey = string.Empty;
        _mergedPreviewColumns = Array.Empty<WholePieceMergedColumn>();
        _mergedPreviewValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _mergedPreviewDefinitions = Array.Empty<WholePieceAbValueDefinition>();
        _mergedFailedColumns = Array.Empty<string>();
        _weldParameterRows.RemoveAll(row => row.StationNo == CurrentStationNo);
        _weldParameterLayoutKey = string.Empty;
        _weldParameterPreviewSchemaKey = string.Empty;
        _weldParameterVisibleValueKey = string.Empty;
        _weldParameterTableBound = false;

        // 未开工或完工后必须连列一起清空：只清行会残留“焊点序号/焊点结果/提示”空表头。
        ClearWeldPreviewGrid(CurrentWeldPreviewGrid);

        SetControlText(CurrentLivePreviewStatusLabel, string.Empty);
        SetControlText(CurrentLiveProductNoLabel, string.Empty);
        SetControlText(CurrentLiveTouchCountLabel, string.Empty);
        ApplyProgramLimitsDisplay();
        ApplyProductResultToGroup(CurrentStationNo, ProductionConstants.TestResults.NotAvailable);
    }

    private void ClearCurrentProductHistoryDisplay()
    {
        ConfigureProductHistoryTableColumns(
            CurrentProductHistoryTable,
            [],
            CurrentStationNo,
            ProductHistoryDisplayOptions.Default with
            {
                ShowTestFlagInHistory = _currentSettings.ShowTestFlagInHistory != false
            });
        BindProductHistoryRows(CurrentProductHistoryTable, []);
    }

    /// <summary>
    /// 应用实时预览摘要。
    /// </summary>
    /// <param name="snapshot">状态快照。</param>
    private void ApplyLivePreviewSummary(ProductRealtimePreviewSnapshot snapshot, bool productChanged)
    {
        var hasErrorMessage = !string.IsNullOrWhiteSpace(snapshot.Message);
        var statusLabel = CurrentLivePreviewStatusLabel;
        SetControlText(statusLabel, hasErrorMessage ? "实时采集异常" : "实时采集正常");
        statusLabel.ForeColor = hasErrorMessage ? UiColors.Status.Danger : UiColors.Status.Success;

        SetControlText(CurrentLiveProductNoLabel, $"产品编号：{FormatLiveSummaryValue(snapshot.ProductNo)}");
        SetControlText(CurrentLiveTouchCountLabel, $"{NormalizeDisplayText(snapshot.PointName, "焊点")}：{FormatLiveSummaryValue(snapshot.TouchCountText)}");
        ApplyProgramLimitsDisplay();
        ApplyProductResultToGroup(
            snapshot.StationNo,
            productChanged ? ProductionConstants.TestResults.NotAvailable : snapshot.ProductResult);
    }

    /// <summary>
    /// 在实时预览上方显示本次开工固化的程序最大允许值，方便现场核对参数。
    /// 取任务快照而非当前选中程序，保证显示值与产品判定使用同一份数据。
    /// 只对整件检测设备有意义，其余设备类型隐藏整个标签，不占布局宽度。
    /// </summary>
    private void ApplyProgramLimitsDisplay()
    {
        var visible = string.Equals(
            _currentSettings.ProcessParameterDeviceType?.Trim(),
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
            StringComparison.OrdinalIgnoreCase);
        lblLiveProgramLimits1.Visible = visible;
        if (!visible)
        {
            return;
        }

        var caption = _localizer.GetString(TextKeys.Monitor.Label.ProgramLimits);
        var summary = ProgramContentJsonRules.BuildLimitsSummary(
            GetCurrentStationState().ActiveTask?.ProgramContentSnapshot);
        SetControlText(
            lblLiveProgramLimits1,
            $"{caption}：{(string.IsNullOrWhiteSpace(summary) ? "--" : summary)}");
    }

    private bool HasRealtimeProductChanged(ProductRealtimePreviewSnapshot snapshot)
    {
        var productNo = FormatLiveSummaryValue(snapshot.ProductNo);
        var changed = !_lastRealtimeProductNumbers.TryGetValue(snapshot.StationNo, out var previousProductNo)
            || !string.Equals(previousProductNo, productNo, StringComparison.OrdinalIgnoreCase);
        _lastRealtimeProductNumbers[snapshot.StationNo] = productNo;
        return changed;
    }

    private void ApplyProductResultToGroup(int stationNo, string? productResult)
    {
        var tag = stationNo == 2 ? tagResult2 : tagResult1;
        var resultText = FormatLiveSummaryValue(productResult);
        tag.Text = $"工位{stationNo}{resultText}";
        tag.ForeColor = Color.White;
        tag.BackColor = ResolveStationResultColor(resultText);
    }

    private static string FormatLiveSummaryValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();
    }

    /// <summary>
    /// 将实时预览行转换为焊接参数行并刷新界面。
    /// </summary>
    /// <param name="snapshotRows">实时快照行集合。</param>
    private void ApplyRealtimeWeldParameterRows(IReadOnlyList<ProductRealtimePreviewRow> snapshotRows)
    {
        var nextRows = snapshotRows
            .OrderBy(row => row.Sort)
            .Select(ToWeldParameterRow)
            .ToList();
        ApplyWeldParameterRows(nextRows, preserveStableValues: false);
    }

    /// <summary>
    /// 应用新的焊接参数行，按布局变化决定重绑或局部刷新。
    /// </summary>
    /// <param name="nextRows">下一批行数据。</param>
    private void ApplyWeldParameterRows(
        IReadOnlyList<WeldParameterRow> nextRows,
        bool preserveStableValues = true)
    {
        if (preserveStableValues)
        {
            PreserveStablePreviewValues(nextRows);
        }
        var nextLayoutKey = BuildPreviewLayoutKey(nextRows);
        var nextVisibleValueKey = BuildPreviewValueKey(nextRows);
        var layoutChanged = !_weldParameterTableBound
            || !string.Equals(nextLayoutKey, _weldParameterLayoutKey, StringComparison.Ordinal);

        ReplaceWeldParameterRows(nextRows);
        if (layoutChanged)
        {
            // 列结构、焊点数量或测试项变化时必须重绑，否则表格列与数据会错位。
            BindWeldParameterTable(forceRebind: true);
            return;
        }

        if (string.Equals(nextVisibleValueKey, _weldParameterVisibleValueKey, StringComparison.Ordinal))
        {
            // 可见值没有变化时跳过刷新，降低高频 PLC 快照对 UI 的压力。
            return;
        }

        _weldParameterVisibleValueKey = nextVisibleValueKey;
        RefreshWeldParameterRows();
    }

    /// <summary>
    /// 替换焊接参数行。
    /// </summary>
    /// <param name="rows">行数据集合。</param>
    private void ReplaceWeldParameterRows(IEnumerable<WeldParameterRow> rows)
    {
        _weldParameterRows.Clear();
        _weldParameterRows.AddRange(rows);
        SortWeldParameterRows();
    }

    /// <summary>
    /// 在新快照缺少值时保留上一帧稳定值，减少界面闪烁。
    /// </summary>
    /// <param name="nextRows">下一批行数据。</param>
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

    /// <summary>
    /// 判断Empty预览值。
    /// </summary>
    /// <param name="value">待处理值。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private static bool IsEmptyPreviewValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "--", StringComparison.Ordinal);
    }

    /// <summary>
    /// 设置Control文本。
    /// </summary>
    /// <param name="control">目标控件。</param>
    /// <param name="text">显示文本。</param>
    private static void SetControlText(Control control, string? text)
    {
        var value = text ?? string.Empty;
        if (!string.Equals(control.Text, value, StringComparison.Ordinal))
        {
            control.Text = value;
        }
    }

    /// <summary>
    /// 处理到焊接参数行。
    /// </summary>
    /// <param name="row">表格行数据。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
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
            PointName = row.PointName,
            PointNoHeader = row.PointNoHeader,
            PointResultHeader = row.PointResultHeader,
            PointCountHeader = row.PointCountHeader,
            ParameterName = row.ItemName,
            Unit = row.Unit,
            EnableActual = row.EnableActual,
            EnableUpper = row.EnableUpper,
            EnableLower = row.EnableLower,
            EnableResult = row.EnableResult,
            ActualHeader = row.ActualHeader,
            UpperHeader = row.UpperHeader,
            LowerHeader = row.LowerHeader,
            ResultHeader = row.ResultHeader,
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

    /// <summary>
    /// 解析预览表达式绑定。
    /// </summary>
    /// <param name="baseAddress">PLC 基础地址。</param>
    /// <param name="contextOffset">相对基础地址的上下文偏移。</param>
    /// <param name="expression">PLC 表达式。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private PlcExpressionBinding ResolvePreviewExpressionBinding(string baseAddress, int contextOffset, string? expression)
    {
        if (_plcExpressionReadService.TryResolve(baseAddress, contextOffset, expression, out var binding, out _))
        {
            return binding;
        }

        var expressionText = expression?.Trim() ?? string.Empty;
        return new PlcExpressionBinding(expressionText, AppConstants.PlcDataTypes.Int16, 0, expressionText);
    }

    #endregion

    #region 方案预览与解析

    /// <summary>
    /// 排队刷新方案预览，并限制短时间内的重复刷新。
    /// </summary>
    /// <param name="force">是否强制刷新。</param>
    private void QueueRefreshSchemePreview(bool force)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (!IsRunningWeldTask(GetCurrentStationState().ActiveTask))
        {
            ClearCurrentRealtimePreviewDisplay();
            return;
        }

        if (!force && DateTime.Now - _lastSchemePreviewRefreshTime < TimeSpan.FromSeconds(2))
        {
            // 非强制刷新做短时间节流，避免 PLC/MES 状态频繁变化时反复重建预览表。
            return;
        }

        _lastSchemePreviewRefreshTime = DateTime.Now;
        _ = RefreshSchemePreviewAsync(force);
    }

    /// <summary>
    /// 异步解析产品身份并刷新当前工位方案预览。
    /// </summary>
    /// <param name="force">是否强制刷新。</param>
    /// <returns>表示异步操作的任务。</returns>
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
            var identity = ResolveOnlineProductIdentity(stationNo);
            if (identity is null && IsOfflineInputEditable(GetCurrentStationState()))
            {
                if (HasOfflineProgramSelectionByUser(stationNo))
                {
                    identity = ResolveOfflineSelectedRecipeProductIdentity(stationNo);
                }
                else
                {
                    identity = await ReadPlcRecipeProductIdentityAsync(stationNo);
                    identity ??= ResolveOfflineSelectedRecipeProductIdentity(stationNo);
                }
            }

            if (identity is null)
            {
                // 没有产品身份时无法确定测试方案，保持当前提示行或上一帧预览。
                return;
            }

            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            RunOnUiThread(
                () => ApplySchemePreview(identity, force),
                "MonitorView.RefreshSchemePreview");
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

    /// <summary>
    /// 从当前任务、工单或本地程序中解析在线产品身份。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
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

    /// <summary>
    /// 标记操作员已显式选择当前工位的离线程序。
    /// </summary>
    private void MarkOfflineProgramSelectionByUser(int stationNo)
    {
        _offlineProgramSelectedByUserStations.Add(NormalizeStationNo(stationNo));
    }

    /// <summary>
    /// 判断操作员是否已显式选择指定工位的离线程序。
    /// </summary>
    private bool HasOfflineProgramSelectionByUser(int stationNo)
    {
        return _offlineProgramSelectedByUserStations.Contains(NormalizeStationNo(stationNo));
    }

    /// <summary>
    /// 在离开编辑上下文时清除操作员选择的离线程序状态。
    /// </summary>
    private void ClearOfflineProgramSelectionByUser(int stationNo)
    {
        _offlineProgramSelectedByUserStations.Remove(NormalizeStationNo(stationNo));
        _userSelectedOfflineProductNums.Remove(NormalizeStationNo(stationNo));
    }

    /// <summary>
    /// 根据离线界面当前选择的配方解析产品身份。
    /// </summary>
    private ProductIdentity? ResolveOfflineSelectedRecipeProductIdentity(int stationNo)
    {
        var localProgram = GetSelectedOfflineProgramNameOption()?.Program;
        if (localProgram is null
            || string.IsNullOrWhiteSpace(ProgramRecipeMappingRules.Resolve(localProgram, stationNo)))
        {
            return null;
        }

        return new ProductIdentity(
            stationNo,
            localProgram.ProductNum?.Trim() ?? string.Empty,
            localProgram.ProductModel?.Trim() ?? string.Empty,
            "SelectedOfflineProgram");
    }

    /// <summary>
    /// 异步读取 PLC 配方编号并反查产品身份。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>异步操作结果。</returns>
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

    /// <summary>
    /// 异步读取Plc地址文本结果。
    /// </summary>
    /// <param name="logicalKey">PLC 逻辑地址键。</param>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>异步操作结果。</returns>
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

    /// <summary>
    /// 应用方案预览，并在方案结构变化时刷新焊接参数表。
    /// </summary>
    /// <param name="identity">产品身份信息。</param>
    /// <param name="force">是否强制刷新。</param>
    private void ApplySchemePreview(ProductIdentity identity, bool force)
    {
        if (identity.StationNo != CurrentStationNo)
        {
            return;
        }

        if (!IsRunningWeldTask(GetCurrentStationState().ActiveTask))
        {
            ClearCurrentRealtimePreviewDisplay();
            return;
        }

        _currentProductIdentity = identity;
        if (ShouldApplyProductIdentityToInputs(identity))
        {
            SetProductNumSelectionText(identity.ProductNum);
        }

        var processConfig = ResolveRealtimePreviewProcessConfig(identity);
        var activeTaskId = GetCurrentStationState().ActiveTask?.Id ?? 0;
        var previewKey = $"{identity.StationNo}|{identity.ProductNum}|{identity.ProductModel}|{identity.Source}|{activeTaskId}|{processConfig?.Id}|{processConfig?.SchemeId}";
        if (!force
            && string.Equals(previewKey, _lastSchemePreviewKey, StringComparison.Ordinal)
            && _weldParameterRows.Count > 0)
        {
            // 产品、任务和方案都没变时复用现有预览，避免频繁重建表格。
            return;
        }

        // 生成方案行前先缓存上一帧数据，用于把实时值带回新预览行。
        var previousRows = _weldParameterRows
            .Where(row => !string.IsNullOrWhiteSpace(row.ItemKey))
            .GroupBy(row => row.UniqueKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var nextRows = BuildSchemePreviewRows(identity, processConfig, previousRows).ToList();

        _lastSchemePreviewKey = previewKey;
        ApplyWeldParameterRows(nextRows);
    }

    /// <summary>
    /// 判断方案预览解析出的产品身份是否可以直接写入产品工号/型号控件。
    /// 工单已加载时由工单绑定负责回填；在线空闲无工单时不应显示上一件产品。
    /// </summary>
    /// <param name="identity">方案预览解析出的产品身份。</param>
    /// <returns>允许写入输入控件返回 true。</returns>
    private bool ShouldApplyProductIdentityToInputs(ProductIdentity identity)
    {
        if (identity.StationNo != CurrentStationNo || string.IsNullOrWhiteSpace(identity.ProductNum))
        {
            return false;
        }

        var state = GetCurrentStationState();
        if (state.CurrentWorkOrder is not null)
        {
            return false;
        }

        return state.ActiveTask is not null || IsOfflineInputEditable(state);
    }

    /// <summary>
    /// 根据产品工艺和测试方案生成方案预览行。
    /// </summary>
    /// <param name="identity">产品身份信息。</param>
    /// <param name="config">产品工艺配置。</param>
    /// <param name="previousRows">上一次预览行缓存。</param>
    /// <returns>解析后的集合。</returns>
    private IEnumerable<WeldParameterRow> BuildSchemePreviewRows(ProductIdentity identity,
        BizProductProcessConfig? config, IReadOnlyDictionary<string, WeldParameterRow> previousRows)
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
    /// 解析实时预览工序Config。
    /// </summary>
    /// <param name="identity">产品身份信息。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
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

    private static string ResolvePointName(BizProductProcessConfig config)
        => NormalizeDisplayText(config.PointName, "焊点");

    private static string ResolvePointNoHeader(BizProductProcessConfig config)
        => NormalizeDisplayText(config.PointNoHeader, $"{ResolvePointName(config)}序号");

    private static string ResolvePointResultHeader(BizProductProcessConfig config)
        => NormalizeDisplayText(config.PointResultHeader, $"{ResolvePointName(config)}结果");

    private static string ResolvePointCountHeader(BizProductProcessConfig config)
        => NormalizeDisplayText(config.PointCountHeader, $"{ResolvePointName(config)}数");

    private static string ResolveDetailHeader(BizSchemeDetail detail, DimTestItem item, string role)
    {
        var itemName = NormalizeDisplayText(item.ItemName, $"测试项{item.ItemId}");
        var schemeRole = role switch
        {
            PreviewActualRole => SchemeDetailValueRole.Actual,
            PreviewUpperRole => SchemeDetailValueRole.Upper,
            PreviewLowerRole => SchemeDetailValueRole.Lower,
            PreviewResultRole => SchemeDetailValueRole.Result,
            _ => SchemeDetailValueRole.Actual
        };
        return SchemeDetailRoleRules.ResolveHeader(SchemeDetailRoleRules.GetHeader(detail, schemeRole), itemName, schemeRole);
    }

    /// <summary>
    /// 解析方案测试项。
    /// </summary>
    /// <param name="schemeId">测试方案编号。</param>
    /// <returns>解析后的集合。</returns>
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
                Item = allItems.FirstOrDefault(item => item.ItemId == detail.ItemId),
                Detail = detail
            })
            .Where(item => item.Item is not null)
            .Select(item =>
            {
                SchemeDetailRoleRules.ClearUnavailableRoles(item.Detail, item.Item!);
                return item;
            })
            .Where(item => HasAnyPreviewEnabled(item.Detail))
            .Select(item => new SchemePreviewItem(item.Sort, item.Item!, item.Detail))
            .ToList();
    }

    /// <summary>
    /// 创建方案预览行。
    /// </summary>
    /// <param name="identity">产品身份信息。</param>
    /// <param name="config">产品工艺配置。</param>
    /// <param name="schemeItem">测试方案项。</param>
    /// <param name="touchNo">焊点号。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
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
            PointName = ResolvePointName(config),
            PointNoHeader = ResolvePointNoHeader(config),
            PointResultHeader = ResolvePointResultHeader(config),
            PointCountHeader = ResolvePointCountHeader(config),
            ParameterName = item.ItemName,
            Unit = item.Unit ?? string.Empty,
            EnableActual = detail.EnableActual,
            EnableUpper = detail.EnableUpper,
            EnableLower = detail.EnableLower,
            EnableResult = detail.EnableResult,
            ActualHeader = ResolveDetailHeader(detail, item, PreviewActualRole),
            UpperHeader = ResolveDetailHeader(detail, item, PreviewUpperRole),
            LowerHeader = ResolveDetailHeader(detail, item, PreviewLowerRole),
            ResultHeader = ResolveDetailHeader(detail, item, PreviewResultRole),
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

    /// <summary>
    /// 解析Item键。
    /// </summary>
    /// <param name="item">测试项。</param>
    /// <returns>处理后的文本。</returns>
    private static string ResolveItemKey(DimTestItem item)
    {
        return ResolveItemKey(item.ItemId, item.ItemName);
    }

    /// <summary>
    /// 解析Item键。
    /// </summary>
    /// <param name="itemId">测试项编号。</param>
    /// <param name="itemName">测试项名称。</param>
    /// <returns>处理后的文本。</returns>
    private static string ResolveItemKey(int itemId, string? itemName)
    {
        if (itemId > 0)
        {
            return $"item_{itemId}";
        }

        return itemName?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// 创建信息行。
    /// </summary>
    /// <param name="identity">产品身份信息。</param>
    /// <param name="title">标题文本。</param>
    /// <param name="detail">详情。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
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

    /// <summary>
    /// 复制最新值。
    /// </summary>
    /// <param name="previousRows">上一次预览行缓存。</param>
    /// <param name="target">目标对象。</param>
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
        var displayOptions = ResolveRecordDisplayOptions(record);
        var knownRows = new[]
        {
            CreateFallbackWeldParameterRow(record, displayOptions, "test_result", displayOptions.PointResultHeader, FormatNullableText(record.TestResult), 90)
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
            .Select((item, index) => CreateFallbackWeldParameterRow(record, displayOptions, item.Key, item.Key, FormatNullableText(item.Value), 100 + index));

        return knownRows.Concat(dynamicRows);
    }

    private WeldParameterRow CreateFallbackWeldParameterRow(
        BizWeldPointRecord record,
        ProductHistoryDisplayOptions displayOptions,
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
            ProductNum = _currentProductIdentity?.ProductNum ?? GetCurrentStationState().ActiveTask?.ProductNum ?? string.Empty,
            ProductModel = _currentProductIdentity?.ProductModel ?? GetCurrentStationState().ActiveTask?.ProductModel ?? string.Empty,
            TouchIndex = ParsePositiveInt(record.TouchNo),
            TouchNo = record.TouchNo,
            PointName = displayOptions.PointName,
            PointNoHeader = displayOptions.PointNoHeader,
            PointResultHeader = displayOptions.PointResultHeader,
            PointCountHeader = displayOptions.PointCountHeader,
            ParameterName = parameterName,
            Value = value,
            Result = FormatNullableText(record.TestResult),
            RecordTime = record.Ts.ToString("HH:mm:ss"),
            Sort = ParsePositiveInt(record.TouchNo) * 10000 + sort,
            ItemKey = itemKey
        };
    }

    private ProductHistoryDisplayOptions ResolveRecordDisplayOptions(BizWeldPointRecord record)
    {
        var activeTask = GetCurrentStationState().ActiveTask;
        var config = activeTask is not null
            ? _productProcessConfigService.FindActiveForTask(activeTask, record.StationNo)
            : null;
        if (config is null && _currentProductIdentity is { } identity && !string.IsNullOrWhiteSpace(identity.ProductNum))
        {
            config = _productProcessConfigService.FindActive(identity.ProductNum, record.StationNo);
        }

        var showTestFlagInHistory = _currentSettings.ShowTestFlagInHistory != false;
        return config is null
            ? ProductHistoryDisplayOptions.Default with { ShowTestFlagInHistory = showTestFlagInHistory }
            : ProductHistoryDisplayOptions.FromConfig(config, showTestFlagInHistory);
    }

    private static string? FindRecordResult(BizWeldPointRecord record, WeldParameterRow row, IReadOnlyDictionary<string, string> rawValues)
    {
        return FindRawValue(rawValues, $"{row.ItemKey}_result", $"{row.ParameterName}结果")
            ?? record.TestResult;
    }

    /// <summary>
    /// 查找原始值。
    /// </summary>
    /// <param name="rawValues">原始采集值集合。</param>
    /// <param name="keys">键。</param>
    /// <returns>处理后的文本。</returns>
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

    /// <summary>
    /// 解析Positive整数。
    /// </summary>
    /// <param name="value">待处理值。</param>
    /// <returns>解析或计算后的数值。</returns>
    private static int ParsePositiveInt(string? value)
    {
        return int.TryParse(value, out var result) && result > 0 ? result : 0;
    }

    /// <summary>
    /// 规范化Plc文本。
    /// </summary>
    /// <param name="value">待处理值。</param>
    /// <returns>处理后的文本。</returns>
    private static string NormalizePlcText(string? value)
    {
        return value?.Trim().Trim('\0') ?? string.Empty;
    }

    /// <summary>
    /// 排序焊接参数行。
    /// </summary>
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

    /// <summary>
    /// 解析焊接原始值。
    /// </summary>
    /// <param name="rawDataJson">原始采集 JSON。</param>
    /// <returns>解析后的集合。</returns>
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

    #endregion

    #region 表格样式

    /// <summary>
    /// 配置Tables。
    /// </summary>
    private void ConfigureTables()
    {
        TableStyleHelper.ApplyAntdTable(tableMetric1, AntdUI.ColumnsMode.Fill);
        TableStyleHelper.ApplyAntdTable(tableMetric2, AntdUI.ColumnsMode.Fill);
        TableStyleHelper.ApplyAntdTable(tableHistory1, AntdUI.ColumnsMode.Fill);
        TableStyleHelper.ApplyAntdTable(tableHistory2, AntdUI.ColumnsMode.Fill);

        ApplyProductionMetricTableStyle(34, 36);
        ApplyWeldParameterTableStyle();
    }

    /// <summary>
    /// 应用生产指标表格行高和间距。
    /// </summary>
    private void ApplyProductionMetricTableStyle(int rowHeight, int headerHeight)
    {
        ApplyProductionMetricTableStyle(tableMetric1, rowHeight, headerHeight);
        ApplyProductionMetricTableStyle(tableMetric2, rowHeight, headerHeight);
    }

    /// <summary>
    /// 应用生产指标表格行高和间距。
    /// </summary>
    private static void ApplyProductionMetricTableStyle(AntdUI.Table table, int rowHeight, int headerHeight)
    {
        table.RowHeight = Math.Max(1, rowHeight);
        table.RowHeightHeader = Math.Max(1, headerHeight);
        table.Gap = 4;
        table.GapCell = 2;
        table.Gaps = new Size(4, 4);
    }

    /// <summary>
    /// 应用焊接参数表格Style。
    /// </summary>
    private void ApplyWeldParameterTableStyle()
    {
        ApplyWeldParameterTableStyle(dgvPreview1);
        ApplyWeldParameterTableStyle(dgvPreview2);
    }
    /// <summary>
    /// 应用焊接参数表格Style。
    /// </summary>
    /// <param name="grid">目标表格控件。</param>
    private static void ApplyWeldParameterTableStyle(DataGridView grid)
    {
        ControlRenderingHelper.EnableDoubleBuffering(grid);
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

    #endregion

    #region 设备状态与格式化辅助

    /// <summary>
    /// 获取设备状态键。
    /// </summary>
    /// <param name="statusCode">状态Code。</param>
    /// <returns>处理后的文本。</returns>
    private static string GetDeviceStatusKey(short? statusCode)
    {
        return statusCode switch
        {
            ProductionConstants.PlcDeviceStatuses.Unknown => TextKeys.DeviceStatus.Unknown,
            ProductionConstants.PlcDeviceStatuses.Running => TextKeys.DeviceStatus.Running,
            ProductionConstants.PlcDeviceStatuses.Paused => TextKeys.DeviceStatus.Paused,
            ProductionConstants.PlcDeviceStatuses.Stopped => TextKeys.DeviceStatus.Stopped,
            ProductionConstants.PlcDeviceStatuses.Alarm => TextKeys.DeviceStatus.Alarm,
            _ => TextKeys.DeviceStatus.Unknown
        };
    }

    /// <summary>
    /// 获取设备状态颜色。
    /// </summary>
    /// <param name="statusCode">状态Code。</param>
    /// <param name="isSuccess">判断成功。</param>
    /// <returns>用于界面显示的颜色。</returns>
    private static Color GetDeviceStatusColor(short? statusCode, bool isSuccess)
    {
        if (!isSuccess)
        {
            return UiColors.Status.Danger;
        }

        return statusCode switch
        {
            ProductionConstants.PlcDeviceStatuses.Running => UiColors.Status.Success,
            ProductionConstants.PlcDeviceStatuses.Paused => UiColors.Status.Warning,
            ProductionConstants.PlcDeviceStatuses.Stopped => UiColors.Status.Muted,
            ProductionConstants.PlcDeviceStatuses.Alarm => UiColors.Status.Danger,
            _ => UiColors.Status.Muted
        };
    }

    /// <summary>
    /// 格式化可空文本。
    /// </summary>
    /// <param name="value">待处理值。</param>
    /// <returns>处理后的文本。</returns>
    private string FormatNullableText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? _localizer.GetString(TextKeys.Production.NotAvailable)
            : value.Trim();
    }

    private static string NormalizeDisplayText(string? value, string fallback)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue)
            ? fallback
            : normalizedValue;
    }

    /// <summary>
    /// 格式化测试结果文本。
    /// </summary>
    /// <param name="value">待处理值。</param>
    /// <returns>处理后的文本。</returns>
    private string FormatTestResultText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "--", StringComparison.Ordinal))
        {
            return _localizer.GetString(TextKeys.Production.NotAvailable);
        }

        var resultText = value.Trim();
        return TestResultRules.ToDisplayText(resultText);
    }

    /// <summary>
    /// 计算比率。
    /// </summary>
    /// <param name="numerator">numerator。</param>
    /// <param name="denominator">denominator。</param>
    /// <returns>解析或计算后的数值。</returns>
    private static double? CalculateRate(int numerator, int denominator)
    {
        return denominator > 0
            ? (double)numerator / denominator
            : null;
    }

    /// <summary>
    /// 格式化比率。
    /// </summary>
    /// <param name="value">待处理值。</param>
    /// <returns>处理后的文本。</returns>
    private string FormatRate(double? value)
    {
        return value.HasValue
            ? value.Value.ToString("P2")
            : _localizer.GetString(TextKeys.Production.NotAvailable);
    }

    #endregion

    #region 配方下发与校验

    /// <summary>
    /// 解析配方编号用于Started任务。
    /// </summary>
    /// <param name="task">焊接任务。</param>
    /// <param name="selectedProgram">当前选中的程序。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private RecipeCodeResolution ResolveRecipeCodeForStartedTask(
        BizWeldTask task,
        ProgramDataRes? selectedProgram,
        int stationNo)
    {
        if (task.IsOfflineCreated)
        {
            var productNum = FirstNonEmpty(task.ProductNum, selectedProgram?.ProductNum);
            var offlineProgramId = FirstNonEmpty(selectedProgram?.Id, task.ProgramId);
            var localProgram = ResolveLocalProgramByProgramId(offlineProgramId)
                ?? ResolveLocalProgramByNameAndProduct(selectedProgram?.ProgramName ?? task.ProgramName, productNum);
            var mappedRecipeCode = ProgramRecipeMappingRules.Resolve(localProgram, stationNo);
            return new RecipeCodeResolution(
                mappedRecipeCode,
                "LocalProgram",
                $"ProgramId={offlineProgramId}; ProductNumber={productNum}; LocalProgramMatched={localProgram is not null}; LocalProgramId={localProgram?.Id}; RecipeCodePresent={!string.IsNullOrWhiteSpace(mappedRecipeCode)}; TaskGuid={task.LocalExpStartId}");
        }

        var programId = FirstNonEmpty(selectedProgram?.Id, task.ProgramId);
        var localProgramById = ResolveLocalProgramByProgramId(programId);
        var mappedRecipeCodeById = ProgramRecipeMappingRules.Resolve(localProgramById, stationNo);
        return new RecipeCodeResolution(
            mappedRecipeCodeById,
            "ProgramId",
            $"ProgramId={programId}; LocalProgramMatched={localProgramById is not null}; LocalProgramId={localProgramById?.Id}; RecipeCodePresent={!string.IsNullOrWhiteSpace(mappedRecipeCodeById)}; ExpStartId={task.ExpStartId}");
    }

    private BizProgram? ResolveLocalProgramByProgramId(string? mesProgramId)
    {
        var normalizedProgramId = mesProgramId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedProgramId))
        {
            return null;
        }

        var programs = _localProgramSnapshot;
        if (normalizedProgramId.StartsWith("local-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(normalizedProgramId["local-".Length..], out var localProgramId))
        {
            var localProgram = programs.FirstOrDefault(program => program.Id == localProgramId);
            if (localProgram is not null)
            {
                return localProgram;
            }
        }

        var settings = _currentSettings;
        return programs
            .Where(program => string.Equals(program.ProgramId?.Trim(), normalizedProgramId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(program => SameText(program.DeviceId, settings.DeviceId))
            .ThenByDescending(program => program.UpdatedTime)
            .FirstOrDefault();
    }

    /// <summary>
    /// 离线任务按程序名称和产品工号恢复准确的本地程序，避免同一产品多个程序时误取最新记录。
    /// </summary>
    private BizProgram? ResolveLocalProgramByNameAndProduct(string? programName, string? productNum)
    {
        var normalizedProgramName = programName?.Trim();
        var normalizedProductNum = productNum?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedProgramName))
        {
            return null;
        }

        var settings = _currentSettings;
        return _localProgramSnapshot
            .Where(program => SameText(program.ProgramName, normalizedProgramName))
            .Where(program => string.IsNullOrWhiteSpace(normalizedProductNum) || SameText(program.ProductNum, normalizedProductNum))
            .OrderByDescending(program => SameText(program.DeviceId, settings.DeviceId))
            .ThenByDescending(program => program.UpdatedTime)
            .FirstOrDefault();
    }

    /// <summary>
    /// 开工成功后异步下发配方编号，并按设置执行 PLC 校验。
    /// </summary>
    /// <param name="task">焊接任务。</param>
    /// <param name="selectedProgram">当前选中的程序。</param>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>表示异步操作的任务。</returns>
    private async Task DispatchRecipeCodeAfterStartAsync(BizWeldTask task, ProgramDataRes? selectedProgram, int stationNo)
    {
        var sourceResolution = ResolveRecipeCodeForStartedTask(task, selectedProgram, stationNo);
        var sourceRecipeCode = NormalizeRecipeCode(sourceResolution.RecipeCode);
        if (string.IsNullOrWhiteSpace(sourceRecipeCode))
        {
            WriteRecipeFlowLog(
                "RecipeCodeResolveFailed",
                ProductionFlowLogTexts.Summaries.RecipeCodeResolveFailed,
                $"{sourceResolution.Source}; {sourceResolution.Detail}",
                stationNo,
                "Error");
            throw new BusinessOperationException(
                "PLC.RecipeCode",
                "PLC 配方未配置",
                BuildRecipeResolveFailureDetail(task, sourceResolution));
        }

        if (!SharesRecipeTaskAcrossStations())
        {
            task.RecipeCode = sourceRecipeCode;
        }
        var validateRecipe = _currentSettings.ValidateRecipeAfterStart;
        foreach (var targetStationNo in ResolveWorkOrderSignalStations(stationNo))
        {
            var resolution = ResolveRecipeCodeForStartedTask(task, selectedProgram, targetStationNo);
            var targetRecipeCode = NormalizeRecipeCode(resolution.RecipeCode);
            if (string.IsNullOrWhiteSpace(targetRecipeCode))
            {
                throw new BusinessOperationException(
                    "PLC.RecipeCode",
                    "PLC 配方未配置",
                    BuildRecipeResolveFailureDetail(task, resolution));
            }

            WriteRecipeFlowLog(
                "RecipeCodeWriteStarted",
                ProductionFlowLogTexts.Summaries.RecipeCodeWriteStarted,
                $"{resolution.Source}; {resolution.Detail}; RecipeCode={targetRecipeCode}",
                targetStationNo,
                plcSignal: AppConstants.PlcLogicalKeys.PcRecipeCode);

            if (!validateRecipe)
            {
                // 未启用校验时只写 PC 配方地址，保持与旧现场 PLC 流程兼容。
                var writeResult = await _plcBusinessSignalService.WriteTextAsync(
                    AppConstants.PlcLogicalKeys.PcRecipeCode,
                    targetStationNo,
                    targetRecipeCode);
                if (!writeResult.IsSuccess)
                {
                    WriteRecipeFlowLog(
                        "RecipeCodeWriteFailed",
                        ProductionFlowLogTexts.Summaries.RecipeCodeWriteFailed,
                        $"{resolution.Source}; {resolution.Detail}; RecipeCode={targetRecipeCode}; Detail={writeResult.Message}",
                        targetStationNo,
                        "Error",
                        AppConstants.PlcLogicalKeys.PcRecipeCode,
                        writeResult.Address);
                    throw new BusinessOperationException(
                        "PLC.RecipeCode",
                        "PLC 配方下发失败",
                        $"Station={targetStationNo}; RecipeCode={targetRecipeCode}; Detail={writeResult.Message}");
                }

                WriteRecipeFlowLog(
                    "RecipeCodeWriteSucceeded",
                    ProductionFlowLogTexts.Summaries.RecipeCodeWriteSucceeded,
                    $"{resolution.Source}; {resolution.Detail}; RecipeCode={targetRecipeCode}; ValidateRecipe=false",
                    targetStationNo,
                    plcSignal: AppConstants.PlcLogicalKeys.PcRecipeCode,
                    plcAddress: writeResult.Address);
                continue;
            }

            // 启用校验时同步写 PC 配方并等待 PLC 配方回读一致，防止设备使用错误配方。
            var syncResult = await _plcBusinessSignalService.SyncRecipeCodeAsync(
                targetStationNo,
                targetRecipeCode,
                RecipePreparationTimeout);
            if (!syncResult.IsSuccess)
            {
                WriteRecipeFlowLog(
                    "RecipeCodeValidationFailed",
                    ProductionFlowLogTexts.Summaries.RecipeCodeValidationFailed,
                    $"{resolution.Source}; {resolution.Detail}; PC={syncResult.PcRecipeCode}; PLC={syncResult.PlcRecipeCode}; Detail={syncResult.Message}",
                    targetStationNo,
                    "Error",
                    AppConstants.PlcLogicalKeys.PlcRecipeCode);
                throw new BusinessOperationException(
                    "PLC.RecipeCodeCheck",
                    "PLC 配方校验失败",
                    $"Station={targetStationNo}; PC={syncResult.PcRecipeCode}; PLC={syncResult.PlcRecipeCode}; Detail={syncResult.Message}");
            }

            WriteRecipeFlowLog(
                "RecipeCodeValidationSucceeded",
                ProductionFlowLogTexts.Summaries.RecipeCodeValidationSucceeded,
                $"{resolution.Source}; {resolution.Detail}; RecipeCode={syncResult.PcRecipeCode}; PLC={syncResult.PlcRecipeCode}",
                targetStationNo,
                plcSignal: AppConstants.PlcLogicalKeys.PlcRecipeCode);
        }

        SetRuntimeStatusSuccess(validateRecipe
            ? TextKeys.Monitor.RuntimeStatus.RecipeCodeValidationSucceeded
            : TextKeys.Monitor.RuntimeStatus.RecipeCodeWriteSucceeded);
    }

    /// <summary>
    /// 构建配方解析失败详情。
    /// </summary>
    /// <param name="task">焊接任务。</param>
    /// <param name="resolution">resolution。</param>
    /// <returns>处理后的文本。</returns>
    private static string BuildRecipeResolveFailureDetail(BizWeldTask task, RecipeCodeResolution resolution)
    {
        var lookupHint = task.IsOfflineCreated
            ? "离线任务需要按 ProductNumber 匹配本地程序并读取 RecipeCode。"
            : "在线任务需要按 ProgramId 匹配本地程序并读取 RecipeCode。";
        return $"{lookupHint} {resolution.Detail}";
    }

    #endregion

    #region PLC 业务信号写入与调和

    /// <summary>
    /// 开工成功后写入需要的 PLC 业务信号。
    /// </summary>
    /// <param name="program">程序数据。</param>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>表示异步操作的任务。</returns>
    private async Task WriteStartBusinessSignalsAsync(ProgramDataRes program, int stationNo)
    {
        await WriteStartBusinessSignalsAfterStartAsync(program, stationNo);
    }

    /// <summary>
    /// 安全写入开工业务信号，异常只更新提示和日志，不回滚开工结果。
    /// </summary>
    /// <param name="program">程序数据。</param>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>表示异步操作的任务。</returns>
    private async Task SafeWriteStartBusinessSignalsAsync(ProgramDataRes program, int stationNo)
    {
        try
        {
            await WriteStartBusinessSignalsAsync(program, stationNo);
        }
        catch (BusinessOperationException ex) when (ex.SourceName?.Contains("Recipe") == true)
        {
            // 配方相关异常不覆盖开工成功状态，只在异常提示区展示风险。
            _exceptionLogService.WriteBusiness(ex.SourceName, ex.Message, ex.Detail);
            SetRuntimeError(TextKeys.Monitor.RuntimeError.RecipeValidationFailed);
        }
        catch (BusinessOperationException ex)
        {
            // 其他业务信号异常同样只提示和记日志，不回滚已经完成的开工。
            _exceptionLogService.WriteBusiness(ex.SourceName, ex.Message, ex.Detail);
            SetRuntimeError(TextKeys.Monitor.RuntimeError.BusinessSignalWriteFailed);
        }
        catch (Exception ex)
        {
            // 未预期异常保留统一提示，详细堆栈写入异常日志。
            _exceptionLogService.Write(ex, "MonitorView.SafeWriteStartBusinessSignals");
            SetRuntimeError(TextKeys.Monitor.RuntimeError.BusinessSignalWriteFailed);
        }
    }

    /// <summary>
    /// 开工完成后按顺序写入工单状态和配方编号。
    /// </summary>
    /// <param name="program">程序数据。</param>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>异步操作成功返回 true，否则返回 false。</returns>
    private async Task<bool> WriteStartBusinessSignalsAfterStartAsync(ProgramDataRes program, int stationNo)
    {
        var task = GetCurrentStationState().ActiveTask ?? _weldTaskService.RestoreUnfinishedTask(stationNo);

        if (task is null || task.EndTime is not null)
        {
            throw new BusinessOperationException(
                "PLC.RecipeCode",
                "PLC 配方下发失败",
                $"No started task exists for station {stationNo}.");
        }

        await RequireWorkOrderStatusWriteAsync(
            stationNo,
            ProductionConstants.PlcWorkOrderStatuses.StartedAllowProduction,
            "PLC.WorkOrderStatus.Start",
            ProductionFlowLogTexts.Summaries.WorkOrderStatusWriteFailed,
            writeOnReadFailure: true,
            mirrorWorkOrderStations: true);

        // 只有 PLC 已允许生产后才下发配方，保证设备状态与任务状态顺序一致。
        await DispatchRecipeCodeAfterStartAsync(task, program, stationNo);
        return true;
    }

    /// <summary>
    /// 完工成功后写入禁止生产的 PLC 工单状态。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>表示异步操作的任务。</returns>
    private async Task WriteFinishBusinessSignalsAsync(int stationNo)
    {
        await RequireWorkOrderStatusWriteAsync(
            stationNo,
            ProductionConstants.PlcWorkOrderStatuses.FinishedForbidProduction,
            "PLC.WorkOrderStatus.Finish",
            ProductionFlowLogTexts.Summaries.WorkOrderStatusWriteFailed,
            writeOnReadFailure: true,
            mirrorWorkOrderStations: true);
    }

    /// <summary>
    /// 异步处理Require工单状态写入。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <param name="status">目标状态值。</param>
    /// <param name="source">触发来源或日志来源。</param>
    /// <param name="summary">日志摘要。</param>
    /// <param name="writeOnReadFailure">读取失败时是否仍尝试写入。</param>
    /// <param name="mirrorWorkOrderStations">是否同步写入同一工单关联的工位。</param>
    /// <returns>表示异步操作的任务。</returns>
    private async Task RequireWorkOrderStatusWriteAsync(int stationNo, int status, string source, string summary, bool writeOnReadFailure, bool mirrorWorkOrderStations)
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

    /// <summary>
    /// 异步确保工单状态。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <param name="expectedStatus">期望的工单状态。</param>
    /// <param name="source">触发来源或日志来源。</param>
    /// <param name="summary">日志摘要。</param>
    /// <param name="context">业务上下文说明。</param>
    /// <param name="writeOnReadFailure">读取失败时是否仍尝试写入。</param>
    /// <param name="mirrorWorkOrderStations">是否同步写入同一工单关联的工位。</param>
    /// <param name="cancellationToken">调和任务取消令牌；显式业务操作使用默认令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    private async Task EnsureWorkOrderStatusAsync(int stationNo, int expectedStatus, string source,
        string summary, string context, bool writeOnReadFailure, bool mirrorWorkOrderStations,
        CancellationToken cancellationToken = default)
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
                (target, value) => _plcBusinessSignalService.WriteWorkOrderStatusAsync(target, value, cancellationToken),
                cancellationToken);
        }
    }

    /// <summary>
    /// 异步确保设备模式。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <param name="expectedMode">期望的设备模式。</param>
    /// <param name="source">触发来源或日志来源。</param>
    /// <param name="summary">日志摘要。</param>
    /// <param name="context">业务上下文说明。</param>
    /// <param name="writeOnReadFailure">读取失败时是否仍尝试写入。</param>
    /// <param name="cancellationToken">调和任务取消令牌；显式业务操作使用默认令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    private Task EnsureDeviceModeAsync(int stationNo, int expectedMode, string source, string summary, string context,
        bool writeOnReadFailure, CancellationToken cancellationToken = default)
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
            (target, value) => _plcBusinessSignalService.WriteDeviceModeAsync(target, value, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// 读取并校验 PLC 整数业务信号，必要时串行写入期望值。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <param name="logicalKey">PLC 逻辑地址键。</param>
    /// <param name="expectedValue">期望写入或保持的整数值。</param>
    /// <param name="source">触发来源或日志来源。</param>
    /// <param name="summary">日志摘要。</param>
    /// <param name="context">业务上下文说明。</param>
    /// <param name="writeOnReadFailure">读取失败时是否仍尝试写入。</param>
    /// <param name="lastSuccessCache">上次成功值缓存。</param>
    /// <param name="signalLock">当前信号的串行写入锁。</param>
    /// <param name="writeAsync">实际执行 PLC 写入的异步委托。</param>
    /// <param name="cancellationToken">调和任务取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    private async Task EnsureIntegerBusinessSignalAsync(int stationNo, string logicalKey, int expectedValue, string source, string summary, string context,
        bool writeOnReadFailure, IDictionary<int, int> lastSuccessCache, SemaphoreSlim signalLock,
        Func<int, int, Task<PlcBusinessSignalResult>> writeAsync, CancellationToken cancellationToken = default)
    {
        var targetStationNo = NormalizeStatusStationNo(stationNo);
        await signalLock.WaitAsync(cancellationToken);
        try
        {
            var readResult = await _plcBusinessSignalService.ReadTextAsync(logicalKey, targetStationNo, cancellationToken);
            var readValueParsed = TryParsePlcSignalInt(readResult.Value, out var currentValue);
            var shouldWrite = !readResult.IsSuccess || !readValueParsed || currentValue != expectedValue;
            PlcBusinessSignalResult? writeResult = null;

            if (!readResult.IsSuccess && !writeOnReadFailure)
            {
                // 调和场景下读取失败不强写，避免通讯不稳定时把未知状态改成错误值。
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
                // PLC 当前值已符合预期，只更新缓存并跳过写入，减少不必要的 PLC 写操作。
                lastSuccessCache[targetStationNo] = expectedValue;
                return;
            }

            // 读取值缺失或不一致时才写入期望值，写入结果统一落生产流程日志。
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

    /// <summary>
    /// 获取工单状态Lock。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private SemaphoreSlim GetWorkOrderStatusLock(int stationNo)
        => GetBusinessSignalLock(_workOrderStatusLocks, stationNo);

    /// <summary>
    /// 获取设备模式Lock。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private SemaphoreSlim GetDeviceModeLock(int stationNo)
        => GetBusinessSignalLock(_deviceModeLocks, stationNo);

    /// <summary>
    /// 获取业务信号Lock。
    /// </summary>
    /// <param name="locks">locks。</param>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
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

    /// <summary>
    /// 尝试解析PLC 信号整数。
    /// </summary>
    /// <param name="value">待处理值。</param>
    /// <param name="number">输出解析后的整数。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private static bool TryParsePlcSignalInt(string? value, out int number)
    {
        return int.TryParse((value ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
    }

    /// <summary>
    /// 写入 PLC 业务信号调和日志，记录读写结果和上下文。
    /// </summary>
    /// <param name="readResult">PLC 读取结果。</param>
    /// <param name="writeResult">PLC 写入结果。</param>
    /// <param name="source">触发来源或日志来源。</param>
    /// <param name="summary">日志摘要。</param>
    /// <param name="stationNo">工位编号。</param>
    /// <param name="plcSignal">PLC 信号名称。</param>
    /// <param name="expectedValue">期望写入或保持的整数值。</param>
    /// <param name="shouldWrite">是否需要执行写入。</param>
    /// <param name="context">业务上下文说明。</param>
    private void WriteBusinessSignalReconcileFlowLog(PlcBusinessSignalResult readResult, PlcBusinessSignalResult? writeResult,
        string source, string summary, int stationNo, string plcSignal, int expectedValue, bool shouldWrite, string context)
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

    /// <summary>
    /// 构建业务信号调和摘要。
    /// </summary>
    /// <param name="plcSignal">PLC 信号名称。</param>
    /// <param name="readResult">PLC 读取结果。</param>
    /// <param name="writeResult">PLC 写入结果。</param>
    /// <param name="failureSummary">失败摘要。</param>
    /// <returns>处理后的文本。</returns>
    private static string BuildBusinessSignalReconcileSummary(string plcSignal, PlcBusinessSignalResult readResult,
        PlcBusinessSignalResult? writeResult, string failureSummary)
    {
        if (!readResult.IsSuccess && writeResult is null)
        {
            return ProductionFlowLogTexts.Summaries.FormatSignalReadFailed(plcSignal);
        }

        if (writeResult is { IsSuccess: true })
        {
            return ProductionFlowLogTexts.Summaries.FormatSignalReconcileSucceeded(plcSignal);
        }

        return failureSummary;
    }

    /// <summary>
    /// 解析业务信号调和严重级别。
    /// </summary>
    /// <param name="readResult">PLC 读取结果。</param>
    /// <param name="writeResult">PLC 写入结果。</param>
    /// <returns>处理后的文本。</returns>
    private static string ResolveBusinessSignalReconcileSeverity(
        PlcBusinessSignalResult readResult,
        PlcBusinessSignalResult? writeResult)
    {
        return !readResult.IsSuccess || writeResult is { IsSuccess: false }
            ? "Error"
            : "Info";
    }

    /// <summary>
    /// 解析PLC 设备模式。
    /// </summary>
    /// <returns>解析或计算后的数值。</returns>
    private int ResolvePlcDeviceMode()
    {
        var settings = _currentSettings;
        return settings.EnableDualStation && settings.EnableDualWorkOrder
            ? ProductionConstants.PlcDeviceModes.DualStationDualWorkOrder
            : ProductionConstants.PlcDeviceModes.SingleOrDualSameWorkOrder;
    }

    #endregion

    #region 配方显示与本地程序辅助

    /// <summary>
    /// 写入配方流程日志。
    /// </summary>
    /// <param name="step">step。</param>
    /// <param name="summary">日志摘要。</param>
    /// <param name="detail">详情。</param>
    /// <param name="stationNo">工位编号。</param>
    /// <param name="level">level。</param>
    /// <param name="plcSignal">PLC 信号名称。</param>
    /// <param name="plcAddress">plc地址。</param>
    private void WriteRecipeFlowLog(string step, string summary, string detail, int stationNo,
        string level = "Info", string plcSignal = "", string plcAddress = "")
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
    /// 解析配方编号用于Display。
    /// </summary>
    /// <param name="activeTask">active任务。</param>
    /// <param name="program">程序数据。</param>
    /// <returns>处理后的文本。</returns>
    private string ResolveRecipeCodeForDisplay(BizWeldTask? activeTask, ProgramDataRes? program)
    {
        if (activeTask is not null && IsRunningWeldTask(activeTask))
        {
            var localProgram = ResolveLocalProgramByProgramId(FirstNonEmpty(program?.Id, activeTask.ProgramId))
                ?? ResolveLocalProgramByNameAndProduct(program?.ProgramName ?? activeTask.ProgramName, activeTask.ProductNum);
            return FirstNonEmpty(
                ProgramRecipeMappingRules.Resolve(localProgram, CurrentStationNo),
                activeTask.RecipeCode,
                program?.RecipeCode);
        }

        if (program is not null)
        {
            var localProgram = ResolveLocalProgramById(program.Id);
            return FirstNonEmpty(
                ProgramRecipeMappingRules.Resolve(localProgram, CurrentStationNo),
                program.RecipeCode);
        }

        if (IsOfflineInputEditable(GetCurrentStationState()))
        {
            var snapshot = _plcRecipeReconcileMonitorService.GetCurrent(CurrentStationNo);
            if (snapshot.IsSuccess && !string.IsNullOrWhiteSpace(snapshot.RecipeCode))
            {
                return snapshot.RecipeCode;
            }
        }

        return "--";
    }

    /// <summary>
    /// 判断是否存在属于当前工单的在线程序待确认选择。
    /// </summary>
    /// <param name="state">当前工位运行态。</param>
    /// <returns>存在有效待确认选择返回 true。</returns>
    private bool HasPendingOnlineProgramSelection(ProductionStationRuntimeState state)
    {
        if (string.IsNullOrWhiteSpace(_pendingOnlineProgramName))
        {
            return false;
        }

        var workOrderKey = state.CurrentWorkOrder?.SN?.Trim();
        return !string.IsNullOrWhiteSpace(workOrderKey)
            && string.Equals(workOrderKey, _pendingOnlineProgramWorkOrderKey, StringComparison.Ordinal);
    }

    /// <summary>
    /// 解析待确认在线程序对应的本地配方号。
    /// </summary>
    /// <param name="programListItem">MES 程序列表项。</param>
    /// <returns>解析到的配方号；不存在时返回空串。</returns>
    private string ResolveRecipeCodeForPendingProgram(MesProgramListItemData programListItem)
    {
        var localProgram = ResolveLocalProgramByProgramId(programListItem.Id);
        if (localProgram is not null)
        {
            return ProgramRecipeMappingRules.Resolve(localProgram, CurrentStationNo);
        }

        var matchedProgram = _localProgramSnapshot
            .FirstOrDefault(program =>
                SameText(program.ProgramName, programListItem.ProgramName)
                && SameText(program.ProductNum, programListItem.ProductNum));
        return ProgramRecipeMappingRules.Resolve(matchedProgram, CurrentStationNo);
    }

    /// <summary>
    /// 仅按程序名称解析本地配方号，用于程序列表重载间隙时的名称预览。
    /// </summary>
    /// <param name="programName">程序名称。</param>
    /// <returns>解析到的配方号；不存在时返回空串。</returns>
    /// <summary>
    /// 解析本地程序。
    /// </summary>
    /// <param name="program">程序数据。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    /// <summary>
    /// Determines whether a weld task is still in production.
    /// </summary>
    private static bool IsRunningWeldTask(BizWeldTask? task)
    {
        return task is not null
            && task.EndTime is null
            && string.Equals(
                task.TaskStatus,
                ProductionConstants.ProductInstanceStatuses.Running,
                StringComparison.OrdinalIgnoreCase);
    }

    private BizProgram? ResolveLocalProgram(ProgramDataRes program)
    {
        var localPrograms = _localProgramSnapshot;
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

    /// <summary>
    /// 解析本地程序按Id。
    /// </summary>
    /// <param name="programId">程序Id。</param>
    /// <param name="deviceId">设备编号。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private BizProgram? ResolveLocalProgramById(string? programId, string? deviceId = null)
    {
        var normalizedProgramId = programId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedProgramId))
        {
            return null;
        }

        return _localProgramSnapshot
            .Where(program => string.Equals(program.ProgramId?.Trim(), normalizedProgramId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(program => SameText(program.DeviceId, deviceId))
            .ThenByDescending(program => program.UpdatedTime)
            .FirstOrDefault();
    }

    /// <summary>
    /// 解析本地程序按配方编号。
    /// </summary>
    /// <param name="recipeCode">配方编号。</param>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
    private BizProgram? ResolveLocalProgramByRecipeCode(string? recipeCode, int stationNo)
    {
        var normalizedRecipeCode = NormalizeRecipeCode(recipeCode);
        if (string.IsNullOrWhiteSpace(normalizedRecipeCode))
        {
            return null;
        }

        var settings = _currentSettings;
        return _localProgramSnapshot
            .Where(program => ProgramRecipeMappingRules.Matches(program, stationNo, normalizedRecipeCode))
            .OrderByDescending(program => SameText(program.DeviceId, settings.DeviceId))
            .ThenByDescending(program => program.UpdatedTime)
            .FirstOrDefault();
    }

    /// <summary>
    /// 规范化配方编号。
    /// </summary>
    /// <param name="value">待处理值。</param>
    /// <returns>处理后的文本。</returns>
    private static string NormalizeRecipeCode(string? value)
    {
        return ProgramRecipeMappingRules.Normalize(NormalizePlcText(value));
    }

    /// <summary>
    /// 比较文本。
    /// </summary>
    /// <param name="left">左侧文本。</param>
    /// <param name="right">右侧文本。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private static bool SameText(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 处理第一个非Empty。
    /// </summary>
    /// <param name="values">候选文本集合。</param>
    /// <returns>处理后的文本。</returns>
    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// 格式化程序。
    /// </summary>
    /// <param name="program">程序数据。</param>
    /// <returns>处理后的文本。</returns>
    private static string FormatProgram(MesProgramListItemData program)
    {
        return $"{program.ProgramName} | {program.ProgramType} | {program.ProductNum} | {program.Id}";
    }

    #endregion

    #region 操作员信息与输入确认

    /// <summary>
    /// 异步提示已校验操作员。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>异步解析得到的文本。</returns>
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
                SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.OperatorValidated);
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

    /// <summary>
    /// 绑定MES 操作员信息。
    /// </summary>
    /// <param name="userInfo">MES 用户信息。</param>
    /// <param name="fallbackEmployeeNumber">兜底员工编号。</param>
    private void BindMesOperatorInfo(UserInfoRes? userInfo, string fallbackEmployeeNumber)
    {
        _syncingOperatorInput = true;
        try
        {
            MesUserName.Text = userInfo?.UserName?.Trim() ?? string.Empty;
            MesUserNumber.Text = string.IsNullOrWhiteSpace(userInfo?.UserNumber)
                ? fallbackEmployeeNumber.Trim()
                : userInfo.UserNumber.Trim();
            inputDeptName.Text = userInfo?.DeptName?.Trim() ?? string.Empty;
            TeamName.Text = userInfo?.TeamName?.Trim() ?? string.Empty;
        }
        finally
        {
            _syncingOperatorInput = false;
        }
    }

    /// <summary>
    /// 绑定运行时操作员信息。
    /// </summary>
    /// <param name="state">工位运行状态。</param>
    /// <param name="activeTask">active任务。</param>
    /// <param name="preserveDraftEmployeeNumber">是否保留未校验的员工号输入。</param>
    private void BindRuntimeOperatorInfo(
        ProductionStationRuntimeState state,
        BizWeldTask? activeTask,
        bool preserveDraftEmployeeNumber = false)
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

        if (preserveDraftEmployeeNumber)
        {
            ClearMesOperatorDisplayInfo();
            return;
        }

        ClearMesOperatorInfo();
    }

    /// <summary>
    /// 绑定离线待开工态的员工信息。
    /// 离线无法向 MES 校验身份，员工号完全是操作员的现场录入项：有任务时回填任务快照，
    /// 否则只清空姓名、部门和班组显示并保留正在输入的员工号，且不复用上一次在线校验残留的 MES 员工信息。
    /// </summary>
    /// <param name="activeTask">当前工位任务；离线可编辑态下通常为 null。</param>
    private void BindOfflineOperatorInfo(BizWeldTask? activeTask)
    {
        var taskOperator = CreateTaskOperatorInfo(activeTask);
        if (taskOperator is not null)
        {
            BindMesOperatorInfo(taskOperator, taskOperator.UserNumber);
            return;
        }

        ClearMesOperatorDisplayInfo();
    }

    /// <summary>
    /// 判断刷新运行态时是否保留正在输入但尚未校验的员工号。
    /// </summary>
    /// <param name="state">当前工位运行态。</param>
    /// <param name="activeTask">当前任务。</param>
    /// <param name="onlineEditable">在线开工字段是否可编辑。</param>
    /// <returns>需要保留返回 true。</returns>
    private bool ShouldPreserveDraftOperatorNumber(
        ProductionStationRuntimeState state,
        BizWeldTask? activeTask,
        bool onlineEditable)
    {
        return onlineEditable
            && activeTask is null
            && state.MesOperatorInfo is null
            && string.IsNullOrWhiteSpace(state.MesOperatorNumber)
            && !string.IsNullOrWhiteSpace(MesUserNumber.Text);
    }

    /// <summary>
    /// 创建任务操作员信息。
    /// </summary>
    /// <param name="task">焊接任务。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
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

    /// <summary>
    /// 清空MES 操作员信息。
    /// </summary>
    private void ClearMesOperatorInfo()
    {
        _syncingOperatorInput = true;
        try
        {
            _validatedOperatorNumber = null;
            MesUserName.Text = string.Empty;
            MesUserNumber.Text = string.Empty;
            inputDeptName.Text = string.Empty;
            TeamName.Text = string.Empty;
        }
        finally
        {
            _syncingOperatorInput = false;
        }
    }

    /// <summary>
    /// 仅清空员工关联显示字段（姓名、部门、班组），不清空员工号输入框本身。
    /// 用于用户修改员工号时即时撤销显示信息。
    /// </summary>
    private void ClearMesOperatorDisplayInfo()
    {
        _syncingOperatorInput = true;
        try
        {
            MesUserName.Text = string.Empty;
            inputDeptName.Text = string.Empty;
            TeamName.Text = string.Empty;
        }
        finally
        {
            _syncingOperatorInput = false;
        }
    }

    /// <summary>
    /// 记录内联员工号校验成功时界面最终显示的员工号。
    /// MES 可能返回规范化后的员工号，因此必须在回填控件后设置标记。
    /// </summary>
    private void MarkInlineOperatorValidated()
    {
        _validatedOperatorNumber = MesUserNumber.Text.Trim();
    }

    /// <summary>
    /// 判断当前员工号是否仍是最近一次通过 MES 校验的员工号。
    /// </summary>
    /// <param name="employeeNumber">当前准备开工的员工号。</param>
    /// <returns>当前员工号已校验返回 true。</returns>
    private bool IsInlineOperatorValidated(string employeeNumber)
    {
        return !string.IsNullOrWhiteSpace(_validatedOperatorNumber)
            && string.Equals(employeeNumber.Trim(), _validatedOperatorNumber?.Trim(), StringComparison.Ordinal);
    }

    /// <summary>
    /// 内联校验员工号身份。
    /// 仅在"操作员弹窗输入"关闭且当前工位处于在线空闲状态时执行；
    /// 成功后回填员工信息并设置校验通过标记，失败则记录业务日志并在提示区报错。
    /// </summary>
    private async Task ValidateOperatorInlineAsync(int stationNo)
    {
        var settings = _currentSettings;
        if (settings.UseOperatorInputDialog ?? true)
        {
            return;
        }

        await RunUiOperationAsync(async () =>
        {
            var state = GetCurrentStationState();
            if (!IsOnlineStartInputEditable(state))
            {
                return;
            }

            var employeeNumber = MesUserNumber.Text.Trim();
            if (string.IsNullOrWhiteSpace(employeeNumber))
            {
                SetRuntimeError(TextKeys.Monitor.RuntimeError.OperatorNumberRequired);
                return;
            }

            ClearRuntimeError();
            SetRuntimeStatus(TextKeys.Monitor.RuntimeStatus.ValidatingOperator);
            var response = await _weldTaskService.ValidateMesOperatorAsync(employeeNumber, stationNo);
            if (!response.IsSuccess)
            {
                _exceptionLogService.WriteBusiness(
                    "MES.ValidateOperator",
                    _localizer.GetString(TextKeys.Monitor.Message.OperatorValidationFailed),
                    response.Msg,
                    $"EmployeeNumber={employeeNumber}");
                SetRuntimeError(TextKeys.Monitor.RuntimeError.OperatorValidationFailedInline);
                _validatedOperatorNumber = null;
                return;
            }

            BindMesOperatorInfo(response.Data, employeeNumber);
            MarkInlineOperatorValidated();
            ClearRuntimeError();
            SetRuntimeStatusSuccess(TextKeys.Monitor.RuntimeStatus.OperatorValidated);
        });
    }

    /// <summary>
    /// 尝试提示输入非负数整数。
    /// </summary>
    /// <param name="titleKey">标题本地化键。</param>
    /// <param name="promptKey">提示本地化键。</param>
    /// <param name="defaultValue">默认值。</param>
    /// <param name="value">待处理值。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
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

    /// <summary>
    /// 尝试提示输入整数。
    /// </summary>
    /// <param name="titleKey">标题本地化键。</param>
    /// <param name="promptKey">提示本地化键。</param>
    /// <param name="defaultValue">默认值。</param>
    /// <param name="value">待处理值。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
    private bool TryPromptInt(string titleKey, string promptKey, int defaultValue, out int value)
    {
        return TryPromptIntText(
            _localizer.GetString(titleKey),
            _localizer.GetString(promptKey),
            defaultValue,
            out value);
    }

    /// <summary>
    /// 尝试提示输入整数文本。
    /// </summary>
    /// <param name="title">标题文本。</param>
    /// <param name="prompt">提示文本。</param>
    /// <param name="defaultValue">默认值。</param>
    /// <param name="value">待处理值。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
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

    #endregion

    #region 通用操作与提示

    /// <summary>
    /// 统一执行界面异步操作，并处理业务异常和未知异常。
    /// </summary>
    /// <param name="action">需要执行的异步操作。</param>
    /// <returns>表示异步操作的任务。</returns>
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
            SetRuntimeError(ResolveBusinessRuntimeErrorKey(ex));
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

    /// <summary>
    /// 串行执行指定工位的开工或完工上报操作。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <param name="actionName">操作名称，用于提示和日志。</param>
    /// <param name="action">需要执行的异步操作。</param>
    /// <returns>表示异步操作的任务。</returns>
    private async Task RunReportOperationAsync(int stationNo, string actionName, Func<Task> action)
    {
        stationNo = NormalizeStationNo(stationNo);
        if (!TryEnterStationOperation(stationNo))
        {
            // 同一工位上报必须串行，避免重复点击造成 MES/PLC 状态交叉写入。
            SetRuntimeError(TextKeys.Monitor.RuntimeError.StationOperationBusy);
            return;
        }

        try
        {
            UseWaitCursor = true;
            // 执行业务前再次选中工位，确保服务层和界面层使用同一个工位上下文。
            SelectStationForOperation(stationNo);
            await action();
        }
        catch (BusinessOperationException ex)
        {
            _exceptionLogService.WriteBusiness(ex.SourceName, ex.Message, ex.Detail);
            SetRuntimeError(TextKeys.Monitor.RuntimeError.StationReportFailed);
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, $"MonitorView.{actionName}");
            SetRuntimeError(TextKeys.Monitor.RuntimeError.StationReportFailed);
        }
        finally
        {
            UseWaitCursor = false;
            // finally 中释放锁，保证异常路径不会永久占用当前工位操作权限。
            ExitStationOperation(stationNo);
        }
    }

    /// <summary>
    /// 尝试进入工位操作锁，防止同一工位重复上报。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    /// <returns>条件满足返回 true，否则返回 false。</returns>
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

    /// <summary>
    /// 释放工位操作锁。
    /// </summary>
    /// <param name="stationNo">工位编号。</param>
    private static void ExitStationOperation(int stationNo)
    {
        lock (StationOperationSync)
        {
            BusyOperationStations.Remove(stationNo);
        }
    }

    /// <summary>
    /// 解析业务异常摘要键。
    /// </summary>
    /// <param name="exception">业务异常。</param>
    /// <returns>本地化摘要键。</returns>
    private static string ResolveBusinessRuntimeErrorKey(BusinessOperationException exception)
    {
        if (exception.SourceName.Contains("Recipe", StringComparison.OrdinalIgnoreCase))
        {
            return TextKeys.Monitor.RuntimeError.RecipeValidationFailed;
        }

        if (exception.SourceName.Contains("PLC", StringComparison.OrdinalIgnoreCase)
            || exception.SourceName.Contains("WorkOrderStatus", StringComparison.OrdinalIgnoreCase))
        {
            return TextKeys.Monitor.RuntimeError.BusinessSignalWriteFailed;
        }

        return TextKeys.Monitor.RuntimeError.OperationFailed;
    }

    /// <summary>
    /// 显示警告。
    /// </summary>
    /// <param name="messageKey">本地化文本键。</param>
    /// <param name="args">本地化文本参数。</param>
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

    /// <summary>
    /// 显示警告文本。
    /// </summary>
    /// <param name="message">提示消息。</param>
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
    /// 显示Business警告。
    /// </summary>
    /// <param name="source">触发来源或日志来源。</param>
    /// <param name="messageKey">本地化文本键。</param>
    /// <param name="detail">详情。</param>
    /// <param name="context">业务上下文说明。</param>
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

    /// <summary>
    /// 显示异常。
    /// </summary>
    /// <param name="message">提示消息。</param>
    private void ShowError(string message)
    {
        MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleError),
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    #endregion

    #region 运行提示更新

    /// <summary>
    /// 设置运行状态。
    /// </summary>
    /// <param name="messageKey">本地化文本键。</param>
    /// <param name="args">本地化文本参数。</param>
    private void SetRuntimeStatus(string messageKey, params object[] args)
    {
        SetRuntimeStatusCore(messageKey, args, null, isSuccess: false);
    }

    /// <summary>
    /// 设置成功运行状态，并通过资源键保存，便于重启后重新本地化显示。
    /// </summary>
    /// <param name="messageKey">本地化文本键。</param>
    /// <param name="args">本地化文本参数。</param>
    private void SetRuntimeStatusSuccess(string messageKey, params object[] args)
    {
        SetRuntimeStatusCore(messageKey, args, null, isSuccess: true);
    }

    /// <summary>
    /// 设置运行状态文本。
    /// </summary>
    /// <param name="message">提示消息。</param>
    /// <param name="isSuccess">判断成功。</param>
    private void SetRuntimeStatusText(string message, bool isSuccess = false)
    {
        SetRuntimeStatusCore(null, Array.Empty<object>(), NormalizeRuntimeSummary(message), isSuccess);
    }

    /// <summary>
    /// 统一写入运行状态字段，避免 key 与动态文本同时残留。
    /// </summary>
    /// <param name="messageKey">本地化文本键；为空时使用动态文本。</param>
    /// <param name="args">本地化文本参数。</param>
    /// <param name="message">动态文本兼容值。</param>
    /// <param name="isSuccess">是否按成功状态显示。</param>
    private void SetRuntimeStatusCore(string? messageKey, object[] args, string? message, bool isSuccess)
    {
        _runtimeStatusKey = messageKey;
        _runtimeStatusArgs = args;
        _runtimeStatusText = message;
        _runtimeStatusTextIsSuccess = isSuccess;
        PersistCurrentRuntimeTipState();
        RefreshRuntimeStatus();
    }

    /// <summary>
    /// 设置运行异常。
    /// </summary>
    /// <param name="messageKey">本地化文本键。</param>
    /// <param name="args">本地化文本参数。</param>
    private void SetRuntimeError(string messageKey, params object[] args)
    {
        SetRuntimeErrorCore(messageKey, args, null, source: null);
    }

    /// <summary>
    /// 设置带来源的运行异常。设备报警等来源用于后续只清除对应异常。
    /// </summary>
    /// <param name="messageKey">本地化文本键。</param>
    /// <param name="source">异常来源。</param>
    /// <param name="args">本地化文本参数。</param>
    private void SetRuntimeErrorWithSource(string messageKey, string? source, params object[] args)
    {
        SetRuntimeErrorCore(messageKey, args, null, source);
    }

    /// <summary>
    /// 设置运行异常文本。
    /// </summary>
    /// <param name="message">提示消息。</param>
    private void SetRuntimeErrorText(string message, string? source = null)
    {
        SetRuntimeErrorCore(null, Array.Empty<object>(), NormalizeRuntimeSummary(message), source);
    }

    /// <summary>
    /// 保存不截断的运行异常详情；界面摘要由刷新逻辑单独生成。
    /// </summary>
    private void SetRuntimeErrorDetailText(
        string message,
        string messageKey,
        string? source,
        params object[] args)
    {
        SetRuntimeErrorCore(messageKey, args, message.Trim(), source);
    }

    /// <summary>
    /// 统一写入运行异常字段，避免 key 与动态文本同时残留。
    /// </summary>
    /// <param name="messageKey">本地化文本键；为空时使用动态文本。</param>
    /// <param name="args">本地化文本参数。</param>
    /// <param name="message">动态文本兼容值。</param>
    /// <param name="source">异常来源。</param>
    private void SetRuntimeErrorCore(string? messageKey, object[] args, string? message, string? source)
    {
        _runtimeErrorKey = messageKey;
        _runtimeErrorArgs = args;
        _runtimeErrorText = message;
        _runtimeErrorSource = source;
        PersistCurrentRuntimeTipState();
        RefreshRuntimeError();
    }

    /// <summary>
    /// 清空运行异常。
    /// </summary>
    private void ClearRuntimeError()
    {
        _runtimeErrorKey = null;
        _runtimeErrorArgs = Array.Empty<object>();
        _runtimeErrorText = null;
        _runtimeErrorSource = null;
        _deviceAlarmRuntimeErrorText = null;
        _deviceAlarmPendingConfirmation = false;
        inputErrorTips.Clear();
        ApplyRuntimeErrorTone(hasError: false);
        PersistCurrentRuntimeTipState();
    }

    #endregion

    #region 运行提示持久化

    /// <summary>
    /// 恢复当前工位上一次保存的运行提示状态。
    /// </summary>
    private void RestoreCurrentRuntimeTipState()
    {
        try
        {
            // 未开工工位不恢复历史提示，避免显示与实际状态不符的旧进展。
            if (!RuntimeTipRestoreRules.ShouldRestoreRuntimeTip(
                    _weldTaskService.GetUnfinishedTask(CurrentStationNo) is not null))
            {
                ResetRuntimeTipStateToDefault();
                return;
            }

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
            _runtimeErrorSource = state.RuntimeErrorSource;
            _deviceAlarmPendingConfirmation = string.Equals(
                _runtimeErrorKey,
                TextKeys.Monitor.RuntimeError.DeviceAlarmPending,
                StringComparison.Ordinal);
            _deviceAlarmRuntimeErrorText = string.Equals(_runtimeErrorSource, RuntimeErrorSourceDeviceAlarm, StringComparison.Ordinal)
                ? _runtimeErrorText
                : null;
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.RestoreCurrentRuntimeTipState");
        }
    }

    /// <summary>
    /// 将运行提示重置为默认的等待业务操作状态。
    /// </summary>
    private void ResetRuntimeTipStateToDefault()
    {
        _runtimeStatusKey = TextKeys.Monitor.RuntimeStatus.Idle;
        _runtimeStatusArgs = Array.Empty<object>();
        _runtimeStatusText = null;
        _runtimeStatusTextIsSuccess = false;
        _runtimeErrorKey = null;
        _runtimeErrorArgs = Array.Empty<object>();
        _runtimeErrorText = null;
        _runtimeErrorSource = null;
        _deviceAlarmPendingConfirmation = false;
        _deviceAlarmRuntimeErrorText = null;
    }

    /// <summary>
    /// 保存当前工位运行状态和异常提示。
    /// </summary>
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
                RuntimeErrorText = _runtimeErrorText,
                RuntimeErrorSource = _runtimeErrorSource
            });
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "MonitorView.PersistCurrentRuntimeTipState");
        }
    }

    /// <summary>
    /// 序列化运行时Args。
    /// </summary>
    /// <param name="args">本地化文本参数。</param>
    /// <returns>处理后的文本。</returns>
    private static string? SerializeRuntimeArgs(object[] args)
    {
        if (args.Length == 0)
        {
            return null;
        }

        var values = args.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).ToArray();
        return JsonSerializer.Serialize(values);
    }

    /// <summary>
    /// 反序列化运行时Args。
    /// </summary>
    /// <param name="json">json。</param>
    /// <returns>解析到的对象；不存在时返回 null。</returns>
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

    #endregion

    #region 运行提示显示

    /// <summary>
    /// 刷新运行时Panels。
    /// </summary>
    private void RefreshRuntimePanels()
    {
        RefreshRuntimeStatus();
        RefreshRuntimeError();
    }

    /// <summary>
    /// 刷新运行状态。
    /// </summary>
    private void RefreshRuntimeStatus()
    {
        inputRunningStatus.Text = _runtimeStatusKey is null
            ? _runtimeStatusText ?? string.Empty
            : BuildLocalizedMessage(_runtimeStatusKey, _runtimeStatusArgs);
        ApplyRuntimeStatusTone();
    }

    /// <summary>
    /// 刷新运行异常。
    /// </summary>
    private void RefreshRuntimeError()
    {
        if (string.Equals(_runtimeErrorSource, RuntimeErrorSourceDeviceAlarm, StringComparison.Ordinal))
        {
            var alarmCount = Math.Max(1, PlcAlarmNotificationRules.SplitMessages(_runtimeErrorText).Count);
            inputErrorTips.Text = BuildLocalizedMessage(
                _deviceAlarmPendingConfirmation
                    ? TextKeys.Monitor.RuntimeError.DeviceAlarmPending
                    : TextKeys.Monitor.RuntimeError.DeviceAlarmSummary,
                alarmCount);
        }
        else
        {
            inputErrorTips.Text = _runtimeErrorKey is null
                ? _runtimeErrorText ?? string.Empty
                : BuildLocalizedMessage(_runtimeErrorKey, _runtimeErrorArgs);
        }

        ApplyRuntimeErrorTone(!string.IsNullOrWhiteSpace(inputErrorTips.Text));
    }

    /// <summary>
    /// 应用运行状态Tone。
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
    /// 应用运行异常Tone。
    /// </summary>
    /// <param name="hasError">当前是否存在异常提示。</param>
    private void ApplyRuntimeErrorTone(bool hasError)
    {
        var color = hasError ? UiColors.Status.Danger : UiColors.Status.Muted;
        grpErrorTips.ForeColor = color;
        inputErrorTips.ForeColor = color;
        btnClearErrorTips.Visible = hasError;
    }

    /// <summary>
    /// 获取运行状态颜色。
    /// </summary>
    /// <param name="messageKey">本地化文本键。</param>
    /// <returns>用于界面显示的颜色。</returns>
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

    /// <summary>
    /// 构建LocalizedMessage。
    /// </summary>
    /// <param name="messageKey">本地化文本键。</param>
    /// <param name="args">本地化文本参数。</param>
    /// <returns>处理后的文本。</returns>
    private string BuildLocalizedMessage(string messageKey, params object[] args)
    {
        return NormalizeRuntimeSummary(_localizer.GetString(messageKey, args));
    }

    /// <summary>
    /// 规范化运行时摘要。
    /// </summary>
    /// <param name="message">提示消息。</param>
    /// <returns>处理后的文本。</returns>
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

    #endregion

    #region 嵌套模型

    private sealed record PlcTextReadResult(bool IsSuccess, string Value, string Detail)
    {
        /// <summary>
        /// 处理成功。
        /// </summary>
        /// <param name="value">待处理值。</param>
        /// <returns>解析到的对象；不存在时返回 null。</returns>
        public static PlcTextReadResult Success(string value) => new(true, value, string.Empty);

        /// <summary>
        /// 处理Failed。
        /// </summary>
        /// <param name="detail">详情。</param>
        /// <returns>解析到的对象；不存在时返回 null。</returns>
        public static PlcTextReadResult Failed(string detail) => new(false, string.Empty, detail);
    }

    private sealed record ProductionMetricRow(string Name, string Value);

    private sealed record ProductHistoryDisplayOptions(
        string PointName,
        string PointNoHeader,
        string PointResultHeader,
        string PointCountHeader,
        int TouchCount,
        bool ShowTestFlagInHistory)
    {
        public static ProductHistoryDisplayOptions Default { get; } = new("焊点", "焊点序号", "焊点结果", "焊点数", 0, true);

        public static ProductHistoryDisplayOptions FromConfig(BizProductProcessConfig config, bool showTestFlagInHistory)
        {
            var pointName = NormalizeDisplayText(config.PointName, "焊点");
            return new ProductHistoryDisplayOptions(
                pointName,
                NormalizeDisplayText(config.PointNoHeader, $"{pointName}序号"),
                NormalizeDisplayText(config.PointResultHeader, $"{pointName}结果"),
                NormalizeDisplayText(config.PointCountHeader, $"{pointName}数"),
                config.TouchCount,
                showTestFlagInHistory);
        }
    }

    private sealed record SchemePreviewItem(int Sort, DimTestItem Item, BizSchemeDetail Detail);

    private sealed record RecipeCodeResolution(string RecipeCode, string Source, string Detail);

    #endregion
}
