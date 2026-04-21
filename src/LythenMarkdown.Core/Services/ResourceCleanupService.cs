using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using LythenMarkdown.Core.Interfaces;

namespace LythenMarkdown.Core.Services;

/// <summary>
/// 资源清理服务实现
/// </summary>
public class ResourceCleanupService : IResourceCleanupService
{
    private readonly ConcurrentDictionary<string, CleanupResource> _resources = new();

    public Task CleanupDocumentAsync(string documentId)
    {
        var documentResources = _resources.Values
            .Where(r => r.DocumentId == documentId);

        foreach (var resource in documentResources)
        {
            try
            {
                resource.CleanupAction?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理资源失败: {ex.Message}");
            }
        }

        // 移除该文档的所有资源记录
        var keysToRemove = _resources.Keys
            .Where(k => _resources.TryGetValue(k, out var r) && r.DocumentId == documentId)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _resources.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    public Task CleanupAllAsync()
    {
        foreach (var resource in _resources.Values)
        {
            try
            {
                resource.CleanupAction?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理资源失败: {ex.Message}");
            }
        }

        _resources.Clear();
        return Task.CompletedTask;
    }

    public void RegisterResource(CleanupResource resource)
    {
        var key = $"{resource.DocumentId}_{resource.Type}";
        _resources[key] = resource;
    }

    public void UnregisterResource(string documentId, CleanupResourceType type)
    {
        var key = $"{documentId}_{type}";
        _resources.TryRemove(key, out _);
    }

    public int GetRegisteredCount()
    {
        return _resources.Count;
    }
}
