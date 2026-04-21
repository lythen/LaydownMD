using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;
using LythenMarkdown.Core.Services;
using Xunit;

namespace LythenMarkdown.Tests.Services;

public class FileServiceTests : IDisposable
{
    private readonly FileService _fileService;
    private readonly string _testDirectory;

    public FileServiceTests()
    {
        _fileService = new FileService();
        _testDirectory = Path.Combine(Path.GetTempPath(), "LythenMarkdownTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    #region OpenFileAsync Tests

    [Fact]
    public async Task OpenFileAsync_WithValidFile_ReturnsDocument()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.md");
        var content = "# Test Content";
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var document = await _fileService.OpenFileAsync(filePath);

        // Assert
        document.Should().NotBeNull();
        document.Content.Should().Be(content);
        document.FilePath.Should().Be(filePath);
        document.OriginalContent.Should().Be(content);
    }

    [Fact]
    public async Task OpenFileAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.md");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _fileService.OpenFileAsync(filePath));
    }

    [Fact]
    public async Task OpenFileAsync_WithLargeFile_ThrowsInvalidOperationException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "large.md");
        // 创建一个超过10MB的文件
        var largeContent = new string('a', 11 * 1024 * 1024);
        await File.WriteAllTextAsync(filePath, largeContent);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _fileService.OpenFileAsync(filePath));
    }

    #endregion

    #region SaveFileAsync Tests

    [Fact]
    public async Task SaveFileAsync_WithValidDocument_SavesContentToFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "save_test.md");
        var document = new Document
        {
            Content = "# Saved Content",
            OriginalContent = ""
        };

        // Act
        await _fileService.SaveFileAsync(document, filePath);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var savedContent = await File.ReadAllTextAsync(filePath);
        savedContent.Should().Be("# Saved Content");
        document.OriginalContent.Should().Be("# Saved Content");
    }

    [Fact]
    public async Task SaveFileAsync_WithNullPath_ThrowsInvalidOperationException()
    {
        // Arrange
        var document = new Document { Content = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _fileService.SaveFileAsync(document, null!));
    }

    [Fact]
    public async Task SaveFileAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "subdir");
        var filePath = Path.Combine(subDir, "test.md");
        var document = new Document { Content = "test" };

        // Act
        await _fileService.SaveFileAsync(document, filePath);

        // Assert
        Directory.Exists(subDir).Should().BeTrue();
        File.Exists(filePath).Should().BeTrue();
    }

    #endregion

    #region FileExists Tests

    [Fact]
    public void FileExists_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "exists.md");
        File.WriteAllText(filePath, "test");

        // Act & Assert
        _fileService.FileExists(filePath).Should().BeTrue();
    }

    [Fact]
    public void FileExists_WithNonExistingFile_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "not_exists.md");

        // Act & Assert
        _fileService.FileExists(filePath).Should().BeFalse();
    }

    #endregion

    #region ValidateFile Tests

    [Fact]
    public void ValidateFile_WithValidMarkdownFile_ReturnsValid()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "valid.md");
        File.WriteAllText(filePath, "# Test");

        // Act
        var (isValid, errorMessage) = _fileService.ValidateFile(filePath);

        // Assert
        isValid.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateFile_WithNonExistentFile_ReturnsInvalid()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "not_exists.md");

        // Act
        var (isValid, errorMessage) = _fileService.ValidateFile(filePath);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("文件不存在");
    }

    [Fact]
    public void ValidateFile_WithNonMarkdownFile_ReturnsInvalid()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "test");

        // Act
        var (isValid, errorMessage) = _fileService.ValidateFile(filePath);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("仅支持 .md 或 .markdown 文件");
    }

    [Fact]
    public void ValidateFile_WithLargeFile_ReturnsInvalid()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "large.md");
        var largeContent = new string('a', 11 * 1024 * 1024);
        File.WriteAllText(filePath, largeContent);

        // Act
        var (isValid, errorMessage) = _fileService.ValidateFile(filePath);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("文件过大");
    }

    #endregion

    #region GetFileInfoAsync Tests

    [Fact]
    public async Task GetFileInfoAsync_WithExistingFile_ReturnsFileInfo()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "info.md");
        File.WriteAllText(filePath, "test");
        var expectedInfo = new FileInfo(filePath);

        // Act
        var result = await _fileService.GetFileInfoAsync(filePath);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(expectedInfo.Name);
        result.Length.Should().Be(expectedInfo.Length);
    }

    [Fact]
    public async Task GetFileInfoAsync_WithNonExistentFile_ReturnsNull()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "not_exists.md");

        // Act
        var result = await _fileService.GetFileInfoAsync(filePath);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsFileModifiedSinceOpenAsync Tests

    [Fact]
    public async Task IsFileModifiedSinceOpenAsync_WithUnmodifiedFile_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, " unmodified.md");
        await File.WriteAllTextAsync(filePath, "# Original");
        var document = await _fileService.OpenFileAsync(filePath);

        // Act
        var result = await _fileService.IsFileModifiedSinceOpenAsync(document);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsFileModifiedSinceOpenAsync_WithModifiedFile_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "modified.md");
        await File.WriteAllTextAsync(filePath, "# Original");
        var document = await _fileService.OpenFileAsync(filePath);
        
        // 模拟外部修改
        await File.WriteAllTextAsync(filePath, "# Changed");

        // Act
        var result = await _fileService.IsFileModifiedSinceOpenAsync(document);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFileModifiedSinceOpenAsync_WithNewDocument_ReturnsFalse()
    {
        // Arrange
        var document = new Document { Content = "new content" };

        // Act
        var result = await _fileService.IsFileModifiedSinceOpenAsync(document);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
