namespace LythenMarkdown.Core.Models;

/// <summary>
/// 主题模式
/// </summary>
public enum ThemeMode
{
    Light = 0,
    Dark = 1,
    System = 2
}

/// <summary>
/// 应用设置模型
/// </summary>
public class AppSettings
{
    /// <summary>
    /// 主题模式
    /// </summary>
    public ThemeMode Theme { get; set; } = ThemeMode.System;

    /// <summary>
    /// 语言代码
    /// </summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    /// 编辑器字体大小
    /// </summary>
    public double FontSize { get; set; } = 14;

    /// <summary>
    /// 编辑器字体
    /// </summary>
    public string FontFamily { get; set; } = "Consolas, monospace";

    /// <summary>
    /// 是否显示行号
    /// </summary>
    public bool ShowLineNumbers { get; set; } = true;

    /// <summary>
    /// 是否启用同步滚动
    /// </summary>
    public bool EnableSyncScroll { get; set; } = true;

    /// <summary>
    /// 自动保存间隔（秒）
    /// </summary>
    public int AutoSaveIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 是否启用自动保存
    /// </summary>
    public bool EnableAutoSave { get; set; } = true;

    /// <summary>
    /// 最近打开的文件列表
    /// </summary>
    public List<string> RecentFiles { get; set; } = new();

    /// <summary>
    /// 最大最近文件数量
    /// </summary>
    public int MaxRecentFiles { get; set; } = 10;
}

/// <summary>
/// 语言信息
/// </summary>
public record LanguageInfo(
    string Code,
    string DisplayName,
    string NativeName
)
{
    public static readonly LanguageInfo[] AvailableLanguages =
    [
        new("zh-CN", "简体中文", "简体中文"),
        new("en-US", "English", "English")
    ];
}
