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
using AutoWeldSystem.Core.Runtime;

namespace AutoWeldSystem.UI.Views;

public partial class SystemSettingView : BaseView
{
    private static readonly PlcTypeOption[] PlcTypeOptions =
    {
        new(AppConstants.PlcTypes.ModbusTcp, TextKeys.SystemSetting.PlcTypeModbusTcp),
        new(AppConstants.PlcTypes.TcpSocket, TextKeys.SystemSetting.PlcTypeTcpSocket),
        new(AppConstants.PlcTypes.SiemensS71200, TextKeys.SystemSetting.PlcTypeSiemensS71200)
    };

    private static readonly PlcStringNumericFormatModeOption[] PlcStringNumericFormatModeOptions =
    {
        new("固定长度裁切", AppConstants.PlcStringNumericFormatModes.Truncate),
        new("四舍五入", AppConstants.PlcStringNumericFormatModes.Round)
    };

    private static readonly UploadModeOption[] UploadModeOptions =
    {
        new(UploadMode.Realtime, "单件实时上传"),
        new(UploadMode.Quantity, "按特定数量上传"),
        new(UploadMode.Batch, "完工批量上传")
    };

    private static readonly ProcessParameterDeviceTypeOption[] ProcessParameterDeviceTypeOptions =
    {
        new("电磁系统", ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic),
        new("整件系统-检测设备", ProductionConstants.ProcessParameterDeviceTypes.WholePieceCheck),
        new("整件系统-点焊设备", ProductionConstants.ProcessParameterDeviceTypes.WholePieceWeld)
    };

    private static readonly CenterServerOption[] CenterServerSystemTypeOptions =
    {
        new("电磁系统", CenterServerConstants.SystemTypes.Electromagnetic),
        new("整件系统", CenterServerConstants.SystemTypes.WholePiece),
        new("其它", CenterServerConstants.SystemTypes.Other)
    };

    private static readonly MesRouteInputDefinition[] MesRouteInputDefinitions =
    {
        new("User", "员工信息路由", MesEndpointRouteRules.UserDefaultRoute, settings => settings.MesUserRoute, (settings, route) => settings.MesUserRoute = route),
        new("WorkOrder", "工单信息路由", MesEndpointRouteRules.WorkOrderDefaultRoute, settings => settings.MesWorkOrderRoute, (settings, route) => settings.MesWorkOrderRoute = route),
        new("ServerTime", "服务器时间路由", MesEndpointRouteRules.ServerTimeDefaultRoute, settings => settings.MesServerTimeRoute, (settings, route) => settings.MesServerTimeRoute = route),
        new("ProgramManage", "程序管理路由", MesEndpointRouteRules.ProgramManageDefaultRoute, settings => settings.MesProgramManageRoute, (settings, route) => settings.MesProgramManageRoute = route),
        new("StartWork", "开工上报路由", MesEndpointRouteRules.StartWorkDefaultRoute, settings => settings.MesStartWorkRoute, (settings, route) => settings.MesStartWorkRoute = route),
        new("WorkStatus", "工单状态路由", MesEndpointRouteRules.WorkStatusDefaultRoute, settings => settings.MesWorkStatusRoute, (settings, route) => settings.MesWorkStatusRoute = route),
        new("EndWork", "完工上报路由", MesEndpointRouteRules.EndWorkDefaultRoute, settings => settings.MesEndWorkRoute, (settings, route) => settings.MesEndWorkRoute = route),
        new("ReportFile", "报告文件路由", MesEndpointRouteRules.ReportFileDefaultRoute, settings => settings.MesReportFileRoute, (settings, route) => settings.MesReportFileRoute = route),
        new("PostData", "PostData路由", MesEndpointRouteRules.PostDataDefaultRoute, settings => settings.MesPostDataRoute, (settings, route) => settings.MesPostDataRoute = route),
        new("Device", "设备编号路由", MesEndpointRouteRules.DeviceDefaultRoute, settings => settings.MesDeviceRoute, (settings, route) => settings.MesDeviceRoute = route),
        new("DeviceStatus", "设备状态路由", MesEndpointRouteRules.DeviceStatusDefaultRoute, settings => settings.MesDeviceStatusRoute, (settings, route) => settings.MesDeviceStatusRoute = route)
    };

