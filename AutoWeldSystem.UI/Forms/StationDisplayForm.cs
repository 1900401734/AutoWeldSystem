using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.UI.Infrastructure;
using AutoWeldSystem.UI.Views;

namespace AutoWeldSystem.UI.Forms;

public sealed class StationDisplayForm : Form
{
    private readonly MonitorView _monitorView;
    private readonly ILocalizationService _localizer;
    private readonly StationDisplayBinding _stationDisplay;
    private bool _readOnly;

    public StationDisplayForm(MonitorView monitorView, ILocalizationService localizer, PermissionUiBinder permissionUiBinder, IAppSettingsService settingsService, int stationNo, bool readOnly)
    {
        _monitorView = monitorView;
        _localizer = localizer;
        InitialStationNo = stationNo == 2 ? 2 : 1;
        _readOnly = readOnly;
        _stationDisplay = new StationDisplayBinding(this, settingsService, localizer, RefreshStationTitle);
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = true;
        MinimizeBox = true;
        MaximizeBox = true;
        WindowState = FormWindowState.Normal;
        Icon = File.Exists(AppAssets.IconPath)
            ? new Icon(AppAssets.IconPath)
            : Icon;

        _monitorView.Dock = DockStyle.Fill;
        permissionUiBinder.Apply(_monitorView);
        _monitorView.ConfigureStationView(
            InitialStationNo,
            readOnly,
            enableBusinessSignalReconcile: false);
        _monitorView.ViewStationChanged += MonitorView_ViewStationChanged;
        RefreshStationTitle();

        Controls.Add(_monitorView);
        Width = 1280;
        Height = 900;
    }

    public void ApplyRuntimeSettingsChanged(AppSettings settings, bool readOnly)
    {
        _readOnly = readOnly;
        RefreshStationTitle();
        _monitorView.ApplyRuntimeSettingsChanged(
            settings,
            readOnly,
            enableBusinessSignalReconcile: false,
            triggerBusinessSignalReconcile: false);
    }

    private void RefreshStationTitle()
    {
        var title = _localizer.GetString(_readOnly ? TextKeys.Common.ExtendedDashboardTitle : TextKeys.Common.ExtendedMonitorTitle);
        Text = _stationDisplay.UsesMapping ? $"{_stationDisplay.Format(_monitorView.ViewStationNo, string.Empty)} — {title}" : title;
    }

    private void MonitorView_ViewStationChanged(object? sender, EventArgs e) => RefreshStationTitle();

    public int InitialStationNo { get; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _monitorView.ViewStationChanged -= MonitorView_ViewStationChanged;
            _monitorView.Dispose();
        }

        base.Dispose(disposing);
    }
}
