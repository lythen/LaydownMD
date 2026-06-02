using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using AvaloniaEdit;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;
using LythenMarkdown.UI.ViewModels;
using LythenMarkdown.UI.Views;
using WinState = Avalonia.Controls.WindowState;
using TabItemModel = LythenMarkdown.Core.Models.TabItem;
using AvaloniaTabItem = Avalonia.Controls.TabItem;

namespace LythenMarkdown.UI;

public partial class MainWindow : Window
{
    private AvaloniaEdit.TextEditor? _mainEditor;
    private AvaloniaEdit.TextEditor? _splitEditor;
    private readonly ISessionService _sessionService;
    
    // 标签页拖拽排序状态
    private TabItemViewModel? _draggedTab;
    private Point _dragStartPoint;
    private bool _isDragging;

    // 弹窗预览管理：TabId -> PreviewPopupWindow，每个标签页最多一个弹窗
    private readonly Dictionary<string, Views.PreviewPopupWindow> _previewPopups = new();
    
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

    /// <summary>
    /// 查找当前可见的编辑器（根据视图模式）
    /// </summary>
    private AvaloniaEdit.TextEditor? FindCurrentVisibleEditor()
    {
        if (DataContext is not ViewModels.MainViewModel vm || vm.SelectedTab == null)
        {
            System.Diagnostics.Debug.WriteLine($"[FindCurrentVisibleEditor] 没有选中的标签页");
            return null;
        }

        var viewMode = vm.SelectedTab.ViewMode;
        System.Diagnostics.Debug.WriteLine($"[FindCurrentVisibleEditor] viewMode={viewMode}");
        
        // 遍历整个窗口的视觉树查找所有 TextEditor
        var allEditors = this.GetVisualDescendants()
            .OfType<AvaloniaEdit.TextEditor>()
            .ToList();

        System.Diagnostics.Debug.WriteLine($"[FindCurrentVisibleEditor] 窗口中共有 {allEditors.Count} 个 TextEditor");

        if (!allEditors.Any()) return null;

        // 遍历所有编辑器，找到可见的
        foreach (var editor in allEditors)
        {
            var isVisible = editor.IsVisible;
            var editorName = editor.Name ?? "(无名称)";
            System.Diagnostics.Debug.WriteLine($"[FindCurrentVisibleEditor] editor={editor.GetHashCode()}, Name={editorName}, IsVisible={isVisible}");
        }

        // 根据视图模式返回正确的编辑器
        // SplitMode: MainEditor 可见（ZIndex=1）
        // EditMode: SplitEditor 可见（ZIndex=2）
        // PreviewMode: 没有编辑器
        foreach (var editor in allEditors)
        {
            if (!editor.IsVisible) continue;

            var editorName = editor.Name ?? "";
            
            if (viewMode == ViewMode.Split && editorName == "MainEditor")
            {
                System.Diagnostics.Debug.WriteLine($"[FindCurrentVisibleEditor] 返回 MainEditor");
                return editor;
            }
            else if (viewMode == ViewMode.Edit && editorName == "SplitEditor")
            {
                System.Diagnostics.Debug.WriteLine($"[FindCurrentVisibleEditor] 返回 SplitEditor");
                return editor;
            }
            else if (viewMode == ViewMode.Preview)
            {
                System.Diagnostics.Debug.WriteLine($"[FindCurrentVisibleEditor] Preview模式，返回null");
                return null;
            }
        }

        // 如果没有匹配到名称，返回第一个可见的编辑器
        System.Diagnostics.Debug.WriteLine($"[FindCurrentVisibleEditor] 返回第一个可见编辑器");
        return allEditors.FirstOrDefault(e => e.IsVisible);
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
        
        // 编辑菜单（已在 XAML 中通过 Click="..." 直接绑定）
        // Find/Replace 仍需代码处理
        this.FindControl<MenuItem>("FindMenuItem")?.AddHandler(MenuItem.ClickEvent, OnFind);
        this.FindControl<MenuItem>("ReplaceMenuItem")?.AddHandler(MenuItem.ClickEvent, OnReplace);

        // 格式化菜单（使用 Command 绑定，在 ViewModel 中处理）
        // 注意：这里不再重复绑定 Click 事件，避免触发两次
        
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
        var editor = FindCurrentVisibleEditor();
        editor?.Undo();
    }
    
    private void OnRedo(object? sender, RoutedEventArgs e)
    {
        var editor = FindCurrentVisibleEditor();
        editor?.Redo();
    }
    
    private void OnCut(object? sender, RoutedEventArgs e)
    {
        var editor = FindCurrentVisibleEditor();
        editor?.Cut();
    }
    
    private void OnCopy(object? sender, RoutedEventArgs e)
    {
        var editor = FindCurrentVisibleEditor();
        editor?.Copy();
    }
    