    private readonly IAppSettingsService _settingsService;
    private readonly IMesProvider _mesProvider;
    private readonly ILocalizationService _localizer;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IWindowsShellIntegrationService _windowsShellIntegrationService;

    private bool _initialized;
    private bool _syncingPlcTypeSelection;
    private bool _syncingPlcStringNumericFormatModeSelection;
    private bool _syncingUploadModeSelection;
    private bool _syncingProcessParameterDeviceTypeSelection;
    private bool _syncingCenterServerSystemTypeSelection;
    private string _selectedPlcType = AppConstants.PlcTypes.ModbusTcp;
    private string _selectedPlcStringNumericFormatMode = AppConstants.PlcStringNumericFormatModes.Truncate;
    private UploadMode _selectedUploadMode = UploadMode.Quantity;
    private string _selectedProcessParameterDeviceType = ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic;
    private string _selectedCenterServerSystemType = CenterServerConstants.SystemTypes.Other;
    private AppSettings _currentSettings;
    private SystemSettingLayoutMode? _lastLayoutMode;
    private Size _lastLayoutViewportSize = Size.Empty;
    private int _lastLayoutDpi;

    public SystemSettingView(
        IAppSettingsService settingsService,
        IMesProvider mesProvider,
        ILocalizationService localizer,
        IPlcCommunicationService plcCommunicationService,
        IWeldTaskService weldTaskService,
        IWindowsShellIntegrationService windowsShellIntegrationService)
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

