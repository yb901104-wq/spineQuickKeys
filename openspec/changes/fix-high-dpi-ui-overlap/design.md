## Context

WinForms 应用在大 DPI（3200×2000 / 200%）下 UI 重叠。根本原因：

1. 布局常量（按钮高度 48px、间距 4px、字号 9pt 等）全是硬编码像素值，没有乘系统 DPI 系数
2. VirtualKeyWindow 有独立的 `_scaleFactor`（用户缩放 50%-200%），但与系统 DPI 完全独立，二者未累乘
3. MainForm 的 DataGridView 列宽固定、工具栏间隙硬编码，不受 DPI 影响
4. SequenceEditor 的 TableLayoutPanel 行高/列宽固定
5. 所有窗口未处理 `DpiChanged` 事件

## Goals / Non-Goals

**Goals**:
- 所有窗口在 100%-200% DPI 下 UI 布局正确、无重叠
- 用户缩放（右键菜单 50%-200%）与系统 DPI 叠加生效
- 支持运行中切换显示器 DPI（DpiChanged 事件）

**Non-Goals**:
- 不修改 config.json / virtual_layout.json 的持久化格式
- 不重写现有布局系统，只做最小侵入改动

## Decisions

### 1. 合并缩放因子：`_effectiveScale = _userScaleFactor × (DeviceDpi / 96f)`

- 系统 DPI 系数 = `DeviceDpi / 96f`（96 = 100% 的标准 DPI）
- 将所有布局常量乘以 `_effectiveScale`，一处修改处处生效
- `_scaleFactor` 保留为"用户缩放"（持久化到布局文件），不存系统 DPI（运行时获取）

### 2. VirtualKeyWindow：提取 `GetEffectiveScale()` 方法

- 现有 `_scaleFactor`（持久化的用户缩放）含义不变
- 新增方法 `float GetEffectiveScale()` => `_scaleFactor * (DeviceDpi / 96f)`
- `RecalculateSize()`, `SetScale()`, `UpdateScale()`, `LoadLayoutData()` 中改用 `GetEffectiveScale()`
- 按钮拖拽阈值（`30 * _scaleFactor` → `30 * GetEffectiveScale()`）
- 覆盖 `OnDpiChanged(DpiChangedEventArgs e)` → 调用 `RecalculateSize()`

### 3. VirtualButtonWidget：字号改用有效缩放

- 移除硬编码的 `new Font("Microsoft YaHei", 9)` 中的固定字号
- 计算方式：`baseFontSize = 9` → `actualFontSize = baseFontSize * effectiveScale`
- `UpdateSize()` 中的尺寸计算从基常量乘 `effectiveScale`
- 提供 `void ApplyScale(float effectiveScale)` 方法，由 VKWindow 在重算时调用

### 4. MainForm：工具栏间隙动态化

- 移除 `dgvPanel.Padding = new Padding(0, 48, 0, 0)`
- 方案：将 toolbar 的 Dock 改为 Top，让 Dock 引擎自动分配高度，无需手动 padding
- DataGridView 列宽改为 `AutoSizeColumnsMode = Fill`（除"选择"和"清除"列外）
- 或保留固定宽度但在 `OnDpiChanged` 中乘以 DPI 系数

### 5. SequenceEditor：固定值改为 DPI 感知

- `topPanel.Height = 130`, row heights `28/42/32`, column width `130`
- 将这些值在构造函数中乘以 `DeviceDpi / 96f`

### 6. DpiChanged 事件策略

- 所有窗口覆盖 `OnDpiChanged(DpiChangedEventArgs e)`
- 调用 `base.OnDpiChanged(e)`，然后重新触发布局计算
- 对 MainForm 额外触发 `RefreshGrid()` 以更新列宽
- 对 VirtualKeyWindow 触发 `RecalculateSize()`

## Risks / Trade-offs

- [风险] `AutoSizeColumnsMode.Fill` 可能改变主窗口现有列宽比例 → 已在设计决策 4 中保留固定宽度+手动缩放方案，不改变布局感
- [风险] `OnDpiChanged` 中频繁 recalculate 可能引起闪烁 → DoubleBuffered 已启用，影响有限
- [权衡] 不在构造函数中乘 DPI 系数，而是在 `OnHandleCreated` / `OnDpiChanged` 中统一处理，避免构造时 DeviceDpi 不可靠
