# 代码审核问题台账

本文档是 `KeyMacro` 项目的代码审核与修复台账。后续修复任何问题前，先在此表更新对应条目的处理状态与本次目标，再进行代码修改；修复完成后回填验证结果。

## 状态说明

| 状态 | 含义 |
| --- | --- |
| 待处理 | 已确认要修，但尚未实施。 |
| 设计保留 | 确认暂不改代码，但需要文档或说明对齐。 |
| 暂不处理 | 明确不修。 |
| 待诊断 | 需要先加检测或收集证据。 |
| 处理中 | 开始修改前先改为此状态。 |
| 已完成 | 代码修完并验证后改为此状态。 |

## 总览表

| 序列 | 问题简介 | 指向功能 | 紧急程度 | 处理状态 |
| --- | --- | --- | --- | --- |
| AUD-001 | 单实例重复启动无法唤醒主窗口 | 程序启动 / 单实例 | 高 | 已完成 |
| AUD-002 | AppData 配置优先级不正确 | 宏序列配置读写 | 高 | 已完成 |
| AUD-003 | AppData 虚拟布局优先级不正确 | 虚拟按键布局读写 | 高 | 已完成 |
| AUD-004 | 目标软件绑定应以进程名为主 | 序列目标软件限定 | 中 | 已完成 |
| AUD-005 | Win 组合键被误发成 Ctrl 组合键 | 宏步骤组合键播放 | 高 | 已完成 |
| AUD-006 | 强制停止时需释放按键/修饰键 | 宏播放停止 / 长按模式 | 高 | 已完成 |
| AUD-007 | 右键菜单期间暂停虚拟按钮循环 | 虚拟按键循环按钮 | 高 | 已完成 |
| AUD-008 | 循环按钮 active 状态需跟随真实循环 | 虚拟按键按钮状态 | 中 | 已完成 |
| AUD-009 | 主列表编辑需立即保存同步 | 主窗口序列列表 | 中 | 已完成 |
| AUD-010 | 复制序列需清空虚拟按键绑定 | 主窗口复制序列 | 中 | 已完成 |
| AUD-011 | 序列编辑器复制步骤事件重复绑定 | 序列编辑器步骤复制 | 高 | 已完成 |
| AUD-012 | Spine 搜索后录制按键需按名称定位 | Spine 热键编辑器录制 | 高 | 已完成 |
| AUD-013 | Spine 中文说明改为独立注解文件 | Spine 热键中文说明 | 中 | 已完成 |
| AUD-014 | Spine 热键导入导出支持 TXT 原文与版本/结构判断 | `.kmp` Spine 热键导入导出 | 高 | 已完成 |
| AUD-015 | Spine 热键导入重复/近似重复项诊断并跳过风险项 | `.kmp` Spine 热键导入 | 高 | 已完成 |
| AUD-016 | VK 布局导入增量新增，重名自动顺延 | `.kmp` VK 布局导入 | 中 | 已完成 |
| AUD-017 | VK 导入后只显示新增窗口，不关闭旧窗口 | `.kmp` VK 布局导入刷新 | 中 | 已完成 |
| AUD-018 | VK 右键设置切换后立即保存 | 虚拟按键窗口右键设置 | 中 | 已完成 |
| AUD-019 | CLI 批量任务异步可取消 | CLI 批量合并 / 导出 | 高 | 已完成 |
| AUD-020 | CLI 使用临时输出，取消/失败清理半成品 | CLI 批量任务文件输出 | 高 | 已完成 |
| AUD-021 | CLI 参数改用 ArgumentList | CLI 命令参数构造 | 中 | 已完成 |
| AUD-022 | 批量复制跳过冲突文件而非整个目录 | 批量复制冲突处理 | 中 | 已完成 |
| AUD-023 | 批量复制冲突弹窗增加取消全部复制 | 批量复制取消流程 | 中 | 已完成 |
| AUD-024 | 批量复制路径拼接改为安全 Path.Combine | 批量复制三段式路径 | 低 | 已完成 |
| AUD-025 | 虚拟按键目标窗口播放方案暂保持激活窗口优先 | 虚拟按键目标窗口播放 | 中 | 设计保留 |
| AUD-026 | Spine TXT 保存备份/编码保留暂不处理 | Spine 热键 TXT 保存 | 低 | 暂不处理 |
| AUD-027 | 批量文件处理进度文字显示缺失或被遮挡 | CLI 批量合并/导出、批量复制、批量重命名/整理/解包 | 高 | 已完成 |
| AUD-028 | Spine 注解格式与空键删除逻辑未按已确认规范落地 | Spine 热键编辑/保存 | 中 | 已完成 |
| AUD-029 | 虚拟按钮左键点击与按钮拖动排序语义冲突 | 虚拟按键窗口交互 | 高 | 已完成 |
| AUD-030 | 虚拟按键窗口缺少按目标进程前台状态自动联动 | 虚拟按键目标窗口显示 | 高 | 已完成 |
| AUD-031 | 边缘吸附、自动隐藏与窗口锁定/菜单/TopMost 交互未统一 | 虚拟按键窗口行为 | 中 | 已完成 |
| AUD-032 | 目标进程自动联动使用激活式显示隐藏，导致前台振荡和误触发按键 | 虚拟按键目标窗口显示 | 高 | 待验证 |
| AUD-033 | V2.87 使用原生 Win32 显示路径后未同步 WinForms 首次显示状态，导致虚拟窗口不可见且管理器显示/隐藏无效 | 虚拟按键窗口生命周期 | 高 | 处理中 |
| AUD-034 | 虚拟按键窗口受固定横竖模式和固定缩放档位限制，无法独立调整窗口尺寸与内部布局 | 虚拟按键窗口布局 | 高 | 处理中 |
| AUD-035 | 新旧按钮缩放入口同时存在并共享运行时状态，导致缩放范围和配置字段语义冲突 | 虚拟按键窗口布局 | 中 | 处理中 |
| AUD-036 | 虚拟按键右键菜单背景与文字/选中状态对比不足，影响菜单可读性 | 虚拟按键窗口交互 | 中 | 处理中 |
| AUD-037 | 批量复制内层双 Tab 合并为单页（选源结构统一 + 原地复制独立开关） | 批量复制 / 原地复制备份 | 中 | 已完成 |
| AUD-038 | 批量复制并入 CLI 窗口子页 + 源文件批量动画选择（并集弹窗 / 交集写入 / 缺项标红） | 首页标签结构 / 批量复制源列表 / CLI 窗口 | 中 | 已完成 |
| AUD-039 | 备份后缀 `-n1` 触发静默排除，真实源文件 `*-n1.spine` 在列表里检索不到 | 批量复制源文件列表 / 显示层过滤 | 高 | 已完成 |
| AUD-040 | 导出页标签改名 + 导出预设（本地持久化） + 批量生成导出配置（4.3+） + export.json 状态列不同步 | CLI 批量导出页 / 本地软件数据 | 中 | 已完成 |

