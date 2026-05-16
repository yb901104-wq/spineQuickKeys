## ADDED Requirements

### Requirement: Duplicate step in SequenceEditor
SequenceEditor SHALL provide a "复制" button per step row that duplicates the step and inserts it immediately after.

#### Scenario: Duplicate step
- **WHEN** user clicks the 复制 button on a step row
- **THEN** a copy of that step is inserted below the original

### Requirement: Duplicate sequence in MainForm
MainForm SHALL provide a "复制序列" button that duplicates the selected sequence with a "_副本" suffix.

#### Scenario: Duplicate sequence
- **WHEN** user selects a sequence and clicks 复制序列
- **THEN** a new sequence with same steps and a suffixed name is added to the list
