## ADDED Requirements

### Requirement: Context menu includes scaling presets

The blank area context menu SHALL include a "缩放" submenu with preset values:
50%, 75%, 100%, 150%, 200%.
The current scale SHALL be marked with a checkmark.
Selecting a preset SHALL set `_scaleFactor` to the corresponding value and call UpdateScale().

#### Scenario: Scale via menu
- **WHEN** user right-clicks blank area
- **WHEN** user selects "缩放 > 150%"
- **THEN** ScaleFactor is set to 1.5
- **THEN** all buttons rescale proportionally

### Requirement: Context menu includes close window

The blank area context menu SHALL include a "关闭窗口" item.
This SHALL hide the VirtualKeyWindow (same behavior as clicking the ✕ button).

#### Scenario: Close from menu
- **WHEN** user right-clicks blank area and selects "关闭窗口"
- **THEN** window is hidden

### Requirement: Context menu includes layout mode toggle

The blank area context menu SHALL include a "单排/多排" toggle item.
The current mode SHALL be indicated with a checkmark.

#### Scenario: Toggle layout
- **WHEN** user right-clicks blank area and selects "单排/多排"
- **THEN** layout mode switches
