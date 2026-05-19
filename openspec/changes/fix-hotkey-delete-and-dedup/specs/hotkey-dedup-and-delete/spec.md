## ADDED Requirements

### Requirement: Load deduplication by name

When loading a TXT file, entries with duplicate `Name` SHALL be deduplicated. Only the first occurrence SHALL be kept; subsequent duplicates SHALL be discarded. This automatically cleans up corrupted files from the previous index-based save bug.

#### Scenario: Load file with duplicate entries

- **WHEN** a TXT file contains two identical lines (e.g., `Deselect: ESCAPE` appearing twice)
- **THEN** only the first `Deselect` entry SHALL be loaded into `_entries`
- **THEN** the second duplicate SHALL be silently discarded

### Requirement: Save skips entries with empty keys

Entries whose `Keys` field is empty or whitespace SHALL NOT be written to the TXT file. Their `ChineseNote` SHALL still be preserved in the annotations file. This enables "deleting" a shortcut by clearing its key binding.

#### Scenario: Clear hotkey and save

- **WHEN** the user clears the Keys field of an entry in the grid
- **WHEN** the user clicks Save
- **THEN** the TXT file SHALL NOT contain a line for that entry
- **THEN** the entry's `ChineseNote` SHALL remain in the annotations file

### Requirement: Save commits pending cell edits

When the Save button is clicked, any cell currently being edited SHALL be committed before the save loop reads cell values. This ensures the latest user input is captured.

#### Scenario: Save during active cell edit

- **WHEN** the user is actively editing a cell in the Keys or ChineseNote column
- **WHEN** the user clicks the Save button without leaving the cell
- **THEN** the pending edit SHALL be committed
- **THEN** the committed value SHALL be read and applied to the entry
