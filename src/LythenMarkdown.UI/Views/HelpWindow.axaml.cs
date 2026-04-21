using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace LythenMarkdown.UI.Views;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        AvaloniaXamlLoader.Load(this);
        
        // 绑定关闭按钮
        this.FindControl<Button>("CloseButton").Click += OnClose;
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
