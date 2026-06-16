using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;
using AutoWeldSystem.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing;
using System.Globalization;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Interfaces.UserManage;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.UI.Forms;

public partial class MainForm : BaseWindow
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISysUserService _userService;
    private readonly ILocalizationService _localizer;
    private readonly IPlcExpressionReadService _plcExpressionReadService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IProductProcessConfigService _productProcessConfigService;
    private readonly ITestSchemeConfigService _testSchemeConfigService;
    private readonly IProductRealtimePreviewService _productRealtimePreviewService;
    private readonly IProgramManageService _programManageService;
    private readonly IAppSettingsService _settingsService;
    private AppSettings _currentSettings;

    private readonly PermissionUiBinder _permissionUiBinder;
    private readonly PlcWriteDebugMessageFilter _plcWriteDebugMessageFilter;
    private readonly Dictionary<string, UserControl> _viewCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PageDefinition> _allPages;
    private readonly List<PageDefinition> _visiblePages = new();
    private StationDisplayForm? _station2DisplayForm;
    private bool _syncingLanguageSelection;
    private bool _startupTimeSyncQueued;

    public MainForm(
        IServiceProvider serviceProvider,
        ISysUserService userService,
        ILocalizationService localizer,
        IPlcExpressionReadService plcExpressionReadService,
        PermissionUiBinder permissionUiBinder,
        IWeldTaskService weldTaskService,
        IProductProcessConfigService productProcessConfigService,
        ITestSchemeConfigService testSchemeConfigService,
        IProductRealtimePreviewService productRealtimePreviewService,
        IProgramManageService programManageService,
        IAppSettingsService settingsService)
    {
        _serviceProvider = serviceProvider;
        _userService = userService;
        _localizer = localizer;
        _plcExpressionReadService = plcExpressionReadService;
        _weldTaskService = weldTaskService;
        _productProcessConfigService = productProcessConfigService;
        _testSchemeConfigService = testSchemeConfigService;
        _productRealtimePreviewService = productRealtimePreviewService;
        _programManageService = programManageService;
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();

        _permissionUiBinder = permissionUiBinder;
        _plcWriteDebugMessageFilter = new PlcWriteDebugMessageFilter(serviceProvider, this);

        InitializeComponent();
        Application.AddMessageFilter(_plcWriteDebugMessageFilter);

        WireSystemInfoEvents();
        _allPages = BuildPages();
        GlobalContext.SessionChanged += GlobalContext_SessionChanged;
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;

        RefreshShell();
    }

    /// <summary>
    /// MES 校时放在主界面显示后后台执行，避免 MES 离线时拖慢程序启动和登录。
    /// </summary>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        QueueStartupServerTimeSync();

        // Wait until the main window has completed its first layout pass. This also
        // ensures that Windows has finished enumerating the active display topology.
        // EnsureStation2DisplayWindow has an optional settings parameter. Wrap it in
        // an explicit parameterless delegate because WinForms invokes this callback
        // without reflection arguments after the first layout pass.
        BeginInvoke(new Action(() => EnsureStation2DisplayWindow()));
    }

    /// <summary>
    /// 语言变化时，导航标题和空页面提示也需要同步刷新。
    /// </summary>
    protected override void OnLanguageChanged()
    {
        RebuildLocalizedPages();
    }

    private void segmented1_SelectIndexChanged(object sender, AntdUI.IntEventArgs e)
    {
        EnsureViewLoaded(e.Value);
        DisplayView(e.Value);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        CloseStation2DisplayWindow();
        Application.RemoveMessageFilter(_plcWriteDebugMessageFilter);
        GlobalContext.SessionChanged -= GlobalContext_SessionChanged;
        _settingsService.SettingsChanged -= SettingsService_SettingsChanged;
        base.OnHandleDestroyed(e);
    }

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        try
        {
            var settings = e.CurrentSettings;
            Interlocked.Exchange(ref _currentSettings, settings);

            if (IsDisposed
                || (!e.HasChanged(nameof(AppSettings.EnableDualStation))
                    && !e.HasChanged(nameof(AppSettings.EnableDualWorkOrder))))
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(() => ApplyDualModeRuntimeSettingsSafely(settings));
                return;
            }

            ApplyDualModeRuntimeSettingsSafely(settings);
        }
        catch (Exception ex)
        {
            _serviceProvider.GetService<IProgramExceptionLogService>()?
                .Write(ex, "MainForm.SettingsChanged");
        }
    }

    private void ApplyDualModeRuntimeSettingsSafely(AppSettings settings)
    {
        try
        {
            ApplyDualModeRuntimeSettings(settings);
        }
        catch (Exception ex)
        {
            _serviceProvider.GetService<IProgramExceptionLogService>()?
                .Write(ex, "MainForm.ApplyDualModeRuntimeSettings");
        }
    }

    private void GlobalContext_SessionChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(RefreshShell);
            return;
        }

        RefreshShell();
        EnsureStation2DisplayWindow();
    }

    private void ApplyDualModeRuntimeSettings(AppSettings settings)
    {
        GetPrimaryMonitorView()?.ApplyRuntimeSettingsChanged(
            settings,
            readOnly: false,
            enableBusinessSignalReconcile: true,
            triggerBusinessSignalReconcile: true);

        if (!settings.EnableDualStation)
        {
            CloseStation2DisplayWindow();
            return;
        }

        EnsureStation2DisplayWindow(settings);
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    /// <summary>
    /// 页面标题是运行时代码生成的，所以语言切换后要重新构建。
    /// </summary>
    private void RebuildLocalizedPages()
    {
        _allPages.Clear();
        _allPages.AddRange(BuildPages());
        RefreshShell();
    }

    /// <summary>
    /// MainForm owns global shell information such as current user, language, and auth actions.
    /// </summary>
    private void RefreshShell()
    {
        ApplySystemInfoTexts();
        BindLanguageSelection();
        BindSessionInfo();
        InitializeNavigation();
        _permissionUiBinder.Apply(this);
    }

    private void WireSystemInfoEvents()
    {
        select_Lang.SelectedIndexChanged += Language_SelectedIndexChanged;
        btnSwitchUser.Click += SwitchUser_Click;
        btnLogout.Click += Logout_Click;
        btnAddressPreview.Click += AddressPreview_Click;
    }

    private void InitializeNavigation()
    {
        var previousPermissionCode = GetCurrentPagePermissionCode();

        segmented1.Items.Clear();
        _visiblePages.Clear();
        _visiblePages.AddRange(_allPages.Where(page => _userService.HasPermission(page.PermissionCode)));

        if (_visiblePages.Count == 0)
        {
            ShowEmptyPermissionPage();
            return;
        }

        foreach (var page in _visiblePages)
        {
            segmented1.Items.Add(new AntdUI.SegmentedItem { Text = page.Title });
        }

        var targetIndex = 0;
        if (!string.IsNullOrWhiteSpace(previousPermissionCode))
        {
            var previousIndex = _visiblePages.FindIndex(page =>
                string.Equals(page.PermissionCode, previousPermissionCode, StringComparison.OrdinalIgnoreCase));

            if (previousIndex >= 0)
            {
                targetIndex = previousIndex;
            }
        }

        segmented1.SelectIndex = targetIndex;
        EnsureViewLoaded(targetIndex);
        DisplayView(targetIndex);
    }

    private void EnsureViewLoaded(int viewIndex)
    {
        if (viewIndex < 0 || viewIndex >= _visiblePages.Count)
        {
            return;
        }

        var page = _visiblePages[viewIndex];
        if (_viewCache.TryGetValue(page.PermissionCode, out var cachedView))
        {
            _permissionUiBinder.Apply(cachedView);
            return;
        }

        var view = page.Factory();
        view.Dock = DockStyle.Fill;
        _permissionUiBinder.Apply(view);
        _viewCache[page.PermissionCode] = view;
    }

    private void DisplayView(int viewIndex)
    {
        if (viewIndex < 0 || viewIndex >= _visiblePages.Count)
        {
            return;
        }

        var page = _visiblePages[viewIndex];
        if (!_viewCache.TryGetValue(page.PermissionCode, out var view))
        {
            return;
        }

        pnlContent.Controls.Clear();
        pnlContent.Tag = page.PermissionCode;
        pnlContent.Controls.Add(view);
    }

    private string? GetCurrentPagePermissionCode()
    {
        return pnlContent.Tag as string;
    }

    private void ShowEmptyPermissionPage()
    {
        pnlContent.Controls.Clear();
        pnlContent.Tag = null;
        pnlContent.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = _localizer.GetString(TextKeys.Main.EmptyPermissionPage)
        });
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

        if (string.Equals(GlobalContext.CurrentLanguage, targetLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _localizer.SetLanguage(targetLanguage);
    }

    private void SwitchUser_Click(object? sender, EventArgs e)
    {
        if (!ConfirmAction(TextKeys.Monitor.Message.SwitchUserConfirm, TextKeys.Monitor.Title.SwitchUserTitle))
        {
            return;
        }

        GlobalContext.IsLogout = true;
        Close();
    }

    private void Logout_Click(object? sender, EventArgs e)
    {
        if (!ConfirmAction(TextKeys.Monitor.Message.LogoutConfirm, TextKeys.Monitor.Title.LogoutTitle))
        {
            return;
        }

        GlobalContext.IsLogout = true;
        Close();
    }

    /// <summary>
    /// 页面定义统一集中在这里，后续新增页面时改动点更少。
    /// </summary>
    private List<PageDefinition> BuildPages()
    {
        return new List<PageDefinition>
        {
            new(_localizer.GetString(TextKeys.Main.NavMonitor), PermissionCodes.Pages.Monitor, CreatePrimaryMonitorView),
            new(_localizer.GetString(TextKeys.Main.NavDataManage), PermissionCodes.Pages.DataManage, () => _serviceProvider.GetRequiredService<DataManageView>()),
            new(_localizer.GetString(TextKeys.Main.NavUserManage), PermissionCodes.Pages.UserManage, () => _serviceProvider.GetRequiredService<UserManageView>()),
            new(_localizer.GetString(TextKeys.Main.NavProgramManage), PermissionCodes.Pages.ProgramManage, () => _serviceProvider.GetRequiredService<ProgramManageView>()),
            new(_localizer.GetString(TextKeys.Main.NavLogManage), PermissionCodes.Pages.LogManage, () => _serviceProvider.GetRequiredService<LogManageView>()),
            new(_localizer.GetString(TextKeys.Main.NavStateManage), PermissionCodes.Pages.StateManage, () => _serviceProvider.GetRequiredService<StateManageView>()),
            new(_localizer.GetString(TextKeys.Main.NavSystemSetting), PermissionCodes.Pages.SystemSetting, () => _serviceProvider.GetRequiredService<SystemSettingView>()),
            new(_localizer.GetString(TextKeys.Main.NavAddressManage), PermissionCodes.Pages.AddressManage, () => _serviceProvider.GetRequiredService<AddressManageView>())
        };
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

    private void BindLanguageSelection()
    {
        _syncingLanguageSelection = true;
        try
        {
            select_Lang.Items.Clear();
            select_Lang.Items.AddRange(new object[]
            {
                _localizer.GetString(TextKeys.Common.LanguageChinese),
                _localizer.GetString(TextKeys.Common.LanguageEnglish)
            });

            select_Lang.SelectedIndex = GlobalContext.CurrentLanguage == AppConstants.Languages.English ? 1 : 0;
        }
        finally
        {
            _syncingLanguageSelection = false;
        }
    }

    private void ApplySystemInfoTexts()
    {
        lblCurUser.Text = _localizer.GetString(TextKeys.Monitor.Label.CurrentUser);
        lblCurLang.Text = _localizer.GetString(TextKeys.Monitor.Label.CurrentLang);
        btnSwitchUser.Text = _localizer.GetString(TextKeys.Monitor.Button.SwitchUser);
        btnLogout.Text = _localizer.GetString(TextKeys.Monitor.Button.Logout);
        btnAddressPreview.Text = "PLC 地址预览";
    }

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

    private MonitorView CreatePrimaryMonitorView()
    {
        var view = _serviceProvider.GetRequiredService<MonitorView>();
        var settings = CurrentSettings;
        if (settings.EnableDualStation)
        {
            view.ConfigureStationView(
                ProductionConstants.Stations.DefaultStationNo,
                readOnly: false,
                enableBusinessSignalReconcile: true);
        }

        return view;
    }

    private void EnsureStation2DisplayWindow(AppSettings? currentSettings = null)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        var settings = currentSettings ?? CurrentSettings;
        if (!settings.EnableDualStation || !_userService.HasPermission(PermissionCodes.Pages.Monitor))
        {
            CloseStation2DisplayWindow();
            return;
        }

        try
        {
            var readOnly = !settings.EnableDualWorkOrder;
            if (_station2DisplayForm is { IsDisposed: false })
            {
                _station2DisplayForm.ApplyRuntimeSettingsChanged(settings, readOnly);
                return;
            }

            var monitorView = _serviceProvider.GetRequiredService<MonitorView>();
            _station2DisplayForm = new StationDisplayForm(
                monitorView,
                _localizer,
                _permissionUiBinder,
                stationNo: 2,
                readOnly);
            _station2DisplayForm.FormClosed += (_, _) => _station2DisplayForm = null;
            PlaceStation2DisplayWindow(_station2DisplayForm);
            // Keep the production display independent from the shell window so it can
            // remain visible and maximized on an extended monitor.
            _station2DisplayForm.Show();
            _station2DisplayForm.Activate();
        }
        catch (Exception ex)
        {
            _serviceProvider.GetService<IProgramExceptionLogService>()?
                .Write(ex, "MainForm.EnsureStation2DisplayWindow");
        }
    }

    private void PlaceStation2DisplayWindow(Form form)
    {
        var secondaryScreen = Screen.AllScreens.FirstOrDefault(screen => !screen.Primary);
        if (secondaryScreen is not null)
        {
            form.Bounds = secondaryScreen.WorkingArea;
            form.WindowState = FormWindowState.Maximized;
            return;
        }

        var ownerScreen = Screen.FromControl(this);
        var width = Math.Min(1280, Math.Max(800, ownerScreen.WorkingArea.Width - 160));
        var height = Math.Min(900, Math.Max(600, ownerScreen.WorkingArea.Height - 120));
        form.Bounds = new Rectangle(
            ownerScreen.WorkingArea.Left + 80,
            ownerScreen.WorkingArea.Top + 60,
            width,
            height);
        form.WindowState = FormWindowState.Normal;
    }

    private void CloseStation2DisplayWindow()
    {
        if (_station2DisplayForm is null)
        {
            return;
        }

        try
        {
            if (!_station2DisplayForm.IsDisposed)
            {
                _station2DisplayForm.Close();
            }
        }
        catch
        {
            // Secondary display cleanup should not block logout or shutdown.
        }
        finally
        {
            _station2DisplayForm = null;
        }
    }

    private MonitorView? GetPrimaryMonitorView()
    {
        return _viewCache.TryGetValue(PermissionCodes.Pages.Monitor, out var view)
            ? view as MonitorView
            : null;
    }

    private void AddressPreview_Click(object? sender, EventArgs e)
    {
        var rows = BuildCurrentAddressPreviewRows();
        using var form = new AddressPreviewForm(rows, _plcExpressionReadService, _localizer);
        form.ShowDialog(this);
    }

    private void QueueStartupServerTimeSync()
    {
        if (_startupTimeSyncQueued)
        {
            return;
        }

        _startupTimeSyncQueued = true;
        _ = Task.Run(async () =>
        {
            try
            {
                await _weldTaskService.SyncServerTimeAsync();
            }
            catch (Exception ex)
            {
                _serviceProvider.GetService<IProgramExceptionLogService>()?
                    .Write(ex, "MainForm.StartupServerTimeSync");
            }
        });
    }

    /// <summary>
    /// 主窗体统一负责生成 PLC 地址预览行，避免再依赖 MonitorView 的私有界面状态。
    /// </summary>
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

            AddAddressPreviewRow(rows, identity, "焊点头", touchText, "焊点编号", ResolveTouchNoBase(config), touchContextOffset, config.TouchNoExpr);
            AddAddressPreviewRow(rows, identity, "焊点头", touchText, "焊点结果", ResolveTouchResultBase(config), touchContextOffset, config.TouchResultExpr);

            foreach (var schemeItem in schemeItems)
            {
                var item = schemeItem.Item;
                var detail = schemeItem.Detail;
                if (detail.EnableActual)
                {
                    AddAddressPreviewRow(rows, identity, "测试项", touchText, $"{item.ItemName} 实际值", config.TestBase, testContextOffset, item.ActualExpression);
                }

                if (detail.EnableUpper)
                {
                    AddAddressPreviewRow(rows, identity, "测试项", touchText, $"{item.ItemName} 上限", config.TestBase, testContextOffset, item.UpperExpression);
                }

                if (detail.EnableLower)
                {
                    AddAddressPreviewRow(rows, identity, "测试项", touchText, $"{item.ItemName} 下限", config.TestBase, testContextOffset, item.LowerExpression);
                }

                if (detail.EnableResult)
                {
                    AddAddressPreviewRow(rows, identity, "测试项", touchText, $"{item.ItemName} 结果", config.TestBase, testContextOffset, item.ResultExpression);
                }
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

    private static string ResolveTouchNoBase(BizProductProcessConfig config)
        => string.IsNullOrWhiteSpace(config.TouchNoBase) ? config.TouchBase : config.TouchNoBase!.Trim();

    private static string ResolveTouchResultBase(BizProductProcessConfig config)
        => string.IsNullOrWhiteSpace(config.TouchResultBase) ? config.TouchBase : config.TouchResultBase!.Trim();

    /// <summary>
    /// 优先使用实时预览服务已缓存的产品身份，缓存不存在时再回退到当前生产运行态。
    /// </summary>
    private ProductIdentity? ResolveCurrentPreviewIdentity(int stationNo)
    {
        var snapshot = _productRealtimePreviewService.GetCurrent(stationNo);
        if (snapshot is not null && !string.IsNullOrWhiteSpace(snapshot.ProductNum))
        {
            return new ProductIdentity(
                stationNo,
                snapshot.ProductNum.Trim(),
                snapshot.ProductModel?.Trim() ?? string.Empty,
                "RealtimePreview");
        }

        return ResolveOnlineProductIdentity(stationNo);
    }

    private ProductIdentity? ResolveOnlineProductIdentity(int stationNo)
    {
        var state = _weldTaskService.CurrentState;
        var selectedProgram = state.CurrentStationNo == stationNo ? state.SelectedProgram : null;
        var currentWorkOrder = state.CurrentStationNo == stationNo ? state.CurrentWorkOrder : null;
        var activeTask = state.CurrentStationNo == stationNo ? state.ActiveTask : null;

        if (state.StationStates.TryGetValue(stationNo, out var station))
        {
            selectedProgram ??= station.SelectedProgram;
            currentWorkOrder ??= station.CurrentWorkOrder;
            activeTask ??= station.ActiveTask;
        }

        var localProgram = selectedProgram is not null
            ? ResolveLocalProgram(selectedProgram)
            : ResolveLocalProgramById(activeTask?.ProgramId, activeTask?.DeviceId);
        if (!string.IsNullOrWhiteSpace(localProgram?.ProductNum))
        {
            return new ProductIdentity(
                stationNo,
                localProgram.ProductNum.Trim(),
                localProgram.ProductModel?.Trim() ?? string.Empty,
                "LocalProgram");
        }

        if (!string.IsNullOrWhiteSpace(currentWorkOrder?.ProdNum))
        {
            return new ProductIdentity(
                stationNo,
                currentWorkOrder.ProdNum.Trim(),
                currentWorkOrder.ProdModel?.Trim() ?? string.Empty,
                "MES");
        }

        if (!string.IsNullOrWhiteSpace(activeTask?.ProductNum))
        {
            return new ProductIdentity(
                stationNo,
                activeTask.ProductNum.Trim(),
                activeTask.ProductModel?.Trim() ?? string.Empty,
                "Task");
        }

        return null;
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
                Item = allItems.FirstOrDefault(item => item.ItemId == detail.ItemId),
                Detail = detail
            })
            .Where(item => item.Item is not null)
            .Where(item => HasAnyEnabledRole(item.Detail))
            .Select(item => new SchemePreviewItem(item.Sort, item.Item!, item.Detail))
            .ToList();
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

    private static bool SameText(string? left, string? right)
    {
        return string.Equals(
            left?.Trim(),
            right?.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private int CurrentStationNo
    {
        get
        {
            if (GetPrimaryMonitorView() is { } monitorView)
            {
                return monitorView.ViewStationNo;
            }

            var stationNo = _weldTaskService.CurrentState.CurrentStationNo;
            return stationNo <= 0
                ? ProductionConstants.Stations.DefaultStationNo
                : stationNo;
        }
    }

    private sealed record ProductIdentity(
        int StationNo,
        string ProductNum,
        string ProductModel,
        string Source);

    private static bool HasAnyEnabledRole(BizSchemeDetail detail)
    {
        return detail.EnableActual || detail.EnableUpper || detail.EnableLower || detail.EnableResult;
    }

    private sealed record SchemePreviewItem(int Sort, DimTestItem Item, BizSchemeDetail Detail);

    private sealed record PageDefinition(string Title, string PermissionCode, Func<UserControl> Factory);
}
