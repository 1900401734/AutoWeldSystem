using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Center;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.Enums;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.UI.Base;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Mes;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.Core.Runtime;

namespace AutoWeldSystem.UI.Views;

public partial class SystemSettingView : BaseView
{
    private const int SuccessMessageAutoCloseSeconds = 4;
    private const int AlertMessageAutoCloseSeconds = 6;

    private enum DeviceSyncOutcome
    {
        Failed,
        Synced,
        Registered
    }

    private static readonly LocalizedOption<string>[] PlcTypeOptions =
    {
        new(AppConstants.PlcTypes.ModbusTcp, TextKeys.SystemSetting.PlcTypeModbusTcp),
        new(AppConstants.PlcTypes.TcpSocket, TextKeys.SystemSetting.PlcTypeTcpSocket),
        new(AppConstants.PlcTypes.SiemensS71200, TextKeys.SystemSetting.PlcTypeSiemensS71200)
    };

    private static readonly LocalizedOption<string>[] PlcStringNumericFormatModeOptions =
    {
        new(AppConstants.PlcStringNumericFormatModes.Truncate, TextKeys.SystemSetting.OptionPlcFormatTruncate),
        new(AppConstants.PlcStringNumericFormatModes.Round, TextKeys.SystemSetting.OptionPlcFormatRound)
    };

    private static readonly LocalizedOption<string>[] PlcAlarmTriggerModeOptions =
    {
        new(AppConstants.PlcAlarmTriggerModes.AddressOnly, TextKeys.SystemSetting.OptionPlcAlarmAddressOnly),
        new(AppConstants.PlcAlarmTriggerModes.DeviceStatusAndAddress, TextKeys.SystemSetting.OptionPlcAlarmDeviceStatusAndAddress)
    };

    private static readonly LocalizedOption<UploadMode>[] UploadModeOptions =
    {
        new(UploadMode.Realtime, TextKeys.SystemSetting.OptionUploadRealtime),
        new(UploadMode.Quantity, TextKeys.SystemSetting.OptionUploadQuantity),
        new(UploadMode.Batch, TextKeys.SystemSetting.OptionUploadBatch)
    };

    private static readonly LocalizedOption<string>[] ProcessParameterDeviceTypeOptions =
    {
        new(ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic, TextKeys.SystemSetting.OptionDeviceElectromagnetic),
        new(ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck, TextKeys.SystemSetting.OptionDeviceWholePieceCheck),
        new(ProductionConstants.ProcessParameterDeviceTypes.WholePieceWeld, TextKeys.SystemSetting.OptionDeviceWholePieceWeld)
    };

    private static readonly LocalizedOption<string>[] InspectionResultSourceOptions =
    {
        new(ProductionConstants.InspectionResultSources.Plc, TextKeys.SystemSetting.OptionInspectionResultSourcePlc),
        new(ProductionConstants.InspectionResultSources.Program, TextKeys.SystemSetting.OptionInspectionResultSourceProgram)
    };

    private static readonly LocalizedOption<string>[] RealtimePointNumberSourceOptions =
    {
        new(ProductionConstants.RealtimePointNumberSources.Plc, TextKeys.SystemSetting.OptionRealtimePointNumberSourcePlc),
        new(ProductionConstants.RealtimePointNumberSources.Program, TextKeys.SystemSetting.OptionRealtimePointNumberSourceProgram)
    };

    private static readonly LocalizedOption<string>[] PairedAggregationModeOptions =
    {
        new(ProductionConstants.PairedAggregationModes.Average, TextKeys.SystemSetting.OptionPairedAggregationAverage),
        new(ProductionConstants.PairedAggregationModes.Maximum, TextKeys.SystemSetting.OptionPairedAggregationMaximum)
    };

    private static readonly LocalizedOption<string>[] CenterServerSystemTypeOptions =
    {
        new(CenterServerConstants.SystemTypes.Electromagnetic, TextKeys.SystemSetting.OptionDeviceElectromagnetic),
        new(CenterServerConstants.SystemTypes.WholePiece, TextKeys.SystemSetting.OptionCenterWholePiece),
        new(CenterServerConstants.SystemTypes.Other, TextKeys.SystemSetting.OptionCenterOther)
    };

    private static readonly MesRouteInputDefinition[] MesRouteInputDefinitions =
    {
        new("User", TextKeys.SystemSetting.RouteUser, MesEndpointRouteRules.UserDefaultRoute, settings => settings.MesUserRoute, (settings, route) => settings.MesUserRoute = route),
        new("WorkOrder", TextKeys.SystemSetting.RouteWorkOrder, MesEndpointRouteRules.WorkOrderDefaultRoute, settings => settings.MesWorkOrderRoute, (settings, route) => settings.MesWorkOrderRoute = route),
        new("ServerTime", TextKeys.SystemSetting.RouteServerTime, MesEndpointRouteRules.ServerTimeDefaultRoute, settings => settings.MesServerTimeRoute, (settings, route) => settings.MesServerTimeRoute = route),
        new("ProgramManage", TextKeys.SystemSetting.RouteProgram, MesEndpointRouteRules.ProgramManageDefaultRoute, settings => settings.MesProgramManageRoute, (settings, route) => settings.MesProgramManageRoute = route),
        new("StartWork", TextKeys.SystemSetting.RouteStartWork, MesEndpointRouteRules.StartWorkDefaultRoute, settings => settings.MesStartWorkRoute, (settings, route) => settings.MesStartWorkRoute = route),
        new("WorkStatus", TextKeys.SystemSetting.RouteWorkStatus, MesEndpointRouteRules.WorkStatusDefaultRoute, settings => settings.MesWorkStatusRoute, (settings, route) => settings.MesWorkStatusRoute = route),
        new("EndWork", TextKeys.SystemSetting.RouteEndWork, MesEndpointRouteRules.EndWorkDefaultRoute, settings => settings.MesEndWorkRoute, (settings, route) => settings.MesEndWorkRoute = route),
        new("ReportFile", TextKeys.SystemSetting.RouteReportFile, MesEndpointRouteRules.ReportFileDefaultRoute, settings => settings.MesReportFileRoute, (settings, route) => settings.MesReportFileRoute = route),
        new("PostData", TextKeys.SystemSetting.RoutePostData, MesEndpointRouteRules.PostDataDefaultRoute, settings => settings.MesPostDataRoute, (settings, route) => settings.MesPostDataRoute = route),
        new("Device", TextKeys.SystemSetting.RouteDevice, MesEndpointRouteRules.DeviceDefaultRoute, settings => settings.MesDeviceRoute, (settings, route) => settings.MesDeviceRoute = route),
        new("DeviceStatus", TextKeys.SystemSetting.RouteDeviceStatus, MesEndpointRouteRules.DeviceStatusDefaultRoute, settings => settings.MesDeviceStatusRoute, (settings, route) => settings.MesDeviceStatusRoute = route),
        new("DeviceStatusQuery", TextKeys.SystemSetting.RouteDeviceStatusQuery, MesEndpointRouteRules.DeviceStatusQueryDefaultRoute, settings => settings.DeviceStatusQueryRoute, (settings, route) => settings.DeviceStatusQueryRoute = route),
        new("DeviceIdSet", TextKeys.SystemSetting.RouteDeviceIdSet, MesEndpointRouteRules.DeviceIdSetDefaultRoute, settings => settings.DeviceIdSetRoute, (settings, route) => settings.DeviceIdSetRoute = route),
        new("Sys", TextKeys.SystemSetting.RouteSys, MesEndpointRouteRules.SysDefaultRoute, settings => settings.MesSysRoute, (settings, route) => settings.MesSysRoute = route)
    };

    private readonly IAppSettingsService _settingsService;
    private readonly IMesProvider _mesProvider;
    private readonly ILocalizationService _localizer;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IWindowsShellIntegrationService _windowsShellIntegrationService;
    private readonly IProgramManageService _programManageService;

    private bool _initialized;
    private bool _syncingPlcTypeSelection;
    private bool _syncingPlcStringNumericFormatModeSelection;
    private bool _syncingPlcAlarmTriggerModeSelection;
    private bool _syncingUploadModeSelection;
    private bool _syncingProcessParameterDeviceTypeSelection;
    private bool _syncingInspectionResultSourceSelection;
    private bool _syncingRealtimePointNumberSourceSelection;
    private bool _syncingPairedAggregationModeSelection;
    private bool _syncingCenterServerSystemTypeSelection;
    private bool _deviceManagementStateKnown;
    private string _selectedPlcType = AppConstants.PlcTypes.ModbusTcp;
    private string _selectedPlcStringNumericFormatMode = AppConstants.PlcStringNumericFormatModes.Truncate;
    private string _selectedPlcAlarmTriggerMode = AppConstants.PlcAlarmTriggerModes.DeviceStatusAndAddress;
    private UploadMode _selectedUploadMode = UploadMode.Quantity;
    private string _selectedProcessParameterDeviceType = ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic;
    private string _selectedInspectionResultSource = ProductionConstants.InspectionResultSources.Plc;
    private string _selectedRealtimePointNumberSource = ProductionConstants.RealtimePointNumberSources.Plc;
    private string _selectedPairedAggregationMode = ProductionConstants.PairedAggregationModes.Average;
    private string _selectedCenterServerSystemType = CenterServerConstants.SystemTypes.Other;
    private AppSettings _currentSettings;
    private SystemSettingLayoutMode? _lastLayoutMode;
    private int _deviceSyncVersion;
    private int _suppressSettingsChangedBinding;

    public SystemSettingView(
        IAppSettingsService settingsService,
        IMesProvider mesProvider,
        ILocalizationService localizer,
        IPlcCommunicationService plcCommunicationService,
        IWeldTaskService weldTaskService,
        IWindowsShellIntegrationService windowsShellIntegrationService,
        IProgramManageService programManageService)
    {
        InitializeComponent();

        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
        _mesProvider = mesProvider;
        _localizer = localizer;
        _plcCommunicationService = plcCommunicationService;
        _weldTaskService = weldTaskService;
        _windowsShellIntegrationService = windowsShellIntegrationService;
        _programManageService = programManageService;
        _weldTaskService.StateChanged += WeldTaskService_StateChanged;

        WireEvents();
    }

