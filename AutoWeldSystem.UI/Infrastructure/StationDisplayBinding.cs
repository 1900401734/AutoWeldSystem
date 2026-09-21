using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Runtime;
using System.Globalization;

namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// 为界面缓存工位显示配置并刷新文案，不触发业务重载或逐单元格数据库查询。
/// </summary>
internal sealed class StationDisplayBinding : IDisposable
{
    private readonly Control _owner;
    private readonly IAppSettingsService _settingsService;
    private readonly ILocalizationService _localizer;
    private readonly Action _refresh;
    private readonly ToolTip _toolTip = new() { ShowAlways = true };
    private AppSettings _settings;
    private bool _disposed;

    public StationDisplayBinding(Control owner, IAppSettingsService settingsService,
        ILocalizationService localizer, Action refresh)
    {
        _owner = owner;
        _settingsService = settingsService;
        _localizer = localizer;
        _refresh = refresh;
        _settings = settingsService.Get();
        _settingsService.SettingsChanged += OnSettingsChanged;
        _localizer.LanguageChanged += OnLanguageChanged;
        _owner.HandleCreated += OnHandleCreated;
        _owner.Disposed += OnDisposed;
    }

    public bool UsesMapping => Volatile.Read(ref _settings).EnableDualStation;

    public string Format(int stationNo, string fallback, bool includePhysicalNumber = false)
        => StationDisplayNameRules.FormatForDisplay(Volatile.Read(ref _settings), stationNo,
            _localizer, fallback, includePhysicalNumber);

    public string FormatTable(int stationNo, string fallback, bool includePhysicalNumber = false)
        => StationDisplayNameRules.FormatForTable(Volatile.Read(ref _settings), stationNo,
            _localizer, fallback, includePhysicalNumber);

    public string FormatValue(object? value, bool includePhysicalNumber = false)
    {
        var fallback = Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
        return int.TryParse(fallback, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stationNo)
            ? FormatTable(stationNo, fallback, includePhysicalNumber)
            : fallback;
    }

    public string FormatMessage(int stationNo, string message)
        => StationDisplayNameRules.FormatKnownMessage(Volatile.Read(ref _settings), stationNo, _localizer, message);

    public string FormatMessage(string message)
    {
        var first = FormatMessage(1, message);
        return first != message ? first : FormatMessage(2, message);
    }

    public void SetCaption(Control control, string text)
    {
        control.Text = text;
        SetToolTip(control, text);
    }

    public void SetToolTip(Control control, string text) => _toolTip.SetToolTip(control, text);

    public bool TryFormatCell(DataGridView grid, DataGridViewCellFormattingEventArgs e, bool includePhysicalNumber = false)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.ColumnIndex >= grid.Columns.Count)
            return false;
        var key = grid.Columns[e.ColumnIndex].DataPropertyName;
        if (key is not ("StationNo" or "Station"))
            return false;
        e.Value = FormatValue(e.Value, includePhysicalNumber);
        e.FormattingApplied = true;
        return true;
    }

    private void OnSettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        if (!e.HasChanged(nameof(AppSettings.EnableDualStation))
            && !e.HasChanged(nameof(AppSettings.Station1DisplayName))
            && !e.HasChanged(nameof(AppSettings.Station2DisplayName)))
            return;
        Volatile.Write(ref _settings, e.CurrentSettings);
        QueueRefresh();
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => QueueRefresh();
    private void OnHandleCreated(object? sender, EventArgs e) => QueueRefresh();
    private void OnDisposed(object? sender, EventArgs e) => Dispose();

    private void QueueRefresh()
    {
        if (_disposed || _owner.IsDisposed || _owner.Disposing || !_owner.IsHandleCreated)
            return;
        try
        {
            // 排到既有语言/设置事件之后，只刷新名称，避免整页重绑清空操作员输入。
            _owner.BeginInvoke((Action)(() =>
            {
                if (!_disposed && !_owner.IsDisposed && !_owner.Disposing)
                    UiThreadDispatcherProvider.Current.TryRun(_owner, _refresh, "StationDisplayBinding.Refresh");
            }));
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _settingsService.SettingsChanged -= OnSettingsChanged;
        _localizer.LanguageChanged -= OnLanguageChanged;
        _owner.HandleCreated -= OnHandleCreated;
        _owner.Disposed -= OnDisposed;
        _toolTip.Dispose();
    }
}
