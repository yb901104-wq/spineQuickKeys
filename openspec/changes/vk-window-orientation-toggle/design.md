## Context

虚拟按键窗口当前使用横向单排布局，`RecalculateSize()` 将按钮水平排列并计算窗口宽度为各按钮宽度之和，高度为固定按钮高度。拖拽排序使用 dx 判断方向，圆角渲染依赖于 `IsFirstInRow`/`IsLastInRow` 标记。

现有「多排」功能（_singleLine=false + WrapContents=true）使用与主布局算法完全不同的尺寸计算路径（依赖 FlowLayoutPanel 自动换行而非精确计算），导致窗口尺寸不可预测。移除后可简化架构。

## Goals / Non-Goals

**Goals:**
- 横向模式行为 100% 保持不变（布局尺寸、拖拽、圆角、增删按钮、缩放）
- 新增竖向模式：按钮从上到下排列，窗口高度=按钮高度之和，宽度=最宽按钮
- 右键菜单提供横排/竖排切换
- 布局方向持久化到 `virtual_layout.json`
- 移除废弃的多排功能及相关代码

**Non-Goals:**
- 不改动按钮增删机制（AddButton/RebuildWidgets）
- 不改动缩放机制（_scaleFactor / GetEffectiveScale / SetScale）
- 不改动目标窗口捕获机制
- 不改动皮肤系统
- 不改动按键绑定逻辑
- 不改动 MacroPlayer 或序列播放

## Decisions

### 1. 方向标志：单一 `_vertical` 布尔值

横向模式为默认（`_vertical = false`），竖向模式为 `_vertical = true`。替代现有的 `_singleLine`。

**考虑过的方案：**
- 枚举 `Orientation.Horizontal / Orientation.Vertical` — 过于工程化，布尔值足够
- 复用现有的 `_singleLine` — 语义已经改变，新建标志更清晰

### 2. 布局计算：`RecalculateSize()` 分支

```
横向（不变）:
  totalW = margin + Σ(BaseBtnWidth(style) + ExtraGap) + (N-1)×gap + margin
  totalH = barH + margin + btnH + margin

竖向:
  totalW = margin + Max(BaseBtnWidth(style) + ExtraGap across all) + margin (+ ncW)
  totalH = barH + margin + Σ(btnH + ExtraGap) + (N-1)×gap + margin (+ ncH)
  FlowDirection = TopDown
  WrapContents = false, AutoScroll = true（纵向时可能需要滚动）
```

**关键约束：** 竖向时所有按钮的 margin 由水平 gap 变为垂直 gap；`halfGap` 公式不变，但用于 Top/Bottom 而非 Left/Right。

### 3. 拖拽排序：dx → dy

```
横向：DragEnded(dx) → ∣dx∣ > threshold → 左/右移动
竖向：DragEnded(dy) → ∣dy∣ > threshold → 上/下移动
```

拖拽阈值统一用 `30 * effScale`，移动步数统一用 `∣dx∣ / (60 * effScale)` → `∣dy∣ / (60 * effScale)`。

### 4. 圆角绘制：`IsFirstInRow`/`IsLastInRow` 语义扩展

当前：
- `IsFirstInRow` = true → 左圆角
- `IsLastInRow` = true → 右圆角

纵向时：
- `IsFirstInRow` = true → 上圆角
- `IsLastInRow` = true → 下圆角

`MakeRoundedPath` 已接受 `roundLeft` 和 `roundRight` 参数，横向时传递 `(IsFirstInRow, IsLastInRow)`，纵向时传递 `(IsFirstInRow, IsLastInRow)` 但对于 TopDown 布局，`roundLeft` 应用于「首」、`roundRight` 应用于「尾」。实际上 `MakeRoundedPath` 的 roundLeft 控制左上和左下，roundRight 控制右上和右下。在竖向模式下，首按钮（最上）应有上圆角，尾按钮（最下）应有下圆角。

方案：`MakeRoundedPath` 参数保持不变，只在 `RebuildWidgets()` 设置 `IsFirstInRow`/`IsLastInRow` 时保持一致——但将 `MakeRoundedPath` 的 roundLeft/roundRight 语义扩展为 roundTop/bottom 的辅助方法。或者更简单：纵向时仍然使用 roundLeft/roundRight 参数，但将 `_isFirstInRow`/`_isLastInRow` 的圆角效果从「左侧/右侧」改为「顶侧/底侧」。

实际上最简单的方法：保持 widget 层面的 `IsFirstInRow`/`IsLastInRow` 设置不变，但修改 `MakeRoundedPath` 调用的上下文，让它感知方向。或者——让 widget 自身知道方向。

更干净的方案：在 `VirtualButtonWidget` 中新增 `VerticalMode` 属性，`MakeRoundedPath` 根据该属性决定 roundLeft 是「左圆角」还是「上圆角」：

```
Vertical=false: roundLeft = 左上+左下, roundRight = 右上+右下
Vertical=true:  roundLeft = 左上+右上, roundRight = 左下+右下
```

此方案对现有横向代码零侵入。

### 5. 持久化

`VirtualLayoutSerializer.LayoutData` 新增 `bool VerticalMode` 字段，默认 false。加载时应用该标志，保存时持久化当前状态。

### 6. 移除多排

移除内容：
- `_singleLine` 字段和 `ToggleLayoutMode()`
- `BuildBlankMenu()` 中的「单排/多排」菜单项及 `m.Opened` 事件中的相关更新
- 布局加载/保存中的 `SingleLineMode`
- `_panel.WrapContents = false` 直接写死，不再受开关控制

**兼容性：** 旧布局文件中的 `SingleLineMode` 字段读取时忽略（不报错），新代码始终单排。

## Risks / Trade-offs

- **[回归风险] 横向模式改动 → 全面测试**：所有横向路径必须保持原输出。设计上采用「分支而非改写」策略——横向走原有代码路径，竖向走新分支
- **[竖向滚动] AutoScroll 开启可能导致意外滚动条**：竖向时若按钮过多超出屏幕，自动出现滚动条是合理行为，但滚动条宽度会影响窗口宽度的精确计算。需要测试后微调
- **[纵向圆角视觉] 首/尾按钮圆角方向改变**：确保竖向时圆角在顶部和底部而非左右，视觉上正确。代码层面已验证方案可行
- **[持久化兼容] 旧布局文件无 VerticalMode 字段**：反序列化时默认 false（横向），向后兼容

## Open Questions

- 按钮间距(ExtraGap)在纵向时是否应该作用于垂直方向而非水平方向？—— 是，间距仅沿布局方向有意义
- 竖向时窗口标题栏在顶部，计算 totalW 时是否需要考虑？—— 不需要，ncW/ncH 由 WinForms 自动计算
