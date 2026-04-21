# Lythen-Markdown 开发规范

## 1. 项目概述

### 1.1 项目简介

Lythen-Markdown 是一款基于 C# 和 Avalonia UI 的跨平台 Markdown 文件浏览与编辑工具。

### 1.2 技术栈

| 类别 | 技术 | 版本 |
|------|------|------|
| UI 框架 | Avalonia UI | 11.2.0 |
| 语言 | C# | 12 |
| 运行时 | .NET | 8.0 |
| MVVM | CommunityToolkit.Mvvm | 8.x |
| Markdown | Markdig | 0.34+ |
| 编辑器 | AvaloniaEdit | 11.x |

### 1.3 目标平台

- Windows 10/11 (x86, x64)
- macOS 10.14+ (x64, arm64)
- Linux (x86, arm64)

---

## 2. 项目结构规范

### 2.1 目录结构

```
LythenMarkdown/
├── src/
│   ├── LythenMarkdown.Core/           # 核心业务模块
│   │   ├── Interfaces/                 # 服务接口
│   │   ├── Models/                     # 数据模型
│   │   ├── Services/                   # 服务实现
│   │   └── LythenMarkdown.Core.csproj
│   │
│   ├── LythenMarkdown.UI/              # UI 模块
│   │   ├── App.axaml                   # 应用入口
│   │   ├── App.axaml.cs
│   │   ├── Program.cs                  # 程序入口
│   │   ├── Views/                      # 视图
│   │   ├── ViewModels/                 # 视图模型
│   │   ├── Controls/                   # 自定义控件
│   │   ├── Resources/                  # 资源文件
│   │   │   ├── Styles/                 # 主题样式
│   │   │   ├── Localization/           # 语言资源
│   │   │   └── Contents/              # 帮助文档
│   │   └── LythenMarkdown.UI.csproj
│   │
│   └── LythenMarkdown.sln              # 解决方案
│
├── docs/                               # 文档
│   ├── requirements/                    # 需求文档
│   └── design/                         # 设计文档
│
├── rules/                              # 开发规范
├── tests/                              # 测试项目
└── README.md
```

### 2.2 模块职责

| 模块 | 职责 | 依赖规则 |
|------|------|----------|
| **Core** | 核心业务逻辑 | 无 UI 依赖，可独立编译测试 |
| **UI** | 界面展示 | 依赖 Core 模块 |

**重要规则**：`Core` 模块禁止依赖 `UI` 模块及其任何 Avalonia 相关组件。

---

## 3. 命名规范

### 3.1 命名约定

| 类型 | 规范 | 示例 |
|------|------|------|
| 类名 | PascalCase | `Document`, `TabItemViewModel` |
| 接口名 | `I` + PascalCase | `IFileService`, `IRenderService` |
| 方法名 | PascalCase | `OpenFileAsync`, `SaveDocument` |
| 属性名 | PascalCase | `FilePath`, `IsModified` |
| 私有字段 | `_camelCase` | `_fileService`, `_isLoading` |
| 常量 | PascalCase | `MaxFileSize`, `DefaultTimeout` |
| 枚举值 | PascalCase | `ViewMode.Edit` |
| 命名空间 | PascalCase | `LythenMarkdown.Core.Models` |

### 3.2 文件命名

| 类型 | 规范 | 示例 |
|------|------|------|
| 类文件 | `{ClassName}.cs` | `Document.cs` |
| 视图 | `{Name}View.axaml` | `EditorView.axaml` |
| 视图代码 | `{Name}View.axaml.cs` | `EditorView.axaml.cs` |
| 视图模型 | `{Name}ViewModel.cs` | `EditorViewModel.cs` |
| 资源文件 | `{Category}.resx` | `Strings.zh-CN.resx` |

### 3.3 目录命名

- 使用 **PascalCase** 或 **kebab-case**
- 保持一致性，同一层级使用相同风格

---

## 4. 代码风格规范

### 4.1 缩进与格式

