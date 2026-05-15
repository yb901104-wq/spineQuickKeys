## 1. VirtualKeyWindow: 合并 DPI 与用户缩放

- [x] 1.1 新增 `float GetEffectiveScale()` 方法，返回 `_scaleFactor * (DeviceDpi / 96f)`
- [x] 1.2 将 `RecalculateSize()` 中所有 `_scaleFactor` 引用改为 `GetEffectiveScale()`
- [x] 1.3 将 `UpdateScale()`, `SetScale()`, `LoadLayoutData()` 中涉及布局计算的 `_scaleFactor` 改为 `GetEffectiveScale()`
- [x] 1.4 覆盖 `OnDpiChanged(DpiChangedEventArgs e)`，调 `RecalculateSize()`
- [x] 1.5 更新拖拽阈值 `30 * _scaleFactor` → `30 * GetEffectiveScale()`
- [x] 1.6 确认 `LoadLayoutData` 中反序列化的 `ScaleFactor` 仍正确作用于用户缩放

## 2. VirtualButtonWidget: 字号与尺寸使用有效缩放

- [x] 2.1 VKWindow 的 `UpdateScale()` 将 `GetEffectiveScale()` 传入 widget 的 `ScaleFactor`
- [x] 2.2 `UpdateSize()` 中的 `Scaled(val)` 自动使用有效缩放（ScaleFactor 已为合并值）
- [x] 2.3 `DrawContent` 字体通过 `Scaled(N)` 自动使用有效缩放
- [x] 2.4 VKWindow 端的 `UpdateScale()` 使用 `GetEffectiveScale()` 替代 `_scaleFactor`

## 3. MainForm: 工具栏间隙与列宽 DPI 感知

- [x] 3.1 移除 `dgvPanel.Padding = new Padding(0, 48, 0, 0)`，让 Dock 引擎自动计算
- [x] 3.2 DataGridView 列宽在 `RefreshGrid()` 中乘以 `DeviceDpi / 96f`
- [x] 3.3 覆盖 `OnDpiChanged`，重新 `RefreshGrid()` 并重算列宽

## 4. SequenceEditor: 固定行高/列宽 DPI 感知

- [x] 4.1 `topPanel.Height = 130` 和各行高（28, 42, 32）在 `OnLoad` 中乘 DPI 系数
- [x] 4.2 首列固定宽度 `130` 乘 DPI 系数
- [x] 4.3 覆盖 `OnDpiChanged` 重建布局

## 5. 验证与整理

- [ ] 5.1 在 100% DPI（1920×1080）下验证回归：主窗口、VK 窗口、序列编辑器、Spine 编辑器
- [ ] 5.2 在 200% DPI（3200×2000）下验证所有窗口无重叠、控件尺寸正确
- [ ] 5.3 验证用户缩放叠加：DPI 200% + 用户 50% = 有效 100%
- [ ] 5.4 版本号 +0.01，更新 CLAUDE.md 修改总结
