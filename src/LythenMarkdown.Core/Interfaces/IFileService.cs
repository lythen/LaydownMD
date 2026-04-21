using System.IO;
using System.Threading.Tasks;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.Core.Interfaces;

/// <summary>
/// 文件服务接口
/// </summary>
public interface IFileService
{
    /// <summary>
    /// 打开文件
    /// </summary>
    Task<Document> OpenFileAsync(string path);

    /// <summary>
    /// 保存文件
    /// </summary>
    Task SaveFileAsync(Document document, string? path = null);

    /// <summary>
    /// 另存为
    /// </summary>
    Task SaveAsAsync(Document document, string path);

    /// <summary>
    /// 获取文件信息
    /// </summary>
    Task<FileInfo?> GetFileInfoAsync(string path);

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    /// 验证文件是否可读
    /// </summary>
    (bool IsValid, string? ErrorMessage) ValidateFile(string path);

    /// <summary>
    /// 检查文件是否被修改
    /// </summary>
    Task<bool> IsFileModifiedSinceOpenAsync(Document document);
}
