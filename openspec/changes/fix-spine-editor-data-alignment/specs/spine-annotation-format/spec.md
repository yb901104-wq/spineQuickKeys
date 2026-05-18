## ADDED Requirements

### Requirement: Annotation entries use structured JSON array

The annotation file SHALL store entries as a JSON array of objects, each containing `name` (string) and `note` (string) fields. This replaces the previous flat dictionary format where keys were hotkey names and values were notes.

#### Scenario: Save produces valid array format

- **WHEN** the user saves changes in the Spine hotkey editor
- **THEN** the annotation JSON file SHALL contain a JSON array
- **THEN** each element SHALL have `name` and `note` fields
- **THEN** entries sorted in the same order as the hotkey list

#### Scenario: Load reads current array format

- **WHEN** the Spine hotkey editor loads a `.annotations.json` file in array format
- **THEN** each entry's `note` SHALL be matched to the hotkey entry by `name`

### Requirement: Backward compatibility with legacy dict format

The system SHALL read the legacy dictionary format (`{"Name": "Note"}`) and treat it equivalently to the new array format.

#### Scenario: Load legacy dict format

- **WHEN** loading an annotation file with the legacy `{"Name":"Note"}` dict format
- **THEN** each key-value pair SHALL be treated as `{"name": key, "note": value}`

### Requirement: Annotation save matches entries by name, not by grid row index

When saving grid edits back to annotation entries, the system MUST match grid rows to `_entries` by the `Name` field (column 0 of the grid), not by numeric row index. This prevents data corruption when a search filter is active.

#### Scenario: Save with active search filter

- **WHEN** the user applies a search filter that reduces visible rows
- **WHEN** the user edits a visible row's Keys or ChineseNote
- **WHEN** the user clicks Save
- **THEN** the edits SHALL be applied to the correct `_entries` item matched by Name
- **THEN** no data from other rows SHALL be written to the wrong entry

#### Scenario: Section header rows are skipped on save

- **WHEN** saving grid edits
- **THEN** section header rows (`Name` starting with `---`) SHALL be skipped
- **THEN** their Keys and ChineseNote values SHALL NOT be updated from the grid
