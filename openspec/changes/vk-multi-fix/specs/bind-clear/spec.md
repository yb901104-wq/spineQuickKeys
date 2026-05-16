## ADDED Requirements

### Requirement: Clear VK binding from SequenceEditor
When the user clears the TriggerVkButtonName field in SequenceEditor, the system SHALL find the previously bound button and clear its BindActionId.

#### Scenario: Clear binding on save
- **WHEN** user edits a sequence, clears the 虚拟按键 field, and saves
- **THEN** the corresponding virtual button's BindActionId is set to null
- **THEN** the binding is removed from virtual_layout.json

### Requirement: Clear trigger hotkey
SequenceEditor SHALL provide a button to clear the trigger hotkey without requiring a replacement.

#### Scenario: Clear hotkey
- **WHEN** user clicks 清除 on the 触发快捷键 field
- **THEN** the hotkey field is cleared
- **THEN** the old hotkey is unregistered
