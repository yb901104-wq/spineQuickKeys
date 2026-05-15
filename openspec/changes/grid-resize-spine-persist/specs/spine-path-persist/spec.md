## ADDED Requirements

### Requirement: Spine path persisted to local file

The system SHALL store the last opened Spine TXT file path in `%APPDATA%\KeyMacro\.spine_path` as a plain text file containing only the path.

On application startup, the system SHALL check if the stored path exists on disk and is valid. If so, the system SHALL silently load the entries into `SpineHotkeyEditor.LastLoadedEntries` without opening the editor UI.

#### Scenario: Path saved on open
- **WHEN** user opens a Spine TXT file via "Spine热键编辑" button
- **THEN** the file path is written to `.spine_path`

#### Scenario: Auto-load on startup
- **WHEN** application starts
- **WHEN** `.spine_path` exists and the file path is valid
- **THEN** entries are loaded into `LastLoadedEntries`
- **THEN** SequenceEditor autocomplete can use these entries without opening Spine editor

#### Scenario: Auto-clear on missing file
- **WHEN** application starts
- **WHEN** `.spine_path` exists but the file no longer exists on disk
- **THEN** the saved path is cleared (`.spine_path` deleted)
- **THEN** `LastLoadedEntries` remains null

### Requirement: Release button clears spine data

The main form SHALL have a "释放" button adjacent to the "Spine热键编辑" button. When clicked, it SHALL clear `SpineHotkeyEditor.LastLoadedEntries` and delete the `.spine_path` file. The button SHALL be disabled when no spine data is loaded.

#### Scenario: Release spine data
- **WHEN** user clicks "释放" button
- **WHEN** `LastLoadedEntries` is not null
- **THEN** `LastLoadedEntries` is set to null
- **THEN** `.spine_path` file is deleted
- **THEN** the button becomes disabled
