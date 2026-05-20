## Context

`D:\AI_cc\spine-tool-1\ReName-UnpackingAtlas\ReName\` 包含 3 个功能 Tab 页，使用标准 WinForms 控件 + System.IO + System.Drawing。目标是整体迁入 KeyMacro 项目，不改动任何已有功能代码。

## Goals / Non-Goals

**Goals:**
- Form1 (含 UI + 逻辑) 完整迁入 KeyMacro
- MainForm 增加"图集工具"按钮打开它
- 搬迁后原有工具可独立删除

**Non-Goals:**
- 不改动 KeyMacro 已有功能的任何代码
- 不重构、不优化源工具代码
- 不改变 UI 布局/尺寸/位置

## Decisions

**Decision**: 搬入 KeyMacro 项目 `Forms/ReNameTool/` 子目录

```
KeyMacro/Forms/ReNameTool/
├── Form1.cs              # 原代码，改 namespace + using
├── Form1.Designer.cs     # 原代码，改 namespace + typeof 引用路径
├── Form1.resx            # 原文件，不动
└── oubao.ico             # 原图标
```

- **为什么不改 Designer.cs 里的坐标**：所有控件位置/大小都是硬编码数值，保持原样即保持布局
- **为什么用 ShowDialog 而不是 Show**：工具是一个独立完整的操作界面，模态窗口符合用户预期
- **删除原 Program.cs**：KeyMacro 的 Program.cs 已经是入口，不需要第二个 Main 方法

**Namespace 变更：**
- `namespace ReName` → `namespace KeyMacro.Forms.ReNameTool`
- `typeof(Form1)` 在 Designer.cs 的 `ComponentResourceManager` 中需同步更新

**图标统一：**
- Form1 构造时 `InitializeComponent()` 后追加 `Icon = IconService.AppIcon;`
- 不再依赖 `oubao.ico`，resx 中的图标资源条目可保留（自动引用 KeyMacro 应用图标）

## Risks / Trade-offs

- **[低]** .NET Framework 4.8 → 9.0：代码仅使用基础 API，无兼容风险
- **[无]** 原项目可保留作为备份，不冲突
