# KeyMacro - Windows 快捷键宏工具

## 概述
后台运行的快捷键宏工具。用户可自定义按键序列，设置间隔时间，绑定触发快捷键。

## 技术栈
- .NET 9.0 + WinForms
- Win32 API (RegisterHotKey, SendKeys)
- JSON 配置存储

## 项目结构
```
KeyMacro/
├── Program.cs               # 入口
├── Models/
│   └── MacroSequence.cs     # 数据模型 (序列 + 步骤)
├── Services/
│   ├── ConfigService.cs     # JSON 配置读写 (%APPDATA%\KeyMacro\config.json)
│   ├── HotkeyService.cs     # Win32 全局热键注册/监听
│   └── MacroPlayer.cs       # SendKeys 按键序列播放引擎
└── Forms/
    ├── MainForm.cs          # 主窗口 + 系统托盘
    └── SequenceEditor.cs    # 序列编辑器 + 热键录制对话框
```
## 功能目标约束
在“添加”功能中，我需要实现以下功能
新建序列
  └──序列名""+快捷键“”+是否启用开关+上移/下移开关+是否循环开关+如果循环间隔循环时间“”+编辑序列按键内容+保存
     └──按键/组合键“”+间隔延迟时间“”+按键/组合键“”+间隔延迟时间“”+...+保存按键
## 关键约定
- **序列（MacroSequence）**：由触发快捷键 + 多个步骤组成
- **步骤（MacroStep）**：类型（单键/组合键/文本）+ 按键值 + 延迟(ms)
- **_suppressEvents**：SequenceEditor 使用此标志防止 DataGridView 事件递归
- **config.json**：存储在 `%APPDATA%\KeyMacro\`，由 ConfigService 管理

## 构建与运行
```bash
dotnet run --project KeyMacro
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 架构要点
- `HotkeyService` 通过重写 `WndProc` 接收 `WM_HOTKEY` 消息
- `MacroPlayer.Play()` 是 async Task，包含 500ms 初始延迟（给用户松手时间）
- 主窗口关闭时隐藏到系统托盘，不退出进程
- 播放期间 `MacroPlayer.IsPlaying` 为 true 以阻止嵌套触发


## 内容修改完成后注意事项
- 1.修改进行前要咨询本次修改属于什么级别的更新，选项分别为：1.问题修复。2.小功能更新。3.大版本更新
- 1.修改完成后要对主界面的标题中的版本好进行迭代。如果此次内容属于问题修复，版本号增加0.01。如果此次内容属于问题修复，版本号增加0.1。如果此次内容属于问题修复，版本号增加1