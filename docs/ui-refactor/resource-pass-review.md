# UI 资源替换后验证记录

本文件记录“资源优先、细化后置”阶段的集中验证结果。它属于 UI 重构计划，不写入 `docs/audit.md`。

## 本轮范围

- 已重新生成 `KeyMacro/assets/ui/` 下通用 UI 资源。
- 已启用 `UiTheme.Apply()` 对普通 WinForms 窗口递归应用主题资源和深色控件样式。
- 虚拟按键窗口本体仍然排除在普通 UI 重构之外，继续使用 `KeyMacro/skins/*` 皮肤系统。
- 已修正 TableLayoutPanel / FlowLayoutPanel / GroupBox / 浅色 Label 背景没有被统一主题化的问题。

## 构建验证

- `dotnet build KeyMacro\KeyMacro.csproj --no-restore`
- `dotnet build docs\ui-refactor\tools\RuntimeWindowCapture\RuntimeWindowCapture.csproj --no-restore -p:UseSharedCompilation=false`
- 结果：0 错误，0 警告。

## 截图验证

集中截图总览：`docs/ui-refactor/resource-pass-contact-sheet.png`

本轮刷新截图：

| 序号 | 界面 | 截图 |
| --- | --- | --- |
| 01 | MainForm | `runtime-mainform-layout-resource-pass.png` |
| 02 | SequenceEditor | `runtime-sequence-editor-layout-resource-pass.png` |
| 03 | HotkeyRecorderForm | `runtime-hotkey-recorder-layout-resource-pass.png` |
| 04 | SpineHotkeyEditor | `runtime-spine-hotkey-editor-layout-resource-pass.png` |
| 05 | VkWindowManager | `runtime-vk-manager-layout-resource-pass.png` |
| 06 | BatchCopyWindow | `runtime-batch-copy-layout-resource-pass.png` |
| 07 | SourceFilePicker | `runtime-source-file-picker-layout-resource-pass.png` |
| 08 | ConflictDialog | `runtime-conflict-dialog-layout-resource-pass.png` |
| 09 | BatchCli 合并页 | `runtime-cli-merge-layout-resource-pass.png` |
| 10 | BatchCli 导出页 | `runtime-cli-export-layout-resource-pass.png` |
| 11 | CLI 动画选择弹窗 | `runtime-cli-animation-select-layout-resource-pass.png` |
| 12 | ReNameTool 重命名页 | `runtime-rename-tool-rename-layout-resource-pass.png` |
| 13 | ReNameTool Spine 文件整理页 | `runtime-rename-tool-organize-layout-resource-pass.png` |
| 14 | ReNameTool 图集解包页 | `runtime-rename-tool-unpack-layout-resource-pass.png` |
| 15 | InputDialog | `runtime-input-dialog-layout-resource-pass.png` |
| 16 | SubfolderSelectDialog | `runtime-subfolder-select-layout-resource-pass.png` |
| 17 | VK 按钮右键菜单 | `runtime-vk-button-menu-layout-resource-pass.png` |

## 后续细化问题暂存

这些问题先作为 UI 重构细化项记录，不作为功能 bug 进入 `docs/audit.md`。

