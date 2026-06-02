using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LythenMarkdown.UI.ViewModels;

namespace LythenMarkdown.UI.Views;

/// <summary>
/// 弹窗预览窗口 - 在新窗口中显示当前标签页的 Markdown 渲染结果
/// 直接订阅 SharedDocument.TextChanged 事件，独立处理防抖和刷新，
/// 不依赖 RenderedControls PropertyChanged 事件链
/// </summary>
public partial class PreviewPopupWindow : Window
{
    private readonly TabItemViewModel _tab;
    private CancellationTokenSource? _debounceCts;
    private bool _disposed;
    private const int DebounceDelayMs = 150;

    public PreviewPopupWindow(TabItemViewModel tab)
    {
        AvaloniaXamlLoader.Load(this);

        _tab = tab;
        Title = $"预览 - {tab.Title}";
        Closed += OnClosed;

        // 首次渲染
        RefreshPreview();

        // 直接订阅 SharedDocument.TextChanged，不依赖 PropertyChanged 链
        _tab.SharedDocument.TextChanged += OnSharedDocumentTextChanged;
        // 订阅 Title 变化
        _tab.PropertyChanged += OnTabPropertyChanged;
    }

    /// <summary>
    /// 文档内容变化时触发防抖渲染
    /// </summary>
    private void OnSharedDocumentTextChanged(object? sender, EventArgs e)
    {
        if (_disposed) return;

        // 取消之前的防抖
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var cts = _debounceCts;

        // 捕获当前文本快照
        var text = _tab.SharedDocument.Text;

        // 在后台线程等待防抖延迟，然后回到 UI 线程刷新
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelayMs, cts.Token);

                // 回到 UI 线程刷新预览
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!cts.Token.IsCancellationRequested && !_disposed)
                    {
                        RefreshPreview();
                    }
                });
            }
            catch (TaskCanceledException)
            {
                // 被取消，正常处理
            }
        }, cts.Token);
    }

    private void OnTabPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_disposed) return;

        // 标题变化时更新窗口标题
        if (e.PropertyName == nameof(TabItemViewModel.Title))
        {
            Title = $"预览 - {_tab.Title}";
        }
    }

    /// <summary>
    /// 刷新弹窗中的预览内容（创建独立控件树副本）
    /// </summary>
    private void RefreshPreview()
    {
        if (_disposed) return;

        var border = this.FindControl<Border>("PreviewContentBorder");
        var emptyHint = this.FindControl<TextBlock>("EmptyHint");

        if (border == null || emptyHint == null) return;

        // 清除旧内容
        border.Child = null;

        try
        {
            // 创建控件树副本（Avalonia 控件不能共享父节点）
            // 使用 SharedDocument.Text 确保获取最新内容
            var previewPanel = _tab.RenderControlsCopy();

            if (previewPanel != null && previewPanel.Children.Count > 0)
            {
                border.Child = previewPanel;
                emptyHint.IsVisible = false;
            }
            else
            {
                emptyHint.IsVisible = true;
            }
        }
        catch
        {
            emptyHint.IsVisible = true;
        }

        // 更新标题（文档名可能变化）
        Title = $"预览 - {_tab.Title}";
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _disposed = true;

        // 取消防抖
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();

        if (_tab != null)
        {
            _tab.SharedDocument.TextChanged -= OnSharedDocumentTextChanged;
            _tab.PropertyChanged -= OnTabPropertyChanged;
        }
    }
}
