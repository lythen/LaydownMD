using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;
using LythenMarkdown.UI.ViewModels;
using LythenMarkdown.UI.Views;
using WinState = Avalonia.Controls.WindowState;

namespace LythenMarkdown.UI;

public partial class MainWindow : Window
{
    private AvaloniaEdit.TextEditor? _mainEditor;
    private AvaloniaEdit.TextEditor? _splitEditor;
    private readonly ISessionService _sessionService;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // 获取 SessionService
        _sessionService = App.GetRequiredService<ISessionService>();
        
        // 获取控件引用
        _mainEditor = this.FindControl<AvaloniaEdit.TextEditor>("MainEditor");
        _splitEditor = this.FindControl<AvaloniaEdit.TextEditor>("SplitEditor");
        
        // 绑定编辑器事件
        if (_mainEditor != null)
        {
            _mainEditor.TextChanged += OnEditorTextChanged;
            _mainEditor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
        }
        if (_splitEditor != null)
        {
            _splitEditor.TextChanged += OnEditorTextChanged;
            _splitEditor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
        }
        
        // 绑定 TabControl 选择变化
        var tabControl = this.FindControl<TabControl>("MainTabControl");
        if (tabControl != null)
        {
            tabControl.SelectionChanged += OnTabSelectionChanged;
        }

