## ADDED Requirements

### Requirement: SpineHotkeyEditor exposes loaded entries via static property

The system SHALL expose the currently loaded Spine hotkey entries from any open SpineHotkeyEditor via a static property `LastLoadedEntries`. The property SHALL be set whenever entries are loaded or refreshed.

#### Scenario: Entries available after loading
- **WHEN** SpineHotkeyEditor loads entries from a file or from imported data
- **THEN** `SpineHotkeyEditor.LastLoadedEntries` contains the loaded entries

#### Scenario: Entries cleared on close
- **WHEN** all SpineHotkeyEditor instances are closed
- **THEN** `SpineHotkeyEditor.LastLoadedEntries` is null

### Requirement: SequenceEditor shows autocomplete dropdown when editing step key cell

When the user edits the "按键/文本" column cell in the steps DataGridView, the system SHALL display a searchable dropdown menu populated from the loaded Spine hotkey entries.

#### Scenario: Dropdown appears on edit
- **WHEN** user begins editing a cell in column index 1 of the steps DataGridView
- **WHEN** `SpineHotkeyEditor.LastLoadedEntries` is not null
- **THEN** a dropdown ListBox appears below the editing cell
- **THEN** the dropdown shows up to 50 matching entries

#### Scenario: No dropdown when no entries loaded
- **WHEN** user begins editing a cell in column index 1
- **WHEN** `SpineHotkeyEditor.LastLoadedEntries` is null
- **THEN** no dropdown is shown
- **THEN** normal text editing continues

### Requirement: Search logic auto-detects input type

The system SHALL detect the user's input content type and search the matching field:
- Input contains Chinese characters → search `ChineseNote` field
- Input contains `+` character → search `Keys` field
- Otherwise → search `Name` field

#### Scenario: Search by Chinese translation
- **WHEN** user types "撤销" in the cell
- **THEN** dropdown filters entries where `ChineseNote` starts with or contains "撤销"

#### Scenario: Search by hotkey combination
- **WHEN** user types "Ctrl+" in the cell
- **THEN** dropdown filters entries where `Keys` starts with or contains "Ctrl+"

#### Scenario: Search by English name
- **WHEN** user types "Undo" in the cell
- **THEN** dropdown filters entries where `Name` starts with or contains "Undo"

### Requirement: Selected entry fills the key binding

When the user selects an entry from the autocomplete dropdown, the system SHALL:
1. Fill the current cell with the entry's `Keys` value
2. If `Keys` contains `+`, set the step type column to "组合键"
3. If `Keys` does not contain `+` and the current type is "文本", set it to "单键"

#### Scenario: Select combo key binding
- **WHEN** user selects an entry with `Keys = "Ctrl+Z"`
- **THEN** cell value is set to "Ctrl+Z"
- **THEN** step type column is set to "组合键"

#### Scenario: Select single key binding
- **WHEN** user selects an entry with `Keys = "F1"`
- **WHEN** current step type is "文本"
- **THEN** cell value is set to "F1"
- **THEN** step type column is set to "单键"

### Requirement: Dropdown supports keyboard navigation

The autocomplete dropdown SHALL support the following keyboard shortcuts:
- `↑` / `↓` — navigate through results
- `Enter` — confirm selection
- `Esc` — close dropdown without selection

#### Scenario: Keyboard navigation
- **WHEN** dropdown is visible
- **WHEN** user presses `↓`
- **THEN** the next item in the list is highlighted
- **WHEN** user presses `Enter`
- **THEN** the highlighted item is selected and filled
- **WHEN** user presses `Esc`
- **THEN** dropdown closes, cell remains in edit mode
