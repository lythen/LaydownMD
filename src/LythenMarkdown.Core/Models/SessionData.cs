using System;
using System.Collections.Generic;

namespace LythenMarkdown.Core.Models;

/// <summary>
/// 会话数据模型
/// </summary>
public class SessionData
{
    /// <summary>
    /// 打开的标签页列表
    /// </summary>
    public List<TabSessionData> OpenTabs { get; set; } = new();

    /// <summary>
    /// 当前激活的标签 ID
    /// </summary>
    public string? ActiveTabId { get; set; }

    /// <summary>
    /// 窗口状态
    /// </summary>
    public WindowState WindowState { get; set; } = new();

    /// <summary>
    /// 最后使用的视图模式
    /// </summary>
    public ViewMode LastViewMode { get; set; } = ViewMode.Split;

    /// <summary>
    /// 会话保存时间
    /// </summary>
    public DateTime SavedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 标签页会话数据
/// </summary>
public class TabSessionData
{
    /// <summary>
    /// 选项卡 ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 文件路径
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// 文档内容（未保存的内容也保存）
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件内容（为空表示新建文档）
    /// </summary>
    public string? OriginalContent { get; set; }

    /// <summary>
    /// 光标位置
    /// </summary>
    public int CursorPosition { get; set; }

    /// <summary>
    /// 滚动偏移
    /// </summary>
    public int ScrollOffsetY { get; set; }

    /// <summary>
    /// 视图模式
    /// </summary>
    public ViewMode ViewMode { get; set; }
}

/// <summary>
/// 窗口状态
/// </summary>
public class WindowState
{
    /// <summary>
    /// 窗口宽度
    /// </summary>
    public int Width { get; set; } = 1200;

    /// <summary>
    /// 窗口高度
    /// </summary>
    public int Height { get; set; } = 800;

    /// <summary>
    /// 窗口 X 位置
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// 窗口 Y 位置
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// 是否最大化
    /// </summary>
    public bool IsMaximized { get; set; }

    /// <summary>
    /// 分栏比例
    /// </summary>
    public double SplitRatio { get; set; } = 0.5;
}
