# UI 重构概览图总览

本页汇总 `docs/ui-refactor/mockups/` 下的高保真静态概览图。图片用于确认未来 UI 重构方向，不代表当前软件已经实现。

参考图：仓库根目录 `UI美术风格参考.png`。只参考风格，不参考布局。

## 视觉规范

![Style Board](mockups/00-style-board.png)

- 深灰工作台背景。
- 紧凑面板、轻微圆角方框和细边框。
- 蓝色主操作/选中态。
- 橙色警示/Spine 关联提示。
- 青色工具/捕获辅助态。
- 输入框、列表和按钮必须有更明显的边界，不能和周边面板糊在一起。
- 按钮需要更黑的圆角底座；激活/未激活状态需要两套图或绘制态。
- 批量处理进度区保持“当前处理对象文字在上，进度条居中显示进度”。

## 主窗口

![Main Window](mockups/01-main-window.png)

- 主工具栏保持所有现有入口。
- 宏序列表格强化扫描效率。
- 状态栏保留配置、VK、热键等运行状态。

## 序列编辑

![Sequence Editor](mockups/02-sequence-editor.png)

- 顶部集中显示序列名称、键盘热键、VK 绑定和目标软件。
- 步骤列表保持表格主导，操作按钮集中在步骤工具栏。
- 确认/取消按钮统一放在右下角。

![Hotkey Recorder](mockups/03-hotkey-recorder.png)

- 录制状态居中，按键结果用胶囊组件显示。
- 适用于序列编辑器录制和 Spine 热键录制。

## Spine 热键编辑

![Spine Hotkey Editor](mockups/04-spine-hotkey-editor.png)

- 文件路径、载入、录制、搜索集中在顶部。
- 表格分组、快捷键、中文说明、状态列统一暗色风格。

## 虚拟按键管理

`VirtualKeyWindow` 虚拟按钮浮窗本体、按钮图片和 VK 皮肤资源本轮不纳入普通 UI 重构。该模块已有独立 SKIN 系统、按钮状态图和布局逻辑，贸然套用普通工具窗口 UI 容易破坏现有功能与现有风格。

但虚拟窗口右键菜单，以及右键菜单触发的“修改按钮名称”“循环延迟”“按钮间距”“删除确认”“捕获结果”等输入/确认弹窗，纳入统一菜单和弹窗美术。

![VK Manager](mockups/05-vk-manager.png)

- 多窗口管理以表格为主。
- 显示/隐藏、允许显示、删除等状态列清晰区分。

## 批量复制

![Batch Copy](mockups/06-batch-copy.png)

- 源文件、三段式目标路径、预览目标分区明确。
- 开始复制按钮保持显眼。
- 进度文字和进度条独立，不遮挡其他功能区。

![Source File Picker](mockups/07-source-file-picker.png)

- 缩略图网格暗色化。
- 已选文件用蓝色边框和勾选文字提示。

![Conflict Dialog](mockups/08-conflict-dialog.png)

- 冲突提示使用橙色警示条。
- 覆盖、跳过、取消全部、打开文件夹按钮分级明确。

## CLI 批量合并/导出

![CLI Merge](mockups/09-cli-merge.png)

- 合并页保留源/目标双列表结构。
- 实验功能使用橙色风险提示。
- 底部进度区保持当前文件文字和进度条。

![CLI Export](mockups/10-cli-export.png)

- 批量导出页突出源目录、文件状态、导出配置和输出目录。
- 导出/单纹理图按钮区分主操作和特殊操作。

![CLI Animation Select](mockups/11-cli-animation-select.png)

- 动画选择弹窗提供搜索和勾选列表。
- 后续实现时可复用通用列表选择弹窗样式。

## 批量重命名 / Spine 整理 / 图集解包

![Rename Tool Rename](mockups/12-rename-tool-rename.png)

- 重命名页从旧固定坐标改为文件列表 + 命名规则双区。
- 选择文件/文件夹/清空集中在底部工具栏。

![Rename Tool Organize](mockups/13-rename-tool-organize.png)

- Spine 文件整理页将说明文字收纳为配置区。
- 源文件夹、保存位置、后缀处理、开始整理形成明确流程。

![Rename Tool Unpack](mockups/14-rename-tool-unpack.png)

- 图集解包页使用大列表 + 底部操作区。
- 进度区位于操作区下方，不遮挡按钮。

## 通用弹窗与选择

![Input Dialog](mockups/15-input-dialog.png)

- 通用输入弹窗使用暗色输入框和明确说明。
- 适用于按钮改名、间距、循环延迟等输入场景。

![Subfolder Select](mockups/16-subfolder-select.png)

- 子文件夹/文件选择弹窗保留搜索、不包含、全选、全不选。
- 列表选中态与全局表格样式一致。

![Core Dialogs And Menus](mockups/17-core-dialogs-and-menus.png)

- 删除确认、导入确认、操作完成弹窗统一深灰样式。
- 托盘菜单、普通工具菜单、VK 按钮右键菜单、VK 空白右键菜单统一深灰圆角菜单样式。
- VK 菜单中的修改按钮名称弹窗复用通用输入弹窗风格。
- 危险项使用红色文字或按钮。

## 下一步建议

1. 先由用户确认整体风格是否符合预期。
2. 如果风格方向确认，再拆成实现阶段：公共主题系统、主窗口/编辑器、批量工具、VK 皮肤、核心弹窗。
3. 每个实现阶段都需要截图验证，确认文字不溢出、控件不遮挡、DPI 和窗口缩放可用。
