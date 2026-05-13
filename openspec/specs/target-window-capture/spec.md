## ADDED Requirements

### Requirement: User can capture target window

The system SHALL provide a "捕获目标窗口" menu item in the VirtualKeyWindow blank area context menu.

When clicked:
1. The VK window SHALL hide itself
2. A translucent overlay window SHALL display a countdown: "请在 3 秒内切换到目标窗口... 3... 2... 1"
3. After the countdown, the system SHALL call `GetForegroundWindow()` to capture the foreground window handle
4. The system SHALL retrieve the process name via `GetWindowThreadProcessId` + `Process.ProcessName`
5. The system SHALL retrieve the window title via `GetWindowText`
6. The VK window SHALL restore itself to visible state
7. The captured target info SHALL be persisted to `VirtualLayoutSerializer.LayoutData`

#### Scenario: Successful capture
- **WHEN** user right-clicks blank area in VK window and selects "捕获目标窗口"
- **WHEN** user switches to Spine within 3 seconds
- **THEN** VK window hides, countdown runs, then VK reappears
- **THEN** the captured process name is saved to layout data

#### Scenario: User cancels capture
- **WHEN** user right-clicks and selects "捕获目标窗口"
- **WHEN** user presses Escape during countdown
- **THEN** capture is cancelled, VK window reappears, no state changed

### Requirement: User can clear target window

The system SHALL provide a "清除目标窗口" menu item in the VK window blank area context menu (only visible when a target is set).

#### Scenario: Clear existing target
- **WHEN** a target is set and user selects "清除目标窗口"
- **THEN** target process name and title are cleared from layout data
- **THEN** subsequent button clicks fall back to normal behavior (no target activation)

### Requirement: Target window info persists across sessions

The system SHALL save captured target info in `VirtualLayoutSerializer.LayoutData`:

```
LayoutData {
    ...
    TargetProcessName: string  // e.g. "Spine", "javaw"
    TargetWindowTitle: string  // optional, for multi-instance disambiguation
}
```

#### Scenario: Save and restore
- **WHEN** user captures a target window
- **WHEN** VK window is closed and reopened
- **THEN** the captured target info is restored from layout data

### Requirement: Target window is resolved by process name

On each button click, the system SHALL resolve the target window handle by:
1. Getting all processes matching `TargetProcessName`
2. If `TargetWindowTitle` is set, find the window with matching title
3. If no title match or title is empty, use the first found window
4. If no process found, SHALL silently skip activation (target process not running)

#### Scenario: Target process running
- **WHEN** target is set to "Spine" and Spine is running
- **WHEN** user clicks a virtual button
- **THEN** system resolves the Spine window handle successfully

#### Scenario: Target process not running
- **WHEN** target is set to "Spine" but Spine is closed
- **WHEN** user clicks a virtual button
- **THEN** system SHALL play the sequence without target activation (fallback to current behavior)
