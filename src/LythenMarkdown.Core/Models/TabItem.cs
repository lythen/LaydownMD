using System;

namespace LythenMarkdown.Core.Models;

/// <summary>
/// 选项卡状态枚举
/// </summary>
public enum ViewMode
{
    /// <summary>仅编辑模式</summary>
    Edit = 0,
    /// <summary>分栏模式</summary>
    Split = 1,
    /// <summary>仅预览模式</summary>
    Preview = 2,
    /// <summary>弹窗模式</summary>
    Popup = 3
}

/// <summary>
/// 选项卡项模型
/// </summary>
public class TabItem
{
    /// <summary>
    /// 选项卡唯一标识
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 显示标题
    /// </summary>
    public string Title { get; set; } = "新建文档";

    /// <summary>
    /// 文件路径
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// 关联的文档
    /// </summary>
    public Document Document { get; set; } = new();

    /// <summary>
    /// 当前视图模式
    /// </summary>
    public ViewMode ViewMode { get; set; } = ViewMode.Split;

    /// <summary>
    /// 是否为激活状态
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 是否已修改
    /// </summary>
    public bool IsModified => Document.IsModified;

    /// <summary>
    /// 关闭时是否强制关闭（跳过保存提示）
    /// </summary>
    public bool ForceClose { get; set; }
}
