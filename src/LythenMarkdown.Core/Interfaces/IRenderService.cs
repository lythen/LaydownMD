using System;
using System.Threading;
using System.Threading.Tasks;

namespace LythenMarkdown.Core.Interfaces;

/// <summary>
/// 渲染服务接口
/// </summary>
public interface IRenderService
{
    /// <summary>
    /// 渲染 Markdown 到 HTML
    /// </summary>
    string Render(string markdown);

    /// <summary>
    /// 异步渲染
    /// </summary>
    Task<string> RenderAsync(string markdown, CancellationToken cancellationToken = default);

    /// <summary>
    /// 防抖渲染（用于实时预览）
    /// </summary>
    CancellationTokenSource DebouncedRender(string markdown, int delayMs, Action<string> onComplete);

    /// <summary>
    /// 清除指定文档的缓存
    /// </summary>
    void ClearCache(string documentId);

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    void ClearAllCache();

    /// <summary>
    /// 获取渲染缓存大小
    /// </summary>
    long GetCacheSize();
}
