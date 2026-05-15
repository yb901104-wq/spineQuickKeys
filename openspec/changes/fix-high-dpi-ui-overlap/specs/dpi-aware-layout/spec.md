## ADDED Requirements

### Requirement: All windows SHALL render correctly at 100%-200% system DPI
The system SHALL compute layout dimensions (button size, spacing, margins, font size) by multiplying base constants with the effective scale factor = user scale × (DeviceDpi / 96f).

#### Scenario: VirtualKeyWindow calculates effective scale
- **WHEN** the window is created on a display with DeviceDpi = 192 (200%)
- **AND** user `_scaleFactor` is 1.0 (default)
- **THEN** effective scale = 2.0, and BASE_BTN_H = 48 × 2.0 = 96px
- **AND** no button overlaps with adjacent buttons

#### Scenario: User scale and system DPI stack
- **WHEN** the window is on a 200% DPI display and user sets `_scaleFactor` to 0.5
- **THEN** effective scale = 0.5 × 2.0 = 1.0, buttons render at 100% visual size

### Requirement: VirtualButtonWidget SHALL use effective scale for font and size
All `DrawString` font sizes and `UpdateSize()` dimensions SHALL be computed from base constants × effective scale, not hardcoded pixel values.

#### Scenario: Widget size matches effective scale
- **WHEN** effective scale = 2.0
- **THEN** SmallIcon button width = 48 × 2.0 = 96px, height = 48 × 2.0 = 96px
- **AND** font size = round(9 × 2.0) = 18pt

### Requirement: All forms SHALL handle DpiChanged events
Forms SHALL override `OnDpiChanged` to recalculate layout when the window moves to a monitor with different DPI.

#### Scenario: Window moves to different DPI monitor
- **WHEN** VirtualKeyWindow moves from a 100% to a 200% DPI monitor
- **THEN** `OnDpiChanged` fires and `RecalculateSize()` recomputes all dimensions
- **AND** the window resizes to fit the new DPI

### Requirement: MainForm DataGridView SHALL scale column widths with DPI
DataGridView fixed-width columns SHALL be multiplied by the DPI factor on form load and DPI change.

#### Scenario: Column width scales at 200% DPI
- **WHEN** MainForm loads on a 200% DPI display
- **THEN** column "序列名称" width = 200 × 2.0 = 400px
- **AND** all columns fit within the form width without horizontal clipping

### Requirement: MainForm toolbar SHALL not overlap with DataGridView
The toolbar's vertical space SHALL be determined by Dock layout, not hardcoded padding.

#### Scenario: Toolbar height auto-adjusts
- **WHEN** toolbar buttons render at 200% DPI
- **THEN** the toolbar FlowLayoutPanel auto-sizes to contain them
- **AND** the DataGridView starts below the toolbar with no overlap

### Requirement: SequenceEditor TableLayoutPanel row heights SHALL be DPI-aware
Fixed row heights (28, 42, 32) and fixed column width (130) SHALL be multiplied by the DPI factor.

#### Scenario: Editor rows scale with DPI
- **WHEN** SequenceEditor opens on a 200% DPI display
- **THEN** row heights = 28×2.0=56, 42×2.0=84, 32×2.0=64
- **AND** all controls fit within their rows
