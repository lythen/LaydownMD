# Lythen-Markdown

一款基于 C# 和 Avalonia UI 的跨平台 Markdown 编辑器

## 目录结构

```
Lythen-Markdown/
├── src/                      # 源代码目录（项目创建后）
│   ├── LythenMarkdown.Core/   # 核心业务模块（无 UI 依赖）
│   │   ├── Interfaces/         # 服务接口
│   │   ├── Models/            # 数据模型
│   │   └── Services/          # 服务实现
│   └── LythenMarkdown.UI/      # UI 模块
│       ├── Views/              # 视图（.axaml）
│       ├── ViewModels/         # 视图模型
│       ├── Controls/           # 自定义控件
│       └── Resources/         # 资源文件（样式、语言）
│
├── docs/                     # 项目文档
│   ├── requirements/          # 需求文档
│   │   └── markdown-editor-requirements.md
│   └── design/               # 设计文档
│       ├── markdown-editor-architecture.md   # 概要设计
│       ├── detailed-design.md              # 详细设计
│       ├── technical-stack.md              # 技术栈说明
│       └── test-plan.md                    # 测试计划
│
├── plan/                    # 开发计划（AI 生成）
│   └── development-plan.md   # 开发计划文档
│
├── tasks/                   # 任务列表（AI 生成）
│   └── task-list.md         # 任务追踪列表
│
├── checklist.md              # 开发检查清单（AI 生成）
│
├── rules/                    # 开发规范
│   └── project_rules.md      # 项目开发规范
│
├── memory/                   # 项目记忆（AI 生成）
│   └── project-memory.md     # 项目记忆文件
│
├── daily/                   # 工作记录（AI 生成）
│   └── YYYY-MM-DD.md         # 每日工作记录
│
├── logo.png                  # 软件 Logo
│
└── README.md                 # 本文件
```

## 文件编码规范

> ⚠️ **重要**：所有新建的文本文件必须使用 **UTF-8 无 BOM** 编码

| 编码类型 | 要求 | 说明 |
|----------|------|------|
| **UTF-8 无 BOM** | ✅ 必须 | 推荐编码，避免兼容性问题 |
| UTF-8 BOM | ❌ 禁用 | 部分工具可能出错 |
| ANSI/GBK | ❌ 禁用 | 仅限特殊场景（如旧系统对接） |

### Visual Studio 设置

```
工具 → 选项 → 文本编辑器 → 文件扩展名
→ 将 .md/.cs/.xaml 等设置为 "UTF-8 无签名 (无 BOM)"
```

## 目录用途说明

| 目录 | 用途 | 创建者 |
|------|------|--------|
| `src/` | 源代码（项目启动后创建） | 开发者 |
| `docs/` | 项目文档、需求说明、设计文档 | 开发者/AI |
| `plan/` | 开发计划、里程碑规划 | AI |
| `tasks/` | 任务追踪列表 | AI |
| `rules/` | 开发规范、编码标准 | AI |
| `memory/` | AI 项目记忆、上下文记录 | AI |
| `daily/` | AI 每日工作记录 | AI |

## 快速链接

- [需求文档](docs/requirements/markdown-editor-requirements.md)
- [架构设计](docs/design/markdown-editor-architecture.md)
- [详细设计](docs/design/detailed-design.md)
- [技术栈](docs/design/technical-stack.md)
- [开发计划](plan/development-plan.md)
- [任务列表](tasks/task-list.md)
- [检查清单](checklist.md)
- [开发规范](rules/project_rules.md)
- [项目记忆](memory/project-memory.md)
