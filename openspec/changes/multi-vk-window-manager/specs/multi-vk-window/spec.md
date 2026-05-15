## ADDED Requirements

### Requirement: Multiple independent VK windows

The system SHALL support multiple independent VirtualKeyWindow instances. Each window SHALL have:
- Its own set of buttons
- Its own target process/window
- Its own position, size, scale, orientation
- Its own skin
- Independent show/hide/delete lifecycle

#### Scenario: Open two windows
- **WHEN** user creates two VK windows (e.g., "窗口1", "窗口2")
- **WHEN** both have enabled=true
- **WHEN** user clicks "开启虚拟按键"
- **THEN** both windows are displayed independently
- **THEN** each window has its own buttons and target

#### Scenario: Window is fully self-contained
- **WHEN** a VK window is created
- **THEN** it manages its own buttons internally
- **THEN** it saves its own layout data back to the global config
- **THEN** its functionality matches the original single-window behavior

### Requirement: Layout file with multi-window support

The `virtual_layout.json` SHALL use a new format with a `windows` array containing all window configurations. The old single-window format SHALL be auto-detected and migrated to the new format on first load.

#### Scenario: Migrate old format
- **WHEN** loading `virtual_layout.json` without a `windows` key
- **THEN** the content is treated as a single window layout
- **THEN** it is wrapped into `windows[0]`
- **THEN** the file is saved in new format on next save

#### Scenario: New format load
- **WHEN** loading `virtual_layout.json` with a `windows` array
- **THEN** all windows are deserialized
- **THEN** each window's data is available for the manager

### Requirement: VkWindowManager management window

A new Form (`VkWindowManager`) SHALL list all VK windows with:
- A checkbox for "允许显示" (enabled)
- A "显示/隐藏" button for individual toggle
- A "删除" button for permanent deletion
- An "[+ 添加新窗口]" button to create a new window

Each row SHALL show: window name, target process, button count.

#### Scenario: Manager shows window list
- **WHEN** user clicks "管理虚拟按键" in MainForm
- **THEN** VkWindowManager opens with all windows listed
- **THEN** each window shows name, target, button count, enabled checkbox

#### Scenario: Toggle enabled in manager
- **WHEN** user checks/unchecks "允许显示" in manager
- **WHEN** "开启虚拟按键" is clicked later
- **THEN** only checked windows are shown

#### Scenario: Add new window from manager
- **WHEN** user clicks "[+ 添加新窗口]"
- **THEN** a new window with auto-numbered name is added
- **THEN** it appears in the manager list
- **THEN** it is enabled by default

#### Scenario: Delete window from manager
- **WHEN** user clicks "删除" on a window row
- **WHEN** the window is currently shown
- **THEN** the window is closed and disposed
- **THEN** the layout data is permanently removed

### Requirement: VK window right-click "删除当前窗口"

The VK window right-click menu SHALL add a "删除当前窗口" item. This SHALL permanently delete the window (same as delete from manager). The existing "关闭窗口" item SHALL remain unchanged (hide only).

#### Scenario: Delete window from right-click
- **WHEN** user right-clicks blank area
- **WHEN** user selects "删除当前窗口"
- **THEN** the window is closed, disposed, and its layout data is removed
- **THEN** the manager list is updated

### Requirement: MainForm open/close buttons

"开启虚拟按键" SHALL show all windows where `enabled=true` (create and display if hidden). "关闭虚拟按键" SHALL hide all currently displayed windows without changing enabled state.

#### Scenario: Open enabled windows
- **WHEN** user clicks "开启虚拟按键"
- **THEN** for each enabled window: if not yet created, create and show; if hidden, show; if already shown, do nothing

#### Scenario: Close all windows
- **WHEN** user clicks "关闭虚拟按键"
- **THEN** all visible VK windows are hidden
- **THEN** no windows are disposed or deleted
