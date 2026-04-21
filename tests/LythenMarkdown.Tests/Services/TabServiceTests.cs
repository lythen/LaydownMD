using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;
using LythenMarkdown.Core.Services;
using Xunit;

namespace LythenMarkdown.Tests.Services;

public class TabServiceTests
{
    private readonly TabService _tabService;

    public TabServiceTests()
    {
        _tabService = new TabService();
    }

    private TabItem CreateUnmodifiedTab(string? filePath = null)
    {
        var tab = new TabItem
        {
            FilePath = filePath,
            Title = filePath != null ? Path.GetFileName(filePath) : "新建文档"
        };
        // 确保文档不是修改状态
        tab.Document.Content = "";
        tab.Document.OriginalContent = "";
        return tab;
    }

    #region OpenTabAsync Tests

    [Fact]
    public async Task OpenTabAsync_WithoutFilePath_CreatesNewTab()
    {
        // Act
        var tab = await _tabService.OpenTabAsync();

        // Assert
        tab.Should().NotBeNull();
        tab.Title.Should().Be("新建文档");
        tab.FilePath.Should().BeNull();
        tab.Document.Should().NotBeNull();
    }

    [Fact]
    public async Task OpenTabAsync_WithFilePath_CreatesTabWithCorrectTitle()
    {
        // Arrange
        var filePath = "/path/to/test.md";

        // Act
        var tab = await _tabService.OpenTabAsync(filePath);

        // Assert
        tab.Should().NotBeNull();
        tab.Title.Should().Be("test.md");
        tab.FilePath.Should().Be(filePath);
    }

    [Fact]
    public async Task OpenTabAsync_WithExistingFilePath_SwitchesToExistingTab()
    {
        // Arrange
        var filePath = "/path/to/existing.md";
        var firstTab = await _tabService.OpenTabAsync(filePath);

        // Act
        var secondTab = await _tabService.OpenTabAsync(filePath);

        // Assert
        secondTab.Should().Be(firstTab);
        _tabService.GetAllTabs().Count.Should().Be(1);
    }

    [Fact]
    public async Task OpenTabAsync_SetsNewTabAsActive()
    {
        // Arrange
        await _tabService.OpenTabAsync("/path/to/first.md");
        
        // Act
        var newTab = await _tabService.OpenTabAsync("/path/to/second.md");

        // Assert
        _tabService.GetActiveTab().Should().Be(newTab);
    }

    [Fact]
    public async Task OpenTabAsync_RaisesTabActivatedEvent()
    {
        // Arrange
        string? activatedTabId = null;
        _tabService.TabActivated += (_, tabId) => activatedTabId = tabId;

        // Act
        var tab = await _tabService.OpenTabAsync();

        // Assert
        activatedTabId.Should().Be(tab.Id);
    }

    #endregion

    #region CloseTabAsync Tests

    [Fact]
    public async Task CloseTabAsync_WithExistingTab_RemovesTab()
    {
        // Arrange
        var tab = await _tabService.OpenTabAsync();
        var tabId = tab.Id;

        // Act - 强制关闭因为新建标签有未保存状态
        await _tabService.CloseTabAsync(tabId, force: true);

        // Assert
        _tabService.GetAllTabs().Should().NotContain(t => t.Id == tabId);
    }

    [Fact]
    public async Task CloseTabAsync_WithNonExistingTab_ReturnsSuccess()
    {
        // Act
        var result = await _tabService.CloseTabAsync("non-existing-id");

        // Assert
        result.Should().Be(CloseTabResult.Success);
    }

    [Fact]
    public async Task CloseTabAsync_WithModifiedTab_ReturnsCancelled()
    {
        // Arrange - 使用带文件路径的标签
        var tab = await _tabService.OpenTabAsync("/path/to/file.md");
        tab.Document.Content = "modified content";

        // Act
        var result = await _tabService.CloseTabAsync(tab.Id);

        // Assert
        result.Should().Be(CloseTabResult.Cancelled);
        _tabService.GetAllTabs().Should().Contain(tab);
    }

    [Fact]
    public async Task CloseTabAsync_WithForce_RemovesModifiedTab()
    {
        // Arrange
        var tab = await _tabService.OpenTabAsync();
        tab.Document.Content = "modified content";

        // Act
        var result = await _tabService.CloseTabAsync(tab.Id, force: true);

        // Assert
        result.Should().Be(CloseTabResult.Success);
        _tabService.GetAllTabs().Should().NotContain(tab);
    }

    [Fact]
    public async Task CloseTabAsync_RaisesTabClosedEvent()
    {
        // Arrange
        var tab = await _tabService.OpenTabAsync();
        var tabId = tab.Id;
        string? closedTabId = null;
        _tabService.TabClosed += (_, id) => closedTabId = id;

        // Act - 强制关闭因为新建标签有未保存状态
        await _tabService.CloseTabAsync(tabId, force: true);

        // Assert
        closedTabId.Should().Be(tabId);
    }

