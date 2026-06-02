using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using LythenMarkdown.UI.Models;

namespace LythenMarkdown.UI.Services;

/// <summary>
/// Markdown AST 转 Avalonia 控件树渲染器
/// </summary>
public class MarkdownToControlsRenderer
{
    private readonly MarkdownParser _parser = new();

    // 样式定义
    private static readonly IBrush Heading1Brush = new SolidColorBrush(Color.Parse("#1A1A1A"));
    private static readonly IBrush Heading2Brush = new SolidColorBrush(Color.Parse("#333333"));
    private static readonly IBrush Heading3Brush = new SolidColorBrush(Color.Parse("#4D4D4D"));
    private static readonly IBrush BodyBrush = new SolidColorBrush(Color.Parse("#333333"));
    private static readonly IBrush CodeBackgroundBrush = new SolidColorBrush(Color.Parse("#F6F8FA"));
    private static readonly IBrush CodeForegroundBrush = new SolidColorBrush(Color.Parse("#24292E"));
    private static readonly IBrush QuoteBorderBrush = new SolidColorBrush(Color.Parse("#D0D7DE"));
    private static readonly IBrush QuoteBackgroundBrush = new SolidColorBrush(Color.Parse("#F6F8FA"));
    private static readonly IBrush LinkBrush = new SolidColorBrush(Color.Parse("#0969DA"));
    private static readonly IBrush CodeWordBrush = new SolidColorBrush(Color.Parse("#1A1A1A"));
    private static readonly FontFamily CodeFont = new FontFamily("Consolas, Courier New");

    private static readonly double Heading1Size = 28;
    private static readonly double Heading2Size = 24;
    private static readonly double Heading3Size = 20;
    private static readonly double Heading4Size = 18;
    private static readonly double BodySize = 14;
    private static readonly double CodeSize = 13;

    public Panel Render(string markdown)
    {
        var document = _parser.Parse(markdown);
        return RenderDocument(document);
    }

    private Panel RenderDocument(MarkdownDocument document)
    {
        var panel = new StackPanel
        {
            Spacing = 0,
            Margin = new Thickness(16)
        };

        foreach (var block in document.Blocks)
        {
            var control = RenderBlock(block);
            if (control != null)
            {
                panel.Children.Add(control);
            }
        }

        // 确保至少有一个子元素
        if (panel.Children.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = string.Empty,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        return panel;
    }

    private Control? RenderBlock(MarkdownBlock block)
    {
        return block switch
        {
            HeadingBlock heading => RenderHeading(heading),
            ParagraphBlock paragraph => RenderParagraph(paragraph),
            CodeBlock codeBlock => RenderCodeBlock(codeBlock),
            QuoteBlock quoteBlock => RenderQuoteBlock(quoteBlock),
            UnorderedListBlock list => RenderUnorderedList(list),
            OrderedListBlock list => RenderOrderedList(list),
            ThematicBreakBlock => RenderThematicBreak(),
            _ => null
        };
    }

    private TextBlock RenderHeading(HeadingBlock heading)
    {
        var size = heading.Level switch
        {
            1 => Heading1Size,
            2 => Heading2Size,
            3 => Heading3Size,
            _ => Heading4Size
        };

        var color = heading.Level switch
        {
            1 => Heading1Brush,
            2 => Heading2Brush,
            3 => Heading3Brush,
            _ => BodyBrush
        };

        var textBlock = new TextBlock
        {
            FontSize = size,
            FontWeight = heading.Level <= 2 ? FontWeight.Bold : FontWeight.SemiBold,
            Foreground = color,
            Margin = new Thickness(0, heading.Level == 1 ? 16 : 12, 0, 8),
            TextWrapping = TextWrapping.Wrap
        };
        AddInlinesToTextBlock(textBlock, heading.Inlines);
        return textBlock;
    }

    private TextBlock RenderParagraph(ParagraphBlock paragraph)
    {
        var textBlock = new TextBlock
        {
            FontSize = BodySize,
            Foreground = BodyBrush,
            Margin = new Thickness(0, 0, 0, 12),
            TextWrapping = TextWrapping.Wrap
        };
        AddInlinesToTextBlock(textBlock, paragraph.Inlines);
        return textBlock;
    }

    private Border RenderCodeBlock(CodeBlock codeBlock)
    {
        var content = RenderInlinesToString(codeBlock.Inlines);

        return new Border
        {
            Background = CodeBackgroundBrush,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Content = new TextBlock
                {
                    Text = content,
                    FontFamily = CodeFont,
                    FontSize = CodeSize,
                    Foreground = CodeForegroundBrush,
                    TextWrapping = TextWrapping.NoWrap
                }
            }
        };
    }