    private void OnPaste(object? sender, RoutedEventArgs e)
    {
        var editor = FindCurrentVisibleEditor();
        editor?.Paste();
    }
    
    private void OnSelectAll(object? sender, RoutedEventArgs e)
    {
        var editor = FindCurrentVisibleEditor();
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
        // 从视觉树中查找当前可见的编辑器
        var editor = FindCurrentVisibleEditor();
        
        if (editor == null || editor.Document == null)
        {
            System.Diagnostics.Debug.WriteLine($"[OnBold] 没有可用的编辑器");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[OnBold] editor={editor.GetHashCode()}");

        // 使用编辑器的 SelectionStart 和 SelectionLength 获取选区信息
        var caretOffset = editor.CaretOffset;
        var selectionStart = editor.SelectionStart;
        var selectionLength = editor.SelectionLength;
        var textLength = editor.Document.TextLength;
        var selectionEnd = selectionStart + selectionLength;

        System.Diagnostics.Debug.WriteLine($"[OnBold] BEFORE: caretOffset={caretOffset}, selectionStart={selectionStart}, selectionLength={selectionLength}, textLength={textLength}");

        // 直接在编辑器上操作
        if (selectionLength > 0)
        {
            // 有选区：在选区前后插入 ** (总共4个*)
            System.Diagnostics.Debug.WriteLine($"[OnBold] 有选区: startOffset={selectionStart}, endOffset={selectionEnd}, 即将插入 ** text **");
            
            // 先插入后缀（位置靠后）
            editor.Document.Insert(selectionEnd, "**");
            // 再插入前缀
            editor.Document.Insert(selectionStart, "**");
            
            // 重新设置选区（选中原始文本部分）
            editor.SelectionStart = selectionStart + 2;
            editor.SelectionLength = selectionLength;
        }
        else
        {
            // 无选区：插入 **** 并将光标定位到中间
            System.Diagnostics.Debug.WriteLine($"[OnBold] 无选区: 在位置 {caretOffset} 插入 ****");
            editor.Document.Insert(caretOffset, "****");
            editor.CaretOffset = caretOffset + 2;
        }
        
        System.Diagnostics.Debug.WriteLine($"[OnBold] AFTER: Document长度={editor.Document.TextLength}");
    }
    
    /// <summary>
    /// 从 TabItem 的视觉树中查找 TextEditor
    /// </summary>
    private AvaloniaEdit.TextEditor? FindEditorInTabItem(TabItemViewModel tab)
    {
        if (tab == null)
        {
            System.Diagnostics.Debug.WriteLine($"[FindEditor] tab 参数为空");
            return null;
        }
        
        // 直接使用 TabItemViewModel 中注册的编辑器
        // TabItemViewModel 在 RegisterEditor 时会维护编辑器列表
        var editor = tab.GetLastRegisteredEditor();
        
        if (editor == null)
        {
            // 如果没有注册的编辑器，尝试从视觉树查找（作为后备方案）
            var tabControl = this.FindControl<TabControl>("MainTabControl");
            if (tabControl != null)
            {
                var selectedVm = tabControl.SelectedItem as TabItemViewModel;
                if (selectedVm != null)
                {
                    // 查找 DataContext 匹配的 TabItem
                    var tabItem = tabControl.GetVisualDescendants()
                        .OfType<AvaloniaTabItem>()
                        .FirstOrDefault(ti => ti.DataContext == selectedVm);
                        
                    if (tabItem != null)
                    {
                        // 尝试获取 ContentPresenter 中的编辑器
                        var contentPresenter = tabItem.GetVisualDescendants()
                            .OfType<ContentPresenter>()
                            .FirstOrDefault();
                            
                        if (contentPresenter != null)
                        {
                            editor = contentPresenter.GetVisualDescendants()
                                .OfType<AvaloniaEdit.TextEditor>()
                                .LastOrDefault();
                        }
                    }
                }
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"[FindEditor] ViewMode={tab.ViewMode}, 编辑器={editor?.GetHashCode() ?? 0}");
        return editor;
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
        // 使用与 OnBold 相同的方式获取编辑器
        var editor = FindCurrentVisibleEditor();
        if (editor == null || editor.Document == null) return;

        var caretOffset = editor.CaretOffset;
        var selectionLength = editor.SelectionLength;
        var document = editor.Document;

        System.Diagnostics.Debug.WriteLine($"[InsertMarkdown] prefix='{prefix}', suffix='{suffix}', caretOffset={caretOffset}, selectionLength={selectionLength}");

        if (selectionLength > 0)
        {
            // 有选区：在选区前后插入前后缀
            var startOffset = editor.SelectionStart;
            var endOffset = startOffset + selectionLength;
            
            // 先插入后缀（位置靠后）
            document.Insert(endOffset, suffix);
            // 再插入前缀
            document.Insert(startOffset, prefix);
            
            // 重新设置选区
            editor.SelectionStart = startOffset + prefix.Length;
            editor.SelectionLength = selectionLength;
        }
        else
        {
            // 无选区：插入前后缀并将光标定位到中间
            var textToInsert = prefix + suffix;
            document.Insert(caretOffset, textToInsert);
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
        if (DataContext is not ViewModels.MainViewModel vm || vm.SelectedTab == null)
            return;

        var tab = vm.SelectedTab;
        var tabId = tab.Id;

        // 多弹窗管理：如果该标签的弹窗已存在，激活它
        if (_previewPopups.TryGetValue(tabId, out var existingPopup))
        {
            try { existingPopup.Activate(); } catch { _previewPopups.Remove(tabId); }
            return;
        }

        // 创建新弹窗
        var popup = new Views.PreviewPopupWindow(tab)
        {
            // 使用 MainWindow 作为父窗口，确保居中
        };

        // 弹窗关闭时从字典移除
        popup.Closed += (_, _) =>
        {
            _previewPopups.Remove(tabId);
        };

        _previewPopups[tabId] = popup;
        popup.Show(this);
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
        // 处理标签页拖拽排序
        if (e.Data.Contains("TabItem") && sender is Control targetControl)
        {
            var draggedTab = e.Data.Get("TabItem") as TabItemViewModel;
            if (draggedTab != null && DataContext is ViewModels.MainViewModel mainVm)
            {
                // 计算目标标签页索引
                var targetIndex = GetTargetTabIndex(e);
                if (targetIndex >= 0)
                {
                    var currentIndex = mainVm.Tabs.IndexOf(draggedTab);
                    if (currentIndex >= 0 && currentIndex != targetIndex)
                    {
                        // 调整目标索引：如果拖拽到右侧，需要偏移
                        if (targetIndex > currentIndex) targetIndex--;
                        mainVm.MoveTab(currentIndex, targetIndex);
                    }
                }
            }
            return;
        }
        
        // 处理文件拖放
        if (e.Data.Contains(DataFormats.Files) && 
            DataContext is ViewModels.MainViewModel fileVm)
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                foreach (var file in files)
                {
                    var path = file?.Path?.LocalPath;
                    if (!string.IsNullOrEmpty(path))
                    {
                        await fileVm.OpenFileAsync(path);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 计算拖拽目标位置的标签页索引
    /// </summary>
    private int GetTargetTabIndex(DragEventArgs e)
    {
        var tabControl = this.FindControl<TabControl>("MainTabControl");
        if (tabControl == null) return -1;
        
        var dropPoint = e.GetPosition(tabControl);
        
        // 遍历所有 TabItem 找到放置位置
        for (int i = 0; i < tabControl.ItemCount; i++)
        {
            var tabItem = tabControl.ItemContainerGenerator.ContainerFromIndex(i) as AvaloniaTabItem;
            if (tabItem == null) continue;
            
            var bounds = tabItem.Bounds;
            if (bounds.Contains(dropPoint))
            {
                // 判断是在左半部分还是右半部分
                var midPoint = bounds.X + bounds.Width / 2;
                if (dropPoint.X < midPoint)
                {
                    return i;
                }
                else
                {
                    return i + 1;
                }
            }
        }
        
        return -1;
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
    
    #region 标签页拖拽排序
    
    /// <summary>
    /// 标签页指针按下事件 - 开始拖拽
    /// </summary>
    private void OnTabPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not StackPanel stackPanel) return;
        if (stackPanel.Tag is not TabItemViewModel tab) return;
        
        var properties = e.GetCurrentPoint(null).Properties;
        if (properties.IsLeftButtonPressed)
        {
            _draggedTab = tab;
            _dragStartPoint = e.GetPosition(null);
            _isDragging = false;
        }
    }
    
    /// <summary>
    /// 标签页指针移动事件 - 检测拖拽
    /// </summary>
    private void OnTabPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedTab == null) return;
        
        var currentPoint = e.GetPosition(null);
        var diff = currentPoint - _dragStartPoint;
        
        // 移动超过 5 像素才认为是拖拽
        if (!_isDragging && (Math.Abs(diff.X) > 5 || Math.Abs(diff.Y) > 5))
        {
            _isDragging = true;
        }
        
        if (_isDragging)
        {
            // 开始拖拽操作
            var data = new DataObject();
            data.Set("TabItem", _draggedTab);
            
            // 开始拖拽，需要 PointerEventArgs
            DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        }
    }
    
    /// <summary>
    /// 标签页指针释放事件 - 结束拖拽
    /// </summary>
    private void OnTabPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _draggedTab = null;
        _isDragging = false;
    }
    
    #endregion
}
