## ADDED Requirements

### Requirement: Execute file copy
The system SHALL copy selected source files to all generated target directories when user clicks "开始复制".

#### Scenario: Copy files to all targets
- **WHEN** user has selected source files and configured target paths
- **AND** clicks "开始复制"
- **THEN** each selected file is copied to every target directory
- **AND** progress is displayed in the status bar

#### Scenario: Target directory auto-creation
- **WHEN** a target directory does not exist
- **THEN** the system SHALL create it automatically before copying

### Requirement: Progress feedback
The system SHALL display copy progress in a status bar showing current file count vs total.

#### Scenario: Show copy progress
- **WHEN** copying 3 files to 3 targets (9 total operations)
- **THEN** status bar shows "3/9 已完成" during copy
- **AND** shows "复制完成" when all operations finish

### Requirement: File conflict detection and handling
When a file with the same name already exists at a target directory, the system SHALL stop and show a conflict dialog. Each target directory triggers its own dialog.

#### Scenario: Conflict dialog per target directory
- **WHEN** target D:/exp/a/images already contains "cha_idle.png"
- **THEN** a dialog appears with title "目标目录存在同名文件: D:/exp/a/images"
- **AND** lists the conflicting filenames
- **AND** provides three buttons: "覆盖", "跳过", "打开文件夹"

#### Scenario: Open folder from conflict dialog
- **WHEN** user clicks "打开文件夹" in the conflict dialog
- **THEN** Windows Explorer opens at the target directory
- **AND** the conflict dialog remains open (not dismissed)

#### Scenario: Overwrite on conflict
- **WHEN** user clicks "覆盖" in the conflict dialog
- **THEN** the conflicting files are overwritten
- **AND** the dialog closes
- **AND** copy continues

#### Scenario: Skip on conflict
- **WHEN** user clicks "跳过" in the conflict dialog
- **THEN** the conflicting files are NOT copied to this target
- **AND** the dialog closes
- **AND** copy continues

### Requirement: Copy completion log
The system SHALL log all copy operations via OperationLogger when copy completes.

#### Scenario: Log copy result
- **WHEN** copy operation completes (full success or partial)
- **THEN** OperationLogger records: source files count, target directories count, any skipped or overwritten files

### Requirement: Cancel copy
The system SHALL provide a cancel button during copy execution.

#### Scenario: Cancel during copy
- **WHEN** user clicks cancel during copying
- **THEN** current file copy completes
- **AND** remaining copies are aborted
- **AND** status shows "已取消"
