using System;
using System.Collections.Generic;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Models = LythenMarkdown.UI.Models;

namespace LythenMarkdown.UI.Services;

/// <summary>
/// Markdown AST 解析器
/// </summary>
public class MarkdownParser
{
    public Models.MarkdownDocument Parse(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return new Models.MarkdownDocument();
        }

        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        var document = Markdown.Parse(markdown, pipeline);
        return ConvertDocument(document);
    }

    private Models.MarkdownDocument ConvertDocument(Markdig.Syntax.MarkdownDocument document)
    {
        var result = new Models.MarkdownDocument();

        foreach (var block in document)
        {
            var converted = ConvertBlock(block);
            if (converted != null)
            {
                result.Blocks.Add(converted);
            }
        }

        return result;
    }

    private Models.MarkdownBlock? ConvertBlock(Block block)
    {
        switch (block)
        {
            case Markdig.Syntax.HeadingBlock heading:
                return ConvertHeading(heading);
            case Markdig.Syntax.ParagraphBlock paragraph:
                return ConvertParagraph(paragraph);
            case Markdig.Syntax.CodeBlock codeBlock:
                return ConvertCodeBlock(codeBlock);
            case Markdig.Syntax.QuoteBlock quoteBlock:
                return ConvertQuoteBlock(quoteBlock);
            case ListBlock listBlock:
                return ConvertListBlock(listBlock);
            case HtmlBlock:
                // 跳过 HTML 块
                return null;
            default:
                return null;
        }
    }

    private Models.HeadingBlock ConvertHeading(Markdig.Syntax.HeadingBlock heading)
    {
        return new Models.HeadingBlock
        {
            Level = heading.Level,
            Inlines = ConvertInlines(heading.Inline)
        };
    }

    private Models.ParagraphBlock ConvertParagraph(Markdig.Syntax.ParagraphBlock paragraph)
    {
        return new Models.ParagraphBlock
        {
            Inlines = ConvertInlines(paragraph.Inline)
        };
    }

    private Models.CodeBlock ConvertCodeBlock(Markdig.Syntax.CodeBlock codeBlock)
    {
        var content = codeBlock.Lines.ToString().TrimEnd('\n', '\r');

        return new Models.CodeBlock
        {
            Language = string.Empty,
            Inlines = new List<Models.MarkdownInline> { new Models.TextInline { Text = content } }
        };
    }

    private Models.QuoteBlock ConvertQuoteBlock(Markdig.Syntax.QuoteBlock quoteBlock)
    {
        var result = new Models.QuoteBlock();

        foreach (var child in quoteBlock)
        {
            var converted = ConvertBlock(child);
            if (converted != null)
            {
                result.Inlines.AddRange(converted.Inlines);
            }
        }

        return result;
    }

    private Models.MarkdownBlock ConvertListBlock(ListBlock listBlock)
    {
        // 检查是否有序列表
        if (listBlock.IsOrdered)
        {
            var result = new Models.OrderedListBlock
            {
                StartNumber = 1,
                Inlines = new List<Models.MarkdownInline>()
            };

            foreach (var item in listBlock)
            {
                if (item is Markdig.Syntax.ListItemBlock listItem)
                {
                    result.Items.Add(ConvertListItem(listItem));
                }
            }

            return result;
        }
        else
        {
            var result = new Models.UnorderedListBlock
            {
                Inlines = new List<Models.MarkdownInline>()
            };

            foreach (var item in listBlock)
            {
                if (item is Markdig.Syntax.ListItemBlock listItem)
                {
                    result.Items.Add(ConvertListItem(listItem));
                }
            }

            return result;
        }
    }

    private Models.ListItemBlock ConvertListItem(Markdig.Syntax.ListItemBlock item)
    {
        var result = new Models.ListItemBlock();

        foreach (var child in item)
        {
            var converted = ConvertBlock(child);
            if (converted != null)
            {
                result.Inlines.AddRange(converted.Inlines);
            }
        }

        return result;
    }

    private List<Models.MarkdownInline> ConvertInlines(ContainerInline? container)
    {
        var result = new List<Models.MarkdownInline>();

        if (container == null) return result;

        foreach (var inline in container)
        {
            var converted = ConvertInline(inline);
            if (converted != null)
            {
                result.Add(converted);
            }
        }

        return result;
    }

    private Models.MarkdownInline? ConvertInline(MarkdownObject obj)
    {
        switch (obj)
        {
            case LiteralInline literal:
                return new Models.TextInline { Text = literal.Content.ToString() };
            case CodeInline code:
                return new Models.CodeInline { Text = code.Content.ToString() };
            case EmphasisInline emphasis:
                return ConvertEmphasis(emphasis);
            case LinkInline link:
                var linkText = string.Empty;
                if (link.FirstChild is LiteralInline lit)
                {
                    linkText = lit.Content.ToString();
                }
                return new Models.LinkInline
                {
                    Text = linkText,
                    Url = link.Url ?? string.Empty
                };
            case LineBreakInline:
                return new Models.TextInline { Text = "\n" };
            case AutolinkInline autolink:
                return new Models.LinkInline
                {
                    Text = autolink.Url ?? string.Empty,
                    Url = autolink.Url ?? string.Empty
                };
            case HtmlInline html:
                return new Models.TextInline { Text = html.Tag ?? string.Empty };
            default:
                return null;
        }
    }

    private Models.MarkdownInline ConvertEmphasis(EmphasisInline emphasis)
    {
        var children = ConvertInlines(emphasis);

        // 使用 Markdig 的 DelimiterCount 精确判断：**=2（粗体）, *=1（斜体）
        if (emphasis.DelimiterCount >= 2)
        {
            return new Models.BoldInline
            {
                Text = string.Empty,
                Children = children
            };
        }

        return new Models.ItalicInline
        {
            Text = string.Empty,
            Children = children
        };
    }
}
