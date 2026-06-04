# Spine助手 - 快捷键宏工具

## 概述
快捷键宏工具，面向 Spine 动画师。可自定义按键序列、绑定触发快捷键（键盘/虚拟按键）、后台播放。附带 Spine 热键文件编辑器，可直接修改 Spine 的 hotkey TXT 文件。

## 技术栈
- .NET 9.0 + WinForms
- Win32 API (RegisterHotKey, SendKeys)
- JSON 配置存储

## 项目结构
```
KeyMacro/
├── Program.cs               # 入口
├── gen_skin_images.csx      # 皮肤 PNG 状态帧生成脚本
├── assets/
│   └── ui/                  # 通用 UI 美术资源（普通窗口/菜单/弹窗，不含 VK 皮肤）
├── icons/                   # 应用图标资源
│   └── app.ico
├── skins/                   # 皮肤 PNG 资源目录
├── Models/
│   ├── DataBundle.cs        # 统一导入导出数据模型
│   ├── MacroSequence.cs     # 数据模型 (序列 + 步骤)
│   ├── PathHistory.cs       # 批量复制前缀/后缀历史记录
│   ├── SpineCliEntry.cs     # CLI 任务数据模型（含 CliResult）
│   └── VirtualButton.cs     # 虚拟按键数据模型（含 IsSpacer）
├── Services/
│   ├── ConfigService.cs     # JSON 配置读写 (%APPDATA%\KeyMacro\config.json)
│   ├── DataBundleService.cs # 统一导入导出服务
│   ├── HotkeyService.cs     # Win32 全局热键注册/监听
│   ├── IconService.cs       # 应用图标加载（嵌入→磁盘→代码三级回退）
│   ├── MacroPlayer.cs       # SendKeys 按键序列播放引擎
│   ├── OperationLogger.cs   # 文件日志系统 (%APPDATA%\KeyMacro\logs\)
│   ├── UiTheme.cs           # 通用深灰 UI 主题与资源加载（排除 VirtualKeyWindow 本体）
│   ├── BatchCopyService.cs  # 批量复制执行引擎（冲突检测/进度/日志）
│   ├── SpineCliService.cs   # Spine.com CLI 进程调用封装
│   ├── SpineHotkeyService.cs # Spine TXT 文件解析/保存 + 按键名格式转换 + 中文注解
│   ├── VirtualButtonManager.cs # 虚拟按键列表管理（排序/间隔）
│   ├── VirtualKeyBindingManager.cs # 虚拟按键 ↔ 序列绑定
│   ├── VirtualLayoutSerializer.cs # 虚拟按键窗口布局持久化
│   ├── VirtualLoopExecutor.cs # 循环按钮执行器
│   └── VkSkinLoader.cs        # 皮肤资源加载器（嵌入资源 + 磁盘双源）
└── Forms/
    ├── MainForm.cs          # 主窗口 + 系统托盘（导入导出）
    ├── SequenceEditor.cs    # 序列编辑器 + 热键录制对话框
    ├── SpineHotkeyEditor.cs # Spine 热键 TXT 文件编辑窗口（支持数据构造）
    ├── BatchCopyWindow.cs   # 文件批量复制主窗口
    ├── BatchCliWindow.cs    # CLI批量合并/导出窗口（双Tab）
    ├── ConflictDialog.cs    # 复制冲突弹窗（覆盖/跳过/打开文件夹）
    ├── InputDialog.cs       # 通用输入对话框
    ├── SourceFilePicker.cs  # 源文件缩略图浏览+勾选弹窗
    ├── SubfolderSelectDialog.cs # 子文件夹勾选导入对话框
    ├── VirtualKeyWindow.cs  # 虚拟按键浮动窗口
    ├── VkWindowManager.cs   # 多虚拟按键窗口管理器
    └── VirtualButtonWidget.cs # 虚拟按钮自绘控件
```

