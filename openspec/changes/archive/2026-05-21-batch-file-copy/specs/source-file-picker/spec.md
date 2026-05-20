## ADDED Requirements

### Requirement: Browse source directory
The system SHALL provide a directory browser button that opens a FolderBrowserDialog for selecting the source file directory.

#### Scenario: Select source directory
- **WHEN** user clicks the browse button
- **THEN** a FolderBrowserDialog opens for directory selection
- **AND** the selected path is displayed in the source directory input

### Requirement: Display file thumbnails
The system SHALL display all image files in the selected directory as a scrollable thumbnail list in LargeIcon view.

#### Scenario: Load thumbnails after directory selected
- **WHEN** user selects a source directory
- **THEN** the system scans for image files (.png, .jpg, .jpeg, .bmp, .gif)
- **AND** displays each file as a thumbnail with filename label below

#### Scenario: Large directory warning
- **WHEN** the selected directory contains more than 200 image files
- **THEN** system SHALL prompt "目录文件过多（超过200个），建议筛选子目录后重试"
- **AND** load only the first 200 files

### Requirement: File selection by checkbox
Each file thumbnail SHALL have a checkbox for multi-selection. The system SHALL display a count of selected files.

#### Scenario: Select individual files
- **WHEN** user checks a file thumbnail checkbox
- **THEN** the file is marked as selected
- **AND** the selected count updates

#### Scenario: Deselect individual files
- **WHEN** user unchecks a file thumbnail checkbox
- **THEN** the file is removed from selection
- **AND** the selected count updates

### Requirement: Source directory refresh
The system SHALL provide a refresh button to reload the file list from the current source directory.

#### Scenario: Refresh file list
- **WHEN** user clicks the refresh button
- **THEN** the system rescans the current directory
- **AND** reloads all thumbnails
- **AND** resets all file selections

### Requirement: Thumbnail async loading
The system SHALL load thumbnails asynchronously to prevent UI thread blocking.

#### Scenario: Background thumbnail loading
- **WHEN** a directory is selected
- **THEN** thumbnails load on a background thread
- **AND** each thumbnail appears in the list as it becomes available
- **AND** the UI remains responsive during loading
