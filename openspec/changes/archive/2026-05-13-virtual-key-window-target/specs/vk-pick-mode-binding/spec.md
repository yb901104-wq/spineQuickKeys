## ADDED Requirements

### Requirement: SequenceEditor enters VkPickMode when VK window is open

The system SHALL enter VkPickMode when user clicks "录制" or "选择虚拟按键" button on the trigger hotkey row, if a VirtualKeyWindow is open and visible. VkPickMode SHALL NOT require any virtual button to have an existing binding.

#### Scenario: Enter pick mode with open VK window
- **WHEN** user clicks "录制" in SequenceEditor
- **WHEN** a VirtualKeyWindow is open and visible
- **THEN** system enters VkPickMode, shows prompt "请在虚拟按键窗口中点击要绑定的按钮"

#### Scenario: No VK window open
- **WHEN** user clicks "录制" in SequenceEditor
- **WHEN** no VirtualKeyWindow is open
- **THEN** system opens HotkeyRecorderForm for manual keyboard recording

### Requirement: Clicking a virtual button in VkPickMode fills both fields

When in VkPickMode, clicking any virtual button SHALL:
1. Read the button's Name property
2. If the button has a BindActionId, resolve the bound sequence's TriggerHotkey
3. Call ReceiveVkPick(buttonName, hotkey) on the SequenceEditor
4. SequenceEditor SHALL set `_txtVkBind.Text = buttonName`
5. SequenceEditor SHALL set `_txtHotkey.Text = hotkey` (if non-empty, otherwise leave as-is)
6. SequenceEditor SHALL set `IsVkPickMode = false`

#### Scenario: Click bound button in VkPickMode
- **WHEN** VkPickMode is active
- **WHEN** user clicks a virtual button that is bound to a sequence with TriggerHotkey "Ctrl+F1"
- **THEN** `_txtVkBind.Text` is set to the button name
- **THEN** `_txtHotkey.Text` is set to "Ctrl+F1"
- **THEN** VkPickMode is deactivated

#### Scenario: Click unbound button in VkPickMode
- **WHEN** VkPickMode is active
- **WHEN** user clicks a virtual button that has no binding
- **THEN** `_txtVkBind.Text` is set to the button name
- **THEN** `_txtHotkey.Text` is not changed (stays empty or previous value)
- **THEN** VkPickMode is deactivated

### Requirement: SyncVkButtonBindings does not destroy existing bindings

The system SHALL modify `MainForm.SyncVkButtonBindings` so that:
- When a virtual button's name matches a sequence's TriggerVkButtonName → set BindActionId to that sequence's Id
- When no match is found → leave the existing BindActionId intact (do NOT set to null)

#### Scenario: Name-based matching preserves unrelated bindings
- **WHEN** a virtual button has BindActionId set via context menu
- **WHEN** no sequence's TriggerVkButtonName matches this button's name
- **WHEN** SaveAndRefresh() is called
- **THEN** the button's BindActionId is NOT cleared
