# Lythen-Markdown 项目记忆

## 项目概述
基于 Avalonia + AvaloniaEdit 的 Markdown 编辑器，支持多标签页、会话恢复、分栏预览等功能。

## 架构关键点

### 编辑器结构
- 编辑器 (`TextEditor`) 在 `TabControl.ContentTemplate` 的 `DataTemplate` 中动态创建
- 有两个编辑器：`MainEditor`（分栏模式使用）和 `SplitEditor`（编辑模式使用）
- `FindControl<T>()` 在构造函数调用时编辑器尚未创建，返回 null

### 视图模式
- `ViewMode.Split`：显示分栏模式，`MainEditor` 可见
- `ViewMode.Edit`：显示全屏编辑，`SplitEditor` 可见
- `ViewMode.Preview`：预览模式，无编辑器

## 已知问题和解决方案

### 加粗功能修复 (2026-05-06)

**问题**：点击加粗按钮时，标记总是插入到位置 0，而不是光标位置或选区位置。

**原因**：
1. `OnBold` 原本使用 `FindEditorInTabItem()` 获取编辑器
2. 该方法调用 `GetLastRegisteredEditor()` 返回最后注册的编辑器，而非当前活动的编辑器
3. 在视图切换后，返回的是旧的/隐藏的编辑器实例

**临时方案**（第一天尝试但失败）：
- 改用 `_mainEditor ?? _splitEditor` → 失败，因为这些字段在构造函数时为 null

**最终解决方案**：
- 新增 `FindCurrentVisibleEditor()` 方法
- 使用 `this.GetVisualDescendants().OfType<TextEditor>()` 从窗口级别遍历查找
- 根据当前 `ViewMode` 和 `IsVisible` 属性返回正确的编辑器
- `OnBold` 调用此方法获取编辑器

**代码位置**：`src/LythenMarkdown.UI/MainWindow.axaml.cs`

### 格式化功能
- `OnBold`：使用自定义逻辑处理选区
- `OnItalic`、`OnCode`、`OnLink` 等：使用 `InsertMarkdown()` 方法

## 待解决问题

1. **选区问题**（用户反馈未完全解决）：
   - 用户反馈加粗结果为 `********url********`
   - 需要确认：这是否符合预期？（Markdown 加粗语法是 `**文本**`）
   - 如果选区为 `url`，结果应该是 `**url**`（4个*），而非 8 个

2. **插入标记数量**：
   - 当前代码在选区前后各插入 `****`（4个*），总计8个
   - 标准的 Markdown 加粗只需要 `**`（2个*）
   - 需要确认：这是故意设计还是需要修改？

## 会话恢复
- 使用 `ISessionService` 管理会话恢复
- 恢复时会在 `OnTabSelectionChanged` 中调用 `LoadEditorContent` 注册编辑器
- 每个 `TabItemViewModel` 维护一个编辑器列表 `_editors`

## 文件结构
```
src/
├── LythenMarkdown.Core/
│   └── Models/
│       └── TabItem.cs          # ViewMode 枚举定义
├── LythenMarkdown.UI/
│   ├── MainWindow.axaml        # XAML 布局，包含编辑器定义
│   ├── MainWindow.axaml.cs     # 主要逻辑
│   └── ViewModels/
│       ├── MainViewModel.cs
│       └── TabItemViewModel.cs  # 管理文档和编辑器注册
```
