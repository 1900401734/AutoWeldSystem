using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Core.Interfaces.Log;

/// <summary>
/// Independent local device lifecycle log service.
/// It writes JSONL files and exposes events so the log page can refresh in real time.
/// </summary>
public interface IDeviceLifecycleLogService
{
    /// <summary>
    /// Raised after one device lifecycle log entry has been written.
    /// </summary>
    event EventHandler<DeviceLifecycleLogEntry>? LogWritten;

    /// <summary>
    /// Writes one lifecycle entry to the local JSONL file.
    /// </summary>
    void Write(DeviceLifecycleLogEntry entry);

    /// <summary>
    /// Reads lifecycle log entries for the selected date.
    /// </summary>
    IReadOnlyList<DeviceLifecycleLogEntry> GetByDate(DateTime date, int take = 1000);

    /// <summary>
    /// Gets the directory that contains device lifecycle logs.
    /// </summary>
    string GetLogDirectory();
}
