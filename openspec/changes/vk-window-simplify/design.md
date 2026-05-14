## Context

虚拟按键窗口历经多次修改后，缩放手柄、工具栏、ScaleFactor 三者关系仍然混乱。简化方案从根本上移除冲突源（缩放手柄），统一交互模式（菜单缩放），恢复标题栏标准行为。

## Goals / Non-Goals

**Goals:**
- 删除缩放手柄，消除拖拽缩放与 ScaleFactor 的冲突
- 工具栏改为 Dock=Top 标题栏，锁定隐藏/显示不再影响按钮布局
- 窗口无任何最小/最大尺寸限制，完全由内容决定
- 右键缩放菜单增加自定义百分比输入
- RecalculateSize 简洁可靠

**Non-Goals:**
- 不改变按钮播放/绑定逻辑
- 不改变皮肤系统

## Decisions

### 1. 删除缩放手柄
移除 `_resizeGrip` Panel、`_isResizing`、`_resizeStart`、`_resizeStartSize`、相关 Paint/MouseDown/Move/Up 事件、Resize 事件中的定位代码。用户缩放窗口的唯一途径是右键菜单。

### 2. 标题栏 Dock=Top
- `_toolbar.Dock = DockStyle.Top` 
- `Visible` 切换，panel padding 固定不变
- 锁定后标题栏隐藏 → Dock 空间释放 → panel 扩展填满 → 窗口缩短 → 内容（按钮）在原位不动，标题栏消失
- 拖拽通过 toolbar MouseDown/Move/Up 事件（已在 toolbar 控件内，无需 form 级 handler）

### 3. 无尺寸限制
- 删除构造函数中的 `MinimumSize` 
- RecalculateSize 直接设置 ClientSize，不加 Math.Max 限制

### 4. 自定义缩放
```
"缩放 >" 子菜单新增 "自定义..."
  → InputBox("输入缩放比例(10-200):", "自定义缩放", "100")
  → 解析为 float，范围 [0.1, 2.0]
  → SetScaleFromMenu(value)
```

### 5. RecalculateSize
```csharp
void RecalculateSize()
{
    if (widgets.Count == 0) { Size = new Size(60, 40); return; }
    int totalW = padding.Left + padding.Right + widgets.Sum(w => w.Width);
    int totalH = (toolbar.Visible ? 28 : 0) + padding.Top + padding.Bottom + widgets.Max(w => w.Height);
    ClientSize = new Size(totalW, totalH);
}
```

## Risks

无缩放手柄后用户只能通过菜单缩放，频率可能降低，但精度提高。自定义输入支持任意比例（≥10%）。
