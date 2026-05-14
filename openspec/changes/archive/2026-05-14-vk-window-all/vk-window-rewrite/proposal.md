## Why

VirtualKeyWindow.cs 经过 3 轮 change、51 项修补后，代码逻辑混乱。多次 sed 打补丁导致功能残留、坐标系统混乱、边距计算反复出错。决定基于已确定的功能需求完全重写，不留历史债务。

## What Changes

- **完全重写** VirtualKeyWindow.cs，保留 public 接口签名（构造参数、HasBoundButtons）
- 删除所有历史残留字段：_resizeGrip、_isResizing、_resizeStart、_resizeStartSize、_schemeAFailed（已无用）
- 统一拖拽坐标系统：全部使用 Control.MousePosition 屏幕坐标
- RecalculateSize 从零写，公式清晰可验证
- 保留现有 VirtualLayoutSerializer、VirtualButtonWidget 等外部接口不变

## Capabilities

### New Capabilities
- `vk-window-clean-rewrite`: VirtualKeyWindow 完全重写——干净架构、统一坐标、简单边距公式

### Modified Capabilities
<!-- No existing specs modified -->

## Impact

| 文件 | 改动 |
|------|------|
| Forms/VirtualKeyWindow.cs | 完全重写（约 700 行 → 约 500 行，功能不变，代码更清晰） |
