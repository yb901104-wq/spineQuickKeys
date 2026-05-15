## Why

主窗口 DataGridView 列宽固定，窗口缩小时出现横向滚动条而非等比缩放。Spine 热键文件每次重新打开应用需要重新载入，而 SequenceEditor 的自动补全依赖于已载入的 Spine 按键数据，导致每次重启后自动补全不可用。

## What Changes

- **主窗口列宽等比缩放**：将 DataGridView 文本列的 `AutoSizeMode` 从 `None` 改为 `Fill`，设置 `FillWeight` 比例。保留固定列（启用、选择）为 `None`，清除列保持 `Fill`
- **Spine 路径持久化**：在 `%APPDATA%\KeyMacro\.spine_path` 文件中存储最近 Spine TXT 文件路径；启动时自动载入到 `SpineHotkeyEditor.LastLoadedEntries`；Spine 编辑按钮在有缓存时跳过文件对话框
- **新增释放按钮**：在 Spine 编辑按钮旁增加「释放」按钮，点击后清除 `LastLoadedEntries` 和已保存的路径文件

## Capabilities

### New Capabilities
- `spine-path-persist`: Spine 热键文件路径本地持久化，支持启动自动载入和手动释放

### Modified Capabilities
- (无现有 spec 需要修改)

## Impact

- [MainForm.cs](KeyMacro/Forms/MainForm.cs): `RefreshGrid()` 列宽改用 Fill 模式；新增 `_btnSpineRelease` 按钮和事件；`OpenSpineEditor()` 路径记忆；`MainForm_Shown` 中自动载入 spine 数据
- [ConfigService.cs](KeyMacro/Services/ConfigService.cs): 新增 `SaveSpinePath()` 和 `LoadSpinePath()` 静态方法
