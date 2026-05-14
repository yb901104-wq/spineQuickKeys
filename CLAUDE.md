# KeyMacro - Windows 快捷键宏工具

## 概述
后台运行的快捷键宏工具。用户可自定义按键序列，设置间隔时间，绑定触发快捷键。附带 Spine 热键文件编辑器，可直接修改 Spine 的 hotkey TXT 文件。

## 技术栈
- .NET 9.0 + WinForms
- Win32 API (RegisterHotKey, SendKeys)
- JSON 配置存储

## 项目结构
```
KeyMacro/
├── Program.cs               # 入口
├── Models/
│   ├── MacroSequence.cs     # 数据模型 (序列 + 步骤)
│   └── VirtualButton.cs     # 虚拟按键数据模型
├── Services/
│   ├── ConfigService.cs     # JSON 配置读写 (%APPDATA%\KeyMacro\config.json)
│   ├── HotkeyService.cs     # Win32 全局热键注册/监听
│   ├── MacroPlayer.cs       # SendKeys 按键序列播放引擎
│   ├── OperationLogger.cs   # 文件日志系统 (%APPDATA%\KeyMacro\logs\)
│   ├── SpineHotkeyService.cs # Spine TXT 文件解析/保存 + 按键名格式转换 + 中文注解
│   ├── VirtualButtonManager.cs # 虚拟按键列表管理
│   ├── VirtualKeyBindingManager.cs # 虚拟按键 ↔ 序列绑定
│   ├── VirtualLayoutSerializer.cs # 虚拟按键窗口布局持久化
│   └── VirtualLoopExecutor.cs # 循环按钮执行器
└── Forms/
    ├── MainForm.cs          # 主窗口 + 系统托盘
    ├── SequenceEditor.cs    # 序列编辑器 + 热键录制对话框
    ├── SpineHotkeyEditor.cs # Spine 热键 TXT 文件编辑窗口
    ├── VirtualKeyWindow.cs  # 虚拟按键浮动窗口
    └── VirtualButtonWidget.cs # 虚拟按钮自绘控件
```

## 关键约定
- **序列（MacroSequence）**：由触发快捷键 + 多个步骤组成，`LoopCount`（1=单次 >1=循环N次 0=无限）
- **步骤（MacroStep）**：类型（单键/组合键/文本）+ 按键值 + 延迟(ms)
- **_suppressEvents**：SequenceEditor 使用此标志防止 DataGridView 事件递归
- **config.json**：存储在 `%APPDATA%\KeyMacro\`，由 ConfigService 管理
- **Spine TXT 注解文件**：`{文件名}.txt.annotations.json`，存储中文备注，不污染源 TXT
- **Spine 按键格式**：TXT 文件使用 Spine 命名（如 `PERIOD`、`COMMA`），录制时自动转换 WinForms → Spine 格式

## 构建与运行
```bash
dotnet run --project KeyMacro
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishDir="bin/Release/publish"
# 若 exe 被占用: taskkill /f /im KeyMacro.exe 后再 publish
```

## 右键菜单体系
- **按钮右键菜单**（`OnWidgetContextMenu`）：修改按钮名称 / 绑定快捷键 / 按钮循环延迟（仅循环按钮）/ 删除当前按钮
- **空白区域右键菜单**（`BuildBlankMenu`）：增加按钮 / 删除所有按钮 / 置顶/取消置顶 / 透明度 / 按钮位置锁定/解锁 / 捕获/清除目标窗口 / 单排/多排 / 缩放(50-200%) / 保存/重置布局 / 窗口锁定/解锁 / 关闭窗口
- 按钮循环延迟支持自定义数值（通过 InputBox）

## 架构要点
- `HotkeyService` 通过重写 `WndProc` 接收 `WM_HOTKEY` 消息
- `MacroPlayer.Play()` 是 async Task，包含 500ms 初始延迟（给用户松手时间），循环由 `LoopCount` 控制
- `MacroPlayer.PlayToWindow()` 是 async Task，使用 PostMessage 直接向目标窗口发键盘消息，无初始延迟
- 主窗口关闭时隐藏到系统托盘，不退出进程
- 播放期间 `MacroPlayer.IsPlaying` 为 true 以阻止嵌套触发
- `SpineHotkeyService.ToSpineFormat()` 将 WinForms 按键名转回 Spine 格式，避免写回 TXT 后 Spine 无法识别

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
- "触发快捷键"列：绑定键盘热键时显示快捷键，绑定虚拟按键时显示"虚拟按键(按钮名)"
- 所有列均为固定宽度（`AutoSizeMode = None`），拖动分隔线时行为一致

## VkPickMode 绑定流程
- SequenceEditor 有两颗按钮录入触发快捷键：**键盘录入**（始终打开键盘录制窗口）和 **虚拟按键**（始终进入 VkPickMode）
- VkPickMode 带有黄色状态栏提示，支持 Esc 取消和"取消拾取"按钮
- 点击任意虚拟按钮 → 自动拾取关联虚拟按键名称（_txtVkBind）和触发快捷键（_txtHotkey，如有）
- `MainForm.SyncVkButtonBindings` 仅更新名称匹配的按钮，不破坏右键菜单建立的绑定
- `MainForm.RequestOpenVirtualKeys()` 可供 SequenceEditor 在 VK 窗口未打开时自动创建

## 虚拟按键窗口
- `FormBorderStyle = FixedSingle`，原生 Windows 标题栏（自带拖动、关闭、系统菜单）
- 标题栏文字：`[目标] 虚拟按键 (N)`，显示目标名和按钮数量
- 锁定：切换 `FormBorderStyle = None`，标题栏消失，窗口/按钮位置不动
- **无自绘工具栏、无自定义拖拽事件、无缩放手柄**
- **布局模式**：单排（所有按钮横排）/ 多排（自动换行），右键菜单切换
- **缩放**：右键菜单百分比预设（50/75/100/150/200%）+ 自定义输入（10-200%）
- **无拖拽缩放，无最小/最大尺寸限制**

## 虚拟按键布局算法
- 基于基础常量计算，不依赖控件运行时属性：
```
BASE_BTN_H = 48   BASE_GAP = 4   BASE_MARGIN = 10
BaseBtnWidth: SmallIcon=48  LargeIcon=96  LoopIcon=110