    protected override void OnLanguageChanged()
    {
        var scrollOffset = new Point(
            -basicSettingsViewport.AutoScrollPosition.X,
            -basicSettingsViewport.AutoScrollPosition.Y);

        ApplyLocalizedTexts();
        if (!_initialized)
        {
            return;
        }

        BindPlcTypeOptions();
        BindPlcStringNumericFormatModeOptions();
        BindPlcAlarmTriggerModeOptions();
        BindUploadModeOptions();
        BindProcessParameterDeviceTypeOptions();
        BindInspectionResultSourceOptions();
        BindRealtimePointNumberSourceOptions();
        BindPairedAggregationModeOptions();
        BindCenterServerSystemTypeOptions();
        ApplyBasicSettingsLayout(force: true);
        basicSettingsViewport.AutoScrollPosition = scrollOffset;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (_initialized)
        {
            return;
        }

        _initialized = true;
        LoadSettings();
        RefreshDeviceManagementEnabled(force: true);
        ApplyBasicSettingsLayout(force: true);
    }

    /// <summary>
    /// 页面重新显示时刷新设备管理模块状态，确保开工和完工后的界面权限及时更新。
    /// </summary>
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible)
        {
            ApplyBasicSettingsLayout();
        }

        if (Visible && _initialized)
        {
            RefreshDeviceManagementEnabled();
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyBasicSettingsLayout();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ApplyBasicSettingsLayout(force: true);
    }

    /// <summary>
    /// 根据视口尺寸和 DPI 选择基础设置页的列数，并重复使用现有控件实例。
    /// </summary>
    private void ApplyBasicSettingsLayout(bool force = false)
    {
        if (basicSettingsViewport is null || basicSettingsViewport.IsDisposed)
        {
            return;
        }

        var mode = SystemSettingLayoutRules.ResolveMode(basicSettingsViewport.ClientSize.Width, DeviceDpi);
        if (!force && mode == _lastLayoutMode)
        {
            return;
        }

        basicSettingsLayout.SuspendLayout();
        try
        {
            ConfigureBasicSettingsGrid(mode);
            _lastLayoutMode = mode;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("SystemSettingView responsive layout failed: {0}", ex);
            ConfigureBasicSettingsGrid(SystemSettingLayoutMode.SingleColumn);
        }
        finally
        {
            basicSettingsLayout.ResumeLayout(true);
        }
    }

    /// <summary>
    /// 仅移动三个语义列面板，不重建任何输入控件或业务绑定。
    /// </summary>
    private void ConfigureBasicSettingsGrid(SystemSettingLayoutMode mode)
    {
        basicSettingsLayout.ColumnStyles.Clear();
        basicSettingsLayout.RowStyles.Clear();
        basicSettingsLayout.SetColumnSpan(leftSettingsColumn, 1);
        basicSettingsLayout.SetColumnSpan(middleSettingsColumn, 1);
        basicSettingsLayout.SetColumnSpan(rightSettingsColumn, 1);

        switch (mode)
        {
            case SystemSettingLayoutMode.ThreeColumns:
                basicSettingsLayout.ColumnCount = 3;
                basicSettingsLayout.RowCount = 1;
                basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
                basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
                basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33334F));
                basicSettingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                basicSettingsLayout.SetCellPosition(leftSettingsColumn, new TableLayoutPanelCellPosition(0, 0));
                basicSettingsLayout.SetCellPosition(middleSettingsColumn, new TableLayoutPanelCellPosition(1, 0));
                basicSettingsLayout.SetCellPosition(rightSettingsColumn, new TableLayoutPanelCellPosition(2, 0));
                break;

            case SystemSettingLayoutMode.TwoColumns:
                basicSettingsLayout.ColumnCount = 2;
                basicSettingsLayout.RowCount = 2;
                basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                basicSettingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                basicSettingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                basicSettingsLayout.SetCellPosition(leftSettingsColumn, new TableLayoutPanelCellPosition(0, 0));
                basicSettingsLayout.SetCellPosition(middleSettingsColumn, new TableLayoutPanelCellPosition(1, 0));
                basicSettingsLayout.SetCellPosition(rightSettingsColumn, new TableLayoutPanelCellPosition(0, 1));
                basicSettingsLayout.SetColumnSpan(rightSettingsColumn, 2);
                break;

            default:
                basicSettingsLayout.ColumnCount = 1;
                basicSettingsLayout.RowCount = 3;
                basicSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                basicSettingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                basicSettingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                basicSettingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                basicSettingsLayout.SetCellPosition(leftSettingsColumn, new TableLayoutPanelCellPosition(0, 0));
                basicSettingsLayout.SetCellPosition(middleSettingsColumn, new TableLayoutPanelCellPosition(0, 1));
                basicSettingsLayout.SetCellPosition(rightSettingsColumn, new TableLayoutPanelCellPosition(0, 2));
                break;
        }
    }

    /// <summary>
    /// 统一绑定运行时事件，便于集中维护按钮和下拉框行为。
    /// </summary>
    private void WireEvents()
    {
        btnSaveAll.Click += SaveAll_Click;
        btnSyncDevice.Click += SyncDevice_ClickAsync;
        btnTestConnection.Click += TestConnection_ClickAsync;
        btnConnectPlc.Click += ConnectPlc_ClickAsync;
        btnChangeLogPath.Click += (_, _) => SelectFolder(input_LogsPath, BuildFieldName(grpDeviceConfig.Text, lblLogPath.Text));
        btnChangeDataPath.Click += (_, _) => SelectFolder(input_DataPath, BuildFieldName(grpDeviceConfig.Text, lblDataPath.Text));
        btnOpenLogPath.Click += (_, _) => OpenFolder(input_LogsPath.Text, BuildFieldName(grpDeviceConfig.Text, lblLogPath.Text));
        btnOpenDataPath.Click += (_, _) => OpenFolder(input_DataPath.Text, BuildFieldName(grpDeviceConfig.Text, lblDataPath.Text));
        select_PlcType.SelectedIndexChanged += Select_PlcType_SelectedIndexChanged;
        chkEnablePlcStringNumericFormatting.CheckedChanged += ChkEnablePlcStringNumericFormatting_CheckedChanged;
        chkEnablePlcAlarmReading.CheckedChanged += ChkEnablePlcAlarmReading_CheckedChanged;
        selectPlcAlarmTriggerMode.SelectedIndexChanged += SelectPlcAlarmTriggerMode_SelectedIndexChanged;
        selectPlcStringNumericFormatMode.SelectedIndexChanged += SelectPlcStringNumericFormatMode_SelectedIndexChanged;
        selectUploadMode.SelectedIndexChanged += SelectUploadMode_SelectedIndexChanged;
        chkEnableAutoStart.CheckedChanged += ChkEnableAutoStart_CheckedChanged;
        chkEnablePostDataCustomHeader.CheckedChanged += ChkEnablePostDataCustomHeader_CheckedChanged;
        selectProcessParameterDeviceType.SelectedIndexChanged += SelectProcessParameterDeviceType_SelectedIndexChanged;
        selectInspectionResultSource.SelectedIndexChanged += SelectInspectionResultSource_SelectedIndexChanged;
        selectRealtimePointNumberSource.SelectedIndexChanged += SelectRealtimePointNumberSource_SelectedIndexChanged;
        selectPairedAggregationMode.SelectedIndexChanged += SelectPairedAggregationMode_SelectedIndexChanged;
        selectCenterServerSystemType.SelectedIndexChanged += SelectCenterServerSystemType_SelectedIndexChanged;
        chkEnableDualStation.CheckedChanged += (_, _) => UpdateStationDisplayNameVisibility();
    }

    #region Events Handler

    private async void SaveAll_Click(object? sender, EventArgs e)
    {
        if (!TryBuildSettings(out var settings))
        {
            return;
        }

        btnSaveAll.Enabled = false;
        try
        {
            var previousSettings = _currentSettings;
            if (!CanSaveDeviceManagementChange(previousSettings, settings))
            {
                BindSettings(previousSettings);
                RefreshDeviceManagementEnabled(force: true);
                return;
            }

            if (!CanSaveRuntimeModeChange(previousSettings, settings)
                || !CanSaveInspectionResultSourceChange(previousSettings, settings)
                || !CanSavePairedAggregationModeChange(previousSettings, settings)
                || !CanSaveRealtimePointNumberSourceChange(previousSettings, settings))
            {
                BindSettings(previousSettings);
                return;
            }

            var shouldSyncDevice = HasDeviceIdentityChanged(previousSettings, settings);
            var shouldRestartPlc = HasPlcCommunicationChanged(previousSettings, settings);

            var savedSettings = _settingsService.Save(settings);
            _currentSettings = savedSettings;
            BindSettings(savedSettings);
            ApplyStartupIntegrationWithWarning(savedSettings);

            if (shouldRestartPlc)
            {
                await _plcCommunicationService.RestartAsync();
            }

            // Update DeviceId in all local programs when device ID changes to prevent MES sync failures
            if (HasDeviceIdChanged(previousSettings, settings))
            {
                await _programManageService.UpdateAllProgramsDeviceIdAsync(settings.DeviceId);
            }

            if (shouldSyncDevice)
            {
                ShowInfo(TextKeys.SystemSetting.MessageSettingsSavedDeviceSyncBackground);
                StartDeviceSyncAfterSave(previousSettings, savedSettings);
            }
            else
            {
                ShowInfoMessage(_localizer.GetString(TextKeys.Common.SaveSuccess));
            }
        }
        catch (Exception ex)
        {
            ShowErrorMessage(_localizer.GetString(TextKeys.Common.SaveFailed, ex.Message));
        }
        finally
        {
            btnSaveAll.Enabled = true;
        }
    }

    /// <summary>
    /// 手动把当前设备信息同步到 MES。
    /// </summary>
    private async void SyncDevice_ClickAsync(object? sender, EventArgs e)
    {
        if (!TryBuildSettings(out var settings))
        {
            return;
        }

        btnSyncDevice.Enabled = false;
        try
        {
            var previousSettings = _currentSettings;
            if (!CanSaveDeviceManagementChange(previousSettings, settings))
            {
                BindSettings(previousSettings);
                RefreshDeviceManagementEnabled(force: true);
                return;
            }

            if (!CanSaveRuntimeModeChange(previousSettings, settings)
                || !CanSaveInspectionResultSourceChange(previousSettings, settings)
                || !CanSavePairedAggregationModeChange(previousSettings, settings)
                || !CanSaveRealtimePointNumberSourceChange(previousSettings, settings))
            {
                BindSettings(previousSettings);
                return;
            }

            var syncVersion = Interlocked.Increment(ref _deviceSyncVersion);
            var request = await Task.Run(() => BuildDeviceRequest(previousSettings, settings));

            _currentSettings = _settingsService.Save(settings);
            BindSettings(_currentSettings);
            ApplyStartupIntegrationWithWarning(_currentSettings);

            var outcome = await SyncDeviceToMesAsync(request);
            if (outcome != DeviceSyncOutcome.Failed
                && IsCurrentDeviceSync(syncVersion, request.DeviceId)
                && TryMarkDeviceSynced(request.DeviceId))
            {
                if (outcome == DeviceSyncOutcome.Registered)
                {
                    ShowInfo(TextKeys.SystemSetting.MessageDeviceRegisterSuccess, request.DeviceId);
                }
                else
                {
                    ShowInfo(TextKeys.SystemSetting.MessageDeviceSyncSuccess);
                }
            }
        }
        catch (Exception ex)
        {
            ShowError(TextKeys.SystemSetting.MessageDeviceSyncFailed, ex.Message);
        }
        finally
        {
            btnSyncDevice.Enabled = true;
        }
    }

    private async void TestConnection_ClickAsync(object? sender, EventArgs e)
    {
        var mesFieldName = BuildFieldName(grpDeviceConfig.Text, lblMesUrl.Text);
        var baseUrl = input_BaseUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            ShowWarning(TextKeys.SystemSetting.MessageValueRequired, mesFieldName);
            return;
        }

        if (!TryValidateBaseUrl(baseUrl))
        {
            ShowWarning(TextKeys.SystemSetting.MessageInvalidUrl, mesFieldName);
            return;
        }

        btnTestConnection.Enabled = false;
        try
        {
            var timeoutSeconds = Math.Max(3, _currentSettings.MesTimeoutSeconds);
            var response = await _mesProvider.TestConnectionAsync(baseUrl, timeoutSeconds, true);
            if (response.IsSuccess)
            {
                ShowInfo(TextKeys.SystemSetting.MessageConnectionSuccess, mesFieldName);
                return;
            }

            ShowError(TextKeys.SystemSetting.MessageConnectionFailed, mesFieldName, response.Msg);
        }
        catch (Exception ex)
        {
            ShowError(TextKeys.SystemSetting.MessageConnectionFailed, mesFieldName, ex.Message);
        }
        finally
        {
            btnTestConnection.Enabled = true;
        }
    }

    private async void ConnectPlc_ClickAsync(object? sender, EventArgs e)
    {
        await TestTcpEndpointAsync(input_PlcIp.Text, input_PlcPort.Text, grpPlcConfig.Text, btnConnectPlc);
    }

    private void Select_PlcType_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingPlcTypeSelection)
        {
            return;
        }

        if (e.Value < 0 || e.Value >= PlcTypeOptions.Length)
        {
            return;
        }

        _selectedPlcType = PlcTypeOptions[e.Value].Value;
    }

    private void ChkEnablePlcStringNumericFormatting_CheckedChanged(object sender, AntdUI.BoolEventArgs e)
    {
        UpdatePlcStringNumericFormatModeEnabled();
    }

    private void ChkEnablePlcAlarmReading_CheckedChanged(object sender, AntdUI.BoolEventArgs e)
    {
        UpdatePlcAlarmTriggerModeEnabled();
    }

    private void SelectPlcAlarmTriggerMode_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingPlcAlarmTriggerModeSelection
            || e.Value < 0
            || e.Value >= PlcAlarmTriggerModeOptions.Length)
        {
            return;
        }

        _selectedPlcAlarmTriggerMode = PlcAlarmTriggerModeOptions[e.Value].Value;
    }

    private void SelectPlcStringNumericFormatMode_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingPlcStringNumericFormatModeSelection)
        {
            return;
        }

        if (e.Value < 0 || e.Value >= PlcStringNumericFormatModeOptions.Length)
        {
            return;
        }

        _selectedPlcStringNumericFormatMode = PlcStringNumericFormatModeOptions[e.Value].Value;
    }

    private void SelectUploadMode_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingUploadModeSelection)
        {
            return;
        }

        if (e.Value < 0 || e.Value >= UploadModeOptions.Length)
        {
            return;
        }

        _selectedUploadMode = UploadModeOptions[e.Value].Value;
        UpdateUploadBatchSizeEnabled();
    }

    private void SelectProcessParameterDeviceType_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingProcessParameterDeviceTypeSelection)
        {
            return;
        }

        if (e.Value < 0 || e.Value >= ProcessParameterDeviceTypeOptions.Length)
        {
            return;
        }

        var option = ProcessParameterDeviceTypeOptions[e.Value];
        _selectedProcessParameterDeviceType = option.Value;
        UpdateInspectionResultSourceEnabled();
    }

    private void SelectInspectionResultSource_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingInspectionResultSourceSelection)
        {
            return;
        }

        if (e.Value < 0 || e.Value >= InspectionResultSourceOptions.Length)
        {
            return;
        }

        _selectedInspectionResultSource = InspectionResultSourceOptions[e.Value].Value;
    }

    private void SelectPairedAggregationMode_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingPairedAggregationModeSelection)
        {
            return;
        }

        if (e.Value < 0 || e.Value >= PairedAggregationModeOptions.Length)
        {
            return;
        }

        _selectedPairedAggregationMode = PairedAggregationModeOptions[e.Value].Value;
    }

    private void SelectRealtimePointNumberSource_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingRealtimePointNumberSourceSelection)
        {
            return;
        }

        if (e.Value < 0 || e.Value >= RealtimePointNumberSourceOptions.Length)
        {
            return;
        }

        _selectedRealtimePointNumberSource = RealtimePointNumberSourceOptions[e.Value].Value;
    }

    private void SelectCenterServerSystemType_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingCenterServerSystemTypeSelection)
        {
            return;
        }

        if (e.Value < 0 || e.Value >= CenterServerSystemTypeOptions.Length)
        {
            return;
        }

        _selectedCenterServerSystemType = CenterServerSystemTypeOptions[e.Value].Value;
    }

    private void ChkEnableAutoStart_CheckedChanged(object sender, AntdUI.BoolEventArgs e)
    {
        UpdateElevatedAutoStartEnabled();
    }

    private void ChkEnablePostDataCustomHeader_CheckedChanged(object sender, AntdUI.BoolEventArgs e)
    {
        UpdatePostDataHeaderInputsEnabled();
    }

    private void SelectFolder(AntdUI.Input targetInput, string fieldName)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = _localizer.GetString(TextKeys.SystemSetting.MessageSelectFolder, fieldName),
            ShowNewFolderButton = true
        };

        if (!string.IsNullOrWhiteSpace(targetInput.Text) && Directory.Exists(targetInput.Text))
        {
            dialog.SelectedPath = targetInput.Text;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            targetInput.Text = dialog.SelectedPath;
        }
    }

    private string BuildFieldName(string? groupName, string? fieldName)
    {
        var normalizedGroup = NormalizeCaption(groupName);
        var normalizedField = NormalizeCaption(fieldName);
        return string.IsNullOrWhiteSpace(normalizedGroup) ? normalizedField : $"{normalizedGroup} - {normalizedField}";
    }

    #endregion

    private void LoadSettings()
    {
        BindSettings(CurrentSettings);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        Interlocked.Increment(ref _deviceSyncVersion);
        _settingsService.SettingsChanged -= SettingsService_SettingsChanged;
        _weldTaskService.StateChanged -= WeldTaskService_StateChanged;
        base.OnHandleDestroyed(e);
    }

    private void WeldTaskService_StateChanged(object? sender, EventArgs e)
    {
        Volatile.Write(ref _deviceManagementStateKnown, false);
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        RunOnUiThread(
            () =>
            {
                if (!Visible)
                {
                    return;
                }

                RefreshDeviceManagementEnabled(force: true);
            },
            "SystemSettingView.WeldTaskStateChanged");
    }

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        var settings = e.CurrentSettings;
        Interlocked.Exchange(ref _currentSettings, settings);
        if (Volatile.Read(ref _suppressSettingsChangedBinding) != 0)
        {
            return;
        }

        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        RunOnUiThread(() => BindSettings(settings), "SystemSettingView.SettingsChanged");
    }

    /// <summary>
    /// 数据库配置加载后，统一回填到界面控件里。
    /// </summary>
    private void BindSettings(AppSettings settings)
    {
        input_DeviceID.Text = settings.DeviceId;
        input_DeviceName.Text = settings.DeviceName;
        input_DeviceUrl.Text = settings.DeviceBaseUrl;
        input_PlcIp.Text = settings.PlcIp;
        input_PlcPort.Text = settings.PlcPort.ToString().Trim();
        chkEnablePlcStringNumericFormatting.Checked = settings.EnablePlcStringNumericFormatting ?? true;
        chkEnablePlcAlarmReading.Checked = settings.EnablePlcAlarmReading ?? true;
        input_MesTimeout.Text = settings.MesTimeoutSeconds.ToString();
        inputMesHeartbeatInterval.Text = MesConnectionRules.NormalizeHeartbeatIntervalSeconds(
            settings.MesHeartbeatIntervalSeconds).ToString(CultureInfo.InvariantCulture);
        input_LogsPath.Text = settings.LogDirectory;
        input_DataPath.Text = settings.DataDirectory;
        chkEnableAutoStart.Checked = settings.EnableAutoStart ?? true;
        chkEnableElevatedAutoStart.Checked = settings.EnableElevatedAutoStart ?? true;
        chkEnableCenterServerSync.Checked = settings.EnableCenterServerSync;
        inputCenterServerBaseUrl.Text = CenterTelemetryRules.NormalizeBaseUrl(settings.CenterServerBaseUrl);
        inputCenterServerHeartbeatInterval.Text = CenterTelemetryRules.NormalizeHeartbeatIntervalSeconds(
            settings.CenterServerHeartbeatIntervalSeconds).ToString(CultureInfo.InvariantCulture);
        input_BaseUrl.Text = settings.MesBaseUrl;
        BindMesEndpointSettings(settings);
        chkUseOperatorInputDialog.Checked = settings.UseOperatorInputDialog != false;
        chkShowTestFlagInHistory.Checked = settings.ShowTestFlagInHistory != false;
        chkEnableDeviceStatusReport.Checked = settings.EnableDeviceStatusReport != false;
        chkEnableWorkOrderStatusReport.Checked = settings.EnableWorkOrderStatusReport != false;
        chkEnableDualStation.Checked = settings.EnableDualStation || settings.EnableDualWorkOrder;
        inputStation1DisplayName.Text = settings.Station1DisplayName;
        inputStation2DisplayName.Text = settings.Station2DisplayName;
        chkValidateRecipeBeforeStart.Checked = settings.ValidateRecipeAfterStart;
        chkEnableFinishExpQtyPrompt.Checked = settings.EnableFinishExpQtyPrompt;
        inputPlcHeartbeatInterval.Text = PlcHeartbeatSettingsRules.NormalizeReadIntervalMilliseconds(
            settings.PlcHeartbeatReadIntervalMilliseconds).ToString(CultureInfo.InvariantCulture);
        inputPlcHeartbeatTimeout.Text = PlcHeartbeatSettingsRules.NormalizeTimeoutSeconds(
            settings.PlcHeartbeatTimeoutSeconds).ToString(CultureInfo.InvariantCulture);
        inputPlcCommunicationTimeout.Text = PlcHeartbeatSettingsRules.NormalizeCommunicationTimeoutMilliseconds(
            settings.PlcCommunicationTimeoutMilliseconds).ToString(CultureInfo.InvariantCulture);

        _selectedPlcType = NormalizePlcType(settings.PlcType);
        _selectedPlcStringNumericFormatMode = NormalizePlcStringNumericFormatMode(settings.PlcStringNumericFormatMode);
        _selectedPlcAlarmTriggerMode = AppConstants.PlcAlarmTriggerModes.Normalize(settings.PlcAlarmTriggerMode);
        _selectedUploadMode = NormalizeUploadMode(settings.UploadMode);
        _selectedProcessParameterDeviceType = NormalizeProcessParameterDeviceType(settings.ProcessParameterDeviceType);
        _selectedInspectionResultSource = ProductionConstants.InspectionResultSources.Normalize(settings.InspectionResultSource);
        _selectedRealtimePointNumberSource = ProductionConstants.RealtimePointNumberSources.Normalize(settings.RealtimePointNumberSource);
        _selectedPairedAggregationMode = ProductionConstants.PairedAggregationModes.Normalize(settings.PairedAggregationMode);
        chkEnableWholePieceMergedDisplay.Checked = settings.EnableWholePieceMergedDisplay == true;
        chkEnableWholePieceFaceResultDisplay.Checked = settings.EnableWholePieceFaceResultDisplay != false;
        _selectedCenterServerSystemType = NormalizeCenterServerSystemType(settings.CenterServerSystemType);
        inputUploadBatchSize.Text = Math.Max(1, settings.UploadBatchSize).ToString(CultureInfo.InvariantCulture);
        BindPlcTypeOptions();
        BindPlcStringNumericFormatModeOptions();
        BindPlcAlarmTriggerModeOptions();
        BindUploadModeOptions();
        BindProcessParameterDeviceTypeOptions();
        BindInspectionResultSourceOptions();
        BindRealtimePointNumberSourceOptions();
        BindPairedAggregationModeOptions();
        BindCenterServerSystemTypeOptions();
        UpdateInspectionResultSourceEnabled();
        UpdateRealtimePointNumberSourceEnabled();
        UpdatePlcStringNumericFormatModeEnabled();
        UpdatePlcAlarmTriggerModeEnabled();
        UpdateUploadBatchSizeEnabled();
        UpdateElevatedAutoStartEnabled();
        UpdatePostDataHeaderInputsEnabled();
        UpdateStationDisplayNameVisibility();
    }

    private void BindMesEndpointSettings(AppSettings settings)
    {
        foreach (var definition in MesRouteInputDefinitions)
        {
            var input = GetMesRouteInput(definition.Key);
            if (input is not null)
            {
                input.Text = MesEndpointRouteRules.NormalizeRoute(definition.GetRoute(settings), definition.DefaultRoute);
            }
        }

        chkEnablePostDataCustomHeader.Checked = settings.EnablePostDataCustomHeader == true;
        inputPostDataHeaderKey.Text = MesEndpointRouteRules.NormalizeHeaderKey(settings.PostDataHeaderKey);
        inputPostDataHeaderValue.Text = MesEndpointRouteRules.NormalizeHeaderValue(settings.PostDataHeaderValue);
    }

    private AntdUI.Input? GetMesRouteInput(string key)
    {
        return key switch
        {
            "User" => inputMesUserRoute,
            "WorkOrder" => inputMesWorkOrderRoute,
            "ServerTime" => inputMesServerTimeRoute,
            "ProgramManage" => inputMesProgramManageRoute,
            "StartWork" => inputMesStartWorkRoute,
            "WorkStatus" => inputMesWorkStatusRoute,
            "EndWork" => inputMesEndWorkRoute,
            "ReportFile" => inputMesReportFileRoute,
            "PostData" => inputMesPostDataRoute,
            "Device" => inputMesDeviceRoute,
            "DeviceStatus" => inputMesDeviceStatusRoute,
            "DeviceStatusQuery" => inputMesDeviceStatusQueryRoute,
            "DeviceIdSet" => inputMesDeviceIdSetRoute,
            "Sys" => inputMesSysRoute,
            _ => null
        };
    }

    private AntdUI.Label? GetMesRouteLabel(string key)
    {
        return key switch
        {
            "User" => lblMesUserRoute,
            "WorkOrder" => lblMesWorkOrderRoute,
            "ServerTime" => lblMesServerTimeRoute,
            "ProgramManage" => lblMesProgramManageRoute,
            "StartWork" => lblMesStartWorkRoute,
            "WorkStatus" => lblMesWorkStatusRoute,
            "EndWork" => lblMesEndWorkRoute,
            "ReportFile" => lblMesReportFileRoute,
            "PostData" => lblMesPostDataRoute,
            "Device" => lblMesDeviceRoute,
            "DeviceStatus" => lblMesDeviceStatusRoute,
            "DeviceStatusQuery" => lblMesDeviceStatusQueryRoute,
            "DeviceIdSet" => lblMesDeviceIdSetRoute,
            "Sys" => lblMesSysRoute,
            _ => null
        };
    }

    /// <summary>
    /// 根据当前语言回填页面静态文本。
    /// </summary>
    private void ApplyLocalizedTexts()
    {
        tabBasicSettings.Text = _localizer.GetString(TextKeys.SystemSetting.TabBasic);

        grpPlcConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupPlc);
        grpAppConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupApplication);
        grpDeviceConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupDevice);
        grpProductionConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupProduction);
        grpMesConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupMes);
        grpCenterServerConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupCenterServer);

        foreach (var definition in MesRouteInputDefinitions)
        {
            var label = GetMesRouteLabel(definition.Key);
            if (label is not null)
            {
                label.Text = _localizer.GetString(definition.TextKey);
            }
        }

        lblTitle.Text = _localizer.GetString(TextKeys.SystemSetting.Title);
        lblDescription.Text = _localizer.GetString(TextKeys.SystemSetting.Description);

        lblPlcIp.Text = _localizer.GetString(TextKeys.SystemSetting.LabelIp);
        lblPlcPort.Text = _localizer.GetString(TextKeys.SystemSetting.LabelPort);
        lblPlcType.Text = _localizer.GetString(TextKeys.SystemSetting.LabelType);
        chkEnablePlcStringNumericFormatting.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnablePlcStringFormatting);
        chkEnablePlcAlarmReading.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnablePlcAlarmReading);
        lblPlcAlarmTriggerMode.Text = _localizer.GetString(TextKeys.SystemSetting.LabelPlcAlarmTriggerMode);
        lblPlcStringNumericFormatMode.Text = _localizer.GetString(TextKeys.SystemSetting.LabelPlcFormatMode);

        lblDeviceId.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDeviceId);
        lblDeviceName.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDeviceName);
        lblDeviceUrl.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDeviceStatusUrl);
        lblMesUrl.Text = _localizer.GetString(TextKeys.SystemSetting.LabelMesUrl);
        lblMesTimeout.Text = _localizer.GetString(TextKeys.SystemSetting.LabelMesTimeout);
        lblMesHeartbeatInterval.Text = _localizer.GetString(TextKeys.SystemSetting.LabelMesHeartbeatInterval);

        lblLogPath.Text = _localizer.GetString(TextKeys.SystemSetting.LabelLogPath);
        lblDataPath.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDataPath);
        chkEnableAutoStart.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableAutoStart);
        chkEnableElevatedAutoStart.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableElevatedAutoStart);
        lblUploadMode.Text = _localizer.GetString(TextKeys.SystemSetting.UploadMode);
        lblUploadBatchSize.Text = _localizer.GetString(TextKeys.SystemSetting.UploadBatchSize);
        lblPlcHeartbeatInterval.Text = _localizer.GetString(TextKeys.SystemSetting.PlcHeartbeatRate);
        lblPlcHeartbeatTimeout.Text = _localizer.GetString(TextKeys.SystemSetting.PlcHeartbeatTimeout);
        lblPlcCommunicationTimeout.Text = _localizer.GetString(TextKeys.SystemSetting.PlcCommunicationTimeout);
        lblStation1DisplayName.Text = _localizer.GetString(TextKeys.SystemSetting.LabelStation1DisplayName);
        lblStation2DisplayName.Text = _localizer.GetString(TextKeys.SystemSetting.LabelStation2DisplayName);
        inputStation1DisplayName.PlaceholderText = _localizer.GetString(TextKeys.SystemSetting.PlaceholderStationDisplayName);
        inputStation2DisplayName.PlaceholderText = _localizer.GetString(TextKeys.SystemSetting.PlaceholderStationDisplayName);
        chkEnableCenterServerSync.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableCenterServerSync);
        lblCenterServerBaseUrl.Text = _localizer.GetString(TextKeys.SystemSetting.LabelCenterServerUrl);
        lblCenterServerSystemType.Text = _localizer.GetString(TextKeys.SystemSetting.LabelCenterServerSystemType);
        lblCenterServerHeartbeatInterval.Text = _localizer.GetString(TextKeys.SystemSetting.LabelCenterServerHeartbeat);
        lblProcessParameterDeviceType.Text = _localizer.GetString(TextKeys.SystemSetting.LabelProcessParameterDeviceType);
        lblInspectionResultSource.Text = _localizer.GetString(TextKeys.SystemSetting.LabelInspectionResultSource);
        lblRealtimePointNumberSource.Text = _localizer.GetString(TextKeys.SystemSetting.LabelRealtimePointNumberSource);
        lblPairedAggregationMode.Text = _localizer.GetString(TextKeys.SystemSetting.LabelPairedAggregationMode);
        chkEnableWholePieceMergedDisplay.Text = _localizer.GetString(TextKeys.SystemSetting.LabelWholePieceMergedDisplay);
        chkEnableWholePieceFaceResultDisplay.Text = _localizer.GetString(TextKeys.SystemSetting.LabelWholePieceFaceResultDisplay);
        chkEnablePostDataCustomHeader.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnablePostDataHeader);
        lblPostDataHeaderKey.Text = _localizer.GetString(TextKeys.SystemSetting.LabelPostDataHeaderKey);
        lblPostDataHeaderValue.Text = _localizer.GetString(TextKeys.SystemSetting.LabelPostDataHeaderValue);
        chkShowTestFlagInHistory.Text = _localizer.GetString(TextKeys.SystemSetting.ChkShowTestFlagInHistory);

        chkUseOperatorInputDialog.Text = _localizer.GetString(TextKeys.SystemSetting.ChkUseOperatorInputDialog);
        chkEnableDeviceStatusReport.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableDeviceStatusReport);
        chkEnableWorkOrderStatusReport.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableWorkOrderStatusReport);
        chkValidateRecipeBeforeStart.Text = _localizer.GetString(TextKeys.SystemSetting.ChkValidateRecipeAfterStart);
        chkEnableFinishExpQtyPrompt.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableFinishExpQtyPrompt);
        chkEnableDualStation.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableDualStation);

        btnConnectPlc.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonConnect);
        btnSyncDevice.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonSyncDevice);
        btnTestConnection.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonTestConnection);
        btnChangeLogPath.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonChangePath);
        btnChangeDataPath.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonChangePath);
        btnOpenLogPath.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonOpenFolder);
        btnOpenDataPath.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonOpenFolder);
        btnSaveAll.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonApplyAll);
    }

    /// <summary>
    /// 下拉选项显示的是本地化文本，真正入库的是稳定字符串。
    /// </summary>
    private void BindPlcTypeOptions()
    {
        _syncingPlcTypeSelection = true;
        select_PlcType.Items.Clear();
        select_PlcType.Items.AddRange(PlcTypeOptions
            .Select(option => (object)_localizer.GetString(option.TextKey))
            .ToArray());

        var selectedIndex = Array.FindIndex(PlcTypeOptions, option =>
            string.Equals(option.Value, _selectedPlcType, StringComparison.OrdinalIgnoreCase));
        select_PlcType.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        _syncingPlcTypeSelection = false;
    }

    private void BindPlcStringNumericFormatModeOptions()
    {
        _syncingPlcStringNumericFormatModeSelection = true;
        try
        {
            selectPlcStringNumericFormatMode.Items.Clear();
            selectPlcStringNumericFormatMode.Items.AddRange(PlcStringNumericFormatModeOptions
                .Select(option => (object)_localizer.GetString(option.TextKey))
                .ToArray());

            var selectedIndex = Array.FindIndex(PlcStringNumericFormatModeOptions, option =>
                string.Equals(option.Value, _selectedPlcStringNumericFormatMode, StringComparison.OrdinalIgnoreCase));
            selectPlcStringNumericFormatMode.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }
        finally
        {
            _syncingPlcStringNumericFormatModeSelection = false;
        }
    }

    private void BindPlcAlarmTriggerModeOptions()
    {
        _syncingPlcAlarmTriggerModeSelection = true;
        try
        {
            selectPlcAlarmTriggerMode.Items.Clear();
            selectPlcAlarmTriggerMode.Items.AddRange(PlcAlarmTriggerModeOptions
                .Select(option => (object)_localizer.GetString(option.TextKey))
                .ToArray());
            var selectedIndex = Array.FindIndex(PlcAlarmTriggerModeOptions, option =>
                string.Equals(option.Value, _selectedPlcAlarmTriggerMode, StringComparison.OrdinalIgnoreCase));
            selectPlcAlarmTriggerMode.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 1;
        }
        finally
        {
            _syncingPlcAlarmTriggerModeSelection = false;
        }
    }

    private void BindUploadModeOptions()
    {
        _syncingUploadModeSelection = true;
        try
        {
            selectUploadMode.Items.Clear();
            selectUploadMode.Items.AddRange(UploadModeOptions
                .Select(option => (object)_localizer.GetString(option.TextKey))
                .ToArray());

            var selectedIndex = Array.FindIndex(UploadModeOptions, option => option.Value == _selectedUploadMode);
            selectUploadMode.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 1;
        }
        finally
        {
            _syncingUploadModeSelection = false;
        }
    }

    private void BindProcessParameterDeviceTypeOptions()
    {
        _syncingProcessParameterDeviceTypeSelection = true;
        try
        {
            selectProcessParameterDeviceType.Items.Clear();
            selectProcessParameterDeviceType.Items.AddRange(ProcessParameterDeviceTypeOptions
                .Select(option => (object)_localizer.GetString(option.TextKey))
                .ToArray());

            var selectedIndex = Array.FindIndex(ProcessParameterDeviceTypeOptions, option =>
                string.Equals(option.Value, _selectedProcessParameterDeviceType, StringComparison.OrdinalIgnoreCase));
            selectProcessParameterDeviceType.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }
        finally
        {
            _syncingProcessParameterDeviceTypeSelection = false;
        }
    }

    private void BindInspectionResultSourceOptions()
    {
        _syncingInspectionResultSourceSelection = true;
        try
        {
            selectInspectionResultSource.Items.Clear();
            selectInspectionResultSource.Items.AddRange(InspectionResultSourceOptions
                .Select(option => (object)_localizer.GetString(option.TextKey))
                .ToArray());

            var selectedIndex = Array.FindIndex(InspectionResultSourceOptions, option =>
                string.Equals(option.Value, _selectedInspectionResultSource, StringComparison.OrdinalIgnoreCase));
            selectInspectionResultSource.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }
        finally
        {
            _syncingInspectionResultSourceSelection = false;
        }
    }

    private void UpdateInspectionResultSourceEnabled()
    {
        var wholePieceInspection = string.Equals(
            _selectedProcessParameterDeviceType,
            ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck,
            StringComparison.OrdinalIgnoreCase);
        tlpInspectionResultSource.Visible = wholePieceInspection;
        selectInspectionResultSource.Enabled = wholePieceInspection && !HasAnyUnfinishedTask();

        // 合并显示与 A/B 配对聚合只对整件检测有意义；聚合方式影响上传和报表数据，未完工时禁止切换。
        tlpPairedAggregationMode.Visible = wholePieceInspection;
        selectPairedAggregationMode.Enabled = wholePieceInspection && !HasAnyUnfinishedTask();
        chkEnableWholePieceMergedDisplay.Visible = wholePieceInspection;
        chkEnableWholePieceFaceResultDisplay.Visible = wholePieceInspection;
    }

    private void BindPairedAggregationModeOptions()
    {
        _syncingPairedAggregationModeSelection = true;
        try
        {
            selectPairedAggregationMode.Items.Clear();
            selectPairedAggregationMode.Items.AddRange(PairedAggregationModeOptions
                .Select(option => (object)_localizer.GetString(option.TextKey))
                .ToArray());

            var selectedIndex = Array.FindIndex(PairedAggregationModeOptions, option =>
                string.Equals(option.Value, _selectedPairedAggregationMode, StringComparison.OrdinalIgnoreCase));
            selectPairedAggregationMode.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }
        finally
        {
            _syncingPairedAggregationModeSelection = false;
        }
    }

    private void BindRealtimePointNumberSourceOptions()
    {
        _syncingRealtimePointNumberSourceSelection = true;
        try
        {
            selectRealtimePointNumberSource.Items.Clear();
            selectRealtimePointNumberSource.Items.AddRange(RealtimePointNumberSourceOptions
                .Select(option => (object)_localizer.GetString(option.TextKey))
                .ToArray());

            var selectedIndex = Array.FindIndex(RealtimePointNumberSourceOptions, option =>
                string.Equals(option.Value, _selectedRealtimePointNumberSource, StringComparison.OrdinalIgnoreCase));
            selectRealtimePointNumberSource.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }
        finally
        {
            _syncingRealtimePointNumberSourceSelection = false;
        }
    }

    private void UpdateRealtimePointNumberSourceEnabled()
    {
        selectRealtimePointNumberSource.Enabled = !HasAnyUnfinishedTask();
    }

    private void BindCenterServerSystemTypeOptions()
    {
        _syncingCenterServerSystemTypeSelection = true;
        try
        {
            selectCenterServerSystemType.Items.Clear();
            selectCenterServerSystemType.Items.AddRange(CenterServerSystemTypeOptions
                .Select(option => (object)_localizer.GetString(option.TextKey))
                .ToArray());

            var selectedIndex = Array.FindIndex(CenterServerSystemTypeOptions, option =>
                string.Equals(option.Value, _selectedCenterServerSystemType, StringComparison.OrdinalIgnoreCase));
            selectCenterServerSystemType.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 2;
        }
        finally
        {
            _syncingCenterServerSystemTypeSelection = false;
        }
    }

    private void UpdateUploadBatchSizeEnabled()
    {
        inputUploadBatchSize.Enabled = _selectedUploadMode == UploadMode.Quantity;
        if (string.IsNullOrWhiteSpace(inputUploadBatchSize.Text))
        {
            inputUploadBatchSize.Text = "1";
        }
    }

    private void UpdatePlcStringNumericFormatModeEnabled()
    {
        selectPlcStringNumericFormatMode.Enabled = chkEnablePlcStringNumericFormatting.Checked;
    }

    private void UpdatePlcAlarmTriggerModeEnabled()
    {
        selectPlcAlarmTriggerMode.Enabled = chkEnablePlcAlarmReading.Checked;
    }

    private void UpdateElevatedAutoStartEnabled()
    {
        chkEnableElevatedAutoStart.Enabled = chkEnableAutoStart.Checked;
    }

    private void UpdatePostDataHeaderInputsEnabled()
    {
        var enabled = chkEnablePostDataCustomHeader.Checked;
        inputPostDataHeaderKey.Enabled = enabled;
        inputPostDataHeaderValue.Enabled = enabled;
    }

    /// <summary>
    /// 单工位模式不展示与当前生产无关的名称输入区。
    /// </summary>
    private void UpdateStationDisplayNameVisibility()
    {
        stationDisplayNameLayout.Visible = chkEnableDualStation.Checked;
    }

    private void StartDeviceSyncAfterSave(AppSettings previousSettings, AppSettings savedSettings)
    {
        var syncVersion = Interlocked.Increment(ref _deviceSyncVersion);
        _ = SyncDeviceAfterSaveAsync(previousSettings.Clone(), savedSettings.Clone(), syncVersion);
    }

    private async Task SyncDeviceAfterSaveAsync(
        AppSettings previousSettings,
        AppSettings savedSettings,
        int syncVersion)
    {
        AddDeviceReq? request = null;
        try
        {
            request = await Task.Run(() => BuildDeviceRequest(previousSettings, savedSettings));
            var response = await _mesProvider.SetDeviceIdAsync(request);
            if (!IsCurrentDeviceSync(syncVersion, request.DeviceId))
            {
                return;
            }

            if (response.IsSuccess)
            {
                if (TryMarkDeviceSynced(request.DeviceId))
                {
                    ShowInfo(TextKeys.SystemSetting.MessageDeviceSyncSuccess);
                }

                return;
            }

            if (DeviceIdSyncRules.ShouldOfferRegisterAsNew(request.OldDeviceId, response.Msg))
            {
                ShowWarning(TextKeys.SystemSetting.MessageDeviceSyncManualConfirmationRequired);
                return;
            }

            ShowError(TextKeys.SystemSetting.MessageDeviceSyncFailed, response.Msg);
        }
        catch (Exception ex)
        {
            var expectedDeviceId = request?.DeviceId ?? savedSettings.DeviceId;
            if (IsCurrentDeviceSync(syncVersion, expectedDeviceId))
            {
                ShowError(TextKeys.SystemSetting.MessageDeviceSyncFailed, ex.Message);
            }
        }
    }

    private bool IsCurrentDeviceSync(int syncVersion, string? deviceId)
    {
        return syncVersion == Volatile.Read(ref _deviceSyncVersion)
            && SameText(CurrentSettings.DeviceId, deviceId);
    }

    private async Task<DeviceSyncOutcome> SyncDeviceToMesAsync(AddDeviceReq request)
    {
        var response = await _mesProvider.SetDeviceIdAsync(request);
        if (response.IsSuccess)
        {
            return DeviceSyncOutcome.Synced;
        }

        if (!DeviceIdSyncRules.ShouldOfferRegisterAsNew(request.OldDeviceId, response.Msg))
        {
            ShowError(TextKeys.SystemSetting.MessageDeviceSyncFailed, response.Msg);
            return DeviceSyncOutcome.Failed;
        }

        if (!ConfirmRegisterNewDevice(request))
        {
            return DeviceSyncOutcome.Failed;
        }

        var registerRequest = BuildNewDeviceRegistrationRequest(request);
        var registerResponse = await _mesProvider.SetDeviceIdAsync(registerRequest);
        if (registerResponse.IsSuccess)
        {
            return DeviceSyncOutcome.Registered;
        }

        ShowError(TextKeys.SystemSetting.MessageDeviceRegisterFailed, registerResponse.Msg);
        return DeviceSyncOutcome.Failed;
    }

    private bool ConfirmRegisterNewDevice(AddDeviceReq request)
    {
        var message = _localizer.GetString(
            TextKeys.SystemSetting.MessageDeviceRegisterConfirm,
            request.OldDeviceId?.Trim() ?? string.Empty,
            request.DeviceId.Trim());
        return MessageBox.Show(
                this,
                message,
                _localizer.GetString(TextKeys.Common.TitleWarning),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning)
            == DialogResult.Yes;
    }

    private static AddDeviceReq BuildNewDeviceRegistrationRequest(AddDeviceReq request)
    {
        return new AddDeviceReq
        {
            OldDeviceId = string.Empty,
            DeviceId = request.DeviceId,
            DeviceName = request.DeviceName,
            IP = request.IP,
            DevStatusUrl = request.DevStatusUrl,
            PostDataDomain = request.PostDataDomain
        };
    }

    /// <summary>
    /// MES 确认成功后再更新“已同步编号”，保证失败重试时 OldDeviceId 仍然正确。
    /// 后台同步完成时不重新绑定整页，避免覆盖用户尚未应用的新输入。
    /// </summary>
    private bool TryMarkDeviceSynced(string deviceId)
    {
        var settings = CurrentSettings;
        if (!SameText(settings.DeviceId, deviceId))
        {
            return false;
        }

        var updatedSettings = settings.Clone();
        updatedSettings.MesSyncedDeviceId = deviceId.Trim();
        Interlocked.Increment(ref _suppressSettingsChangedBinding);
        try
        {
            _currentSettings = _settingsService.Save(updatedSettings);
        }
        finally
        {
            Interlocked.Decrement(ref _suppressSettingsChangedBinding);
        }

        return true;
    }

    /// <summary>
    /// PLC/总控连通测试都走同一套 TCP 检测逻辑。
    /// </summary>
    private async Task TestTcpEndpointAsync(string hostText, string portText, string endpointName, Control triggerButton)
    {
        var endpointCaption = NormalizeCaption(endpointName);
        if (!TryValidateIp(hostText, BuildFieldName(endpointCaption, lblPlcIp.Text)))
        {
            return;
        }

        if (!TryParsePort(portText, BuildFieldName(endpointCaption, lblPlcPort.Text), out var port))
        {
            return;
        }

        triggerButton.Enabled = false;
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(hostText.Trim(), port).WaitAsync(TimeSpan.FromSeconds(3));
            ShowInfo(TextKeys.SystemSetting.MessageConnectionSuccess, endpointCaption);
        }
        catch (Exception ex)
        {
            ShowError(TextKeys.SystemSetting.MessageConnectionFailed, endpointCaption, ex.Message);
        }
        finally
        {
            triggerButton.Enabled = true;
        }
    }

    private void OpenFolder(string folderPath, string fieldName)
    {
        var normalizedPath = folderPath?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            ShowWarning(TextKeys.SystemSetting.MessageValueRequired, fieldName);
            return;
        }

        if (!Directory.Exists(normalizedPath))
        {
            ShowWarning(TextKeys.SystemSetting.MessageFolderMissing, normalizedPath);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = normalizedPath,
            UseShellExecute = true
        });
    }

    private void ApplyStartupIntegrationWithWarning(AppSettings settings)
    {
        var startupResult = _windowsShellIntegrationService.ApplyStartupIntegration(settings);
        if (!startupResult.Success)
        {
            ShowWarning(TextKeys.SystemSetting.MessageStartupIntegrationFailed, startupResult.Message);
        }
    }

    private bool TryBuildSettings(out AppSettings settings)
    {
        settings = CurrentSettings.Clone();

        var deviceId = input_DeviceID.Text.Trim();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            ShowWarning(TextKeys.SystemSetting.MessageValueRequired, NormalizeCaption(lblDeviceId.Text));
            return false;
        }

        var deviceName = input_DeviceName.Text.Trim();
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            ShowWarning(TextKeys.SystemSetting.MessageValueRequired, NormalizeCaption(lblDeviceName.Text));
            return false;
        }

        var plcIp = input_PlcIp.Text.Trim();
        if (!TryValidateIp(plcIp, BuildFieldName(grpPlcConfig.Text, lblPlcIp.Text)))
        {
            return false;
        }

        if (!TryParsePort(input_PlcPort.Text, BuildFieldName(grpPlcConfig.Text, lblPlcPort.Text), out var plcPort))
        {
            return false;
        }

        var logDirectory = input_LogsPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            ShowWarning(TextKeys.SystemSetting.MessageValueRequired, NormalizeCaption(lblLogPath.Text));
            return false;
        }

        var dataDirectory = input_DataPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            ShowWarning(TextKeys.SystemSetting.MessageValueRequired, NormalizeCaption(lblDataPath.Text));
            return false;
        }

        var mesBaseUrl = input_BaseUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(mesBaseUrl))
        {
            ShowWarning(TextKeys.SystemSetting.MessageValueRequired, NormalizeCaption(lblMesUrl.Text));
            return false;
        }

        if (!TryValidateBaseUrl(mesBaseUrl))
        {
            ShowWarning(TextKeys.SystemSetting.MessageInvalidUrl, NormalizeCaption(lblMesUrl.Text));
            return false;
        }

        var deviceStatusUrl = input_DeviceUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(deviceStatusUrl))
        {
            ShowWarning(TextKeys.SystemSetting.MessageValueRequired, NormalizeCaption(lblDeviceUrl.Text));
            return false;
        }

        if (!TryValidateBaseUrl(deviceStatusUrl))
        {
            ShowWarning(TextKeys.SystemSetting.MessageInvalidUrl, NormalizeCaption(lblDeviceUrl.Text));
            return false;
        }

        var centerServerBaseUrl = inputCenterServerBaseUrl.Text.Trim();
        if (chkEnableCenterServerSync.Checked && string.IsNullOrWhiteSpace(centerServerBaseUrl))
        {
            ShowWarning(TextKeys.SystemSetting.MessageValueRequired, NormalizeCaption(lblCenterServerBaseUrl.Text));
            return false;
        }

        if (!string.IsNullOrWhiteSpace(centerServerBaseUrl) && !TryValidateBaseUrl(centerServerBaseUrl))
        {
            ShowWarning(TextKeys.SystemSetting.MessageInvalidUrl, NormalizeCaption(lblCenterServerBaseUrl.Text));
            return false;
        }

        if (!TryParsePositiveInt(inputUploadBatchSize.Text, NormalizeCaption(lblUploadBatchSize.Text), out var uploadBatchSize))
        {
            return false;
        }

        if (!TryParsePositiveInt(inputPlcHeartbeatInterval.Text, NormalizeCaption(lblPlcHeartbeatInterval.Text), out var heartbeatInterval))
        {
            return false;
        }

        if (!TryParsePositiveInt(inputPlcHeartbeatTimeout.Text, NormalizeCaption(lblPlcHeartbeatTimeout.Text), out var heartbeatTimeout))
        {
            return false;
        }

        if (!TryParsePositiveInt(inputPlcCommunicationTimeout.Text, NormalizeCaption(lblPlcCommunicationTimeout.Text), out var communicationTimeout))
        {
            return false;
        }

        if (!TryParsePositiveInt(inputCenterServerHeartbeatInterval.Text, NormalizeCaption(lblCenterServerHeartbeatInterval.Text), out var centerHeartbeatInterval))
        {
            return false;
        }

        if (!TryParsePositiveInt(inputMesHeartbeatInterval.Text, NormalizeCaption(lblMesHeartbeatInterval.Text), out var mesHeartbeatInterval))
        {
            return false;
        }

        var mesTimeout = input_MesTimeout.Text;
        var enableDualStation = chkEnableDualStation.Checked;
        StationDisplayNames stationNames;
        try
        {
            stationNames = StationDisplayNameRules.NormalizeAndValidate(
                enableDualStation,
                inputStation1DisplayName.Text,
                inputStation2DisplayName.Text);
        }
        catch (ArgumentException ex)
        {
            ShowWarning(ex.Message);
            return false;
        }

        settings.DeviceId = deviceId;
        settings.DeviceName = deviceName;
        settings.DeviceBaseUrl = DeviceApiEndpointRules.NormalizeBaseUrl(deviceStatusUrl);
        settings.PlcIp = plcIp;
        settings.PlcPort = plcPort;
        settings.PlcType = NormalizePlcType(_selectedPlcType);
        settings.EnablePlcStringNumericFormatting = chkEnablePlcStringNumericFormatting.Checked;
        settings.EnablePlcAlarmReading = chkEnablePlcAlarmReading.Checked;
        settings.PlcAlarmTriggerMode = AppConstants.PlcAlarmTriggerModes.Normalize(_selectedPlcAlarmTriggerMode);
        settings.PlcStringNumericFormatMode = NormalizePlcStringNumericFormatMode(_selectedPlcStringNumericFormatMode);
        settings.LogDirectory = logDirectory;
        settings.DataDirectory = dataDirectory;
        settings.EnableAutoStart = chkEnableAutoStart.Checked;
        settings.EnableElevatedAutoStart = chkEnableElevatedAutoStart.Checked;
        settings.EnableCenterServerSync = chkEnableCenterServerSync.Checked;
        settings.CenterServerBaseUrl = CenterTelemetryRules.NormalizeBaseUrl(centerServerBaseUrl);
        settings.CenterServerSystemType = NormalizeCenterServerSystemType(_selectedCenterServerSystemType);
        settings.CenterServerHeartbeatIntervalSeconds = CenterTelemetryRules.NormalizeHeartbeatIntervalSeconds(centerHeartbeatInterval);
        settings.MesBaseUrl = mesBaseUrl;
        settings.MesTimeoutSeconds = int.TryParse(mesTimeout, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeout) && timeout > 0 ? timeout : 10;
        settings.MesHeartbeatIntervalSeconds = MesConnectionRules.NormalizeHeartbeatIntervalSeconds(mesHeartbeatInterval);
        settings.UseOperatorInputDialog = chkUseOperatorInputDialog.Checked;
        settings.ShowTestFlagInHistory = chkShowTestFlagInHistory.Checked;
        settings.EnableDeviceStatusReport = chkEnableDeviceStatusReport.Checked;
        settings.EnableWorkOrderStatusReport = chkEnableWorkOrderStatusReport.Checked;
        settings.EnableDualStation = enableDualStation;
        settings.Station1DisplayName = stationNames.Station1;
        settings.Station2DisplayName = stationNames.Station2;
        settings.EnableDualWorkOrder = enableDualStation && CurrentSettings.EnableDualWorkOrder;
        settings.ValidateRecipeAfterStart = chkValidateRecipeBeforeStart.Checked;
        settings.EnableFinishExpQtyPrompt = chkEnableFinishExpQtyPrompt.Checked;
        settings.PlcHeartbeatReadIntervalMilliseconds = PlcHeartbeatSettingsRules.NormalizeReadIntervalMilliseconds(heartbeatInterval);
        settings.PlcHeartbeatTimeoutSeconds = PlcHeartbeatSettingsRules.NormalizeTimeoutSeconds(heartbeatTimeout);
        settings.PlcCommunicationTimeoutMilliseconds = PlcHeartbeatSettingsRules.NormalizeCommunicationTimeoutMilliseconds(communicationTimeout);
        settings.UploadMode = NormalizeUploadMode(_selectedUploadMode);
        settings.UploadBatchSize = Math.Max(1, uploadBatchSize);
        settings.ProcessParameterDeviceType = NormalizeProcessParameterDeviceType(_selectedProcessParameterDeviceType);
        settings.InspectionResultSource = ProductionConstants.InspectionResultSources.Normalize(_selectedInspectionResultSource);
        settings.RealtimePointNumberSource = ProductionConstants.RealtimePointNumberSources.Normalize(_selectedRealtimePointNumberSource);
        settings.PairedAggregationMode = ProductionConstants.PairedAggregationModes.Normalize(_selectedPairedAggregationMode);
        settings.EnableWholePieceMergedDisplay = chkEnableWholePieceMergedDisplay.Checked;
        settings.EnableWholePieceFaceResultDisplay = chkEnableWholePieceFaceResultDisplay.Checked;
        if (!TryApplyMesEndpointSettings(settings))
        {
            return false;
        }

        return true;
    }

    private bool TryApplyMesEndpointSettings(AppSettings settings)
    {
        foreach (var definition in MesRouteInputDefinitions)
        {
            var input = GetMesRouteInput(definition.Key);
            if (input is null)
            {
                continue;
            }

            if (!MesEndpointRouteRules.TryNormalizeRequiredRoute(
                    input.Text,
                    out var route,
                    out var error))
            {
                ShowWarningMessage(GetMesValidationMessage(error, _localizer.GetString(definition.TextKey)));
                return false;
            }

            definition.SetRoute(settings, route);
        }

        var headerEnabled = chkEnablePostDataCustomHeader.Checked;
        if (!MesEndpointRouteRules.TryValidatePostDataHeader(
                headerEnabled,
                inputPostDataHeaderKey.Text,
                inputPostDataHeaderValue.Text,
                out var headerKey,
                out var headerValue,
                out var headerError))
        {
            ShowWarningMessage(GetMesValidationMessage(headerError));
            return false;
        }

        settings.EnablePostDataCustomHeader = headerEnabled;
        settings.PostDataHeaderKey = headerKey;
        settings.PostDataHeaderValue = headerValue;
        return true;
    }

    private string GetMesValidationMessage(MesEndpointValidationError error, string fieldName = "")
    {
        var key = error switch
        {
            MesEndpointValidationError.Required => TextKeys.SystemSetting.MessageRouteRequired,
            MesEndpointValidationError.AbsoluteUrlNotAllowed => TextKeys.SystemSetting.MessageRelativeRouteRequired,
            MesEndpointValidationError.QueryOrFragmentNotAllowed => TextKeys.SystemSetting.MessageRouteQueryNotAllowed,
            MesEndpointValidationError.InvalidHeaderKey => TextKeys.SystemSetting.MessageHeaderKeyInvalid,
            MesEndpointValidationError.HeaderValueRequired => TextKeys.SystemSetting.MessageHeaderValueRequired,
            _ => string.Empty
        };

        return string.IsNullOrEmpty(key)
            ? string.Empty
            : _localizer.GetString(key, fieldName);
    }

    private bool CanSaveRuntimeModeChange(AppSettings previousSettings, AppSettings newSettings)
    {
        if (!HasDualModeChanged(previousSettings, newSettings))
        {
            return true;
        }

        if (!HasAnyUnfinishedTask())
        {
            return true;
        }

        ShowWarning(TextKeys.SystemSetting.MessageRuntimeModeLocked);
        return false;
    }

    private bool CanSaveInspectionResultSourceChange(AppSettings previousSettings, AppSettings newSettings)
    {
        if (string.Equals(
                ProductionConstants.InspectionResultSources.Normalize(previousSettings.InspectionResultSource),
                ProductionConstants.InspectionResultSources.Normalize(newSettings.InspectionResultSource),
                StringComparison.OrdinalIgnoreCase)
            || !HasAnyUnfinishedTask())
        {
            return true;
        }

        ShowWarning(TextKeys.SystemSetting.MessageInspectionResultSourceLocked);
        return false;
    }

    private bool CanSavePairedAggregationModeChange(AppSettings previousSettings, AppSettings newSettings)
    {
        if (string.Equals(
                ProductionConstants.PairedAggregationModes.Normalize(previousSettings.PairedAggregationMode),
                ProductionConstants.PairedAggregationModes.Normalize(newSettings.PairedAggregationMode),
                StringComparison.OrdinalIgnoreCase)
            || !HasAnyUnfinishedTask())
        {
            return true;
        }

        ShowWarning(TextKeys.SystemSetting.MessagePairedAggregationModeLocked);
        return false;
    }

    private bool CanSaveRealtimePointNumberSourceChange(AppSettings previousSettings, AppSettings newSettings)
    {
        if (string.Equals(
                ProductionConstants.RealtimePointNumberSources.Normalize(previousSettings.RealtimePointNumberSource),
                ProductionConstants.RealtimePointNumberSources.Normalize(newSettings.RealtimePointNumberSource),
                StringComparison.OrdinalIgnoreCase)
            || !HasAnyUnfinishedTask())
        {
            return true;
        }

        ShowWarning(TextKeys.SystemSetting.MessageRealtimePointNumberSourceLocked);
        return false;
    }

    /// <summary>
    /// 软件当前已开工时禁止保存设备身份和设备通信地址，避免活动任务关联到变化后的设备。
    /// </summary>
    private bool CanSaveDeviceManagementChange(AppSettings previousSettings, AppSettings newSettings)
    {
        if (!HasDeviceIdentityChanged(previousSettings, newSettings) || !HasAnyActiveRuntimeTask())
        {
            return true;
        }

        ShowWarning(TextKeys.SystemSetting.MessageDeviceManagementLocked);
        return false;
    }

    /// <summary>
    /// 任一工位在当前软件运行态中已经开工且尚未完工时，统一禁用整个设备管理模块和 MES 配置模块。
    /// </summary>
    private void RefreshDeviceManagementEnabled(bool force = false)
    {
        if (!force && Volatile.Read(ref _deviceManagementStateKnown))
        {
            return;
        }

        var enabled = !HasAnyActiveRuntimeTask();
        grpDeviceConfig.Enabled = enabled;
        grpMesConfig.Enabled = enabled;
        UpdateInspectionResultSourceEnabled();
        UpdateRealtimePointNumberSourceEnabled();
        Volatile.Write(ref _deviceManagementStateKnown, true);
    }

    private bool HasAnyActiveRuntimeTask()
    {
        var state = _weldTaskService.CurrentState;
        return IsActiveRuntimeTask(state.ActiveTask)
            || state.StationStates.Values.Any(station => IsActiveRuntimeTask(station.ActiveTask));
    }

    private static bool IsActiveRuntimeTask(BizWeldTask? task)
    {
        return task is not null
            && task.EndTime is null
            && !string.Equals(
                task.TaskStatus,
                ProductionConstants.ProductInstanceStatuses.Completed,
                StringComparison.OrdinalIgnoreCase);
    }

    private bool HasAnyUnfinishedTask()
    {
        return _weldTaskService.GetUnfinishedTask(1) is not null
            || _weldTaskService.GetUnfinishedTask(2) is not null;
    }

    private static bool HasDualModeChanged(AppSettings oldSettings, AppSettings newSettings)
    {
        return oldSettings.EnableDualStation != newSettings.EnableDualStation
            || oldSettings.EnableDualWorkOrder != newSettings.EnableDualWorkOrder;
    }

    private static bool HasPlcCommunicationChanged(AppSettings oldSettings, AppSettings newSettings)
    {
        return !SameText(oldSettings.PlcIp, newSettings.PlcIp)
            || oldSettings.PlcPort != newSettings.PlcPort
            || !SameText(oldSettings.PlcType, newSettings.PlcType)
            || oldSettings.PlcHeartbeatReadIntervalMilliseconds != newSettings.PlcHeartbeatReadIntervalMilliseconds
            || oldSettings.PlcHeartbeatTimeoutSeconds != newSettings.PlcHeartbeatTimeoutSeconds
            || oldSettings.PlcCommunicationTimeoutMilliseconds != newSettings.PlcCommunicationTimeoutMilliseconds;
    }

    private AddDeviceReq BuildDeviceRequest(AppSettings previousSettings, AppSettings newSettings)
    {
        return new AddDeviceReq
        {
            OldDeviceId = GetMesOldDeviceId(previousSettings, newSettings),
            DeviceId = newSettings.DeviceId.Trim(),
            DeviceName = newSettings.DeviceName.Trim(),
            IP = GetLocalIPv4Address(),
            DevStatusUrl = DeviceApiEndpointRules.BuildDeviceStatusUrl(newSettings.DeviceBaseUrl, newSettings.DeviceId),
            PostDataDomain = DeviceApiEndpointRules.NormalizeBaseUrl(newSettings.MesBaseUrl)
        };
    }

    private static string GetMesOldDeviceId(AppSettings oldSettings, AppSettings newSettings)
    {
        if (!string.IsNullOrWhiteSpace(oldSettings.MesSyncedDeviceId))
        {
            return oldSettings.MesSyncedDeviceId.Trim();
        }

        return SameText(oldSettings.DeviceId, newSettings.DeviceId)
            ? string.Empty
            : oldSettings.DeviceId?.Trim() ?? string.Empty;
    }

    private static bool HasDeviceIdentityChanged(AppSettings oldSettings, AppSettings newSettings)
    {
        return !SameText(oldSettings.DeviceId, newSettings.DeviceId)
            || !SameText(oldSettings.DeviceName, newSettings.DeviceName)
            || !SameText(oldSettings.DeviceBaseUrl, newSettings.DeviceBaseUrl)
            || !SameText(oldSettings.MesBaseUrl, newSettings.MesBaseUrl);
    }

    private static bool HasDeviceIdChanged(AppSettings oldSettings, AppSettings newSettings)
    {
        return !SameText(oldSettings.DeviceId, newSettings.DeviceId);
    }

    private static bool SameText(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.Ordinal);
    }

    private static string GetLocalIPv4Address()
    {
        try
        {
            return Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
                .Select(address => address.ToString())
                .FirstOrDefault(address => !IPAddress.IsLoopback(IPAddress.Parse(address))) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private bool TryValidateIp(string ipText, string fieldName)
    {
        if (IPAddress.TryParse(ipText.Trim(), out _))
        {
            return true;
        }

        ShowWarning(TextKeys.SystemSetting.MessageInvalidIp, fieldName);
        return false;
    }

    private bool TryParsePort(string portText, string fieldName, out int port)
    {
        if (int.TryParse(portText.Trim(), out port) && port > 0 && port <= 65535)
        {
            return true;
        }

        ShowWarning(TextKeys.SystemSetting.MessageInvalidPort, fieldName);
        return false;
    }

    private bool TryParsePositiveInt(string text, string fieldName, out int value)
    {
        if (int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value > 0)
        {
            return true;
        }

        ShowWarning(TextKeys.SystemSetting.MessagePositiveIntegerRequired, fieldName);
        return false;
    }

    private static bool TryValidateBaseUrl(string baseUrl)
    {
        return Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string NormalizeCaption(string? text)
    {
        return (text ?? string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Trim();
    }

    private static string NormalizePlcType(string? plcType)
    {
        return PlcTypeOptions.Any(option => string.Equals(option.Value, plcType, StringComparison.OrdinalIgnoreCase))
            ? plcType ?? AppConstants.PlcTypes.ModbusTcp
            : AppConstants.PlcTypes.ModbusTcp;
    }

    private static string NormalizePlcStringNumericFormatMode(string? mode)
    {
        var normalizedValue = mode?.Trim();
        return PlcStringNumericFormatModeOptions.Any(option => string.Equals(option.Value, normalizedValue, StringComparison.OrdinalIgnoreCase))
            ? normalizedValue!
            : AppConstants.PlcStringNumericFormatModes.Truncate;
    }

    private static UploadMode NormalizeUploadMode(UploadMode mode)
    {
        return Enum.IsDefined(typeof(UploadMode), mode) ? mode : UploadMode.Quantity;
    }

    private static string NormalizeProcessParameterDeviceType(string? value)
    {
        var normalizedValue = value?.Trim();
        return ProcessParameterDeviceTypeOptions.Any(option => string.Equals(option.Value, normalizedValue, StringComparison.OrdinalIgnoreCase))
            ? normalizedValue!
            : ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic;
    }

    private static string NormalizeCenterServerSystemType(string? value)
    {
        var normalizedValue = value?.Trim();
        return CenterServerSystemTypeOptions.Any(option => string.Equals(option.Value, normalizedValue, StringComparison.OrdinalIgnoreCase))
            ? normalizedValue!
            : CenterServerConstants.SystemTypes.Other;
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

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

    private void ShowError(string messageKey, params object[] args)
    {
        ShowErrorMessage(_localizer.GetString(messageKey, args));
    }

    private void ShowErrorMessage(string message)
    {
        if (FindForm() is not { IsDisposed: false, Disposing: false } owner)
        {
            return;
        }

        AntdUI.Message.error(owner, message, autoClose: AlertMessageAutoCloseSeconds);
    }

    private sealed record LocalizedOption<T>(T Value, string TextKey);

    private sealed record MesRouteInputDefinition(
        string Key,
        string TextKey,
        string DefaultRoute,
        Func<AppSettings, string?> GetRoute,
        Action<AppSettings, string> SetRoute);
}
