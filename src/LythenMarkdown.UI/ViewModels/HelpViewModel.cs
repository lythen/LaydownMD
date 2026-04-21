using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using LythenMarkdown.Core.Interfaces;

namespace LythenMarkdown.UI.ViewModels;

public partial class HelpViewModel : ObservableObject
{
    private readonly IHelpService _helpService;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<KeyboardShortcut> _shortcuts = new List<KeyboardShortcut>();

    public HelpViewModel()
    {
        _helpService = App.GetRequiredService<IHelpService>();
    }

    public void LoadIntroduction()
    {
        Title = "软件介绍";
        Content = LoadEmbeddedContent("introduction.md");
        Shortcuts = _helpService.GetKeyboardShortcuts();
    }

    public void LoadUserGuide()
    {
        Title = "使用说明";
        Content = LoadEmbeddedContent("user-guide.md");
        Shortcuts = _helpService.GetKeyboardShortcuts();
    }

    private string LoadEmbeddedContent(string resourceName)
    {
        // 从嵌入资源加载内容
        var assembly = typeof(HelpViewModel).Assembly;
        using var stream = assembly.GetManifestResourceStream(
            $"LythenMarkdown.UI.Resources.Contents.{resourceName}");
        if (stream == null) return string.Empty;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
