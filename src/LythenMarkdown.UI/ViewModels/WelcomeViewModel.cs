using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LythenMarkdown.Core.Interfaces;

namespace LythenMarkdown.UI.ViewModels;

public partial class WelcomeViewModel : ObservableObject
{
    private readonly IInstallationService _installationService;
    private readonly IHelpService _helpService;

    [ObservableProperty]
    private string _appVersion = string.Empty;

    public WelcomeViewModel(
        IInstallationService installationService,
        IHelpService helpService)
    {
        _installationService = installationService;
        _helpService = helpService;

        var versionInfo = _installationService.GetVersionInfo();
        AppVersion = $"版本 {versionInfo.Version}";
    }

    [RelayCommand]
    private void ShowIntroduction()
    {
        _helpService.ShowIntroduction();
    }

    [RelayCommand]
    private void ShowUserGuide()
    {
        _helpService.ShowUserGuide();
    }

    [RelayCommand]
    private void StartUsing()
    {
        _installationService.MarkFirstRunCompleted();
    }
}
