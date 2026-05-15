## MODIFIED Requirements

### Requirement: Right-clicking a virtual button shows only button-specific menu

The button-specific menu SHALL include: modify name, loop delay (for LoopIcon), button gap adjustment, force stop, and delete.
The "绑定快捷键" (bind shortcut) menu item and its submenu SHALL be removed.

**Reason**: SequenceEditor's "关联虚拟按键" field + `SyncVkButtonBindings()` now serves as the single binding mechanism. The right-click binding conflicted with `SyncVkButtonBindings` overwriting `BindActionId`.

#### Scenario: Button right-click shows correct menu
- **WHEN** user right-clicks a VirtualButtonWidget
- **THEN** the button-specific menu appears (modify name / loop delay / gap / force stop / delete)
- **THEN** the "绑定快捷键" item is NOT present
