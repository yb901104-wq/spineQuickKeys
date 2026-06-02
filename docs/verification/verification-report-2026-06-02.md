# V2.79 验证报告（阶段记录）

验证时间：2026-06-02

## 验证边界

- 本阶段先执行构建、静态代码落点核对、发布包存在性检查、日志/配置路径检查。
- 用户交还前台控制后，补充执行了主要窗口入口的前台截图验证。
- 需要真实 Spine TXT、`.kmp`、Spine CLI 项目或人工窗口操作的用例暂缓。

## 已完成验证

| 项目 | 结果 | 证据 |
| --- | --- | --- |
| Git 提交 | 通过 | 最新提交包含 `031b531 2.78 提交验证初始截图`，此前已有 `95cf5d1 2.78 修复审核台账问题并完善验证机制`。 |
| 构建 | 通过 | `dotnet build KeyMacro.sln` 在本机环境通过，0 警告，0 错误。 |
| 发布包 | 通过 | `KeyMacro/bin/Release/publish/KeyMacro.exe` 存在，大小约 113 MB。 |
| 版本号 | 通过 | `MainForm.cs` 标题已更新为 `V2.79`，`AGENTS.md` 已记录 V2.79。 |
| audit 状态 | 通过 | `AUD-001` 到 `AUD-024`、`AUD-027` 标为已完成，`AUD-025` 为设计保留，`AUD-026` 为暂不处理。 |
| 主窗口入口 | 通过（截图） | `docs/verification/screenshots/MF-000_main-cold-start.png`、`docs/verification/screenshots/MF-000_main-initial.png`。 |
| 序列编辑器入口 | 通过（截图） | `docs/verification/screenshots/SE-000_add-sequence-editor.png`，可见名称、热键、VK 绑定、步骤工具栏、确定/取消。 |
| Spine 热键编辑器入口 | 通过（截图） | `docs/verification/screenshots/SH-000_spine-hotkey-editor.png`，可见 TXT 路径、搜索、录制、表格、保存/取消。 |
| VK 管理器入口 | 通过（截图） | `docs/verification/screenshots/VM-000_vk-manager.png`，可见窗口列表、允许显示、显示/删除、新增窗口。 |
| 批量复制入口 | 通过（截图） | `docs/verification/screenshots/BC-000_batch-copy-window.png`，可见源文件列表、三段式目标路径、预览、开始复制。 |
| CLI 批量窗口入口 | 通过（截图） | `docs/verification/screenshots/CLI-000_batch-cli-window.png`，可见 Spine.com 路径、合并 Tab、取消 CLI、源/目标列表。 |
| AppData 配置优先 | 通过（静态） | `ConfigService.ResolveLoadPath()` 优先 AppData，保存固定写入 AppData。 |
| AppData VK 布局优先 | 通过（静态） | `VirtualLayoutSerializer.ResolveLoadPath()` 优先 AppData，保存固定写入 AppData。 |
| 目标软件进程名优先 | 通过（静态） | `HotkeyService.IsForegroundTarget()` 先匹配 `ProcessName`，再回退 `TargetAppPath`。 |
| Win 组合键 | 通过（静态） | `MacroPlayer.SendCombo()` 检测 Win 修饰键后走 native 发送路径。 |
| 强制停止释放按键 | 通过（静态） | `MacroPlayer.ForceStop()` 调用 `ReleaseAllPressedKeys()`。 |
| VK 菜单暂停循环 | 通过（静态） | `VirtualKeyWindow` 菜单 `Opened/Closed` 调用 `PauseForMenu/ResumeFromMenu`。 |
| 循环 active 复位 | 通过（静态） | `VirtualLoopExecutor.LoopEnded` 绑定到 `VirtualKeyWindow.OnLoopEnded`。 |
| 主列表立即保存 | 通过（静态） | `CurrentCellDirtyStateChanged` 和 `CellEndEdit` 调用 `CommitEdit`，变更后 `SaveAndRefresh()`。 |
| 复制序列清空 VK 绑定 | 通过（静态） | `DuplicateSequence()` 将 `TriggerVkButtonName` 设为空。 |
| 步骤复制事件单次绑定 | 通过（静态） | `SequenceEditor` 在初始化绑定 `StepsGrid_CellClick`，未在刷新中重复绑定。 |
| Spine 搜索后录制定位 | 通过（静态） | 录制逻辑按选中行名称定位条目，而非直接使用过滤后行号。 |
| Spine 注解独立文件 | 通过（静态） | `SpineHotkeyService.GetAnnotationPath()` 使用 `{hotkeyFilePath}.annotations.json`。 |
| Spine TXT 原文导入导出 | 通过（静态） | `DataBundle` 包含 `SpineHotkeyRawText`、`SpineHotkeyHash`、`SpineHotkeyNames`。 |
| VK 导入增量新增 | 通过（静态） | 导入时使用 `GetUniqueWindowName()`，新增导入窗口并仅创建新增窗口。 |
| CLI 异步取消 | 通过（静态） | `SpineCliService.RunAsync()` 使用 `WaitForExitAsync` 和 `CancellationToken`。 |
| CLI 参数安全 | 通过（静态） | `ProcessStartInfo.ArgumentList` 逐项添加参数。 |
| CLI 临时输出 | 通过（静态） | 合并/导出/打包使用临时路径，成功后移动，失败/取消清理。 |
| 批量复制冲突处理 | 通过（静态） | `BatchCopyService` 按文件跳过冲突，并支持 `ConflictAction.CancelAll`。 |
| 批量复制路径拼接 | 通过（静态） | 预览目标路径使用 `Path.Combine` 组合规范化路径段。 |
| AUD-027 批量处理进度显示 | 通过（截图） | `docs/verification/screenshots/AUD-027_cli-progress-layout.png`、`AUD-027_batch-copy-progress-layout.png`、`AUD-027_rename-tool-progress-layout.png`、`AUD-027_rename-tool-organize-progress-layout.png`、`AUD-027_rename-tool-unpack-progress-layout.png`：当前处理文件文字位于进度条上方，进度条中间序号可见，未遮挡原有 UI。 |
| Spine 热键 TXT 解析/保存注解 | 通过（服务） | `docs/verification/service-verification-2026-06-02.json`：解析 `370` 条、`17` 个分组，独立 annotations 文件创建成功，`OemPeriod/Oemcomma` 转 Spine 格式成功。 |
| `.kmp` 数据包读取 | 通过（服务） | `docs/verification/service-verification-2026-06-02.json`：`test.kmp` 载入成功，含 `3` 个序列、`1` 个 VK 窗口、`370` 条 Spine 热键，并包含 TXT 原文。 |
| 批量复制 targets1~targets5 | 通过（服务） | `docs/verification/service-verification-2026-06-02.json`：`2` 个源文件同步到 `5` 个目标目录，普通复制、跳过冲突、覆盖冲突、取消全部均通过，最终 targets1~targets5 均已同步。 |
| CLI 路径与项目识别 | 通过（CLI） | `docs/verification/cli-verification-2026-06-02.json`：`D:\Program Files\Spine\Spine.com` 有效；`G5.spine/json/skel` 和 `ribbon_test2.json` 均能读取版本/骨架/动画信息。 |
| CLI 导出 | 通过（CLI） | 使用 `finish.export.json` 导出 `G5.spine` 成功，输出 `docs/verification/cli-output/export-finish/G5.skel.bytes`。 |
| CLI JSON 导入为 Spine | 通过（CLI） | `G5.json` 导入到临时 `.spine` 成功，输出 `docs/verification/cli-output/import-json-to-spine/G5_from_json.spine`。 |
| CLI 安全副本合并 | 通过（CLI） | 将 `G5.json` 导入到 `G5.spine` 的副本成功，输出 `docs/verification/cli-output/merge-safe-copy/G5_target_copy.spine`，原素材未覆盖。 |
| CLI 单纹理打包 | 失败（临时发现） | `docs/verification/cli-verification-2026-06-02.json`：Spine.com 返回 `Input path for packing must be a folder`，已记录 TMP-20260602-002。 |

