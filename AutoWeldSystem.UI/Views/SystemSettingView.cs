using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace AutoWeldSystem.UI.Views;

/// <summary>
/// 系统设置页。
/// 这里负责把数据库里的 AppSettings 显示到界面，并把用户修改后的值再保存回去。
/// </summary>
public partial class SystemSettingView : BaseView
{
    private static readonly PlcTypeOption[] PlcTypeOptions =
    {
        new(AppConstants.PlcTypes.ModbusTcp, TextKeys.SystemSetting.PlcTypeModbusTcp),
        new(AppConstants.PlcTypes.TcpSocket, TextKeys.SystemSetting.PlcTypeTcpSocket),
        new(AppConstants.PlcTypes.SiemensS7, TextKeys.SystemSetting.PlcTypeSiemensS7)
    };

    private static readonly string[] ProductNoSourceOptions =
    {
        ProductionConstants.ProductNoSources.AutoIncrement,
        ProductionConstants.ProductNoSources.Plc,
        ProductionConstants.ProductNoSources.Manual
    };

    private readonly IAppSettingsService _settingsService;
    private readonly IMesProvider _mesProvider;
    private readonly ILocalizationService _localizer;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IProductProcessConfigService _productProcessConfigService;
    private readonly List<BizProductProcessConfig> _productProcessConfigs = new();
    private AppSettings _currentSettings = new();
    private List<ProductProcessConfigTableRow> _productProcessRows = new();
    private ProductProcessConfigTableRow? _selectedProductProcessRow;
    private bool _initialized;
    private bool _syncingPlcTypeSelection;
    private string _selectedPlcType = AppConstants.PlcTypes.ModbusTcp;
    private readonly AntdUI.Checkbox chkUseProductNumberFilter = new();

    public SystemSettingView(
        IAppSettingsService settingsService,
        IMesProvider mesProvider,
        ILocalizationService localizer,
        IPlcCommunicationService plcCommunicationService,
        IProductProcessConfigService productProcessConfigService)
    {
        InitializeComponent();

        _settingsService = settingsService;
        _mesProvider = mesProvider;
        _localizer = localizer;
        _plcCommunicationService = plcCommunicationService;
        _productProcessConfigService = productProcessConfigService;

        ConfigureMesProgramFilterOption();
        ConfigureProductProcessTable();
        WireEvents();
    }

