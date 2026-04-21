using System;
using Avalonia;
using Avalonia.Styling;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.Core.Services;

/// <summary>
/// 主题服务实现
/// </summary>
public class ThemeService : IThemeService
{
    private ThemeMode _currentTheme = ThemeMode.System;

    public event EventHandler<ThemeMode>? ThemeChanged;

    public void SetTheme(ThemeMode mode)
    {
        _currentTheme = mode;
        ApplyTheme(mode);
        ThemeChanged?.Invoke(this, mode);
    }

    public ThemeMode GetCurrentTheme()
    {
        return _currentTheme;
    }

    public void ToggleTheme()
    {
        var newTheme = _currentTheme switch
        {
            ThemeMode.Light => ThemeMode.Dark,
            ThemeMode.Dark => ThemeMode.Light,
            ThemeMode.System => GetSystemTheme(),
            _ => ThemeMode.System
        };

        SetTheme(newTheme);
    }

    private void ApplyTheme(ThemeMode mode)
    {
        if (Application.Current == null) return;

        var actualTheme = mode == ThemeMode.System ? GetSystemTheme() : mode;
        
        // 根据主题模式设置 Application theme
        // Avalonia 使用 FluentTheme，支持 Light/Dark 模式
        Application.Current.RequestedThemeVariant = actualTheme switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private static ThemeMode GetSystemTheme()
    {
        if (Application.Current?.RequestedThemeVariant == ThemeVariant.Dark)
        {
            return ThemeMode.Dark;
        }
        return ThemeMode.Light;
    }
}