        // 订阅 ViewModel 的 EditorContentNeeded 事件
        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.EditorContentNeeded += OnEditorContentNeeded;
        }
        
        // 添加 DragDrop 处理
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
        
        // 添加快捷键处理
        KeyDown += OnKeyDown;
        
        // 添加窗口关闭处理
        Closing += OnWindowClosing;
        
        // 绑定工具栏按钮事件
        BindToolBarButtons();
        
        // 绑定菜单项事件
        BindMenuItems();
        
        // 绑定欢迎页面按钮
        BindWelcomeButtons();
    }

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm && vm.SelectedTab != null)
        {
            // 延迟执行，等待 DataTemplate 控件生成
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                await Task.Delay(50); // 等待 UI 更新
                LoadEditorContent(vm);
                UpdatePreviewControls(vm.SelectedTab);
            }, Avalonia.Threading.DispatcherPriority.Loaded);
        }
    }

    private void OnEditorContentNeeded(object? sender, TabItemViewModel tab)
    {
        // 延迟执行，等待 DataTemplate 控件生成
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            // 等待 UI 更新
            await Task.Delay(100);
            LoadEditorContent(tab);
            UpdatePreviewControls(tab);
        }, Avalonia.Threading.DispatcherPriority.Loaded);
    }

    private void LoadEditorContent(TabItemViewModel tab)
    {
        if (tab == null) return;

        var tabControl = this.FindControl<TabControl>("MainTabControl");
        if (tabControl == null) return;

        // 查找所有 TextEditor 并注册到 tab
        var editors = FindAllTextEditors(tabControl);
        foreach (var editor in editors)
        {
            tab.RegisterEditor(editor);
        }
    }

    /// <summary>
    /// 更新预览控件的内容
    /// </summary>
    private void UpdatePreviewControls(TabItemViewModel? tab)
    {
        if (tab == null) return;

        // 编辑模式不需要预览
        if (tab.ViewMode == ViewMode.Edit)
        {
            return;
        }

        // 获取当前模式的 ScrollViewer
        var scrollViewer = GetActivePreviewScrollViewer(tab.ViewMode);
        if (scrollViewer == null) 
        {
            System.Diagnostics.Debug.WriteLine("[Debug] ScrollViewer not found");
            return;
        }
        
        // 清空预览区域
        scrollViewer.Content = null;

        // 创建新的控件树副本
        var previewControls = tab.RenderControlsCopy();
        
        System.Diagnostics.Debug.WriteLine($"[Preview] ViewMode={tab.ViewMode}, Children={previewControls.Children.Count}");

        // 设置预览内容
        scrollViewer.Content = previewControls;
        System.Diagnostics.Debug.WriteLine("[Preview] Set content");
    }

    /// <summary>
    /// 获取当前 Tab 的预览 ScrollViewer
    /// </summary>
    private ScrollViewer? GetActivePreviewScrollViewer(ViewMode viewMode)
    {
        var tabControl = this.FindControl<TabControl>("MainTabControl");
        if (tabControl == null) 
        {
            System.Diagnostics.Debug.WriteLine("[Debug] TabControl not found");
            return null;
        }

        // 遍历 TabControl 的 Visual 子元素，通过 Name 精确查找 ScrollViewer
        var scrollViewers = tabControl.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .ToList();
            
        System.Diagnostics.Debug.WriteLine($"[Debug] Found {scrollViewers.Count} ScrollViewers");

        // 根据视图模式返回对应的 ScrollViewer
        // 预览模式返回 PreviewScrollViewer（全屏预览）
        // 分栏模式返回 SplitPreviewScrollViewer（右侧预览）
        if (viewMode == ViewMode.Preview)
        {
            return scrollViewers.FirstOrDefault(sv => sv.Name == "PreviewScrollViewer")
                   ?? scrollViewers.FirstOrDefault();
        }
        else if (viewMode == ViewMode.Split)
        {
            return scrollViewers.FirstOrDefault(sv => sv.Name == "SplitPreviewScrollViewer")
                   ?? scrollViewers.Skip(1).FirstOrDefault();
        }

        return null;
    }

    private List<AvaloniaEdit.TextEditor> FindAllTextEditors(Avalonia.Controls.Control? control)
    {
        var editors = new List<AvaloniaEdit.TextEditor>();
        if (control == null) return editors;

        if (control is AvaloniaEdit.TextEditor editor)
        {
            editors.Add(editor);
        }

        foreach (var child in control.GetVisualChildren())
        {
            if (child is Avalonia.Controls.Control childControl)
            {
                editors.AddRange(FindAllTextEditors(childControl));
            }
        }

        return editors;
    }

    private void LoadEditorContent(ViewModels.MainViewModel vm)
    {
        LoadEditorContent(vm.SelectedTab!);
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void BindToolBarButtons()
    {
        // 文件操作
        this.FindControl<Button>("NewToolButton")?.AddHandler(Button.ClickEvent, OnNewFile);
        this.FindControl<Button>("OpenToolButton")?.AddHandler(Button.ClickEvent, OnOpenFile);
        this.FindControl<Button>("SaveToolButton")?.AddHandler(Button.ClickEvent, OnSaveFile);
        
        // 撤销重做
        this.FindControl<Button>("UndoToolButton")?.AddHandler(Button.ClickEvent, OnUndo);
        this.FindControl<Button>("RedoToolButton")?.AddHandler(Button.ClickEvent, OnRedo);
        
        // 格式化
        this.FindControl<Button>("BoldToolButton")?.AddHandler(Button.ClickEvent, OnBold);
        this.FindControl<Button>("ItalicToolButton")?.AddHandler(Button.ClickEvent, OnItalic);
        this.FindControl<Button>("CodeToolButton")?.AddHandler(Button.ClickEvent, OnCode);
        this.FindControl<Button>("LinkToolButton")?.AddHandler(Button.ClickEvent, OnLink);
        this.FindControl<Button>("ListToolButton")?.AddHandler(Button.ClickEvent, OnUnorderedList);
        this.FindControl<Button>("OrderedListToolButton")?.AddHandler(Button.ClickEvent, OnOrderedList);
        this.FindControl<Button>("QuoteToolButton")?.AddHandler(Button.ClickEvent, OnQuote);
        this.FindControl<Button>("H1ToolButton")?.AddHandler(Button.ClickEvent, OnHeading1);
        this.FindControl<Button>("H2ToolButton")?.AddHandler(Button.ClickEvent, OnHeading2);
        
        // 视图模式
        this.FindControl<Button>("EditModeToolButton")?.AddHandler(Button.ClickEvent, OnEditMode);
        this.FindControl<Button>("SplitModeToolButton")?.AddHandler(Button.ClickEvent, OnSplitMode);
        this.FindControl<Button>("PreviewModeToolButton")?.AddHandler(Button.ClickEvent, OnPreviewMode);
    }
    
    private void BindMenuItems()
    {
        // 文件菜单
        this.FindControl<MenuItem>("NewMenuItem")?.AddHandler(MenuItem.ClickEvent, OnNewFile);
        this.FindControl<MenuItem>("OpenMenuItem")?.AddHandler(MenuItem.ClickEvent, OnOpenFile);
        this.FindControl<MenuItem>("SaveMenuItem")?.AddHandler(MenuItem.ClickEvent, OnSaveFile);
        this.FindControl<MenuItem>("SaveAsMenuItem")?.AddHandler(MenuItem.ClickEvent, OnSaveAsFile);
        this.FindControl<MenuItem>("CloseMenuItem")?.AddHandler(MenuItem.ClickEvent, OnCloseTab);
        this.FindControl<MenuItem>("ExitMenuItem")?.AddHandler(MenuItem.ClickEvent, OnExit);
        
        // 编辑菜单
        this.FindControl<MenuItem>("UndoMenuItem")?.AddHandler(MenuItem.ClickEvent, OnUndo);
        this.FindControl<MenuItem>("RedoMenuItem")?.AddHandler(MenuItem.ClickEvent, OnRedo);
        this.FindControl<MenuItem>("CutMenuItem")?.AddHandler(MenuItem.ClickEvent, OnCut);
        this.FindControl<MenuItem>("CopyMenuItem")?.AddHandler(MenuItem.ClickEvent, OnCopy);
        this.FindControl<MenuItem>("PasteMenuItem")?.AddHandler(MenuItem.ClickEvent, OnPaste);
        this.FindControl<MenuItem>("SelectAllMenuItem")?.AddHandler(MenuItem.ClickEvent, OnSelectAll);
        this.FindControl<MenuItem>("FindMenuItem")?.AddHandler(MenuItem.ClickEvent, OnFind);
        this.FindControl<MenuItem>("ReplaceMenuItem")?.AddHandler(MenuItem.ClickEvent, OnReplace);
        
        // 视图菜单
        this.FindControl<MenuItem>("EditModeMenuItem")?.AddHandler(MenuItem.ClickEvent, OnEditMode);
        this.FindControl<MenuItem>("SplitModeMenuItem")?.AddHandler(MenuItem.ClickEvent, OnSplitMode);
        this.FindControl<MenuItem>("PreviewModeMenuItem")?.AddHandler(MenuItem.ClickEvent, OnPreviewMode);
        this.FindControl<MenuItem>("PopupPreviewMenuItem")?.AddHandler(MenuItem.ClickEvent, OnPopupPreview);
        this.FindControl<MenuItem>("FullscreenMenuItem")?.AddHandler(MenuItem.ClickEvent, OnFullscreen);
        
        // 帮助菜单
        this.FindControl<MenuItem>("HelpMenuItem")?.AddHandler(MenuItem.ClickEvent, OnHelp);
        this.FindControl<MenuItem>("AboutMenuItem")?.AddHandler(MenuItem.ClickEvent, OnAbout);
    }
    
    private void BindWelcomeButtons()
    {
        this.FindControl<Button>("WelcomeNewButton")?.AddHandler(Button.ClickEvent, OnNewFile);
        this.FindControl<Button>("WelcomeOpenButton")?.AddHandler(Button.ClickEvent, OnOpenFile);
    }
    
    private void OnTabCloseClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is TabItemViewModel tab && DataContext is ViewModels.MainViewModel vm)
        {
            vm.CloseTabCommand.Execute(tab);
        }
    }
    
    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        if (_mainEditor != null)
        {
            var line = _mainEditor.TextArea.Caret.Line;
            var column = _mainEditor.TextArea.Caret.Column;
            var lineColText = this.FindControl<TextBlock>("LineColumnText");
            if (lineColText != null)
            {
                lineColText.Text = $"行 {line}, 列 {column}";
            }
        }
        else if (_splitEditor != null)
        {
            var line = _splitEditor.TextArea.Caret.Line;
            var column = _splitEditor.TextArea.Caret.Column;
            var lineColText = this.FindControl<TextBlock>("LineColumnText");
            if (lineColText != null)
            {
                lineColText.Text = $"行 {line}, 列 {column}";
            }
        }
    }
    
    private void OnNewFile(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.NewFileCommand.Execute(null);
            ShowEditorUI();
        }
    }
    
    private async void OnOpenFile(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            await vm.OpenFileCommand.ExecuteAsync(null);
            ShowEditorUI();
        }
    }
    
    private async void OnSaveFile(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            await vm.SaveCommand.ExecuteAsync(null);
        }
    }
    
    private async void OnSaveAsFile(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            await vm.SaveAsCommand.ExecuteAsync(null);
        }
    }
    
    private void OnCloseTab(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm && vm.SelectedTab != null)
        {
            vm.CloseTabCommand.Execute(vm.SelectedTab);
            if (!vm.Tabs.Any())
            {
                ShowEmptyUI();
            }
        }
    }
    
    private void OnExit(object? sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private void OnUndo(object? sender, RoutedEventArgs e)
    {
        var editor = _mainEditor ?? _splitEditor;
        editor?.Undo();
    }
    
    private void OnRedo(object? sender, RoutedEventArgs e)
    {
        var editor = _mainEditor ?? _splitEditor;
        editor?.Redo();
    }
    
    private void OnCut(object? sender, RoutedEventArgs e)
    {
        var editor = _mainEditor ?? _splitEditor;
        editor?.Cut();
    }
    
    private void OnCopy(object? sender, RoutedEventArgs e)
    {
        var editor = _mainEditor ?? _splitEditor;
        editor?.Copy();
    }
    
    private void OnPaste(object? sender, RoutedEventArgs e)
    {
        var editor = _mainEditor ?? _splitEditor;
        editor?.Paste();
    }
    
    private void OnSelectAll(object? sender, RoutedEventArgs e)
    {
        var editor = _mainEditor ?? _splitEditor;
        editor?.SelectAll();
    }
    
    private void OnFind(object? sender, RoutedEventArgs e)
    {
        // TODO: 实现查找对话框
    }
    
    private void OnReplace(object? sender, RoutedEventArgs e)
    {
        // TODO: 实现替换对话框
    }
    
    private void OnBold(object? sender, RoutedEventArgs e)
    {
        InsertMarkdown("**", "**");
    }
    
    private void OnItalic(object? sender, RoutedEventArgs e)
    {
        InsertMarkdown("*", "*");
    }
    
    private void OnCode(object? sender, RoutedEventArgs e)
    {
        InsertMarkdown("`", "`");
    }
    
    private void OnLink(object? sender, RoutedEventArgs e)
    {
        InsertMarkdown("[", "](url)");
    }
    
    private void OnUnorderedList(object? sender, RoutedEventArgs e)
    {
        InsertMarkdown("- ", "");
    }
    
    private void OnOrderedList(object? sender, RoutedEventArgs e)
    {
        InsertMarkdown("1. ", "");
    }
    
    private void OnQuote(object? sender, RoutedEventArgs e)
    {
        InsertMarkdown("> ", "");
    }
    
    private void OnHeading1(object? sender, RoutedEventArgs e)
    {
        InsertMarkdown("# ", "");
    }
    
    private void OnHeading2(object? sender, RoutedEventArgs e)
    {
        InsertMarkdown("## ", "");
    }
    
    private void InsertMarkdown(string prefix, string suffix)
    {
        var editor = _mainEditor ?? _splitEditor;
        if (editor == null) return;
        
        var start = editor.SelectionStart;
        var length = editor.SelectionLength;
        var text = editor.Text;
        
        if (length > 0)
        {
            var selectedText = text.Substring(start, length);
            editor.Text = text.Substring(0, start) + prefix + selectedText + suffix + text.Substring(start + length);
            editor.SelectionStart = start + prefix.Length;
            editor.SelectionLength = length;
        }
        else
        {
            var caretOffset = editor.CaretOffset;
            editor.Text = text.Substring(0, caretOffset) + prefix + suffix + text.Substring(caretOffset);
            editor.CaretOffset = caretOffset + prefix.Length;
        }
    }
    
    private void OnEditMode(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.SetViewModeCommand.Execute(ViewMode.Edit);
            UpdateViewModeText("编辑模式");
        }
    }
    
    private void OnSplitMode(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.SetViewModeCommand.Execute(ViewMode.Split);
            UpdateViewModeText("分栏模式");
            // 切换到分栏模式时更新预览
            UpdatePreviewControls(vm.SelectedTab);
        }
    }
    
    private void OnPreviewMode(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.SetViewModeCommand.Execute(ViewMode.Preview);
            UpdateViewModeText("预览模式");
            // 切换到预览模式时更新预览
            UpdatePreviewControls(vm.SelectedTab);
        }
    }
    
    private void OnPopupPreview(object? sender, RoutedEventArgs e)
    {
        // TODO: 实现弹窗预览
    }
    
    private void OnFullscreen(object? sender, RoutedEventArgs e)
    {
        if (WindowState == WinState.FullScreen)
            WindowState = WinState.Normal;
        else
            WindowState = WinState.FullScreen;
    }
    
    private void OnHelp(object? sender, RoutedEventArgs e)
    {
        var helpWindow = new HelpWindow();
        helpWindow.Show();
    }
    
    private void OnAbout(object? sender, RoutedEventArgs e)
    {
        var aboutDialog = new AboutDialog();
        aboutDialog.Show();
    }
    
    private void UpdateViewModeText(string text)
    {
        var viewModeText = this.FindControl<TextBlock>("ViewModeText");
        if (viewModeText != null)
        {
            viewModeText.Text = text;
        }
    }
    
    private void ShowEditorUI()
    {
        var emptyState = this.FindControl<StackPanel>("EmptyStatePanel");
        var editorDock = this.FindControl<DockPanel>("EditorDockPanel");
        if (emptyState != null) emptyState.IsVisible = false;
        if (editorDock != null) editorDock.IsVisible = true;
    }
    
    private void ShowEmptyUI()
    {
        var emptyState = this.FindControl<StackPanel>("EmptyStatePanel");
        var editorDock = this.FindControl<DockPanel>("EditorDockPanel");
        if (emptyState != null) emptyState.IsVisible = true;
        if (editorDock != null) editorDock.IsVisible = false;
    }
    
    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        // 内容变化通过 TextDocument 自动同步到 TabItemViewModel
        // 这里只需要更新状态信息
        if (DataContext is ViewModels.MainViewModel viewModel)
        {
            if (sender is TextEditor editor)
            {
                viewModel.CurrentDocumentText = editor.Text;
            }
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files) && 
            DataContext is ViewModels.MainViewModel viewModel)
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                foreach (var file in files)
                {
                    var path = file?.Path?.LocalPath;
                    if (!string.IsNullOrEmpty(path))
                    {
                        await viewModel.OpenFileAsync(path);
                    }
                }
            }
        }
    }

    private bool _isClosing;
    
    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[OnWindowClosing] 开始执行...");
        
        if (_isClosing)
        {
            System.Diagnostics.Debug.WriteLine($"[OnWindowClosing] 已在关闭中，跳过");
            return;
        }
        _isClosing = true;
        
        if (DataContext is ViewModels.MainViewModel viewModel)
        {
            // 取消订阅事件，防止事件触发时访问正在关闭的 UI
            viewModel.EditorContentNeeded -= OnEditorContentNeeded;

            System.Diagnostics.Debug.WriteLine($"[OnWindowClosing] 直接保存会话（不弹出保存对话框）");
            
            // 直接保存会话 - 会话保存是自动的，不询问用户
            // 用户可以选择手动保存到文件，但这不是关闭时的必要操作
            SaveSessionAndClose(viewModel);
        }
    }
    
    private void SaveSessionAndClose(MainViewModel viewModel)
    {
        System.Diagnostics.Debug.WriteLine($"[SaveSessionAndClose] 开始保存会话...");
        
        // 确保所有标签页的 Document.Content 已同步
        int syncedCount = 0;
        foreach (var tab in viewModel.Tabs)
        {
            if (tab.EnsureContentSynced())
            {
                syncedCount++;
            }
            System.Diagnostics.Debug.WriteLine($"[SaveSessionAndClose] 标签 {tab.Title}: Content长度={tab.Document.Content?.Length ?? 0}");
        }
        System.Diagnostics.Debug.WriteLine($"[SaveSessionAndClose] 同步了 {syncedCount} 个标签页的内容");
        
        // 保存会话（同步）
        var session = viewModel.BuildSessionDataSync();
        _sessionService.SaveSessionSync(session);
        System.Diagnostics.Debug.WriteLine($"[SaveSessionAndClose] 会话保存完成");
        
        // 手动关闭窗口
        System.Diagnostics.Debug.WriteLine($"[SaveSessionAndClose] 关闭窗口");
        Close();
    }

    /// <summary>
    /// 全局快捷键处理
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ViewModels.MainViewModel viewModel)
            return;

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        // Ctrl+S 保存当前文档
        if (ctrl && !shift && e.Key == Key.S)
        {
            OnSaveFile(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        // Shift+Ctrl+S 另存为
        if (ctrl && shift && e.Key == Key.S)
        {
            OnSaveAsFile(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        // Ctrl+1 编辑模式
        if (ctrl && e.Key == Key.D1)
        {
            viewModel.SetViewModeCommand.Execute(ViewMode.Edit);
            e.Handled = true;
        }
        // Ctrl+2 分栏模式
        else if (ctrl && e.Key == Key.D2)
        {
            viewModel.SetViewModeCommand.Execute(ViewMode.Split);
            UpdatePreviewControls(viewModel.SelectedTab);
            e.Handled = true;
        }
        // Ctrl+3 预览模式
        else if (ctrl && e.Key == Key.D3)
        {
            viewModel.SetViewModeCommand.Execute(ViewMode.Preview);
            UpdatePreviewControls(viewModel.SelectedTab);
            e.Handled = true;
        }
    }

    /// <summary>
    /// 调试：保存会话
    /// </summary>
    private async void OnSaveSession(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel viewModel)
        {
            await viewModel.SaveSessionAsync();
            var path = viewModel.GetSessionService().GetSessionFilePath();
            System.Diagnostics.Debug.WriteLine($"[Debug] 会话已保存到: {path}");
            await ShowMessageAsync(this, $"会话已保存到:\n{path}");
        }
    }

    /// <summary>
    /// 调试：打开 session.json 文件位置
    /// </summary>
    private void OnOpenSessionFile(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel viewModel)
        {
            var path = viewModel.GetSessionService().GetSessionFilePath();
            // 打开文件所在目录
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
        }
    }

    /// <summary>
    /// 调试：显示会话内容
    /// </summary>
    private async void OnShowSessionContent(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel viewModel)
        {
            var path = viewModel.GetSessionService().GetSessionFilePath();
            if (System.IO.File.Exists(path))
            {
                var content = await System.IO.File.ReadAllTextAsync(path);
                await ShowMessageAsync(this, $"Session 内容:\n\n{content}");
            }
            else
            {
                await ShowMessageAsync(this, $"session.json 不存在:\n{path}");
            }
        }
    }

    /// <summary>
    /// 显示消息对话框
    /// </summary>
    private async Task ShowMessageAsync(Window parent, string message)
    {
        var dialog = new Window
        {
            Title = "提示",
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 15,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new Button { Content = "确定", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center }
                }
            }
        };
        ((Button)((StackPanel)dialog.Content).Children[1]).Click += (s, args) => dialog.Close();
        await dialog.ShowDialog(parent);
    }
}
