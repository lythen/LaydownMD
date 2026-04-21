using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Services;
using Xunit;

namespace LythenMarkdown.Tests.Services;

public class HelpServiceTests
{
    private readonly HelpService _helpService;

    public HelpServiceTests()
    {
        _helpService = new HelpService();
    }

    #region GetKeyboardShortcuts Tests

    [Fact]
    public void GetKeyboardShortcuts_ReturnsNonEmptyList()
    {
        // Act
        var shortcuts = _helpService.GetKeyboardShortcuts();

        // Assert
        shortcuts.Should().NotBeEmpty();
    }

    [Fact]
    public void GetKeyboardShortcuts_ContainsExpectedShortcuts()
    {
        // Act
        var shortcuts = _helpService.GetKeyboardShortcuts();
        var shortcutActions = shortcuts.Select(s => s.Action).ToList();

        // Assert
        shortcutActions.Should().Contain("新建文件");
        shortcutActions.Should().Contain("打开文件");
        shortcutActions.Should().Contain("保存文件");
        shortcutActions.Should().Contain("关闭标签");
    }

    [Fact]
    public void GetKeyboardShortcuts_EachShortcutHasPlatformBindings()
    {
        // Act
        var shortcuts = _helpService.GetKeyboardShortcuts();

        // Assert
        foreach (var shortcut in shortcuts)
        {
            shortcut.Windows.Should().NotBeNullOrEmpty();
            shortcut.Mac.Should().NotBeNullOrEmpty();
            shortcut.Linux.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void GetKeyboardShortcuts_ReturnsReadOnlyList()
    {
        // Act
        var shortcuts = _helpService.GetKeyboardShortcuts();

        // Assert
        shortcuts.Should().BeAssignableTo<IReadOnlyList<KeyboardShortcut>>();
    }

    [Fact]
    public void GetKeyboardShortcuts_ContainsFormattingShortcuts()
    {
        // Act
        var shortcuts = _helpService.GetKeyboardShortcuts();
        var shortcutActions = shortcuts.Select(s => s.Action).ToList();

        // Assert
        shortcutActions.Should().Contain("粗体");
        shortcutActions.Should().Contain("斜体");
    }

    [Fact]
    public void GetKeyboardShortcuts_ContainsViewModeShortcuts()
    {
        // Act
        var shortcuts = _helpService.GetKeyboardShortcuts();
        var shortcutActions = shortcuts.Select(s => s.Action).ToList();

        // Assert
        shortcutActions.Should().Contain("编辑模式");
        shortcutActions.Should().Contain("分栏模式");
        shortcutActions.Should().Contain("预览模式");
    }

    #endregion

    #region Show Methods Tests (Basic smoke tests)

    [Fact]
    public void ShowIntroduction_DoesNotThrow()
    {
        // Act & Assert
        _helpService.Invoking(s => s.ShowIntroduction()).Should().NotThrow();
    }

    [Fact]
    public void ShowUserGuide_DoesNotThrow()
    {
        // Act & Assert
        _helpService.Invoking(s => s.ShowUserGuide()).Should().NotThrow();
    }

    [Fact]
    public void ShowAbout_DoesNotThrow()
    {
        // Act & Assert
        _helpService.Invoking(s => s.ShowAbout()).Should().NotThrow();
    }

    #endregion

    #region Update Methods Tests

    [Fact]
    public async Task CheckForUpdatesAsync_ReturnsNull()
    {
        // Act
        var result = await _helpService.CheckForUpdatesAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DownloadAndInstallUpdateAsync_ReturnsFalse()
    {
        // Arrange
        var update = new UpdateInfo("2.0.0", "https://example.com/update", "Change log", 1024, "hash");

        // Act
        var result = await _helpService.DownloadAndInstallUpdateAsync(update);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsUpdateAvailable_ReturnsFalse()
    {
        // Act
        var result = _helpService.IsUpdateAvailable();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region KeyboardShortcut Record Tests

    [Fact]
    public void KeyboardShortcut_CanBeCreated()
    {
        // Arrange & Act
        var shortcut = new KeyboardShortcut("Test Action", "Ctrl+T", "Cmd+T", "Ctrl+T");

        // Assert
        shortcut.Action.Should().Be("Test Action");
        shortcut.Windows.Should().Be("Ctrl+T");
        shortcut.Mac.Should().Be("Cmd+T");
        shortcut.Linux.Should().Be("Ctrl+T");
    }

    #endregion

    #region UpdateInfo Record Tests

    [Fact]
    public void UpdateInfo_CanBeCreated()
    {
        // Arrange & Act
        var updateInfo = new UpdateInfo("1.0.0", "https://example.com", "Changes", 1024, "hash");

        // Assert
        updateInfo.Version.Should().Be("1.0.0");
        updateInfo.DownloadUrl.Should().Be("https://example.com");
        updateInfo.ChangeLog.Should().Be("Changes");
        updateInfo.FileSize.Should().Be(1024);
        updateInfo.Sha256Hash.Should().Be("hash");
    }

    #endregion
}
