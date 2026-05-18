## Why

Spine 快捷键编辑器在保存时使用行号而非名称来匹配数据，导致搜索过滤后保存会产生快捷键按键/功能说明错位。同时，主窗口的"释放"按钮在打开编辑器后始终不会激活。annotations 文件使用字典格式，手动编辑不便。

## What Changes

- **修复释放按钮状态**：`OpenSpineEditor()` 关闭编辑器后调用 `UpdateSpineReleaseButton()`
- **修复保存错位**：`BtnSave_Click` 改为按快捷键名称（Name）匹配回写，而非行号索引
- **annotations 格式改为 JSON 数组**：从 `Dictionary<string, string>` 改为 `List<AnnotationEntry>`，数组元素包含 name 和 note 字段，兼容旧格式读取

## Capabilities

### New Capabilities
- `spine-annotation-format`: Spine 快捷键注解文件格式定义，JSON 数组结构，包含 name/note 字段

### Modified Capabilities

无。修复内容不涉及现有 spec 定义的行为变更。

## Impact

- `SpineHotkeyEditor.cs`：BtnSave_Click 保存逻辑
- `SpineHotkeyService.cs`：LoadAnnotations/SaveAnnotations 方法，AnnotationEntry 模型
- `MainForm.cs`：OpenSpineEditor 方法
- 用户磁盘上的 `.annotations.json` 文件：格式从字典迁移为数组
