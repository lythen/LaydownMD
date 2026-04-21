using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace LythenMarkdown.UI.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        AvaloniaXamlLoader.Load(this);
        
        // 绑定关闭按钮
        this.FindControl<Button>("CloseButton").Click += OnClose;
        
        // 设置版本信息
        var version = typeof(AboutDialog).Assembly.GetName().Version;
        if (version != null)
        {
            var versionText = this.FindControl<TextBlock>("VersionText");
            if (versionText != null)
            {
                versionText.Text = $"版本 {version.Major}.{version.Minor}.{version.Build}";
            }
        }
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
