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
    /// 是否有标签页
    /// </summary>
    public bool HasTabs => Tabs.Count > 0;

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
        System.Diagnostics.Debug.WriteLine($"[OnSelectedTabChanged] old={oldValue?.Title ?? "null"}, new={newValue?.Title ?? "null"}");
        
        // 当切换选项卡时，触发事件通知 View 注册编辑器
        if (newValue != null)
        {
            EditorContentNeeded?.Invoke(this, newValue);
            
            // 同步 CurrentViewMode 为当前标签页的视图模式
            if (oldValue != null && oldValue.ViewMode != newValue.ViewMode)
            {
                CurrentViewMode = newValue.ViewMode;
                OnPropertyChanged(nameof(IsEditModeVisible));
                OnPropertyChanged(nameof(IsSplitModeVisible));
                OnPropertyChanged(nameof(IsPreviewModeVisible));
                OnPropertyChanged(nameof(CurrentViewModeText));
                OnPropertyChanged(nameof(IsEditModeChecked));
                OnPropertyChanged(nameof(IsSplitModeChecked));
                OnPropertyChanged(nameof(IsPreviewModeChecked));
            }
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
        System.Diagnostics.Debug.WriteLine("[Session] InitializeAsync 开始...");
        IsLoading = true;
        try
        {
            await _settingsService.LoadSettingsAsync();
            _themeService.SetTheme(_settingsService.CurrentSettings.Theme);
            _localizationService.SetLanguage(_settingsService.CurrentSettings.Language);

            // 尝试恢复会话
            await RestoreSessionAsync();
            System.Diagnostics.Debug.WriteLine($"[Session] 恢复完成，当前标签数: {Tabs.Count}, SelectedTab: {SelectedTab?.Title ?? "null"}");

            // 如果没有恢复任何标签页（会话为空或加载失败），创建新标签页
            if (Tabs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[Session] 无会话，创建新标签");
                await NewFileAsync();
                System.Diagnostics.Debug.WriteLine($"[Session] 创建新标签后，SelectedTab: {SelectedTab?.Title ?? "null"}");
            }

            // 启动自动保存（每30秒）
            _sessionService.SetSessionDataProvider(() => BuildSessionData());
            _sessionService.StartAutoSave(30);

            StatusMessage = "就绪";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 从会话恢复标签页
    /// </summary>
    private async Task RestoreSessionAsync()
    {
        System.Diagnostics.Debug.WriteLine("[Session] 开始恢复会话...");
        try
        {
            var session = await _sessionService.LoadSessionAsync();
            System.Diagnostics.Debug.WriteLine($"[Session] 加载会话: {(session == null ? "null" : $"{session.OpenTabs.Count} 个标签")}");
            
            if (session == null || session.OpenTabs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[Session] 会话为空，跳过恢复");
                return;
            }

            // 记录要恢复的视图模式（标签ID -> 视图模式）
            var viewModesToRestore = session.OpenTabs.ToDictionary(t => t.Id, t => t.ViewMode);

            var restoredCount = 0;
            foreach (var tabSession in session.OpenTabs)
            {
                System.Diagnostics.Debug.WriteLine($"[Session] 处理标签: {tabSession.Id}, 文件: {tabSession.FilePath}");
                
                // 如果有文件路径，尝试打开文件
                if (!string.IsNullOrEmpty(tabSession.FilePath))
                {
                    // 检查文件是否存在
                    if (!File.Exists(tabSession.FilePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"[Session] 文件不存在，跳过: {tabSession.FilePath}");
                        // 文件已删除，跳过
                        continue;
                    }

                    try
                    {
                        // 优先使用会话中保存的内容（可能有未保存的编辑）
                        // 如果会话内容为空，才从文件读取
                        var content = !string.IsNullOrEmpty(tabSession.Content) 
                            ? tabSession.Content 
                            : await File.ReadAllTextAsync(tabSession.FilePath);
                        
                        // 原始内容优先使用会话中保存的值（支持连续编辑场景）
                        // 如果会话中没有保存 OriginalContent，才从磁盘读取
                        var originalContent = !string.IsNullOrEmpty(tabSession.OriginalContent) 
                            ? tabSession.OriginalContent 
                            : await File.ReadAllTextAsync(tabSession.FilePath);
                        
                        var fileInfo = new FileInfo(tabSession.FilePath);
                        var document = new Document
                        {
                            Id = tabSession.Id,  // 使用保存的 Id
                            FilePath = tabSession.FilePath,
                            Content = content,
                            OriginalContent = originalContent,
                            ModifiedAt = fileInfo.LastWriteTime,
                            IsReadOnly = fileInfo.IsReadOnly,
                            CursorPosition = tabSession.CursorPosition,
                            ScrollOffsetY = tabSession.ScrollOffsetY
                        };
                        var tab = new TabItemViewModel(this, document);
                        Tabs.Add(tab);
                        restoredCount++;
                        System.Diagnostics.Debug.WriteLine($"[Session] 成功恢复文件: {tabSession.FilePath}, Content长度={content.Length}, OriginalContent长度={originalContent.Length}, IsModified={document.IsModified}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Session] 打开文件失败: {ex.Message}");
                        // 打开失败，跳过
                        continue;
                    }
                }
                else
                {
                    // 新建文档，恢复内容
                    var document = new Document
                    {
                        Id = tabSession.Id,  // 使用保存的 Id
                        Content = tabSession.Content,
                        OriginalContent = tabSession.OriginalContent,
                        CursorPosition = tabSession.CursorPosition,
                        ScrollOffsetY = tabSession.ScrollOffsetY
                    };
                    var tab = new TabItemViewModel(this, document);
                    Tabs.Add(tab);
                    restoredCount++;
                    System.Diagnostics.Debug.WriteLine($"[Session] 成功恢复新建文档: {tabSession.Id}, Content长度={tabSession.Content?.Length ?? 0}");
                }
            }

            // 恢复每个标签页的视图模式，并标记编辑状态
            foreach (var tab in Tabs)
            {
                if (viewModesToRestore.TryGetValue(tab.Id, out var viewMode))
                {
                    tab.ViewMode = viewMode;
                    System.Diagnostics.Debug.WriteLine($"[Session] 恢复标签 {tab.Title} 的视图模式: {viewMode}");
                }
                
                // 如果文档内容与原始内容不同，标记为编辑状态
                if (tab.Document.IsModified)
                {
                    tab.MarkAsEdited();
                    System.Diagnostics.Debug.WriteLine($"[Session] 标记标签 {tab.Title} 为编辑状态");
                }
            }

            // 恢复当前激活的标签
            if (!string.IsNullOrEmpty(session.ActiveTabId))
            {
                System.Diagnostics.Debug.WriteLine($"[Session] 查找激活标签: {session.ActiveTabId}");
                var activeTab = Tabs.FirstOrDefault(t => t.Id == session.ActiveTabId);
                System.Diagnostics.Debug.WriteLine($"[Session] 找到激活标签: {activeTab?.Title ?? "null"}");
                if (activeTab != null)
                {
                    SelectedTab = activeTab;
                    System.Diagnostics.Debug.WriteLine($"[Session] 已设置激活标签: {activeTab.Title}");
                }
            }

            // 恢复视图模式：使用活动标签页自身的视图模式，而不是 LastViewMode
            // 这样每个标签页都能恢复自己的视图模式
            if (SelectedTab != null)
            {
                var activeTabViewMode = SelectedTab.ViewMode;
                CurrentViewMode = activeTabViewMode;
                OnPropertyChanged(nameof(IsEditModeVisible));
                OnPropertyChanged(nameof(IsSplitModeVisible));
                OnPropertyChanged(nameof(IsPreviewModeVisible));
                OnPropertyChanged(nameof(CurrentViewModeText));
                OnPropertyChanged(nameof(IsEditModeChecked));
                OnPropertyChanged(nameof(IsSplitModeChecked));
                OnPropertyChanged(nameof(IsPreviewModeChecked));
                System.Diagnostics.Debug.WriteLine($"[Session] 恢复活动标签 {SelectedTab.Title} 的视图模式: {activeTabViewMode}");
            }

            if (restoredCount > 0)
            {
                StatusMessage = $"已恢复 {restoredCount} 个标签页";
                System.Diagnostics.Debug.WriteLine($"[Session] 恢复完成，最终标签数: {Tabs.Count}, 激活标签: {SelectedTab?.Title}");
                // 通知 HasTabs 属性变化
                OnPropertyChanged(nameof(HasTabs));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"恢复会话失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 构建会话数据（用于自动保存）
    /// </summary>
    private SessionData BuildSessionData()
    {
        System.Diagnostics.Debug.WriteLine($"[BuildSessionData] SelectedTab = {SelectedTab?.Title ?? "null"}, Id = {SelectedTab?.Id}");
        System.Diagnostics.Debug.WriteLine($"[BuildSessionData] 当前标签数: {Tabs.Count}");
        
        var session = new SessionData
        {
            ActiveTabId = SelectedTab?.Id,
            LastViewMode = CurrentViewMode
        };

        foreach (var tab in Tabs)
        {
            // 确保 Document.Content 与 SharedDocument.Text 同步（窗口关闭前可能没有触发 TextChanged）
            tab.Document.Content = tab.SharedDocument.Text;
            
            var tabSession = new TabSessionData
            {
                Id = tab.Id,
                FilePath = tab.Document.FilePath,
                Content = tab.Document.Content,
                OriginalContent = tab.Document.OriginalContent,
                CursorPosition = tab.Document.CursorPosition,
                ScrollOffsetY = tab.Document.ScrollOffsetY,
                ViewMode = tab.ViewMode
            };
            session.OpenTabs.Add(tabSession);
            System.Diagnostics.Debug.WriteLine($"[BuildSessionData] 标签: Id={tab.Id}, Title={tab.Title}, Content长度={tab.Document.Content?.Length ?? 0}, SharedDocument.Text长度={tab.SharedDocument.Text?.Length ?? 0}");
        }

        System.Diagnostics.Debug.WriteLine($"[BuildSessionData] 返回 session: {session.OpenTabs.Count} 个标签");
        return session;
    }

    /// <summary>
    /// 保存当前会话
    /// </summary>
    public async Task SaveSessionAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[SaveSessionAsync] 开始保存会话，当前标签数: {Tabs.Count}");
            var session = BuildSessionData();
            System.Diagnostics.Debug.WriteLine($"[SaveSessionAsync] BuildSessionData 完成，标签数: {session.OpenTabs.Count}");
            foreach (var tab in session.OpenTabs)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveSessionAsync] 标签 {tab.Id}: Content长度={tab.Content?.Length ?? 0}");
            }
            await _sessionService.SaveSessionAsync(session);
            System.Diagnostics.Debug.WriteLine($"[Session] 已保存会话，包含 {session.OpenTabs.Count} 个标签页");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SaveSessionAsync] 保存会话失败: {ex.Message}, StackTrace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 获取会话服务（用于调试）
    /// </summary>
    public ISessionService GetSessionService() => _sessionService;

    /// <summary>
    /// 同步构建会话数据（用于窗口关闭时）
    /// </summary>
    public SessionData BuildSessionDataSync()
    {
        System.Diagnostics.Debug.WriteLine($"[BuildSessionDataSync] SelectedTab = {SelectedTab?.Title ?? "null"}, Id = {SelectedTab?.Id}");
        System.Diagnostics.Debug.WriteLine($"[BuildSessionDataSync] 当前标签数: {Tabs.Count}");
        
        var session = new SessionData
        {
            ActiveTabId = SelectedTab?.Id,
            LastViewMode = CurrentViewMode
        };

        foreach (var tab in Tabs)
        {
            // 确保 Document.Content 与 SharedDocument.Text 同步
            tab.Document.Content = tab.SharedDocument.Text;
            
            var tabSession = new TabSessionData
            {
                Id = tab.Id,
                FilePath = tab.Document.FilePath,
                Content = tab.Document.Content,
                OriginalContent = tab.Document.OriginalContent,
                CursorPosition = tab.Document.CursorPosition,
                ScrollOffsetY = tab.Document.ScrollOffsetY,
                ViewMode = tab.ViewMode
            };
            session.OpenTabs.Add(tabSession);
            System.Diagnostics.Debug.WriteLine($"[BuildSessionDataSync] 标签: Id={tab.Id}, Title={tab.Title}, Content长度={tab.Document.Content?.Length ?? 0}");
        }

        System.Diagnostics.Debug.WriteLine($"[BuildSessionDataSync] 返回 session: {session.OpenTabs.Count} 个标签");
        return session;
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
        // 直接在 Tabs 中查找已打开的文件
        var existingTab = Tabs.FirstOrDefault(t => t.Document.FilePath == path);
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            StatusMessage = $"已切换到: {existingTab.Title}";
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
    private async Task CloseTabsToLeftAsync(TabItemViewModel? keepTab)
    {
        if (keepTab == null) return;

        var index = Tabs.IndexOf(keepTab);
        if (index <= 0) return; // 没有左边的标签

        var tabsToClose = Tabs.Take(index).ToList();
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
    private async Task CloseTabsToRightAsync(TabItemViewModel? keepTab)
    {
        if (keepTab == null) return;

        var index = Tabs.IndexOf(keepTab);
        var tabsToClose = Tabs.Skip(index + 1).ToList();
        
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
    private void OpenFileLocation(TabItemViewModel? tab)
    {
        if (tab == null) return;
        
        var filePath = tab.Document.FilePath;
        if (string.IsNullOrEmpty(filePath))
        {
            StatusMessage = "无法打开位置：文件尚未保存";
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                // Windows: 使用 explorer /select 打开文件所在目录并选中文件
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath}\"",
                    UseShellExecute = true
                });
                StatusMessage = $"已打开: {directory}";
            }
            else
            {
                StatusMessage = "目录不存在";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开失败: {ex.Message}";
        }
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
