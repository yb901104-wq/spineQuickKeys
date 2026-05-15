## ADDED Requirements

### Requirement: Main form grid columns resize proportionally

When the user resizes the main window, the DataGridView columns SHALL redistribute available width proportionally.
Fixed-width columns (checkbox, button columns) SHALL keep their DPI-scaled width.
Resizable columns SHALL share remaining space by assigned ratios.

Column width allocation:
- `启用` (checkbox): fixed DPI-scaled width
- `序列名称`: 30% of remaining space
- `触发快捷键`: 20%
- `目标软件`: 20%
- `步骤数`: 10%
- `间隔(ms)`: 10%
- `循环(次)`: 10%
- `选择` (button): fixed DPI-scaled width
- `清除` (button): remaining width

#### Scenario: Resize window wider
- **WHEN** user drags the main window wider
- **THEN** resizable columns expand proportionally
- **THEN** fixed-width columns stay unchanged

#### Scenario: Resize window narrower
- **WHEN** user drags the main window narrower
- **THEN** resizable columns shrink proportionally
- **THEN** no column compresses below its minimum content width

#### Scenario: DPI change rescales fixed columns
- **WHEN** window moves to a different DPI monitor
- **THEN** fixed-width columns update their DPI-scaled width
- **THEN** resizable columns recalculate from the new remaining space
