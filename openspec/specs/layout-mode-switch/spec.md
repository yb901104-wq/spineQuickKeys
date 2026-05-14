## ADDED Requirements

### Requirement: Two layout modes available

The system SHALL support two layout modes for the VirtualKeyWindow button panel:

**Single-row mode** (排):
- `WrapContents = false`
- `AutoSize = true` (form auto-grows horizontally)
- All buttons in a single horizontal row
- Window width grows as buttons are added

**Multi-row mode** (多排):
- `WrapContents = true`
- `AutoSize = false` (fixed window width)
- Buttons wrap to next row when panel width is exceeded
- Window width is fixed (from layout or user resize)

#### Scenario: Single-row mode
- **WHEN** user toggles to single-row mode
- **THEN** buttons are arranged in one horizontal row
- **THEN** window width adjusts to fit all buttons

#### Scenario: Multi-row mode
- **WHEN** user toggles to multi-row mode
- **THEN** buttons wrap to multiple rows as needed
- **THEN** window width stays at its current value

### Requirement: Layout mode switchable via context menu

The blank area context menu SHALL include a `"单排/多排"` toggle item.
The current mode SHALL be indicated with a checkmark prefix.
The mode SHALL be persisted in `VirtualLayoutSerializer.LayoutData`.

#### Scenario: Toggle mode
- **WHEN** user right-clicks blank area and selects "单排/多排"
- **THEN** layout mode switches to the opposite mode
- **THEN** the menu item shows the new mode name
- **THEN** the mode is saved to layout data

### Requirement: Single-row mode auto-sizes window

In single-row mode, the system SHALL set `AutoSize = true` and `AutoSizeMode = GrowAndShrink`.
When buttons are added or removed, the window SHALL resize automatically to fit all buttons in one row.

#### Scenario: Auto-size on button add
- **WHEN** in single-row mode
- **WHEN** user adds a new button
- **THEN** window width increases to accommodate the new button
- **THEN** window height stays the same (single row)

#### Scenario: Auto-size on button remove
- **WHEN** in single-row mode
- **WHEN** user deletes a button
- **THEN** window width decreases
