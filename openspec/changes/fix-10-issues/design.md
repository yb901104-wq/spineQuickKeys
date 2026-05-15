## Context

虚拟按键窗口在完全重写后（v1.98）采用 math-based 尺寸计算、原生标题栏、无自定义工具栏。但遗留 10 个问题，涵盖渲染（透明/粉线/文字）、布局（边框/残留/排序/间隔）、UI（列宽/菜单）和持久化（无导入导出）。本次涉及 8 个现有文件 + 2 个新文件。

## Goals / Non-Goals

**Goals:**
- 修复全部 10 个渲染、布局、交互问题
- 实现统一的导入导出机制，覆盖 Spine 热键、序列、VK 布局和设置
- 按钮拖拽排序替代旧的无用位置锁定
- 新增"间隔"控件类型

**Non-Goals:**
- 不改变皮肤系统的架构（仍用现有 VkSkinLoader）
- 不改变 VK 窗口的标题栏方案（原生标题栏）
- 不引入第三方依赖

## Decisions

### P1: 透明区域修复 — 放弃 TransparencyKey 方案

**问题**：`TransparencyKey = ChromaKey (#FF00FF)` 结合 PNG 的 GDI+ 渲染，在抗锯齿边缘产生半透明混合，形成粉色圈。

**方案**：不再依赖 OS 级透明。当有皮肤背景图时：
- 去掉 `TransparencyKey = ChromaKey`
- 按钮 Widget 用 `g.Clear(Color.Transparent)` 清底，GDI+ 的 DrawImage 支持 PNG alpha 通道
- 窗口背景 9-slice 图直接绘制到 panel，panel.BackgroundImage 配合 Panel_PaintBg
- 提取 `btn_bg_top` 颜色作为窗口底色，避免 PNG 空位露出后方窗口颜色

**替代方案**：ImageAttributes.SetColorKey — 但需要遍历每个像素，且对半透明边缘仍然无效。选择更简洁的方案。

### P5: 按钮文字布局重组

移除首字大图标，按钮内容改为纯文字居中。各样式调整：
- SmallIcon: 名称 1 行居中，字号 Scaled(9)，白色
- LargeIcon: 名称居中，字号 Scaled(10)
- LoopIcon: 左侧名称 + 右侧输入框，字号 Scaled(9)

### P7: 拖拽排序 — 基于列表交换

FlowLayoutPanel 按 `_buttons` 列表顺序排列 widget。拖拽排序时检测鼠标 hover 到哪个目标 widget，松开时在 `VirtualButtonManager` 中执行 `MoveButton(id, newIndex)` 交换位置，然后 `RebuildWidgets()` 刷新。不需要复杂的拖拽预览。

### P9: 间隔控件

`VirtualButton` 新增 `bool IsSpacer` 字段。Spacer 的 widget 不响应点击、不绘制内容（仅透明背景或微小分隔线）。`RecalculateSize()` 中遇到 spacer 用固定宽度 `BASE_SPACER_W = 20`（@100% scale）替代 style-based 宽度。右键菜单中通过"增加间隔"在当前按钮后插入。

### P10: 导入导出数据格式

单一 JSON 文件，结构：

```json
{
  "version": "1.0",
  "createdAt": "2026-05-15T...",
  "spineHotkeys": [ ... ],
  "sequences": [ ... ],
  "vkLayout": { ... },
  "vkSettings": { ... }
}
```

导入时逐项弹确认对话框。Spine 热键编辑在导入时打开编辑器窗口（如果用原生文件路径方式），或者直接替换内存中的数据（如果编辑器支持从数据结构加载）。

**决策**：SpineHotkeyEditor 增加一个接受 `List<SpineHotkeyEntry>` 的构造重载，导入时直接填充数据而非写到文件。如果用户确认导入，再进行文件写入。

## Risks / Trade-offs

- [P1 TransparencyKey 移除] → 某些第三方皮肤若依赖 #FF00FF 做透明指示，可能出现兼容性问题。→ 只在有 PNG 时去掉 TransparencyKey，无 PNG 时保持原样
- [P7 拖拽排序] → 拖拽过程中会反复调用 RebuildWidgets 导致闪烁。→ 设置 panel.SuspendLayout/ResumeLayout，并在鼠标释放后才触发排序
- [P10 导入覆盖] → 用户可能意外导入错误的配置覆盖当前数据。→ 导入前弹整体提醒，且每步都有"是/否"确认，序列导入前备份原数据
