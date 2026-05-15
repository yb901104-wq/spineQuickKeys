## 1. 移除多排功能（不改变横向行为）

- [x] 1.1 删除 `VirtualKeyWindow._singleLine` 字段
- [x] 1.2 删除 `ToggleLayoutMode()` 方法及其所有调用
- [x] 1.3 从 `BuildBlankMenu()` 移除「单排/多排」菜单项及 `m.Opened` 事件中的相关更新
- [x] 1.4 从 `LoadLayoutData()` 和 `SaveLayout()` 移除 `SingleLineMode` 的读写；`SaveLayout` LayoutData 改为使用 VerticalMode
- [x] 1.5 从 `ReloadLayout()` 移除 `_singleLine` 相关代码
- [x] 1.6 硬编码 `_panel.WrapContents = false`，不受开关控制；移除构造函数和加载路径中的 `WrapContents`/`AutoScroll` 条件赋值
- [ ] 1.7 构建并验证横向模式所有功能不变

## 2. 新增横排/竖排切换基础结构

- [x] 2.1 在 `VirtualKeyWindow` 新增 `private bool _vertical = false;` 字段
- [x] 2.2 在 `BuildBlankMenu()` 新增「竖向模式」toggle 菜单项（带勾选标记），点击时切换 `_vertical` 并调用布局刷新和持久化
- [x] 2.3 实现方向切换方法：根据 `_vertical` 设置 `_panel.FlowDirection`（LeftToRight/TopDown），然后调用 `RecalculateSize()`

## 3. 竖向布局计算

- [x] 3.1 在 `RecalculateSize()` 中新增竖向分支：宽度 = margin + max(button width + ExtraGap) + margin + ncW；高度 = barH + margin + Σ(btnH + ExtraGap) + (N-1)×gap + margin + ncH
- [x] 3.2 竖向时 widget Margin 改用上/下间距：`new Padding(0, halfGap, 0, halfGap + eg)` 而非左右
- [ ] 3.3 确认竖向时窗口宽度不会随按钮增减变化（只取决于最宽按钮）

## 4. 拖拽排序适配方向

- [x] 4.1 在 `OnButtonDragEnded()` 中根据 `_vertical` 选择使用 dy 还是 dx
- [x] 4.2 竖向阈值和步数公式：`threshold = 30 × effScale`，`steps = |dy| / (60 × effScale)`

## 5. 圆角绘制适配方向

- [x] 5.1 在 `VirtualButtonWidget` 新增 `public bool VerticalMode { get; set; }` 属性
- [x] 5.2 修改 `MakeRoundedPath`：当 `VerticalMode = true` 时，`roundLeft` → 上圆角（左上+右上），`roundRight` → 下圆角（左下+右下）
- [x] 5.3 在 `VirtualKeyWindow.RebuildWidgets()` 中为每个 widget 设置 `VerticalMode = _vertical`
- [ ] 5.4 确认横向时圆角行为完全不变

## 6. 布局持久化

- [x] 6.1 在 `VirtualLayoutSerializer.LayoutData` 新增 `public bool VerticalMode { get; set; }` 字段
- [x] 6.2 `SaveLayout()` 保存 `VerticalMode = _vertical`
- [x] 6.3 `LoadLayoutData()` 和 `ReloadLayout()` 恢复 `_vertical` 并应用到面板方向和 widget 方向

## 7. 构建验证

- [x] 7.1 构建项目，解决编译错误
- [ ] 7.2 验证横向模式：增删按钮、缩放、拖拽排序、置顶、透明度、目标窗口、皮肤渲染
- [ ] 7.3 验证竖向模式：增删按钮、窗口尺寸计算、拖拽排序、圆角绘制、缩放、持久化
- [ ] 7.4 验证横/竖切换：切换后布局正确、来回切换无累积偏差
