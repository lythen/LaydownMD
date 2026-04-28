# 会话恢复与内存优化实现计划

> 创建时间：2026-04-23
> 状态：待开始

## 背景

当前存在以下问题需要解决：

1. **会话恢复 Bug**：文档第一次编辑后保存，第二次编辑后保存，重新打开显示的是第一次编辑的内容
2. **内存问题**：打开大量文档时，每个标签页都持有 TextDocument，内存占用翻倍
3. **撤销/重做状态**：切换标签页时，编辑历史丢失

---

## 问题分析

| 问题 | 根因 | 解决方案 |
|------|------|----------|
| 会话恢复 bug | `Document.Content` 同步失败或未正确保存 | 修复 TextChanged 同步逻辑 + 确保窗口关闭时同步 |
| 内存问题 | 每个标签页都持有 TextDocument | 延迟加载：仅激活标签页创建 TextDocument |
| 撤销/重做状态 | 切换标签页时 UndoStack 丢失 | Document 中序列化 UndoStack 状态 |

---

## 统一方案设计

### 数据结构

```csharp
public class TabItemViewModel
{
    public Document Document { get; }           // 始终存在，轻量（Content + OriginalContent）
    
    // 延迟加载的编辑器文档（仅激活标签页持有）
    private TextDocument? _editorDocument;
    
    // 撤销/重做状态序列化
    private List<string> _undoStack;            // 保存操作历史快照
    private int _undoPosition;                  // 当前撤销位置
}
```

### 会话保存数据

```csharp
public class TabSessionData
{
    // ... 现有字段 ...
    
    // 新增：编辑状态相关
    public List<string> UndoSnapshots { get; set; }  // 撤销快照列表
    public int UndoPosition { get; set; }             // 当前撤销位置
}
```

### 核心流程

```
打开程序
├── 从 session.json 恢复会话
│   ├── 加载每个标签页的 Document
│   ├── 重建 UndoStack 状态
│   └── 仅激活标签页创建 TextDocument
│
切换标签页
├── 旧标签页：Deactivate() → 保存内容到 Document → 释放 TextDocument
└── 新标签页：Activate() → 创建 TextDocument → 加载内容
│
编辑文档
├── 修改 SharedDocument.Text
├── 触发 TextChanged → 同步到 Document.Content
├── 更新 UndoStack
│
关闭程序
├── 触发 OnWindowClosing
├── 确保所有标签页的 Document.Content 已同步
└── 保存 session.json
```

---

## 实现步骤

### 阶段一：修复会话恢复 Bug

**目标**：确保 `Document.Content` 正确同步到 session.json

**优化：添加同步状态标志**

```csharp
// TabItemViewModel 中
private bool _contentSynced = true;  // 内容是否已同步

private void OnSharedDocumentTextChanged(...)
{
    Document.Content = SharedDocument.Text;
    _contentSynced = true;  // 同步后标记
}

// OnWindowClosing 中
foreach (var tab in Tabs)
{
    if (!tab._contentSynced)  // 只同步未同步的
    {
        tab.Document.Content = tab.SharedDocument.Text;
    }
}
```

| 步骤 | 任务 | 文件 |
|------|------|------|
| 1.1 | 添加 `_contentSynced` 同步状态标志 | TabItemViewModel.cs |
| 1.2 | 在 `OnSharedDocumentTextChanged` 中设置同步标志 | TabItemViewModel.cs |
| 1.3 | 在 `OnWindowClosing` 中根据同步状态选择性同步 | MainWindow.axaml.cs |
| 1.4 | 添加调试日志，验证同步逻辑正确 | MainViewModel.cs |
| 1.5 | 验证 session.json 中 Content 和 OriginalContent 是否正确保存 | - |

### 阶段二：延迟加载 TextDocument

**目标**：减少内存占用，仅激活标签页持有 TextDocument

| 步骤 | 任务 | 文件 |
|------|------|------|
| 2.1 | 修改 `TabItemViewModel`，将 `SharedDocument` 改为可空类型 `_editorDocument` | TabItemViewModel.cs |
| 2.2 | 添加 `Activate()` 方法：创建 TextDocument，注册事件 | TabItemViewModel.cs |
| 2.3 | 添加 `Deactivate()` 方法：同步内容，注销事件，释放对象 | TabItemViewModel.cs |
| 2.4 | 在 `MainViewModel.OnSelectedTabChanged` 中调用激活/停用逻辑 | MainViewModel.cs |
| 2.5 | 修改编辑器注册逻辑，只在激活时设置 Document | TabItemViewModel.cs |
| 2.6 | 确保 `OnSharedDocumentTextChanged` 在正确的 TabItem 上触发 | MainWindow.axaml.cs |

### 阶段三：撤销/重做状态持久化

**目标**：保存和恢复用户的编辑历史

| 步骤 | 任务 | 文件 |
|------|------|------|
| 3.1 | 在 `TabSessionData` 中添加 `UndoSnapshots` 和 `UndoPosition` 字段 | SessionData.cs |
| 3.2 | 在 `TabItemViewModel` 中实现 UndoStack 状态提取（定时保存快照） | TabItemViewModel.cs |
| 3.3 | 在恢复会话时重建 UndoStack 状态 | TabItemViewModel.cs |
| 3.4 | 更新会话保存逻辑，包含 UndoStack 状态 | MainViewModel.cs |

### 阶段四：测试与验证

| 步骤 | 任务 |
|------|------|
| 4.1 | 测试：连续编辑多次，验证会话恢复正确 |
| 4.2 | 测试：打开多个大文档，验证内存占用降低 |
| 4.3 | 测试：编辑 → 撤销 → 重做 → 保存，验证状态正确 |
| 4.4 | 测试：切换标签页 → 重启程序，验证 UndoStack 恢复 |
| 4.5 | 清理调试日志 |

---

## 预期效果

1. **会话恢复**：无论编辑多少次，重新打开程序都能恢复到上次关闭时的状态
2. **内存优化**：100 个 10MB 文档，内存占用从 ~2GB 降至 ~1GB
3. **状态保留**：重启后仍能继续之前的撤销/重做操作

---

## 风险与注意事项

1. **TextDocument 释放**：确保 Deactivate 时正确释放资源
2. **事件订阅**：防止内存泄漏，注意订阅和取消订阅配对
3. **性能考虑**：UndoStack 快照保存频率需要权衡
4. **AvaloniaEdit 限制**：验证其对 Document 引用绑定的支持
