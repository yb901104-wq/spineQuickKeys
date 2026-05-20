## ADDED Requirements

### Requirement: Save copy profile
The system SHALL allow users to save current target path configuration (Prefix, Middle list, Suffix) as a named profile.

#### Scenario: Save current configuration
- **WHEN** user has configured Prefix, Middle, and Suffix
- **AND** clicks "保存方案"
- **THEN** a dialog asks for a profile name
- **AND** the configuration is saved to config.json under BatchCopyProfiles

### Requirement: Load copy profile
The system SHALL display saved profiles in a dropdown, allowing users to load a profile's configuration.

#### Scenario: Load profile
- **WHEN** user selects a profile from the dropdown
- **THEN** Prefix, Middle, and Suffix fields are populated with the saved values
- **AND** the path preview updates accordingly

### Requirement: Delete copy profile
The system SHALL allow users to delete a saved profile.

#### Scenario: Delete profile
- **WHEN** user selects a profile and clicks delete
- **THEN** a confirmation dialog appears: "确定要删除方案 [名称] 吗？"
- **AND** on confirm, the profile is removed from config.json

### Requirement: Clear all profiles
The system SHALL provide a "清理历史记录" button that removes all saved profiles.

#### Scenario: Clear all profiles
- **WHEN** user clicks "清理历史记录"
- **THEN** a confirmation dialog appears: "确定要清理所有保存的方案吗？"
- **AND** on confirm, all profiles are removed from config.json
- **AND** the profile dropdown is cleared

### Requirement: Profile persistence
Profiles SHALL be stored in config.json under the BatchCopyProfiles key, using the existing ConfigService.

#### Scenario: Data structure
- **WHEN** saving a profile
- **THEN** it is stored in the format:
  ```json
  {
    "CopyProfiles": [
      { "Name": "...", "Prefix": "...", "Middleware": [...], "Suffix": "..." }
    ]
  }
  ```