## 问题详情

## 本轮修复记录

- 版本：V2.89（施工中）
- 范围：AUD-034
- 代码变更：虚拟按键窗口新增右下角自由缩放、窗口尺寸锁定、自适应网格/单行/单列/固定行列布局、连续按钮比例、统一四边内边距和按钮间距；旧 `VerticalMode` / `ScaleFactor` 配置继续兼容。
- 当前验证：Debug 构建已通过，0 警告，0 错误；尚未完成真实窗口操作验证，AUD-034 仍保持“处理中”。

- 版本：V2.89 修复补丁（施工中）
- 范围：AUD-035、AUD-036
- 代码变更：移除独立“旧版缩放”右键菜单，运行时统一使用 `ButtonScale`；保留 `ScaleFactor` 仅用于旧配置迁移和兼容镜像。右键菜单改用更深的菜单背景、更亮的正文文字、更明显的悬停色、危险项悬停色和左侧强调条。
- 当前验证：待构建和发布后进行菜单可读性、旧配置加载、新按钮比例保存及重启恢复验证。

- 版本：V2.87
- 范围：AUD-032
- 代码变更：目标进程联动改为不激活窗口的 Win32 显示/隐藏路径；增加行为可见状态，避免 Show/Hide/TopMost 反复触发前台事件形成振荡；目标联动不再把虚拟窗口切换带来的前台变化误当作目标离开。
- 代码验证：`dotnet build KeyMacro.sln --no-restore` 已通过，0 警告，0 错误；`dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true` 已通过，生成 `KeyMacro/bin/Release/publish-v2.87/KeyMacro.exe`。
- 后续修复：V2.87 现场发现 Win32 原生可见状态与 WinForms `Visible` 状态不一致，管理器查询和自动行为误判窗口状态，新增 AUD-033；改用 `IsWindowVisible` 作为统一可见性判断，并在首次显示后启动行为监听。
- 手工验证：待重新执行目标进程自动显示/隐藏、前台不抢占与 Alt 误触发场景；新增 `VK-044` 至 `VK-046`。

- 版本：V2.86
- 范围：AUD-029 至 AUD-031
- 代码变更：虚拟按钮左键仅播放；中键可在锁定/无标题栏状态下拖动窗口；右键菜单提供按钮排序；按目标进程名监听前台变化并自动显示/隐藏；增加 WorkingArea 边缘吸附和默认关闭的边缘自动隐藏；窗口实例查找改用 WindowData.Name。
- 代码验证：`dotnet build KeyMacro.sln --no-restore` 已通过，0 警告，0 错误。
- 手工验证：待按 `VK-026` 至 `VK-043`、`SV-009` 至 `SV-012` 执行并截图。

- 版本：V2.85
- 范围：AUD-028
- 代码变更：Spine 注解保存统一输出 `[{"Name":"...","Note":"..."}]` 数组格式，读取兼容旧字典格式；清空快捷键时不再写入空键 TXT 行，但继续保留非空中文注解。
- 代码验证：`dotnet build KeyMacro.sln --no-restore` 已通过，0 警告，0 错误。
- 手工验证：尚未执行；后续按 `SH-004`、`SH-005`、`SH-009` 与 `IE-005` 补充截图验证。

- 版本：V2.79
- 范围：AUD-027
- 代码验证：`dotnet build KeyMacro.sln` 已通过，0 警告，0 错误。
- 截图验证：已生成 `docs/verification/screenshots/AUD-027_cli-progress-layout.png`、`AUD-027_batch-copy-progress-layout.png`、`AUD-027_rename-tool-progress-layout.png`、`AUD-027_rename-tool-organize-progress-layout.png`、`AUD-027_rename-tool-unpack-progress-layout.png`。
- 发布验证：`dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishDir="bin/Release/publish" KeyMacro\KeyMacro.csproj` 已通过。

- 版本：V2.78
- 范围：AUD-001 至 AUD-024
- 代码验证：`dotnet build KeyMacro.sln` 已通过。
- 手工验证：尚未执行截图验证；后续按 `docs/verification/manual-verification-plan.md` 逐项执行，发现问题先写入 `docs/verification/findings.tmp.md`。

### AUD-001 单实例重复启动无法唤醒主窗口

- 现状：程序使用全局 Mutex 限制单实例；第二次启动时通过固定窗口标题查找已有主窗口，但标题仍写死为旧版本 `V2.13`。
- 目标：重复启动时保持只运行一个进程，并唤醒当前运行中的主窗口。
- 建议处理：改为不依赖版本号的窗口查找/唤醒方式，或使用稳定窗口标题/消息机制。
- 验证方式：运行程序后再次启动 exe，确认已有主窗口显示到前台。
- 备注：已确认用户期望为“唤醒主窗口”。

### AUD-002 AppData 配置优先级不正确

- 现状：`config.json` 加载时优先项目目录，保存时写入 AppData，可能出现保存后下次仍读取项目旧配置。
- 目标：成品软件以 `%APPDATA%\KeyMacro\config.json` 作为用户配置主来源。
- 建议处理：运行时优先读写 AppData；项目配置仅作为首次默认或开发回退。
- 验证方式：存在项目配置与 AppData 配置时，确认加载 AppData；保存后重启仍读取用户配置。
- 备注：已确认采用 AppData 用户配置优先。

### AUD-003 AppData 虚拟布局优先级不正确

- 现状：`virtual_layout.json` 加载时可能优先项目目录，保存时可能写回项目布局，污染仓库默认资源。
- 目标：运行时以 `%APPDATA%\KeyMacro\virtual_layout.json` 作为用户布局主来源。
- 建议处理：AppData 布局优先；项目/嵌入式布局仅作为首次默认模板。
- 验证方式：调整 VK 布局后重启，确认从 AppData 恢复；项目布局文件不被运行时改写。
- 备注：已确认采用用户布局优先。

### AUD-004 目标软件绑定应以进程名为主

- 现状：序列目标软件限定主要依赖完整 exe 路径匹配，换电脑、换安装目录或换 Spine 版本后容易失效。
- 目标：目标绑定针对应用本身，而不是某台电脑上的具体安装路径。
- 建议处理：新增/使用进程名作为主要匹配字段，旧路径作为兼容回退和显示参考。
- 验证方式：绑定 Spine 后更换安装路径或导入到另一台电脑，前台进程名一致时热键仍生效。
- 备注：已确认用户期望不受软件版本、项目名和安装路径影响。

### AUD-005 Win 组合键被误发成 Ctrl 组合键

