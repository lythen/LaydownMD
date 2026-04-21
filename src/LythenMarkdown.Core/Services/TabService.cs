using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.Core.Services;

/// <summary>
/// 选项卡服务实现
/// </summary>
public class TabService : ITabService
{
    private readonly List<TabItem> _tabs = new();
    private string? _activeTabId;

    public event EventHandler<string>? TabClosed;
    public event EventHandler<string>? TabActivated;

    public async Task<TabItem> OpenTabAsync(string? filePath = null)
    {
        TabItem tab;

        if (!string.IsNullOrEmpty(filePath))
        {
            var existingTab = FindTabByPath(filePath);
            if (existingTab != null)
            {
                SwitchTab(existingTab.Id);
                return existingTab;
            }
        }

        tab = new TabItem();

        if (!string.IsNullOrEmpty(filePath))
        {
            tab.FilePath = filePath;
            tab.Title = Path.GetFileName(filePath);
        }

        _tabs.Add(tab);
        SwitchTab(tab.Id);

        return await Task.FromResult(tab);
    }

    public async Task<CloseTabResult> CloseTabAsync(string tabId, bool force = false)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab == null)
        {
            return CloseTabResult.Success;
        }

        if (!force && tab.IsModified)
        {
            // 需要显示保存对话框，这里简化为返回 Cancelled
            // 实际实现需要在 ViewModel 层处理对话框
            return CloseTabResult.Cancelled;
        }

        _tabs.Remove(tab);
        TabClosed?.Invoke(this, tabId);

        if (_activeTabId == tabId)
        {
            // 激活另一个标签
            var nextTab = _tabs.LastOrDefault();
            if (nextTab != null)
            {
                SwitchTab(nextTab.Id);
            }
        }

        return await Task.FromResult(CloseTabResult.Success);
    }

    public async Task CloseOtherTabsAsync(string keepTabId, bool force = false)
    {
        var tabsToClose = _tabs.Where(t => t.Id != keepTabId).ToList();
        
        foreach (var tab in tabsToClose)
        {
            await CloseTabAsync(tab.Id, force);
        }
    }

    public async Task CloseAllTabsAsync(bool force = false)
    {
        var tabsToClose = _tabs.ToList();
        
        foreach (var tab in tabsToClose)
        {
            await CloseTabAsync(tab.Id, force);
        }
    }

    public void SwitchTab(string tabId)
    {
        if (_activeTabId != tabId)
        {
            foreach (var tab in _tabs)
            {
                tab.IsActive = tab.Id == tabId;
            }

            _activeTabId = tabId;
            TabActivated?.Invoke(this, tabId);
        }
    }

    public TabItem? FindTabByPath(string filePath)
    {
        return _tabs.FirstOrDefault(t => t.FilePath == filePath);
    }

    public IReadOnlyList<TabItem> GetAllTabs()
    {
        return _tabs.AsReadOnly();
    }

    public TabItem? GetActiveTab()
    {
        return _tabs.FirstOrDefault(t => t.Id == _activeTabId);
    }

    public bool HasUnsavedTabs()
    {
        return _tabs.Any(t => t.IsModified);
    }

    public IReadOnlyList<TabItem> GetUnsavedTabs()
    {
        return _tabs.Where(t => t.IsModified).ToList().AsReadOnly();
    }
}
