using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Views;

/// <summary>
/// PLC 地址维护页面。
/// 固定业务信号和测试项目地址分开维护，避免用户把业务触发信号与工艺采集参数混在一起。
/// </summary>
public partial class AddressManageView : BaseView
{
    private readonly IPlcAddressService _addressService;
    private readonly ITestItemTemplateService _testItemTemplateService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IPlcProductionMonitorService _plcProductionMonitorService;
    private readonly IPlcWorkIdMonitorService _plcWorkIdMonitorService;
    private readonly IPlcWeldCycleMonitorService _plcWeldCycleMonitorService;
    private readonly ILocalizationService _localizer;
    private readonly List<BizPlcAddress> _allAddresses = new();
    private readonly List<BizTestItemTemplate> _testItemTemplates = new();
    private readonly List<BizTestItemTemplateItem> _allTestItemAddresses = new();
    private List<PlcAddressTableRow> _currentRows = new();
    private List<TestItemAddressTableRow> _currentTestItemRows = new();
    private PlcAddressTableRow? _selectedRow;
    private TestItemAddressTableRow? _selectedTestItemRow;
    private string _addressKeyword = string.Empty;
    private string _testItemKeyword = string.Empty;
    private bool _initialized;

    public AddressManageView(
        IPlcAddressService addressService,
        ITestItemTemplateService testItemTemplateService,
        IPlcCommunicationService plcCommunicationService,
        IPlcProductionMonitorService plcProductionMonitorService,
        IPlcWorkIdMonitorService plcWorkIdMonitorService,
        IPlcWeldCycleMonitorService plcWeldCycleMonitorService,
        ILocalizationService localizer)
    {
        _addressService = addressService;
        _testItemTemplateService = testItemTemplateService;
        _plcCommunicationService = plcCommunicationService;
        _plcProductionMonitorService = plcProductionMonitorService;
        _plcWorkIdMonitorService = plcWorkIdMonitorService;
        _plcWeldCycleMonitorService = plcWeldCycleMonitorService;
        _localizer = localizer;

        InitializeComponent();
        ConfigureTables();
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
        LoadData();
    }

    protected override void OnLanguageChanged()
    {
        ApplyLocalizedTexts();
        ConfigureBusinessAddressColumns();
        ConfigureTestItemAddressColumns();
        ApplyAddressFilter(_addressKeyword);
        ApplyTestItemAddressFilter(_testItemKeyword);
    }

    /// <summary>
    /// 初始化 AntdUI 表格的通用视觉和编辑行为。
    /// </summary>
    private void ConfigureTables()
    {
        TableStyleHelper.ApplyAntdTable(tableAddresses);
        TableStyleHelper.ApplyAntdTable(tableTestItemAddresses);
        tableAddresses.EditLostFocus = true;
        tableAddresses.LostFocusClearSelection = false;
        tableTestItemAddresses.EditLostFocus = true;
        tableTestItemAddresses.LostFocusClearSelection = false;

        ConfigureBusinessAddressColumns();
        ConfigureTestItemAddressColumns();
    }

