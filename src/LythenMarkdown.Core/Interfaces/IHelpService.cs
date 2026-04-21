using System.Collections.Generic;
using System.Threading.Tasks;

namespace LythenMarkdown.Core.Interfaces;

/// <summary>
/// 帮助服务接口
/// </summary>
public interface IHelpService
{
    /// <summary>
    /// 显示软件介绍
    /// </summary>
    void ShowIntroduction();

    /// <summary>
    /// 显示使用说明
    /// </summary>
    void ShowUserGuide();

    /// <summary>
    /// 显示关于对话框
    /// </summary>
    void ShowAbout();

    /// <summary>
    /// 获取快捷键列表
    /// </summary>
    IReadOnlyList<KeyboardShortcut> GetKeyboardShortcuts();

    // ========== 在线更新功能 (P2) ==========

    /// <summary>
    /// 检查更新
    /// </summary>
    Task<UpdateInfo?> CheckForUpdatesAsync();

    /// <summary>
    /// 下载并安装更新
    /// </summary>
    Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo update, IProgress<int>? progress = null);

    /// <summary>
    /// 获取当前版本是否有可用更新
    /// </summary>
    bool IsUpdateAvailable();
}

/// <summary>
/// 键盘快捷键
/// </summary>
public record KeyboardShortcut(
    string Action,
    string Windows,
    string Mac,
    string Linux
);

/// <summary>
/// 更新信息
/// </summary>
public record UpdateInfo(
    string Version,
    string DownloadUrl,
    string ChangeLog,
    long FileSize,
    string Sha256Hash
);
