using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private ThemeMode _selectedTheme;

    [ObservableProperty]
    private string _selectedLanguage = "zh-CN";

    [ObservableProperty]
    private double _fontSize = 14;

    [ObservableProperty]
    private string _fontFamily = "Consolas, monospace";

    [ObservableProperty]
    private bool _showLineNumbers = true;

    [ObservableProperty]
    private bool _enableSyncScroll = true;

    [ObservableProperty]
    private bool _enableAutoSave = true;

    [ObservableProperty]
    private int _autoSaveInterval = 30;

    public SettingsViewModel()
    {
        _settingsService = App.GetRequiredService<ISettingsService>();
        _themeService = App.GetRequiredService<IThemeService>();
        _localizationService = App.GetRequiredService<ILocalizationService>();
    }

    public void LoadSettings()
    {
        var settings = _settingsService.CurrentSettings;
        SelectedTheme = settings.Theme;
        SelectedLanguage = settings.Language;
        FontSize = settings.FontSize;
        FontFamily = settings.FontFamily;
        ShowLineNumbers = settings.ShowLineNumbers;
        EnableSyncScroll = settings.EnableSyncScroll;
        EnableAutoSave = settings.EnableAutoSave;
        AutoSaveInterval = settings.AutoSaveIntervalSeconds;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var settings = new AppSettings
        {
            Theme = SelectedTheme,
            Language = SelectedLanguage,
            FontSize = FontSize,
            FontFamily = FontFamily,
            ShowLineNumbers = ShowLineNumbers,
            EnableSyncScroll = EnableSyncScroll,
            EnableAutoSave = EnableAutoSave,
            AutoSaveIntervalSeconds = AutoSaveInterval
        };

        await _settingsService.SaveSettingsAsync(settings);
        _themeService.SetTheme(SelectedTheme);
        _localizationService.SetLanguage(SelectedLanguage);
    }

    [RelayCommand]
    private void Cancel()
    {
        LoadSettings();
    }
}
