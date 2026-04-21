using System;
using System.Threading.Tasks;

namespace LythenMarkdown.Core.Interfaces;

/// <summary>
/// 资源类型
/// </summary>
public enum CleanupResourceType
{
    RenderCache,
    TempFile,
    SessionSnapshot,
    EditHistory,
    FileHandle
}

/// <summary>
/// 需要清理的资源
/// </summary>
public class CleanupResource
{
    public CleanupResourceType Type { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public string? ResourcePath { get; set; }
    public Action? CleanupAction { get; set; }
}

/// <summary>
/// 资源清理服务接口
/// </summary>
public interface IResourceCleanupService
{
    /// <summary>
    /// 清理单个文档资源
    /// </summary>
    Task CleanupDocumentAsync(string documentId);

    /// <summary>
    /// 清理所有资源
    /// </summary>
    Task CleanupAllAsync();

    /// <summary>
    /// 注册需要清理的资源
    /// </summary>
    void RegisterResource(CleanupResource resource);

    /// <summary>
    /// 注销资源
    /// </summary>
    void UnregisterResource(string documentId, CleanupResourceType type);

    /// <summary>
    /// 获取已注册资源数量
    /// </summary>
    int GetRegisteredCount();
}
