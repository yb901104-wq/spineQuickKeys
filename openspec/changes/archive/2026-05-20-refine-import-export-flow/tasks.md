## 1. DataBundle model

- [x] 1.1 Change `VkData` from `WindowLayoutData?` to `List<WindowLayoutData>? VkDataList`

## 2. Export

- [x] 2.1 SpineHotkeys: read from ConfigService.LoadSpinePath() via SpineHotkeyService.Load()
- [x] 2.2 VkData: export all windows instead of first only

## 3. Import

- [x] 3.1 Backward compat: if VkDataList is null, try reading from old VkData field
- [x] 3.2 Spine hotkeys: key-aligned replacement in TXT file
- [x] 3.3 Translations: key-aligned replacement in spine_translations.txt
- [x] 3.4 Window import: iterate VkDataList, check name collision, prompt per window
