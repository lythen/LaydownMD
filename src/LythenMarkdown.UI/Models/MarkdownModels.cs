using System;
using System.Collections.Generic;

namespace LythenMarkdown.UI.Models;

/// <summary>
/// Markdown 渲染块的基类
/// </summary>
public abstract class MarkdownBlock
{
    public List<MarkdownInline> Inlines { get; set; } = new();
}

/// <summary>
/// 内联元素基类
/// </summary>
public abstract class MarkdownInline
{
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// 纯文本
/// </summary>
public class TextInline : MarkdownInline { }

/// <summary>
/// 加粗文本
/// </summary>
public class BoldInline : MarkdownInline
{
    public List<MarkdownInline> Children { get; set; } = new();
}

/// <summary>
/// 斜体文本
/// </summary>
public class ItalicInline : MarkdownInline
{
    public List<MarkdownInline> Children { get; set; } = new();
}

/// <summary>
/// 删除线文本
/// </summary>
public class StrikethroughInline : MarkdownInline
{
    public List<MarkdownInline> Children { get; set; } = new();
}

/// <summary>
/// 行内代码
/// </summary>
public class CodeInline : MarkdownInline { }

/// <summary>
/// 链接
/// </summary>
public class LinkInline : MarkdownInline
{
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// 图片
/// </summary>
public class ImageInline : MarkdownInline
{
    public string Alt { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// 标题块
/// </summary>
public class HeadingBlock : MarkdownBlock
{
    public int Level { get; set; } = 1;
}

/// <summary>
/// 段落块
/// </summary>
public class ParagraphBlock : MarkdownBlock { }

/// <summary>
/// 代码块
/// </summary>
public class CodeBlock : MarkdownBlock
{
    public string Language { get; set; } = string.Empty;
}

/// <summary>
/// 引用块
/// </summary>
public class QuoteBlock : MarkdownBlock { }

/// <summary>
/// 无序列表块
/// </summary>
public class UnorderedListBlock : MarkdownBlock
{
    public List<ListItemBlock> Items { get; set; } = new();
}

/// <summary>
/// 有序列表块
/// </summary>
public class OrderedListBlock : MarkdownBlock
{
    public List<ListItemBlock> Items { get; set; } = new();
    public int StartNumber { get; set; } = 1;
}

/// <summary>
/// 列表项
/// </summary>
public class ListItemBlock : MarkdownBlock { }

/// <summary>
/// 水平线
/// </summary>
public class ThematicBreakBlock : MarkdownBlock { }

/// <summary>
/// Markdown 文档
/// </summary>
public class MarkdownDocument
{
    public List<MarkdownBlock> Blocks { get; set; } = new();
}