| 序号 | 界面/范围 | 现象 | 建议后续处理 |
| --- | --- | --- | --- |
| UI-RP-001 | 全局窗口标题栏 | 已进入替换第三阶段：普通窗口接入 Windows DWM 原生深色标题栏，标题栏由系统绘制为深色。 | 未改为自绘标题栏，因此保留拖动、最小化、最大化、关闭和系统菜单；`VirtualKeyWindow` 本体继续排除。 |
| UI-RP-002 | DataGridView 内嵌按钮/复选框/下拉编辑格 | 已完成第一轮显示层细化：表格按钮、复选框、下拉格改为深色自绘；不改变单元格点击、勾选和编辑逻辑。 | 继续观察少量单元格选中/编辑态是否仍出现浅色块，必要时逐表格处理。 |
| UI-RP-003 | CLI 动画选择 / 子文件夹选择 | 真实功能仍是 CheckedListBox，和概览图中的表格式结构不同。 | 这是功能结构差异，需用户确认后才可换成表格控件。 |
| UI-RP-004 | ReNameTool / BatchCopy / BatchCli 板块比例 | 资源统一后整体可用，但与概览图的板块比例仍有差距。 | 下一阶段按运行截图逐界面微调位置、宽高和间距。 |
| UI-RP-005 | VK 菜单截图 | 截图只验证右键菜单主题，不验证虚拟按键窗口本体。 | 符合约束；虚拟窗口本体继续使用独立 SKIN 系统。 |
| UI-RP-006 | Details 模式 ListView | 已完成 CLI 明细列表表头/行深色自绘，并让最后一列自动补满列表宽度；缩略图 LargeIcon 列表未受影响。 | 后续只观察真实导出配置状态色是否需要更强的成功/缺失区分。 |
| UI-RP-007 | WinForms 原生 TabControl | 已进入替换第一阶段：CLI 与 ReNameTool 改用 `DarkTabControl`，页签带白条基本消除。 | 继续观察运行截图；若后续仍需进一步统一标题栏/系统边框，另立高风险阶段处理。 |
| UI-RP-008 | WinForms 原生 CheckedListBox | 已进入替换第一阶段：CLI 动画选择与子文件夹选择改用 `DarkCheckedListBox`，勾选框本体改为深色自绘。 | 继续保留 `CheckedListBox` 数据/事件模型，不改为表格；若需要表格式多列，再由用户确认。 |
| UI-RP-009 | 普通 ListBox | 已完成普通 ListBox 深色行、交替行和选中态绘制；已有自绘逻辑的特殊列表不重复接管。 | 后续观察批量复制和 ReNameTool 长路径文字是否需要横向滚动或 tooltip。 |
| UI-RP-010 | 普通 CheckBox / RadioButton | 已完成普通复选框和单选框深色绘制；Checked 状态、AutoCheck 和事件逻辑不变。 | 后续观察少数状态型复选框是否需要不同强调色，例如“有效/无效”。 |
| UI-RP-011 | WinForms 原生 ComboBox | 已进入替换第二阶段：批量复制前缀/后缀历史输入框改用 `DarkComboBox`，右侧下拉按钮改为深色覆盖绘制。 | 仅替换真实普通 ComboBox 使用点；DataGridView 内嵌下拉格继续由表格自绘处理。 |

## 资源后细化记录

### 2026-06-04 DataGridView 交互单元格

- 范围：主窗口、序列编辑器、Spine 热键编辑器、VK 管理器等使用 `DataGridView` 的界面。
- 修改：在 `UiTheme` 中增加表格按钮、复选框、下拉格的显示层自绘。
- 保留：不改变列结构、不改变数据绑定、不改变 CellClick/CellValueChanged/编辑逻辑。
- 验证：`dotnet build KeyMacro\KeyMacro.csproj --no-restore` 与截图工具构建均为 0 错误 / 0 警告。
- 截图：`runtime-mainform-grid-refine.png`、`runtime-sequence-editor-grid-refine.png`、`runtime-spine-hotkey-editor-grid-refine.png`、`runtime-vk-manager-grid-refine.png`。

### 2026-06-04 DataGridView 浅色状态格清理

- 范围：主窗口、序列编辑器、Spine 热键编辑器、VK 管理器的列级状态色。
- 修改：将原本用于“可编辑/可选择/危险操作/分组行”的浅黄、浅蓝、浅红、浅灰背景替换为深色主题对应色。
- 保留：只改 `DefaultCellStyle` 和 `CellFormatting` 的颜色，不改变可编辑列、只读列、按钮列和分组行语义。
- 验证：`dotnet build KeyMacro\KeyMacro.csproj --no-restore` 与截图工具构建均为 0 错误 / 0 警告。
- 截图：`runtime-mainform-grid-refine2.png`、`runtime-sequence-editor-grid-refine2.png`、`runtime-spine-hotkey-editor-grid-refine2.png`、`runtime-vk-manager-grid-refine2.png`。