## 临时发现

| 编号 | 摘要 | 状态 |
| --- | --- | --- |
| TMP-20260602-001 | 托盘隐藏后，单实例唤醒仍可能因 `MainWindowHandle == 0` 失败。 | 已记录到 `docs/verification/findings.tmp.md`，待用户确认是否进入 audit。 |
| TMP-20260602-002 | CLI “单纹理图”传入 `.spine` 文件执行 pack，Spine.com 要求输入为文件夹。 | 已记录到 `docs/verification/findings.tmp.md`，待用户确认是否进入 audit。 |

## UI 自动化说明

主窗口和主要功能窗口均已取得截图。尝试进一步自动执行“填写名称、添加步骤、保存序列”等深层 UI 操作时，WinForms 模态窗口、`Process.MainWindowHandle` 状态以及高 DPI 下的坐标点击在自动化中表现不稳定，验证脚本无法稳定继续。该自动化不稳定不直接判定为功能失败；实际疑点已单独记录为 TMP-20260602-001。

## 已使用测试素材

| 类型 | 路径 | 验证结果 |
| --- | --- | --- |
| Spine 热键 TXT | `manual_test_assets/spine_hotkeys/hotkeys-1.txt` | 已用于服务层解析、保存注解、按键格式转换验证。 |
| KMP 数据包 | `manual_test_assets/kmp/test.kmp` | 已用于服务层读取验证。 |
| 批量复制源文件 | `manual_test_assets/batch_copy/source/201.png`、`manual_test_assets/batch_copy/source/zzps-skin.json` | 已复制到 `targets1` 至 `targets5`。 |
| 批量复制目标目录 | `manual_test_assets/batch_copy/targets1` 至 `targets5` | 已完成普通同步、跳过冲突、覆盖冲突、取消全部验证，并最终保持同步状态。 |
| CLI 项目 | `manual_test_assets/cli/projects/` | 已完成项目识别、导出、JSON 导入、安全副本合并验证；单纹理打包失败并记录临时发现。 |
| CLI 导出配置 | `manual_test_assets/cli/export_configs/finish.export.json` | 已用于 `G5.spine` 导出验证。 |