## 关键约定
- **序列（MacroSequence）**：由触发快捷键 + 多个步骤组成，`LoopCount`（1=单次 >1=循环N次 0=无限）
- **步骤（MacroStep）**：类型（单键/组合键/文本）+ 按键值 + 延迟(ms)
- **序列编辑器**：步骤列表每行末尾有"复制"按钮，可快速复制步骤行
- **_suppressEvents**：SequenceEditor 使用此标志防止 DataGridView 事件递归
- **config.json**：存储在 `%APPDATA%\KeyMacro\`，由 ConfigService 管理
- **Spine TXT 注解文件**：`{文件名}.txt.annotations.json`，存储中文备注，不污染源 TXT
- **Spine 按键格式**：TXT 文件使用 Spine 命名（如 `PERIOD`、`COMMA`），录制时自动转换 WinForms → Spine 格式
- **IconService**：所有窗口图标统一加载，优先级 嵌入资源 → 磁盘文件 → 代码生成回退（蓝底 K 字）

## 构建与运行
```bash
dotnet run --project KeyMacro
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishDir="bin/Release/publish"
# 若 exe 被占用: taskkill /f /im KeyMacro.exe 后再 publish
```

## 右键菜单体系
- **按钮右键菜单**（`OnWidgetContextMenu`）：修改按钮名称 / 按钮循环延迟（仅循环按钮）/ 增加间隔 / 强制停止 / 删除当前按钮
- **空白区域右键菜单**（`BuildBlankMenu`）：增加按钮 / 删除所有按钮 / 置顶/取消置顶 / 透明度 / 按钮位置锁定/解锁 / 捕获/清除目标窗口 / 竖向模式 / 缩放(50-200%) / 窗口锁定/解锁 / 关闭窗口
- 按钮循环延迟支持自定义数值（通过 InputBox）
- **间隔**（Spacer）：通过"增加间隔"在按钮之间插入固定宽度空白分隔条，不可交互，随缩放等比变化

## 架构要点
- `HotkeyService` 通过重写 `WndProc` 接收 `WM_HOTKEY` 消息
- `MacroPlayer.Play()` 是 async Task，包含 500ms 初始延迟（给用户松手时间），循环由 `LoopCount` 控制
- `MacroPlayer.PlayToWindow()` 是 async Task，使用 PostMessage 直接向目标窗口发键盘消息，无初始延迟
- 主窗口关闭时隐藏到系统托盘，不退出进程
- 播放期间 `MacroPlayer.IsPlaying` 为 true 以阻止嵌套触发
- `SpineHotkeyService.ToSpineFormat()` 将 WinForms 按键名转回 Spine 格式，避免写回 TXT 后 Spine 无法识别
- `BatchCliWindow` 使用 `SpineCliService` 封装 CLI 调用，TabControl 分"合并"（`-r` 集中管理 / 实验功能 `--merge`）和"批量导出"两页
- 批量导出 Tab 支持切换 export.json 配置方案（finish/demotion/自定义），通过 `_exportConfigName` 字段控制检测文件名

## 循环机制
- `MacroSequence` 无单独的 `Loop` 开关字段，由 `LoopCount` 单一控制：1=执行一次，>1=循环N次，0=无限循环
- 循环播放中的按钮再次点击可停止（调用 `MacroPlayer.Stop()`）
- 主窗口列显示"循环(次)"，MainForm 不再有"循环执行"复选框

## 虚拟按键目标窗口机制
- **目标窗口捕获**：VK 窗口右键菜单"捕获目标窗口"→ 隐藏窗口 → 3 秒倒计时 → 捕获前台窗口进程名 + 窗口标题 → 持久化到布局文件
- **方案 A（PlayToWindow）**：通过 PostMessage 直接向目标窗口注入 WM_KEYDOWN/WM_KEYUP/WM_CHAR 消息，不切换焦点
- **方案 B（自动激活）**：播放前 SetForegroundWindow 激活目标窗口，再用 SendKeys 发送
- 两方案共存自动降级：优先方案 A，若检测无效则自动切换到方案 B

## 主窗口序列列表
- 工具栏按钮：添加 / 编辑 / 删除 / 测试 / **复制序列** / 暂停全部 / Spine热键编辑 / 释放 / 删除全部 / VK 开/关/管理 / 批量重命名 / **批量复制** / 导入/导出
- "触发快捷键"列：绑定键盘热键时显示快捷键，绑定虚拟按键时显示"虚拟按键(按钮名)"
- 列宽使用 Fill 模式等比分配（`AutoSizeMode = Fill` + `FillWeight`），随窗口缩放自动调整

## VkPickMode 绑定流程
- SequenceEditor 有三颗按钮管理触发快捷键：**键盘录入**（打开键盘录制窗口）、**虚拟按键**（进入 VkPickMode）、**清除**（清空热键）
- VkPickMode 带有黄色状态栏提示，支持 Esc 取消和"取消拾取"按钮
- 点击任意虚拟按钮 → 自动拾取复合键 `"窗口名/按钮名"`（_txtVkBind）和触发快捷键（_txtHotkey，如有）
- `MainForm.SyncVkButtonBindings` 通过复合键 `"窗口名/按钮名"` 匹配，再回退到纯按钮名匹配；自动同步 `BindActionId`
- `MainForm.RequestOpenVirtualKeys()` 可供 SequenceEditor 在 VK 窗口未打开时自动创建

## 虚拟按键窗口
- `FormBorderStyle = FixedSingle`，原生 Windows 标题栏（自带拖动、关闭、系统菜单）
- 标题栏文字：`[目标] 虚拟按键 (N)`，显示目标名和按钮数量
- 锁定：切换 `FormBorderStyle = None`，标题栏消失，窗口/按钮位置不动
- **无自绘工具栏、无自定义拖拽事件、无缩放手柄**
- **布局方向**：横排（默认）/ 竖排，右键菜单切换。始终单排，无多排模式
- **缩放**：右键菜单百分比预设（50/75/100/150/200%）+ 自定义输入（10-200%）
- **无拖拽缩放，无最小/最大尺寸限制**

## 虚拟按键布局算法
- 基于基础常量计算，不依赖控件运行时属性：
```
BASE_BTN_H = 48   BASE_GAP = 4   BASE_MARGIN = 10   BASE_SPACER_W = 20
BaseBtnWidth: SmallIcon=48  LargeIcon=96  LoopIcon=110

