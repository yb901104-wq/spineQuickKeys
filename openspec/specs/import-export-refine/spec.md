# import-export-refine

## Requirements

### REQ1: Export all VK windows
- `DataBundle.VkData` → `List<WindowLayoutData> VkDataList`
- Export `LoadAll().Windows` not just `FirstOrDefault()`
- Backward compat: import code handles old single-window format

### REQ2: Export SpineHotkeys from file, not editor
- Use `ConfigService.LoadSpinePath()` to get last spine TXT path
- `SpineHotkeyService.Load()` to read entries
- No-op if no spine path configured

### REQ3: Key-aligned spine hotkey import
- Parse target TXT line by line
- For each `name: keys` line, look up imported entry by name
- Only replace `keys` part (`:` after content)
- Never add, delete, duplicate, or modify `name` part
- Write back to file preserving all structure (comments, blank lines, section headers)

### REQ4: Key-aligned translation import
- Parse `spine_translations.txt` line by line
- For each `key=value` line, look up imported entry by name
- Only replace `value` part (`=` after content)
- Write back preserving structure

### REQ5: Granular import confirmation
```
Import confirmation:
├── [ ] Import spine hotkey bindings? (key-aligned replacement)
├── [ ] Import key function descriptions? (translations)
├── [ ] Import all macro sequences?
├── [ ] Import window "xxx"? (repeat per window)
```
- Window name collision: detect before prompting, show warning in prompt text
- If `VkDataList` is empty/null, skip window section entirely

### REQ6: Window name collision detection
- When iterating imported windows, check `_vkSerializer.LoadAll().Windows` for name match
- If collision found, show `"窗口 \"xxx\" 已存在, 是否覆盖?"` in the prompt
- On yes: replace existing window data at that index
- On no: skip that window
