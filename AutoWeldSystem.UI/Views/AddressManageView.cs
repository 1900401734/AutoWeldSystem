using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.Plc;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Forms;
using AutoWeldSystem.UI.Infrastructure;
using System.Globalization;
using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.UI.Views;

/// <summary>
/// PLC 地址与工艺配置维护页面。
/// 页面统一维护业务信号地址，以及产品工艺、测试方案、方案明细和测试项字典这四类采集配置。
/// </summary>
public partial class AddressManageView : BaseView
{
    // 三次点击用于调试测试地址，间隔需要比系统双击时间更宽松，避免第三次点击过慢导致计数被重置。
    private const int TestAddressTripleClickIntervalMs = 1200;

    private readonly IPlcAddressService _addressService;
    private readonly IProductProcessConfigService _productProcessConfigService;
    private readonly ITestSchemeConfigService _testSchemeConfigService;
    private readonly IProgramManageService _programManageService;
    private readonly IPlcCommunicationService _plcCommunicationService;
    private readonly IPlcExpressionReadService _plcExpressionReadService;
    private readonly IPlcProductionMonitorService _plcProductionMonitorService;
    private readonly IPlcWorkIdMonitorService _plcWorkIdMonitorService;
    private readonly IPlcWeldCycleMonitorService _plcWeldCycleMonitorService;
    private readonly ILocalizationService _localizer;
    private readonly IProgramExceptionLogService _exceptionLogService;

    private readonly List<BizPlcAddress> _allAddresses = new();
    private readonly List<BizProductProcessConfig> _productProcessConfigs = new();
    private readonly List<BizTestScheme> _testSchemes = new();
    private readonly List<BizSchemeDetail> _schemeDetails = new();
    private readonly List<DimTestItem> _testItems = new();
    private readonly List<BizProgram> _programOptions = new();
    private readonly Dictionary<DimTestItem, int> _temporaryTestItemIds = new();

    private List<PlcAddressTableRow> _currentRows = new();
    private List<ProductProcessTableRow> _currentProductProcessRows = new();
    private List<TestSchemeTableRow> _currentSchemeRows = new();
    private List<TestItemTableRow> _currentItemRows = new();
    private List<SchemeDetailRoleTableRow> _currentSchemeDetailRoleRows = new();

    private readonly DataGridView _schemeDetailRoleGrid = new();

    private PlcAddressTableRow? _selectedRow;
    private ProductProcessTableRow? _selectedProductProcessRow;
    private TestSchemeTableRow? _selectedSchemeRow;
    private TestItemTableRow? _selectedItemRow;
    private PlcAddressTableRow? _lastBusinessAddressClickRow;

    private string _addressKeyword = string.Empty;
    private string _productProcessKeyword = string.Empty;
    private string _schemeKeyword = string.Empty;
    private string _detailKeyword = string.Empty;
    private string _itemKeyword = string.Empty;
    private long _lastBusinessAddressClickTicks;
    private int _businessAddressClickCount;
    private string _currentSchemeDetailSchemeId = string.Empty;
    private bool _saving;
    private bool _initialized;
    private bool _handlingSchemeDetailTreeCheck;
    private bool _syncingSchemeDetailSchemeSelection;

    public AddressManageView(
        IPlcAddressService addressService,
        IProductProcessConfigService productProcessConfigService,
        ITestSchemeConfigService testSchemeConfigService,
        IProgramManageService programManageService,
        IPlcCommunicationService plcCommunicationService,
        IPlcExpressionReadService plcExpressionReadService,
        IPlcProductionMonitorService plcProductionMonitorService,
        IPlcWorkIdMonitorService plcWorkIdMonitorService,
        IPlcWeldCycleMonitorService plcWeldCycleMonitorService,
        ILocalizationService localizer,
        IProgramExceptionLogService exceptionLogService)
    {
        _addressService = addressService;
        _productProcessConfigService = productProcessConfigService;
        _testSchemeConfigService = testSchemeConfigService;
        _programManageService = programManageService;
        _plcCommunicationService = plcCommunicationService;
        _plcExpressionReadService = plcExpressionReadService;
        _plcProductionMonitorService = plcProductionMonitorService;
        _plcWorkIdMonitorService = plcWorkIdMonitorService;
        _plcWeldCycleMonitorService = plcWeldCycleMonitorService;
        _localizer = localizer;
        _exceptionLogService = exceptionLogService;

        InitializeComponent();
        ConfigureTables();
        InitializeSchemeDetailRoleGrid();
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
        ConfigureProductProcessColumns();
        ConfigureTestSchemeColumns();
        ConfigureTestItemColumns();
        ApplyActiveFilter(GetActiveKeyword());
    }

    /// <summary>
    /// 初始化所有表格的统一外观和编辑行为。
    /// </summary>
    private void ConfigureTables()
    {
        TableStyleHelper.ApplyAntdTable(tableAddresses);
        TableStyleHelper.ApplyAntdTable(tableProcess);
        TableStyleHelper.ApplyAntdTable(tableTestSchemes);
        TableStyleHelper.ApplyAntdTable(tableTestItems);

        tableAddresses.EditLostFocus = true;
        tableAddresses.LostFocusClearSelection = false;
        tableProcess.EditLostFocus = true;
        tableProcess.LostFocusClearSelection = false;
        tableTestSchemes.EditLostFocus = true;
        tableTestSchemes.LostFocusClearSelection = false;
        tableTestItems.EditLostFocus = true;
        tableTestItems.LostFocusClearSelection = false;

        ConfigureBusinessAddressColumns();
        ConfigureProductProcessColumns();
        ConfigureTestSchemeColumns();
        ConfigureTestItemColumns();
    }

