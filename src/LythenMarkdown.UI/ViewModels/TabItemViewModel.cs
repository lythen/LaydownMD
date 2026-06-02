using System;
using System.Text;
using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;
using LythenMarkdown.UI.Services;

namespace LythenMarkdown.UI.ViewModels;

public partial class TabItemViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;
    private readonly IRenderService _renderService;
    private readonly IFileService _fileService;
    private readonly MarkdownToControlsRenderer _controlsRenderer;
    private CancellationTokenSource? _renderDebounceCts;
    
    // 防抖延迟（毫秒）
    private const int DebounceDelayMs = 150;

    /// <summary>
    /// 是否已进行手动编辑（用于判断是否为编辑状态）
    /// </summary>
    private bool _hasManualEdit;

    /// <summary>
    /// 撤销/重做操作前的 HasManualEdit 状态（用于恢复）
    /// </summary>
    private bool _previousHasManualEdit;

    /// <summary>
    /// Document.Content 是否已与 SharedDocument.Text 同步
    /// true: 无需再次同步; false: 需要同步
    /// </summary>
    private bool _contentSynced = true;

    [ObservableProperty]
    private string _id;

    [ObservableProperty]
    private Document _document;

    [ObservableProperty]
    private ViewMode _viewMode = ViewMode.Split;

    /// <summary>
    /// 预览区域是否显示
    /// </summary>
    public bool IsPreviewVisible => ViewMode == ViewMode.Split || ViewMode == ViewMode.Preview;

    [ObservableProperty]
    private string _renderedHtml = string.Empty;

    /// <summary>
    /// 用于 WebView 加载的 data: URI 格式 HTML
    /// </summary>
    [ObservableProperty]
    private string _webViewSource = string.Empty;

    /// <summary>
    /// 渲染后的 Avalonia 控件树（用于原生渲染）
    /// </summary>
    [ObservableProperty]
    private Panel? _renderedControls;

    [ObservableProperty]
    private string _title = "新建文档";

    // 共享的 TextDocument，所有编辑器都使用这个
    public TextDocument SharedDocument { get; }

    /// <summary>
    /// 内容是否被修改（Content != OriginalContent）
    /// </summary>
    public bool IsModified => Document.IsModified;

    /// <summary>
    /// 是否为编辑状态（进行过手动编辑，通过撤销/重做恢复到原始内容时会变回 false）
    /// </summary>
    public bool IsEdited => _hasManualEdit;

    /// <summary>
    /// 是否为只读文档
    /// </summary>
    public bool IsReadOnly => Document.IsReadOnly;

    /// <summary>
    /// 父级 MainViewModel 引用（用于 ContextMenu 绑定）
    /// </summary>
    public MainViewModel MainViewModel => _mainViewModel;

    /// <summary>
    /// 选项卡标题（根据状态显示不同样式）
    /// - 只读文档：🔒 标题
    /// - 编辑状态：标题 *
    /// - 原始状态：标题
    /// </summary>
    public string TabTitle
    {
        get
        {
            var title = Title;
            if (IsReadOnly)
                return $"🔒 {title}";
            if (IsEdited)
                return $"{title} *";
            return title;
        }
    }

    /// <summary>
    /// 只读状态变化时更新标题和编辑器只读状态
    /// </summary>
    partial void OnDocumentChanged(Document value)
    {
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(TabTitle));
        
        // 更新所有已注册编辑器的只读状态
        foreach (var editor in _editors)
        {
            editor.IsReadOnly = IsReadOnly;
        }
    }

    // 编辑器引用列表（可能有多个编辑器同时引用同一个 Document）
    private readonly List<TextEditor> _editors = new();

    public TabItemViewModel(MainViewModel mainViewModel, Document document)
    {
        _mainViewModel = mainViewModel;
        _renderService = App.GetRequiredService<IRenderService>();
        _fileService = App.GetRequiredService<IFileService>();
        _controlsRenderer = new MarkdownToControlsRenderer();
        _id = document.Id;
        _document = document;
        _title = document.Title;
        
        // 创建共享的 TextDocument
        SharedDocument = new TextDocument(document.Content ?? string.Empty);
        
        // 监听 TextDocument 内容变化
        SharedDocument.TextChanged += OnSharedDocumentTextChanged;
        
        System.Diagnostics.Debug.WriteLine($"[TabItem] 初始化文档 {Title}, Content长度={document.Content?.Length ?? 0}, IsModified={document.IsModified}");
        
        // 标记为已初始化（在设置 TextChanged 事件之后，但在任何用户编辑之前）
        _isInitialized = true;
        
        // 如果文档有修改历史，标记为已编辑状态
        if (document.IsModified)
        {
            _hasManualEdit = true;
            System.Diagnostics.Debug.WriteLine($"[TabItem] 文档 {Title} 被标记为已编辑（从会话恢复或之前有编辑）");
        }
        
        ForceRender(); // 初始渲染
    }

    /// <summary>
    /// 是否已完成初始化（用于忽略初始化时的 TextChanged 事件）
    /// </summary>
    private bool _isInitialized;

    /// <summary>
    /// 共享文档内容变化后触发
    /// </summary>
    private void OnSharedDocumentTextChanged(object? sender, EventArgs e)
    {
        // 只读文档不处理
        if (IsReadOnly) return;

        // 忽略初始化时的 TextChanged 事件
        if (!_isInitialized)
        {
            System.Diagnostics.Debug.WriteLine($"[TabItem] 忽略初始化时的 TextChanged");
            return;
        }

        // 记录调试信息
        var newText = SharedDocument.Text;
        System.Diagnostics.Debug.WriteLine($"[TabItem] TextChanged: SharedDocument.Text={newText?.Length ?? -1}chars");

        // 通过比较当前内容与原始内容来判断是否为编辑状态
        // 无论通过何种方式（直接编辑、撤销、重做）到达原始内容，都应重置编辑状态
        var isAtOriginalState = string.Equals(newText, Document.OriginalContent, StringComparison.Ordinal);
        
        if (isAtOriginalState)
        {
            // 回到原始内容，清空编辑状态
            if (_hasManualEdit)
            {
                System.Diagnostics.Debug.WriteLine($"[TabItem] 回到原始内容，设置 _hasManualEdit=false");
                _hasManualEdit = false;
                UpdateTitleForEditState();
            }
        }
        else
        {
            // 内容与原始内容不同，标记为编辑状态
            if (!_hasManualEdit)
            {
                System.Diagnostics.Debug.WriteLine($"[TabItem] 检测到编辑，设置 _hasManualEdit=true");
                _hasManualEdit = true;
                UpdateTitleForEditState();
            }
        }

        // 同步 Document.Content - 始终同步，确保会话保存时内容正确
        var contentChanged = Document.Content != SharedDocument.Text;
        if (contentChanged)
        {
            System.Diagnostics.Debug.WriteLine($"[TabItem] 同步 Document.Content");
            Document.Content = SharedDocument.Text;
            _contentSynced = true;
            OnPropertyChanged(nameof(IsModified));
        }
        else
        {
            _contentSynced = true;
        }
        
        // 防抖渲染
        DebouncedRender(SharedDocument.Text);
    }

    /// <summary>
    /// 更新标题以反映编辑状态
    /// </summary>
    private void UpdateTitleForEditState()
    {
        OnPropertyChanged(nameof(IsEdited));
        OnPropertyChanged(nameof(TabTitle));
    }

    /// <summary>
    /// 注册编辑器（每次切换 Tab 时调用，确保编辑器显示正确的文档内容）
    /// </summary>
    public void RegisterEditor(TextEditor editor)
    {
        if (!_editors.Contains(editor))
        {
            _editors.Add(editor);
            System.Diagnostics.Debug.WriteLine($"[TabItem] 注册新编辑器，当前已注册 {_editors.Count} 个");
        }
        // 每次都设置 Document，确保编辑器显示正确的内容
        var currentDocContent = SharedDocument.Text;
        System.Diagnostics.Debug.WriteLine($"[TabItem] 设置编辑器 Document, SharedDocument.Text 长度={currentDocContent?.Length ?? -1}");
        editor.Document = SharedDocument;
        
        // 设置只读状态
        editor.IsReadOnly = IsReadOnly;
        
        System.Diagnostics.Debug.WriteLine($"[TabItem] 编辑器注册完成, Document.Content 长度={Document.Content?.Length ?? -1}");
    }

    /// <summary>
    /// 注销编辑器
    /// </summary>
    public void UnregisterEditor(TextEditor editor)
    {
        _editors.Remove(editor);
    }

    public void UpdateContent(string content)
    {
        // 直接更新 TextDocument，会触发 TextChanged 事件
        if (SharedDocument.Text != content)
        {
            SharedDocument.Text = content;
        }
    }

    /// <summary>
    /// 在当前光标位置插入 Markdown 格式
    /// </summary>
    /// <param name="prefix">前缀（如 **）</param>
    /// <param name="suffix">后缀（如 **）</param>
    /// <param name="caretOffset">当前光标位置</param>
    /// <param name="selectionLength">选区长度（0表示无选区）</param>
    public void InsertMarkdown(string prefix, string suffix, int caretOffset, int selectionLength)
    {
        if (selectionLength > 0)
        {
            // 有选中文本：在选区末尾插入后缀，在开头插入前缀
            var endOffset = caretOffset + selectionLength;
            SharedDocument.Insert(endOffset, suffix);
            SharedDocument.Insert(caretOffset, prefix);
        }
        else
        {
            // 无选中文本：插入前后缀，光标定位到中间
            SharedDocument.Insert(caretOffset, prefix + suffix);
        }
    }

    /// <summary>
    /// 获取第一个已注册的编辑器
    /// </summary>
    public TextEditor? GetFirstRegisteredEditor()
    {
        return _editors.FirstOrDefault();
    }
    
    /// <summary>
    /// 获取最后一个已注册的编辑器（通常是最新的编辑器）
    /// </summary>
    public TextEditor? GetLastRegisteredEditor()
    {
        return _editors.LastOrDefault();
    }

    /// <summary>
    /// 标记为已编辑状态（用于从会话恢复时）
    /// </summary>
    public void MarkAsEdited()
    {
        if (!_hasManualEdit)
        {
            _hasManualEdit = true;
            UpdateTitleForEditState();
        }
    }

    /// <summary>
    /// 确保 Document.Content 与 SharedDocument.Text 同步
    /// 返回是否进行了同步操作
    /// </summary>
    public bool EnsureContentSynced()
    {
        if (_contentSynced)
        {
            System.Diagnostics.Debug.WriteLine($"[TabItem] {Title}: 内容已同步，无需操作");
            return false;
        }

        System.Diagnostics.Debug.WriteLine($"[TabItem] {Title}: 强制同步内容到 Document.Content");
        Document.Content = SharedDocument.Text;
        _contentSynced = true;
        return true;
    }

    /// <summary>
    /// 标记内容已修改，需要同步
    /// </summary>
    public void MarkContentDirty()
    {
        _contentSynced = false;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (Document.IsNew)
        {
            return;
        }

        await _fileService.SaveFileAsync(Document);
        
        // 保存后更新 OriginalContent，这样 IsModified 才能正确反映状态
        Document.OriginalContent = Document.Content;
        
        Title = Document.Title;
        // 保存后不再处于编辑状态
        _hasManualEdit = false;
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(IsEdited));
        OnPropertyChanged(nameof(TabTitle));
    }

    public async Task SaveAsAsync(string path)
    {
        await _fileService.SaveAsAsync(Document, path);
        
        // 保存后更新 OriginalContent
        Document.OriginalContent = Document.Content;
        
        Title = Document.Title;
        // 保存后不再处于编辑状态
        _hasManualEdit = false;
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(IsEdited));
        OnPropertyChanged(nameof(TabTitle));
    }

    /// <summary>
    /// 防抖渲染（150ms 延迟），渲染工作在后台线程执行，避免 UI 卡死
    /// </summary>
    private void DebouncedRender(string content)
    {
        // 取消之前的渲染
        _renderDebounceCts?.Cancel();
        _renderDebounceCts = new CancellationTokenSource();

        // 捕获内容快照，避免闭包引用变化
        var contentSnapshot = content;

        // 在后台线程执行渲染，防抖延迟也在后台
        _ = Task.Run(async () =>
        {
            try
            {
                // 等待防抖延迟（在后台线程）
                await Task.Delay(DebounceDelayMs, _renderDebounceCts.Token);

                // 后台线程：执行 HTML 渲染（无 UI 依赖，可后台运行）
                var html = _renderService.Render(contentSnapshot);

                // 回到 UI 线程：构建 Avalonia 控件树（必须在 UI 线程）
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        var controls = _controlsRenderer.Render(contentSnapshot);
                        RenderedHtml = html;
                        WebViewSource = CreateDataUri(html);
                        RenderedControls = controls;
                    }
                    catch
                    {
                        // 忽略控件树构建时的异常（如窗口已关闭）
                    }
                });
            }
            catch (TaskCanceledException)
            {
                // 被取消，正常处理
            }
        }, _renderDebounceCts.Token);
    }

    public void ForceRender()
    {
        _renderDebounceCts?.Cancel();
        var content = SharedDocument.Text;

        // 后台线程渲染 HTML，UI 线程构建控件树
        Task.Run(() =>
        {
            var html = _renderService.Render(content);
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var controls = _controlsRenderer.Render(content);
                RenderedHtml = html;
                WebViewSource = CreateDataUri(html);
                RenderedControls = controls;
            });
        });
    }

    /// <summary>
    /// 创建 data: URI 用于 WebView 加载 HTML
    /// </summary>
    private static string CreateDataUri(string html)
    {
        // 添加基础样式
        var styledHtml = $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<style>
body {{
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
    font-size: 16px;
    line-height: 1.6;
    padding: 20px;
    max-width: 100%;
    margin: 0 auto;
    color: #333;
    background: #ffffff;
}}
h1, h2, h3, h4, h5, h6 {{
    margin-top: 24px;
    margin-bottom: 16px;
    font-weight: 600;
    line-height: 1.25;
}}
h1 {{ font-size: 2em; border-bottom: 1px solid #eaecef; padding-bottom: 0.3em; }}
h2 {{ font-size: 1.5em; border-bottom: 1px solid #eaecef; padding-bottom: 0.3em; }}
h3 {{ font-size: 1.25em; }}
code {{
    padding: 0.2em 0.4em;
    margin: 0;
    font-size: 85%;
    background-color: rgba(27,31,35,0.05);
    border-radius: 3px;
    font-family: 'SF Mono', Consolas, 'Liberation Mono', Menlo, monospace;
}}
pre {{
    padding: 16px;
    overflow: auto;
    font-size: 85%;
    line-height: 1.45;
    background-color: #f6f8fa;
    border-radius: 3px;
}}
pre code {{
    padding: 0;
    margin: 0;
    background-color: transparent;
    border-radius: 0;
}}
blockquote {{
    padding: 0 1em;
    color: #6a737d;
    border-left: 0.25em solid #dfe2e5;
}}
a {{
    color: #0366d6;
    text-decoration: none;
}}
a:hover {{
    text-decoration: underline;
}}
img {{
    max-width: 100%;
    height: auto;
}}
table {{
    border-collapse: collapse;
    width: 100%;
    margin: 16px 0;
}}
table th, table td {{
    padding: 6px 13px;
    border: 1px solid #dfe2e5;
}}
table tr:nth-child(2n) {{
    background-color: #f6f8fa;
}}
hr {{
    height: 0.25em;
    padding: 0;
    margin: 24px 0;
    background-color: #e1e4e8;
    border: 0;
}}
ul, ol {{
    padding-left: 2em;
}}
li + li {{
    margin-top: 0.25em;
}}
</style>
</head>
<body>
{html}
</body>
</html>";

        // Base64 编码 HTML 内容
        var bytes = Encoding.UTF8.GetBytes(styledHtml);
        var base64 = Convert.ToBase64String(bytes);
        return $"data:text/html;charset=utf-8;base64,{base64}";
    }

    partial void OnViewModeChanged(ViewMode value)
    {
        // 通知预览可见性变化
        OnPropertyChanged(nameof(IsPreviewVisible));
        
        if (value == ViewMode.Split || value == ViewMode.Preview)
        {
            ForceRender();
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _renderDebounceCts?.Cancel();
        _renderDebounceCts?.Dispose();
        SharedDocument.TextChanged -= OnSharedDocumentTextChanged;
    }

    /// <summary>
    /// 创建渲染控件的副本（Avalonia 控件不能共享父节点）
    /// </summary>
    public Panel RenderControlsCopy()
    {
        // 重新渲染以创建新的控件树实例
        return _controlsRenderer.Render(SharedDocument.Text);
    }
}