### 2026-06-04 Details ListView 表头和行色

- 范围：CLI 批量合并/导出页中的 `View.Details` 列表。
- 修改：在 `UiTheme` 中为 Details 模式 ListView 增加深色表头、深色行、选中态绘制，并让最后一列自动补满可视宽度，避免表头右侧出现原生白色空白。
- 保留：不改变列表列语义、列表项数据、双击/选择/删除等逻辑；`SourceFilePicker` 的 `View.LargeIcon + CheckBoxes` 缩略图列表不启用该自绘。
- 验证：`dotnet build KeyMacro\KeyMacro.csproj --no-restore` 与截图工具构建均为 0 错误 / 0 警告。
- 截图：`runtime-cli-merge-listview-refine2.png`、`runtime-cli-export-listview-refine2.png`、`runtime-source-file-picker-listview-regression2.png`。

### 2026-06-04 TabControl 页签试点

- 范围：CLI 批量合并/导出、ReNameTool 等使用 `TabControl` 的界面。
- 修改：在 `UiTheme` 中为 Tab 页签增加深色自绘，并改用 `FlatButtons` 外观，减少原生白色页签按钮。
- 保留：不改变 Tab 页数量、顺序、内容和切换逻辑。
- 验证：`dotnet build KeyMacro\KeyMacro.csproj --no-restore` 与截图工具构建均为 0 错误 / 0 警告。
- 截图：`runtime-cli-export-tabs-refine2.png`、`runtime-rename-tool-rename-tabs-refine2.png`。
- 残留：WinForms 原生 `TabControl` 仍会在页签带背景绘制一条白色区域；完全消除需要自定义控件或改为按钮式导航，先记录为 UI-RP-007，不继续硬改。

### 2026-06-04 CheckedListBox 勾选列表试点

- 范围：SubfolderSelectDialog、CLI 动画选择弹窗。
- 修改：列表背景、文字和行绘制已深色化，并应用 `ThreeDCheckBoxes=false`。
- 保留：不改变勾选项、勾选逻辑、全选/全不选、确认/取消流程。
- 验证：`dotnet build KeyMacro\KeyMacro.csproj --no-restore` 与截图工具构建均为 0 错误 / 0 警告。
- 截图：`runtime-subfolder-select-checkedlist-refine2.png`、`runtime-cli-animation-select-checkedlist-refine2.png`。
- 残留：原生勾选框本体仍为白色；完全替换需要自定义控件或改用表格/列表绘制，先记录为 UI-RP-008。

### 2026-06-04 普通 ListBox 行绘制

- 范围：批量复制源列表/预览列表、ReNameTool 三页文件列表等普通 `ListBox`。
- 修改：为普通 ListBox 增加深色行、交替行和选中态绘制；已有自绘逻辑的列表不重复接管。
- 保留：不改变列表项数据、选择、添加、删除、清空、预览更新和批处理逻辑。
- 验证：`dotnet build KeyMacro\KeyMacro.csproj --no-restore` 与截图工具构建均为 0 错误 / 0 警告。
- 截图：`runtime-batch-copy-listbox-refine2.png`、`runtime-rename-tool-rename-listbox-refine2.png`、`runtime-rename-tool-organize-listbox-refine2.png`、`runtime-rename-tool-unpack-listbox-refine2.png`。

### 2026-06-04 普通 CheckBox / RadioButton 绘制

