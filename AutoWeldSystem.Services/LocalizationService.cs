using System.Globalization;
using System.Resources;
using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Runtime;

namespace AutoWeldSystem.Services;

/// <summary>
/// 统一管理界面文本读取、语言切换和语言设置持久化。
/// </summary>
public class LocalizationService : ILocalizationService
{
    private static readonly ResourceManager ResourceManager =
        new("AutoWeldSystem.Core.Localization.UiText", typeof(GlobalContext).Assembly);

    private readonly IAppSettingsService _settingsService;
    private readonly object _persistLock = new();
    private int _persistVersion;
    private AppSettings _currentSettings;

    public LocalizationService(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += OnSettingsChanged;

        CurrentLanguage = NormalizeLanguage(_currentSettings.Language);
        GlobalContext.SetLanguage(CurrentLanguage);
    }

    public string CurrentLanguage { get; private set; }

    public event EventHandler? LanguageChanged;

    public string GetString(string key)
    {
        var culture = new CultureInfo(CurrentLanguage);
        return ResourceManager.GetString(key, culture) ?? key;
    }

    public string GetString(string key, params object[] args)
    {
        var template = GetString(key);
        return args.Length == 0
            ? template
            : string.Format(new CultureInfo(CurrentLanguage), template, args);
    }

    public void SetLanguage(string cultureCode)
    {
        var targetLanguage = NormalizeLanguage(cultureCode);
        if (string.Equals(CurrentLanguage, targetLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CurrentLanguage = targetLanguage;
        GlobalContext.SetLanguage(targetLanguage);
        LanguageChanged?.Invoke(this, EventArgs.Empty);

        QueueLanguagePersist(targetLanguage);
    }

    private void QueueLanguagePersist(string targetLanguage)
    {
        var version = Interlocked.Increment(ref _persistVersion);

        _ = Task.Run(() =>
        {
            try
            {
                // 快速连续切换语言时，只保存最后一次选择，避免 UI 线程等待数据库。
                Thread.Sleep(150);
                if (version != Volatile.Read(ref _persistVersion))
                {
                    return;
                }

                lock (_persistLock)
                {
                    var settings = CurrentSettings.Clone();
                    if (string.Equals(settings.Language, targetLanguage, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    settings.Language = targetLanguage;
                    _settingsService.Save(settings);
                }
            }
            catch
            {
                // 持久化失败不影响本次界面切换；下次启动仍会读取数据库中的旧值。
            }
        });
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void OnSettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        var settings = e.CurrentSettings;
        Interlocked.Exchange(ref _currentSettings, settings);

        if (!e.HasChanged(nameof(AppSettings.Language)))
        {
            return;
        }

        var language = NormalizeLanguage(settings.Language);
        if (string.Equals(CurrentLanguage, language, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CurrentLanguage = language;
        GlobalContext.SetLanguage(language);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string NormalizeLanguage(string? cultureCode)
    {
        return string.Equals(cultureCode, AppConstants.Languages.English, StringComparison.OrdinalIgnoreCase)
            ? AppConstants.Languages.English
            : AppConstants.Languages.Chinese;
    }
}
