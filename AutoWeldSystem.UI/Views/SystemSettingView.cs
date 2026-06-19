using AutoWeldSystem.Core.Constants;
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

    private readonly IAppSettingsService _settingsService;
    private readonly IMesProvider _mesProvider;
    private readonly ILocalizationService _localizer;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IWeldTaskService _weldTaskService;
    private readonly IWindowsShellIntegrationService _windowsShellIntegrationService;

    private bool _initialized;
    private bool _syncingPlcTypeSelection;
    private bool _syncingUploadModeSelection;
    private bool _syncingDualModeSelection;
    private bool _syncingProcessParameterDeviceTypeSelection;
    private string _selectedPlcType = AppConstants.PlcTypes.ModbusTcp;
    private UploadMode _selectedUploadMode = UploadMode.Quantity;
    private string _selectedProcessParameterDeviceType = ProductionConstants.ProcessParameterDeviceTypes.Electromagnetic;
    private AppSettings _currentSettings;

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
        btnConnectMasterController.Click += ConnectMasterController_ClickAsync;
        btnChangeLogPath.Click += (_, _) => SelectFolder(input_LogsPath, BuildFieldName(grpDeviceConfig.Text, lblLogPath.Text));
        btnChangeDataPath.Click += (_, _) => SelectFolder(input_DataPath, BuildFieldName(grpDeviceConfig.Text, lblDataPath.Text));
        btnOpenLogPath.Click += (_, _) => OpenFolder(input_LogsPath.Text, BuildFieldName(grpDeviceConfig.Text, lblLogPath.Text));
        btnOpenDataPath.Click += (_, _) => OpenFolder(input_DataPath.Text, BuildFieldName(grpDeviceConfig.Text, lblDataPath.Text));
        select_PlcType.SelectedIndexChanged += Select_PlcType_SelectedIndexChanged;
        selectUploadMode.SelectedIndexChanged += SelectUploadMode_SelectedIndexChanged;
        chkEnableDualStation.CheckedChanged += ChkEnableDualStation_CheckedChanged;
        chkEnableDualWorkOrder.CheckedChanged += ChkEnableDualWorkOrder_CheckedChanged;
        selectProcessParameterDeviceType.SelectedIndexChanged += SelectProcessParameterDeviceType_SelectedIndexChanged;
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
            _windowsShellIntegrationService.ApplyStartupIntegration(_currentSettings);
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
            if (!CanSaveRuntimeModeChange(previousSettings, settings))
            {
                BindSettings(previousSettings);
                return;
            }

            var request = BuildDeviceRequest(previousSettings, settings);

            _currentSettings = _settingsService.Save(settings);
            BindSettings(_currentSettings);
            _windowsShellIntegrationService.ApplyStartupIntegration(_currentSettings);

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

    private async void ConnectMasterController_ClickAsync(object? sender, EventArgs e)
    {
        await TestTcpEndpointAsync(input_MasterIp.Text, input_MasterPort.Text, grpMasterConfig.Text, btnConnectMasterController);
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

    private void ChkEnableDualStation_CheckedChanged(object sender, AntdUI.BoolEventArgs e)
    {
        if (_syncingDualModeSelection)
        {
            return;
        }

        // 双工单必须依赖双工位；取消双工位时同步取消双工单，避免保存非法组合。
        if (!e.Value && chkEnableDualWorkOrder.Checked)
        {
            SetDualModeCheckboxes(enableDualStation: false, enableDualWorkOrder: false);
        }
    }

    private void ChkEnableDualWorkOrder_CheckedChanged(object sender, AntdUI.BoolEventArgs e)
    {
        if (_syncingDualModeSelection)
        {
            return;
        }

        // 用户勾选双工单时自动开启双工位，表达“双工位双工单”模式。
        if (e.Value && !chkEnableDualStation.Checked)
        {
            SetDualModeCheckboxes(enableDualStation: true, enableDualWorkOrder: true);
        }
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
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
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
        input_MasterIp.Text = settings.MasterControlIp;
        input_MasterPort.Text = settings.MasterControlPort.ToString().Trim();
        input_MesTimeout.Text = settings.MesTimeoutSeconds.ToString();
        input_LogsPath.Text = settings.LogDirectory;
        input_DataPath.Text = settings.DataDirectory;
        chkEnableAutoStart.Checked = settings.EnableAutoStart ?? true;
        input_BaseUrl.Text = settings.MesBaseUrl;
        chkUseProductNumberFilter.Checked = settings.UseProductNumberFilter;
        SetDualModeCheckboxes(settings.EnableDualStation, settings.EnableDualWorkOrder);
        chkValidateRecipeBeforeStart.Checked = settings.ValidateRecipeAfterStart;
        chkEnableFinishExpQtyPrompt.Checked = settings.EnableFinishExpQtyPrompt;
        inputPlcHeartbeatInterval.Text = Math.Clamp(settings.PlcHeartbeatReadIntervalMilliseconds <= 0 ? 300 : settings.PlcHeartbeatReadIntervalMilliseconds, 100, 5000).ToString(CultureInfo.InvariantCulture);

        _selectedPlcType = NormalizePlcType(settings.PlcType);
        _selectedUploadMode = NormalizeUploadMode(settings.UploadMode);
        _selectedProcessParameterDeviceType = NormalizeProcessParameterDeviceType(settings.ProcessParameterDeviceType);
        inputUploadBatchSize.Text = Math.Max(1, settings.UploadBatchSize).ToString(CultureInfo.InvariantCulture);
        BindPlcTypeOptions();
        BindUploadModeOptions();
        BindProcessParameterDeviceTypeOptions();
        UpdateUploadBatchSizeEnabled();
    }

    /// <summary>
    /// 根据当前语言回填页面静态文本。
    /// </summary>
    private void ApplyLocalizedTexts()
    {
        tabBasicSettings.Text = _localizer.GetString(TextKeys.SystemSetting.TabBasic);

        grpPlcConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupPlc);
        grpMasterConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupController);
        grpAppConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupApplication);
        grpDeviceConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupDevice);
        grpProductionConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupProduction);
        grpMesConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupMes);

        lblTitle.Text = _localizer.GetString(TextKeys.SystemSetting.Title);
        lblDescription.Text = _localizer.GetString(TextKeys.SystemSetting.Description);

        lblPlcIp.Text = _localizer.GetString(TextKeys.SystemSetting.LabelIp);
        lblPlcPort.Text = _localizer.GetString(TextKeys.SystemSetting.LabelPort);
        lblPlcType.Text = _localizer.GetString(TextKeys.SystemSetting.LabelType);

        lblMasterIp.Text = _localizer.GetString(TextKeys.SystemSetting.LabelIp);
        lblMasterPort.Text = _localizer.GetString(TextKeys.SystemSetting.LabelPort);

        lblDeviceId.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDeviceId);
        lblDeviceName.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDeviceName);
        lblDeviceUrl.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDeviceStatusUrl);
        lblMesUrl.Text = _localizer.GetString(TextKeys.SystemSetting.LabelMesUrl);
        label1.Text = "MES超时(s)";

        lblLogPath.Text = _localizer.GetString(TextKeys.SystemSetting.LabelLogPath);
        lblDataPath.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDataPath);
        chkEnableAutoStart.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableAutoStart);
        lblUploadMode.Text = _localizer.GetString(TextKeys.SystemSetting.UploadMode);
        lblUploadBatchSize.Text = _localizer.GetString(TextKeys.SystemSetting.UploadBatchSize);
        lblPlcHeartbeatInterval.Text = _localizer.GetString(TextKeys.SystemSetting.PlcHeartbeatRate);
        lblProcessParameterDeviceType.Text = "过程参数设备类型";

        BindUploadModeOptions();
        BindProcessParameterDeviceTypeOptions();

        chkUseProductNumberFilter.Text = _localizer.GetString(TextKeys.SystemSetting.ChkUseProductNumberFilter);
        chkValidateRecipeBeforeStart.Text = _localizer.GetString(TextKeys.SystemSetting.ChkValidateRecipeAfterStart);
        chkEnableFinishExpQtyPrompt.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableFinishExpQtyPrompt);
        chkEnableDualStation.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableDualStation);
        chkEnableDualWorkOrder.Text = _localizer.GetString(TextKeys.SystemSetting.ChkEnableDualWorkOrder);

        btnConnectPlc.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonConnect);
        btnConnectMasterController.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonConnect);
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

    private void UpdateUploadBatchSizeEnabled()
    {
        inputUploadBatchSize.Enabled = _selectedUploadMode == UploadMode.Quantity;
        if (string.IsNullOrWhiteSpace(inputUploadBatchSize.Text))
        {
            inputUploadBatchSize.Text = "1";
        }
    }

    /// <summary>
    /// 从配置回填双工位/双工单开关时临时屏蔽联动事件，避免加载配置时反复触发 UI 逻辑。
    /// </summary>
    private void SetDualModeCheckboxes(bool enableDualStation, bool enableDualWorkOrder)
    {
        _syncingDualModeSelection = true;
        try
        {
            chkEnableDualStation.Checked = enableDualStation || enableDualWorkOrder;
            chkEnableDualWorkOrder.Checked = enableDualWorkOrder;
        }
        finally
        {
            _syncingDualModeSelection = false;
        }
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

        var masterControlIp = input_MasterIp.Text.Trim();
        if (!TryValidateIp(masterControlIp, BuildFieldName(grpMasterConfig.Text, lblMasterIp.Text)))
        {
            return false;
        }

        if (!TryParsePort(input_MasterPort.Text, BuildFieldName(grpMasterConfig.Text, lblMasterPort.Text), out var masterControlPort))
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

        if (!TryParsePositiveInt(inputUploadBatchSize.Text, NormalizeCaption(lblUploadBatchSize.Text), out var uploadBatchSize))
        {
            return false;
        }

        if (!TryParsePositiveInt(inputPlcHeartbeatInterval.Text, NormalizeCaption(lblPlcHeartbeatInterval.Text), out var heartbeatInterval))
        {
            return false;
        }

        var mesTimeout = input_MesTimeout.Text;
        var enableDualStation = chkEnableDualStation.Checked;
        var enableDualWorkOrder = chkEnableDualWorkOrder.Checked;
        if (enableDualWorkOrder && !enableDualStation)
        {
            ShowWarningMessage("启用双工单时必须同时启用双工位。");
            return false;
        }

        settings.DeviceId = deviceId;
        settings.DeviceName = deviceName;
        settings.DeviceBaseUrl = deviceStatusUrl;
        settings.PlcIp = plcIp;
        settings.PlcPort = plcPort;
        settings.PlcType = NormalizePlcType(_selectedPlcType);
        settings.MasterControlIp = masterControlIp;
        settings.MasterControlPort = masterControlPort;
        settings.LogDirectory = logDirectory;
        settings.DataDirectory = dataDirectory;
        settings.EnableAutoStart = chkEnableAutoStart.Checked;
        settings.MesBaseUrl = mesBaseUrl;
        settings.MesTimeoutSeconds = int.TryParse(mesTimeout, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeout) && timeout > 0 ? timeout : 10;
        settings.UseProductNumberFilter = chkUseProductNumberFilter.Checked;
        settings.EnableDualStation = enableDualStation;
        settings.EnableDualWorkOrder = enableDualWorkOrder;
        settings.ValidateRecipeAfterStart = chkValidateRecipeBeforeStart.Checked;
        settings.EnableFinishExpQtyPrompt = chkEnableFinishExpQtyPrompt.Checked;
        settings.PlcHeartbeatReadIntervalMilliseconds = Math.Clamp(heartbeatInterval, 100, 5000);
        settings.UploadMode = NormalizeUploadMode(_selectedUploadMode);
        settings.UploadBatchSize = Math.Max(1, uploadBatchSize);
        settings.ProcessParameterDeviceType = NormalizeProcessParameterDeviceType(_selectedProcessParameterDeviceType);
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
            DevStatusUrl = newSettings.DeviceBaseUrl?.Trim() ?? string.Empty,
            PostDataDomain = EnsureTrailingSlash(newSettings.MesBaseUrl)
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

    private static string EnsureTrailingSlash(string text)
    {
        var value = text.Trim();
        return value.EndsWith("/", StringComparison.Ordinal) ? value : $"{value}/";
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

    private sealed record UploadModeOption(UploadMode Value, string DisplayName);

    private sealed record ProcessParameterDeviceTypeOption(string DisplayName, string Value);
}
