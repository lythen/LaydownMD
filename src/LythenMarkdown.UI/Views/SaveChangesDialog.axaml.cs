using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace LythenMarkdown.UI.Views;

public partial class SaveChangesDialog : Window
{
    public string Message { get; set; } = "是否保存对以下文件的更改？";
    public string FileName { get; set; } = "";

    public SaveChangesDialog()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = this;
        
        // 绑定按钮事件
        this.FindControl<Button>("SaveButton").Click += OnSave;
        this.FindControl<Button>("DiscardButton").Click += OnDiscard;
        this.FindControl<Button>("CancelButton").Click += OnCancel;
        
        // 设置按钮内容
        this.FindControl<Button>("SaveButton").Content = "保存(_S)";
        this.FindControl<Button>("DiscardButton").Content = "不保存(_D)";
        this.FindControl<Button>("CancelButton").Content = "取消(_C)";
    }

    public SaveChangesDialog(string fileName)
    {
        AvaloniaXamlLoader.Load(this);
        FileName = fileName;
        Message = string.IsNullOrEmpty(fileName) 
            ? "是否保存对新建文档的更改？" 
            : $"是否保存对 \"{fileName}\" 的更改？";
        DataContext = this;
        
        // 绑定按钮事件
        this.FindControl<Button>("SaveButton").Click += OnSave;
        this.FindControl<Button>("DiscardButton").Click += OnDiscard;
        this.FindControl<Button>("CancelButton").Click += OnCancel;
        
        // 设置按钮内容
        this.FindControl<Button>("SaveButton").Content = "保存(_S)";
        this.FindControl<Button>("DiscardButton").Content = "不保存(_D)";
        this.FindControl<Button>("CancelButton").Content = "取消(_C)";
    }

    public static async Task<SaveChangesResult> ShowAsync(Window parent, string fileName)
    {
        var dialog = new SaveChangesDialog(fileName)
        {
            Owner = parent
        };

        var result = await dialog.ShowDialog<SaveChangesResult>(parent);
        return result;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        Close(SaveChangesResult.Save);
    }

    private void OnDiscard(object sender, RoutedEventArgs e)
    {
        Close(SaveChangesResult.Discard);
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Close(SaveChangesResult.Cancel);
    }
}

public enum SaveChangesResult
{
    Save,
    Discard,
    Cancel
}