- 现状：宏播放中 `SendCombo` 将 `win` 映射为 SendKeys 的 `^`，实际表示 Ctrl。
- 目标：宏步骤中的 `Win+某键` 应真实按下 Windows 键。
- 建议处理：含 Win 的组合键改用底层按键发送路径，并用 `try/finally` 确保释放修饰键。
- 验证方式：创建 `Win+E` 或 `Win+R` 步骤，确认执行真实 Windows 快捷键而不是 Ctrl 快捷键。
- 备注：已确认按建议修复。

### AUD-006 强制停止时需释放按键/修饰键

- 现状：长按或组合键播放期间取消 `Task.Delay` 后，后续 KEYUP 可能不执行，存在按键残留风险。
- 目标：右键强制停止应立即取消当前播放，并释放所有可能已按下的按键/修饰键。
- 建议处理：保留普通停止的温和语义；强制停止路径增加释放保障。
- 验证方式：播放长按步骤时强制停止，确认目标软件和系统不残留按键按下状态。
- 备注：普通二次点击停止仍应等待当前轮结束。

### AUD-007 右键菜单期间暂停虚拟按钮循环

- 现状：循环执行时打开按钮右键菜单会让前台焦点变为虚拟按键窗口/菜单，下一轮 SendKeys 可能发到错误目标。
- 目标：右键菜单打开期间不进入下一轮循环，避免快捷键打断菜单或发错窗口。
- 建议处理：菜单打开时进入临时暂停状态；菜单关闭且未强停时恢复；菜单中强制停止则立即取消并释放按键。
- 验证方式：循环运行中打开右键菜单，确认菜单期间不继续发下一轮；关闭菜单后可恢复；强停能立即停止。
- 备注：已确认此项可实施。

### AUD-008 循环按钮 active 状态需跟随真实循环

- 现状：循环按钮设置 active 后，循环完成或取消时可能没有复位。
- 目标：按钮高亮状态反映真实循环生命周期。
- 建议处理：循环开始时高亮；温和停止等待当前轮时保持高亮；完全停止或强停后取消高亮。
- 验证方式：测试循环自然结束、二次点击停止、右键强停，确认 active 状态正确复位。
- 备注：已确认采用“跟随真实循环”。

### AUD-009 主列表编辑需立即保存同步

- 现状：主窗口序列列表中的启用、循环间隔、循环次数可能需要离开单元格才触发保存。
- 目标：用户编辑后立即保存配置并同步热键注册状态。
- 建议处理：复选框和可编辑单元格提交后立即 `CommitEdit`/保存/刷新注册。
- 验证方式：点击启用复选框后不切换单元格，立即测试热键注册状态。
- 备注：已确认采用立即保存。

### AUD-010 复制序列需清空虚拟按键绑定

- 现状：复制序列时清空键盘热键，但保留虚拟按键绑定名，可能让副本抢占同一按钮绑定。
- 目标：复制出的序列默认不绑定任何触发源。
- 建议处理：复制时同时清空 `TriggerHotkey` 和 `TriggerVkButtonName`。
- 验证方式：复制已有虚拟按键绑定的序列，确认副本触发快捷键为空且不绑定 VK 按钮。
- 备注：已确认清空绑定。

### AUD-011 序列编辑器复制步骤事件重复绑定

- 现状：`RefreshSteps()` 每次执行都会绑定一次步骤表 `CellClick` 事件，刷新多次后点一次复制可能插入多行。
- 目标：步骤行末尾“复制”按钮点击一次只复制一行。
- 建议处理：将事件绑定移到初始化阶段，或绑定前先解绑。
- 验证方式：多次上移/下移步骤后点击复制，确认只插入一条重复步骤。
- 备注：已确认保持原功能，仅修重复触发。

### AUD-012 Spine 搜索后录制按键需按名称定位

- 现状：Spine 热键编辑器搜索过滤后，录制按键逻辑使用表格行号对应原始 `_entries`，可能错判当前行。
- 目标：无论是否搜索过滤，录制都作用于当前选中的快捷键名称。
- 建议处理：从选中行读取名称，再在 `_entries` 中按名称查找对应 entry。
- 验证方式：搜索后选中某快捷键并录制，确认只更新该名称对应条目。
- 备注：已确认按建议修复。

### AUD-013 Spine 中文说明改为独立注解文件

- 现状：文档约定为每个 Spine TXT 独立注解文件，但代码使用全局 `spine_translations.txt`。
- 目标：每个 Spine 热键 TXT 使用独立中文说明文件，避免不同配置串备注。
- 建议处理：改为 `{热键文件名}.annotations.json` 或等价独立文件；旧全局说明作为迁移/回退来源。
- 验证方式：加载两份不同 Spine TXT，分别编辑中文说明，确认互不影响。
- 备注：已确认采用独立注解文件。

### AUD-014 Spine 热键导入导出支持 TXT 原文与版本/结构判断

- 现状：`.kmp` 导入导出主要依赖解析后的热键列表，可能因解析/重写导致重复、丢格式或跨版本问题。
- 目标：导出保留 TXT 原文；导入时根据版本/结构推荐完整覆盖恢复或按名称合并迁移。
- 建议处理：导出记录 TXT 原文、解析列表、哈希、条目集合和可检测版本；导入时比较目标结构并推荐模式。
- 验证方式：同版本导入推荐完整覆盖；不同结构/版本导入推荐按名称合并；用户仍可选择另一模式。
- 备注：已确认根据版本/结构决定覆盖或合并迁移。

### AUD-015 Spine 热键导入重复/近似重复项诊断并跳过风险项

- 现状：历史上可能固定几个 Spine 热键项在导入后出现重复或近似重复。
- 目标：导入前检测重复/近似重复名称，风险项不导入，正常项继续。
- 建议处理：扫描导入包和目标 TXT 的规范化名称，弹窗和日志列出风险项，并跳过这些条目。
- 验证方式：构造重复/近似重复条目，确认风险项跳过，其他热键仍导入。
- 备注：已确认跳过风险项，不自动删行。

### AUD-016 VK 布局导入增量新增，重名自动顺延

- 现状：VK 布局导入遇到重名时询问覆盖，存在影响本机已有布局的风险。
- 目标：导入行为默认在本机布局外新增，不覆盖已有窗口。
- 建议处理：重名时自动顺延命名，如 `窗口1_导入1`、`窗口1_导入2`。
- 验证方式：导入包含同名窗口的 `.kmp`，确认本机原窗口保留，导入窗口以新名称新增。
- 备注：已确认自动顺延新增。

### AUD-017 VK 导入后只显示新增窗口，不关闭旧窗口

