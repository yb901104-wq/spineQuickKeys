## 1. SpineHotkeyEditor 数据暴露

- [x] 1.1 [SpineHotkeyEditor.cs](KeyMacro/Forms/SpineHotkeyEditor.cs): 新增 `static List<SpineHotkeyEntry>? LastLoadedEntries` 属性
- [x] 1.2 [SpineHotkeyEditor.cs](KeyMacro/Forms/SpineHotkeyEditor.cs): 在 `LoadEntries()` 末尾设置 `LastLoadedEntries = _entries`
- [x] 1.3 [SpineHotkeyEditor.cs](KeyMacro/Forms/SpineHotkeyEditor.cs): 在数据构造器中设置 `LastLoadedEntries = entries`
- [x] 1.4 [SpineHotkeyEditor.cs](KeyMacro/Forms/SpineHotkeyEditor.cs): 在 `FormClosed` 中清除 `LastLoadedEntries`

## 2. 搜索下拉菜单实现

- [x] 2.1 [SequenceEditor.cs](KeyMacro/Forms/SequenceEditor.cs): 添加 `ListBox _suggestionList` 字段
- [x] 2.2 [SequenceEditor.cs](KeyMacro/Forms/SequenceEditor.cs): 实现搜索分流方法（中文/含+/其他）
- [x] 2.3 [SequenceEditor.cs](KeyMacro/Forms/SequenceEditor.cs): 实现下拉弹出逻辑（定位在单元格下方）
- [x] 2.4 [SequenceEditor.cs](KeyMacro/Forms/SequenceEditor.cs): 实现键盘导航（↑↓EnterEsc）
- [x] 2.5 [SequenceEditor.cs](KeyMacro/Forms/SequenceEditor.cs): 实现选中填充逻辑（填入 Keys，同步步骤类型）

## 3. 集成与测试

- [x] 3.1 `dotnet build` 确认编译通过
