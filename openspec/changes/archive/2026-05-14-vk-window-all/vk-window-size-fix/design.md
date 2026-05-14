## Context

VK 窗口当前存在三个问题：

**A: 尺寸循环依赖**
- Resize 事件调 UpdateScale → ScaleFactor 变 → RecalculateSize 设 ClientSize → 再次触发 Resize
- 拖拽松手后 RecalculateSize 算出不同尺寸，窗口跳跃
- 单排模式从 layout 恢复旧 Size，与 ScaleFactor 计算冲突

**B: 工具栏隐藏移位**
- 用 Visible=false 隐藏工具栏，Dock=Top 让 Fill 面板扩展，按钮上移

**C: 交互不可用**
- 工具栏子控件拦截 MouseDown，无法拖动
- 缩放手柄被 Dock=Fill 的 panel 覆盖

## Goals / Non-Goals

**Goals:**
- ScaleFactor→UpdateScale→RecalculateSize 单向依赖，无循环
- 窗口始终比按钮大 10%，增删按钮自适应
- 拖拽缩放实时预览，松手吸附
- 单排只恢复 ScaleFactor 和位置，不恢复尺寸
- LayoutData 持久化 ScaleFactor
- 工具栏隐藏不改变按钮位置（Height 0↔28）
- 工具栏可拖拽
- 缩放手柄可见可用

**Non-Goals:**
- 不改变多排模式基本行为（固定宽度、高度自适应）
- 不涉及按钮皮肤渲染

## Decisions

### A1: Resize 事件只挪缩放手柄
```csharp
Resize += (_, _) => _resizeGrip.Location = new Point(ClientSize.Width - 14, ClientSize.Height - 14);
```
不再调用 UpdateScale。窗口尺寸变化不再影响按钮缩放。

### A2: 单排拖拽流程
```
MouseDown: 记录开始状态
MouseMove: 从拖拽距离算 _scaleFactor → UpdateScale() 预览（窗口不动）
MouseUp:   RecalculateSize() → 窗口吸附到按钮×1.1
```

### A3: RecalculateSize 单排计算
```
totalW = sum(widget.Width) + panel.Padding.Left + panel.Padding.Right
totalH = max(widget.Height) + panel.Padding.Top + panel.Padding.Bottom + toolbarH
ClientSize = (totalW × 1.1, totalH × 1.1)
```
不读当前 ClientSize，完全由 widget 当前尺寸（已含 ScaleFactor）决定。

### A4: LoadLayoutData 单排不恢复 Size
```csharp
Location = new Point(data.WindowX, data.WindowY);
_scaleFactor = data.ScaleFactor > 0 ? data.ScaleFactor : 1.0f;
// 不恢复 Size → 由 RecalculateSize 计算
```
然后调 UpdateScale + RecalculateSize。

### A5: 工具栏隐藏用 Height
```csharp
_toolbar.Height = _windowLocked ? 0 : 28;
```
不切换 Visible。Layout 中 toolbar 始终占 28px Dock 空间，按钮位置不变。

### A6: 工具栏拖拽
```csharp
_lblToolbarInfo.MouseDown += OnToolbarMouseDown;
_btnClose.MouseDown += OnToolbarMouseDown;  // 关闭按钮除外
```
复用已有的拖拽逻辑。

### A7: Controls 顺序
```csharp
Controls.Add(_toolbar);     // Dock=Top
Controls.Add(_panel);       // Dock=Fill  
Controls.Add(_resizeGrip);  // 最上层
_resizeGrip.BringToFront();
```

## Risks / Trade-offs

| 风险 | 缓解 |
|------|------|
| 旧 layout 文件无 ScaleFactor 字段 | 反序列化默认为 0，代码判断 `> 0 才用`，否则用 1.0 |
| 拖拽时只预览不改变窗口，松手才吸附 | 有半秒延迟感，但避免了拖拽中尺寸跳跃 |