    private void ConfigureBusinessAddressColumns()
    {
        tableAddresses.Columns.Clear();
        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.AddressName), TextKeys.Grid.PlcAddressName, readOnly: true));
        tableAddresses.Columns.Add(CreateRawColumn(nameof(PlcAddressTableRow.StationNo), "工位(0共享)", readOnly: true));
        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.Sort), TextKeys.Grid.PlcAddressSort));
        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.Address), TextKeys.Grid.PlcAddress));
        tableAddresses.Columns.Add(CreateDataTypeColumn(nameof(PlcAddressTableRow.DataType), _localizer.GetString(TextKeys.Grid.PlcAddressDataType)));
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

    private void ConfigureTestItemAddressColumns()
    {
        tableTestItemAddresses.Columns.Clear();
        tableTestItemAddresses.Columns.Add(CreateTemplateColumn());
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.StationNo), "工位(0共享)"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.TouchNo), "焊点(0共享)"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.Sort), "排序"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.ItemKey), "测试项键"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.ItemName), "测试项目"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.ActualAddress), "实际值地址"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.UpperAddress), "上限地址"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.LowerAddress), "下限地址"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.ResultAddress), "结果地址"));
        tableTestItemAddresses.Columns.Add(CreateDataTypeColumn(nameof(TestItemAddressTableRow.ValueDataType), "数值类型"));
        tableTestItemAddresses.Columns.Add(CreateDataTypeColumn(nameof(TestItemAddressTableRow.ResultDataType), "结果类型"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.ValueDataLength), "数值长度"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.ResultDataLength), "结果长度"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.Scale), "缩放"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.Offset), "偏移"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.DecimalPlaces), "小数位"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.Unit), "单位"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.MesFieldPrefix), "MES字段"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.ReportColumnName), "报表列"));
        tableTestItemAddresses.Columns.Add(CreateTestItemRequiredColumn());
        tableTestItemAddresses.Columns.Add(CreateTestItemEnabledColumn());
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.Description), "备注"));
        tableTestItemAddresses.Columns.Add(CreateRawColumn(nameof(TestItemAddressTableRow.UpdatedTime), "更新时间", readOnly: true, displayFormat: "yyyy-MM-dd HH:mm:ss"));
        TableStyleHelper.ApplyAntdColumnDefaults(tableTestItemAddresses);
    }

    private AntdUI.Column CreateTableColumn(string key, string titleKey, bool readOnly = false, string? displayFormat = null)
    {
        return CreateRawColumn(key, _localizer.GetString(titleKey), readOnly, displayFormat);
    }

    private AntdUI.Column CreateRawColumn(string key, string title, bool readOnly = false, string? displayFormat = null)
    {
        return new AntdUI.Column(key, title)
        {
            Align = GetColumnAlign(key),
            ReadOnly = readOnly,
            Editable = !readOnly,
            Ellipsis = true,
            DisplayFormat = displayFormat
        };
    }

    private AntdUI.ColumnSelect CreateTemplateColumn()
    {
        return new AntdUI.ColumnSelect(nameof(TestItemAddressTableRow.TemplateId), "测试项目模板*")
        {
            Align = AntdUI.ColumnAlign.Center,
            Editable = true,
            Items = _testItemTemplates
                .OrderBy(template => template.Sort)
                .ThenBy(template => template.TemplateName)
                .Select(template => new AntdUI.SelectItem(GetTemplateDisplayName(template)) { Tag = template.Id })
                .ToList()
        };
    }

    private AntdUI.ColumnSelect CreateDataTypeColumn(string key, string title)
    {
        return new AntdUI.ColumnSelect(key, title)
        {
            Align = AntdUI.ColumnAlign.Center,
            Editable = true,
            Items = AppConstants.PlcDataTypes.All
                .Select(dataType => new AntdUI.SelectItem(dataType) { Tag = dataType })
                .ToList()
        };
    }

    private AntdUI.ColumnSwitch CreateEnabledColumn()
    {
        return new AntdUI.ColumnSwitch(nameof(PlcAddressTableRow.Enabled), _localizer.GetString(TextKeys.Grid.PlcAddressEnabled))
        {
            Align = AntdUI.ColumnAlign.Center,
            AutoCheck = true
        };
    }

    private static AntdUI.ColumnSwitch CreateTestItemRequiredColumn()
    {
        return new AntdUI.ColumnSwitch(nameof(TestItemAddressTableRow.Required), "必填")
        {
            Align = AntdUI.ColumnAlign.Center,
            AutoCheck = true
        };
    }

    private static AntdUI.ColumnSwitch CreateTestItemEnabledColumn()
    {
        return new AntdUI.ColumnSwitch(nameof(TestItemAddressTableRow.Enabled), "启用")
        {
            Align = AntdUI.ColumnAlign.Center,
            AutoCheck = true
        };
    }

    private static AntdUI.ColumnAlign GetColumnAlign(string key)
    {
        return key.Contains("Address", StringComparison.OrdinalIgnoreCase)
            || key is nameof(PlcAddressTableRow.Description)
                or nameof(TestItemAddressTableRow.Description)
                or nameof(TestItemAddressTableRow.ItemName)
                or nameof(TestItemAddressTableRow.ItemKey)
            ? AntdUI.ColumnAlign.Left
            : AntdUI.ColumnAlign.Center;
    }

    private void WireEvents()
    {
        btnSave.Click += Save_Click;
        btnRefresh.Click += (_, _) => LoadData();
        btnTest.Click += TestSelected_Click;
        btnAddTestItemAddress.Click += AddTestItemAddress_Click;
        btnDeleteTestItemAddress.Click += DeleteTestItemAddress_Click;
        queryAddresses.QueryClick += (_, keyword) => ApplyActiveFilter(keyword);
        tabAddressCategories.SelectedIndexChanged += (_, _) => SwitchActiveFilterText();

        tableAddresses.CellClick += TableAddresses_CellClick;
        tableAddresses.CellEndEdit += TableAddresses_CellEndEdit;
        tableAddresses.CellEndValueEdit += TableAddresses_CellEndValueEdit;
        tableAddresses.CellEditComplete += TableAddresses_CellEditComplete;
        tableAddresses.CheckedChanged += TableAddresses_CheckedChanged;

        tableTestItemAddresses.CellClick += TableTestItemAddresses_CellClick;
        tableTestItemAddresses.CellEndEdit += TableAddresses_CellEndEdit;
        tableTestItemAddresses.CellEndValueEdit += TableAddresses_CellEndValueEdit;
        tableTestItemAddresses.CellEditComplete += TableAddresses_CellEditComplete;
        tableTestItemAddresses.CheckedChanged += TableAddresses_CheckedChanged;
    }

    private void TableAddresses_CellClick(object sender, AntdUI.TableClickEventArgs e)
    {
        if (e.Record is PlcAddressTableRow row)
        {
            _selectedRow = row;
        }
    }

    private void TableTestItemAddresses_CellClick(object sender, AntdUI.TableClickEventArgs e)
    {
        if (e.Record is TestItemAddressTableRow row)
        {
            _selectedTestItemRow = row;
        }
    }

    private bool TableAddresses_CellEndEdit(object sender, AntdUI.TableEndEditEventArgs e)
    {
        var value = e.Value?.Trim() ?? string.Empty;

        if (e.Record is PlcAddressTableRow)
        {
            return e.Column.Key switch
            {
                nameof(PlcAddressTableRow.Sort) => IsNonNegativeInt(value),
                nameof(PlcAddressTableRow.StationNo) => IsNonNegativeInt(value),
                nameof(PlcAddressTableRow.DataLength) => IsPositiveInt(value),
                _ => true
            };
        }

        if (e.Record is TestItemAddressTableRow)
        {
            return e.Column.Key switch
            {
                nameof(TestItemAddressTableRow.TemplateId) => ResolveTemplateId(value) > 0,
                nameof(TestItemAddressTableRow.StationNo) => IsNonNegativeInt(value),
                nameof(TestItemAddressTableRow.TouchNo) => IsNonNegativeInt(value),
                nameof(TestItemAddressTableRow.Sort) => IsNonNegativeInt(value),
                nameof(TestItemAddressTableRow.ItemKey) => !string.IsNullOrWhiteSpace(value),
                nameof(TestItemAddressTableRow.ItemName) => !string.IsNullOrWhiteSpace(value),
                nameof(TestItemAddressTableRow.ValueDataLength) => IsPositiveInt(value),
                nameof(TestItemAddressTableRow.ResultDataLength) => IsPositiveInt(value),
                nameof(TestItemAddressTableRow.Scale) => IsDecimal(value),
                nameof(TestItemAddressTableRow.Offset) => IsDecimal(value),
                nameof(TestItemAddressTableRow.DecimalPlaces) => IsNonNegativeInt(value),
                _ => true
            };
        }

        return true;
    }

    private bool TableAddresses_CellEndValueEdit(object sender, AntdUI.TableEndValueEditEventArgs e)
    {
        var value = GetSelectValueText(e.Value);
        if (e.Record is TestItemAddressTableRow itemRow)
        {
            switch (e.Column.Key)
            {
                case nameof(TestItemAddressTableRow.TemplateId):
                    itemRow.TemplateId = ResolveTemplateId(value);
                    return itemRow.TemplateId > 0;

                case nameof(TestItemAddressTableRow.ValueDataType):
                    itemRow.ValueDataType = value;
                    return AppConstants.PlcDataTypes.All.Contains(value);

                case nameof(TestItemAddressTableRow.ResultDataType):
                    itemRow.ResultDataType = value;
                    return AppConstants.PlcDataTypes.All.Contains(value);
            }
        }

        if (e.Record is PlcAddressTableRow addressRow && e.Column.Key == nameof(PlcAddressTableRow.DataType))
        {
            addressRow.DataType = value;
            return AppConstants.PlcDataTypes.All.Contains(value);
        }

        return true;
    }

    private void TableAddresses_CheckedChanged(object sender, AntdUI.TableCheckEventArgs e)
    {
        if (e.Record is PlcAddressTableRow addressRow)
        {
            _selectedRow = addressRow;
            addressRow.Enabled = e.Value;
            return;
        }

        if (e.Record is TestItemAddressTableRow itemRow)
        {
            _selectedTestItemRow = itemRow;
            if (e.Column.Key == nameof(TestItemAddressTableRow.Required))
            {
                itemRow.Required = e.Value;
                return;
            }

            itemRow.Enabled = e.Value;
        }
    }

    private void TableAddresses_CellEditComplete(object sender, AntdUI.ITableEventArgs e)
    {
        if (e.Record is PlcAddressTableRow addressRow)
        {
            _selectedRow = addressRow;
            addressRow.Normalize();
            tableAddresses.Refresh();
            return;
        }

        if (e.Record is TestItemAddressTableRow itemRow)
        {
            _selectedTestItemRow = itemRow;
            itemRow.Normalize();
            tableTestItemAddresses.Refresh();
        }
    }

    private void ApplyLocalizedTexts()
    {
        lblTitle.Text = _localizer.GetString(TextKeys.Address.Title);
        lblDescription.Text = "维护 PLC 业务信号地址和各测试项目的采集地址。测试项目地址通过模板与产品工艺配置关联。";
        btnSave.Text = _localizer.GetString(TextKeys.Address.ButtonSave);
        btnRefresh.Text = _localizer.GetString(TextKeys.Address.ButtonRefresh);
        btnTest.Text = _localizer.GetString(TextKeys.Address.ButtonTest);
        btnAddTestItemAddress.Text = "新增";
        btnDeleteTestItemAddress.Text = "删除选中";
        lblTestItemAddressHint.Text = "测试项目地址来自测试项目模板；工位 0 表示所有工位共享，焊点 0 表示所有焊点共享。";
        tabBusinessAddresses.Text = "业务信号地址";
        tabTestItemAddresses.Text = "测试项目地址";
    }

    private void LoadData()
    {
        try
        {
            _allAddresses.Clear();
            _allAddresses.AddRange(_addressService.GetAll());

            _testItemTemplates.Clear();
            _testItemTemplates.AddRange(_testItemTemplateService.GetTemplates(includeDisabled: true));

            _allTestItemAddresses.Clear();
            foreach (var template in _testItemTemplates)
            {
                _allTestItemAddresses.AddRange(_testItemTemplateService.GetItems(template.Id, includeDisabled: true));
            }

            ConfigureTestItemAddressColumns();
            ApplyAddressFilter(_addressKeyword);
            ApplyTestItemAddressFilter(_testItemKeyword);
        }
        catch (Exception ex)
        {
            ShowError(_localizer.GetString(TextKeys.Address.MessageSaveFailed, ex.Message));
        }
    }

    private void ApplyActiveFilter(string? keyword)
    {
        if (tabAddressCategories.SelectedTab == tabTestItemAddresses)
        {
            ApplyTestItemAddressFilter(keyword);
            return;
        }

        ApplyAddressFilter(keyword);
    }

    private void SwitchActiveFilterText()
    {
        queryAddresses.Text = tabAddressCategories.SelectedTab == tabTestItemAddresses
            ? _testItemKeyword
            : _addressKeyword;
    }

    private void ApplyAddressFilter(string? keyword)
    {
        EndTableEdit();

        _addressKeyword = keyword?.Trim() ?? string.Empty;
        var selectedAddressKey = _selectedRow?.AddressKey;

        var filteredAddresses = _allAddresses
            .Where(address => string.IsNullOrWhiteSpace(_addressKeyword)
                || Contains(address.AddressKey, _addressKeyword)
                || Contains(address.LogicalKey, _addressKeyword)
                || Contains(address.StationNo.ToString(), _addressKeyword)
                || Contains(GetAddressDisplayName(address), _addressKeyword)
                || Contains(address.Address, _addressKeyword)
                || Contains(address.DataType, _addressKeyword)
                || Contains(address.Description, _addressKeyword))
            .OrderBy(address => address.Sort)
            .ThenBy(address => address.StationNo)
            .ThenBy(address => address.AddressKey)
            .ToList();

        _currentRows = filteredAddresses
            .Select(address => new PlcAddressTableRow(address, GetAddressDisplayName(address)))
            .ToList();

        tableAddresses.DataSource = _currentRows;
        tableAddresses.Refresh();
        SelectVisibleRow(selectedAddressKey);
    }

    private void ApplyTestItemAddressFilter(string? keyword)
    {
        EndTableEdit();

        _testItemKeyword = keyword?.Trim() ?? string.Empty;
        var selectedItemId = _selectedTestItemRow?.Id;

        var filteredItems = _allTestItemAddresses
            .Where(item => string.IsNullOrWhiteSpace(_testItemKeyword)
                || Contains(GetTemplateDisplayName(item.TemplateId), _testItemKeyword)
                || Contains(item.StationNo.ToString(), _testItemKeyword)
                || Contains(item.TouchNo.ToString(), _testItemKeyword)
                || Contains(item.ItemKey, _testItemKeyword)
                || Contains(item.ItemName, _testItemKeyword)
                || Contains(item.ActualAddress, _testItemKeyword)
                || Contains(item.UpperAddress, _testItemKeyword)
                || Contains(item.LowerAddress, _testItemKeyword)
                || Contains(item.ResultAddress, _testItemKeyword)
                || Contains(item.ValueDataType, _testItemKeyword)
                || Contains(item.ResultDataType, _testItemKeyword)
                || Contains(item.Unit, _testItemKeyword)
                || Contains(item.MesFieldPrefix, _testItemKeyword)
                || Contains(item.ReportColumnName, _testItemKeyword)
                || Contains(item.Description, _testItemKeyword))
            .OrderBy(item => GetTemplateSort(item.TemplateId))
            .ThenBy(item => item.TemplateId)
            .ThenBy(item => item.StationNo)
            .ThenBy(item => item.TouchNo)
            .ThenBy(item => item.Sort)
            .ThenBy(item => item.ItemKey)
            .ToList();

        _currentTestItemRows = filteredItems
            .Select(item => new TestItemAddressTableRow(item, _testItemTemplates))
            .ToList();

        tableTestItemAddresses.DataSource = _currentTestItemRows;
        tableTestItemAddresses.Refresh();
        SelectVisibleTestItemRow(selectedItemId);
    }

    private void AddTestItemAddress_Click(object? sender, EventArgs e)
    {
        EndTableEdit();

        var template = GetDefaultTemplate();
        if (template is null)
        {
            ShowWarning("请先在系统设置中新增测试项目模板。");
            return;
        }

        var stationNo = _selectedTestItemRow?.StationNo ?? ProductionConstants.Stations.SharedStationNo;
        var touchNo = _selectedTestItemRow?.TouchNo ?? 0;
        var sort = _allTestItemAddresses
            .Where(item => item.TemplateId == template.Id && item.StationNo == stationNo && item.TouchNo == touchNo)
            .Select(item => item.Sort)
            .DefaultIfEmpty(0)
            .Max() + 10;

        var item = new BizTestItemTemplateItem
        {
            TemplateId = template.Id,
            StationNo = stationNo,
            TouchNo = touchNo,
            ItemKey = BuildNewItemKey(template.Id, stationNo, touchNo),
            ItemName = "新测试项目",
            ValueDataType = AppConstants.PlcDataTypes.Float,
            ResultDataType = AppConstants.PlcDataTypes.Int16,
            ValueDataLength = 1,
            ResultDataLength = 1,
            Scale = 1m,
            DecimalPlaces = 2,
            Required = false,
            Enabled = true,
            Sort = sort,
            Description = "用户新增测试项目，请配置 PLC 地址和字段映射。",
            UpdatedTime = DateTime.Now
        };

        _allTestItemAddresses.Add(item);
        _testItemKeyword = string.Empty;
        queryAddresses.Text = string.Empty;
        ApplyTestItemAddressFilter(_testItemKeyword);
        _selectedTestItemRow = _currentTestItemRows.FirstOrDefault(row => ReferenceEquals(row.Source, item));
        SelectVisibleTestItemRow(_selectedTestItemRow?.Id);
    }

    private void DeleteTestItemAddress_Click(object? sender, EventArgs e)
    {
        EndTableEdit();

        var item = _selectedTestItemRow?.Source;
        if (item is null)
        {
            ShowWarning(_localizer.GetString(TextKeys.Address.MessageSelectFirst));
            return;
        }

        var result = MessageBox.Show(
            this,
            $"确认删除测试项目“{item.ItemName}”吗？",
            _localizer.GetString(TextKeys.Common.TitleWarning),
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning);
        if (result != DialogResult.OK)
        {
            return;
        }

        try
        {
            if (item.Id > 0)
            {
                _testItemTemplateService.DeleteItem(item.Id);
            }

            _allTestItemAddresses.Remove(item);
            _selectedTestItemRow = null;
            ApplyTestItemAddressFilter(_testItemKeyword);
        }
        catch (Exception ex)
        {
            ShowError($"删除测试项目失败：{ex.Message}");
        }
    }

    private async void Save_Click(object? sender, EventArgs e)
    {
        EndTableEdit();

        try
        {
            var addresses = _allAddresses.ToList();
            var testItems = _allTestItemAddresses.ToList();
            NormalizeAddresses(addresses);
            NormalizeTestItemAddresses(testItems);
            ValidateTestItemAddresses(testItems);
            _addressService.SaveAll(addresses);
            _testItemTemplateService.SaveItems(testItems);
            await _plcProductionMonitorService.ReloadAddressesAsync();
            await _plcWorkIdMonitorService.ReloadAddressAsync();
            await _plcWeldCycleMonitorService.ReloadAddressesAsync();
            await _plcCommunicationService.RestartAsync();
            LoadData();
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

        if (tabAddressCategories.SelectedTab == tabTestItemAddresses)
        {
            await TestSelectedTestItemAsync();
            return;
        }

        var address = _selectedRow?.Source;
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

        var result = await ReadAddressAsync(address.Address, address.DataType, address.DataLength);
        if (result.IsSuccess)
        {
            ShowInfo(_localizer.GetString(TextKeys.Address.MessageTestSuccess, GetAddressDisplayName(address), result.Value ?? string.Empty));
            return;
        }

        ShowWarning(_localizer.GetString(TextKeys.Address.MessageTestFailed, GetAddressDisplayName(address), result.Message));
    }

    private async Task TestSelectedTestItemAsync()
    {
        var item = _selectedTestItemRow?.Source;
        if (item is null)
        {
            ShowWarning(_localizer.GetString(TextKeys.Address.MessageSelectFirst));
            return;
        }

        if (string.IsNullOrWhiteSpace(item.ActualAddress))
        {
            ShowWarning($"请先填写 {item.ItemName} 的实际值地址。");
            return;
        }

        var result = await ReadAddressAsync(item.ActualAddress, item.ValueDataType, item.ValueDataLength);
        if (result.IsSuccess)
        {
            ShowInfo($"{item.ItemName} 实际值读取成功：{result.Value}");
            return;
        }

        ShowWarning($"{item.ItemName} 实际值读取失败：{result.Message}");
    }

    private async Task<PlcServiceResult<string>> ReadAddressAsync(string? address, string? dataTypeValue, int dataLength)
    {
        var plcAddress = address?.Trim() ?? string.Empty;
        var dataType = NormalizeDataType(dataTypeValue);

        return dataType switch
        {
            AppConstants.PlcDataTypes.Bool => ToTextResult(await _plcCommunicationService.ReadBoolAsync(plcAddress)),
            AppConstants.PlcDataTypes.Int32 => ToTextResult(await _plcCommunicationService.ReadInt32Async(plcAddress)),
            AppConstants.PlcDataTypes.Float => ToTextResult(await _plcCommunicationService.ReadFloatAsync(plcAddress)),
            AppConstants.PlcDataTypes.String => ToTextResult(await _plcCommunicationService.ReadStringAsync(plcAddress, (ushort)Math.Max(1, dataLength))),
            _ => ToTextResult(await _plcCommunicationService.ReadInt16Async(plcAddress))
        };
    }

    private static PlcServiceResult<string> ToTextResult<T>(PlcServiceResult<T> result)
    {
        return result.IsSuccess
            ? PlcServiceResult<string>.Success(result.Value?.ToString() ?? string.Empty, result.Message)
            : PlcServiceResult<string>.Fail(result.Message);
    }

    private void EndTableEdit()
    {
        tableAddresses.EditModeClose();
        tableTestItemAddresses.EditModeClose();
    }

    private void SelectVisibleRow(string? selectedAddressKey)
    {
        _selectedRow = _currentRows.FirstOrDefault(row => row.AddressKey == selectedAddressKey)
            ?? _currentRows.FirstOrDefault();

        if (_selectedRow is not null)
        {
            tableAddresses.SetSelected(_selectedRow, true);
        }
    }

    private void SelectVisibleTestItemRow(int? selectedItemId)
    {
        _selectedTestItemRow = _currentTestItemRows.FirstOrDefault(row => row.Id == selectedItemId)
            ?? _currentTestItemRows.FirstOrDefault();

        if (_selectedTestItemRow is not null)
        {
            tableTestItemAddresses.SetSelected(_selectedTestItemRow, true);
        }
    }

    private static void NormalizeAddresses(IEnumerable<BizPlcAddress> addresses)
    {
        foreach (var address in addresses)
        {
            address.Address = address.Address?.Trim();
            address.LogicalKey = string.IsNullOrWhiteSpace(address.LogicalKey) ? address.AddressKey : address.LogicalKey.Trim();
            address.StationNo = Math.Max(0, address.StationNo);
            address.DataType = NormalizeDataType(address.DataType);
            address.DataLength = Math.Max(1, address.DataLength);
            address.Sort = Math.Max(0, address.Sort);
        }
    }

    private static void NormalizeTestItemAddresses(IEnumerable<BizTestItemTemplateItem> items)
    {
        foreach (var item in items)
        {
            item.TemplateId = Math.Max(0, item.TemplateId);
            item.StationNo = Math.Max(ProductionConstants.Stations.SharedStationNo, item.StationNo);
            item.TouchNo = Math.Max(0, item.TouchNo);
            item.ItemKey = NormalizeRequiredText(item.ItemKey);
            item.ItemName = NormalizeRequiredText(item.ItemName);
            item.ActualAddress = NormalizeNullableText(item.ActualAddress);
            item.UpperAddress = NormalizeNullableText(item.UpperAddress);
            item.LowerAddress = NormalizeNullableText(item.LowerAddress);
            item.ResultAddress = NormalizeNullableText(item.ResultAddress);
            item.ValueDataType = NormalizeDataType(item.ValueDataType);
            item.ResultDataType = NormalizeDataType(item.ResultDataType);
            item.ValueDataLength = Math.Max(1, item.ValueDataLength);
            item.ResultDataLength = Math.Max(1, item.ResultDataLength);
            item.Scale = item.Scale == 0 ? 1m : item.Scale;
            item.DecimalPlaces = Math.Clamp(item.DecimalPlaces, 0, 6);
            item.Unit = NormalizeNullableText(item.Unit);
            item.MesFieldPrefix = NormalizeNullableText(item.MesFieldPrefix);
            item.ReportColumnName = NormalizeNullableText(item.ReportColumnName);
            item.Sort = Math.Max(0, item.Sort);
            item.Description = NormalizeNullableText(item.Description);
        }
    }

    private static void ValidateTestItemAddresses(IEnumerable<BizTestItemTemplateItem> items)
    {
        var enabledItems = items.Where(item => item.Enabled).ToList();
        foreach (var item in enabledItems)
        {
            if (item.TemplateId <= 0)
            {
                throw new InvalidOperationException($"测试项目“{item.ItemName}”尚未绑定模板。");
            }

            if (item.Required && string.IsNullOrWhiteSpace(item.ActualAddress))
            {
                throw new InvalidOperationException($"必采测试项目“{item.ItemName}”尚未配置实际值地址。");
            }
        }

        var duplicate = enabledItems
            .GroupBy(
                item => $"{item.TemplateId}\u001F{item.StationNo}\u001F{item.TouchNo}\u001F{item.ItemKey}",
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            var first = duplicate.First();
            throw new InvalidOperationException($"模板“{first.TemplateId}”中工位“{first.StationNo}”、焊点“{first.TouchNo}”存在重复测试项键“{first.ItemKey}”。");
        }
    }

    private int ResolveTemplateId(string value)
    {
        if (int.TryParse(value, out var templateId)
            && _testItemTemplates.Any(template => template.Id == templateId))
        {
            return templateId;
        }

        var template = _testItemTemplates.FirstOrDefault(item =>
            string.Equals(GetTemplateDisplayName(item), value, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.TemplateCode, value, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.TemplateName, value, StringComparison.OrdinalIgnoreCase));

        return template?.Id ?? 0;
    }

    private BizTestItemTemplate? GetDefaultTemplate()
    {
        return _selectedTestItemRow is null
            ? _testItemTemplates.OrderBy(template => template.Sort).ThenBy(template => template.TemplateCode).FirstOrDefault()
            : _testItemTemplates.FirstOrDefault(template => template.Id == _selectedTestItemRow.TemplateId)
                ?? _testItemTemplates.OrderBy(template => template.Sort).ThenBy(template => template.TemplateCode).FirstOrDefault();
    }

    private string GetTemplateDisplayName(int templateId)
    {
        var template = _testItemTemplates.FirstOrDefault(item => item.Id == templateId);
        return template is null ? string.Empty : GetTemplateDisplayName(template);
    }

    private static string GetTemplateDisplayName(BizTestItemTemplate template)
    {
        return string.IsNullOrWhiteSpace(template.TemplateCode)
            ? template.TemplateName
            : $"{template.TemplateName} ({template.TemplateCode})";
    }

    private int GetTemplateSort(int templateId)
    {
        return _testItemTemplates.FirstOrDefault(template => template.Id == templateId)?.Sort ?? int.MaxValue;
    }

    private string BuildNewItemKey(int templateId, int stationNo, int touchNo)
    {
        var index = 1;
        string key;
        do
        {
            key = $"custom_{stationNo}_{touchNo}_{index}";
            index++;
        }
        while (_allTestItemAddresses.Any(item =>
            item.TemplateId == templateId
            && item.StationNo == stationNo
            && item.TouchNo == touchNo
            && string.Equals(item.ItemKey, key, StringComparison.OrdinalIgnoreCase)));

        return key;
    }

    private static string GetSelectValueText(object? value)
    {
        return value switch
        {
            null => string.Empty,
            AntdUI.SelectItem item => (item.Tag?.ToString() ?? item.Text ?? string.Empty).Trim(),
            _ => value.ToString()?.Trim() ?? string.Empty
        };
    }

    private string GetAddressDisplayName(BizPlcAddress address)
    {
        var logicalKey = string.IsNullOrWhiteSpace(address.LogicalKey)
            ? address.AddressKey
            : address.LogicalKey;
        var key = logicalKey switch
        {
            AppConstants.PlcAddressKeys.PcHeartBeat => TextKeys.Address.NamePcHeartbeat,
            AppConstants.PlcAddressKeys.PlcHeartBeat => TextKeys.Address.NamePlcHeartbeat,
            AppConstants.PlcAddressKeys.DeviceStatus => TextKeys.Address.NameDeviceStatus,
            AppConstants.PlcAddressKeys.WeldStart => TextKeys.Address.NameWeldStart,
            AppConstants.PlcAddressKeys.WeldEnd => TextKeys.Address.NameWeldEnd,
            AppConstants.PlcAddressKeys.WeldCollectionAck => TextKeys.Address.NameWeldCollectionAck,
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

    private static string NormalizeDataType(string? dataType)
    {
        return AppConstants.PlcDataTypes.All.Contains(dataType)
            ? dataType!
            : AppConstants.PlcDataTypes.Int16;
    }

    private static string NormalizeRequiredText(string? value)
    {
        var normalizedValue = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            throw new InvalidOperationException("必填字段不能为空。");
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

    private static bool Contains(string? source, string keyword)
    {
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPositiveInt(string value)
    {
        return int.TryParse(value, out var number) && number > 0;
    }

    private static bool IsNonNegativeInt(string value)
    {
        return int.TryParse(value, out var number) && number >= 0;
    }

    private static bool IsDecimal(string value)
    {
        return decimal.TryParse(value, out _);
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
    /// 业务信号地址表格行。表格编辑的是包装属性，保存时仍回写到原始地址实体。
    /// </summary>
    private sealed class PlcAddressTableRow(BizPlcAddress source, string addressName)
    {
        public BizPlcAddress Source { get; } = source;

        public string AddressKey => Source.AddressKey;

        public string AddressName { get; } = addressName;

        public int StationNo => Source.StationNo;

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

        public void Normalize()
        {
            Source.LogicalKey = string.IsNullOrWhiteSpace(Source.LogicalKey) ? Source.AddressKey : Source.LogicalKey.Trim();
            Source.StationNo = Math.Max(0, Source.StationNo);
            Source.Address = NormalizeNullableText(Source.Address);
            Source.DataType = NormalizeDataType(Source.DataType);
            Source.DataLength = Math.Max(1, Source.DataLength);
            Source.Sort = Math.Max(0, Source.Sort);
            Source.Description = NormalizeNullableText(Source.Description);
        }
    }

    /// <summary>
    /// 测试项目地址表格行。一个测试项包含实际值、上下限和结果四类 PLC 地址。
    /// </summary>
    private sealed class TestItemAddressTableRow(BizTestItemTemplateItem source, IReadOnlyList<BizTestItemTemplate> templates)
    {
        public BizTestItemTemplateItem Source { get; } = source;

        private readonly IReadOnlyList<BizTestItemTemplate> _templates = templates;

        public int Id => Source.Id;

        public int TemplateId
        {
            get => Source.TemplateId;
            set => Source.TemplateId = Math.Max(0, value);
        }

        public string TemplateName
        {
            get
            {
                var template = _templates.FirstOrDefault(item => item.Id == Source.TemplateId);
                return template is null ? string.Empty : GetTemplateDisplayName(template);
            }
            set
            {
                var normalized = value.Trim();
                var template = _templates.FirstOrDefault(item =>
                    string.Equals(GetTemplateDisplayName(item), normalized, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.TemplateCode, normalized, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.TemplateName, normalized, StringComparison.OrdinalIgnoreCase));
                Source.TemplateId = template?.Id ?? Source.TemplateId;
            }
        }

        public int StationNo
        {
            get => Source.StationNo;
            set => Source.StationNo = Math.Max(ProductionConstants.Stations.SharedStationNo, value);
        }

        public int TouchNo
        {
            get => Source.TouchNo;
            set => Source.TouchNo = Math.Max(0, value);
        }

        public int Sort
        {
            get => Source.Sort;
            set => Source.Sort = Math.Max(0, value);
        }

        public string ItemKey
        {
            get => Source.ItemKey;
            set => Source.ItemKey = value.Trim();
        }

        public string ItemName
        {
            get => Source.ItemName;
            set => Source.ItemName = value.Trim();
        }

        public string? ActualAddress
        {
            get => Source.ActualAddress;
            set => Source.ActualAddress = NormalizeNullableText(value);
        }

        public string? UpperAddress
        {
            get => Source.UpperAddress;
            set => Source.UpperAddress = NormalizeNullableText(value);
        }

        public string? LowerAddress
        {
            get => Source.LowerAddress;
            set => Source.LowerAddress = NormalizeNullableText(value);
        }

        public string? ResultAddress
        {
            get => Source.ResultAddress;
            set => Source.ResultAddress = NormalizeNullableText(value);
        }

        public string ValueDataType
        {
            get => Source.ValueDataType;
            set => Source.ValueDataType = NormalizeDataType(value);
        }

        public string ResultDataType
        {
            get => Source.ResultDataType;
            set => Source.ResultDataType = NormalizeDataType(value);
        }

        public int ValueDataLength
        {
            get => Source.ValueDataLength;
            set => Source.ValueDataLength = Math.Max(1, value);
        }

        public int ResultDataLength
        {
            get => Source.ResultDataLength;
            set => Source.ResultDataLength = Math.Max(1, value);
        }

        public decimal Scale
        {
            get => Source.Scale;
            set => Source.Scale = value == 0 ? 1m : value;
        }

        public decimal Offset
        {
            get => Source.Offset;
            set => Source.Offset = value;
        }

        public int DecimalPlaces
        {
            get => Source.DecimalPlaces;
            set => Source.DecimalPlaces = Math.Clamp(value, 0, 6);
        }

        public string? Unit
        {
            get => Source.Unit;
            set => Source.Unit = NormalizeNullableText(value);
        }

        public string? MesFieldPrefix
        {
            get => Source.MesFieldPrefix;
            set => Source.MesFieldPrefix = NormalizeNullableText(value);
        }

        public string? ReportColumnName
        {
            get => Source.ReportColumnName;
            set => Source.ReportColumnName = NormalizeNullableText(value);
        }

        public bool Required
        {
            get => Source.Required;
            set => Source.Required = value;
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

        public void Normalize()
        {
            Source.TemplateId = Math.Max(0, Source.TemplateId);
            Source.StationNo = Math.Max(ProductionConstants.Stations.SharedStationNo, Source.StationNo);
            Source.TouchNo = Math.Max(0, Source.TouchNo);
            Source.Sort = Math.Max(0, Source.Sort);
            Source.ItemKey = Source.ItemKey?.Trim() ?? string.Empty;
            Source.ItemName = Source.ItemName?.Trim() ?? string.Empty;
            Source.ActualAddress = NormalizeNullableText(Source.ActualAddress);
            Source.UpperAddress = NormalizeNullableText(Source.UpperAddress);
            Source.LowerAddress = NormalizeNullableText(Source.LowerAddress);
            Source.ResultAddress = NormalizeNullableText(Source.ResultAddress);
            Source.ValueDataType = NormalizeDataType(Source.ValueDataType);
            Source.ResultDataType = NormalizeDataType(Source.ResultDataType);
            Source.ValueDataLength = Math.Max(1, Source.ValueDataLength);
            Source.ResultDataLength = Math.Max(1, Source.ResultDataLength);
            Source.Scale = Source.Scale == 0 ? 1m : Source.Scale;
            Source.DecimalPlaces = Math.Clamp(Source.DecimalPlaces, 0, 6);
            Source.Unit = NormalizeNullableText(Source.Unit);
            Source.MesFieldPrefix = NormalizeNullableText(Source.MesFieldPrefix);
            Source.ReportColumnName = NormalizeNullableText(Source.ReportColumnName);
            Source.Description = NormalizeNullableText(Source.Description);
        }
    }
}
