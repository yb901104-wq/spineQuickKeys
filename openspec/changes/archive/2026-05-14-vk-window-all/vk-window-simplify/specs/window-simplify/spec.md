## ADDED Requirements

### Requirement: No resize grip

The system SHALL NOT have a resize grip control. All window scaling SHALL be done via the right-click context menu.

#### Scenario: No resize handle
- **WHEN** VirtualKeyWindow is open
- **THEN** there is no resize grip at the bottom-right corner
- **THEN** user cannot drag to resize the window

### Requirement: Title bar with Dock=Top

The toolbar SHALL use `Dock = DockStyle.Top`. When unlocked, it SHALL be visible and take 28px of layout space. When locked, it SHALL be hidden and the panel SHALL fill the released space.

#### Scenario: Lock toggle
- **WHEN** user locks window
- **THEN** title bar hides
- **THEN** window height decreases by 28px
- **THEN** button positions within the panel do not shift

### Requirement: No size limits

The system SHALL NOT enforce any minimum or maximum window size. The window SHALL be sized purely by RecalculateSize based on content.

### Requirement: Custom scale input

The scale submenu SHALL include a "自定义..." option. When clicked, an InputBox SHALL prompt for a percentage (10-200, default 100). Invalid input SHALL be ignored.

#### Scenario: Custom scale
- **WHEN** user selects "缩放 > 自定义..."
- **WHEN** user enters "150"
- **THEN** ScaleFactor is set to 1.5
- **THEN** buttons and window resize accordingly

### Requirement: RecalculateSize formula

RecalculateSize SHALL use:
- Width = panel padding left + sum of widget widths + panel padding right
- Height = (toolbar visible ? 28 : 0) + panel padding top + max widget height + panel padding bottom

#### Scenario: Add button
- **WHEN** user adds a button in single-line mode
- **THEN** window width increases by the button width
- **THEN** window height stays the same