- 现状：导入 VK 布局后可能关闭并重开所有 VK 窗口，打断本机已有窗口状态。
- 目标：导入后只创建/显示新增窗口，未受影响的本机窗口不关闭、不闪烁。
- 建议处理：记录本次新增窗口，只对新增窗口创建实例；不要全量关闭 `_vkWindows`。
- 验证方式：打开本机 VK 窗口后导入新窗口，确认旧窗口保持原状态，新窗口显示。
- 备注：已确认只显示新增。

### AUD-018 VK 右键设置切换后立即保存

- 现状：置顶、按钮位置锁定等右键设置只修改内存，异常退出或未触发关闭保存时可能丢失。
- 目标：用户切换设置后立即持久化。
- 建议处理：相关菜单项状态改变后调用 `SaveLayout()`。
- 验证方式：切换置顶/锁定后直接重启程序，确认设置保留。
- 备注：已确认立即保存。

### AUD-019 CLI 批量任务异步可取消

- 现状：`SpineCliService.RunAsync()` 内部使用同步 `WaitForExit()`，执行 Spine.com 时 UI 可能卡住。
- 目标：CLI 批量合并/导出期间 UI 保持响应，并支持用户取消。
- 建议处理：改用 `WaitForExitAsync` 和 `CancellationToken`；窗口增加取消入口。
- 验证方式：执行长时间 CLI 任务时拖动窗口/点击取消，确认 UI 响应且可停止后续任务。
- 备注：已确认异步加取消按钮。

### AUD-020 CLI 使用临时输出，取消/失败清理半成品

- 现状：CLI 合并/导出可能直接创建最终文件，取消或失败时留下半成品。
- 目标：取消/失败不污染最终输出，已完成任务保留。
- 建议处理：当前任务输出到临时目录/临时文件，成功后提交到最终位置；取消或失败清理当前半成品。
- 验证方式：中途取消 CLI 任务，确认当前临时文件被清理，已成功提交的文件保留，未开始任务不执行。
- 备注：已确认此策略。

### AUD-021 CLI 参数改用 ArgumentList

- 现状：CLI 参数通过字符串拼接，路径或动画名含特殊字符时可能被错误解析。
- 目标：CLI 参数逐项传递，减少引号/空格/特殊字符导致的执行错误。
- 建议处理：作为 CLI 重构一部分，将 `ProcessStartInfo` 改用 `ArgumentList`。
- 验证方式：使用带空格和特殊字符的路径/动画名执行导出或合并，确认 Spine.com 参数正确。
- 备注：已确认作为 CLI 重构的一部分实施。

### AUD-022 批量复制跳过冲突文件而非整个目录

- 现状：目标目录存在任意同名冲突时，选择跳过会跳过整个目标目录，非冲突文件也不复制。
- 目标：“跳过”只跳过冲突文件，非冲突文件继续复制。
- 建议处理：复制服务按文件级别判断冲突；Skip 仅跳过冲突文件。
- 验证方式：目标目录只有一个文件冲突时选择跳过，确认其他源文件仍复制到该目标目录。
- 备注：已确认跳过冲突文件。

### AUD-023 批量复制冲突弹窗增加取消全部复制

- 现状：冲突弹窗打开时外部取消不立即生效，用户必须先处理弹窗。
- 目标：冲突处理阶段可明确取消整个复制任务。
- 建议处理：冲突弹窗增加“取消全部复制”；复制服务支持 `CancelAll` 动作并停止后续任务。
- 验证方式：复制遇到冲突时点击取消全部复制，确认弹窗关闭且后续目标不再复制。
- 备注：已确认增加取消全部复制。

### AUD-024 批量复制路径拼接改为安全 Path.Combine

- 现状：三段式目标路径使用字符串拼接 `/`，普通路径可用，但 UNC、空前缀、绝对中间段等边界不稳。
- 目标：保持三段式使用方式不变，提高路径拼接容错性。
- 建议处理：规范化前缀/中间/后缀后用 `Path.Combine`；中间和后缀按相对段处理。
- 验证方式：测试普通路径、多层中间路径、后缀带斜杠、UNC 前缀，确认预览和实际复制一致。
- 备注：已确认不改变使用者实际操作。

### AUD-025 虚拟按键目标窗口播放方案暂保持激活窗口优先

- 现状：代码当前捕获目标窗口后使用激活窗口 + SendKeys，而非文档中描述的后台注入优先。
- 目标：暂保持当前兼容性优先方案，避免重新引入历史上的后台注入不稳定问题。
- 建议处理：本轮不改代码；后续仅在确认 PostMessage 可靠或加入充分降级检测后再调整。
- 验证方式：无需本轮验证；如后续调整，应分别测试 Spine 和其他目标窗口。
- 备注：状态为设计保留，需要后续同步文档描述。

### AUD-026 Spine TXT 保存备份/编码保留暂不处理

- 现状：Spine 热键 TXT 保存未额外创建备份，也未专门保留原编码。
- 目标：本轮保持现状，不增加额外复杂度。
- 建议处理：暂不处理；依赖 Spine 自带热键重置功能恢复。
- 验证方式：无需本轮验证。
- 备注：已确认暂不处理。

### AUD-027 批量文件处理进度文字显示缺失或被遮挡

- 现状：CLI 实验合并使用原生 `ProgressBar` 叠加 `Label`，用户真实操作时能看到进度条但看不到当前处理文件文字；CLI 普通合并、批量导出、单纹理图未覆盖进度更新。批量复制只有底部状态文字，没有进度条。ReNameTool 的批量重命名、Spine 文件整理、图集解包没有进度条和当前文件文字。
- 目标：所有批量处理文件的入口都使用一致的进度显示结构：当前处理文件文字独立显示在进度条上方，进度条中间显示当前序号/总数；文字不能被原生进度条遮挡，也不能遮住原有按钮、输入框、列表和状态区。
- 建议处理：新增共用自绘 `TextProgressBar` 控件；CLI 底部进度区改为独立当前文件 `Label` + 自绘进度条，并覆盖普通合并、实验合并、批量导出、单纹理图；批量复制服务发出结构化进度事件，窗口底部显示当前复制/跳过项；ReNameTool 三个页签在列表与底部操作区之间增加同款进度区。
- 验证方式：已通过 `dotnet build KeyMacro.sln`；已用截图 harness 生成模拟进度状态截图，确认 CLI、批量复制、ReNameTool 重命名页、整理页、解包页的当前处理文件文字在进度条上方可见，进度条中间序号可见，且未遮挡原有按钮、输入框、列表和状态区。
- 备注：用户已确认将 TMP-20260602-003、TMP-20260602-004、TMP-20260602-005 合并录入并直接修复。修复过程中发现 ReNameTool 解包页列表高度会覆盖进度区，已同步缩短列表并复查截图。

