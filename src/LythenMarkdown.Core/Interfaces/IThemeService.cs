using System;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.Core.Interfaces;

/// <summary>
/// 主题服务接口
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// 设置主题
    /// </summary>
    void SetTheme(ThemeMode mode);

    /// <summary>
    /// 获取当前主题
    /// </summary>
    ThemeMode GetCurrentTheme();

    /// <summary>
    /// 切换到下一个主题
    /// </summary>
    void ToggleTheme();

    /// <summary>
    /// 主题变更时事件
    /// </summary>
    event EventHandler<ThemeMode>? ThemeChanged;
}
