## ADDED Requirements

### Requirement: Button name uniqueness across all windows
All virtual button names SHALL be unique across all windows managed by VkWindowManager.

#### Scenario: Rename to duplicate name prevented
- **WHEN** user renames a button to a name that already exists in another window
- **THEN** system shows a warning and rejects the rename

#### Scenario: Add button with duplicate name prevented
- **WHEN** user adds a new button whose auto-generated name already exists
- **THEN** system picks the next available number suffix
