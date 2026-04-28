using System;

namespace LythenMarkdown.Core.Models;

/// <summary>
/// 文档模型
/// </summary>
public class Document
{
    /// <summary>
    /// 文档唯一标识符 (GUID)
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 文件路径，null 表示新建文档
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// 文档内容
    /// </summary>
    public string Content 
    { 
        get => _content; 
        set
        {
            _content = value;
        }
    }
    private string _content = string.Empty;

    /// <summary>
    /// 原始内容（用于检测修改）
    /// </summary>
    public string? OriginalContent { get; set; }

    /// <summary>
    /// 光标位置（字符索引）
    /// </summary>
    public int CursorPosition { get; set; }

    /// <summary>
    /// 滚动偏移 Y
    /// </summary>
    public int ScrollOffsetY { get; set; }

    /// <summary>
    /// 是否已修改（内容与原始内容不同）
    /// </summary>
    public bool IsModified => Content != OriginalContent;

    /// <summary>
    /// 是否为新建文档（未保存到文件）
    /// </summary>
    public bool IsNew => string.IsNullOrEmpty(FilePath);

    /// <summary>
    /// 是否为只读文档
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 获取文档标题（文件名或"新建文档"）
    /// </summary>
    public string Title => string.IsNullOrEmpty(FilePath) 
        ? "新建文档" 
        : Path.GetFileName(FilePath);

    /// <summary>
    /// 创建文档副本
    /// </summary>
    public Document Clone()
    {
        return new Document
        {
            Id = this.Id,
            FilePath = this.FilePath,
            Content = this.Content,
            OriginalContent = this.OriginalContent,
            CursorPosition = this.CursorPosition,
            ScrollOffsetY = this.ScrollOffsetY,
            CreatedAt = this.CreatedAt,
            ModifiedAt = this.ModifiedAt
        };
    }
}