### AUD-037 批量复制内层双 Tab 合并为单页（V2.91）

- 现状：`BatchCopyWindow`（已嵌入 `MainForm` 第 3 个主标签，外层 4 主 Tab 不动）内部用 `DarkTabControl` 分两页：Tab1「批量复制到目的地」（`SourceFilePicker` 单文件勾选 + 三段式目标路径 + 全量文件 × 全量路径）、Tab2「原地复制备份」（`InplaceBackupPanel` 多文件夹只读扫描 + 后缀/关键词过滤 + 五列勾选 Grid + 备份后缀 + INV-1~8）。两页来回切换，选源方式与目标语义割裂。
- 目标：合成单页左右分栏。左列沿用 Tab2 选源结构；右列沿用 Tab1 目标路径结构但改为竖向堆叠；右列顶部新增独立勾选项「原地复制」，勾选后目标路径整卡置灰禁用，改由备份后缀决定输出名。底部主按钮通栏且文案随模式切换；进度条与状态栏仍窗口级共享。
- 建议处理（分步，依据 `docs/batch-copy-merge/design.md` D1–D12 推荐默认）：
  1. `InplaceBackupPanel`：A/B/C 区保持原样；把 D 区「备份后缀 + 示例」拆成可由宿主重新挂载的控件，隐藏 D 区自带主按钮与行；新增宿主调用入口 `StartBackupFromHostAsync()`、勾选快照 `CheckedFilesSnapshot`、后缀启用态 `SetSuffixInputEnabled()`、模式感知状态文案 `HostInplaceMode`、`UseExternalAction()`。
  2. `BatchCopyWindow`：移除内层 `TabControl` 与 Tab1 旧的单文件源控件（`SourceFilePicker` 本窗口不再调用）；构建左右弹性分栏（816 : 556 填满内容区，保留 12px padding / 10px gap），右列 = 模式卡（勾选项 + 备份后缀）+ 目标卡（前缀 / 中间 / 路径后缀 / 预览目标，竖向）；底部主按钮 / 进度 / 状态栏维持通栏。
  3. 模式路由：未勾选 → `BatchCopyService.CopyFilesAsync(勾选快照, GetTargetPaths(), OnConflictAsync)`；勾选 → `InplaceBackupPanel.StartBackupFromHostAsync()`。两个 Service **不合并**、不改 INV-1~8、不复用 `ConflictDialog`/覆盖语义。
  4. 执行集合统一为「点按钮瞬间的勾选快照」（含被过滤隐藏项）；切换模式不清空源/扫描/勾选/过滤/目标路径输入，不触发磁盘 I/O；任务运行期间锁定模式开关与目标/后缀输入（替代原「禁止切 Tab」）。
  5. 三套历史分离不变（`PrefixHistory` / `SuffixHistory` / `BackupSuffixHistory`），关闭窗口的历史写回与 Cancel + 等待任务结束保护不变；`UiTheme.Apply(this, UiWindowProfile.BatchCopy)` 与嵌入机制不变。
  6. 版本：V2.9 → V2.91（用户指定版本号）；同步 `CLAUDE.md` / `AGENTS.md` / 验证用例；`dotnet build` + `dotnet publish` 产出单文件 exe。
- 验证方式：`dotnet build KeyMacro.sln` 0 错误 0 警告；两模式手工用例见 `docs/verification/manual-verification-plan.md` 新增 BCM 小节（勾选项置灰范围、执行集合、切换保留、运行中锁定、冲突弹窗、INV-1~8 不覆盖、历史写回、关闭保护）。
- 备注：状态 **已完成**（2026-11-09 18:37 立项，18:49 完成）。用户已拍板 R3 版式（外框通栏不收窄，两列各 +56px 拉长填满），并确认 D1–D9 按推荐默认执行、D12 采用「各 +56」。
- 验证结果：
  1. `dotnet build KeyMacro.sln` **0 警告 0 错误**（首次编译暴露 `_externalAction` 字段缺失与 WFO1000 序列化告警，已改为私有字段修复）。
  2. 用项目自带 `RuntimeWindowCapture` 生成真实 WinForms 截图 `docs/verification/screenshots/BCM-001_merged_layout.png`，确认单页左右分栏、模式卡（勾选项 / 备份后缀 / 生成示例）、竖向目标路径卡（前缀 / 中间 / 路径后缀 / 预览目标）、底部主按钮 / 进度 / 状态栏齐全且无裁切；放大复核发现窄右列把「生成示例」挤成两行，已改为独占一行后复拍确认。
  3. `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true` 产出 18:59 单文件 exe（113,896,872 B，与旧版同字节数属 PE 对齐巧合）；18:55 按用户指定把版本号统一为 **V2.91** 后重新编译发布，UTF-16 校验新 exe 含 `V2.91` 且不含 `V3.0`；二进制检出本次新增符号 `StartBackupFromHostAsync` / `CheckedFilesSnapshot` / `BuildModeCard` / `ApplyMode` / `DetachSuffixConfig` / `UseExternalAction`，旧 V2.9 exe 全不含，两版二进制不相同。
  4. 改动前三个源文件备份至 `docs/batch-copy-merge/backup/`，旧 exe 备份至 `KeyMacro/bin/Release/publish-v2.9-pre-merge/`（不与交付物并列呈现）。
- **未做且不得宣称通过**：两模式真实交互回归（勾选、执行到磁盘、冲突弹窗、INV-1~8 不覆盖、切换保留、运行中锁定、三套历史写回、关闭保护）需用户真机手工确认，用例见 `docs/verification/manual-verification-plan.md` 的 BCM 小节。

### AUD-038 批量复制并入 CLI 窗口子页 + 源文件批量动画选择

- 状态：**已完成**（2026-11-09 立项并施工完成；施工依据 `docs/batch-copy-cli-integration/施工文档.md`，渲染图已审查通过）。版本 **V2.92**（用户拍板），本次按用户要求不执行 git 提交。
- 现状：外层主标签 4 个，「批量复制」与「CLI批量合并/导出」各占一个，入口分散；批量复制源文件列表 5 列只有文件属性，无法查看/批量选择 `.spine` 内的动画。
- 本次目标：
  1. 删除外层「批量复制」主标签；`BatchCliWindow` 内层 `DarkTabControl` 由 2 页变 3 页，顺序固定为 **批量复制 → 合并 → 批量导出**，`BatchCopyWindow` 整体原样嵌入（两层非顶层 Form 嵌套），内部左右分栏、A/B/C、模式卡、目标路径卡、底部按钮/进度/状态零改动。
  2. `InplaceBackupPanel` C 区新增「选择动画」按钮 + 门槛备注 + 第 6 列「动画」；门槛 = 勾选快照（含被过滤隐藏项）全部为 `.spine`，否则弹窗列出违规文件且不打开动画窗。
  3. 动画弹窗 = 所有勾选 `.spine` 动画的**并集**（同名合并去重）；确认后逐文件写入 `S ∩ A(f)`，缺失部分不写、整行标红**仅提醒**；未参与行显示 `—`。
  4. 动画为**纯内存展示元数据**，不参与复制/备份执行集合、不写回源文件、不持久化；`BatchCopyService` / `InplaceBackupService` / `SpineCliService` / 合并页动画选择 / 配置文件结构零改动。