- **缩进**：4 个空格（不使用 Tab）
- **行长度**：建议不超过 120 字符
- **空行**：方法之间空 1 行，逻辑段落之间空 1 行
- **大括号**：单独一行（K&R 风格）

```csharp
public class Document
{
    private string _content;
    private bool _isModified;

    public void Load()
    {
        if (string.IsNullOrEmpty(_content))
        {
            return;
        }
    }
}
```

### 4.2 using 指令

- 按字母顺序排列
- `System` 命名空间放在最前面
- `using static` 单独一组
- 项目内命名空间放在第三方之前

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using LythenMarkdown.Core.Interfaces;
using LythenMarkdown.Core.Models;
```

### 4.3 注释规范

| 注释类型 | 格式 | 使用场景 |
|----------|------|----------|
| 文件头 | `/// <summary>` | 每个公共类型顶部 |
| 方法说明 | `/// <summary>` | 所有公共方法 |
| 参数说明 | `/// <param name="xxx">` | 方法参数 |
| 返回说明 | `/// <returns>` | 有返回值的方法 |
| 代码注释 | `// xxx` | 复杂逻辑说明 |

```csharp
/// <summary>
/// 文档模型类
/// </summary>
public class Document
{
    /// <summary>
    /// 文档内容
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// 加载文档
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>加载是否成功</returns>
    public async Task<bool> LoadAsync(string filePath)
    {
        // 检查文件是否存在
        if (!File.Exists(filePath))
        {
            return false;
        }
        // ... 加载逻辑
    }
}
```

### 4.4 访问修饰符

| 修饰符 | 使用场景 |
|--------|----------|
| `public` | 对外公开的 API |
| `internal` | 同程序集内访问（推荐用于服务实现） |
| `private` | 类内部使用 |
| `protected` | 子类可访问 |
| `private protected` | 同程序集内的子类 |

---

## 5. 架构规范

### 5.1 MVVM 模式

```
View (Axaml) ←绑定→ ViewModel ←调用→ Service ←→ Model
```

| 层 | 职责 | 禁止 |
|----|------|------|
| View | UI 展示，数据绑定 | 禁止业务逻辑 |
| ViewModel | UI 状态，命令处理 | 禁止直接操作文件/数据库 |
| Service | 业务逻辑实现 | 禁止 UI 相关引用 |
| Model | 数据结构 | 仅数据结构 |

### 5.2 ViewModel 规范

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class EditorViewModel : ObservableObject
{
    // 属性使用 [ObservableProperty] 生成通知
    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private bool _isModified;

    // 命令使用 [RelayCommand]
    [RelayCommand]
    private async Task SaveAsync()
    {
        // 命令实现
    }
}
```

### 5.3 服务接口规范

```csharp
public interface IFileService
{
    /// <summary>
    /// 打开文件
    /// </summary>
    Task<Document> OpenFileAsync(string path);

    /// <summary>
    /// 保存文件
    /// </summary>
    Task SaveFileAsync(Document document, string? path = null);

    /// <summary>
    /// 获取文件信息
    /// </summary>
    FileInfo GetFileInfo(string path);
}
```

### 5.4 依赖注入规范

- 所有服务通过构造函数注入
- ViewModel 只通过接口访问服务
- 避免服务 locator 反模式

```csharp
public partial class MainViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly IRenderService _renderService;

    public MainViewModel(
        IFileService fileService,
        IRenderService renderService)
    {
        _fileService = fileService;
        _renderService = renderService;
    }
}
```

---

## 6. Git 使用规范

### 6.1 分支管理

| 分支类型 | 命名规范 | 说明 |
|----------|----------|------|
| 主分支 | `main` | 生产环境代码 |
| 开发分支 | `develop` | 开发主分支 |
| 功能分支 | `feature/{feature-name}` | 新功能开发 |
| 修复分支 | `fix/{issue-id}` | Bug 修复 |
| 发布分支 | `release/{version}` | 版本发布 |

### 6.2 分支命名

```bash
# 功能分支
feature/tab-management
feature/theme-switching
feature/multi-language

