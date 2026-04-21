using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using LythenMarkdown.UI.ViewModels;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LythenMarkdown.UI;

public partial class App : Application
{
    private static IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 禁用绑定验证插件以减少噪音
            // BindingPlugins.DataValidators.RemoveAt(0);

            // 配置依赖注入
            ConfigureServices();

            // 创建主窗口
            var mainWindow = new MainWindow();
            var mainViewModel = GetService<MainViewModel>();
            
            if (mainViewModel != null)
            {
                mainWindow.DataContext = mainViewModel;
                _ = mainViewModel.InitializeAsync();
            }

            desktop.MainWindow = mainWindow;
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void ConfigureServices()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

        // Core Services (无 UI 依赖)
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IRenderService, RenderService>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<ITabService, TabService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IResourceCleanupService, ResourceCleanupService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<TabItemViewModel>();
        services.AddTransient<PreviewViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<WelcomeViewModel>();
        services.AddTransient<HelpViewModel>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public static T? GetService<T>() where T : class
    {
        if (_serviceProvider == null) return null;
        var svc = _serviceProvider.GetService(typeof(T));
        return svc as T;
    }

    public static T GetRequiredService<T>() where T : class
    {
        var service = GetService<T>();
        return service ?? throw new InvalidOperationException($"Service {typeof(T).Name} not registered");
    }
}