- 建议处理（分步，S1–S9 详见施工文档）：立项与基线备份 → `MainForm` 入口重组 → `BatchCliWindow` 子页嵌入与 `Close()+Dispose()` 清理 → `BackupFileEntry` 加 3 个内存字段 → `InplaceBackupPanel` 加列/工具条 → 门槛/并集弹窗/交集写入/标红 → 编译核对范围 → 实机验证与截图 → 版本号/文档/审计/发布。
- 决策默认（用户未逐项拍板，按文档推荐执行）：D0=A 全量勾选快照、D1=A 标签文字不改、D3=A 双进度区各管各的、D4=A 复用 `SpineCliService.GetProjectInfo`、D5=A 动画列在「所在目录」前、D6=A C 区独立工具条、D7=A 原备注文案、D8=A 忽略大小写按首次出现、D9=A 整行深红、D10=A 未参与行显示 `—`、D11=A 不新增跨页互斥、D13=A 纯展示。
- 验证方式：`dotnet build KeyMacro` 0 警告 0 错误；`git diff --name-only -- KeyMacro/` 仅 4 个 `.cs`；按施工文档 §7 V1–V29 逐项核对，重点是 G2 门槛、并集同名合并、交集写入、标红仅提醒、两层嵌入模态弹窗、退出后路径历史写回。
- 备注：改动前 4 个源文件备份在 `docs/batch-copy-cli-integration/backup/*.before`。
- 验证结果：
  1. `dotnet build KeyMacro` 与 `dotnet build RuntimeWindowCapture` 均 **0 警告 0 错误**；`git diff --name-only -- KeyMacro/` 恰好 4 个 `.cs`（`MainForm.cs` / `BatchCliWindow.cs` / `InplaceBackupPanel.cs` / `BackupFileEntry.cs`），V29 通过。
  2. **动画语义断言 10/10 PASS**（`docs/batch-copy-cli-integration/verification-logic.txt`，由 `RuntimeWindowCapture verify-animation-logic` 反射调用产品代码纯逻辑方法）：并集同名合并且保留首次拼写、`S ∩ A(f)` 交集写入并保留 `A(f)` 原始大小写、缺项标红、不标红用例、二次确认全量覆盖无残留、零匹配「（无匹配动画）」、未参与行「—」、G2 扩展名口径（`.SPINE` 通过 / `.spine.json`·`.png`·`.atlas`·无扩展名拦截）。
  3. **实机截图 5 张**（`docs/batch-copy-cli-integration/runtime-*.png`，由项目自带 `RuntimeWindowCapture` 生成）：`runtime-1` 真实两层非顶层 Form 嵌套（`MainForm → BatchCliWindow → BatchCopyWindow`）完整显示，外层 3 主标签、内层 3 子标签「批量复制 / 合并 / 批量导出」顺序正确；6 列表头、动画列、整行标红（含勾选列）、动画工具条与备注、右列目标卡、底部按钮/进度/状态栏齐全无裁切。`runtime-2/3` 合并页与批量导出页原控件零改动。`runtime-4` 顶层 CLI 窗口下的批量复制页。`runtime-5` G2 门槛报错弹窗实机弹出，正确列出 `hero.atlas` / `hero.png` 两个违规文件且未打开动画窗，弹窗未被主窗口遮挡（同时验证两层嵌套下的模态 owner 正确）。
  4. 退出链：`MainForm.ExitApp → CloseEmbeddedPage(_cliPage) → Close() → BatchCliWindow.OnFormClosed/Dispose → CloseCopyPage() → BatchCopyWindow.Close() + Dispose()`，截图工具走完整退出链无异常，路径历史写回链被触发（V26 链路验证通过，落盘内容需真机确认）。
- **未做且不得宣称通过**（与 AUD-037 同口径）：真实 Spine.com 读取 `.spine` 动画列表、动画弹窗的实机模态显示、两模式真实复制到磁盘、INV-1~8 不覆盖、三套历史落盘内容、跨页任务并发，均需用户真机手工确认，用例见 `docs/verification/manual-verification-plan.md` 的 BCC 小节。
- 实施差异（已在 `docs/batch-copy-cli-integration/施工文档.md` 施工记录逐条说明）：D2 顶栏按 A 原样保留（动画读取依赖它）；列宽按实机可用宽度收窄以避免横向滚动；弹窗用系统原生 `MessageBox`（浅色，与项目其它报错一致），渲染图中的深色弹窗为示意稿；G3 的 Spine.com 路径从 `ConfigService.LoadCliSpinePath()` 读取，等价于宿主顶栏每次变更后的落盘值，因此无需改动 `BatchCopyWindow.cs`（R2 字节不变）。

### AUD-039 备份后缀触发静默排除，导致 `*-n1.spine` 检索不到

- 状态：**已完成**（2026-11-09 用户报告并经对话确认；用户拍板「彻底取消静默排除」+ 版本 **V2.93**）。
- 症状：在批量复制页选择文件夹后，同一目录下 `G2.spine`、`tx-xy.spine` 正常显示，唯独 `G2-n1.spine` 找不到；`D:\动画工程\monster-spine-h细线` 下每个子文件夹都恰好漏掉一个 `*-n1.spine`（实测共 29 个 / 全目录 115 个 `.spine`）。
- 根因（显示层，非扫描层）：`InplaceBackupService.ScanAsync` 用 `EnumerateFiles(dir, "*")` 无文件名过滤，文件均已扫入；但 `InplaceBackupPanel.RebuildVisible()` 先执行 `_scanned.Where(e => !IsLikelyBackup(e, suffix))`，`IsLikelyBackup` 判定 `stem.EndsWith(suffix)`。`LoadInitialSuffix()` 取 `%APPDATA%\KeyMacro\path_history.json` 的 `BackupSuffixHistory[0]`，用户该文件当前为 `-n1`，于是 `G2-n1` 命中并被静默剔除。状态栏仅有一句不显眼的「已排除 N 个疑似备份文件」。
- 加剧因素：默认「目标路径模式」下备份后缀输入框被置灰，用户看不到当前后缀；工具条上的「后缀」下拉实为扩展名筛选，与备份后缀无关。且目标路径模式根本不生成带后缀副本，却被一并套用防套娃规则。
- 排除误判：已验证 `G2-n1.spine` 文件属性为 `Archive`（非隐藏 / 非系统 / 非重解析点），排除文件损坏、权限与符号链接可能。
- 本次目标（用户选择「彻底取消静默排除」）：
  1. 删除 `IsLikelyBackup` 与 `_excludedCount`，`RebuildVisible()` 让全部扫描结果进入可见列表，是否勾选由用户决定；
  2. 状态栏不再显示「已排除 N 个疑似备份文件」；后缀变化不再触发列表重算，仅刷新占用预检；
  3. 防重复覆盖继续由 INV-4「目标已存在自动跳过」承担，不动 `InplaceBackupService` / `BatchCopyService` / 配置结构 / UI 布局 / INV-1~8。
