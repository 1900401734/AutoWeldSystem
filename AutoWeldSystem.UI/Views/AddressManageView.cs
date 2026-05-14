using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Views;

/// <summary>
/// PLC 地址维护页面。
/// 使用固定地址用途加表格内编辑，既方便现场配置，也避免业务代码依赖用户自定义名称。
/// </summary>
public partial class AddressManageView : BaseView
{
    private readonly IPlcAddressService _addressService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IPlcProductionMonitorService _plcProductionMonitorService;
    private readonly IPlcWorkIdMonitorService _plcWorkIdMonitorService;
    private readonly ILocalizationService _localizer;
    private readonly List<BizPlcAddress> _allAddresses = new();
    private List<PlcAddressTableRow> _currentRows = new();
    private PlcAddressTableRow? _selectedRow;
    private string _addressKeyword = string.Empty;
    private bool _initialized;

    public AddressManageView(
        IPlcAddressService addressService,
        IPlcCommunicationService plcCommunicationService,
        IPlcProductionMonitorService plcProductionMonitorService,
        IPlcWorkIdMonitorService plcWorkIdMonitorService,
        ILocalizationService localizer)
    {
        _addressService = addressService;
        _plcCommunicationService = plcCommunicationService;
        _plcProductionMonitorService = plcProductionMonitorService;
        _plcWorkIdMonitorService = plcWorkIdMonitorService;
        _localizer = localizer;

        InitializeComponent();
        ConfigureTable();
        WireEvents();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (_initialized)
        {
            return;
        }

        _initialized = true;
        ApplyLocalizedTexts();
        LoadAddresses();
    }

    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ConfigureTableColumns();
        ApplyAddressFilter(_addressKeyword);
        tableAddresses.Refresh();
    }

    /// <summary>
    /// 初始化 AntdUI 表格，只做一次控件级配置。
    /// </summary>
    private void ConfigureTable()
    {
        TableStyleHelper.ApplyAntdTable(tableAddresses);
        tableAddresses.EditLostFocus = true;
        tableAddresses.LostFocusClearSelection = false;

        ConfigureTableColumns();
    }

    /// <summary>
    /// 按当前语言重建列标题，避免语言切换后仍显示旧表头。
    /// </summary>
    private void ConfigureTableColumns()
    {
        tableAddresses.Columns.Clear();

        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.AddressName), TextKeys.Grid.PlcAddressName, readOnly: true));
        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.Sort), TextKeys.Grid.PlcAddressSort));
        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.Address), TextKeys.Grid.PlcAddress));
        tableAddresses.Columns.Add(CreateDataTypeColumn());
        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.DataLength), TextKeys.Grid.PlcAddressDataLength));
        tableAddresses.Columns.Add(CreateEnabledColumn());
        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.Description), TextKeys.Grid.PlcAddressDescription));
        tableAddresses.Columns.Add(CreateTableColumn(
            nameof(PlcAddressTableRow.UpdatedTime),
            TextKeys.Grid.PlcAddressUpdatedTime,
            readOnly: true,
            displayFormat: "yyyy-MM-dd HH:mm:ss"));
        TableStyleHelper.ApplyAntdColumnDefaults(tableAddresses);
    }

    /// <summary>
    /// 创建普通文本列，统一处理只读、编辑和显示格式。
    /// </summary>
    private AntdUI.Column CreateTableColumn(string key, string titleKey, bool readOnly = false, string? displayFormat = null)
    {
        return new AntdUI.Column(key, _localizer.GetString(titleKey))
        {
            Align = GetColumnAlign(key),
            ReadOnly = readOnly,
            Editable = !readOnly,
            Ellipsis = true,
            DisplayFormat = displayFormat
        };
    }

    /// <summary>
    /// 数据类型列使用下拉项，减少手工输入错误。
    /// </summary>
    private AntdUI.ColumnSelect CreateDataTypeColumn()
    {
        return new AntdUI.ColumnSelect(nameof(PlcAddressTableRow.DataType), _localizer.GetString(TextKeys.Grid.PlcAddressDataType))
        {
            Align = AntdUI.ColumnAlign.Center,
            Editable = true,
            Items = AppConstants.PlcDataTypes.All
                .Select(dataType => new AntdUI.SelectItem(dataType) { Tag = dataType })
                .ToList()
        };
    }

    /// <summary>
    /// 启用列使用开关控件，点击即可变更布尔值。
    /// </summary>
    private AntdUI.ColumnSwitch CreateEnabledColumn()
    {
        return new AntdUI.ColumnSwitch(nameof(PlcAddressTableRow.Enabled), _localizer.GetString(TextKeys.Grid.PlcAddressEnabled))
        {
            Align = AntdUI.ColumnAlign.Center,
            AutoCheck = true
        };
    }

    /// <summary>
    /// 数字、开关这类字段居中显示，现场人员扫表时更容易对齐查看。
    /// </summary>
    private static AntdUI.ColumnAlign GetColumnAlign(string key)
    {
        return key is nameof(PlcAddressTableRow.Sort)
                or nameof(PlcAddressTableRow.DataLength)
                or nameof(PlcAddressTableRow.UpdatedTime)
            ? AntdUI.ColumnAlign.Center
            : AntdUI.ColumnAlign.Left;
    }

    /// <summary>
    /// 统一绑定事件，避免构造函数里堆积细节。
    /// </summary>
    private void WireEvents()
    {
        btnSave.Click += Save_Click;
        btnRefresh.Click += (_, _) => LoadAddresses();
        btnTest.Click += TestSelected_Click;
        queryAddresses.QueryClick += (_, keyword) => ApplyAddressFilter(keyword);
        tableAddresses.CellClick += TableAddresses_CellClick;
        tableAddresses.CellEndEdit += TableAddresses_CellEndEdit;
        tableAddresses.CellEndValueEdit += TableAddresses_CellEndValueEdit;
        tableAddresses.CellEditComplete += TableAddresses_CellEditComplete;
        tableAddresses.CheckedChanged += TableAddresses_CheckedChanged;
    }

    private void TableAddresses_CellClick(object sender, AntdUI.TableClickEventArgs e)
    {
        if (e.Record is PlcAddressTableRow row)
        {
            _selectedRow = row;
        }
    }

    /// <summary>
    /// 文本编辑结束前先校验输入；返回 false 时 AntdUI.Table 会拒绝这次提交。
    /// </summary>
    private bool TableAddresses_CellEndEdit(object sender, AntdUI.TableEndEditEventArgs e)
    {
        if (e.Record is not PlcAddressTableRow)
        {
            return true;
        }

        var value = e.Value?.Trim() ?? string.Empty;

        return e.Column.Key switch
        {
            nameof(PlcAddressTableRow.Sort) => IsNonNegativeInt(value),
            nameof(PlcAddressTableRow.DataLength) => IsPositiveInt(value),
            _ => true
        };
    }

    /// <summary>
    /// 下拉编辑只接受系统支持的数据类型，避免保存未知类型后 PLC 读取逻辑无法判断。
    /// </summary>
    private bool TableAddresses_CellEndValueEdit(object sender, AntdUI.TableEndValueEditEventArgs e)
    {
        return e.Record is not PlcAddressTableRow
            || e.Column.Key != nameof(PlcAddressTableRow.DataType)
            || AppConstants.PlcDataTypes.All.Contains(e.Value?.ToString());
    }

    /// <summary>
    /// 开关列点击后同步当前选中行，并显式写回 Enabled，避免不同版本控件行为差异。
    /// </summary>
    private void TableAddresses_CheckedChanged(object sender, AntdUI.TableCheckEventArgs e)
    {
        if (e.Record is not PlcAddressTableRow row)
        {
            return;
        }

        _selectedRow = row;
        row.Enabled = e.Value;
    }

    /// <summary>
    /// 编辑完成后重新归一化当前行，并刷新显示，避免空长度、负排序等非法值留在界面上。
    /// </summary>
    private void TableAddresses_CellEditComplete(object sender, AntdUI.ITableEventArgs e)
    {
        if (e.Record is not PlcAddressTableRow row)
        {
            return;
        }

        _selectedRow = row;
        row.Normalize();
        tableAddresses.Refresh();
    }

    private void ApplyLocalizedTexts()
    {
        lblTitle.Text = _localizer.GetString(TextKeys.Address.Title);
        lblDescription.Text = _localizer.GetString(TextKeys.Address.Description);
        btnSave.Text = _localizer.GetString(TextKeys.Address.ButtonSave);
        btnRefresh.Text = _localizer.GetString(TextKeys.Address.ButtonRefresh);
        btnTest.Text = _localizer.GetString(TextKeys.Address.ButtonTest);
    }

    private void LoadAddresses()
    {
        try
        {
            _allAddresses.Clear();
            _allAddresses.AddRange(_addressService.GetAll());
            ApplyAddressFilter(_addressKeyword);
        }
        catch (Exception ex)
        {
            ShowError(_localizer.GetString(TextKeys.Address.MessageSaveFailed, ex.Message));
        }
    }

    /// <summary>
    /// 搜索只影响界面显示，不改变完整地址集合，避免保存时漏掉被筛选隐藏的地址。
    /// </summary>
    private void ApplyAddressFilter(string? keyword)
    {
        EndTableEdit();

        _addressKeyword = keyword?.Trim() ?? string.Empty;
        var selectedAddressKey = _selectedRow?.AddressKey;

        var filteredAddresses = _allAddresses
            .Where(address => string.IsNullOrWhiteSpace(_addressKeyword)
                || Contains(address.AddressKey, _addressKeyword)
                || Contains(GetAddressDisplayName(address), _addressKeyword)
                || Contains(address.Address, _addressKeyword)
                || Contains(address.DataType, _addressKeyword)
                || Contains(address.Description, _addressKeyword))
            .OrderBy(address => address.Sort)
            .ThenBy(address => address.AddressKey)
            .ToList();

        _currentRows = filteredAddresses
            .Select(address => new PlcAddressTableRow(address, GetAddressDisplayName(address)))
            .ToList();

        tableAddresses.DataSource = _currentRows;
        tableAddresses.Refresh();
        SelectVisibleRow(selectedAddressKey);
    }

    /// <summary>
    /// 保存后重启 PLC 通讯服务，让新的心跳地址立即生效。
    /// </summary>
    private async void Save_Click(object? sender, EventArgs e)
    {
        EndTableEdit();

        try
        {
            var addresses = GetCurrentAddresses();
            NormalizeAddresses(addresses);
            _addressService.SaveAll(addresses);
            await _plcProductionMonitorService.ReloadAddressesAsync();
            await _plcWorkIdMonitorService.ReloadAddressAsync();
            await _plcCommunicationService.RestartAsync();
            LoadAddresses();
            ShowInfo(_localizer.GetString(TextKeys.Address.MessageSaveSuccess));
        }
        catch (Exception ex)
        {
            ShowError(_localizer.GetString(TextKeys.Address.MessageSaveFailed, ex.Message));
        }
    }

    private async void TestSelected_Click(object? sender, EventArgs e)
    {
        EndTableEdit();

        var address = GetSelectedAddress();
        if (address is null)
        {
            ShowWarning(_localizer.GetString(TextKeys.Address.MessageSelectFirst));
            return;
        }

        if (string.IsNullOrWhiteSpace(address.Address))
        {
            ShowWarning(_localizer.GetString(TextKeys.Address.MessageAddressRequired, GetAddressDisplayName(address)));
            return;
        }

        var result = await ReadAddressAsync(address);
        if (result.IsSuccess)
        {
            ShowInfo(_localizer.GetString(TextKeys.Address.MessageTestSuccess, GetAddressDisplayName(address), result.Value ?? string.Empty));
            return;
        }

        ShowWarning(_localizer.GetString(TextKeys.Address.MessageTestFailed, GetAddressDisplayName(address), result.Message));
    }

    private async Task<PlcServiceResult<string>> ReadAddressAsync(BizPlcAddress address)
    {
        var plcAddress = address.Address?.Trim() ?? string.Empty;
        var dataType = NormalizeDataType(address.DataType);

        return dataType switch
        {
            AppConstants.PlcDataTypes.Bool => ToTextResult(await _plcCommunicationService.ReadBoolAsync(plcAddress)),
            AppConstants.PlcDataTypes.Int32 => ToTextResult(await _plcCommunicationService.ReadInt32Async(plcAddress)),
            AppConstants.PlcDataTypes.Float => ToTextResult(await _plcCommunicationService.ReadFloatAsync(plcAddress)),
            AppConstants.PlcDataTypes.String => ToTextResult(await _plcCommunicationService.ReadStringAsync(plcAddress, (ushort)Math.Max(1, address.DataLength))),
            _ => ToTextResult(await _plcCommunicationService.ReadInt16Async(plcAddress))
        };
    }

    private static PlcServiceResult<string> ToTextResult<T>(PlcServiceResult<T> result)
    {
        return result.IsSuccess
            ? PlcServiceResult<string>.Success(result.Value?.ToString() ?? string.Empty, result.Message)
            : PlcServiceResult<string>.Fail(result.Message);
    }

    private List<BizPlcAddress> GetCurrentAddresses()
    {
        return _allAddresses.ToList();
    }

    private BizPlcAddress? GetSelectedAddress()
    {
        return _selectedRow?.Source;
    }

    /// <summary>
    /// 关闭单元格编辑框，确保保存或筛选前把最新输入写回行对象。
    /// </summary>
    private void EndTableEdit()
    {
        tableAddresses.EditModeClose();
    }

    /// <summary>
    /// 筛选后尽量保留原选中行；原行不可见时默认选中第一行。
    /// </summary>
    private void SelectVisibleRow(string? selectedAddressKey)
    {
        _selectedRow = _currentRows.FirstOrDefault(row => row.AddressKey == selectedAddressKey)
            ?? _currentRows.FirstOrDefault();

        if (_selectedRow is not null)
        {
            tableAddresses.SetSelected(_selectedRow, true);
        }
    }

    private static void NormalizeAddresses(IEnumerable<BizPlcAddress> addresses)
    {
        foreach (var address in addresses)
        {
            address.Address = address.Address?.Trim();
            address.DataType = NormalizeDataType(address.DataType);
            address.DataLength = Math.Max(1, address.DataLength);
            address.Sort = Math.Max(0, address.Sort);
        }
    }

    private static string NormalizeDataType(string? dataType)
    {
        return AppConstants.PlcDataTypes.All.Contains(dataType)
            ? dataType!
            : AppConstants.PlcDataTypes.Int16;
    }

    private static int ParsePositiveInt(string value)
    {
        return int.TryParse(value, out var number)
            ? Math.Max(1, number)
            : 1;
    }

    private static int ParseNonNegativeInt(string value)
    {
        return int.TryParse(value, out var number)
            ? Math.Max(0, number)
            : 0;
    }

    private static bool IsPositiveInt(string value)
    {
        return int.TryParse(value, out var number) && number > 0;
    }

    private static bool IsNonNegativeInt(string value)
    {
        return int.TryParse(value, out var number) && number >= 0;
    }

    private string GetAddressDisplayName(BizPlcAddress address)
    {
        var key = address.AddressKey switch
        {
            AppConstants.PlcAddressKeys.PcHeartBeat => TextKeys.Address.NamePcHeartbeat,
            AppConstants.PlcAddressKeys.PlcHeartBeat => TextKeys.Address.NamePlcHeartbeat,
            AppConstants.PlcAddressKeys.DeviceStatus => TextKeys.Address.NameDeviceStatus,
            AppConstants.PlcAddressKeys.WeldStart => TextKeys.Address.NameWeldStart,
            AppConstants.PlcAddressKeys.WeldEnd => TextKeys.Address.NameWeldEnd,
            AppConstants.PlcAddressKeys.WorkId => TextKeys.Address.NameWorkId,
            AppConstants.PlcAddressKeys.LegacySerialNumber => TextKeys.Address.NameWorkId,
            AppConstants.PlcAddressKeys.ProgramName => TextKeys.Address.NameProgramName,
            AppConstants.PlcAddressKeys.ProductModel => TextKeys.Address.NameProductModel,
            AppConstants.PlcAddressKeys.TotalProduction => TextKeys.Address.NameTotalProduction,
            AppConstants.PlcAddressKeys.TargetProduction => TextKeys.Address.NameTargetProduction,
            AppConstants.PlcAddressKeys.AcceptedQuantity => TextKeys.Address.NameAcceptedQuantity,
            AppConstants.PlcAddressKeys.RejectedQuantity => TextKeys.Address.NameRejectedQuantity,
            _ => string.Empty
        };

        return string.IsNullOrWhiteSpace(key)
            ? address.AddressName
            : _localizer.GetString(key);
    }

    private static bool Contains(string? source, string keyword)
    {
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private void ShowInfo(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleInfo), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleWarning), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ShowError(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleError), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>
    /// AntdUI.Table 的数据行。显示字段走包装类，编辑字段直接写回原始 PLC 地址对象。
    /// </summary>
    private sealed class PlcAddressTableRow(BizPlcAddress source, string addressName)
    {
        public BizPlcAddress Source { get; } = source;

        public string AddressKey => Source.AddressKey;

        public string AddressName { get; } = addressName;

        public int Sort
        {
            get => Source.Sort;
            set => Source.Sort = Math.Max(0, value);
        }

        public string? Address
        {
            get => Source.Address;
            set => Source.Address = NormalizeNullableText(value);
        }

        public string DataType
        {
            get => Source.DataType;
            set => Source.DataType = NormalizeDataType(value);
        }

        public int DataLength
        {
            get => Source.DataLength;
            set => Source.DataLength = Math.Max(1, value);
        }

        public bool Enabled
        {
            get => Source.Enabled;
            set => Source.Enabled = value;
        }

        public string? Description
        {
            get => Source.Description;
            set => Source.Description = NormalizeNullableText(value);
        }

        public DateTime UpdatedTime => Source.UpdatedTime;

        /// <summary>
        /// 单行数据清理，供表格编辑完成后立即修正显示值。
        /// </summary>
        public void Normalize()
        {
            Source.Address = NormalizeNullableText(Source.Address);
            Source.DataType = NormalizeDataType(Source.DataType);
            Source.DataLength = Math.Max(1, Source.DataLength);
            Source.Sort = Math.Max(0, Source.Sort);
            Source.Description = NormalizeNullableText(Source.Description);
        }
    }

    private static string? NormalizeNullableText(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue)
            ? null
            : normalizedValue;
    }
}
