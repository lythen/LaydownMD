using System;
using System.IO;
using System.Threading.Tasks;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.Core.Services;

/// <summary>
/// 文件服务实现
/// </summary>
public class FileService : IFileService
{
    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    public async Task<Document> OpenFileAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"文件不存在: {path}");
        }

        var fileInfo = new FileInfo(path);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"文件过大，最大支持 {MaxFileSizeBytes / 1024 / 1024}MB");
        }

        var content = await File.ReadAllTextAsync(path);

        return new Document
        {
            FilePath = path,
            Content = content,
            OriginalContent = content,
            ModifiedAt = fileInfo.LastWriteTime
        };
    }

    public async Task SaveFileAsync(Document document, string? path = null)
    {
        var savePath = path ?? document.FilePath;
        if (string.IsNullOrEmpty(savePath))
        {
            throw new InvalidOperationException("保存路径不能为空");
        }

        var directory = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(savePath, document.Content);

        document.FilePath = savePath;
        document.OriginalContent = document.Content;
        document.ModifiedAt = DateTime.Now;
    }

    public async Task SaveAsAsync(Document document, string path)
    {
        await SaveFileAsync(document, path);
    }

    public Task<FileInfo?> GetFileInfoAsync(string path)
    {
        if (!File.Exists(path))
        {
            return Task.FromResult<FileInfo?>(null);
        }
        return Task.FromResult<FileInfo?>(new FileInfo(path));
    }

    public bool FileExists(string path) => File.Exists(path);

    public (bool IsValid, string? ErrorMessage) ValidateFile(string path)
    {
        if (!File.Exists(path))
        {
            return (false, "文件不存在");
        }

        var fileInfo = new FileInfo(path);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
            return (false, $"文件过大，最大支持 {MaxFileSizeBytes / 1024 / 1024}MB");
        }

        if (!path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) &&
            !path.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "仅支持 .md 或 .markdown 文件");
        }

        return (true, null);
    }

    public async Task<bool> IsFileModifiedSinceOpenAsync(Document document)
    {
        if (string.IsNullOrEmpty(document.FilePath) || !File.Exists(document.FilePath))
        {
            return false;
        }

        var currentContent = await File.ReadAllTextAsync(document.FilePath);
        return currentContent != document.OriginalContent;
    }
}
