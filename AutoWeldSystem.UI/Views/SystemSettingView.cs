using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.UI.Base;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace AutoWeldSystem.UI.Views;

/// <summary>
/// 系统设置页。
/// 本页只维护基础系统参数；产品工艺和测试方案配置统一移动到地址维护页。
/// </summary>
public partial class SystemSettingView : BaseView
{
    private static readonly PlcTypeOption[] PlcTypeOptions =
    {
        new(AppConstants.PlcTypes.ModbusTcp, TextKeys.SystemSetting.PlcTypeModbusTcp),
        new(AppConstants.PlcTypes.TcpSocket, TextKeys.SystemSetting.PlcTypeTcpSocket),
        new(AppConstants.PlcTypes.SiemensS7, TextKeys.SystemSetting.PlcTypeSiemensS7)
    };

    private readonly IAppSettingsService _settingsService;
    private readonly IMesProvider _mesProvider;
    private readonly ILocalizationService _localizer;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private AppSettings _currentSettings = new();
    private bool _initialized;
    private bool _syncingPlcTypeSelection;
    private string _selectedPlcType = AppConstants.PlcTypes.ModbusTcp;

    public SystemSettingView(
        IAppSettingsService settingsService,
        IMesProvider mesProvider,
        ILocalizationService localizer,
        IPlcCommunicationService plcCommunicationService)
    {
        InitializeComponent();

        _settingsService = settingsService;
        _mesProvider = mesProvider;
        _localizer = localizer;
        _plcCommunicationService = plcCommunicationService;

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
    /// 统一绑定运行时事件，避免逻辑散落在 Designer 文件里。
    /// </summary>
    private void WireEvents()
    {
        btnSaveAll.Click += SaveAll_Click;
        btnSyncDevice.Click += SyncDevice_ClickAsync;
        btnTestConnection.Click += TestConnection_ClickAsync;
        btnConnectPlc.Click += ConnectPlc_ClickAsync;
        btnConnectMasterController.Click += ConnectMasterController_ClickAsync;
        btnChangeLogPath.Click += (_, _) => SelectFolder(input_LogsPath, BuildFieldName(grpAppConfig.Text, lblLogPath.Text));
        btnChangeDataPath.Click += (_, _) => SelectFolder(input_DataPath, BuildFieldName(grpAppConfig.Text, lblDataPath.Text));
        btnOpenLogPath.Click += (_, _) => OpenFolder(input_LogsPath.Text, BuildFieldName(grpAppConfig.Text, lblLogPath.Text));
        btnOpenDataPath.Click += (_, _) => OpenFolder(input_DataPath.Text, BuildFieldName(grpAppConfig.Text, lblDataPath.Text));
        select_PlcType.SelectedIndexChanged += Select_PlcType_SelectedIndexChanged;
    }

    private void LoadSettings()
    {
        _currentSettings = _settingsService.Get();
        BindSettings(_currentSettings);
        ApplyLocalizedTexts();
    }

    /// <summary>
    /// 数据库配置加载后，统一回填到界面控件里。
    /// </summary>
    private void BindSettings(AppSettings settings)
    {
        input_DeviceID.Text = settings.DeviceId;
        input_DeviceName.Text = settings.DeviceName;
        input_DeviceUrl.Text = settings.DeviceStatusUrl ?? string.Empty;
        input_PlcIp.Text = settings.PlcIp;
        input_PlcPort.Text = settings.PlcPort.ToString();
        input_MasterIp.Text = settings.MasterControlIp;
        input_MasterPort.Text = settings.MasterControlPort.ToString();
        input_LogsPath.Text = settings.LogDirectory;
        input_DataPath.Text = settings.DataDirectory;
        input_BaseUrl.Text = settings.MesBaseUrl;
        chkUseProductNumberFilter.Checked = settings.UseProductNumberFilter;
        chkEnableDualStationMode.Checked = settings.EnableDualStationMode;
        chkValidateRecipeBeforeStart.Checked = settings.ValidateRecipeBeforeStart;

        _selectedPlcType = NormalizePlcType(settings.PlcType);
        BindPlcTypeOptions();
    }

    /// <summary>
    /// 页面静态文本不依赖 Designer 资源切换，这里手动统一设置。
    /// </summary>
    private void ApplyLocalizedTexts()
    {
        lblTitle.Text = _localizer.GetString(TextKeys.SystemSetting.Title);
        lblDescription.Text = _localizer.GetString(TextKeys.SystemSetting.Description);
        grpPlcConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupPlc);
        grpMasterConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupController);
        grpAppConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupApplication);
        groupBox1.Text = "生产配置";
        grpMesConfig.Text = "MES配置";

        lblPlcIp.Text = _localizer.GetString(TextKeys.SystemSetting.LabelIp);
        lblPlcPort.Text = _localizer.GetString(TextKeys.SystemSetting.LabelPort);
        lblPlcType.Text = _localizer.GetString(TextKeys.SystemSetting.LabelType);
        lblMasterIp.Text = _localizer.GetString(TextKeys.SystemSetting.LabelIp);
        lblMasterPort.Text = _localizer.GetString(TextKeys.SystemSetting.LabelPort);
        lblDeviceId.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDeviceId);
        lblDeviceName.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDeviceName);
        lblDeviceUrl.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDeviceStatusUrl);
        lblLogPath.Text = _localizer.GetString(TextKeys.SystemSetting.LabelLogPath);
        lblDataPath.Text = _localizer.GetString(TextKeys.SystemSetting.LabelDataPath);
        lblMesUrl.Text = _localizer.GetString(TextKeys.SystemSetting.LabelMesUrl);
        chkUseProductNumberFilter.Text = _localizer.GetString(TextKeys.SystemSetting.LabelUseProductNumberFilter);
        chkValidateRecipeBeforeStart.Text = _localizer.GetString(TextKeys.SystemSetting.LabelValidateRecipeBeforeStart);
        chkEnableDualStationMode.Text = "启用双工位双工单模式";

        btnConnectPlc.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonConnect);
        btnConnectMasterController.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonConnect);
        btnSyncDevice.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonSyncDevice);
        btnTestConnection.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonTestConnection);
        btnChangeLogPath.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonChangePath);
        btnChangeDataPath.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonChangePath);
        btnOpenLogPath.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonOpenFolder);
        btnOpenDataPath.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonOpenFolder);
        btnSaveAll.Text = _localizer.GetString(TextKeys.SystemSetting.ButtonApplyAll);
        tabBasicSettings.Text = "基础设置";
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

    private void Select_PlcType_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingPlcTypeSelection)
        {
            return;
        }

        if (select_PlcType.SelectedIndex < 0 || select_PlcType.SelectedIndex >= PlcTypeOptions.Length)
        {
            return;
        }

        _selectedPlcType = PlcTypeOptions[select_PlcType.SelectedIndex].Value;
    }

    /// <summary>
    /// 全局保存按钮负责收集当前页全部可编辑设置，并持久化到数据库。
    /// </summary>
    private async void SaveAll_Click(object? sender, EventArgs e)
    {
        if (!TryBuildSettings(out var settings))
        {
            return;
        }

        try
        {
            var previousSettings = _currentSettings;
            var shouldSyncDevice = HasDeviceIdentityChanged(previousSettings, settings);
            var syncRequest = BuildDeviceRequest(previousSettings, settings);

            _currentSettings = _settingsService.Save(settings);
            BindSettings(_currentSettings);
            await _plcCommunicationService.RestartAsync();

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

    private async void TestConnection_ClickAsync(object? sender, EventArgs e)
    {
        var mesFieldName = BuildFieldName(grpAppConfig.Text, lblMesUrl.Text);
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
            var response = await _mesProvider.TestConnectionAsync(baseUrl, timeoutSeconds);
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
            var request = BuildDeviceRequest(previousSettings, settings);

            _currentSettings = _settingsService.Save(settings);
            BindSettings(_currentSettings);

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

    private async Task<bool> SyncDeviceToMesAsync(AddDeviceRequest request, Control triggerButton, bool showSuccessMessage)
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
        var settings = _settingsService.Get();
        settings.MesSyncedDeviceId = settings.DeviceId;
        _currentSettings = _settingsService.Save(settings);
        BindSettings(_currentSettings);
    }

    private async void ConnectPlc_ClickAsync(object? sender, EventArgs e)
    {
        await TestTcpEndpointAsync(input_PlcIp.Text, input_PlcPort.Text, grpPlcConfig.Text, btnConnectPlc);
    }

    private async void ConnectMasterController_ClickAsync(object? sender, EventArgs e)
    {
        await TestTcpEndpointAsync(input_MasterIp.Text, input_MasterPort.Text, grpMasterConfig.Text, btnConnectMasterController);
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
        settings = _settingsService.Get();

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

        settings.DeviceId = deviceId;
        settings.DeviceName = deviceName;
        settings.DeviceStatusUrl = deviceStatusUrl;
        settings.PlcIp = plcIp;
        settings.PlcPort = plcPort;
        settings.PlcType = NormalizePlcType(_selectedPlcType);
        settings.MasterControlIp = masterControlIp;
        settings.MasterControlPort = masterControlPort;
        settings.LogDirectory = logDirectory;
        settings.DataDirectory = dataDirectory;
        settings.MesBaseUrl = mesBaseUrl;
        settings.UseProductNumberFilter = chkUseProductNumberFilter.Checked;
        settings.EnableDualStationMode = chkEnableDualStationMode.Checked;
        settings.ValidateRecipeBeforeStart = chkValidateRecipeBeforeStart.Checked;
        return true;
    }

    private AddDeviceRequest BuildDeviceRequest(AppSettings previousSettings, AppSettings newSettings)
    {
        return new AddDeviceRequest
        {
            OldDeviceId = GetMesOldDeviceId(previousSettings, newSettings),
            DeviceId = newSettings.DeviceId.Trim(),
            DeviceName = newSettings.DeviceName.Trim(),
            IP = GetLocalIPv4Address(),
            DevStatusUrl = newSettings.DeviceStatusUrl?.Trim() ?? string.Empty,
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
            || !SameText(oldSettings.DeviceStatusUrl, newSettings.DeviceStatusUrl)
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

    private static bool TryValidateBaseUrl(string baseUrl)
    {
        return Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private string BuildFieldName(string? groupName, string? fieldName)
    {
        var normalizedGroup = NormalizeCaption(groupName);
        var normalizedField = NormalizeCaption(fieldName);
        return string.IsNullOrWhiteSpace(normalizedGroup)
            ? normalizedField
            : $"{normalizedGroup} - {normalizedField}";
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

    private void ShowError(string messageKey, params object[] args)
    {
        MessageBox.Show(this, _localizer.GetString(messageKey, args), _localizer.GetString(TextKeys.Common.TitleError), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void ShowErrorMessage(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleError), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private sealed record PlcTypeOption(string Value, string TextKey);
}
