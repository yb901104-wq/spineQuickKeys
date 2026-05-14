## Context

VirtualKeyWindow.cs 当前代码因多次修补存在以下问题：

1. sed 补丁导致的残留代码（已删除字段仍在某些 lambda 中引用）
2. 坐标系统混用（form 坐标、toolbar 坐标、screen 坐标交叉使用）
3. RecalculateSize 公式历经 4 次修改，当前版本可能仍少算 Form.Padding
4. 三套 MouseDown 事件共存，拖拽行为不可预测

重写方案：保留外部接口（构造参数、事件订阅），内部全部从零写。

## Goals / Non-Goals

**Goals:**
- 功能完全等同于简化方案（标题栏 Dock=Top、菜单缩放、RecalculateSize、单排/多排）
- 消除所有历史残留
- 统一坐标系统
- 代码行数减少，可读性提高

**Non-Goals:**
- 不改 VirtualLayoutSerializer（仅读取现有 LayoutData 字段）
- 不改 VirtualButtonWidget（按钮渲染不变）
- 不改右键菜单逻辑（BuildBlankMenu、OnWidgetContextMenu 保留）

## Decisions

### 架构
```
VirtualKeyWindow
├── Fields (_toolbar, _panel, _widgets, _scaleFactor, 布局状态)
├── Constructor (创建控件、加载布局、绑定事件)
├── Layout (BuildBlankMenu, SaveLayout, LoadLayoutData, RecalculateSize)
├── Interaction (OnButtonClicked, 拖拽, 锁/解锁, 缩放菜单)
└── Capture (目标窗口捕获, ResolveTargetWindow)
```

### 拖拽系统（全部用 screen 坐标）
```csharp
toolbar.MouseDown → _dragStart = Control.MousePosition
toolbar.MouseMove → delta = MousePosition - _dragStart → Left/Top += delta → _dragStart = MousePosition
toolbar.MouseUp → _isDraggingWindow = false
// 关闭按钮区域 (右侧28px) 排除拖拽触发
```

### RecalculateSize
```csharp
int formPad = Padding.Horizontal; // = 2 (1+1)
int barH = _toolbar.Visible ? 28 : 0;

// 单排
totalW = formPad + panel.Padding.Left + sum(widget.Width) + panel.Padding.Right;
totalH = barH + panel.Padding.Top + max(widget.Height) + panel.Padding.Bottom;
ClientSize = (totalW, totalH);

// 多排
totalH = barH + 行高和 + panel.Padding.Top + panel.Padding.Bottom;
ClientSize = (ClientSize.Width, totalH);
```

### Scale 菜单
```csharp
预设值: 50/75/100/150/200 → SetScaleFromMenu(pct/100f)
自定义: InputBox(10-200) → SetScaleFromMenu(pct/100f)
```

## Risks

重写后需完整验证：添加按钮、删除按钮、缩放、锁定、拖拽、加载布局。风险可控，不影响 MacroPlayer/HotkeyService 等核心播放逻辑。