    /// <summary>
    /// 初始化方案明细右侧输出配置表格。
    /// TreeView 负责快速勾选采集字段，表格负责编辑表头、报表和 MES 输出配置。
    /// </summary>
    private void InitializeSchemeDetailRoleGrid()
    {
        _schemeDetailRoleGrid.AllowUserToAddRows = false;
        _schemeDetailRoleGrid.AllowUserToDeleteRows = false;
        _schemeDetailRoleGrid.AutoGenerateColumns = false;
        _schemeDetailRoleGrid.BackgroundColor = Color.White;
        _schemeDetailRoleGrid.BorderStyle = BorderStyle.FixedSingle;
        _schemeDetailRoleGrid.Dock = DockStyle.Fill;
        _schemeDetailRoleGrid.MultiSelect = false;
        _schemeDetailRoleGrid.RowHeadersVisible = false;
        _schemeDetailRoleGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _schemeDetailRoleGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SchemeDetailRoleTableRow.ItemName),
            HeaderText = "测试项",
            ReadOnly = true,
            Width = 150
        });
        _schemeDetailRoleGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SchemeDetailRoleTableRow.RoleName),
            HeaderText = "字段",
            ReadOnly = true,
            Width = 76
        });
        _schemeDetailRoleGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(SchemeDetailRoleTableRow.Enabled),
            HeaderText = "采集",
            Width = 58
        });
        _schemeDetailRoleGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SchemeDetailRoleTableRow.HeaderText),
            HeaderText = "显示表头",
            Width = 150
        });
        _schemeDetailRoleGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(SchemeDetailRoleTableRow.ReportEnabled),
            HeaderText = "报表",
            Width = 58
        });
        _schemeDetailRoleGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(SchemeDetailRoleTableRow.MesEnabled),
            HeaderText = "MES",
            Width = 58
        });
        _schemeDetailRoleGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SchemeDetailRoleTableRow.MesFieldName),
            HeaderText = "MES字段名",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });

        TableStyleHelper.ApplyDataGridView(_schemeDetailRoleGrid);

        // 左侧树用于快速勾选采集字段，右侧表格用于维护每个字段的输出配置。
        schemeDetailLayout.Controls.Remove(treeSchemeDetails);
        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = 340,
            SplitterWidth = 6
        };
        treeSchemeDetails.Dock = DockStyle.Fill;
        splitContainer.Panel1.Controls.Add(treeSchemeDetails);
        splitContainer.Panel2.Controls.Add(_schemeDetailRoleGrid);
        schemeDetailLayout.Controls.Add(splitContainer, 0, 2);
    }

    #region 业务信号

    private void ConfigureBusinessAddressColumns()
    {
        tableAddresses.Columns.Clear();
        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.Sort), TextKeys.Grid.Sort));
        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.AddressName), TextKeys.Grid.Name, readOnly: true));
        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.StationNo), TextKeys.Grid.Station, readOnly: true));
        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.Address), TextKeys.Grid.Address));
        tableAddresses.Columns.Add(CreateDataTypeColumn(nameof(PlcAddressTableRow.DataType), _localizer.GetString(TextKeys.Grid.DataType)));
        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.DataLength), TextKeys.Grid.Length));
        tableAddresses.Columns.Add(CreateAddressEnabledColumn(_localizer.GetString(TextKeys.Grid.Enabled)));
        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.Description), TextKeys.Grid.Description));
        tableAddresses.Columns.Add(CreateTableColumn(nameof(PlcAddressTableRow.UpdatedTime), TextKeys.Grid.UpdatedTime, readOnly: true, displayFormat: "yyyy-MM-dd HH:mm:ss"));
        TableStyleHelper.ApplyAntdColumnDefaults(tableAddresses);
    }

    #endregion


    private void ConfigureProductProcessColumns()
    {
        tableProcess.Columns.Clear();
        tableProcess.Columns.Add(CreateProgramProductNumColumn());
        tableProcess.Columns.Add(CreateSchemeSelectColumn(nameof(ProductProcessTableRow.SchemeId), "测试方案ID"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.StationNo), "工位(0共享)"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.TouchCount), "焊点数量"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.PointName), "采集点名称"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.PointNoHeader), "编号表头"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.PointResultHeader), "结果表头"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.PointCountHeader), "数量表头"));
        tableProcess.Columns.Add(CreateProductProcessTestFlagColumn());
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.ProductBase), "产品头基地址"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.ProductLen), "产品头长度"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.ProductNoExpr), "产品编号偏移"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.ProductResultExpr), "产品结果偏移"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.ActualTouchCountExpr), "实际焊点数偏移"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.PresetTouchCountExpr), "预设焊点数偏移"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.TouchNoBase), "焊点编号基地址"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.TouchResultBase), "焊点结果基地址"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.TouchHeaderLen), "焊点头长度"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.TouchNoExpr), "焊点编号偏移"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.TouchResultExpr), "焊点结果偏移"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.TestBase), "测试项基地址"));
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.TestAreaLen), "测试区长度"));
        tableProcess.Columns.Add(CreateProductProcessEnabledColumn());
        tableProcess.Columns.Add(CreateRawColumn(nameof(ProductProcessTableRow.UpdatedTime), "更新时间", readOnly: true, displayFormat: "yyyy-MM-dd HH:mm:ss"));
        TableStyleHelper.ApplyAntdColumnDefaults(tableProcess);
    }

    private void ConfigureTestSchemeColumns()
    {
        tableTestSchemes.Columns.Clear();
        tableTestSchemes.Columns.Add(CreateRawColumn(nameof(TestSchemeTableRow.SchemeId), "测试方案ID"));
        tableTestSchemes.Columns.Add(CreateRawColumn(nameof(TestSchemeTableRow.SchemeName), "方案名称"));
        tableTestSchemes.Columns.Add(CreateRawColumn(nameof(TestSchemeTableRow.Description), "备注"));
        TableStyleHelper.ApplyAntdColumnDefaults(tableTestSchemes);
    }

    private void ConfigureSchemeDetailColumns()
    {
        BindSchemeDetailSchemeOptions();
    }

    private void ConfigureTestItemColumns()
    {
        tableTestItems.Columns.Clear();
        tableTestItems.Columns.Add(CreateRawColumn(nameof(TestItemTableRow.ItemId), "测试项ID", readOnly: true));
        tableTestItems.Columns.Add(CreateRawColumn(nameof(TestItemTableRow.ItemName), "测试项目"));
        tableTestItems.Columns.Add(CreateRawColumn(nameof(TestItemTableRow.Unit), "单位"));
        tableTestItems.Columns.Add(CreateRawColumn(nameof(TestItemTableRow.ActualExpression), "实际值偏移"));
        tableTestItems.Columns.Add(CreateRawColumn(nameof(TestItemTableRow.UpperExpression), "上限偏移"));
        tableTestItems.Columns.Add(CreateRawColumn(nameof(TestItemTableRow.LowerExpression), "下限偏移"));
        tableTestItems.Columns.Add(CreateRawColumn(nameof(TestItemTableRow.ResultExpression), "结果偏移"));
        TableStyleHelper.ApplyAntdColumnDefaults(tableTestItems);
    }

    private AntdUI.Column CreateTableColumn(string key, string titleKey, bool readOnly = false, string? displayFormat = null)
    {
        return CreateRawColumn(key, _localizer.GetString(titleKey), readOnly, displayFormat);
    }

    /// <summary>
    /// 创建表头
    /// </summary>
    /// <param name="key"></param>
    /// <param name="title"></param>
    /// <param name="readOnly"></param>
    /// <param name="displayFormat"></param>
    /// <returns></returns>
    private static AntdUI.Column CreateRawColumn(string key, string title, bool readOnly = false, string? displayFormat = null)
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

    private AntdUI.ColumnSelect CreateProgramProductNumColumn()
    {
        return new AntdUI.ColumnSelect(nameof(ProductProcessTableRow.ProductNum), "产品工号*")
        {
            Align = AntdUI.ColumnAlign.Center,
            Editable = true,
            Items = _programOptions
                .Where(program => !string.IsNullOrWhiteSpace(program.ProductNum))
                .GroupBy(program => program.ProductNum.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key)
                .Select(group => new AntdUI.SelectItem(group.Key) { Tag = group.Key })
                .ToList()
        };
    }

    private AntdUI.ColumnSelect CreateSchemeSelectColumn(string key, string title)
    {
        return new AntdUI.ColumnSelect(key, title)
        {
            Align = AntdUI.ColumnAlign.Center,
            Editable = true,
            Items = _testSchemes
                .OrderBy(scheme => scheme.SchemeId)
                .Select(scheme => new AntdUI.SelectItem($"{scheme.SchemeId} - {scheme.SchemeName}") { Tag = scheme.SchemeId })
                .ToList()
        };
    }

    private static AntdUI.ColumnSelect CreateDataTypeColumn(string key, string title)
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

    private static AntdUI.ColumnSwitch CreateAddressEnabledColumn(string? titleKey)
    {
        return new AntdUI.ColumnSwitch(nameof(PlcAddressTableRow.Enabled), titleKey ?? string.Empty)
        {
            Align = AntdUI.ColumnAlign.Center,
            AutoCheck = true
        };
    }

    private static AntdUI.ColumnSwitch CreateProductProcessEnabledColumn()
    {
        return new AntdUI.ColumnSwitch(nameof(ProductProcessTableRow.Enabled), "启用")
        {
            Align = AntdUI.ColumnAlign.Center,
            AutoCheck = true
        };
    }

    private static AntdUI.ColumnSwitch CreateProductProcessTestFlagColumn()
    {
        return new AntdUI.ColumnSwitch(nameof(ProductProcessTableRow.ShowTestFlagInHistory), "历史显示试焊件")
        {
            Align = AntdUI.ColumnAlign.Center,
            AutoCheck = true
        };
    }

    private static AntdUI.ColumnAlign GetColumnAlign(string key)
    {
        return key.Contains("Address", StringComparison.OrdinalIgnoreCase)
            || key.Contains("Base", StringComparison.OrdinalIgnoreCase)
            || key.Contains("Expr", StringComparison.OrdinalIgnoreCase)
            || key.Contains("Expression", StringComparison.OrdinalIgnoreCase)
            || key is nameof(PlcAddressTableRow.Description)
                or nameof(TestSchemeTableRow.Description)
                or nameof(TestItemTableRow.ItemName)
            ? AntdUI.ColumnAlign.Left
            : AntdUI.ColumnAlign.Center;
    }

    private void WireEvents()
    {
        btnSave.Click += Save_Click;
        btnRefresh.Click += (_, _) => LoadData();
        btnTest.Click += TestSelected_Click;
        queryAddresses.QueryClick += (_, keyword) => ApplyActiveFilter(keyword);
        tabAddressCategories.SelectedIndexChanged += (_, _) => SwitchActiveFilterText();

        tableAddresses.CellClick += Table_CellClick;
        tableAddresses.CellEndEdit += Table_CellEndEdit;
        tableAddresses.CellEndValueEdit += Table_CellEndValueEdit;
        tableAddresses.CellEditComplete += Table_CellEditComplete;
        tableAddresses.CheckedChanged += Table_CheckedChanged;

        tableProcess.CellClick += Table_CellClick;
        tableProcess.CellEndEdit += Table_CellEndEdit;
        tableProcess.CellEndValueEdit += Table_CellEndValueEdit;
        tableProcess.CellEditComplete += Table_CellEditComplete;
        tableProcess.CheckedChanged += Table_CheckedChanged;

        btnAddProductProcess.Click += AddProductProcess_Click;
        btnDeleteProductProcess.Click += DeleteProductProcess_Click;
        btnPreviewProductProcessAddress.Click += PreviewProductProcessAddress_Click;
        btnAddScheme.Click += AddScheme_Click;
        btnDeleteScheme.Click += DeleteScheme_Click;
        btnAddTestItem.Click += AddTestItem_Click;
        btnDeleteTestItem.Click += DeleteTestItem_Click;
        selectSchemeDetailScheme.SelectedIndexChanged += SchemeDetailScheme_SelectedIndexChanged;
        treeSchemeDetails.AfterCheck += SchemeDetailTree_AfterCheck;

        tableTestSchemes.CellClick += Table_CellClick;
        tableTestSchemes.CellEndEdit += Table_CellEndEdit;
        tableTestSchemes.CellEndValueEdit += Table_CellEndValueEdit;
        tableTestSchemes.CellEditComplete += Table_CellEditComplete;

        tableTestItems.CellClick += Table_CellClick;
        tableTestItems.CellEndEdit += Table_CellEndEdit;
        tableTestItems.CellEndValueEdit += Table_CellEndValueEdit;
        tableTestItems.CellEditComplete += Table_CellEditComplete;
    }

    private void ApplyLocalizedTexts()
    {
        lblTitle.Text = _localizer.GetString(TextKeys.Address.Title);
        lblDescription.Text = "维护 PLC 业务信号地址、产品工艺、测试方案、方案明细和测试项字典。";
        btnSave.Text = "保存当前页";
        btnRefresh.Text = _localizer.GetString(TextKeys.Address.ButtonRefresh);
        btnTest.Text = _localizer.GetString(TextKeys.Address.ButtonTest);
        tabBusinessAddresses.Text = "业务信号地址";
        tabTestItemAddresses.Text = "产品工艺";
        tabTestSchemes.Text = "测试方案";
        tabSchemeDetails.Text = "方案明细";
        tabTestItems.Text = "测试项字典";
        lblBindingProduct.Text = "产品工号 ProductNumber";
        lblBindingProcess.Text = "产品工艺 SchemeId";
        lblBindingScheme.Text = "测试方案";
        lblBindingDetail.Text = "方案明细 ItemId";
        lblBindingItem.Text = "测试项字典 表达式";
        lblBindingPreview.Text = "PLC 地址预览";
        lblTestItemAddressHint.Text = "维护产品工号、工位、焊点数量和 PLC 数据区布局；测试方案决定采集哪些测试项。";
        lblProductProcessGroupHint.Text = "分组填写：产品头保存产品级字段，焊点头按焊点头长度递增，测试项区按测试区长度递增；最终地址可通过 PLC 地址预览核对。";
        lblSchemeDetailHint.Text = "先选择测试方案，再勾选该方案包含的测试项字段；新增测试项字典后会自动出现在树中。";
        lblSchemeDetailScheme.Text = "测试方案";
        btnAddProductProcess.Text = "新增";
        btnDeleteProductProcess.Text = "删除";
        btnPreviewProductProcessAddress.Text = "PLC 地址预览";
        btnAddScheme.Text = "新增";
        btnDeleteScheme.Text = "删除";
        btnAddTestItem.Text = "新增";
        btnDeleteTestItem.Text = "删除";
        SyncActiveCommandState();
    }

    private void LoadData(bool showError = true)
    {
        try
        {
            EndTableEdit();
            treeSchemeDetails.Nodes.Clear();
            _allAddresses.Clear();
            _allAddresses.AddRange(_addressService.GetAll());
            _programOptions.Clear();
            _programOptions.AddRange(_programManageService.GetPrograms());
            _testSchemes.Clear();
            _testSchemes.AddRange(_testSchemeConfigService.GetSchemes());
            _temporaryTestItemIds.Clear();
            _testItems.Clear();
            _testItems.AddRange(_testSchemeConfigService.GetItems());
            _schemeDetails.Clear();
            _schemeDetails.AddRange(_testSchemeConfigService.GetDetails());
            _productProcessConfigs.Clear();
            _productProcessConfigs.AddRange(_productProcessConfigService.GetAll(includeDisabled: true));

            ConfigureProductProcessColumns();
            ConfigureSchemeDetailColumns();
            ApplyAddressFilter(_addressKeyword);
            ApplyProductProcessFilter(_productProcessKeyword);
            ApplySchemeFilter(_schemeKeyword);
            ApplyDetailFilter(_detailKeyword);
            ApplyItemFilter(_itemKeyword);
            SyncActiveCommandState();
        }
        catch (Exception ex)
        {
            if (showError)
            {
                ShowError(_localizer.GetString(TextKeys.Address.MessageSaveFailed, ex.Message));
                return;
            }

            throw;
        }
    }

    private void ApplyActiveFilter(string? keyword)
    {
        if (tabAddressCategories.SelectedTab == tabBusinessAddresses)
        {
            ApplyAddressFilter(keyword);
            return;
        }

        if (tabAddressCategories.SelectedTab == tabTestItemAddresses)
        {
            ApplyProductProcessFilter(keyword);
            return;
        }

        if (tabAddressCategories.SelectedTab == tabTestSchemes)
        {
            ApplySchemeFilter(keyword);
            return;
        }

        if (tabAddressCategories.SelectedTab == tabSchemeDetails)
        {
            ApplyDetailFilter(keyword);
            return;
        }

        ApplyItemFilter(keyword);
    }

    private string GetActiveKeyword()
    {
        if (tabAddressCategories.SelectedTab == tabBusinessAddresses)
        {
            return _addressKeyword;
        }

        if (tabAddressCategories.SelectedTab == tabTestItemAddresses)
        {
            return _productProcessKeyword;
        }

        if (tabAddressCategories.SelectedTab == tabTestSchemes)
        {
            return _schemeKeyword;
        }

        if (tabAddressCategories.SelectedTab == tabSchemeDetails)
        {
            return _detailKeyword;
        }

        return _itemKeyword;
    }

    private void SwitchActiveFilterText()
    {
        queryAddresses.Text = GetActiveKeyword();
        SyncActiveCommandState();
    }

    private void SyncActiveCommandState()
    {
        btnTest.Enabled = tabAddressCategories.SelectedTab == tabBusinessAddresses;
        btnPreviewProductProcessAddress.Enabled = _selectedProductProcessRow is not null;
    }

    /// <summary>
    /// 根据当前选中的产品工艺行，给用户展示一眼能看懂的绑定摘要。
    /// </summary>
    private void UpdateProductProcessSummary()
    {
        var config = _selectedProductProcessRow?.Source;
        if (config is null)
        {
            lblProductProcessSummary.Text = "选择一条产品工艺后，可查看产品 -> 焊点 -> 测试项绑定摘要，并打开 PLC 地址预览。";
            return;
        }

        var schemeItemCount = ResolveSchemeItems(config.SchemeId).Count;
        var touchCount = Math.Max(1, config.TouchCount);
        var totalItemRows = touchCount * schemeItemCount;
        var stationText = config.StationNo == ProductionConstants.Stations.SharedStationNo
            ? "共享工位"
            : $"工位 {config.StationNo}";

        lblProductProcessSummary.Text =
            $"当前绑定：产品 {config.ProductNum} / {stationText} / 方案 {config.SchemeId} / 焊点 {touchCount} 个 / 每焊点 {schemeItemCount} 个测试项 / 共 {totalItemRows} 条测试项记录。";
    }

    private void ApplyAddressFilter(string? keyword)
    {
        EndTableEdit();
        _addressKeyword = keyword?.Trim() ?? string.Empty;

        var selectedLogicalKey = _selectedRow?.Source.LogicalKey;
        var selectedStationNo = _selectedRow?.Source.StationNo;

        var filteredAddresses = _allAddresses
            .Where(address => string.IsNullOrWhiteSpace(_addressKeyword)
                || Contains(address.LogicalKey, _addressKeyword)
                || Contains(address.StationNo.ToString(), _addressKeyword)
                || Contains(GetAddressDisplayName(address), _addressKeyword)
                || Contains(address.Address, _addressKeyword)
                || Contains(address.DataType, _addressKeyword)
                || Contains(address.Description, _addressKeyword))
            .OrderBy(address => address.Sort)
            .ThenBy(address => address.StationNo)
            .ToList();

        _currentRows = filteredAddresses
            .Select(address => new PlcAddressTableRow(address, GetAddressDisplayName(address)))
            .ToList();

        tableAddresses.DataSource = _currentRows;
        tableAddresses.Refresh();
        SelectVisibleRow(selectedLogicalKey, selectedStationNo);
    }

    private void ApplyProductProcessFilter(string? keyword)
    {
        EndTableEdit();
        _productProcessKeyword = keyword?.Trim() ?? string.Empty;
        var selectedId = _selectedProductProcessRow?.Id;

        _currentProductProcessRows = _productProcessConfigs
            .Where(config => string.IsNullOrWhiteSpace(_productProcessKeyword)
                || Contains(config.ProductNum, _productProcessKeyword)
                || Contains(config.SchemeId, _productProcessKeyword)
                || Contains(config.StationNo.ToString(), _productProcessKeyword)
                || Contains(config.ProductBase, _productProcessKeyword)
                || Contains(config.TouchBase, _productProcessKeyword)
                || Contains(config.TouchNoBase, _productProcessKeyword)
                || Contains(config.TouchResultBase, _productProcessKeyword)
                || Contains(config.TestBase, _productProcessKeyword))
            .OrderBy(config => config.ProductNum)
            .ThenBy(config => config.StationNo)
            .ThenBy(config => config.Id)
            .Select(config => new ProductProcessTableRow(config))
            .ToList();

        tableProcess.DataSource = _currentProductProcessRows;
        tableProcess.Refresh();
        SelectVisibleProductProcessRow(selectedId);
    }

    private void ApplySchemeFilter(string? keyword)
    {
        EndTableEdit();
        _schemeKeyword = keyword?.Trim() ?? string.Empty;
        var selectedId = _selectedSchemeRow?.SchemeId;

        _currentSchemeRows = _testSchemes
            .Where(scheme => string.IsNullOrWhiteSpace(_schemeKeyword)
                || Contains(scheme.SchemeId, _schemeKeyword)
                || Contains(scheme.SchemeName, _schemeKeyword)
                || Contains(scheme.Description, _schemeKeyword))
            .OrderBy(scheme => scheme.SchemeId)
            .Select(scheme => new TestSchemeTableRow(scheme))
            .ToList();

        tableTestSchemes.DataSource = _currentSchemeRows;
        tableTestSchemes.Refresh();
        SelectVisibleSchemeRow(selectedId);
    }

    private void ApplyDetailFilter(string? keyword)
    {
        EndTableEdit();
        _detailKeyword = keyword?.Trim() ?? string.Empty;
        SyncCurrentSchemeDetailTreeToMemory();
        SyncCurrentSchemeDetailGridToMemory();
        LoadSchemeDetailTree(_detailKeyword);
    }

    private void BindSchemeDetailSchemeOptions()
    {
        SyncCurrentSchemeDetailTreeToMemory();
        SyncCurrentSchemeDetailGridToMemory();

        var previousSchemeId = _currentSchemeDetailSchemeId;
        var schemes = _testSchemes.OrderBy(scheme => scheme.SchemeId).ToList();
        var selectedIndex = schemes.FindIndex(scheme =>
            string.Equals(scheme.SchemeId, previousSchemeId, StringComparison.OrdinalIgnoreCase));
        if (selectedIndex < 0 && schemes.Count > 0)
        {
            selectedIndex = 0;
        }

        _syncingSchemeDetailSchemeSelection = true;
        try
        {
            selectSchemeDetailScheme.Items.Clear();
            foreach (var scheme in schemes)
            {
                selectSchemeDetailScheme.Items.Add(new AntdUI.SelectItem($"{scheme.SchemeId} - {scheme.SchemeName}")
                {
                    Tag = scheme.SchemeId
                });
            }

            selectSchemeDetailScheme.SelectedIndex = selectedIndex;
            selectSchemeDetailScheme.Text = selectedIndex >= 0
                ? $"{schemes[selectedIndex].SchemeId} - {schemes[selectedIndex].SchemeName}"
                : string.Empty;
            _currentSchemeDetailSchemeId = selectedIndex >= 0 ? schemes[selectedIndex].SchemeId : string.Empty;
        }
        finally
        {
            _syncingSchemeDetailSchemeSelection = false;
        }

        LoadSchemeDetailTree(_detailKeyword);
    }

    private void SchemeDetailScheme_SelectedIndexChanged(object? sender, AntdUI.IntEventArgs e)
    {
        if (_syncingSchemeDetailSchemeSelection)
        {
            return;
        }

        SyncCurrentSchemeDetailTreeToMemory();
        SyncCurrentSchemeDetailGridToMemory();
        _currentSchemeDetailSchemeId = ResolveSelectedSchemeDetailSchemeId();
        LoadSchemeDetailTree(_detailKeyword);
    }

    private string ResolveSelectedSchemeDetailSchemeId()
    {
        var selectedValue = GetSelectValueText(selectSchemeDetailScheme.SelectedValue);
        if (!string.IsNullOrWhiteSpace(selectedValue))
        {
            return selectedValue;
        }

        var schemes = _testSchemes.OrderBy(scheme => scheme.SchemeId).ToList();
        return selectSchemeDetailScheme.SelectedIndex >= 0 && selectSchemeDetailScheme.SelectedIndex < schemes.Count
            ? schemes[selectSchemeDetailScheme.SelectedIndex].SchemeId
            : string.Empty;
    }

    private void LoadSchemeDetailTree(string? keyword)
    {
        _handlingSchemeDetailTreeCheck = true;
        treeSchemeDetails.BeginUpdate();
        try
        {
            treeSchemeDetails.Nodes.Clear();
            if (string.IsNullOrWhiteSpace(_currentSchemeDetailSchemeId))
            {
                treeSchemeDetails.Nodes.Add(new TreeNode("请先维护并选择测试方案。"));
                return;
            }

            var visibleItems = _testItems
                .Where(item => item.ItemId > 0)
                .Where(item => MatchesSchemeDetailKeyword(item, keyword))
                .OrderBy(item => item.ItemId)
                .ToList();
            if (visibleItems.Count == 0)
            {
                treeSchemeDetails.Nodes.Add(new TreeNode("当前没有可配置的测试项。"));
                return;
            }

            var detailByItemId = _schemeDetails
                .Where(detail => SameScheme(detail.SchemeId, _currentSchemeDetailSchemeId))
                .GroupBy(detail => detail.ItemId)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var item in visibleItems)
            {
                detailByItemId.TryGetValue(item.ItemId, out var detail);
                treeSchemeDetails.Nodes.Add(CreateSchemeDetailItemNode(item, detail));
            }

            treeSchemeDetails.ExpandAll();
        }
        finally
        {
            treeSchemeDetails.EndUpdate();
            _handlingSchemeDetailTreeCheck = false;
            BindSchemeDetailRoleRows();
        }
    }

    private TreeNode CreateSchemeDetailItemNode(DimTestItem item, BizSchemeDetail? detail)
    {
        var parent = new TreeNode($"{item.ItemId} - {item.ItemName}")
        {
            Tag = new SchemeDetailTreeNodeTag(item.ItemId, null)
        };

        parent.Nodes.Add(CreateSchemeDetailRoleNode(item.ItemId, SchemeDetailValueRole.Actual, "实际值", detail?.EnableActual ?? false));
        parent.Nodes.Add(CreateSchemeDetailRoleNode(item.ItemId, SchemeDetailValueRole.Upper, "上限", detail?.EnableUpper ?? false));
        parent.Nodes.Add(CreateSchemeDetailRoleNode(item.ItemId, SchemeDetailValueRole.Lower, "下限", detail?.EnableLower ?? false));
        parent.Nodes.Add(CreateSchemeDetailRoleNode(item.ItemId, SchemeDetailValueRole.Result, "结果", detail?.EnableResult ?? false));
        parent.Checked = parent.Nodes.Cast<TreeNode>().Any(node => node.Checked);
        return parent;
    }

    private static TreeNode CreateSchemeDetailRoleNode(
        int itemId,
        SchemeDetailValueRole role,
        string text,
        bool isChecked)
    {
        return new TreeNode(text)
        {
            Tag = new SchemeDetailTreeNodeTag(itemId, role),
            Checked = isChecked
        };
    }

    private static bool MatchesSchemeDetailKeyword(DimTestItem item, string? keyword)
    {
        var normalizedKeyword = keyword?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            return true;
        }

        return Contains(item.ItemId.ToString(CultureInfo.InvariantCulture), normalizedKeyword)
            || Contains(item.ItemName, normalizedKeyword)
            || Contains(item.Unit, normalizedKeyword)
            || Contains("实际值", normalizedKeyword)
            || Contains("上限", normalizedKeyword)
            || Contains("下限", normalizedKeyword)
            || Contains("结果", normalizedKeyword);
    }

    private void SchemeDetailTree_AfterCheck(object? sender, TreeViewEventArgs e)
    {
        var node = e.Node;
        if (_handlingSchemeDetailTreeCheck || node is null)
        {
            return;
        }

        _handlingSchemeDetailTreeCheck = true;
        try
        {
            if (node.Tag is SchemeDetailTreeNodeTag { Role: null })
            {
                SetChildCheckedState(node, node.Checked);
            }

            UpdateParentCheckedState(node.Parent);
        }
        finally
        {
            _handlingSchemeDetailTreeCheck = false;
        }

        // 树节点是采集开关的快捷入口，切换后同步刷新右侧输出配置表。
        SyncCurrentSchemeDetailTreeToMemory();
        BindSchemeDetailRoleRows();
    }

    private void SetChildCheckedState(TreeNode node, bool isChecked)
    {
        foreach (TreeNode child in node.Nodes)
        {
            child.Checked = isChecked;
            SetChildCheckedState(child, isChecked);
        }
    }

    private void UpdateParentCheckedState(TreeNode? node)
    {
        if (node is null)
        {
            return;
        }

        node.Checked = node.Nodes.Cast<TreeNode>().Any(child => child.Checked);
        UpdateParentCheckedState(node.Parent);
    }

    private void SyncCurrentSchemeDetailTreeToMemory()
    {
        var schemeId = _currentSchemeDetailSchemeId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(schemeId) || treeSchemeDetails.Nodes.Count == 0)
        {
            return;
        }

        var visibleItemIds = treeSchemeDetails.Nodes
            .Cast<TreeNode>()
            .Select(node => node.Tag as SchemeDetailTreeNodeTag)
            .Where(tag => tag is { Role: null })
            .Select(tag => tag!.ItemId)
            .Distinct()
            .ToHashSet();
        if (visibleItemIds.Count == 0)
        {
            return;
        }

        var existingDetails = _schemeDetails
            .Where(detail => SameScheme(detail.SchemeId, schemeId) && visibleItemIds.Contains(detail.ItemId))
            .GroupBy(detail => detail.ItemId)
            .ToDictionary(group => group.Key, group => group.First());

        _schemeDetails.RemoveAll(detail => SameScheme(detail.SchemeId, schemeId) && visibleItemIds.Contains(detail.ItemId));
        foreach (TreeNode itemNode in treeSchemeDetails.Nodes)
        {
            if (itemNode.Tag is not SchemeDetailTreeNodeTag { Role: null } itemTag)
            {
                continue;
            }

            var detail = existingDetails.TryGetValue(itemTag.ItemId, out var existingDetail)
                ? existingDetail
                : new BizSchemeDetail { SchemeId = schemeId, ItemId = itemTag.ItemId };
            ApplySchemeDetailRoleState(detail, itemNode);
            if (HasAnyEnabledRole(detail))
            {
                _schemeDetails.Add(detail);
            }
        }
    }

    private void BindSchemeDetailRoleRows()
    {
        var schemeId = _currentSchemeDetailSchemeId?.Trim() ?? string.Empty;
        var visibleItemIds = treeSchemeDetails.Nodes
            .Cast<TreeNode>()
            .Select(node => node.Tag as SchemeDetailTreeNodeTag)
            .Where(tag => tag is { Role: null })
            .Select(tag => tag!.ItemId)
            .Distinct()
            .ToHashSet();

        var detailByItemId = _schemeDetails
            .Where(detail => SameScheme(detail.SchemeId, schemeId) && visibleItemIds.Contains(detail.ItemId))
            .GroupBy(detail => detail.ItemId)
            .ToDictionary(group => group.Key, group => group.First());

        _currentSchemeDetailRoleRows = _testItems
            .Where(item => visibleItemIds.Contains(item.ItemId))
            .OrderBy(item => item.ItemId)
            .SelectMany(item =>
            {
                var detail = detailByItemId.TryGetValue(item.ItemId, out var existingDetail)
                    ? existingDetail
                    : CreateEmptySchemeDetail(schemeId, item.ItemId);
                return CreateSchemeDetailRoleRows(detail, item);
            })
            .ToList();

        _schemeDetailRoleGrid.DataSource = null;
        _schemeDetailRoleGrid.DataSource = _currentSchemeDetailRoleRows;
    }

    private void SyncCurrentSchemeDetailGridToMemory()
    {
        _schemeDetailRoleGrid.EndEdit();
        var schemeId = _currentSchemeDetailSchemeId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(schemeId) || _currentSchemeDetailRoleRows.Count == 0)
        {
            return;
        }

        var visibleItemIds = _currentSchemeDetailRoleRows
            .Select(row => row.ItemId)
            .Distinct()
            .ToHashSet();

        _schemeDetails.RemoveAll(detail => SameScheme(detail.SchemeId, schemeId) && visibleItemIds.Contains(detail.ItemId));
        foreach (var group in _currentSchemeDetailRoleRows.GroupBy(row => row.ItemId))
        {
            var detail = group.First().Source;
            detail.SchemeId = schemeId;
            detail.ItemId = group.Key;
            foreach (var row in group)
            {
                row.NormalizeForSave();
            }

            if (HasAnyEnabledRole(detail))
            {
                _schemeDetails.Add(detail);
            }
        }
    }

    private IEnumerable<SchemeDetailRoleTableRow> CreateSchemeDetailRoleRows(BizSchemeDetail detail, DimTestItem item)
    {
        yield return new SchemeDetailRoleTableRow(detail, item, SchemeDetailValueRole.Actual);
        yield return new SchemeDetailRoleTableRow(detail, item, SchemeDetailValueRole.Upper);
        yield return new SchemeDetailRoleTableRow(detail, item, SchemeDetailValueRole.Lower);
        yield return new SchemeDetailRoleTableRow(detail, item, SchemeDetailValueRole.Result);
    }

    private static BizSchemeDetail CreateEmptySchemeDetail(string schemeId, int itemId)
    {
        return new BizSchemeDetail
        {
            SchemeId = schemeId,
            ItemId = itemId,
            EnableActual = false,
            EnableUpper = false,
            EnableLower = false,
            EnableResult = false
        };
    }

    private static void ApplySchemeDetailRoleState(BizSchemeDetail detail, TreeNode itemNode)
    {
        detail.EnableActual = IsSchemeDetailRoleChecked(itemNode, SchemeDetailValueRole.Actual);
        detail.EnableUpper = IsSchemeDetailRoleChecked(itemNode, SchemeDetailValueRole.Upper);
        detail.EnableLower = IsSchemeDetailRoleChecked(itemNode, SchemeDetailValueRole.Lower);
        detail.EnableResult = IsSchemeDetailRoleChecked(itemNode, SchemeDetailValueRole.Result);
    }

    private static bool IsSchemeDetailRoleChecked(TreeNode itemNode, SchemeDetailValueRole role)
    {
        return itemNode.Nodes
            .Cast<TreeNode>()
            .Any(node => node.Checked
                && node.Tag is SchemeDetailTreeNodeTag tag
                && tag.Role == role);
    }

    private void ApplyItemFilter(string? keyword)
    {
        EndTableEdit();
        _itemKeyword = keyword?.Trim() ?? string.Empty;
        var selectedId = _selectedItemRow?.ItemId;

        _currentItemRows = _testItems
            .Where(item => string.IsNullOrWhiteSpace(_itemKeyword)
                || Contains(item.ItemId.ToString(), _itemKeyword)
                || Contains(item.ItemName, _itemKeyword)
                || Contains(item.Unit, _itemKeyword)
                || Contains(item.ActualExpression, _itemKeyword)
                || Contains(item.UpperExpression, _itemKeyword)
                || Contains(item.LowerExpression, _itemKeyword)
                || Contains(item.ResultExpression, _itemKeyword))
            .OrderBy(item => item.ItemId)
            .Select(item => new TestItemTableRow(item, GetTestItemDisplayId(item)))
            .ToList();

        tableTestItems.DataSource = _currentItemRows;
        tableTestItems.Refresh();
        SelectVisibleItemRow(selectedId);
    }

    private async void Save_Click(object? sender, EventArgs e)
    {
        if (_saving)
        {
            return;
        }

        _saving = true;
        btnSave.Enabled = false;
        try
        {
            EndTableEdit();

            if (tabAddressCategories.SelectedTab == tabBusinessAddresses)
            {
                await SaveBusinessAddressesAsync();
                return;
            }

            if (tabAddressCategories.SelectedTab == tabTestItemAddresses)
            {
                SaveProductProcesses();
                return;
            }

            if (tabAddressCategories.SelectedTab == tabTestSchemes)
            {
                SaveSchemes();
                return;
            }

            if (tabAddressCategories.SelectedTab == tabSchemeDetails)
            {
                SaveSchemeDetails();
                return;
            }

            SaveTestItems();
        }
        catch (Exception ex)
        {
            ShowError(_localizer.GetString(TextKeys.Address.MessageSaveFailed, ex.Message));
        }
        finally
        {
            btnSave.Enabled = true;
            _saving = false;
        }
    }

    private async Task SaveBusinessAddressesAsync()
    {
        var addresses = _allAddresses.ToList();
        NormalizeAddresses(addresses);
        _addressService.SaveAll(addresses);
        await RefreshAddressDependentServicesQuietlyAsync();
        ReloadBusinessAddressDataAfterSave();
        ShowInfo(_localizer.GetString(TextKeys.Address.MessageSaveSuccess));
    }

    /// <summary>
    /// 地址已经写入数据库后，逐个刷新依赖地址的后台服务。
    /// 某一个后台刷新失败时只记录日志，不影响其他服务继续刷新。
    /// </summary>
    private async Task RefreshAddressDependentServicesQuietlyAsync()
    {
        await TryRefreshAddressDependentServiceAsync(
            "AddressManageView.RefreshProductionMonitorAddresses",
            () => _plcProductionMonitorService.ReloadAddressesAsync());
        await TryRefreshAddressDependentServiceAsync(
            "AddressManageView.RefreshWorkIdMonitorAddress",
            () => _plcWorkIdMonitorService.ReloadAddressAsync());
        await TryRefreshAddressDependentServiceAsync(
            "AddressManageView.RefreshWeldCycleMonitorAddresses",
            () => _plcWeldCycleMonitorService.ReloadAddressesAsync());
        await TryRefreshAddressDependentServiceAsync(
            "AddressManageView.RestartPlcCommunication",
            () => _plcCommunicationService.RestartAsync());
    }

    /// <summary>
    /// 统一保护单个后台刷新步骤，避免保存成功后因为后台服务异常误报保存失败。
    /// </summary>
    private async Task TryRefreshAddressDependentServiceAsync(string source, Func<Task> refreshAction)
    {
        try
        {
            await refreshAction();
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, source);
        }
    }

    /// <summary>
    /// 业务信号地址保存后只刷新当前地址页。
    /// 全量 LoadData 会读取另外四个配置页签，任一新配置表异常都会被误报为地址保存后的刷新失败。
    /// </summary>
    private void ReloadBusinessAddressDataAfterSave()
    {
        try
        {
            _allAddresses.Clear();
            _allAddresses.AddRange(_addressService.GetAll());
            ApplyAddressFilter(_addressKeyword);
            SyncActiveCommandState();
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, "AddressManageView.ReloadBusinessAddressDataAfterSave");
        }
    }

    private string? TryReloadDataAfterSave(string source)
    {
        try
        {
            LoadData(showError: false);
            return null;
        }
        catch (Exception ex)
        {
            _exceptionLogService.Write(ex, source);
            return ex.Message;
        }
    }

    private void ShowPostSaveResult(string successMessage, params string?[] warningMessages)
    {
        var warnings = warningMessages
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message!.Trim())
            .ToList();
        if (warnings.Count == 0)
        {
            ShowInfo(successMessage);
            return;
        }

        ShowWarning($"{successMessage}\r\n但保存后的刷新操作失败：{string.Join("；", warnings)}");
    }

    private void SaveProductProcesses()
    {
        NormalizeProductProcesses(_productProcessConfigs);
        ValidateProductProcesses(_productProcessConfigs);
        foreach (var config in _productProcessConfigs.OrderBy(config => config.ProductNum).ThenBy(config => config.StationNo))
        {
            _productProcessConfigService.Save(config);
        }

        ShowPostSaveResult("产品工艺配置已保存。", TryReloadDataAfterSave("AddressManageView.ReloadAfterProductProcessSave"));
    }

    private void SaveSchemes()
    {
        NormalizeSchemes(_testSchemes);
        ValidateSchemes(_testSchemes);
        foreach (var scheme in _testSchemes.OrderBy(scheme => scheme.SchemeId))
        {
            _testSchemeConfigService.SaveScheme(scheme);
        }

        ShowPostSaveResult("测试方案已保存。", TryReloadDataAfterSave("AddressManageView.ReloadAfterSchemeSave"));
    }

    private void SaveSchemeDetails()
    {
        SyncCurrentSchemeDetailTreeToMemory();
        SyncCurrentSchemeDetailGridToMemory();

        var details = _schemeDetails
            .Where(HasAnyEnabledRole)
            .OrderBy(detail => detail.SchemeId)
            .ThenBy(detail => detail.DetailId)
            .ToList();
        NormalizeSchemeDetails(details);
        ValidateSchemeDetails(details);

        var currentKeys = details
            .Select(detail => BuildSchemeDetailKey(detail.SchemeId, detail.ItemId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var persistedDetail in _testSchemeConfigService.GetDetails())
        {
            if (!currentKeys.Contains(BuildSchemeDetailKey(persistedDetail.SchemeId, persistedDetail.ItemId)))
            {
                _testSchemeConfigService.DeleteDetail(persistedDetail.DetailId);
            }
        }

        foreach (var detail in details)
        {
            _testSchemeConfigService.SaveDetail(detail);
        }

        ShowPostSaveResult("方案明细已保存。", TryReloadDataAfterSave("AddressManageView.ReloadAfterSchemeDetailSave"));
    }

    private void SaveTestItems()
    {
        NormalizeTestItems(_testItems);
        foreach (var item in _testItems.OrderBy(item => item.ItemId <= 0).ThenBy(item => item.ItemId))
        {
            var saved = _testSchemeConfigService.SaveItem(item);
            item.ItemId = saved.ItemId;
        }

        ShowPostSaveResult("测试项字典已保存。", TryReloadDataAfterSave("AddressManageView.ReloadAfterTestItemSave"));
    }

    private async void TestSelected_Click(object? sender, EventArgs e)
    {
        EndTableEdit();

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

    private void RegisterBusinessAddressTestClick(PlcAddressTableRow row)
    {
        var now = Environment.TickCount64;
        var isContinuousSameRow = ReferenceEquals(_lastBusinessAddressClickRow, row)
            && now - _lastBusinessAddressClickTicks <= TestAddressTripleClickIntervalMs;

        _businessAddressClickCount = isContinuousSameRow
            ? _businessAddressClickCount + 1
            : 1;
        _lastBusinessAddressClickRow = row;
        _lastBusinessAddressClickTicks = now;

        if (_businessAddressClickCount < 3)
        {
            return;
        }

        ResetBusinessAddressTestClick();
        btnTest.PerformClick();
    }

    private void ResetBusinessAddressTestClick()
    {
        _lastBusinessAddressClickRow = null;
        _lastBusinessAddressClickTicks = 0;
        _businessAddressClickCount = 0;
    }

    private async Task<PlcServiceResult<string>> ReadAddressAsync(string? address, string? dataTypeValue, int dataLength)
    {
        return await _plcExpressionReadService.ReadResolvedAddressTextAsync(
            address,
            dataTypeValue,
            stringLength: dataLength);
    }

    private void PreviewProductProcessAddress_Click(object? sender, EventArgs e)
    {
        EndTableEdit();
        var row = _selectedProductProcessRow;
        if (row is null)
        {
            ShowWarning("请先选择一条产品工艺配置。");
            return;
        }

        row.NormalizeForDisplay();
        UpdateProductProcessSummary();

        var rows = BuildProductProcessAddressPreviewRows(row.Source);
        using var form = new AddressPreviewForm(rows, _plcExpressionReadService, _localizer);
        form.ShowDialog(this);
    }

    /// <summary>
    /// 按“产品工艺 -> 方案明细 -> 测试项字典”的关系，生成给地址预览窗体显示的行。
    /// </summary>
    private IReadOnlyList<PlcAddressPreviewRow> BuildProductProcessAddressPreviewRows(BizProductProcessConfig config)
    {
        var identity = ResolveProductProcessPreviewIdentity(config);
        var schemeItems = ResolveSchemeItems(config.SchemeId);
        var pointName = ResolvePointName(config);
        var pointNoHeader = ResolvePointNoHeader(config);
        var pointResultHeader = ResolvePointResultHeader(config);
        var pointCountHeader = ResolvePointCountHeader(config);
        var rows = new List<PlcAddressPreviewRow>();

        AddProductProcessAddressPreviewRow(rows, identity, "产品头", "-", "产品编号", config.ProductBase, 0, config.ProductNoExpr);
        AddProductProcessAddressPreviewRow(rows, identity, "产品头", "-", "产品结果", config.ProductBase, 0, config.ProductResultExpr);
        AddProductProcessAddressPreviewRow(rows, identity, "产品头", "-", $"实际{pointCountHeader}", config.ProductBase, 0, config.ActualTouchCountExpr);
        AddProductProcessAddressPreviewRow(rows, identity, "产品头", "-", $"预设{pointCountHeader}", config.ProductBase, 0, config.PresetTouchCountExpr);

        if (schemeItems.Count == 0)
        {
            rows.Add(PlcAddressPreviewRow.Info(identity.StationNo, $"测试方案 {config.SchemeId} 尚未配置方案明细。"));
        }

        for (var touchNo = 1; touchNo <= Math.Max(1, config.TouchCount); touchNo++)
        {
            var touchContextOffset = (touchNo - 1) * config.TouchHeaderLen;
            var testContextOffset = (touchNo - 1) * config.TestAreaLen;
            var touchText = touchNo.ToString(CultureInfo.InvariantCulture);

            AddProductProcessAddressPreviewRow(rows, identity, $"{pointName}头", touchText, pointNoHeader, ResolveTouchNoBase(config), touchContextOffset, config.TouchNoExpr);
            AddProductProcessAddressPreviewRow(rows, identity, $"{pointName}头", touchText, pointResultHeader, ResolveTouchResultBase(config), touchContextOffset, config.TouchResultExpr);

            foreach (var schemeItem in schemeItems)
            {
                var item = schemeItem.Item;
                var detail = schemeItem.Detail;
                if (detail.EnableActual)
                {
                    AddProductProcessAddressPreviewRow(rows, identity, "测试项", touchText, ResolveSchemeDetailHeader(detail, item, SchemeDetailValueRole.Actual), config.TestBase, testContextOffset, item.ActualExpression);
                }

                if (detail.EnableUpper)
                {
                    AddProductProcessAddressPreviewRow(rows, identity, "测试项", touchText, ResolveSchemeDetailHeader(detail, item, SchemeDetailValueRole.Upper), config.TestBase, testContextOffset, item.UpperExpression);
                }

                if (detail.EnableLower)
                {
                    AddProductProcessAddressPreviewRow(rows, identity, "测试项", touchText, ResolveSchemeDetailHeader(detail, item, SchemeDetailValueRole.Lower), config.TestBase, testContextOffset, item.LowerExpression);
                }

                if (detail.EnableResult)
                {
                    AddProductProcessAddressPreviewRow(rows, identity, "测试项", touchText, ResolveSchemeDetailHeader(detail, item, SchemeDetailValueRole.Result), config.TestBase, testContextOffset, item.ResultExpression);
                }
            }
        }

        return rows.Count > 0
            ? rows
            : new[] { PlcAddressPreviewRow.Info(identity.StationNo, "当前产品工艺没有可预览的 PLC 地址表达式。") };
    }

    private ProductProcessPreviewIdentity ResolveProductProcessPreviewIdentity(BizProductProcessConfig config)
    {
        var productNum = config.ProductNum?.Trim() ?? string.Empty;
        var productModel = _programOptions
            .FirstOrDefault(program => string.Equals(program.ProductNum?.Trim(), productNum, StringComparison.OrdinalIgnoreCase))
            ?.ProductModel?.Trim() ?? string.Empty;

        return new ProductProcessPreviewIdentity(config.StationNo, productNum, productModel);
    }

    private IReadOnlyList<ProductSchemePreviewItem> ResolveSchemeItems(string? schemeId)
    {
        var normalizedSchemeId = schemeId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedSchemeId))
        {
            return Array.Empty<ProductSchemePreviewItem>();
        }

        return _schemeDetails
            .Where(detail => string.Equals(detail.SchemeId?.Trim(), normalizedSchemeId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(detail => detail.DetailId)
            .Select(detail => new
            {
                Sort = detail.DetailId,
                Item = _testItems.FirstOrDefault(item => item.ItemId == detail.ItemId),
                Detail = detail
            })
            .Where(detail => detail.Item is not null)
            .Select(detail => new ProductSchemePreviewItem(detail.Sort, detail.Item!, detail.Detail))
            .ToList();
    }

    private void AddProductProcessAddressPreviewRow(
        ICollection<PlcAddressPreviewRow> rows,
        ProductProcessPreviewIdentity identity,
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

        var binding = ResolveProductProcessPreviewBinding(baseAddress, contextOffset, expression);
        rows.Add(new PlcAddressPreviewRow
        {
            Station = identity.StationText,
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

    private PlcExpressionBinding ResolveProductProcessPreviewBinding(string baseAddress, int contextOffset, string? expression)
    {
        if (_plcExpressionReadService.TryResolve(baseAddress, contextOffset, expression, out var binding, out _))
        {
            return binding;
        }

        var expressionText = expression?.Trim() ?? string.Empty;
        return new PlcExpressionBinding(expressionText, AppConstants.PlcDataTypes.Int16, 0, expressionText);
    }

    private static string ResolvePointName(BizProductProcessConfig config)
        => NormalizeNullableText(config.PointName) ?? "焊点";

    private static string ResolvePointNoHeader(BizProductProcessConfig config)
        => NormalizeNullableText(config.PointNoHeader) ?? $"{ResolvePointName(config)}序号";

    private static string ResolvePointResultHeader(BizProductProcessConfig config)
        => NormalizeNullableText(config.PointResultHeader) ?? $"{ResolvePointName(config)}结果";

    private static string ResolvePointCountHeader(BizProductProcessConfig config)
        => NormalizeNullableText(config.PointCountHeader) ?? $"{ResolvePointName(config)}数";

    private static string ResolveSchemeDetailHeader(BizSchemeDetail detail, DimTestItem item, SchemeDetailValueRole role)
    {
        return role switch
        {
            SchemeDetailValueRole.Actual => NormalizeNullableText(detail.ActualHeader) ?? $"{item.ItemName}实际值",
            SchemeDetailValueRole.Upper => NormalizeNullableText(detail.UpperHeader) ?? $"{item.ItemName}上限",
            SchemeDetailValueRole.Lower => NormalizeNullableText(detail.LowerHeader) ?? $"{item.ItemName}下限",
            SchemeDetailValueRole.Result => NormalizeNullableText(detail.ResultHeader) ?? $"{item.ItemName}结果",
            _ => item.ItemName
        };
    }

    private static string ResolveTouchNoBase(BizProductProcessConfig config)
        => string.IsNullOrWhiteSpace(config.TouchNoBase) ? config.TouchBase : config.TouchNoBase!.Trim();

    private static string ResolveTouchResultBase(BizProductProcessConfig config)
        => string.IsNullOrWhiteSpace(config.TouchResultBase) ? config.TouchBase : config.TouchResultBase!.Trim();

    private void AddProductProcess_Click(object? sender, EventArgs e)
    {
        EndTableEdit();
        var productNum = _programOptions
            .Select(program => program.ProductNum?.Trim())
            .FirstOrDefault(productNum => !string.IsNullOrWhiteSpace(productNum)) ?? string.Empty;
        var schemeId = _testSchemes.FirstOrDefault()?.SchemeId ?? "S01";

        var config = new BizProductProcessConfig
        {
            ProductNum = productNum,
            SchemeId = schemeId,
            StationNo = ProductionConstants.Stations.SharedStationNo,
            TouchCount = 1,
            PointName = "焊点",
            PointNoHeader = "焊点序号",
            PointResultHeader = "焊点结果",
            PointCountHeader = "焊点数",
            ShowTestFlagInHistory = true,
            ProductBase = "DB8.0",
            ProductLen = 32,
            ProductNoExpr = "0:I-0",
            ProductResultExpr = "4:H-4",
            TouchBase = "DB8.32",
            TouchNoBase = "DB8.32",
            TouchResultBase = "DB8.32",
            TouchHeaderLen = 16,
            TouchNoExpr = "0:I-0",
            TouchResultExpr = "4:H-4",
            TestBase = "DB8.100",
            TestAreaLen = 48,
            Enabled = true
        };

        _productProcessConfigs.Add(config);
        ApplyProductProcessFilter(_productProcessKeyword);
        _selectedProductProcessRow = _currentProductProcessRows.FirstOrDefault(row => ReferenceEquals(row.Source, config));
        if (_selectedProductProcessRow is not null)
        {
            tableProcess.SetSelected(_selectedProductProcessRow, true);
        }

        UpdateProductProcessSummary();
        SyncActiveCommandState();
    }

    private void DeleteProductProcess_Click(object? sender, EventArgs e)
    {
        EndTableEdit();
        var row = _selectedProductProcessRow;
        if (row is null)
        {
            ShowWarning("请先选择一条产品工艺配置。");
            return;
        }

        if (row.Id <= 0)
        {
            _productProcessConfigs.Remove(row.Source);
            ApplyProductProcessFilter(_productProcessKeyword);
            UpdateProductProcessSummary();
            SyncActiveCommandState();
            return;
        }

        if (!ConfirmDelete("确定删除选中的产品工艺配置吗？"))
        {
            return;
        }

        _productProcessConfigService.Delete(row.Id);
        LoadData();
    }

    private void AddScheme_Click(object? sender, EventArgs e)
    {
        EndTableEdit();
        var scheme = new BizTestScheme
        {
            SchemeId = BuildNextSchemeId(),
            SchemeName = "新测试方案"
        };

        _testSchemes.Add(scheme);
        ApplySchemeFilter(_schemeKeyword);
        ConfigureProductProcessColumns();
        treeSchemeDetails.Nodes.Clear();
        _currentSchemeDetailSchemeId = scheme.SchemeId;
        ConfigureSchemeDetailColumns();
    }

    private void DeleteScheme_Click(object? sender, EventArgs e)
    {
        EndTableEdit();
        var row = _selectedSchemeRow;
        if (row is null)
        {
            ShowWarning("请先选择一个测试方案。");
            return;
        }

        if (!ConfirmDelete("确定删除选中的测试方案吗？该方案下的明细也会一起删除。"))
        {
            return;
        }

        if (_testSchemeConfigService.GetSchemes().Any(scheme => scheme.SchemeId == row.SchemeId))
        {
            _testSchemeConfigService.DeleteScheme(row.SchemeId);
            LoadData();
            return;
        }

        _schemeDetails.RemoveAll(detail => string.Equals(detail.SchemeId, row.SchemeId, StringComparison.OrdinalIgnoreCase));
        _testSchemes.Remove(row.Source);
        if (SameScheme(_currentSchemeDetailSchemeId, row.SchemeId))
        {
            _currentSchemeDetailSchemeId = string.Empty;
        }

        ApplySchemeFilter(_schemeKeyword);
        ConfigureSchemeDetailColumns();
    }

    private void AddTestItem_Click(object? sender, EventArgs e)
    {
        EndTableEdit();
        var item = new DimTestItem
        {
            ItemName = "新测试项",
            Unit = string.Empty,
            ActualExpression = "0:H-0"
        };

        _temporaryTestItemIds[item] = BuildNextTemporaryTestItemId();
        _testItems.Add(item);
        ApplyItemFilter(_itemKeyword);
        ConfigureSchemeDetailColumns();
    }

    private void DeleteTestItem_Click(object? sender, EventArgs e)
    {
        EndTableEdit();
        var row = _selectedItemRow;
        if (row is null)
        {
            ShowWarning("请先选择一个测试项。");
            return;
        }

        if (!row.IsPersisted)
        {
            _temporaryTestItemIds.Remove(row.Source);
            _testItems.Remove(row.Source);
            ApplyItemFilter(_itemKeyword);
            ConfigureSchemeDetailColumns();
            return;
        }

        if (!ConfirmDelete("确定删除选中的测试项吗？引用该测试项的方案明细也会一起删除。"))
        {
            return;
        }

        _testSchemeConfigService.DeleteItem(row.PersistedItemId);
        LoadData();
    }

    private void Table_CellClick(object sender, AntdUI.TableClickEventArgs e)
    {
        switch (e.Record)
        {
            case PlcAddressTableRow row:
                _selectedRow = row;
                RegisterBusinessAddressTestClick(row);
                break;
            case ProductProcessTableRow row:
                ResetBusinessAddressTestClick();
                _selectedProductProcessRow = row;
                UpdateProductProcessSummary();
                SyncActiveCommandState();
                break;
            case TestSchemeTableRow row:
                ResetBusinessAddressTestClick();
                _selectedSchemeRow = row;
                break;
            case TestItemTableRow row:
                ResetBusinessAddressTestClick();
                _selectedItemRow = row;
                break;
            default:
                ResetBusinessAddressTestClick();
                break;
        }
    }

    private bool Table_CellEndEdit(object sender, AntdUI.TableEndEditEventArgs e)
    {
        var value = e.Value?.Trim() ?? string.Empty;
        return e.Record switch
        {
            PlcAddressTableRow => e.Column.Key switch
            {
                nameof(PlcAddressTableRow.Sort) => IsNonNegativeInt(value),
                nameof(PlcAddressTableRow.StationNo) => IsNonNegativeInt(value),
                nameof(PlcAddressTableRow.DataLength) => IsPositiveInt(value),
                _ => true
            },
            ProductProcessTableRow => e.Column.Key switch
            {
                nameof(ProductProcessTableRow.ProductNum) => !string.IsNullOrWhiteSpace(value),
                nameof(ProductProcessTableRow.SchemeId) => !string.IsNullOrWhiteSpace(value),
                nameof(ProductProcessTableRow.StationNo) => IsNonNegativeInt(value),
                nameof(ProductProcessTableRow.TouchCount) => IsPositiveInt(value),
                nameof(ProductProcessTableRow.PointName) => !string.IsNullOrWhiteSpace(value),
                nameof(ProductProcessTableRow.PointNoHeader) => !string.IsNullOrWhiteSpace(value),
                nameof(ProductProcessTableRow.PointResultHeader) => !string.IsNullOrWhiteSpace(value),
                nameof(ProductProcessTableRow.PointCountHeader) => !string.IsNullOrWhiteSpace(value),
                nameof(ProductProcessTableRow.ProductLen) => IsPositiveInt(value),
                nameof(ProductProcessTableRow.TouchHeaderLen) => IsPositiveInt(value),
                nameof(ProductProcessTableRow.TestAreaLen) => IsPositiveInt(value),
                _ => true
            },
            TestSchemeTableRow => e.Column.Key switch
            {
                nameof(TestSchemeTableRow.SchemeId) => !string.IsNullOrWhiteSpace(value),
                nameof(TestSchemeTableRow.SchemeName) => !string.IsNullOrWhiteSpace(value),
                _ => true
            },
            TestItemTableRow => e.Column.Key switch
            {
                nameof(TestItemTableRow.ItemName) => !string.IsNullOrWhiteSpace(value),
                nameof(TestItemTableRow.ActualExpression) => !string.IsNullOrWhiteSpace(value),
                _ => true
            },
            _ => true
        };
    }

    private bool Table_CellEndValueEdit(object sender, AntdUI.TableEndValueEditEventArgs e)
    {
        var value = GetSelectValueText(e.Value);
        switch (e.Record)
        {
            case PlcAddressTableRow addressRow when e.Column.Key == nameof(PlcAddressTableRow.DataType):
                addressRow.DataType = value;
                return AppConstants.PlcDataTypes.All.Contains(value);

            case ProductProcessTableRow processRow when e.Column.Key == nameof(ProductProcessTableRow.ProductNum):
                processRow.ProductNum = value;
                return !string.IsNullOrWhiteSpace(value);

            case ProductProcessTableRow processRow when e.Column.Key == nameof(ProductProcessTableRow.SchemeId):
                processRow.SchemeId = value;
                return !string.IsNullOrWhiteSpace(value);

            default:
                return true;
        }
    }

    private void Table_CheckedChanged(object sender, AntdUI.TableCheckEventArgs e)
    {
        if (e.Record is PlcAddressTableRow addressRow)
        {
            _selectedRow = addressRow;
            addressRow.Enabled = e.Value;
            return;
        }

        if (e.Record is ProductProcessTableRow processRow)
        {
            _selectedProductProcessRow = processRow;
            if (e.Column.Key == nameof(ProductProcessTableRow.ShowTestFlagInHistory))
            {
                processRow.ShowTestFlagInHistory = e.Value;
            }
            else
            {
                processRow.Enabled = e.Value;
            }

            UpdateProductProcessSummary();
            SyncActiveCommandState();
        }
    }

    private void Table_CellEditComplete(object sender, AntdUI.ITableEventArgs e)
    {
        switch (e.Record)
        {
            case PlcAddressTableRow row:
                _selectedRow = row;
                row.Normalize();
                tableAddresses.Refresh();
                break;
            case ProductProcessTableRow row:
                _selectedProductProcessRow = row;
                row.NormalizeForDisplay();
                tableProcess.Refresh();
                UpdateProductProcessSummary();
                SyncActiveCommandState();
                break;
            case TestSchemeTableRow row:
                _selectedSchemeRow = row;
                row.NormalizeForDisplay();
                tableTestSchemes.Refresh();
                ConfigureProductProcessColumns();
                ConfigureSchemeDetailColumns();
                break;
            case TestItemTableRow row:
                _selectedItemRow = row;
                row.NormalizeForDisplay();
                tableTestItems.Refresh();
                ConfigureSchemeDetailColumns();
                UpdateProductProcessSummary();
                break;
        }
    }

    private void EndTableEdit()
    {
        tableAddresses.EditModeClose();
        tableProcess.EditModeClose();
        tableTestSchemes.EditModeClose();
        tableTestItems.EditModeClose();
        _schemeDetailRoleGrid.EndEdit();
    }

    private void SelectVisibleRow(string? logicalKey, int? stationNo)
    {
        _selectedRow = _currentRows.FirstOrDefault(row => row.LogicalKey == logicalKey && row.StationNo == stationNo)
            ?? _currentRows.FirstOrDefault();

        if (_selectedRow is not null)
        {
            tableAddresses.SetSelected(_selectedRow, true);
        }
    }

    private void SelectVisibleProductProcessRow(int? selectedId)
    {
        _selectedProductProcessRow = _currentProductProcessRows.FirstOrDefault(row => row.Id == selectedId)
            ?? _currentProductProcessRows.FirstOrDefault();
        if (_selectedProductProcessRow is not null)
        {
            tableProcess.SetSelected(_selectedProductProcessRow, true);
        }

        UpdateProductProcessSummary();
        SyncActiveCommandState();
    }

    private void SelectVisibleSchemeRow(string? selectedId)
    {
        _selectedSchemeRow = _currentSchemeRows.FirstOrDefault(row => row.SchemeId == selectedId)
            ?? _currentSchemeRows.FirstOrDefault();
        if (_selectedSchemeRow is not null)
        {
            tableTestSchemes.SetSelected(_selectedSchemeRow, true);
        }
    }

    private void SelectVisibleItemRow(int? selectedId)
    {
        _selectedItemRow = _currentItemRows.FirstOrDefault(row => row.ItemId == selectedId)
            ?? _currentItemRows.FirstOrDefault();
        if (_selectedItemRow is not null)
        {
            tableTestItems.SetSelected(_selectedItemRow, true);
        }
    }

    private static void NormalizeAddresses(IEnumerable<BizPlcAddress> addresses)
    {
        foreach (var address in addresses)
        {
            address.Address = address.Address?.Trim();
            address.LogicalKey = address.LogicalKey.Trim();
            address.StationNo = IsBusinessAddressKey(address.LogicalKey)
                ? Math.Max(ProductionConstants.Stations.DefaultStationNo, address.StationNo)
                : Math.Max(ProductionConstants.Stations.SharedStationNo, address.StationNo);
            address.DataType = NormalizeDataType(address.DataType);
            address.DataLength = Math.Max(1, address.DataLength);
            address.Sort = Math.Max(0, address.Sort);
        }
    }

    private void NormalizeProductProcesses(IEnumerable<BizProductProcessConfig> configs)
    {
        foreach (var config in configs)
        {
            config.ProductNum = NormalizeRequiredText(config.ProductNum, "产品工号不能为空。");
            config.SchemeId = NormalizeRequiredText(config.SchemeId, "测试方案ID不能为空。");
            config.StationNo = Math.Max(ProductionConstants.Stations.SharedStationNo, config.StationNo);
            config.TouchCount = Math.Max(1, config.TouchCount);
            config.PointName = NormalizeRequiredText(config.PointName, "采集点名称不能为空。");
            config.PointNoHeader = NormalizeRequiredText(config.PointNoHeader, "采集点编号表头不能为空。");
            config.PointResultHeader = NormalizeRequiredText(config.PointResultHeader, "采集点结果表头不能为空。");
            config.PointCountHeader = NormalizeRequiredText(config.PointCountHeader, "采集点数量表头不能为空。");
            config.ShowTestFlagInHistory ??= true;
            config.ProductBase = NormalizeRequiredText(config.ProductBase, "产品头基地址不能为空。");
            config.ProductLen = Math.Max(1, config.ProductLen);
            config.ProductNoExpr = NormalizeRequiredText(config.ProductNoExpr, "产品编号偏移不能为空。");
            config.ProductResultExpr = NormalizeRequiredText(config.ProductResultExpr, "产品结果偏移不能为空。");
            config.ActualTouchCountExpr = NormalizeNullableText(config.ActualTouchCountExpr);
            config.PresetTouchCountExpr = NormalizeNullableText(config.PresetTouchCountExpr);
            config.TouchBase = NormalizeRequiredText(config.TouchBase, "焊点头基地址不能为空。");
            config.TouchNoBase = NormalizeNullableText(config.TouchNoBase) ?? config.TouchBase;
            config.TouchResultBase = NormalizeNullableText(config.TouchResultBase) ?? config.TouchBase;
            config.TouchHeaderLen = Math.Max(1, config.TouchHeaderLen);
            config.TouchNoExpr = NormalizeRequiredText(config.TouchNoExpr, "焊点编号偏移不能为空。");
            config.TouchResultExpr = NormalizeRequiredText(config.TouchResultExpr, "焊点结果偏移不能为空。");
            config.TestBase = NormalizeRequiredText(config.TestBase, "测试项基地址不能为空。");
            config.TestAreaLen = Math.Max(1, config.TestAreaLen);
        }
    }

    private void ValidateProductProcesses(IEnumerable<BizProductProcessConfig> configs)
    {
        var enabledConfigs = configs.Where(config => config.Enabled).ToList();
        var missingScheme = enabledConfigs.FirstOrDefault(config => !_testSchemes.Any(scheme => scheme.SchemeId == config.SchemeId));
        if (missingScheme is not null)
        {
            throw new InvalidOperationException($"产品工号“{missingScheme.ProductNum}”绑定的测试方案“{missingScheme.SchemeId}”不存在。");
        }

        var duplicate = enabledConfigs
            .GroupBy(config => $"{config.ProductNum}\u001F{config.StationNo}", StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            var first = duplicate.First();
            throw new InvalidOperationException($"产品工号“{first.ProductNum}”、工位“{first.StationNo}”存在重复启用配置。");
        }
    }

    private static void NormalizeSchemes(IEnumerable<BizTestScheme> schemes)
    {
        foreach (var scheme in schemes)
        {
            scheme.SchemeId = NormalizeRequiredText(scheme.SchemeId, "测试方案ID不能为空。");
            scheme.SchemeName = NormalizeRequiredText(scheme.SchemeName, "测试方案名称不能为空。");
            scheme.Description = NormalizeNullableText(scheme.Description);
        }
    }

    private static void ValidateSchemes(IEnumerable<BizTestScheme> schemes)
    {
        var duplicate = schemes
            .GroupBy(scheme => scheme.SchemeId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"测试方案ID“{duplicate.Key}”重复。");
        }
    }

    private void NormalizeSchemeDetails(IEnumerable<BizSchemeDetail> details)
    {
        foreach (var detail in details)
        {
            detail.SchemeId = NormalizeRequiredText(detail.SchemeId, "测试方案ID不能为空。");
            if (detail.ItemId <= 0)
            {
                throw new InvalidOperationException("方案明细必须选择测试项。");
            }

            if (!_testSchemes.Any(scheme => scheme.SchemeId == detail.SchemeId))
            {
                throw new InvalidOperationException($"测试方案“{detail.SchemeId}”不存在。");
            }

            var item = _testItems.FirstOrDefault(item => item.ItemId == detail.ItemId);
            if (item is null)
            {
                throw new InvalidOperationException($"测试项ID“{detail.ItemId}”不存在。");
            }

            detail.ActualHeader = NormalizeNullableText(detail.ActualHeader) ?? $"{item.ItemName}实际值";
            detail.UpperHeader = NormalizeNullableText(detail.UpperHeader) ?? $"{item.ItemName}上限";
            detail.LowerHeader = NormalizeNullableText(detail.LowerHeader) ?? $"{item.ItemName}下限";
            detail.ResultHeader = NormalizeNullableText(detail.ResultHeader) ?? $"{item.ItemName}结果";
            detail.ActualMesFieldName = NormalizeNullableText(detail.ActualMesFieldName);
            detail.UpperMesFieldName = NormalizeNullableText(detail.UpperMesFieldName);
            detail.LowerMesFieldName = NormalizeNullableText(detail.LowerMesFieldName);
            detail.ResultMesFieldName = NormalizeNullableText(detail.ResultMesFieldName);
            ValidateMesFieldName(detail.EnableActual, detail.MesActual, detail.ActualMesFieldName, item.ItemName, "实际值");
            ValidateMesFieldName(detail.EnableUpper, detail.MesUpper, detail.UpperMesFieldName, item.ItemName, "上限");
            ValidateMesFieldName(detail.EnableLower, detail.MesLower, detail.LowerMesFieldName, item.ItemName, "下限");
            ValidateMesFieldName(detail.EnableResult, detail.MesResult, detail.ResultMesFieldName, item.ItemName, "结果");

            if (!HasAnyEnabledRole(detail))
            {
                throw new InvalidOperationException("方案明细至少需要启用实际值、上限、下限或结果中的一项。");
            }
        }
    }

    private static void ValidateMesFieldName(
        bool collectEnabled,
        bool? mesEnabled,
        string? mesFieldName,
        string itemName,
        string roleName)
    {
        if (collectEnabled && mesEnabled == true && string.IsNullOrWhiteSpace(mesFieldName))
        {
            throw new InvalidOperationException($"{itemName}{roleName}已启用 MES 上传，必须填写 MES 字段名。");
        }
    }

    private static void ValidateSchemeDetails(IEnumerable<BizSchemeDetail> details)
    {
        var duplicate = details
            .GroupBy(detail => $"{detail.SchemeId}\u001F{detail.ItemId}", StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            var first = duplicate.First();
            throw new InvalidOperationException($"测试方案“{first.SchemeId}”中测试项“{first.ItemId}”重复。");
        }
        var duplicateMesField = details
            .SelectMany(EnumerateEnabledMesFields)
            .GroupBy(field => $"{field.SchemeId}\u001F{field.MesFieldName}", StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateMesField is not null)
        {
            var first = duplicateMesField.First();
            throw new InvalidOperationException($"测试方案“{first.SchemeId}”中 MES 字段名“{first.MesFieldName}”重复。");
        }
    }

    private static void NormalizeTestItems(IEnumerable<DimTestItem> items)
    {
        foreach (var item in items)
        {
            item.ItemName = NormalizeRequiredText(item.ItemName, "测试项名称不能为空。");
            item.Unit = NormalizeNullableText(item.Unit);
            item.ActualExpression = NormalizeRequiredText(item.ActualExpression, "实际值偏移不能为空。");
            item.UpperExpression = NormalizeNullableText(item.UpperExpression);
            item.LowerExpression = NormalizeNullableText(item.LowerExpression);
            item.ResultExpression = NormalizeNullableText(item.ResultExpression);
            ValidateOffsetExpression(item.ActualExpression, "实际值偏移");
            ValidateOffsetExpression(item.UpperExpression, "上限偏移");
            ValidateOffsetExpression(item.LowerExpression, "下限偏移");
            ValidateOffsetExpression(item.ResultExpression, "结果偏移");
        }
    }

    private static void ValidateOffsetExpression(string? expression, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return;
        }

        try
        {
            PlcOffsetExpression.Parse(expression);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{fieldName}无效：{ex.Message}", ex);
        }
    }

    private string BuildNextSchemeId()
    {
        var index = _testSchemes.Count + 1;
        while (_testSchemes.Any(scheme => string.Equals(scheme.SchemeId, $"S{index:00}", StringComparison.OrdinalIgnoreCase)))
        {
            index++;
        }

        return $"S{index:00}";
    }

    private int GetTestItemDisplayId(DimTestItem item)
    {
        if (item.ItemId > 0)
        {
            return item.ItemId;
        }

        if (_temporaryTestItemIds.TryGetValue(item, out var displayId))
        {
            return displayId;
        }

        displayId = BuildNextTemporaryTestItemId();
        _temporaryTestItemIds[item] = displayId;
        return displayId;
    }

    private int BuildNextTemporaryTestItemId()
    {
        var persistedMax = _testItems
            .Where(item => item.ItemId > 0)
            .Select(item => item.ItemId)
            .DefaultIfEmpty(0)
            .Max();
        var temporaryMax = _temporaryTestItemIds.Values
            .DefaultIfEmpty(0)
            .Max();
        return Math.Max(persistedMax, temporaryMax) + 1;
    }

    private string GetAddressDisplayName(BizPlcAddress address)
    {
        var key = address.LogicalKey switch
        {
            AppConstants.PlcLogicalKeys.PcHeartBeat => TextKeys.Address.NamePcHeartbeat,
            AppConstants.PlcLogicalKeys.PlcHeartBeat => TextKeys.Address.NamePlcHeartbeat,
            AppConstants.PlcLogicalKeys.DeviceStatus => TextKeys.Address.NameDeviceStatus,
            AppConstants.PlcLogicalKeys.WorkId => TextKeys.Address.NameWorkId,
            AppConstants.PlcLogicalKeys.PcRecipeCode => TextKeys.Address.NamePcRecipeCode,
            AppConstants.PlcLogicalKeys.PlcRecipeCode => TextKeys.Address.NamePlcRecipeCode,
            AppConstants.PlcLogicalKeys.WorkOrderStatus => TextKeys.Address.NameWorkOrderStatus,
            AppConstants.PlcLogicalKeys.DeviceMode => TextKeys.Address.NameDeviceMode,
            AppConstants.PlcLogicalKeys.ProductDataReady => TextKeys.Address.NameProductDataReady,
            AppConstants.PlcLogicalKeys.ProductCollectionFeedback => TextKeys.Address.NameProductCollectionFeedback,
            AppConstants.PlcLogicalKeys.TotalProduction => TextKeys.Address.NameTotalProduction,
            AppConstants.PlcLogicalKeys.AcceptedQuantity => TextKeys.Address.NameAcceptedQuantity,
            AppConstants.PlcLogicalKeys.RejectedQuantity => TextKeys.Address.NameRejectedQuantity,
            _ => string.Empty
        };

        return string.IsNullOrWhiteSpace(key)
            ? address.AddressName
            : _localizer.GetString(key);
    }

    private static bool IsBusinessAddressKey(string? logicalKey)
    {
        return logicalKey is AppConstants.PlcLogicalKeys.PcHeartBeat
            or AppConstants.PlcLogicalKeys.PlcHeartBeat
            or AppConstants.PlcLogicalKeys.DeviceStatus
            or AppConstants.PlcLogicalKeys.WorkId
            or AppConstants.PlcLogicalKeys.PcRecipeCode
            or AppConstants.PlcLogicalKeys.PlcRecipeCode
            or AppConstants.PlcLogicalKeys.WorkOrderStatus
            or AppConstants.PlcLogicalKeys.DeviceMode
            or AppConstants.PlcLogicalKeys.ProductDataReady
            or AppConstants.PlcLogicalKeys.ProductCollectionFeedback
            or AppConstants.PlcLogicalKeys.TotalProduction
            or AppConstants.PlcLogicalKeys.AcceptedQuantity
            or AppConstants.PlcLogicalKeys.RejectedQuantity;
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

    private static string NormalizeDataType(string? dataType)
    {
        return AppConstants.PlcDataTypes.All.Contains(dataType)
            ? dataType!
            : AppConstants.PlcDataTypes.Int16;
    }

    private static string NormalizeRequiredText(string? value, string message)
    {
        var normalizedValue = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            throw new InvalidOperationException(message);
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

    private static bool SameScheme(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasAnyEnabledRole(BizSchemeDetail detail)
    {
        return detail.EnableActual || detail.EnableUpper || detail.EnableLower || detail.EnableResult;
    }

    private static IEnumerable<SchemeMesField> EnumerateEnabledMesFields(BizSchemeDetail detail)
    {
        if (detail.EnableActual && detail.MesActual == true && !string.IsNullOrWhiteSpace(detail.ActualMesFieldName))
        {
            yield return new SchemeMesField(detail.SchemeId, detail.ActualMesFieldName.Trim());
        }

        if (detail.EnableUpper && detail.MesUpper == true && !string.IsNullOrWhiteSpace(detail.UpperMesFieldName))
        {
            yield return new SchemeMesField(detail.SchemeId, detail.UpperMesFieldName.Trim());
        }

        if (detail.EnableLower && detail.MesLower == true && !string.IsNullOrWhiteSpace(detail.LowerMesFieldName))
        {
            yield return new SchemeMesField(detail.SchemeId, detail.LowerMesFieldName.Trim());
        }

        if (detail.EnableResult && detail.MesResult == true && !string.IsNullOrWhiteSpace(detail.ResultMesFieldName))
        {
            yield return new SchemeMesField(detail.SchemeId, detail.ResultMesFieldName.Trim());
        }
    }

    private static string BuildSchemeDetailKey(string? schemeId, int itemId)
    {
        return $"{schemeId?.Trim() ?? string.Empty}\u001F{itemId}";
    }

    private static bool IsPositiveInt(string value)
    {
        return int.TryParse(value, out var number) && number > 0;
    }

    private static bool IsNonNegativeInt(string value)
    {
        return int.TryParse(value, out var number) && number >= 0;
    }

    private bool ConfirmDelete(string message)
    {
        return MessageBox.Show(
            this,
            message,
            _localizer.GetString(TextKeys.Common.TitleConfirmDelete),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes;
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
    /// 产品工艺地址预览使用的产品身份，避免预览逻辑直接依赖监控页的运行态对象。
    /// </summary>
    private sealed record ProductProcessPreviewIdentity(int StationNo, string ProductNum, string ProductModel)
    {
        public string StationText => StationNo == ProductionConstants.Stations.SharedStationNo
            ? "共享工位"
            : $"工位{StationNo}";
    }

    /// <summary>
    /// 产品工艺预览时使用的方案明细行，Sort 保留方案明细顺序。
    /// </summary>
    private sealed record ProductSchemePreviewItem(int Sort, DimTestItem Item, BizSchemeDetail Detail);

    private sealed record SchemeMesField(string SchemeId, string MesFieldName);

    private enum SchemeDetailValueRole
    {
        Actual,
        Upper,
        Lower,
        Result
    }

    private sealed record SchemeDetailTreeNodeTag(int ItemId, SchemeDetailValueRole? Role);

    /// <summary>
    /// 方案明细输出配置表格行。
    /// 同一个 BizSchemeDetail 会拆成四行，分别维护实际值、上限、下限和结果的输出配置。
    /// </summary>
    private sealed class SchemeDetailRoleTableRow(BizSchemeDetail source, DimTestItem item, SchemeDetailValueRole role)
    {
        public BizSchemeDetail Source { get; } = source;

        public int ItemId => item.ItemId;

        public string ItemName => item.ItemName;

        public SchemeDetailValueRole Role { get; } = role;

        public string RoleName => Role switch
        {
            SchemeDetailValueRole.Actual => "实际值",
            SchemeDetailValueRole.Upper => "上限",
            SchemeDetailValueRole.Lower => "下限",
            SchemeDetailValueRole.Result => "结果",
            _ => string.Empty
        };

        public bool Enabled
        {
            get => GetEnabled(Source, Role);
            set => SetEnabled(Source, Role, value);
        }

        public string HeaderText
        {
            get => ResolveSchemeDetailHeader(Source, item, Role);
            set => SetHeader(Source, Role, NormalizeNullableText(value) ?? ResolveDefaultHeader(item, Role));
        }

        public bool ReportEnabled
        {
            get => GetReportEnabled(Source, Role);
            set => SetReportEnabled(Source, Role, value);
        }

        public bool MesEnabled
        {
            get => GetMesEnabled(Source, Role);
            set => SetMesEnabled(Source, Role, value);
        }

        public string? MesFieldName
        {
            get => GetMesFieldName(Source, Role);
            set => SetMesFieldName(Source, Role, NormalizeNullableText(value));
        }

        public void NormalizeForSave()
        {
            HeaderText = HeaderText;
            MesFieldName = MesFieldName;
        }

        private static string ResolveDefaultHeader(DimTestItem item, SchemeDetailValueRole role)
        {
            return role switch
            {
                SchemeDetailValueRole.Actual => $"{item.ItemName}实际值",
                SchemeDetailValueRole.Upper => $"{item.ItemName}上限",
                SchemeDetailValueRole.Lower => $"{item.ItemName}下限",
                SchemeDetailValueRole.Result => $"{item.ItemName}结果",
                _ => item.ItemName
            };
        }

        private static bool GetEnabled(BizSchemeDetail detail, SchemeDetailValueRole role)
        {
            return role switch
            {
                SchemeDetailValueRole.Actual => detail.EnableActual,
                SchemeDetailValueRole.Upper => detail.EnableUpper,
                SchemeDetailValueRole.Lower => detail.EnableLower,
                SchemeDetailValueRole.Result => detail.EnableResult,
                _ => false
            };
        }

        private static void SetEnabled(BizSchemeDetail detail, SchemeDetailValueRole role, bool value)
        {
            switch (role)
            {
                case SchemeDetailValueRole.Actual:
                    detail.EnableActual = value;
                    break;
                case SchemeDetailValueRole.Upper:
                    detail.EnableUpper = value;
                    break;
                case SchemeDetailValueRole.Lower:
                    detail.EnableLower = value;
                    break;
                case SchemeDetailValueRole.Result:
                    detail.EnableResult = value;
                    break;
            }
        }

        private static void SetHeader(BizSchemeDetail detail, SchemeDetailValueRole role, string value)
        {
            switch (role)
            {
                case SchemeDetailValueRole.Actual:
                    detail.ActualHeader = value;
                    break;
                case SchemeDetailValueRole.Upper:
                    detail.UpperHeader = value;
                    break;
                case SchemeDetailValueRole.Lower:
                    detail.LowerHeader = value;
                    break;
                case SchemeDetailValueRole.Result:
                    detail.ResultHeader = value;
                    break;
            }
        }

        private static bool GetReportEnabled(BizSchemeDetail detail, SchemeDetailValueRole role)
        {
            return role switch
            {
                SchemeDetailValueRole.Actual => detail.ReportActual ?? detail.EnableActual,
                SchemeDetailValueRole.Upper => detail.ReportUpper ?? detail.EnableUpper,
                SchemeDetailValueRole.Lower => detail.ReportLower ?? detail.EnableLower,
                SchemeDetailValueRole.Result => detail.ReportResult ?? detail.EnableResult,
                _ => false
            };
        }

        private static void SetReportEnabled(BizSchemeDetail detail, SchemeDetailValueRole role, bool value)
        {
            switch (role)
            {
                case SchemeDetailValueRole.Actual:
                    detail.ReportActual = value;
                    break;
                case SchemeDetailValueRole.Upper:
                    detail.ReportUpper = value;
                    break;
                case SchemeDetailValueRole.Lower:
                    detail.ReportLower = value;
                    break;
                case SchemeDetailValueRole.Result:
                    detail.ReportResult = value;
                    break;
            }
        }

        private static bool GetMesEnabled(BizSchemeDetail detail, SchemeDetailValueRole role)
        {
            return role switch
            {
                SchemeDetailValueRole.Actual => detail.MesActual ?? false,
                SchemeDetailValueRole.Upper => detail.MesUpper ?? false,
                SchemeDetailValueRole.Lower => detail.MesLower ?? false,
                SchemeDetailValueRole.Result => detail.MesResult ?? false,
                _ => false
            };
        }

        private static void SetMesEnabled(BizSchemeDetail detail, SchemeDetailValueRole role, bool value)
        {
            switch (role)
            {
                case SchemeDetailValueRole.Actual:
                    detail.MesActual = value;
                    break;
                case SchemeDetailValueRole.Upper:
                    detail.MesUpper = value;
                    break;
                case SchemeDetailValueRole.Lower:
                    detail.MesLower = value;
                    break;
                case SchemeDetailValueRole.Result:
                    detail.MesResult = value;
                    break;
            }
        }

        private static string? GetMesFieldName(BizSchemeDetail detail, SchemeDetailValueRole role)
        {
            return role switch
            {
                SchemeDetailValueRole.Actual => detail.ActualMesFieldName,
                SchemeDetailValueRole.Upper => detail.UpperMesFieldName,
                SchemeDetailValueRole.Lower => detail.LowerMesFieldName,
                SchemeDetailValueRole.Result => detail.ResultMesFieldName,
                _ => null
            };
        }

        private static void SetMesFieldName(BizSchemeDetail detail, SchemeDetailValueRole role, string? value)
        {
            switch (role)
            {
                case SchemeDetailValueRole.Actual:
                    detail.ActualMesFieldName = value;
                    break;
                case SchemeDetailValueRole.Upper:
                    detail.UpperMesFieldName = value;
                    break;
                case SchemeDetailValueRole.Lower:
                    detail.LowerMesFieldName = value;
                    break;
                case SchemeDetailValueRole.Result:
                    detail.ResultMesFieldName = value;
                    break;
            }
        }
    }

    /// <summary>
    /// 业务信号地址表格行。表格编辑的是包装属性，保存时仍回写到原始地址实体。
    /// </summary>
    private sealed class PlcAddressTableRow(BizPlcAddress source, string addressName)
    {
        public BizPlcAddress Source { get; } = source;

        public string AddressName { get; } = addressName;

        public string LogicalKey
        {
            get => Source.LogicalKey;
            set => Source.LogicalKey = value.Trim();
        }

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
            Source.LogicalKey = Source.LogicalKey.Trim();
            Source.StationNo = IsBusinessAddressKey(Source.LogicalKey)
                ? Math.Max(ProductionConstants.Stations.DefaultStationNo, Source.StationNo)
                : Math.Max(ProductionConstants.Stations.SharedStationNo, Source.StationNo);
            Source.Address = NormalizeNullableText(Source.Address);
            Source.DataType = NormalizeDataType(Source.DataType);
            Source.DataLength = Math.Max(1, Source.DataLength);
            Source.Sort = Math.Max(0, Source.Sort);
            Source.Description = NormalizeNullableText(Source.Description);
        }
    }

    /// <summary>
    /// 产品工艺表格行。界面编辑包装属性，保存时回写原始实体。
    /// </summary>
    private sealed class ProductProcessTableRow(BizProductProcessConfig source)
    {
        public BizProductProcessConfig Source { get; } = source;

        public int Id => Source.Id;

        public string ProductNum
        {
            get => Source.ProductNum ?? string.Empty;
            set => Source.ProductNum = value.Trim();
        }

        public string SchemeId
        {
            get => Source.SchemeId;
            set => Source.SchemeId = value.Trim();
        }

        public int StationNo
        {
            get => Source.StationNo;
            set => Source.StationNo = Math.Max(ProductionConstants.Stations.SharedStationNo, value);
        }

        public int TouchCount
        {
            get => Source.TouchCount;
            set => Source.TouchCount = Math.Max(1, value);
        }

        public string PointName
        {
            get => Source.PointName;
            set => Source.PointName = value.Trim();
        }

        public string PointNoHeader
        {
            get => Source.PointNoHeader;
            set => Source.PointNoHeader = value.Trim();
        }

        public string PointResultHeader
        {
            get => Source.PointResultHeader;
            set => Source.PointResultHeader = value.Trim();
        }

        public string PointCountHeader
        {
            get => Source.PointCountHeader;
            set => Source.PointCountHeader = value.Trim();
        }

        public string ProductBase
        {
            get => Source.ProductBase;
            set => Source.ProductBase = value.Trim();
        }

        public int ProductLen
        {
            get => Source.ProductLen;
            set => Source.ProductLen = Math.Max(1, value);
        }

        public string ProductNoExpr
        {
            get => Source.ProductNoExpr;
            set => Source.ProductNoExpr = value.Trim();
        }

        public string ProductResultExpr
        {
            get => Source.ProductResultExpr;
            set => Source.ProductResultExpr = value.Trim();
        }

        public string? ActualTouchCountExpr
        {
            get => Source.ActualTouchCountExpr;
            set => Source.ActualTouchCountExpr = NormalizeNullableText(value);
        }

        public string? PresetTouchCountExpr
        {
            get => Source.PresetTouchCountExpr;
            set => Source.PresetTouchCountExpr = NormalizeNullableText(value);
        }

        public string TouchBase
        {
            get => Source.TouchBase;
            set => Source.TouchBase = value.Trim();
        }

        public string TouchNoBase
        {
            get => string.IsNullOrWhiteSpace(Source.TouchNoBase) ? Source.TouchBase : Source.TouchNoBase!;
            set => Source.TouchNoBase = value.Trim();
        }

        public string TouchResultBase
        {
            get => string.IsNullOrWhiteSpace(Source.TouchResultBase) ? Source.TouchBase : Source.TouchResultBase!;
            set => Source.TouchResultBase = value.Trim();
        }

        public int TouchHeaderLen
        {
            get => Source.TouchHeaderLen;
            set => Source.TouchHeaderLen = Math.Max(1, value);
        }

        public string TouchNoExpr
        {
            get => Source.TouchNoExpr;
            set => Source.TouchNoExpr = value.Trim();
        }

        public string TouchResultExpr
        {
            get => Source.TouchResultExpr;
            set => Source.TouchResultExpr = value.Trim();
        }

        public string TestBase
        {
            get => Source.TestBase;
            set => Source.TestBase = value.Trim();
        }

        public int TestAreaLen
        {
            get => Source.TestAreaLen;
            set => Source.TestAreaLen = Math.Max(1, value);
        }

        public bool Enabled
        {
            get => Source.Enabled;
            set => Source.Enabled = value;
        }

        public bool ShowTestFlagInHistory
        {
            get => Source.ShowTestFlagInHistory != false;
            set => Source.ShowTestFlagInHistory = value;
        }

        public DateTime UpdatedTime => Source.UpdatedTime;

        public void NormalizeForDisplay()
        {
            Source.ProductNum = Source.ProductNum?.Trim();
            Source.SchemeId = string.IsNullOrWhiteSpace(Source.SchemeId) ? "S01" : Source.SchemeId.Trim();
            Source.StationNo = Math.Max(ProductionConstants.Stations.SharedStationNo, Source.StationNo);
            Source.TouchCount = Math.Max(1, Source.TouchCount);
            Source.PointName = NormalizeNullableText(Source.PointName) ?? "焊点";
            Source.PointNoHeader = NormalizeNullableText(Source.PointNoHeader) ?? $"{Source.PointName}序号";
            Source.PointResultHeader = NormalizeNullableText(Source.PointResultHeader) ?? $"{Source.PointName}结果";
            Source.PointCountHeader = NormalizeNullableText(Source.PointCountHeader) ?? $"{Source.PointName}数";
            Source.ShowTestFlagInHistory ??= true;
            Source.ProductBase = Source.ProductBase.Trim();
            Source.ProductLen = Math.Max(1, Source.ProductLen);
            Source.ProductNoExpr = Source.ProductNoExpr.Trim();
            Source.ProductResultExpr = Source.ProductResultExpr.Trim();
            Source.ActualTouchCountExpr = NormalizeNullableText(Source.ActualTouchCountExpr);
            Source.PresetTouchCountExpr = NormalizeNullableText(Source.PresetTouchCountExpr);
            Source.TouchBase = Source.TouchBase.Trim();
            Source.TouchNoBase = NormalizeNullableText(Source.TouchNoBase) ?? Source.TouchBase;
            Source.TouchResultBase = NormalizeNullableText(Source.TouchResultBase) ?? Source.TouchBase;
            Source.TouchHeaderLen = Math.Max(1, Source.TouchHeaderLen);
            Source.TouchNoExpr = Source.TouchNoExpr.Trim();
            Source.TouchResultExpr = Source.TouchResultExpr.Trim();
            Source.TestBase = Source.TestBase.Trim();
            Source.TestAreaLen = Math.Max(1, Source.TestAreaLen);
        }
    }

    /// <summary>
    /// 测试方案表格行。
    /// </summary>
    private sealed class TestSchemeTableRow(BizTestScheme source)
    {
        public BizTestScheme Source { get; } = source;

        public string SchemeId
        {
            get => Source.SchemeId;
            set => Source.SchemeId = value.Trim();
        }

        public string SchemeName
        {
            get => Source.SchemeName;
            set => Source.SchemeName = value.Trim();
        }

        public string? Description
        {
            get => Source.Description;
            set => Source.Description = NormalizeNullableText(value);
        }

        public void NormalizeForDisplay()
        {
            Source.SchemeId = Source.SchemeId?.Trim() ?? string.Empty;
            Source.SchemeName = Source.SchemeName?.Trim() ?? string.Empty;
            Source.Description = NormalizeNullableText(Source.Description);
        }
    }

    /// <summary>
    /// 测试项字典表格行。
    /// </summary>
    private sealed class TestItemTableRow(DimTestItem source, int displayItemId)
    {
        public DimTestItem Source { get; } = source;

        public int ItemId { get; } = displayItemId;

        public int PersistedItemId => Source.ItemId;

        public bool IsPersisted => Source.ItemId > 0;

        public string ItemName
        {
            get => Source.ItemName;
            set => Source.ItemName = value.Trim();
        }

        public string? Unit
        {
            get => Source.Unit;
            set => Source.Unit = NormalizeNullableText(value);
        }

        public string ActualExpression
        {
            get => Source.ActualExpression;
            set => Source.ActualExpression = value.Trim();
        }

        public string? UpperExpression
        {
            get => Source.UpperExpression;
            set => Source.UpperExpression = NormalizeNullableText(value);
        }

        public string? LowerExpression
        {
            get => Source.LowerExpression;
            set => Source.LowerExpression = NormalizeNullableText(value);
        }

        public string? ResultExpression
        {
            get => Source.ResultExpression;
            set => Source.ResultExpression = NormalizeNullableText(value);
        }

        public void NormalizeForDisplay()
        {
            Source.ItemName = Source.ItemName?.Trim() ?? string.Empty;
            Source.Unit = NormalizeNullableText(Source.Unit);
            Source.ActualExpression = Source.ActualExpression?.Trim() ?? string.Empty;
            Source.UpperExpression = NormalizeNullableText(Source.UpperExpression);
            Source.LowerExpression = NormalizeNullableText(Source.LowerExpression);
            Source.ResultExpression = NormalizeNullableText(Source.ResultExpression);
        }
    }
}