# 修复分支
fix/close-tab-memory-leak
fix/encoding-detection

# 发布分支
release/v1.0.0
```

### 6.3 提交信息规范

**格式**：
```
<type>(<scope>): <subject>

<body>

<footer>
```

**Type 类型**：

| Type | 说明 |
|------|------|
| `feat` | 新功能 |
| `fix` | Bug 修复 |
| `docs` | 文档更新 |
| `style` | 代码格式（不影响功能） |
| `refactor` | 重构（不是新功能或修复） |
| `perf` | 性能优化 |
| `test` | 测试相关 |
| `chore` | 构建/工具相关 |

**示例**：

```
feat(tab): 新增选项卡右键菜单批量关闭功能

- 新增"关闭其他"和"关闭全部"选项
- 添加未保存文件的保存确认对话框
- 支持新建文件的保存路径选择

Closes #123
```

```
fix(editor): 修复大文件渲染卡顿问题

- 添加虚拟滚动支持
- 优化 Markdig 解析性能

Closes #456
```

### 6.4 Commit 规范

```bash
# 好的提交
git commit -m "feat(editor): 支持分栏模式同步滚动"

# 不好的提交
git commit -m "更新"
git commit -m "fix something"
git commit -m "asdfgh"
```

---

## 7. 文档规范

### 7.1 必需文档

| 文档 | 位置 | 更新时机 |
|------|------|----------|
| README.md | 项目根目录 | 必填 |
| 需求文档 | `docs/requirements/` | 功能变更时 |
| 设计文档 | `docs/design/` | 架构/设计变更时 |
| API 文档 | 代码内 XML 注释 | 公共 API |

### 7.2 README.md 内容

```markdown
# Lythen-Markdown

一款基于 C# 的 Markdown 文件浏览与编辑工具

## 功能特性

- [x] 四种视图模式
- [x] 跨平台支持
- [ ] ...

## 技术栈

- Avalonia UI 11.2.0
- .NET 8.0
- Markdig

## 开发

### 环境要求

- .NET 8.0 SDK
- Visual Studio 2022 / Rider

### 构建

```bash
dotnet build
dotnet run --project src/LythenMarkdown.UI
```

### 发布