横排:
  窗口宽 = margin + sum(各按钮实际宽) + (N-1)×gap + margin + 边框补偿
  窗口高 = 标题栏(28) + margin + btnH + margin + 边框补偿
竖排:
  窗口宽 = margin + max(各按钮实际宽) + margin + 边框补偿
  窗口高 = 标题栏(28) + margin + sum(各按钮高) + (N-1)×gap + margin + 边框补偿
```
- 按钮间距 `gap`、边距 `margin`、间隔宽度随 ScaleFactor 等比缩放
- Panel.Padding 和 widget.Margin 同步更新，保证视觉一致性

## VkWindowManager 多窗口管理
- 主界面三按钮：「开启虚拟按键」（显示所有 enabled 窗口）「关闭虚拟按键」（隐藏所有窗口）「管理虚拟按键」（打开管理器）
- VkWindowManager 以 DataGridView 列出所有窗口：名称、目标进程、按钮数、允许显示(checkbox)、显示/隐藏、删除
- 每个窗口独立运行：独立按钮列表、目标进程、缩放/方向/皮肤、位置尺寸
- VK 窗口自身右键菜单含「删除当前窗口」彻底删除，「关闭窗口」仅隐藏
- 布局文件 `virtual_layout.json` 使用多窗口格式 `{"Windows": [...]}`，旧单窗口格式自动迁移

## 虚拟按键右键菜单
- 按钮右键菜单顶部显示 `[ 按钮名 ]` 作为标题（禁用，仅展示），下方依次为修改名称/循环延迟（仅循环按钮）/按钮间距/强制停止/删除
- 空白区域右键菜单顶部为增加按钮选项，下方为窗口控制（置顶/透明度/锁定/目标窗口/竖向模式/缩放/窗口锁定/关闭）

## 日志系统
- `OperationLogger` 是静态类，日志路径 `%APPDATA%\KeyMacro\logs\yyyy-MM-dd.log`
- 自动清理 7 天前的日志，单文件超 5MB 自动轮转
- 关键操作（播放序列、热键触发、配置读写、VkPickMode）均记录日志

## 皮肤资源系统
- `VkSkinLoader` 加载皮肤资源，支持**嵌入式资源**（发布后的 exe）和**磁盘**（开发时 `dotnet run`）双源
- 有图（PNG）→ 绘制图片；无图 → 回退 GDI+ 硬编码绘制
- 按钮图片按样式命名：`btn_{style}_{state}.png`（style=small/large/loop, state=normal/pressed/active），无样式名时回退通用 `btn_{state}.png`
- 窗口背景 `window_bg.png` 使用 9-slice 缩放（边距 10px）
- 资源放在 `KeyMacro/skins/<名称>/` 目录，通过 `virtual_layout.json` 的 `SkinPath` 字段指定
- `skin.json` 可配置颜色字段（可选），缺失时使用硬编码默认值
- `ConfigService`/`VirtualLayoutSerializer`/`SpineHotkeyService` 均采用双路径策略：先加载项目目录（CWD），再 APPDATA，最后回退嵌入式默认值

## DPI 缩放
- `HighDpiMode.PerMonitorV2` 已在 `Program.cs` 中设置
- **VirtualKeyWindow**：`GetEffectiveScale()` = `_scaleFactor * (DeviceDpi / 96f)` 合并系统 DPI 和用户缩放
- **VirtualButtonWidget**：`ScaleFactor` 接收 VKWindow 传入的有效缩放值，`Scaled(val)` 自动用于尺寸；字号改用按钮高度的比例（`Height * 0.17`），自动适配 DPI
- **MainForm**：DataGridView 列宽在 `RefreshGrid()` 中乘以 `DeviceDpi / 96f`
- **SequenceEditor**：`OnLoad` 中对 `_topPanel` 行高/列宽应用 DPI 系数
- 所有窗口覆盖 `OnDpiChanged` 以支持跨显示器 DPI 切换

## 批量复制（V2.7 新增）
- 主窗口工具栏"批量复制"按钮 → 打开 `BatchCopyWindow`
- **源文件选择**：点击"选择文件" → 弹出 `SourceFilePicker` 弹窗（目录浏览 + 缩略图预览 + 勾选）→ 确认后路径列表展示
- **源列表操作**：已选文件实时列表，支持选择文件去重追加、移除选中项、清空列表
- **目标路径三段式**：前缀（`D:/exp/`）+ 中间列表（逐行读取）+ 后缀（`images`）→ 拼接生成完整路径
- **智能导入**：前缀选择目录时检测子文件夹 → 提示 → 子文件夹勾选导入对话框（全选/全不选）→ 自动填入中间文本框
- **中间列表**：多行 TextBox，用户可自由增删改每行内容，支持 `/` 多层级
- **实时预览**：任何段变化后 300ms 防抖更新预览列表
- **冲突处理**：每目标目录独立 `ConflictDialog` → 列出同名文件 → 覆盖/跳过/打开文件夹（`Path.GetFullPath` 标准化路径）
- **历史记录**：前缀/后缀使用 ComboBox，输入自动记忆，关闭窗口自动保存，支持一键清理历史
- **复制引擎**：`BatchCopyService` 异步执行，自动创建目录，进度状态栏，取消支持，OperationLogger 日志

## 统一导入导出
- 主工具栏"导入"/"导出"按钮，使用 `.kmp` 格式（JSON）
- 导出包含：Spine 热键编辑（如有打开）、序列设置、VK 布局、VK 设置
- 导入时分 4 部分逐项确认（Spine 热键/序列/VK 布局/VK 设置）
- [DataBundle.cs](KeyMacro/Models/DataBundle.cs) 定义数据模型
- [DataBundleService.cs](KeyMacro/Services/DataBundleService.cs) 负责序列化/反序列化

## 透明与渲染
- 不再使用 `TransparencyKey = #FF00FF` 方案（避免粉色边缘残留）
- 按钮 Widget 使用 `g.Clear(Color.Transparent)` + PNG 原生 Alpha
- 窗口背景 9-slice 直接绘制到 panel
- 皮肤背景图时使用 `skin.json` 中 `window_bg` 颜色作为 Panel 底色

