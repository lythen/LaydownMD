# 缺失功能分析与完成计划

> 创建日期：2026-04-28
> 分析人：AI Assistant

---

## 1. 选项卡拖拽排序 (T1.3.5)

### 当前状态：❌ 未完成

### 分析

**已有基础**：
- `MainViewModel.MoveTab(int fromIndex, int toIndex)` 方法已实现
- 注册了 DragDrop 事件处理（MainWindow.axaml.cs）

**缺失部分**：
1. TabControl 没有设置 `AllowDrop="True"`
2. 没有实现标签页之间的拖拽排序逻辑
3. 当前 `OnDragEnter`/`OnDragOver`/`OnDrop` 只处理外部文件拖入

### 实现方案

```
1. 修改 TabControl：
   - 添加 AllowDrop="True"
   - 为 ItemTemplate 添加 PointerPressed/PointerMoved/PointerReleased 事件

2. 实现拖拽逻辑：
   - 记录拖拽源标签索引
   - 在拖拽过程中计算目标索引
   - 释放时调用 MoveTab() 重新排序
```

### 预估工时：3h

---

## 2. 格式化快捷键和基础编辑命令

### 当前状态：✅ 已完成（但有冗余代码）

### 分析

**已实现功能**（MainWindow.axaml.cs）：
- `OnUndo()` → `editor.Undo()`
- `OnRedo()` → `editor.Redo()`
- `OnCut()` → `editor.Cut()`
- `OnCopy()` → `editor.Copy()`
- `OnPaste()` → `editor.Paste()`
- `OnSelectAll()` → `editor.SelectAll()`

**菜单绑定**（BindMenuItems 方法）：
```csharp
this.FindControl<MenuItem>("UndoMenuItem")?.AddHandler(...)
this.FindControl<MenuItem>("RedoMenuItem")?.AddHandler(...)
// ...
```

**冗余代码**：
MainViewModel 中的同名方法是空实现（TODO 注释），属于冗余代码：
- `Undo()` - 空实现
- `Redo()` - 空实现
- `Copy()` - 空实现
- `Cut()` - 空实现
- `Paste()` - 空实现

### 建议
删除 MainViewModel 中的冗余方法，或将它们重定向到编辑器操作。

### 预估工时：0.5h（清理）

---

## 3. 格式操作对选定文本无效

### 当前状态：⚠️ 代码已实现，需验证

### 分析

**当前实现**（MainWindow.axaml.cs - InsertMarkdown 方法）：
```csharp
private void InsertMarkdown(string prefix, string suffix)
{
    var editor = _mainEditor ?? _splitEditor;
    if (editor == null) return;
    
    var start = editor.SelectionStart;
    var length = editor.SelectionLength;
    var text = editor.Text;
    
    if (length > 0)
    {
        // 有选中文本：用符号包裹
        var selectedText = text.Substring(start, length);
        editor.Text = text.Substring(0, start) + prefix + selectedText + suffix + text.Substring(start + length);
        editor.SelectionStart = start + prefix.Length;
        editor.SelectionLength = length;
    }
    else
    {
        // 无选中文本：插入符号并将光标放在中间
        var caretOffset = editor.CaretOffset;
        editor.Text = text.Substring(0, caretOffset) + prefix + suffix + text.Substring(caretOffset);
        editor.CaretOffset = caretOffset + prefix.Length;
    }
}
```

**逻辑分析**：
- ✅ 检测是否有选中文本
- ✅ 有选中时用 prefix/suffix 包裹
- ✅ 无选中时插入符号并定位光标

**可能的问题**：
设置 `editor.Text` 后，`SelectionStart` 和 `SelectionLength` 可能不会按预期工作（AvaloniaEdit 行为）。

### 验证步骤
1. 选中文字 "hello"
2. 点击加粗按钮
3. 预期结果：`**hello**`，"hello" 仍为选中状态

### 预估工时：1h（调试 + 修复）

---

## 完成计划

| 优先级 | 任务 | 工时 | 状态 |
|--------|------|------|------|
| P0 | 选项卡拖拽排序 | 3h | ✅ 已完成 |
| P1 | 清理冗余代码 | 0.5h | ✅ 已完成 |
| P1 | 验证并修复格式操作 | 1h | ✅ 已完成 |

---

## 执行顺序建议

1. **先验证格式操作** - 确保现有代码正常工作
2. **清理冗余代码** - 移除无用的 TODO 方法
3. **实现拖拽排序** - 最后完成最复杂的任务
