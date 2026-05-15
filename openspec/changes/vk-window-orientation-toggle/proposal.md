## Why

虚拟按键窗口目前仅支持横向单排布局，部分用户需要纵向排列按钮以适应侧边栏或窄条区域的放置需求。同时由于「多排」功能与现有布局算法存在冲突，需要先移除多排功能，避免与横/竖切换产生混淆。

## What Changes

- **移除多排功能**：删除 `_singleLine` 开关、`ToggleLayoutMode()` 方法、右键菜单「单排/多排」选项、持久化中的 `SingleLineMode` 字段。横向单排行为完全保持不变
- **新增横排/竖排切换**：右键菜单新增「竖向模式」切换选项；`RecalculateSize()` 分方向计算窗口尺寸；`FlowLayoutPanel.FlowDirection` 在 `LeftToRight` / `TopDown` 间切换
- **拖拽排序适配方向**：横向使用 dx 判断、纵向使用 dy 判断
- **圆角绘制适配方向**：横向首尾按钮圆角 → 纵向首尾按钮圆角
- **持久化新增方向字段**：布局文件中保存 `VerticalMode` 布尔值

## Capabilities

### New Capabilities
- `orientation-toggle`: 虚拟按键窗口横排/竖排方向切换，包括布局计算、拖拽排序、圆角绘制全方向适配

### Modified Capabilities
- `layout-mode-switch`: 移除多排（单排/多排切换）能力，替换为横排/竖排方向切换

## Impact

- [VirtualKeyWindow.cs](KeyMacro/Forms/VirtualKeyWindow.cs): 新增 `_vertical` 标志；`RecalculateSize()` 分方向；`BuildBlankMenu()` 修改菜单项；`OnButtonDragEnded()` 适配方向；持久化新增字段；移除 `_singleLine` 和 `ToggleLayoutMode()`
- [VirtualButtonWidget.cs](KeyMacro/Forms/VirtualButtonWidget.cs): `IsFirstInRow`/`IsLastInRow` 在纵向模式下应作为 `IsFirstInColumn`/`IsLastInColumn` 处理；`MakeRoundedPath` 的圆角逻辑需感知方向
- [VirtualLayoutSerializer.cs](KeyMacro/Services/VirtualLayoutSerializer.cs): 布局数据模型新增 `VerticalMode` 字段
- 无 API 变更，无外部依赖变更
