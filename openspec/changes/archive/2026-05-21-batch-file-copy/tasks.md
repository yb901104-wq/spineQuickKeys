## 1. Source File Picker (弹窗模式)

- [x] 1.1 Create `SourceFilePicker` dialog form (directory browse + thumbnail preview + file check)
- [x] 1.2 Move async thumbnail loading from `BatchCopyWindow` to `SourceFilePicker`
- [x] 1.3 Replace embedded thumbnail panel in `BatchCopyWindow` with file path list + [选择文件] button
- [x] 1.4 Implement file list dedup on re-select (compare by full path)

## 2. Middle List (多行文本模式)

- [x] 2.1 Replace FlowLayoutPanel tag buttons with multiline `TextBox` in target panel
- [x] 2.2 Implement add-line button (InputDialog → append to textbox)
- [x] 2.3 Implement delete-selected-line button
- [x] 2.4 Update path combination logic to read lines from multiline textbox
- [x] 2.5 Update subfolder import to write to multiline textbox instead of tag list

## 3. Target Path History（ComboBox 下拉记忆）

- [x] 3.1 Add `PathHistory` model to config (`PrefixHistory`, `SuffixHistory`)
- [x] 3.2 Add load/save/clear history methods in `ConfigService`
- [x] 3.3 Replace `TextBox` prefix/suffix with `ComboBox` (DropDown mode) with auto-save on input
- [x] 3.4 Add "清理历史记录" button that clears both combo box histories
- [x] 3.5 Remove old profile system (save/load/delete profile buttons, `CopyProfileItem` model, `copy_profiles.json`)

## 4. Fixes & Cleanup

- [x] 4.1 Fix `explorer.exe` call in `ConflictDialog` — use `Path.GetFullPath(targetDir)`
- [x] 4.2 Remove unused profile UI from `BatchCopyWindow`
- [x] 4.3 Clean up unused imports and fields
- [x] 4.4 Build and verify 0 warnings / 0 errors
- [x] 4.5 Update CLAUDE.md with final file structure

## 5. Test

- [ ] 5.1 Test with real Spine project directories
- [ ] 5.2 Test conflict dialog open folder
- [ ] 5.3 Test history persistence across app restarts