## 通用 UI 资源系统
- `KeyMacro/assets/ui/` 是普通 WinForms 窗口的 UI 美术资源包，包含按钮多状态、输入框、列表、Tab、进度条、菜单、弹窗、标题栏按钮和图标。
- `UiTheme.Apply(form, profile)` 负责套用深灰专业软件风格、默认窗口尺寸、按钮状态图、输入框/列表/表格/菜单/进度条配色。
- `UiTheme` 会优先从磁盘向上查找 `assets/ui`，找不到时回退到嵌入资源；`KeyMacro.csproj` 已将 `assets/ui/**` 作为 `EmbeddedResource`。
- 主题只服务普通窗口、核心弹窗和菜单；**不得直接套用到 `VirtualKeyWindow` 本体、`VirtualButtonWidget` 或 `KeyMacro/skins/*`**，避免破坏 VK 皮肤和布局算法。
- UI 重构必须遵守“代码功能为准”：按钮文字、事件绑定、表格列、菜单项以 `KeyMacro/Forms` 真实代码为准；设计图只指导布局和视觉。

## 问题台账与验证机制（必遵）

### 代码问题台账
- 正式代码审核与待修问题统一记录在 [docs/audit.md](docs/audit.md)。
- 修复任何代码问题前，必须先在 `docs/audit.md` 中找到或新增对应 `AUD-XXX` 条目。
- 开始修改前，将对应条目的 `处理状态` 改为 `处理中`，并补充本次修复目标。
- 修复完成并验证后，将状态改为 `已完成`，并补充验证结果。
- 新发现的问题不得直接开修；先追加到 `docs/audit.md` 或先记录到临时发现文件并等待确认，再进入修复。
- `docs/audit.md` 中标记为 `设计保留` 或 `暂不处理` 的问题，除非用户重新确认，否则不得擅自修改。

