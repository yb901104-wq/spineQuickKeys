# Round3 布局对齐截图对比

本文件记录 2026-06-04 这一轮“概览图 vs 运行时截图”的并排对比结果。目标是检查窗口默认尺寸、控件分区、列表/输入框/按钮/进度区的位置是否接近概览图；本轮不代表最终深灰圆角美术资源已经替换。

## 对比图索引

| 序号 | 界面 | 对比图 | 当前结论 |
| --- | --- | --- | --- |
| 01 | MainForm 主窗口 | `comparisons/round3/01-main-window-compare.png` | 功能入口、列表列和底部状态栏已对齐；状态栏显示就绪、配置来源、VK 窗口数、热键数。 |
| 02 | SequenceEditor 序列编辑器 | `comparisons/round3/02-sequence-editor-compare.png` | 已收窄触发快捷键输入区，步骤表格增加左右边距，底部按钮顺序已对齐为取消/确定。 |
| 03 | HotkeyRecorderForm | `comparisons/round3/03-hotkey-recorder-compare.png` | 已缩小为短时录制弹窗；确认/取消按钮因真实逻辑冲突暂不新增。 |
| 04 | SpineHotkeyEditor | `comparisons/round3/04-spine-hotkey-editor-compare.png` | TXT 文件区、搜索、录制、快捷键列表、保存/取消已对齐；截图样例补齐 4 行。 |
| 05 | VkWindowManager | `comparisons/round3/05-vk-manager-compare.png` | 窗口列表和底部新增/关闭已对齐；截图工具补齐 3 行样例，不新增保存布局/显示全部/隐藏全部。 |
| 06 | BatchCopyWindow | `comparisons/round3/06-batch-copy-compare.png` | 源文件、目标路径、预览、开始复制、进度区均已呈现；进度文字在进度条上方。 |
| 07 | SourceFilePicker | `comparisons/round3/07-source-file-picker-compare.png` | 缩略图浏览区、顶部路径、底部确认区已对齐；截图工具保留占位缩略图样例。 |
| 08 | ConflictDialog | `comparisons/round3/08-conflict-dialog-compare.png` | 已调整为中型冲突弹窗并完成深色主题化；保留打开文件夹/覆盖/跳过冲突/取消全部复制 4 个真实操作。 |
| 09 | BatchCli 合并页 | `comparisons/round3/09-cli-merge-compare.png` | 源/目标列表固定高度后更接近概览图；动画选择、from/to、实验合并勾选、执行和进度区均可见。 |
| 10 | BatchCli 导出页 | `comparisons/round3/10-cli-export-compare.png` | 文件列表固定高度后刷新配置、输出目录、导出/单纹理图、进度区均可见。 |
| 11 | CLI 动画选择弹窗 | `comparisons/round3/11-cli-animation-select-compare.png` | 真实代码为简单勾选列表，未新增概览图搜索框/表格列头；该差异记录为待确认。 |
| 12 | ReNameTool 重命名页 | `comparisons/round3/12-rename-tool-rename-compare.png` | 文件列表、命名规则区、路径输入和进度条已横向扩到概览图工作区，并完成低风险深色主题化。 |
| 13 | ReNameTool 整理页 | `comparisons/round3/13-rename-tool-organize-compare.png` | 待整理列表、整理配置和进度条已横向扩到概览图工作区，按钮已分开，并完成低风险深色主题化。 |
| 14 | ReNameTool 解包页 | `comparisons/round3/14-rename-tool-unpack-compare.png` | Atlas 列表、解包操作区和进度条已横向扩到概览图工作区，操作控件完整可见，并完成低风险深色主题化。 |
| 15 | InputDialog | `comparisons/round3/15-input-dialog-compare.png` | 输入框和确认/取消可见；用于 VK 修改按钮名称等场景。 |
| 16 | SubfolderSelectDialog | `comparisons/round3/16-subfolder-select-compare.png` | 搜索、不包含、全选/全不选、勾选列表、确认/取消均在框内。 |
| 17 | VK 按钮右键菜单 | `comparisons/round3/17-vk-button-menu-compare.png` | VK 本体不改；右键菜单样式和功能项截图保留。 |

## 当前保留差异

- 所有运行时截图仍使用 WinForms 原生标题栏和浅色控件；深灰圆角主题、按钮双态图和自绘标题栏属于后续资源替换阶段。
- `MainForm` 底部状态栏已在运行截图中可见，不再作为待确认新增项。
- `ReNameTool` 仍基于 Designer 固定坐标，已做大尺寸布局修正，但后续如果要完全贴近概览图，建议拆成 TableLayoutPanel/Panel 分区重构。
- 虚拟按键窗口本体仍不纳入普通 UI 重构，只保留 VK 右键菜单、托盘菜单和相关输入弹窗截图。

## 下一轮建议

1. 先由用户按对比图确认哪些界面布局仍需继续靠近概览图。
2. 若确认布局方向，下一步再进入“统一深灰主题资源替换”。
3. 若某个界面存在功能缺失或多余入口，先记录到 `docs/ui-refactor/per-interface-adjustment.md`，再决定是否改代码。

## Round3 微调补充（继续阶段）

- 已根据差异清单处理 SequenceEditor、SpineHotkeyEditor、BatchCopyWindow、BatchCliWindow、SourceFilePicker 和 ReNameTool 的布局微调。
- `SourceFilePicker` 截图工具已加入占位缩略图，后续能直接检查缩略图列表间距和勾选状态。
- `BatchCliWindow` 合并页/导出页已增加任务板块标题，导出配置行已加高。
- `ReNameTool` 三个页签已补充板块标题背景；由于仍是 Designer 固定坐标，后续如需完全贴近概览图，建议单独重构为布局容器。
- `ReNameTool` 整理页已追加修复“保存位置 / 清空列表”按钮重叠，最新 `13-rename-tool-organize-compare.png` 已刷新。
- `ReNameTool` 三页已追加横向工作区扩展：列表/配置标题/进度条从 760px 级别扩到 1320px 级别，更接近概览图的宽屏结构。
- `SequenceEditor` 底部确认区已追加修正按钮视觉顺序，与概览图保持“取消 / 确定”。
- `ReNameTool` 三页已作为阶段 5 的低风险试点完成深色主题化：仅调整颜色、页签自绘、输入框/列表/按钮/进度区视觉，不改变功能和事件。
