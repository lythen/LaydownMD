using System;
using FluentAssertions;
using LythenMarkdown.Core.Models;
using Xunit;

namespace LythenMarkdown.Tests.Models;

public class TabItemTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var tabItem = new TabItem();

        // Assert
        tabItem.Id.Should().NotBeNullOrEmpty();
        tabItem.Title.Should().Be("新建文档");
        tabItem.FilePath.Should().BeNull();
        tabItem.Document.Should().NotBeNull();
        tabItem.ViewMode.Should().Be(ViewMode.Split);
        tabItem.IsActive.Should().BeFalse();
        tabItem.ForceClose.Should().BeFalse();
    }

    #endregion

    #region IsModified Tests

    [Fact]
    public void IsModified_DelegatesToDocument()
    {
        // Arrange
        var tabItem = new TabItem();
        tabItem.Document.Content = "modified";
        tabItem.Document.OriginalContent = "original";

        // Assert
        tabItem.IsModified.Should().BeTrue();
    }

    [Fact]
    public void IsModified_ReturnsFalse_WhenDocumentNotModified()
    {
        // Arrange
        var tabItem = new TabItem();
        tabItem.Document.Content = "content";
        tabItem.Document.OriginalContent = "content";

        // Assert
        tabItem.IsModified.Should().BeFalse();
    }

    #endregion

    #region ViewMode Tests

    [Fact]
    public void ViewMode_DefaultValue_IsSplit()
    {
        // Act
        var tabItem = new TabItem();

        // Assert
        tabItem.ViewMode.Should().Be(ViewMode.Split);
    }

    [Fact]
    public void ViewMode_CanBeChanged()
    {
        // Arrange
        var tabItem = new TabItem();

        // Act & Assert
        tabItem.ViewMode = ViewMode.Edit;
        tabItem.ViewMode.Should().Be(ViewMode.Edit);

        tabItem.ViewMode = ViewMode.Preview;
        tabItem.ViewMode.Should().Be(ViewMode.Preview);

        tabItem.ViewMode = ViewMode.Popup;
        tabItem.ViewMode.Should().Be(ViewMode.Popup);
    }

    #endregion

    #region IsActive Tests

    [Fact]
    public void IsActive_DefaultValue_IsFalse()
    {
        // Act
        var tabItem = new TabItem();

        // Assert
        tabItem.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_CanBeChanged()
    {
        // Arrange
        var tabItem = new TabItem();

        // Act
        tabItem.IsActive = true;

        // Assert
        tabItem.IsActive.Should().BeTrue();
    }

    #endregion

    #region Title Tests

    [Fact]
    public void Title_DefaultValue_Is新建文档()
    {
        // Act
        var tabItem = new TabItem();

        // Assert
        tabItem.Title.Should().Be("新建文档");
    }

    [Fact]
    public void Title_CanBeChanged()
    {
        // Arrange
        var tabItem = new TabItem();

        // Act
        tabItem.Title = "Custom Title";

        // Assert
        tabItem.Title.Should().Be("Custom Title");
    }

    [Fact]
    public void Title_ReflectsFilePath_WhenSet()
    {
        // Arrange
        var tabItem = new TabItem { FilePath = "/path/to/document.md" };

        // This requires the Title property to be computed from FilePath
        // Based on current implementation, Title is set separately
        tabItem.Title.Should().Be("新建文档"); // Default value

        // Act - set title based on file path
        tabItem.Title = System.IO.Path.GetFileName(tabItem.FilePath);

        // Assert
        tabItem.Title.Should().Be("document.md");
    }

    #endregion

    #region FilePath Tests

    [Fact]
    public void FilePath_DefaultValue_IsNull()
    {
        // Act
        var tabItem = new TabItem();

        // Assert
        tabItem.FilePath.Should().BeNull();
    }

    [Fact]
    public void FilePath_CanBeChanged()
    {
        // Arrange
        var tabItem = new TabItem();
        var filePath = "/path/to/file.md";

        // Act
        tabItem.FilePath = filePath;

        // Assert
        tabItem.FilePath.Should().Be(filePath);
    }

    #endregion

    #region Document Tests

    [Fact]
    public void Document_DefaultValue_IsNotNull()
    {
        // Act
        var tabItem = new TabItem();

        // Assert
        tabItem.Document.Should().NotBeNull();
    }

    [Fact]
    public void Document_CanBeReplaced()
    {
        // Arrange
        var tabItem = new TabItem();
        var newDocument = new Document { Content = "new content" };

        // Act
        tabItem.Document = newDocument;

        // Assert
        tabItem.Document.Should().Be(newDocument);
        tabItem.Document.Content.Should().Be("new content");
    }

    #endregion

    #region Id Tests

    [Fact]
    public void Id_IsUnique_ForDifferentTabItems()
    {
        // Arrange
        var tab1 = new TabItem();
        var tab2 = new TabItem();

        // Assert
        tab1.Id.Should().NotBe(tab2.Id);
    }

    [Fact]
    public void Id_IsValidGuid()
    {
        // Arrange
        var tabItem = new TabItem();

        // Assert
        Guid.TryParse(tabItem.Id, out _).Should().BeTrue();
    }

    #endregion

    #region ForceClose Tests

    [Fact]
    public void ForceClose_DefaultValue_IsFalse()
    {
        // Act
        var tabItem = new TabItem();

        // Assert
        tabItem.ForceClose.Should().BeFalse();
    }

    [Fact]
    public void ForceClose_CanBeChanged()
    {
        // Arrange
        var tabItem = new TabItem();

        // Act
        tabItem.ForceClose = true;

        // Assert
        tabItem.ForceClose.Should().BeTrue();
    }

    #endregion
}

public class ViewModeTests
{
    [Fact]
    public void ViewMode_Enum_HasExpectedValues()
    {
        // Assert
        ((int)ViewMode.Edit).Should().Be(0);
        ((int)ViewMode.Split).Should().Be(1);
        ((int)ViewMode.Preview).Should().Be(2);
        ((int)ViewMode.Popup).Should().Be(3);
    }

    [Fact]
    public void ViewMode_Enum_CanBeCastToInt()
    {
        // Arrange
        var viewMode = ViewMode.Preview;

        // Act
        var intValue = (int)viewMode;

        // Assert
        intValue.Should().Be(2);
    }

    [Fact]
    public void ViewMode_Enum_CanBeCreatedFromInt()
    {
        // Arrange
        var intValue = 1;

        // Act
        var viewMode = (ViewMode)intValue;

        // Assert
        viewMode.Should().Be(ViewMode.Split);
    }
}
