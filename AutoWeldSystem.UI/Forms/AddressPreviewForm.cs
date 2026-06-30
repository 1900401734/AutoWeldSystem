using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Plc;
using AutoWeldSystem.UI.Base;
using AutoWeldSystem.UI.Infrastructure;
using System.Globalization;
using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.UI.Forms;

/// <summary>
/// PLC 地址预览窗口。
/// 用于核对产品头、焊点头和测试项的最终 PLC 读取地址。
/// </summary>
public partial class AddressPreviewForm : BaseWindow
{
    // 三次点击比双击更难稳定完成，因此使用独立的调试触发窗口，不直接套用系统双击间隔。
    private const int TestAddressTripleClickIntervalMs = 1200;

    private readonly IReadOnlyList<PlcAddressPreviewRow> _rows;
    private readonly IPlcExpressionReadService _plcExpressionReadService;
    private readonly ILocalizationService _localizer;
    private readonly PlcWriteDebugLauncher _plcWriteDebugLauncher;
    private readonly System.Windows.Forms.ContextMenuStrip _previewContextMenu = new();
    private ToolStripMenuItem? _previewReadMenuItem;
    private ToolStripMenuItem? _previewWriteMenuItem;
    private string _keyword = string.Empty;
    private PlcAddressPreviewRow? _selectedRow;
    private PlcAddressPreviewRow? _lastPreviewClickRow;
    private long _lastPreviewClickTicks;
    private int _previewClickCount;

    public AddressPreviewForm(
        IReadOnlyList<PlcAddressPreviewRow> rows,
        IPlcExpressionReadService plcExpressionReadService,
        ILocalizationService localizer,
        PlcWriteDebugLauncher plcWriteDebugLauncher)
    {
        InitializeComponent();

        _rows = rows;
        _plcExpressionReadService = plcExpressionReadService;
        _localizer = localizer;
        _plcWriteDebugLauncher = plcWriteDebugLauncher;
        ConfigureTable();
        ConfigureContextMenu();
        BindRows();

        inputQuery.QueryClick += (_, keyword) => ApplyFilter(keyword);
        tableAddressPreview.CellClick += TableAddressPreview_CellClick;
        tableAddressPreview.MouseUp += TableAddressPreview_MouseUp;
        btnTestSelected.Click += TestSelected_Click;
        btnClose.Click += (_, _) => Close();
        CancelButton = btnClose;
    }

    /// <summary>
    /// 初始化地址预览表格右键菜单。
    /// 右键操作只针对当前选中的预览行，避免误操作到其它地址。
    /// </summary>
    private void ConfigureContextMenu()
    {
        _previewReadMenuItem = new ToolStripMenuItem("读取", null, PreviewReadMenu_Click);
        _previewWriteMenuItem = new ToolStripMenuItem("写入", null, PreviewWriteMenu_Click);
        _previewContextMenu.Items.AddRange(
        [
            _previewReadMenuItem,
            _previewWriteMenuItem
        ]);
    }

    private void TableAddressPreview_CellClick(object sender, AntdUI.TableClickEventArgs e)
    {
        _selectedRow = e.Record as PlcAddressPreviewRow;
        if (_selectedRow is null)
        {
            ResetPreviewTestClick();
            return;
        }

        RegisterPreviewTestClick(_selectedRow);
    }

    private void TableAddressPreview_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        var canOperate = HasUsableAddress(_selectedRow);
        if (_previewReadMenuItem is not null)
        {
            _previewReadMenuItem.Enabled = canOperate;
        }

        if (_previewWriteMenuItem is not null)
        {
            _previewWriteMenuItem.Enabled = canOperate;
        }