- 行为变化（需用户知悉）：原地复制模式下勾选 `G2-n1.spine` 且后缀仍为 `-n1` 时，会生成 `G2-n1-n1.spine`（不覆盖已有文件）；这是取消静默排除后的预期结果。
- 验证方式：`dotnet build KeyMacro` 0 警告 0 错误；行为断言覆盖「后缀 -n1 下 `G2-n1.spine` 可见」「换后缀结果不变」「扩展名 / 关键词过滤回归」；用真实目录走产品 `RebuildVisible` 出实机截图。
- 备注：改动前源文件备份于 `docs/fix-n1-silent-exclude/backup/InplaceBackupPanel.cs.before`。
- 验证结果：
  1. `dotnet build KeyMacro` 与 `dotnet build RuntimeWindowCapture` 均 **0 警告 0 错误**。按 `diff -u` 对照改前备份，本轮产品代码只改 `InplaceBackupPanel.cs` 5 处（删 `_excludedCount` 字段 / 删 `ClearList` 赋值 / 删 `IsLikelyBackup` / `RebuildVisible` 改为全量 / 删状态栏提示，外加 `_txtSuffix.TextChanged` 改走 `DebounceStatusHint`）；`MainForm.cs` 只改标题版本号 V2.92 → V2.93。`InplaceBackupService` / `BatchCopyService` / `BackupFileEntry` / 配置结构 / UI 布局 / INV-1~8 全部零改动。
  2. **AUD-039 断言 14 PASS / 0 FAIL / 0 SKIP**（`docs/fix-n1-silent-exclude/verification-n1-filter.txt`，由 `RuntimeWindowCapture verify-n1-filter` 反射调用产品代码 `RebuildVisible`）：后缀 `-n1` / `_bf` / `G2` / 空值下可见数恒等于扫描数；`G2-n1.spine` 与 `G10-skin_merged-n1.spine` 均可见；扩展名过滤（只剩 5 个 `.spine` 且含 2 个 `-n1`）与关键词过滤（`-n1` 命中 2 个）回归通过；状态栏不再出现「疑似备份文件」；产品代码已无 `IsLikelyBackup` 方法与 `_excludedCount` 字段。产品面板构造时实测读到的初始后缀就是 `-n1`，与用户 `path_history.json` 一致。
  3. **真实工程目录只读复核**：`D:\动画工程\monster-spine-h细线` 扫描 2835 个文件，可见 2835 个（零静默排除），其中 29 个 `*-n1.spine` 逐个核对全部在可见列表中。
  4. **实机截图** `docs/fix-n1-silent-exclude/runtime-n1-fixed.png`：用 `D:\动画工程\monster-spine-h细线\G2` 真实目录走产品 `ScanAsync → RebuildVisible → RefreshGrid` 全链路、后缀保持现场值 `-n1`、扩展名筛选 `.spine`，列表中 `G2-n1.spine`（第一行）/ `G2.spine` / `tx-xy.spine` 三个文件全部显示；6 列表头、动画列、选择动画工具条、右列目标卡、底部主按钮与状态栏齐全无裁切；状态栏为「就绪 · 已扫描 98 个文件 · 勾选文件后配置目标路径并点击『开始复制』」，不再出现「已排除 N 个疑似备份文件」。
  5. 发布 exe 与版本串校验见 V2.93 版本历史条目。
- **未做且不得宣称通过**：两模式真实复制到磁盘、INV-1~8 不覆盖全量回归、退出后三套历史落盘内容，需用户真机手工确认，用例见 `docs/verification/manual-verification-plan.md` 的 N1 小节。
- git：按 AGENTS.md:261 询问后用户拍板「暂不提交」，工作区保留 AUD-038（V2.92）+ AUD-039（V2.93）两批未提交改动。
- **行为变化（需用户知悉）**：原地复制模式下勾选 `G2-n1.spine` 且后缀仍为 `-n1` 时会生成 `G2-n1-n1.spine`（INV-4 保证不覆盖已有文件）；这是取消静默排除后的预期结果，如不希望出现可把备份后缀改为 `-n2` 等不与实际文件名冲突的值。

### AUD-040 导出页改名 / 导出预设 / 批量生成导出配置 / 状态列修复

- 状态：**已完成**（2026-11-09 用户审查施工文档与渲染图后下达开工令，并补充两条决策；版本 **V2.94**（用户拍板），按用户要求不执行 git 提交）。
- 施工依据：`docs/export-preset/施工文档.md`（待审查稿）+ `preview.html` / 5 张渲染图。
- 现状：`MainForm.cs:198` 主标签「CLI批量合并/导出」、`BatchCliWindow.cs:324` 子标签「合并」；批量导出页 `exportPanel` 为 2 列，仅「导出 / 单纹理图」，无预设、无批量生成配置。
- **状态 bug 根因（用户报「export.json 状态永远不正确，必须切配置名才刷新」）**：`BuildExportTab()` 中 `:1229` 先 `_rbFinish.Checked = true`，`:1230` 才订阅 `CheckedChanged` → 初始选中不触发事件 → 字段 `_exportConfigName` 停留在初值 `"export.json"`（`:58`），而 UI 显示 `finish.export.json`；`RefreshExportStatus()` / `AddExportEntry()` 都读这个错字段，扫描后也没有重算。
- 本次目标：
  1. R1/R2 改名：主标签 →「批量复制/合成/导出」、子标签「合并」→「批量合并」、`BatchCliWindow` 顶层窗口标题同步改名；索引与懒加载零改动。
  2. R3–R7 导出预设：底部追加「保存为导出预设」、右侧新增第 3 列预设列表（三列）+ 行 4 的「激活预设 / 删除预设 / 删除全部预设」；命名用 `InputDialog`；双击与按钮走同一激活方法，恢复源目录 / 全部文件路径 / 输出目录 / 导出配置名与单选项（含「其他」手填值）。
  3. **用户补充决策 ①**：「批量生成导出配置」按钮后面必须加备注「（必须 4.3 及以上版本才能使用）」。据此 **D0 定为 A（真·CLI 透传 `--last-export-settings`），取消 B 模板兜底 / C 自动回退**；生成失败不静默降级，汇总里明确提示版本要求。
  4. **用户补充决策 ②**：导出预设必须**保存在本地软件数据文档内** → 独立文件 `%APPDATA%\KeyMacro\export_presets.json`，每次变更**立即落盘**（D6=A 确认），重启后仍在；不塞进现有 `config.json` / `path_history.json`，不写项目/工程目录。
  5. R8/R9 批量生成：按当前配置名（含「其他」手填值），用 CLI 在列表每个文件所在目录生成同名配置；按目录去重、已存在跳过、失败继续、进度与取消复用现有骨架、结束联动刷新状态列。
  6. R10 状态根治：单一真值源 `CurrentExportConfigName` + 修正初始化顺序 + 扫描/切换/激活/生成完成后联动刷新。