## 暂缓验证

| 范围 | 原因 | 后续方式 |
| --- | --- | --- |
| MainForm 添加/编辑/删除/复制/导入/导出深层操作 | UI 自动化未稳定完成；入口已截图 | 后续建议手工按 `manual-verification-plan.md` 执行，或补一个专用自动化测试工具。 |
| SequenceEditor 热键录入、步骤上下移、复制 | 需要键盘录制/表格编辑/模态窗口稳定控制；入口已截图 | 后续手工执行并截图。 |
| VirtualKeyWindow 右键菜单、循环按钮、强停 | 需要前台焦点、右键菜单和实际播放目标 | 后续手工执行并截图。 |
| Spine 热键 TXT UI 完整导入导出 | 服务层已验证 TXT 解析/注解；UI 弹窗流程尚未完整点击 | 后续手工验证保存对话框、导入推荐弹窗、重复项诊断弹窗。 |
| `.kmp` UI 往返导入导出 | 服务层已验证读取 `test.kmp`；UI 确认弹窗流程尚未完整点击 | 后续手工验证导入确认、VK 重名顺延可视结果。 |
| 批量复制 UI 与冲突弹窗 | 服务层已验证复制引擎；UI 冲突弹窗尚未逐项点击 | 后续手工验证 SourceFilePicker、ConflictDialog 截图。 |
| Spine CLI 取消 | 需要稳定长任务或专用可取消测试场景 | 当前 CLI 素材执行较快，尚未验证真实中途取消。 |