```bash
dotnet publish -c Release -r win-x86
```
```

---

## 8. 错误处理规范

> ⚠️ **核心原则：任何未处理的异常都可能导致程序闪退！所有异常必须被捕获或全局处理。**

### 8.1 防闪退原则（最高优先级）

#### 8.1.1 全局异常处理（必须实现）

```csharp
// Program.cs - 必须在程序入口处设置
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 设置全局未处理异常处理
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // 非 UI 线程未捕获异常
    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        LogException("UnhandledException", ex);

        if (e.IsTerminating)
        {
            // 程序即将终止，保存关键状态
            SaveCriticalState();
            Environment.Exit(1);
        }
    }

    // 未观察的 Task 异常
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException("UnobservedTaskException", e.Exception);
        e.SetObserved(); // 防止进程崩溃
    }
}
```

#### 8.1.2 Avalonia UI 异常处理

```csharp
// App.axaml.cs
public override void OnFrameworkInitializationCompleted()
{
    // UI 线程异常处理
    Dispatcher.UIThread.UnhandledException += (s, e) =>
    {
        LogException("UIThreadException", e.Exception);

        // 显示友好错误提示
        ShowErrorDialog("发生了一个错误，软件将继续运行。");

        e.Handled = true; // 标记为已处理，防止崩溃
    };

    base.OnFrameworkInitializationCompleted();
}
```

#### 8.1.3 异常处理黄金法则

| 规则 | 说明 |
|------|------|
| **所有 async 方法必须 await** | `.Wait()` 或 `.Result` 可能导致死锁和异常丢失 |
| **Task 异常必须观察** | 未观察的 Task 异常会导致进程崩溃 |
| **事件处理器必须 try-catch** | 特别是文件操作、IO 操作 |
| **永远不要 `throw;` 在 finally 中** | 可能掩盖原始异常 |
| **异常日志必须完整** | 包含堆栈信息，便于复现问题 |

### 8.2 异常处理原则

| 场景 | 处理方式 | 闪退风险 |
|------|----------|----------|
| 可恢复错误 | 返回 `Result<T>` 或抛出特定异常 | ⭐ 无 |
| 不可恢复错误 | 记录日志，优雅降级，显示错误提示 | ⭐⭐ 低 |
| 用户输入错误 | 验证后提示用户，不抛异常 | ⭐ 无 |
| 外部依赖失败 | 重试 + 降级 + 通知用户 | ⭐⭐ 低 |
| **未捕获异常** | **全局处理 + 日志 + 友好提示** | ⭐⭐⭐⭐⭐ 最高 |

### 8.3 典型错误处理模式

#### 8.3.1 文件操作（高风险）

```csharp
public async Task<Document> OpenFileAsync(string path)
{
    try
    {
        // 前置验证
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("文件路径不能为空", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("文件不存在", path);

        // 文件大小检查
        var fileInfo = new FileInfo(path);
        if (fileInfo.Length > MaxFileSize)
            throw new InvalidOperationException($"文件超过大小限制 ({MaxFileSize / 1024 / 1024}MB)");

        // 读取文件
        var content = await File.ReadAllTextAsync(path);
        return new Document { Content = content, FilePath = path };
    }
    catch (FileNotFoundException ex)
    {
        Logger.Warning($"文件不存在: {path}");
        throw; // 重新抛出，业务层决定如何处理
    }
    catch (UnauthorizedAccessException ex)
    {
        Logger.Error($"无权限访问文件: {path}", ex);
        throw new InvalidOperationException("无权限访问该文件", ex);
    }
    catch (IOException ex)
    {
        Logger.Error($"文件读取失败: {path}", ex);
        throw new InvalidOperationException("文件读取失败，请检查文件是否被其他程序占用", ex);
    }
}
```

#### 8.3.2 渲染操作（高风险）

```csharp
public async Task<string> RenderAsync(string markdown)
{
    try
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        return await Task.Run(() =>
        {
            try
            {
                return Markdig.Markdown.ToHtml(markdown);
            }
            catch (Exception ex)
            {
                Logger.Error("Markdown 解析失败", ex);
                return $"<p style='color:red'>渲染错误: {ex.Message}</p>";
            }
        });
    }
    catch (OutOfMemoryException ex)
    {
        Logger.Error("内存不足，渲染失败", ex);
        return "<p>文件过大，无法渲染</p>";
    }
}
```

#### 8.3.3 ViewModel 命令（必须安全）

```csharp
[RelayCommand]
private async Task SaveFileAsync()
{
    try
    {
        IsSaving = true;
        ErrorMessage = null;

        await _fileService.SaveFileAsync(CurrentDocument);
        IsModified = false;
    }
    catch (Exception ex)
    {
        Logger.Error("保存文件失败", ex);
        ErrorMessage = $"保存失败: {ex.Message}";
        // 不要抛出异常，让 UI 可以继续响应
    }
    finally
    {
        IsSaving = false;
    }
}
```

### 8.4 日志规范

```csharp
public interface ILoggerService
{
    void Trace(string message);
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? ex = null);
    void Fatal(string message, Exception? ex = null); // 致命错误
}

// 日志内容必须包含
// - 时间戳
// - 日志级别
// - 异常类型和消息
// - 完整堆栈跟踪
// - 相关上下文信息（文件路径、操作等）
```

### 8.5 错误码定义

```csharp
public static class ErrorCodes
{
    // 文件错误 (1000-1999)
    public const int FileNotFound = 1001;
    public const int FileSaveFailed = 1002;
    public const int InvalidFileFormat = 1003;
    public const int FileTooLarge = 1004;
    public const int FileAccessDenied = 1005;

