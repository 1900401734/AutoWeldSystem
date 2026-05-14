using AutoWeldSystem.Core.Models;

namespace AutoWeldSystem.Core.Interfaces;

public interface IAppSettingsService
{
    AppSettings Get();

    AppSettings Save(AppSettings settings);
}
