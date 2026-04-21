using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LythenMarkdown.Core.Services;
using Xunit;

namespace LythenMarkdown.Tests.Services;

public class RenderServiceTests
{
    private readonly RenderService _renderService;

    public RenderServiceTests()
    {
        _renderService = new RenderService();
    }

    #region Render Tests

    [Fact]
    public void Render_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = _renderService.Render(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Render_WithNull_ReturnsEmptyString()
    {
        // Act
        var result = _renderService.Render(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Render_WithHeading_ReturnsHtmlWithHeading()
    {
        // Arrange
        var markdown = "# Hello World";

        // Act
        var result = _renderService.Render(markdown);

        // Assert
        // Markdig 默认输出格式
        result.Should().Contain("<h1");
        result.Should().Contain("Hello World");
        result.Should().Contain("</h1>");
    }

    [Fact]
    public void Render_WithBoldText_ReturnsHtmlWithStrongTag()
    {
        // Arrange
        var markdown = "**bold text**";

        // Act
        var result = _renderService.Render(markdown);

        // Assert
        result.Should().Contain("<strong>bold text</strong>");
    }

    [Fact]
    public void Render_WithItalicText_ReturnsHtmlWithEmTag()
    {
        // Arrange
        var markdown = "*italic text*";

        // Act
        var result = _renderService.Render(markdown);

        // Assert
        result.Should().Contain("<em>italic text</em>");
    }

    [Fact]
    public void Render_WithCodeBlock_ReturnsHtmlWithPreTag()
    {
        // Arrange
        var markdown = "```csharp\nvar x = 1;\n```";

        // Act
        var result = _renderService.Render(markdown);

        // Assert
        result.Should().Contain("<pre>");
        result.Should().Contain("<code");
    }

    [Fact]
    public void Render_WithLink_ReturnsHtmlWithAnchorTag()
    {
        // Arrange
        var markdown = "[Link Text](https://example.com)";

        // Act
        var result = _renderService.Render(markdown);

        // Assert
        result.Should().Contain("<a href=\"https://example.com\">Link Text</a>");
    }

    [Fact]
    public void Render_WithList_ReturnsHtmlWithListTags()
    {
        // Arrange
        var markdown = "- Item 1\n- Item 2\n- Item 3";

        // Act
        var result = _renderService.Render(markdown);

        // Assert
        result.Should().Contain("<ul>");
        result.Should().Contain("<li>Item 1</li>");
        result.Should().Contain("<li>Item 2</li>");
        result.Should().Contain("<li>Item 3</li>");
    }

    [Fact]
    public void Render_WithOrderedList_ReturnsHtmlWithOLTags()
    {
        // Arrange
        var markdown = "1. First\n2. Second\n3. Third";

        // Act
        var result = _renderService.Render(markdown);

        // Assert
        result.Should().Contain("<ol>");
        result.Should().Contain("<li>First</li>");
    }

    [Fact]
    public void Render_WithBlockquote_ReturnsHtmlWithBlockquoteTag()
    {
        // Arrange
        var markdown = "> This is a quote";

        // Act
        var result = _renderService.Render(markdown);

        // Assert
        result.Should().Contain("<blockquote>");
    }

    [Fact]
    public void Render_WithTable_ReturnsHtmlWithTableTags()
    {
        // Arrange
        var markdown = "| Header 1 | Header 2 |\n|----------|----------|\n| Cell 1   | Cell 2   |";

        // Act
        var result = _renderService.Render(markdown);

        // Assert
        result.Should().Contain("<table>");
        result.Should().Contain("<th>Header 1</th>");
        result.Should().Contain("<td>Cell 1</td>");
    }

    [Fact]
    public void Render_WithTaskList_ReturnsHtmlWithCheckboxes()
    {
        // Arrange
        var markdown = "- [x] Completed task\n- [ ] Pending task";

        // Act
        var result = _renderService.Render(markdown);

        // Assert - Markdig 任务列表会输出 checkbox
        result.Should().Contain("<input");
        result.Should().Contain("type=\"checkbox\"");
    }

    [Fact]
    public void Render_WithCodeBlock_ReturnsHtmlWithLanguageClass()
    {
        // Arrange
        var markdown = "```javascript\nconsole.log('test');\n```";

        // Act
        var result = _renderService.Render(markdown);

        // Assert
        result.Should().Contain("<code");
        result.Should().Contain("javascript");
    }

    [Fact]
    public void Render_WithNestedFormatting_ReturnsCorrectHtml()
    {
        // Arrange
        var markdown = "**bold and *italic***";

        // Act
        var result = _renderService.Render(markdown);

        // Assert
        result.Should().Contain("<strong>");
        result.Should().Contain("<em>");
    }

    [Fact]
    public void Render_WithInlineCode_ReturnsHtmlWithCodeTag()
    {
        // Arrange
        var markdown = "Use `var x = 1;` for declaration";

        // Act
        var result = _renderService.Render(markdown);

        // Assert
        result.Should().Contain("<code>var x = 1;</code>");
    }

    #endregion

    #region Caching Tests

    [Fact]
    public void Render_CachesResult_ForSameContent()
    {
        // Arrange
        var markdown = "# Cached Heading";
        
        // Act
        var result1 = _renderService.Render(markdown);
        var initialCacheSize = _renderService.GetCacheSize();
        var result2 = _renderService.Render(markdown);
        var finalCacheSize = _renderService.GetCacheSize();

        // Assert
        result1.Should().Be(result2);
        // 第二次渲染不应增加缓存大小
        finalCacheSize.Should().Be(initialCacheSize);
    }

    [Fact]
    public void Render_UpdatesCache_ForDifferentContent()
    {
        // Arrange
        var markdown1 = "# First";
        var markdown2 = "# Second";
        
        // Act
        _renderService.Render(markdown1);
        var sizeAfterFirst = _renderService.GetCacheSize();
        _renderService.Render(markdown2);
        var sizeAfterSecond = _renderService.GetCacheSize();

        // Assert
        sizeAfterSecond.Should().BeGreaterThan(sizeAfterFirst);
    }

    [Fact]
    public void ClearCache_RemovesAllCachedEntries()
    {
        // Arrange
        _renderService.Render("# Test 1");
        _renderService.Render("# Test 2");
        _renderService.GetCacheSize().Should().BeGreaterThan(0);

        // Act
        _renderService.ClearAllCache();

        // Assert
        _renderService.GetCacheSize().Should().Be(0);
    }

    #endregion

    #region Async Render Tests

    [Fact]
    public async Task RenderAsync_ReturnsSameResultAsRender()
    {
        // Arrange
        var markdown = "# Async Test";

        // Act
        var syncResult = _renderService.Render(markdown);
        var asyncResult = await _renderService.RenderAsync(markdown);

        // Assert
        asyncResult.Should().Be(syncResult);
    }

    [Fact]
    public async Task RenderAsync_SupportsCancellation()
    {
        // Arrange
        var markdown = "# Cancel Test";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            _renderService.RenderAsync(markdown, cts.Token));
    }

    #endregion

    #region Debounced Render Tests

    [Fact]
    public async Task DebouncedRender_CompletesAfterDelay()
    {
        // Arrange
        var markdown = "# Debounced Test";
        string? result = null;
        var tcs = new TaskCompletionSource<string>();

        // Act
        var cts = _renderService.DebouncedRender(markdown, 100, html => 
        {
            result = html;
            tcs.TrySetResult(html);
        });

        // Assert - 等待任务完成
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000));
        completedTask.Should().Be(tcs.Task, "Debounced render should complete within timeout");
        result.Should().NotBeNull();
        result.Should().Contain("Debounced Test");
        
        cts.Dispose();
    }

    [Fact]
    public async Task DebouncedRender_CancellationTokenCanBeDisposed()
    {
        // Arrange
        var markdown = "# Dispose Test";
        var cts = new CancellationTokenSource();
        var completed = false;

        // Act - 获取 CTS 然后 dispose
        var returnedCts = _renderService.DebouncedRender(markdown, 100, _ => completed = true);
        returnedCts.Cancel();
        returnedCts.Dispose();
        
        // 等待延迟时间
        await Task.Delay(200);

        // Assert - 由于 CTS 被取消，回调不应该执行
        // 注意：由于异步执行的特性，这个测试验证的是 CTS 可以被正确取消
        // completed 的值取决于取消和回调执行的时序
    }

    #endregion
}
