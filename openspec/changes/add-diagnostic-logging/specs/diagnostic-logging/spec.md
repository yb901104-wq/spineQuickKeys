## ADDED Requirements

### Requirement: Diagnostic logging on VK button click

The system SHALL log a diagnostic trace on each virtual button click covering:
- button name and BindActionId
- binding resolution result (sequence found or not)
- target window resolution result
- playback scheme selection

All diagnostic log lines SHALL be prefixed with `[DIAG]`.

#### Scenario: Full trace on button click
- **WHEN** user clicks a virtual button
- **THEN** system logs VKClick with button name, BindActionId, IsPlaying
- **THEN** system logs VKBinding with resolution result
- **THEN** system logs VKTarget with resolution result
- **THEN** system logs VKPlay with scheme selection

### Requirement: Diagnostic logging on binding sync

The system SHALL log diagnostic info when `SyncVkButtonBindings` runs.

#### Scenario: Sync summary
- **WHEN** SyncVkButtonBindings completes
- **THEN** system logs VKSync with matched button count and total sequence count
