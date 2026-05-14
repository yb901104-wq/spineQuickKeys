## ADDED Requirements

### Requirement: Window has no title bar

The system SHALL set `FormBorderStyle = FormBorderStyle.None` on the VirtualKeyWindow.
The system SHALL NOT display the standard Windows title bar or window border.
The system SHALL draw custom borders using the active skin or default GDI+ colors.

#### Scenario: No title bar visible
- **WHEN** VirtualKeyWindow opens
- **THEN** there is no Windows title bar, no system menu, no minimize/maximize buttons
- **THEN** the window has a custom 1px border drawn in the active skin's colors

### Requirement: Toolbar shown when unlocked

The system SHALL display a toolbar at the top of the VirtualKeyWindow when the window is unlocked.
The toolbar SHALL contain:
- Target process name display: `[目标: Spine]` or `[无目标]`
- Button count display: `N 个按钮`
- Close button (✕)

The toolbar SHALL be hidden when `_windowLocked` is true.

#### Scenario: Toolbar visible when unlocked
- **WHEN** window is unlocked
- **THEN** toolbar is visible at the top with target name, button count, and close button

#### Scenario: Toolbar hidden when locked
- **WHEN** window is locked
- **THEN** toolbar is hidden

### Requirement: Window draggable via toolbar

The system SHALL allow dragging the VirtualKeyWindow by the toolbar area when the toolbar is visible.
When the toolbar is hidden (window locked), the system SHALL NOT allow dragging by mouse.

#### Scenario: Drag via toolbar
- **WHEN** toolbar is visible
- **WHEN** user presses left mouse button on the toolbar and moves
- **THEN** the window follows the mouse cursor

#### Scenario: No dragging when locked
- **WHEN** window is locked (toolbar hidden)
- **WHEN** user presses left mouse button and moves
- **THEN** the window does NOT move

### Requirement: Right-click menu has close option

The blank area context menu SHALL include a "关闭窗口" item that hides the VirtualKeyWindow.
The system SHALL use the existing FormClosing behavior (hide instead of close).

#### Scenario: Close via menu
- **WHEN** user right-clicks blank area and selects "关闭窗口"
- **THEN** the window is hidden (not closed), same as clicking the ✕ button

### Requirement: Window lock controls toolbar visibility and dragging

The `_windowLocked` state SHALL:
- When locked: hide toolbar, disable all window dragging
- When unlocked: show toolbar, enable dragging via toolbar
- The right-click "窗口锁定/解锁" menu item SHALL toggle this state

#### Scenario: Lock toggle
- **WHEN** user toggles "窗口锁定/解锁" in the context menu
- **THEN** toolbar visibility and dragging behavior change accordingly

### Requirement: Resize handle at bottom-right corner

The system SHALL display a resize grip at the bottom-right corner of the VirtualKeyWindow.
The resize grip SHALL be a small 16×16 area with a classic diagonal-line pattern.
Dragging the resize grip SHALL change the window size.
The window size change SHALL trigger `UpdateScale()`, scaling all buttons proportionally.

#### Scenario: Resize via handle
- **WHEN** user presses left mouse button on the resize grip and drags
- **THEN** window size changes
- **THEN** ScaleFactor recalculates and all buttons rescale
