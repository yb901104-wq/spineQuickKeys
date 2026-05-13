## ADDED Requirements

### Requirement: Right-clicking a virtual button shows only button-specific menu

When the user right-clicks a VirtualButtonWidget, the system SHALL display the button-specific context menu (OnWidgetContextMenu) and SHALL NOT display the blank area context menu (BuildBlankMenu).

#### Scenario: Button right-click shows correct menu
- **WHEN** user right-clicks a VirtualButtonWidget
- **THEN** the button-specific menu appears (modify name / bind shortcut / loop delay / delete)
- **THEN** the blank area menu does NOT appear
- **THEN** the user does not see two menus simultaneously

### Requirement: Right-clicking blank area shows only window-level menu

When the user right-clicks the empty area of the VirtualKeyWindow's FlowLayoutPanel (not on any button), the system SHALL display the blank area context menu (BuildBlankMenu).

#### Scenario: Blank area right-click shows correct menu
- **WHEN** user right-clicks an empty area of the VK window
- **THEN** the blank area menu appears (add button / delete all / topmost / opacity / position lock / save layout / window lock)
- **THEN** only one menu is shown

### Requirement: VirtualButtonWidget blocks context menu propagation

The system SHALL set an empty ContextMenuStrip on each VirtualButtonWidget to prevent WinForms from propagating WM_CONTEXTMENU to the parent FlowLayoutPanel's ContextMenuStrip.

#### Scenario: Widget context menu blocked
- **WHEN** a VirtualButtonWidget is created in RebuildWidgets()
- **THEN** its ContextMenuStrip property is set to a non-null empty ContextMenuStrip
- **THEN** right-clicking the widget does not trigger the parent panel's context menu
