using AutoWeldSystem.Core.Entities;

namespace AutoWeldSystem.Core.Runtime;

public sealed class AppSettingsChangedEventArgs : EventArgs
{
    private readonly AppSettings _previousSettings;
    private readonly AppSettings _currentSettings;
    private readonly HashSet<string> _changedPropertySet;

    public AppSettingsChangedEventArgs(
        AppSettings previousSettings,
        AppSettings currentSettings,
        IEnumerable<string> changedProperties)
    {
        ArgumentNullException.ThrowIfNull(previousSettings);
        ArgumentNullException.ThrowIfNull(currentSettings);
        ArgumentNullException.ThrowIfNull(changedProperties);

        _previousSettings = previousSettings.Clone();
        _currentSettings = currentSettings.Clone();
        _changedPropertySet = new HashSet<string>(changedProperties, StringComparer.Ordinal);
        ChangedProperties = Array.AsReadOnly(_changedPropertySet.OrderBy(name => name, StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// Gets an independent snapshot of the settings before persistence.
    /// </summary>
    public AppSettings PreviousSettings => _previousSettings.Clone();

    /// <summary>
    /// Gets an independent snapshot of the persisted settings.
    /// </summary>
    public AppSettings CurrentSettings => _currentSettings.Clone();

    /// <summary>
    /// Gets the business properties whose values changed. Id and UpdatedTime are excluded.
    /// </summary>
    public IReadOnlyList<string> ChangedProperties { get; }

    public bool HasChanged(string propertyName)
    {
        return !string.IsNullOrWhiteSpace(propertyName) && _changedPropertySet.Contains(propertyName);
    }
}
