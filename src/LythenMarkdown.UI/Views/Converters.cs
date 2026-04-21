using System;
using System.Globalization;
using Avalonia.Data.Converters;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.UI.Views;

/// <summary>
/// 视图模式到布尔值转换器
/// </summary>
public class ViewModeToBoolConverter : IValueConverter
{
    public ViewMode TargetMode { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ViewMode mode)
        {
            return mode == TargetMode;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
        {
            return TargetMode;
        }
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}

/// <summary>
/// 反向布尔值转换器
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return false;
    }
}

/// <summary>
/// 视图模式枚举到布尔转换器（用于 XAML 资源）
/// </summary>
public static class ViewModeConverters
{
    // 编辑模式转换器
    public static readonly ViewModeToBoolConverter EditMode = new() { TargetMode = ViewMode.Edit };
    
    // 分栏模式转换器
    public static readonly ViewModeToBoolConverter SplitMode = new() { TargetMode = ViewMode.Split };
    
    // 预览模式转换器
    public static readonly ViewModeToBoolConverter PreviewMode = new() { TargetMode = ViewMode.Preview };
}