- 范围：CLI 批量合并/导出页、ReNameTool Spine 文件整理页等普通 `CheckBox` / `RadioButton`。
- 修改：在 `UiTheme` 中为普通复选框和单选框增加深色背景、边框、勾选/选中点绘制。
- 保留：不改变 Checked 状态、AutoCheck、分组互斥、CheckedChanged 事件或任何业务逻辑。
- 验证：`dotnet build KeyMacro\KeyMacro.csproj --no-restore` 与截图工具构建均为 0 错误 / 0 警告。
- 截图：`runtime-cli-merge-check-radio-refine.png`、`runtime-cli-export-check-radio-refine.png`、`runtime-rename-tool-organize-check-radio-refine.png`。

### 2026-06-04 ComboBox 下拉项试点

- 范围：批量复制前缀/后缀历史、其他普通 `ComboBox` 下拉项。
- 修改：在 `UiTheme` 中为普通 ComboBox 增加 OwnerDraw 下拉项深色绘制。
- 保留：不改变输入值、下拉选项、SelectedIndex/SelectedValue、TextChanged/SelectedIndexChanged 等事件逻辑。
- 验证：`dotnet build KeyMacro\KeyMacro.csproj --no-restore` 与截图工具构建均为 0 错误 / 0 警告。
- 截图：`runtime-batch-copy-combo-refine.png`、`runtime-sequence-editor-combo-regression.png`。
- 残留：可输入 DropDown ComboBox 右侧下拉按钮仍为原生白色；完全替换需要自定义控件，先记录为 UI-RP-011。

### 2026-06-04 原生控件替换第一阶段

- 范围：BatchCliWindow 合并/导出页、ReNameTool 三个页签、CLI 动画选择弹窗、SubfolderSelectDialog。
- 修改：新增 `KeyMacro/Controls/DarkTabControl.cs`，以控件继承方式替换原生 `TabControl` 的页签区域绘制，减少页签带白条；新增 `KeyMacro/Controls/DarkCheckedListBox.cs`，以控件继承方式自绘深色勾选框、行背景、选中态和悬停态。
- 保留：不改变 Tab 页数量、顺序、SelectedIndex、TabPage 内容；不改变 CheckedItems、GetItemChecked、SetItemChecked、ItemCheck、CheckOnClick、全选/全不选、确认/取消等业务逻辑。
- 修正：`DarkCheckedListBox` 在控件句柄创建前批量设置初始勾选状态时不再调用 `BeginInvoke`，避免 CLI 动画选择弹窗创建时崩溃。
- 验证：`dotnet build KeyMacro\KeyMacro.csproj --no-restore` 与 `dotnet build docs\ui-refactor\tools\RuntimeWindowCapture\RuntimeWindowCapture.csproj --no-restore -p:UseSharedCompilation=false` 均为 0 错误 / 0 警告。
- 截图：`native-replace-contact-sheet.png`；单图包括 `runtime-cli-merge-native-replace.png`、`runtime-cli-export-native-replace.png`、`runtime-cli-animation-select-native-replace.png`、`runtime-rename-rename-native-replace.png`、`runtime-rename-organize-native-replace.png`、`runtime-rename-unpack-native-replace.png`、`runtime-subfolder-select-native-replace.png`。
- 残留：原生窗口标题栏、ComboBox 右侧下拉按钮和系统滚动条仍为高风险原生项，本阶段未替换。

### 2026-06-04 原生控件替换第二阶段

- 范围：BatchCopyWindow 的前缀/后缀历史输入框。
- 修改：新增 `KeyMacro/Controls/DarkComboBox.cs`，局部替换 `_cmbPrefix` 与 `_cmbSuffix`，覆盖绘制右侧下拉按钮、边框、箭头和下拉项深色行。
- 保留：不改变 `DropDownStyle.DropDown`、用户输入、历史项、自动完成、`TextUpdate`、`SelectedIndexChanged`、保存历史和预览刷新逻辑。
- 验证：`dotnet build KeyMacro\KeyMacro.csproj --no-restore` 与 `dotnet build docs\ui-refactor\tools\RuntimeWindowCapture\RuntimeWindowCapture.csproj --no-restore -p:UseSharedCompilation=false` 均为 0 错误 / 0 警告。
- 截图：`runtime-batch-copy-combo-native-replace.png`。
- 残留：多行 TextBox、ListBox 等系统滚动条仍为原生白色滚动条；该项涉及自定义滚动区域或替换控件，风险高，本阶段未处理。

