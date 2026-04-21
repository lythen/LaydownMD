using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LythenMarkdown.Core.Interfaces;

namespace LythenMarkdown.Core.Services;

/// <summary>
/// 帮助服务实现
/// </summary>
public class HelpService : IHelpService
{
    private readonly HttpClient _httpClient;

    public HelpService()
    {
        _httpClient = new HttpClient();
    }

    public void ShowIntroduction()
    {
        // TODO: 实现软件介绍窗口
    }

    public void ShowUserGuide()
    {
        // TODO: 实现使用说明窗口
    }

    public void ShowAbout()
    {
        // TODO: 实现关于对话框
    }

    public IReadOnlyList<KeyboardShortcut> GetKeyboardShortcuts()
    {
        return new List<KeyboardShortcut>
        {
            new("新建文件", "Ctrl+N", "Cmd+N", "Ctrl+N"),
            new("打开文件", "Ctrl+O", "Cmd+O", "Ctrl+O"),
            new("保存文件", "Ctrl+S", "Cmd+S", "Ctrl+S"),
            new("另存为", "Ctrl+Shift+S", "Cmd+Shift+S", "Ctrl+Shift+S"),
            new("关闭标签", "Ctrl+W", "Cmd+W", "Ctrl+W"),
            new("退出程序", "Alt+F4", "Cmd+Q", "Ctrl+Q"),
            new("撤销", "Ctrl+Z", "Cmd+Z", "Ctrl+Z"),
            new("重做", "Ctrl+Y", "Cmd+Shift+Z", "Ctrl+Y"),
            new("剪切", "Ctrl+X", "Cmd+X", "Ctrl+X"),
            new("复制", "Ctrl+C", "Cmd+C", "Ctrl+C"),
            new("粘贴", "Ctrl+V", "Cmd+V", "Ctrl+V"),
            new("编辑模式", "Ctrl+1", "Cmd+1", "Ctrl+1"),
            new("分栏模式", "Ctrl+2", "Cmd+2", "Ctrl+2"),
            new("预览模式", "Ctrl+3", "Cmd+3", "Ctrl+3"),
            new("粗体", "Ctrl+B", "Cmd+B", "Ctrl+B"),
            new("斜体", "Ctrl+I", "Cmd+I", "Ctrl+I")
        };
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        // TODO: 实现更新检查
        // 连接到更新服务器检查新版本
        return await Task.FromResult<UpdateInfo?>(null);
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo update, IProgress<int>? progress = null)
    {
        // TODO: 实现更新下载和安装
        return await Task.FromResult(false);
    }

    public bool IsUpdateAvailable()
    {
        // TODO: 实现更新检查
        return false;
    }
}
