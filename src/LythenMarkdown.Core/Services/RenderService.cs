using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Markdig;
using LythenMarkdown.Core.Interfaces;

namespace LythenMarkdown.Core.Services;

/// <summary>
/// 渲染服务实现
/// </summary>
public class RenderService : IRenderService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly ConcurrentDictionary<string, string> _cache = new();
    private long _cacheSize;

    public RenderService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .UseTaskLists()
            .UseAutoLinks()
            .Build();
    }

    public string Render(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        // 检查缓存
        if (_cache.TryGetValue(markdown, out var cachedHtml))
        {
            return cachedHtml;
        }

        var html = Markdown.ToHtml(markdown, _pipeline);
        
        // 缓存结果
        _cache[markdown] = html;
        Interlocked.Add(ref _cacheSize, Encoding.UTF8.GetByteCount(html));

        return html;
    }

    public Task<string> RenderAsync(string markdown, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Render(markdown), cancellationToken);
    }

    public CancellationTokenSource DebouncedRender(string markdown, int delayMs, Action<string> onComplete)
    {
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, token);
                if (!token.IsCancellationRequested)
                {
                    var html = Render(markdown);
                    onComplete?.Invoke(html);
                }
            }
            catch (TaskCanceledException)
            {
                // 正常取消
            }
        }, token);

        return cts;
    }

    public void ClearCache(string documentId)
    {
        _cache.Clear();
        _cacheSize = 0;
    }

    public void ClearAllCache()
    {
        _cache.Clear();
        _cacheSize = 0;
    }

    public long GetCacheSize() => _cacheSize;
}