    private Border RenderQuoteBlock(QuoteBlock quoteBlock)
    {
        var textBlock = new TextBlock
        {
            FontSize = BodySize,
            Foreground = BodyBrush,
            TextWrapping = TextWrapping.Wrap
        };
        AddInlinesToTextBlock(textBlock, quoteBlock.Inlines);

        return new Border
        {
            Background = QuoteBackgroundBrush,
            BorderBrush = QuoteBorderBrush,
            BorderThickness = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 0, 0, 12),
            Child = textBlock
        };
    }

    private StackPanel RenderUnorderedList(UnorderedListBlock list)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 12),
            Spacing = 4
        };

        foreach (var item in list.Items)
        {
            var textBlock = new TextBlock
            {
                FontSize = BodySize,
                Foreground = BodyBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2)
            };
            textBlock.Inlines.Add(new Run { Text = "• " });
            AddInlinesToTextBlock(textBlock, item.Inlines);
            panel.Children.Add(textBlock);

            // 嵌套列表
            var nestedContent = RenderNestedLists(item);
            if (nestedContent != null)
            {
                panel.Children.Add(new Border
                {
                    Margin = new Thickness(16, 4, 0, 0),
                    Child = nestedContent
                });
            }
        }

        return panel;
    }

    private StackPanel RenderOrderedList(OrderedListBlock list)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 12),
            Spacing = 4
        };

        int number = list.StartNumber;
        foreach (var item in list.Items)
        {
            var textBlock = new TextBlock
            {
                FontSize = BodySize,
                Foreground = BodyBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2)
            };
            textBlock.Inlines.Add(new Run { Text = $"{number}. " });
            AddInlinesToTextBlock(textBlock, item.Inlines);
            panel.Children.Add(textBlock);
            number++;

            // 嵌套列表
            var nestedContent = RenderNestedLists(item);
            if (nestedContent != null)
            {
                panel.Children.Add(new Border
                {
                    Margin = new Thickness(16, 4, 0, 0),
                    Child = nestedContent
                });
            }
        }

        return panel;
    }

    private StackPanel? RenderNestedLists(ListItemBlock item)
    {
        // 简化实现：只处理第一个子块
        return null;
    }

    private Border RenderThematicBreak()
    {
        return new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.Parse("#D0D7DE")),
            Margin = new Thickness(0, 12, 0, 12)
        };
    }

    /// <summary>
    /// 将内联元素转为纯文本（用于代码块、列表前缀等不需要格式化的场景）
    /// </summary>
    private static string RenderInlinesToString(List<MarkdownInline> inlines)
    {
        var result = new StringBuilder();

        foreach (var inline in inlines)
        {
            if (inline is TextInline text)
                result.Append(text.Text);
            else if (inline is CodeInline code)
                result.Append(code.Text);
            else if (inline is LinkInline link)
                result.Append(link.Text);
            else if (inline is BoldInline bold)
                result.Append(RenderInlinesToString(bold.Children));
            else if (inline is ItalicInline italic)
                result.Append(RenderInlinesToString(italic.Children));
        }

        return result.ToString();
    }

    /// <summary>
    /// 将内联元素转换为格式化 Run 并添加到 TextBlock.Inlines
    /// </summary>
    private static void AddInlinesToTextBlock(TextBlock textBlock, List<MarkdownInline> inlines)
    {
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case TextInline text:
                    textBlock.Inlines.Add(new Run { Text = text.Text });
                    break;

                case CodeInline code:
                    textBlock.Inlines.Add(new Run
                    {
                        Text = code.Text,
                        FontFamily = CodeFont,
                        Background = new SolidColorBrush(Color.Parse("#F0F0F0"))
                    });
                    break;

                case BoldInline bold:
                    textBlock.Inlines.Add(new Run
                    {
                        Text = RenderInlinesToString(bold.Children),
                        FontWeight = FontWeight.Bold
                    });
                    break;

                case ItalicInline italic:
                    textBlock.Inlines.Add(new Run
                    {
                        Text = RenderInlinesToString(italic.Children),
                        FontStyle = FontStyle.Italic
                    });
                    break;

                case LinkInline link:
                    textBlock.Inlines.Add(new Run
                    {
                        Text = link.Text,
                        Foreground = LinkBrush
                    });
                    break;
            }
        }
    }
}
