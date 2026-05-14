## ADDED Requirements

### Requirement: Complete rewrite with same public interface

The system SHALL completely rewrite VirtualKeyWindow.cs while preserving:
- Constructor signature
- HasBoundButtons() method
- All event subscriptions (Clicked, Dragged, ContextMenuRequested, LoopCountEdited)
- BuildBlankMenu and OnWidgetContextMenu methods
- Capture/Clear target window and ResolveTargetWindow methods

#### Scenario: Same external behavior
- **WHEN** MainForm creates VirtualKeyWindow with same constructor args
- **THEN** the window SHALL function identically from the outside

### Requirement: No historical residual code

The rewrite SHALL NOT include:
- _resizeGrip field or any resize grip code
- _isResizing, _resizeStart, _resizeStartSize fields
- _schemeAFailed field (no longer applicable)
- Floating/overlay toolbar logic
- × 1.1 margin multiplier
- Any form-level OnMouseDown/OnMouseMove/OnMouseUp overrides

### Requirement: Unified drag coordinates

All window drag code SHALL use Control.MousePosition (screen coordinates). The close button area (rightmost 28px of toolbar) SHALL be excluded from drag triggering.

#### Scenario: Reliable drag
- **WHEN** user drags the toolbar
- **THEN** window follows mouse smoothly without jitter
- **WHEN** user clicks the close button
- **THEN** window closes (does NOT start drag)

### Requirement: RecalculateSize formula

#### Scenario: Single-line calculation
- **WHEN** _singleLineMode is true and widgets exist
- **THEN** totalW = Padding.Horizontal + panel.Padding.Left + sum(widget.Width) + panel.Padding.Right
- **THEN** totalH = (toolbarVisible ? 28 : 0) + panel.Padding.Top + max(widget.Height) + panel.Padding.Bottom

#### Scenario: No clipping
- **WHEN** buttons are added in single-line mode
- **THEN** each button SHALL be fully visible
- **THEN** the rightmost button SHALL NOT be clipped
