## Context

主窗口 DataGridView 有 9 列，其中 7 列固定宽度 + 1 列 Fill，窗口缩小时出现滚动条。Spine 文件路径每次重启需要重新选择，SequenceEditor 自动补全依赖 `LastLoadedEntries` 静态字段。

## Goals / Non-Goals

**Goals:**
- 主窗口列宽随窗口等比缩放，无横向滚动条
- Spine 文件路径持久化到磁盘，启动时自动载入
- 新增释放按钮手动清除缓存

**Non-Goals:**
- 不改动 SequenceEditor 的自动补全逻辑
- 不改动 SpineHotkeyEditor 的 UI 或保存机制
- 不改动 ConfigService 现有 Load/Save 签名

## Decisions

### 1. 列宽：Fill 模式 + FillWeight

`DataGridViewAutoSizeColumnsMode.Fill` 由 WinForms 自动分配剩余空间。固定列用 `None`，弹性列用 `Fill` 设置权重。最后一个按钮列也设为 `Fill` 接收尾部的剩余宽度。

权重分配：
```
序列名称=28  快捷键=18  目标=18  步骤数=10  间隔=12  循环=12  清除=2
总权重 = 100，每列宽 = 剩余空间 × (weight / 100)
```

### 2. Spine 路径存储：独立纯文本文件

`%APPDATA%\KeyMacro\.spine_path` — 只存一行路径文本。不碰 ConfigService，避免 JSON 格式兼容问题。读/写均为静态方法。

### 3. 加载时机：Shown 事件中静默载入

MainForm_Shown 中读取路径文件 → SpineHotkeyService 解析 → 设 LastLoadedEntries。无需打开编辑器 UI。

## Risks / Trade-offs

- **[列宽] 窗口极窄时列宽可能过小** → 设置 `MinimumWidth` 兜底
- **[Spine 路径] 文件被删除后路径仍保存** → 启动加载时检查 File.Exists，不存在则清空
