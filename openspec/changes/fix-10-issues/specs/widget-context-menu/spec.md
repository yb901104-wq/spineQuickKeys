## ADDED Requirements

### Requirement: Button right-click menu includes "增加间隔" option

The system SHALL add an "增加间隔" menu item to the button-specific context menu (OnWidgetContextMenu). When clicked, it SHALL insert a spacer VirtualButton after the current button in the button list. Spacers SHALL be non-interactive (no click, no hover effect).

#### Scenario: Add spacer from button menu
- **WHEN** user right-clicks any VirtualButtonWidget
- **THEN** the context menu shows "增加间隔" option
- **WHEN** user clicks "增加间隔"
- **THEN** a spacer widget is inserted after the current button in the layout
- **THEN** the window recalculation includes the spacer's fixed width

#### Scenario: Spacer has no interaction
- **WHEN** user clicks on a spacer widget
- **THEN** nothing happens (no macro execution, no binding check)

## MODIFIED Requirements

### Requirement: Right-clicking blank area shows only window-level menu

When the user right-clicks the empty area of the VirtualKeyWindow's FlowLayoutPanel (not on any button), the system SHALL display the blank area context menu (BuildBlankMenu). The menu SHALL NOT include "保存布局" or "重置布局" items.

#### Scenario: Blank area right-click shows correct menu
- **WHEN** user right-clicks an empty area of the VK window
- **THEN** the blank area menu appears (add button / delete all / topmost / opacity / position lock / single-multi row / scale / window lock / close)
- **THEN** the menu does NOT contain "保存布局" item
- **THEN** the menu does NOT contain "重置布局" item
- **THEN** only one menu is shown
