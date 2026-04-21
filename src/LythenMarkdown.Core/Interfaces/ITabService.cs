using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LythenMarkdown.Core.Models;

namespace LythenMarkdown.Core.Interfaces;

/// <summary>
/// 关闭选项结果
/// </summary>
public enum CloseTabResult
{
    Success,
    Cancelled,
    ForceClosed
}

/// <summary>
/// 选项卡服务接口
/// </summary>
public interface ITabService
{
    /// <summary>
    /// 打开标签
    /// </summary>
    Task<TabItem> OpenTabAsync(string? filePath = null);

    /// <summary>
    /// 关闭标签
    /// </summary>
    Task<CloseTabResult> CloseTabAsync(string tabId, bool force = false);

    /// <summary>
    /// 关闭其他标签
    /// </summary>
    Task CloseOtherTabsAsync(string keepTabId, bool force = false);

    /// <summary>
    /// 关闭所有标签
    /// </summary>
    Task CloseAllTabsAsync(bool force = false);

    /// <summary>
    /// 切换到指定标签
    /// </summary>
    void SwitchTab(string tabId);

    /// <summary>
    /// 通过文件路径查找标签
    /// </summary>
    TabItem? FindTabByPath(string filePath);

    /// <summary>
    /// 获取所有打开的标签
    /// </summary>
    IReadOnlyList<TabItem> GetAllTabs();

    /// <summary>
    /// 获取当前激活的标签
    /// </summary>
    TabItem? GetActiveTab();

    /// <summary>
    /// 是否有未保存的标签
    /// </summary>
    bool HasUnsavedTabs();

    /// <summary>
    /// 获取未保存的标签列表
    /// </summary>
    IReadOnlyList<TabItem> GetUnsavedTabs();

    /// <summary>
    /// 标签关闭时事件
    /// </summary>
    event EventHandler<string>? TabClosed;

    /// <summary>
    /// 标签激活时事件
    /// </summary>
    event EventHandler<string>? TabActivated;
}
