using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.UI.Infrastructure;
using AutoWeldSystem.UI.Views;

namespace AutoWeldSystem.UI.Forms;

public sealed class StationDisplayForm : Form
{
    private readonly MonitorView _monitorView;

    public StationDisplayForm(
        MonitorView monitorView,
        ILocalizationService localizer,
        PermissionUiBinder permissionUiBinder,
        int stationNo,
        bool readOnly)
    {
        _monitorView = monitorView;
        InitialStationNo = stationNo == 2 ? 2 : 1;
        Text = readOnly
            ? "扩展屏生产看板"
            : "扩展屏生产监控";
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

        Controls.Add(_monitorView);
        Width = 1280;
        Height = 900;
    }

    public void ApplyRuntimeSettingsChanged(AppSettings settings, bool readOnly)
    {
        Text = readOnly
            ? "扩展屏生产看板"
            : "扩展屏生产监控";
        _monitorView.ApplyRuntimeSettingsChanged(
            settings,
            readOnly,
            enableBusinessSignalReconcile: false,
            triggerBusinessSignalReconcile: false);
    }

    public int InitialStationNo { get; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _monitorView.Dispose();
        }

        base.Dispose(disposing);
    }
}
