using System;
using System.Globalization;
using Avalonia.Data.Converters;
using AvaloniaEdit;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.UI.ViewModels;

public static class ViewModeConverters
{
    /// <summary>
    /// 检查是否为分栏模式
    /// </summary>
    public static readonly FuncValueConverter<ViewMode?, bool> IsSplit =
        new(mode => mode == ViewMode.Split || mode == null);
    
    /// <summary>
    /// 检查是否为预览模式
    /// </summary>
    public static readonly FuncValueConverter<ViewMode?, bool> IsPreview =
        new(mode => mode == ViewMode.Preview);
    
    /// <summary>
    /// 检查是否为编辑模式
    /// </summary>
    public static readonly FuncValueConverter<ViewMode?, bool> IsEdit =
        new(mode => mode == ViewMode.Edit);
}