        _previewContextMenu.Show(tableAddressPreview, e.Location);
    }

    private void PreviewReadMenu_Click(object? sender, EventArgs e)
    {
        // 读取复用底部“测试选中地址”按钮，避免两套 PLC 读取提示格式。
        TestSelected_Click(sender, e);
    }

    private void PreviewWriteMenu_Click(object? sender, EventArgs e)
    {
        if (!TryGetSelectedPreviewRow(out var row))
        {
            return;
        }

        _plcWriteDebugLauncher.Show(
            this,
            new PlcWriteDebugPreset(row.ResolvedAddress, row.DataType));
    }

    private void RegisterPreviewTestClick(PlcAddressPreviewRow row)
    {
        var now = Environment.TickCount64;
        var isContinuousSameRow = ReferenceEquals(_lastPreviewClickRow, row)
            && now - _lastPreviewClickTicks <= TestAddressTripleClickIntervalMs;

        _previewClickCount = isContinuousSameRow
            ? _previewClickCount + 1
            : 1;
        _lastPreviewClickRow = row;
        _lastPreviewClickTicks = now;

        if (_previewClickCount < 3)
        {
            return;
        }

        ResetPreviewTestClick();
        btnTestSelected.PerformClick();
    }

    private void ResetPreviewTestClick()
    {
        _lastPreviewClickRow = null;
        _lastPreviewClickTicks = 0;
        _previewClickCount = 0;
    }

    private async void TestSelected_Click(object? sender, EventArgs e)
    {
        if (!TryGetSelectedPreviewRow(out var row))
        {
            return;
        }

        btnTestSelected.Enabled = false;
        try
        {
            var valueRole = GetValueRole(row);
            var binding = new PlcExpressionBinding(
                row.ResolvedAddress,
                row.DataType,
                row.Rule,
                row.Expression,
                row.DecimalPlaces);
            var result = await _plcExpressionReadService.ReadBindingTextAsync(binding, valueRole);

            if (result.IsSuccess)
            {
                ShowInfo($"字段：{valueRole}\r\n地址：{row.ResolvedAddress}\r\n读取值：{result.Value ?? string.Empty}");
                return;
            }

            ShowWarning($"字段：{valueRole}\r\n地址：{row.ResolvedAddress}\r\n失败原因：{result.Message}");
        }
        finally
        {
            btnTestSelected.Enabled = true;
        }
    }

    private bool TryGetSelectedPreviewRow(out PlcAddressPreviewRow row)
    {
        row = _selectedRow!;
        if (HasUsableAddress(row))
        {
            return true;
        }

        ShowWarning("请先选择一条有效地址。");
        return false;
    }

    private static bool HasUsableAddress(PlcAddressPreviewRow? row)
        => row is not null && !string.IsNullOrWhiteSpace(row.ResolvedAddress);

    private static string GetValueRole(PlcAddressPreviewRow row)
    {
        return string.IsNullOrWhiteSpace(row.ValueRole)
            ? "PLC地址"
            : row.ValueRole.Trim();
    }

    /// <summary>
    /// 绑定地址预览数据。
    /// 表格列属于窗口界面结构，集中放在 Designer 文件中配置。
    /// </summary>
    private void BindRows()
    {
        ApplyFilter(_keyword);
    }

    /// <summary>
    /// 按关键词过滤地址预览行，只影响当前弹窗中的显示数据。
    /// </summary>
    private void ApplyFilter(string? keyword)
    {
        _keyword = keyword?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_keyword) && !string.IsNullOrEmpty(inputQuery.Text))
        {
            inputQuery.Text = string.Empty;
        }

        _selectedRow = null;
        ResetPreviewTestClick();

        var filteredRows = string.IsNullOrWhiteSpace(_keyword)
            ? _rows.ToList()
            : _rows.Where(row => IsMatched(row, _keyword)).ToList();

        tableAddressPreview.DataSource = filteredRows;
        tableAddressPreview.Refresh();
    }

    /// <summary>
    /// 搜索范围覆盖预览表的主要可见列，便于按地址、字段、产品或焊点快速定位。
    /// </summary>
    private static bool IsMatched(PlcAddressPreviewRow row, string keyword)
    {
        return Contains(row.Station, keyword)
            || Contains(row.ProductNum, keyword)
            || Contains(row.ProductModel, keyword)
            || Contains(row.Category, keyword)
            || Contains(row.TouchNo, keyword)
            || Contains(row.ValueRole, keyword)
            || Contains(row.BaseAddress, keyword)
            || Contains(row.ContextOffset.ToString(CultureInfo.InvariantCulture), keyword)
            || Contains(row.Expression, keyword)
            || Contains(row.DataType, keyword)
            || Contains(row.Rule.ToString(CultureInfo.InvariantCulture), keyword)
            || Contains(row.DecimalPlaces?.ToString(), keyword)
            || Contains(row.ResolvedAddress, keyword);
    }

    private static bool Contains(string? value, string keyword)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private void ShowInfo(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleInfo), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(this, message, _localizer.GetString(TextKeys.Common.TitleWarning), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
