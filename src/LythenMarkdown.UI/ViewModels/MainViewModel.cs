using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;
using LythenMarkdown.UI.Views;

namespace LythenMarkdown.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly ITabService _tabService;
    private readonly ISessionService _sessionService;
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly IRenderService _renderService;

    [ObservableProperty]
    private ObservableCollection<TabItemViewModel> _tabs = new();

    /// <summary>
    /// 移动选项卡到新位置（支持拖拽排序）
    /// </summary>
    public void MoveTab(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Tabs.Count ||
            toIndex < 0 || toIndex >= Tabs.Count ||
            fromIndex == toIndex)
        {
            return;
        }

        var item = Tabs[fromIndex];
        Tabs.RemoveAt(fromIndex);
        Tabs.Insert(toIndex, item);
        SelectedTab = item;
    }

    [ObservableProperty]
    private TabItemViewModel? _selectedTab;

    partial void OnSelectedTabChanged(TabItemViewModel? oldValue, TabItemViewModel? newValue)
    {
        // 当切换选项卡时，触发事件通知 View 注册编辑器
        if (newValue != null)
        {
            EditorContentNeeded?.Invoke(this, newValue);
        }
    }

    /// <summary>
    /// 当需要加载编辑器内容时触发
    /// </summary>
    public event EventHandler<TabItemViewModel>? EditorContentNeeded;

    [ObservableProperty]
    private ViewMode _currentViewMode = ViewMode.Split;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private string _currentTabInfo = "";

    [ObservableProperty]
    private string _currentDocumentText = "";

    public string RenderedHtml => _renderService.Render(CurrentDocumentText);

    public string CurrentViewModeText => CurrentViewMode switch
    {
        ViewMode.Edit => "编辑模式",
        ViewMode.Split => "分栏模式",
        ViewMode.Preview => "预览模式",
        ViewMode.Popup => "弹窗模式",
        _ => ""
    };

    public bool IsEditModeVisible => CurrentViewMode == ViewMode.Edit;
    public bool IsSplitModeVisible => CurrentViewMode == ViewMode.Split;
    public bool IsPreviewModeVisible => CurrentViewMode == ViewMode.Preview;

    // 视图模式切换按钮状态
    public bool IsEditModeChecked => CurrentViewMode == ViewMode.Edit;
    public bool IsSplitModeChecked => CurrentViewMode == ViewMode.Split || CurrentViewMode == null;
    public bool IsPreviewModeChecked => CurrentViewMode == ViewMode.Preview;

    public static int[] HeadingLevels { get; } = { 1, 2, 3, 4, 5, 6 };

    public MainViewModel()
    {
        _fileService = App.GetRequiredService<IFileService>();
        _tabService = App.GetRequiredService<ITabService>();
        _sessionService = App.GetRequiredService<ISessionService>();
        _settingsService = App.GetRequiredService<ISettingsService>();
        _themeService = App.GetRequiredService<IThemeService>();
        _localizationService = App.GetRequiredService<ILocalizationService>();
        _renderService = App.GetRequiredService<IRenderService>();

        _tabService.TabClosed += OnTabClosed;
        _tabService.TabActivated += OnTabActivated;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            await _settingsService.LoadSettingsAsync();
            _themeService.SetTheme(_settingsService.CurrentSettings.Theme);
            _localizationService.SetLanguage(_settingsService.CurrentSettings.Language);

            // 创建新标签页
            await NewFileAsync();

            StatusMessage = "就绪";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NewFileAsync()
    {
        var document = new Document();
        var tab = new TabItemViewModel(this, document);
        Tabs.Add(tab);
        SelectedTab = tab;
        await _tabService.OpenTabAsync();
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "打开 Markdown 文件",
            Filters = new()
            {
                new FileDialogFilter
                {
                    Name = "Markdown 文件",
                    Extensions = new() { "md", "markdown" }
                },
                new FileDialogFilter
                {
                    Name = "所有文件",
                    Extensions = new() { "*" }
                }
            },
            AllowMultiple = false
        };

        var parent = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var result = await dialog.ShowAsync(parent);
        if (result != null && result.Length > 0)
        {
            await OpenFileAsync(result[0]);
        }
    }

    public async Task OpenFileAsync(string path)
    {
        var existingTab = _tabService.FindTabByPath(path);
        if (existingTab != null)
        {
            SelectedTab = Tabs.FirstOrDefault(t => t.Id == existingTab.Id);
            return;
        }

        try
        {
            StatusMessage = "正在打开文件...";
            var document = await _fileService.OpenFileAsync(path);
            var tab = new TabItemViewModel(this, document);
            Tabs.Add(tab);
            SelectedTab = tab;
            await _settingsService.AddRecentFileAsync(path);
            StatusMessage = $"已打开: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedTab == null) return;

        if (SelectedTab.Document.IsNew)
        {
            await SaveAsAsync();
            return;
        }

        try
        {
            StatusMessage = "正在保存...";
            await SelectedTab.SaveAsync();
            StatusMessage = $"已保存: {SelectedTab.Document.Title}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        if (SelectedTab == null) return;

        var dialog = new SaveFileDialog
        {
            Title = "另存为",
            Filters = new()
            {
                new FileDialogFilter
                {
                    Name = "Markdown 文件",
                    Extensions = new() { "md" }
                }
            },
            DefaultExtension = "md"
        };

        var parent = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var result = await dialog.ShowAsync(parent);
        if (!string.IsNullOrEmpty(result))
        {
            try
            {
                StatusMessage = "正在保存...";
                await SelectedTab.SaveAsAsync(result);
                StatusMessage = $"已保存: {Path.GetFileName(result)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task CloseTabAsync(TabItemViewModel? tab)
    {
        if (tab == null) return;

        if (tab.IsModified)
        {
            var parent = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (parent != null)
            {
                var result = await SaveChangesDialog.ShowAsync(parent, tab.Document.Title);
                
                switch (result)
                {
                    case SaveChangesResult.Save:
                        if (tab.Document.IsNew)
                        {
                            await SaveAsAsync();
                        }
                        else
                        {
                            await tab.SaveAsync();
                        }
                        break;
                    case SaveChangesResult.Cancel:
                        return;
                    case SaveChangesResult.Discard:
                        // 不保存，继续关闭
                        break;
                }
            }
        }

        var index = Tabs.IndexOf(tab);
        await _tabService.CloseTabAsync(tab.Id, force: true);
        Tabs.Remove(tab);

        if (Tabs.Count > 0)
        {
            SelectedTab = Tabs[Math.Min(index, Tabs.Count - 1)];
        }
        else
        {
            await NewFileAsync();
        }
    }

    [RelayCommand]
    private async Task CloseOtherTabsAsync(TabItemViewModel? keepTab)
    {
        if (keepTab == null) return;

        var tabsToClose = Tabs.Where(t => t != keepTab).ToList();
        foreach (var tab in tabsToClose)
        {
            if (tab.IsModified)
            {
                var parent = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                if (parent != null)
                {
                    var result = await SaveChangesDialog.ShowAsync(parent, tab.Document.Title);
                    
                    switch (result)
                    {
                        case SaveChangesResult.Save:
                            if (tab.Document.IsNew)
                            {
                                SelectedTab = tab;
                                await SaveAsAsync();
                            }
                            else
                            {
                                await tab.SaveAsync();
                            }
                            break;
                        case SaveChangesResult.Cancel:
                            return;
                        case SaveChangesResult.Discard:
                            break;
                    }
                }
            }
            
            await _tabService.CloseTabAsync(tab.Id, force: true);
            Tabs.Remove(tab);
        }
    }

    [RelayCommand]
    private async Task CloseAllTabsAsync()
    {
        var tabsToClose = Tabs.ToList();
        foreach (var tab in tabsToClose)
        {
            if (tab.IsModified)
            {
                var parent = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                if (parent != null)
                {
                    var result = await SaveChangesDialog.ShowAsync(parent, tab.Document.Title);
                    
                    switch (result)
                    {
                        case SaveChangesResult.Save:
                            if (tab.Document.IsNew)
                            {
                                SelectedTab = tab;
                                await SaveAsAsync();
                            }
                            else
                            {
                                await tab.SaveAsync();
                            }
                            break;
                        case SaveChangesResult.Cancel:
                            return;
                        case SaveChangesResult.Discard:
                            break;
                    }
                }
            }
            
            await _tabService.CloseTabAsync(tab.Id, force: true);
            Tabs.Remove(tab);
        }
        
        await NewFileAsync();
    }

    [RelayCommand]
    private void SetViewMode(ViewMode mode)
    {
        CurrentViewMode = mode;
        if (SelectedTab != null)
        {
            SelectedTab.ViewMode = mode;
        }
        OnPropertyChanged(nameof(IsEditModeVisible));
        OnPropertyChanged(nameof(IsSplitModeVisible));
        OnPropertyChanged(nameof(IsPreviewModeVisible));
        OnPropertyChanged(nameof(CurrentViewModeText));
        OnPropertyChanged(nameof(IsEditModeChecked));
        OnPropertyChanged(nameof(IsSplitModeChecked));
        OnPropertyChanged(nameof(IsPreviewModeChecked));
    }

    [RelayCommand]
    private void ShowPopupPreview()
    {
        // TODO: 实现弹窗预览
    }

    [RelayCommand]
    private void Undo()
    {
        // TODO: 实现撤销
    }

    [RelayCommand]
    private void Redo()
    {
        // TODO: 实现重做
    }

    [RelayCommand]
    private void Copy()
    {
        // TODO: 实现复制
    }

    [RelayCommand]
    private void Cut()
    {
        // TODO: 实现剪切
    }

    [RelayCommand]
    private void Paste()
    {
        // TODO: 实现粘贴
    }

    [RelayCommand]
    private void Exit()
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    [RelayCommand]
    private async Task ShowUserGuideAsync()
    {
        var parent = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (parent == null) return;

        var helpViewModel = new HelpViewModel();
        helpViewModel.LoadUserGuide();
        
        var helpWindow = new HelpWindow { DataContext = helpViewModel };
        await helpWindow.ShowDialog(parent);
    }

    [RelayCommand]
    private async Task ShowIntroductionAsync()
    {
        var parent = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (parent == null) return;

        var helpViewModel = new HelpViewModel();
        helpViewModel.LoadIntroduction();
        
        var helpWindow = new HelpWindow { DataContext = helpViewModel };
        await helpWindow.ShowDialog(parent);
    }

    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        var parent = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (parent == null) return;

        var aboutDialog = new AboutDialog();
        await aboutDialog.ShowDialog(parent);
    }

    public void OnTextChanged(string text)
    {
        if (SelectedTab != null)
        {
            SelectedTab.Document.Content = text;
            CurrentDocumentText = text;
            OnPropertyChanged(nameof(RenderedHtml));
        }
    }

    private void OnTabClosed(object? sender, string tabId)
    {
        var tab = Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            Tabs.Remove(tab);
        }
    }

    private void OnTabActivated(object? sender, string tabId)
    {
        var tab = Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            SelectedTab = tab;
        }
    }
}