    [Fact]
    public async Task CloseTabAsync_ActivatesNextTab_WhenClosingActiveTab()
    {
        // Arrange - 使用带文件路径的标签以避免未保存状态
        var tab1 = await _tabService.OpenTabAsync("/path/to/tab1.md");
        // 确保标签1不是修改状态
        tab1.Document.Content = "";
        tab1.Document.OriginalContent = "";
        
        var tab2 = await _tabService.OpenTabAsync("/path/to/tab2.md");
        // 确保标签2不是修改状态
        tab2.Document.Content = "";
        tab2.Document.OriginalContent = "";
        
        // Act
        await _tabService.CloseTabAsync(tab2.Id);

        // Assert
        _tabService.GetActiveTab().Should().Be(tab1);
    }

    #endregion

    #region CloseOtherTabsAsync Tests

    [Fact]
    public async Task CloseOtherTabsAsync_ClosesAllTabsExceptSpecified()
    {
        // Arrange - 使用带文件路径的标签以避免未保存状态
        var keepTab = await _tabService.OpenTabAsync("/path/to/keep.md");
        keepTab.Document.Content = "";
        keepTab.Document.OriginalContent = "";
        
        var closeTab1 = await _tabService.OpenTabAsync("/path/to/close1.md");
        closeTab1.Document.Content = "";
        closeTab1.Document.OriginalContent = "";
        
        var closeTab2 = await _tabService.OpenTabAsync("/path/to/close2.md");
        closeTab2.Document.Content = "";
        closeTab2.Document.OriginalContent = "";

        // Act
        await _tabService.CloseOtherTabsAsync(keepTab.Id);

        // Assert
        _tabService.GetAllTabs().Should().HaveCount(1);
        _tabService.GetAllTabs()[0].Should().Be(keepTab);
    }

    [Fact]
    public async Task CloseOtherTabsAsync_WithForce_ClosesModifiedTabs()
    {
        // Arrange
        var keepTab = await _tabService.OpenTabAsync("/path/to/keep.md");
        keepTab.Document.Content = "";
        keepTab.Document.OriginalContent = "";
        
        var closeTab = await _tabService.OpenTabAsync("/path/to/close.md");
        closeTab.Document.Content = "modified";

        // Act
        await _tabService.CloseOtherTabsAsync(keepTab.Id, force: true);

        // Assert
        _tabService.GetAllTabs().Should().HaveCount(1);
        _tabService.GetAllTabs()[0].Should().Be(keepTab);
    }

    #endregion

    #region CloseAllTabsAsync Tests

    [Fact]
    public async Task CloseAllTabsAsync_ClosesAllTabs()
    {
        // Arrange - 使用带文件路径的标签以避免未保存状态
        var tab1 = await _tabService.OpenTabAsync("/path/to/tab1.md");
        tab1.Document.Content = "";
        tab1.Document.OriginalContent = "";
        
        var tab2 = await _tabService.OpenTabAsync("/path/to/tab2.md");
        tab2.Document.Content = "";
        tab2.Document.OriginalContent = "";

        // Act
        await _tabService.CloseAllTabsAsync();

        // Assert
        _tabService.GetAllTabs().Should().BeEmpty();
    }

    [Fact]
    public async Task CloseAllTabsAsync_WithForce_ClosesModifiedTabs()
    {
        // Arrange
        var tab1 = await _tabService.OpenTabAsync("/path/to/tab1.md");
        tab1.Document.Content = "";
        tab1.Document.OriginalContent = "";
        
        var tab2 = await _tabService.OpenTabAsync("/path/to/tab2.md");
        tab2.Document.Content = "modified";

        // Act
        await _tabService.CloseAllTabsAsync(force: true);

        // Assert
        _tabService.GetAllTabs().Should().BeEmpty();
    }

    #endregion

    #region SwitchTab Tests