- 决策默认（用户未逐项回答 D1–D19，按施工文档推荐执行）：D1=A（配置内容来自项目内最近一次导出设置）、D2=A（窗口标题同步改名）、D3=B（同名拒绝）、D4=A（保存并恢复配置名/单选项）、D5=A（记录创建时间）、D7=A（不校验目录）、D8=A（默认名 `预设 N`）、D9=B（保留 ButtonWidth 旧 case）、D10=B（三列列表）、D11=A（320px 一行三按钮）、D12=A（缺失文件跳过并提示）、D13=A（激活不重扫磁盘）、D14=A（删除二次确认）、D15=A（每目录只生成一次）、D16=A（只禁生成/导出/打包）、D17=A（取消不回滚）、D18=A（已存在跳过）、D19=A（radio 文本不改）。
- 验证方式：`dotnet build KeyMacro` 0 警告 0 错误；`git diff --name-only -- KeyMacro/` 核对范围；`RuntimeWindowCapture verify-export-preset` 行为断言；实机截图覆盖改名标签 / 预设列 / 4.3 备注 / 激活结果 / 状态修复；按施工文档 §7 V1–V30 核对。
- 备注：改动前 4 个源文件备份于 `docs/export-preset/backup/*.before`。
- 验证结果：
  1. `dotnet build KeyMacro` 与 `dotnet build RuntimeWindowCapture` 均 **0 警告 0 错误**。按备份逐文件对照，本轮产品代码只改 4 个 `.cs`（`MainForm.cs` 标签文字 + 版本号、`BatchCliWindow.cs` 导出页、`ConfigService.cs` 新增 4 个静态方法、`SpineCliService.cs` 新增 2 个方法），新增 1 个 `Models/ExportPreset.cs`；`InplaceBackupPanel.cs` / `BackupFileEntry.cs` 在 `git diff` 中出现是 AUD-038/039 两批**未提交**改动，本轮字节零改动。配置结构、合并页、批量复制页、顶栏、进度区公共行为、INV-1~8 全部零改动（V30 通过）。
  2. **AUD-040 断言 33 PASS / 0 FAIL**（`docs/export-preset/verification-export-preset.txt`，`RuntimeWindowCapture verify-export-preset`）：涵盖 V1/V2 三处改名与外层仍 3 主标签、V21 初始真值源 `finish.export.json`（旧代码为 `export.json`）、V21 产品代码已无 `_exportConfigName` 字段、V23 切 demotion / 切回 finish 立即变化、V24「其他」手填值、V22 临时目录真实文件下「无配置 ❌ → 点刷新 ✅ → 删除后 ❌」、U1 备注文字与 `UiTheme.Orange`、R3/R6/R8 五个新按钮、R4 预设列表三列、V9 空列表禁用与选中后启用、U2 JSON 往返（中文名 / 路径 / 其他手填名 / 单选项 / 创建时间）、U2 损坏文件返回空列表、U2 默认落盘路径 `C:\Users\Administrator\AppData\Roaming\KeyMacro\export_presets.json`、R7 激活「其他」预设不弹 InputBox 且恢复目录、U1 CLI 参数构造、D15 同目录去重、V24 手填名作为生成文件名。
  3. **前两轮回归**：AUD-038 动画语义 10/10 PASS、AUD-039 后缀过滤 14/14 PASS（`docs/export-preset/verification-*-regression.txt`）。
  4. **实机截图 6 张**（`docs/export-preset/runtime-*.png`，真实 WinForms 而非 HTML）：`runtime-1-export-presets.png` 两层嵌套 + 改名标签 + 3 预设 + 5 个新按钮 + 橙色 4.3 备注；`runtime-2-activated.png` 激活后源目录 / 输出目录 / 3 行 ✅ / 已激活提示；`runtime-3-cli-export-top.png` 顶层 CLI 窗口导出页；`runtime-4-merge-regression.png` 合并页；`runtime-5-copy-regression.png` 批量复制页 6 列与动画列；`runtime-6-n1-regression.png` `-n1.spine` 仍可见。均无裁切、无重叠、无横向滚动条。
  5. **U1 只读复核**：Spine 官方命令行界面文档确认 `--last-export-settings` 与 `--ignore-unknown`，`-o` 可为完整文件路径；本机 `D:\Program Files\Spine\Spine.com` 为 Launcher 4.1.10，`--help` / `--advanced` 均未列该参数，与用户「必须 4.3 及以上版本才能使用」一致。
  6. 发布 exe 与版本串校验见 V2.94 版本历史条目。
- **未做且不得宣称通过**（与 AUD-038/039 同口径）：本机启动器 4.1.10 无法验证真实 CLI 生成配置文件，**需用户在 Spine 4.3 及以上环境真机确认**；命名弹窗 / 删除二次确认的模态显示、重启程序后预设从 `export_presets.json` 恢复、预设含已不存在文件时的跳过提示、批量生成的取消与失败汇总、两模式真实复制到磁盘、INV-1~8 全量回归，均需用户真机手工确认，用例见 `docs/verification/manual-verification-plan.md` 的 EP 小节。
- git：按 AGENTS.md:261 询问后用户拍板「暂不提交」，工作区保留 AUD-038（V2.92）+ AUD-039（V2.93）+ AUD-040（V2.94）三批未提交改动。

## 后续维护规则

1. 修复代码前，先将对应条目的 `处理状态` 改为 `处理中`，并在详情中补充本次目标。
2. 修复完成并验证后，将状态改为 `已完成`，并在详情中补充验证结果。
3. 新发现的问题先追加到本台账，分配新的 `AUD-XXX` 序号，再决定是否进入实现。
4. 若某项经讨论不再修改，将状态改为 `设计保留` 或 `暂不处理`，并记录原因。