窗口宽 = margin + sum(各按钮实际宽) + (N-1)×gap + margin + 边框补偿
窗口高 = 标题栏(28) + margin + btnH + margin + 边框补偿
```
- 按钮间距 `gap`、边距 `margin` 随 ScaleFactor 等比缩放
- Panel.Padding 和 widget.Margin 同步更新，保证视觉一致性

## 虚拟按键右键菜单
- 按钮右键菜单顶部显示 `[ 按钮名 ]` 作为标题（禁用，仅展示），下方依次为修改名称/绑定快捷键/循环延迟/删除
- 空白区域右键菜单顶部为增加按钮选项，下方为窗口控制（置顶/透明度/锁定/目标窗口/单排多排/缩放/保存重置布局/关闭）

## 日志系统
- `OperationLogger` 是静态类，日志路径 `%APPDATA%\KeyMacro\logs\yyyy-MM-dd.log`
- 自动清理 7 天前的日志，单文件超 5MB 自动轮转
- 关键操作（播放序列、热键触发、配置读写、VkPickMode）均记录日志

## 版本管理与发布流程（必遵）

1. 修改完成后确认本次更新级别：问题修复 / 小功能更新 / 大版本更新 / 未修复
2. 修改后在 [MainForm.cs](KeyMacro/Forms/MainForm.cs#L24) 标题中迭代版本号：
   - 问题修复 → +0.01（如 1.2 → 1.21）并且抹去更更低位的数字
   - 小功能更新 → +0.1（如 1.2 → 1.3）并且抹去更更低位的数字
   - 大版本更新 → +1（如 1.2 → 2.0）并且抹去更更低位的数字
   - 未修复问题，暂不更改版本号
3. 总结并修改 CLAUDE.md   
4. 总结修改内容，询问是否提交 git（以当前版本号作为提交名称，并写明修改摘要）
5. 修改完成后导出一个单独的.exe应用供测试，如遇应用已开启导致无法修改就强行终止应用再尝试导出
# Git 提交规则（强制）

当你需要为我自动提交代码时，必须严格遵守以下流程，**绝不省略**：

1. 执行 `git status` 以展示当前工作区状态。
2. 使用 `git add -A` 暂存 **所有** 更改（包括新增、修改、删除）。
3. 再次运行 `git status` 确认暂存区无误。
4. 执行 `git commit -m "提交信息"`。

## 禁止的行为
- 禁止使用 `git commit -a` 或 `git commit -am "..."` —— 它们不会包括未跟踪的新文件。
- 禁止只 `git add` 个别文件，除非我明确要求“只提交某个文件”。
- 禁止在子目录执行 `git add .` 导致其他目录的更改被遗漏。
- 提交前若存在合并冲突或钩子检查失败，必须报告我，不得强制跳过（如 `--no-verify`）。