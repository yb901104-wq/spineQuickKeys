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
| `mainform-layout` | `MainForm` | 打开主窗口，用于 round3 布局对齐概览图截图 |
| `spine-hotkey-editor` | `SpineHotkeyEditor` | 使用示例 Spine 热键行，便于检查只读名称列、快捷键编辑列、中文说明列 |
| `vk-manager` | `VkWindowManager` | 使用当前布局数据，便于检查窗口名称、目标、按钮数、允许显示、显示/隐藏、删除列 |
| `batch-copy` | `BatchCopyWindow` | 打开批量复制主界面，便于检查源文件、目标路径、预览、进度条和底部状态区 |
| `source-file-picker` | `SourceFilePicker` | 打开源文件选择器，便于检查目录输入、浏览/刷新、缩略图列表、确认/取消 |
| `conflict-dialog` | `ConflictDialog` | 使用示例冲突文件，便于检查冲突列表和打开文件夹/覆盖/跳过/取消全部复制按钮 |
| `input-dialog` | `InputDialog` | 使用修改按钮名称示例，便于检查提示、输入框、确定/取消 |
| `subfolder-select` | `SubfolderSelectDialog` | 使用示例条目，便于检查搜索、不包含、全选/全不选、勾选列表、确认/取消 |
| `batch-cli-merge` | `BatchCliWindow` | 打开合并页，便于检查 CLI 路径、源/目标列表、动画选择、from/to、实验合并、进度区 |
| `batch-cli-export` | `BatchCliWindow` | 打开导出页，便于检查源目录、扫描、文件列表、导出配置、输出目录、导出/单纹理图、进度区 |
| `rename-tool-rename` | `ReNameTool.Form1` | 打开重命名页，便于检查文件列表、全名替换、关键词替换、清空、选择文件/文件夹和进度区 |
| `rename-tool-organize` | `ReNameTool.Form1` | 打开 Spine 文件整理页，便于检查列表、bytes/txt 勾选、源/保存位置、清空、开始整理和进度区 |
| `rename-tool-unpack` | `ReNameTool.Form1` | 打开 Spine 图集自动解包页，便于检查列表、目标文件夹、清空、开始解包和进度区 |
| `hotkey-recorder` | `HotkeyRecorderForm` | 打开热键录制弹窗，便于检查提示区域和录制状态文字 |
| `vk-blank-menu` | `VirtualKeyWindow` | 打开测试 VK 窗口并弹出空白区域右键菜单，便于检查新增按钮、窗口控制、目标窗口、缩放等菜单项 |
| `vk-button-menu` | `VirtualKeyWindow` | 打开测试 VK 窗口并弹出按钮右键菜单，便于检查修改名称、按钮间距、强制停止、删除当前按钮 |
| `tray-menu` | `MainForm` | 打开主窗口并弹出托盘菜单对象，便于检查打开主窗口、暂停全部、退出 |

示例：

```powershell
dotnet run --project docs/ui-refactor/tools/RuntimeWindowCapture/RuntimeWindowCapture.csproj -- `
  sequence-editor `
  docs/ui-refactor/runtime-sequence-editor-round2-check.png
```

后续新增窗口截图入口时，只允许加入 `docs/ui-refactor/tools/RuntimeWindowCapture/Program.cs`，不得每次临时创建一次性截图项目。

## Round3 布局对齐截图

`round3` 用于检查控件位置、窗口默认尺寸、板块比例和列表/输入框/按钮区域是否接近概览图；它不代表最终美术资源已经替换。

| 界面 | 截图 |
| --- | --- |
| MainForm | `runtime-mainform-layout-round3-check.png` |
| SequenceEditor | `runtime-sequence-editor-layout-round3-check.png` |

## 注意事项

- 如果目标窗口需要用户操作进入，则先手动或通过真实应用打开，再执行截图脚本。
- 截图脚本只负责捕获窗口，不负责修改应用状态。
- 如果应用已有托盘实例导致新实例无法显示，应先确认是否需要关闭旧实例，再截图。
- 虚拟按键窗口本体不纳入普通 UI 重构截图；只截图右键菜单和相关输入弹窗。
- 右键菜单/托盘菜单可用 `RuntimeWindowCapture` 的菜单入口截图；若需人工复核，再真实触发菜单后执行 `capture-window.ps1 -ActiveWindow`。


## Round3 布局对齐截图补充（2026-06-04）

本轮截图用于对比最新运行时窗口与 UI 概览图的控件位置、默认尺寸、列表/输入框/按钮区域和进度区，不代表最终美术资源已经替换。

| 界面 | 运行时截图 |
| --- | --- |
| MainForm | `runtime-mainform-layout-round3-check.png` |
| SequenceEditor | `runtime-sequence-editor-layout-round3-check.png` |
| SpineHotkeyEditor | `runtime-spine-hotkey-editor-layout-round3-check.png` |
| VkWindowManager | `runtime-vk-manager-layout-round3-check.png` |
| BatchCopyWindow | `runtime-batch-copy-layout-round3-check.png` |
| SourceFilePicker | `runtime-source-file-picker-layout-round3-check.png` |
| BatchCliWindow 合并页 | `runtime-cli-merge-layout-round3-check.png` |
| BatchCliWindow 导出页 | `runtime-cli-export-layout-round3-check.png` |
| BatchCliWindow 动画选择弹窗 | `runtime-cli-animation-select-layout-round3-check.png` |
| ReNameTool 重命名页 | `runtime-rename-tool-rename-layout-round3-check.png` |
| ReNameTool Spine 整理页 | `runtime-rename-tool-organize-layout-round3-check.png` |
| ReNameTool 图集解包页 | `runtime-rename-tool-unpack-layout-round3-check.png` |
| HotkeyRecorderForm | `runtime-hotkey-recorder-layout-round3-check.png` |
| ConflictDialog | `runtime-conflict-dialog-layout-round3-check.png` |
| InputDialog | `runtime-input-dialog-layout-round3-check.png` |
| SubfolderSelectDialog | `runtime-subfolder-select-layout-round3-check.png` |
| VK 空白区域右键菜单 | `runtime-vk-blank-menu-layout-round3-check.png` |
| VK 按钮右键菜单 | `runtime-vk-button-menu-layout-round3-check.png` |
| 托盘菜单 | `runtime-tray-menu-layout-round3-check.png` |

本轮同时更新了 `RuntimeWindowCapture` 的示例数据填充：批量复制、CLI 合并/导出、ReNameTool 三个页签会在截图时显示示例列表和进度条文字，便于检查“当前处理文件文字在进度条上方，进度条中间显示进度”的结构。


## Round3 并排对比图

已生成概览图与运行截图的并排对比图，索引见 `docs/ui-refactor/layout-round3-comparison.md`，图片保存在 `docs/ui-refactor/comparisons/round3/`。
