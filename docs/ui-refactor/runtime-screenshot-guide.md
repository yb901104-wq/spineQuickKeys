# 运行时 UI 截图验证机制

本文件用于固定 UI 调整后的截图验证流程，避免每次临时重写截图脚本。

## 已保留截图

上一轮真实运行截图保存在 `docs/ui-refactor/`，文件名统一为 `runtime-*.png`。

| 界面 | 截图 |
| --- | --- |
| MainForm 主窗口 | `runtime-mainform-check.png` |
| MainForm 第二轮 | `runtime-mainform-round2-check.png` |
| SequenceEditor 序列编辑器 | `runtime-sequence-editor-check.png` |
| HotkeyRecorderForm 热键录制 | `runtime-hotkey-recorder-check.png` |
| SpineHotkeyEditor | `runtime-spine-hotkey-editor-check.png` |
| VkWindowManager | `runtime-vk-manager-check.png` |
| BatchCopyWindow | `runtime-batch-copy-check.png` |
| SourceFilePicker | `runtime-source-file-picker-check.png` |
| ConflictDialog | `runtime-conflict-dialog-check.png` |
| InputDialog | `runtime-input-dialog-check.png` |
| SubfolderSelectDialog | `runtime-subfolder-select-check.png` |
| BatchCliWindow 合并页 | `runtime-cli-merge-check.png` |
| BatchCliWindow 导出页 | `runtime-cli-export-check.png` |
| ReNameTool 批量重命名 | `runtime-rename-tool-rename-check.png` |
| ReNameTool Spine 整理 | `runtime-rename-tool-organize-check.png` |
| ReNameTool 图集解包 | `runtime-rename-tool-unpack-check.png` |

## 固定截图规则

1. 每次 UI 修改后，先 `dotnet build KeyMacro/KeyMacro.csproj --no-restore`。
2. 由运行中的真实窗口进入目标界面，不再为每个窗口临时设计打开脚本。
3. 使用 `docs/ui-refactor/tools/capture-window.ps1` 捕获指定标题窗口。
4. 新截图命名格式：`runtime-{界面名}-round{轮次}-check.png`。
5. 截图后在 `per-interface-adjustment.md` 中记录：截图文件、检查项、是否有遮挡/出框/功能缺失。

## 通用截图命令

示例：捕获标题包含“编辑序列”的窗口。

```powershell
powershell -ExecutionPolicy Bypass -File docs/ui-refactor/tools/capture-window.ps1 `
  -TitlePattern "编辑序列" `
  -Output docs/ui-refactor/runtime-sequence-editor-round2-check.png
```

示例：捕获当前激活窗口。

```powershell
powershell -ExecutionPolicy Bypass -File docs/ui-refactor/tools/capture-window.ps1 `
  -ActiveWindow `
  -Output docs/ui-refactor/runtime-active-check.png
```

## 可复用窗体截图工具

对于需要直接打开项目窗体并填入示例数据的验证，使用 `docs/ui-refactor/tools/RuntimeWindowCapture/`。

当前支持：

| key | 窗体 | 说明 |
| --- | --- | --- |
| `sequence-editor` | `SequenceEditor` | 使用示例序列和示例步骤行，便于检查列表单元格、下拉列、复制按钮列 |
| `spine-hotkey-editor` | `SpineHotkeyEditor` | 使用示例 Spine 热键行，便于检查只读名称列、快捷键编辑列、中文说明列 |
| `vk-manager` | `VkWindowManager` | 使用当前布局数据，便于检查窗口名称、目标、按钮数、允许显示、显示/隐藏、删除列 |
| `batch-copy` | `BatchCopyWindow` | 打开批量复制主界面，便于检查源文件、目标路径、预览、进度条和底部状态区 |
| `source-file-picker` | `SourceFilePicker` | 打开源文件选择器，便于检查目录输入、浏览/刷新、缩略图列表、确认/取消 |

示例：

```powershell
dotnet run --project docs/ui-refactor/tools/RuntimeWindowCapture/RuntimeWindowCapture.csproj -- `
  sequence-editor `
  docs/ui-refactor/runtime-sequence-editor-round2-check.png
```

后续新增窗口截图入口时，只允许加入 `docs/ui-refactor/tools/RuntimeWindowCapture/Program.cs`，不得每次临时创建一次性截图项目。

## 注意事项

- 如果目标窗口需要用户操作进入，则先手动或通过真实应用打开，再执行截图脚本。
- 截图脚本只负责捕获窗口，不负责修改应用状态。
- 如果应用已有托盘实例导致新实例无法显示，应先确认是否需要关闭旧实例，再截图。
- 虚拟按键窗口本体不纳入普通 UI 重构截图；只截图右键菜单和相关输入弹窗。
