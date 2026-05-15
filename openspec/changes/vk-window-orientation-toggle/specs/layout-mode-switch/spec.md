## REMOVED Requirements

### Requirement: Two layout modes available

**Reason**: 多排模式与基于精确计算的布局算法冲突，窗口尺寸不可预测。已由横排/竖排方向切换替代。

**Migration**: 使用方向切换（orientation-toggle）能力替代。横向单排行为保持不变，竖向模式提供垂直排列（等同于单列多排效果）。

### Requirement: Multi-row mode

**Reason**: 由于多排被整体移除，多排模式及其相关假设不再适用。

**Migration**: 无。如需垂直排列多个按钮，请使用竖向模式。

### Requirement: Multi-row mode auto-sizes window

**Reason**: 多排模式被移除，不再需要多排尺寸调整。

**Migration**: 无。

### Requirement: Single-row mode auto-sizes window

**Reason**: 单排模式现在是唯一布局模式，不再需要与多排区分的「单排模式」概念。窗口自动尺寸调整能力由 `RecalculateSize()` 统一处理。

**Migration**: 窗口始终使用 `RecalculateSize()` 精确计算尺寸。

## MODIFIED Requirements

### Requirement: Layout mode switchable via context menu

The blank area context menu SHALL include an orientation toggle instead of the old "单排/多排" toggle.
The orientation toggle SHALL switch between horizontal and vertical layout.
The orientation SHALL be persisted in `VirtualLayoutSerializer.LayoutData`.

**Reason**: 原有"单排/多排"切换已由"横排/竖排"方向切换替代。多排功能被移除，方向切换提供更清晰的横/纵选择。

#### Scenario: Toggle orientation
- **WHEN** user right-clicks blank area and selects orientation toggle
- **THEN** layout orientation switches
- **THEN** the orientation is saved to layout data