### 2026-06-04 原生控件替换第三阶段

- 范围：所有调用 `UiTheme.Apply()` 的普通 WinForms 窗口；继续排除 `VirtualKeyWindow` 本体。
- 修改：新增 `KeyMacro/Services/NativeWindowTheme.cs`，通过 `DwmSetWindowAttribute` 请求 Windows 原生深色标题栏，避免改成无边框自绘窗口。
- 保留：不改变 `FormBorderStyle`、窗口拖动、最小化、最大化、关闭、系统菜单、任务栏显示、父窗口定位和 DPI 行为；系统不支持时静默回退原生标题栏。
- 验证：`dotnet build KeyMacro\KeyMacro.csproj --no-restore` 与 `dotnet build docs\ui-refactor\tools\RuntimeWindowCapture\RuntimeWindowCapture.csproj --no-restore -p:UseSharedCompilation=false` 均为 0 错误 / 0 警告。
- 截图：`native-dark-titlebar-contact-sheet.png`；单图包括 `runtime-mainform-titlebar-native-dark.png`、`runtime-batch-copy-titlebar-native-dark.png`、`runtime-input-dialog-titlebar-native-dark.png`。
- 残留：Windows 原生标题栏按钮图标仍由系统决定，不做自定义美术按钮；系统滚动条仍未替换。

### 2026-06-04 原生控件替换第四阶段

- 范围：所有调用 `UiTheme.Apply()` 的普通 WinForms 窗口控件句柄；继续排除 `VirtualKeyWindow` 本体。
- 修改：在 `NativeWindowTheme.ApplyDarkControlChrome()` 中通过 `SetWindowTheme(handle, "DarkMode_Explorer", null)` 请求 Windows 原生深色控件主题，改善 TextBox/ListBox/ListView 等控件的系统滚动条白色残留。
- 保留：不替换 TextBox/ListBox/ListView 控件，不自绘滚动条，不改变鼠标滚动、键盘滚动、选择、输入、复制粘贴或列表数据逻辑；系统不支持时静默回退。
- 验证：`dotnet build KeyMacro\KeyMacro.csproj --no-restore` 与 `dotnet build docs\ui-refactor\tools\RuntimeWindowCapture\RuntimeWindowCapture.csproj --no-restore -p:UseSharedCompilation=false` 均为 0 错误 / 0 警告。
- 截图：`runtime-batch-copy-native-scrollbar-theme.png`。
- 残留：个别控件或旧版 Windows 的滚动条是否变深取决于系统原生主题支持；本阶段不做高风险自定义滚动条。

## 结论

资源优先阶段已可进入下一步：基于新的统一资源截图，再逐界面做布局、控件尺寸和局部样式细化。

## 最新全界面基线

### 2026-06-04 resource-refined

- 构建：`dotnet build KeyMacro\KeyMacro.csproj --no-restore`，结果 0 错误 / 0 警告。
- 截图工具：`dotnet build docs\ui-refactor\tools\RuntimeWindowCapture\RuntimeWindowCapture.csproj --no-restore -p:UseSharedCompilation=false`，结果 0 错误 / 0 警告。
- 联系图：`resource-refined-contact-sheet.png`。
- 单图：`runtime-*-resource-refined.png`。
- 说明：该批截图包含按钮、表格、Details ListView、普通 ListBox、普通 CheckBox/RadioButton、ComboBox 下拉项等低风险资源后细化结果，可作为下一轮逐界面布局细修的最新基线。
