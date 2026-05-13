## ADDED Requirements

### Requirement: MacroPlayer supports target window parameter

The system SHALL add a new method `PlayToWindow(MacroSequence sequence, IntPtr targetHwnd)` to `MacroPlayer`.

This method SHALL:
- Use `PostMessage` instead of `SendKeys`/`keybd_event` for all keyboard simulation
- Not include the 500ms initial delay (not needed since target is known)
- Support all three step types: `Key`, `Combo`, `Text`

#### Scenario: Play single key to target window
- **WHEN** a sequence has a step of type "单键" with key "F5"
- **WHEN** `PlayToWindow` is called with a valid target HWND
- **THEN** system posts `WM_KEYDOWN` + `WM_KEYUP` for VK_F5 to the target window

#### Scenario: Play combo to target window
- **WHEN** a sequence has a step of type "组合键" with keys "Ctrl+Z"
- **WHEN** `PlayToWindow` is called with a valid target HWND
- **THEN** system posts `WM_KEYDOWN` for VK_CONTROL, then `WM_KEYDOWN` + `WM_KEYUP` for VK_Z, then `WM_KEYUP` for VK_CONTROL

#### Scenario: Play text to target window
- **WHEN** a sequence has a step of type "文本" with text "hello"
- **WHEN** `PlayToWindow` is called with a valid target HWND
- **THEN** system posts `WM_CHAR` messages for 'h', 'e', 'l', 'l', 'o' to the target window

### Requirement: PostMessage uses correct lParam construction

The system SHALL construct `lParam` for each `PostMessage` call:
- ScanCode: obtained via `MapVirtualKey(wParam, MAPVK_VK_TO_VSC)`
- Extended flag: set for extended keys (arrows, PageUp/Down, Home/End, Insert, Delete)
- KeyDown: `WM_KEYDOWN` with bit 30 = 0
- KeyUp: `WM_KEYUP` with bit 30 = 1, bit 31 = 1

#### Scenario: Extended key handling
- **WHEN** a key is an extended key (e.g., RIGHT, END, INSERT)
- **THEN** lParam bit 24 SHALL be set to 1

### Requirement: PlayToWindow supports hold/press mode

For `PressMode.Hold` steps, the system SHALL:
1. Post `WM_KEYDOWN` for the key
2. Wait `HoldDurationMs`
3. Post `WM_KEYUP` for the key

#### Scenario: Hold key in target window
- **WHEN** a step has press mode "长按" with hold duration 500ms
- **WHEN** `PlayToWindow` is called
- **THEN** system posts WM_KEYDOWN, waits 500ms, then posts WM_KEYUP

### Requirement: System detects PostMessage failure and falls back

After `PlayToWindow` completes, the system SHALL check if the target window is now the foreground window:
- If target IS foreground → PostMessage succeeded, do nothing extra
- If target is NOT foreground → PostMessage likely had no effect, mark scheme-A as failed for this session
- Subsequent button clicks in the same session SHALL fall back to scheme B (auto-activate)

#### Scenario: Auto-detect PostMessage failure
- **WHEN** PlayToWindow is called but target does not respond
- **WHEN** after playback target is not the foreground window
- **THEN** system flags scheme-A as failed for this session
- **THEN** next button click uses scheme B (auto-activate)
