## ADDED Requirements

### Requirement: MainForm has Import and Export buttons

The system SHALL add "导入" and "导出" buttons to the MainForm toolbar, positioned after the existing "开启虚拟按键"/"关闭虚拟按键" buttons.

#### Scenario: Export button visible on toolbar
- **WHEN** MainForm is shown
- **THEN** an "导出" button appears on the toolbar to the right of existing buttons
- **THEN** an "导入" button appears next to the "导出" button

### Requirement: Export creates a unified JSON file

When the user clicks the Export button, the system SHALL open a SaveFileDialog and save all current data (Spine hotkeys, macro sequences, VK layout, VK settings) into a single JSON file. The file SHALL contain a version field and timestamp.

#### Scenario: Successful export
- **WHEN** user clicks "导出" button
- **THEN** a SaveFileDialog opens with filter "导出文件 (*.kmp)|*.kmp"
- **WHEN** user selects a save path and confirms
- **THEN** system collects: loaded Spine hotkey entries (if any editor is open), all macro sequences, current VK button layout, and VK window settings
- **THEN** a JSON file is written containing all four sections with version "1.0"
- **THEN** a success message is shown

### Requirement: Import shows per-section confirmation

When the user clicks the Import button, the system SHALL:
1. Open an OpenFileDialog
2. Load and validate the JSON file
3. Show an initial warning: "导入操作将分项确认，包含以下 4 部分：1. Spine热键编辑  2. 序列设置  3. 虚拟按键布局  4. 虚拟按键设置"
4. For each section, ask "是否导入 [部分名]？" with Yes/No
5. Only import sections the user confirms

#### Scenario: Full import flow
- **WHEN** user clicks "导入" button
- **THEN** an OpenFileDialog opens with filter "导出文件 (*.kmp)|*.kmp"
- **WHEN** user selects a valid file
- **THEN** system parses the JSON and validates format
- **THEN** a warning dialog appears listing all 4 parts
- **THEN** for each section, a Yes/No dialog is shown
- **WHEN** user selects "Yes" for Spine hotkeys
- **THEN** the SpineHotkeyEditor opens with imported data
- **WHEN** user selects "Yes" for sequences
- **THEN** current sequences are backed up and replaced with imported ones
- **WHEN** user selects "Yes" for VK layout
- **THEN** VK button list is replaced with imported buttons
- **WHEN** user selects "Yes" for VK settings
- **THEN** VK window scale, layout mode, opacity, topmost, target window settings are applied

#### Scenario: Partial import
- **WHEN** user selects "No" for a section
- **THEN** that section is skipped entirely
- **THEN** remaining sections proceed normally

### Requirement: SpineHotkeyEditor supports loading from data

The SpineHotkeyEditor SHALL support a constructor overload that accepts a List of SpineHotkeyEntry and a target file path, allowing import to populate the editor without reading from disk.

#### Scenario: Open editor from imported data
- **WHEN** import flow passes imported hotkey entries to SpineHotkeyEditor
- **THEN** the editor opens showing the imported entries
- **WHEN** user clicks Save in the editor
- **THEN** entries are saved to the specified file path
