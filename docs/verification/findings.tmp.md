# 临时验证发现记录

此文件只记录验证过程中发现的疑似问题。记录后先在对话中确认，再决定是否追加到 `docs/audit.md`。不要直接把临时发现当作正式待修问题。

## 记录模板

```text
发现编号：TMP-YYYYMMDD-001
关联用例：
发现时间：
界面/模块：
操作步骤：
预期结果：
实际结果：
截图路径：
日志路径：
初步判断：
建议是否录入 audit：
用户确认结果：
```

## 临时发现列表

### TMP-20260602-001

发现编号：TMP-20260602-001
关联用例：MF-014 单实例唤醒 / MF-012 托盘行为
发现时间：2026-06-02
界面/模块：程序启动 / 单实例 / 托盘隐藏
操作步骤：静态检查 `Program.FindExistingMainWindow()`；该逻辑只从同名进程读取 `Process.MainWindowHandle`，拿到非零句柄后才执行 `ShowWindow`/`SetForegroundWindow`。
预期结果：主窗口关闭隐藏到系统托盘后，再次启动 exe 应能唤醒已有主窗口。
实际结果：WinForms 主窗口被 `Hide()` 隐藏到托盘后，`Process.MainWindowHandle` 可能为 0；当前单实例路径没有备用 IPC、消息窗口或 NotifyIcon 回调，因此第二次启动可能只退出，不唤醒主窗口。
截图路径：`docs/verification/screenshots/MF-000_main-cold-start.png`、`docs/verification/screenshots/MF-000_main-initial.png`
日志路径：`%APPDATA%\KeyMacro\logs\2026-06-02.log`
初步判断：AUD-001 的“普通可见窗口冷启动/可见态”场景可工作；验证期间多次观测到 KeyMacro 进程仍响应但 `MainWindowHandle == 0`，而单实例唤醒正依赖该值，因此“托盘隐藏后唤醒”目标疑似未完全修复。
建议是否录入 audit：建议用户确认后录入新的问题，或重新打开 AUD-001。
用户确认结果：待确认。

### TMP-20260602-003

发现编号：TMP-20260602-003
关联用例：BC-011 正常复制 / BC-015 复制中取消
发现时间：2026-06-02
界面/模块：`BatchCopyWindow` / 批量复制
操作步骤：检查批量复制窗口 UI 与复制进度更新逻辑。
预期结果：批量复制应有进度条，进度条中间显示当前处理文件/目标。
实际结果：批量复制没有 `ProgressBar` 控件；复制中只在底部 `_lblStatus` 显示 `复制中: 文件名 → 目标目录 (done/total)`，不是进度条中间文字。
截图路径：`docs/verification/screenshots/BC-000_batch-copy-window.png`
日志路径：无
初步判断：复制引擎有进度文本事件，但 UI 缺少进度条和居中文字覆盖层。
建议是否录入 audit：建议用户确认后追加为批量复制进度显示问题。
用户确认结果：已确认，合并录入 `docs/audit.md` 的 AUD-027 并直接修复。

### TMP-20260602-004

发现编号：TMP-20260602-004
关联用例：CLI-007 实验合并 / CLI-006 普通合并 / CLI-011 批量导出 / CLI-012 单纹理打包
发现时间：2026-06-02
界面/模块：`BatchCliWindow` / CLI 批量合并导出
操作步骤：检查 CLI 窗口进度条创建与调用位置；用户执行真实“实验合并”后反馈进度条可见，但进度条上方没有显示当前进度文字。
预期结果：普通合并、实验合并、批量导出、单纹理打包均有进度条，进度条中间显示当前处理文件。
实际结果：CLI 窗口确实有 `ProgressBar` 和居中覆盖 `_lblProgress`；但用户真实执行实验合并时只看到进度条，没有看到当前处理文件/进度文字。普通合并、批量导出、单纹理打包也没有调用 `ShowProgress/SetProgress`。此前模拟调用 `ShowProgress(true)` + `SetProgress(2, 5, "[2/5] G5_target_copy.spine")` 的截图也显示：底部进度条区域出现，但居中文件文字不可见。
截图路径：`docs/verification/screenshots/CLI-000_batch-cli-window.png`、`docs/verification/screenshots/CLI-EXP-PROGRESS-visual.png`
日志路径：无
初步判断：CLI 进度条组件存在，但覆盖范围不完整；实验合并的文字不可见可能是原生 WinForms `ProgressBar` 作为独立 HWND 覆盖了同 Panel 内的 Label，或 z-order/透明背景导致 Label 没有真正绘制到进度条上方。建议改为自绘进度条控件，或用单独 Label 放在进度条上方/下方而不是覆盖原生 ProgressBar。
建议是否录入 audit：建议用户确认后追加为 CLI 进度显示覆盖不完整问题。
用户确认结果：已确认，合并录入 `docs/audit.md` 的 AUD-027 并直接修复。

### TMP-20260602-005

发现编号：TMP-20260602-005
关联用例：RN-003 批量重命名 / RN-005 atlas 整理 / 图集自动解包
发现时间：2026-06-02
界面/模块：`ReNameTool.Form1` / 批量重命名、SPINE 文件整理、SPINE 图集自动解包
操作步骤：检查 ReNameTool 设计器控件和执行循环。
预期结果：批量重命名/整理/解包模块应有进度条，进度条中间显示当前处理文件。
实际结果：ReNameTool 没有 `ProgressBar` 控件；执行过程主要是同步循环和完成/错误 `MessageBox`，没有进度条，也没有当前处理文件居中文字。
截图路径：暂无
日志路径：无
初步判断：该模块整体缺少进度显示机制。
建议是否录入 audit：建议用户确认后追加为 ReNameTool 进度显示缺失问题。
用户确认结果：已确认，合并录入 `docs/audit.md` 的 AUD-027 并直接修复。

### TMP-20260602-002

发现编号：TMP-20260602-002
关联用例：CLI-012 单纹理打包 / CLI 批量合并导出窗口
发现时间：2026-06-02
界面/模块：`BatchCliWindow` / `SpineCliService.Pack`
操作步骤：使用 `manual_test_assets/cli/projects/G5.spine` 执行 `SpineCliService.Pack(project, outputDir, packName)`。
预期结果：点击“单纹理图”后能够完成纹理打包，输出 atlas/png 或等价打包结果。
实际结果：Spine.com 返回失败：`ERROR: Input path for packing must be a folder`，因为当前 Pack 调用把 `.spine` 文件作为 `-i` 输入传给 `-p` 打包命令。
截图路径：`docs/verification/screenshots/CLI-000_batch-cli-window.png`
日志路径：`docs/verification/cli-verification-2026-06-02.json`
初步判断：当前“单纹理图”功能的 CLI 参数/输入对象可能不符合 Spine CLI 打包要求；应改为选择/传入图片文件夹或导出后的图片目录，再执行 texture pack。
建议是否录入 audit：建议用户确认后追加为新的 CLI 打包问题。
用户确认结果：待确认。
