## Why

在编辑序列步骤时，用户手动输入按键值需要记忆按键格式和热键组合。通过集成 Spine 热键文档的搜索补全，用户只需输入模糊关键词即可在热键列表中检索并自动填入正确的快捷键组合，减少记忆负担和输入错误。

## What Changes

- 编辑序列步骤的"按键/文本"单元格时，自动弹出下拉搜索菜单
- 搜索逻辑根据输入内容自动分流：
  - 含中文字符 → 匹配 Spine 热键的中文翻译（ChineseNote）
  - 含 `+` 号 → 匹配快捷键绑定值（Keys）
  - 其他 → 匹配热键英文原名（Name）
- 选中后自动填入对应的快捷键组合（Keys 字段）
- SpineHotkeyEditor 暴露静态当前条目数据供 SequenceEditor 访问
- 仅当用户已载入 Spine 热键文档时启用补全

## Capabilities

### New Capabilities
- `step-autocomplete`: 序列编辑器中步骤按键输入的热键搜索补全功能

### Modified Capabilities

（无）

## Impact

- [SpineHotkeyEditor.cs](KeyMacro/Forms/SpineHotkeyEditor.cs): 新增静态属性 `LastLoadedEntries` 暴露当前热键条目
- [SequenceEditor.cs](KeyMacro/Forms/SequenceEditor.cs): 步骤表格编辑时新增弹出搜索下拉菜单
