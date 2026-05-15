## ADDED Requirements

### Requirement: App config stores Spine last file path

The system SHALL maintain an application-level configuration separate from sequence data, stored in `config.json`. 
The configuration SHALL include a `LastSpineFilePath` field that records the most recently opened Spine hotkey TXT file path.

**Format compatibility**: If `config.json` contains a bare JSON array (old format, `List<MacroSequence>`), the system SHALL treat it as valid and migrate to the new format on next save.

#### Scenario: LastSpineFilePath saved on open
- **WHEN** user opens a Spine hotkey TXT file from MainForm
- **THEN** the file path is saved to `config.json` as `LastSpineFilePath`
- **THEN** the path persists across application restarts

#### Scenario: Auto-load last spine file
- **WHEN** user clicks "Spine热键编辑" button
- **WHEN** `LastSpineFilePath` exists and file is still on disk
- **THEN** the Spine editor opens directly with that file (no file dialog)

#### Scenario: File dialog fallback
- **WHEN** user clicks "Spine热键编辑" button
- **WHEN** `LastSpineFilePath` is empty or file no longer exists
- **THEN** the OpenFileDialog is shown as before
