using System;
using FluentAssertions;
using LythenMarkdown.Core.Models;
using Xunit;

namespace LythenMarkdown.Tests.Models;

public class DocumentTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var document = new Document();

        // Assert
        document.Id.Should().NotBeNullOrEmpty();
        document.FilePath.Should().BeNull();
        document.Content.Should().BeEmpty();
        document.OriginalContent.Should().BeNull();
        document.CursorPosition.Should().Be(0);
        document.ScrollOffsetY.Should().Be(0);
        document.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        document.ModifiedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region IsModified Tests

    [Fact]
    public void IsModified_WithSameContent_ReturnsFalse()
    {
        // Arrange
        var document = new Document
        {
            Content = "test content",
            OriginalContent = "test content"
        };

        // Assert
        document.IsModified.Should().BeFalse();
    }

    [Fact]
    public void IsModified_WithDifferentContent_ReturnsTrue()
    {
        // Arrange
        var document = new Document
        {
            Content = "new content",
            OriginalContent = "original content"
        };

        // Assert
        document.IsModified.Should().BeTrue();
    }

    [Fact]
    public void IsModified_WithNullOriginalContent_ReturnsTrue()
    {
        // Arrange
        var document = new Document
        {
            Content = "some content",
            OriginalContent = null
        };

        // Assert
        document.IsModified.Should().BeTrue();
    }

    [Fact]
    public void IsModified_WithBothEmpty_ReturnsFalse()
    {
        // Arrange
        var document = new Document
        {
            Content = string.Empty,
            OriginalContent = string.Empty
        };

        // Assert
        document.IsModified.Should().BeFalse();
    }

    #endregion

    #region IsNew Tests

    [Fact]
    public void IsNew_WithNullFilePath_ReturnsTrue()
    {
        // Arrange
        var document = new Document { FilePath = null };

        // Assert
        document.IsNew.Should().BeTrue();
    }

    [Fact]
    public void IsNew_WithEmptyFilePath_ReturnsTrue()
    {
        // Arrange
        var document = new Document { FilePath = string.Empty };

        // Assert
        document.IsNew.Should().BeTrue();
    }

    [Fact]
    public void IsNew_WithFilePath_ReturnsFalse()
    {
        // Arrange
        var document = new Document { FilePath = "/path/to/file.md" };

        // Assert
        document.IsNew.Should().BeFalse();
    }

    #endregion

    #region Title Tests

    [Fact]
    public void Title_WithNullFilePath_Returns新建文档()
    {
        // Arrange
        var document = new Document { FilePath = null };

        // Assert
        document.Title.Should().Be("新建文档");
    }

    [Fact]
    public void Title_WithEmptyFilePath_Returns新建文档()
    {
        // Arrange
        var document = new Document { FilePath = string.Empty };

        // Assert
        document.Title.Should().Be("新建文档");
    }

    [Fact]
    public void Title_WithFilePath_ReturnsFileName()
    {
        // Arrange
        var document = new Document { FilePath = "/path/to/my-document.md" };

        // Assert
        document.Title.Should().Be("my-document.md");
    }

    [Fact]
    public void Title_WithFullPath_ReturnsOnlyFileName()
    {
        // Arrange
        var document = new Document { FilePath = "C:\\Users\\Test\\Documents\\readme.md" };

        // Assert
        document.Title.Should().Be("readme.md");
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new Document
        {
            FilePath = "/path/to/file.md",
            Content = "original content",
            OriginalContent = "original content",
            CursorPosition = 10,
            ScrollOffsetY = 100
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.Id.Should().Be(original.Id);
        clone.FilePath.Should().Be(original.FilePath);
        clone.Content.Should().Be(original.Content);
        clone.OriginalContent.Should().Be(original.OriginalContent);
        clone.CursorPosition.Should().Be(original.CursorPosition);
        clone.ScrollOffsetY.Should().Be(original.ScrollOffsetY);
    }

    [Fact]
    public void Clone_CopiedContentIsIndependent()
    {
        // Arrange
        var original = new Document
        {
            Content = "original content"
        };

        // Act
        var clone = original.Clone();
        clone.Content = "modified content";

        // Assert
        original.Content.Should().Be("original content");
        clone.Content.Should().Be("modified content");
    }

    #endregion

    #region Id Tests

    [Fact]
    public void Id_IsUnique_ForDifferentDocuments()
    {
        // Arrange
        var doc1 = new Document();
        var doc2 = new Document();

        // Assert
        doc1.Id.Should().NotBe(doc2.Id);
    }

    [Fact]
    public void Clone_PreservesId()
    {
        // Arrange
        var original = new Document();
        var originalId = original.Id;

        // Act
        var clone = original.Clone();

        // Assert
        clone.Id.Should().Be(originalId);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void CreatedAt_CanBeModified()
    {
        // Arrange
        var document = new Document();
        var customDate = new DateTime(2020, 1, 1);

        // Act
        document.CreatedAt = customDate;

        // Assert
        document.CreatedAt.Should().Be(customDate);
    }

    [Fact]
    public void ModifiedAt_CanBeModified()
    {
        // Arrange
        var document = new Document();
        var customDate = new DateTime(2021, 6, 15);

        // Act
        document.ModifiedAt = customDate;

        // Assert
        document.ModifiedAt.Should().Be(customDate);
    }

    #endregion
}