    /// <summary>
    /// 语言切换时，当前页的静态标题、按钮和下拉选项都要一起刷新。
    /// </summary>
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
        LoadProductProcessConfigs();
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
        btnAddProductProcess.Click += AddProductProcess_Click;
        btnSaveProductProcesses.Click += SaveProductProcesses_Click;
        btnDisableProductProcess.Click += DisableProductProcess_Click;
        btnRefreshProductProcesses.Click += (_, _) => LoadProductProcessConfigs();
        tableProductProcesses.CellClick += TableProductProcesses_CellClick;
        tableProductProcesses.CellEndEdit += TableProductProcesses_CellEndEdit;
        tableProductProcesses.CellEndValueEdit += TableProductProcesses_CellEndValueEdit;
        tableProductProcesses.CellEditComplete += TableProductProcesses_CellEditComplete;
        tableProductProcesses.CheckedChanged += TableProductProcesses_CheckedChanged;
    }

    /// <summary>
    /// 产品工艺配置表只负责维护静态工艺数据，不直接触发 PLC 或 MES 交互。
    /// </summary>
    private void ConfigureProductProcessTable()
    {
        TableStyleHelper.ApplyAntdTable(tableProductProcesses);
        tableProductProcesses.EditLostFocus = true;
        tableProductProcesses.LostFocusClearSelection = false;

        tableProductProcesses.Columns.Clear();
        tableProductProcesses.Columns.Add(CreateProductProcessColumn(nameof(ProductProcessConfigTableRow.ProductModel), "产品型号"));
        tableProductProcesses.Columns.Add(CreateProductProcessColumn(nameof(ProductProcessConfigTableRow.StationNo), "工位(0共享)"));
        tableProductProcesses.Columns.Add(CreateProductProcessColumn(nameof(ProductProcessConfigTableRow.ProcessNo), "工序号"));
        tableProductProcesses.Columns.Add(CreateProductProcessColumn(nameof(ProductProcessConfigTableRow.ProcessName), "工序名称"));
        tableProductProcesses.Columns.Add(CreateProductProcessColumn(nameof(ProductProcessConfigTableRow.WeldPointCount), "每件焊点数"));
        tableProductProcesses.Columns.Add(CreateProductProcessColumn(nameof(ProductProcessConfigTableRow.CollectionGroup), "采集组"));
        tableProductProcesses.Columns.Add(CreateProductProcessColumn(nameof(ProductProcessConfigTableRow.ProgramMatchRule), "程序匹配规则"));
        tableProductProcesses.Columns.Add(CreateProductNoSourceColumn());
        tableProductProcesses.Columns.Add(CreateProductProcessEnabledColumn());
        tableProductProcesses.Columns.Add(CreateProductProcessColumn(nameof(ProductProcessConfigTableRow.Sort), "排序"));
        tableProductProcesses.Columns.Add(CreateProductProcessColumn(nameof(ProductProcessConfigTableRow.Description), "备注"));
        tableProductProcesses.Columns.Add(CreateProductProcessColumn(nameof(ProductProcessConfigTableRow.UpdatedTime), "更新时间", readOnly: true, displayFormat: "yyyy-MM-dd HH:mm:ss"));
        TableStyleHelper.ApplyAntdColumnDefaults(tableProductProcesses);
    }

    private static AntdUI.Column CreateProductProcessColumn(string key, string title, bool readOnly = false, string? displayFormat = null)
    {
        return new AntdUI.Column(key, title)
        {
            Align = AntdUI.ColumnAlign.Center,
            ColAlign = AntdUI.ColumnAlign.Center,
            ReadOnly = readOnly,
            Editable = !readOnly,
            Ellipsis = true,
            DisplayFormat = displayFormat
        };
    }

    private static AntdUI.ColumnSelect CreateProductNoSourceColumn()
    {
        return new AntdUI.ColumnSelect(nameof(ProductProcessConfigTableRow.ProductNoSource), "产品编号来源")
        {
            Align = AntdUI.ColumnAlign.Center,
            Editable = true,
            Items = ProductNoSourceOptions
                .Select(source => new AntdUI.SelectItem(source) { Tag = source })
                .ToList()
        };
    }

    private static AntdUI.ColumnSwitch CreateProductProcessEnabledColumn()
    {
        return new AntdUI.ColumnSwitch(nameof(ProductProcessConfigTableRow.Enabled), "启用")
        {
            Align = AntdUI.ColumnAlign.Center,
            AutoCheck = true
        };
    }

    /// <summary>
    /// Adds the MES program-list filter option at runtime to keep Designer code stable.
    /// </summary>
    private void ConfigureMesProgramFilterOption()
    {
        chkUseProductNumberFilter.AutoSizeMode = AntdUI.TAutoSize.Width;
        chkUseProductNumberFilter.Checked = true;
        chkUseProductNumberFilter.Dock = DockStyle.Fill;
        chkUseProductNumberFilter.Margin = new Padding(6, 3, 3, 3);
        chkUseProductNumberFilter.Name = nameof(chkUseProductNumberFilter);
        chkUseProductNumberFilter.TabIndex = 10;
        chkUseProductNumberFilter.Text = _localizer.GetString(TextKeys.SystemSetting.LabelUseProductNumberFilter);

        tableLayoutPanel3.Controls.Add(chkUseProductNumberFilter, 3, 4);
    }

    private void LoadSettings()
    {
        _currentSettings = _settingsService.Get();
        BindSettings(_currentSettings);
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

        _selectedPlcType = NormalizePlcType(settings.PlcType);
        BindPlcTypeOptions();
    }

    /// <summary>
    /// 页面静态文本不依赖 Designer 资源切换，这里手动统一设置，便于后续继续扩展语言包。
    /// </summary>
    private void ApplyLocalizedTexts()
    {
        lblTitle.Text = _localizer.GetString(TextKeys.SystemSetting.Title);
        lblDescription.Text = _localizer.GetString(TextKeys.SystemSetting.Description);
        grpPlcConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupPlc);
        grpMasterConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupController);
        grpAppConfig.Text = _localizer.GetString(TextKeys.SystemSetting.GroupApplication);

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
        tabProductProcess.Text = "产品工艺配置";
        lblProductProcessTitle.Text = "产品工艺配置";
        lblProductProcessDescription.Text = "维护工位、产品型号、工序号、每件焊点数量和采集参数组。工位 0 表示所有工位共享配置。";
        btnAddProductProcess.Text = "新增";
        btnSaveProductProcesses.Text = "保存";
        btnDisableProductProcess.Text = "禁用选中";
        btnRefreshProductProcesses.Text = "刷新";
    }

    /// <summary>
    /// 下拉选项显示的是本地化文本，真正入库的则是稳定的字符串值。
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
    /// 从数据库加载全部产品工艺配置，包含已禁用项，方便现场恢复或排查历史配置。
    /// </summary>
    private void LoadProductProcessConfigs()
    {
        try
        {
            EndProductProcessEdit();
            _productProcessConfigs.Clear();
            _productProcessConfigs.AddRange(_productProcessConfigService.GetAll(includeDisabled: true));
            RefreshProductProcessRows();
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"产品工艺配置加载失败：{ex.Message}");
        }
    }

    private void RefreshProductProcessRows()
    {
        var selectedId = _selectedProductProcessRow?.Id;

        _productProcessRows = _productProcessConfigs
            .OrderBy(config => config.Sort)
            .ThenBy(config => config.ProductModel)
            .ThenBy(config => config.ProcessNo)
            .Select(config => new ProductProcessConfigTableRow(config))
            .ToList();

        tableProductProcesses.DataSource = _productProcessRows;
        tableProductProcesses.Refresh();
        SelectVisibleProductProcessRow(selectedId);
    }

    private void AddProductProcess_Click(object? sender, EventArgs e)
    {
        EndProductProcessEdit();

        var nextSort = _productProcessConfigs.Count == 0
            ? 10
            : _productProcessConfigs.Max(config => config.Sort) + 10;

        var config = new BizProductProcessConfig
        {
            ProductModel = "默认型号",
            StationNo = ProductionConstants.Stations.SharedStationNo,
            ProcessNo = "05",
            ProcessName = "默认工序",
            WeldPointCount = 1,
            CollectionGroup = "default",
            ProductNoSource = ProductionConstants.ProductNoSources.AutoIncrement,
            Enabled = true,
            Sort = nextSort,
            Description = "请按现场产品和工序修改"
        };

        _productProcessConfigs.Add(config);
        RefreshProductProcessRows();
        _selectedProductProcessRow = _productProcessRows.FirstOrDefault(row => ReferenceEquals(row.Source, config));
        SelectVisibleProductProcessRow(_selectedProductProcessRow?.Id);
    }

    private void SaveProductProcesses_Click(object? sender, EventArgs e)
    {
        EndProductProcessEdit();

        try
        {
            NormalizeProductProcessConfigs(_productProcessConfigs);
            ValidateProductProcessConfigs(_productProcessConfigs);

            foreach (var config in _productProcessConfigs.OrderBy(config => config.Sort))
            {
                _productProcessConfigService.Save(config);
            }

            _productProcessConfigs.Clear();
            _productProcessConfigs.AddRange(_productProcessConfigService.GetAll(includeDisabled: true));
            RefreshProductProcessRows();
            ShowInfoMessage("产品工艺配置已保存。");
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"产品工艺配置保存失败：{ex.Message}");
        }
    }

    private void DisableProductProcess_Click(object? sender, EventArgs e)
    {
        EndProductProcessEdit();

        var selectedConfig = _selectedProductProcessRow?.Source;
        if (selectedConfig is null)
        {
            ShowWarningMessage("请先选择一条产品工艺配置。");
            return;
        }

        try
        {
            if (selectedConfig.Id <= 0)
            {
                _productProcessConfigs.Remove(selectedConfig);
                RefreshProductProcessRows();
                return;
            }

            _productProcessConfigService.Disable(selectedConfig.Id);
            LoadProductProcessConfigs();
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"产品工艺配置禁用失败：{ex.Message}");
        }
    }

    private void TableProductProcesses_CellClick(object sender, AntdUI.TableClickEventArgs e)
    {
        if (e.Record is ProductProcessConfigTableRow row)
        {
            _selectedProductProcessRow = row;
        }
    }

    private bool TableProductProcesses_CellEndEdit(object sender, AntdUI.TableEndEditEventArgs e)
    {
        var value = e.Value?.Trim() ?? string.Empty;

        if (e.Record is not ProductProcessConfigTableRow)
        {
            return true;
        }

        return e.Column.Key switch
        {
            nameof(ProductProcessConfigTableRow.ProductModel) => !string.IsNullOrWhiteSpace(value),
            nameof(ProductProcessConfigTableRow.StationNo) => IsNonNegativeInt(value),
            nameof(ProductProcessConfigTableRow.ProcessNo) => !string.IsNullOrWhiteSpace(value),
            nameof(ProductProcessConfigTableRow.WeldPointCount) => IsPositiveInt(value),
            nameof(ProductProcessConfigTableRow.Sort) => IsNonNegativeInt(value),
            _ => true
        };
    }

    private bool TableProductProcesses_CellEndValueEdit(object sender, AntdUI.TableEndValueEditEventArgs e)
    {
        return e.Column.Key != nameof(ProductProcessConfigTableRow.ProductNoSource)
            || ProductNoSourceOptions.Contains(e.Value?.ToString());
    }

    private void TableProductProcesses_CellEditComplete(object sender, AntdUI.ITableEventArgs e)
    {
        if (e.Record is not ProductProcessConfigTableRow row)
        {
            return;
        }

        _selectedProductProcessRow = row;
        row.Normalize();
        tableProductProcesses.Refresh();
    }

    private void TableProductProcesses_CheckedChanged(object sender, AntdUI.TableCheckEventArgs e)
    {
        if (e.Record is not ProductProcessConfigTableRow row)
        {
            return;
        }

        _selectedProductProcessRow = row;
        row.Enabled = e.Value;
    }

    private void SelectVisibleProductProcessRow(int? selectedId)
    {
        _selectedProductProcessRow = selectedId is > 0
            ? _productProcessRows.FirstOrDefault(row => row.Id == selectedId)
            : _productProcessRows.FirstOrDefault(row => row.Id <= 0);

        _selectedProductProcessRow ??= _productProcessRows.FirstOrDefault();

        if (_selectedProductProcessRow is not null)
        {
            tableProductProcesses.SetSelected(_selectedProductProcessRow, true);
        }
    }

    private void EndProductProcessEdit()
    {
        tableProductProcesses.EditModeClose();
    }

    private static void NormalizeProductProcessConfigs(IEnumerable<BizProductProcessConfig> configs)
    {
        foreach (var config in configs)
        {
            config.ProductModel = NormalizeRequiredText(config.ProductModel);
            config.StationNo = Math.Max(ProductionConstants.Stations.SharedStationNo, config.StationNo);
            config.ProcessNo = NormalizeRequiredText(config.ProcessNo);
            config.ProcessName = NormalizeNullableText(config.ProcessName);
            config.WeldPointCount = Math.Max(1, config.WeldPointCount);
            config.CollectionGroup = string.IsNullOrWhiteSpace(config.CollectionGroup)
                ? "default"
                : config.CollectionGroup.Trim();
            config.ProgramMatchRule = NormalizeNullableText(config.ProgramMatchRule);
            config.ProductNoSource = NormalizeProductNoSource(config.ProductNoSource);
            config.Sort = Math.Max(0, config.Sort);
            config.Description = NormalizeNullableText(config.Description);
        }
    }

    private static void ValidateProductProcessConfigs(IEnumerable<BizProductProcessConfig> configs)
    {
        var enabledConfigs = configs
            .Where(config => config.Enabled)
            .ToList();

        var duplicate = enabledConfigs
            .GroupBy(
                config => $"{config.StationNo}\u001F{config.ProductModel}\u001F{config.ProcessNo}",
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            var first = duplicate.First();
            throw new InvalidOperationException($"工位“{first.StationNo}”、产品型号“{first.ProductModel}”与工序号“{first.ProcessNo}”存在重复启用配置。");
        }
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

            if (shouldSyncDevice)
            {
                if (await SyncDeviceToMesAsync(syncRequest, btnSaveAll, false))
                {
                    MarkDeviceSynced();
                }
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
    /// 手动把当前设备信息同步到 MES。适合现场只改设备编号或设备状态地址时立即提交。
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

    /// <summary>
    /// 调用 MES 设置设备编号接口。MesProvider 内部会把请求和响应写入 MES 交互日志。
    /// </summary>
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
    /// PLC/总控连通测试都走同一套 TCP 检测逻辑，减少重复代码。
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

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        targetInput.Text = dialog.SelectedPath;
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
        return true;
    }

    /// <summary>
    /// 生成 MES “设置设备编号”请求。
    /// OldDeviceId 为空表示新增；不为空表示按旧编号更新设备资料。
    /// </summary>
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

    /// <summary>
    /// 只有影响 MES 设备资料的字段变更时，保存全部才自动同步，减少不必要的接口调用和日志噪声。
    /// </summary>
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
        return value.EndsWith("/", StringComparison.Ordinal)
            ? value
            : $"{value}/";
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

    private static string NormalizeProductNoSource(string? productNoSource)
    {
        return ProductNoSourceOptions.Contains(productNoSource)
            ? productNoSource!
            : ProductionConstants.ProductNoSources.AutoIncrement;
    }

    private static string NormalizeRequiredText(string? value)
    {
        var normalizedValue = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            throw new InvalidOperationException("产品型号和工序号不能为空。");
        }

        return normalizedValue;
    }

    private static string? NormalizeNullableText(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue)
            ? null
            : normalizedValue;
    }

    private static bool IsPositiveInt(string value)
    {
        return int.TryParse(value, out var number) && number > 0;
    }

    private static bool IsNonNegativeInt(string value)
    {
        return int.TryParse(value, out var number) && number >= 0;
    }

    private void ShowInfo(string messageKey, params object[] args)
    {
        ShowInfoMessage(_localizer.GetString(messageKey, args));
    }

    private void ShowInfoMessage(string message)
    {
        MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleInfo),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ShowWarningMessage(string message)
    {
        MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleWarning),
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private void ShowWarning(string messageKey, params object[] args)
    {
        MessageBox.Show(
            this,
            _localizer.GetString(messageKey, args),
            _localizer.GetString(TextKeys.Common.TitleWarning),
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private void ShowError(string messageKey, params object[] args)
    {
        MessageBox.Show(
            this,
            _localizer.GetString(messageKey, args),
            _localizer.GetString(TextKeys.Common.TitleError),
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private void ShowErrorMessage(string message)
    {
        MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleError),
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    /// <summary>
    /// 产品工艺配置表格行。界面编辑的是包装属性，保存时仍回写到原始实体。
    /// </summary>
    private sealed class ProductProcessConfigTableRow(BizProductProcessConfig source)
    {
        public BizProductProcessConfig Source { get; } = source;

        public int Id => Source.Id;

        public string ProductModel
        {
            get => Source.ProductModel;
            set => Source.ProductModel = value.Trim();
        }

        public int StationNo
        {
            get => Source.StationNo;
            set => Source.StationNo = Math.Max(ProductionConstants.Stations.SharedStationNo, value);
        }

        public string ProcessNo
        {
            get => Source.ProcessNo;
            set => Source.ProcessNo = value.Trim();
        }

        public string? ProcessName
        {
            get => Source.ProcessName;
            set => Source.ProcessName = NormalizeNullableText(value);
        }

        public int WeldPointCount
        {
            get => Source.WeldPointCount;
            set => Source.WeldPointCount = Math.Max(1, value);
        }

        public string CollectionGroup
        {
            get => Source.CollectionGroup;
            set => Source.CollectionGroup = string.IsNullOrWhiteSpace(value) ? "default" : value.Trim();
        }

        public string? ProgramMatchRule
        {
            get => Source.ProgramMatchRule;
            set => Source.ProgramMatchRule = NormalizeNullableText(value);
        }

        public string ProductNoSource
        {
            get => Source.ProductNoSource;
            set => Source.ProductNoSource = NormalizeProductNoSource(value);
        }

        public bool Enabled
        {
            get => Source.Enabled;
            set => Source.Enabled = value;
        }

        public int Sort
        {
            get => Source.Sort;
            set => Source.Sort = Math.Max(0, value);
        }

        public string? Description
        {
            get => Source.Description;
            set => Source.Description = NormalizeNullableText(value);
        }

        public DateTime UpdatedTime => Source.UpdatedTime;

        public void Normalize()
        {
            Source.ProductModel = Source.ProductModel.Trim();
            Source.StationNo = Math.Max(ProductionConstants.Stations.SharedStationNo, Source.StationNo);
            Source.ProcessNo = Source.ProcessNo.Trim();
            Source.ProcessName = NormalizeNullableText(Source.ProcessName);
            Source.WeldPointCount = Math.Max(1, Source.WeldPointCount);
            Source.CollectionGroup = string.IsNullOrWhiteSpace(Source.CollectionGroup)
                ? "default"
                : Source.CollectionGroup.Trim();
            Source.ProgramMatchRule = NormalizeNullableText(Source.ProgramMatchRule);
            Source.ProductNoSource = NormalizeProductNoSource(Source.ProductNoSource);
            Source.Sort = Math.Max(0, Source.Sort);
            Source.Description = NormalizeNullableText(Source.Description);
        }
    }

    private sealed record PlcTypeOption(string Value, string TextKey);
}