### 功能验证机制
- 项目功能验证方案统一维护在 [docs/verification/manual-verification-plan.md](docs/verification/manual-verification-plan.md)。
- 验证覆盖每个界面、模块和可操作功能，包括填写、复制、录入、删除、上下移、导入导出、右键菜单、取消、冲突处理等基础操作。
- 验证过程中的截图证据统一存放在 `docs/verification/screenshots/`，截图文件名使用 `CASE-ID_step_result.png` 格式。
- 验证发现的疑似问题先记录到 [docs/verification/findings.tmp.md](docs/verification/findings.tmp.md)，不得直接写入正式 audit。
- 临时发现需要先通过对话确认，确认后再追加到 `docs/audit.md` 并分配新的 `AUD-XXX` 序号。
- 手工测试素材统一放在 `manual_test_assets/`，按 [manual_test_assets/README.md](manual_test_assets/README.md) 的目录约定存放。

## 版本管理与发布流程（必遵）

1. 修改完成后确认本次更新级别：问题修复 / 小功能更新 / 大版本更新 
2. 修改后在 [MainForm.cs](KeyMacro/Forms/MainForm.cs#L31) 标题中迭代版本号：
   - 问题修复 → +0.01（如 1.2 → 1.21）并且抹去更更低位的数字
   - 小功能更新 → +0.1（如 1.2 → 1.3）并且抹去更更低位的数字
   - 大版本更新 → +1（如 1.2 → 2.0）并且抹去更更低位的数字
3. 总结并修改 AGENTS.md   
4. 总结修改内容，询问是否提交 git（以当前版本号作为提交名称，并写明修改摘要）
5. 修改完成后导出一个单独的.exe应用供测试，如遇应用已开启导致无法修改就强行终止应用再尝试导出

## 版本历史
- **V2.84** (2026-06-04): UI 原生控件替换第四阶段，普通控件句柄接入 Windows `DarkMode_Explorer` 原生深色主题，改善 TextBox/ListBox/ListView 等系统滚动条白色残留；不替换滚动控件、不改变滚动逻辑
- **V2.83** (2026-06-04): UI 原生控件替换第三阶段，接入 Windows DWM 原生深色标题栏，普通窗口标题栏由系统绘制为深色并保留拖动、最小化、最大化、关闭和系统菜单；继续排除 `VirtualKeyWindow` 本体
- **V2.82** (2026-06-04): UI 原生控件替换第二阶段，新增 `DarkComboBox` 并局部替换批量复制前缀/后缀历史输入框，覆盖原生白色下拉按钮；保留输入、历史、自动完成、TextUpdate/SelectedIndexChanged 逻辑
- **V2.81** (2026-06-04): UI 原生控件替换第一阶段，新增 `DarkTabControl` 和 `DarkCheckedListBox`，替换 CLI、ReNameTool、动画选择、子文件夹选择中的原生页签白条和白色勾选框；保留现有 TabPage、CheckedItems、ItemCheck 与业务逻辑
- **V2.8** (2026-06-03): 启用 UI 资源包与 `UiTheme` 通用主题，普通窗口默认尺寸按 UI 概览图调整，按钮/输入/列表/菜单/进度条切换为深灰专业软件风格；排除 `VirtualKeyWindow` 本体和 VK 皮肤资源
- **V2.79** (2026-06-02): 修复 AUD-027，统一 CLI 批量合并/导出、批量复制、批量重命名/整理/解包的批量进度显示；新增自绘进度条，当前处理文件文字独立显示在进度条上方，避免被原生 ProgressBar 遮挡
- **V2.78** (2026-06-02): 修复代码审核台账 AUD-001~AUD-024（单实例唤醒、AppData 配置/布局优先、目标进程绑定、Win 组合键、强制停止释放、VK 循环菜单暂停、Spine 热键原文导入导出、VK 增量导入、CLI 异步取消、批量复制冲突处理等）
- **V2.77** (2026-05-22): Spine 4.3 CLI 实验合并功能（--merge/-a）、批量导出配置选择、进度条、文件选择过滤修复、排除筛选
- **V2.76** (2026-05-21): 修复批量CLI弹窗选择文件路径异常，探索JSON合并骨架方案并总结文档
- **V2.75** (2026-05-21): 新增CLI批量合并/导出工具（SubfolderSelectDialog 重构、批量CLI窗口、SpineCliService）
- **V2.7** (2026-05-21): 新增批量复制功能，重构中间列表/源文件选择/历史记录
- **V2.6** (2026-05-20): 导入导出精细化管理（全部窗口导出、按 key 对位导入、逐项确认、重名检测）
- **V2.5** (2026-05-20): 合并批量重命名/spine解包整理工具，移除测试按钮，工具栏重新排序
- **V2.18** (2026-05-20): 修复目标窗口标题精确匹配失败后无 fallback 的问题
- **V2.17** (2026-05-20): 修复 `SendCombo` 组合键播放时字母大写导致多按 Shift 的问题
- **V2.16**: dedup load, skip empty keys on save, EndEdit on save
# Git 提交规则（强制）

当你需要为我自动提交代码时，必须严格遵守以下流程，**绝不省略**：

1. 执行 `git status` 以展示当前工作区状态。
2. 使用 `git add -A` 暂存 **所有** 更改（包括新增、修改、删除）。
3. 再次运行 `git status` 确认暂存区无误。
4. 执行 `git commit -m "提交信息"`。

## 禁止的行为
- 禁止使用 `git commit -a` 或 `git commit -am "..."` —— 它们不会包括未跟踪的新文件。
- 禁止只 `git add` 个别文件，除非我明确要求"只提交某个文件"。
- 禁止在子目录执行 `git add .` 导致其他目录的更改被遗漏。
- 提交前若存在合并冲突或钩子检查失败，必须报告我，不得强制跳过（如 `--no-verify`）。
