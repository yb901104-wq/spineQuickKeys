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
- **序列（MacroSequence）**：由触发快捷键 + 多个步骤组成
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
- **空白区域右键菜单**（`BuildBlankMenu`）：增加按钮 / 删除所有按钮 / 置顶/取消置顶 / 透明度 / 按钮位置锁定/解锁 / 保存/重置布局 / 窗口锁定/解锁
- 按钮循环延迟支持自定义数值（通过 InputBox）

## 架构要点
- `HotkeyService` 通过重写 `WndProc` 接收 `WM_HOTKEY` 消息
- `MacroPlayer.Play()` 是 async Task，包含 500ms 初始延迟（给用户松手时间）
- 主窗口关闭时隐藏到系统托盘，不退出进程
- 播放期间 `MacroPlayer.IsPlaying` 为 true 以阻止嵌套触发
- `SpineHotkeyService.ToSpineFormat()` 将 WinForms 按键名转回 Spine 格式，避免写回 TXT 后 Spine 无法识别

## 版本管理与发布流程（必遵）

**在开始任何代码修改之前，必须先询问用户本次更新级别，得到回复后方可开始编码。**

1. 修改前确认本次更新级别：问题修复 / 小功能更新 / 大版本更新 / 未修复
2. 修改后在 [MainForm.cs](KeyMacro/Forms/MainForm.cs#L24) 标题中迭代版本号：
   - 问题修复 → +0.01（如 1.2 → 1.21）并且抹去更更低位的数字
   - 小功能更新 → +0.1（如 1.2 → 1.3）并且抹去更更低位的数字
   - 大版本更新 → +1（如 1.2 → 2.0）并且抹去更更低位的数字
   - 未修复问题，暂不更改版本号
3. 修改完成后编译单独的 .exe 供测试
4. 总结修改内容，询问是否提交 git（以当前版本号作为提交名称）
5. 总结并修改 CLAUDE.md
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