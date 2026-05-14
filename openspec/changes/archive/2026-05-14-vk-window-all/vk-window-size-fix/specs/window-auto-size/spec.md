## ADDED Requirements

### Requirement: ScaleFactor is single source of truth

The system SHALL use `_scaleFactor` as the sole determinant of button size in single-line mode.
Window size SHALL be derived from button content + ScaleFactor, never set independently.
The Resize event SHALL NOT call UpdateScale (only reposition resize grip).

#### Scenario: Resize does not scale buttons
- **WHEN** window resizes (e.g., programmatic RecalculateSize)
- **THEN** Resize handler only moves resize grip
- **THEN** button sizes do NOT change

### Requirement: Resize grip changes ScaleFactor

Dragging the resize grip SHALL compute ScaleFactor from the drag distance.
During drag: UpdateScale SHALL be called to preview button scaling.
On mouse release: RecalculateSize SHALL be called to snap window to fit buttons × 1.1.

#### Scenario: Drag preview + snap
- **WHEN** user drags resize grip
- **THEN** buttons scale in real-time (preview)
- **THEN** window size stays during drag
- **WHEN** user releases mouse
- **THEN** window snaps to size = button area × 1.1

### Requirement: Window size from content

In single-line mode, `RecalculateSize()` SHALL compute:
- Width = (sum of all widget widths + panel padding) × 1.1
- Height = (max widget height + panel padding + toolbar height) × 1.1
The computation SHALL NOT read current ClientSize.

#### Scenario: Add button
- **WHEN** user adds a button in single-line mode
- **THEN** window width increases to fit new button + 10% margin
- **THEN** ScaleFactor stays unchanged

#### Scenario: Remove button
- **WHEN** user deletes a button in single-line mode
- **THEN** window width decreases

### Requirement: LoadLayout restores ScaleFactor not Size

In single-line mode, LoadLayoutData SHALL restore Location and ScaleFactor from layout.
Window Size SHALL NOT be restored—RecalculateSize SHALL compute it from content + ScaleFactor.

#### Scenario: Load single-line layout
- **WHEN** layout has ScaleFactor=1.5 and 3 buttons
- **WHEN** VirtualKeyWindow loads
- **THEN** Location is restored from layout
- **THEN** ScaleFactor is restored to 1.5
- **THEN** Window Size is computed from button sizes × 1.5 × 1.1

### Requirement: ScaleFactor persisted

LayoutData SHALL include a `ScaleFactor` field (float, default 0).
On save: current ScaleFactor SHALL be written.
On load: if > 0, use it; if 0 (old layout), default to 1.0.

#### Scenario: Save and restore
- **WHEN** user sets ScaleFactor to 1.5 and saves layout
- **WHEN** window is reopened
- **THEN** ScaleFactor is 1.5

### Requirement: Toolbar height toggles, not visibility

When window is locked, toolbar SHALL set Height=0 instead of Visible=false.
When unlocked, toolbar SHALL restore Height=28.
The toolbar Dock=Top space SHALL remain allocated regardless of lock state.

#### Scenario: Lock toolbar
- **WHEN** user locks window
- **THEN** toolbar Height becomes 0
- **THEN** button positions do not change

#### Scenario: Unlock toolbar
- **WHEN** user unlocks window
- **THEN** toolbar Height becomes 28

### Requirement: Toolbar draggable from label

The toolbar label SHALL forward MouseDown events for window dragging.
The close button SHALL NOT forward drag events.

#### Scenario: Drag from label
- **WHEN** toolbar is visible
- **WHEN** user presses left mouse button on toolbar label and moves
- **THEN** window follows mouse cursor

### Requirement: Resize grip rendered on top

The resize grip panel SHALL be added as the last control and call BringToFront().
It SHALL be visible at the bottom-right corner of the client area.

#### Scenario: Resize grip visible
- **WHEN** window is unlocked
- **THEN** resize grip is visible at bottom-right
- **THEN** resize grip is not obscured by other controls
