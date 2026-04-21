using CommunityToolkit.Mvvm.ComponentModel;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.UI.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
    private readonly IRenderService _renderService;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _renderedHtml = string.Empty;

    public PreviewViewModel()
    {
        _renderService = App.GetRequiredService<IRenderService>();
    }

    public void UpdateContent(string markdown)
    {
        Content = markdown;
        RenderedHtml = _renderService.Render(markdown);
    }
}