    // 渲染错误 (2000-2999)
    public const int RenderFailed = 2001;
    public const int RenderTimeout = 2002;
    public const int OutOfMemory = 2003;

    // 会话错误 (3000-3999)
    public const int SessionLoadFailed = 3001;
    public const int SessionSaveFailed = 3002;

    // 系统错误 (9000-9999)
    public const int UnhandledException = 9001;
    public const int ThreadException = 9002;
    public const int OutOfMemory = 9003;
}
```

### 8.6 用户友好错误提示

| 错误类型 | 提示方式 | 示例 |
|----------|----------|------|
| 文件不存在 | 对话框 | "文件已被移动或删除" |
| 保存失败 | 对话框 + 重试 | "保存失败，可能是磁盘已满或文件被占用" |
| 渲染失败 | 内嵌提示 | 显示红色错误提示，不中断编辑 |
| 系统错误 | 对话框 + 日志 | "发生未知错误，已记录日志" |

**提示原则**：
- 使用用户能理解的语言，避免技术术语
- 提供解决建议
- 绝不显示堆栈跟踪给普通用户
- 记录完整错误信息到日志文件

### 8.7 崩溃恢复机制

```csharp
// 启动时检查并尝试恢复
public async Task<bool> TryRecoverSessionAsync()
{
    try
    {
        var session = await _sessionService.LoadSessionAsync();
        if (session != null && session.HasCrashedSession)
        {
            Logger.Info("检测到上次异常退出，尝试恢复会话");
            await RestoreSessionAsync(session);
            return true;
        }
    }
    catch (Exception ex)
    {
        Logger.Error("会话恢复失败", ex);
        // 恢复失败不影响程序启动
    }
    return false;
}
```

---

## 9. 测试规范

### 9.1 测试覆盖

| 类型 | 目标覆盖率 | 说明 |
|------|------------|------|
| 单元测试 | Core 模块 80%+ | 业务逻辑测试 |
| 集成测试 | 关键流程 | 服务间协作 |
| UI 测试 | 重要交互 | P0 功能 |

### 9.2 测试命名

```csharp
[Fact]
public void FileService_OpenFile_WithValidPath_ReturnsDocument()

[Fact]
public void FileService_OpenFile_WithInvalidPath_ThrowsFileNotFoundException()

[Fact]
public async Task TabService_CloseTab_WithUnsavedChanges_PromptsUser()
```

---

## 10. 性能规范

### 10.1 性能目标

| 指标 | 目标值 |
|------|--------|
| 启动时间 | < 3 秒 |
| 文件打开 (< 1MB) | < 1 秒 |
| 实时预览延迟 | < 100ms |
| 内存占用 | < 200MB |

### 10.2 渲染优化

- 编辑时使用防抖（150ms）
- 弹窗模式使用懒加载
- 大文件 (> 100KB) 使用虚拟滚动
- 异步渲染，不阻塞 UI

---

## 11. 安全规范

### 11.1 文件安全

- 不执行文件内容，仅作为纯文本处理
- 限制打开文件大小 (< 10MB)
- 路径规范化，防止路径遍历攻击
- 敏感操作前验证文件权限

### 11.2 数据安全

- 不在日志中输出文件内容
- 临时文件使用安全随机名
- 退出时清除敏感缓存
- 配置文件加密存储敏感信息

---

## 12. 版本规范

### 12.1 语义化版本

```
主版本.次版本.修订号[-预发布标签]
1.0.0
1.1.0-beta.1
2.0.0-rc.1
```

| 序号 | 规则 |
|------|------|
| 主版本 | 不兼容的 API 变更 |
| 次版本 | 向后兼容的功能新增 |
| 修订号 | 向后兼容的问题修复 |

### 12.2 发布流程

1. `develop` 分支完成功能开发
2. 创建 `release/v{x.y.z}` 分支
3. 修复 release 分支问题
4. 合并到 `main` 并打标签
5. 合并回 `develop`

---

> 📝 **提示**：本规范将随项目发展持续更新。如有疑问，请提 Issue 讨论。
