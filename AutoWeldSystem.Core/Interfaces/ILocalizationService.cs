namespace AutoWeldSystem.Core.Interfaces;

public interface ILocalizationService
{
    string CurrentLanguage { get; }

    event EventHandler? LanguageChanged;

    string GetString(string key);

    string GetString(string key, params object[] args);

    void SetLanguage(string cultureCode);
}
