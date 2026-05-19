## Why

Spine 快捷键编辑器存在两个问题：删除快捷键（清空按键组合）后保存不生效，以及历史污
染导致 TXT 文件中出现条目重复。前者根因是 DataGridView 正在编辑的单元格在保存前未
提交，后者根因是旧版本行号索引保存 bug 留下了重复行。

## What Changes

- **BtnSave_Click 保存前提交编辑**：调用 `_dgv.EndEdit()` 确保单元格值写入后再读取
- **加载去重**：`SpineHotkeyService.Load()` 中同名条目只保留第一个
- **空键条目不写入 TXT**：Keys 为空的条目跳过 TXT 写入，只在 annotations 中保留

## Capabilities

### New Capabilities
- `hotkey-dedup-and-delete`: Spine 热键文件加载去重、空键条目跳过写入、保存前提交编辑

### Modified Capabilities

无。

## Impact

- `SpineHotkeyService.cs`：Load/Save 方法
- `SpineHotkeyEditor.cs`：BtnSave_Click 开头添加 EndEdit
