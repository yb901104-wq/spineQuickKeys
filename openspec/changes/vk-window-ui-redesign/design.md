## Context

虚拟按键窗口目前使用 `FormBorderStyle = Sizable`，有标准 Windows 标题栏和可拖拽边框。按钮在 FlowLayoutPanel 中默认换行排列，窗口大小固定 (400×300) 由用户手动调整并保存在布局文件中。按钮样式通过 GDI+ 自绘 (VirtualButtonWidget.OnPaint)，无外部资源加载机制。

当前窗口锁定仅禁止按钮拖拽，不改变窗口外观。右键菜单已包含基本的窗口控制（置顶/透明度/锁定/保存布局）。

## Goals / Non-Goals

**Goals:**
- 去除标准窗口标题栏和边框，改为纯自绘无边框窗口
- 窗口解锁时显示工具栏（目标名+按钮数+关闭），锁定后完全隐藏
- 右下角缩放手柄拖拽改变 ScaleFactor，按钮同步缩放
- 单排（水平撑开AutoSize）和多排（固定宽度换行）两种布局可切换
- 右键菜单增加缩放预设值、布局切换、关闭窗口
- 自定义皮肤系统：skin.json 配色 + PNG 图标 + 9-slice 贴图，无图回退 GDI+

**Non-Goals:**
- 不改变现有按钮绑定/播放逻辑
- 不修改 MacroPlayer 或 HotkeyService
- 不涉及多窗口/多显示器特殊处理（但需保证基本兼容）
- 皮肤系统不包含运行时热重载（需重启应用）

## Decisions

### 决策 1：工具栏实现方式——独立 Panel 嵌入

在 FlowLayoutPanel 上方增加一个 `Panel` 作为工具栏：

```
┌──────────────────────────────────────────┐
│ 工具栏 (Panel, Dock=Top, Height=32)       │ ← 锁定隐藏
│ [目标: Spine]    3 个按钮           [✕]   │
├──────────────────────────────────────────┤
│ FlowLayoutPanel (Dock=Fill)              │
│ ┌──┐ ┌──┐ ┌──┐                         │
│ │  │ │  │ │  │                         │
│ └──┘ └──┘ └──┘                         │
├──────────────────────────────────────────┤
│ 缩放手柄 (右下角)                          │
└──────────────────────────────────────────┘
```

工具栏包含：
- 左侧：目标窗口名（`[目标: Spine]`）或 `[无目标]`
- 中间：按钮数量（`3 个按钮`）
- 右侧：「关闭按钮」(✕)

工具栏 Visible 受 `_windowLocked` 控制，锁定隐藏后窗口不可拖拽。

拖拽行为：工具栏区域的 MouseDown → 拖拽窗口（复用现有 OnMouseDown，将热区从 `e.Y < 24` 改为 `e.Y < 工具栏高度` 或在工具栏 MouseDown 中处理）

### 决策 2：缩放手柄——Panel + Paint 绘制

在右下角添加一个 16×16 的缩放手柄 Panel：
- Dock = Bottom + Right 或 Anchor = Bottom + Right
- 自绘经典 resize 斜线图案 (三个小斜线)
- MouseDown/MouseMove 处理拖拽缩放
- 缩放时改变 `_scaleFactor`（范围 0.5-2.0，步进 0.1），调用 UpdateScale()
- 缩放手柄的拖拽和 ScaleFactor 联动

缩放值存储到 `VirtualLayoutSerializer.LayoutData.ScaleFactor`。

### 决策 3：布局模式切换——_singleLineMode 字段

新增 `_singleLineMode` (bool) 字段：

| 模式 | WrapContents | AutoSize | 窗口宽度 |
|------|-------------|----------|---------|
| 单排 (true) | false | true | 自动撑宽 |
| 多排 (false) | true | false | 固定/上次宽度 |

切换时：
1. 设置 `_singleLineMode = !_singleLineMode`
2. 调整 panel 属性
3. 调用 RebuildWidgets() 或手动重置布局
4. 保存到 LayoutData

限制：单排模式下，AutoSize 使窗口宽度自适应，用户无法手动调整宽度。多排模式下固定宽度（从布局加载或上次保存值）。

### 决策 4：皮肤系统架构

```
VkSkinLoader
  ├── Load(skinName) → SkinData
  ├── GetButtonImage(style, state) → Image?
  ├── GetWindowBackground() → Image?
  └── GetColor(key) → Color?

SkinData
  ├── Name: string
  ├── Colors: Dictionary<string, string>
  ├── WindowBg: Image? (9-slice)
  ├── ButtonNormal: Image?
  ├── ButtonHover: Image?
  ├── ButtonPressed: Image?
  └── ButtonActive: Image?
```

skin.json 格式：
```json
{
  "name": "my-skin",
  "author": "",
  "version": "1.0",
  "colors": {
    "window_bg": "#0D0D0D",
    "window_border": "#000000",
    "window_rim": "#3C3C3C",
    "btn_bg_top": "#4A4A4A",
    "btn_bg_bottom": "#383838",
    "btn_text": "#E0E0E0",
    "btn_active_glow": "#00E5FF",
    "toolbar_bg": "#1A1A1A",
    "toolbar_text": "#AAAAAA"
  }
}
```

图片加载策略：
1. 从 `skins/<name>/` 目录加载对应 PNG
2. 图片存在 → 用于绘制（按钮用 DrawImage，背景用 9-slice）
3. 图片不存在 → 用 skin.json 中的颜色值走 GDI+ 绘制
4. skin.json 不存在或缺失字段 → 用硬编码默认值（当前行为）

资源规范文档 (`SKIN_GUIDE.md`) 包含：
- 目录结构
- 文件名约定
- 9-slice 规则（边距像素）
- 图片尺寸建议
- skin.json 完整字段说明
- 示例皮肤

### 决策 5：右键菜单更新

BuildBlankMenu 中新增/修改项：

```
...
────
单排/多排              ← 新增，当前模式加 ✓
────
缩放 >
  50%
  75%
  100% (✓)
  150%
  200%
────
关闭窗口               ← 新增
```

同时，OnWidgetContextMenu（按钮右键菜单）不需要改——按钮级操作（改名/绑定/循环/删除）不变。

## Risks / Trade-offs

| 风险 | 程度 | 缓解措施 |
|------|------|----------|
| FormBorderStyle=None 后窗口失去标准 Alt+F4 等行为 | 低 | 右键菜单提供关闭，FormClosing 保留 Hide 行为 |
| 皮肤 PNG 过大影响启动性能 | 低 | 图片在第一次需要时才加载，缓存到字典 |
| 9-slice 切图计算复杂 | 中 | 文档中明确标注边距像素规则，提供模板 PSD |
| 单排 AutoSize 模式下窗口超出屏幕 | 中 | 达到屏幕 90% 宽度时自动降 ScaleFactor 或启用水平滚动 |
| 现有布局文件不兼容（增加新字段） | 低 | JSON 反序列化忽略未知字段，默认值兼容 |
