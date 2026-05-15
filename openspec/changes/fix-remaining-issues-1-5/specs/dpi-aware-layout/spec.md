## MODIFIED Requirements

### Requirement: SequenceEditor DPI-aware layout

The SequenceEditor form SHALL apply DPI scaling to all its controls, not just the top panel.

The following SHALL be scaled by `DeviceDpi / 96f`:
- DataGridView column widths (类型=90, 按键/文本=Fill, 延迟=80, 触发方式=90, 按压时长=100)
- Steps toolbar height (38)
- Bottom panel height (48)
- Suggestion dropdown ListBox: Width (300), Height (200), ItemHeight (20), Font size (9)
- Hotkey panel button widths (100 each)
- All font sizes (10pt → scaled)

#### Scenario: SequenceEditor opens on high-DPI display
- **WHEN** SequenceEditor opens on a 200% DPI display
- **THEN** all column widths, toolbars, dropdowns, and fonts are 2x their 96 DPI size
- **THEN** no controls overlap or clip

#### Scenario: SequenceEditor moves to different DPI monitor
- **WHEN** SequenceEditor is moved between monitors with different DPIs
- **THEN** `OnDpiChanged` re-applies scaling to all affected controls

#### Scenario: Suggestion dropdown is DPI-scaled
- **WHEN** user edits a step key cell on a high-DPI display
- **THEN** the autocomplete suggestion dropdown is displayed at scaled size
- **THEN** the list items and text are readable at the correct scale