        WireEvents();
    }

    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        BindPlcTypeOptions();
        BindPlcStringNumericFormatModeOptions();
        ApplyBasicSettingsLayout(force: true);
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
        RefreshDeviceManagementEnabled();
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

        var viewportSize = basicSettingsViewport.ClientSize;
        var mode = SystemSettingLayoutRules.ResolveMode(basicSettingsViewport.ClientSize.Width, DeviceDpi);
        if (!force && mode == _lastLayoutMode && viewportSize == _lastLayoutViewportSize && DeviceDpi == _lastLayoutDpi)
        {
            return;
        }

        basicSettingsLayout.SuspendLayout();
        try
        {
            ConfigureBasicSettingsGrid(mode);
            _lastLayoutMode = mode;
            _lastLayoutViewportSize = viewportSize;
            _lastLayoutDpi = DeviceDpi;
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
        btnChangeProgramFilePath.Click += (_, _) => SelectFolder(input_ProgramFilePath, BuildFieldName(grpDeviceConfig.Text, lblProgramFilePath.Text));
        btnOpenLogPath.Click += (_, _) => OpenFolder(input_LogsPath.Text, BuildFieldName(grpDeviceConfig.Text, lblLogPath.Text));
        btnOpenDataPath.Click += (_, _) => OpenFolder(input_DataPath.Text, BuildFieldName(grpDeviceConfig.Text, lblDataPath.Text));
        btnOpenProgramFilePath.Click += (_, _) => OpenFolder(input_ProgramFilePath.Text, BuildFieldName(grpDeviceConfig.Text, lblProgramFilePath.Text));
        select_PlcType.SelectedIndexChanged += Select_PlcType_SelectedIndexChanged;
        chkEnablePlcStringNumericFormatting.CheckedChanged += ChkEnablePlcStringNumericFormatting_CheckedChanged;
        chkEnablePlcAlarmReading.CheckedChanged += ChkEnablePlcAlarmReading_CheckedChanged;
        selectPlcStringNumericFormatMode.SelectedIndexChanged += SelectPlcStringNumericFormatMode_SelectedIndexChanged;
        selectUploadMode.SelectedIndexChanged += SelectUploadMode_SelectedIndexChanged;
        chkEnableAutoStart.CheckedChanged += ChkEnableAutoStart_CheckedChanged;
        chkEnablePostDataCustomHeader.CheckedChanged += ChkEnablePostDataCustomHeader_CheckedChanged;
        selectProcessParameterDeviceType.SelectedIndexChanged += SelectProcessParameterDeviceType_SelectedIndexChanged;
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

        try
        {
            var previousSettings = _currentSettings;
            if (!CanSaveDeviceManagementChange(previousSettings, settings))
            {
                BindSettings(previousSettings);
                RefreshDeviceManagementEnabled();
                return;
            }

            if (!CanSaveRuntimeModeChange(previousSettings, settings))
            {
                BindSettings(previousSettings);
                return;
            }

            var shouldSyncDevice = HasDeviceIdentityChanged(previousSettings, settings);
            var shouldRestartPlc = HasPlcCommunicationChanged(previousSettings, settings);
            var syncRequest = BuildDeviceRequest(previousSettings, settings);

            _currentSettings = _settingsService.Save(settings);
            BindSettings(_currentSettings);
            ApplyStartupIntegrationWithWarning(_currentSettings);

            if (shouldRestartPlc)
            {
                await _plcCommunicationService.RestartAsync();
            }

            if (shouldSyncDevice && await SyncDeviceToMesAsync(syncRequest, btnSaveAll, false))
            {
                MarkDeviceSynced();
            }

            ShowInfoMessage(_localizer.GetString(TextKeys.Common.SaveSuccess));
        }
        catch (Exception ex)
        {
            ShowErrorMessage(_localizer.GetString(TextKeys.Common.SaveFailed, ex.Message));
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

        try
        {
            var previousSettings = _currentSettings;
            if (!CanSaveDeviceManagementChange(previousSettings, settings))
            {
                BindSettings(previousSettings);
                RefreshDeviceManagementEnabled();
                return;
            }

            if (!CanSaveRuntimeModeChange(previousSettings, settings))
            {
                BindSettings(previousSettings);
                return;
            }

            var request = BuildDeviceRequest(previousSettings, settings);

            _currentSettings = _settingsService.Save(settings);
            BindSettings(_currentSettings);
            ApplyStartupIntegrationWithWarning(_currentSettings);

            if (await SyncDeviceToMesAsync(request, btnSyncDevice, true))
            {
                MarkDeviceSynced();
            }
        }
        catch (Exception ex)
        {
            ShowError(TextKeys.SystemSetting.MessageDeviceSyncFailed, ex.Message);
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
                ShowInfo(TextKeys.SystemSetting.MessageMesConnectionSuccess, response.Data?.CurrentTime ?? string.Empty);
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
        ApplyLocalizedTexts();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _settingsService.SettingsChanged -= SettingsService_SettingsChanged;
        base.OnHandleDestroyed(e);
    }

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        var settings = e.CurrentSettings;
        Interlocked.Exchange(ref _currentSettings, settings);
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
        input_LogsPath.Text = settings.LogDirectory;
        input_DataPath.Text = settings.DataDirectory;
        input_ProgramFilePath.Text = settings.ProgramFileDirectory;
        chkEnableAutoStart.Checked = settings.EnableAutoStart ?? true;
        chkEnableElevatedAutoStart.Checked = settings.EnableElevatedAutoStart ?? true;
        chkEnableCenterServerSync.Checked = settings.EnableCenterServerSync;
        inputCenterServerBaseUrl.Text = CenterTelemetryRules.NormalizeBaseUrl(settings.CenterServerBaseUrl);
        inputCenterServerHeartbeatInterval.Text = CenterTelemetryRules.NormalizeHeartbeatIntervalSeconds(
            settings.CenterServerHeartbeatIntervalSeconds).ToString(CultureInfo.InvariantCulture);
        input_BaseUrl.Text = settings.MesBaseUrl;
        BindMesEndpointSettings(settings);
        chkUseProductNumberFilter.Checked = settings.UseProductNumberFilter;
        chkUseOperatorInputDialog.Checked = settings.UseOperatorInputDialog != false;
        chkShowTestFlagInHistory.Checked = settings.ShowTestFlagInHistory != false;
        chkEnableDeviceStatusReport.Checked = settings.EnableDeviceStatusReport != false;
        chkEnableWorkOrderStatusReport.Checked = settings.EnableWorkOrderStatusReport != false;
        chkEnableDualStation.Checked = settings.EnableDualStation || settings.EnableDualWorkOrder;
        inputStation1DisplayName.Text = settings.Station1DisplayName;
        inputStation2DisplayName.Text = settings.Station2DisplayName;
        chkValidateRecipeBeforeStart.Checked = settings.ValidateRecipeAfterStart;
        chkEnableFinishExpQtyPrompt.Checked = settings.EnableFinishExpQtyPrompt;
        inputPlcHeartbeatInterval.Text = Math.Clamp(settings.PlcHeartbeatReadIntervalMilliseconds <= 0 ? 300 : settings.PlcHeartbeatReadIntervalMilliseconds, 100, 5000).ToString(CultureInfo.InvariantCulture);

        _selectedPlcType = NormalizePlcType(settings.PlcType);
        _selectedPlcStringNumericFormatMode = NormalizePlcStringNumericFormatMode(settings.PlcStringNumericFormatMode);
        _selectedUploadMode = NormalizeUploadMode(settings.UploadMode);
        _selectedProcessParameterDeviceType = NormalizeProcessParameterDeviceType(settings.ProcessParameterDeviceType);
        _selectedCenterServerSystemType = NormalizeCenterServerSystemType(settings.CenterServerSystemType);
        inputUploadBatchSize.Text = Math.Max(1, settings.UploadBatchSize).ToString(CultureInfo.InvariantCulture);
        BindPlcTypeOptions();
        BindPlcStringNumericFormatModeOptions();
        BindUploadModeOptions();
        BindProcessParameterDeviceTypeOptions();
        BindCenterServerSystemTypeOptions();
        UpdatePlcStringNumericFormatModeEnabled();
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
        grpCenterServerConfig.Text = "中心服务器";

        lblTitle.Text = _localizer.GetString(TextKeys.SystemSetting.Title);
        lblDescription.Text = _localizer.GetString(TextKeys.SystemSetting.Description);

        lblPlcIp.Text = _localizer.GetString(TextKeys.SystemSetting.LabelIp);
        lblPlcPort.Text = _localizer.GetString(TextKeys.SystemSetting.LabelPort);
        lblPlcType.Text = _localizer.GetString(TextKeys.SystemSetting.LabelType);
        chkEnablePlcStringNumericFormatting.Text = "启用 PLC 字符串数值处理";
        chkEnablePlcAlarmReading.Text = "启用PLC报警读取";
        lblPlcStringNumericFormatMode.Text = "处理方式";

        lblDeviceId.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDeviceId);
        lblDeviceName.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDeviceName);
        lblDeviceUrl.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDeviceStatusUrl);
        lblMesUrl.Text = _localizer.GetString(TextKeys.SystemSetting.LabelMesUrl);
        lblMesTimeout.Text = "MES超时(s)";

        lblLogPath.Text = _localizer.GetString(TextKeys.SystemSetting.LabelLogPath);
        lblDataPath.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDataPath);
        lblProgramFilePath.Text = "程序目录";
        chkEnableAutoStart.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableAutoStart);
        chkEnableElevatedAutoStart.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableElevatedAutoStart);
        lblUploadMode.Text = _localizer.GetString(TextKeys.SystemSetting.UploadMode);
        lblUploadBatchSize.Text = _localizer.GetString(TextKeys.SystemSetting.UploadBatchSize);
        lblPlcHeartbeatInterval.Text = _localizer.GetString(TextKeys.SystemSetting.PlcHeartbeatRate);
        lblStation1DisplayName.Text = _localizer.GetString(TextKeys.SystemSetting.LabelStation1DisplayName);
        lblStation2DisplayName.Text = _localizer.GetString(TextKeys.SystemSetting.LabelStation2DisplayName);
        inputStation1DisplayName.PlaceholderText = _localizer.GetString(TextKeys.SystemSetting.PlaceholderStationDisplayName);
        inputStation2DisplayName.PlaceholderText = _localizer.GetString(TextKeys.SystemSetting.PlaceholderStationDisplayName);
        chkEnableCenterServerSync.Text = "启用中心服务器同步";
        lblCenterServerBaseUrl.Text = "中心服务器地址";
        lblCenterServerSystemType.Text = "系统类型";
        lblCenterServerHeartbeatInterval.Text = "心跳间隔(s)";
        lblProcessParameterDeviceType.Text = "过程参数设备类型";
        chkEnablePostDataCustomHeader.Text = "启用PostData自定义Header";
        lblPostDataHeaderKey.Text = "Header Key";
        lblPostDataHeaderValue.Text = "Header Value";
        chkShowTestFlagInHistory.Text = "产品历史显示试焊件";

        BindUploadModeOptions();
        BindPlcStringNumericFormatModeOptions();
        BindProcessParameterDeviceTypeOptions();

        chkUseProductNumberFilter.Text = _localizer.GetString(TextKeys.SystemSetting.ChkUseProductNumberFilter);
        chkUseOperatorInputDialog.Text = _localizer.GetString(TextKeys.SystemSetting.ChkUseOperatorInputDialog);
        chkEnableDeviceStatusReport.Text = "启用设备状态上报";
        chkEnableWorkOrderStatusReport.Text = "启用工单状态上报";
        chkValidateRecipeBeforeStart.Text = _localizer.GetString(TextKeys.SystemSetting.ChkValidateRecipeAfterStart);
        chkEnableFinishExpQtyPrompt.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableFinishExpQtyPrompt);
        chkEnableDualStation.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableDualStation);

        btnConnectPlc.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonConnect);
        btnSyncDevice.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonSyncDevice);
        btnTestConnection.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonTestConnection);
        btnChangeLogPath.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonChangePath);
        btnChangeDataPath.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonChangePath);
        btnChangeProgramFilePath.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonChangePath);
        btnOpenLogPath.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonOpenFolder);
        btnOpenDataPath.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonOpenFolder);
        btnOpenProgramFilePath.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonOpenFolder);
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
                .Select(option => (object)option.DisplayName)
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

    private void BindUploadModeOptions()
    {
        _syncingUploadModeSelection = true;
        try
        {
            selectUploadMode.Items.Clear();
            selectUploadMode.Items.AddRange(UploadModeOptions
                .Select(option => (object)option.DisplayName)
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
                .Select(option => (object)option.DisplayName)
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

    private void BindCenterServerSystemTypeOptions()
    {
        _syncingCenterServerSystemTypeSelection = true;
        try
        {
            selectCenterServerSystemType.Items.Clear();
            selectCenterServerSystemType.Items.AddRange(CenterServerSystemTypeOptions
                .Select(option => (object)option.DisplayName)
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

    private async Task<bool> SyncDeviceToMesAsync(AddDeviceReq request, Control triggerButton, bool showSuccessMessage)
    {
        triggerButton.Enabled = false;
        try
        {
            var response = await _mesProvider.SetDeviceIdAsync(request);
            if (response.IsSuccess)
            {
                if (showSuccessMessage)
                {
                    ShowInfo(TextKeys.SystemSetting.MessageDeviceSyncSuccess);
                }

                return true;
            }

            ShowError(TextKeys.SystemSetting.MessageDeviceSyncFailed, response.Msg);
            return false;
        }
        finally
        {
            triggerButton.Enabled = true;
        }
    }

    /// <summary>
    /// MES 确认成功后再更新“已同步编号”，保证失败重试时 OldDeviceId 仍然正确。
    /// </summary>
    private void MarkDeviceSynced()
    {
        var settings = CurrentSettings.Clone();
        settings.MesSyncedDeviceId = settings.DeviceId;
        _currentSettings = _settingsService.Save(settings);
        BindSettings(_currentSettings);
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
            ShowWarningMessage(startupResult.Message);
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

        var programFileDirectory = input_ProgramFilePath.Text.Trim();
        if (string.IsNullOrWhiteSpace(programFileDirectory))
        {
            ShowWarning(TextKeys.SystemSetting.MessageValueRequired, NormalizeCaption(lblProgramFilePath.Text));
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

        if (!TryParsePositiveInt(inputCenterServerHeartbeatInterval.Text, NormalizeCaption(lblCenterServerHeartbeatInterval.Text), out var centerHeartbeatInterval))
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
        settings.PlcStringNumericFormatMode = NormalizePlcStringNumericFormatMode(_selectedPlcStringNumericFormatMode);
        settings.LogDirectory = logDirectory;
        settings.DataDirectory = dataDirectory;
        settings.ProgramFileDirectory = programFileDirectory;
        settings.EnableAutoStart = chkEnableAutoStart.Checked;
        settings.EnableElevatedAutoStart = chkEnableElevatedAutoStart.Checked;
        settings.EnableCenterServerSync = chkEnableCenterServerSync.Checked;
        settings.CenterServerBaseUrl = CenterTelemetryRules.NormalizeBaseUrl(centerServerBaseUrl);
        settings.CenterServerSystemType = NormalizeCenterServerSystemType(_selectedCenterServerSystemType);
        settings.CenterServerHeartbeatIntervalSeconds = CenterTelemetryRules.NormalizeHeartbeatIntervalSeconds(centerHeartbeatInterval);
        settings.MesBaseUrl = mesBaseUrl;
        settings.MesTimeoutSeconds = int.TryParse(mesTimeout, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeout) && timeout > 0 ? timeout : 10;
        settings.UseProductNumberFilter = chkUseProductNumberFilter.Checked;
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
        settings.PlcHeartbeatReadIntervalMilliseconds = Math.Clamp(heartbeatInterval, 100, 5000);
        settings.UploadMode = NormalizeUploadMode(_selectedUploadMode);
        settings.UploadBatchSize = Math.Max(1, uploadBatchSize);
        settings.ProcessParameterDeviceType = NormalizeProcessParameterDeviceType(_selectedProcessParameterDeviceType);
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
                    definition.DisplayName,
                    out var route,
                    out var errorMessage))
            {
                ShowWarningMessage(errorMessage);
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
            ShowWarningMessage(headerError);
            return false;
        }

        settings.EnablePostDataCustomHeader = headerEnabled;
        settings.PostDataHeaderKey = headerKey;
        settings.PostDataHeaderValue = headerValue;
        return true;
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

        ShowWarningMessage("存在未完工任务，不能切换双工位/双工单模式，请先完工后再调整。");
        return false;
    }

    /// <summary>
    /// 未完工期间禁止保存设备身份和设备通信地址，避免运行中的任务关联到变化后的设备。
    /// </summary>
    private bool CanSaveDeviceManagementChange(AppSettings previousSettings, AppSettings newSettings)
    {
        if (!HasDeviceIdentityChanged(previousSettings, newSettings) || !HasAnyUnfinishedTask())
        {
            return true;
        }

        ShowWarningMessage("存在未完工任务，请先完工后再修改设备管理信息。");
        return false;
    }

    /// <summary>
    /// 任一工位存在未完工任务时，统一禁用整个设备管理模块。
    /// </summary>
    private void RefreshDeviceManagementEnabled()
    {
        grpDeviceConfig.Enabled = !HasAnyUnfinishedTask();
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
            || oldSettings.PlcHeartbeatReadIntervalMilliseconds != newSettings.PlcHeartbeatReadIntervalMilliseconds;
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

        MessageBox.Show(
            this,
            $"{fieldName} 必须是大于 0 的整数。",
            _localizer.GetString(TextKeys.Common.TitleWarning),
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
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
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleInfo), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowWarning(string messageKey, params object[] args)
    {
        MessageBox.Show(this, _localizer.GetString(messageKey, args), _localizer.GetString(TextKeys.Common.TitleWarning), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ShowWarningMessage(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleWarning), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ShowError(string messageKey, params object[] args)
    {
        MessageBox.Show(this, _localizer.GetString(messageKey, args), _localizer.GetString(TextKeys.Common.TitleError), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void ShowErrorMessage(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleError), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private sealed record PlcTypeOption(string Value, string TextKey);

    private sealed record PlcStringNumericFormatModeOption(string DisplayName, string Value);

    private sealed record UploadModeOption(UploadMode Value, string DisplayName);

    private sealed record ProcessParameterDeviceTypeOption(string DisplayName, string Value);

    private sealed record CenterServerOption(string DisplayName, string Value);

    private sealed record MesRouteInputDefinition(
        string Key,
        string DisplayName,
        string DefaultRoute,
        Func<AppSettings, string?> GetRoute,
        Action<AppSettings, string> SetRoute);
}