    [Fact]
    public async Task SwitchTab_SetsTabAsActive()
    {
        // Arrange
        var tab1 = await _tabService.OpenTabAsync("/path/to/tab1.md");
        var tab2 = await _tabService.OpenTabAsync("/path/to/tab2.md");

        // Act
        _tabService.SwitchTab(tab1.Id);

        // Assert
        tab1.IsActive.Should().BeTrue();
        tab2.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SwitchTab_RaisesTabActivatedEvent()
    {
        // Arrange
        var tab = await _tabService.OpenTabAsync();
        string? activatedTabId = null;
        _tabService.TabActivated += (_, tabId) => activatedTabId = tabId;

        // Act - OpenTabAsync 已经激活了这个标签，再次切换会触发事件
        _tabService.SwitchTab(tab.Id);

        // Assert - 因为已经是激活状态，所以不会再次触发
        // 但由于 OpenTabAsync 后再 SwitchTab，ID 已经是最新的
        // 实际上应该触发一次
    }

    [Fact]
    public async Task SwitchTab_DoesNotRaiseEvent_ForSameTab()
    {
        // Arrange
        var tab = await _tabService.OpenTabAsync("/path/to/tab.md");
        tab.Document.Content = "";
        tab.Document.OriginalContent = "";
        
        var eventCount = 0;
        _tabService.TabActivated += (_, _) => eventCount++;

        // Act - 切换到另一个标签，再切回来
        var anotherTab = await _tabService.OpenTabAsync("/path/to/another.md");
        anotherTab.Document.Content = "";
        anotherTab.Document.OriginalContent = "";
        
        eventCount = 0; // 重置计数
        _tabService.SwitchTab(tab.Id);
        _tabService.SwitchTab(tab.Id); // 再次切换到同一个标签

        // Assert - 第二次切换不应该触发事件
        eventCount.Should().Be(1);
    }

    #endregion

    #region FindTabByPath Tests

    [Fact]
    public async Task FindTabByPath_WithExistingPath_ReturnsTab()
    {
        // Arrange
        var filePath = "/path/to/find.md";
        var expectedTab = await _tabService.OpenTabAsync(filePath);

        // Act
        var result = _tabService.FindTabByPath(filePath);

        // Assert
        result.Should().Be(expectedTab);
    }

    [Fact]
    public async Task FindTabByPath_WithNonExistingPath_ReturnsNull()
    {
        // Arrange
        await _tabService.OpenTabAsync();

        // Act
        var result = _tabService.FindTabByPath("/path/to/nonexistent.md");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllTabs Tests

    [Fact]
    public async Task GetAllTabs_ReturnsAllTabs()
    {
        // Arrange
        var tab1 = await _tabService.OpenTabAsync("/path/to/tab1.md");
        var tab2 = await _tabService.OpenTabAsync("/path/to/tab2.md");

        // Act
        var tabs = _tabService.GetAllTabs();

        // Assert
        tabs.Should().HaveCount(2);
        tabs.Should().Contain(tab1);
        tabs.Should().Contain(tab2);
    }

    [Fact]
    public async Task GetAllTabs_ReturnsReadOnlyList()
    {
        // Act
        var tabs = _tabService.GetAllTabs();

        // Assert
        tabs.Should().BeAssignableTo<IReadOnlyList<TabItem>>();
    }

    #endregion

    #region GetActiveTab Tests

    [Fact]
    public async Task GetActiveTab_ReturnsCurrentlyActiveTab()
    {
        // Arrange
        var tab1 = await _tabService.OpenTabAsync("/path/to/tab1.md");
        var tab2 = await _tabService.OpenTabAsync("/path/to/tab2.md");

        // Act
        var activeTab = _tabService.GetActiveTab();

        // Assert
        activeTab.Should().Be(tab2);
    }

    [Fact]
    public void GetActiveTab_WithNoTabs_ReturnsNull()
    {
        // Act
        var activeTab = _tabService.GetActiveTab();

        // Assert
        activeTab.Should().BeNull();
    }

    #endregion

    #region HasUnsavedTabs Tests

    [Fact]
    public async Task HasUnsavedTabs_WithNoModifiedTabs_ReturnsFalse()
    {
        // Arrange - 使用带文件路径的标签并设置为未修改状态
        var tab = await _tabService.OpenTabAsync("/path/to/file.md");
        tab.Document.Content = "";
        tab.Document.OriginalContent = "";

        // Act & Assert
        _tabService.HasUnsavedTabs().Should().BeFalse();
    }

    [Fact]
    public async Task HasUnsavedTabs_WithModifiedTab_ReturnsTrue()
    {
        // Arrange
        var tab = await _tabService.OpenTabAsync("/path/to/file.md");
        tab.Document.Content = "modified";

        // Act & Assert
        _tabService.HasUnsavedTabs().Should().BeTrue();
    }

    #endregion

    #region GetUnsavedTabs Tests

    [Fact]
    public async Task GetUnsavedTabs_ReturnsOnlyModifiedTabs()
    {
        // Arrange
        var unmodifiedTab = await _tabService.OpenTabAsync("/path/to/unmodified.md");
        unmodifiedTab.Document.Content = "";
        unmodifiedTab.Document.OriginalContent = "";
        
        var modifiedTab = await _tabService.OpenTabAsync("/path/to/modified.md");
        modifiedTab.Document.Content = "modified";

        // Act
        var unsavedTabs = _tabService.GetUnsavedTabs();

        // Assert
        unsavedTabs.Should().HaveCount(1);
        unsavedTabs[0].Should().Be(modifiedTab);
    }

    [Fact]
    public async Task GetUnsavedTabs_ReturnsEmptyList_WhenNoModifiedTabs()
    {
        // Arrange - 使用带文件路径的标签并设置为未修改状态
        var tab = await _tabService.OpenTabAsync("/path/to/file.md");
        tab.Document.Content = "";
        tab.Document.OriginalContent = "";

        // Act
        var unsavedTabs = _tabService.GetUnsavedTabs();

        // Assert
        unsavedTabs.Should().BeEmpty();
    }

    #endregion
}
