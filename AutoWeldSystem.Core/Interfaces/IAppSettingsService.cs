using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Runtime;

namespace AutoWeldSystem.Core.Interfaces;

public interface IAppSettingsService
{
    event EventHandler<AppSettingsChangedEventArgs>? SettingsChanged;

    AppSettings Get();

    AppSettings Save(AppSettings settings);
}
